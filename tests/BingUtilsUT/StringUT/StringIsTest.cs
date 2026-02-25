using Bing.Text;
namespace BingUtilsUT.StringUT;
[Trait("StringUT", "Strings.Is")]
public class StringIsTest
{
    #region IsUpper/IsLower 测试
    /// <summary>
    /// 测试 - 是否为大写
    /// </summary>
    [Fact]
    public void Test_IsUpper()
    {
        // 空字符串和null的处理 - 根据FilterForLetters的行为调整
        Strings.IsUpper("").ShouldBeTrue();       // 没有字母字符时返回true
        Strings.IsUpper(null).ShouldBeTrue();     // null处理
        Strings.IsUpper("   ").ShouldBeTrue();    // 只有空白字符
        Strings.IsUpper("123").ShouldBeTrue();    // 只有数字，没有字母
        Strings.IsUpper("!@#").ShouldBeTrue();    // 只有特殊字符，没有字母
        // 包含字母的测试
        Strings.IsUpper("a").ShouldBeFalse();
        Strings.IsUpper("A").ShouldBeTrue();
        Strings.IsUpper("aa").ShouldBeFalse();
        Strings.IsUpper("AA").ShouldBeTrue();
        Strings.IsUpper("aA").ShouldBeFalse();
        Strings.IsUpper("Aa").ShouldBeFalse();
        Strings.IsUpper("a123").ShouldBeFalse();
        Strings.IsUpper("A123").ShouldBeTrue();
        Strings.IsUpper("aa123").ShouldBeFalse();
        Strings.IsUpper("AA123").ShouldBeTrue();
        Strings.IsUpper("aA123").ShouldBeFalse();
        Strings.IsUpper("Aa123").ShouldBeFalse();
        Strings.IsUpper("a°").ShouldBeFalse();
        Strings.IsUpper("A°").ShouldBeTrue();
        Strings.IsUpper("aa°").ShouldBeFalse();
        Strings.IsUpper("AA°").ShouldBeTrue();
        Strings.IsUpper("aA°").ShouldBeFalse();
        Strings.IsUpper("Aa°").ShouldBeFalse();
        Strings.IsUpper("a ").ShouldBeFalse();
        Strings.IsUpper("A ").ShouldBeTrue();
        Strings.IsUpper("a a").ShouldBeFalse();
        Strings.IsUpper("A A").ShouldBeTrue();
        Strings.IsUpper("a A").ShouldBeFalse();
        Strings.IsUpper("A a").ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - 是否为小写
    /// </summary>
    [Fact]
    public void Test_IsLower()
    {
        // 空字符串和null的处理 - 根据FilterForLetters的行为调整
        Strings.IsLower("").ShouldBeTrue();       // 没有字母字符时返回true
        Strings.IsLower(null).ShouldBeTrue();     // null处理
        Strings.IsLower("   ").ShouldBeTrue();    // 只有空白字符
        Strings.IsLower("123").ShouldBeTrue();    // 只有数字，没有字母
        Strings.IsLower("!@#").ShouldBeTrue();    // 只有特殊字符，没有字母
        // 包含字母的测试
        Strings.IsLower("a").ShouldBeTrue();
        Strings.IsLower("A").ShouldBeFalse();
        Strings.IsLower("aa").ShouldBeTrue();
        Strings.IsLower("AA").ShouldBeFalse();
        Strings.IsLower("aA").ShouldBeFalse();
        Strings.IsLower("Aa").ShouldBeFalse();
        Strings.IsLower("a123").ShouldBeTrue();
        Strings.IsLower("A123").ShouldBeFalse();
        Strings.IsLower("aa123").ShouldBeTrue();
        Strings.IsLower("AA123").ShouldBeFalse();
        Strings.IsLower("aA123").ShouldBeFalse();
        Strings.IsLower("Aa123").ShouldBeFalse();
        Strings.IsLower("a°").ShouldBeTrue();
        Strings.IsLower("A°").ShouldBeFalse();
        Strings.IsLower("aa°").ShouldBeTrue();
        Strings.IsLower("AA°").ShouldBeFalse();
        Strings.IsLower("aA°").ShouldBeFalse();
        Strings.IsLower("Aa°").ShouldBeFalse();
        Strings.IsLower("a ").ShouldBeTrue();
        Strings.IsLower("A ").ShouldBeFalse();
        Strings.IsLower("a a").ShouldBeTrue();
        Strings.IsLower("A A").ShouldBeFalse();
        Strings.IsLower("a A").ShouldBeFalse();
        Strings.IsLower("A a").ShouldBeFalse();
    }
    #endregion
    #region IsChinese 测试
    /// <summary>
    /// 测试 IsChinese(char value) 方法，当字符是中文时应返回 True
    /// </summary>
    [Fact]
    public void IsChineseChar_Should_Return_True_For_Chinese_Character()
    {
        // Arrange
        var character = '你';
        // Act
        var result = Strings.IsChinese(character);
        // Assert
        Assert.True(result);
    }
    /// <summary>
    /// 测试 IsChinese(char value) 方法，当字符不是中文时应返回 False
    /// </summary>
    [Fact]
    public void IsChineseChar_Should_Return_False_For_Non_Chinese_Character()
    {
        // Arrange
        var character = 'A';
        // Act
        var result = Strings.IsChinese(character);
        // Assert
        Assert.False(result);
    }
    /// <summary>
    /// 测试 IsChinese(string text) 方法，当字符串全部由中文字符组成时应返回 True
    /// </summary>
    [Fact]
    public void IsChineseString_Should_Return_True_For_Chinese_String()
    {
        // Arrange
        var text = "你好世界";
        // Act
        var result = Strings.IsChinese(text);
        // Assert
        Assert.True(result);
    }
    /// <summary>
    /// 测试 IsChinese(string text) 方法，当字符串含有非中文字符时应返回 False
    /// </summary>
    [Fact]
    public void IsChineseString_Should_Return_False_For_Non_Chinese_String()
    {
        // Arrange
        var text = "Hello";
        // Act
        var result = Strings.IsChinese(text);
        // Assert
        Assert.False(result);
    }
    /// <summary>
    /// 测试 IsChinese(string text) 方法，当字符串包含中英文混合时应返回 False
    /// </summary>
    [Fact]
    public void IsChineseString_Should_Return_False_For_Mixed_String()
    {
        // Arrange
        var text = "你好World";
        // Act
        var result = Strings.IsChinese(text);
        // Assert
        Assert.False(result);
    }
    /// <summary>
    /// 测试 IsChinese(string text) 方法，当字符串为空时应返回 False
    /// </summary>
    [Fact]
    public void IsChineseString_Should_Return_False_For_Empty_String()
    {
        // Arrange
        var text = string.Empty;
        // Act
        var result = Strings.IsChinese(text);
        // Assert
        Assert.False(result);
    }
    #endregion
    #region IsAllUpperCase 测试
    /// <summary>
    /// 测试 - IsAllUpperCase - 全部大写字母返回true
    /// </summary>
    [Theory]
    [InlineData("HELLO", true)]
    [InlineData("ABC", true)]
    [InlineData("WORLD", true)]
    [InlineData("Z", true)]
    public void IsAllUpperCase_WithAllUpperCaseLetters_ReturnsTrue(string text, bool expected)
    {
        // Act
        var result = Strings.IsAllUpperCase(text);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsAllUpperCase - 包含非大写字母返回false
    /// </summary>
    [Theory]
    [InlineData("Hello", false)]       // 混合大小写
    [InlineData("hello", false)]       // 全小写
    [InlineData("HELLO123", false)]    // 包含数字
    [InlineData("HELLO!", false)]      // 包含特殊字符
    [InlineData("HELLO ", false)]      // 包含空格
    [InlineData("", false)]            // 空字符串
    [InlineData("   ", false)]         // 空白字符串
    [InlineData(null, false)]          // null
    public void IsAllUpperCase_WithNonUpperCaseContent_ReturnsFalse(string text, bool expected)
    {
        // Act
        var result = Strings.IsAllUpperCase(text);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IsAllLowerCase 测试
    /// <summary>
    /// 测试 - IsAllLowerCase - 全部小写字母返回true
    /// </summary>
    [Theory]
    [InlineData("hello", true)]
    [InlineData("abc", true)]
    [InlineData("world", true)]
    [InlineData("z", true)]
    public void IsAllLowerCase_WithAllLowerCaseLetters_ReturnsTrue(string text, bool expected)
    {
        // Act
        var result = Strings.IsAllLowerCase(text);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsAllLowerCase - 包含非小写字母返回false
    /// </summary>
    [Theory]
    [InlineData("Hello", false)]       // 混合大小写
    [InlineData("HELLO", false)]       // 全大写
    [InlineData("hello123", false)]    // 包含数字
    [InlineData("hello!", false)]      // 包含特殊字符
    [InlineData("hello ", false)]      // 包含空格
    [InlineData("", false)]            // 空字符串
    [InlineData("   ", false)]         // 空白字符串
    [InlineData(null, false)]          // null
    public void IsAllLowerCase_WithNonLowerCaseContent_ReturnsFalse(string text, bool expected)
    {
        // Act
        var result = Strings.IsAllLowerCase(text);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IsAllLetters 测试
    /// <summary>
    /// 测试 - IsAllLetters - 只包含字母返回true
    /// </summary>
    [Theory]
    [InlineData("Hello", true)]
    [InlineData("ABC", true)]
    [InlineData("abc", true)]
    [InlineData("AbCdEf", true)]
    [InlineData("测试", true)]          // Unicode字母
    [InlineData("HelloWorld", true)]
    public void IsAllLetters_WithOnlyLetters_ReturnsTrue(string text, bool expected)
    {
        // Act
        var result = Strings.IsAllLetters(text);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsAllLetters - 包含非字母返回false
    /// </summary>
    [Theory]
    [InlineData("Hello123", false)]    // 包含数字
    [InlineData("Hello!", false)]      // 包含特殊字符
    [InlineData("Hello ", false)]      // 包含空格
    [InlineData("Hello_World", false)] // 包含下划线
    [InlineData("", false)]            // 空字符串
    [InlineData("   ", false)]         // 空白字符串
    [InlineData(null, false)]          // null
    [InlineData("123", false)]         // 纯数字
    public void IsAllLetters_WithNonLetters_ReturnsFalse(string text, bool expected)
    {
        // Act
        var result = Strings.IsAllLetters(text);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IsAllDigits 测试
    /// <summary>
    /// 测试 - IsAllDigits - 只包含数字返回true
    /// </summary>
    [Theory]
    [InlineData("123", true)]
    [InlineData("0", true)]
    [InlineData("999999", true)]
    [InlineData("0123456789", true)]
    [InlineData("٠١٢٣", true)]         // Unicode数字（阿拉伯数字）
    public void IsAllDigits_WithOnlyDigits_ReturnsTrue(string text, bool expected)
    {
        // Act
        var result = Strings.IsAllDigits(text);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsAllDigits - 包含非数字返回false
    /// </summary>
    [Theory]
    [InlineData("123abc", false)]      // 包含字母
    [InlineData("123!", false)]        // 包含特殊字符
    [InlineData("123 ", false)]        // 包含空格
    [InlineData("12.34", false)]       // 包含小数点
    [InlineData("-123", false)]        // 包含负号
    [InlineData("", false)]            // 空字符串
    [InlineData("   ", false)]         // 空白字符串
    [InlineData(null, false)]          // null
    [InlineData("abc", false)]         // 纯字母
    public void IsAllDigits_WithNonDigits_ReturnsFalse(string text, bool expected)
    {
        // Act
        var result = Strings.IsAllDigits(text);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IsAlphanumeric 测试
    /// <summary>
    /// 测试 - IsAlphanumeric - 只包含字母和数字返回true
    /// </summary>
    [Theory]
    [InlineData("Hello123", true)]
    [InlineData("ABC123", true)]
    [InlineData("test999", true)]
    [InlineData("123ABC", true)]
    [InlineData("a1B2c3", true)]
    [InlineData("测试123", true)]       // Unicode字母+数字
    [InlineData("Hello", true)]        // 纯字母
    [InlineData("123", true)]          // 纯数字
    public void IsAlphanumeric_WithLettersAndDigits_ReturnsTrue(string text, bool expected)
    {
        // Act
        var result = Strings.IsAlphanumeric(text);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsAlphanumeric - 包含其他字符返回false
    /// </summary>
    [Theory]
    [InlineData("Hello 123", false)]   // 包含空格
    [InlineData("Hello!", false)]      // 包含特殊字符
    [InlineData("Hello_123", false)]   // 包含下划线
    [InlineData("Hello-123", false)]   // 包含连字符
    [InlineData("Hello.123", false)]   // 包含点号
    [InlineData("", false)]            // 空字符串
    [InlineData("   ", false)]         // 空白字符串
    [InlineData(null, false)]          // null
    public void IsAlphanumeric_WithSpecialCharacters_ReturnsFalse(string text, bool expected)
    {
        // Act
        var result = Strings.IsAlphanumeric(text);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IsAsciiLetters 测试
    /// <summary>
    /// 测试 - IsAsciiLetters - 只包含ASCII字母返回true
    /// </summary>
    [Theory]
    [InlineData("Hello", true)]
    [InlineData("ABC", true)]
    [InlineData("abc", true)]
    [InlineData("AbCdEf", true)]
    [InlineData("z", true)]
    [InlineData("Z", true)]
    public void IsAsciiLetters_WithOnlyAsciiLetters_ReturnsTrue(string text, bool expected)
    {
        // Act
        var result = Strings.IsAsciiLetters(text);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsAsciiLetters - 包含非ASCII字母返回false
    /// </summary>
    [Theory]
    [InlineData("Hello123", false)]    // 包含数字
    [InlineData("Hello!", false)]      // 包含特殊字符
    [InlineData("Hello ", false)]      // 包含空格
    [InlineData("测试", false)]         // Unicode字符
    [InlineData("Héllo", false)]       // 重音字符
    [InlineData("", false)]            // 空字符串
    [InlineData("   ", false)]         // 空白字符串
    [InlineData(null, false)]          // null
    public void IsAsciiLetters_WithNonAsciiContent_ReturnsFalse(string text, bool expected)
    {
        // Act
        var result = Strings.IsAsciiLetters(text);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IsAsciiDigits 测试
    /// <summary>
    /// 测试 - IsAsciiDigits - 只包含ASCII数字返回true
    /// </summary>
    [Theory]
    [InlineData("123", true)]
    [InlineData("0", true)]
    [InlineData("999999", true)]
    [InlineData("0123456789", true)]
    public void IsAsciiDigits_WithOnlyAsciiDigits_ReturnsTrue(string text, bool expected)
    {
        // Act
        var result = Strings.IsAsciiDigits(text);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsAsciiDigits - 包含非ASCII数字返回false
    /// </summary>
    [Theory]
    [InlineData("123abc", false)]      // 包含字母
    [InlineData("123!", false)]        // 包含特殊字符
    [InlineData("123 ", false)]        // 包含空格
    [InlineData("٠١٢٣", false)]        // Unicode数字
    [InlineData("12.34", false)]       // 包含小数点
    [InlineData("-123", false)]        // 包含负号
    [InlineData("", false)]            // 空字符串
    [InlineData("   ", false)]         // 空白字符串
    [InlineData(null, false)]          // null
    public void IsAsciiDigits_WithNonAsciiContent_ReturnsFalse(string text, bool expected)
    {
        // Act
        var result = Strings.IsAsciiDigits(text);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IsAsciiAlphanumeric 测试
    /// <summary>
    /// 测试 - IsAsciiAlphanumeric - 只包含ASCII字母和数字返回true
    /// </summary>
    [Theory]
    [InlineData("Hello123", true)]
    [InlineData("ABC123", true)]
    [InlineData("test999", true)]
    [InlineData("123ABC", true)]
    [InlineData("a1B2c3", true)]
    [InlineData("Hello", true)]        // 纯字母
    [InlineData("123", true)]          // 纯数字
    public void IsAsciiAlphanumeric_WithAsciiLettersAndDigits_ReturnsTrue(string text, bool expected)
    {
        // Act
        var result = Strings.IsAsciiAlphanumeric(text);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsAsciiAlphanumeric - 包含非ASCII字符返回false
    /// </summary>
    [Theory]
    [InlineData("Hello 123", false)]   // 包含空格
    [InlineData("Hello!", false)]      // 包含特殊字符
    [InlineData("Hello_123", false)]   // 包含下划线
    [InlineData("测试123", false)]      // Unicode字符
    [InlineData("Héllo123", false)]    // 重音字符
    [InlineData("٠١٢٣", false)]        // Unicode数字
    [InlineData("", false)]            // 空字符串
    [InlineData("   ", false)]         // 空白字符串
    [InlineData(null, false)]          // null
    public void IsAsciiAlphanumeric_WithNonAsciiContent_ReturnsFalse(string text, bool expected)
    {
        // Act
        var result = Strings.IsAsciiAlphanumeric(text);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IsValidIdentifier 测试
    /// <summary>
    /// 测试 - IsValidIdentifier - 有效标识符返回true
    /// </summary>
    [Theory]
    [InlineData("_test", true)]        // 以下划线开头
    [InlineData("test", true)]         // 以字母开头
    [InlineData("Test", true)]         // 以大写字母开头
    [InlineData("test123", true)]      // 包含数字
    [InlineData("test_123", true)]     // 包含下划线
    [InlineData("_", true)]            // 单个下划线
    [InlineData("a", true)]            // 单个字母
    [InlineData("Test_Case_123", true)] // 复杂标识符
    public void IsValidIdentifier_WithValidIdentifiers_ReturnsTrue(string text, bool expected)
    {
        // Act
        var result = Strings.IsValidIdentifier(text);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsValidIdentifier - 无效标识符返回false
    /// </summary>
    [Theory]
    [InlineData("123test", false)]     // 以数字开头
    [InlineData("test-123", false)]    // 包含连字符
    [InlineData("test 123", false)]    // 包含空格
    [InlineData("test!", false)]       // 包含特殊字符
    [InlineData("test.method", false)] // 包含点号
    [InlineData("", false)]            // 空字符串
    [InlineData("   ", false)]         // 空白字符串
    [InlineData(null, false)]          // null
    [InlineData("测试", false)]         // Unicode字符
    public void IsValidIdentifier_WithInvalidIdentifiers_ReturnsFalse(string text, bool expected)
    {
        // Act
        var result = Strings.IsValidIdentifier(text);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IsChinese 边界测试
    /// <summary>
    /// 测试 - IsChinese - 中文字符边界测试
    /// </summary>
    [Theory]
    [InlineData('\u4E00', true)]       // 中文字符起始
    [InlineData('\u9FA5', true)]       // 中文字符结束
    [InlineData('\u4DFF', false)]      // 中文字符前一个
    [InlineData('\u9FA6', false)]      // 中文字符后一个
    [InlineData('中', true)]           // 普通中文字符
    [InlineData('国', true)]           // 普通中文字符
    [InlineData('A', false)]           // 英文字符
    [InlineData('1', false)]           // 数字字符
    [InlineData('！', false)]          // 中文标点
    public void IsChinese_CharacterBoundaryTest_ReturnsExpectedResult(char character, bool expected)
    {
        // Act
        var result = Strings.IsChinese(character);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsChinese - 字符串边界和特殊情况
    /// </summary>
    [Theory]
    [InlineData("中国", true)]         // 纯中文
    [InlineData("中国123", false)]     // 中文+数字
    [InlineData("中国ABC", false)]     // 中文+英文
    [InlineData("中国！", false)]      // 中文+标点
    [InlineData("中 国", false)]       // 中文+空格
    [InlineData("\u4E00\u9FA5", true)] // 边界字符
    [InlineData("", false)]            // 空字符串
    [InlineData(" ", false)]           // 单个空格
    [InlineData("\t", false)]          // 制表符
    [InlineData("\n", false)]          // 换行符
    public void IsChinese_StringBoundaryTest_ReturnsExpectedResult(string text, bool expected)
    {
        // Act
        var result = Strings.IsChinese(text);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 性能测试
    /// <summary>
    /// 测试 - 性能测试 - 确保方法在合理时间内执行
    /// </summary>
    [Fact]
    public void PerformanceTest_AllMethods_ShouldCompleteInReasonableTime()
    {
        // Arrange
        var testData = new[]
        {
            "Hello World 123!",
            "UPPERCASE",
            "lowercase",
            "AlphaNumeric123",
            "中文测试",
            "_validIdentifier123",
            "12345",
            "ASCII_ONLY",
            string.Empty,
            new string('A', 1000) // 长字符串
        };
        // Act & Assert - 应该在合理时间内完成
        Should.CompleteIn(() =>
        {
            foreach (var data in testData)
            {
                for (int i = 0; i < 1000; i++)
                {
                    Strings.IsUpper(data);
                    Strings.IsLower(data);
                    Strings.IsChinese(data);
                    Strings.IsAllUpperCase(data);
                    Strings.IsAllLowerCase(data);
                    Strings.IsAllLetters(data);
                    Strings.IsAllDigits(data);
                    Strings.IsAlphanumeric(data);
                    Strings.IsAsciiLetters(data);
                    Strings.IsAsciiDigits(data);
                    Strings.IsAsciiAlphanumeric(data);
                    Strings.IsValidIdentifier(data);
                }
            }
        }, TimeSpan.FromSeconds(5)); // 应该在5秒内完成
    }
    #endregion
    #region 综合场景测试
    /// <summary>
    /// 测试 - 综合场景 - 各种真实数据验证
    /// </summary>
    [Fact]
    public void RealWorldScenarios_VariousInputs_ShouldWorkCorrectly()
    {
        // 变量名验证
        Strings.IsValidIdentifier("userName").ShouldBeTrue();
        Strings.IsValidIdentifier("_privateField").ShouldBeTrue();
        Strings.IsValidIdentifier("MAX_SIZE").ShouldBeTrue();
        Strings.IsValidIdentifier("123invalid").ShouldBeFalse();
        // 密码强度检查（仅字母数字）
        Strings.IsAlphanumeric("Password123").ShouldBeTrue();
        Strings.IsAlphanumeric("Password!123").ShouldBeFalse();
        // 产品编码验证
        Strings.IsAsciiAlphanumeric("PROD123ABC").ShouldBeTrue();
        Strings.IsAsciiAlphanumeric("PROD-123").ShouldBeFalse();
        // 国际化文本检查
        Strings.IsChinese("用户名").ShouldBeTrue();
        Strings.IsChinese("User用户").ShouldBeFalse();
        // 数据清洗检查
        Strings.IsAllDigits("1234567890").ShouldBeTrue();
        Strings.IsAllLetters("ABCDEFG").ShouldBeTrue();
        // 大小写检查（只检查字母）
        Strings.IsUpper("HELLO123!").ShouldBeTrue();    // 字母都是大写
        Strings.IsLower("hello123!").ShouldBeTrue();    // 字母都是小写
        Strings.IsUpper("123!@#").ShouldBeTrue();       // 没有字母字符
        Strings.IsLower("123!@#").ShouldBeTrue();       // 没有字母字符
    }
    /// <summary>
    /// 测试 - 异常输入处理
    /// </summary>
    [Fact]
    public void ExceptionInputHandling_ShouldNotThrow()
    {
        // 确保所有方法都能安全处理异常输入
        Should.NotThrow(() =>
        {
            Strings.IsUpper(null);
            Strings.IsLower(string.Empty);
            Strings.IsChinese("  ");
            Strings.IsAllUpperCase("\0");
            Strings.IsAllLowerCase("\uFFFF");
            Strings.IsValidIdentifier(new string('a', 10000));
        });
    }
    #endregion
}
