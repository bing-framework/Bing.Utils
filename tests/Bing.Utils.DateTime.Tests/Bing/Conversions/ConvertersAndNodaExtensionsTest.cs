using NodaTime;
namespace Bing.Conversions;
/// <summary>
/// 测试类：覆盖 `ConvertersAndNodaExtensions` 相关行为。
/// </summary>
[Trait("DateTimeUT", "ConversionsAndNodaExtensions")]
public class ConvertersAndNodaExtensionsTest
{
    /// <summary>
    /// 测试用例：验证 `DayOfWeekConv` 在 `ToInt32` 场景下，结果为 `ShouldUseExpectedOffset`。
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
    /// 测试用例：验证 `TimeSpanConv` 在 `ToDateTime` 场景下，结果为 `ShouldConvertByTicks`。
    /// </summary>
    [Fact]
    public void TimeSpanConv_ToDateTime_ShouldConvertByTicks()
    {
        var ts = TimeSpan.FromHours(2);
        var dt = TimeSpanConv.ToDateTime(ts);
        dt.Ticks.ShouldBe(ts.Ticks);
    }
    /// <summary>
    /// 测试用例：验证 `TimeSpanConv` 在 `ToDateTime_Negative` 场景下，结果为 `ShouldThrow`。
    /// </summary>
    [Fact]
    public void TimeSpanConv_ToDateTime_Negative_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => TimeSpanConv.ToDateTime(TimeSpan.FromTicks(-1)));
    }
    /// <summary>
    /// 测试用例：验证 `TimeSpanConvExtensions` 在 `CastToDateTime` 场景下，结果为 `ShouldDelegate`。
    /// </summary>
    [Fact]
    public void TimeSpanConvExtensions_CastToDateTime_ShouldDelegate()
    {
        var ts = TimeSpan.FromMinutes(5);
        ts.CastToDateTime().Ticks.ShouldBe(ts.Ticks);
    }
}

