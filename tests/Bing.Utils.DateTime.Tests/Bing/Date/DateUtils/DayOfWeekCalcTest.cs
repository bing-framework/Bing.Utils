using System.Linq;
using NodaTime;
namespace Bing.Date.DateUtils;
/// <summary>
/// 星期偏移、循环间隔和区间枚举测试。
/// </summary>
[Trait("DateTimeUT", "DayOfWeek.Calc")]
public class DayOfWeekCalcTest
{
    /// <summary>
    /// 验证星期按正负天数循环偏移。
    /// </summary>
    /// <param name="source">起始星期。</param>
    /// <param name="days">偏移天数。</param>
    /// <param name="expected">预期星期。</param>
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
    /// 验证相同星期的间隔为零。
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
    /// 验证按正向星期顺序计算间隔天数。
    /// </summary>
    /// <param name="start">起始星期。</param>
    /// <param name="end">结束星期。</param>
    /// <param name="expectedDays">预期间隔天数。</param>
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
    /// 验证跨周计算正向间隔天数。
    /// </summary>
    /// <param name="start">起始星期。</param>
    /// <param name="end">结束星期。</param>
    /// <param name="expectedDays">预期间隔天数。</param>
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
    /// 验证 ISO 星期的正向间隔天数。
    /// </summary>
    /// <param name="start">起始星期。</param>
    /// <param name="end">结束星期。</param>
    /// <param name="expectedDays">预期间隔天数。</param>
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
    /// 验证包含未指定 ISO 星期的间隔为零。
    /// </summary>
    /// <param name="start">起始 ISO 星期。</param>
    /// <param name="end">结束 ISO 星期。</param>
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
    /// 验证普通星期的间隔计算成功。
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
    /// 验证 ISO 星期的间隔计算成功。
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
    /// 验证包含未指定 ISO 星期时间隔计算失败。
    /// </summary>
    /// <param name="start">起始 ISO 星期。</param>
    /// <param name="end">结束 ISO 星期。</param>
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
    /// 验证正数天数偏移后的星期。
    /// </summary>
    /// <param name="start">起始星期。</param>
    /// <param name="daysToAdd">偏移天数。</param>
    /// <param name="expected">预期星期。</param>
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
    /// 验证负数天数偏移后的星期。
    /// </summary>
    /// <param name="start">起始星期。</param>
    /// <param name="daysToAdd">偏移天数。</param>
    /// <param name="expected">预期星期。</param>
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
    /// 验证相同起止星期的区间只包含该星期。
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
    /// 验证不含边界的星期区间只枚举中间日期。
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
    /// 验证包含边界的星期区间枚举起止及中间日期。
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
