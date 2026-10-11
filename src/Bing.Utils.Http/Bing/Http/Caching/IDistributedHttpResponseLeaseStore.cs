namespace Bing.Http.Caching;

/// <summary>

/// 为分布式 HTTP 响应回源提供按键租约协调能力。

/// </summary>
/// <remarks>租约只协调回源，不替代快照存储的一致性、故障恢复或跨节点时钟语义。</remarks>
public interface IDistributedHttpResponseLeaseStore
{
    /// <summary>
    /// 尝试获取指定缓存键的独占回源租约。
    /// </summary>
    /// <param name="key">全局稳定缓存键。</param>
    /// <param name="leaseTtl">租约有效期，必须为正数且为有限值。</param>
    /// <param name="cancellationToken">取消获取租约的令牌。</param>
    /// <returns>获取成功时返回租约；已由其他节点持有时返回 <see langword="null"/>。</returns>
    Task<IDistributedHttpResponseLease> TryAcquireAsync(string key, TimeSpan leaseTtl,
        CancellationToken cancellationToken = default);
}

/// <summary>

/// 表示一个分布式 HTTP 响应回源租约。

/// </summary>
public interface IDistributedHttpResponseLease
{
    /// <summary>
    /// 释放租约并允许其他节点继续回源。
    /// </summary>
    /// <param name="cancellationToken">取消释放操作的令牌。</param>
    /// <returns>异步释放任务。</returns>
    Task ReleaseAsync(CancellationToken cancellationToken = default);
}
