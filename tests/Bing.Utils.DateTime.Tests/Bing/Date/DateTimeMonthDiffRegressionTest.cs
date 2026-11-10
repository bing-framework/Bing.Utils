using System;
using System.Globalization;

namespace Bing.Date;

/// <summary>
/// 验证整月差按公历年月日字段计算。
/// </summary>
public class DateTimeMonthDiffRegressionTest
{
    /// <summary>
    /// 验证整月差对相同日期、月内日期、完整月份、跨年和极值使用完整月数。
    /// </summary>
    /// <param name="left">区间一侧的日期。</param>
    /// <param name="right">区间另一侧的日期。</param>
    /// <param name="expected">不含不足整月部分的月数。</param>
    [Theory]
    [InlineData("2023-01-15", "2023-01-15", 0)]
    [InlineData("2023-01-15", "2023-01-16", 0)]
    [InlineData("2023-01-15", "2023-02-14", 0)]
    [InlineData("2023-01-15", "2023-02-15", 1)]
    [InlineData("2023-01-15", "2023-02-16", 1)]
    [InlineData("2023-12-15", "2024-01-14", 0)]
    [InlineData("2023-12-15", "2024-01-15", 1)]
    [InlineData("2024-01-31", "2024-02-28", 0)]
    [InlineData("2024-02-29", "2025-02-28", 11)]
    [InlineData("0001-01-01", "9999-12-31", 119987)]
    public void GetMonthDiff_UsesCompletedCalendarMonths_ReturnsExpected(string left, string right, int expected)
    {
        var leftDate = DateTime.ParseExact(left, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var rightDate = DateTime.ParseExact(right, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        leftDate.GetMonthDiff(rightDate).ShouldBe(expected);
        rightDate.GetMonthDiff(leftDate).ShouldBe(expected);
    }

    /// <summary>
    /// 验证完整月差忽略时间和 Kind，只读取年月日字段。
    /// </summary>
    /// <param name="kind">两个输入日期的 Kind。</param>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void GetMonthDiff_IgnoresTimeOfDayAndKind_UsesDateFields(DateTimeKind kind)
    {
        var left = new DateTime(2023, 1, 15, 23, 59, 0, kind);
        var right = new DateTime(2023, 5, 10, 0, 1, 0, kind);

        left.GetMonthDiff(right).ShouldBe(3);
        right.GetMonthDiff(left).ShouldBe(3);

        var sameDateLate = new DateTime(2023, 5, 10, 23, 59, 0, kind);
        var sameDateEarly = new DateTime(2023, 5, 10, 0, 1, 0, kind);
        sameDateLate.GetMonthDiff(sameDateEarly).ShouldBe(0);

        var sameDayNumberLate = new DateTime(2023, 1, 15, 23, 59, 0, DateTimeKind.Utc);
        var sameDayNumberEarly = new DateTime(2023, 5, 15, 0, 1, 0, DateTimeKind.Local);
        sameDayNumberLate.GetMonthDiff(sameDayNumberEarly).ShouldBe(4);
        sameDayNumberEarly.GetMonthDiff(sameDayNumberLate).ShouldBe(4);
    }
}
