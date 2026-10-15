using System.Net.Http;
using System.Threading;
using Bing.Http.Clients;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// 验证 HTTP 客户端与委托处理器协作完成服务器错误重试。
/// </summary>
[Trait("Bing.Http", "HttpClientService.RetryIntegration")]
public class HttpClientServiceRetryIntegrationTest
{
    /// <summary>
    /// 承载重试测试端点的测试主机。
    /// </summary>
    private readonly IHost _host;

    /// <summary>
    /// 初始化一个 <see cref="HttpClientServiceRetryIntegrationTest"/> 类型的实例。
    /// </summary>
    /// <param name="host">承载集成测试端点的主机。</param>
    public HttpClientServiceRetryIntegrationTest(IHost host)
    {
        _host = host;
    }

    /// <summary>
    /// 验证服务器首次返回错误后重试一次并得到成功结果。
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
    /// 在服务器返回 5xx 时按次数限制重试的测试处理器。
    /// </summary>
    private sealed class RetryOnServerErrorHandler : DelegatingHandler
    {
        /// <summary>
        /// 首次发送失败后允许的最大重试次数。
        /// </summary>
        private readonly int _maxRetryCount;

        /// <summary>
        /// 初始化一个 <see cref="RetryOnServerErrorHandler"/> 类型的实例。
        /// </summary>
        /// <param name="maxRetryCount">首次发送失败后允许的最大重试次数。</param>
        public RetryOnServerErrorHandler(int maxRetryCount)
        {
            _maxRetryCount = maxRetryCount;
        }

        /// <summary>
        /// 获取或设置处理器累计发送请求的次数。
        /// </summary>
        public int SendCount { get; private set; }

        /// <inheritdoc />
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

        /// <summary>
        /// 复制请求方法、地址、版本及请求头以用于无正文重试。
        /// </summary>
        /// <param name="request">待复制的原始请求。</param>
        /// <returns>不包含请求正文的新请求实例。</returns>
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
