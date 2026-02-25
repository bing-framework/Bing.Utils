using NodaTime;

namespace Bing.Date;

/// <summary>
/// 测试类：补充 DateTimeExtensions 的核心边界与契约行为。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.Additional")]
public class DateTimeExtensionsAdditionalContractTest
{
    /// <summary>
    /// 测试用例：AddWeeks 在正负偏移场景应按 7 天步进。
    /// </summary>
    [Theory]
    [InlineData(2, "2024-01-29")]
    [InlineData(-2, "2024-01-01")]
    [InlineData(0, "2024-01-15")]
    public void AddWeeks_OffsetWeeks_ReturnsExpectedDate(int weeks, string expected)
    {
        var source = new DateTime(2024, 1, 15);

        var result = source.AddWeeks(weeks);

        result.ShouldBe(DateTime.Parse(expected));
    }

    /// <summary>
    /// 测试用例：AddQuarters 在跨年与负偏移场景应按 3 个月步进。
    /// </summary>
    [Theory]
    [InlineData(1, "2024-05-29")]
    [InlineData(-1, "2023-11-29")]
    [InlineData(2, "2024-08-29")]
    public void AddQuarters_CrossYearAndBackward_ReturnsExpectedDate(int quarters, string expected)
    {
        var source = new DateTime(2024, 2, 29);

        var result = source.AddQuarters(quarters);

        result.ShouldBe(DateTime.Parse(expected));
    }

    /// <summary>
    /// 测试用例：AddDuration 应按 Duration 精确偏移日期时间。
    /// </summary>
    [Fact]
    public void AddDuration_WithNodaDuration_ReturnsExpectedDateTime()
    {
        var source = new DateTime(2024, 1, 15, 10, 20, 30, DateTimeKind.Utc);
        var duration = Duration.FromHours(1) + Duration.FromMinutes(2) + Duration.FromSeconds(3);

        var result = source.AddDuration(duration);

        result.ShouldBe(new DateTime(2024, 1, 15, 11, 22, 33, DateTimeKind.Utc));
    }

    /// <summary>
    /// 测试用例：GetMonthDiff 在不同日期组合下应符合当前实现规则。
    /// </summary>
    [Theory]
    [InlineData("2023-01-15", "2023-05-10", 4)]
    [InlineData("2023-01-10", "2023-05-15", 5)]
    [InlineData("2023-05-15", "2023-01-10", 5)]
    public void GetMonthDiff_DifferentDayRules_ReturnsCurrentExpected(string left, string right, int expected)
    {
        var l = DateTime.Parse(left);
        var r = DateTime.Parse(right);

        var result = l.GetMonthDiff(r);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：GetTotalMonthDiff 在含小数月差场景应返回可预期结果。
    /// </summary>
    [Fact]
    public void GetTotalMonthDiff_WithPartialMonth_ReturnsExpectedPrecision()
    {
        var left = new DateTime(2023, 1, 15);
        var right = new DateTime(2023, 5, 20);

        var result = left.GetTotalMonthDiff(right);

        result.ShouldBe(4.161290322580645, 1e-12);
    }

    /// <summary>
    /// 测试用例：IsBetween 在包含与不包含边界时应返回不同结果。
    /// </summary>
    [Fact]
    public void IsBetween_BoundaryToggle_ReturnsExpected()
    {
        var value = new DateTime(2024, 1, 1);
        var min = new DateTime(2024, 1, 1);
        var max = new DateTime(2024, 1, 31);

        value.IsBetween(min, max, includeBoundary: true).ShouldBeTrue();
        value.IsBetween(min, max, includeBoundary: false).ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：IsMorning 在边界小时应返回预期结果。
    /// </summary>
    [Theory]
    [InlineData(5, false)]
    [InlineData(6, true)]
    [InlineData(11, true)]
    [InlineData(12, false)]
    public void IsMorning_BoundaryHour_ReturnsExpected(int hour, bool expected)
    {
        var value = new DateTime(2024, 1, 1, hour, 0, 0);

        value.IsMorning().ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：IsAfternoon 在边界小时应返回预期结果。
    /// </summary>
    [Theory]
    [InlineData(11, false)]
    [InlineData(12, true)]
    [InlineData(17, true)]
    [InlineData(18, false)]
    public void IsAfternoon_BoundaryHour_ReturnsExpected(int hour, bool expected)
    {
        var value = new DateTime(2024, 1, 1, hour, 0, 0);

        value.IsAfternoon().ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：IsDusk 在边界小时应返回预期结果。
    /// </summary>
    [Theory]
    [InlineData(15, false)]
    [InlineData(16, true)]
    [InlineData(18, true)]
    [InlineData(19, false)]
    public void IsDusk_BoundaryHour_ReturnsExpected(int hour, bool expected)
    {
        var value = new DateTime(2024, 1, 1, hour, 0, 0);

        value.IsDusk().ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：IsEvening 在跨零点时段应返回预期结果。
    /// </summary>
    [Theory]
    [InlineData(23, true)]
    [InlineData(2, true)]
    [InlineData(12, false)]
    public void IsEvening_CrossMidnightWindow_ReturnsExpected(int hour, bool expected)
    {
        var value = new DateTime(2024, 1, 1, hour, 0, 0);

        value.IsEvening().ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：IsAM 与 IsPM 在 12 点边界应返回互斥结果。
    /// </summary>
    [Theory]
    [InlineData(11, true, false)]
    [InlineData(12, false, true)]
    [InlineData(0, true, false)]
    public void IsAMAndIsPM_BoundaryHour12_ReturnsExpected(int hour, bool expectedAm, bool expectedPm)
    {
        var value = new DateTime(2024, 1, 1, hour, 0, 0);

        value.IsAM().ShouldBe(expectedAm);
        value.IsPM().ShouldBe(expectedPm);
    }

    /// <summary>
    /// 测试用例：IsDateEqual 与 IsTimeEqual 应分别只比较日期与时间部分。
    /// </summary>
    [Fact]
    public void IsDateEqualAndIsTimeEqual_DifferentDateAndTimeParts_ReturnsExpected()
    {
        var left = new DateTime(2024, 1, 1, 10, 20, 30);
        var rightSameDate = new DateTime(2024, 1, 1, 8, 0, 0);
        var rightSameTime = new DateTime(2024, 8, 8, 10, 20, 30);

        left.IsDateEqual(rightSameDate).ShouldBeTrue();
        left.IsTimeEqual(rightSameDate).ShouldBeFalse();
        left.IsDateEqual(rightSameTime).ShouldBeFalse();
        left.IsTimeEqual(rightSameTime).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：Beginning/End 子日精度方法应保留 Kind 且返回正确边界值。
    /// </summary>
    [Fact]
    public void BeginningAndEnd_SubDayPrecision_ShouldKeepKindAndExpectedValue()
    {
        var source = new DateTime(2024, 1, 15, 10, 20, 30, 456, DateTimeKind.Utc);

        source.BeginningOfSecond().ShouldBe(new DateTime(2024, 1, 15, 10, 20, 30, 0, DateTimeKind.Utc));
        source.EndOfSecond().ShouldBe(new DateTime(2024, 1, 15, 10, 20, 30, 999, DateTimeKind.Utc));
        source.BeginningOfMinute().ShouldBe(new DateTime(2024, 1, 15, 10, 20, 0, 0, DateTimeKind.Utc));
        source.EndOfMinute().ShouldBe(new DateTime(2024, 1, 15, 10, 20, 59, 999, DateTimeKind.Utc));
        source.BeginningOfHour().ShouldBe(new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc));
        source.EndOfHour().ShouldBe(new DateTime(2024, 1, 15, 10, 59, 59, 999, DateTimeKind.Utc));
    }

    /// <summary>
    /// 测试用例：DaysInMonth/DaysInYear 在闰年与平年应返回正确天数。
    /// </summary>
    [Fact]
    public void DaysInMonthAndDaysInYear_LeapAndCommonYear_ReturnsExpected()
    {
        var leapFeb = new DateTime(2024, 2, 1);
        var commonFeb = new DateTime(2023, 2, 1);

        leapFeb.DaysInMonth().ShouldBe(29);
        commonFeb.DaysInMonth().ShouldBe(28);
        leapFeb.DaysInYear().ShouldBe(366);
        commonFeb.DaysInYear().ShouldBe(365);
    }

    /// <summary>
    /// 测试用例：GetQuarterEnum 对四个季度月份应映射到正确枚举值。
    /// </summary>
    [Theory]
    [InlineData(1, Quarter.Q1)]
    [InlineData(4, Quarter.Q2)]
    [InlineData(8, Quarter.Q3)]
    [InlineData(12, Quarter.Q4)]
    public void GetQuarterEnum_QuarterMonths_ReturnsExpectedEnum(int month, Quarter expected)
    {
        var value = new DateTime(2024, month, 1);

        value.GetQuarterEnum().ShouldBe(expected);
    }
}
