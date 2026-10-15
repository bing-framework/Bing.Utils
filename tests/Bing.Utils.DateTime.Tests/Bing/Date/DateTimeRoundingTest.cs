namespace Bing.Date;

/// <summary>
/// 验证日期时间按任意间隔向上与向下取整。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.Rounding")]
public class DateTimeRoundingTest
{
    /// <summary>
    /// 验证向上取整返回最近的秒、分钟或小时边界。
    /// </summary>
    /// <param name="source">待取整的日期时间文本。</param>
    /// <param name="intervalSeconds">以秒表示的取整间隔。</param>
    /// <param name="expected">预期取整后的日期时间文本。</param>
    [Theory]
    [InlineData("2024-01-01 10:00:00.001", 1, "2024-01-01 10:00:01")]
    [InlineData("2024-01-01 10:00:01", 60, "2024-01-01 10:01:00")]
    [InlineData("2024-01-01 10:01:00", 3600, "2024-01-01 11:00:00")]
    public void RoundUp_StandardIntervals_ReturnsNextBoundary(string source, int intervalSeconds, string expected)
    {
        // Arrange
        var dateTime = DateTime.Parse(source);

        // Act
        var result = dateTime.RoundUp(TimeSpan.FromSeconds(intervalSeconds));

        // Assert
        result.ShouldBe(DateTime.Parse(expected));
    }

    /// <summary>
    /// 验证向下取整支持不规则间隔并保持边界值不变。
    /// </summary>
    [Fact]
    public void RoundDown_IrregularIntervalAndBoundary_ReturnsExpectedBoundary()
    {
        // Arrange
        var source = new DateTime(1, 1, 1, 0, 8, 59);
        var boundary = DateTime.MinValue.AddMinutes(7);
        var interval = TimeSpan.FromMinutes(7);

        // Act
        var rounded = source.RoundDown(interval);
        var preserved = boundary.RoundDown(interval);

        // Assert
        rounded.ShouldBe(DateTime.MinValue.AddMinutes(7));
        preserved.ShouldBe(boundary);
    }

    /// <summary>
    /// 验证取整保留原时间种类。
    /// </summary>
    /// <param name="kind">需要验证保留的时间种类。</param>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    public void RoundUpAndRoundDown_DateTimeKind_PreservesOriginalKind(DateTimeKind kind)
    {
        // Arrange
        var source = new DateTime(2024, 1, 1, 10, 1, 1, kind);

        // Act
        var roundedUp = source.RoundUp(TimeSpan.FromMinutes(5));
        var roundedDown = source.RoundDown(TimeSpan.FromMinutes(5));

        // Assert
        roundedUp.Kind.ShouldBe(kind);
        roundedDown.Kind.ShouldBe(kind);
    }

    /// <summary>
    /// 验证取整拒绝零值及负值间隔。
    /// </summary>
    /// <param name="seconds">以秒表示的无效取整间隔。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RoundUpAndRoundDown_NonPositiveInterval_ThrowsArgumentOutOfRangeException(int seconds)
    {
        // Arrange
        var source = DateTime.UtcNow;
        var interval = TimeSpan.FromSeconds(seconds);

        // Act and Assert
        Should.Throw<ArgumentOutOfRangeException>(() => source.RoundUp(interval)).ParamName.ShouldBe("interval");
        Should.Throw<ArgumentOutOfRangeException>(() => source.RoundDown(interval)).ParamName.ShouldBe("interval");
    }

    /// <summary>
    /// 验证日期最小值可向下取整且最大值向上溢出时抛出异常。
    /// </summary>
    [Fact]
    public void RoundUpAndRoundDown_DateTimeExtremes_UsesDefinedBoundaryBehavior()
    {
        // Arrange
        var interval = TimeSpan.FromDays(1);

        // Act
        var minimum = DateTime.MinValue.RoundDown(interval);

        // Assert
        minimum.ShouldBe(DateTime.MinValue);
        Should.Throw<ArgumentOutOfRangeException>(() => DateTime.MaxValue.RoundUp(interval)).ParamName.ShouldBe("interval");
    }
}
