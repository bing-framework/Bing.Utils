namespace Bing.Date.Chinese;

/// <summary>
/// 表示一个节气及其北京时间自然日。
/// </summary>
/// <remarks>日期为午夜 Unspecified，不表示精确交节时间点。</remarks>
public readonly struct SolarTermDate
{
    /// <summary>
    /// 初始化一个 <see cref="SolarTermDate"/> 类型的实例。
    /// </summary>
    /// <param name="term">节气。</param>
    /// <param name="date">北京时间自然日。</param>
    internal SolarTermDate(ChineseSolarTerms term, DateTime date)
    {
        Term = term;
        Date = date;
    }

    /// <summary>
    /// 获取节气枚举。
    /// </summary>
    public ChineseSolarTerms Term { get; }

    /// <summary>
    /// 获取午夜 Unspecified 的北京时间自然日。
    /// </summary>
    public DateTime Date { get; }
}

/// <summary>
/// 按官方 UTC+8 日期表提供类型化节气查询。
/// </summary>
/// <remarks>查询不联网、不分配结果对象；范围外拒绝，范围内无结果返回 null。</remarks>
public static class SolarTermCalendar
{
    /// <summary>
    /// 获取支持的最早自然日。
    /// </summary>
    public static DateTime MinDate => new(ChineseSolarTermDateData.MinYear, 1, 1);

    /// <summary>
    /// 获取支持的最晚自然日。
    /// </summary>
    public static DateTime MaxDate => new(ChineseSolarTermDateData.MaxYear, 12, 31);

    /// <summary>
    /// 获取内嵌官方日期表的数据记录版本。
    /// </summary>
    public static string DataVersion => "2026-10-29";

    /// <summary>
    /// 查询指定北京时间自然日当天的节气。
    /// </summary>
    /// <param name="date">只读取年月日，不转换 Kind 或时间字段。</param>
    /// <returns>当天的节气；当天不是节气时返回 null。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期超出支持范围。</exception>
    public static SolarTermDate? GetTerm(DateTime date) => Find(date, 0);

    /// <summary>
    /// 查询指定时间点所在北京时间自然日的节气。
    /// </summary>
    /// <param name="instant">任意有效偏移的时间点。</param>
    /// <returns>UTC+8 当天的节气；当天不是节气时返回 null。</returns>
    /// <exception cref="ArgumentOutOfRangeException">时间点对应的 UTC+8 日期超出支持范围。</exception>
    public static SolarTermDate? GetTerm(DateTimeOffset instant) => GetTerm(ToBeijingDate(instant));

    /// <summary>
    /// 查询严格早于指定自然日的上一节气。
    /// </summary>
    /// <param name="date">北京时间自然日，只读取年月日。</param>
    /// <returns>上一节气；表中没有更早日期时返回 null。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期超出支持范围。</exception>
    public static SolarTermDate? GetPrevious(DateTime date) => Find(date, -1);

    /// <summary>
    /// 查询严格早于时间点所在北京时间自然日的上一节气。
    /// </summary>
    /// <param name="instant">待查询的时间点。</param>
    /// <returns>上一节气；表中没有更早日期时返回 null。</returns>
    /// <exception cref="ArgumentOutOfRangeException">UTC+8 日期超出支持范围。</exception>
    public static SolarTermDate? GetPrevious(DateTimeOffset instant) => GetPrevious(ToBeijingDate(instant));

    /// <summary>
    /// 查询严格晚于指定自然日的下一节气。
    /// </summary>
    /// <param name="date">北京时间自然日，只读取年月日。</param>
    /// <returns>下一节气；表中没有更晚日期时返回 null。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期超出支持范围。</exception>
    public static SolarTermDate? GetNext(DateTime date) => Find(date, 1);

    /// <summary>
    /// 查询严格晚于时间点所在北京时间自然日的下一节气。
    /// </summary>
    /// <param name="instant">待查询的时间点。</param>
    /// <returns>下一节气；表中没有更晚日期时返回 null。</returns>
    /// <exception cref="ArgumentOutOfRangeException">UTC+8 日期超出支持范围。</exception>
    public static SolarTermDate? GetNext(DateTimeOffset instant) => GetNext(ToBeijingDate(instant));

    /// <summary>
    /// 将时间点换算为表使用的固定 UTC+8 自然日并先检查边界。
    /// </summary>
    /// <param name="instant">输入时间点。</param>
    /// <returns>午夜 Unspecified 自然日。</returns>
    private static DateTime ToBeijingDate(DateTimeOffset instant)
    {
        var ticks = instant.UtcDateTime.Ticks + 8 * TimeSpan.TicksPerHour;
        if (ticks < MinDate.Ticks || ticks >= MaxDate.Ticks + TimeSpan.TicksPerDay)
            throw new ArgumentOutOfRangeException(nameof(instant), "北京时间日期超出节气表支持范围。");
        return new DateTime(ticks, DateTimeKind.Unspecified).Date;
    }

    /// <summary>
    /// 在最多 24 项的年度表中按日期方向查找。
    /// </summary>
    /// <param name="date">查询日期。</param>
    /// <param name="direction">零表示当天，负数表示上一项，正数表示下一项。</param>
    /// <returns>匹配项；在表边界没有结果时返回 null。</returns>
    private static SolarTermDate? Find(DateTime date, int direction)
    {
        if (!ChineseSolarTermDateData.IsSupportedYear(date.Year))
            throw new ArgumentOutOfRangeException(nameof(date), "日期超出节气表支持范围。");
        var day = date.Date;
        for (var step = 0; step < 24; step++)
        {
            var index = direction < 0 ? 23 - step : step;
            var item = Create(date.Year, index);
            if (direction == 0 ? item.Date == day : direction < 0 ? item.Date < day : item.Date > day)
                return item;
        }
        if (direction < 0 && date.Year > ChineseSolarTermDateData.MinYear)
            return Create(date.Year - 1, 23);
        if (direction > 0 && date.Year < ChineseSolarTermDateData.MaxYear)
            return Create(date.Year + 1, 0);
        return null;
    }

    /// <summary>
    /// 从年度表索引构造节气日期。
    /// </summary>
    /// <param name="year">已验证年份。</param>
    /// <param name="index">从小寒开始的表索引。</param>
    /// <returns>节气和午夜日期。</returns>
    private static SolarTermDate Create(int year, int index) => new(
        // 公开枚举从立春开始，表从小寒开始。
        (ChineseSolarTerms)((index + 22) % 24),
        new DateTime(year, index / 2 + 1, ChineseSolarTermDateData.GetDayOfMonth(year, index)));
}
