namespace Bing.Date;

/// <summary>UTC 闭区间及比较一致性的回归。</summary>
public class DateTimeRangeUtcRegressionTest
{
    private static DateTime Utc(int day) => new(2024, 1, day, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MixedOffsets_AreSortedByInstantAndHavePositiveElapsedDuration()
    {
        var first = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.FromHours(14));
        var second = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.FromHours(-10));
        var range = new DateTimeRange(second, first);
        range.TotalHours.ShouldBe(23);
        range.StartTime.ShouldBe(first.UtcDateTime);
        range.EndTime.ShouldBe(second.UtcDateTime);
        range.StartTime.Kind.ShouldBe(DateTimeKind.Utc);
        range.EndTime.Kind.ShouldBe(DateTimeKind.Utc);
        new DateTimeRange(range).ShouldBe(range);
    }

    [Fact]
    public void MixedKinds_RepresentEqualRangesAndHashCodes()
    {
        var range = new DateTimeRange(Utc(1), Utc(2));
        var local = new DateTimeRange(Utc(1).ToLocalTime(), Utc(2).ToLocalTime());
        var unspecified = new DateTimeRange(DateTime.SpecifyKind(Utc(1).ToLocalTime(), DateTimeKind.Unspecified),
            DateTime.SpecifyKind(Utc(2).ToLocalTime(), DateTimeKind.Unspecified));
        local.ShouldBe(range);
        unspecified.ShouldBe(range);
        local.GetHashCode().ShouldBe(range.GetHashCode());
        range.Contains(Utc(1).ToLocalTime()).ShouldBeTrue();
        range.Contains(Utc(2)).ShouldBeTrue();
        range.Contains(Utc(2).AddTicks(1)).ShouldBeFalse();
        range.UtcStartTime.ShouldBe(range.StartTime);
        DateTimeRange.Empty.StartTime.Kind.ShouldBe(DateTimeKind.Utc);
        DateTimeRange.Empty.EndTime.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void IntervalAlgebra_HandlesContainmentTouchingAndCustomImplementations()
    {
        var outer = new DateTimeRange(Utc(1), Utc(10));
        var inner = new DateTimeRange(Utc(3), Utc(5));
        outer.HasIntersect(inner).ShouldBeTrue();
        inner.HasIntersect(outer).ShouldBeTrue();
        outer.Intersect(inner).range.ShouldBe(inner);
        inner.Intersect(outer).range.ShouldBe(inner);
        outer.Union(inner).ShouldBe(outer);
        inner.Union(outer).ShouldBe(outer);
        var custom = new CustomRange(Utc(5).ToLocalTime(), Utc(3).ToLocalTime());
        outer.Contains(custom).ShouldBeTrue();
        inner.In(custom).ShouldBeTrue();
        outer.Intersect(custom).range.ShouldBe(inner);
        outer.Union(custom).ShouldBe(outer);
        outer.HasIntersect(Utc(10), Utc(11)).ShouldBeTrue();
        outer.Intersect(Utc(10), Utc(11)).range.TimeSpan.ShouldBe(TimeSpan.Zero);
        outer.HasIntersect(Utc(10).AddTicks(1), Utc(11)).ShouldBeFalse();
    }

    [Fact]
    public void Comparison_IsLexicographicAndAntisymmetricForOverlappingRanges()
    {
        var a = new DateTimeRange(Utc(1), Utc(10));
        var b = new DateTimeRange(Utc(1), Utc(11));
        var c = new DateTimeRange(Utc(2), Utc(3));
        a.CompareTo(b).ShouldBeLessThan(0);
        b.CompareTo(a).ShouldBeGreaterThan(0);
        b.CompareTo(c).ShouldBeLessThan(0);
        a.CompareTo(c).ShouldBeLessThan(0);
    }

    [Theory]
    [InlineData(3, 10, -5, -4, 23)]
    [InlineData(11, 3, -4, -5, 25)]
    public void DstOffsetChange_UsesElapsedHours(int month, int day, int before, int after, int hours)
    {
        var start = new DateTimeOffset(2024, month, day, 0, 0, 0, TimeSpan.FromHours(before));
        var end = new DateTimeOffset(2024, month, day + 1, 0, 0, 0, TimeSpan.FromHours(after));
        var range = new DateTimeRange(start, end);
        range.TotalHours.ShouldBe(hours);
        new DateTimeRange(start.UtcDateTime, range.TimeSpan).ShouldBe(range);
        new DateTimeRange(range.TimeSpan, end.UtcDateTime).ShouldBe(range);
    }

    private sealed class CustomRange : IDateTimeRange
    {
        public CustomRange(DateTime start, DateTime end) { StartTime = start; EndTime = end; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }
    }

    [Fact]
    public void CalendarFactories_EndAtTheLastTick()
    {
        foreach (var range in new[] { DateTimeRange.Today, DateTimeRange.Yesterday, DateTimeRange.ThisMonth, DateTimeRange.ThisYear })
        {
            range.StartTime.Kind.ShouldBe(DateTimeKind.Utc);
            range.EndTime.Kind.ShouldBe(DateTimeKind.Utc);
            (range.EndTime.Ticks % TimeSpan.TicksPerSecond).ShouldBe(TimeSpan.TicksPerSecond - 1);
        }
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void DurationConstructors_NormalizeBeforeApplyingElapsedDuration(DateTimeKind kind)
    {
        var start = DateTime.SpecifyKind(new DateTime(2024, 3, 10, 0, 0, 0), kind);
        var duration = TimeSpan.FromHours(24).Add(TimeSpan.FromTicks(1));
        var fromStart = new DateTimeRange(start, duration);
        fromStart.StartTime.Kind.ShouldBe(DateTimeKind.Utc);
        fromStart.StartTime.ShouldBe(start.ToUniversalTime());
        fromStart.EndTime.ShouldBe(start.ToUniversalTime().Add(duration));
        fromStart.TimeSpan.ShouldBe(duration);
        var end = DateTime.SpecifyKind(new DateTime(2024, 11, 4, 0, 0, 0), kind);
        var fromEnd = new DateTimeRange(duration, end);
        fromEnd.EndTime.Kind.ShouldBe(DateTimeKind.Utc);
        fromEnd.EndTime.ShouldBe(end.ToUniversalTime());
        fromEnd.StartTime.ShouldBe(end.ToUniversalTime().Subtract(duration));
        fromEnd.TimeSpan.ShouldBe(duration);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void NonSentinelEndpoints_MustNotSaturateAtUtcLimits(DateTimeKind kind)
    {
        foreach (var value in new[] { DateTime.MinValue.AddTicks(1), DateTime.MaxValue.AddTicks(-1) })
        {
            var input = DateTime.SpecifyKind(value, kind);
            var offset = TimeZoneInfo.Local.GetUtcOffset(input).Ticks;
            var utcTicks = input.Ticks - offset;
            if (utcTicks < DateTime.MinValue.Ticks || utcTicks > DateTime.MaxValue.Ticks)
            {
                Should.Throw<ArgumentException>(() => new DateTimeRange(input, input));
                Should.Throw<ArgumentException>(() => DateTimeRange.Empty.Contains(input));
            }
            else
            {
                var range = new DateTimeRange(input, input);
                range.StartTime.Ticks.ShouldBe(utcTicks);
                range.StartTime.Kind.ShouldBe(DateTimeKind.Utc);
            }
        }
    }
}
