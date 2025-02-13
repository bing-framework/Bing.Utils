using Bing.Date.DateUtils;

namespace Bing.Date;

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
}