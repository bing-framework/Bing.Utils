using System.Globalization;

namespace Bing.Date;

/// <summary>
/// Unix时间戳类，提供基于Unix纪元（1970年1月1日UTC）的时间戳功能
/// </summary>
/// <remarks>
/// <para>此类继承自TimeStamp，专门处理Unix时间戳格式，具有以下特点：</para>
/// <para>- 基准时间：1970年1月1日 00:00:00 UTC</para>
/// <para>- 精度：秒级精度（标准Unix时间戳）</para>
/// <para>- 范围：1970年1月1日至2038年1月19日（32位）或更远（64位）</para>
/// <para>- 用途：与外部系统交互、API调用、跨平台时间处理</para>
/// <para>- 兼容性：符合POSIX标准，与大多数编程语言和系统兼容</para>
/// <para>常用场景：Web API、数据库记录、日志系统、缓存过期时间、文件系统时间戳等</para>
/// </remarks>
public class UnixTimeStamp : TimeStamp
{
    #region 常量

    /// <summary>
    /// Unix纪元时间（1970年1月1日 00:00:00 UTC）
    /// </summary>
    public static readonly DateTime UnixEpoch = TimeOptions.UnixEpoch;

    /// <summary>
    /// Unix时间戳的最小值（对应1970年1月1日）
    /// </summary>
    public const long MinUnixTimestamp = 0L;

    /// <summary>
    /// Unix时间戳的最大值（32位有符号整数的最大值，对应2038年1月19日）
    /// </summary>
    public const long MaxUnixTimestamp32Bit = 2147483647L; // 2^31 - 1

    /// <summary>
    /// Unix时间戳的实际最大值（64位，对应9999年12月31日）
    /// </summary>
    /// <remarks>
    /// 计算公式：(DateTime.MaxValue.ToUniversalTime().Ticks - UnixEpoch.Ticks) / TimeSpan.TicksPerSecond
    /// 为了避免静态初始化依赖问题，这里使用延迟计算的属性
    /// </remarks>
    public static long MaxUnixTimestamp => _maxUnixTimestamp ??= CalculateMaxUnixTimestamp();

    /// <summary>
    /// 缓存的最大Unix时间戳值
    /// </summary>
    private static long? _maxUnixTimestamp;

    /// <summary>
    /// 计算最大Unix时间戳值
    /// </summary>
    private static long CalculateMaxUnixTimestamp()
    {
        return (long)(DateTime.MaxValue.ToUniversalTime() - UnixEpoch).TotalSeconds;
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="UnixTimeStamp"/>类型的实例
    /// </summary>
    /// <remarks>
    /// 此构造函数创建一个表示当前时间的Unix时间戳对象。
    /// </remarks>
    /// <example>
    /// <code>
    /// var unixTimeStamp = new UnixTimeStamp();
    /// Console.WriteLine($"当前Unix时间戳: {unixTimeStamp.ToTimestamp()}");
    /// </code>
    /// </example>
    public UnixTimeStamp() : this(DateTime.Now) { }

    /// <summary>
    /// 初始化一个<see cref="UnixTimeStamp"/>类型的实例
    /// </summary>
    /// <param name="timestamp">Unix时间戳值（秒）</param>
    /// <exception cref="ArgumentOutOfRangeException">当时间戳超出有效范围时抛出</exception>
    /// <remarks>
    /// 将Unix时间戳（秒）转换为DateTime对象并创建时间戳实例。
    /// 有效范围：0到MaxUnixTimestamp
    /// </remarks>
    /// <example>
    /// <code>
    /// var unixTimeStamp = new UnixTimeStamp(1640995200); // 2022-01-01 00:00:00 UTC
    /// Console.WriteLine($"时间: {unixTimeStamp.ToDateTime():yyyy-MM-dd HH:mm:ss}");
    /// </code>
    /// </example>
    public UnixTimeStamp(long timestamp) : this(FromUnixTimestampFunc(timestamp), timestamp) { }

    /// <summary>
    /// 初始化一个<see cref="UnixTimeStamp"/>类型的实例
    /// </summary>
    /// <param name="dt">时间对象</param>
    /// <remarks>
    /// 将DateTime对象转换为Unix时间戳并创建时间戳实例。
    /// 如果DateTime的Kind为Unspecified，将按本地时间处理。
    /// </remarks>
    /// <example>
    /// <code>
    /// var specific = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    /// var unixTimeStamp = new UnixTimeStamp(specific);
    /// Console.WriteLine($"Unix时间戳: {unixTimeStamp.ToTimestamp()}");
    /// </code>
    /// </example>
    public UnixTimeStamp(DateTime dt) : this(dt, ToUnixTimestampFunc(dt)) { }

    /// <summary>
    /// 初始化一个<see cref="UnixTimeStamp"/>类型的实例
    /// </summary>
    ///<param name="dt">时间对象</param>
    /// <param name="timestamp">Unix时间戳值</param>
    protected UnixTimeStamp(DateTime dt, long timestamp) : base(dt, dt.Ticks)
    {
        // 重新设置为Unix时间戳
        m_timestamp = timestamp;
    }

    #endregion

    #region 核心转换方法

    /// <summary>
    /// 转换为时间对象
    /// </summary>
    /// <returns>对应的DateTime对象（本地时间）</returns>
    /// <remarks>
    /// 返回当前Unix时间戳对应的本地时间。
    /// </remarks>
    /// <example>
    /// <code>
    /// var unixTimeStamp = new UnixTimeStamp(1640995200);
    /// DateTime localTime = unixTimeStamp.ToDateTime();
    /// Console.WriteLine($"本地时间: {localTime:yyyy-MM-dd HH:mm:ss}");
    /// </code>
    /// </example>
    public override DateTime ToDateTime() => m_datetime;

    /// <summary>
    /// 转换为Unix时间戳值
    /// </summary>
    /// <returns>Unix时间戳值（秒）</returns>
    /// <remarks>
    /// 返回当前时间对应的Unix时间戳值，以秒为单位。
    /// </remarks>
    /// <example>
    /// <code>
    /// var unixTimeStamp = new UnixTimeStamp();
    /// long timestamp = unixTimeStamp.ToTimestamp();
    /// Console.WriteLine($"Unix时间戳: {timestamp}");
    /// </code>
    /// </example>
    public override long ToTimestamp() => m_timestamp;

    /// <summary>
    /// 转换为UTC时间对象
    /// </summary>
    /// <returns>对应的UTC DateTime对象</returns>
    /// <remarks>
    /// 返回当前Unix时间戳对应的UTC时间，不受时区影响。
    /// </remarks>
    /// <example>
    /// <code>
    /// var unixTimeStamp = new UnixTimeStamp();
    /// DateTime utcTime = unixTimeStamp.ToUtcDateTime();
    /// Console.WriteLine($"UTC时间: {utcTime:yyyy-MM-dd HH:mm:ss} UTC");
    /// </code>
    /// </example>
    public DateTime ToUtcDateTime() => UnixEpoch.AddSeconds(m_timestamp);

    #endregion

    #region 静态转换方法

    /// <summary>
    /// 时间转Unix时间戳函数
    /// </summary>
    private static readonly Func<DateTime, long> ToUnixTimestampFunc = time =>
    {
        var utcTime = time.Kind switch
        {
            DateTimeKind.Utc => time,
            DateTimeKind.Local => time.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(time, DateTimeKind.Utc), // 将Unspecified视为UTC时间
            _ => time.ToUniversalTime()
        };

        return (long)(utcTime - UnixEpoch).TotalSeconds;
    };

    /// <summary>
    /// Unix时间戳转时间函数
    /// </summary>
    private static readonly Func<long, DateTime> FromUnixTimestampFunc = timestamp =>
    {
        if (timestamp < MinUnixTimestamp || timestamp > MaxUnixTimestamp)
            throw new ArgumentOutOfRangeException(nameof(timestamp), $"Unix时间戳值超出有效范围。有效范围：{MinUnixTimestamp} 到 {MaxUnixTimestamp}");
        var utcTime = UnixEpoch.AddSeconds(timestamp);
        return utcTime.ToLocalTime();
    };

    #endregion

    #region 静态属性和方法

    /// <summary>
    /// 获取当前时间的Unix时间戳
    /// </summary>
    /// <returns>当前本地时间的Unix时间戳值</returns>
    /// <remarks>
    /// 提供快速获取当前Unix时间戳的静态方法，使用本地时间。
    /// </remarks>
    /// <example>
    /// <code>
    /// long currentUnixTimestamp = UnixTimeStamp.Now();
    /// Console.WriteLine($"当前Unix时间戳: {currentUnixTimestamp}");
    /// </code>
    /// </example>
    public new static long Now() => ToUnixTimestampFunc(DateTime.Now);

    /// <summary>
    /// 获取当前UTC时间的Unix时间戳
    /// </summary>
    /// <returns>当前UTC时间的Unix时间戳值</returns>
    /// <remarks>
    /// 提供快速获取当前UTC Unix时间戳的静态方法，推荐在大多数场景中使用。
    /// </remarks>
    /// <example>
    /// <code>
    /// long utcUnixTimestamp = UnixTimeStamp.UtcNow();
    /// Console.WriteLine($"当前UTC Unix时间戳: {utcUnixTimestamp}");
    /// </code>
    /// </example>
    public new static long UtcNow() => ToUnixTimestampFunc(DateTime.UtcNow);

    /// <summary>
    /// 创建表示当前时间的Unix时间戳对象
    /// </summary>
    /// <returns>表示当前时间的Unix时间戳实例</returns>
    public new static UnixTimeStamp CreateNow() => new(DateTime.Now);

    /// <summary>
    /// 创建表示当前UTC时间的Unix时间戳对象
    /// </summary>
    /// <returns>表示当前UTC时间的Unix时间戳实例</returns>
    public new static UnixTimeStamp CreateUtcNow() => new(DateTime.UtcNow);

    /// <summary>
    /// 从Unix时间戳创建实例
    /// </summary>
    /// <param name="unixTimestamp">Unix时间戳值（秒）</param>
    /// <returns>Unix时间戳实例</returns>
    public static UnixTimeStamp FromUnixTime(long unixTimestamp) => new(unixTimestamp);

    /// <summary>
    /// 从毫秒级Unix时间戳创建实例
    /// </summary>
    /// <param name="unixTimestampMilliseconds">Unix时间戳值（毫秒）</param>
    /// <returns>Unix时间戳实例</returns>
    /// <example>
    /// <code>
    /// // JavaScript Date.now() 返回毫秒级时间戳
    /// long jsTimestamp = 1640995200000;
    /// var unixTimeStamp = UnixTimeStamp.FromUnixTimeMilliseconds(jsTimestamp);
    /// </code>
    /// </example>
    public static UnixTimeStamp FromUnixTimeMilliseconds(long unixTimestampMilliseconds) => new(unixTimestampMilliseconds / 1000);

    #endregion

    #region 实用工具方法

    /// <summary>
    /// 转换为毫秒级Unix时间戳
    /// </summary>
    /// <returns>毫秒级Unix时间戳</returns>
    /// <remarks>
    /// 用于与JavaScript等需要毫秒级时间戳的系统交互。
    /// </remarks>
    /// <example>
    /// <code>
    /// var unixTimeStamp = new UnixTimeStamp();
    /// long millisecondsTimestamp = unixTimeStamp.ToUnixTimeMilliseconds();
    /// Console.WriteLine($"毫秒级时间戳: {millisecondsTimestamp}");
    /// </code>
    /// </example>
    public long ToUnixTimeMilliseconds() => m_timestamp * 1000;

    /// <summary>
    /// 检查时间戳是否在2038年问题范围内
    /// </summary>
    /// <returns>如果时间戳超过32位有符号整数最大值则返回true</returns>
    /// <remarks>
    /// 32位系统的Unix时间戳会在2038年1月19日溢出，此方法用于检测这个问题。
    /// </remarks>
    public bool IsAfter2038Problem() => m_timestamp > MaxUnixTimestamp32Bit;

    /// <summary>
    /// 获取距离Unix纪元的天数
    /// </summary>
    /// <returns>自1970年1月1日以来的天数</returns>
    public int GetDaysSinceEpoch() => (int)(m_timestamp / 86400); // 86400 = 24 * 60 * 60

    /// <summary>
    /// 添加指定的秒数
    /// </summary>
    /// <param name="seconds">要添加的秒数</param>
    /// <returns>新的Unix时间戳对象</returns>
    /// <example>
    /// <code>
    /// var unixTimeStamp = new UnixTimeStamp();
    /// var futureStamp = unixTimeStamp.AddSeconds(3600); // 一小时后
    /// Console.WriteLine($"一小时后: {futureStamp.ToDateTime():HH:mm:ss}");
    /// </code>
    /// </example>
    public UnixTimeStamp AddSeconds(long seconds) => new(m_timestamp + seconds);

    /// <summary>
    /// 添加指定的分钟数
    /// </summary>
    /// <param name="minutes">要添加的分钟数</param>
    /// <returns>新的Unix时间戳对象</returns>
    public UnixTimeStamp AddMinutes(long minutes) => AddSeconds(minutes * 60);

    /// <summary>
    /// 添加指定的小时数
    /// </summary>
    /// <param name="hours">要添加的小时数</param>
    /// <returns>新的Unix时间戳对象</returns>
    public UnixTimeStamp AddHours(long hours) => AddSeconds(hours * 3600);

    /// <summary>
    /// 添加指定的天数
    /// </summary>
    /// <param name="days">要添加的天数</param>
    /// <returns>新的Unix时间戳对象</returns>
    public UnixTimeStamp AddDays(long days) => AddSeconds(days * 86400);

    /// <summary>
    /// 添加指定的时间间隔
    /// </summary>
    /// <param name="timeSpan">要添加的时间间隔</param>
    /// <returns>新的Unix时间戳对象</returns>
    public override TimeStamp Add(TimeSpan timeSpan) => new UnixTimeStamp(m_datetime.Add(timeSpan));

    /// <summary>
    /// 减去指定的时间间隔
    /// </summary>
    /// <param name="timeSpan">要减去的时间间隔</param>
    /// <returns>新的Unix时间戳对象</returns>
    public override TimeStamp Subtract(TimeSpan timeSpan) => new UnixTimeStamp(m_datetime.Subtract(timeSpan));

    #endregion

    #region 格式化方法

    /// <summary>
    /// 将Unix时间戳格式化为ISO 8601字符串（UTC时间）
    /// </summary>
    /// <returns>ISO 8601格式的UTC时间字符串</returns>
    /// <example>
    /// <code>
    /// var unixTimeStamp = new UnixTimeStamp();
    /// string iso8601 = unixTimeStamp.ToIso8601String();
    /// Console.WriteLine($"ISO 8601: {iso8601}"); // 2024-01-01T12:00:00Z
    /// </code>
    /// </example>
    public string ToIso8601String() => ToUtcDateTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    /// <summary>
    /// 将Unix时间戳格式化为RFC 2822字符串
    /// </summary>
    /// <returns>RFC 2822格式的时间字符串</returns>
    /// <example>
    /// <code>
    /// var unixTimeStamp = new UnixTimeStamp();
    /// string rfc2822 = unixTimeStamp.ToRfc2822String();
    /// Console.WriteLine($"RFC 2822: {rfc2822}"); // Mon, 01 Jan 2024 12:00:00 GMT
    /// </code>
    /// </example>
    public string ToRfc2822String() => ToUtcDateTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture);

    /// <summary>
    /// 将Unix时间戳转换为人类可读的相对时间字符串
    /// </summary>
    /// <returns>相对时间字符串（如"2小时前"、"3天后"等）</returns>
    /// <example>
    /// <code>
    /// var pastTime = new UnixTimeStamp(UnixTimeStamp.Now() - 3600);
    /// string relative = pastTime.ToRelativeString(); // "1小时前"
    /// </code>
    /// </example>
    public string ToRelativeString()
    {
        var now = Now();
        var diff = now - m_timestamp;
        var absDiff = Math.Abs(diff);

        var (value, unit, suffix) = absDiff switch
        {
            < 60 => (absDiff, "秒", diff < 0 ? "后" : "前"),
            < 3600 => (absDiff / 60, "分钟", diff < 0 ? "后" : "前"),
            < 86400 => (absDiff / 3600, "小时", diff < 0 ? "后" : "前"),
            < 2592000 => (absDiff / 86400, "天", diff < 0 ? "后" : "前"),
            < 31536000 => (absDiff / 2592000, "个月", diff < 0 ? "后" : "前"),
            _ => (absDiff / 31536000, "年", diff < 0 ? "后" : "前")
        };

        return $"{value}{unit}{suffix}";
    }

    #endregion

    /// <summary>
    /// 当前Unix时间戳委托（兼容性）
    /// </summary>
    /// <remarks>此属性已过时，建议使用Now()静态方法</remarks>
    [Obsolete("请使用 UnixTimeStamp.Now() 静态方法替代此属性", false)]
    public static Func<long> NowUnixTimeStamp = () => ToUnixTimestampFunc(DateTime.Now);

    /// <summary>
    /// 当前UTC Unix时间戳委托（兼容性）
    /// </summary>
    /// <remarks>此属性已过时，建议使用UtcNow()静态方法</remarks>
    [Obsolete("请使用 UnixTimeStamp.UtcNow() 静态方法替代此属性", false)]
    public static Func<long> UtcNowUnixTimeStamp = () => ToUnixTimestampFunc(DateTime.UtcNow);
}