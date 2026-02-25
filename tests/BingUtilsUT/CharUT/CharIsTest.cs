using Bing.Text;
namespace BingUtilsUT.CharUT;
[Trait("CharUT", "Char.Is")]
public class CharIsTest
{
    #region IsBlankChar 方法测试
    /// <summary>
    /// 测试 - IsBlankChar - 标准空白字符
    /// </summary>
    [Theory]
    [InlineData(' ', true)]          // 普通空格 (U+0020)
    [InlineData('\t', true)]         // 制表符 (U+0009)
    [InlineData('\n', true)]         // 换行符 (U+000A)
    [InlineData('\r', true)]         // 回车符 (U+000D)
    [InlineData('\v', true)]         // 垂直制表符 (U+000B)
    [InlineData('\f', true)]         // 换页符 (U+000C)
    public void IsBlankChar_StandardWhitespace_ReturnsExpected(char input, bool expected)
    {
        // Act
        var result = CharJudge.IsBlankChar(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsBlankChar - Unicode空白字符
    /// </summary>
    [Theory]
    [InlineData('\u00A0', true)]     // 不间断空格 (NO-BREAK SPACE)
    [InlineData('\u1680', true)]     // 奥甘空格 (OGHAM SPACE MARK)
    [InlineData('\u2000', true)]     // EN QUAD
    [InlineData('\u2001', true)]     // EM QUAD
    [InlineData('\u2002', true)]     // EN SPACE
    [InlineData('\u2003', true)]     // EM SPACE
    [InlineData('\u2004', true)]     // THREE-PER-EM SPACE
    [InlineData('\u2005', true)]     // FOUR-PER-EM SPACE
    [InlineData('\u2006', true)]     // SIX-PER-EM SPACE
    [InlineData('\u2007', true)]     // FIGURE SPACE
    [InlineData('\u2008', true)]     // PUNCTUATION SPACE
    [InlineData('\u2009', true)]     // THIN SPACE
    [InlineData('\u200A', true)]     // HAIR SPACE
    [InlineData('\u202F', true)]     // NARROW NO-BREAK SPACE
    [InlineData('\u205F', true)]     // MEDIUM MATHEMATICAL SPACE
    [InlineData('\u3000', true)]     // 全角空格 (IDEOGRAPHIC SPACE)
    public void IsBlankChar_UnicodeWhitespace_ReturnsTrue(char input, bool expected)
    {
        // Act
        var result = CharJudge.IsBlankChar(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsBlankChar - Unicode分隔符
    /// </summary>
    [Theory]
    [InlineData('\u2028', true)]     // LINE SEPARATOR
    [InlineData('\u2029', true)]     // PARAGRAPH SEPARATOR
    public void IsBlankChar_UnicodeSeparators_ReturnsTrue(char input, bool expected)
    {
        // Act
        var result = CharJudge.IsBlankChar(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsBlankChar - 特殊空白字符
    /// </summary>
    [Theory]
    [InlineData('\ufeff', true)]     // ZERO WIDTH NO-BREAK SPACE (BOM)
    [InlineData('\u202a', true)]     // LEFT-TO-RIGHT EMBEDDING
    [InlineData('\u0000', true)]     // NULL
    [InlineData('\u3164', true)]     // HANGUL FILLER
    [InlineData('\u2800', true)]     // BRAILLE PATTERN BLANK
    [InlineData('\u180e', true)]     // MONGOLIAN VOWEL SEPARATOR
    public void IsBlankChar_SpecialBlankChars_ReturnsTrue(char input, bool expected)
    {
        // Act
        var result = CharJudge.IsBlankChar(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsBlankChar - 非空白字符
    /// </summary>
    [Theory]
    [InlineData('a', false)]         // 小写字母
    [InlineData('A', false)]         // 大写字母
    [InlineData('0', false)]         // 数字
    [InlineData('中', false)]        // 中文字符
    [InlineData('!', false)]         // 标点符号
    [InlineData('@', false)]         // 特殊符号
    [InlineData('_', false)]         // 下划线
    [InlineData('-', false)]         // 连字符
    [InlineData('.', false)]         // 点号
    [InlineData('€', false)]         // 欧元符号
    [InlineData('α', false)]         // 希腊字母
    public void IsBlankChar_NonBlankChars_ReturnsFalse(char input, bool expected)
    {
        // Act
        var result = CharJudge.IsBlankChar(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsBlankChar - 边界条件
    /// </summary>
    [Theory]
    [InlineData('\u0001', false)]    // 控制字符但非空白
    [InlineData('\u001F', false)]    // 控制字符但非空白
    [InlineData('\u007F', false)]    // DELETE字符
    [InlineData('\u0080', false)]    // 扩展ASCII开始
    [InlineData('\u00FF', false)]    // 扩展ASCII结束
    public void IsBlankChar_BoundaryConditions_ReturnsExpected(char input, bool expected)
    {
        // Act
        var result = CharJudge.IsBlankChar(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsBlankChar - 验证与.NET内置方法的一致性
    /// </summary>
    [Fact]
    public void IsBlankChar_ConsistencyWithDotNetMethods_ValidatesCorrectly()
    {
        // Arrange
        var standardWhitespaceChars = new char[] { ' ', '\t', '\n', '\r', '\v', '\f' };
        // Act & Assert
        foreach (var ch in standardWhitespaceChars)
        {
            // CharJudge.IsBlankChar应该对所有.NET认为的空白字符返回true
            char.IsWhiteSpace(ch).ShouldBeTrue($"Char '{ch}' (U+{(int)ch:X4}) should be whitespace");
            CharJudge.IsBlankChar(ch).ShouldBeTrue($"CharJudge should identify '{ch}' (U+{(int)ch:X4}) as blank");
        }
    }
    /// <summary>
    /// 测试 - IsBlankChar - 性能测试
    /// </summary>
    [Fact]
    public void IsBlankChar_Performance_CompletesInReasonableTime()
    {
        // Arrange
        const int iterations = 100000;
        var testChars = new char[] { ' ', '\t', 'a', '中', '\u3000', '\u0000' };
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                foreach (var ch in testChars)
                {
                    CharJudge.IsBlankChar(ch);
                }
            }
        }, TimeSpan.FromSeconds(1), "IsBlankChar should complete within 1 second");
    }
    #endregion
    #region IsEmoji 方法测试
    /// <summary>
    /// 测试 - IsEmoji - 单字节emoji符号范围
    /// </summary>
    [Theory]
    [InlineData('\u2600', true)]     // BLACK SUN WITH RAYS (☀)
    [InlineData('\u2601', true)]     // CLOUD (☁)
    [InlineData('\u2614', true)]     // UMBRELLA WITH RAIN DROPS (☔)
    [InlineData('\u2615', true)]     // HOT BEVERAGE (☕)
    [InlineData('\u2648', true)]     // ARIES (♈)
    [InlineData('\u2649', true)]     // TAURUS (♉)
    [InlineData('\u2708', true)]     // AIRPLANE (✈)
    [InlineData('\u2709', true)]     // ENVELOPE (✉)
    [InlineData('\u2728', true)]     // SPARKLES (✨)
    [InlineData('\u2764', true)]     // HEAVY BLACK HEART (❤)
    public void IsEmoji_SingleByteEmojiSymbols_ReturnsTrue(char input, bool expected)
    {
        // Act
        var result = CharJudge.IsEmoji(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsEmoji - 使用字符串索引访问emoji
    /// </summary>
    [Fact]
    public void IsEmoji_EmojiFromStrings_WorksCorrectly()
    {
        // Arrange - 使用字符串来处理多字节emoji
        var emojiStrings = new[]
        {
            "😀", // GRINNING FACE
            "😃", // GRINNING FACE WITH BIG EYES  
            "😄", // GRINNING FACE WITH SMILING EYES
            "🌟", // GLOWING STAR
            "🌈", // RAINBOW
            "🔥", // FIRE
            "💖", // SPARKLING HEART
            "🎉", // PARTY POPPER
            "🐶", // DOG FACE
            "🍎"  // RED APPLE
        };
        // Act & Assert
        foreach (var emojiStr in emojiStrings)
        {
            // 对于多字节emoji，我们测试高代理字符
            if (emojiStr.Length > 1 && char.IsHighSurrogate(emojiStr[0]))
            {
                // 高代理字符应该被识别为emoji范围
                var highSurrogate = emojiStr[0];
                var result = CharJudge.IsEmoji(highSurrogate);
                // 检查字符是否在emoji的高代理范围内
                var codePoint = (int)highSurrogate;
                var isInEmojiRange = (codePoint >= 0xD83C && codePoint <= 0xD83F); // emoji高代理范围
                result.ShouldBe(isInEmojiRange,
                    $"High surrogate '{highSurrogate}' (U+{codePoint:X4}) from emoji '{emojiStr}' should match emoji range check");
            }
        }
    }
    /// <summary>
    /// 测试 - IsEmoji - Unicode范围边界测试
    /// </summary>
    [Theory]
    [InlineData('\u25FF', false)]    // Misc Symbols 范围前
    [InlineData('\u2600', true)]     // Misc Symbols 开始
    [InlineData('\u26FF', true)]     // Misc Symbols 结尾
    [InlineData('\u2700', true)]     // Dingbats 开始
    [InlineData('\u27BF', true)]     // Dingbats 结尾
    [InlineData('\u27C0', false)]    // 超出Dingbats范围
    [InlineData('\u2B4F', false)]    // Star 范围前
    [InlineData('\u2B50', true)]     // STAR (⭐)
    [InlineData('\u2B51', false)]    // Star 范围后
    public void IsEmoji_UnicodeBoundaries_ReturnsExpected(char input, bool expected)
    {
        // Act
        var result = CharJudge.IsEmoji(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsEmoji - 非emoji字符
    /// </summary>
    [Theory]
    [InlineData('a', false)]         // 小写字母
    [InlineData('A', false)]         // 大写字母
    [InlineData('0', false)]         // 数字
    [InlineData('9', false)]         // 数字
    [InlineData('中', false)]        // 中文字符
    [InlineData('文', false)]        // 中文字符
    [InlineData('!', false)]         // 标点符号
    [InlineData('?', false)]         // 标点符号
    [InlineData('@', false)]         // 特殊符号
    [InlineData('#', false)]         // 特殊符号
    [InlineData(' ', false)]         // 空格
    [InlineData('\t', false)]        // 制表符
    [InlineData('\n', false)]        // 换行符
    [InlineData('_', false)]         // 下划线
    [InlineData('-', false)]         // 连字符
    [InlineData('=', false)]         // 等号
    [InlineData('+', false)]         // 加号
    public void IsEmoji_NonEmojiChars_ReturnsFalse(char input, bool expected)
    {
        // Act
        var result = CharJudge.IsEmoji(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsEmoji - 控制字符和特殊字符
    /// </summary>
    [Theory]
    [InlineData('\u0000', false)]    // NULL
    [InlineData('\u0001', false)]    // START OF HEADING
    [InlineData('\u001F', false)]    // UNIT SEPARATOR
    [InlineData('\u007F', false)]    // DELETE
    [InlineData('\u0080', false)]    // 控制字符
    [InlineData('\u009F', false)]    // 控制字符
    [InlineData('\uFEFF', false)]    // ZERO WIDTH NO-BREAK SPACE
    [InlineData('\uFFFF', false)]    // 非字符
    public void IsEmoji_ControlChars_ReturnsFalse(char input, bool expected)
    {
        // Act
        var result = CharJudge.IsEmoji(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsEmoji - 代理字符处理
    /// </summary>
    [Theory]
    [InlineData('\uD83C', true)]    // 高代理字符，但不在emoji范围
    [InlineData('\uD83D', true)]     // emoji高代理字符范围
    [InlineData('\uD83E', true)]     // emoji高代理字符范围
    [InlineData('\uDC00', false)]    // 低代理字符
    [InlineData('\uDFFF', false)]    // 低代理字符
    public void IsEmoji_SurrogateChars_HandlesCorrectly(char input, bool expected)
    {
        // Act
        var result = CharJudge.IsEmoji(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsEmoji - 性能测试
    /// </summary>
    [Fact]
    public void IsEmoji_Performance_CompletesInReasonableTime()
    {
        // Arrange
        const int iterations = 100000;
        var testChars = new char[] { '\u2600', '\u2764', 'a', '中', ' ', '\uD83D', '\u26FF' };
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                foreach (var ch in testChars)
                {
                    CharJudge.IsEmoji(ch);
                }
            }
        }, TimeSpan.FromSeconds(1), "IsEmoji should complete within 1 second");
    }
    #endregion
    #region 集成测试
    /// <summary>
    /// 测试 - 综合字符分类测试
    /// </summary>
    [Theory]
    [InlineData(' ', true, false)]      // 空格：空白符，非emoji
    [InlineData('\u2600', false, true)] // 太阳符号：非空白符，是emoji
    [InlineData('a', false, false)]     // 字母：非空白符，非emoji
    [InlineData('\t', true, false)]     // 制表符：空白符，非emoji
    [InlineData('\u2764', false, true)] // 心形：非空白符，是emoji
    [InlineData('\u3000', true, false)] // 全角空格：空白符，非emoji
    [InlineData('中', false, false)]    // 中文：非空白符，非emoji
    [InlineData('\u0000', true, false)] // NULL：空白符，非emoji
    public void IntegratedCharacterClassification_VariousChars_ReturnsExpected(
        char input, bool expectedBlank, bool expectedEmoji)
    {
        // Act
        var isBlank = CharJudge.IsBlankChar(input);
        var isEmoji = CharJudge.IsEmoji(input);
        // Assert
        isBlank.ShouldBe(expectedBlank, $"Character '{input}' (U+{(int)input:X4}) blank check failed");
        isEmoji.ShouldBe(expectedEmoji, $"Character '{input}' (U+{(int)input:X4}) emoji check failed");
        // 一个字符不应该既是空白符又是emoji（根据当前实现）
        if (isBlank && isEmoji)
        {
            Assert.True(false, $"Character '{input}' (U+{(int)input:X4}) cannot be both blank and emoji");
        }
    }
    /// <summary>
    /// 测试 - 字符串处理场景测试
    /// </summary>
    [Fact]
    public void StringProcessingScenarios_RealWorldExamples_WorksCorrectly()
    {
        // Arrange - 使用包含单字节emoji的字符串
        var testString = "Hello ☀ World\t\n中文⭐test";
        // Act & Assert
        foreach (char ch in testString)
        {
            var isBlank = CharJudge.IsBlankChar(ch);
            var isEmoji = CharJudge.IsEmoji(ch);
            // 验证一些预期结果
            switch (ch)
            {
                case ' ':
                case '\t':
                case '\n':
                    isBlank.ShouldBeTrue($"'{ch}' should be blank");
                    isEmoji.ShouldBeFalse($"'{ch}' should not be emoji");
                    break;
                case '\u2600': // ☀
                case '\u2B50': // ⭐
                    isBlank.ShouldBeFalse($"'{ch}' should not be blank");
                    isEmoji.ShouldBeTrue($"'{ch}' should be emoji");
                    break;
                case 'H':
                case 'e':
                case 'l':
                case 'o':
                case 'W':
                case 'r':
                case 'd':
                case 't':
                case 's':
                    isBlank.ShouldBeFalse($"'{ch}' should not be blank");
                    isEmoji.ShouldBeFalse($"'{ch}' should not be emoji");
                    break;
                case '中':
                case '文':
                    isBlank.ShouldBeFalse($"'{ch}' should not be blank");
                    isEmoji.ShouldBeFalse($"'{ch}' should not be emoji");
                    break;
            }
        }
    }
    /// <summary>
    /// 测试 - 多字节Emoji字符串处理
    /// </summary>
    [Fact]
    public void StringProcessingScenarios_MultiByteEmoji_HandlesCorrectly()
    {
        // Arrange
        var testString = "😀🌟💖";
        // Act & Assert
        foreach (char ch in testString)
        {
            var isBlank = CharJudge.IsBlankChar(ch);
            var isEmoji = CharJudge.IsEmoji(ch);
            // 多字节emoji的高代理字符应该被正确识别
            if (char.IsHighSurrogate(ch))
            {
                isBlank.ShouldBeFalse($"High surrogate '{ch}' (U+{(int)ch:X4}) should not be blank");
                // 高代理字符在emoji范围内应该被识别为emoji
                var codePoint = (int)ch;
                var expectedEmoji = (codePoint >= 0xD83C && codePoint <= 0xD83F);
                isEmoji.ShouldBe(expectedEmoji, $"High surrogate '{ch}' (U+{codePoint:X4}) emoji check");
            }
            else if (char.IsLowSurrogate(ch))
            {
                isBlank.ShouldBeFalse($"Low surrogate '{ch}' (U+{(int)ch:X4}) should not be blank");
                isEmoji.ShouldBeFalse($"Low surrogate '{ch}' (U+{(int)ch:X4}) should not be emoji");
            }
        }
    }
    #endregion
    #region 原有测试（保留兼容性）
    /// <summary>
    /// 测试 - 是否空白符（原有测试）
    /// </summary>
    [Fact]
    public void Test_IsBlankChar()
    {
        char a = '\u00A0';
        CharJudge.IsBlankChar(a).ShouldBeTrue();
        char a2 = '\u0020';
        CharJudge.IsBlankChar(a2).ShouldBeTrue();
        char a3 = '\u3000';
        CharJudge.IsBlankChar(a3).ShouldBeTrue();
        char a4 = '\u0000';
        CharJudge.IsBlankChar(a4).ShouldBeTrue();
        char a5 = ' ';
        CharJudge.IsBlankChar(a5).ShouldBeTrue();
        char a6 = '\u202a';
        CharJudge.IsBlankChar(a6).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - 是否 Emoji 表情符（原有测试）
    /// </summary>
    [Fact]
    public void Test_IsEmoji()
    {
        string a = """莉🌹""";
        CharJudge.IsEmoji(a[0]).ShouldBeFalse();
        CharJudge.IsEmoji(a[1]).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - Trim操作（原有测试）
    /// </summary>
    [Fact]
    public void Test_Trim()
    {
        var str = "‪C:/Users/maple/Desktop/tone.txt";
        str[0].ShouldBe('\u202a');
        CharJudge.IsBlankChar(str[0]).ShouldBeTrue();
        str = str.Trim('\u202a');
        str[0].ShouldBe('C');
        CharJudge.IsBlankChar(str[0]).ShouldBeFalse();
    }
    #endregion
    #region 实用方法测试
    /// <summary>
    /// 测试 - 字符分类统计
    /// </summary>
    [Fact]
    public void CharacterClassificationStatistics_VariousInputs_ReturnsAccurateCount()
    {
        // Arrange
        var testText = "Hello ☀ 世界\t\n⭐ 123!";
        int blankCount = 0;
        int emojiCount = 0;
        int otherCount = 0;
        // Act
        foreach (char ch in testText)
        {
            if (CharJudge.IsBlankChar(ch))
                blankCount++;
            else if (CharJudge.IsEmoji(ch))
                emojiCount++;
            else
                otherCount++;
        }
        // Assert
        blankCount.ShouldBe(5); // 空格、制表符、换行符
        emojiCount.ShouldBe(2); // ☀ 和 ⭐
        otherCount.ShouldBe(testText.Length - blankCount - emojiCount);
    }
    /// <summary>
    /// 测试 - Unicode分类一致性检查
    /// </summary>
    [Fact]
    public void UnicodeClassificationConsistency_CommonCharacters_MaintainsLogicalRelationships()
    {
        // Arrange & Act & Assert
        // 所有标准空白字符都应该被IsBlankChar识别
        for (char ch = '\u0009'; ch <= '\u000D'; ch++) // Tab到Carriage Return
        {
            if (char.IsWhiteSpace(ch))
            {
                CharJudge.IsBlankChar(ch).ShouldBeTrue($"Standard whitespace char U+{(int)ch:X4} should be blank");
            }
        }
        // ASCII字母数字字符不应该是空白或emoji
        for (char ch = 'A'; ch <= 'Z'; ch++)
        {
            CharJudge.IsBlankChar(ch).ShouldBeFalse($"Letter '{ch}' should not be blank");
            CharJudge.IsEmoji(ch).ShouldBeFalse($"Letter '{ch}' should not be emoji");
        }
        for (char ch = '0'; ch <= '9'; ch++)
        {
            CharJudge.IsBlankChar(ch).ShouldBeFalse($"Digit '{ch}' should not be blank");
            CharJudge.IsEmoji(ch).ShouldBeFalse($"Digit '{ch}' should not be emoji");
        }
    }
    #endregion
}
