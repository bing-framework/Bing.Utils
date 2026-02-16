using NodaTime;

namespace Bing.Conversions;

[Trait("DateTimeUT", "ConversionsAndNodaExtensions")]
public class ConvertersAndNodaExtensionsTest
{
    [Fact]
    public void DayOfWeekConv_ToInt32_ShouldUseExpectedOffset()
    {
        DayOfWeekConv.ToInt32(DayOfWeek.Sunday).ShouldBe(1);
        DayOfWeekConv.ToInt32(DayOfWeek.Monday).ShouldBe(2);
        DayOfWeekConv.ToInt32(DayOfWeek.Saturday, 0).ShouldBe(6);
        DayOfWeek.Wednesday.CastToInt32(10).ShouldBe(13);
    }

    [Fact]
    public void TimeSpanConv_ToDateTime_ShouldConvertByTicks()
    {
        var ts = TimeSpan.FromHours(2);
        var dt = TimeSpanConv.ToDateTime(ts);
        dt.Ticks.ShouldBe(ts.Ticks);
    }

    [Fact]
    public void TimeSpanConv_ToDateTime_Negative_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => TimeSpanConv.ToDateTime(TimeSpan.FromTicks(-1)));
    }

    [Fact]
    public void TimeSpanConvExtensions_CastToDateTime_ShouldDelegate()
    {
        var ts = TimeSpan.FromMinutes(5);
        ts.CastToDateTime().Ticks.ShouldBe(ts.Ticks);
    }
}
