using System.Globalization;

namespace Bing.Date.Chinese;
/// <summary>
/// 中国日期扩展
/// </summary>
/// <remarks>按输入日期字段判断，不根据 DateTime.Kind 换算时区。</remarks>
public static class DateTimeLeapExtensions
{
    /// <summary>
    /// 判断日期所属农历年份是否为闰年。
    /// </summary>
    /// <param name="dt">日期</param>
    /// <returns>农历年份存在闰月时为 true，否则为 false。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期超出农历支持范围。</exception>
    public static bool IsLeapYear(this DateTime dt) => ChineseLunarCalendar.IsLeapYear(dt);
    /// <summary>
    /// 判断指定年份是否为闰年
    /// </summary>
    /// <param name="dt">日期</param>
    /// <param name="calendar">中国农历日历</param>
    /// <returns>如果指定的年份是闰年，则返回true；否则返回false</returns>
    /// <exception cref="ArgumentNullException">历法为 null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">日期超出历法支持范围。</exception>
    public static bool IsLeapYear(this DateTime dt, ChineseLunisolarCalendar calendar) => ChineseDateHelper.IsLeapYear(calendar, dt);
    /// <summary>
    /// 判断日期是否处于农历闰月。
    /// </summary>
    /// <param name="dt">日期</param>
    /// <returns>日期处于闰月时为 true，否则为 false。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期超出农历支持范围。</exception>
    public static bool IsLeapMonth(this DateTime dt) => ChineseLunarCalendar.IsLeapMonth(dt);
    /// <summary>
    /// 判断指定月份是否为闰月
    /// </summary>
    /// <param name="dt">日期</param>
    /// <param name="calendar">中国农历日历</param>
    /// <returns>如果指定的月份是闰月，则返回true；否则返回false</returns>
    /// <exception cref="ArgumentNullException">历法为 null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">日期超出历法支持范围。</exception>
    public static bool IsLeapMonth(this DateTime dt, ChineseLunisolarCalendar calendar) => ChineseDateHelper.IsLeapMonth(calendar, dt);
    /// <summary>
    /// 判断日期是否为农历闰日。
    /// </summary>
    /// <param name="dt">日期</param>
    /// <returns>日期为农历闰日时为 true，否则为 false。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期超出农历支持范围。</exception>
    public static bool IsLeapDay(this DateTime dt) => ChineseLunarCalendar.IsLeapDay(dt);
    /// <summary>
    /// 判断指定日期是否为闰日
    /// </summary>
    /// <param name="dt">日期</param>
    /// <param name="calendar">中国农历日历</param>
    /// <returns>如果指定的日期是闰日，则返回true；否则返回false</returns>
    /// <exception cref="ArgumentNullException">历法为 null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">日期超出历法支持范围。</exception>
    public static bool IsLeapDay(this DateTime dt, ChineseLunisolarCalendar calendar) => ChineseDateHelper.IsLeapDay(calendar, dt);
}
