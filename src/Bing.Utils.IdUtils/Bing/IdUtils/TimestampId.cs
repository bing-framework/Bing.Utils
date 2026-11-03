using System.Diagnostics;
using System.Globalization;

namespace Bing.IdUtils;

/// <summary>
/// 基于毫秒时间戳和四位序号生成唯一字符串 ID。
/// </summary>
public class TimestampId
{
    private const int MAX_END_NUMBER = 9999;
    private const long DEFAULT_WAIT_TIMEOUT_MILLISECONDS = 1000;
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly DateTime _initialDateTime;
    private readonly Func<DateTime> _utcNow;
    private readonly Func<long> _monotonicMilliseconds;
    private readonly Action<int> _delay;
    private readonly TimeSpan _waitTimeout;
    private readonly object _syncRoot = new();
    private long _lastTimestamp = -1;
    private long _sequence;
    private static TimestampId _timestampId;
    private static readonly object TimestampIdLock = new();

    /// <summary>
    /// 初始化一个 <see cref="TimestampId"/> 类型的实例。
    /// </summary>
    /// <param name="initialDateTime">计算时间戳差值时使用的可选起始时间。</param>
    private TimestampId(DateTime? initialDateTime)
        : this(initialDateTime, () => DateTime.UtcNow, GetMonotonicMilliseconds,
            TimeSpan.FromMilliseconds(DEFAULT_WAIT_TIMEOUT_MILLISECONDS), null)
    {
    }

    /// <summary>
    /// 为测试提供可控时钟和等待策略的构造入口。
    /// </summary>
    /// <param name="initialDateTime">计算时间戳差值时使用的可选起始时间。</param>
    /// <param name="utcNow">返回当前 UTC 时间的委托。</param>
    /// <param name="monotonicMilliseconds">返回单调毫秒计时的委托。</param>
    /// <param name="waitTimeout">序列耗尽时的最大等待时间。</param>
    /// <param name="delay">等待委托；未提供时使用线程休眠。</param>
    internal TimestampId(
        DateTime? initialDateTime,
        Func<DateTime> utcNow,
        Func<long> monotonicMilliseconds,
        TimeSpan waitTimeout,
        Action<int> delay = null)
    {
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        _monotonicMilliseconds = monotonicMilliseconds ?? throw new ArgumentNullException(nameof(monotonicMilliseconds));
        if (waitTimeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(waitTimeout), waitTimeout, "等待预算不能为负数。");

        _waitTimeout = waitTimeout;
        _delay = delay ?? (milliseconds =>
        {
            if (milliseconds > 0)
                Thread.Sleep(milliseconds);
        });
        _initialDateTime = NormalizeUtc(initialDateTime ?? DateTime.MinValue);

        var now = NormalizeUtc(_utcNow());
        if (_initialDateTime > now)
            throw new ArgumentOutOfRangeException(nameof(initialDateTime), initialDateTime, "初始时间不能晚于当前 UTC 时间。");
    }

    /// <summary>
    /// 获取用于计算时间戳差值的起始时间。
    /// </summary>
    protected DateTime InitialDateTime => _initialDateTime;

    /// <summary>
    /// 获取单个共享实例。首次调用的时间基准生效，后续调用忽略新的时间基准。
    /// </summary>
    /// <param name="initialDateTime">计算时间戳差值时使用的可选起始时间。</param>
    /// <returns>共享的时间戳 ID 生成器。</returns>
    public static TimestampId GetInstance(DateTime? initialDateTime = null)
    {
        lock (TimestampIdLock)
        {
            if (_timestampId != null)
                return _timestampId;

            _timestampId = new TimestampId(initialDateTime);
            return _timestampId;
        }
    }

    /// <summary>
    /// 为测试程序集清理共享实例状态。
    /// </summary>
    internal static void ResetInstanceForTests()
    {
        lock (TimestampIdLock)
            _timestampId = null;
    }

    /// <summary>
    /// 获取唯一时间戳 ID。
    /// </summary>
    /// <returns>由毫秒时间戳和四位序号组成的数字字符串。</returns>
    public string GetId()
    {
        var timestamp = GetUniqueTimeStamp(_lastTimestamp, out var sequence);
        return timestamp.ToString(CultureInfo.InvariantCulture) + sequence.ToString("D4", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 获取唯一时间戳和序号。
    /// </summary>
    /// <param name="lastTimeStamp">保留的兼容参数，实际使用实例内部状态。</param>
    /// <param name="temp">当前时间戳内的序号。</param>
    /// <returns>当前时间戳。</returns>
    public long GetUniqueTimeStamp(long lastTimeStamp, out long temp)
    {
        lock (_syncRoot)
        {
            var timestamp = GetTimeStamp();
            if (_lastTimestamp >= 0 && timestamp < _lastTimestamp)
                throw new InvalidOperationException("系统时钟发生回拨，无法生成单调递增的时间戳 ID。");

            var sequence = timestamp == _lastTimestamp ? _sequence + 1 : 1;
            if (sequence > MAX_END_NUMBER)
            {
                timestamp = WaitForNextTimestamp(_lastTimestamp);
                sequence = 1;
            }

            _lastTimestamp = timestamp;
            _sequence = sequence;
            temp = sequence;
            return timestamp;
        }
    }

    /// <summary>
    /// 将当前时间转换为相对于起始时间的 UTC 毫秒数。
    /// </summary>
    private long GetTimeStamp()
    {
        var now = NormalizeUtc(_utcNow());
        if (now < _initialDateTime)
            throw new InvalidOperationException("系统时钟早于配置的初始时间。");

        return (now - _initialDateTime).Ticks / TimeSpan.TicksPerMillisecond;
    }

    /// <summary>
    /// 等待时间推进到下一个毫秒，并使用单调计时器限制等待时间。
    /// </summary>
    private long WaitForNextTimestamp(long lastTimestamp)
    {
        var startedAt = _monotonicMilliseconds();
        while (true)
        {
            var timestamp = GetTimeStamp();
            if (timestamp < lastTimestamp)
                throw new InvalidOperationException("等待序列耗尽期间检测到系统时钟回拨。");
            if (_monotonicMilliseconds() - startedAt >= _waitTimeout.TotalMilliseconds)
                throw new TimeoutException("等待时间戳推进超时。");
            if (timestamp > lastTimestamp)
                return timestamp;

            _delay(1);
        }
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        if (value == DateTime.MinValue)
            return UnixEpoch;
        if (value.Kind == DateTimeKind.Utc)
            return value;
        if (value.Kind == DateTimeKind.Local)
            return value.ToUniversalTime();
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static long GetMonotonicMilliseconds() =>
        (long)(Stopwatch.GetTimestamp() * 1000d / Stopwatch.Frequency);
}
