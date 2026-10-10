using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>

/// 验证可选响应能力与 TestServer HTTP 链路集成。

/// </summary>
public class HttpCapabilityExtensionsIntegrationTest
{
    /// <summary>
    /// 承载集成测试服务的宿主实例。
    /// </summary>
    private readonly IHost _host;
    /// <summary>
    /// 初始化一个<see cref="HttpCapabilityExtensionsIntegrationTest"/>类型的实例。
    /// </summary>
    /// <param name="host">用于测试的依赖注入应用实例。</param>
    public HttpCapabilityExtensionsIntegrationTest(IHost host) => _host = host;

    /// <summary>

    /// 验证保留异常元数据并读取原始正文。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_PreservesErrorMetadataAndRawBody()
    {
        using var client = _host.GetTestServer().CreateClient();
        var service = new HttpClientService().SetHttpClient(client);

        var result = await service.Get("/api/test7/status/418").GetResponseAsync(includeRawContent: true);

        result.StatusCode.ShouldBe((HttpStatusCode)418);
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.Data.ShouldBeNull();
        result.RawContent.ShouldContain("error:418");
        result.ContentHeaders["Content-Type"].Single().ShouldContain("application/json");
    }

    /// <summary>

    /// 验证 HEAD 和 OPTIONS 请求经过现有客户端管线发送。

    /// </summary>
    [Fact]
    public async Task RequestExtensions_SendHeadAndOptionsThroughExistingClientPipeline()
    {
        var observed = new List<string>();
        using var client = new HttpClient(new ObservingHandler(
            (request, _) =>
            {
                observed.Add(request.Method.Method);
                return Task.CompletedTask;
            })
        {
            InnerHandler = _host.GetTestServer().CreateHandler()
        })
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var service = new HttpClientService().SetHttpClient(client);

        await service.Head("/api/test7/delay/0").GetResultAsync();
        await service.Options("/api/test7/delay/0").GetResultAsync();

        observed.ShouldBe(new[] { "HEAD", "OPTIONS" });
    }

    /// <summary>

    /// 验证命名客户端的管线配置生效并发送 JSON 正文。

    /// </summary>
    [Fact]
    public async Task PatchExtension_UsesNamedClientPipelineAndSendsJsonBody()
    {
        var observed = new List<string>();
        string body = null;
        var services = new ServiceCollection();
        services.AddHttpClient("capability", client => client.BaseAddress = new Uri("http://localhost/"))
            .AddHttpMessageHandler(() => new ObservingHandler(async (request, _) =>
            {
                observed.Add(request.Method.Method + ":" + request.Content?.Headers.ContentType?.MediaType);
                body = await request.Content.ReadAsStringAsync();
            }))
            .ConfigurePrimaryHttpMessageHandler(() => _host.GetTestServer().CreateHandler());
        using var provider = services.BuildServiceProvider();
        var service = new HttpClientService(provider.GetRequiredService<IHttpClientFactory>());

        var response = await service.Patch<string>("/api/test7/compose/42", new { Name = "Ada" })
            .HttpClientName("capability")
            .GetResponseAsync(includeRawContent: true);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        observed.ShouldBe(new[] { "PATCH:application/json" });
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("Name").GetString().ShouldBe("Ada");
    }

    /// <summary>

    /// 接收 HTTP 请求并调用观察回调的消息处理器。

    /// </summary>
    private sealed class ObservingHandler : DelegatingHandler
    {
        /// <summary>
        /// 测试处理流程使用的委托。
        /// </summary>
        private readonly Func<HttpRequestMessage, CancellationToken, Task> _observe;
        /// <summary>
        /// 初始化一个<see cref="ObservingHandler"/>类型的实例。
        /// </summary>
        /// <param name="observe">接收请求后执行的观察回调。</param>
        public ObservingHandler(Func<HttpRequestMessage, CancellationToken, Task> observe) => _observe = observe;
        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await _observe(request, cancellationToken);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
