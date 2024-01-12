using Bing.Helpers;

namespace Bing.Date;

/// <summary>
/// 日期时间判断测试
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.Is")]
public class DateTimeIsTest
{
    /// <summary>
    /// 测试 - 是否清晨
    /// </summary>
    [Theory]
    [InlineData("2024-01-12 00:00:00", true)]
    [InlineData("2024-01-12 02:00:00", true)]
    [InlineData("2024-01-12 04:00:00", true)]
    [InlineData("2024-01-12 05:59:59", true)]
    [InlineData("2024-01-12 06:00:00", false)]
    [InlineData("2024-01-12 08:00:00", false)]
    [InlineData("2024-01-12 23:59:59", false)]
    public void Test_IsEarlyMorning(string input, bool result)
    {
        var dt = Conv.ToDate(input);
        dt.IsEarlyMorning().ShouldBe(result);
    }

    /// <summary>
    /// 测试 - 是否闰年
    /// </summary>
    [Theory]
    [InlineData("2004-01-01", true)]
    [InlineData("2008-01-01", true)]
    [InlineData("2012-01-01", true)]
    [InlineData("2016-01-01", true)]
    [InlineData("2020-01-01", true)]
    [InlineData("2021-01-01", false)]
    [InlineData("2022-01-01", false)]
    [InlineData("2023-01-01", false)]
    [InlineData("2024-01-01", true)]
    public void Test_IsLeapYear(string input, bool result)
    {
        var dt = Conv.ToDate(input);
        dt.IsLeapYear().ShouldBe(result);
    }
}