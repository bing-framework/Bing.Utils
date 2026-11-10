using System;

namespace Bing.Date.Chinese;
/// <summary>
/// 表示已验证的中国农历自然日。
/// </summary>
/// <remarks>
/// 月份使用 1～12 的显示月及独立闰月标志；默认值无效，不包含时区或时刻。
/// </remarks>
public readonly struct LunarDate : IEquatable<LunarDate>
{
    /// <summary>
    /// 与农历日期对应的午夜 Unspecified 公历日期。
    /// </summary>
    private readonly DateTime _date;
    /// <summary>
    /// 初始化一个 <see cref="LunarDate"/> 类型的实例。
    /// </summary>
    /// <param name="date">对应的公历自然日。</param>
    /// <param name="year">农历年份。</param>
    /// <param name="month">农历显示月份。</param>
    /// <param name="day">农历日。</param>
    /// <param name="calendarMonth">BCL 农历月份序号。</param>
    /// <param name="isLeapMonth">是否为闰月。</param>
    internal LunarDate(DateTime date, int year, int month, int day, int calendarMonth, bool isLeapMonth)
    {
        _date = date;
        Year = year;
        Month = month;
        Day = day;
        CalendarMonth = calendarMonth;
        IsLeapMonth = isLeapMonth;
    }
    /// <summary>
    /// 获取该值是否由农历工厂创建；默认值为 false。
    /// </summary>
    public bool IsValid => Year != 0;
    /// <summary>
    /// 获取农历年份；默认值为零。
    /// </summary>
    public int Year { get; }
    /// <summary>
    /// 获取农历显示月份，合法值为 1～12；默认值为零。
    /// </summary>
    public int Month { get; }
    /// <summary>
    /// 获取农历日；默认值为零。
    /// </summary>
    public int Day { get; }
    /// <summary>
    /// 获取是否为显示月份对应的闰月。
    /// </summary>
    public bool IsLeapMonth { get; }
    /// <summary>
    /// 获取 BCL 农历月份序号，闰年可能为 13；默认值为零。
    /// </summary>
    public int CalendarMonth { get; }
    /// <summary>
    /// 获取对应的公历自然日。
    /// </summary>
    /// <returns>午夜且 Kind 为 Unspecified 的公历日期。</returns>
    /// <exception cref="InvalidOperationException">当前值是无效的默认值。</exception>
    public DateTime ToDateTime()
    {
        EnsureValid();
        return _date;
    }
    /// <summary>
    /// 获取当前农历月份的天数。
    /// </summary>
    /// <returns>月份的实际天数。</returns>
    /// <exception cref="InvalidOperationException">当前值是无效的默认值。</exception>
    public int GetDaysInMonth()
    {
        EnsureValid();
        return ChineseLunarCalendar.DaysInMonth(this);
    }
    /// <summary>
    /// 获取当前农历年份的天数。
    /// </summary>
    /// <returns>年份的实际天数。</returns>
    /// <exception cref="InvalidOperationException">当前值是无效的默认值。</exception>
    public int GetDaysInYear()
    {
        EnsureValid();
        return ChineseLunarCalendar.DaysInYear(this);
    }
    /// <summary>
    /// 使用现有简体或繁体字表格式化农历日期。
    /// </summary>
    /// <param name="traditionalChineseCharacters">是否使用繁体字形。</param>
    /// <param name="includeYear">是否包含农历年份。</param>
    /// <returns>农历日期文本。</returns>
    /// <exception cref="InvalidOperationException">当前值是无效的默认值。</exception>
    public string ToChineseString(bool traditionalChineseCharacters = false, bool includeYear = true)
    {
        EnsureValid();
        return ChineseDateHelper.FormatLunarDate(Year, Month, Day, IsLeapMonth, traditionalChineseCharacters, includeYear);
    }
    /// <summary>
    /// 按公历自然日数量移动农历日期。
    /// </summary>
    /// <param name="days">有符号自然日偏移，不表示经过的时间长度。</param>
    /// <returns>偏移后的农历日期。</returns>
    /// <exception cref="InvalidOperationException">当前值是无效的默认值。</exception>
    /// <exception cref="ArgumentOutOfRangeException">目标日期超出农历支持范围。</exception>
    public LunarDate AddDays(int days)
    {
        EnsureValid();
        var earlierDays = (_date.Ticks - ChineseLunarCalendar.MinDate.Ticks) / TimeSpan.TicksPerDay;
        var laterDays = (ChineseLunarCalendar.MaxDate.Ticks - _date.Ticks) / TimeSpan.TicksPerDay;
        if (days < -earlierDays || days > laterDays)
            throw new ArgumentOutOfRangeException(nameof(days), days, "目标日期超出农历支持范围。");
        return ChineseLunarCalendar.FromDate(_date.AddDays(days));
    }
    /// <summary>
    /// 按实际农历月份序列移动日期。
    /// </summary>
    /// <param name="months">有符号农历月偏移，闰月单独计为一个月。</param>
    /// <param name="overflow">目标月缺少原日号时的处理策略。</param>
    /// <returns>偏移后的农历日期。</returns>
    /// <exception cref="InvalidOperationException">当前值是无效的默认值。</exception>
    /// <exception cref="ArgumentOutOfRangeException">策略无效、目标超出范围或默认策略拒绝截断。</exception>
    public LunarDate AddMonths(int months, LunarDayOverflow overflow = LunarDayOverflow.Reject)
    {
        EnsureValid();
        return ChineseLunarCalendar.AddMonths(this, months, overflow);
    }
    /// <summary>
    /// 将农历日期移动到指定年份，保留显示月和日号。
    /// </summary>
    /// <param name="year">目标农历年份。</param>
    /// <param name="missingLeapMonth">目标年没有同名闰月时的处理策略。</param>
    /// <param name="missingDay">目标月没有原日号时的处理策略。</param>
    /// <returns>目标农历年份中的对应日期。</returns>
    /// <exception cref="InvalidOperationException">当前值为无效默认值。</exception>
    /// <exception cref="ArgumentOutOfRangeException">年份或策略无效，或策略拒绝目标日期。</exception>
    public LunarDate InYear(int year, LunarLeapMonthOverflow missingLeapMonth = LunarLeapMonthOverflow.Reject,
        LunarDayOverflow missingDay = LunarDayOverflow.Reject)
    {
        EnsureValid();
        return ChineseLunarCalendar.MoveToYear(this, year, missingLeapMonth, missingDay, nameof(year), year);
    }
    /// <summary>
    /// 按农历年份偏移，并保留显示月和日号。
    /// </summary>
    /// <param name="years">有符号农历年偏移。</param>
    /// <param name="missingLeapMonth">目标年没有同名闰月时的处理策略。</param>
    /// <param name="missingDay">目标月没有原日号时的处理策略。</param>
    /// <returns>目标农历年份中的对应日期。</returns>
    /// <exception cref="InvalidOperationException">当前值为无效默认值。</exception>
    /// <exception cref="ArgumentOutOfRangeException">偏移或策略无效，或策略拒绝目标日期。</exception>
    public LunarDate AddYears(int years, LunarLeapMonthOverflow missingLeapMonth = LunarLeapMonthOverflow.Reject,
        LunarDayOverflow missingDay = LunarDayOverflow.Reject)
    {
        EnsureValid();
        return ChineseLunarCalendar.MoveToYear(this, (long)Year + years, missingLeapMonth, missingDay, nameof(years), years);
    }
    /// <inheritdoc />
    /// <remarks>两个值表示相同自然日或均为默认值时相等。</remarks>
    public bool Equals(LunarDate other) => _date.Ticks == other._date.Ticks;
    /// <inheritdoc />
    public override bool Equals(object obj) => obj is LunarDate other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() => _date.Ticks.GetHashCode();
    /// <summary>
    /// 比较两个农历日期是否相等。
    /// </summary>
    /// <param name="left">左侧日期。</param>
    /// <param name="right">右侧日期。</param>
    /// <returns>相等时为 true，否则为 false。</returns>
    public static bool operator ==(LunarDate left, LunarDate right) => left.Equals(right);
    /// <summary>
    /// 比较两个农历日期是否不同。
    /// </summary>
    /// <param name="left">左侧日期。</param>
    /// <param name="right">右侧日期。</param>
    /// <returns>不相等时为 true，否则为 false。</returns>
    public static bool operator !=(LunarDate left, LunarDate right) => !left.Equals(right);
    /// <summary>
    /// 拒绝默认构造的无效值参与日期运算。
    /// </summary>
    /// <exception cref="InvalidOperationException">当前值是无效的默认值。</exception>
    private void EnsureValid()
    {
        if (!IsValid)
            throw new InvalidOperationException("默认 LunarDate 不表示有效农历日期。");
    }
}
/// <summary>
/// 指定农历月份偏移时目标月缺少原日号的处理策略。
/// </summary>
public enum LunarDayOverflow
{
    /// <summary>
    /// 拒绝不存在原日号的目标月份。
    /// </summary>
    Reject,
    /// <summary>
    /// 将日期调整为目标月份的最后一天。
    /// </summary>
    ClampToLastDay
}
/// <summary>
/// 目标农历年没有原闰月时的处理策略。
/// </summary>
public enum LunarLeapMonthOverflow
{
    /// <summary>
    /// 拒绝没有同名闰月的目标年份。
    /// </summary>
    Reject,
    /// <summary>
    /// 使用相同显示月份的普通月。
    /// </summary>
    UseRegularMonth
}
