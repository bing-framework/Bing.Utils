namespace Bing.Date;
/// <summary>
/// 验证日期时间的分量设置及无效日期检查。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.Update")]
public class DateTimeUpdateTest
{
    /// <summary>
    /// 验证指定时分时保留日期及秒以下分量。
    /// </summary>
    [Fact]
    public void At_WithHourAndMinute_SetsExpectedTime()
    {
        var date = new DateTime(2024, 2, 29, 8, 9, 10, 123, DateTimeKind.Utc);
        var result = date.At(6, 30);
        result.ShouldBe(new DateTime(2024, 2, 29, 6, 30, 10, 123, DateTimeKind.Utc));
    }
    /// <summary>
    /// 验证指定时分秒与毫秒时保留日期及时间种类。
    /// </summary>
    [Fact]
    public void At_WithHourMinuteSecondMillisecond_SetsExpectedTime()
    {
        var date = new DateTime(2024, 2, 29, 8, 9, 10, 123, DateTimeKind.Utc);
        var result = date.At(1, 2, 3, 4);
        result.ShouldBe(new DateTime(2024, 2, 29, 1, 2, 3, 4, DateTimeKind.Utc));
    }
    /// <summary>
    /// 验证午夜与正午设置清除较小时间分量。
    /// </summary>
    [Fact]
    public void AtMidnightAndAtNoon_ReturnExpectedTime()
    {
        var date = new DateTime(2024, 2, 29, 8, 9, 10, 123, DateTimeKind.Unspecified);
        date.AtMidnight().ShouldBe(new DateTime(2024, 2, 29, 0, 0, 0, 0, DateTimeKind.Unspecified));
        date.AtNoon().ShouldBe(new DateTime(2024, 2, 29, 12, 0, 0, 0, DateTimeKind.Unspecified));
    }
    /// <summary>
    /// 验证指定新日期时保留时间分量及时间种类。
    /// </summary>
    [Fact]
    public void On_SetsDateAndKeepsTimePart()
    {
        var date = new DateTime(2024, 2, 29, 8, 9, 10, 123, DateTimeKind.Local);
        var result = date.On(2023, 1, 31);
        result.ShouldBe(new DateTime(2023, 1, 31, 8, 9, 10, 123, DateTimeKind.Local));
    }
    /// <summary>
    /// 验证各分量设置方法仅更新指定分量。
    /// </summary>
    [Fact]
    public void SetDateAndTimeHelpers_UpdateSinglePartOnly()
    {
        var date = new DateTime(2024, 2, 28, 8, 9, 10, 123, DateTimeKind.Local);
        date.SetHour(1).ShouldBe(new DateTime(2024, 2, 28, 1, 9, 10, 123, DateTimeKind.Local));
        date.SetMinute(2).ShouldBe(new DateTime(2024, 2, 28, 8, 2, 10, 123, DateTimeKind.Local));
        date.SetSecond(3).ShouldBe(new DateTime(2024, 2, 28, 8, 9, 3, 123, DateTimeKind.Local));
        date.SetMillisecond(4).ShouldBe(new DateTime(2024, 2, 28, 8, 9, 10, 4, DateTimeKind.Local));
        date.SetYear(2023).ShouldBe(new DateTime(2023, 2, 28, 8, 9, 10, 123, DateTimeKind.Local));
        date.SetMonth(1).ShouldBe(new DateTime(2024, 1, 28, 8, 9, 10, 123, DateTimeKind.Local));
        date.SetDay(15).ShouldBe(new DateTime(2024, 2, 15, 8, 9, 10, 123, DateTimeKind.Local));
    }
    /// <summary>
    /// 验证时间种类设置保留日期时间分量。
    /// </summary>
    [Fact]
    public void SetKind_UpdatesDateTimeKind()
    {
        var date = new DateTime(2024, 2, 29, 8, 9, 10, 123, DateTimeKind.Local);
        var result = date.SetKind(DateTimeKind.Utc);
        result.ShouldBe(new DateTime(2024, 2, 29, 8, 9, 10, 123, DateTimeKind.Utc));
    }
    /// <summary>
    /// 验证小时设置拒绝二十四时。
    /// </summary>
    [Fact]
    public void SetTime_InvalidHour_ThrowsArgumentOutOfRangeException()
    {
        var date = new DateTime(2024, 2, 29, 8, 9, 10, 123, DateTimeKind.Utc);
        Should.Throw<ArgumentOutOfRangeException>(() => date.SetTime(24));
    }
    /// <summary>
    /// 验证一月三十一日无法直接设置到二月。
    /// </summary>
    [Fact]
    public void SetMonth_WhenCurrentDayNotInTargetMonth_ThrowsArgumentOutOfRangeException()
    {
        var date = new DateTime(2024, 1, 31, 8, 9, 10, 123, DateTimeKind.Utc);
        Should.Throw<ArgumentOutOfRangeException>(() => date.SetMonth(2));
    }
}
