namespace Bing.Date;

/// <summary>
/// 日期时间区间起止顺序归一化的回归测试。
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class DateTimeRangeRegressionTest
{
    /// <summary>
    /// 验证构造函数传入起始时间大于结束时间时，应自动归一化区间顺序
    /// </summary>
    [Fact]
    [Trait("DefectPattern", "DateTime.DateTimeRange.NormalizeStartEnd")]
    public void Ctor_StartGreaterThanEnd_ShouldNormalizeRangeOrder()
    {
        var start = new DateTime(2026, 2, 20, 10, 0, 0);
        var end = new DateTime(2026, 2, 19, 10, 0, 0);

        var range = new DateTimeRange(start, end);

        range.StartTime.ShouldBe(end);
        range.EndTime.ShouldBe(start);
        range.TotalDays.ShouldBe(1d);
    }
}
