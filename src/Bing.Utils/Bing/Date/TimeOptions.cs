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
    internal static readonly DateTime Date1970 = new DateTime(1970, 1, 1);

    /// <summary>
    /// 最小日期
    /// </summary>
    internal static readonly DateTime MinDate = new DateTime(1900, 1, 1);

    /// <summary>
    /// 最大日期
    /// </summary>
    internal static readonly DateTime MaxDate = new DateTime(9999, 12, 31, 23, 59, 59, 999);

    /// <summary>
    /// 初始化js日期时间戳
    /// </summary>
    public static long InitialJavaScriptDateTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;

    #endregion

    /// <summary>
    /// 是否使用Utc日期
    /// </summary>
    public static bool IsUseUtc { get; set; } = false;
}