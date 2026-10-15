namespace Bing.Date;
/// <summary>
/// 验证日期有效范围、当天判定及工作日分类。
/// </summary>
[Trait("DateTimeUT", "DateJudge.Additional")]
public class DateJudgeAdditionalTest
{
    /// <summary>
    /// 验证有效日期判定在上下边界附近的结果。
    /// </summary>
    /// <param name="year">测试日期的年份。</param>
    /// <param name="month">测试日期的月份。</param>
    /// <param name="day">测试日期的日分量。</param>
    /// <param name="hour">测试日期的小时分量。</param>
    /// <param name="minute">测试日期的分钟分量。</param>
    /// <param name="second">测试日期的秒分量。</param>
    /// <param name="millisecond">测试日期的毫秒分量。</param>
    /// <param name="expected">预期日期是否处于有效范围。</param>
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
    /// 验证最大日期时间值当前不满足有效日期判定。
    /// </summary>
    [Fact]
    public void IsValid_DateTimeMaxValue_ReturnsFalse()
    {
        DateJudge.IsValid(DateTime.MaxValue).ShouldBeFalse();
    }
    /// <summary>
    /// 验证空日期与空时间点均不被识别为今天。
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
    /// 验证当天判定能够区分今天与昨天。
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
    /// 验证周末与工作日判定互为反向。
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

