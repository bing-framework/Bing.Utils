using System.Net.Http;
using System.Threading;
using Bing.Http.Clients;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// Http客户端测试 - 重试协作链路
/// </summary>
[Trait("Bing.Http", "HttpClientService.RetryIntegration")]
public class HttpClientServiceRetryIntegrationTest
{
    private readonly IHost _host;

    /// <summary>
    /// 初始化测试类
    /// </summary>
    public HttpClientServiceRetryIntegrationTest(IHost host)
    {
        _host = host;
    }

    /// <summary>
    /// 测试 - 通过真实DelegatingHandler重试链路实现500后重试成功
    /// </summary>
    [Fact]
    public async Task Test_Get_RetryHandler_RetriesAndSucceeds_1()
    {
        var retryHandler = new RetryOnServerErrorHandler(maxRetryCount: 1)
        {
            InnerHandler = _host.GetTestServer().CreateHandler()
        };

        using var client = new HttpClient(retryHandler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var httpClient = new HttpClientService().SetHttpClient(client);
        var requestKey = Guid.NewGuid().ToString("N");
        var result = await httpClient.Get($"/api/test7/retry-once/{requestKey}").GetResultAsync();

        result.ShouldBe("ok:2");
        retryHandler.SendCount.ShouldBe(2);
    }

    /// <summary>
    /// 仅用于集成测试的简单重试处理器：当响应为5xx时重试
    /// </summary>
    private sealed class RetryOnServerErrorHandler : DelegatingHandler
    {
        private readonly int _maxRetryCount;

        public RetryOnServerErrorHandler(int maxRetryCount)
        {
            _maxRetryCount = maxRetryCount;
        }

        public int SendCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var original = request;
            for (var attempt = 0; ; attempt++)
            {
                var current = attempt == 0 ? request : CloneRequestWithoutBody(original);
                SendCount++;
                var response = await base.SendAsync(current, cancellationToken);
                if ((int)response.StatusCode < 500 || attempt >= _maxRetryCount)
                    return response;
                response.Dispose();
            }
        }

        private static HttpRequestMessage CloneRequestWithoutBody(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            clone.Version = request.Version;
            return clone;
        }
    }
}
