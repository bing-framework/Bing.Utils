using System;

namespace Bing.Date;

/// <summary>
/// 验证带偏移日期时间取整时的日期边界和精度。
/// </summary>
public sealed class DateTimeOffsetRoundingBoundaryRegressionTest
{
    /// <summary>
    /// 验证最小公历日期上的合法日取整不会因中间本地午夜越界而失败。
    /// </summary>
    [Fact]
    public void RoundDay_MinimumDateWithFourteenHourOffset_ReturnsNextDay()
    {
        var source = new DateTimeOffset(1, 1, 1, 14, 0, 0, TimeSpan.FromHours(14));

        var result = source.Round(RoundTo.Day);

        ShouldRepresentSameValue(result,
            new DateTimeOffset(1, 1, 2, 0, 0, 0, TimeSpan.FromHours(14)));
    }

    /// <summary>
    /// 验证最小公历日期上的合法小时取整不会先构造不可表示的本地整点。
    /// </summary>
    [Fact]
    public void RoundHour_MinimumDateWithThirtyMinuteOffset_ReturnsNextHour()
    {
        var source = new DateTimeOffset(1, 1, 1, 0, 30, 0, TimeSpan.FromMinutes(30));

        var result = source.Round(RoundTo.Hour);

        ShouldRepresentSameValue(result,
            new DateTimeOffset(1, 1, 1, 1, 0, 0, TimeSpan.FromMinutes(30)));
    }

    /// <summary>
    /// 验证各取整精度在临界点前一 tick 和临界点处保留正确偏移与时间点。
    /// </summary>
    [Fact]
    public void Round_BoundaryTicks_PreservesExpectedInstantAndOffset()
    {
        var offset = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(45);
        var date = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Unspecified);
        var cases = new[]
        {
            (new DateTimeOffset(date.AddHours(12).AddTicks(4_999_999), offset), RoundTo.Second,
                new DateTimeOffset(date.AddHours(12), offset)),
            (new DateTimeOffset(date.AddHours(12).AddTicks(5_000_000), offset), RoundTo.Second,
                new DateTimeOffset(date.AddHours(12).AddSeconds(1), offset)),
            (new DateTimeOffset(date.AddHours(12).AddTicks(5_000_001), offset), RoundTo.Second,
                new DateTimeOffset(date.AddHours(12).AddSeconds(1), offset)),
            (new DateTimeOffset(date.AddHours(12).AddSeconds(29).AddTicks(9_999_999), offset), RoundTo.Minute,
                new DateTimeOffset(date.AddHours(12), offset)),
            (new DateTimeOffset(date.AddHours(12).AddSeconds(30), offset), RoundTo.Minute,
                new DateTimeOffset(date.AddHours(12).AddMinutes(1), offset)),
            (new DateTimeOffset(date.AddHours(12).AddSeconds(30).AddTicks(1), offset), RoundTo.Minute,
                new DateTimeOffset(date.AddHours(12).AddMinutes(1), offset)),
            (new DateTimeOffset(date.AddHours(12).AddMinutes(29).AddSeconds(59).AddTicks(9_999_999), offset), RoundTo.Hour,
                new DateTimeOffset(date.AddHours(12), offset)),
            (new DateTimeOffset(date.AddHours(12).AddMinutes(30), offset), RoundTo.Hour,
                new DateTimeOffset(date.AddHours(13), offset)),
            (new DateTimeOffset(date.AddHours(12).AddMinutes(30).AddTicks(1), offset), RoundTo.Hour,
                new DateTimeOffset(date.AddHours(13), offset)),
            (new DateTimeOffset(date.AddHours(12).AddTicks(-1), offset), RoundTo.Day,
                new DateTimeOffset(date, offset)),
            (new DateTimeOffset(date.AddHours(12), offset), RoundTo.Day,
                new DateTimeOffset(date.AddDays(1), offset)),
            (new DateTimeOffset(date.AddHours(12).AddTicks(1), offset), RoundTo.Day,
                new DateTimeOffset(date.AddDays(1), offset))
        };

        foreach (var (source, precision, expected) in cases)
        {
            var result = source.Round(precision);

            ShouldRepresentSameValue(result, expected);
        }
    }

    /// <summary>
    /// 验证最终取整结果越过支持边界时仍会抛出范围异常。
    /// </summary>
    /// <param name="precision">触发越界的取整精度。</param>
    [Theory]
    [InlineData(RoundTo.Second)]
    [InlineData(RoundTo.Minute)]
    [InlineData(RoundTo.Hour)]
    [InlineData(RoundTo.Day)]
    public void Round_MaximumUtcValue_WhenResultOverflows_Throws(RoundTo precision)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => DateTimeOffset.MaxValue.Round(precision));
    }

    /// <summary>
    /// 验证正偏移下最小 UTC 日期的向下日取整结果越界时仍会抛出范围异常。
    /// </summary>
    [Fact]
    public void RoundDay_MinimumUtcDateWithPositiveOffset_WhenResultOverflows_Throws()
    {
        var source = new DateTimeOffset(1, 1, 1, 8, 0, 0, TimeSpan.FromHours(8));

        Should.Throw<ArgumentOutOfRangeException>(() => source.Round(RoundTo.Day));
    }

    /// <summary>
    /// 验证负偏移造成的 UTC 上界越界仍被拒绝。
    /// </summary>
    [Fact]
    public void RoundHour_MaximumUtcDayWithNegativeFourteenHourOffset_Throws()
    {
        var source = new DateTimeOffset(9999, 12, 31, 9, 30, 0, TimeSpan.FromHours(-14));

        Should.Throw<ArgumentOutOfRangeException>(() => source.Round(RoundTo.Hour));
    }

    /// <summary>
    /// 验证不支持的取整精度会被拒绝。
    /// </summary>
    [Fact]
    public void Round_InvalidPrecision_Throws()
    {
        var source = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

        Should.Throw<ArgumentOutOfRangeException>(() => source.Round((RoundTo)int.MaxValue));
    }

    /// <summary>
    /// 验证实际时间点、钟面 ticks 和 UTC 偏移都与预期值一致。
    /// </summary>
    /// <param name="actual">取整得到的值。</param>
    /// <param name="expected">预期的值。</param>
    private static void ShouldRepresentSameValue(DateTimeOffset actual, DateTimeOffset expected)
    {
        actual.Ticks.ShouldBe(expected.Ticks);
        actual.UtcDateTime.Ticks.ShouldBe(expected.UtcDateTime.Ticks);
        actual.Offset.ShouldBe(expected.Offset);
    }
}
