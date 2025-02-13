using Bing.Helpers;

namespace Bing.Date;

/// <summary>
/// 日期时间导航测试 - 获取
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.Get")]
public class DateTimeNavigationGetTest
{
    /// <summary>
    /// 测试 - 获取指定日期所属季度
    /// </summary>
    [Theory]
    [InlineData("2023-01-01", 1)]
    [InlineData("2023-02-01", 1)]
    [InlineData("2023-03-01", 1)]
    [InlineData("2023-04-01", 2)]
    [InlineData("2023-05-01", 2)]
    [InlineData("2023-06-01", 2)]
    [InlineData("2023-07-01", 3)]
    [InlineData("2023-08-01", 3)]
    [InlineData("2023-09-01", 3)]
    [InlineData("2023-10-01", 4)]
    [InlineData("2023-11-01", 4)]
    [InlineData("2023-12-01", 4)]
    public void Test_QuarterOfMonth(string input, int result)
    {
        var dt = Conv.ToDate(input);
        dt.GetQuarter().ShouldBe(result);
    }

    /// <summary>
    /// 测试 - 获取指定日期是所在年份的第几周
    /// </summary>
    [Theory]
    [InlineData("2016-01-01", 1)]
    [InlineData("2016-01-03", 2)]
    [InlineData("2023-01-01", 1)]
    [InlineData("2023-01-03", 1)]
    [InlineData("2024-01-01", 1)]
    [InlineData("2024-01-03", 1)]
    public void Test_GetWeekOfYear(string input, int result)
    {
        var dt = Conv.ToDate(input);
        dt.GetWeekOfYear().ShouldBe(result);
    }
}