using System.Globalization;

namespace Bing.Date.Chinese;

/// <summary>
/// 测试类：覆盖 <see cref="DateTimeLeapExtensions"/> 相关行为。
/// 注意：无 calendar 参数重载内部传 null 历法给 ChineseDateHelper，会抛出 ArgumentNullException，
/// 因此仅测试需要显式传入 <see cref="ChineseLunisolarCalendar"/> 的重载。
/// 判断逻辑依赖公历年份/月份映射至农历年/月槽位，详见 ChineseDateHelper 实现。
/// </summary>
[Trait("DateTimeUT", "ChineseDate.LeapExtensions")]
public class DateTimeLeapExtensionsTest
{
    /// <summary>
    /// 中国农历日历实例
    /// </summary>
    private readonly ChineseLunisolarCalendar _calendar = new();

    #region IsLeapYear (with calendar)

    /// <summary>
    /// 测试用例：验证 <see cref="DateTimeLeapExtensions.IsLeapYear(DateTime, ChineseLunisolarCalendar)"/> 在 `LeapYear2020` 场景下，结果为 `ReturnsTrue`。
    /// 2020年(庚子年)有闰四月，IsLeapYear 使用公历年=2020 查询农历闰年状态
    /// </summary>
    [Fact]
    public void IsLeapYear_LeapYear2020_ReturnsTrue()
    {
        var dt = new DateTime(2020, 6, 1);
        dt.IsLeapYear(_calendar).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="DateTimeLeapExtensions.IsLeapYear(DateTime, ChineseLunisolarCalendar)"/> 在 `NonLeapYear2021` 场景下，结果为 `ReturnsFalse`。
    /// 2021年(辛丑年)无闰月
    /// </summary>
    [Fact]
    public void IsLeapYear_NonLeapYear2021_ReturnsFalse()
    {
        var dt = new DateTime(2021, 6, 1);
        dt.IsLeapYear(_calendar).ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="DateTimeLeapExtensions.IsLeapYear(DateTime, ChineseLunisolarCalendar)"/> 在 `LeapYear2023` 场景下，结果为 `ReturnsTrue`。
    /// 2023年有闰二月
    /// </summary>
    [Fact]
    public void IsLeapYear_LeapYear2023_ReturnsTrue()
    {
        var dt = new DateTime(2023, 5, 1);
        dt.IsLeapYear(_calendar).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="DateTimeLeapExtensions.IsLeapYear(DateTime)"/> 在 `NoCalendarOverload` 场景下，结果为 `ThrowsArgumentNullException`。
    /// 无历法重载内部传 null 给 ChineseDateHelper，会抛出 ArgumentNullException
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
    /// 测试用例：验证 <see cref="DateTimeLeapExtensions.IsLeapMonth(DateTime, ChineseLunisolarCalendar)"/> 在 `WithDateInLeapMonth2020` 场景下，结果为 `ReturnsTrue`。
    /// 2020年5月对应农历闰四月槽位(内部month=5)，IsLeapMonth(2020,5)=true
    /// </summary>
    [Fact]
    public void IsLeapMonth_WithDateInLeapMonth2020_ReturnsTrue()
    {
        // 2020-05-25 公历月=5，对应农历年2020 month槽位5=闰四月
        var dt = new DateTime(2020, 5, 25);
        dt.IsLeapMonth(_calendar).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="DateTimeLeapExtensions.IsLeapMonth(DateTime, ChineseLunisolarCalendar)"/> 在 `WithDateInLeapMonth2023` 场景下，结果为 `ReturnsTrue`。
    /// 2023-03-25 公历月=3，对应农历年2023 month槽位3=闰二月
    /// </summary>
    [Fact]
    public void IsLeapMonth_WithDateInLeapMonth2023_ReturnsTrue()
    {
        var dt = new DateTime(2023, 3, 25);
        dt.IsLeapMonth(_calendar).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="DateTimeLeapExtensions.IsLeapMonth(DateTime, ChineseLunisolarCalendar)"/> 在 `WithDateNotInLeapMonth` 场景下，结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 <see cref="DateTimeLeapExtensions.IsLeapDay(DateTime, ChineseLunisolarCalendar)"/> 在 `WithDayInLeapMonth` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsLeapDay_WithDayInLeapMonth_ReturnsTrue()
    {
        // 2020-05-25 公历月=5，农历year=2020 month=5为闰四月，属于闰日
        var dt = new DateTime(2020, 5, 25);
        dt.IsLeapDay(_calendar).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="DateTimeLeapExtensions.IsLeapDay(DateTime, ChineseLunisolarCalendar)"/> 在 `WithDayNotInLeapMonth` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void IsLeapDay_WithDayNotInLeapMonth_ReturnsFalse()
    {
        var dt = new DateTime(2023, 1, 22);
        dt.IsLeapDay(_calendar).ShouldBeFalse();
    }

    #endregion
}
