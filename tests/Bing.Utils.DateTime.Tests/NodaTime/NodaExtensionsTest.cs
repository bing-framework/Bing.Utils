namespace NodaTime;
/// <summary>
/// 测试类：覆盖 `NodaExtensions` 相关行为。
/// </summary>
[Trait("DateTimeUT", "NodaExtensions")]
public class NodaExtensionsTest
{
    /// <summary>
    /// 测试用例：验证 `NodaDurationExtensions` 在 `AsDuration` 场景下，结果为 `ShouldConvertFromTimeSpan`。
    /// </summary>
    [Fact]
    public void NodaDurationExtensions_AsDuration_ShouldConvertFromTimeSpan()
    {
        var ts = TimeSpan.FromSeconds(90);
        var duration = ts.AsDuration();
        duration.ToTimeSpan().ShouldBe(ts);
    }
    /// <summary>
    /// 测试用例：验证 `NodaDurationExtensions` 在 `AsDurationOfWeeks` 场景下，结果为 `ShouldConvert`。
    /// </summary>
    [Fact]
    public void NodaDurationExtensions_AsDurationOfWeeks_ShouldConvert()
    {
        var duration = 2.AsDurationOfWeeks();
        duration.ToTimeSpan().ShouldBe(TimeSpan.FromDays(14));
    }
    /// <summary>
    /// 测试用例：验证 `NodaPeriodExtensions` 在 `AsPeriodAndBack` 场景下，结果为 `ShouldKeepTicks`。
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
    /// 测试用例：验证 `NodaPeriodExtensions` 在 `PeriodFactories` 场景下，结果为 `ShouldProduceExpectedValues`。
    /// </summary>
    [Fact]
    public void NodaPeriodExtensions_PeriodFactories_ShouldProduceExpectedValues()
    {
        3.AsPeriodOfQuarters().Months.ShouldBe(9);
        2.AsPeriodOfYears().Years.ShouldBe(2);
        15L.AsPeriodOfSeconds().Seconds.ShouldBe(15);
    }
}

