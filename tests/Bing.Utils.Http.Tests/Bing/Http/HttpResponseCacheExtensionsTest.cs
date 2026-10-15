using System.Net;
using System.Net.Http;
using System.Threading;
using Bing.Http;
using Bing.Http.Caching;
using Bing.Http.Clients;

namespace Bing.Utils.Http.Tests.Bing.Http;



/// <summary>
/// 验证显式 HTTP 响应缓存接入扩展。
/// </summary>
public class HttpResponseCacheExtensionsTest
{
    /// <summary>
    /// 验证缓存命中复用同一响应快照及实体标签，且不再次调用工厂。
    /// </summary>
    [Fact]
    public async Task ExecuteCached_UsesOneFactoryAndPreservesSnapshot()
    {
        var cache = new InMemoryHttpResponseCache<string>();
        var calls = 0;

        var first = await cache.ExecuteCachedAsync("item:1", TimeSpan.FromMinutes(1), () =>
        {
            calls++;
            return Task.FromResult(Response("value"));
        });
        var second = await cache.ExecuteCachedAsync("item:1", TimeSpan.FromMinutes(1), () =>
        {
            calls++;
            return Task.FromResult(Response("unexpected"));
        });

        calls.ShouldBe(1);
        second.ShouldBeSameAs(first);
        second.Headers["ETag"].Single().ShouldBe("\"v1\"");
    }



    /// <summary>
    /// 验证显式允许认证请求缓存后仅在未命中时发送请求。
    /// </summary>
    [Fact]
    public async Task GetCached_SendsOnlyOnMissAndSupportsExplicitAuthenticatedOptIn()
    {
        var sends = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("value")
            };
        }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/item");
        var cache = new InMemoryHttpResponseCache<string>();

        var first = await request.GetCachedAsync(cache, "item:1", TimeSpan.FromMinutes(1),
            requestContainsAuthorization: true, allowAuthenticated: true);
        var second = await request.GetCachedAsync(cache, "item:1", TimeSpan.FromMinutes(1),
            requestContainsAuthorization: true, allowAuthenticated: true);

        sends.ShouldBe(1);
        second.ShouldBeSameAs(first);
        second.Data.ShouldBe("value");
    }



    /// <summary>
    /// 验证未显式允许缓存认证请求时在发送前拒绝执行。
    /// </summary>
    [Fact]
    public async Task GetCached_RejectsAuthenticatedRequestWithoutExplicitOptIn()
    {
        var sends = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/item");
        var cache = new InMemoryHttpResponseCache<string>();

        await Should.ThrowAsync<InvalidOperationException>(() => request.GetCachedAsync(
            cache, "private:1", TimeSpan.FromMinutes(1), requestContainsAuthorization: true));
        sends.ShouldBe(0);
    }



    /// <summary>
    /// 创建包含固定实体标签的字符串响应样例。
    /// </summary>
    /// <param name="value">同时用作结果数据及原始正文的字符串。</param>
    /// <returns>实体标签为 <c>v1</c> 的成功响应快照。</returns>
    private static HttpResponseResult<string> Response(string value)
        => new HttpResponseResult<string>(HttpStatusCode.OK,
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["ETag"] = new[] { "\"v1\"" }
            },
            new Dictionary<string, IReadOnlyList<string>>(), value, value);



    /// <summary>
    /// 根据同步回调生成 HTTP 响应的消息处理器。
    /// </summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        /// <summary>
        /// 测试处理流程使用的委托。
        /// </summary>
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _send;



        /// <summary>
        /// 初始化一个 <see cref="StubHandler"/> 类型的实例。
        /// </summary>
        /// <param name="send">根据请求生成响应的委托。</param>
        public StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> send)
            => _send = send;

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_send(request, cancellationToken));
    }
}
