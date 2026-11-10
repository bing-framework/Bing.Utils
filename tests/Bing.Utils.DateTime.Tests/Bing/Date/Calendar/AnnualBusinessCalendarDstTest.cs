namespace Bing.Date.Calendars;
/// <summary>
/// 验证年度业务日偏移保留本地夏令时重复时间的选择。
/// </summary>
public class AnnualBusinessCalendarDstTest
{
    /// <summary>
    /// 验证正反业务日偏移能分别恢复回拨期间的两种实际时间点。
    /// </summary>
    [NewYorkFact]
    public void AddBusinessDays_BothFoldOccurrences_RoundTripWithoutChangingUtcChoice()
    {
        var annual = new AnnualBusinessCalendar(2024, "test", "1", "synthetic",
            restDays: Array.Empty<DayOfWeek>());
        var set = new BusinessCalendarSet(new[] { annual });
        foreach (var utcHour in new[] { 5, 6 })
        {
            var utc = new DateTime(2024, 11, 3, utcHour, 30, 20, DateTimeKind.Utc).AddTicks(1_234_567);
            var original = utc.ToLocalTime();
            TimeZoneInfo.Local.IsAmbiguousTime(original).ShouldBeTrue();

            var results = new[]
            {
                annual.AddBusinessDays(original.AddDays(1), -1),
                annual.AddBusinessDays(original.AddDays(-1), 1),
                annual.AddBusinessDays(annual.AddBusinessDays(original, 1), -1),
                annual.AddBusinessDays(annual.AddBusinessDays(original, -1), 1),
                annual.AddBusinessDays(original, 0),
                set.AddBusinessDays(original.AddDays(1), -1)
            };
            foreach (var result in results)
            {
                result.Ticks.ShouldBe(original.Ticks);
                result.Kind.ShouldBe(DateTimeKind.Local);
                result.ToUniversalTime().Ticks.ShouldBe(utc.Ticks);
                result.ToBinary().ShouldBe(original.ToBinary());
            }
        }
    }
}
