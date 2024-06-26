using System.Globalization;
using Bing.Extensions;

namespace Bing.Date;

/// <summary>
/// 日期(<see cref="DateTime"/>) 判断器
/// </summary>
public static class DateJudge
{
    /// <summary>
    /// 最小日期
    /// </summary>
    internal static readonly DateTime MinDate = new(1900, 1, 1);

    /// <summary>
    /// 最大日期
    /// </summary>
    internal static readonly DateTime MaxDate = new(9999, 12, 31, 23, 59, 59, 999);

    /// <summary>
    /// 日历
    /// </summary>
    private static readonly Calendar _calendar = CultureInfo.InvariantCulture.Calendar;

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

    #region IsSameDay(是否为同一天)

    /// <summary>
    /// 判断是否为同一天
    /// </summary>
    /// <param name="date">日期1</param>
    /// <param name="compareDate">日期2</param>
    /// <returns>是否为同一天</returns>
    public static bool IsSameDay(DateTime? date, DateTime? compareDate)
    {
        if (date is null || compareDate is null)
            return false;
        return date.Value.Date == compareDate.Value.Date;
    }

    #endregion

    #region IsSameWeek(是否为同一周)

    /// <summary>
    /// 判断是否为同一周
    /// </summary>
    /// <param name="date">日期1</param>
    /// <param name="compareDate">日期2</param>
    /// <param name="firstDayOfWeek"></param>
    /// <returns>是否为同一周</returns>
    public static bool IsSameWeek(DateTime? date, DateTime? compareDate, DayOfWeek firstDayOfWeek)
    {
        if (date is null || compareDate is null)
            return false;
        return IsSameYear(date, compareDate) 
               && date.Value.GetWeekOfYear(CalendarWeekRule.FirstDay, firstDayOfWeek) == compareDate.Value.GetWeekOfYear(CalendarWeekRule.FirstDay, firstDayOfWeek);
    }

    #endregion

    #region IsSameMonth(是否为同一月)

    /// <summary>
    /// 判断是否为同一月
    /// </summary>
    /// <param name="date">日期1</param>
    /// <param name="compareDate">日期2</param>
    /// <returns>是否为同一月</returns>
    public static bool IsSameMonth(DateTime? date, DateTime? compareDate)
    {
        if (date is null || compareDate is null)
            return false;
        return IsSameYear(date, compareDate) && date.Value.Month == compareDate.Value.Month;
    }

    #endregion

    #region IsSameYear(是否为同一年)

    /// <summary>
    /// 判断是否为同一年
    /// </summary>
    /// <param name="date">日期1</param>
    /// <param name="compareDate">日期2</param>
    /// <returns>是否为同一年</returns>
    public static bool IsSameYear(DateTime? date, DateTime? compareDate)
    {
        if (date is null || compareDate is null)
            return false;
        return date.Value.Year == compareDate.Value.Year;
    }

    #endregion

    #region IsValid(是否有效时间)

    /// <summary>
    /// 判断指定时间是否有效时间
    /// </summary>
    /// <param name="dt">时间</param>
    public static bool IsValid(DateTime dt) => dt >= MinDate && dt <= MaxDate;

    #endregion
}