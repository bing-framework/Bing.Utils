using System.Globalization;

namespace Bing.Date.Chinese;

/// <summary>
/// 测试类：覆盖 <see cref="ChineseDateInfo"/> 相关行为。
/// </summary>
[Trait("DateTimeUT", "ChineseDate.DateInfo")]
public class ChineseDateInfoTest
{
    #region Constructor & Properties

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo"/> 在 `Constructor_WithDateTime` 场景下，结果为 `PropertiesArePositive`。
    /// </summary>
    [Fact]
    public void Constructor_WithDateTime_PropertiesArePositive()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        info.ChineseYear.ShouldBeGreaterThan(0);
        info.ChineseMonth.ShouldBeGreaterThan(0);
        info.ChineseDay.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo"/> 在 `Constructor_WithCalendar` 场景下，结果为 `PropertiesMatchCalendar`。
    /// </summary>
    [Fact]
    public void Constructor_WithCalendar_PropertiesMatchCalendar()
    {
        var dt = new DateTime(2023, 5, 1);
        var calendar = new ChineseLunisolarCalendar();
        var info = new ChineseDateInfo(dt, calendar);
        info.ChineseYear.ShouldBe(calendar.GetYear(dt));
        info.ChineseMonth.ShouldBe(calendar.GetMonth(dt));
        info.ChineseDay.ShouldBe(calendar.GetDayOfMonth(dt));
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.ToDateTime"/> 在 `RoundTrip` 场景下，结果为 `ReturnsSameDate`。
    /// </summary>
    [Fact]
    public void ToDateTime_RoundTrip_ReturnsSameDate()
    {
        var dt = new DateTime(2023, 5, 1);
        var info = new ChineseDateInfo(dt);
        info.ToDateTime().ShouldBe(dt);
    }

    #endregion

    #region IsLeapYear / IsLeapMonth / IsLeapDay

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.IsLeapYear"/> 在 `LeapYear2020` 场景下，结果为 `ReturnsTrue`。
    /// 2020年(庚子年)有闰四月
    /// </summary>
    [Fact]
    public void IsLeapYear_LeapYear2020_ReturnsTrue()
    {
        var info = new ChineseDateInfo(new DateTime(2020, 6, 1));
        info.IsLeapYear().ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.IsLeapYear"/> 在 `NonLeapYear` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void IsLeapYear_NonLeapYear_ReturnsFalse()
    {
        var info = new ChineseDateInfo(new DateTime(2021, 6, 1));
        info.IsLeapYear().ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.IsLeapMonth"/> 在 `DateInLeapMonth` 场景下，结果为 `ReturnsTrue`。
    /// 2020年5月(公历)对应农历闰四月槽位(month=5)
    /// </summary>
    [Fact]
    public void IsLeapMonth_DateInLeapMonth_ReturnsTrue()
    {
        // 2020-05-25：公历month=5，对应农历年2020的第5个月槽位为闰四月
        var info = new ChineseDateInfo(new DateTime(2020, 5, 25));
        info.IsLeapMonth().ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.IsLeapMonth"/> 在 `DateNotInLeapMonth` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void IsLeapMonth_DateNotInLeapMonth_ReturnsFalse()
    {
        var info = new ChineseDateInfo(new DateTime(2020, 4, 1));
        info.IsLeapMonth().ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.IsLeapDay"/> 在 `DayInLeapMonth` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsLeapDay_DayInLeapMonth_ReturnsTrue()
    {
        // 2020-05-25：公历month=5，对应农历年2020 month=5为闰四月，任意日均为闰日
        var info = new ChineseDateInfo(new DateTime(2020, 5, 25));
        info.IsLeapDay().ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.IsLeapDay"/> 在 `DayNotInLeapMonth` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void IsLeapDay_DayNotInLeapMonth_ReturnsFalse()
    {
        var info = new ChineseDateInfo(new DateTime(2020, 4, 1));
        info.IsLeapDay().ShouldBeFalse();
    }

    #endregion

    #region IsWeekend / IsWorkDay

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.IsWeekend"/> 在 `Saturday` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsWeekend_Saturday_ReturnsTrue()
    {
        // 2023-05-06 是星期六
        var info = new ChineseDateInfo(new DateTime(2023, 5, 6));
        info.IsWeekend().ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.IsWorkDay"/> 在 `Monday` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsWorkDay_Monday_ReturnsTrue()
    {
        // 2023-05-08 是星期一
        var info = new ChineseDateInfo(new DateTime(2023, 5, 8));
        info.IsWorkDay().ShouldBeTrue();
    }

    #endregion

    #region GetChineseYear / GetSexagenaryYear

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.GetChineseYear"/> 在 `ValidDate` 场景下，结果为 `ReturnsNonEmpty`。
    /// </summary>
    [Fact]
    public void GetChineseYear_ValidDate_ReturnsNonEmpty()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        info.GetChineseYear().ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.GetSexagenaryYear"/> 在 `Year2023RabbitYear` 场景下，结果为 `ReturnsCancao`。
    /// 2023年(癸卯年), 农历新年2023-01-22，节气立春2023-02-04之后
    /// </summary>
    [Fact]
    public void GetSexagenaryYear_Year2023RabbitYear_ReturnsCancao()
    {
        // 2023-05-01 属于癸卯年
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        var sexagenaryYear = info.GetSexagenaryYear();
        sexagenaryYear.ShouldNotBeNullOrEmpty();
        sexagenaryYear.ShouldContain("癸卯");
    }

    #endregion

    #region GetChineseMonth / GetChineseDay / GetChineseDate

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.GetChineseMonth"/> 在 `ValidDate` 场景下，结果为 `ReturnsNonEmpty`。
    /// </summary>
    [Fact]
    public void GetChineseMonth_ValidDate_ReturnsNonEmpty()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        info.GetChineseMonth().ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.GetChineseDay"/> 在 `ValidDate` 场景下，结果为 `ReturnsNonEmpty`。
    /// </summary>
    [Fact]
    public void GetChineseDay_ValidDate_ReturnsNonEmpty()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        info.GetChineseDay().ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.GetChineseDate"/> 在 `ReturnsMonthPlusDay` 场景下，结果为 `ConcatenatesMonthAndDay`。
    /// </summary>
    [Fact]
    public void GetChineseDate_ReturnsMonthPlusDay_ConcatenatesMonthAndDay()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        var date = info.GetChineseDate();
        var month = info.GetChineseMonth();
        var day = info.GetChineseDay();
        date.ShouldBe(month + day);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.GetChineseDateWithYear"/> 在 `ValidDate` 场景下，结果为 `ContainsYearMonthDay`。
    /// </summary>
    [Fact]
    public void GetChineseDateWithYear_ValidDate_ContainsYearMonthDay()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        var dateWithYear = info.GetChineseDateWithYear();
        var year = info.GetChineseYear();
        dateWithYear.ShouldStartWith(year);
        dateWithYear.Length.ShouldBeGreaterThan(year.Length);
    }

    #endregion

    #region GetSolarTerm

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.GetSolarTerm"/> 在 `LiChunDay` 场景下，结果为 `ReturnsLiChun`。
    /// 2023-02-04 是立春
    /// </summary>
    [Fact]
    public void GetSolarTerm_LiChunDay_ReturnsLiChun()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 2, 4));
        info.GetSolarTerm().ShouldBe("立春");
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.GetSolarTerm"/> 在 `NonSolarTermDay` 场景下，结果为 `ReturnsEmpty`。
    /// </summary>
    [Fact]
    public void GetSolarTerm_NonSolarTermDay_ReturnsEmpty()
    {
        // 2023-05-01 不是节气日
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        info.GetSolarTerm().ShouldBeEmpty();
    }

    #endregion

    #region AddDays / Tomorrow / Yesterday

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.AddDays"/> 在 `PositiveDays` 场景下，结果为 `AdvancesDate`。
    /// </summary>
    [Fact]
    public void AddDays_PositiveDays_AdvancesDate()
    {
        var dt = new DateTime(2023, 5, 1);
        var info = new ChineseDateInfo(dt);
        var advanced = info.AddDays(3);
        advanced.ToDateTime().ShouldBe(dt.AddDays(3));
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.Tomorrow"/> 在 `BasicCase` 场景下，结果为 `AdvancesOneDay`。
    /// </summary>
    [Fact]
    public void Tomorrow_BasicCase_AdvancesOneDay()
    {
        var dt = new DateTime(2023, 5, 1);
        var info = new ChineseDateInfo(dt);
        info.Tomorrow().ToDateTime().ShouldBe(dt.AddDays(1));
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.Yesterday"/> 在 `BasicCase` 场景下，结果为 `SubtractsOneDay`。
    /// </summary>
    [Fact]
    public void Yesterday_BasicCase_SubtractsOneDay()
    {
        var dt = new DateTime(2023, 5, 1);
        var info = new ChineseDateInfo(dt);
        info.Yesterday().ToDateTime().ShouldBe(dt.AddDays(-1));
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.AddWorkDays"/> 在 `StartsOnFriday` 场景下，结果为 `SkipsWeekend`。
    /// 从周五加1工作日应跳过周末，到达下周一
    /// </summary>
    [Fact]
    public void AddWorkDays_StartsOnFriday_SkipsWeekend()
    {
        // 2023-05-05 是周五
        var dt = new DateTime(2023, 5, 5);
        var info = new ChineseDateInfo(dt);
        var nextWorkDay = info.AddWorkDays(1);
        // 下一个工作日应该是 2023-05-08 (周一)
        nextWorkDay.ToDateTime().ShouldBe(new DateTime(2023, 5, 8));
    }

    #endregion

    #region GetDaysInYear / GetDaysInMonth / DayOfYear

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.GetDaysInYear"/> 在 `LeapYear` 场景下，结果为 `ReturnsMoreDays`。
    /// 农历闰年(13个月)比普通年多天
    /// </summary>
    [Fact]
    public void GetDaysInYear_LeapYear_ReturnsMoreDaysThanNonLeapYear()
    {
        var leapYearInfo = new ChineseDateInfo(new DateTime(2020, 6, 1)); // 庚子年(闰年)
        var nonLeapYearInfo = new ChineseDateInfo(new DateTime(2021, 6, 1)); // 辛丑年(非闰年)
        leapYearInfo.GetDaysInYear().ShouldBeGreaterThan(nonLeapYearInfo.GetDaysInYear());
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.GetDaysInMonth"/> 在 `ValidDate` 场景下，结果为 `ReturnsReasonableDays`。
    /// </summary>
    [Fact]
    public void GetDaysInMonth_ValidDate_ReturnsReasonableDays()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        // 农历月份天数通常为 29 或 30
        info.GetDaysInMonth().ShouldBeInRange(29, 30);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.DayOfYear"/> 在 `ValidDate` 场景下，结果为 `ReturnsDayInRange`。
    /// </summary>
    [Fact]
    public void DayOfYear_ValidDate_ReturnsDayInRange()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        info.DayOfYear().ShouldBeInRange(1, 385);
    }

    #endregion

    #region Of / OfLunar

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.Of"/> 在 `ValidGregorianDate` 场景下，结果为 `ReturnsSameAsConstructor`。
    /// </summary>
    [Fact]
    public void Of_ValidGregorianDate_ReturnsSameAsConstructor()
    {
        var dt = new DateTime(2023, 5, 1);
        var fromOf = ChineseDateInfo.Of(2023, 5, 1);
        var fromCtor = new ChineseDateInfo(dt);
        fromOf.ToDateTime().ShouldBe(fromCtor.ToDateTime());
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.OfLunar"/> 在 `LanternFestival2023` 场景下，结果为 `ReturnsCorrectGregorianDate`。
    /// 2023年农历正月十五(元宵节)对应公历 2023-02-05
    /// </summary>
    [Fact]
    public void OfLunar_LanternFestival2023_ReturnsCorrectGregorianDate()
    {
        // 2023年癸卯年正月十五 = 公历 2023-02-05
        var info = ChineseDateInfo.OfLunar(2023, 1, 15);
        info.ToDateTime().ShouldBe(new DateTime(2023, 2, 5));
    }

    #endregion

    #region GetDayOfWeek

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseDateInfo.GetDayOfWeek"/> 在 `KnownMonday` 场景下，结果为 `ReturnsMonday`。
    /// </summary>
    [Fact]
    public void GetDayOfWeek_KnownMonday_ReturnsMonday()
    {
        // 2023-05-08 是周一
        var info = new ChineseDateInfo(new DateTime(2023, 5, 8));
        info.GetDayOfWeek().ShouldBe(DayOfWeek.Monday);
    }

    #endregion
}
