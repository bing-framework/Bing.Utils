using NodaTime;
using NodaTime.Calendars;

namespace Bing.Date;
/// <summary>
/// 提供 ISO 8601 周历与公历日期之间的转换。
/// </summary>
/// <remarks>
/// ISO 周以星期一为每周首日，包含新年首个星期四的周为第 1 周。输入日期按年月日字段解释，不转换时区。
/// </remarks>
public static class IsoWeekCalendar
{
    /// <summary>
    /// ISO 8601 周所属年份的计算规则。
    /// </summary>
    private static readonly IWeekYearRule IsoRule = WeekYearRules.Iso;
    /// <summary>
    /// 获取日期对应的 ISO 周所属年份和周号。
    /// </summary>
    /// <param name="date">公历日期，忽略时间和 Kind。</param>
    /// <returns>包含 ISO 周所属年份及周号的元组。</returns>
    public static (int WeekYear, int WeekNumber) GetIsoWeek(DateTime date)
    {
        var localDate = LocalDate.FromDateTime(date);
        return (IsoRule.GetWeekYear(localDate), IsoRule.GetWeekOfWeekYear(localDate));
    }
    /// <summary>
    /// 获取指定 ISO 周所属年份的周数。
    /// </summary>
    /// <param name="weekYear">ISO 周所属年份，范围为 1～9999。</param>
    /// <returns>该年包含 52 周或 53 周。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="weekYear" /> 不在 1～9999 范围内。</exception>
    public static int GetWeeksInYear(int weekYear)
    {
        ValidateWeekYear(weekYear);
        return IsoRule.GetWeeksInWeekYear(weekYear, CalendarSystem.Iso);
    }
    /// <summary>
    /// 根据 ISO 周所属年份、周号和星期恢复公历自然日。
    /// </summary>
    /// <param name="weekYear">ISO 周所属年份，范围为 1～9999。</param>
    /// <param name="weekNumber">ISO 周号，必须在该年份的有效周数范围内。</param>
    /// <param name="dayOfWeek">星期值，使用 .NET 的 <see cref="DayOfWeek" />。</param>
    /// <returns>午夜且 Kind 为 Unspecified 的公历日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">年份、周号或星期值无效，或结果超出 DateTime 可表示范围。</exception>
    public static DateTime GetDate(int weekYear, int weekNumber, DayOfWeek dayOfWeek)
    {
        ValidateWeekYear(weekYear);
        var weeksInYear = IsoRule.GetWeeksInWeekYear(weekYear, CalendarSystem.Iso);
        if (weekNumber < 1 || weekNumber > weeksInYear)
            throw new ArgumentOutOfRangeException(nameof(weekNumber), weekNumber, $"ISO 周号必须在 1～{weeksInYear} 范围内。");

        var day = (int)dayOfWeek;
        if (day < (int)DayOfWeek.Sunday || day > (int)DayOfWeek.Saturday)
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, "星期值无效。");

        var isoDay = day == (int)DayOfWeek.Sunday ? IsoDayOfWeek.Sunday : (IsoDayOfWeek)day;
        LocalDate result;
        try
        {
            result = IsoRule.GetLocalDate(weekYear, weekNumber, isoDay, CalendarSystem.Iso);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new ArgumentOutOfRangeException(nameof(weekYear), weekYear, exception.Message);
        }

        try
        {
            return result.ToDateTimeUnspecified();
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, exception.Message);
        }
    }
    /// <summary>
    /// 验证 ISO 周所属年份在可表示范围内。
    /// </summary>
    /// <param name="weekYear">ISO 周所属年份。</param>
    private static void ValidateWeekYear(int weekYear)
    {
        if (weekYear < 1 || weekYear > 9999)
            throw new ArgumentOutOfRangeException(nameof(weekYear), weekYear, "ISO 周所属年份必须在 1～9999 范围内。");
    }
}
