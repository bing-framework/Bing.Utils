namespace Bing.Helpers;

/// <summary>
/// 正则表达式操作 测试类
/// </summary>
[Trait("Bing.Helpers", "Serialize")]
public class RegexsTest
{
    #region IsMatch 测试

    /// <summary>
    /// 测试 - IsMatch - 正常匹配场景
    /// </summary>
    [Theory]
    [InlineData("123", @"\d+", true)]
    [InlineData("abc", @"\d+", false)]
    [InlineData("test@email.com", @"^\w+@\w+\.\w+$", true)]
    [InlineData("invalid-email", @"^\w+@\w+\.\w+$", false)]
    [InlineData("", @"\d+", false)]
    public void IsMatch_NormalScenarios_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        // Act
        var result = Regexs.IsMatch(input, pattern);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsMatch - 空值参数异常
    /// </summary>
    [Fact]
    public void IsMatch_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Regexs.IsMatch(null, @"\d+"));
    }

    /// <summary>
    /// 测试 - IsMatch - 空模式参数异常
    /// </summary>
    [Fact]
    public void IsMatch_NullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Regexs.IsMatch("123", null));
    }

    /// <summary>
    /// 测试 - IsMatchCached - 缓存版本正常功能
    /// </summary>
    [Theory]
    [InlineData("123", @"\d+", true)]
    [InlineData("abc", @"\d+", false)]
    public void IsMatchCached_NormalScenarios_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        // Act
        var result = Regexs.IsMatchCached(input, pattern);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsMatchCached - 大小写敏感选项
    /// </summary>
    [Fact]
    public void IsMatchCached_CaseSensitiveOption_RespectsOption()
    {
        // Arrange
        const string input = "ABC";
        const string pattern = "abc";

        // Act
        var ignoreCase = Regexs.IsMatchCached(input, pattern, RegexOptions.IgnoreCase);
        var caseSensitive = Regexs.IsMatchCached(input, pattern, RegexOptions.None);

        // Assert
        ignoreCase.ShouldBeTrue();
        caseSensitive.ShouldBeFalse();
    }

    #endregion

    #region Match 测试

    /// <summary>
    /// 测试 - Match - 成功匹配返回有效结果
    /// </summary>
    [Fact]
    public void Match_SuccessfulMatch_ReturnsValidMatch()
    {
        // Arrange
        const string input = "Hello 123 World";
        const string pattern = @"\d+";

        // Act
        var match = Regexs.Match(input, pattern);

        // Assert
        match.Success.ShouldBeTrue();
        match.Value.ShouldBe("123");
        match.Index.ShouldBe(6);
    }

    /// <summary>
    /// 测试 - Match - 无匹配返回失败结果
    /// </summary>
    [Fact]
    public void Match_NoMatch_ReturnsFailedMatch()
    {
        // Arrange
        const string input = "Hello World";
        const string pattern = @"\d+";

        // Act
        var match = Regexs.Match(input, pattern);

        // Assert
        match.Success.ShouldBeFalse();
        match.Value.ShouldBe("");
    }

    /// <summary>
    /// 测试 - MatchCached - 缓存版本功能一致
    /// </summary>
    [Fact]
    public void MatchCached_SameAsNonCached_ReturnsConsistentResult()
    {
        // Arrange
        const string input = "Price: $99.99";
        const string pattern = @"\$(\d+\.\d+)";

        // Act
        var normalMatch = Regexs.Match(input, pattern);
        var cachedMatch = Regexs.MatchCached(input, pattern);

        // Assert
        normalMatch.Success.ShouldBe(cachedMatch.Success);
        normalMatch.Value.ShouldBe(cachedMatch.Value);
        normalMatch.Groups[1].Value.ShouldBe(cachedMatch.Groups[1].Value);
    }

    #endregion

    #region Matches 测试

    /// <summary>
    /// 测试 - Matches - 查找所有匹配项
    /// </summary>
    [Fact]
    public void Matches_MultipleMatches_ReturnsAllMatches()
    {
        // Arrange
        const string input = "Phone: 123-456-7890, Mobile: 987-654-3210";
        const string pattern = @"\d{3}-\d{3}-\d{4}";

        // Act
        var matches = Regexs.Matches(input, pattern);

        // Assert
        matches.Count.ShouldBe(2);
        matches[0].Value.ShouldBe("123-456-7890");
        matches[1].Value.ShouldBe("987-654-3210");
    }

    /// <summary>
    /// 测试 - Matches - 无匹配返回空集合
    /// </summary>
    [Fact]
    public void Matches_NoMatches_ReturnsEmptyCollection()
    {
        // Arrange
        const string input = "No numbers here";
        const string pattern = @"\d+";

        // Act
        var matches = Regexs.Matches(input, pattern);

        // Assert
        matches.Count.ShouldBe(0);
    }

    /// <summary>
    /// 测试 - MatchesCached - 缓存版本功能
    /// </summary>
    [Fact]
    public void MatchesCached_MultipleWords_FindsAllWords()
    {
        // Arrange
        const string input = "Hello World Test";
        const string pattern = @"\b\w+\b";

        // Act
        var matches = Regexs.MatchesCached(input, pattern);

        // Assert
        matches.Count.ShouldBe(3);
        matches[0].Value.ShouldBe("Hello");
        matches[1].Value.ShouldBe("World");
        matches[2].Value.ShouldBe("Test");
    }

    #endregion

    #region Replace 测试

    /// <summary>
    /// 测试 - Replace - 简单字符串替换
    /// </summary>
    [Fact]
    public void Replace_SimpleReplacement_ReplacesCorrectly()
    {
        // Arrange
        const string input = "Hello 123 World 456";
        const string pattern = @"\d+";
        const string replacement = "XXX";

        // Act
        var result = Regexs.Replace(input, pattern, replacement);

        // Assert
        result.ShouldBe("Hello XXX World XXX");
    }

    /// <summary>
    /// 测试 - Replace - 使用捕获组替换
    /// </summary>
    [Fact]
    public void Replace_WithCaptureGroups_UsesCaptureGroupsCorrectly()
    {
        // Arrange
        const string input = "John Doe, Jane Smith";
        const string pattern = @"(\w+)\s+(\w+)";
        const string replacement = "$2, $1";

        // Act
        var result = Regexs.Replace(input, pattern, replacement);

        // Assert
        result.ShouldBe("Doe, John, Smith, Jane");
    }

    /// <summary>
    /// 测试 - Replace - 空输入返回空字符串
    /// </summary>
    [Fact]
    public void Replace_EmptyInput_ReturnsEmptyString()
    {
        // Act
        var result = Regexs.Replace("", @"\d+", "XXX");

        // Assert
        result.ShouldBe("");
    }

    /// <summary>
    /// 测试 - Replace - 空值替换参数异常
    /// </summary>
    [Fact]
    public void Replace_NullReplacement_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Regexs.Replace("test", @"\d+", null));
    }

    /// <summary>
    /// 测试 - ReplaceCached - 缓存版本功能一致
    /// </summary>
    [Fact]
    public void ReplaceCached_SameAsNonCached_ReturnsConsistentResult()
    {
        // Arrange
        const string input = "Replace 123 and 456";
        const string pattern = @"\d+";
        const string replacement = "NUM";

        // Act
        var normalResult = Regexs.Replace(input, pattern, replacement);
        var cachedResult = Regexs.ReplaceCached(input, pattern, replacement);

        // Assert
        normalResult.ShouldBe(cachedResult);
        cachedResult.ShouldBe("Replace NUM and NUM");
    }

    #endregion

    #region Split 测试

    /// <summary>
    /// 测试 - Split - 使用正则表达式分割字符串
    /// </summary>
    [Fact]
    public void Split_WithRegexPattern_SplitsCorrectly()
    {
        // Arrange
        const string input = "apple,banana;orange:grape";
        const string pattern = @"[,;:]";

        // Act
        var result = Regexs.Split(input, pattern);

        // Assert
        result.Length.ShouldBe(4);
        result[0].ShouldBe("apple");
        result[1].ShouldBe("banana");
        result[2].ShouldBe("orange");
        result[3].ShouldBe("grape");
    }

    /// <summary>
    /// 测试 - Split - 空输入返回空数组
    /// </summary>
    [Fact]
    public void Split_EmptyInput_ReturnsEmptyArray()
    {
        // Act
        var result = Regexs.Split("", @"\s+");

        // Assert
        result.Length.ShouldBe(0);
    }

    /// <summary>
    /// 测试 - Split - 无匹配分隔符返回原字符串
    /// </summary>
    [Fact]
    public void Split_NoSeparatorMatches_ReturnsOriginalString()
    {
        // Arrange
        const string input = "noseparators";
        const string pattern = @"\d+";

        // Act
        var result = Regexs.Split(input, pattern);

        // Assert
        result.Length.ShouldBe(1);
        result[0].ShouldBe(input);
    }

    /// <summary>
    /// 测试 - SplitCached - 缓存版本功能
    /// </summary>
    [Fact]
    public void SplitCached_WhitespacePattern_SplitsWords()
    {
        // Arrange
        const string input = "  one   two  three  ";
        const string pattern = @"\s+";

        // Act
        var result = Regexs.SplitCached(input, pattern);

        // Assert - 正则表达式分割会在开头和结尾的空白处产生空字符串
        result.Length.ShouldBe(5); // 修正期望值从 4 改为 5
        result[0].ShouldBe(""); // 开头的空白前
        result[1].ShouldBe("one");
        result[2].ShouldBe("two");
        result[3].ShouldBe("three");
        result[4].ShouldBe(""); // 结尾的空白后
    }

    /// <summary>
    /// 测试 - SplitCached - 缓存版本功能（无前后空白）
    /// </summary>
    [Fact]
    public void SplitCached_WhitespacePattern_SplitsWordsWithoutLeadingTrailing()
    {
        // Arrange
        const string input = "one   two  three";
        const string pattern = @"\s+";

        // Act
        var result = Regexs.SplitCached(input, pattern);

        // Assert
        result.Length.ShouldBe(3);
        result[0].ShouldBe("one");
        result[1].ShouldBe("two");
        result[2].ShouldBe("three");
    }

    #endregion

    #region GetValue 测试

    /// <summary>
    /// 测试 - GetValue - 获取完整匹配值
    /// </summary>
    [Theory]
    [InlineData("123", @"\d+", "", "123")]
    [InlineData("abc123def", @"\d+", "", "123")]
    [InlineData("no match", @"\d+", "", "")]
    [InlineData("", @"\d+", "", "")]
    public void GetValue_WithoutResultPattern_ReturnsExpectedValue(string input, string pattern, string resultPattern, string expected)
    {
        // Act
        var result = Regexs.GetValue(input, pattern, resultPattern);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - GetValue - 使用捕获组模式
    /// </summary>
    [Theory]
    [InlineData("123abc456", @"(\d+)([a-z]+)(\d+)", "$1", "123")]
    [InlineData("123abc456", @"(\d+)([a-z]+)(\d+)", "$2", "abc")]
    [InlineData("123abc456", @"(\d+)([a-z]+)(\d+)", "$3", "456")]
    [InlineData("123abc456", @"\d+([a-z]+)\d+", "$1", "abc")]
    [InlineData("123abc456", @"\d+([a-z]\d+)", "$1", "")]
    public void GetValue_WithCaptureGroups_ReturnsCorrectGroup(string input, string pattern, string resultPattern, string expected)
    {
        // Act
        var result = Regexs.GetValue(input, pattern, resultPattern);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - GetValue - 无效捕获组引用返回空字符串
    /// </summary>
    [Fact]
    public void GetValue_InvalidGroupReference_ReturnsEmptyString()
    {
        // Arrange
        const string input = "test123";
        const string pattern = @"(\d+)";
        const string resultPattern = "$5"; // 不存在的捕获组

        // Act
        var result = Regexs.GetValue(input, pattern, resultPattern);

        // Assert
        result.ShouldBe("");
    }

    /// <summary>
    /// 测试 - GetValue - 空模式参数异常
    /// </summary>
    [Fact]
    public void GetValue_NullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Regexs.GetValue("test", null));
    }

    #endregion

    #region GetValues 测试

    /// <summary>
    /// 测试 - GetValues - 空输入返回空字典
    /// </summary>
    [Fact]
    public void GetValues_EmptyInput_ReturnsEmptyDictionary()
    {
        // Act
        var result = Regexs.GetValues("", @"\d+", null);

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试 - GetValues - 无匹配返回空字典
    /// </summary>
    [Fact]
    public void GetValues_NoMatch_ReturnsEmptyDictionary()
    {
        // Act
        var result = Regexs.GetValues("123abc456", @"\d{5}", new[] { "$1" });

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试 - GetValues - 单个捕获组
    /// </summary>
    [Fact]
    public void GetValues_SingleCaptureGroup_ReturnsCorrectValue()
    {
        // Arrange
        const string input = "123abc456";
        const string pattern = @"(\d*)";
        var resultPatterns = new[] { "$1" };

        // Act
        var result = Regexs.GetValues(input, pattern, resultPatterns);

        // Assert
        result.ShouldContainKey("$1");
        result["$1"].ShouldBe("123");
    }

    /// <summary>
    /// 测试 - GetValues - 多个捕获组
    /// </summary>
    [Fact]
    public void GetValues_MultipleCaptureGroups_ReturnsAllGroups()
    {
        // Arrange
        const string input = "123abc456";
        const string pattern = @"(\d*)([a-z]*)(\d*)";
        var resultPatterns = new[] { "$1", "$2", "$3" };

        // Act
        var result = Regexs.GetValues(input, pattern, resultPatterns);

        // Assert
        result.ShouldContainKey("$1");
        result.ShouldContainKey("$2");
        result.ShouldContainKey("$3");
        result["$1"].ShouldBe("123");
        result["$2"].ShouldBe("abc");
        result["$3"].ShouldBe("456");
    }

    /// <summary>
    /// 测试 - GetValues - 空结果模式数组处理
    /// </summary>
    [Fact]
    public void GetValues_NullResultPatterns_ReturnsWholeMatch()
    {
        // Arrange
        const string input = "test123";
        const string pattern = @"\d+";

        // Act
        var result = Regexs.GetValues(input, pattern, null);

        // Assert
        result.ShouldContainKey("");
        result[""].ShouldBe("123");
    }

    /// <summary>
    /// 测试 - GetValues - 重复键处理
    /// </summary>
    [Fact]
    public void GetValues_DuplicatePatterns_HandlesKeyConflicts()
    {
        // Arrange
        const string input = "123abc456";
        const string pattern = @"(\d+)([a-z]+)(\d+)";
        var resultPatterns = new[] { "$1", "$1", "$2" }; // 重复的 $1

        // Act
        var result = Regexs.GetValues(input, pattern, resultPatterns);

        // Assert
        result.Count.ShouldBe(3);
        result.ShouldContainKey("$1");
        result.ShouldContainKey("$1_1"); // 重复键的处理
        result.ShouldContainKey("$2");
    }

    #endregion

    #region 边界条件和异常测试

    /// <summary>
    /// 测试 - 各方法 - 处理特殊字符
    /// </summary>
    [Fact]
    public void RegexMethods_SpecialCharacters_HandleCorrectly()
    {
        // Arrange
        const string input = "Price: $99.99 (10% off)";
        const string pattern = @"\$(\d+\.\d+)";

        // Act & Assert
        Regexs.IsMatch(input, pattern).ShouldBeTrue();
        Regexs.Match(input, pattern).Groups[1].Value.ShouldBe("99.99");
        Regexs.Replace(input, pattern, "PRICE").ShouldContain("PRICE");
    }

    /// <summary>
    /// 测试 - 各方法 - 处理 Unicode 字符
    /// </summary>
    [Fact]
    public void RegexMethods_UnicodeCharacters_HandleCorrectly()
    {
        // Arrange
        const string input = "你好123世界456";
        const string pattern = @"\d+";

        // Act & Assert
        var matches = Regexs.Matches(input, pattern);
        matches.Count.ShouldBe(2);
        matches[0].Value.ShouldBe("123");
        matches[1].Value.ShouldBe("456");
    }

    /// <summary>
    /// 测试 - 各方法 - 大字符串性能
    /// </summary>
    [Fact]
    public void RegexMethods_LargeString_PerformsReasonably()
    {
        // Arrange
        var largeInput = new string('a', 10000) + "123" + new string('b', 10000);
        const string pattern = @"\d+";

        // Act & Assert (这里主要测试不会抛异常，性能测试在实际场景中进行)
        Should.NotThrow(() =>
        {
            var isMatch = Regexs.IsMatchCached(largeInput, pattern);
            var match = Regexs.MatchCached(largeInput, pattern);
            isMatch.ShouldBeTrue();
            match.Value.ShouldBe("123");
        });
    }

    #endregion

    #region 缓存功能测试

    /// <summary>
    /// 测试 - 缓存方法 - 多次调用使用缓存
    /// </summary>
    [Fact]
    public void CachedMethods_MultipleCallsWithSamePattern_UsesCache()
    {
        // Arrange
        const string input1 = "test123";
        const string input2 = "example456";
        const string pattern = @"\d+";

        // Act - 多次调用相同模式
        var result1 = Regexs.IsMatchCached(input1, pattern);
        var result2 = Regexs.IsMatchCached(input2, pattern);
        var match1 = Regexs.MatchCached(input1, pattern);
        var match2 = Regexs.MatchCached(input2, pattern);

        // Assert
        result1.ShouldBeTrue();
        result2.ShouldBeTrue();
        match1.Value.ShouldBe("123");
        match2.Value.ShouldBe("456");
    }

    /// <summary>
    /// 测试 - 缓存与非缓存方法 - 结果一致性
    /// </summary>
    [Theory]
    [InlineData("hello123world", @"\d+")]
    [InlineData("test@email.com", @"\w+@\w+\.\w+")]
    [InlineData("no-match", @"\d{10}")]
    public void CachedVsNonCached_SamePattern_ReturnsSameResults(string input, string pattern)
    {
        // Act
        var cachedMatch = Regexs.IsMatchCached(input, pattern);
        var normalMatch = Regexs.IsMatch(input, pattern);

        var cachedResult = Regexs.MatchCached(input, pattern);
        var normalResult = Regexs.Match(input, pattern);

        // Assert
        cachedMatch.ShouldBe(normalMatch);
        cachedResult.Success.ShouldBe(normalResult.Success);
        cachedResult.Value.ShouldBe(normalResult.Value);
    }

    #endregion
}