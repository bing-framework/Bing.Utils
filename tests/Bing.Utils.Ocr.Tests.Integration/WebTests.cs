using Bing.Ocr;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkiaSharp;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Bing.Utils.Ocr.Tests.Integration;

public class WebTests
{
    [Fact]
    public async Task Upload_UsesRealPreprocessorAndReturnsMappedLines()
    {
        using var factory=new Factory(); using var client=factory.CreateClient();
        var token=await Session(client);
        using var form=Form(Png(),"{\"region\":{\"x\":4,\"y\":2,\"width\":8,\"height\":6},\"rotationDegrees\":90,\"scale\":0.5}");
        using var request=new HttpRequestMessage(HttpMethod.Post,"/api/ocr"){Content=form};
        request.Headers.Add("X-CSRF-TOKEN",token);
        using var response=await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("test 中文",doc.RootElement.GetProperty("text").GetString());
        Assert.Equal(16,doc.RootElement.GetProperty("imageWidth").GetInt32());
        Assert.Equal(10,doc.RootElement.GetProperty("imageHeight").GetInt32());
        var point=doc.RootElement.GetProperty("lines")[0].GetProperty("polygon")[0];
        Assert.Equal(4,point.GetProperty("x").GetDouble()); Assert.Equal(8,point.GetProperty("y").GetDouble());
        Assert.Equal("no-store",response.Headers.CacheControl!.ToString());
        Assert.Equal(1,factory.Provider.Calls);
    }
    [Fact]
    public async Task MissingCsrfAndUnsupportedImage_DoNotInvokeProvider()
    {
        using var factory=new Factory(); using var client=factory.CreateClient();
        using(var form=Form(Png())) Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync("/api/ocr",form)).StatusCode);
        var token=await Session(client);
        using var request=new HttpRequestMessage(HttpMethod.Post,"/api/ocr"){Content=Form(Encoding.UTF8.GetBytes("%PDF-1.7"))};
        request.Headers.Add("X-CSRF-TOKEN",token);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.SendAsync(request)).StatusCode);
        Assert.Equal(0,factory.Provider.Calls);
    }
    [Theory]
    [InlineData("failure",HttpStatusCode.ServiceUnavailable)] [InlineData("busy",HttpStatusCode.TooManyRequests)]
    [InlineData("timeout",HttpStatusCode.GatewayTimeout)]
    public async Task ServiceFailures_ReturnStableErrors(string failure,HttpStatusCode status)
    {
        using var factory=new Factory(); factory.Provider.Failure=failure;
        using var client=factory.CreateClient(); var token=await Session(client);
        using var request=new HttpRequestMessage(HttpMethod.Post,"/api/ocr"){Content=Form(Png())};
        request.Headers.Add("X-CSRF-TOKEN",token);
        using var response=await client.SendAsync(request);
        Assert.Equal(status,response.StatusCode);
        Assert.DoesNotContain("internal-secret",await response.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task DuplicateImage_AndInvalidOptions_AreRejected()
    {
        using var factory=new Factory(); using var client=factory.CreateClient(); var token=await Session(client);
        using(var form=Form(Png())) {
            form.Add(new ByteArrayContent(Png()),"image","other.png");
            using var request=new HttpRequestMessage(HttpMethod.Post,"/api/ocr"){Content=form};
            request.Headers.Add("X-CSRF-TOKEN",token);
            Assert.Equal(HttpStatusCode.BadRequest,(await client.SendAsync(request)).StatusCode);
        }
        using var bad=new HttpRequestMessage(HttpMethod.Post,"/api/ocr"){Content=Form(Png(),"{\"rotationDegrees\":45}")};
        bad.Headers.Add("X-CSRF-TOKEN",token);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.SendAsync(bad)).StatusCode);
        Assert.Equal(0,factory.Provider.Calls);
    }

    private static async Task<string> Session(HttpClient client)
    {
        using var response=await client.GetAsync("/api/session"); response.EnsureSuccessStatusCode();
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("token").GetString()!;
    }
    private static MultipartFormDataContent Form(byte[] bytes,string options="{}")
    {
        var content=new MultipartFormDataContent();
        content.Add(new ByteArrayContent(bytes),"image","sample.png");
        content.Add(new StringContent(options,Encoding.UTF8),"options"); return content;
    }
    private static byte[] Png()
    {
        using var bitmap=new SKBitmap(16,10); bitmap.Erase(SKColors.White);
        using var image=SKImage.FromBitmap(bitmap); using var data=image.Encode(SKEncodedImageFormat.Png,100);
        return data.ToArray();
    }
    private sealed class Factory : WebApplicationFactory<global::Program>
    {
        public FakeProvider Provider { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services=>{services.RemoveAll<IOcrProvider>();services.AddSingleton<IOcrProvider>(Provider);});
        }
    }
    private sealed class FakeProvider : IOcrProvider
    {
        public int Calls; public string? Failure; public string Name=>"web-test";
        public void ValidateOptions(OcrOptions options) { }
        public Task<OcrProviderResult> RecognizeAsync(PreparedOcrImage image,OcrOptions options,CancellationToken ct=default)
        {
            Calls++;
            if(Failure=="failure") throw new OcrProviderException("internal-secret");
            if(Failure=="busy") throw new OcrBusyException();
            if(Failure=="timeout") throw new OcrTimeoutException();
            return Task.FromResult(new OcrProviderResult(new[]{new OcrLine("test 中文",new OcrPoint[]{new(0,0),new(image.Width,0),new(image.Width,image.Height),new(0,image.Height)},.5)}));
        }
    }
}
