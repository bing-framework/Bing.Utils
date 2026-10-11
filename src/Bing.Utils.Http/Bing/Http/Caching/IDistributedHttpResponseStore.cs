namespace Bing.Http.Caching;

/// <summary>

/// 定义由宿主实现的共享 HTTP 响应快照存储契约。

/// </summary>
/// <remarks>
/// 该接口只描述存储边界，不提供网络、数据库或 Redis 实现。宿主必须定义跨节点可见性、故障恢复、时钟和一致性语义。
/// 版本令牌用于避免并发写覆盖；实现应保证单个键的 CompareAndSet 原子性。
/// </remarks>
public interface IDistributedHttpResponseStore
{
    /// <summary>
    /// 读取尚未过期的共享快照。
    /// </summary>
    /// <param name="key">全局稳定缓存键。</param>
    /// <param name="cancellationToken">取消读取的令牌。</param>
    /// <returns>命中的快照；未命中或已过期时为 <see langword="null"/>。</returns>
    Task<DistributedHttpResponseSnapshot> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>

    /// 无条件写入快照并返回新版本。

    /// </summary>
    /// <param name="key">全局稳定缓存键。</param>
    /// <param name="snapshot">要写入的快照。</param>
    /// <param name="ttl">有效期，必须为正数且为有限值。</param>
    /// <param name="cancellationToken">取消写入的令牌。</param>
    /// <returns>包含存储分配版本令牌的快照。</returns>
    Task<DistributedHttpResponseSnapshot> SetAsync(string key, DistributedHttpResponseSnapshot snapshot,
        TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>

    /// 仅在版本仍匹配时原子替换快照。

    /// </summary>
    /// <param name="key">全局稳定缓存键。</param>
    /// <param name="expectedVersion">读取时得到的版本令牌；为空表示键必须不存在。</param>
    /// <param name="snapshot">替换快照。</param>
    /// <param name="ttl">有效期，必须为正数且为有限值。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>版本匹配并完成替换时为 <see langword="true"/>。</returns>
    Task<bool> CompareAndSetAsync(string key, string expectedVersion,
        DistributedHttpResponseSnapshot snapshot, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>

    /// 移除快照；提供版本时仅在版本匹配时移除。

    /// </summary>
    /// <param name="key">全局稳定缓存键。</param>
    /// <param name="expectedVersion">可选的版本令牌；为空表示无条件移除。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>实际移除快照时为 <see langword="true"/>。</returns>
    Task<bool> RemoveAsync(string key, string expectedVersion = null, CancellationToken cancellationToken = default);
}
