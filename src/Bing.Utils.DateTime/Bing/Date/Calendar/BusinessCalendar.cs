using System;
using System.Collections.Generic;

namespace Bing.Date.Calendars;

/// <summary>
/// 使用周休息日、节假日和补班日期计算业务日。
/// </summary>
public sealed class BusinessCalendar
{
    private readonly HashSet<DayOfWeek> _restDays;
    private readonly HashSet<DateTime> _holidays;
    private readonly HashSet<DateTime> _workingDayOverrides;

    /// <summary>
    /// 初始化业务日历。补班日期优先于周休息日和节假日。
    /// </summary>
    /// <param name="restDays">每周休息日；未指定时默认为周六和周日。</param>
    /// <param name="holidays">节假日日期，时间部分会被忽略。</param>
    /// <param name="workingDayOverrides">补班日期，时间部分会被忽略。</param>
    /// <exception cref="ArgumentException">周休息日包含无效值或覆盖一周全部七天。</exception>
    public BusinessCalendar(
        IEnumerable<DayOfWeek> restDays = null,
        IEnumerable<DateTime> holidays = null,
        IEnumerable<DateTime> workingDayOverrides = null)
    {
        _restDays = new HashSet<DayOfWeek>(restDays ?? new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });
        foreach (var day in _restDays)
        {
            if ((int)day < (int)DayOfWeek.Sunday || (int)day > (int)DayOfWeek.Saturday)
                throw new ArgumentException("周休息日包含无效的星期值。", nameof(restDays));
        }

        if (_restDays.Count == 7)
            throw new ArgumentException("周休息日不能覆盖一周全部七天。", nameof(restDays));

        _holidays = NormalizeDates(holidays);
        _workingDayOverrides = NormalizeDates(workingDayOverrides);
    }

    /// <summary>
    /// 判断指定日期是否为业务日。
    /// </summary>
    /// <param name="date">待判断日期；时间部分会被忽略。</param>
    /// <returns>补班日期，或既非周休息日也非节假日时返回 <see langword="true"/>。</returns>
    public bool IsBusinessDay(DateTime date)
    {
        var day = date.Date;
        if (_workingDayOverrides.Contains(day))
            return true;

        return !_restDays.Contains(day.DayOfWeek) && !_holidays.Contains(day);
    }

    /// <summary>
    /// 从指定日期向前或向后移动指定数量的业务日。
    /// </summary>
    /// <param name="date">起始日期。</param>
    /// <param name="businessDays">业务日偏移量；正数向后，负数向前，零返回原值。</param>
    /// <returns>保留起始值时间部分和 <see cref="DateTime.Kind"/> 的结果。</returns>
    /// <exception cref="ArgumentOutOfRangeException">偏移导致日期超出 <see cref="DateTime"/> 表示范围。</exception>
    public DateTime AddBusinessDays(DateTime date, int businessDays)
    {
        if (businessDays == 0)
            return date;

        var direction = businessDays > 0 ? 1 : -1;
        var remaining = Math.Abs((long)businessDays);
        var result = date;

        while (remaining > 0)
        {
            result = result.AddDays(direction);
            if (IsBusinessDay(result))
                remaining--;
        }

        return result;
    }

    /// <summary>
    /// 统计半开日期区间中的业务日数量。
    /// </summary>
    /// <param name="startInclusive">区间起点，包含。</param>
    /// <param name="endExclusive">区间终点，不包含。</param>
    /// <returns>正向区间返回业务日数量；反向区间返回相反数。</returns>
    public int CountBusinessDays(DateTime startInclusive, DateTime endExclusive)
    {
        var start = startInclusive.Date;
        var end = endExclusive.Date;
        var comparison = DateTime.Compare(start, end);
        if (comparison == 0)
            return 0;

        return comparison < 0
            ? CountForward(start, end)
            : -CountForward(end, start);
    }

    private int CountForward(DateTime startInclusive, DateTime endExclusive)
    {
        var count = 0;
        for (var date = startInclusive; date < endExclusive; date = date.AddDays(1))
        {
            if (IsBusinessDay(date))
                count++;
        }

        return count;
    }

    private static HashSet<DateTime> NormalizeDates(IEnumerable<DateTime> dates)
    {
        var result = new HashSet<DateTime>();
        if (dates == null)
            return result;

        foreach (var date in dates)
            result.Add(date.Date);

        return result;
    }
}
