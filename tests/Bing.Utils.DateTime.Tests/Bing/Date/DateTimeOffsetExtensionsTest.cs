using System.Globalization;
using System.Collections.Generic;

namespace Bing.Date;

/// <summary>
/// Test class: verifies DateTimeOffset extension contracts for navigation, boundaries and exceptions.
/// </summary>
[Trait("DateTimeUT", "DateTimeOffsetExtensions")]
public class DateTimeOffsetExtensionsTest
{
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

    [Theory]
    [MemberData(nameof(GetLastDayOfMonthCases))]
    public void LastDayOfMonth_BoundaryDates_ReturnsExpected(DateTimeOffset dto, int expectedDay)
    {
        dto.LastDayOfMonth().Day.ShouldBe(expectedDay);
    }

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

    [Theory]
    [InlineData("en-US", "2023-12-31")]
    [InlineData("zh-CN", "2024-01-01")]
    public void FirstDayOfWeek_WithCulture_ShouldUseCultureRule(string cultureName, string expected)
    {
        var dto = new DateTimeOffset(2024, 1, 3, 12, 30, 45, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 1, 3)));

        var result = dto.FirstDayOfWeek(new CultureInfo(cultureName));

        result.Date.ShouldBe(DateTime.ParseExact(expected, "yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void LastDayOfWeek_ShouldBeSixDaysAfterFirstDayOfWeek()
    {
        var dto = CreateLocalDateTimeOffset();
        var first = dto.FirstDayOfWeek();
        var last = dto.LastDayOfWeek();

        last.Date.ShouldBe(first.Date.AddDays(6));
    }

    [Fact]
    public void WeekBeforeAndWeekAfter_ShouldShiftSevenDays()
    {
        var dto = CreateLocalDateTimeOffset();

        dto.WeekBefore().ShouldBe(dto.AddDays(-7));
        dto.WeekAfter().ShouldBe(dto.AddDays(7));
    }

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

    [Fact]
    public void NextDayOfWeekAndPreviousDayOfWeek_ShouldReturnExpectedDate()
    {
        var dto = new DateTimeOffset(2024, 1, 1, 12, 30, 45, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 1, 1)));

        dto.NextDayOfWeek(DayOfWeek.Friday).Date.ShouldBe(new DateTime(2024, 1, 5));
        dto.PreviousDayOfWeek(DayOfWeek.Friday).Date.ShouldBe(new DateTime(2023, 12, 29));
    }

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

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void SetTime_InvalidHour_ShouldThrowArgumentOutOfRangeException(int invalidHour)
    {
        var dto = CreateLocalDateTimeOffset();

        var exception = Should.Throw<ArgumentOutOfRangeException>(() => dto.SetTime(invalidHour));

        exception.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void SetMonth_WhenTargetMonthHasNoCurrentDay_ShouldThrowArgumentOutOfRangeException(int targetMonth)
    {
        var dto = new DateTimeOffset(2024, 1, 31, 12, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 1, 31)));

        Should.Throw<ArgumentOutOfRangeException>(() => dto.SetMonth(targetMonth));
    }

    [Fact]
    public void SetYear_LeapDayToCommonYear_ShouldThrowArgumentOutOfRangeException()
    {
        var dto = new DateTimeOffset(2024, 2, 29, 12, 0, 0, TimeSpan.FromHours(8));

        Should.Throw<ArgumentOutOfRangeException>(() => dto.SetYear(2023));
    }

    public static IEnumerable<object[]> GetDayBoundaryCases()
    {
        yield return new object[] { new DateTimeOffset(2024, 2, 29, 12, 0, 0, TimeSpan.FromHours(8)) };   // leap day
        yield return new object[] { new DateTimeOffset(2024, 1, 31, 12, 0, 0, TimeSpan.FromHours(-5)) };  // month end
        yield return new object[] { new DateTimeOffset(2024, 3, 10, 12, 0, 0, TimeSpan.FromHours(-5)) };  // DST start date
        yield return new object[] { new DateTimeOffset(2024, 11, 3, 12, 0, 0, TimeSpan.FromHours(-4)) };  // DST end date
        yield return new object[] { new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.FromHours(14)) };  // max offset
        yield return new object[] { new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.FromHours(-12)) }; // min offset
    }

    public static IEnumerable<object[]> GetLastDayOfMonthCases()
    {
        yield return new object[] { new DateTimeOffset(2024, 2, 10, 12, 0, 0, TimeSpan.FromHours(8)), 29 };
        yield return new object[] { new DateTimeOffset(2023, 2, 10, 12, 0, 0, TimeSpan.FromHours(8)), 28 };
        yield return new object[] { new DateTimeOffset(2024, 4, 10, 12, 0, 0, TimeSpan.FromHours(8)), 30 };
        yield return new object[] { new DateTimeOffset(2024, 1, 10, 12, 0, 0, TimeSpan.FromHours(8)), 31 };
    }

    private static DateTimeOffset CreateLocalDateTimeOffset()
    {
        var localOffset = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 2, 28));
        return new DateTimeOffset(2024, 2, 28, 12, 30, 45, 123, localOffset);
    }
}
