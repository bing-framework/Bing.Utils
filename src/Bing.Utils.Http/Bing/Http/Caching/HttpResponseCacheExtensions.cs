using Bing.Http.Clients;

namespace Bing.Http.Caching;

/// <summary>

/// 为 HTTP 响应缓存提供显式启用的请求和回源扩展。

/// </summary>
public static class HttpResponseCacheExtensions
{
    /// <summary>
    /// 执行响应工厂并缓存不可释放的响应快照。
    /// </summary>
    /// <typeparam name="TResult">响应转换后的数据类型。</typeparam>
    /// <param name="cache">要使用的响应缓存。</param>
    /// <param name="cacheKey">调用方定义的稳定缓存键。</param>
    /// <param name="ttl">回源响应的缓存有效期。</param>
    /// <param name="responseFactory">缓存未命中时执行的响应工厂。</param>
    /// <param name="requestContainsAuthorization">请求是否包含认证信息。</param>
    /// <param name="allowAuthenticated">是否明确允许缓存认证请求。</param>
    /// <returns>缓存命中或回源得到的响应快照。</returns>
    /// <remarks>缓存只保存状态码、头快照、转换结果和可选原始正文，不保存可释放的 <see cref="HttpResponseMessage"/>。</remarks>
    public static Task<HttpResponseResult<TResult>> ExecuteCachedAsync<TResult>(
        this IHttpResponseCache<TResult> cache, string cacheKey, TimeSpan ttl,
        Func<Task<HttpResponseResult<TResult>>> responseFactory,
        bool requestContainsAuthorization = false, bool allowAuthenticated = false)
        where TResult : class
    {
        if (cache == null) throw new ArgumentNullException(nameof(cache));
        if (responseFactory == null) throw new ArgumentNullException(nameof(responseFactory));
        return cache.GetOrCreateAsync(cacheKey, ttl, responseFactory,
            requestContainsAuthorization, allowAuthenticated);
    }

    /// <summary>

    /// 发送请求并将响应快照按指定键显式缓存。

    /// </summary>
    /// <typeparam name="TResult">响应转换后的数据类型。</typeparam>
    /// <param name="request">要发送的请求。</param>
    /// <param name="cache">要使用的响应缓存。</param>
    /// <param name="cacheKey">调用方定义的稳定缓存键。</param>
    /// <param name="ttl">回源响应的缓存有效期。</param>
    /// <param name="requestContainsAuthorization">请求是否包含认证信息。</param>
    /// <param name="allowAuthenticated">是否明确允许缓存认证请求。</param>
    /// <param name="includeRawContent">是否保留响应正文文本快照。</param>
    /// <param name="cancellationToken">用于回源请求的取消令牌。</param>
    /// <returns>缓存命中或回源得到的响应快照。</returns>
    /// <remarks>此方法不会自动启用缓存；调用方必须显式传入缓存实例和缓存键。</remarks>
    public static Task<HttpResponseResult<TResult>> GetCachedAsync<TResult>(
        this IHttpRequest<TResult> request, IHttpResponseCache<TResult> cache, string cacheKey, TimeSpan ttl,
        bool requestContainsAuthorization = false, bool allowAuthenticated = false,
        bool includeRawContent = false, CancellationToken cancellationToken = default)
        where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (cache == null) throw new ArgumentNullException(nameof(cache));
        return cache.ExecuteCachedAsync(cacheKey, ttl,
            () => request.GetResponseAsync(includeRawContent, cancellationToken),
            requestContainsAuthorization, allowAuthenticated);
    }

    /// <summary>

    /// 发送请求并将文件缓存响应按指定键显式缓存，同时支持取消锁等待。

    /// </summary>
    /// <remarks>此重载面向 byte[] 响应；文件缓存只保存不可释放的响应快照。</remarks>
    /// <param name="request">要发送的请求。</param>
    /// <param name="cache">要使用的文件缓存。</param>
    /// <param name="cacheKey">调用方定义的稳定缓存键。</param>
    /// <param name="ttl">回源响应的缓存有效期。</param>
    /// <param name="requestContainsAuthorization">请求是否包含认证信息。</param>
    /// <param name="allowAuthenticated">是否明确允许缓存认证请求。</param>
    /// <param name="includeRawContent">是否保留响应正文文本快照。</param>
    /// <param name="cancellationToken">用于取消锁等待、回源和缓存提交的令牌。</param>
    /// <returns>缓存命中或回源得到的响应快照。</returns>
    public static Task<HttpResponseResult<byte[]>> GetCachedAsync(
        this IHttpRequest<byte[]> request, FileHttpResponseCache cache, string cacheKey, TimeSpan ttl,
        bool requestContainsAuthorization = false, bool allowAuthenticated = false,
        bool includeRawContent = false, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (cache == null) throw new ArgumentNullException(nameof(cache));
        return cache.GetOrCreateAsync(cacheKey, ttl,
            () => request.GetResponseAsync(includeRawContent, cancellationToken), cancellationToken,
            requestContainsAuthorization, allowAuthenticated);
    }
}
