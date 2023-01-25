namespace Bing.Date;

/// <summary>
/// 日期(<see cref="DateTime"/>) 判断器
/// </summary>
public static class DateJudge
{
    /// <summary>
    /// 最小日期
    /// </summary>
    internal static readonly DateTime MinDate = new DateTime(1900, 1, 1);

    /// <summary>
    /// 最大日期
    /// </summary>
    internal static readonly DateTime MaxDate = new DateTime(9999, 12, 31, 23, 59, 59, 999);

    #region IsToday(是否今天)

    /// <summary>
    /// 判断日期是否为今天
    /// </summary>
    /// <param name="dt">时间</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsToday(DateTime dt) => dt.IsToday();

    /// <summary>
    /// 判断日期是否为今天
    /// </summary>
    /// <param name="dt">时间</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsToday(DateTime? dt) => dt.IsToday();

    /// <summary>
    /// 判断日期是否为今天
    /// </summary>
    /// <param name="dtOffset">时间偏移</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsToday(DateTimeOffset dtOffset) => dtOffset.IsToday();

    /// <summary>
    /// 判断日期是否为今天
    /// </summary>
    /// <param name="dtOffset">时间偏移</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsToday(DateTimeOffset? dtOffset) => dtOffset.IsToday();

    #endregion

    #region IsWeekend(是否周末)

    /// <summary>
    /// 判断日期是否为周末。
    /// </summary>
    /// <param name="dt">时间</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWeekend(DateTime dt) => dt.IsWeekend();

    /// <summary>
    /// 判断日期是否为周末。
    /// </summary>
    /// <param name="dt">时间</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWeekend(DateTime? dt) => dt.IsWeekend();

    #endregion

    #region IsWeekday(是否工作日)

    /// <summary>
    /// 判断日期是否为工作日。
    /// </summary>
    /// <param name="dt">时间</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWeekday(DateTime dt) => dt.IsWeekday();

    /// <summary>
    /// 判断日期是否为工作日。
    /// </summary>
    /// <param name="dt">时间</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWeekday(DateTime? dt) => dt.IsWeekday();

    #endregion

    #region IsValid(是否有效时间)

    /// <summary>
    /// 判断指定时间是否有效时间
    /// </summary>
    /// <param name="dt">时间</param>
    public static bool IsValid(DateTime dt) => dt >= MinDate && dt <= MaxDate;

    #endregion
}