using Bing.Helpers;

namespace Bing.Date;

/// <summary>
/// 回归验证日期导航、边界精度、分量设置及时区偏移。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.PrecisionNavigation")]
public class DatePrecisionNavigationRegressionTest
{
    private const long SubMillisecondTicks = 4567;
    private static readonly TimeSpan NonSystemOffset = TimeSpan.FromMinutes(330);

    /// <summary>
    /// 验证无星期参数的月初工厂返回当月一日。
    /// </summary>
    [Fact]
    public void CreateFirstDayOfMonth_WithoutWeekday_ReturnsDayOne()
    {
        DateTimeFactory.CreateFirstDayOfMonth(2024, 6).ShouldBe(new DateTime(2024, 6, 1));
        DateTimeFactory.CreateFirstDayOfMonth(2024, 6, DayOfWeek.Sunday).ShouldBe(new DateTime(2024, 6, 2));
    }

    /// <summary>
    /// 验证月末、季末和年末的星期导航最多向前六日。
    /// </summary>
    [Fact]
    public void LastDayOfPeriods_AllWeekdaysStayWithinPeriodEnd()
    {
        var source = new DateTime(2024, 1, 15);
        var monthEnd = new DateTime(2024, 1, 31);
        var quarterEnd = new DateTime(2024, 3, 31);
        var yearEnd = new DateTime(2024, 12, 31);

        foreach (DayOfWeek dayOfWeek in System.Enum.GetValues(typeof(DayOfWeek)))
        {
            AssertLastWeekday(source.LastDayOfMonth(dayOfWeek), monthEnd, dayOfWeek);
            AssertLastWeekday(source.LastDayOfQuarter(dayOfWeek), quarterEnd, dayOfWeek);
            AssertLastWeekday(source.LastDayOfYear(dayOfWeek), yearEnd, dayOfWeek);
        }

        source.LastDayOfMonth(DayOfWeek.Monday).ShouldBe(new DateTime(2024, 1, 29));
    }

    /// <summary>
    /// 验证各 DateTime 分量设置保留未修改的秒下 ticks 和 Kind。
    /// </summary>
    [Fact]
    public void DateTimeSetters_PreserveSubMillisecondTicksAndKind()
    {
        foreach (var kind in new[] { DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local })
        {
            var source = new DateTime(2024, 6, 15, 10, 20, 30, 123, kind).AddTicks(SubMillisecondTicks);
            var cases = new (DateTime Actual, DateTime Expected)[]
            {
                (source.SetHour(11), new DateTime(2024, 6, 15, 11, 20, 30, 123, kind).AddTicks(SubMillisecondTicks)),
                (source.SetMinute(21), new DateTime(2024, 6, 15, 10, 21, 30, 123, kind).AddTicks(SubMillisecondTicks)),
                (source.SetSecond(31), new DateTime(2024, 6, 15, 10, 20, 31, 123, kind).AddTicks(SubMillisecondTicks)),
                (source.SetYear(2025), new DateTime(2025, 6, 15, 10, 20, 30, 123, kind).AddTicks(SubMillisecondTicks)),
                (source.SetMonth(7), new DateTime(2024, 7, 15, 10, 20, 30, 123, kind).AddTicks(SubMillisecondTicks)),
                (source.SetDay(16), new DateTime(2024, 6, 16, 10, 20, 30, 123, kind).AddTicks(SubMillisecondTicks)),
                (source.SetDate(2025), new DateTime(2025, 6, 15, 10, 20, 30, 123, kind).AddTicks(SubMillisecondTicks)),
                (source.SetDate(2025, 7), new DateTime(2025, 7, 15, 10, 20, 30, 123, kind).AddTicks(SubMillisecondTicks)),
                (source.SetDate(2025, 7, 16), new DateTime(2025, 7, 16, 10, 20, 30, 123, kind).AddTicks(SubMillisecondTicks)),
                (source.SetTime(11), new DateTime(2024, 6, 15, 11, 20, 30, 123, kind).AddTicks(SubMillisecondTicks)),
                (source.SetTime(11, 21), new DateTime(2024, 6, 15, 11, 21, 30, 123, kind).AddTicks(SubMillisecondTicks)),
                (source.SetTime(11, 21, 31), new DateTime(2024, 6, 15, 11, 21, 31, 123, kind).AddTicks(SubMillisecondTicks)),
            };

            foreach (var (actual, expected) in cases)
                AssertDateTime(actual, expected);

            AssertDateTime(
                source.SetMillisecond(654),
                new DateTime(2024, 6, 15, 10, 20, 30, 654, kind).AddTicks(SubMillisecondTicks));
            AssertDateTime(
                source.SetTime(11, 21, 31, 654),
                new DateTime(2024, 6, 15, 11, 21, 31, 654, kind).AddTicks(SubMillisecondTicks));
            AssertDateTime(source.SetKind(DateTimeKind.Utc), DateTime.SpecifyKind(source, DateTimeKind.Utc));
            AssertDateTime(source.ToUtc(), DateTime.SpecifyKind(source, DateTimeKind.Utc));
        }
    }

    /// <summary>
    /// 验证 DateTimeOffset 分量设置保留秒下 ticks 和原始固定偏移。
    /// </summary>
    [Fact]
    public void DateTimeOffsetSetters_PreserveSubMillisecondTicksAndOffset()
    {
        var source = CreateOffsetDateTime(2024, 6, 15, 10, 20, 30, 123);
        var cases = new (DateTimeOffset Actual, DateTimeOffset Expected)[]
        {
            (source.SetHour(11), CreateOffsetDateTime(2024, 6, 15, 11, 20, 30, 123)),
            (source.SetMinute(21), CreateOffsetDateTime(2024, 6, 15, 10, 21, 30, 123)),
            (source.SetSecond(31), CreateOffsetDateTime(2024, 6, 15, 10, 20, 31, 123)),
            (source.SetYear(2025), CreateOffsetDateTime(2025, 6, 15, 10, 20, 30, 123)),
            (source.SetMonth(7), CreateOffsetDateTime(2024, 7, 15, 10, 20, 30, 123)),
            (source.SetDay(16), CreateOffsetDateTime(2024, 6, 16, 10, 20, 30, 123)),
            (source.SetDate(2025), CreateOffsetDateTime(2025, 6, 15, 10, 20, 30, 123)),
            (source.SetDate(2025, 7), CreateOffsetDateTime(2025, 7, 15, 10, 20, 30, 123)),
            (source.SetDate(2025, 7, 16), CreateOffsetDateTime(2025, 7, 16, 10, 20, 30, 123)),
            (source.SetTime(11), CreateOffsetDateTime(2024, 6, 15, 11, 20, 30, 123)),
            (source.SetTime(11, 21), CreateOffsetDateTime(2024, 6, 15, 11, 21, 30, 123)),
            (source.SetTime(11, 21, 31), CreateOffsetDateTime(2024, 6, 15, 11, 21, 31, 123)),
        };

        foreach (var (actual, expected) in cases)
            AssertDateTimeOffset(actual, expected);

        AssertDateTimeOffset(source.SetMillisecond(654), CreateOffsetDateTime(2024, 6, 15, 10, 20, 30, 654));
        AssertDateTimeOffset(source.SetTime(11, 21, 31, 654), CreateOffsetDateTime(2024, 6, 15, 11, 21, 31, 654));
    }

    /// <summary>
    /// 验证清零型时间入口清除被截断 ticks，并保留日期 Kind。
    /// </summary>
    [Fact]
    public void DateTimeBeginningHelpers_ClearLowerTicksAndPreserveKind()
    {
        foreach (var kind in new[] { DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local })
        {
            var source = new DateTime(2024, 6, 15, 10, 20, 30, 123, kind).AddTicks(SubMillisecondTicks);
            var cases = new (DateTime Actual, DateTime Expected)[]
            {
                (source.AtMidnight(), new DateTime(2024, 6, 15, 0, 0, 0, kind)),
                (source.AtNoon(), new DateTime(2024, 6, 15, 12, 0, 0, kind)),
                (source.BeginningOfSecond(), new DateTime(2024, 6, 15, 10, 20, 30, kind)),
                (source.BeginningOfMinute(), new DateTime(2024, 6, 15, 10, 20, 0, kind)),
                (source.BeginningOfHour(), new DateTime(2024, 6, 15, 10, 0, 0, kind)),
                (source.BeginningOfDay(), new DateTime(2024, 6, 15, 0, 0, 0, kind)),
                (source.BeginningOfMonth(), new DateTime(2024, 6, 1, 0, 0, 0, kind)),
                (source.BeginningOfQuarter(), new DateTime(2024, 4, 1, 0, 0, 0, kind)),
                (source.BeginningOfYear(), new DateTime(2024, 1, 1, 0, 0, 0, kind)),
            };

            foreach (var (actual, expected) in cases)
                AssertDateTime(actual, expected);

            source.FirstDayOfQuarter().Kind.ShouldBe(kind);
            source.LastDayOfQuarter().Kind.ShouldBe(kind);
        }
    }

    /// <summary>
    /// 验证 DateTimeOffset 清零型入口移除亚毫秒 ticks 并保留偏移。
    /// </summary>
    [Fact]
    public void DateTimeOffsetBeginningHelpers_ClearLowerTicksAndPreserveOffset()
    {
        var source = CreateOffsetDateTime(2024, 6, 15, 10, 20, 30, 123);
        var cases = new (DateTimeOffset Actual, DateTimeOffset Expected)[]
        {
            (source.AtMidnight(), new DateTimeOffset(2024, 6, 15, 0, 0, 0, NonSystemOffset)),
            (source.AtNoon(), new DateTimeOffset(2024, 6, 15, 12, 0, 0, NonSystemOffset)),
            (source.BeginningOfDay(), new DateTimeOffset(2024, 6, 15, 0, 0, 0, NonSystemOffset)),
        };

        foreach (var (actual, expected) in cases)
            AssertDateTimeOffset(actual, expected);
    }

    /// <summary>
    /// 验证 DateTime 周期终点返回最后一个 tick，且 MaxValue 不越界。
    /// </summary>
    [Fact]
    public void DateTimeEndHelpers_ReturnLastTickIncludingMaxValue()
    {
        var source = new DateTime(2024, 6, 15, 10, 20, 30, 123, DateTimeKind.Utc).AddTicks(SubMillisecondTicks);
        var endOfSecond = new DateTime(2024, 6, 15, 10, 20, 30, DateTimeKind.Utc).AddTicks(TimeSpan.TicksPerSecond - 1);
        var cases = new (DateTime Actual, DateTime Expected)[]
        {
            (source.EndOfSecond(), endOfSecond),
            (source.EndOfMinute(), new DateTime(2024, 6, 15, 10, 20, 59, DateTimeKind.Utc).AddTicks(TimeSpan.TicksPerSecond - 1)),
            (source.EndOfHour(), new DateTime(2024, 6, 15, 10, 59, 59, DateTimeKind.Utc).AddTicks(TimeSpan.TicksPerSecond - 1)),
            (source.EndOfDay(), new DateTime(2024, 6, 15, 23, 59, 59, DateTimeKind.Utc).AddTicks(TimeSpan.TicksPerSecond - 1)),
            (source.EndOfMonth(), new DateTime(2024, 6, 30, 23, 59, 59, DateTimeKind.Utc).AddTicks(TimeSpan.TicksPerSecond - 1)),
            (source.EndOfQuarter(), new DateTime(2024, 6, 30, 23, 59, 59, DateTimeKind.Utc).AddTicks(TimeSpan.TicksPerSecond - 1)),
            (source.EndOfYear(), new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(TimeSpan.TicksPerSecond - 1)),
        };

        foreach (var (actual, expected) in cases)
            AssertDateTime(actual, expected);

        var maximum = DateTime.MaxValue;
        maximum.EndOfSecond().ShouldBe(maximum);
        maximum.EndOfMinute().ShouldBe(maximum);
        maximum.EndOfHour().ShouldBe(maximum);
        maximum.EndOfDay().ShouldBe(maximum);
        maximum.EndOfMonth().ShouldBe(maximum);
        maximum.EndOfQuarter().ShouldBe(maximum);
        maximum.EndOfYear().ShouldBe(maximum);
    }

    /// <summary>
    /// 验证 DateTimeOffset 日终保留固定偏移，并正确处理 ±14 小时边界。
    /// </summary>
    [Fact]
    public void DateTimeOffsetEndOfDay_PreservesOffsetAndLastTickAtLimits()
    {
        foreach (var offset in new[] { NonSystemOffset, TimeSpan.FromHours(14), TimeSpan.FromHours(-14) })
        {
            var source = new DateTimeOffset(2024, 6, 15, 10, 20, 30, 123, offset).AddTicks(SubMillisecondTicks);
            var expected = new DateTimeOffset(2024, 6, 15, 23, 59, 59, 999, offset).AddTicks(9999);
            AssertDateTimeOffset(source.EndOfDay(), expected);
        }

        DateTimeOffset.MinValue.BeginningOfDay().ShouldBe(DateTimeOffset.MinValue);
        DateTimeOffset.MaxValue.EndOfDay().ShouldBe(DateTimeOffset.MaxValue);
    }

    /// <summary>
    /// 验证周末终点遵循受控文化，并在最大日期保留最后一个 tick 和 Kind。
    /// </summary>
    [Fact]
    public void DateTimeEndOfWeek_UsesConfiguredCultureAndCapsAtMaximum()
    {
        var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            var culture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
            culture.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Monday;
            System.Globalization.CultureInfo.CurrentCulture = culture;

            foreach (var kind in new[] { DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local })
            {
                var source = new DateTime(2024, 6, 15, 12, 34, 56, 789, kind);
                var expected = new DateTime(2024, 6, 16, 23, 59, 59, 999, kind).AddTicks(9999);
                AssertDateTime(source.EndOfWeek(), expected);

                var maximum = DateTime.SpecifyKind(DateTime.MaxValue, kind);
                AssertDateTime(maximum.EndOfWeek(), maximum);
            }
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previousCulture;
        }
    }

    /// <summary>
    /// 验证最大 UTC 瞬间对应的负偏移日期无法扩展到本地日终。
    /// </summary>
    [Fact]
    public void DateTimeOffsetEndOfDay_ThrowsWhenLocalDayEndExceedsUtcMaximum()
    {
        var localAtUtcMaximum = new DateTime(9999, 12, 31, 9, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999);
        var source = new DateTimeOffset(localAtUtcMaximum, TimeSpan.FromHours(-14));

        source.UtcDateTime.Ticks.ShouldBe(DateTime.MaxValue.Ticks);
        Should.Throw<ArgumentOutOfRangeException>(() => source.EndOfDay());
    }

    /// <summary>
    /// 验证 DateTimeOffset 日历导航保留钟面时间、Offset 和亚毫秒精度。
    /// </summary>
    [Fact]
    public void DateTimeOffsetNavigation_PreservesFixedOffsetAndSubMillisecondTicks()
    {
        var source = CreateOffsetDateTime(2024, 6, 15, 12, 34, 56, 123);
        var cases = new (DateTimeOffset Actual, DateTimeOffset Expected)[]
        {
            (source.NextYear(), CreateOffsetDateTime(2025, 6, 15, 12, 34, 56, 123)),
            (source.PreviousYear(), CreateOffsetDateTime(2023, 6, 15, 12, 34, 56, 123)),
            (source.NextQuarter(), CreateOffsetDateTime(2024, 9, 15, 12, 34, 56, 123)),
            (source.PreviousQuarter(), CreateOffsetDateTime(2024, 3, 15, 12, 34, 56, 123)),
            (source.NextMonth(), CreateOffsetDateTime(2024, 7, 15, 12, 34, 56, 123)),
            (source.PreviousMonth(), CreateOffsetDateTime(2024, 5, 15, 12, 34, 56, 123)),
            (source.NextWeek(), CreateOffsetDateTime(2024, 6, 22, 12, 34, 56, 123)),
            (source.PreviousWeek(), CreateOffsetDateTime(2024, 6, 8, 12, 34, 56, 123)),
            (source.NextDay(), CreateOffsetDateTime(2024, 6, 16, 12, 34, 56, 123)),
            (source.PreviousDay(), CreateOffsetDateTime(2024, 6, 14, 12, 34, 56, 123)),
            (source.NextDayOfWeek(DayOfWeek.Monday), CreateOffsetDateTime(2024, 6, 17, 12, 34, 56, 123)),
            (source.PreviousDayOfWeek(DayOfWeek.Monday), CreateOffsetDateTime(2024, 6, 10, 12, 34, 56, 123)),
        };

        foreach (var (actual, expected) in cases)
            AssertDateTimeOffset(actual, expected);
    }

    /// <summary>
    /// 验证日期闭区间包含精确起点，并保留上下界的时间分量。
    /// </summary>
    [Fact]
    public void IsDateBetweenWithBoundary_IncludesStartAndPreservesBoundaryTimes()
    {
        var min = new DateTime(2024, 6, 15, 8, 30, 0).AddTicks(1234567);
        var max = new DateTime(2024, 6, 16, 9, 45, 0).AddTicks(7654321);
        var exclusiveEnd = max.AddDays(1);

        min.IsDateBetweenWithBoundary(min, max).ShouldBeTrue();
        min.AddTicks(-1).IsDateBetweenWithBoundary(min, max).ShouldBeFalse();
        exclusiveEnd.AddTicks(-1).IsDateBetweenWithBoundary(min, max).ShouldBeTrue();
        exclusiveEnd.IsDateBetweenWithBoundary(min, max).ShouldBeFalse();
        new DateTime(2024, 6, 16, 9, 44, 59, 999).IsDateBetweenWithBoundary(min, max).ShouldBeTrue();
    }

    /// <summary>
    /// 验证可空边界、逆序边界和最大日期的区间结果。
    /// </summary>
    [Fact]
    public void IsDateBetweenWithBoundary_NullableReversedAndMaximumValues()
    {
        var value = new DateTime(2024, 6, 15, 12, 0, 0);
        var min = new DateTime(2024, 6, 15, 12, 0, 0);
        var max = new DateTime(2024, 6, 15, 12, 0, 0);

        value.IsDateBetweenWithBoundary((DateTime?)null, (DateTime?)null).ShouldBeTrue();
        value.IsDateBetweenWithBoundary(min, (DateTime?)null).ShouldBeTrue();
        value.AddTicks(-1).IsDateBetweenWithBoundary(min, (DateTime?)null).ShouldBeFalse();
        value.IsDateBetweenWithBoundary((DateTime?)null, max).ShouldBeTrue();
        max.AddDays(1).IsDateBetweenWithBoundary((DateTime?)null, max).ShouldBeFalse();
        value.IsDateBetweenWithBoundary(new DateTime(2024, 6, 16), new DateTime(2024, 6, 14)).ShouldBeFalse();
        DateTime.MaxValue.IsDateBetweenWithBoundary(DateTime.MaxValue, DateTime.MaxValue).ShouldBeTrue();
        DateTime.MaxValue.IsDateBetweenWithBoundary((DateTime?)null, DateTime.MaxValue).ShouldBeTrue();
    }

    /// <summary>
    /// 验证纪元跨度按 Kind 解释并保留全部 ticks。
    /// </summary>
    [Fact]
    public void ToEpochTimeSpan_UsesUtcInstantAndTreatsUnspecifiedAsUtc()
    {
        var utc = new DateTime(2024, 6, 15, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234567);
        var local = utc.ToLocalTime();
        var unspecified = DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);

        local.ToEpochTimeSpan().ShouldBe(utc.ToEpochTimeSpan());
        unspecified.ToEpochTimeSpan().ShouldBe(utc.ToEpochTimeSpan());
        utc.ToEpochTimeSpan().Ticks.ShouldBe(utc.Ticks - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks);
    }

    private static void AssertLastWeekday(DateTime actual, DateTime periodEnd, DayOfWeek expectedWeekday)
    {
        actual.DayOfWeek.ShouldBe(expectedWeekday);
        var daysBeforeEnd = (periodEnd - actual.Date).Days;
        daysBeforeEnd.ShouldBeInRange(0, 6);
    }

    private static void AssertDateTime(DateTime actual, DateTime expected)
    {
        actual.Ticks.ShouldBe(expected.Ticks);
        actual.Kind.ShouldBe(expected.Kind);
    }

    private static DateTimeOffset CreateOffsetDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond) =>
        new DateTimeOffset(year, month, day, hour, minute, second, millisecond, NonSystemOffset).AddTicks(SubMillisecondTicks);

    private static void AssertDateTimeOffset(DateTimeOffset actual, DateTimeOffset expected)
    {
        actual.Ticks.ShouldBe(expected.Ticks);
        actual.Offset.ShouldBe(expected.Offset);
        actual.DateTime.Kind.ShouldBe(DateTimeKind.Unspecified);
    }

    [Fact]
    public void LastDayOfWeek_MaximumYear_KeepsRepresentableDateAndPrecision()
    {
        var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            var culture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
            culture.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Monday;
            System.Globalization.CultureInfo.CurrentCulture = culture;
            var max = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            AssertDateTime(max.LastDayOfWeek(culture), max);
            var before = new DateTime(9999, 12, 27, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234567);
            AssertDateTime(before.LastDayOfWeek(culture), new DateTime(9999, 12, 31, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234567));
            AssertDateTimeOffset(DateTimeOffset.MaxValue.LastDayOfWeek(), DateTimeOffset.MaxValue);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previousCulture;
        }
    }
}
