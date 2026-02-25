using System.Globalization;
namespace Bing.Date.Chinese;
/// <summary>
/// 中国二十四节气帮助类测试
/// </summary>
[Trait("DateTimeUT", "ChineseDate.SolarTerm")]
public class ChineseSolarTermHelperTest
{
    /// <summary>
    /// 中国农历日历实例
    /// </summary>
    private readonly ChineseLunisolarCalendar _calendar = new();
    /// <summary>
    /// 测试 - GetName - 获取节气的中文名称
    /// </summary>
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
    /// 测试 - GetEnglishName - 获取节气的英文名称
    /// </summary>
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
    /// 测试 - GetSolarTerm - 获取指定日期的节气
    /// </summary>
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
    /// 测试 - GetSolarTerm - 非节气日期返回空字符串
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
    /// 测试 - GetLastSolarTerm - 获取上一个节气
    /// </summary>
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
    /// 测试 - GetNextSolarTerm - 获取下一个节气
    /// </summary>
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
    /// 测试 - GetSolarTermEnum - 获取节气枚举
    /// </summary>
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
    /// 测试 - GetLastSolarTermEnum - 获取上一个节气枚举
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
    /// 测试 - GetNextSolarTermEnum - 获取下一个节气枚举
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
    /// 测试 - 无效日期参数 - 抛出异常
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
    /// 测试 - 日期超出范围 - 返回空结果
    /// </summary>
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
