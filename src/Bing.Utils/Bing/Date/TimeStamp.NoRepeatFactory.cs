namespace Bing.Date;

/// <summary>
/// 不重复时间戳工厂
/// </summary>
/// <remarks>
/// <para>此工厂类确保生成的时间戳严格递增，避免重复值，具有以下特点：</para>
/// <para>- 线程安全：配置、生成与诊断共享同一把锁</para>
/// <para>- 严格递增：同一实例按 UTC ticks 发布候选时间点</para>
/// <para>- 精确控制：可配置时间戳间隔，确保唯一性</para>
/// <para>- 内存友好：减少不必要的对象分配</para>
/// <para>唯一性仅限单实例一次 Reset 生命周期；Unix 包装按秒量化，不保证秒值唯一</para>
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
    private long _incrementTicks = 4 * TimeSpan.TicksPerMillisecond;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个 <see cref="NoRepeatTimeStampFactory"/> 类型的实例。
    /// </summary>
    /// <param name="incrementMs">自增毫秒数，默认为4毫秒</param>
    /// <exception cref="ArgumentOutOfRangeException">当 incrementMs 非有限、非正、少于一个 tick 或超出范围时抛出</exception>
    /// <remarks>
    /// <para>建议的incrementMs值： </para>
    /// <para>- 高并发场景：0.1-1毫秒 </para>
    /// <para>- 一般场景：1-5毫秒 </para>
    /// <para>- 低频场景：5-10毫秒 </para>
    /// </remarks>
    public NoRepeatTimeStampFactory(double incrementMs = 4.0)
    {
        _incrementTicks = ValidateIncrement(incrementMs, nameof(incrementMs));
    }

    #endregion

    #region 属性

    /// <summary>
    /// 获取或设置自增毫秒数
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">设置值非有限、非正、少于一个 tick 或超出范围时抛出</exception>
    public double IncrementMs
    {
        get { lock (_lockObj) return (double)_incrementTicks / TimeSpan.TicksPerMillisecond; }
        set
        {
            var ticks = ValidateIncrement(value, nameof(value));
            lock (_lockObj) _incrementTicks = ticks;
        }
    }

    /// <summary>
    /// 显式向零量化，避免不同框架的毫秒舍入行为改变最小增量。
    /// </summary>
    /// <param name="value">待量化的毫秒增量。</param>
    /// <param name="parameterName">公开入口的参数名。</param>
    /// <returns>至少为一个 tick 的增量。</returns>
    private static long ValidateIncrement(double value, string parameterName)
    {
        var ticks = value * TimeSpan.TicksPerMillisecond;
        if (double.IsNaN(ticks) || double.IsInfinity(ticks) || ticks < 1 || ticks >= (double)long.MaxValue)
            throw new ArgumentOutOfRangeException(parameterName, value, "增量必须是可表示且至少为一个 tick 的有限正数。");
        return checked((long)ticks);
    }

    /// <summary>
    /// 获取当前工厂已生成的时间戳数量（近似值）
    /// </summary>
    /// <remarks>
    /// 按最后 UTC ticks 除以当前增量计算近似距离，不是真实生成次数；Reset 后从零重新开始
    /// </remarks>
    public long GeneratedCount
    {
        get { lock (_lockObj) return _lastTimestampTicks / _incrementTicks; }
    }

    #endregion

    #region 核心时间戳生成方法

    /// <summary>
    /// 获取时间戳（本地时间）
    /// </summary>
    /// <returns>不重复的本地时间戳</returns>
    /// <exception cref="ArgumentOutOfRangeException">序列耗尽或候选时间无法表示为本地时间；没有对应输入参数时 ParamName 为 null。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime GetTimeStamp() => GetTimeStampCore(DateTime.Now);

    /// <summary>
    /// 获取UTC时间戳
    /// </summary>
    /// <returns>不重复的UTC时间戳</returns>
    /// <exception cref="ArgumentOutOfRangeException">序列耗尽；ParamName 为 null。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime GetUtcTimeStamp() => GetTimeStampCore(DateTime.UtcNow);

    /// <summary>
    /// 获取指定基准时间的时间戳
    /// </summary>
    /// <param name="baseTime">基准时间</param>
    /// <returns>不重复的时间戳</returns>
    /// <exception cref="ArgumentOutOfRangeException">基准转换越界、序列耗尽或输出无法无损表示；参数名为 baseTime。</exception>
    /// <remarks>
    /// 保留基准的 Kind；Unspecified 按本地解释。内部始终按 UTC 时间点递增
    /// </remarks>
    public DateTime GetTimeStamp(DateTime baseTime) => GetTimeStampCore(baseTime, nameof(baseTime));

    /// <summary>
    /// 获取时间戳的核心方法
    /// </summary>
    /// <param name="referenceTime">参考时间</param>
    /// <param name="parameterName">公开入口的时间参数名；从系统时钟获取时为 null。</param>
    /// <returns>确保不重复的时间戳</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DateTime GetTimeStampCore(DateTime referenceTime, string parameterName = null)
    {
        // Unspecified 沿用本地时间解释；DateTimeOffset 会拒绝越过 UTC 范围的输入。
        long referenceTicks;
        try
        {
            referenceTicks = new DateTimeOffset(referenceTime).UtcDateTime.Ticks;
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentOutOfRangeException(parameterName, referenceTime, exception.Message);
        }
        lock (_lockObj)
        {
            var candidate = NextTicks(referenceTicks, parameterName);
            var result = FormatTimestamp(candidate, referenceTime.Kind, parameterName);
            _lastTimestampTicks = candidate;
            return result;
        }
    }

    /// <summary>
    /// 在持锁期间计算候选值，检查通过前不发布水位。
    /// </summary>
    /// <param name="referenceTicks">参考时间的 UTC ticks。</param>
    /// <param name="parameterName">导致序列请求越界的公开参数名；无对应参数时为 null。</param>
    /// <returns>可表示且严格递增的 UTC ticks。</returns>
    private long NextTicks(long referenceTicks, string parameterName = null)
    {
        if (referenceTicks > _lastTimestampTicks)
            return referenceTicks;
        if (_incrementTicks > DateTime.MaxValue.Ticks - _lastTimestampTicks)
            throw new ArgumentOutOfRangeException(parameterName, "时间戳序列已超出 DateTime 范围。");
        return _lastTimestampTicks + _incrementTicks;
    }

    /// <summary>
    /// 转换显示 Kind；本地转换不可表示时拒绝，避免 BCL 饱和截断造成重复。
    /// </summary>
    /// <param name="utcTicks">待显示时间的 UTC ticks。</param>
    /// <param name="kind">目标时间种类。</param>
    /// <param name="parameterName">指定显示种类的公开参数名；无对应参数时为 null。</param>
    /// <returns>可以无损表示该时间点的时间。</returns>
    private static DateTime FormatTimestamp(long utcTicks, DateTimeKind kind, string parameterName = null)
    {
        var utc = new DateTime(utcTicks, DateTimeKind.Utc);
        if (kind == DateTimeKind.Utc)
            return utc;
        var local = utc.ToLocalTime();
        var result = kind == DateTimeKind.Local ? local : DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        // Unspecified 不能携带 DST 重复时间的选择信息，必须校验最终输出而非中间 Local 值。
        if (result.ToUniversalTime().Ticks != utcTicks)
            throw new ArgumentOutOfRangeException(parameterName, "时间戳无法无损表示为指定 Kind 的本地时间。");
        return result;
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
    /// <exception cref="ArgumentOutOfRangeException">数量无效或整批超出 UTC 范围时参数名为 count；本地显示越界时参数名为 useUtc。</exception>
    /// <remarks>
    /// 批量生成可以减少锁竞争，提高性能
    /// </remarks>
    public DateTime[] GetTimeStamps(int count, bool useUtc = false)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "生成数量必须大于0");
        var referenceTicks = DateTime.UtcNow.Ticks;
        var kind = useUtc ? DateTimeKind.Utc : DateTimeKind.Local;
        lock (_lockObj)
        {
            var first = NextTicks(referenceTicks, nameof(count));
            if ((long)count - 1 > (DateTime.MaxValue.Ticks - first) / _incrementTicks)
                throw new ArgumentOutOfRangeException(nameof(count), "整批时间戳超出 DateTime 范围。");
            var last = first + ((long)count - 1) * _incrementTicks;
            FormatTimestamp(first, kind, nameof(useUtc));
            FormatTimestamp(last, kind, nameof(useUtc));
            var result = new DateTime[count];
            for (var i = 0; i < count; i++)
                result[i] = FormatTimestamp(first + (long)i * _incrementTicks, kind, nameof(useUtc));
            _lastTimestampTicks = last;
            return result;
        }
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
    /// <exception cref="ArgumentOutOfRangeException">序列耗尽时 ParamName 为 null；本地显示越界时参数名为 useUtc。</exception>
    /// <remarks>
    /// 此方法不会改变内部状态，仅用于预览
    /// </remarks>
    public DateTime PeekNextTimeStamp(bool useUtc = false)
    {
        var referenceTicks = DateTime.UtcNow.Ticks;
        lock (_lockObj)
            return FormatTimestamp(NextTicks(referenceTicks), useUtc ? DateTimeKind.Utc : DateTimeKind.Local, nameof(useUtc));
    }

    /// <summary>
    /// 获取工厂的诊断信息
    /// </summary>
    /// <returns>诊断信息</returns>
    public string GetDiagnosticInfo()
    {
        lock (_lockObj)
        {
            var lastDateTime = new DateTime(_lastTimestampTicks, DateTimeKind.Utc);
            return $"NoRepeatTimeStampFactory状态: " +
                   $"IncrementMs={IncrementMs:F3}, " +
                   $"LastTimestamp={lastDateTime:yyyy-MM-dd HH:mm:ss.ffffff}, " +
                   $"GeneratedCount≈{GeneratedCount}";
        }
    }

    #endregion

    #region ToString重写

    /// <inheritdoc />
    public override string ToString() => $"NoRepeatTimeStampFactory(IncrementMs: {IncrementMs:F3})";

    #endregion
}
