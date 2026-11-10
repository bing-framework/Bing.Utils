using System;
using System.Collections.Generic;

namespace Bing.Date.Calendars;
/// <summary>
/// 将业务维护的年度日历组合成一个不可变的跨年日历。
/// </summary>
/// <remarks>
/// 只使用构造时提供的年度快照；不会联网、补充默认规则或跨缺失年份推断工作日。
/// 并发读取安全，更新规则时应创建新实例并替换引用。
/// </remarks>
public sealed class BusinessCalendarSet
{
    /// <summary>
    /// 按公历年索引已加载的不可变年度快照。
    /// </summary>
    private readonly Dictionary<int, AnnualBusinessCalendar> _calendars;
    /// <summary>
    /// 初始化一个 <see cref="BusinessCalendarSet"/> 类型的实例。
    /// </summary>
    /// <param name="calendars">预加载的年度日历集合，每年必须唯一且至少提供一个年度。</param>
    /// <exception cref="ArgumentNullException">年度日历集合为 null。</exception>
    /// <exception cref="ArgumentException">集合为空，包含 null、重复年度或不同地区的日历。</exception>
    public BusinessCalendarSet(IEnumerable<AnnualBusinessCalendar> calendars)
    {
        if (calendars == null)
            throw new ArgumentNullException(nameof(calendars));

        var snapshots = new Dictionary<int, AnnualBusinessCalendar>();
        string region = null;
        foreach (var calendar in calendars)
        {
            if (calendar == null)
                throw new ArgumentException("年度日历集合不能包含 null。", nameof(calendars));

            if (region == null)
                region = calendar.Region;
            else if (!string.Equals(region, calendar.Region, StringComparison.Ordinal))
                throw new ArgumentException("年度日历必须属于同一地区。", nameof(calendars));

            if (snapshots.ContainsKey(calendar.Year))
                throw new ArgumentException("年度日历集合不能包含重复年份。", nameof(calendars));
            snapshots.Add(calendar.Year, calendar);

            // AnnualBusinessCalendar 的年份范围限定为 1～9999，重复检查会在达到边界时自然结束枚举。
        }

        if (snapshots.Count == 0)
            throw new ArgumentException("年度日历集合不能为空。", nameof(calendars));

        Region = region;
        _calendars = snapshots;
    }
    /// <summary>
    /// 获取该日历集合适用的地区标识。
    /// </summary>
    public string Region { get; }
    /// <summary>
    /// 判断指定日期是否为业务日。
    /// </summary>
    /// <param name="date">待判断日期；时间和 Kind 不参与比较。</param>
    /// <returns>年度规则认定为业务日时返回 <see langword="true"/>。</returns>
    /// <exception cref="KeyNotFoundException">日期所属年度未加载。</exception>
    public bool IsBusinessDay(DateTime date) => GetCalendar(date.Year).IsBusinessDay(date);
    /// <summary>
    /// 统计日期字段构成的半开区间中的业务日数量。
    /// </summary>
    /// <param name="startInclusive">包含的起始日期。</param>
    /// <param name="endExclusive">不包含的结束日期。</param>
    /// <returns>正向区间返回业务日数，反向区间返回相反数；同一自然日返回零。</returns>
    /// <exception cref="KeyNotFoundException">半开区间实际涉及的年份未加载。</exception>
    public int CountBusinessDays(DateTime startInclusive, DateTime endExclusive)
    {
        var start = startInclusive.Date;
        var end = endExclusive.Date;
        if (start == end)
            return 0;
        if (start > end)
            return -CountForward(end, start);

        return CountForward(start, end);
    }
    /// <summary>
    /// 从起始日期向前或向后偏移指定数量的业务日，不计起点。
    /// </summary>
    /// <param name="date">起始日期；必须属于已加载年度。</param>
    /// <param name="businessDays">有符号偏移量；正数向后，负数向前，零返回原值。</param>
    /// <returns>目标日期；保留原输入的时分秒、ticks、Kind 和本地 DST 歧义标记。</returns>
    /// <exception cref="KeyNotFoundException">搜索所需的某个年度未加载。</exception>
    /// <exception cref="ArgumentOutOfRangeException">目标超出 DateTime 范围，或可表示的日历范围。</exception>
    public DateTime AddBusinessDays(DateTime date, int businessDays)
    {
        var calendar = GetCalendar(date.Year);
        if (businessDays == 0)
            return date;

        var remaining = Math.Abs((long)businessDays);
        // 在访问年度配置前排除不可能落入 DateTime 的极大偏移，避免 int.MinValue 触发长时间遍历。
        if (remaining > (DateTime.MaxValue.Date - DateTime.MinValue.Date).Days)
            throw new ArgumentOutOfRangeException(nameof(businessDays), businessDays, "偏移超出 DateTime 可表示的自然日范围。");

        var target = businessDays > 0
            ? AddForward(date, calendar, remaining)
            : AddBackward(date, calendar, remaining);

        // DateTime.AddDays 还会保留本地 DST 重叠时间所需的内部标志。
        var dayDelta = (target.Date - date.Date).Days;
        return date.AddDays(dayDelta);
    }
    /// <summary>
    /// 对正向日期区间逐年使用年度前缀索引计数。
    /// </summary>
    /// <param name="startInclusive">自然日起点。</param>
    /// <param name="endExclusive">自然日终点。</param>
    /// <returns>半开区间内的业务日数量。</returns>
    private int CountForward(DateTime startInclusive, DateTime endExclusive)
    {
        var count = 0;
        var year = startInclusive.Year;
        var lastYear = endExclusive.Year;
        // 次年元旦是半开哨兵，不要求加载其尚未开始参与统计的年度。
        if (endExclusive.Month == 1 && endExclusive.Day == 1 && lastYear > year)
            lastYear--;

        while (year <= lastYear)
        {
            var calendar = GetCalendar(year);
            var yearStart = new DateTime(year, 1, 1);
            var segmentStart = startInclusive.Year == year ? startInclusive : yearStart;
            var segmentEnd = lastYear == year
                ? endExclusive
                : new DateTime(year + 1, 1, 1);
            count = checked(count + calendar.CountBusinessDays(segmentStart, segmentEnd));

            if (year == lastYear)
                break;
            year++;
        }

        return count;
    }
    /// <summary>
    /// 在每年的剩余日期和下一年度规则间前进，按全年业务日总数跳过完整年度。
    /// </summary>
    /// <param name="date">起始时间。</param>
    /// <param name="calendar">起始年度日历。</param>
    /// <param name="remaining">尚未消耗的正向业务日数。</param>
    /// <returns>目标年度中的目标日期。</returns>
    private DateTime AddForward(DateTime date, AnnualBusinessCalendar calendar, long remaining)
    {
        var year = calendar.Year;
        var lastDate = new DateTime(year, 12, 31);
        var availableAfterStart = date.Date == lastDate
            ? 0
            : calendar.CountBusinessDays(date.Date.AddDays(1), year == 9999 ? DateTime.MaxValue.Date : new DateTime(year + 1, 1, 1));
        if (year == 9999 && date.Date != lastDate && calendar.IsBusinessDay(lastDate))
            availableAfterStart++;

        if (remaining <= availableAfterStart)
            return calendar.AddBusinessDays(date, (int)remaining);
        remaining -= availableAfterStart;

        while (year < 9999)
        {
            year++;
            calendar = GetCalendar(year);
            var yearStart = new DateTime(year, 1, 1);
            var total = calendar.TotalBusinessDays;
            if (remaining > total)
            {
                remaining -= total;
                continue;
            }

            var includesFirstDay = calendar.IsBusinessDay(yearStart);
            var offset = includesFirstDay ? remaining - 1 : remaining;
            if (offset <= int.MaxValue)
                return calendar.AddBusinessDays(yearStart, (int)offset);
            remaining = offset;
        }

        throw new ArgumentOutOfRangeException("businessDays", "偏移目标超出 DateTime.MaxValue。");
    }
    /// <summary>
    /// 在每年的起始日期和上一年度规则间后退，按全年业务日总数跳过完整年度。
    /// </summary>
    /// <param name="date">起始时间。</param>
    /// <param name="calendar">起始年度日历。</param>
    /// <param name="remaining">尚未消耗的正向业务日数。</param>
    /// <returns>目标年度中的目标日期。</returns>
    private DateTime AddBackward(DateTime date, AnnualBusinessCalendar calendar, long remaining)
    {
        var year = calendar.Year;
        var availableBeforeStart = calendar.CountBusinessDays(new DateTime(year, 1, 1), date.Date);
        if (remaining <= availableBeforeStart)
            return calendar.AddBusinessDays(date, -(int)remaining);
        remaining -= availableBeforeStart;

        while (year > 1)
        {
            year--;
            calendar = GetCalendar(year);
            var yearEnd = new DateTime(year, 12, 31);
            var total = calendar.TotalBusinessDays;
            if (remaining > total)
            {
                remaining -= total;
                continue;
            }

            var includesLastDay = calendar.IsBusinessDay(yearEnd);
            var offset = includesLastDay ? remaining - 1 : remaining;
            if (offset <= int.MaxValue)
                return calendar.AddBusinessDays(yearEnd, -(int)offset);
            remaining = offset;
        }

        throw new ArgumentOutOfRangeException("businessDays", "偏移目标超出 DateTime.MinValue。");
    }
    /// <summary>
    /// 获取指定年度的快照，并在缺年时明确拒绝运算。
    /// </summary>
    /// <param name="year">所需公历年。</param>
    /// <returns>预加载的年度规则。</returns>
    /// <exception cref="KeyNotFoundException">所需年度未包含在该集合内。</exception>
    private AnnualBusinessCalendar GetCalendar(int year)
    {
        if (!_calendars.TryGetValue(year, out var calendar))
            throw new KeyNotFoundException($"地区 {Region} 未加载 {year} 年的业务日历。");
        return calendar;
    }
}
