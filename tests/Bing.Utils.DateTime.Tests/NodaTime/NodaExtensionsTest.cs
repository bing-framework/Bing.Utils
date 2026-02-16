namespace NodaTime;

[Trait("DateTimeUT", "NodaExtensions")]
public class NodaExtensionsTest
{
    [Fact]
    public void NodaDurationExtensions_AsDuration_ShouldConvertFromTimeSpan()
    {
        var ts = TimeSpan.FromSeconds(90);
        var duration = ts.AsDuration();
        duration.ToTimeSpan().ShouldBe(ts);
    }

    [Fact]
    public void NodaDurationExtensions_AsDurationOfWeeks_ShouldConvert()
    {
        var duration = 2.AsDurationOfWeeks();
        duration.ToTimeSpan().ShouldBe(TimeSpan.FromDays(14));
    }

    [Fact]
    public void NodaPeriodExtensions_AsPeriodAndBack_ShouldKeepTicks()
    {
        var ts = TimeSpan.FromMilliseconds(1234);
        var period = ts.AsPeriod();
        period.Ticks.ShouldBe(ts.Ticks);
        period.AsTimeSpan().Ticks.ShouldBe(ts.Ticks);
        period.AsDuration().ToTimeSpan().Ticks.ShouldBe(ts.Ticks);
    }

    [Fact]
    public void NodaPeriodExtensions_PeriodFactories_ShouldProduceExpectedValues()
    {
        3.AsPeriodOfQuarters().Months.ShouldBe(9);
        2.AsPeriodOfYears().Years.ShouldBe(2);
        15L.AsPeriodOfSeconds().Seconds.ShouldBe(15);
    }
}
