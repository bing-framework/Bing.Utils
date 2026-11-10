using System;
using System.Collections.Generic;

namespace Bing.Date.Chinese;

/// <summary>
/// 将调用方提供的年度节气时刻表组合成不可变的跨年查询快照。
/// </summary>
/// <remarks>
/// 每年的来源、版本及分辨率保留在对应年度表中。集合不联网，也不推算缺失数据。
/// 查询跨过集合内部缺失年份时抛出异常，不会跳过该年返回更远的节气。
/// </remarks>
public sealed class SolarTermInstantCalendarSet
{
    /// <summary>
    /// 允许加载的最小公历年份。
    /// </summary>
    private const int MinimumSupportedYear = 1;
    /// <summary>
    /// 允许加载的最大公历年份，为跨年查询保留边界。
    /// </summary>
    private const int MaximumSupportedYear = 9998;
    /// <summary>
    /// 北京时区相对 UTC 的固定偏移刻度数。
    /// </summary>
    private const long BeijingOffsetTicks = 8 * TimeSpan.TicksPerHour;
    /// <summary>
    /// 按年份索引的已加载节气时刻表。
    /// </summary>
    private readonly Dictionary<int, SolarTermInstantCalendar> _calendars;

    /// <summary>
    /// 初始化一个 <see cref="SolarTermInstantCalendarSet"/> 类型的实例。
    /// </summary>
    /// <param name="calendars">至少一个年度表，每个年份最多一个；允许不连续年份及不同元数据。</param>
    /// <exception cref="ArgumentNullException">集合为 null。</exception>
    /// <exception cref="ArgumentException">集合为空、包含 null 或重复年份。</exception>
    public SolarTermInstantCalendarSet(IEnumerable<SolarTermInstantCalendar> calendars)
    {
        if (calendars == null)
            throw new ArgumentNullException(nameof(calendars));

        var snapshots = new Dictionary<int, SolarTermInstantCalendar>();
        var minYear = MaximumSupportedYear;
        var maxYear = MinimumSupportedYear;
        foreach (var calendar in calendars)
        {
            if (calendar == null)
                throw new ArgumentException("年度节气时刻表不能包含 null。", nameof(calendars));
            if (snapshots.ContainsKey(calendar.Year))
                throw new ArgumentException("年度节气时刻表不能包含重复年份。", nameof(calendars));

            snapshots.Add(calendar.Year, calendar);
            minYear = Math.Min(minYear, calendar.Year);
            maxYear = Math.Max(maxYear, calendar.Year);
        }

        if (snapshots.Count == 0)
            throw new ArgumentException("年度节气时刻表不能为空。", nameof(calendars));

        _calendars = snapshots;
        MinYear = minYear;
        MaxYear = maxYear;
    }

    /// <summary>
    /// 获取已加载年度中的最小年份；中间年份可能缺失。
    /// </summary>
    public int MinYear { get; }

    /// <summary>
    /// 获取已加载年度中的最大年份；中间年份可能缺失。
    /// </summary>
    public int MaxYear { get; }

    /// <summary>
    /// 获取已加载的年度表数量。
    /// </summary>
    public int Count => _calendars.Count;

    /// <summary>
    /// 获取指定年份的年度表及其来源、版本和分辨率。
    /// </summary>
    /// <param name="year">北京时间所属年份。</param>
    /// <returns>已加载的不可变年度表。</returns>
    /// <exception cref="ArgumentOutOfRangeException">年份不在 1～9998 范围内。</exception>
    /// <exception cref="KeyNotFoundException">该年度未加载。</exception>
    public SolarTermInstantCalendar GetCalendar(int year)
    {
        if (year < MinimumSupportedYear || year > MaximumSupportedYear)
            throw new ArgumentOutOfRangeException(nameof(year), year, "节气时刻表只支持 1～9998 年。");
        if (!_calendars.TryGetValue(year, out var calendar))
            throw new KeyNotFoundException($"未加载 {year} 年的节气时刻表。");
        return calendar;
    }

    /// <summary>
    /// 获取指定年份和节气对应的时间点。
    /// </summary>
    /// <param name="year">北京时间所属年份。</param>
    /// <param name="term">节气枚举。</param>
    /// <returns>该年度节气的 UTC 时间点。</returns>
    /// <exception cref="ArgumentOutOfRangeException">年份或节气枚举无效。</exception>
    /// <exception cref="KeyNotFoundException">该年度未加载。</exception>
    public SolarTermInstant GetTerm(int year, ChineseSolarTerms term) => GetCalendar(year).GetTerm(term);

    /// <summary>
    /// 获取严格早于或包含给定时间点的上一节气。
    /// </summary>
    /// <param name="instant">任意偏移的时间点；按固定 UTC+8 确定所属年份。</param>
    /// <param name="includeBoundary">为 true 时包含相同时间点。</param>
    /// <returns>上一节气；已加载范围最早年度之前没有数据时返回 null。</returns>
    /// <exception cref="ArgumentOutOfRangeException">北京时间无法表示或不在支持的年份范围内。</exception>
    /// <exception cref="KeyNotFoundException">查询或跨年所需的年度未加载。</exception>
    public SolarTermInstant? GetPrevious(DateTimeOffset instant, bool includeBoundary = false)
    {
        var year = GetBeijingYear(instant);
        var result = GetCalendar(year).GetPrevious(instant, includeBoundary);
        if (result.HasValue || year == MinYear)
            return result;

        return GetCalendar(year - 1).GetTerm(ChineseSolarTerms.WinterSolstice);
    }

    /// <summary>
    /// 获取严格晚于或包含给定时间点的下一节气。
    /// </summary>
    /// <param name="instant">任意偏移的时间点；按固定 UTC+8 确定所属年份。</param>
    /// <param name="includeBoundary">为 true 时包含相同时间点。</param>
    /// <returns>下一节气；已加载范围最晚年度之后没有数据时返回 null。</returns>
    /// <exception cref="ArgumentOutOfRangeException">北京时间无法表示或不在支持的年份范围内。</exception>
    /// <exception cref="KeyNotFoundException">查询或跨年所需的年度未加载。</exception>
    public SolarTermInstant? GetNext(DateTimeOffset instant, bool includeBoundary = false)
    {
        var year = GetBeijingYear(instant);
        var result = GetCalendar(year).GetNext(instant, includeBoundary);
        if (result.HasValue || year == MaxYear)
            return result;

        return GetCalendar(year + 1).GetTerm(ChineseSolarTerms.SlightCold);
    }

    /// <summary>
    /// 获取时间点在固定 UTC+8 下的年份。
    /// </summary>
    /// <param name="instant">待查询的时间点。</param>
    /// <returns>受支持的北京时间年份。</returns>
    private static int GetBeijingYear(DateTimeOffset instant)
    {
        var ticks = instant.UtcDateTime.Ticks + BeijingOffsetTicks;
        if (ticks > DateTime.MaxValue.Ticks)
            throw new ArgumentOutOfRangeException(nameof(instant), instant, "北京时间超出可表示范围。");

        var year = new DateTime(ticks, DateTimeKind.Unspecified).Year;
        if (year > MaximumSupportedYear)
            throw new ArgumentOutOfRangeException(nameof(instant), instant, "北京时间年份超出节气时刻表支持范围。");
        return year;
    }
}
