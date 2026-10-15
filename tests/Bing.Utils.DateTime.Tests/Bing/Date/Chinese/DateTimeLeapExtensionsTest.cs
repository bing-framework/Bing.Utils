using System.Globalization;

namespace Bing.Date.Chinese;

/// <summary>
/// 农历闰年、闰月和闰日扩展测试。
/// </summary>
/// <remarks>含历法的重载使用显式农历历法；无历法重载的空历法异常另有用例验证。闰月判断沿用当前实现的公历年月映射规则。</remarks>
[Trait("DateTimeUT", "ChineseDate.LeapExtensions")]
public class DateTimeLeapExtensionsTest
{
    /// <summary>
    /// 闰年、闰月和闰日判断使用的农历历法。
    /// </summary>
    private readonly ChineseLunisolarCalendar _calendar = new();

    #region IsLeapYear (with calendar)

    /// <summary>
    /// 验证二〇二〇年被判断为农历闰年。
    /// </summary>
    [Fact]
    public void IsLeapYear_LeapYear2020_ReturnsTrue()
    {
        var dt = new DateTime(2020, 6, 1);
        dt.IsLeapYear(_calendar).ShouldBeTrue();
    }

    /// <summary>
    /// 验证二〇二一年被判断为农历平年。
    /// </summary>
    [Fact]
    public void IsLeapYear_NonLeapYear2021_ReturnsFalse()
    {
        var dt = new DateTime(2021, 6, 1);
        dt.IsLeapYear(_calendar).ShouldBeFalse();
    }

    /// <summary>
    /// 验证二〇二三年被判断为农历闰年。
    /// </summary>
    [Fact]
    public void IsLeapYear_LeapYear2023_ReturnsTrue()
    {
        var dt = new DateTime(2023, 5, 1);
        dt.IsLeapYear(_calendar).ShouldBeTrue();
    }

    /// <summary>
    /// 验证无历法重载因缺少历法而抛出参数空值异常。
    /// </summary>
    [Fact]
    public void IsLeapYear_NoCalendarOverload_ThrowsArgumentNullException()
    {
        var dt = new DateTime(2020, 6, 1);
        Should.Throw<ArgumentNullException>(() => dt.IsLeapYear());
    }

    #endregion

    #region IsLeapMonth (with calendar)

    /// <summary>
    /// 验证二〇二〇年五月的当前闰月映射结果。
    /// </summary>
    [Fact]
    public void IsLeapMonth_WithDateInLeapMonth2020_ReturnsTrue()
    {
        // 2020-05-25 公历月=5，对应农历年2020 month槽位5=闰四月
        var dt = new DateTime(2020, 5, 25);
        dt.IsLeapMonth(_calendar).ShouldBeTrue();
    }

    /// <summary>
    /// 验证二〇二三年三月的当前闰月映射结果。
    /// </summary>
    [Fact]
    public void IsLeapMonth_WithDateInLeapMonth2023_ReturnsTrue()
    {
        var dt = new DateTime(2023, 3, 25);
        dt.IsLeapMonth(_calendar).ShouldBeTrue();
    }

    /// <summary>
    /// 验证非闰月日期返回否定结果。
    /// </summary>
    [Fact]
    public void IsLeapMonth_WithDateNotInLeapMonth_ReturnsFalse()
    {
        var dt = new DateTime(2023, 1, 22);
        dt.IsLeapMonth(_calendar).ShouldBeFalse();
    }

    #endregion

    #region IsLeapDay (with calendar)

    /// <summary>
    /// 验证当前闰月映射下的日期被判断为闰日。
    /// </summary>
    [Fact]
    public void IsLeapDay_WithDayInLeapMonth_ReturnsTrue()
    {
        // 2020-05-25 公历月=5，农历year=2020 month=5为闰四月，属于闰日
        var dt = new DateTime(2020, 5, 25);
        dt.IsLeapDay(_calendar).ShouldBeTrue();
    }

    /// <summary>
    /// 验证普通农历日期不属于闰日。
    /// </summary>
    [Fact]
    public void IsLeapDay_WithDayNotInLeapMonth_ReturnsFalse()
    {
        var dt = new DateTime(2023, 1, 22);
        dt.IsLeapDay(_calendar).ShouldBeFalse();
    }

    #endregion
}
