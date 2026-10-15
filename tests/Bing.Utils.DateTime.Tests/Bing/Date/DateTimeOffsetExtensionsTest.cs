using System.Collections.Generic;
using System.Globalization;

namespace Bing.Date;

/// <summary>
/// 验证带偏移时间点的日期边界、导航与分量设置。
/// </summary>
[Trait("DateTimeUT", "DateTimeOffsetExtensions")]
public class DateTimeOffsetExtensionsTest
{
    /// <summary>
    /// 验证日边界计算保留原日期与偏移量。
    /// </summary>
    /// <param name="dto">覆盖闰日、月末、夏令时切换日期及偏移边界的测试时间点。</param>
    [Theory]
    [MemberData(nameof(GetDayBoundaryCases))]
    public void BeginningOfDayAndEndOfDay_ShouldKeepDateAndOffset(DateTimeOffset dto)
    {
        var beginning = dto.BeginningOfDay();
        var end = dto.EndOfDay();

        beginning.Year.ShouldBe(dto.Year);
        beginning.Month.ShouldBe(dto.Month);
        beginning.Day.ShouldBe(dto.Day);
        beginning.Hour.ShouldBe(0);
        beginning.Minute.ShouldBe(0);
        beginning.Second.ShouldBe(0);
        beginning.Offset.ShouldBe(dto.Offset);

        end.Year.ShouldBe(dto.Year);
        end.Month.ShouldBe(dto.Month);
        end.Day.ShouldBe(dto.Day);
        end.Hour.ShouldBe(23);
        end.Minute.ShouldBe(59);
        end.Second.ShouldBe(59);
        end.Millisecond.ShouldBe(999);
        end.Offset.ShouldBe(dto.Offset);
    }

    /// <summary>
    /// 验证月末日期匹配对应月份的实际天数。
    /// </summary>
    /// <param name="dto">用于计算月末的测试时间点。</param>
    /// <param name="expectedDay">预期月份的最后一天数值。</param>
    [Theory]
    [MemberData(nameof(GetLastDayOfMonthCases))]
    public void LastDayOfMonth_BoundaryDates_ReturnsExpected(DateTimeOffset dto, int expectedDay)
    {
        dto.LastDayOfMonth().Day.ShouldBe(expectedDay);
    }

    /// <summary>
    /// 验证年、季度及月的首尾日期。
    /// </summary>
    [Fact]
    public void FirstAndLastDayHelpers_ShouldReturnExpectedDate()
    {
        var dto = new DateTimeOffset(2024, 2, 20, 12, 30, 45, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 2, 20)));

        dto.FirstDayOfYear().Date.ShouldBe(new DateTime(2024, 1, 1));
        dto.LastDayOfYear().Date.ShouldBe(new DateTime(2024, 12, 31));
        dto.FirstDayOfQuarter().Date.ShouldBe(new DateTime(2024, 1, 1));
        dto.LastDayOfQuarter().Date.ShouldBe(new DateTime(2024, 3, 31));
        dto.FirstDayOfMonth().Date.ShouldBe(new DateTime(2024, 2, 1));
        dto.LastDayOfMonth().Date.ShouldBe(new DateTime(2024, 2, 29));
    }

    /// <summary>
    /// 验证每周首日遵循指定区域性。
    /// </summary>
    /// <param name="cultureName">确定每周首日的区域性名称。</param>
    /// <param name="expected">预期周首日的日期文本。</param>
    [Theory]
    [InlineData("en-US", "2023-12-31")]
    [InlineData("zh-CN", "2024-01-01")]
    public void FirstDayOfWeek_WithCulture_ShouldUseCultureRule(string cultureName, string expected)
    {
        var dto = new DateTimeOffset(2024, 1, 3, 12, 30, 45, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 1, 3)));

        var result = dto.FirstDayOfWeek(new CultureInfo(cultureName));

        result.Date.ShouldBe(DateTime.ParseExact(expected, "yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// 验证周末日期比周首日期晚六天。
    /// </summary>
    [Fact]
    public void LastDayOfWeek_ShouldBeSixDaysAfterFirstDayOfWeek()
    {
        var dto = CreateLocalDateTimeOffset();
        var first = dto.FirstDayOfWeek();
        var last = dto.LastDayOfWeek();

        last.Date.ShouldBe(first.Date.AddDays(6));
    }

    /// <summary>
    /// 验证前后一周分别偏移七天。
    /// </summary>
    [Fact]
    public void WeekBeforeAndWeekAfter_ShouldShiftSevenDays()
    {
        var dto = CreateLocalDateTimeOffset();

        dto.WeekBefore().ShouldBe(dto.AddDays(-7));
        dto.WeekAfter().ShouldBe(dto.AddDays(7));
    }

    /// <summary>
    /// 验证年、季度、月、周及日的前后导航结果。
    /// </summary>
    [Fact]
    public void NavigationHelpers_ShouldMoveToExpectedDate()
    {
        var dto = new DateTimeOffset(2024, 1, 15, 12, 30, 45, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 1, 15)));

        dto.NextYear().Date.ShouldBe(new DateTime(2025, 1, 15));
        dto.PreviousYear().Date.ShouldBe(new DateTime(2023, 1, 15));
        dto.NextQuarter().Date.ShouldBe(new DateTime(2024, 4, 15));
        dto.PreviousQuarter().Date.ShouldBe(new DateTime(2023, 10, 15));
        dto.NextMonth().Date.ShouldBe(new DateTime(2024, 2, 15));
        dto.PreviousMonth().Date.ShouldBe(new DateTime(2023, 12, 15));
        dto.NextWeek().Date.ShouldBe(new DateTime(2024, 1, 22));
        dto.PreviousWeek().Date.ShouldBe(new DateTime(2024, 1, 8));
        dto.NextDay().Date.ShouldBe(new DateTime(2024, 1, 16));
        dto.PreviousDay().Date.ShouldBe(new DateTime(2024, 1, 14));
        dto.Tomorrow().Date.ShouldBe(new DateTime(2024, 1, 16));
        dto.Yesterday().Date.ShouldBe(new DateTime(2024, 1, 14));
    }

    /// <summary>
    /// 验证指定星期的前后日期导航。
    /// </summary>
    [Fact]
    public void NextDayOfWeekAndPreviousDayOfWeek_ShouldReturnExpectedDate()
    {
        var dto = new DateTimeOffset(2024, 1, 1, 12, 30, 45, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 1, 1)));

        dto.NextDayOfWeek(DayOfWeek.Friday).Date.ShouldBe(new DateTime(2024, 1, 5));
        dto.PreviousDayOfWeek(DayOfWeek.Friday).Date.ShouldBe(new DateTime(2023, 12, 29));
    }

    /// <summary>
    /// 验证时间与日期分量设置方法更新指定分量。
    /// </summary>
    [Fact]
    public void AtAndSetHelpers_ShouldUpdateExpectedPart()
    {
        var dto = CreateLocalDateTimeOffset();

        dto.At(1, 2).Hour.ShouldBe(1);
        dto.At(1, 2).Minute.ShouldBe(2);
        dto.At(1, 2, 3).Second.ShouldBe(3);
        dto.At(1, 2, 3, 4).Millisecond.ShouldBe(4);
        dto.AtMidnight().Hour.ShouldBe(0);
        dto.AtNoon().Hour.ShouldBe(12);
        dto.SetHour(5).Hour.ShouldBe(5);
        dto.SetMinute(6).Minute.ShouldBe(6);
        dto.SetSecond(7).Second.ShouldBe(7);
        dto.SetMillisecond(8).Millisecond.ShouldBe(8);
        dto.SetYear(2023).Year.ShouldBe(2023);
        dto.SetMonth(1).Month.ShouldBe(1);
        dto.SetDay(10).Day.ShouldBe(10);
    }

    /// <summary>
    /// 验证重新指定日期时保留时间及偏移量。
    /// </summary>
    [Fact]
    public void On_ShouldSetDateAndKeepTime()
    {
        var dto = CreateLocalDateTimeOffset();

        var result = dto.On(2023, 1, 2);

        result.Year.ShouldBe(2023);
        result.Month.ShouldBe(1);
        result.Day.ShouldBe(2);
        result.Hour.ShouldBe(dto.Hour);
        result.Minute.ShouldBe(dto.Minute);
        result.Second.ShouldBe(dto.Second);
        result.Offset.ShouldBe(dto.Offset);
    }

    /// <summary>
    /// 验证小时设置拒绝超出范围的数值。
    /// </summary>
    /// <param name="invalidHour">用于验证范围检查的无效小时值。</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void SetTime_InvalidHour_ShouldThrowArgumentOutOfRangeException(int invalidHour)
    {
        var dto = CreateLocalDateTimeOffset();

        var exception = Should.Throw<ArgumentOutOfRangeException>(() => dto.SetTime(invalidHour));

        exception.Message.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 验证目标月份不包含当前日时月份设置抛出异常。
    /// </summary>
    /// <param name="targetMonth">不包含三十一日的目标月份。</param>
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void SetMonth_WhenTargetMonthHasNoCurrentDay_ShouldThrowArgumentOutOfRangeException(int targetMonth)
    {
        var dto = new DateTimeOffset(2024, 1, 31, 12, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 1, 31)));

        Should.Throw<ArgumentOutOfRangeException>(() => dto.SetMonth(targetMonth));
    }

    /// <summary>
    /// 验证闰日无法直接设置为平年的对应日期。
    /// </summary>
    [Fact]
    public void SetYear_LeapDayToCommonYear_ShouldThrowArgumentOutOfRangeException()
    {
        var dto = new DateTimeOffset(2024, 2, 29, 12, 0, 0, TimeSpan.FromHours(8));

        Should.Throw<ArgumentOutOfRangeException>(() => dto.SetYear(2023));
    }

    /// <summary>
    /// 提供日边界计算的日期与偏移测试数据。
    /// </summary>
    /// <returns>覆盖闰日、月末、夏令时切换日期及偏移边界的数据行。</returns>
    public static IEnumerable<object[]> GetDayBoundaryCases()
    {
        yield return new object[] { new DateTimeOffset(2024, 2, 29, 12, 0, 0, TimeSpan.FromHours(8)) };   // leap day
        yield return new object[] { new DateTimeOffset(2024, 1, 31, 12, 0, 0, TimeSpan.FromHours(-5)) };  // month end
        yield return new object[] { new DateTimeOffset(2024, 3, 10, 12, 0, 0, TimeSpan.FromHours(-5)) };  // DST start date
        yield return new object[] { new DateTimeOffset(2024, 11, 3, 12, 0, 0, TimeSpan.FromHours(-4)) };  // DST end date
        yield return new object[] { new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.FromHours(14)) };  // max offset
        yield return new object[] { new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.FromHours(-12)) }; // min offset
    }

    /// <summary>
    /// 提供不同月份的月末日期测试数据。
    /// </summary>
    /// <returns>包含输入时间点与预期月末日数的数据行。</returns>
    public static IEnumerable<object[]> GetLastDayOfMonthCases()
    {
        yield return new object[] { new DateTimeOffset(2024, 2, 10, 12, 0, 0, TimeSpan.FromHours(8)), 29 };
        yield return new object[] { new DateTimeOffset(2023, 2, 10, 12, 0, 0, TimeSpan.FromHours(8)), 28 };
        yield return new object[] { new DateTimeOffset(2024, 4, 10, 12, 0, 0, TimeSpan.FromHours(8)), 30 };
        yield return new object[] { new DateTimeOffset(2024, 1, 10, 12, 0, 0, TimeSpan.FromHours(8)), 31 };
    }

    /// <summary>
    /// 创建使用本地时区偏移的固定测试时间点。
    /// </summary>
    /// <returns>使用本地偏移的 2024 年 2 月 28 日测试时间点。</returns>
    private static DateTimeOffset CreateLocalDateTimeOffset()
    {
        var localOffset = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 2, 28));
        return new DateTimeOffset(2024, 2, 28, 12, 30, 45, 123, localOffset);
    }
}
