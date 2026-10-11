namespace Bing.Http.Caching;

/// <summary>

/// 为分布式 HTTP 响应快照提供可选的命名空间版本失效能力。

/// </summary>
/// <remarks>命名空间版本由宿主存储共享；快照和租约实现应把版本变化视为新的逻辑 key。</remarks>
public interface IDistributedHttpResponseNamespaceStore
{
    /// <summary>
    /// 读取命名空间当前版本。
    /// </summary>
    /// <param name="namespaceName">稳定的命名空间名称。</param>
    /// <param name="cancellationToken">取消读取的令牌。</param>
    /// <returns>当前版本令牌；首次使用时应返回稳定的初始版本。</returns>
    Task<string> GetNamespaceVersionAsync(string namespaceName, CancellationToken cancellationToken = default);

    /// <summary>

    /// 原子推进命名空间版本并使旧快照失效。

    /// </summary>
    /// <param name="namespaceName">稳定的命名空间名称。</param>
    /// <param name="cancellationToken">取消失效操作的令牌。</param>
    /// <returns>推进后的新版本令牌。</returns>
    Task<string> InvalidateNamespaceAsync(string namespaceName, CancellationToken cancellationToken = default);

    /// <summary>

    /// 清理指定命名空间的旧版本快照并保留当前版本。

    /// </summary>
    /// <param name="namespaceName">稳定的命名空间名称。</param>
    /// <param name="cancellationToken">取消清理操作的令牌。</param>
    /// <returns>实际移除的快照数量。</returns>
    Task<int> CleanupNamespaceAsync(string namespaceName, CancellationToken cancellationToken = default);
}
