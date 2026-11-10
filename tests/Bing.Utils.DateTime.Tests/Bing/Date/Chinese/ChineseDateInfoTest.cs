using System.Globalization;

namespace Bing.Date.Chinese;

/// <summary>
/// 农历日期信息的转换、名称、判断及日期偏移测试。
/// </summary>
[Trait("DateTimeUT", "ChineseDate.DateInfo")]
public class ChineseDateInfoTest
{
    #region Constructor & Properties

    /// <summary>
    /// 验证默认历法构造的农历年月日均为正数。
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
    /// 验证指定历法构造的农历年月日与历法计算一致。
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
    /// 验证转换回公历后保留原日期。
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
    /// 验证二〇二〇年被判断为农历闰年。
    /// </summary>
    [Fact]
    public void IsLeapYear_LeapYear2020_ReturnsTrue()
    {
        var info = new ChineseDateInfo(new DateTime(2020, 6, 1));
        info.IsLeapYear().ShouldBeTrue();
    }

    /// <summary>
    /// 验证二〇二一年被判断为农历平年。
    /// </summary>
    [Fact]
    public void IsLeapYear_NonLeapYear_ReturnsFalse()
    {
        var info = new ChineseDateInfo(new DateTime(2021, 6, 1));
        info.IsLeapYear().ShouldBeFalse();
    }

    /// <summary>
    /// 验证当前映射下的闰月日期返回肯定结果。
    /// </summary>
    [Fact]
    public void IsLeapMonth_DateInLeapMonth_ReturnsTrue()
    {
        // 2020-05-25：公历month=5，对应农历年2020的第5个月槽位为闰四月
        var info = new ChineseDateInfo(new DateTime(2020, 5, 25));
        info.IsLeapMonth().ShouldBeTrue();
    }

    /// <summary>
    /// 验证当前映射下的非闰月日期返回否定结果。
    /// </summary>
    [Fact]
    public void IsLeapMonth_DateNotInLeapMonth_ReturnsFalse()
    {
        var info = new ChineseDateInfo(new DateTime(2020, 4, 1));
        info.IsLeapMonth().ShouldBeFalse();
    }

    /// <summary>
    /// 验证当前映射下的闰月日期被判断为闰日。
    /// </summary>
    [Fact]
    public void IsLeapDay_DayInLeapMonth_ReturnsTrue()
    {
        // 2020-05-25：公历month=5，对应农历年2020 month=5为闰四月，任意日均为闰日
        var info = new ChineseDateInfo(new DateTime(2020, 5, 25));
        info.IsLeapDay().ShouldBeTrue();
    }

    /// <summary>
    /// 验证当前映射下的普通日期不属于闰日。
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
    /// 验证星期六被判断为周末。
    /// </summary>
    [Fact]
    public void IsWeekend_Saturday_ReturnsTrue()
    {
        // 2023-05-06 是星期六
        var info = new ChineseDateInfo(new DateTime(2023, 5, 6));
        info.IsWeekend().ShouldBeTrue();
    }

    /// <summary>
    /// 验证星期一被判断为工作日。
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
    /// 验证中文年份名称非空。
    /// </summary>
    [Fact]
    public void GetChineseYear_ValidDate_ReturnsNonEmpty()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        info.GetChineseYear().ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证农历新年前夕及春节当天的对象年份采用农历年，并保持简繁体文本。
    /// </summary>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日。</param>
    /// <param name="lunarYear">预期农历年。</param>
    /// <param name="simplifiedYear">预期简体农历年文本。</param>
    /// <param name="traditionalYear">预期繁体农历年文本。</param>
    /// <param name="simplifiedDate">预期简体完整农历日期。</param>
    /// <param name="traditionalDate">预期繁体完整农历日期。</param>
    [Theory]
    [InlineData(2, 9, 2023, "二零二三年", "貳零貳叁年", "二零二三年腊月三十", "貳零貳叁年臘月叁拾")]
    [InlineData(2, 10, 2024, "二零二四年", "貳零貳肆年", "二零二四年正月初一", "貳零貳肆年正月初壹")]
    public void GetChineseYearAndDateWithYear_LunarNewYearBoundary_UsesLunarYear(
        int month,
        int day,
        int lunarYear,
        string simplifiedYear,
        string traditionalYear,
        string simplifiedDate,
        string traditionalDate)
    {
        var info = new ChineseDateInfo(new DateTime(2024, month, day));

        info.ChineseYear.ShouldBe(lunarYear);
        info.GetChineseYear().ShouldBe(simplifiedYear);
        info.GetChineseYear(traditionalChineseCharacters: true).ShouldBe(traditionalYear);
        info.GetChineseDateWithYear().ShouldBe(simplifiedDate);
        info.GetChineseDateWithYear(traditionalChineseCharacters: true).ShouldBe(traditionalDate);
    }

    /// <summary>
    /// 验证底层日期 helper 仍按公历年份输出中文年份。
    /// </summary>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日。</param>
    [Theory]
    [InlineData(2, 9)]
    [InlineData(2, 10)]
    public void ChineseDateHelperGetChineseYear_LunarNewYearBoundary_UsesGregorianYear(int month, int day)
    {
        var date = new DateTime(2024, month, day);

        ChineseDateHelper.GetChineseYear(date).ShouldBe("二零二四年");
        ChineseDateHelper.GetChineseYear(date, traditionalChineseCharacters: true).ShouldBe("貳零貳肆年");
    }

    /// <summary>
    /// 验证农历年份格式化拒绝空历法及超出历法范围的日期。
    /// </summary>
    [Fact]
    public void GetChineseYear_InvalidCalendarOrDate_IsRejected()
    {
        var noCalendar = new ChineseDateInfo(new DateTime(2024, 2, 9), null);
        var outsideRange = new ChineseDateInfo(ChineseLunarCalendar.MinDate.AddDays(-1));

        Should.Throw<ArgumentNullException>(() => noCalendar.GetChineseYear()).ParamName.ShouldBe("calendar");
        Should.Throw<ArgumentOutOfRangeException>(() => outsideRange.GetChineseYear()).ParamName.ShouldBe("dt");
    }

    /// <summary>
    /// 验证二〇二三年日期的干支年名称包含癸卯。
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
    /// 验证农历月份名称非空。
    /// </summary>
    [Fact]
    public void GetChineseMonth_ValidDate_ReturnsNonEmpty()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        info.GetChineseMonth().ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证农历日名称非空。
    /// </summary>
    [Fact]
    public void GetChineseDay_ValidDate_ReturnsNonEmpty()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        info.GetChineseDay().ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证农历日期文本由月份和日名称拼接而成。
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
    /// 验证完整农历日期以中文年份开头并包含后续文本。
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
    /// 验证立春当日返回立春名称。
    /// </summary>
    [Fact]
    public void GetSolarTerm_LiChunDay_ReturnsLiChun()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 2, 4));
        info.GetSolarTerm().ShouldBe("立春");
    }

    /// <summary>
    /// 验证非节气日期返回空文本。
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
    /// 验证按指定天数向后偏移日期。
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
    /// 验证明天对应下一天的公历日期。
    /// </summary>
    [Fact]
    public void Tomorrow_BasicCase_AdvancesOneDay()
    {
        var dt = new DateTime(2023, 5, 1);
        var info = new ChineseDateInfo(dt);
        info.Tomorrow().ToDateTime().ShouldBe(dt.AddDays(1));
    }

    /// <summary>
    /// 验证昨天对应前一天的公历日期。
    /// </summary>
    [Fact]
    public void Yesterday_BasicCase_SubtractsOneDay()
    {
        var dt = new DateTime(2023, 5, 1);
        var info = new ChineseDateInfo(dt);
        info.Yesterday().ToDateTime().ShouldBe(dt.AddDays(-1));
    }

    /// <summary>
    /// 验证从星期五增加一个工作日会跳过周末。
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
    /// 验证所选农历闰年的天数多于平年。
    /// </summary>
    [Fact]
    public void GetDaysInYear_LeapYear_ReturnsMoreDaysThanNonLeapYear()
    {
        var leapYearInfo = new ChineseDateInfo(new DateTime(2020, 6, 1)); // 庚子年(闰年)
        var nonLeapYearInfo = new ChineseDateInfo(new DateTime(2021, 6, 1)); // 辛丑年(非闰年)
        leapYearInfo.GetDaysInYear().ShouldBeGreaterThan(nonLeapYearInfo.GetDaysInYear());
    }

    /// <summary>
    /// 验证农历月份天数为二十九或三十天。
    /// </summary>
    [Fact]
    public void GetDaysInMonth_ValidDate_ReturnsReasonableDays()
    {
        var info = new ChineseDateInfo(new DateTime(2023, 5, 1));
        // 农历月份天数通常为 29 或 30
        info.GetDaysInMonth().ShouldBeInRange(29, 30);
    }

    /// <summary>
    /// 验证年、月天数按日期对应的农历年和农历月计算。
    /// </summary>
    [Fact]
    public void GetDaysInYearAndMonth_UsesLunarCalendarParts()
    {
        var info = new ChineseDateInfo(new DateTime(2024, 1, 31));

        info.ChineseYear.ShouldBe(2023);
        info.ChineseMonth.ShouldBe(13); // BCL 的闰月年按月份序号计数，腊月为第 13 月。
        info.GetDaysInYear().ShouldBe(384);
        info.GetDaysInMonth().ShouldBe(30);
    }

    /// <summary>
    /// 验证农历年内序号位于合理范围。
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
    /// 验证公历年月日工厂与构造函数结果一致。
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
    /// 验证二〇二三年元宵节的农历日期转换为二月五日。
    /// </summary>
    [Fact]
    public void OfLunar_LanternFestival2023_ReturnsCorrectGregorianDate()
    {
        // 2023年癸卯年正月十五 = 公历 2023-02-05
        var info = ChineseDateInfo.OfLunar(2023, 1, 15);
        info.ToDateTime().ShouldBe(new DateTime(2023, 2, 5));
    }

    /// <summary>
    /// 验证旧农历工厂的月份参数继续采用 BCL 月份槽位，闰二月占用第三槽。
    /// </summary>
    /// <param name="calendarMonth">BCL 农历月份槽位。</param>
    /// <param name="gregorianMonth">对应的公历月份。</param>
    /// <param name="gregorianDay">对应的公历日期。</param>
    /// <param name="isLeapMonth">是否为闰月槽位。</param>
    [Theory]
    [InlineData(2, 2, 20, false)]
    [InlineData(3, 3, 22, true)]
    [InlineData(4, 4, 20, false)]
    public void OfLunar_MonthSlot_PreservesLeapMonthContract(
        int calendarMonth, int gregorianMonth, int gregorianDay, bool isLeapMonth)
    {
        var info = ChineseDateInfo.OfLunar(2023, calendarMonth, 1);

        info.ToDateTime().ShouldBe(new DateTime(2023, gregorianMonth, gregorianDay));
        info.ChineseMonth.ShouldBe(calendarMonth);
        info.IsLeapMonth().ShouldBe(isLeapMonth);
    }

    #endregion

    #region GetDayOfWeek

    /// <summary>
    /// 验证已知星期一日期的星期值。
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
