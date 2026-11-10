namespace Bing.Date.TimeZones;
/// <summary>
/// 验证显式 UTC 转换和区间显示投影不会改变时间点。
/// </summary>
public class TimeZoneExplicitConversionTest
{
    /// <summary>
    /// 验证实际时区转换与历史 Kind 标签操作不同，且保留小数 tick。
    /// </summary>
    [Fact]
    public void ToUtc_ConvertsWallTimeInsteadOfRelabeling()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Fixed8", TimeSpan.FromHours(8), "Fixed8", "Fixed8");
        var local = new DateTime(2024, 1, 1, 12, 0, 0).AddTicks(9876);
        var converted = TimeZoneConversion.ToUtc(local, zone);
        converted.Ticks.ShouldBe(local.AddHours(-8).Ticks);
        converted.Kind.ShouldBe(DateTimeKind.Utc);
        local.ToUtc().Ticks.ShouldBe(local.Ticks);
        Should.Throw<ArgumentException>(() => TimeZoneConversion.ToUtc(converted, zone));
        Should.Throw<ArgumentException>(() => TimeZoneConversion.ToUtc(DateTime.SpecifyKind(local, DateTimeKind.Local), zone));
        Should.Throw<ArgumentNullException>(() => TimeZoneConversion.ToUtc(local, null));
        Should.Throw<ArgumentOutOfRangeException>(() => TimeZoneConversion.ToUtc(DateTime.MinValue, zone));
    }
    /// <summary>
    /// 验证两个端点各自采用 DST 偏移，区间时长和原对象不变。
    /// </summary>
    [Fact]
    public void RangeProjection_UsesEachEndpointOffsetAcrossDst()
    {
        var zone = CreateZone();
        var start = TimeZoneConversion.ToUtc(new DateTime(2024, 3, 10), zone);
        var end = TimeZoneConversion.ToUtc(new DateTime(2024, 3, 11), zone);
        var range = new DateTimeRange(start, end);
        var projected = TimeZoneConversion.ToTimeZone(range, zone);
        projected.StartTime.Offset.ShouldBe(TimeSpan.FromHours(-5));
        projected.EndTime.Offset.ShouldBe(TimeSpan.FromHours(-4));
        (projected.EndTime - projected.StartTime).TotalHours.ShouldBe(23);
        projected.StartTime.UtcDateTime.ShouldBe(start);
        projected.EndTime.UtcDateTime.ShouldBe(end);
        range.StartTime.Kind.ShouldBe(DateTimeKind.Utc);
        range.StartTime.ShouldBe(start);
        Should.Throw<ArgumentNullException>(() => TimeZoneConversion.ToTimeZone((IDateTimeRange)null, zone));
        Should.Throw<ArgumentNullException>(() => TimeZoneConversion.ToTimeZone(range, null));
        Should.Throw<ArgumentOutOfRangeException>(() => TimeZoneConversion.ToTimeZone(new DateTimeRange(), zone));
        var reversed = TimeZoneConversion.ToTimeZone(new ReversedRange(end, start), zone);
        reversed.StartTime.ShouldBe(projected.StartTime);
        reversed.EndTime.ShouldBe(projected.EndTime);
    }
    /// <summary>
    /// 验证缺失钟面时间拒绝，重复钟面时间必须显式消歧。
    /// </summary>
    [Fact]
    public void ToUtc_DstGapAndFold_FollowsExplicitPolicy()
    {
        var zone = CreateZone();
        Should.Throw<ArgumentException>(() => TimeZoneConversion.ToUtc(new DateTime(2024, 3, 10, 2, 30, 0), zone));
        var fold = new DateTime(2024, 11, 3, 1, 30, 0);
        Should.Throw<ArgumentException>(() => TimeZoneConversion.ToUtc(fold, zone));
        var early = TimeZoneConversion.ToUtc(fold, zone, AmbiguousTimeResolution.Earlier);
        var late = TimeZoneConversion.ToUtc(fold, zone, AmbiguousTimeResolution.Later);
        early.ShouldBe(new DateTime(2024, 11, 3, 5, 30, 0, DateTimeKind.Utc));
        (late - early).ShouldBe(TimeSpan.FromHours(1));
        Should.Throw<ArgumentOutOfRangeException>(() => TimeZoneConversion.ToUtc(fold, zone, (AmbiguousTimeResolution)99));
    }
    /// <summary>
    /// 创建不依赖操作系统时区数据库的测试时区。
    /// </summary>
    /// <returns>2024 年有 DST 的测试时区。</returns>
    private static TimeZoneInfo CreateZone() => TimeZoneInfo.CreateCustomTimeZone("TestDST", TimeSpan.FromHours(-5),
        "TestDST", "Standard", "Daylight", new[]
        {
            TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31),
                TimeSpan.FromHours(1),
                TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 10),
                TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 3))
        });
    /// <summary>
    /// 提供用于验证自定义端点排序的区间。
    /// </summary>
    /// <param name="start">区间起始时间。</param>
    /// <param name="end">区间结束时间。</param>
    private sealed class ReversedRange(DateTime start, DateTime end) : IDateTimeRange
    {
        /// <inheritdoc />
        public DateTime StartTime => start;
        /// <inheritdoc />
        public DateTime EndTime => end;
    }
}
