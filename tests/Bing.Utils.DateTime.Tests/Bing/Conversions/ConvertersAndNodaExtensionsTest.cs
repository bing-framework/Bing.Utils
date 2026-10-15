using NodaTime;
namespace Bing.Conversions;
/// <summary>
/// 验证星期数值转换及时间间隔到日期时间的转换。
/// </summary>
[Trait("DateTimeUT", "ConversionsAndNodaExtensions")]
public class ConvertersAndNodaExtensionsTest
{
    /// <summary>
    /// 验证星期数值转换应用指定的起始偏移。
    /// </summary>
    [Fact]
    public void DayOfWeekConv_ToInt32_ShouldUseExpectedOffset()
    {
        DayOfWeekConv.ToInt32(DayOfWeek.Sunday).ShouldBe(1);
        DayOfWeekConv.ToInt32(DayOfWeek.Monday).ShouldBe(2);
        DayOfWeekConv.ToInt32(DayOfWeek.Saturday, 0).ShouldBe(6);
        DayOfWeek.Wednesday.CastToInt32(10).ShouldBe(13);
    }
    /// <summary>
    /// 验证时间间隔按时钟周期数转换为日期时间。
    /// </summary>
    [Fact]
    public void TimeSpanConv_ToDateTime_ShouldConvertByTicks()
    {
        var ts = TimeSpan.FromHours(2);
        var dt = TimeSpanConv.ToDateTime(ts);
        dt.Ticks.ShouldBe(ts.Ticks);
    }
    /// <summary>
    /// 验证负时间间隔无法转换为日期时间。
    /// </summary>
    [Fact]
    public void TimeSpanConv_ToDateTime_Negative_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => TimeSpanConv.ToDateTime(TimeSpan.FromTicks(-1)));
    }
    /// <summary>
    /// 验证时间间隔转换扩展保留时钟周期数。
    /// </summary>
    [Fact]
    public void TimeSpanConvExtensions_CastToDateTime_ShouldDelegate()
    {
        var ts = TimeSpan.FromMinutes(5);
        ts.CastToDateTime().Ticks.ShouldBe(ts.Ticks);
    }
}

