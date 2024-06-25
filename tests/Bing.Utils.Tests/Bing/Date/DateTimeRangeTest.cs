namespace Bing.Date;

/// <summary>
/// 时间范围 测试
/// </summary>
public class DateTimeRangeTest
{
    /// <summary>
    /// 当前时间
    /// </summary>
    private static readonly DateTime _now = new(2024, 6, 25, 14, 37, 56, 78);

    /// <summary>
    /// 测试 - 构造函数 - 默认
    /// </summary>
    [Fact]
    public void Test_Ctor_Default()
    {
        var range = new DateTimeRange();
        range.StartTime.ShouldBe(DateTime.MinValue);
        range.EndTime.ShouldBe(DateTime.MaxValue);

        var now = new DateTime(2024, 1, 15, 13, 41, 22);
        range = new DateTimeRange(now.Date.AddDays(-1), now.Date.AddDays(2));
        range.StartTime.Day.ShouldBe(14);
        range.EndTime.Day.ShouldBe(17);
    }

    /// <summary>
    /// 测试 - 匹配相等
    /// </summary>
    [Fact]
    public void Test_Equals()
    {
        var range1 = new DateTimeRange(_now, _now.AddMinutes(1));
        var range2 = new DateTimeRange(_now, _now.AddMinutes(1));
        Assert.True(range1==range2);
    }

    /// <summary>
    /// 测试 - 属性
    /// </summary>
    [Fact]
    public void Test_Properties()
    {
        DateTimeRange.Today.StartTime.ShouldBeGreaterThan(DateTimeRange.Yesterday.EndTime);
        DateTimeRange.Today.EndTime.ShouldBeLessThan(DateTimeRange.Tomorrow.StartTime);

        DateTimeRange.ThisMonth.StartTime.Day.ShouldBe(1);
        DateTimeRange.LastMonth.StartTime.Day.ShouldBe(1);
        DateTimeRange.NextMonth.StartTime.Day.ShouldBe(1);

        DateTimeRange.ThisMonth.StartTime.ShouldBeGreaterThan(DateTimeRange.LastMonth.EndTime);
        DateTimeRange.ThisMonth.EndTime.ShouldBeLessThan(DateTimeRange.NextMonth.StartTime);

        DateTimeRange.ThisYear.StartTime.Month.ShouldBe(1);
        DateTimeRange.ThisYear.StartTime.Day.ShouldBe(1);

        DateTimeRange.ThisYear.StartTime.ShouldBeGreaterThan(DateTimeRange.LastYear.EndTime);
        DateTimeRange.ThisYear.EndTime.ShouldBeLessThan(DateTimeRange.NextYear.StartTime);

        DateTimeRange.Last7DaysExceptToday.EndTime.ShouldBeLessThan(DateTimeRange.Today.StartTime);
        DateTimeRange.Last30DaysExceptToday.EndTime.ShouldBeLessThan(DateTimeRange.Today.StartTime);
    }
}