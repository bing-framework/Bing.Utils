using Bing.Date.Calendars;

namespace Bing.Date;

/// <summary>
/// 验证日期截断及导航保留精度、Kind 和本地重复时间选择。
/// </summary>
public class DateTimeNavigationBoundaryRegressionTest
{
    /// <summary>
    /// 验证各时间单位清除低位 ticks，保留 Kind，并支持日期极值。
    /// </summary>
    /// <param name="kind">原值及预期结果的日期种类。</param>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void BeginningOfTimeUnits_AllKinds_TruncateTicksAndSupportExtremes(DateTimeKind kind)
    {
        var value = new DateTime(2024, 11, 3, 1, 30, 20, kind).AddTicks(1_234_567);
        var cases = new (DateTime Actual, DateTime Expected)[]
        {
            (value.BeginningOfSecond(), new DateTime(2024, 11, 3, 1, 30, 20, kind)),
            (value.BeginningOfMinute(), new DateTime(2024, 11, 3, 1, 30, 0, kind)),
            (value.BeginningOfHour(), new DateTime(2024, 11, 3, 1, 0, 0, kind)),
            (value.BeginningOfDay(), new DateTime(2024, 11, 3, 0, 0, 0, kind)),
            (value.BeginningOfDay(8), new DateTime(2024, 11, 3, 8, 0, 0, kind))
        };
        foreach (var item in cases)
        {
            item.Actual.Ticks.ShouldBe(item.Expected.Ticks);
            item.Actual.Kind.ShouldBe(kind);
        }

        var minimum = DateTime.SpecifyKind(DateTime.MinValue, kind);
        minimum.BeginningOfSecond().ToBinary().ShouldBe(minimum.ToBinary());
        minimum.BeginningOfMinute().ToBinary().ShouldBe(minimum.ToBinary());
        minimum.BeginningOfHour().ToBinary().ShouldBe(minimum.ToBinary());
        minimum.BeginningOfDay().ToBinary().ShouldBe(minimum.ToBinary());

        var maximum = DateTime.SpecifyKind(DateTime.MaxValue, kind);
        maximum.BeginningOfSecond().Ticks.ShouldBe(new DateTime(9999, 12, 31, 23, 59, 59).Ticks);
        maximum.BeginningOfMinute().Ticks.ShouldBe(new DateTime(9999, 12, 31, 23, 59, 0).Ticks);
        maximum.BeginningOfHour().Ticks.ShouldBe(new DateTime(9999, 12, 31, 23, 0, 0).Ticks);
        maximum.BeginningOfDay().Ticks.ShouldBe(new DateTime(9999, 12, 31).Ticks);
        maximum.BeginningOfDay().Kind.ShouldBe(kind);
    }

    /// <summary>
    /// 验证重复小时的两次出现分别截断到各自的实际时间点。
    /// </summary>
    [NewYorkFact]
    public void BeginningOfTimeUnits_BothFoldOccurrences_PreserveUtcChoice()
    {
        foreach (var utcHour in new[] { 5, 6 })
        {
            var utc = new DateTime(2024, 11, 3, utcHour, 30, 20, DateTimeKind.Utc).AddTicks(1_234_567);
            var local = utc.ToLocalTime();
            TimeZoneInfo.Local.IsAmbiguousTime(local).ShouldBeTrue();

            AssertLocalBoundary(local.BeginningOfSecond(), new DateTime(2024, 11, 3, 1, 30, 20),
                new DateTime(2024, 11, 3, utcHour, 30, 20, DateTimeKind.Utc));
            AssertLocalBoundary(local.BeginningOfMinute(), new DateTime(2024, 11, 3, 1, 30, 0),
                new DateTime(2024, 11, 3, utcHour, 30, 0, DateTimeKind.Utc));
            AssertLocalBoundary(local.BeginningOfHour(), new DateTime(2024, 11, 3, 1, 0, 0),
                new DateTime(2024, 11, 3, utcHour, 0, 0, DateTimeKind.Utc));

            var exactBoundary = new DateTime(2024, 11, 3, utcHour, 0, 0, DateTimeKind.Utc).ToLocalTime();
            exactBoundary.BeginningOfSecond().ToBinary().ShouldBe(exactBoundary.ToBinary());
            exactBoundary.BeginningOfMinute().ToBinary().ShouldBe(exactBoundary.ToBinary());
            exactBoundary.BeginningOfHour().ToBinary().ShouldBe(exactBoundary.ToBinary());
        }
    }

    /// <summary>
    /// 验证自然日及更大周期导航清零时间后仍可恢复原重复时间选择。
    /// </summary>
    [NewYorkFact]
    public void CalendarBoundaries_RestoreOriginalClockTicks_PreserveFoldChoice()
    {
        foreach (var utcHour in new[] { 5, 6 })
        {
            var utc = new DateTime(2024, 11, 3, utcHour, 30, 20, DateTimeKind.Utc).AddTicks(1_234_567);
            var local = utc.ToLocalTime();
            var boundaries = new[]
            {
                local.BeginningOfDay(), local.BeginningOfDay(0),
                local.BeginningOfWeek(), local.BeginningOfWeek(0),
                local.BeginningOfMonth(), local.BeginningOfMonth(0),
                local.BeginningOfQuarter(), local.BeginningOfQuarter(0),
                local.BeginningOfYear(), local.BeginningOfYear(0),
                local.FirstDayOfQuarter(), local.LastDayOfQuarter()
            };
            foreach (var boundary in boundaries)
            {
                boundary.TimeOfDay.Ticks.ShouldBe(0);
                boundary.Kind.ShouldBe(DateTimeKind.Local);
                var restored = boundary.AddTicks(local.Ticks - boundary.Ticks);
                restored.ToUniversalTime().Ticks.ShouldBe(utc.Ticks);
                restored.ToBinary().ShouldBe(local.ToBinary());
            }
        }
    }

    /// <summary>
    /// 同时核对钟面精度、Kind 和 UTC 时间点。
    /// </summary>
    /// <param name="actual">实际本地日期值。</param>
    /// <param name="expectedClock">预期钟面日期值。</param>
    /// <param name="expectedUtc">预期 UTC 时间点。</param>
    private static void AssertLocalBoundary(DateTime actual, DateTime expectedClock, DateTime expectedUtc)
    {
        actual.Ticks.ShouldBe(expectedClock.Ticks);
        actual.Kind.ShouldBe(DateTimeKind.Local);
        actual.ToUniversalTime().Ticks.ShouldBe(expectedUtc.Ticks);
    }
}
