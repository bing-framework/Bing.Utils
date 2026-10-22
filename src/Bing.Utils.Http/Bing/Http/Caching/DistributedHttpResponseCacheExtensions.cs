namespace Bing.Http.Caching;

/// <summary>
/// 为宿主提供显式启用的分布式 HTTP 响应缓存入口。
/// </summary>
/// <remarks>该扩展只接入 byte[] 响应；回源并发协调、租约和跨节点一致性由存储契约实现负责。</remarks>
public static class DistributedHttpResponseCacheExtensions
{
    /// <summary>
    /// 执行回源并将 byte[] 响应快照写入分布式存储。
    /// </summary>
    /// <param name="store">宿主提供的分布式快照存储。</param>
    /// <param name="cacheKey">全局稳定缓存键。</param>
    /// <param name="ttl">快照有效期。</param>
    /// <param name="responseFactory">缓存未命中时的回源委托。</param>
    /// <param name="requestContainsAuthorization">请求是否包含认证信息。</param>
    /// <param name="allowAuthenticated">是否明确允许缓存认证请求。</param>
    /// <param name="cancellationToken">取消读取、回源或写入的令牌。</param>
    /// <param name="namespaceName">可选的命名空间名称；需要存储支持命名空间版本契约。</param>
    /// <returns>缓存命中或回源得到的 HTTP 响应快照。</returns>
    public static async Task<HttpResponseResult<byte[]>> ExecuteCachedAsync(
        this IDistributedHttpResponseStore store, string cacheKey, TimeSpan ttl,
        Func<Task<HttpResponseResult<byte[]>>> responseFactory,
        bool requestContainsAuthorization = false, bool allowAuthenticated = false,
        CancellationToken cancellationToken = default, string namespaceName = null)
    {
        Validate(store, cacheKey, ttl, requestContainsAuthorization, allowAuthenticated);
        var effectiveKey = await ResolveNamespaceKeyAsync(store, cacheKey, namespaceName, cancellationToken)
            .ConfigureAwait(false);
        var cached = await store.GetAsync(effectiveKey, cancellationToken).ConfigureAwait(false);
        if (cached != null)
            return ToResponse(cached);

        if (store is IDistributedHttpResponseLeaseStore leaseStore)
        {
            var lease = await WaitForLeaseAsync(leaseStore, store, effectiveKey, ttl, cancellationToken)
                .ConfigureAwait(false);
            if (lease != null)
            {
                try
                {
                    cached = await store.GetAsync(effectiveKey, cancellationToken).ConfigureAwait(false);
                    if (cached != null) return ToResponse(cached);
                    return await ExecuteOriginAsync(store, effectiveKey, ttl, responseFactory,
                        cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    await lease.ReleaseAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }

            cached = await store.GetAsync(effectiveKey, cancellationToken).ConfigureAwait(false);
            if (cached != null) return ToResponse(cached);
        }

        return await ExecuteOriginAsync(store, effectiveKey, ttl, responseFactory, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 读取分布式快照，未命中时执行 byte[] 响应回源。
    /// </summary>
    /// <param name="store">宿主提供的分布式快照存储。</param>
    /// <param name="cacheKey">全局稳定缓存键。</param>
    /// <param name="ttl">快照有效期。</param>
    /// <param name="responseFactory">缓存未命中时的回源委托。</param>
    /// <param name="requestContainsAuthorization">请求是否包含认证信息。</param>
    /// <param name="allowAuthenticated">是否明确允许缓存认证请求。</param>
    /// <param name="cancellationToken">取消读取、回源或写入的令牌。</param>
    /// <param name="namespaceName">可选的命名空间名称；需要存储支持命名空间版本契约。</param>
    /// <returns>缓存命中或回源得到的 HTTP 响应快照。</returns>
    public static Task<HttpResponseResult<byte[]>> GetCachedAsync(
        this IDistributedHttpResponseStore store, string cacheKey, TimeSpan ttl,
        Func<Task<HttpResponseResult<byte[]>>> responseFactory,
        bool requestContainsAuthorization = false,
        bool allowAuthenticated = false,
        CancellationToken cancellationToken = default, string namespaceName = null)
    {
        return store.ExecuteCachedAsync(cacheKey, ttl, responseFactory,
            requestContainsAuthorization, allowAuthenticated, cancellationToken, namespaceName);
    }

    /// <summary>
    /// 校验分布式响应缓存的存储、键和有效期。
    /// </summary>
    private static void Validate(IDistributedHttpResponseStore store, string key, TimeSpan ttl,
        bool requestContainsAuthorization, bool allowAuthenticated)
    {
        if (store == null) throw new ArgumentNullException(nameof(store));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A distributed cache key is required.", nameof(key));
        if (ttl <= TimeSpan.Zero || ttl == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(ttl));
        if (requestContainsAuthorization && !allowAuthenticated)
            throw new InvalidOperationException("Authenticated responses require explicit cache opt-in.");
    }

    /// <summary>
    /// 根据命名空间版本生成当前有效的缓存键。
    /// </summary>
    private static async Task<string> ResolveNamespaceKeyAsync(
        IDistributedHttpResponseStore store, string cacheKey, string namespaceName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(namespaceName)) return cacheKey;
        if (!(store is IDistributedHttpResponseNamespaceStore namespaceStore))
            throw new InvalidOperationException("The selected distributed store does not support namespace invalidation.");
        var version = await namespaceStore.GetNamespaceVersionAsync(namespaceName, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(version))
            throw new InvalidDataException("The distributed namespace store returned an empty version.");
        return namespaceName + ":" + version + ":" + cacheKey;
    }

    /// <summary>
    /// 等待获得分布式缓存回源租约。
    /// </summary>
    private static async Task<IDistributedHttpResponseLease> WaitForLeaseAsync(
        IDistributedHttpResponseLeaseStore leaseStore, IDistributedHttpResponseStore store,
        string key, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var leaseTtl = ttl > TimeSpan.FromSeconds(30) ? TimeSpan.FromSeconds(30) : ttl;
        var deadline = DateTime.UtcNow + leaseTtl;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lease = await leaseStore.TryAcquireAsync(key, leaseTtl, cancellationToken).ConfigureAwait(false);
            if (lease != null) return lease;
            var cached = await store.GetAsync(key, cancellationToken).ConfigureAwait(false);
            if (cached != null) return null;
            if (DateTime.UtcNow >= deadline) return null;
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 执行缓存未命中时的回源请求并保存响应快照。
    /// </summary>
    private static async Task<HttpResponseResult<byte[]>> ExecuteOriginAsync(
        IDistributedHttpResponseStore store, string cacheKey, TimeSpan ttl,
        Func<Task<HttpResponseResult<byte[]>>> responseFactory, CancellationToken cancellationToken)
    {
        var response = await responseFactory().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The distributed cache response factory returned null.");
        cancellationToken.ThrowIfCancellationRequested();
        await store.SetAsync(cacheKey, ToSnapshot(response), ttl, cancellationToken).ConfigureAwait(false);
        return response;
    }

    /// <summary>
    /// 将响应结果转换为可持久化的快照。
    /// </summary>
    private static DistributedHttpResponseSnapshot ToSnapshot(HttpResponseResult<byte[]> response)
        => new DistributedHttpResponseSnapshot(response.StatusCode, response.Headers,
            response.ContentHeaders, response.Data, response.RawContent);

    /// <summary>
    /// 将持久化快照还原为响应结果。
    /// </summary>
    private static HttpResponseResult<byte[]> ToResponse(DistributedHttpResponseSnapshot snapshot)
        => new HttpResponseResult<byte[]>(snapshot.StatusCode, snapshot.Headers,
            snapshot.ContentHeaders, snapshot.Body, snapshot.RawContent);
}
