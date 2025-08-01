namespace Bing.Text.RegularExpressions;

/// <summary>
/// 正则表达式池统计信息
/// </summary>
/// <remarks>
/// 提供正则表达式池的运行时统计数据，包括缓存使用情况、命中率等关键性能指标。
/// 用于监控和优化正则表达式池的性能表现。
/// </remarks>
public readonly record struct PoolStatistics
{
    /// <summary>
    /// 当前缓存数量
    /// </summary>
    /// <value>正则表达式池中当前缓存的正则表达式对象数量</value>
    public int Count { get; init; }

    /// <summary>
    /// 缓存命中次数
    /// </summary>
    /// <value>从缓存中成功获取正则表达式对象的总次数</value>
    public long HitCount { get; init; }

    /// <summary>
    /// 缓存未命中次数
    /// </summary>
    /// <value>需要新建正则表达式对象的总次数</value>
    public long MissCount { get; init; }

    /// <summary>
    /// 缓存命中率
    /// </summary>
    /// <value>缓存命中次数占总访问次数的比例，范围为 0.0 到 1.0</value>
    public double HitRate { get; init; }

    /// <summary>
    /// 最大缓存大小
    /// </summary>
    /// <value>正则表达式池允许的最大缓存容量</value>
    public int MaxSize { get; init; }

    /// <summary>
    /// 获取总访问次数
    /// </summary>
    /// <value>缓存命中次数和未命中次数的总和</value>
    public long TotalAccess => HitCount + MissCount;

    /// <summary>
    /// 获取缓存使用率
    /// </summary>
    /// <value>当前缓存数量占最大容量的比例，范围为 0.0 到 1.0</value>
    public double UsageRate => MaxSize > 0 ? (double)Count / MaxSize : 0.0;

    /// <summary>
    /// 获取格式化的统计信息字符串
    /// </summary>
    /// <returns>包含关键统计数据的格式化字符串</returns>
    public override string ToString()
    {
        return $"RegexPool Statistics: Count={Count}/{MaxSize} ({UsageRate:P1}), " +
               $"HitRate={HitRate:P2} ({HitCount}/{TotalAccess}), " +
               $"MissCount={MissCount}";
    }
}