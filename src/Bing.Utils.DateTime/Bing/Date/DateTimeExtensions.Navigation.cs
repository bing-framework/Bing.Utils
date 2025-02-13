using System.Globalization;
using Bing.Date.DateUtils;

namespace Bing.Date;

// 日期时间 - 导航
public static partial class DateTimeExtensions
{
    #region Offset

    /// <summary>
    /// 日期偏移
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="offsetVal">偏移值</param>
    /// <param name="styles">日期偏移风格</param>
    public static DateTime OffsetBy(this DateTime dt, int offsetVal, DateOffsetStyles styles) =>
        styles switch
        {
            DateOffsetStyles.Day => DateTimeCalc.OffsetByDays(dt, offsetVal),
            DateOffsetStyles.Week => DateTimeCalc.OffsetByWeeks(dt, offsetVal),
            DateOffsetStyles.Month => DateTimeCalc.OffsetByMonths(dt, offsetVal, DateTimeOffsetOptions.Relatively),
            DateOffsetStyles.Quarters => DateTimeCalc.OffsetByQuarters(dt, offsetVal, DateTimeOffsetOptions.Relatively),
            DateOffsetStyles.Year => DateTimeCalc.OffsetByYears(dt, offsetVal, DateTimeOffsetOptions.Relatively),
            _ => DateTimeCalc.OffsetByDays(dt, offsetVal)
        };

    /// <summary>
    /// 时间偏移
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="offsetVal">偏移值</param>
    /// <param name="styles">时间偏移风格</param>
    public static DateTime OffsetBy(this DateTime dt, int offsetVal, TimeOffsetStyles styles) =>
        styles switch
        {
            TimeOffsetStyles.Millisecond => DateTimeCalc.OffsetByMillisecond(dt, offsetVal),
            TimeOffsetStyles.Second => DateTimeCalc.OffsetBySeconds(dt, offsetVal),
            TimeOffsetStyles.Minute => DateTimeCalc.OffsetByMinutes(dt, offsetVal),
            TimeOffsetStyles.Hour => DateTimeCalc.OffsetByHours(dt, offsetVal),
            _ => DateTimeCalc.OffsetBySeconds(dt, offsetVal)
        };

    #endregion

    #region Begin

    /// <summary>
    /// 获取某秒的开始时间，类似于“2023-01-03 01:01:01.000”。
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime BeginningOfSecond(this DateTime dt) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0, dt.Kind);

    /// <summary>
    /// 获取某分钟的开始时间，类似于“2023-01-03 01:01:00.000”。
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime BeginningOfMinute(this DateTime dt) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, 0, dt.Kind);

    /// <summary>
    /// 获取某小时的开始时间，类似于“2023-01-03 01:00:00.000”。
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime BeginningOfHour(this DateTime dt) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, 0, dt.Kind);

    /// <summary>
    /// 获取某天的开始时间，类似于“2023-01-03 00:00:00.000”。
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime BeginningOfDay(this DateTime dt) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, 0, 0, 0, 0, dt.Kind);

    /// <summary>
    /// 获取某天的开始时间，类似于“2023-01-03 00:00:00.000”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    public static DateTime BeginningOfDay(this DateTime dt, int timeZoneOffset) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, 0, 0, 0, 0, dt.Kind).AddHours(timeZoneOffset);

    /// <summary>
    /// 获取某周的开始时间
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime BeginningOfWeek(this DateTime dt) => dt.FirstDayOfWeek().BeginningOfDay();

    /// <summary>
    /// 获取某周的开始时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    public static DateTime BeginningOfWeek(this DateTime dt, int timeZoneOffset) => dt.FirstDayOfWeek().BeginningOfDay(timeZoneOffset);

    /// <summary>
    /// 获取某月的开始时间
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime BeginningOfMonth(this DateTime dt) => dt.FirstDayOfMonth().BeginningOfDay();

    /// <summary>
    /// 获取某月的开始时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    public static DateTime BeginningOfMonth(this DateTime dt, int timeZoneOffset) => dt.FirstDayOfMonth().BeginningOfDay(timeZoneOffset);

    /// <summary>
    /// 获取某季度的开始时间
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime BeginningOfQuarter(this DateTime dt) => dt.FirstDayOfQuarter().BeginningOfDay();

    /// <summary>
    /// 获取某季度的开始时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    public static DateTime BeginningOfQuarter(this DateTime dt, int timeZoneOffset) => dt.FirstDayOfQuarter().BeginningOfDay(timeZoneOffset);

    /// <summary>
    /// 获取某年的开始时间，类似于“2023-01-01 00:00:00”。
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime BeginningOfYear(this DateTime dt) => dt.FirstDayOfYear().BeginningOfDay();

    /// <summary>
    /// 获取某年的开始时间，类似于“2023-01-01 00:00:00”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    public static DateTime BeginningOfYear(this DateTime dt, int timeZoneOffset) => dt.FirstDayOfYear().BeginningOfDay(timeZoneOffset);

    #endregion

    #region End

    /// <summary>
    /// 获取某秒的结束时间，类似于“2023-01-03 01:01:01.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime EndOfSecond(this DateTime dt) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 999, dt.Kind);

    /// <summary>
    /// 获取某分钟的结束时间，类似于“2023-01-03 01:01:59.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime EndOfMinute(this DateTime dt) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 59, 999, dt.Kind);

    /// <summary>
    /// 获取某小时的结束时间，类似于“2023-01-03 01:59:59.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime EndOfHour(this DateTime dt) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, dt.Hour, 59, 59, 999, dt.Kind);

    /// <summary>
    /// 获取某天的结束时间，类似于“2023-01-03 23:59:59.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime EndOfDay(this DateTime dt) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, 23, 59, 59, 999, dt.Kind);

    /// <summary>
    /// 获取某天的结束时间，类似于“2023-01-03 23:59:59.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    public static DateTime EndOfDay(this DateTime dt, int timeZoneOffset) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, 23, 59, 59, 999, dt.Kind).AddHours(timeZoneOffset);

    /// <summary>
    /// 获取某周的结束时间
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime EndOfWeek(this DateTime dt) => dt.LastDayOfWeek().EndOfDay();

    /// <summary>
    /// 获取某周的结束时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    public static DateTime EndOfWeek(this DateTime dt, int timeZoneOffset) => dt.LastDayOfWeek().EndOfDay(timeZoneOffset);

    /// <summary>
    /// 获取某月的结束时间
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime EndOfMonth(this DateTime dt) => dt.LastDayOfMonth().EndOfDay();

    /// <summary>
    /// 获取某月的结束时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    public static DateTime EndOfMonth(this DateTime dt, int timeZoneOffset) => dt.LastDayOfMonth().EndOfDay(timeZoneOffset);

    /// <summary>
    /// 获取某季度的结束时间
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime EndOfQuarter(this DateTime dt) => dt.LastDayOfQuarter().EndOfDay();

    /// <summary>
    /// 获取某季度的结束时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    public static DateTime EndOfQuarter(this DateTime dt, int timeZoneOffset) => dt.LastDayOfQuarter().EndOfDay(timeZoneOffset);

    /// <summary>
    /// 获取某年的结束时间，类似于“2023-12-31 23:59:59.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime EndOfYear(this DateTime dt) => dt.LastDayOfYear().EndOfDay();

    /// <summary>
    /// 获取某年的结束时间，类似于“2023-12-31 23:59:59.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    public static DateTime EndOfYear(this DateTime dt, int timeZoneOffset) => dt.LastDayOfYear().EndOfDay(timeZoneOffset);

    #endregion

    #region Get

    /// <summary>
    /// 获取两个时间之间的间隔
    /// </summary>
    /// <param name="leftDt">时间</param>
    /// <param name="rightDt">时间</param>
    public static TimeSpan GetTimeSpan(this DateTime leftDt, DateTime rightDt) => rightDt - leftDt;

    /// <summary>
    /// 获取指定年份的第一天
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime FirstDayOfYear(this DateTime dt) => dt.SetDate(dt.Year, 1, 1);

    /// <summary>
    /// 获取指定年份的第一个星期指定的某一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime FirstDayOfYear(this DateTime dt, DayOfWeek dayOfWeek)
    {
        var ret = dt.FirstDayOfYear();
        return DayOfWeekCalc.TryDaysBetween(ret.DayOfWeek, dayOfWeek, out var day) && day > 0
            ? ret.AddDays(day)
            : ret;
    }

    /// <summary>
    /// 获取指定季度的第一天
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime FirstDayOfQuarter(this DateTime dt)
    {
        var currentQuarter = dt.GetQuarter();
        var month = 3 * currentQuarter - 2;
        return DateTimeFactory.Create(dt.Year, month, 1);
    }

    /// <summary>
    /// 获取指定季度的第一个星期指定的某一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime FirstDayOfQuarter(this DateTime dt, DayOfWeek dayOfWeek)
    {
        var ret = dt.FirstDayOfQuarter();
        return DayOfWeekCalc.TryDaysBetween(ret.DayOfWeek, dayOfWeek, out var day) && day > 0
            ? ret.AddDays(day)
            : ret;
    }

    /// <summary>
    /// 获取指定月份的第一天
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime FirstDayOfMonth(this DateTime dt) => dt.SetDay(1);

    /// <summary>
    /// 获取指定月份的第一个星期指定的某一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime FirstDayOfMonth(this DateTime dt, DayOfWeek dayOfWeek)
    {
        var ret = dt.FirstDayOfMonth();
        return DayOfWeekCalc.TryDaysBetween(ret.DayOfWeek, dayOfWeek, out var day) && day > 0
            ? ret.AddDays(day)
            : ret;
    }

    /// <summary>
    /// 获取指定星期的第一天
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime FirstDayOfWeek(this DateTime dt) => dt.FirstDayOfWeek(null);

    /// <summary>
    /// 获取指定星期的第一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="cultureInfo">区域信息</param>
    public static DateTime FirstDayOfWeek(this DateTime dt, CultureInfo cultureInfo)
    {
        cultureInfo ??= CultureInfo.CurrentCulture;
        var firstDayOfWeek = cultureInfo.DateTimeFormat.FirstDayOfWeek;
        var offset = dt.DayOfWeek - firstDayOfWeek < 0 ? 7 : 0;
        var numberOfDaysSinceBeginningOfTheWeek = dt.DayOfWeek + offset - firstDayOfWeek;
        return dt.AddDays(-numberOfDaysSinceBeginningOfTheWeek);
    }

    /// <summary>
    /// 获取指定年份的最后一天
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime LastDayOfYear(this DateTime dt) => dt.SetDate(dt.Year, 12, 31);

    /// <summary>
    /// 获取指定年份的最后一个星期指定的某一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime LastDayOfYear(this DateTime dt, DayOfWeek dayOfWeek)
    {
        var ret = dt.LastDayOfYear();
        return DayOfWeekCalc.TryDaysBetween(ret.DayOfWeek, dayOfWeek, out var day) && day == 0
            ? ret
            : ret.AddDays(-day);
    }

    /// <summary>
    /// 获取指定季度的最后一天
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime LastDayOfQuarter(this DateTime dt)
    {
        var currentQuarter = dt.GetQuarter();
        var month = 3 * currentQuarter;
        var day = DateTime.DaysInMonth(dt.Year, month);
        return DateTimeFactory.Create(dt.Year, month, day);
    }

    /// <summary>
    /// 获取指定季度的最后一个星期指定的某一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime LastDayOfQuarter(this DateTime dt, DayOfWeek dayOfWeek)
    {
        var ret = dt.LastDayOfQuarter();
        return DayOfWeekCalc.TryDaysBetween(ret.DayOfWeek, dayOfWeek, out var day) && day == 0
            ? ret
            : ret.AddDays(-day);
    }

    /// <summary>
    /// 获取指定月份的最后一天
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime LastDayOfMonth(this DateTime dt) => dt.SetDay(dt.DaysInMonth());

    /// <summary>
    /// 获取指定月份的最后一个星期指定的某一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime LastDayOfMonth(this DateTime dt, DayOfWeek dayOfWeek)
    {
        var ret = dt.LastDayOfMonth();
        return DayOfWeekCalc.TryDaysBetween(ret.DayOfWeek, dayOfWeek, out var day) && day == 0
            ? ret
            : ret.AddDays(-day);
    }

    /// <summary>
    /// 获取指定星期的最后一天
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime LastDayOfWeek(this DateTime dt) => dt.LastDayOfWeek(null);

    /// <summary>
    /// 获取指定星期的最后一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="cultureInfo">区域信息</param>
    public static DateTime LastDayOfWeek(this DateTime dt, CultureInfo cultureInfo) => dt.FirstDayOfWeek(cultureInfo).AddDays(6);

    /// <summary>
    /// 获取指定月份中的天数
    /// </summary>
    /// <param name="dt">时间</param>
    public static int DaysInMonth(this DateTime dt) => DateTime.DaysInMonth(dt.Year, dt.Month);

    /// <summary>
    /// 获取指定年份中的天数
    /// </summary>
    /// <param name="dt">时间</param>
    public static int DaysInYear(this DateTime dt) => DateTime.IsLeapYear(dt.Year) ? 366 : 365;

    /// <summary>
    /// 获取指定日期所属季度，从1开始计数
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>第几个季度</returns>
    public static int GetQuarter(this DateTime dt) => (dt.Month - 1) / 3 + 1;

    /// <summary>
    /// 获得指定日期所属季度
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>第几个季度枚举</returns>
    public static Quarter GetQuarterEnum(this DateTime dt)
    {
        var quarter = GetQuarter(dt);
        return quarter switch
        {
            1 => Quarter.Q1,
            2 => Quarter.Q2,
            3 => Quarter.Q3,
            4 => Quarter.Q4,
            _ => throw new ArgumentOutOfRangeException(nameof(quarter), "不支持的季度")
        };
    }

    /// <summary>
    /// 获取指定的星期是一年中的第几个星期
    /// </summary>
    /// <param name="dt">时间</param>
    public static int GetWeekOfYear(this DateTime dt) => GetWeekOfYear(dt, new DateTimeFormatInfo().CalendarWeekRule, new DateTimeFormatInfo().FirstDayOfWeek);

    /// <summary>
    /// 获取指定的星期是一年中的第几个星期
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="weekRule">星期规则</param>
    public static int GetWeekOfYear(this DateTime dt, CalendarWeekRule weekRule) => GetWeekOfYear(dt, weekRule, new DateTimeFormatInfo().FirstDayOfWeek);

    /// <summary>
    /// 获取指定的星期是一年中的第几个星期
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="firstDayOfWeek">星期几</param>
    public static int GetWeekOfYear(this DateTime dt, DayOfWeek firstDayOfWeek) => GetWeekOfYear(dt, new DateTimeFormatInfo().CalendarWeekRule, firstDayOfWeek);

    /// <summary>
    /// 获取指定的星期是一年中的第几个星期
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="weekRule">星期规则</param>
    /// <param name="firstDayOfWeek">星期几</param>
    public static int GetWeekOfYear(this DateTime dt, CalendarWeekRule weekRule, DayOfWeek firstDayOfWeek) => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dt, weekRule, firstDayOfWeek);

    #endregion

    #region Navigation

    /// <summary>
    /// 下一年（明年）
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime NextYear(this DateTime dt) => dt.OffsetBy(1, DateOffsetStyles.Year);

    /// <summary>
    /// 上一年（去年）
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime PreviousYear(this DateTime dt) => dt.OffsetBy(-1, DateOffsetStyles.Year);

    /// <summary>
    /// 下个季度
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime NextQuarter(this DateTime dt) => dt.OffsetBy(1, DateOffsetStyles.Quarters);

    /// <summary>
    /// 上个季度
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime PreviousQuarter(this DateTime dt) => dt.OffsetBy(-1, DateOffsetStyles.Quarters);

    /// <summary>
    /// 下个月
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime NextMonth(this DateTime dt) => dt.OffsetBy(1, DateOffsetStyles.Month);

    /// <summary>
    /// 上个月
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime PreviousMonth(this DateTime dt) => dt.OffsetBy(-1, DateOffsetStyles.Month);

    /// <summary>
    /// 下个星期
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime NextWeek(this DateTime dt) => dt.OffsetBy(1, DateOffsetStyles.Week);

    /// <summary>
    /// 上个星期
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime PreviousWeek(this DateTime dt) => dt.OffsetBy(-1, DateOffsetStyles.Week);

    /// <summary>
    /// 下一天（明天）
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime NextDay(this DateTime dt) => dt.OffsetBy(1, DateOffsetStyles.Day);

    /// <summary>
    /// 上一天（昨天）
    /// </summary>
    /// <param name="dt">时间</param>
    public static DateTime PreviousDay(this DateTime dt) => dt.OffsetBy(-1, DateOffsetStyles.Day);

    /// <summary>
    /// 明天
    /// </summary>
    /// <param name="dt">时间</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime Tomorrow(this DateTime dt) => dt.NextDay();

    /// <summary>
    /// 昨天
    /// </summary>
    /// <param name="dt">时间</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime Yesterday(this DateTime dt) => dt.PreviousDay();

    /// <summary>
    /// 下个星期几
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime NextDayOfWeek(this DateTime dt, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetOfDayOfWeek(dt, dayOfWeek, 1);

    /// <summary>
    /// 上个星期几
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime PreviousDayOfWeek(this DateTime dt, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetOfDayOfWeek(dt, dayOfWeek, -1);

    #endregion
}