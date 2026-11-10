using System.Globalization;
using Bing.Date.DateUtils;

namespace Bing.Date;

/// <summary>
/// 日期时间扩展方法。
/// </summary>
public static partial class DateTimeExtensions
{
    #region Offset

    /// <summary>
    /// 日期偏移
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="offsetVal">偏移值</param>
    /// <param name="styles">日期偏移风格</param>
    /// <returns>按指定时间间隔偏移后的日期时间。</returns>
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
    /// <returns>按指定时间间隔偏移后的日期时间。</returns>
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
    /// <returns>清除秒以下 ticks，并保留 Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime BeginningOfSecond(this DateTime dt) => dt.AddTicks(-(dt.Ticks % TimeSpan.TicksPerSecond));

    /// <summary>
    /// 获取某分钟的开始时间，类似于“2023-01-03 01:01:00.000”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>清除分钟以下 ticks，并保留 Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime BeginningOfMinute(this DateTime dt) => dt.AddTicks(-(dt.Ticks % TimeSpan.TicksPerMinute));

    /// <summary>
    /// 获取某小时的开始时间，类似于“2023-01-03 01:00:00.000”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>清除小时以下 ticks，并保留 Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime BeginningOfHour(this DateTime dt) => dt.AddTicks(-(dt.Ticks % TimeSpan.TicksPerHour));

    /// <summary>
    /// 获取某天的开始时间，类似于“2023-01-03 00:00:00.000”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>午夜日期值，保留 Kind 和本地重复时间选择标记。</returns>
    public static DateTime BeginningOfDay(this DateTime dt) => dt.Date;

    /// <summary>
    /// 获取某天的开始时间，类似于“2023-01-03 00:00:00.000”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    /// <returns>当天午夜按指定小时数偏移后的钟面值，保留 Kind 和本地重复时间选择标记。</returns>
    /// <remarks>仅执行固定小时数的算术偏移，不解析时区规则。</remarks>
    public static DateTime BeginningOfDay(this DateTime dt, int timeZoneOffset) => dt.BeginningOfDay().AddHours(timeZoneOffset);

    /// <summary>
    /// 获取某周的开始时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在周起点的日期时间。</returns>
    public static DateTime BeginningOfWeek(this DateTime dt) => dt.FirstDayOfWeek().BeginningOfDay();

    /// <summary>
    /// 获取某周的开始时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    /// <returns>所在周起点的日期时间。</returns>
    public static DateTime BeginningOfWeek(this DateTime dt, int timeZoneOffset) => dt.FirstDayOfWeek().BeginningOfDay(timeZoneOffset);

    /// <summary>
    /// 获取某月的开始时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在月起点的日期时间。</returns>
    public static DateTime BeginningOfMonth(this DateTime dt) => dt.FirstDayOfMonth().BeginningOfDay();

    /// <summary>
    /// 获取某月的开始时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    /// <returns>所在月起点的日期时间。</returns>
    public static DateTime BeginningOfMonth(this DateTime dt, int timeZoneOffset) => dt.FirstDayOfMonth().BeginningOfDay(timeZoneOffset);

    /// <summary>
    /// 获取某季度的开始时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在季度起点的日期时间。</returns>
    public static DateTime BeginningOfQuarter(this DateTime dt) => dt.FirstDayOfQuarter().BeginningOfDay();

    /// <summary>
    /// 获取某季度的开始时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    /// <returns>所在季度起点的日期时间。</returns>
    public static DateTime BeginningOfQuarter(this DateTime dt, int timeZoneOffset) => dt.FirstDayOfQuarter().BeginningOfDay(timeZoneOffset);

    /// <summary>
    /// 获取某年的开始时间，类似于“2023-01-01 00:00:00”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在年起点的日期时间。</returns>
    public static DateTime BeginningOfYear(this DateTime dt) => dt.FirstDayOfYear().BeginningOfDay();

    /// <summary>
    /// 获取某年的开始时间，类似于“2023-01-01 00:00:00”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    /// <returns>所在年起点的日期时间。</returns>
    public static DateTime BeginningOfYear(this DateTime dt, int timeZoneOffset) => dt.FirstDayOfYear().BeginningOfDay(timeZoneOffset);

    #endregion

    #region End

    /// <summary>
    /// 获取某秒的结束时间，类似于“2023-01-03 01:01:01.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在秒最后一个刻度的日期时间。</returns>
    public static DateTime EndOfSecond(this DateTime dt) => dt.AddTicks(TimeSpan.TicksPerSecond - 1 - dt.Ticks % TimeSpan.TicksPerSecond);

    /// <summary>
    /// 获取某分钟的结束时间，类似于“2023-01-03 01:01:59.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在分钟最后一个刻度的日期时间。</returns>
    public static DateTime EndOfMinute(this DateTime dt) => dt.AddTicks(TimeSpan.TicksPerMinute - 1 - dt.Ticks % TimeSpan.TicksPerMinute);

    /// <summary>
    /// 获取某小时的结束时间，类似于“2023-01-03 01:59:59.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在小时最后一个刻度的日期时间。</returns>
    public static DateTime EndOfHour(this DateTime dt) => dt.AddTicks(TimeSpan.TicksPerHour - 1 - dt.Ticks % TimeSpan.TicksPerHour);

    /// <summary>
    /// 获取某天的结束时间，类似于“2023-01-03 23:59:59.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在日最后一个刻度的日期时间。</returns>
    public static DateTime EndOfDay(this DateTime dt) => dt.AddTicks(TimeSpan.TicksPerDay - 1 - dt.Ticks % TimeSpan.TicksPerDay);

    /// <summary>
    /// 获取某天的结束时间，类似于“2023-01-03 23:59:59.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    /// <returns>所在日最后一个刻度的日期时间。</returns>
    public static DateTime EndOfDay(this DateTime dt, int timeZoneOffset) => dt.EndOfDay().AddHours(timeZoneOffset);

    /// <summary>
    /// 获取某周的结束时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在周最后一个刻度的日期时间。</returns>
    public static DateTime EndOfWeek(this DateTime dt)
    {
        var first = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        var days = ((int)first + 6 - (int)dt.DayOfWeek + 7) % 7;
        var remainingDays = (DateTime.MaxValue.Ticks - dt.Date.Ticks) / TimeSpan.TicksPerDay;
        return days > remainingDays
            ? DateTime.SpecifyKind(DateTime.MaxValue, dt.Kind)
            : dt.Date.AddDays(days).EndOfDay();
    }

    /// <summary>
    /// 获取某周的结束时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    /// <returns>所在周最后一个刻度的日期时间。</returns>
    public static DateTime EndOfWeek(this DateTime dt, int timeZoneOffset) => dt.EndOfWeek().AddHours(timeZoneOffset);

    /// <summary>
    /// 获取某月的结束时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在月最后一个刻度的日期时间。</returns>
    public static DateTime EndOfMonth(this DateTime dt) => dt.LastDayOfMonth().EndOfDay();

    /// <summary>
    /// 获取某月的结束时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    /// <returns>所在月最后一个刻度的日期时间。</returns>
    public static DateTime EndOfMonth(this DateTime dt, int timeZoneOffset) => dt.LastDayOfMonth().EndOfDay(timeZoneOffset);

    /// <summary>
    /// 获取某季度的结束时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在季度最后一个刻度的日期时间。</returns>
    public static DateTime EndOfQuarter(this DateTime dt) => dt.LastDayOfQuarter().EndOfDay();

    /// <summary>
    /// 获取某季度的结束时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    /// <returns>所在季度最后一个刻度的日期时间。</returns>
    public static DateTime EndOfQuarter(this DateTime dt, int timeZoneOffset) => dt.LastDayOfQuarter().EndOfDay(timeZoneOffset);

    /// <summary>
    /// 获取某年的结束时间，类似于“2023-12-31 23:59:59.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在年最后一个刻度的日期时间。</returns>
    public static DateTime EndOfYear(this DateTime dt) => dt.LastDayOfYear().EndOfDay();

    /// <summary>
    /// 获取某年的结束时间，类似于“2023-12-31 23:59:59.999”。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="timeZoneOffset">时区偏移量</param>
    /// <returns>所在年最后一个刻度的日期时间。</returns>
    public static DateTime EndOfYear(this DateTime dt, int timeZoneOffset) => dt.LastDayOfYear().EndOfDay(timeZoneOffset);

    #endregion

    #region Get

    /// <summary>
    /// 获取两个时间之间的间隔
    /// </summary>
    /// <param name="leftDt">时间</param>
    /// <param name="rightDt">时间</param>
    /// <returns>右侧日期时间减去左侧日期时间的间隔，可以为负值。</returns>
    public static TimeSpan GetTimeSpan(this DateTime leftDt, DateTime rightDt) => rightDt - leftDt;

    /// <summary>
    /// 获取指定年份的第一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在年第一天的日期时间。</returns>
    public static DateTime FirstDayOfYear(this DateTime dt) => dt.SetDate(dt.Year, 1, 1);

    /// <summary>
    /// 获取指定年份的第一个星期指定的某一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>定位到指定星期后的日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">dayOfWeek 不是有效的星期枚举。</exception>
    public static DateTime FirstDayOfYear(this DateTime dt, DayOfWeek dayOfWeek)
    {
        DayOfWeekCalc.Validate(dayOfWeek, nameof(dayOfWeek));
        var ret = dt.FirstDayOfYear();
        return ret.AddDays(DayOfWeekCalc.DaysBetween(ret.DayOfWeek, dayOfWeek));
    }

    /// <summary>
    /// 获取指定季度的第一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>季度第一天的午夜，保留 Kind 和本地重复时间选择标记。</returns>
    public static DateTime FirstDayOfQuarter(this DateTime dt)
    {
        var currentQuarter = dt.GetQuarter();
        var month = 3 * currentQuarter - 2;
        return dt.SetDate(dt.Year, month, 1).Date;
    }

    /// <summary>
    /// 获取指定季度的第一个星期指定的某一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>定位到指定星期后的日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">dayOfWeek 不是有效的星期枚举。</exception>
    public static DateTime FirstDayOfQuarter(this DateTime dt, DayOfWeek dayOfWeek)
    {
        DayOfWeekCalc.Validate(dayOfWeek, nameof(dayOfWeek));
        var ret = dt.FirstDayOfQuarter();
        return ret.AddDays(DayOfWeekCalc.DaysBetween(ret.DayOfWeek, dayOfWeek));
    }

    /// <summary>
    /// 获取指定月份的第一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在月第一天的日期时间。</returns>
    public static DateTime FirstDayOfMonth(this DateTime dt) => dt.SetDay(1);

    /// <summary>
    /// 获取指定月份的第一个星期指定的某一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>定位到指定星期后的日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">dayOfWeek 不是有效的星期枚举。</exception>
    public static DateTime FirstDayOfMonth(this DateTime dt, DayOfWeek dayOfWeek)
    {
        DayOfWeekCalc.Validate(dayOfWeek, nameof(dayOfWeek));
        var ret = dt.FirstDayOfMonth();
        return ret.AddDays(DayOfWeekCalc.DaysBetween(ret.DayOfWeek, dayOfWeek));
    }

    /// <summary>
    /// 获取指定星期的第一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在周第一天的日期时间。</returns>
    public static DateTime FirstDayOfWeek(this DateTime dt) => dt.FirstDayOfWeek(null);

    /// <summary>
    /// 获取指定星期的第一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="cultureInfo">区域信息</param>
    /// <returns>所在周第一天的日期时间。</returns>
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
    /// <returns>所在年最后一天的日期时间。</returns>
    public static DateTime LastDayOfYear(this DateTime dt) => dt.SetDate(dt.Year, 12, 31);

    /// <summary>
    /// 获取指定年份的最后一个星期指定的某一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>定位到指定星期后的日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">dayOfWeek 不是有效的星期枚举。</exception>
    public static DateTime LastDayOfYear(this DateTime dt, DayOfWeek dayOfWeek)
    {
        DayOfWeekCalc.Validate(dayOfWeek, nameof(dayOfWeek));
        var ret = dt.LastDayOfYear();
        return ret.AddDays(-DayOfWeekCalc.DaysBetween(dayOfWeek, ret.DayOfWeek));
    }

    /// <summary>
    /// 获取指定季度的最后一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>季度最后一天的午夜，保留 Kind 和本地重复时间选择标记。</returns>
    public static DateTime LastDayOfQuarter(this DateTime dt)
    {
        var currentQuarter = dt.GetQuarter();
        var month = 3 * currentQuarter;
        var day = DateTime.DaysInMonth(dt.Year, month);
        return dt.SetDate(dt.Year, month, day).Date;
    }

    /// <summary>
    /// 获取指定季度的最后一个星期指定的某一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>定位到指定星期后的日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">dayOfWeek 不是有效的星期枚举。</exception>
    public static DateTime LastDayOfQuarter(this DateTime dt, DayOfWeek dayOfWeek)
    {
        DayOfWeekCalc.Validate(dayOfWeek, nameof(dayOfWeek));
        var ret = dt.LastDayOfQuarter();
        return ret.AddDays(-DayOfWeekCalc.DaysBetween(dayOfWeek, ret.DayOfWeek));
    }

    /// <summary>
    /// 获取指定月份的最后一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在月最后一天的日期时间。</returns>
    public static DateTime LastDayOfMonth(this DateTime dt) => dt.SetDay(dt.DaysInMonth());

    /// <summary>
    /// 获取指定月份的最后一个星期指定的某一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>定位到指定星期后的日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">dayOfWeek 不是有效的星期枚举。</exception>
    public static DateTime LastDayOfMonth(this DateTime dt, DayOfWeek dayOfWeek)
    {
        DayOfWeekCalc.Validate(dayOfWeek, nameof(dayOfWeek));
        var ret = dt.LastDayOfMonth();
        return ret.AddDays(-DayOfWeekCalc.DaysBetween(dayOfWeek, ret.DayOfWeek));
    }

    /// <summary>
    /// 获取指定星期的最后一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在周最后一天的日期时间。</returns>
    public static DateTime LastDayOfWeek(this DateTime dt) => dt.LastDayOfWeek(null);

    /// <summary>
    /// 获取指定星期的最后一天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="cultureInfo">区域信息</param>
    /// <returns>所在周最后一天的日期时间。</returns>
    public static DateTime LastDayOfWeek(this DateTime dt, CultureInfo cultureInfo)
    {
        var first = dt.FirstDayOfWeek(cultureInfo);
        // 最后一个可表示的周可能不足七天；保持输入的时分秒和 Kind。
        var days = Math.Min(6, (DateTime.MaxValue.Date - first.Date).Days);
        return first.AddDays(days);
    }

    /// <summary>
    /// 获取指定月份中的天数
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在月份包含的自然日数。</returns>
    public static int DaysInMonth(this DateTime dt) => DateTime.DaysInMonth(dt.Year, dt.Month);

    /// <summary>
    /// 获取指定年份中的天数
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>所在年份包含的自然日数。</returns>
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
    /// <returns>按指定周历规则计算的周号。</returns>
    public static int GetWeekOfYear(this DateTime dt) => GetWeekOfYear(dt, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);

    /// <summary>
    /// 获取指定的星期是一年中的第几个星期
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="weekRule">星期规则</param>
    /// <returns>按指定周历规则计算的周号。</returns>
    public static int GetWeekOfYear(this DateTime dt, CalendarWeekRule weekRule) => GetWeekOfYear(dt, weekRule, DayOfWeek.Sunday);

    /// <summary>
    /// 获取指定的星期是一年中的第几个星期
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="firstDayOfWeek">星期几</param>
    /// <returns>按指定周历规则计算的周号。</returns>
    public static int GetWeekOfYear(this DateTime dt, DayOfWeek firstDayOfWeek) => GetWeekOfYear(dt, CalendarWeekRule.FirstDay, firstDayOfWeek);

    /// <summary>
    /// 获取指定的星期是一年中的第几个星期
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="weekRule">星期规则</param>
    /// <param name="firstDayOfWeek">星期几</param>
    /// <returns>按指定周历规则计算的周号。</returns>
    public static int GetWeekOfYear(this DateTime dt, CalendarWeekRule weekRule, DayOfWeek firstDayOfWeek) => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dt, weekRule, firstDayOfWeek);

    #endregion

    #region Navigation

    /// <summary>
    /// 下一年（明年）
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>向后偏移一个年后的日期时间。</returns>
    public static DateTime NextYear(this DateTime dt) => dt.OffsetBy(1, DateOffsetStyles.Year);

    /// <summary>
    /// 上一年（去年）
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>向前偏移一个年后的日期时间。</returns>
    public static DateTime PreviousYear(this DateTime dt) => dt.OffsetBy(-1, DateOffsetStyles.Year);

    /// <summary>
    /// 下个季度
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>向后偏移一个季度后的日期时间。</returns>
    public static DateTime NextQuarter(this DateTime dt) => dt.OffsetBy(1, DateOffsetStyles.Quarters);

    /// <summary>
    /// 上个季度
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>向前偏移一个季度后的日期时间。</returns>
    public static DateTime PreviousQuarter(this DateTime dt) => dt.OffsetBy(-1, DateOffsetStyles.Quarters);

    /// <summary>
    /// 下个月
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>向后偏移一个月后的日期时间。</returns>
    public static DateTime NextMonth(this DateTime dt) => dt.OffsetBy(1, DateOffsetStyles.Month);

    /// <summary>
    /// 上个月
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>向前偏移一个月后的日期时间。</returns>
    public static DateTime PreviousMonth(this DateTime dt) => dt.OffsetBy(-1, DateOffsetStyles.Month);

    /// <summary>
    /// 下个星期
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>向后偏移一个周后的日期时间。</returns>
    public static DateTime NextWeek(this DateTime dt) => dt.OffsetBy(1, DateOffsetStyles.Week);

    /// <summary>
    /// 上个星期
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>向前偏移一个周后的日期时间。</returns>
    public static DateTime PreviousWeek(this DateTime dt) => dt.OffsetBy(-1, DateOffsetStyles.Week);

    /// <summary>
    /// 下一天（明天）
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>向后偏移一个日后的日期时间。</returns>
    public static DateTime NextDay(this DateTime dt) => dt.OffsetBy(1, DateOffsetStyles.Day);

    /// <summary>
    /// 上一天（昨天）
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>向前偏移一个日后的日期时间。</returns>
    public static DateTime PreviousDay(this DateTime dt) => dt.OffsetBy(-1, DateOffsetStyles.Day);

    /// <summary>
    /// 明天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>下一自然日的日期时间。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime Tomorrow(this DateTime dt) => dt.NextDay();

    /// <summary>
    /// 昨天
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>上一自然日的日期时间。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime Yesterday(this DateTime dt) => dt.PreviousDay();

    /// <summary>
    /// 下个星期几
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>定位到指定星期后的日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">dayOfWeek 不是有效的星期枚举。</exception>
    public static DateTime NextDayOfWeek(this DateTime dt, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetOfDayOfWeek(dt, dayOfWeek, 1);

    /// <summary>
    /// 上个星期几
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>定位到指定星期后的日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">dayOfWeek 不是有效的星期枚举。</exception>
    public static DateTime PreviousDayOfWeek(this DateTime dt, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetOfDayOfWeek(dt, dayOfWeek, -1);

    #endregion
}
