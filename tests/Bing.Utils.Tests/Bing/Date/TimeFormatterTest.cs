using System.Globalization;
namespace Bing.Date;
/// <summary>
/// 日期/时间格式化工具类 测试类
/// </summary>
public class TimeFormatterTest
{
    /// <summary>
    /// 测试 - Format(DateTime) 方法
    /// </summary>
    [Fact]
    public void Test_Format_DateTime_Default()
    {
        // Arrange
        var dateTime = new DateTime(2025, 6, 18, 17, 20, 1);
        // Act
        var result = TimeFormatter.Format(dateTime);
        // Assert
        Assert.Equal("2025-06-18 17:20:01", result);
    }
    /// <summary>
    /// 测试 - Format(DateTime, TimeFormatType) 方法
    /// </summary>
    [Fact]
    public void Test_Format_DateTime_WithFormatType()
    {
        // Arrange
        var dateTime = new DateTime(2025, 6, 18, 17, 20, 1);
        // Act & Assert
        Assert.Equal("2025-06-18", TimeFormatter.Format(dateTime, TimeFormatType.NormDate));
        Assert.Equal("17:20:01", TimeFormatter.Format(dateTime, TimeFormatType.NormTime));
        Assert.Equal("2025年06月18日", TimeFormatter.Format(dateTime, TimeFormatType.ChineseDate));
        Assert.Equal("20250618", TimeFormatter.Format(dateTime, TimeFormatType.PureDate));
        Assert.Equal("2025_06_18", TimeFormatter.Format(dateTime, TimeFormatType.UnderlineDate));
    }
    /// <summary>
    /// 测试 - Format(DateTime, TimeFormatType, CultureInfo) 方法
    /// </summary>
    [Fact]
    public void Test_Format_DateTime_WithFormatTypeAndCulture()
    {
        // Arrange
        var dateTime = new DateTime(2025, 6, 18, 17, 20, 1);
        var culture = new CultureInfo("en-US");
        // Act
        var result = TimeFormatter.Format(dateTime, TimeFormatType.NormDateTime, culture);
        // Assert
        Assert.Equal("2025-06-18 17:20:01", result);
    }
    /// <summary>
    /// 测试 - Format(DateTime?) 方法 - 有值的情况
    /// </summary>
    [Fact]
    public void Test_Format_NullableDateTime_WithValue()
    {
        // Arrange
        DateTime? dateTime = new DateTime(2025, 6, 18, 17, 20, 1);
        // Act
        var result = TimeFormatter.Format(dateTime);
        // Assert
        Assert.Equal("2025-06-18 17:20:01", result);
    }
    /// <summary>
    /// 测试 - Format(DateTime?) 方法 - null 的情况
    /// </summary>
    [Fact]
    public void Test_Format_NullableDateTime_Null()
    {
        // Arrange
        DateTime? dateTime = null;
        // Act
        var result = TimeFormatter.Format(dateTime);
        // Assert
        Assert.Equal(string.Empty, result);
    }
    /// <summary>
    /// 测试 - Format(DateTime?, TimeFormatType) 方法
    /// </summary>
    [Fact]
    public void Test_Format_NullableDateTime_WithFormatType()
    {
        // Arrange
        DateTime? dateTime = new DateTime(2025, 6, 18, 17, 20, 1);
        // Act & Assert
        Assert.Equal("2025-06-18", TimeFormatter.Format(dateTime, TimeFormatType.NormDate));
        Assert.Equal(string.Empty, TimeFormatter.Format((DateTime?)null, TimeFormatType.NormDate));
    }
    /// <summary>
    /// 测试 - Format(DateTime?, TimeFormatType, CultureInfo) 方法
    /// </summary>
    [Fact]
    public void Test_Format_NullableDateTime_WithFormatTypeAndCulture()
    {
        // Arrange
        DateTime? dateTime = new DateTime(2025, 6, 18, 17, 20, 1);
        var culture = new CultureInfo("en-US");
        // Act & Assert
        Assert.Equal("2025-06-18 17:20:01", TimeFormatter.Format(dateTime, TimeFormatType.NormDateTime, culture));
        Assert.Equal(string.Empty, TimeFormatter.Format((DateTime?)null, TimeFormatType.NormDateTime, culture));
    }
    /// <summary>
    /// 测试 - Now() 方法
    /// </summary>
    [Fact]
    public void Test_Now()
    {
        // Act
        var result = TimeFormatter.Now();
        // 不直接比较完整值，因为测试运行时间不确定，只检查格式是否符合预期
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$", result);
    }
    /// <summary>
    /// 测试 - Now(TimeFormatType) 方法
    /// </summary>
    [Fact]
    public void Test_Now_WithFormatType()
    {
        // Act & Assert - 只检查格式是否符合预期
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", TimeFormatter.Now(TimeFormatType.NormDate));
        Assert.Matches(@"^\d{2}:\d{2}:\d{2}$", TimeFormatter.Now(TimeFormatType.NormTime));
        Assert.Matches(@"^\d{8}$", TimeFormatter.Now(TimeFormatType.PureDate));
    }
    /// <summary>
    /// 测试 - Now(TimeFormatType, CultureInfo) 方法
    /// </summary>
    [Fact]
    public void Test_Now_WithFormatTypeAndCulture()
    {
        // Arrange
        var culture = new CultureInfo("en-US");
        // Act & Assert - 只检查格式是否符合预期
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$", TimeFormatter.Now(TimeFormatType.NormDateTime, culture));
    }
    /// <summary>
    /// 测试 - Register 方法
    /// </summary>
    [Fact]
    public void Test_Register()
    {
        // Arrange
        var customFormat = "yyyy/MM/dd";
        var customType = TimeFormatType.NormDate;
        var dateTime = new DateTime(2025, 6, 18);
        try
        {
            // Act
            TimeFormatter.Register(customType, customFormat);
            var result = TimeFormatter.Format(dateTime, customType);
            // Assert
            Assert.Equal("2025/06/18", result);
        }
        finally
        {
            // 恢复原始格式，避免影响其他测试
            TimeFormatter.Register(customType, "yyyy-MM-dd");
        }
    }
    /// <summary>
    /// 测试 - Register 方法 - 参数异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Test_Register_WithInvalidPattern_ThrowsArgumentException(string pattern)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            TimeFormatter.Register(TimeFormatType.Default, pattern));
        Assert.Equal("pattern", exception.ParamName);
    }
    /// <summary>
    /// 测试 - GetPattern 方法
    /// </summary>
    [Fact]
    public void Test_GetPattern()
    {
        // Act & Assert
        Assert.Equal("yyyy-MM-dd HH:mm:ss", TimeFormatter.GetPattern(TimeFormatType.Default));
        Assert.Equal("yyyy-MM-dd", TimeFormatter.GetPattern(TimeFormatType.NormDate));
        Assert.Equal("yyyy年MM月dd日", TimeFormatter.GetPattern(TimeFormatType.ChineseDate));
        Assert.Equal("yyyyMMdd", TimeFormatter.GetPattern(TimeFormatType.PureDate));
    }
    /// <summary>
    /// 测试 - TryParsePattern 方法 - 成功情况
    /// </summary>
    [Fact]
    public void Test_TryParsePattern_Success()
    {
        // Arrange
        var pattern = "yyyy-MM-dd";
        // Act
        var success = TimeFormatter.TryParsePattern(pattern, out var type);
        // Assert
        Assert.True(success);
        Assert.Equal(TimeFormatType.NormDate, type);
    }
    /// <summary>
    /// 测试 - TryParsePattern 方法 - 不区分大小写
    /// </summary>
    [Fact]
    public void Test_TryParsePattern_CaseInsensitive()
    {
        // Arrange
        var pattern = "YYYY-MM-DD"; // 全大写
        // Act
        var success = TimeFormatter.TryParsePattern(pattern, out var type);
        // Assert
        Assert.True(success);
        Assert.Equal(TimeFormatType.NormDate, type);
    }
    /// <summary>
    /// 测试 - TryParsePattern 方法 - 失败情况
    /// </summary>
    [Fact]
    public void Test_TryParsePattern_Failure()
    {
        // Arrange
        var pattern = "yyyy/MM/dd"; // 不存在的格式
        // Act
        var success = TimeFormatter.TryParsePattern(pattern, out var type);
        // Assert
        Assert.False(success);
        Assert.Equal(default(TimeFormatType), type);
    }
    /// <summary>
    /// 测试特殊时间格式类型
    /// </summary>
    [Fact]
    public void Test_SpecialFormatTypes()
    {
        // Arrange
        var dateTime = new DateTime(2025, 6, 18, 17, 20, 1, 123);
        // Act & Assert - 毫秒格式测试
        Assert.Equal("17:20:01.123", TimeFormatter.Format(dateTime, TimeFormatType.NormTimeMs));
        Assert.Equal("2025-06-18 17:20:01.123", TimeFormatter.Format(dateTime, TimeFormatType.NormDateTimeMs));
        Assert.Equal("17时20分01秒.123", TimeFormatter.Format(dateTime, TimeFormatType.ChineseTimeMs));
        Assert.Equal("172001123", TimeFormatter.Format(dateTime, TimeFormatType.PureTimeMs));
        // 时分格式测试
        Assert.Equal("2025-06-18 17:20", TimeFormatter.Format(dateTime, TimeFormatType.NormDateTimeMinute));
        Assert.Equal("2025年06月18日 17时20分", TimeFormatter.Format(dateTime, TimeFormatType.ChineseDateTimeMinute));
        Assert.Equal("202506181720", TimeFormatter.Format(dateTime, TimeFormatType.PureDateTimeMinute));
        // 年月格式测试
        Assert.Equal("2025-06", TimeFormatter.Format(dateTime, TimeFormatType.NormMonth));
        Assert.Equal("2025年06月", TimeFormatter.Format(dateTime, TimeFormatType.ChineseMonth));
        Assert.Equal("202506", TimeFormatter.Format(dateTime, TimeFormatType.PureMonth));
        // 年格式测试
        Assert.Equal("2025", TimeFormatter.Format(dateTime, TimeFormatType.NormYear));
        Assert.Equal("2025年", TimeFormatter.Format(dateTime, TimeFormatType.ChineseYear));
        Assert.Equal("2025", TimeFormatter.Format(dateTime, TimeFormatType.PureYear));
    }
    /// <summary>
    /// 测试不同区域文化下的格式化
    /// </summary>
    [Fact]
    public void Test_DifferentCultures()
    {
        // Arrange
        var dateTime = new DateTime(2025, 6, 18, 17, 20, 1);
        var usCulture = new CultureInfo("en-US");
        var frCulture = new CultureInfo("fr-FR"); // 法国
        var jpCulture = new CultureInfo("ja-JP"); // 日本
        // Act & Assert
        // 不同文化下的日期时间格式应该保持一致（因为我们使用的是固定格式字符串）
        Assert.Equal("2025-06-18 17:20:01", TimeFormatter.Format(dateTime, TimeFormatType.Default, usCulture));
        Assert.Equal("2025-06-18 17:20:01", TimeFormatter.Format(dateTime, TimeFormatType.Default, frCulture));
        Assert.Equal("2025-06-18 17:20:01", TimeFormatter.Format(dateTime, TimeFormatType.Default, jpCulture));
    }
}

