using NodaTime;

namespace Bing.Date;

/// <summary>
/// 日期时间扩展的运算、时段和边界契约测试。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.Additional")]
public class DateTimeExtensionsAdditionalContractTest
{
    /// <summary>
    /// 验证周偏移按七天步进。
    /// </summary>
    /// <param name="weeks">偏移周数。</param>
    /// <param name="expected">预期日期。</param>
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
    /// 验证季度偏移按三个月步进并支持跨年。
    /// </summary>
    /// <param name="quarters">偏移季度数。</param>
    /// <param name="expected">预期日期。</param>
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
    /// 验证按 NodaTime 时长偏移日期时间。
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
    /// 验证整月差按日期顺序和日部分计算。
    /// </summary>
    /// <param name="left">第一个日期。</param>
    /// <param name="right">第二个日期。</param>
    /// <param name="expected">预期月差。</param>
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
    /// 验证包含不足整月部分的月差计算。
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
    /// 验证区间判断遵循是否包含边界的设置。
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
    /// 验证上午时段的小时边界。
    /// </summary>
    /// <param name="hour">待判断的小时。</param>
    /// <param name="expected">预期判断结果。</param>
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
    /// 验证下午时段的小时边界。
    /// </summary>
    /// <param name="hour">待判断的小时。</param>
    /// <param name="expected">预期判断结果。</param>
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
    /// 验证傍晚时段的小时边界。
    /// </summary>
    /// <param name="hour">待判断的小时。</param>
    /// <param name="expected">预期判断结果。</param>
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
    /// 验证晚间时段的小时边界。
    /// </summary>
    /// <param name="hour">待判断的小时。</param>
    /// <param name="expected">预期判断结果。</param>
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
    /// 验证午前和午后以十二点为分界。
    /// </summary>
    /// <param name="hour">待判断的小时。</param>
    /// <param name="expectedAm">预期午前判断结果。</param>
    /// <param name="expectedPm">预期午后判断结果。</param>
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
    /// 验证日期相等和时间相等分别只比较对应部分。
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
    /// 验证秒、分和小时的起止边界保留日期时间种类。
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
    /// 验证闰年和平年的月份与全年天数。
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
    /// 验证月份映射到对应季度。
    /// </summary>
    /// <param name="month">待判断的月份。</param>
    /// <param name="expected">预期季度。</param>
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
