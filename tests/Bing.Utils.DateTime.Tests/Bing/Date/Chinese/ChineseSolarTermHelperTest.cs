using System.Globalization;
namespace Bing.Date.Chinese;
/// <summary>
/// 二十四节气名称及前后节气定位测试。
/// </summary>
[Trait("DateTimeUT", "ChineseDate.SolarTerm")]
public class ChineseSolarTermHelperTest
{
    /// <summary>
    /// 节气计算使用的农历历法。
    /// </summary>
    private readonly ChineseLunisolarCalendar _calendar = new();
    /// <summary>
    /// 验证节气的简体和繁体中文名称。
    /// </summary>
    /// <param name="term">节气枚举值。</param>
    /// <param name="traditional">是否使用繁体文字。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(ChineseSolarTerms.BeginningOfSpring, false, "立春")]
    [InlineData(ChineseSolarTerms.VernalEquinox, false, "春分")]
    [InlineData(ChineseSolarTerms.QingmingFestival, true, "清明")]
    [InlineData(ChineseSolarTerms.TheWakingOfInsects, true, "驚蟄")]
    public void GetName_ValidTerms_ReturnsCorrectName(ChineseSolarTerms term, bool traditional, string expected)
    {
        // Act
        var result = ChineseSolarTermHelper.GetName(term, traditional);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证节气的英文名称。
    /// </summary>
    /// <param name="term">节气枚举值。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(ChineseSolarTerms.BeginningOfSpring, "Beginning of Spring")]
    [InlineData(ChineseSolarTerms.VernalEquinox, "Vernal Equinox")]
    [InlineData(ChineseSolarTerms.QingmingFestival, "Qingming Festival")]
    [InlineData(ChineseSolarTerms.SummerSolstice, "Summer Solstice")]
    public void GetEnglishName_ValidTerms_ReturnsCorrectName(ChineseSolarTerms term, string expected)
    {
        // Act
        var result = ChineseSolarTermHelper.GetEnglishName(term);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证节气当日返回对应名称。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    /// <param name="traditional">是否使用繁体文字。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, 2, 4, false, "立春")] // 2023年立春：2月4日
    [InlineData(2023, 3, 21, false, "春分")] // 2023年春分：3月21日
    [InlineData(2023, 6, 21, false, "夏至")] // 2023年夏至：6月21日
    [InlineData(2023, 12, 22, false, "冬至")] // 2023年冬至：12月22日
    public void GetSolarTerm_TermDay_ReturnsSolarTermName(int year, int month, int day, bool traditional, string expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseSolarTermHelper.GetSolarTerm(_calendar, date, traditional);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证非节气日期返回空字符串。
    /// </summary>
    [Fact]
    public void GetSolarTerm_NonTermDay_ReturnsEmptyString()
    {
        // Arrange
        var date = new DateTime(2023, 2, 10); // 非节气日
        // Act
        var result = ChineseSolarTermHelper.GetSolarTerm(_calendar, date);
        // Assert
        result.ShouldBe(string.Empty);
    }
    /// <summary>
    /// 验证上一个节气的名称及日期早于查询日期。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, 2, 10, "立春")] // 立春后几天，上一个是立春
    [InlineData(2023, 3, 25, "春分")] // 春分后几天，上一个是春分
    public void GetLastSolarTerm_ValidDate_ReturnsLastTerm(int year, int month, int day, string expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseSolarTermHelper.GetLastSolarTerm(_calendar, date, out var termDate);
        // Assert
        result.ShouldBe(expected);
        termDate.ShouldNotBe(default);
        termDate.ShouldBeLessThan(date);
    }
    /// <summary>
    /// 验证下一个节气的名称及日期晚于查询日期。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, 1, 25, "立春")] // 立春前几天，下一个是立春
    [InlineData(2023, 3, 15, "春分")] // 春分前几天，下一个是春分
    public void GetNextSolarTerm_ValidDate_ReturnsNextTerm(int year, int month, int day, string expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseSolarTermHelper.GetNextSolarTerm(_calendar, date, out var termDate);
        // Assert
        result.ShouldBe(expected);
        termDate.ShouldNotBe(default);
        termDate.ShouldBeGreaterThan(date);
    }
    /// <summary>
    /// 验证节气当日返回对应枚举值。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, 2, 4, ChineseSolarTerms.BeginningOfSpring)] // 2023年立春
    [InlineData(2023, 3, 21, ChineseSolarTerms.VernalEquinox)] // 2023年春分
    public void GetSolarTermEnum_TermDay_ReturnsSolarTermEnum(int year, int month, int day, ChineseSolarTerms expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseSolarTermHelper.GetSolarTermEnum(_calendar, date);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证上一个节气的枚举值及日期。
    /// </summary>
    [Fact]
    public void GetLastSolarTermEnum_ValidDate_ReturnsLastTermEnum()
    {
        // Arrange
        var date = new DateTime(2023, 2, 10); // 立春后几天
        // Act
        var result = ChineseSolarTermHelper.GetLastSolarTermEnum(_calendar, date, out var termDate);
        // Assert
        result.ShouldBe(ChineseSolarTerms.BeginningOfSpring);
        termDate.ShouldNotBe(default);
        termDate.ShouldBeLessThan(date);
    }
    /// <summary>
    /// 验证下一个节气的枚举值及日期。
    /// </summary>
    [Fact]
    public void GetNextSolarTermEnum_ValidDate_ReturnsNextTermEnum()
    {
        // Arrange
        var date = new DateTime(2023, 3, 15); // 春分前几天
        // Act
        var result = ChineseSolarTermHelper.GetNextSolarTermEnum(_calendar, date, out var termDate);
        // Assert
        result.ShouldBe(ChineseSolarTerms.VernalEquinox);
        termDate.ShouldNotBe(default);
        termDate.ShouldBeGreaterThan(date);
    }
    /// <summary>
    /// 验证节气查询拒绝空历法。
    /// </summary>
    [Fact]
    public void GetSolarTerm_NullCalendar_ThrowsArgumentNullException()
    {
        // Arrange
        var date = new DateTime(2023, 2, 4);
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => ChineseSolarTermHelper.GetSolarTerm(null, date));
    }
    /// <summary>
    /// 验证支持范围外的日期返回空字符串。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    [Theory]
    [InlineData(1800, 1, 1)] // 早于基准日期
    [InlineData(2200, 1, 1)] // 超出最大支持日期
    public void GetSolarTerm_DateOutOfRange_ReturnsEmptyString(int year, int month, int day)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseSolarTermHelper.GetSolarTerm(_calendar, date);
        // Assert
        result.ShouldBe(string.Empty);
    }
}
