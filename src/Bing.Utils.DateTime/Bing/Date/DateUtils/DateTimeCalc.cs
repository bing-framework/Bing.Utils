using Bing.Conversions;
using NodaTime;

namespace Bing.Date.DateUtils;

/// <summary>
/// 日期时间计算帮助类
/// </summary>
internal static class DateTimeCalcHelper
{
    /// <summary>
    /// 获取指定月份中某个星期几的日期
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份</param>
    /// <param name="weekAtMonth">第几个周，从1开始</param>
    /// <param name="dayOfWeek">星期几，0表示星期日，1-6表示星期一至星期六</param>
    /// <returns>目标日期的天数</returns>
    public static int GetTargetDays(int year, int month, int weekAtMonth, int dayOfWeek)
    {
        if (year <= 0)
            throw new ArgumentOutOfRangeException(nameof(year), "年份必须大于0");
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "月份必须在1到12之间");
        if (weekAtMonth < 1 || weekAtMonth > 5)
            throw new ArgumentOutOfRangeException(nameof(weekAtMonth), "周数必须在1到5之间");
        if (dayOfWeek < 0 || dayOfWeek > 6)
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "星期几必须在0到6之间");
        // 获取当月第一天
        var fd = DateTimeFactory.Create(year, month, 1);
        // 计算当月第一天到指定星期几的天数
        var daysNeeded = dayOfWeek - (int)fd.DayOfWeek;
        // 如果为负数，表示需要到下一周才能找到指定的星期几
        if (daysNeeded < 0)
            daysNeeded += 7;
        // 计算目标日期的天数：第一个符合条件的日期 + (周数-1)*7
        return daysNeeded + 1 + 7 * (weekAtMonth - 1);
    }

    /// <summary>
    /// 计算偏移指定月份后的年月
    /// </summary>
    /// <param name="year">起始年份</param>
    /// <param name="month">起始月份</param>
    /// <param name="offsetMonths">偏移月份数，正数表示向后，负数表示向前</param>
    /// <returns>偏移后的年月元组</returns>
    /// <exception cref="ArgumentOutOfRangeException">当年份或月份超出有效范围时抛出</exception>
    public static (int Year, int Month) Calc(int year, int month, int offsetMonths)
    {
        if (year <= 0)
            throw new ArgumentOutOfRangeException(nameof(year), "年份必须大于0");
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "月份必须在1到12之间");
        if (offsetMonths == 0)
            return (year, month);
        
        var totalMonths = year * 12 + (month - 1) + offsetMonths;
        var newYear = totalMonths / 12;
        var newMonth = totalMonths % 12 + 1;

        return (newYear, newMonth);
    }
}

/// <summary>
/// 时间日期计算器
/// </summary>
public static class DateTimeCalc
{
    #region Offset by Milliseconds

    /// <summary>
    /// 按毫秒偏移时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="millisecond">要偏移的毫秒数，正数表示向后，负数表示向前</param>
    /// <returns>偏移后的新日期时间</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByMillisecond(DateTime dt, int millisecond) => dt + millisecond.Milliseconds();

    #endregion

    #region Offset by Seconds

    /// <summary>
    /// 按秒数偏移时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="seconds">要偏移的秒数，正数表示向后，负数表示向前</param>
    /// <returns>偏移后的新日期时间</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetBySeconds(DateTime dt, int seconds) => dt + seconds.Seconds();

    #endregion

    #region Offset by Minutes

    /// <summary>
    /// 按分钟数偏移时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="minutes">要偏移的分钟数，正数表示向后，负数表示向前</param>
    /// <returns>偏移后的新日期时间</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByMinutes(DateTime dt, int minutes) => dt + minutes.Minutes();

    #endregion

    #region Offset by Hours

    /// <summary>
    /// 按小时数偏移时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="hours">要偏移的小时数，正数表示向后，负数表示向前</param>
    /// <returns>偏移后的新日期时间</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByHours(DateTime dt, int hours) => dt + hours.Hours();

    #endregion

    #region Offset by Days

    /// <summary>
    /// 按天数偏移时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="days">要偏移的天数，正数表示向后，负数表示向前</param>
    /// <returns>偏移后的新日期时间</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByDays(DateTime dt, int days) => dt + days.Days();

    #endregion

    #region Offset by Week

    /// <summary>
    /// 获取指定年月的特定星期几的日期
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份，1-12</param>
    /// <param name="weekAtMonth">第几个星期，1-5</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>计算得到的日期时间，如果不存在则返回DateTime.MinValue</returns>
    /// <exception cref="ArgumentException">当weekAtMonth无效时抛出</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByWeek(int year, int month, int weekAtMonth, DayOfWeek dayOfWeek) => OffsetByWeek(year, month, weekAtMonth, dayOfWeek.CastToInt32(0));

    /// <summary>
    /// 获取指定年月的特定星期几的日期
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份，1-12</param>
    /// <param name="weekAtMonth">第几个星期，1-5</param>
    /// <param name="dayOfWeek">星期几，0-6，0表示周日</param>
    /// <returns>计算得到的日期时间，如果不存在则返回DateTime.MinValue</returns>
    /// <exception cref="ArgumentException">当weekAtMonth无效时抛出</exception>
    /// <exception cref="ArgumentException"></exception>
    public static DateTime OffsetByWeek(int year, int month, int weekAtMonth, int dayOfWeek)
    {
        if (weekAtMonth < 1 || weekAtMonth > 5)
            throw new ArgumentException("weekAtMonth必须在1到5之间", nameof(weekAtMonth));
        var targetDay = DateTimeCalcHelper.GetTargetDays(year, month, weekAtMonth, dayOfWeek);
        if (targetDay > DateTime.DaysInMonth(year, month))
            return DateTime.MinValue;
        return DateTimeFactory.Create(year, month, targetDay);
    }

    /// <summary>
    /// 尝试获取指定年月的特定星期几的日期
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份，1-12</param>
    /// <param name="weekAtMonth">第几个星期，1-5</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <param name="result">输出参数，存储计算结果</param>
    /// <returns>如果成功计算出有效日期则返回true，否则返回false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryOffsetByWeek(int year, int month, int weekAtMonth, DayOfWeek dayOfWeek, out DateTime result) =>
        TryOffsetByWeek(year, month, weekAtMonth, dayOfWeek.CastToInt32(0), out result);

    /// <summary>
    /// 尝试获取指定年月的特定星期几的日期
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份，1-12</param>
    /// <param name="weekAtMonth">第几个星期，1-5</param>
    /// <param name="dayOfWeek">星期几，0-6，0表示周日</param>
    /// <param name="result">输出参数，存储计算结果</param>
    /// <returns>如果成功计算出有效日期则返回true，否则返回false</returns>
    /// <exception cref="ArgumentException">当weekAtMonth无效时抛出</exception>
    public static bool TryOffsetByWeek(int year, int month, int weekAtMonth, int dayOfWeek, out DateTime result)
    {
        if (weekAtMonth < 1 || weekAtMonth > 5)
            throw new ArgumentException("weekAtMonth必须在1到5之间", nameof(weekAtMonth));
        var targetDay = DateTimeCalcHelper.GetTargetDays(year, month, weekAtMonth, dayOfWeek);
        var invalid = targetDay > DateTime.DaysInMonth(year, month);
        result = invalid ? DateTime.MinValue : DateTimeFactory.Create(year, month, targetDay);
        return !invalid;
    }

    #endregion

    #region Offset by Week Before / After

    /// <summary>
    /// 按周数偏移时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="weeks">要偏移的周数，正数表示向后，负数表示向前</param>
    /// <returns>偏移后的新日期时间</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByWeeks(DateTime dt, int weeks) => dt + weeks.Weeks();

    /// <summary>
    /// 获取指定日期的上一个指定星期几
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="dayOfWeek">目标星期几</param>
    /// <returns>上一个匹配的日期</returns>
    public static DateTime OffsetByWeekBefore(DateTime dt, DayOfWeek dayOfWeek)
    {
        var daysSubtract = (int)dayOfWeek - (int)dt.DayOfWeek;
        return (int)dayOfWeek < (int)dt.DayOfWeek 
            ? dt.AddDays(daysSubtract) 
            : dt.AddDays(daysSubtract - 7);
    }

    /// <summary>
    /// 获取指定日期的下一个指定星期几
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="dayOfWeek">目标星期几</param>
    /// <returns>下一个匹配的日期</returns>
    public static DateTime OffsetByWeekAfter(DateTime dt, DayOfWeek dayOfWeek)
    {
        var daysNeeded = (int)dayOfWeek - (int)dt.DayOfWeek;
        if (dayOfWeek == dt.DayOfWeek)
            return dt.AddDays(7);
        return (int)dayOfWeek >= (int)dt.DayOfWeek 
            ? dt.AddDays(daysNeeded) 
            : dt.AddDays(daysNeeded + 7);
    }

    #endregion

    #region Offset by DayOfWeek

    /// <summary>
    /// 按指定星期几和周数偏移时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="dayOfWeek">目标星期几</param>
    /// <param name="weekOffset">周偏移量，正数表示向后几周，负数表示向前几周</param>
    /// <returns>偏移后的新日期时间</returns>
    public static DateTime OffsetOfDayOfWeek(DateTime dt, DayOfWeek dayOfWeek, int weekOffset)
    {
        // 如果周偏移为0且目标星期几与当前日期的星期几相同，则返回当前日期
        if (weekOffset == 0)
        {
            if (dayOfWeek == dt.DayOfWeek)
                return dt;

            // weekOffset为0但星期几不同，则返回本周对应的星期几
            int daysToAdd = ((int)dayOfWeek - (int)dt.DayOfWeek + 7) % 7;
            return dt.AddDays(daysToAdd);
        }

        if (weekOffset > 0)
        {
            // 正向偏移：找到下一个目标星期几
            DateTime nextOccurrence;
            if (dayOfWeek == dt.DayOfWeek)
            {
                // 如果当前日期就是目标星期几，则需要加7天
                nextOccurrence = dt.AddDays(7);
            }
            else
            {
                // 否则找到下一次出现的目标星期几
                int dayDiff = ((int)dayOfWeek - (int)dt.DayOfWeek + 7) % 7;
                nextOccurrence = dt.AddDays(dayDiff);
            }

            // 再加上剩余的周数
            return nextOccurrence.AddDays(7 * (weekOffset - 1));
        }
        else // weekOffset < 0
        {
            // 负向偏移：找到上一个目标星期几
            DateTime prevOccurrence;
            if (dayOfWeek == dt.DayOfWeek)
            {
                // 如果当前日期就是目标星期几，则需要减7天
                prevOccurrence = dt.AddDays(-7);
            }
            else
            {
                // 否则找到上一次出现的目标星期几
                int dayDiff = ((int)dt.DayOfWeek - (int)dayOfWeek + 7) % 7;
                prevOccurrence = dt.AddDays(-dayDiff);
            }

            // 再减去剩余的周数
            return prevOccurrence.AddDays(7 * (weekOffset + 1));
        }
    }

    #endregion

    #region Offset by Months

    /// <summary>
    /// 按月份数偏移时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="months">要偏移的月份数，正数表示向后，负数表示向前</param>
    /// <param name="options">日期时间偏移选项，指定偏移行为</param>
    /// <returns>偏移后的新日期时间</returns>
    public static DateTime OffsetByMonths(DateTime dt, int months, DateTimeOffsetOptions options = DateTimeOffsetOptions.Absolute)
    {
        // 绝对偏移模式，直接使用TimeSpan偏移
        if (options == DateTimeOffsetOptions.Absolute)
            return dt.AddMonths(months);

        // 相对偏移模式，需要考虑月份天数变化
        var calcResult = DateTimeCalcHelper.Calc(dt.Year, dt.Month, months);
        var firstDayOfMonth = dt.SetDate(calcResult.Year, calcResult.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.LastDayOfMonth().Day;
        var day = dt.Day > lastDayOfMonth ? lastDayOfMonth : dt.Day;
        return dt.SetDate(calcResult.Year, calcResult.Month, day);
    }

    #endregion

    #region Offset by Quarters

    /// <summary>
    /// 按季度数偏移时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="quarters">要偏移的季度数，正数表示向后，负数表示向前</param>
    /// <param name="options">日期时间偏移选项，指定偏移行为</param>
    /// <returns>偏移后的新日期时间</returns>
    public static DateTime OffsetByQuarters(DateTime dt, int quarters, DateTimeOffsetOptions options = DateTimeOffsetOptions.Absolute) =>
        options == DateTimeOffsetOptions.Absolute
            ? OffsetByMonths(dt, quarters * 3, DateTimeOffsetOptions.Absolute)
            : OffsetByMonths(dt, quarters * 3, DateTimeOffsetOptions.Relatively);

    #endregion

    #region Offset by Years

    /// <summary>
    /// 按年数偏移时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="years">要偏移的年数，正数表示向后，负数表示向前</param>
    /// <param name="options">日期时间偏移选项，指定偏移行为</param>
    /// <returns>偏移后的新日期时间</returns>
    public static DateTime OffsetByYears(DateTime dt, int years, DateTimeOffsetOptions options = DateTimeOffsetOptions.Absolute) =>
        options == DateTimeOffsetOptions.Absolute
            ? dt.AddYears(years)
            : OffsetByMonths(dt, years * 12, DateTimeOffsetOptions.Relatively);

    #endregion

    #region Offset by Duration

    /// <summary>
    /// 按持续时间偏移时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="duration">要偏移的持续时间</param>
    /// <returns>偏移后的新日期时间</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime OffsetByDuration(DateTime dt, Duration duration) => dt + duration.ToTimeSpan();

    #endregion
    
}