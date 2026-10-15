using System.Globalization;
namespace Bing.Date.Chinese;
/// <summary>
/// 农历日期名称、时辰和闰年判断测试。
/// </summary>
[Trait("DateTimeUT", "ChineseDate")]
public class ChineseDateHelperTest
{
    /// <summary>
    /// 农历日期计算使用的历法。
    /// </summary>
    private readonly ChineseLunisolarCalendar _calendar = new();
    /// <summary>
    /// 验证公历年份转换为简体或繁体中文数字。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="traditional">是否使用繁体文字。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, false, "二零二三年")]
    [InlineData(2023, true, "貳零貳叁年")]
    [InlineData(2000, false, "二零零零年")]
    [InlineData(1990, true, "壹玖玖零年")]
    public void GetChineseYear_ValidDate_ReturnsCorrectChineseYear(int year, bool traditional, string expected)
    {
        // Arrange
        var date = new DateTime(year, 1, 1);
        // Act
        var result = ChineseDateHelper.GetChineseYear(date, traditional);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证指定日期的干支年名称。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    /// <param name="traditional">是否使用繁体文字。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, 1, 22, false, "癸卯年")] // 兔年
    [InlineData(2022, 2, 1, false, "壬寅年")] // 虎年
    [InlineData(2021, 3, 1, false, "辛丑年")] // 牛年
    [InlineData(2020, 4, 1, false, "庚子年")] // 鼠年
    public void GetSexagenaryYear_ValidDate_ReturnsCorrectSexagenaryYear(int year, int month, int day, bool traditional, string expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseDateHelper.GetSexagenaryYear(_calendar, date, traditional);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证获取干支年时拒绝空历法。
    /// </summary>
    [Fact]
    public void GetSexagenaryYear_NullCalendar_ThrowsArgumentNullException()
    {
        // Arrange
        var date = new DateTime(2023, 1, 1);
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => ChineseDateHelper.GetSexagenaryYear(null, date));
    }
    /// <summary>
    /// 验证指定日期的农历月份名称。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    /// <param name="traditional">是否使用繁体文字。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, 1, 22, false, "正月")] // 农历正月
    [InlineData(2023, 2, 20, false, "二月")] // 农历二月
    [InlineData(2023, 6, 19, false, "五月")] // 农历五月
    public void GetChineseMonth_ValidDate_ReturnsCorrectChineseMonth(int year, int month, int day, bool traditional, string expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseDateHelper.GetChineseMonth(_calendar, date, traditional);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证闰月名称的简体和繁体格式。
    /// </summary>
    [Fact]
    public void GetChineseMonth_LeapMonth_ReturnsCorrectLeapMonthName()
    {
        // Arrange - 2023年闰二月开始于公历2023年3月22日
        var date = new DateTime(2023, 3, 25);
        // Act
        var result = ChineseDateHelper.GetChineseMonth(_calendar, date, false);
        var traditional = ChineseDateHelper.GetChineseMonth(_calendar, date, true);
        // Assert
        result.ShouldBe("闰二月");
        traditional.ShouldBe("閏貳月");
    }
    /// <summary>
    /// 验证指定日期的农历日名称。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    /// <param name="traditional">是否使用繁体文字。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, 1, 22, false, "初一")] // 农历正月初一
    [InlineData(2023, 1, 31, false, "初十")] // 农历正月初十
    [InlineData(2023, 3, 21, false, "三十")] // 农历二月三十
    [InlineData(2023, 2, 10, false, "二十")] // 农历正月二十
    [InlineData(2023, 2, 15, false, "廿五")] // 农历正月廿五
    public void GetChineseDay_ValidDate_ReturnsCorrectChineseDay(int year, int month, int day, bool traditional, string expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseDateHelper.GetChineseDay(_calendar, date, traditional);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证指定时刻的简体或繁体时辰名称。
    /// </summary>
    /// <param name="hour">小时。</param>
    /// <param name="minute">分钟。</param>
    /// <param name="traditional">是否使用繁体文字。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(0, 0, false, "子时")] // 子时 (23:00-1:00)
    [InlineData(1, 0, false, "子时")]
    [InlineData(2, 0, false, "丑时")] // 丑时 (1:00-3:00)
    [InlineData(4, 0, false, "寅时")] // 寅时 (3:00-5:00)
    [InlineData(6, 0, false, "卯时")] // 卯时 (5:00-7:00)
    [InlineData(8, 0, false, "辰时")] // 辰时 (7:00-9:00)
    [InlineData(10, 0, false, "巳时")] // 巳时 (9:00-11:00)
    [InlineData(12, 0, false, "午时")] // 午时 (11:00-13:00)
    [InlineData(14, 0, false, "未时")] // 未时 (13:00-15:00)
    [InlineData(16, 0, false, "申时")] // 申时 (15:00-17:00)
    [InlineData(18, 0, false, "酉时")] // 酉时 (17:00-19:00)
    [InlineData(20, 0, false, "戌时")] // 戌时 (19:00-21:00)
    [InlineData(22, 0, false, "亥时")] // 亥时 (21:00-23:00)
    public void GetChineseHour_ValidTime_ReturnsCorrectChineseHour(int hour, int minute, bool traditional, string expected)
    {
        // Arrange
        var date = new DateTime(2023, 1, 1, hour, minute, 0);
        // Act
        var result = ChineseDateHelper.GetChineseHour(date, traditional);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证零点和非整点时刻的时辰名称。
    /// </summary>
    [Fact]
    public void GetChineseHour_MinuteNotZero_AdvancesToNextHour()
    {
        // Arrange
        var date1 = new DateTime(2023, 1, 1, 0, 0, 0); // 子时
        var date2 = new DateTime(2023, 1, 1, 2, 1, 0); // 子时进位到丑时
        // Act
        var result1 = ChineseDateHelper.GetChineseHour(date1, false);
        var result2 = ChineseDateHelper.GetChineseHour(date2, false);
        // Assert
        result1.ShouldBe("子时");
        result2.ShouldBe("丑时");
    }
    /// <summary>
    /// 验证完整农历日期文本可选择包含时辰。
    /// </summary>
    [Fact]
    public void GetChineseDateTime_ValidDate_ReturnsFullChineseDate()
    {
        // Arrange
        var date = new DateTime(2023, 1, 22, 8, 30, 0); // 农历正月初一 辰时
        // Act
        var result = ChineseDateHelper.GetChineseDateTime(_calendar, date, true, false);
        var resultNoHour = ChineseDateHelper.GetChineseDateTime(_calendar, date, false, false);
        // Assert
        result.ShouldBe("癸卯年正月初一辰时");
        resultNoHour.ShouldBe("癸卯年正月初一");
    }
    /// <summary>
    /// 验证日期与年份重载的农历闰年判断结果一致。
    /// </summary>
    /// <param name="year">待判断的年份。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, true)] // 2023农历闰二月
    [InlineData(2022, false)]
    [InlineData(2020, true)] // 2020农历闰四月
    public void IsLeapYear_ValidYear_ReturnsCorrectResult(int year, bool expected)
    {
        // Arrange
        var date = new DateTime(year, 1, 1);
        // Act
        var result = ChineseDateHelper.IsLeapYear(_calendar, date);
        var resultInt = ChineseDateHelper.IsLeapYear(_calendar, year);
        // Assert
        result.ShouldBe(expected);
        resultInt.ShouldBe(expected);
    }
    /// <summary>
    /// 验证指定日期的农历闰月判断。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, 3, 25, true)] // 2023年闰二月
    [InlineData(2023, 1, 22, false)]
    [InlineData(2020, 5, 25, true)] // 2020年闰四月
    public void IsLeapMonth_ValidDate_ReturnsCorrectResult(int year, int month, int day, bool expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseDateHelper.IsLeapMonth(_calendar, date);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证普通农历日期不属于闰日。
    /// </summary>
    [Fact]
    public void IsLeapDay_ValidDate_ReturnsCorrectResult()
    {
        // Arrange
        var regularDay = new DateTime(2023, 1, 22);
        // Act & Assert
        ChineseDateHelper.IsLeapDay(_calendar, regularDay).ShouldBeFalse();
    }
}
