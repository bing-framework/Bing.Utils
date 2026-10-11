namespace Bing.Http.Caching;

/// <summary>

/// HTTP 响应缓存的进程内累计统计快照。

/// </summary>
public sealed class HttpResponseCacheStatistics
{
    /// <summary>
    /// 初始化一个<see cref="HttpResponseCacheStatistics"/>类型的实例。
    /// </summary>
    /// <param name="hits">缓存命中次数。</param>
    /// <param name="misses">缓存未命中次数。</param>
    /// <param name="evictions">因容量或过期被移除的条目次数。</param>
    /// <param name="corruptEntries">损坏或不支持格式的记录次数。</param>
    /// <param name="legacyMigrations">旧格式成功迁移次数。</param>
    /// <param name="migrationFailures">旧格式迁移失败次数。</param>
    /// <param name="lockWaits">等待缓存锁重试的次数。</param>
    /// <param name="expiredEntriesRemoved">清理操作移除的过期条目数。</param>
    /// <param name="temporaryFilesRemoved">清理操作移除的临时文件数。</param>
    internal HttpResponseCacheStatistics(long hits, long misses, long evictions, long corruptEntries,
        long legacyMigrations, long migrationFailures, long lockWaits, long expiredEntriesRemoved,
        long temporaryFilesRemoved)
    {
        Hits = hits;
        Misses = misses;
        Evictions = evictions;
        CorruptEntries = corruptEntries;
        LegacyMigrations = legacyMigrations;
        MigrationFailures = migrationFailures;
        LockWaits = lockWaits;
        ExpiredEntriesRemoved = expiredEntriesRemoved;
        TemporaryFilesRemoved = temporaryFilesRemoved;
    }

    /// <summary>

    /// 缓存命中次数。

    /// </summary>
    public long Hits { get; }

    /// <summary>

    /// 缓存未命中次数。

    /// </summary>
    public long Misses { get; }

    /// <summary>

    /// 因容量或过期被移除的条目次数。

    /// </summary>
    public long Evictions { get; }

    /// <summary>

    /// 检测到损坏或不支持格式的记录次数。

    /// </summary>
    public long CorruptEntries { get; }

    /// <summary>

    /// 旧格式成功迁移次数。

    /// </summary>
    public long LegacyMigrations { get; }

    /// <summary>

    /// 旧格式迁移失败次数。

    /// </summary>
    public long MigrationFailures { get; }

    /// <summary>

    /// 等待缓存锁重试的次数。

    /// </summary>
    public long LockWaits { get; }

    /// <summary>

    /// 清理操作移除的过期条目数。

    /// </summary>
    public long ExpiredEntriesRemoved { get; }

    /// <summary>

    /// 清理操作移除的临时文件数。

    /// </summary>
    public long TemporaryFilesRemoved { get; }
}

/// <summary>

/// 为缓存实现提供可选运行统计的能力接口。

/// </summary>
public interface IHttpResponseCacheStatistics
{
    /// <summary>
    /// 获取当前累计统计快照。
    /// </summary>
    /// <returns>不会随后续操作变化的统计快照。</returns>
    HttpResponseCacheStatistics GetStatistics();
}
