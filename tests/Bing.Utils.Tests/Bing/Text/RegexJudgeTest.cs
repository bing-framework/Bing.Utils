namespace Bing.Text;
/// <summary>
/// 正则表达式检查器测试
/// </summary>
[Trait("TextUT", "RegexJudge")]
public class RegexJudgeTest
{
    #region 基础功能测试
    /// <summary>
    /// 测试 - IsMatch - 基本字符串匹配
    /// </summary>
    [Theory]
    [InlineData("hello", "hello", true)]
    [InlineData("hello", "Hello", true)]   // 默认忽略大小写
    [InlineData("hello", "HELLO", true)]   // 默认忽略大小写
    [InlineData("hello", "world", false)]
    [InlineData("hello world", "hello", true)]
    [InlineData("hello world", "world", true)]
    [InlineData("hello world", "test", false)]
    public void IsMatch_BasicStringMatching_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsMatch - 带RegexOptions参数
    /// </summary>
    [Theory]
    [InlineData("hello", "Hello", RegexOptions.None, false)]           // 区分大小写
    [InlineData("hello", "Hello", RegexOptions.IgnoreCase, true)]      // 忽略大小写
    [InlineData("hello", "HELLO", RegexOptions.None, false)]           // 区分大小写
    [InlineData("hello", "HELLO", RegexOptions.IgnoreCase, true)]      // 忽略大小写
    [InlineData("Hello World", "hello", RegexOptions.IgnoreCase, true)]
    [InlineData("Hello World", "hello", RegexOptions.None, false)]
    public void IsMatch_WithRegexOptions_ReturnsExpectedResult(string input, string pattern, RegexOptions options, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern, options);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 正则表达式模式测试
    /// <summary>
    /// 测试 - IsMatch - 数字正则表达式
    /// </summary>
    [Theory]
    [InlineData("123", @"\d+", true)]
    [InlineData("abc", @"\d+", false)]
    [InlineData("a1b2c3", @"\d+", true)]
    [InlineData("abc123", @"\d+", true)]
    [InlineData("123abc", @"\d+", true)]
    [InlineData("", @"\d+", false)]
    [InlineData("   ", @"\d+", false)]
    public void IsMatch_DigitPattern_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsMatch - 字母正则表达式
    /// </summary>
    [Theory]
    [InlineData("abc", @"[a-zA-Z]+", true)]
    [InlineData("ABC", @"[a-zA-Z]+", true)]
    [InlineData("123", @"[a-zA-Z]+", false)]
    [InlineData("a1b2c3", @"[a-zA-Z]+", true)]
    [InlineData("123abc", @"[a-zA-Z]+", true)]
    [InlineData("", @"[a-zA-Z]+", false)]
    public void IsMatch_LetterPattern_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsMatch - 邮箱正则表达式
    /// </summary>
    [Theory]
    [InlineData("user@example.com", @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", true)]
    [InlineData("test.email@domain.co.uk", @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", true)]
    [InlineData("invalid-email", @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", false)]
    [InlineData("user@", @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", false)]
    [InlineData("@example.com", @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", false)]
    public void IsMatch_EmailPattern_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsMatch - 手机号正则表达式
    /// </summary>
    [Theory]
    [InlineData("13812345678", @"^1[3-9]\d{9}$", true)]
    [InlineData("15912345678", @"^1[3-9]\d{9}$", true)]
    [InlineData("18612345678", @"^1[3-9]\d{9}$", true)]
    [InlineData("12812345678", @"^1[3-9]\d{9}$", false)]  // 无效号段
    [InlineData("1381234567", @"^1[3-9]\d{9}$", false)]   // 位数不够
    [InlineData("138123456789", @"^1[3-9]\d{9}$", false)] // 位数太多
    public void IsMatch_MobilePattern_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsMatch - 中文字符正则表达式
    /// </summary>
    [Theory]
    [InlineData("中文", @"[\u4e00-\u9fa5]+", true)]
    [InlineData("测试", @"[\u4e00-\u9fa5]+", true)]
    [InlineData("Hello", @"[\u4e00-\u9fa5]+", false)]
    [InlineData("中文ABC", @"[\u4e00-\u9fa5]+", true)]   // 包含中文
    [InlineData("ABC中文", @"[\u4e00-\u9fa5]+", true)]   // 包含中文
    [InlineData("123", @"[\u4e00-\u9fa5]+", false)]
    public void IsMatch_ChinesePattern_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 边界条件测试
    /// <summary>
    /// 测试 - IsMatch - 空值和null处理
    /// </summary>
    [Theory]
    [InlineData("", @"\d+", false)]
    [InlineData("   ", @"\d+", false)]
    [InlineData("test", "", true)]           // 空模式匹配任何字符串
    [InlineData("", "", true)]               // 空字符串匹配空模式
    public void IsMatch_EmptyAndWhitespace_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsMatch - null输入处理
    /// </summary>
    [Fact]
    public void IsMatch_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RegexJudge.IsMatch(null, @"\d+"));
    }
    /// <summary>
    /// 测试 - IsMatch - null模式处理
    /// </summary>
    [Fact]
    public void IsMatch_WithNullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RegexJudge.IsMatch("test", null));
    }
    /// <summary>
    /// 测试 - IsMatch - 无效正则表达式模式
    /// </summary>
    [Theory]
    [InlineData("test", "[")]           // 不完整的字符类
    [InlineData("test", "*")]           // 无效的重复符
    [InlineData("test", "?")]           // 无效的量词
    [InlineData("test", "(")]           // 不完整的分组
    public void IsMatch_WithInvalidPattern_ThrowsArgumentException(string input, string pattern)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => RegexJudge.IsMatch(input, pattern));
    }
    #endregion
    #region 复杂正则表达式测试
    /// <summary>
    /// 测试 - IsMatch - URL正则表达式
    /// </summary>
    [Theory]
    [InlineData("http://www.example.com", @"^https?://[\w\-]+(\.[\w\-]+)+([\w\-\.,@?^=%&:/~\+#]*[\w\-\@?^=%&/~\+#])?$", true)]
    [InlineData("https://example.com/path", @"^https?://[\w\-]+(\.[\w\-]+)+([\w\-\.,@?^=%&:/~\+#]*[\w\-\@?^=%&/~\+#])?$", true)]
    [InlineData("ftp://files.example.com", @"^https?://[\w\-]+(\.[\w\-]+)+([\w\-\.,@?^=%&:/~\+#]*[\w\-\@?^=%&/~\+#])?$", false)]
    [InlineData("www.example.com", @"^https?://[\w\-]+(\.[\w\-]+)+([\w\-\.,@?^=%&:/~\+#]*[\w\-\@?^=%&/~\+#])?$", false)]
    public void IsMatch_UrlPattern_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsMatch - 日期格式正则表达式
    /// </summary>
    [Theory]
    [InlineData("2023-01-01", @"^\d{4}-\d{2}-\d{2}$", true)]
    [InlineData("2023/01/01", @"^\d{4}/\d{2}/\d{2}$", true)]
    [InlineData("01-01-2023", @"^\d{2}-\d{2}-\d{4}$", true)]
    [InlineData("2023-1-1", @"^\d{4}-\d{2}-\d{2}$", false)]      // 不符合格式
    [InlineData("invalid-date", @"^\d{4}-\d{2}-\d{2}$", false)]
    public void IsMatch_DatePattern_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsMatch - IP地址正则表达式
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", true)]
    [InlineData("10.0.0.1", @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", true)]
    [InlineData("255.255.255.255", @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", true)]
    [InlineData("256.1.1.1", @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", false)]
    [InlineData("192.168.1", @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", false)]
    public void IsMatch_IpPattern_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 多种RegexOptions测试
    /// <summary>
    /// 测试 - IsMatch - 多行模式
    /// </summary>
    [Theory]
    [InlineData("line1\nline2", @"^line2$", RegexOptions.Multiline, true)]
    [InlineData("line1\nline2", @"^line2$", RegexOptions.None, false)]
    [InlineData("line1\r\nline2", @"^line2$", RegexOptions.Multiline, true)]
    public void IsMatch_MultilineOptions_ReturnsExpectedResult(string input, string pattern, RegexOptions options, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern, options);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsMatch - 单行模式
    /// </summary>
    [Theory]
    [InlineData("line1\nline2", @"line1.line2", RegexOptions.Singleline, true)]
    [InlineData("line1\nline2", @"line1.line2", RegexOptions.None, false)]
    public void IsMatch_SinglelineOptions_ReturnsExpectedResult(string input, string pattern, RegexOptions options, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern, options);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsMatch - 组合选项
    /// </summary>
    [Theory]
    [InlineData("Hello\nWORLD", @"hello.*world", RegexOptions.IgnoreCase | RegexOptions.Singleline, true)]
    [InlineData("Hello\nWORLD", @"hello.*world", RegexOptions.IgnoreCase, false)]
    [InlineData("Hello\nWORLD", @"hello.*world", RegexOptions.Singleline, false)]
    [InlineData("Hello\nWORLD", @"hello.*world", RegexOptions.None, false)]
    public void IsMatch_CombinedOptions_ReturnsExpectedResult(string input, string pattern, RegexOptions options, bool expected)
    {
        // Act
        var result = RegexJudge.IsMatch(input, pattern, options);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 性能和压力测试
    /// <summary>
    /// 测试 - IsMatch - 性能测试
    /// </summary>
    [Fact]
    public void IsMatch_Performance_ShouldCompleteInReasonableTime()
    {
        // Arrange
        var testData = new[]
        {
            ("user@example.com", @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"),
            ("13812345678", @"^1[3-9]\d{9}$"),
            ("192.168.1.1", @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"),
            ("中文测试", @"[\u4e00-\u9fa5]+"),
            ("Hello World", @"[a-zA-Z\s]+")
        };
        // Act & Assert - 应该在合理时间内完成
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                foreach (var (input, pattern) in testData)
                {
                    RegexJudge.IsMatch(input, pattern);
                }
            }
        }, TimeSpan.FromSeconds(2)); // 应该在2秒内完成
    }
    /// <summary>
    /// 测试 - IsMatch - 长字符串处理
    /// </summary>
    [Fact]
    public void IsMatch_WithLongString_ShouldHandleCorrectly()
    {
        // Arrange
        var longString = new string('a', 10000);
        var pattern = @"a+";
        // Act
        var result = RegexJudge.IsMatch(longString, pattern);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - IsMatch - 复杂正则表达式
    /// </summary>
    [Fact]
    public void IsMatch_WithComplexPattern_ShouldWork()
    {
        // Arrange
        var input = "The quick brown fox jumps over the lazy dog 123";
        var pattern = @"^The\s+(?<adjective1>\w+)\s+(?<color>\w+)\s+(?<animal1>\w+)\s+jumps\s+over\s+the\s+(?<adjective2>\w+)\s+(?<animal2>\w+)\s+(?<number>\d+)$";
        // Act
        var result = RegexJudge.IsMatch(input, pattern);
        // Assert
        result.ShouldBeTrue();
    }
    #endregion
    #region 实际应用场景测试
    /// <summary>
    /// 测试 - IsMatch - 实际应用场景
    /// </summary>
    [Fact]
    public void IsMatch_RealWorldScenarios_ShouldWorkCorrectly()
    {
        // 邮箱验证
        RegexJudge.IsMatch("user@example.com", @"^[^\s@]+@[^\s@]+\.[^\s@]+$").ShouldBeTrue();
        RegexJudge.IsMatch("invalid.email", @"^[^\s@]+@[^\s@]+\.[^\s@]+$").ShouldBeFalse();
        // 手机号验证
        RegexJudge.IsMatch("13812345678", @"^1[3-9]\d{9}$").ShouldBeTrue();
        RegexJudge.IsMatch("12345678901", @"^1[3-9]\d{9}$").ShouldBeFalse();
        // 身份证号验证
        RegexJudge.IsMatch("110101199003078515", @"^\d{17}[\dX]$").ShouldBeTrue();
        RegexJudge.IsMatch("11010119900307851X", @"^\d{17}[\dX]$").ShouldBeTrue();
        // 密码强度验证（至少8位，包含字母和数字）
        RegexJudge.IsMatch("Password123", @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$").ShouldBeTrue();
        RegexJudge.IsMatch("password", @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$").ShouldBeFalse();
        // 中文姓名验证
        RegexJudge.IsMatch("张三", @"^[\u4e00-\u9fa5]{2,4}$").ShouldBeTrue();
        RegexJudge.IsMatch("Zhang San", @"^[\u4e00-\u9fa5]{2,4}$").ShouldBeFalse();
        // 版本号验证
        RegexJudge.IsMatch("1.2.3", @"^\d+\.\d+\.\d+$").ShouldBeTrue();
        RegexJudge.IsMatch("v1.2.3", @"^\d+\.\d+\.\d+$").ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - IsMatch - 边界值测试
    /// </summary>
    [Fact]
    public void IsMatch_BoundaryValues_ShouldHandleCorrectly()
    {
        // 最小匹配
        RegexJudge.IsMatch("a", @"a").ShouldBeTrue();
        RegexJudge.IsMatch("", @"").ShouldBeTrue();
        // 特殊字符
        RegexJudge.IsMatch("hello@world", @"hello@world").ShouldBeTrue();
        RegexJudge.IsMatch("test.file", @"test\.file").ShouldBeTrue();
        RegexJudge.IsMatch("test.file", @"test.file").ShouldBeTrue(); // . 匹配任意字符
        // Unicode字符
        RegexJudge.IsMatch("café", @"caf[eé]").ShouldBeTrue();
        RegexJudge.IsMatch("测试", @"测试").ShouldBeTrue();
        // 转义字符
        RegexJudge.IsMatch("$100", @"\$\d+").ShouldBeTrue();
        RegexJudge.IsMatch("(test)", @"\(test\)").ShouldBeTrue();
    }
    #endregion
    #region 异常处理测试
    /// <summary>
    /// 测试 - IsMatch - 异常输入安全处理
    /// </summary>
    [Fact]
    public void IsMatch_ExceptionHandling_ShouldBeSafe()
    {
        // 测试各种异常输入，确保方法不会意外崩溃
        Should.NotThrow(() =>
        {
            // 特殊字符
            RegexJudge.IsMatch("test\0null", @"test");
            RegexJudge.IsMatch("test\r\n", @"test");
            RegexJudge.IsMatch("test\t\b", @"test");
        });
        // 无效正则表达式应该抛出异常
        Should.Throw<ArgumentException>(() => RegexJudge.IsMatch("test", "["));
        Should.Throw<ArgumentException>(() => RegexJudge.IsMatch("test", "*"));
    }
    #endregion
}
