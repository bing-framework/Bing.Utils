namespace Bing.Date;
/// <summary>
/// 验证带偏移时间点的季度末导航。
/// </summary>
/// <remarks>
/// 覆盖日期范围端点、闰年和固定偏移边界。
/// </remarks>
[Trait("DateTimeUT", "DateTimeOffsetExtensions.QuarterBoundary")]
public class DateTimeOffsetQuarterBoundaryTest
{
    /// <summary>
    /// 验证季度末计算返回正确日期并保留时刻和偏移。
    /// </summary>
    /// <remarks>
    /// 覆盖各季度及合法偏移。
    /// </remarks>
    /// <param name="year">输入日期的年份。</param>
    /// <param name="month">输入日期的月份。</param>
    /// <param name="day">输入日期的日。</param>
    /// <param name="hour">输入时间的小时。</param>
    /// <param name="minute">输入时间的分钟。</param>
    /// <param name="second">输入时间的秒。</param>
    /// <param name="millisecond">输入时间的毫秒。</param>
    /// <param name="offsetHours">输入日期时间的 UTC 偏移小时数。</param>
    /// <param name="expectedYear">预期季度末日期的年份。</param>
    /// <param name="expectedMonth">预期季度末日期的月份。</param>
    /// <param name="expectedDay">预期季度末日期的日。</param>
    [Theory]
    [InlineData(1, 2, 1, 0, 0, 0, 0, 14, 1, 3, 31)]
    [InlineData(2024, 2, 29, 12, 34, 56, 123, 1, 2024, 3, 31)]
    [InlineData(2024, 4, 15, 8, 9, 10, 11, 14, 2024, 6, 30)]
    [InlineData(2024, 7, 1, 20, 21, 22, 999, 0, 2024, 9, 30)]
    [InlineData(2024, 10, 31, 23, 59, 58, 321, -14, 2024, 12, 31)]
    [InlineData(9999, 12, 1, 0, 0, 0, 0, 14, 9999, 12, 31)]
    public void LastDayOfQuarter_RepresentableResult_PreservesTimeAndOffset(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int millisecond,
        int offsetHours,
        int expectedYear,
        int expectedMonth,
        int expectedDay)
    {
        var offset = TimeSpan.FromHours(offsetHours);
        var source = new DateTimeOffset(year, month, day, hour, minute, second, millisecond, offset)
            .AddTicks(6789);
        var expected = new DateTimeOffset(expectedYear, expectedMonth, expectedDay, hour, minute, second,
            millisecond, offset).AddTicks(6789);

        var result = source.LastDayOfQuarter();

        result.ShouldBe(expected);
        result.Ticks.ShouldBe(expected.Ticks);
        result.UtcDateTime.Ticks.ShouldBe(expected.UtcDateTime.Ticks);
        result.Offset.ShouldBe(offset);
    }
    /// <summary>
    /// 验证季度末超出时间范围时拒绝计算。
    /// </summary>
    /// <remarks>
    /// 起始值合法，只有季度末的 UTC 时间越界。
    /// </remarks>
    [Fact]
    public void LastDayOfQuarter_UnrepresentableFinalUtcTime_Throws()
    {
        var source = new DateTimeOffset(9999, 10, 1, 23, 0, 0, TimeSpan.FromHours(-14));

        Should.Throw<ArgumentOutOfRangeException>(() => source.LastDayOfQuarter());
    }
}
