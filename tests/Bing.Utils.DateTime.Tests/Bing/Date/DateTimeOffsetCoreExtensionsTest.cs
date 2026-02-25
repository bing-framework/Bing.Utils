namespace Bing.Date;
/// <summary>
/// 时间点扩展核心能力测试
/// </summary>
[Trait("DateTimeUT", "DateTimeOffsetExtensions.Core")]
public class DateTimeOffsetCoreExtensionsTest
{
    [Fact]
    public void AddDateTimeSpan_And_SubtractDateTimeSpan_ShouldReturnExpectedCurrentBehavior()
    {
        var offset = TimeSpan.FromHours(8);
        var dto = new DateTimeOffset(2024, 1, 15, 10, 20, 30, offset);
        var span = new DateTimeSpan(1, 1, TimeSpan.FromHours(2));
        var added = dto.AddDateTimeSpan(span);
        var subtracted = added.SubtractDateTimeSpan(span);
        added.ShouldBe(new DateTimeOffset(2025, 2, 15, 12, 20, 30, offset));
        subtracted.ShouldBe(new DateTimeOffset(2024, 1, 15, 14, 20, 30, offset));
    }
    [Fact]
    public void AddBusinessDays_ShouldSkipWeekends()
    {
        var offset = TimeSpan.FromHours(8);
        var friday = new DateTimeOffset(2024, 1, 5, 9, 0, 0, offset);
        var next = friday.AddBusinessDays(1);
        next.Date.ShouldBe(new DateTime(2024, 1, 8));
        next.DayOfWeek.ShouldBe(DayOfWeek.Monday);
    }
    [Fact]
    public void AddBusinessDays_NegativeDays_ShouldMoveBackwardSkippingWeekends()
    {
        var offset = TimeSpan.FromHours(8);
        var monday = new DateTimeOffset(2024, 1, 8, 9, 0, 0, offset);
        var prev = monday.AddBusinessDays(-1);
        prev.Date.ShouldBe(new DateTime(2024, 1, 5));
        prev.DayOfWeek.ShouldBe(DayOfWeek.Friday);
    }
    [Fact]
    public void SubtractBusinessDays_ShouldDelegateToNegativeAddBusinessDays()
    {
        var offset = TimeSpan.FromHours(8);
        var monday = new DateTimeOffset(2024, 1, 8, 9, 0, 0, offset);
        var result = monday.SubtractBusinessDays(1);
        result.Date.ShouldBe(new DateTime(2024, 1, 5));
    }
    [Fact]
    public void IsToday_ForNullableNull_ShouldReturnFalse()
    {
        DateTimeOffset? value = null;
        value.IsToday().ShouldBeFalse();
    }
    [Fact]
    public void IsBeforeAndIsAfter_ShouldReturnExpected()
    {
        var now = DateTimeOffset.Now;
        var future = now.AddMinutes(1);
        var past = now.AddMinutes(-1);
        past.IsBefore(now).ShouldBeTrue();
        future.IsAfter(now).ShouldBeTrue();
        now.IsBefore(past).ShouldBeFalse();
    }
    [Fact]
    public void IsInTheFutureAndIsInThePast_ShouldReturnExpected()
    {
        var future = DateTimeOffset.Now.AddMinutes(1);
        var past = DateTimeOffset.Now.AddMinutes(-1);
        future.IsInTheFuture().ShouldBeTrue();
        past.IsInThePast().ShouldBeTrue();
    }
    [Fact]
    public void IsSameMonth_CurrentBehavior_IgnoresYear()
    {
        var left = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var right = new DateTimeOffset(2023, 1, 31, 23, 0, 0, TimeSpan.Zero);
        left.IsSameMonth(right).ShouldBeTrue();
    }
    [Fact]
    public void IsSameDayAndIsSameYear_ShouldReturnExpected()
    {
        var left = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var sameDay = new DateTimeOffset(2024, 1, 1, 23, 59, 59, TimeSpan.Zero);
        var otherYear = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        left.IsSameDay(sameDay).ShouldBeTrue();
        left.IsSameYear(sameDay).ShouldBeTrue();
        left.IsSameYear(otherYear).ShouldBeFalse();
    }
    [Theory]
    [InlineData(499, 12, 30)]
    [InlineData(500, 12, 31)]
    public void Round_Second_ShouldRoundByMilliseconds(int ms, int expectedMinute, int expectedSecond)
    {
        var dto = new DateTimeOffset(2024, 1, 1, 8, 12, 30, ms, TimeSpan.FromHours(8));
        var rounded = dto.Round(RoundTo.Second);
        rounded.Minute.ShouldBe(expectedMinute);
        rounded.Second.ShouldBe(expectedSecond);
        rounded.Offset.ShouldBe(dto.Offset);
    }
    [Fact]
    public void Round_MinuteHourDay_ShouldRoundByBoundaries()
    {
        var offset = TimeSpan.FromHours(8);
        var minuteBoundary = new DateTimeOffset(2024, 1, 1, 8, 12, 30, 0, offset);
        var hourBoundary = new DateTimeOffset(2024, 1, 1, 8, 30, 0, 0, offset);
        var dayBoundary = new DateTimeOffset(2024, 1, 1, 12, 0, 0, 0, offset);
        minuteBoundary.Round(RoundTo.Minute).ShouldBe(new DateTimeOffset(2024, 1, 1, 8, 13, 0, offset));
        hourBoundary.Round(RoundTo.Hour).ShouldBe(new DateTimeOffset(2024, 1, 1, 9, 0, 0, offset));
        dayBoundary.Round(RoundTo.Day).ShouldBe(new DateTimeOffset(2024, 1, 2, 0, 0, 0, offset));
    }
    [Fact]
    public void Round_UnknownEnum_ShouldThrowArgumentOutOfRangeException()
    {
        var dto = DateTimeOffset.Now;
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => dto.Round((RoundTo)999));
        ex.ParamName.ShouldBe("roundTo");
    }
}
