namespace Bing.Date.Calendars;
/// <summary>
/// 仅在纽约时区运行本地夏令时回拨测试。
/// </summary>
public sealed class NewYorkFactAttribute : FactAttribute
{
    /// <summary>
    /// 初始化一个 <see cref="NewYorkFactAttribute"/> 类型的实例。
    /// </summary>
    public NewYorkFactAttribute()
    {
        var zone = TimeZoneInfo.Local.Id;
        if (zone != "America/New_York" && zone != "Eastern Standard Time")
            Skip = "Requires America/New_York or Eastern Standard Time.";
    }
}
/// <summary>
/// 验证跨年业务日偏移保留夏令时重复时间选择。
/// </summary>
public class BusinessCalendarSetDstTest
{
    /// <summary>
    /// 验证回拨时同一钟面时间的两种 UTC 时间点跨年后保持各自的歧义标志。
    /// </summary>
    [NewYorkFact]
    public void AddBusinessDays_AcrossDstFold_PreservesOriginalInstantChoice()
    {
        var calendar = new BusinessCalendarSet(new[]
        {
            new AnnualBusinessCalendar(2024, "test", "2024", "test", restDays: Array.Empty<DayOfWeek>()),
            new AnnualBusinessCalendar(2025, "test", "2025", "test", restDays: Array.Empty<DayOfWeek>())
        });
        var originalEarly = new DateTime(2024, 11, 3, 5, 30, 0, DateTimeKind.Utc).ToLocalTime();
        var originalLate = new DateTime(2024, 11, 3, 6, 30, 0, DateTimeKind.Utc).ToLocalTime();
        TimeZoneInfo.Local.IsAmbiguousTime(originalEarly).ShouldBeTrue();
        originalEarly.Ticks.ShouldBe(originalLate.Ticks);
        originalEarly.Kind.ShouldBe(DateTimeKind.Local);

        var earlyResult = calendar.AddBusinessDays(originalEarly, 364);
        var lateResult = calendar.AddBusinessDays(originalLate, 364);

        earlyResult.Ticks.ShouldBe(lateResult.Ticks);
        earlyResult.Kind.ShouldBe(DateTimeKind.Local);
        lateResult.Kind.ShouldBe(DateTimeKind.Local);
        earlyResult.ToUniversalTime().ShouldBe(new DateTime(2025, 11, 2, 5, 30, 0, DateTimeKind.Utc));
        lateResult.ToUniversalTime().ShouldBe(new DateTime(2025, 11, 2, 6, 30, 0, DateTimeKind.Utc));
    }
}
