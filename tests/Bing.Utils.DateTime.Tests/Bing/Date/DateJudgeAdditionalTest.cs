namespace Bing.Date;
/// <summary>
/// 测试类：覆盖 `DateJudgeAdditional` 相关行为。
/// </summary>
[Trait("DateTimeUT", "DateJudge.Additional")]
public class DateJudgeAdditionalTest
{
    /// <summary>
    /// 测试用例：验证 `IsValid` 在 `BoundaryDate` 场景下，结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData(1899, 12, 31, 23, 59, 59, 999, false)]
    [InlineData(1900, 1, 1, 0, 0, 0, 0, true)]
    [InlineData(9999, 12, 31, 23, 59, 59, 999, true)]
    [InlineData(9999, 12, 31, 23, 59, 59, 998, true)]
    public void IsValid_BoundaryDate_ReturnsExpectedResult(
        int year, int month, int day, int hour, int minute, int second, int millisecond, bool expected)
    {
        var value = new DateTime(year, month, day, hour, minute, second, millisecond);
        DateJudge.IsValid(value).ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `IsValid` 在 `DateTimeMaxValue` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void IsValid_DateTimeMaxValue_ReturnsFalse()
    {
        DateJudge.IsValid(DateTime.MaxValue).ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsToday` 在 `NullableNull` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void IsToday_NullableNull_ReturnsFalse()
    {
        DateTime? dt = null;
        DateTimeOffset? dto = null;
        DateJudge.IsToday(dt).ShouldBeFalse();
        DateJudge.IsToday(dto).ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsToday` 在 `TodayAndYesterday` 场景下，结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Fact]
    public void IsToday_TodayAndYesterday_ReturnsExpectedResult()
    {
        var today = DateTime.Today;
        var yesterday = DateTime.Today.AddDays(-1);
        DateJudge.IsToday(today).ShouldBeTrue();
        DateJudge.IsToday(yesterday).ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsWeekendAndIsWeekday` 在 `SameDate` 场景下，结果为 `AreComplementary`。
    /// </summary>
    [Fact]
    public void IsWeekendAndIsWeekday_SameDate_AreComplementary()
    {
        var saturday = new DateTime(2024, 1, 6);
        var wednesday = new DateTime(2024, 1, 3);
        DateJudge.IsWeekend(saturday).ShouldBeTrue();
        DateJudge.IsWeekday(saturday).ShouldBeFalse();
        DateJudge.IsWeekend(wednesday).ShouldBeFalse();
        DateJudge.IsWeekday(wednesday).ShouldBeTrue();
    }
}

