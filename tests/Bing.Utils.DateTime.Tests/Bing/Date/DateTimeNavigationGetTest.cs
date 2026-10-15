using Bing.Helpers;
namespace Bing.Date;
/// <summary>
/// 验证日期所属季度及年内周序号。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.Get")]
public class DateTimeNavigationGetTest
{
    /// <summary>
    /// 验证各月份返回对应季度序号。
    /// </summary>
    /// <param name="input">待计算季度的日期文本。</param>
    /// <param name="result">预期季度序号。</param>
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
    /// 验证跨年附近日期的年内周序号。
    /// </summary>
    /// <param name="input">待计算周序号的日期文本。</param>
    /// <param name="result">预期年内周序号。</param>
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
