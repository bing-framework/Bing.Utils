namespace Bing.Date.Chinese;
/// <summary>
/// 表示节气数据源声明的 UTC 时间点。
/// </summary>
/// <remarks>该值由调用方提供，类型本身不验证数据来源、观测误差或授权。</remarks>
public readonly struct SolarTermInstant
{
    /// <summary>
    /// 初始化一个 <see cref="SolarTermInstant"/> 类型的实例。
    /// </summary>
    /// <param name="term">取值范围为 0～23 的节气枚举。</param>
    /// <param name="instant">节气发生的时间点，保存时会转换为 UTC。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="term" /> 不属于二十四节气。</exception>
    public SolarTermInstant(ChineseSolarTerms term, DateTimeOffset instant)
    {
        if ((int)term < 0 || (int)term > 23)
            throw new ArgumentOutOfRangeException(nameof(term), term, "节气枚举值必须在 0～23 范围内。");

        Term = term;
        Instant = instant.ToUniversalTime();
    }
    /// <summary>
    /// 获取节气枚举。
    /// </summary>
    public ChineseSolarTerms Term { get; }
    /// <summary>
    /// 获取已归一化为零偏移的 UTC 时间点。
    /// </summary>
    public DateTimeOffset Instant { get; }
}
/// <summary>
/// 保存调用方提供的单年度二十四节气 UTC 时间点并支持相邻查询。
/// </summary>
/// <remarks>
/// <para>数据由调用方提供；本类型不下载、嵌入或推算节气时刻，也不自动跨年查询。</para>
/// <para>分辨率只用于检查数据时间点的刻度对齐，不表示时间测量误差；不得把日期表午夜当成交节时刻。</para>
/// <para>支持年份为 1～9998；不支持 9999 年，以避免 UTC+8 换算和下一年度边界无法表示。</para>
/// </remarks>
public sealed class SolarTermInstantCalendar
{
    /// <summary>
    /// 确定数据所声明的北京时间所属年月的固定偏移。
    /// </summary>
    private static readonly TimeSpan BeijingOffset = TimeSpan.FromHours(8);
    /// <summary>
    /// 按小寒至冬至的次序保存的不可变输入快照。
    /// </summary>
    private readonly SolarTermInstant[] _terms;
    /// <summary>
    /// 初始化一个 <see cref="SolarTermInstantCalendar"/> 类型的实例。
    /// </summary>
    /// <param name="year">数据所属年份，范围为 1～9998。</param>
    /// <param name="version">非空白的数据版本标识。</param>
    /// <param name="source">非空白的数据来源说明；调用方负责确认数据使用授权。</param>
    /// <param name="resolution">数据声明的时间分辨率，必须大于零且不超过一天。</param>
    /// <param name="terms">恰好包含二十四个节气时间点的集合。</param>
    /// <exception cref="ArgumentOutOfRangeException">年份或分辨率超出支持范围。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="terms" /> 为空引用。</exception>
    /// <exception cref="ArgumentException">元数据为空白，或节气集合不完整、重复、错序、月份错误或精度不符。</exception>
    public SolarTermInstantCalendar(int year, string version, string source, TimeSpan resolution,
        IEnumerable<SolarTermInstant> terms)
    {
        if (year < 1 || year > 9998)
            throw new ArgumentOutOfRangeException(nameof(year), year, "节气时间表只支持 1～9998 年。");
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("数据版本不能为空白。", nameof(version));
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("数据来源不能为空白。", nameof(source));
        if (resolution <= TimeSpan.Zero || resolution > TimeSpan.FromDays(1))
            throw new ArgumentOutOfRangeException(nameof(resolution), resolution,
                "时间分辨率必须大于零且不超过一天。");
        if (terms == null)
            throw new ArgumentNullException(nameof(terms));

        var orderedTerms = new SolarTermInstant[24];
        var seen = new bool[24];
        var count = 0;
        foreach (var item in terms)
        {
            if (count == orderedTerms.Length)
                throw new ArgumentException("节气集合必须恰好包含二十四项。", nameof(terms));

            var termValue = (int)item.Term;
            if (termValue < 0 || termValue > 23)
                throw new ArgumentException("节气集合包含无效枚举值。", nameof(terms));

            var index = ToChronologicalIndex(item.Term);
            if (seen[index])
                throw new ArgumentException("节气集合不能包含重复节气。", nameof(terms));

            seen[index] = true;
            orderedTerms[index] = item;
            count++;
        }

        if (count != orderedTerms.Length)
            throw new ArgumentException("节气集合必须恰好包含二十四项。", nameof(terms));

        var previousUtcTicks = -1L;
        for (var index = 0; index < orderedTerms.Length; index++)
        {
            if (!seen[index])
                throw new ArgumentException("节气集合缺少必需的节气。", nameof(terms));

            var item = orderedTerms[index];
            DateTimeOffset beijingInstant;
            try
            {
                beijingInstant = item.Instant.ToOffset(BeijingOffset);
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException("节气时间无法表示为北京时间。", nameof(terms), exception);
            }

            var expectedMonth = index / 2 + 1;
            if (beijingInstant.Year != year || beijingInstant.Month != expectedMonth)
                throw new ArgumentException("节气月份或北京时间所属年份与枚举顺序不匹配。", nameof(terms));

            var utcTicks = item.Instant.UtcDateTime.Ticks;
            if (utcTicks <= previousUtcTicks)
                throw new ArgumentException("节气时间必须按小寒至冬至的顺序严格递增。", nameof(terms));
            if (utcTicks % resolution.Ticks != 0)
                throw new ArgumentException("节气 UTC ticks 必须与声明的时间分辨率对齐。", nameof(terms));

            previousUtcTicks = utcTicks;
        }

        Year = year;
        Version = version;
        Source = source;
        Resolution = resolution;
        _terms = orderedTerms;
    }
    /// <summary>
    /// 获取节气数据所属年份。
    /// </summary>
    public int Year { get; }
    /// <summary>
    /// 获取调用方提供的数据版本标识。
    /// </summary>
    public string Version { get; }
    /// <summary>
    /// 获取调用方提供的数据来源说明。
    /// </summary>
    public string Source { get; }
    /// <summary>
    /// 获取调用方声明且已用于校验 UTC ticks 的时间分辨率。
    /// </summary>
    public TimeSpan Resolution { get; }
    /// <summary>
    /// 获取指定节气的时间点。
    /// </summary>
    /// <param name="term">二十四节气枚举。</param>
    /// <returns>与该节气对应的 UTC 时间点。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="term" /> 不属于二十四节气。</exception>
    public SolarTermInstant GetTerm(ChineseSolarTerms term)
    {
        return _terms[ToChronologicalIndex(term)];
    }
    /// <summary>
    /// 获取严格早于或包含指定时间点的上一节气。
    /// </summary>
    /// <param name="instant">任意有效偏移的时间点。</param>
    /// <param name="includeBoundary"><see langword="true" /> 时包含同一时间点的节气；否则严格早于。</param>
    /// <returns>单年度表内最近的上一节气；没有结果时返回 <see langword="null" />。</returns>
    public SolarTermInstant? GetPrevious(DateTimeOffset instant, bool includeBoundary = false)
    {
        var index = includeBoundary ? UpperBound(instant.UtcDateTime.Ticks) : LowerBound(instant.UtcDateTime.Ticks);
        return index == 0 ? null : _terms[index - 1];
    }
    /// <summary>
    /// 获取严格晚于或包含指定时间点的下一节气。
    /// </summary>
    /// <param name="instant">任意有效偏移的时间点。</param>
    /// <param name="includeBoundary"><see langword="true" /> 时包含同一时间点的节气；否则严格晚于。</param>
    /// <returns>单年度表内最近的下一节气；没有结果时返回 <see langword="null" />。</returns>
    public SolarTermInstant? GetNext(DateTimeOffset instant, bool includeBoundary = false)
    {
        var index = includeBoundary ? LowerBound(instant.UtcDateTime.Ticks) : UpperBound(instant.UtcDateTime.Ticks);
        return index == _terms.Length ? null : _terms[index];
    }
    /// <summary>
    /// 将公开枚举索引映射为从小寒开始的年度时序索引。
    /// </summary>
    /// <param name="term">待映射的节气。</param>
    /// <returns>范围为 0～23 的年度时序索引。</returns>
    private static int ToChronologicalIndex(ChineseSolarTerms term)
    {
        var termValue = (int)term;
        if (termValue < 0 || termValue > 23)
            throw new ArgumentOutOfRangeException(nameof(term), term, "节气枚举值必须在 0～23 范围内。");

        return termValue >= 22 ? termValue - 22 : termValue + 2;
    }
    /// <summary>
    /// 查找第一个时间点大于或等于目标 UTC ticks 的索引。
    /// </summary>
    /// <param name="utcTicks">UTC ticks。</param>
    /// <returns>插入位置，可能等于数组长度。</returns>
    private int LowerBound(long utcTicks)
    {
        var low = 0;
        var high = _terms.Length;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (_terms[middle].Instant.UtcDateTime.Ticks < utcTicks)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }
    /// <summary>
    /// 查找第一个时间点严格大于目标 UTC ticks 的索引。
    /// </summary>
    /// <param name="utcTicks">UTC ticks。</param>
    /// <returns>插入位置，可能等于数组长度。</returns>
    private int UpperBound(long utcTicks)
    {
        var low = 0;
        var high = _terms.Length;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (_terms[middle].Instant.UtcDateTime.Ticks <= utcTicks)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }
}
