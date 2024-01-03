namespace Bing.Date;

/// <summary>
/// 时间配置
/// </summary>
public static class TimeOptions
{
    #region 字段

    /// <summary>
    /// 1970年1月1日
    /// </summary>
    internal static readonly DateTime Date1970 = new(1970, 1, 1);

    /// <summary>
    /// 最小日期
    /// </summary>
    internal static readonly DateTime MinDate = new(1900, 1, 1);

    /// <summary>
    /// 最大日期
    /// </summary>
    internal static readonly DateTime MaxDate = new(9999, 12, 31, 23, 59, 59, 999);

    /// <summary>
    /// 每秒的时钟刻度数
    /// </summary>
    internal const long TicksPerSec = 10000000L;

    /// <summary>
    /// 每毫秒的时钟刻度数
    /// </summary>
    internal const long TicksPreMillisecond = 10000L;

    /// <summary>
    /// UNIX 时间戳的起始时钟刻度数
    /// </summary>
    internal const long UnixEpochTicks = 621355968000000000L;

    /// <summary>
    /// 本地时间（GMT+8时区）
    /// </summary>
    internal static readonly TimeZoneInfo GMT8 =TimeZoneInfo.CreateCustomTimeZone("GMT-8",TimeSpan.FromHours(8), "China Standard Time","(UTC+8)China Standard Time");

    /// <summary>
    /// 初始化js日期时间戳
    /// </summary>
    public static long InitialJavaScriptDateTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;

    /// <summary>
    /// 表示 Unix 纪元，即 Unix 时间戳计算的起始时间点。
    /// </summary>
    public static readonly DateTime UnixEpoch = new(UnixEpochTicks, DateTimeKind.Utc);

    #endregion

    /// <summary>
    /// 是否使用Utc日期
    /// </summary>
    public static bool IsUseUtc { get; set; } = false;
}