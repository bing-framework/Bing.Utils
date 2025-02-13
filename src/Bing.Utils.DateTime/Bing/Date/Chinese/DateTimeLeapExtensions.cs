using System.Globalization;

namespace Bing.Date.Chinese;

/// <summary>
/// 中国日期扩展
/// </summary>
public static class DateTimeLeapExtensions
{
    /// <summary>
    /// 判断指定年份是否为闰年
    /// </summary>
    /// <param name="dt">日期</param>
    /// <returns>如果指定的年份是闰年，则返回true；否则返回false</returns>
    public static bool IsLeapYear(this DateTime dt) => ChineseDateHelper.IsLeapYear(null, dt);

    /// <summary>
    /// 判断指定年份是否为闰年
    /// </summary>
    /// <param name="dt">日期</param>
    /// <param name="calendar">中国农历日历</param>
    /// <returns>如果指定的年份是闰年，则返回true；否则返回false</returns>
    public static bool IsLeapYear(this DateTime dt, ChineseLunisolarCalendar calendar) => ChineseDateHelper.IsLeapYear(calendar, dt);

    /// <summary>
    /// 判断指定月份是否为闰月
    /// </summary>
    /// <param name="dt">日期</param>
    /// <returns>如果指定的月份是闰月，则返回true；否则返回false</returns>
    public static bool IsLeapMonth(this DateTime dt) => ChineseDateHelper.IsLeapMonth(null, dt);

    /// <summary>
    /// 判断指定月份是否为闰月
    /// </summary>
    /// <param name="dt">日期</param>
    /// <param name="calendar">中国农历日历</param>
    /// <returns>如果指定的月份是闰月，则返回true；否则返回false</returns>
    public static bool IsLeapMonth(this DateTime dt, ChineseLunisolarCalendar calendar) => ChineseDateHelper.IsLeapMonth(calendar, dt);

    /// <summary>
    /// 判断指定日期是否为闰日
    /// </summary>
    /// <param name="dt">日期</param>
    /// <returns>如果指定的日期是闰日，则返回true；否则返回false</returns>
    public static bool IsLeapDay(this DateTime dt) => ChineseDateHelper.IsLeapDay(null, dt);

    /// <summary>
    /// 判断指定日期是否为闰日
    /// </summary>
    /// <param name="dt">日期</param>
    /// <param name="calendar">中国农历日历</param>
    /// <returns>如果指定的日期是闰日，则返回true；否则返回false</returns>
    public static bool IsLeapDay(this DateTime dt, ChineseLunisolarCalendar calendar) => ChineseDateHelper.IsLeapDay(calendar, dt);
}