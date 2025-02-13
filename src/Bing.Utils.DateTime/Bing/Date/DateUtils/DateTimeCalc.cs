using Bing.Conversions;
using NodaTime;

namespace Bing.Date.DateUtils;

/// <summary>
/// 日期时间计算帮助类
/// </summary>
internal static class DateTimeCalcHelper
{
    /// <summary>
    /// 获取目标天数
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="weekAtMonth">第几个周</param>
    /// <param name="dayOfWeek">星期几</param>
    public static int GetTargetDays(int year, int month, int weekAtMonth, int dayOfWeek)
    {
        var fd = DateTimeFactory.Create(year, month, 1);
        var daysNeeded = dayOfWeek - (int)fd.DayOfWeek;
        if (daysNeeded < 0)
            daysNeeded += 7;
        return daysNeeded + 1 + 7 * (weekAtMonth - 1);
    }

    /// <summary>
    /// 计算
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="offsetMonths">偏移月份</param>
    /// <returns>(年，月)</returns>
    public static (int Year, int Month) Calc(int year, int month, int offsetMonths)
    {
        if (offsetMonths == 0)
            return (year, month);
        var z = offsetMonths > 0 ? 1 : -1;
        var offset = Math.Abs(offsetMonths);

        for (var i = 0; i < offset; i++)
        {
            if (z > 0 && month == 12)
            {
                year++;
                month = 1;
            }
            else if (z < 0 && month == 1)
            {
                year--;
                month = 12;
            }
            else
            {
                month += 1 * z;
            }
        }

        return (year, month);
    }
}

/// <summary>
/// 时间日期计算器
/// </summary>
public static class DateTimeCalc
{
    #region Offset by Milliseconds

    /// <summary>
    /// 毫秒偏移量。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="millisecond">毫秒数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByMillisecond(DateTime dt, int millisecond) => dt + millisecond.Milliseconds();

    #endregion

    #region Offset by Seconds

    /// <summary>
    /// 秒数偏移量。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="seconds">秒数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetBySeconds(DateTime dt, int seconds) => dt + seconds.Seconds();

    #endregion

    #region Offset by Minutes

    /// <summary>
    /// 分钟数偏移量。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="minutes">分钟数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByMinutes(DateTime dt, int minutes) => dt + minutes.Minutes();

    #endregion

    #region Offset by Hours

    /// <summary>
    /// 小时数偏移量。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="hours">小时数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByHours(DateTime dt, int hours) => dt + hours.Hours();

    #endregion

    #region Offset by Days

    /// <summary>
    /// 天数偏移量。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="days">天数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByDays(DateTime dt, int days) => dt + days.Days();

    #endregion

    #region Offset by Week

    /// <summary>
    /// 根据指定的年月和偏移信息创建 <see cref="DateTime"/>
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="weekAtMonth">第几个星期</param>
    /// <param name="dayOfWeek">星期几</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByWeek(int year, int month, int weekAtMonth, DayOfWeek dayOfWeek) => OffsetByWeek(year, month, weekAtMonth, dayOfWeek.CastToInt32(0));

    /// <summary>
    /// 根据指定的年月和偏移信息创建 <see cref="DateTime"/>
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="weekAtMonth">第几个星期</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <exception cref="ArgumentException"></exception>
    public static DateTime OffsetByWeek(int year, int month, int weekAtMonth, int dayOfWeek)
    {
        if (weekAtMonth == 0 || weekAtMonth > 5)
            throw new ArgumentException("weekAtMonth is invalid.", nameof(weekAtMonth));
        var targetDay = DateTimeCalcHelper.GetTargetDays(year, month, weekAtMonth, dayOfWeek);
        if (targetDay > DateTime.DaysInMonth(year, month))
            return DateTime.MinValue;
        return DateTimeFactory.Create(year, month, targetDay);
    }

    /// <summary>
    /// 根据指定的年月和偏移信息创建 <see cref="DateTime"/>
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="weekAtMonth">第几个星期</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <param name="result">结果</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryOffsetByWeek(int year, int month, int weekAtMonth, DayOfWeek dayOfWeek, out DateTime result) =>
        TryOffsetByWeek(year, month, weekAtMonth, dayOfWeek.CastToInt32(0), out result);

    /// <summary>
    /// 根据指定的年月和偏移信息创建 <see cref="DateTime"/>
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="weekAtMonth">第几个星期</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <param name="result">结果</param>
    /// <exception cref="ArgumentException"></exception>
    public static bool TryOffsetByWeek(int year, int month, int weekAtMonth, int dayOfWeek, out DateTime result)
    {
        if (weekAtMonth == 0 || weekAtMonth > 5)
            throw new ArgumentException("weekAtMonth is invalid.", nameof(weekAtMonth));
        var targetDay = DateTimeCalcHelper.GetTargetDays(year, month, weekAtMonth, dayOfWeek);
        var invalid = targetDay > DateTime.DaysInMonth(year, month);
        result = invalid ? DateTime.MinValue : DateTimeFactory.Create(year, month, targetDay);
        return !invalid;
    }

    #endregion

    #region Offset by Week Before / After

    /// <summary>
    /// 偏移指定周数
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="weeks">周数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByWeeks(DateTime dt, int weeks) => dt + weeks.Weeks();

    /// <summary>
    /// 根据指定的日期，获取上一个工作日（如周一）
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime OffsetByWeekBefore(DateTime dt, DayOfWeek dayOfWeek)
    {
        var daysSubtract = (int)dayOfWeek - (int)dt.DayOfWeek;
        return (int)dayOfWeek < (int)dt.DayOfWeek ? dt.AddDays(daysSubtract) : dt.AddDays(daysSubtract - 7);
    }

    /// <summary>
    /// 根据指定的日期，获取下一个工作日（如周一）
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime OffsetByWeekAfter(DateTime dt, DayOfWeek dayOfWeek)
    {
        var daysNeeded = (int)dayOfWeek - (int)dt.DayOfWeek;
        return (int)dayOfWeek >= (int)dt.DayOfWeek ? dt.AddDays(daysNeeded) : dt.AddDays(daysNeeded + 7);
    }

    #endregion

    #region Offset by DayOfWeek

    /// <summary>
    /// 偏移指定星期
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <param name="weekOffset">偏移星期数</param>
    public static DateTime OffsetOfDayOfWeek(DateTime dt, DayOfWeek dayOfWeek, int weekOffset)
    {
        var z = weekOffset > 0 ? 1 : -1;
        var offset = DayOfWeekCalc.DaysBetween(dt.DayOfWeek, dayOfWeek);
        offset = offset == 0 ? 7 : offset;
        return dt.OffsetBy(offset * z * weekOffset, DateOffsetStyles.Day);
    }

    #endregion

    #region Offset by Months

    /// <summary>
    /// 偏移指定月份数
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="months">月份数</param>
    /// <param name="options">日期时间偏移选项</param>
    public static DateTime OffsetByMonths(DateTime dt, int months, DateTimeOffsetOptions options = DateTimeOffsetOptions.Absolute)
    {
        if (options == DateTimeOffsetOptions.Absolute)
            return dt + months.Months();

        var calcResult = DateTimeCalcHelper.Calc(dt.Year, dt.Month, months);
        var firstDayOfMonth = dt.SetDate(calcResult.Year, calcResult.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.LastDayOfMonth().Day;
        var day = dt.Day > lastDayOfMonth ? lastDayOfMonth : dt.Day;
        return dt.SetDate(calcResult.Year, calcResult.Month, day);
    }

    #endregion

    #region Offset by Quarters

    /// <summary>
    /// 偏移指定季度数
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="quarters">季度数</param>
    /// <param name="options">日期时间偏移选项</param>
    public static DateTime OffsetByQuarters(DateTime dt, int quarters, DateTimeOffsetOptions options = DateTimeOffsetOptions.Absolute) =>
        options == DateTimeOffsetOptions.Absolute
            ? dt + quarters.Quarters()
            : OffsetByMonths(dt, quarters * 3, DateTimeOffsetOptions.Relatively);

    #endregion

    #region Offset by Years

    /// <summary>
    /// 偏移指定年数
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="years">年数</param>
    /// <param name="options">日期时间偏移选项</param>
    public static DateTime OffsetByYears(DateTime dt, int years, DateTimeOffsetOptions options = DateTimeOffsetOptions.Absolute) =>
        options == DateTimeOffsetOptions.Absolute
            ? dt + years.Years()
            : OffsetByMonths(dt, years * 12, DateTimeOffsetOptions.Relatively);

    #endregion

    #region Offset by Duration

    /// <summary>
    /// 偏移指定的持续时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="duration">持续时间</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByDuration(DateTime dt, Duration duration) => dt + duration.ToTimeSpan();

    #endregion
    
}