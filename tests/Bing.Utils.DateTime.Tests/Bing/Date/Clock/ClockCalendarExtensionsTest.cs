using System;
using Bing.Date.Clock;

namespace Bing.Date;

/// <summary>
/// 可注入时钟和业务时区的日期场景。
/// </summary>
public class ClockCalendarExtensionsTest
{
    /// <summary>
    /// 不依赖系统时区数据库的固定 UTC+8 测试时区。
    /// </summary>
    private static readonly TimeZoneInfo China = TimeZoneInfo.CreateCustomTimeZone(
        "Test+08", TimeSpan.FromHours(8), "Test+08", "Test+08");

    /// <summary>
    /// 验证日期字段和时间点按显式业务时区判断是否为今天。
    /// </summary>
    [Fact]
    public void IsToday_DateFieldsAndInstantsUseExplicitBusinessZone()
    {
        IClock clock = new FixedClock(new DateTimeOffset(2024, 2, 9, 16, 0, 0, TimeSpan.Zero));
        var utcEvening = new DateTimeOffset(2024, 2, 9, 17, 0, 0, TimeSpan.Zero);
        var previousInstant = new DateTimeOffset(2024, 2, 9, 15, 59, 59, TimeSpan.Zero);

        DateTimeFactory.Today(clock, China).ShouldBe(new DateTime(2024, 2, 10));
        DateTimeFactory.Today(clock, China).Kind.ShouldBe(DateTimeKind.Unspecified);
        new DateTime(2024, 2, 10, 0, 0, 0, DateTimeKind.Utc).IsToday(clock, China).ShouldBeTrue();
        new DateTime(2024, 2, 9).IsToday(clock, China).ShouldBeFalse();
        utcEvening.IsToday(clock, China).ShouldBeTrue();
        utcEvening.ToOffset(TimeSpan.FromHours(-5)).IsToday(clock, China).ShouldBeTrue();
        previousInstant.IsToday(clock, China).ShouldBeFalse();
        ((DateTime?)null).IsToday(clock, China).ShouldBeFalse();
        ((DateTimeOffset?)null).IsToday(clock, China).ShouldBeFalse();
        new DateTime(2000, 2, 10).ToCalculateAge(clock, China).ShouldBe(24);
    }

    /// <summary>
    /// 验证注入时钟的重载保留时间种类并遵循相对时间边界。
    /// </summary>
    [Fact]
    public void ClockOverloads_PreserveInstantKindAndRelativeBoundaries()
    {
        var instant = new DateTimeOffset(2024, 5, 1, 4, 0, 0, TimeSpan.Zero).AddTicks(1234);
        IClock clock = new FixedClock(instant);

        DateTimeFactory.UtcNow(clock).ShouldBe(instant.UtcDateTime);
        DateTimeFactory.UtcNow(clock).Kind.ShouldBe(DateTimeKind.Utc);
        DateTimeFactory.Now(clock).ShouldBe(instant.LocalDateTime);
        DateTimeFactory.Now(clock).Kind.ShouldBe(DateTimeKind.Local);
        2.Days().Before(clock).ShouldBe(instant.LocalDateTime.AddDays(-2));
        2.Days().FromNow(clock).ShouldBe(instant.LocalDateTime.AddDays(2));
        2.Days().OffsetBefore(clock).ShouldBe(instant.ToLocalTime().AddDays(-2));
        2.Days().OffsetFromNow(clock).ShouldBe(instant.ToLocalTime().AddDays(2));
        instant.IsInThePast(clock).ShouldBeFalse();
        instant.IsInTheFuture(clock).ShouldBeFalse();
        instant.AddTicks(-1).IsInThePast(clock).ShouldBeTrue();
        instant.AddTicks(1).IsInTheFuture(clock).ShouldBeTrue();
    }

    /// <summary>
    /// 验证注入时钟的重载检查参数并投影到指定时区。
    /// </summary>
    [Fact]
    public void ClockOverloads_ValidateArgumentsAndProjection()
    {
        IClock clock = new FixedClock(DateTimeOffset.MaxValue);
        Should.Throw<ArgumentNullException>(() => DateTimeFactory.Now(null)).ParamName.ShouldBe("clock");
        Should.Throw<ArgumentNullException>(() => DateTime.Today.IsToday(null, China)).ParamName.ShouldBe("clock");
        Should.Throw<ArgumentNullException>(() => ((DateTime?)null).IsToday(clock, null)).ParamName.ShouldBe("timeZone");
        Should.Throw<ArgumentOutOfRangeException>(() => DateTime.Today.IsToday(clock, China));
    }

    /// <summary>
    /// 验证 IsToday 对注入的时钟仅读取一次。
    /// </summary>
    [Fact]
    public void IsToday_ReadsClockOnce()
    {
        var clock = new CountingClock();
        new DateTime(2024, 2, 10).IsToday(clock, China).ShouldBeTrue();
        clock.ReadCount.ShouldBe(1);
    }

    /// <summary>
    /// 验证偏移日期运算跨越夏令时后保留起始偏移。
    /// </summary>
    [Fact]
    public void OffsetFromNow_KeepsTheStartingOffsetAcrossDst()
    {
        IClock clock = new FixedClock(new DateTimeOffset(2024, 3, 10, 5, 30, 0, TimeSpan.Zero));
        var start = clock.UtcNow.ToLocalTime();
        var result = 1.Days().OffsetFromNow(clock);

        result.Offset.ShouldBe(start.Offset);
        result.DateTime.ShouldBe(start.DateTime.AddDays(1));
        if (TimeZoneInfo.Local.Id == "America/New_York")
            result.Offset.ShouldNotBe(TimeZoneInfo.Local.GetUtcOffset(result.UtcDateTime));
    }

    /// <summary>
    /// 验证夏令时短日和长日的日期判断边界。
    /// </summary>
    /// <param name="clockMonth">时钟时间的月份。</param>
    /// <param name="clockDay">时钟时间的日期。</param>
    /// <param name="clockHour">时钟时间的小时。</param>
    /// <param name="clockMinute">时钟时间的分钟。</param>
    /// <param name="sameMonth">预期当日时间的月份。</param>
    /// <param name="sameDay">预期当日时间的日期。</param>
    /// <param name="sameHour">预期当日时间的小时。</param>
    /// <param name="sameMinute">预期当日时间的分钟。</param>
    /// <param name="nextMonth">预期次日时间的月份。</param>
    /// <param name="nextDay">预期次日时间的日期。</param>
    /// <param name="nextHour">预期次日时间的小时。</param>
    [Theory]
    [InlineData(3, 10, 5, 30, 3, 11, 3, 30, 3, 11, 4)]
    [InlineData(11, 3, 4, 30, 11, 4, 4, 30, 11, 4, 5)]
    public void IsToday_HandlesShortAndLongDstDays(int clockMonth, int clockDay, int clockHour, int clockMinute,
        int sameMonth, int sameDay, int sameHour, int sameMinute, int nextMonth, int nextDay, int nextHour)
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("TestDST", TimeSpan.FromHours(-5), "TestDST", "Standard", "Daylight",
            new[]
            {
                TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31),
                    TimeSpan.FromHours(1),
                    TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 10),
                    TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 3))
            });
        IClock clock = new FixedClock(new DateTimeOffset(2024, clockMonth, clockDay, clockHour, clockMinute, 0, TimeSpan.Zero));
        var same = new DateTimeOffset(2024, sameMonth, sameDay, sameHour, sameMinute, 0, TimeSpan.Zero);
        var next = new DateTimeOffset(2024, nextMonth, nextDay, nextHour, 0, 0, TimeSpan.Zero);

        same.IsToday(clock, zone).ShouldBeTrue();
        next.IsToday(clock, zone).ShouldBeFalse();
    }

    /// <summary>
    /// 记录 UTC 时间读取次数的测试时钟。
    /// </summary>
    private sealed class CountingClock : IClock
    {
        /// <summary>
        /// 获取已读取 UTC 时间的次数。
        /// </summary>
        public int ReadCount { get; private set; }

        /// <summary>
        /// 获取固定 UTC 时间并递增读取计数。
        /// </summary>
        public DateTimeOffset UtcNow
        {
            get
            {
                ReadCount++;
                return new DateTimeOffset(2024, 2, 9, 16, 0, 0, TimeSpan.Zero);
            }
        }
    }
}
