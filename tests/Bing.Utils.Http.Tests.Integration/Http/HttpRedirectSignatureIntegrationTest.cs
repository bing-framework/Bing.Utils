using System.Net.Http;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;

namespace Bing.Utils.Http.Tests.Integration.Http;



/// <summary>
/// 验证真实自动重定向对请求签名拦截器的调用边界。
/// </summary>
public class HttpRedirectSignatureIntegrationTest
{
    /// <summary>
    /// 验证重定向后的请求不会执行签名拦截器。
    /// </summary>
    [Fact]
    public async Task AutomaticRedirect_DoesNotRunSigningInterceptorForRedirectedRequest()
    {
        var serverPaths = new List<string>();
        using var host = new WebHostBuilder()
            .UseKestrel()
            .UseUrls("http://127.0.0.1:0")
            .Configure(app => app.Run(async context =>
            {
                serverPaths.Add(context.Request.Path.Value);
                if (context.Request.Path == "/start")
                {
                    context.Response.Redirect("/final");
                    return;
                }
                await context.Response.WriteAsync("final");
            }))
            .Build();
        await host.StartAsync();
        var address = host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.Single();
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
        var signer = new PathSigningInterceptor();

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, address + "/start")
            .UseInterceptor(signer)
            .GetResultAsync();

        result.ShouldBe("final");
        serverPaths.ShouldBe(new[] { "/start", "/final" });
        signer.SignedPaths.ShouldBe(new[] { "/start" });
    }



    /// <summary>
    /// 根据请求路径生成并写入签名信息的拦截器。
    /// </summary>
    private sealed class PathSigningInterceptor : HttpRequestInterceptor
    {
        /// <summary>
        /// 获取已执行签名拦截的请求路径列表。
        /// </summary>
        public List<string> SignedPaths { get; } = new();

        /// <inheritdoc />
        public override Task BeforeSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            SignedPaths.Add(context.Request.RequestUri.AbsolutePath);
            context.Request.Headers.TryAddWithoutValidation("X-Signature-Path", context.Request.RequestUri.AbsolutePath);
            return Task.CompletedTask;
        }
    }
}
