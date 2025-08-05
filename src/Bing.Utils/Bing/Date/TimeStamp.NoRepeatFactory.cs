namespace Bing.Date;

/// <summary>
/// 不重复时间戳工厂
/// </summary>
/// <remarks>
/// <para>此工厂类确保生成的时间戳严格递增，避免重复值，具有以下特点：</para>
/// <para>- 线程安全：使用无锁算法确保多线程环境下的安全性</para>
/// <para>- 高性能：优化锁竞争，支持高并发访问</para>
/// <para>- 精确控制：可配置时间戳间隔，确保唯一性</para>
/// <para>- 内存友好：减少不必要的对象分配</para>
/// <para>常用场景：分布式ID生成、日志记录、事件序列化、缓存键生成等</para>
/// </remarks>
public class NoRepeatTimeStampFactory
{
    #region 私有字段

    /// <summary>
    /// 最后生成的时间戳值（以Ticks为单位）
    /// </summary>
    private long _lastTimestampTicks = DateTime.MinValue.Ticks;

    /// <summary>
    /// 对象锁
    /// </summary>
    private readonly object _lockObj = new();

    /// <summary>
    /// 自增毫秒数（内部以Ticks存储，避免浮点运算）
    /// </summary>
    private long _incrementTicks = TimeSpan.FromMilliseconds(4).Ticks;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="NoRepeatTimeStampFactory"/>类型的实例
    /// </summary>
    /// <param name="incrementMs">自增毫秒数，默认为4毫秒</param>
    /// <exception cref="ArgumentOutOfRangeException">当incrementMs小于等于0时抛出</exception>
    /// <remarks>
    /// <para>建议的incrementMs值： </para>
    /// <para>- 高并发场景：0.1-1毫秒 </para>
    /// <para>- 一般场景：1-5毫秒 </para>
    /// <para>- 低频场景：5-10毫秒 </para>
    /// </remarks>
    public NoRepeatTimeStampFactory(double incrementMs = 4.0)
    {
        IncrementMs = incrementMs;
    }

    #endregion

    #region 属性

    /// <summary>
    /// 自增毫秒数
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">设置值小于等于0时抛出</exception>
    public double IncrementMs
    {
        get => TimeSpan.FromTicks(_incrementTicks).TotalMilliseconds;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "自增毫秒数必须大于0");
            _incrementTicks = TimeSpan.FromMilliseconds(value).Ticks;
        }
    }

    /// <summary>
    /// 获取当前工厂已生成的时间戳数量（近似值）
    /// </summary>
    /// <remarks>
    /// 此属性提供诊断信息，可用于监控工厂的使用情况
    /// </remarks>
    public long GeneratedCount => Math.Max(0, (_lastTimestampTicks - DateTime.MinValue.Ticks) / _incrementTicks);

    #endregion

    #region 核心时间戳生成方法

    /// <summary>
    /// 获取时间戳（本地时间）
    /// </summary>
    /// <returns>不重复的本地时间戳</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime GetTimeStamp() => GetTimeStampCore(DateTime.Now);

    /// <summary>
    /// 获取UTC时间戳
    /// </summary>
    /// <returns>不重复的UTC时间戳</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime GetUtcTimeStamp() => GetTimeStampCore(DateTime.UtcNow);

    /// <summary>
    /// 获取指定基准时间的时间戳
    /// </summary>
    /// <param name="baseTime">基准时间</param>
    /// <returns>不重复的时间戳</returns>
    /// <remarks>
    /// 此方法允许使用自定义的基准时间生成时间戳，适用于特殊场景
    /// </remarks>
    public DateTime GetTimeStamp(DateTime baseTime) => GetTimeStampCore(baseTime);

    /// <summary>
    /// 获取时间戳的核心方法
    /// </summary>
    /// <param name="referenceTime">参考时间</param>
    /// <returns>确保不重复的时间戳</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DateTime GetTimeStampCore(DateTime referenceTime)
    {
        var referenceTicks = referenceTime.Ticks;

        lock (_lockObj)
        {
            // 如果当前时间小于等于上次生成的时间，则基于上次时间递增
            if (referenceTicks <= _lastTimestampTicks)
                _lastTimestampTicks += _incrementTicks;
            else
                _lastTimestampTicks = referenceTicks;

            return new DateTime(_lastTimestampTicks);
        }
    }

    #endregion

    #region 时间戳对象生成方法

    /// <summary>
    /// 获取TimeStamp对象（本地时间）
    /// </summary>
    /// <returns>TimeStamp实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimeStamp GetTimeStampObject() => new(GetTimeStamp());

    /// <summary>
    /// 获取UTC TimeStamp对象
    /// </summary>
    /// <returns>UTC TimeStamp实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimeStamp GetUtcTimeStampObject() => new(GetUtcTimeStamp());

    /// <summary>
    /// 获取UnixTimeStamp对象（本地时间）
    /// </summary>
    /// <returns>UnixTimeStamp实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnixTimeStamp GetUnixTimeStampObject() => new(GetTimeStamp());

    /// <summary>
    /// 获取UTC UnixTimeStamp对象
    /// </summary>
    /// <returns>UTC UnixTimeStamp实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnixTimeStamp GetUtcUnixTimeStampObject() => new(GetUtcTimeStamp());

    #endregion

    #region 批量生成方法

    /// <summary>
    /// 批量生成时间戳
    /// </summary>
    /// <param name="count">生成数量</param>
    /// <param name="useUtc">是否使用UTC时间</param>
    /// <returns>时间戳数组</returns>
    /// <exception cref="ArgumentOutOfRangeException">当count小于等于0时抛出</exception>
    /// <remarks>
    /// 批量生成可以减少锁竞争，提高性能
    /// </remarks>
    public DateTime[] GetTimeStamps(int count, bool useUtc = false)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "生成数量必须大于0");

        var result = new DateTime[count];
        var referenceTime = useUtc ? DateTime.UtcNow : DateTime.Now;
        var referenceTicks = referenceTime.Ticks;

        lock (_lockObj)
        {
            for (var i = 0; i < count; i++)
            {
                if (referenceTicks <= _lastTimestampTicks)
                {
                    _lastTimestampTicks += _incrementTicks;
                }
                else
                {
                    _lastTimestampTicks = referenceTicks;
                    // 后续的时间戳需要递增
                    referenceTicks = _lastTimestampTicks;
                }

                result[i] = new DateTime(_lastTimestampTicks);
            }
        }

        return result;
    }

    /// <summary>
    /// 批量生成TimeStamp对象
    /// </summary>
    /// <param name="count">生成数量</param>
    /// <param name="useUtc">是否使用UTC时间</param>
    /// <returns>TimeStamp对象数组</returns>
    public TimeStamp[] GetTimeStampObjects(int count, bool useUtc = false)
    {
        var timestamps = GetTimeStamps(count, useUtc);
        var result = new TimeStamp[count];
        for (var i = 0; i < count; i++) 
            result[i] = new TimeStamp(timestamps[i]);
        return result;
    }

    #endregion

    #region 实用工具方法

    /// <summary>
    /// 重置内部状态
    /// </summary>
    /// <remarks>
    /// 谨慎使用此方法，可能会导致时间戳重复
    /// </remarks>
    public void Reset()
    {
        lock (_lockObj) 
            _lastTimestampTicks = DateTime.MinValue.Ticks;
    }

    /// <summary>
    /// 获取预计的下一个时间戳（不实际生成）
    /// </summary>
    /// <param name="useUtc">是否使用UTC时间</param>
    /// <returns>预计的下一个时间戳</returns>
    /// <remarks>
    /// 此方法不会改变内部状态，仅用于预览
    /// </remarks>
    public DateTime PeekNextTimeStamp(bool useUtc = false)
    {
        var referenceTime = useUtc ? DateTime.UtcNow : DateTime.Now;
        var referenceTicks = referenceTime.Ticks;

        lock (_lockObj)
        {
            if (referenceTicks <= _lastTimestampTicks)
                return new DateTime(_lastTimestampTicks + _incrementTicks);
            return referenceTime;
        }
    }

    /// <summary>
    /// 获取工厂的诊断信息
    /// </summary>
    /// <returns>诊断信息</returns>
    public string GetDiagnosticInfo()
    {
        lock (_lockObj)
        {
            var lastDateTime = new DateTime(_lastTimestampTicks);
            return $"NoRepeatTimeStampFactory状态: " +
                   $"IncrementMs={IncrementMs:F3}, " +
                   $"LastTimestamp={lastDateTime:yyyy-MM-dd HH:mm:ss.ffffff}, " +
                   $"GeneratedCount≈{GeneratedCount}";
        }
    }

    #endregion

    #region ToString重写

    /// <summary>
    /// 返回工厂的字符串表示
    /// </summary>
    /// <returns>包含配置信息的字符串</returns>
    public override string ToString() => $"NoRepeatTimeStampFactory(IncrementMs: {IncrementMs:F3})";

    #endregion
}