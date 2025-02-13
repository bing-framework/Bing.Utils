using System.Globalization;

namespace Bing.Date.Chinese;

/// <summary>
/// 中国农历日期
/// </summary>
public class ChineseDateInfo
{
    /// <summary>
    /// 内部日期时间
    /// </summary>
    private DateTime InternalDateTime { get; }

    /// <summary>
    /// 中国农历日历
    /// </summary>
    private ChineseLunisolarCalendar Calendar { get; }

    /// <summary>
    /// 初始化一个<see cref="ChineseDateInfo"/>类型的实例。
    /// </summary>
    /// <param name="dt">日期时间</param>
    public ChineseDateInfo(DateTime dt)
    {
        InternalDateTime = dt;
        Calendar = new ChineseLunisolarCalendar();
    }

    /// <summary>
    /// 初始化一个<see cref="ChineseDateInfo"/>类型的实例。
    /// </summary>
    /// <param name="dt">日期时间</param>
    /// <param name="calendar">中国农历日历</param>
    public ChineseDateInfo(DateTime dt, ChineseLunisolarCalendar calendar)
    {
        InternalDateTime = dt;
        Calendar = calendar;
    }

    /// <summary>
    /// 内部日期时间
    /// </summary>
    internal DateTime InternalTime => InternalDateTime;

    /// <summary>
    /// 获取农历年份
    /// </summary>
    public int ChineseYear => Calendar.GetYear(InternalDateTime);

    /// <summary>
    /// 获取农历月份
    /// </summary>
    public int ChineseMonth => Calendar.GetMonth(InternalDateTime);

    /// <summary>
    /// 获取农历日份
    /// </summary>
    public int ChineseDay => Calendar.GetDayOfMonth(InternalDateTime);

    /// <summary>
    /// 当前年份是否为闰年。
    /// </summary>
    /// <returns>如果当前年份是闰年，则返回true；否则返回false。</returns>
    public bool IsLeapYear() => ChineseDateHelper.IsLeapYear(Calendar, InternalDateTime);

    /// <summary>
    /// 当前月份是否为闰月。
    /// </summary>
    /// <returns>如果当前月份是闰月，则返回true；否则返回false。</returns>
    public bool IsLeapMonth() => ChineseDateHelper.IsLeapMonth(Calendar, InternalDateTime);

    /// <summary>
    /// 当前日期是否为闰日。
    /// </summary>
    /// <returns>如果当前日期是闰日，则返回true；否则返回false。</returns>
    public bool IsLeapDay() => ChineseDateHelper.IsLeapDay(Calendar, InternalDateTime);

    /// <summary>
    /// 当前日期是否为周末。
    /// </summary>
    /// <returns>如果当前日期是周末，则返回true；否则返回false。</returns>
    public bool IsWeekend() => InternalDateTime.IsWeekend();

    /// <summary>
    /// 当前日期是否为工作日。
    /// </summary>
    /// <returns>如果当前日期是工作日，则返回true；否则返回false。</returns>
    public bool IsWorkDay() => InternalDateTime.IsWeekday();

    /// <summary>
    /// 获取干支年。
    /// </summary>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>干支年</returns>
    public string GetSexagenaryYear(bool traditionalChineseCharacters = false) => ChineseDateHelper.GetSexagenaryYear(Calendar, InternalDateTime, traditionalChineseCharacters);

    /// <summary>
    /// 获取汉字农历年份。
    /// </summary>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>农历年份</returns>
    public string GetChineseYear(bool traditionalChineseCharacters = false) => ChineseDateHelper.GetChineseYear(InternalDateTime, traditionalChineseCharacters);

    /// <summary>
    /// 获取汉字农历月份。
    /// </summary>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>农历月份</returns>
    public string GetChineseMonth(bool traditionalChineseCharacters = false) => ChineseDateHelper.GetChineseMonth(Calendar, InternalDateTime, traditionalChineseCharacters);

    /// <summary>
    /// 获取汉字农历日份。
    /// </summary>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>农历日份</returns>
    public string GetChineseDay(bool traditionalChineseCharacters = false) => ChineseDateHelper.GetChineseDay(Calendar, InternalDateTime, traditionalChineseCharacters);

    /// <summary>
    /// 获取农历日期。
    /// </summary>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>返回格式化的农历日期字符串。</returns>
    public string GetChineseDate(bool traditionalChineseCharacters = false) => $"{GetChineseMonth(traditionalChineseCharacters)}{GetChineseDay(traditionalChineseCharacters)}";

    /// <summary>
    /// 获取农历日期，包含年份。
    /// </summary>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>返回格式化的农历日期字符串，包含年份。</returns>
    public string GetChineseDateWithYear(bool traditionalChineseCharacters = false) => $"{GetChineseYear(traditionalChineseCharacters)}{GetChineseDate(traditionalChineseCharacters)}";

    /// <summary>
    /// 获取农历日期，包含干支年。
    /// </summary>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>返回格式化的农历日期字符串，包含干支年。</returns>
    public string GetChineseDateWithSexagenaryYear(bool traditionalChineseCharacters = false) => $"{GetSexagenaryYear(traditionalChineseCharacters)}{GetChineseDate(traditionalChineseCharacters)}";

    /// <summary>
    /// 获取节气。
    /// </summary>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>如果当前日期是节气，则返回节气的名称；否则返回空字符串。</returns>
    public string GetSolarTerm(bool traditionalChineseCharacters = false) => ChineseSolarTermHelper.GetSolarTerm(Calendar, InternalDateTime, traditionalChineseCharacters);

    /// <summary>
    /// 获取上一个节气。
    /// </summary>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>如果找到符合条件的节气，则返回节气的名称；否则返回空字符串。</returns>
    public string GetLastSolarTerm(bool traditionalChineseCharacters = false) => ChineseSolarTermHelper.GetLastSolarTerm(Calendar, InternalDateTime, traditionalChineseCharacters);

    /// <summary>
    /// 获取上一个节气和具体的公历时间。
    /// </summary>
    /// <param name="dt">计算得到的符合条件的节气日期。</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>如果找到符合条件的节气，则返回节气的名称；否则返回空字符串。</returns>
    public string GetLastSolarTerm(out DateTime dt, bool traditionalChineseCharacters = false) => ChineseSolarTermHelper.GetLastSolarTerm(Calendar, InternalDateTime, out dt, traditionalChineseCharacters);

    /// <summary>
    /// 获取下一个节气。
    /// </summary>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>如果找到符合条件的节气，则返回节气的名称；否则返回空字符串。</returns>
    public string GetNextSolarTerm(bool traditionalChineseCharacters = false) => ChineseSolarTermHelper.GetNextSolarTerm(Calendar, InternalDateTime, traditionalChineseCharacters);

    /// <summary>
    /// 获取下一个节气和具体的公历时间。
    /// </summary>
    /// <param name="dt">计算得到的符合条件的节气日期。</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>如果找到符合条件的节气，则返回节气的名称；否则返回空字符串。</returns>
    public string GetNextSolarTerm(out DateTime dt, bool traditionalChineseCharacters = false) => ChineseSolarTermHelper.GetNextSolarTerm(Calendar, InternalDateTime, out dt, traditionalChineseCharacters);

    /// <summary>
    /// 获取明天的农历日期信息。
    /// </summary>
    /// <returns>返回一个新的<see cref="ChineseDateInfo"/>实例，表示明天的日期。</returns>
    public ChineseDateInfo Tomorrow() => AddDays(1);

    /// <summary>
    /// 获取昨天的农历日期信息。
    /// </summary>
    /// <returns>返回一个新的<see cref="ChineseDateInfo"/>实例，表示昨天的日期。</returns>
    public ChineseDateInfo Yesterday() => AddDays(-1);

    /// <summary>
    /// 添加指定天数到当前农历日期。
    /// </summary>
    /// <param name="days">要添加的天数。</param>
    /// <returns>返回一个新的<see cref="ChineseDateInfo"/>实例，表示添加天数后的日期。</returns>
    public ChineseDateInfo AddDays(int days) => new(InternalDateTime.AddDays(days), Calendar);

    /// <summary>
    /// 添加指定工作日天数到当前农历日期。
    /// </summary>
    /// <param name="days">要添加的工作日天数。如果天数小于等于0，则默认添加1天。</param>
    /// <returns>返回一个新的<see cref="ChineseDateInfo"/>实例，表示添加工作日天数后的日期。</returns>
    public ChineseDateInfo AddWorkDays(int days)
    {
        var cc = new ChineseDateInfo(InternalDateTime);
        if (days <= 0)
            days = 1;
        while (true)
        {
            cc = cc.Tomorrow();
            if (cc.IsWorkDay())
                days--;
            if (days == 0)
                return cc;
        }
    }

    /// <summary>
    /// 添加指定月数到当前农历日期。
    /// </summary>
    /// <param name="months">要添加的月数。</param>
    /// <returns>返回一个新的<see cref="ChineseDateInfo"/>实例，表示添加月数后的日期。</returns>
    public ChineseDateInfo AddMonths(int months) => new(InternalDateTime.AddMonths(months));

    /// <summary>
    /// 获取农历年中的总天数。
    /// </summary>
    /// <returns>返回指定农历年中的总天数。</returns>
    public int GetDaysInYear() => Calendar.GetDaysInYear(InternalDateTime.Year);

    /// <summary>
    /// 获取农历月中的总天数。
    /// </summary>
    /// <returns>返回指定农历月中的总天数。</returns>
    public int GetDaysInMonth() => Calendar.GetDaysInMonth(InternalDateTime.Year, InternalDateTime.Month);

    /// <summary>
    /// 农历年中的第几天。
    /// </summary>
    /// <returns>返回指定日期在农历年中的天数序号。</returns>
    public int DayOfYear() => Calendar.GetDayOfYear(InternalDateTime);

    /// <summary>
    /// 农历月中的第几天。
    /// </summary>
    /// <returns>返回指定日期在农历月中的天数序号。</returns>
    public int GetDayOfMonth() => Calendar.GetDayOfMonth(InternalDateTime);

    /// <summary>
    /// 一周中的第几天。
    /// </summary>
    /// <returns>返回指定日期是一周中的哪一天。</returns>
    public DayOfWeek GetDayOfWeek() => Calendar.GetDayOfWeek(InternalDateTime);

    /// <summary>
    /// 转换为 <see cref="DateTime"/>。
    /// </summary>
    public DateTime ToDateTime() => InternalDateTime;

    /// <summary>
    /// 创建一个新的<see cref="ChineseDateInfo"/>实例。
    /// </summary>
    /// <param name="year">公历年份</param>
    /// <param name="month">公历月份</param>
    /// <param name="day">公历日</param>
    /// <returns>返回一个新的<see cref="ChineseDateInfo"/>实例，表示指定的公历日期。</returns>
    public static ChineseDateInfo Of(int year, int month, int day) => new(DateTimeFactory.Create(year, month, day));

    /// <summary>
    /// 创建一个新的<see cref="ChineseDateInfo"/>实例，基于农历日期。
    /// </summary>
    /// <param name="year">农历年份</param>
    /// <param name="month">农历月份</param>
    /// <param name="day">农历日</param>
    /// <returns>返回一个新的<see cref="ChineseDateInfo"/>实例，表示指定的农历日期。</returns>
    public static ChineseDateInfo OfLunar(int year, int month, int day)
    {
        var calendar = new ChineseLunisolarCalendar();
        return new(calendar.ToDateTime(year, month, day, 0, 0, 0, 0));
    }
}