using System.Globalization;
namespace Bing.Date;
/// <summary>
/// 验证日期时间偏移、边界及相邻日期导航。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.Navigation")]
public class DateTimeNavigationTest
{
    /// <summary>
    /// 验证日期偏移按指定单位计算。
    /// </summary>
    /// <param name="style">日期偏移单位。</param>
    /// <param name="offset">以指定日期单位计算的偏移量。</param>
    /// <param name="expected">预期偏移后的日期时间文本。</param>
    [Theory]
    [InlineData(DateOffsetStyles.Day, 1, "2024-01-16 10:20:30")]
    [InlineData(DateOffsetStyles.Week, 1, "2024-01-22 10:20:30")]
    [InlineData(DateOffsetStyles.Month, 1, "2024-02-15 10:20:30")]
    [InlineData(DateOffsetStyles.Quarters, 1, "2024-04-15 10:20:30")]
    [InlineData(DateOffsetStyles.Year, 1, "2025-01-15 10:20:30")]
    public void OffsetBy_DateOffsetStyles_UsesExpectedUnit(DateOffsetStyles style, int offset, string expected)
    {
        var date = new DateTime(2024, 1, 15, 10, 20, 30);
        var result = date.OffsetBy(offset, style);
        result.ShouldBe(DateTime.Parse(expected));
    }
    /// <summary>
    /// 验证未定义日期偏移单位当前按天处理。
    /// </summary>
    [Fact]
    public void OffsetBy_UnknownDateOffsetStyle_DefaultsToDay()
    {
        var date = new DateTime(2024, 1, 15, 10, 20, 30);
        var result = date.OffsetBy(2, (DateOffsetStyles)999);
        result.ShouldBe(new DateTime(2024, 1, 17, 10, 20, 30));
    }
    /// <summary>
    /// 验证时间偏移按指定单位计算。
    /// </summary>
    /// <param name="style">时间偏移单位。</param>
    /// <param name="offset">以指定时间单位计算的偏移量。</param>
    /// <param name="expected">预期偏移后的日期时间文本。</param>
    [Theory]
    [InlineData(TimeOffsetStyles.Millisecond, 1, "2024-01-15 10:20:30.124")]
    [InlineData(TimeOffsetStyles.Second, 1, "2024-01-15 10:20:31.123")]
    [InlineData(TimeOffsetStyles.Minute, 1, "2024-01-15 10:21:30.123")]
    [InlineData(TimeOffsetStyles.Hour, 1, "2024-01-15 11:20:30.123")]
    public void OffsetBy_TimeOffsetStyles_UsesExpectedUnit(TimeOffsetStyles style, int offset, string expected)
    {
        var date = new DateTime(2024, 1, 15, 10, 20, 30, 123);
        var result = date.OffsetBy(offset, style);
        result.ShouldBe(DateTime.Parse(expected));
    }
    /// <summary>
    /// 验证日边界计算保留日期与时间种类。
    /// </summary>
    [Fact]
    public void BeginningAndEndOfDay_PreserveDateAndKind()
    {
        var date = new DateTime(2024, 3, 12, 8, 9, 10, 123, DateTimeKind.Utc);
        var begin = date.BeginningOfDay();
        var end = date.EndOfDay();
        begin.ShouldBe(new DateTime(2024, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc));
        end.ShouldBe(new DateTime(2024, 3, 12, 23, 59, 59, 999, DateTimeKind.Utc));
    }
    /// <summary>
    /// 验证日边界按给定小时偏移进行算术调整。
    /// </summary>
    [Fact]
    public void BeginningAndEndOfDay_WithTimeZoneOffset_AddsOffsetHours()
    {
        var date = new DateTime(2024, 3, 12, 8, 9, 10, 123, DateTimeKind.Unspecified);
        var begin = date.BeginningOfDay(8);
        var end = date.EndOfDay(-5);
        begin.ShouldBe(new DateTime(2024, 3, 12, 8, 0, 0, 0, DateTimeKind.Unspecified));
        end.ShouldBe(new DateTime(2024, 3, 12, 18, 59, 59, 999, DateTimeKind.Unspecified));
    }
    /// <summary>
    /// 验证周边界遵循指定区域性的每周首日。
    /// </summary>
    [Fact]
    public void FirstAndLastDayOfWeek_WithCulture_UseCultureFirstDay()
    {
        var date = new DateTime(2024, 1, 3);
        var usCulture = new CultureInfo("en-US");
        var cnCulture = new CultureInfo("zh-CN");
        var usFirst = date.FirstDayOfWeek(usCulture);
        var usLast = date.LastDayOfWeek(usCulture);
        var cnFirst = date.FirstDayOfWeek(cnCulture);
        var cnLast = date.LastDayOfWeek(cnCulture);
        usFirst.ShouldBe(new DateTime(2023, 12, 31));
        usLast.ShouldBe(new DateTime(2024, 1, 6));
        cnFirst.ShouldBe(new DateTime(2024, 1, 1));
        cnLast.ShouldBe(new DateTime(2024, 1, 7));
    }
    /// <summary>
    /// 验证月边界与季度边界包含闰年二月的正确日期。
    /// </summary>
    [Fact]
    public void FirstAndLastDayOfMonthAndQuarter_ReturnExpectedDates()
    {
        var date = new DateTime(2024, 2, 20);
        date.FirstDayOfMonth().ShouldBe(new DateTime(2024, 2, 1));
        date.LastDayOfMonth().ShouldBe(new DateTime(2024, 2, 29));
        date.FirstDayOfQuarter().ShouldBe(new DateTime(2024, 1, 1));
        date.LastDayOfQuarter().ShouldBe(new DateTime(2024, 3, 31));
    }
    /// <summary>
    /// 验证年、季度、月、周及日的前后导航结果。
    /// </summary>
    [Fact]
    public void NextAndPreviousHelpers_ReturnExpectedDates()
    {
        var date = new DateTime(2024, 1, 15);
        date.NextYear().ShouldBe(new DateTime(2025, 1, 15));
        date.PreviousYear().ShouldBe(new DateTime(2023, 1, 15));
        date.NextQuarter().ShouldBe(new DateTime(2024, 4, 15));
        date.PreviousQuarter().ShouldBe(new DateTime(2023, 10, 15));
        date.NextMonth().ShouldBe(new DateTime(2024, 2, 15));
        date.PreviousMonth().ShouldBe(new DateTime(2023, 12, 15));
        date.NextWeek().ShouldBe(new DateTime(2024, 1, 22));
        date.PreviousWeek().ShouldBe(new DateTime(2024, 1, 8));
        date.NextDay().ShouldBe(new DateTime(2024, 1, 16));
        date.PreviousDay().ShouldBe(new DateTime(2024, 1, 14));
        date.Tomorrow().ShouldBe(new DateTime(2024, 1, 16));
        date.Yesterday().ShouldBe(new DateTime(2024, 1, 14));
    }
    /// <summary>
    /// 验证指定星期的前后日期导航。
    /// </summary>
    [Fact]
    public void NextDayOfWeekAndPreviousDayOfWeek_ReturnExpectedDates()
    {
        var monday = new DateTime(2024, 1, 1);
        var nextFriday = monday.NextDayOfWeek(DayOfWeek.Friday);
        var previousFriday = monday.PreviousDayOfWeek(DayOfWeek.Friday);
        nextFriday.ShouldBe(new DateTime(2024, 1, 5));
        previousFriday.ShouldBe(new DateTime(2023, 12, 29));
    }
}
