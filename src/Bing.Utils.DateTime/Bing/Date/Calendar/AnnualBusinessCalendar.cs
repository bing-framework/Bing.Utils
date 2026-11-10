using System;
using System.Collections.Generic;

namespace Bing.Date.Calendars;
/// <summary>
/// 保存指定年度的不可变业务日历和业务日计数索引。
/// </summary>
/// <remarks>
/// <para>构造时按整年预计算前缀计数，实例仅保留年度元数据及一个最多 367 项的整数数组。</para>
/// <para>节假日和补班日期由调用方提供；该类型不包含地区官方日历数据。</para>
/// </remarks>
public sealed class AnnualBusinessCalendar
{
    /// <summary>
    /// 保存每个自然日之前累计的业务日数量。
    /// </summary>
    private readonly int[] _businessDayPrefix;
    /// <summary>
    /// 初始化一个 <see cref="AnnualBusinessCalendar"/> 类型的实例。
    /// </summary>
    /// <param name="year">公历年度，范围为 1 到 9999。</param>
    /// <param name="region">地区或适用范围标识。</param>
    /// <param name="version">调用方维护的日历版本标识。</param>
    /// <param name="source">调用方维护的数据来源说明。</param>
    /// <param name="holidays">当年节假日；忽略时间部分并按日期去重。</param>
    /// <param name="workingDayOverrides">当年补班日；忽略时间部分并按日期去重，且优先于周休息日和节假日。</param>
    /// <param name="restDays">每周休息日；省略时默认为周六和周日。</param>
    /// <exception cref="ArgumentOutOfRangeException">年度不在支持范围内，或节假日、补班日期不属于指定年度。</exception>
    /// <exception cref="ArgumentException">地区、版本、来源为空白，或休息日配置无效。</exception>
    public AnnualBusinessCalendar(
        int year,
        string region,
        string version,
        string source,
        IEnumerable<DateTime> holidays = null,
        IEnumerable<DateTime> workingDayOverrides = null,
        IEnumerable<DayOfWeek> restDays = null)
    {
        if (year < 1 || year > 9999)
            throw new ArgumentOutOfRangeException(nameof(year), year, "年度必须位于 1 到 9999 之间。");
        if (string.IsNullOrWhiteSpace(region))
            throw new ArgumentException("日历地区不能为空白。", nameof(region));
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("日历版本不能为空白。", nameof(version));
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("日历来源不能为空白。", nameof(source));

        Year = year;
        Region = region;
        Version = version;
        Source = source;

        var normalizedHolidays = NormalizeDates(holidays, year, nameof(holidays));
        var normalizedOverrides = NormalizeDates(workingDayOverrides, year, nameof(workingDayOverrides));
        var calendar = new BusinessCalendar(restDays, normalizedHolidays, normalizedOverrides);

        var dayCount = new DateTime(year, 12, 31).DayOfYear;
        _businessDayPrefix = new int[dayCount + 1];
        var firstDay = new DateTime(year, 1, 1);
        for (var dayIndex = 0; dayIndex < dayCount; dayIndex++)
        {
            _businessDayPrefix[dayIndex + 1] = _businessDayPrefix[dayIndex] +
                                               (calendar.IsBusinessDay(firstDay.AddDays(dayIndex)) ? 1 : 0);
        }

        TotalBusinessDays = _businessDayPrefix[dayCount];
    }
    /// <summary>
    /// 获取日历所属年度。
    /// </summary>
    public int Year { get; }
    /// <summary>
    /// 获取日历适用的地区或范围标识。
    /// </summary>
    public string Region { get; }
    /// <summary>
    /// 获取由调用方维护的日历版本标识。
    /// </summary>
    public string Version { get; }
    /// <summary>
    /// 获取由调用方维护的日历数据来源说明。
    /// </summary>
    public string Source { get; }
    /// <summary>
    /// 获取该年度的业务日总数。
    /// </summary>
    public int TotalBusinessDays { get; }
    /// <summary>
    /// 判断指定日期是否为该年度的业务日。
    /// </summary>
    /// <param name="date">待判断日期；忽略时间和 Kind。</param>
    /// <returns>该日期是业务日时为 <see langword="true"/>。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期不属于该日历年度。</exception>
    public bool IsBusinessDay(DateTime date)
    {
        EnsureInYear(date, nameof(date));
        var dayIndex = date.DayOfYear - 1;
        return _businessDayPrefix[dayIndex + 1] != _businessDayPrefix[dayIndex];
    }
    /// <summary>
    /// 统计同一年度半开日期区间内的业务日数量。
    /// </summary>
    /// <param name="startInclusive">包含的起始日期；允许下一年度 1 月 1 日作为边界哨兵。</param>
    /// <param name="endExclusive">不包含的结束日期；允许下一年度 1 月 1 日作为边界哨兵。</param>
    /// <returns>正向区间返回业务日数，反向区间返回其相反数。</returns>
    /// <exception cref="ArgumentOutOfRangeException">端点超出该年度及允许的下一年度 1 月 1 日哨兵。</exception>
    public int CountBusinessDays(DateTime startInclusive, DateTime endExclusive)
    {
        var startIndex = GetBoundaryIndex(startInclusive, nameof(startInclusive));
        var endIndex = GetBoundaryIndex(endExclusive, nameof(endExclusive));
        return endIndex >= startIndex
            ? _businessDayPrefix[endIndex] - _businessDayPrefix[startIndex]
            : -(_businessDayPrefix[startIndex] - _businessDayPrefix[endIndex]);
    }
    /// <summary>
    /// 从指定日期向前或向后移动指定数量的业务日。
    /// </summary>
    /// <param name="date">起始日期；必须属于该年度，且不计入偏移量。</param>
    /// <param name="businessDays">业务日偏移量；正数向后、负数向前，零返回原值。</param>
    /// <returns>目标业务日，并保留起始值的时间 ticks、Kind 和本地重复时间选择标记。</returns>
    /// <remarks>按日历日期偏移原值，不重新解析时区规则。</remarks>
    /// <exception cref="ArgumentOutOfRangeException">起点不属于该年度、偏移为零以外的目标超出年度，或目标业务日不存在。</exception>
    public DateTime AddBusinessDays(DateTime date, int businessDays)
    {
        EnsureInYear(date, nameof(date));
        if (businessDays == 0)
            return date;

        var startIndex = date.DayOfYear - 1;
        long targetRank;
        if (businessDays > 0)
        {
            targetRank = (long)_businessDayPrefix[startIndex + 1] + businessDays;
            if (targetRank > TotalBusinessDays)
                throw new ArgumentOutOfRangeException(nameof(businessDays), businessDays, "偏移目标超出该年度的业务日范围。");
        }
        else
        {
            targetRank = (long)_businessDayPrefix[startIndex] + businessDays + 1L;
            if (targetRank < 1)
                throw new ArgumentOutOfRangeException(nameof(businessDays), businessDays, "偏移目标超出该年度的业务日范围。");
        }

        var targetPrefixIndex = LowerBoundPrefix((int)targetRank);
        var dayOffset = targetPrefixIndex - 1 - startIndex;
        return date.AddTicks((long)dayOffset * TimeSpan.TicksPerDay);
    }
    /// <summary>
    /// 将年度内日期或允许的次年元旦哨兵转换为前缀数组索引。
    /// </summary>
    /// <param name="date">待转换的日期端点。</param>
    /// <param name="parameterName">对外报告的参数名。</param>
    /// <returns>该日期之前的自然日数量。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期不属于该年度或允许的次年元旦哨兵。</exception>
    private int GetBoundaryIndex(DateTime date, string parameterName)
    {
        if (date.Year == Year)
            return date.DayOfYear - 1;

        if (Year < 9999 && date.Year == Year + 1 && date.Month == 1 && date.Day == 1)
            return _businessDayPrefix.Length - 1;

        throw new ArgumentOutOfRangeException(parameterName, date, "区间端点必须属于该年度；结束边界可使用下一年度 1 月 1 日。");
    }
    /// <summary>
    /// 验证单日期运算的输入日期属于该日历年度。
    /// </summary>
    /// <param name="date">待验证日期。</param>
    /// <param name="parameterName">对外报告的参数名。</param>
    /// <exception cref="ArgumentOutOfRangeException">日期年份与日历年度不一致。</exception>
    private void EnsureInYear(DateTime date, string parameterName)
    {
        if (date.Year != Year)
            throw new ArgumentOutOfRangeException(parameterName, date, "日期必须属于该日历年度。");
    }
    /// <summary>
    /// 查找累计业务日数量首次达到目标序号的前缀位置。
    /// </summary>
    /// <param name="businessDayRank">从年度第一天开始的一基业务日序号。</param>
    /// <returns>目标业务日之后的前缀索引。</returns>
    private int LowerBoundPrefix(int businessDayRank)
    {
        var low = 1;
        var high = _businessDayPrefix.Length - 1;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (_businessDayPrefix[middle] < businessDayRank)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }
    /// <summary>
    /// 校验年度归属、按自然日去重并复制输入日期集合。
    /// </summary>
    /// <param name="dates">待规范化的日期集合。</param>
    /// <param name="year">允许的公历年度。</param>
    /// <param name="parameterName">对外报告的参数名。</param>
    /// <returns>按首次出现顺序保存的去重自然日列表。</returns>
    /// <exception cref="ArgumentOutOfRangeException">任一日期不属于指定年度。</exception>
    private static List<DateTime> NormalizeDates(IEnumerable<DateTime> dates, int year, string parameterName)
    {
        var result = new List<DateTime>();
        if (dates == null)
            return result;

        var seen = new HashSet<DateTime>();
        foreach (var date in dates)
        {
            if (date.Year != year)
                throw new ArgumentOutOfRangeException(parameterName, date, "节假日和补班日期必须属于指定年度。");

            var normalizedDate = date.Date;
            if (seen.Add(normalizedDate))
                result.Add(normalizedDate);
        }

        return result;
    }
}
