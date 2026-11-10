using Bing.Date;
using Bing.Date.Chinese;

namespace Bing.Utils.DateTimeScenario;
/// <summary>
/// 同时使用公历和农历扩展时的调用场景。
/// </summary>
public class DateTimeLeapUsageScenarioTest
{
    /// <summary>
    /// 验证可通过静态调用明确区分公历和农历闰年，并继续使用农历闰月扩展。
    /// </summary>
    [Fact]
    public void GregorianAndLunarExtensions_UseExplicitYearChecks()
    {
        var date = new System.DateTime(2023, 3, 22);

        DateTimeLeapExtensions.IsLeapYear(date).ShouldBeTrue();
        System.DateTime.IsLeapYear(date.Year).ShouldBeFalse();
        date.IsLeapMonth().ShouldBeTrue();
        date.IsLeapDay().ShouldBeTrue();
    }
}
