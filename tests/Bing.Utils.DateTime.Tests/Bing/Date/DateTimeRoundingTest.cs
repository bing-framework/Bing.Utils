namespace Bing.Date;

/// <summary>
/// <see cref="DateTimeExtensions"/> 任意间隔取整单元测试
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.Rounding")]
public class DateTimeRoundingTest
{
    /// <summary>
    /// 测试目的：向上取整应返回最近的秒、分钟和小时边界。
    /// </summary>
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
    /// 测试目的：向下取整应支持不规则间隔并保持边界值不变。
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
    /// 测试目的：取整应保留 UTC 和 Local 的 Kind。
    /// </summary>
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
    /// 测试目的：零和负间隔应抛出参数范围异常。
    /// </summary>
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
    /// 测试目的：最小值可向下取整，最大值无法向上取整时应避免溢出。
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