#if NET6_0_OR_GREATER
using Bing.Date.Calendars;
using Bing.Date.Chinese;
using Bing.Date.TimeZones;

namespace Bing.Date;
/// <summary>
/// 将 .NET 6 的日期和时间值接入日期模块的农历、ISO 周、业务日历及时区功能。
/// </summary>
public static class DateOnlyTimeOnlyExtensions
{
    /// <summary>
    /// 将公历自然日转换为农历日期。
    /// </summary>
    /// <param name="date">公历日期；没有时间或时区信息。</param>
    /// <returns>对应的农历日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">公历日期超出农历支持范围。</exception>
    public static LunarDate ToLunarDate(this DateOnly date) =>
        ChineseLunarCalendar.FromDate(ToDateTime(date));
    /// <summary>
    /// 尝试将公历自然日转换为农历日期。
    /// </summary>
    /// <param name="date">公历日期。</param>
    /// <param name="result">成功时为农历日期；失败时为默认无效值。</param>
    /// <returns>日期受支持时为 true。</returns>
    public static bool TryToLunarDate(this DateOnly date, out LunarDate result) =>
        ChineseLunarCalendar.TryFromDate(ToDateTime(date), out result);
    /// <summary>
    /// 将农历日期转换为公历自然日。
    /// </summary>
    /// <param name="date">有效农历日期。</param>
    /// <returns>对应的公历日期。</returns>
    /// <exception cref="InvalidOperationException">农历日期是默认无效值。</exception>
    public static DateOnly ToDateOnly(this LunarDate date) => DateOnly.FromDateTime(date.ToDateTime());
    /// <summary>
    /// 获取公历日期所属 ISO 周年和周号。
    /// </summary>
    /// <param name="date">公历自然日。</param>
    /// <returns>ISO 周所属年份及周号。</returns>
    public static (int WeekYear, int WeekNumber) GetIsoWeek(this DateOnly date) =>
        IsoWeekCalendar.GetIsoWeek(ToDateTime(date));
    /// <summary>
    /// 根据 ISO 周所属年份、周号和星期恢复公历自然日。
    /// </summary>
    /// <param name="weekYear">ISO 周所属年份。</param>
    /// <param name="weekNumber">ISO 周号。</param>
    /// <param name="dayOfWeek">星期值。</param>
    /// <returns>对应的公历自然日。</returns>
    /// <exception cref="ArgumentOutOfRangeException">ISO 周参数无效或日期超出支持范围。</exception>
    public static DateOnly GetDateFromIsoWeek(int weekYear, int weekNumber, DayOfWeek dayOfWeek) =>
        DateOnly.FromDateTime(IsoWeekCalendar.GetDate(weekYear, weekNumber, dayOfWeek));
    /// <summary>
    /// 判断年度业务日历中的日期是否为业务日。
    /// </summary>
    /// <param name="calendar">年度业务日历。</param>
    /// <param name="date">待判断日期。</param>
    /// <returns>该日期为业务日时为 true。</returns>
    /// <exception cref="ArgumentNullException">业务日历为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">日期不属于该日历年度。</exception>
    public static bool IsBusinessDay(this AnnualBusinessCalendar calendar, DateOnly date)
    {
        EnsureCalendar(calendar);
        return calendar.IsBusinessDay(ToDateTime(date));
    }
    /// <summary>
    /// 统计年度业务日历中半开日期区间的业务日数量。
    /// </summary>
    /// <param name="calendar">年度业务日历。</param>
    /// <param name="startInclusive">包含的起始日期。</param>
    /// <param name="endExclusive">不包含的结束日期。</param>
    /// <returns>半开区间内的业务日数量，反向区间返回负数。</returns>
    /// <exception cref="ArgumentNullException">业务日历为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">区间端点超出日历年度及其次年元旦边界。</exception>
    public static int CountBusinessDays(this AnnualBusinessCalendar calendar, DateOnly startInclusive, DateOnly endExclusive)
    {
        EnsureCalendar(calendar);
        return calendar.CountBusinessDays(ToDateTime(startInclusive), ToDateTime(endExclusive));
    }
    /// <summary>
    /// 在年度业务日历中偏移指定数量的业务日。
    /// </summary>
    /// <param name="calendar">年度业务日历。</param>
    /// <param name="date">起始日期；不计入偏移量。</param>
    /// <param name="businessDays">有符号业务日偏移量。</param>
    /// <returns>偏移后的公历日期。</returns>
    /// <exception cref="ArgumentNullException">业务日历为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">目标超出该年度可表示的业务日范围。</exception>
    public static DateOnly AddBusinessDays(this AnnualBusinessCalendar calendar, DateOnly date, int businessDays)
    {
        EnsureCalendar(calendar);
        return DateOnly.FromDateTime(calendar.AddBusinessDays(ToDateTime(date), businessDays));
    }
    /// <summary>
    /// 判断已加载的跨年业务日历中的日期是否为业务日。
    /// </summary>
    /// <param name="calendar">跨年业务日历。</param>
    /// <param name="date">待判断日期。</param>
    /// <returns>该日期为业务日时为 true。</returns>
    /// <exception cref="ArgumentNullException">业务日历为空。</exception>
    /// <exception cref="KeyNotFoundException">日期所属年度未加载。</exception>
    public static bool IsBusinessDay(this BusinessCalendarSet calendar, DateOnly date)
    {
        EnsureCalendar(calendar);
        return calendar.IsBusinessDay(ToDateTime(date));
    }
    /// <summary>
    /// 统计跨年业务日历中半开日期区间的业务日数量。
    /// </summary>
    /// <param name="calendar">跨年业务日历。</param>
    /// <param name="startInclusive">包含的起始日期。</param>
    /// <param name="endExclusive">不包含的结束日期。</param>
    /// <returns>半开区间内的业务日数量，反向区间返回负数。</returns>
    /// <exception cref="ArgumentNullException">业务日历为空。</exception>
    /// <exception cref="KeyNotFoundException">区间实际经过的年度未加载。</exception>
    public static int CountBusinessDays(this BusinessCalendarSet calendar, DateOnly startInclusive, DateOnly endExclusive)
    {
        EnsureCalendar(calendar);
        return calendar.CountBusinessDays(ToDateTime(startInclusive), ToDateTime(endExclusive));
    }
    /// <summary>
    /// 在已加载的跨年业务日历中偏移指定数量的业务日。
    /// </summary>
    /// <param name="calendar">跨年业务日历。</param>
    /// <param name="date">起始日期；不计入偏移量。</param>
    /// <param name="businessDays">有符号业务日偏移量。</param>
    /// <returns>偏移后的公历日期。</returns>
    /// <exception cref="ArgumentNullException">业务日历为空。</exception>
    /// <exception cref="KeyNotFoundException">搜索所需年度未加载。</exception>
    /// <exception cref="ArgumentOutOfRangeException">目标日期超出可表示范围。</exception>
    public static DateOnly AddBusinessDays(this BusinessCalendarSet calendar, DateOnly date, int businessDays)
    {
        EnsureCalendar(calendar);
        return DateOnly.FromDateTime(calendar.AddBusinessDays(ToDateTime(date), businessDays));
    }
    /// <summary>
    /// 按显式时区将日期和墙上时间解析为明确的时间点。
    /// </summary>
    /// <param name="date">来源时区中的自然日。</param>
    /// <param name="time">来源时区中的墙上时间，保留 tick 精度。</param>
    /// <param name="sourceTimeZone">日期和时间所属时区。</param>
    /// <param name="ambiguousTimeResolution">夏令时重复时间的解析策略；默认拒绝。</param>
    /// <returns>保留原有子秒精度的时间点和时区偏移。</returns>
    /// <exception cref="ArgumentNullException">来源时区为空。</exception>
    /// <exception cref="ArgumentException">墙上时间因 DST 跳时而无效，或重复时间未显式消歧。</exception>
    /// <exception cref="ArgumentOutOfRangeException">歧义策略无效或 UTC 时间点不可表示。</exception>
    public static DateTimeOffset ResolveLocalTime(
        this DateOnly date,
        TimeOnly time,
        TimeZoneInfo sourceTimeZone,
        AmbiguousTimeResolution ambiguousTimeResolution = AmbiguousTimeResolution.Reject) =>
        TimeZoneConversion.ResolveLocalTime(ToDateTime(date, time), sourceTimeZone, ambiguousTimeResolution);
    /// <summary>
    /// 将日期与时间转换为未指定时区的日期时间。
    /// </summary>
    /// <param name="date">不含时区的自然日。</param>
    /// <returns>指定自然日的午夜，Kind 为 Unspecified。</returns>
    private static DateTime ToDateTime(DateOnly date) => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
    /// <summary>
    /// 将日期与时间转换为未指定时区的日期时间。
    /// </summary>
    /// <param name="date">不含时区的自然日。</param>
    /// <param name="time">不含时区的墙上时间。</param>
    /// <returns>指定日期和时间，Kind 为 Unspecified。</returns>
    private static DateTime ToDateTime(DateOnly date, TimeOnly time) => date.ToDateTime(time, DateTimeKind.Unspecified);
    /// <summary>
    /// 验证业务日历不为空。
    /// </summary>
    /// <param name="calendar">待验证的业务日历。</param>
    private static void EnsureCalendar(AnnualBusinessCalendar calendar)
    {
        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar));
    }
    /// <summary>
    /// 验证业务日历不为空。
    /// </summary>
    /// <param name="calendar">待验证的业务日历。</param>
    private static void EnsureCalendar(BusinessCalendarSet calendar)
    {
        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar));
    }
}
#endif
