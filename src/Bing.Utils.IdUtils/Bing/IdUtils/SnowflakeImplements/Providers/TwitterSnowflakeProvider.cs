using System.Diagnostics;

namespace Bing.IdUtils.SnowflakeImplements.Providers;

/// <summary>
/// 基于 Twitter 算法的雪花ID 提供程序
/// </summary>
internal class TwitterSnowflakeProvider : BaseSnowflakeProvider, ISnowflakeProvider
{
    #region 常量

    /// <summary>
    /// 基准时间
    /// </summary>
    /// <remarks>开始时间截(毫秒) 2010-11-04 09:42:54</remarks>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    public const long TWEPOCH = 1288834974657L;

    /// <summary>
    /// 机器标识位数
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const int WORKER_ID_BITS = 5;

    /// <summary>
    /// 数据中心标志位数
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const int DATA_CENTER_ID_BITS = 5;

    /// <summary>
    /// 序列号标识位数
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const int SEQUENCE_BITS = 12;

    /// <summary>
    /// 机器ID最大值，结果是31。
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const long MAX_WORKER_ID = -1L ^ (-1L << WORKER_ID_BITS);

    /// <summary>
    /// 数据中心标志ID最大值，结果是31。
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const long MAX_DATA_CENTER_ID = -1L ^ (-1L << DATA_CENTER_ID_BITS);

    /// <summary>
    /// 生成序列的掩码，这里为4095
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const long SEQUENCE_MASK = -1L ^ (-1L << SEQUENCE_BITS);

    /// <summary>
    /// 等待物理时钟追上逻辑时钟的最长时间（毫秒）。
    /// </summary>
    private const long MAX_WAIT_MILLISECONDS = 1000L;

    /// <summary>
    /// 41 位时间字段的最大值。
    /// </summary>
    private const long MAX_TIMESTAMP = (1L << 41) - 1;

    /// <summary>
    /// 机器ID的偏移量(12)
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo 
    const int WORKER_ID_SHIFT = SEQUENCE_BITS;

    /// <summary>
    /// 数据中心ID的偏移量(12+5)
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const int DATA_CENTER_ID_SHIFT = SEQUENCE_BITS + WORKER_ID_BITS;

    /// <summary>
    /// 时间戳的偏移量(5+5+12)
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const int TIMESTAMP_LEFT_SHIFT = SEQUENCE_BITS + WORKER_ID_BITS + DATA_CENTER_ID_BITS;

    #endregion

    #region 字段

    /// <summary>
    /// 毫秒内序列(0 ~ 4095)
    /// </summary>
    private long _sequence = 0L;

    /// <summary>
    /// 上次生成ID的时间截
    /// </summary>
    private long _lastTimestamp = -1L;

    /// <summary>
    /// 是否已经成功生成过标识。
    /// </summary>
    private bool _hasGenerated;

    /// <summary>
    /// 同步锁
    /// </summary>
    private readonly object _lock = new();

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个 <see cref="TwitterSnowflakeProvider"/> 类型的实例。
    /// </summary>
    /// <param name="workerId">机器ID，取值范围: 0 ~ 31</param>
    /// <param name="datacenterId">数据中心ID，取值范围: 0 ~ 31</param>
    /// <param name="sequence">毫秒内序列，取值范围: 0 ~ 4095</param>
    /// <exception cref="ArgumentException"></exception>
    public TwitterSnowflakeProvider(long workerId, long datacenterId, long sequence = 0L)
    {
        // 如果超出范围就抛出异常
        if (workerId > MAX_WORKER_ID || workerId < 0)
            throw new ArgumentException($"worker Id 必须大于等于0，且不能大于 MaxWorkerId：{MAX_WORKER_ID}");
        if (datacenterId > MAX_DATA_CENTER_ID || datacenterId < 0)
            throw new ArgumentException($"datacenter Id 必须大于等于0，且不能大于 MaxDatacenterId：{MAX_DATA_CENTER_ID}");
        if (sequence > SEQUENCE_MASK || sequence < 0)
            throw new ArgumentOutOfRangeException(nameof(sequence), sequence, $"sequence 必须位于 0 到 {SEQUENCE_MASK} 之间。");

        // 先校验再赋值
        WorkerId = workerId;
        DatacenterId = datacenterId;
        _sequence = sequence;
    }

    #endregion

    #region 属性

    /// <summary>
    /// 获取或设置工作机器ID(0 ~ 31)
    /// </summary>
    public long WorkerId { get; protected set; }

    /// <summary>
    /// 获取或设置数据中心ID(0 ~ 31)
    /// </summary>
    public long DatacenterId { get; protected set; }

    /// <summary>
    /// 获取或设置毫秒内序列(0 ~ 4095)
    /// </summary>
    public long Sequence
    {
        get => _sequence;
        internal set
        {
            if (value < 0 || value > SEQUENCE_MASK)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"sequence 必须位于 0 到 {SEQUENCE_MASK} 之间。");
            _sequence = value;
        }
    }

    #endregion

    /// <inheritdoc />
    public override long NextId()
    {
        lock (_lock)
        {
            var timestamp = TimeGen();
            ValidateTimestamp(timestamp);
            var sequence = _sequence;

            if (!_hasGenerated)
            {
                // 首次发号保留构造函数传入的初始 sequence。
            }
            else
            {
                if (timestamp < _lastTimestamp)
                    throw new InvalidOperationException($"时间戳发生回拨，拒绝为 {_lastTimestamp - timestamp} 毫秒生成 ID。");

                if (timestamp == _lastTimestamp)
                {
                    sequence++;
                    if (sequence > SEQUENCE_MASK)
                    {
                        // 等待失败时不修改字段，保留最后一个成功的状态。
                        timestamp = TilNextMillis(_lastTimestamp);
                        ValidateTimestamp(timestamp);
                        sequence = 0;
                    }
                }
                else
                {
                    sequence = 0;
                }
            }

            var relativeTimestamp = timestamp - TWEPOCH;
            var id = (relativeTimestamp << TIMESTAMP_LEFT_SHIFT)
                     | (DatacenterId << DATA_CENTER_ID_SHIFT)
                     | (WorkerId << WORKER_ID_SHIFT)
                     | sequence;

            _lastTimestamp = timestamp;
            _sequence = sequence;
            _hasGenerated = true;

            return id;
        }
    }

    /// <summary>
    /// 获取增量时间戳，防止产生的时间比之前的时间还要小（由于NTP回拨等问题），保持增量的趋势
    /// </summary>
    /// <param name="lastTimestamp">最后一个时间戳</param>
    /// <returns>大于 lastTimestamp 的系统毫秒时间戳。</returns>
    protected virtual long TilNextMillis(long lastTimestamp)
    {
        var startedAt = MonotonicMilliseconds();
        while (true)
        {
            var timestamp = TimeGen();
            if (timestamp < lastTimestamp)
                throw new InvalidOperationException($"等待下一毫秒时检测到时钟回拨，最后时间戳为 {lastTimestamp}，当前时间戳为 {timestamp}。");

            var elapsed = MonotonicMilliseconds() - startedAt;
            if (timestamp > lastTimestamp)
            {
                if (elapsed >= MAX_WAIT_MILLISECONDS)
                    throw new TimeoutException("等待雪花算法的下一毫秒超时。");
                return timestamp;
            }

            if (elapsed >= MAX_WAIT_MILLISECONDS)
                throw new TimeoutException("等待雪花算法的下一毫秒超时。");

            Delay(1);
        }
    }

    /// <summary>
    /// 获取单调毫秒计时，用于等待超时判断。
    /// </summary>
    protected virtual long MonotonicMilliseconds() => (long)(Stopwatch.GetTimestamp() * 1000d / Stopwatch.Frequency);

    /// <summary>
    /// 等待指定的毫秒数。
    /// </summary>
    protected virtual void Delay(int milliseconds)
    {
        if (milliseconds > 0)
            Thread.Sleep(milliseconds);
    }

    /// <summary>
    /// 校验 Twitter 雪花时间字段范围。
    /// </summary>
    private static void ValidateTimestamp(long timestamp)
    {
        var relativeTimestamp = timestamp - TWEPOCH;
        if (relativeTimestamp < 0 || relativeTimestamp > MAX_TIMESTAMP)
            throw new InvalidOperationException("当前时间超出 Twitter 雪花算法的 41 位时间字段范围。");
    }
}
