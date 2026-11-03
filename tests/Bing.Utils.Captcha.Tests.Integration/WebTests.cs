using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bing.Captcha;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public sealed class WebTests
{
    private sealed class Store : ICaptchaStore, IDisposable
    {
        private readonly MemoryCaptchaStore _inner = new();
        public bool FailWrites;
        public bool FailTakes;
        public Task ReplaceAsync(CaptchaContext context, CaptchaEntry entry, CancellationToken token=default)
            => FailWrites ? throw new IOException("storage offline") : _inner.ReplaceAsync(context,entry,token);
        public Task<CaptchaEntry?> TryTakeAsync(CaptchaContext context,string id,CancellationToken token=default)
            => FailTakes ? throw new IOException("storage offline") : _inner.TryTakeAsync(context,id,token);
        public void Dispose() => _inner.Dispose();
    }
    private sealed class LoopbackFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, continuation) => { context.Connection.RemoteIpAddress=IPAddress.Loopback; return continuation(); });
            next(app);
        };
    }
    private sealed class Renderer : ICaptchaRenderer
    {
        public string Answer = "";
        public bool Fail;
        public CaptchaImage Render(string text,CancellationToken cancellationToken=default)
        { if(Fail) throw new IOException("renderer offline"); Answer=text;return new CaptchaImage(new byte[]{137,80,78,71,13,10,26,10},160,60); }
    }
    private sealed class Factory : WebApplicationFactory<Program>
    {
        public readonly Renderer Renderer = new();
        public readonly Store Store = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(Path.GetFullPath("../../../../../samples/CaptchaWeb",AppContext.BaseDirectory));
            builder.ConfigureTestServices(services=>
            {
                services.RemoveAll<ICaptchaRenderer>(); services.AddSingleton<ICaptchaRenderer>(Renderer);
                services.RemoveAll<ICaptchaStore>(); services.AddSingleton<ICaptchaStore>(_=>Store);
                services.RemoveAll<ICaptchaKeyProvider>(); services.AddSingleton<ICaptchaKeyProvider>(new StaticCaptchaKeyProvider("test",new[]{new CaptchaKey("test",new byte[32])}));
                services.AddSingleton<IStartupFilter,LoopbackFilter>();
            });
        }
        public HttpClient Client()=>CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost"),HandleCookies=true});
    }
    private static async Task Session(HttpClient client)
    {
        var response=await client.GetAsync("/api/session");response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.CacheControl!.NoStore);
        var json=await response.Content.ReadFromJsonAsync<JsonElement>();client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",json.GetProperty("token").GetString());
        Assert.Contains(response.Headers.GetValues("Set-Cookie"),c=>c.Contains("captcha.binding=")&&c.Contains("secure")&&c.Contains("httponly")&&c.Contains("samesite=strict"));
    }
    private static async Task<string> Create(HttpClient client,string flow="form")
    {
        var response=await client.PostAsJsonAsync("/api/captchas",new {flowId=flow});response.EnsureSuccessStatusCode();
        Assert.Equal("application/json",response.Content.Headers.ContentType!.MediaType);
        Assert.True(response.Headers.CacheControl!.NoStore);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("captchaId").GetString()!;
    }
    [Fact]
    public async Task FullFlow_AntiforgeryBindingRefreshAndReplay()
    {
        using var factory=new Factory();using var first=factory.Client();using var second=factory.Client();
        await Session(first);await Session(second);
        using var invalidToken = new HttpRequestMessage(HttpMethod.Post,"/api/captchas") { Content=JsonContent.Create(new {flowId="form"}) };
        invalidToken.Headers.Add("X-CSRF-TOKEN","invalid");
        using var rejected=await first.SendAsync(invalidToken);
        Assert.Equal(HttpStatusCode.BadRequest,rejected.StatusCode);
        Assert.True(rejected.Headers.CacheControl!.NoStore);
        var old=await Create(first);var current=await Create(first);var answer=factory.Renderer.Answer;
        var body=new {flowId="form",captchaId=current,answer};
        Assert.Equal(HttpStatusCode.BadRequest,(await second.PostAsJsonAsync("/api/demo/submit",body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await first.PostAsJsonAsync("/api/demo/submit",new{flowId="other",captchaId=current,answer})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await first.PostAsJsonAsync("/api/demo/submit",new{flowId="form",captchaId=old,answer})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await first.PostAsJsonAsync("/api/demo/submit",body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await first.PostAsJsonAsync("/api/demo/submit",body)).StatusCode);
    }
    [Fact]
    public async Task WrongAnswer_RequiresRefresh_AndFlowIdsCannotBypassRateLimit()
    {
        using var factory=new Factory();using var client=factory.Client();await Session(client);
        var id=await Create(client);var answer=factory.Renderer.Answer;
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/demo/submit",new{flowId="form",captchaId=id,answer="WRONG-ANSWER"})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/demo/submit",new{flowId="form",captchaId=id,answer})).StatusCode);
        id=await Create(client);
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/demo/submit",new{flowId="form",captchaId=id,answer=factory.Renderer.Answer})).StatusCode);
        HttpResponseMessage? last=null;
        for(var i=0;i<25;i++) {last?.Dispose();last=await client.PostAsJsonAsync("/api/captchas",new{flowId="new-"+i});}
        Assert.Equal((HttpStatusCode)429,last!.StatusCode);
        Assert.True(last.Headers.CacheControl!.NoStore);last.Dispose();
    }
    [Fact]
    public async Task RenderingFailure_ReturnsServiceError_AndKeepsPreviousChallenge()
    {
        using var factory=new Factory();using var client=factory.Client();await Session(client);
        var id=await Create(client);var answer=factory.Renderer.Answer;factory.Renderer.Fail=true;
        var failure=await client.PostAsJsonAsync("/api/captchas",new {flowId="form"});
        Assert.Equal(HttpStatusCode.ServiceUnavailable,failure.StatusCode);
        Assert.True(failure.Headers.CacheControl!.NoStore);
        Assert.Equal("captcha_unavailable",(await failure.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString());
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/demo/submit",new {flowId="form",captchaId=id,answer})).StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StorageFailure_RejectsOperationWithServiceError(bool duringCreate)
    {
        using var factory=new Factory();using var client=factory.Client();await Session(client);
        var id=await Create(client);var answer=factory.Renderer.Answer;
        factory.Store.FailWrites=duringCreate;factory.Store.FailTakes=!duringCreate;
        using var response=duringCreate
            ? await client.PostAsJsonAsync("/api/captchas",new {flowId="form"})
            : await client.PostAsJsonAsync("/api/demo/submit",new {flowId="form",captchaId=id,answer});
        Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);
        Assert.Equal("application/json",response.Content.Headers.ContentType!.MediaType);
        Assert.True(response.Headers.CacheControl!.NoStore);
        var body=await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("captcha_unavailable",body.GetProperty("error").GetString());
        Assert.False(body.TryGetProperty("message",out _));
        factory.Store.FailWrites=false;factory.Store.FailTakes=false;
        // 本故障桩在存储操作前失败；恢复后仍可使用原挑战。
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/demo/submit",new {flowId="form",captchaId=id,answer})).StatusCode);
    }

    [Fact]
    public async Task IpLimit_AppliesAcrossSessions_WithoutHittingSessionLimit()
    {
        using var factory=new Factory();
        var clients=Enumerable.Range(0,4).Select(_=>factory.Client()).ToArray();
        try
        {
            foreach(var client in clients)
            {
                await Session(client);
                for(var i=0;i<14;i++)
                {
                    using var response=await client.GetAsync("/api/session");
                    Assert.Equal(HttpStatusCode.OK,response.StatusCode);
                }
            }
            // 四个会话各 15 次，均未达到单会话 20 次限制，但共享 IP 已满 60 次。
            using var fresh=factory.Client();using var limited=await fresh.GetAsync("/api/session");
            Assert.Equal((HttpStatusCode)429,limited.StatusCode);
            Assert.True(limited.Headers.CacheControl!.NoStore);
        }
        finally { foreach(var client in clients) client.Dispose(); }
    }
}
