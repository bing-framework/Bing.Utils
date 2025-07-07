using NodaTime;
using System.Linq;

namespace Bing.Date.DateUtils;

/// <summary>
/// 星期计算测试
/// </summary>
[Trait("DateTimeUT", "DayOfWeek.Calc")]
public class DayOfWeekCalcTest
{
    [Theory]
    [InlineData(DayOfWeek.Monday, 1, DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Monday, 5, DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Monday, 8, DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Monday, -1, DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday, -5, DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Monday, -8, DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday, 0, DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Monday, 7, DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Sunday, 1, DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Sunday, 5, DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Sunday, 8, DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Sunday, -1, DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday, -5, DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Sunday, -8, DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday, 0, DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Sunday, 7, DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Saturday, 1, DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Saturday, 5, DayOfWeek.Thursday)]
    [InlineData(DayOfWeek.Saturday, 8, DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Saturday, -1, DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Saturday, -5, DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Saturday, -8, DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Saturday, 0, DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Saturday, 7, DayOfWeek.Saturday)]
    public void Test_AddDays(DayOfWeek source, int days, DayOfWeek expected)
    {
        var result = DayOfWeekCalc.AddDays(source, days);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - DaysBetween - 计算相同星期几间隔天数为0
    /// </summary>
    [Fact]
    public void DaysBetween_SameDayOfWeek_ReturnsZero()
    {
        // Arrange
        var day = DayOfWeek.Monday;

        // Act
        var result = DayOfWeekCalc.DaysBetween(day, day);

        // Assert
        result.ShouldBe(0);
    }

    /// <summary>
    /// 测试 - DaysBetween - 计算常规星期几间隔
    /// </summary>
    [Theory]
    [InlineData(DayOfWeek.Monday, DayOfWeek.Wednesday, 2)]
    [InlineData(DayOfWeek.Wednesday, DayOfWeek.Friday, 2)]
    [InlineData(DayOfWeek.Monday, DayOfWeek.Sunday, 6)]
    public void DaysBetween_NormalDayOfWeek_ReturnsCorrectDays(DayOfWeek start, DayOfWeek end, int expectedDays)
    {
        // Act
        var result = DayOfWeekCalc.DaysBetween(start, end);

        // Assert
        result.ShouldBe(expectedDays);
    }

    /// <summary>
    /// 测试 - DaysBetween - 计算跨周的星期几间隔
    /// </summary>
    [Theory]
    [InlineData(DayOfWeek.Wednesday, DayOfWeek.Monday, 5)]
    [InlineData(DayOfWeek.Sunday, DayOfWeek.Saturday, 6)]
    [InlineData(DayOfWeek.Sunday, DayOfWeek.Monday, 1)]
    public void DaysBetween_CrossWeekDayOfWeek_ReturnsCorrectDays(DayOfWeek start, DayOfWeek end, int expectedDays)
    {
        // Act
        var result = DayOfWeekCalc.DaysBetween(start, end);

        // Assert
        result.ShouldBe(expectedDays);
    }

    /// <summary>
    /// 测试 - DaysBetween - 计算ISO星期几间隔
    /// </summary>
    [Theory]
    [InlineData(IsoDayOfWeek.Monday, IsoDayOfWeek.Wednesday, 2)]
    [InlineData(IsoDayOfWeek.Saturday, IsoDayOfWeek.Monday, 2)]
    public void DaysBetween_IsoDayOfWeek_ReturnsCorrectDays(IsoDayOfWeek start, IsoDayOfWeek end, int expectedDays)
    {
        // Act
        var result = DayOfWeekCalc.DaysBetween(start, end);

        // Assert
        result.ShouldBe(expectedDays);
    }

    /// <summary>
    /// 测试 - DaysBetween - 当ISO星期几为None时返回0
    /// </summary>
    [Theory]
    [InlineData(IsoDayOfWeek.None, IsoDayOfWeek.Monday)]
    [InlineData(IsoDayOfWeek.Monday, IsoDayOfWeek.None)]
    [InlineData(IsoDayOfWeek.None, IsoDayOfWeek.None)]
    public void DaysBetween_IsoDayOfWeekNone_ReturnsZero(IsoDayOfWeek start, IsoDayOfWeek end)
    {
        // Act
        var result = DayOfWeekCalc.DaysBetween(start, end);

        // Assert
        result.ShouldBe(0);
    }

    /// <summary>
    /// 测试 - TryDaysBetween - 常规星期几计算成功
    /// </summary>
    [Fact]
    public void TryDaysBetween_NormalDayOfWeek_ReturnsTrue()
    {
        // Arrange
        var start = DayOfWeek.Monday;
        var end = DayOfWeek.Thursday;

        // Act
        bool success = DayOfWeekCalc.TryDaysBetween(start, end, out int days);

        // Assert
        success.ShouldBeTrue();
        days.ShouldBe(3);
    }

    /// <summary>
    /// 测试 - TryDaysBetween - 常规ISO星期几计算成功
    /// </summary>
    [Fact]
    public void TryDaysBetween_NormalIsoDayOfWeek_ReturnsTrue()
    {
        // Arrange
        var start = IsoDayOfWeek.Monday;
        var end = IsoDayOfWeek.Thursday;

        // Act
        bool success = DayOfWeekCalc.TryDaysBetween(start, end, out int days);

        // Assert
        success.ShouldBeTrue();
        days.ShouldBe(3);
    }

    /// <summary>
    /// 测试 - TryDaysBetween - ISO星期几为None时计算失败
    /// </summary>
    [Theory]
    [InlineData(IsoDayOfWeek.None, IsoDayOfWeek.Monday)]
    [InlineData(IsoDayOfWeek.Monday, IsoDayOfWeek.None)]
    [InlineData(IsoDayOfWeek.None, IsoDayOfWeek.None)]
    public void TryDaysBetween_IsoDayOfWeekNone_ReturnsFalse(IsoDayOfWeek start, IsoDayOfWeek end)
    {
        // Act
        bool success = DayOfWeekCalc.TryDaysBetween(start, end, out int days);

        // Assert
        success.ShouldBeFalse();
        days.ShouldBe(0);
    }

    /// <summary>
    /// 测试 - AddDays - 添加正数天数
    /// </summary>
    [Theory]
    [InlineData(DayOfWeek.Monday, 2, DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Friday, 3, DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Saturday, 8, DayOfWeek.Sunday)]
    public void AddDays_PositiveDays_ReturnsCorrectDayOfWeek(DayOfWeek start, int daysToAdd, DayOfWeek expected)
    {
        // Act
        var result = DayOfWeekCalc.AddDays(start, daysToAdd);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - AddDays - 添加负数天数
    /// </summary>
    [Theory]
    [InlineData(DayOfWeek.Wednesday, -2, DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Monday, -1, DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Thursday, -9, DayOfWeek.Tuesday)]
    public void AddDays_NegativeDays_ReturnsCorrectDayOfWeek(DayOfWeek start, int daysToAdd, DayOfWeek expected)
    {
        // Act
        var result = DayOfWeekCalc.AddDays(start, daysToAdd);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - GetDaysBetween - 获取相同星期几
    /// </summary>
    [Fact]
    public void GetDaysBetween_SameDayOfWeek_ReturnsSingleDay()
    {
        // Arrange
        var day = DayOfWeek.Monday;

        // Act
        var result = DayOfWeekCalc.GetDaysBetween(day, day).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContain(day);
    }

    /// <summary>
    /// 测试 - GetDaysBetween - 获取不包含边界的星期几
    /// </summary>
    [Fact]
    public void GetDaysBetween_WithoutBoundary_ReturnsOnlyMiddleDays()
    {
        // Arrange
        var start = DayOfWeek.Monday;
        var end = DayOfWeek.Friday;

        // Act
        var result = DayOfWeekCalc.GetDaysBetween(start, end, false).ToList();

        // Assert
        result.Count.ShouldBe(3);
        result.ShouldContain(DayOfWeek.Tuesday);
        result.ShouldContain(DayOfWeek.Wednesday);
        result.ShouldContain(DayOfWeek.Thursday);
        result.ShouldNotContain(start);
        result.ShouldNotContain(end);
    }

    /// <summary>
    /// 测试 - GetDaysBetween - 获取包含边界的星期几
    /// </summary>
    [Fact]
    public void GetDaysBetween_WithBoundary_ReturnsAllDays()
    {
        // Arrange
        var start = DayOfWeek.Sunday;
        var end = DayOfWeek.Wednesday;

        // Act
        var result = DayOfWeekCalc.GetDaysBetween(start, end).ToList();

        // Assert
        result.Count.ShouldBe(4);
        result.ShouldContain(DayOfWeek.Sunday);
        result.ShouldContain(DayOfWeek.Monday);
        result.ShouldContain(DayOfWeek.Tuesday);
        result.ShouldContain(DayOfWeek.Wednesday);
    }
}