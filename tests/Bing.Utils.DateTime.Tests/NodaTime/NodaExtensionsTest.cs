namespace NodaTime;
/// <summary>
/// 验证时间间隔与 NodaTime 时长、周期之间的转换。
/// </summary>
[Trait("DateTimeUT", "NodaExtensions")]
public class NodaExtensionsTest
{
    /// <summary>
    /// 验证时间间隔转换为时长后保持原值。
    /// </summary>
    [Fact]
    public void NodaDurationExtensions_AsDuration_ShouldConvertFromTimeSpan()
    {
        var ts = TimeSpan.FromSeconds(90);
        var duration = ts.AsDuration();
        duration.ToTimeSpan().ShouldBe(ts);
    }
    /// <summary>
    /// 验证周数转换为对应天数的时长。
    /// </summary>
    [Fact]
    public void NodaDurationExtensions_AsDurationOfWeeks_ShouldConvert()
    {
        var duration = 2.AsDurationOfWeeks();
        duration.ToTimeSpan().ShouldBe(TimeSpan.FromDays(14));
    }
    /// <summary>
    /// 验证时间间隔与周期、时长互转时保留时钟周期数。
    /// </summary>
    [Fact]
    public void NodaPeriodExtensions_AsPeriodAndBack_ShouldKeepTicks()
    {
        var ts = TimeSpan.FromMilliseconds(1234);
        var period = ts.AsPeriod();
        period.Ticks.ShouldBe(ts.Ticks);
        period.AsTimeSpan().Ticks.ShouldBe(ts.Ticks);
        period.AsDuration().ToTimeSpan().Ticks.ShouldBe(ts.Ticks);
    }
    /// <summary>
    /// 验证季度、年及秒数工厂设置对应周期分量。
    /// </summary>
    [Fact]
    public void NodaPeriodExtensions_PeriodFactories_ShouldProduceExpectedValues()
    {
        3.AsPeriodOfQuarters().Months.ShouldBe(9);
        2.AsPeriodOfYears().Years.ShouldBe(2);
        15L.AsPeriodOfSeconds().Seconds.ShouldBe(15);
    }
}

