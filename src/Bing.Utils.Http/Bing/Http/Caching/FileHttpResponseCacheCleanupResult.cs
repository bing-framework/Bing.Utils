namespace Bing.Http.Caching;

/// <summary>

/// 文件 HTTP 响应缓存一次清理操作的结果。

/// </summary>
public sealed class FileHttpResponseCacheCleanupResult
{
    /// <summary>
    /// 初始化一个 <see cref="FileHttpResponseCacheCleanupResult"/> 类型的实例。
    /// </summary>
    /// <param name="expiredEntriesRemoved">移除的过期缓存条目数。</param>
    /// <param name="temporaryFilesRemoved">移除的遗留临时文件数。</param>
    public FileHttpResponseCacheCleanupResult(int expiredEntriesRemoved, int temporaryFilesRemoved)
    {
        ExpiredEntriesRemoved = expiredEntriesRemoved;
        TemporaryFilesRemoved = temporaryFilesRemoved;
    }

    /// <summary>

    /// 获取移除的过期缓存条目数。

    /// </summary>
    public int ExpiredEntriesRemoved { get; }

    /// <summary>

    /// 获取移除的遗留临时文件数。

    /// </summary>
    public int TemporaryFilesRemoved { get; }

    /// <summary>

    /// 获取本次实际移除的文件总数。

    /// </summary>
    public int TotalRemoved => ExpiredEntriesRemoved + TemporaryFilesRemoved;
}
