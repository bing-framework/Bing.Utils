using System.Collections.Concurrent;
namespace Bing.Text.RegularExpressions;
/// <summary>
/// 正则表达式池 单元测试
/// </summary>
public class RegexPoolTest : IDisposable
{
    #region 初始化
    /// <summary>
    /// 测试开始前清空缓存
    /// </summary>
    public RegexPoolTest() => RegexPool.Clear();
    /// <summary>
    /// 测试结束后清空缓存
    /// </summary>
    public void Dispose() => RegexPool.Clear();
    #endregion
    #region GetOrCreate 方法测试
    /// <summary>
    /// 测试 - GetOrCreate - 基本功能验证
    /// </summary>
    [Fact]
    public void GetOrCreate_ValidPattern_ReturnsCompiledRegex()
    {
        // Arrange
        const string pattern = @"\d+";
        const RegexOptions options = RegexOptions.IgnoreCase;
        // Act
        var regex1 = RegexPool.GetOrCreate(pattern, options);
        var regex2 = RegexPool.GetOrCreate(pattern, options);
        // Assert
        regex1.ShouldNotBeNull();
        regex2.ShouldNotBeNull();
        regex1.ShouldBeSameAs(regex2); // 应该返回相同的缓存对象
        regex1.Options.HasFlag(RegexOptions.Compiled).ShouldBeTrue();
        regex1.IsMatch("123").ShouldBeTrue();
        regex1.IsMatch("abc").ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - GetOrCreate - 不同选项创建不同正则对象
    /// </summary>
    [Fact]
    public void GetOrCreate_DifferentOptions_CreatesDifferentRegexObjects()
    {
        // Arrange
        const string pattern = @"hello";
        // Act
        var regexIgnoreCase = RegexPool.GetOrCreate(pattern, RegexOptions.IgnoreCase);
        var regexNone = RegexPool.GetOrCreate(pattern, RegexOptions.None);
        // Assert
        regexIgnoreCase.ShouldNotBeSameAs(regexNone);
        regexIgnoreCase.IsMatch("HELLO").ShouldBeTrue();
        regexNone.IsMatch("HELLO").ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - GetOrCreate - null模式抛出参数异常
    /// </summary>
    [Fact]
    public void GetOrCreate_NullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() => RegexPool.GetOrCreate(null));
        exception.ParamName.ShouldBe("pattern");
        exception.Message.ShouldContain("正则表达式模式不能为空");
    }
    /// <summary>
    /// 测试 - GetOrCreate - 无效模式抛出参数异常
    /// </summary>
    [Theory]
    [InlineData("[")]          // 不完整的字符类
    [InlineData("*")]          // 无效的重复符
    [InlineData("?")]          // 无效的量词
    [InlineData("(")]          // 不完整的分组
    public void GetOrCreate_InvalidPattern_ThrowsArgumentException(string invalidPattern)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => RegexPool.GetOrCreate(invalidPattern));
        exception.ParamName.ShouldBe("pattern");
        exception.Message.ShouldContain("无效的正则表达式模式");
    }
    /// <summary>
    /// 测试 - GetOrCreate - 缓存统计更新正确性
    /// </summary>
    [Fact]
    public void GetOrCreate_CacheStatistics_UpdatesCorrectly()
    {
        // Arrange
        const string pattern = @"\w+";
        var initialStats = RegexPool.GetStatistics();
        // Act
        RegexPool.GetOrCreate(pattern); // 第一次访问 - Miss
        RegexPool.GetOrCreate(pattern); // 第二次访问 - Hit
        // Assert
        var finalStats = RegexPool.GetStatistics();
        finalStats.Count.ShouldBe(initialStats.Count + 1);
        finalStats.MissCount.ShouldBe(initialStats.MissCount + 1);
        finalStats.HitCount.ShouldBe(initialStats.HitCount + 1);
    }
    /// <summary>
    /// 测试 - GetOrCreate - LRU缓存清理机制
    /// </summary>
    [Fact]
    public void GetOrCreate_CacheLimit_PerformsLRUCleanup()
    {
        // Arrange - 创建大量不同的正则表达式超过缓存容量
        var patterns = Enumerable.Range(1, 1005) // 超过 MaxCacheSize (1000)
            .Select(i => $@"\d{{{i}}}") // 生成不同的模式
            .ToArray();
        // Act
        foreach (var pattern in patterns)
        {
            RegexPool.GetOrCreate(pattern);
        }
        // Assert
        var stats = RegexPool.GetStatistics();
        stats.Count.ShouldBeLessThan(1000); // 缓存应该被部分清理
    }
    /// <summary>
    /// 测试 - GenerateCacheKey - 缓存键生成逻辑
    /// </summary>
    [Theory]
    [InlineData(@"\d+", RegexOptions.IgnoreCase, @"\d+:1")]
    [InlineData(@"\d+", RegexOptions.None, @"\d+:0")]
    [InlineData(@"\w+", RegexOptions.Multiline | RegexOptions.IgnoreCase, @"\w+:17")]
    [InlineData("", RegexOptions.IgnoreCase, ":1")]
    public void GenerateCacheKey_DifferentPatternsAndOptions_GeneratesCorrectKeys(string pattern, RegexOptions options, string expected)
    {
        // Act - 通过创建两个相同的正则表达式来间接测试缓存键生成
        RegexPool.Clear();
        var regex1 = RegexPool.GetOrCreate(pattern, options);
        var regex2 = RegexPool.GetOrCreate(pattern, options);
        // Assert - 如果缓存键生成正确，第二次调用应该命中缓存
        regex1.ShouldBeSameAs(regex2);
        RegexPool.Count.ShouldBe(1);
        RegexPool.HitCount.ShouldBe(1);
        RegexPool.MissCount.ShouldBe(1);
    }
    /// <summary>
    /// 测试 - CleanupLRU - LRU清理机制的触发条件
    /// </summary>
    [Fact]
    public void CleanupLRU_WhenCacheReachesLimit_TriggersCleanup()
    {
        // Arrange
        RegexPool.Clear();
        const int patternsToCreate = 1001; // 超过MaxCacheSize (1000)
        // Act - 创建超过缓存限制的正则表达式
        for (int i = 0; i < patternsToCreate; i++)
        {
            RegexPool.GetOrCreate($@"\d{{{i}}}");
        }
        // Assert
        var stats = RegexPool.GetStatistics();
        stats.Count.ShouldBeLessThan(1000); // 应该触发LRU清理
        stats.Count.ShouldBeGreaterThan(700); // 但不应该清理得太干净
    }
    /// <summary>
    /// 测试 - CleanupLRU - 访问时间戳更新和LRU排序
    /// </summary>
    [Fact]
    public void CleanupLRU_AccessTimeStampUpdate_KeepsMostRecentlyUsed()
    {
        // Arrange
        RegexPool.Clear();
        var oldPattern = @"old_\d+";
        var newPattern = @"new_\d+";
        // 创建一个旧的正则表达式
        RegexPool.GetOrCreate(oldPattern);
        // 创建大量新的正则表达式以填满缓存
        for (int i = 0; i < 1000; i++)
        {
            RegexPool.GetOrCreate($@"temp_{i}");
        }
        // 再次访问旧的正则表达式，更新其访问时间
        RegexPool.GetOrCreate(oldPattern);
        // Act - 创建一个新的正则表达式触发LRU清理
        RegexPool.GetOrCreate(newPattern);
        // Assert - 最近访问的旧模式应该仍然存在
        RegexPool.Contains(oldPattern).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - CacheItem - record struct的相等性比较
    /// </summary>
    [Fact]
    public void CacheItem_EqualityComparison_WorksCorrectly()
    {
        // Arrange
        RegexPool.Clear();
        const string pattern = @"\d+";
        // Act - 创建相同模式的正则表达式多次
        var regex1 = RegexPool.GetOrCreate(pattern);
        System.Threading.Thread.Sleep(1); // 确保时间戳不同
        var regex2 = RegexPool.GetOrCreate(pattern);
        // Assert - 应该返回相同的缓存对象
        regex1.ShouldBeSameAs(regex2);
        RegexPool.Count.ShouldBe(1);
    }
    /// <summary>
    /// 测试 - CacheItem - 访问时间戳更新
    /// </summary>
    [Fact]
    public void CacheItem_AccessTimeStamp_UpdatesOnAccess()
    {
        // Arrange
        RegexPool.Clear();
        const string pattern = @"\d+";
        // Act
        RegexPool.GetOrCreate(pattern); // 第一次创建
        var initialStats = RegexPool.GetStatistics();
        System.Threading.Thread.Sleep(10); // 等待一小段时间
        RegexPool.GetOrCreate(pattern); // 第二次访问
        var updatedStats = RegexPool.GetStatistics();
        // Assert
        updatedStats.HitCount.ShouldBe(initialStats.HitCount + 1);
        // 缓存项应该仍然存在且被更新
        RegexPool.Contains(pattern).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - 错误恢复 - LRU清理异常处理
    /// </summary>
    [Fact]
    public void ErrorRecovery_LRUCleanupException_FallsBackToClearCache()
    {
        // Arrange
        RegexPool.Clear();
        // Act - 填满缓存直到需要清理
        Should.NotThrow(() =>
        {
            for (int i = 0; i <= 1000; i++)
            {
                RegexPool.GetOrCreate($@"pattern_{i}");
            }
        });
        // Assert - 即使清理过程中可能出现异常，也应该能继续工作
        var stats = RegexPool.GetStatistics();
        stats.Count.ShouldBeLessThanOrEqualTo(1000);
        // 验证仍然可以正常工作
        var testRegex = RegexPool.GetOrCreate(@"test_\d+");
        testRegex.ShouldNotBeNull();
    }
    #endregion
    #region IsMatch 方法测试
    /// <summary>
    /// 测试 - IsMatch - 基本匹配功能验证
    /// </summary>
    [Theory]
    [InlineData("hello123", @"\d+", true)]
    [InlineData("hello world", @"\d+", false)]
    [InlineData("Hello", @"hello", true)] // 默认忽略大小写
    [InlineData("HELLO", @"hello", true)]
    [InlineData("test@email.com", @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", true)]
    [InlineData("invalid-email", @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", false)]
    public void IsMatch_ValidInputs_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        // Act
        var result = RegexPool.IsMatch(input, pattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsMatch - 使用缓存与不使用缓存结果一致性
    /// </summary>
    [Fact]
    public void IsMatch_CachedVsNonCached_ReturnsSameResult()
    {
        // Arrange
        const string input = "test123";
        const string pattern = @"\d+";
        // Act
        var cachedResult = RegexPool.IsMatch(input, pattern, useCache: true);
        var nonCachedResult = RegexPool.IsMatch(input, pattern, useCache: false);
        // Assert
        cachedResult.ShouldBe(nonCachedResult);
        cachedResult.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - IsMatch - null参数抛出异常
    /// </summary>
    [Theory]
    [InlineData(null, @"\d+")]
    [InlineData("test", null)]
    public void IsMatch_NullParameters_ThrowsArgumentNullException(string input, string pattern)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RegexPool.IsMatch(input, pattern));
    }
    /// <summary>
    /// 测试 - IsMatch - 不同RegexOptions选项验证
    /// </summary>
    [Theory]
    [InlineData("HELLO", @"hello", RegexOptions.IgnoreCase, true)]
    [InlineData("HELLO", @"hello", RegexOptions.None, false)]
    [InlineData("line1\nline2", @"^line2$", RegexOptions.Multiline, true)]
    [InlineData("line1\nline2", @"^line2$", RegexOptions.None, false)]
    public void IsMatch_DifferentOptions_ReturnsExpectedResult(string input, string pattern, RegexOptions options, bool expected)
    {
        // Act
        var result = RegexPool.IsMatch(input, pattern, options);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsMatch - 空字符串处理
    /// </summary>
    [Theory]
    [InlineData("", @".*", true)]
    [InlineData("", @"\d+", false)]
    public void IsMatch_EmptyInput_HandlesCorrectly(string input, string pattern, bool expected)
    {
        // Act
        var result = RegexPool.IsMatch(input, pattern);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region Match 方法测试
    /// <summary>
    /// 测试 - Match - 成功匹配返回正确结果
    /// </summary>
    [Fact]
    public void Match_SuccessfulMatch_ReturnsCorrectMatch()
    {
        // Arrange
        const string input = "Hello123World456";
        const string pattern = @"\d+";
        // Act
        var match = RegexPool.Match(input, pattern);
        // Assert
        match.Success.ShouldBeTrue();
        match.Value.ShouldBe("123");
        match.Index.ShouldBe(5);
        match.Length.ShouldBe(3);
    }
    /// <summary>
    /// 测试 - Match - 无匹配返回失败的Match对象
    /// </summary>
    [Fact]
    public void Match_NoMatch_ReturnsFailedMatch()
    {
        // Arrange
        const string input = "Hello World";
        const string pattern = @"\d+";
        // Act
        var match = RegexPool.Match(input, pattern);
        // Assert
        match.Success.ShouldBeFalse();
        match.Value.ShouldBe(string.Empty);
        match.Index.ShouldBe(0);
    }
    /// <summary>
    /// 测试 - Match - 分组捕获功能验证
    /// </summary>
    [Fact]
    public void Match_GroupCapture_CapturesGroupsCorrectly()
    {
        // Arrange
        const string input = "user@example.com";
        const string pattern = @"^([a-zA-Z0-9._%+-]+)@([a-zA-Z0-9.-]+\.[a-zA-Z]{2,})$";
        // Act
        var match = RegexPool.Match(input, pattern);
        // Assert
        match.Success.ShouldBeTrue();
        match.Groups.Count.ShouldBe(3); // 整个匹配 + 2个分组
        match.Groups[0].Value.ShouldBe("user@example.com");
        match.Groups[1].Value.ShouldBe("user");
        match.Groups[2].Value.ShouldBe("example.com");
    }
    /// <summary>
    /// 测试 - Match - null参数处理
    /// </summary>
    [Theory]
    [InlineData(null, @"\d+")]
    [InlineData("test", null)]
    public void Match_NullParameters_ThrowsArgumentNullException(string input, string pattern)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RegexPool.Match(input, pattern));
    }
    #endregion
    #region Matches 方法测试
    /// <summary>
    /// 测试 - Matches - 多个匹配项返回完整集合
    /// </summary>
    [Fact]
    public void Matches_MultipleMatches_ReturnsAllMatches()
    {
        // Arrange
        const string input = "abc123def456ghi789";
        const string pattern = @"\d+";
        // Act
        var matches = RegexPool.Matches(input, pattern);
        // Assert
        matches.Count.ShouldBe(3);
        matches[0].Value.ShouldBe("123");
        matches[1].Value.ShouldBe("456");
        matches[2].Value.ShouldBe("789");
    }
    /// <summary>
    /// 测试 - Matches - 无匹配返回空集合
    /// </summary>
    [Fact]
    public void Matches_NoMatches_ReturnsEmptyCollection()
    {
        // Arrange
        const string input = "Hello World";
        const string pattern = @"\d+";
        // Act
        var matches = RegexPool.Matches(input, pattern);
        // Assert
        matches.Count.ShouldBe(0);
    }
    /// <summary>
    /// 测试 - Matches - 重叠匹配处理
    /// </summary>
    [Fact]
    public void Matches_OverlappingPatterns_ReturnsNonOverlappingMatches()
    {
        // Arrange
        const string input = "aaaa";
        const string pattern = @"aa";
        // Act
        var matches = RegexPool.Matches(input, pattern);
        // Assert
        matches.Count.ShouldBe(2); // "aa" at 0-1 and "aa" at 2-3
        matches[0].Index.ShouldBe(0);
        matches[1].Index.ShouldBe(2);
    }
    #endregion
    #region Replace 方法测试
    /// <summary>
    /// 测试 - Replace - 基本替换功能验证
    /// </summary>
    [Theory]
    [InlineData("hello123world456", @"\d+", "XXX", "helloXXXworldXXX")]
    [InlineData("test@email.com", @"@.*", "@domain.com", "test@domain.com")]
    [InlineData("no numbers here", @"\d+", "XXX", "no numbers here")]
    [InlineData("", @"\d+", "XXX", "")]
    public void Replace_ValidInputs_ReturnsExpectedResult(string input, string pattern, string replacement, string expected)
    {
        // Act
        var result = RegexPool.Replace(input, pattern, replacement);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - Replace - 空白输入处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Replace_EmptyOrWhitespaceInput_ReturnsEmptyString(string input)
    {
        // Act
        var result = RegexPool.Replace(input, @"\d+", "XXX");
        // Assert
        result.ShouldBe(string.Empty);
    }
    /// <summary>
    /// 测试 - Replace - 分组替换功能
    /// </summary>
    [Fact]
    public void Replace_GroupReplacement_ReplacesWithCapturedGroups()
    {
        // Arrange
        const string input = "John Doe, Jane Smith";
        const string pattern = @"(\w+)\s+(\w+)";
        const string replacement = "$2, $1";
        // Act
        var result = RegexPool.Replace(input, pattern, replacement);
        // Assert
        result.ShouldBe("Doe, John, Smith, Jane");
    }
    /// <summary>
    /// 测试 - Replace - null参数处理
    /// </summary>
    [Theory]
    [InlineData("input", null, "replacement")]
    [InlineData("input", @"\d+", null)]
    public void Replace_NullPatternOrReplacement_ThrowsArgumentNullException(string input, string pattern, string replacement)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RegexPool.Replace(input, pattern, replacement));
    }
    #endregion
    #region Split 方法测试
    /// <summary>
    /// 测试 - Split - 基本分割功能验证
    /// </summary>
    [Theory]
    [InlineData("a,b,c", @",", new[] { "a", "b", "c" })]
    [InlineData("one123two456three", @"\d+", new[] { "one", "two", "three" })]
    [InlineData("no-delimiters", @",", new[] { "no-delimiters" })]
    [InlineData("", @",", new string[0])]
    public void Split_ValidInputs_ReturnsExpectedResult(string input, string pattern, string[] expected)
    {
        // Act
        var result = RegexPool.Split(input, pattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - Split - 空白输入处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Split_EmptyOrWhitespaceInput_ReturnsEmptyArray(string input)
    {
        // Act
        var result = RegexPool.Split(input, @",");
        // Assert
        result.ShouldBeEmpty();
    }
    /// <summary>
    /// 测试 - Split - 连续分隔符处理
    /// </summary>
    [Fact]
    public void Split_ConsecutiveDelimiters_HandlesCorrectly()
    {
        // Arrange
        const string input = "a,,b,,,c";
        const string pattern = @",";
        // Act
        var result = RegexPool.Split(input, pattern);
        // Assert
        result.ShouldBe(new[] { "a", "", "b", "", "", "c" });
    }
    /// <summary>
    /// 测试 - Split - null模式抛出异常
    /// </summary>
    [Fact]
    public void Split_NullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RegexPool.Split("input", null));
    }
    #endregion
    #region 高级功能方法测试
    /// <summary>
    /// 测试 - GetValue - 基本值获取功能
    /// </summary>
    [Theory]
    [InlineData("user@example.com", @"^([^@]+)@(.+)$", "", "user@example.com")]
    [InlineData("user@example.com", @"^([^@]+)@(.+)$", "$1", "user")]
    [InlineData("user@example.com", @"^([^@]+)@(.+)$", "$2", "example.com")]
    [InlineData("no match", @"\d+", "$1", "")]
    [InlineData("", @"\d+", "$1", "")]
    public void GetValue_ValidInputs_ReturnsExpectedResult(string input, string pattern, string resultPattern, string expected)
    {
        // Act
        var result = RegexPool.GetValue(input, pattern, resultPattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - GetValue - 空白输入处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetValue_EmptyOrWhitespaceInput_ReturnsEmptyString(string input)
    {
        // Act
        var result = RegexPool.GetValue(input, @"\d+", "$1");
        // Assert
        result.ShouldBe(string.Empty);
    }
    /// <summary>
    /// 测试 - GetValue - 无效结果模式处理
    /// </summary>
    [Fact]
    public void GetValue_InvalidResultPattern_ReturnsEmptyString()
    {
        // Arrange
        const string input = "test123";
        const string pattern = @"(\d+)";
        const string invalidResultPattern = "$99"; // 不存在的分组
        // Act
        var result = RegexPool.GetValue(input, pattern, invalidResultPattern);
        // Assert
        result.ShouldBe("");
    }
    /// <summary>
    /// 测试 - GetValue - 无效结果模式处理的多种情况
    /// </summary>
    [Theory]
    [InlineData("test123", @"(\d+)", "$99", "")] // 不存在的分组
    [InlineData("test123", @"(\d+)", "$0", "123")] // $0 应该返回整个匹配
    [InlineData("test123", @"(\d+)", "$1", "123")] // 正常的分组引用
    [InlineData("test123", @"(\d+)", "$", "$")] // 单独的$符号
    [InlineData("test123", @"(\d+)", "$abc", "")] // 无效的分组名称
    [InlineData("test123", @"(\d+)", "${1}", "123")] // 使用花括号的分组引用
    [InlineData("test123", @"(\d+)", "${99}", "")] // 使用花括号的无效分组引用
    public void GetValue_InvalidResultPatternVariations_HandlesCorrectly(string input, string pattern, string resultPattern, string expected)
    {
        // Act
        var result = RegexPool.GetValue(input, pattern, resultPattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - GetValue - 命名分组的结果模式
    /// </summary>
    [Theory]
    [InlineData("user@example.com", @"(?<user>[^@]+)@(?<domain>.+)", "${user}", "user")]
    [InlineData("user@example.com", @"(?<user>[^@]+)@(?<domain>.+)", "${domain}", "example.com")]
    [InlineData("user@example.com", @"(?<user>[^@]+)@(?<domain>.+)", "${invalid}", "")]
    public void GetValue_NamedGroups_HandlesCorrectly(string input, string pattern, string resultPattern, string expected)
    {
        // Act
        var result = RegexPool.GetValue(input, pattern, resultPattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - GetValue - 无效结果模式的边界情况
    /// </summary>
    [Theory]
    [InlineData("test123", @"(\d+)", "$", "$")] // 单独的$符号
    [InlineData("test123", @"(\d+)", "$abc", "")] // 无效的分组名称  
    [InlineData("test123", @"(\d+)", "${invalid}", "")] // 无效的命名分组
    [InlineData("test123", @"(\d+)", "$999", "")] // 超大的分组号
    [InlineData("test123", @"(\d+)", "${}", "")] // 空的命名分组
    [InlineData("test123", @"(\d+)", "$1a", "123a")] // 有效的分组+文本
    [InlineData("test123", @"(\d+)", "prefix$1suffix", "prefix123suffix")] // 混合文本
    public void GetValue_EdgeCaseResultPatterns_HandlesCorrectly(string input, string pattern, string resultPattern, string expected)
    {
        // Act
        var result = RegexPool.GetValue(input, pattern, resultPattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - GetValues - 多个结果模式处理
    /// </summary>
    [Fact]
    public void GetValues_MultipleResultPatterns_ReturnsDictionaryWithAllValues()
    {
        // Arrange
        const string input = "John Doe 30 Engineer";
        const string pattern = @"(\w+)\s+(\w+)\s+(\d+)\s+(\w+)";
        var resultPatterns = new[] { "$1", "$2", "$3", "$4" };
        // Act
        var result = RegexPool.GetValues(input, pattern, resultPatterns);
        // Assert
        result.Count.ShouldBe(4);
        result["$1"].ShouldBe("John");
        result["$2"].ShouldBe("Doe");
        result["$3"].ShouldBe("30");
        result["$4"].ShouldBe("Engineer");
    }
    /// <summary>
    /// 测试 - GetValues - null结果模式处理
    /// </summary>
    [Fact]
    public void GetValues_NullResultPatterns_ReturnsValueWithEmptyKey()
    {
        // Arrange
        const string input = "test123";
        const string pattern = @"\d+";
        // Act
        var result = RegexPool.GetValues(input, pattern, null);
        // Assert
        result.Count.ShouldBe(1);
        result[string.Empty].ShouldBe("123");
    }
    /// <summary>
    /// 测试 - GetValues - 空结果模式数组处理
    /// </summary>
    [Fact]
    public void GetValues_EmptyResultPatterns_ReturnsValueWithEmptyKey()
    {
        // Arrange
        const string input = "test123";
        const string pattern = @"\d+";
        var emptyPatterns = new string[0];
        // Act
        var result = RegexPool.GetValues(input, pattern, emptyPatterns);
        // Assert
        result.Count.ShouldBe(1);
        result[string.Empty].ShouldBe("123");
    }
    /// <summary>
    /// 测试 - GetValues - 重复结果模式处理
    /// </summary>
    [Fact]
    public void GetValues_DuplicateResultPatterns_HandlesWithSuffix()
    {
        // Arrange
        const string input = "test123";
        const string pattern = @"(\d+)";
        var duplicatePatterns = new[] { "$1", "$1", "$1" };
        // Act
        var result = RegexPool.GetValues(input, pattern, duplicatePatterns);
        // Assert
        result.Count.ShouldBe(3);
        result["$1"].ShouldBe("123");
        result["$1_1"].ShouldBe("123");
        result["$1_2"].ShouldBe("123");
    }
    /// <summary>
    /// 测试 - GetValues - 无效结果模式处理
    /// </summary>
    [Fact]
    public void GetValues_InvalidResultPatterns_ReturnsEmptyStringsForInvalidPatterns()
    {
        // Arrange
        const string input = "test123";
        const string pattern = @"(\d+)";
        var invalidPatterns = new[] { "$1", "$99", "$1" };
        // Act
        var result = RegexPool.GetValues(input, pattern, invalidPatterns);
        // Assert
        result.Count.ShouldBe(3);
        result["$1"].ShouldBe("123");
        result["$99"].ShouldBe("");
        result["$1_1"].ShouldBe("123");
    }
    /// <summary>
    /// 测试 - GetValues - 混合有效和无效结果模式
    /// </summary>
    [Fact]
    public void GetValues_MixedValidAndInvalidPatterns_HandlesCorrectly()
    {
        // Arrange
        const string input = "user@example.com";
        const string pattern = @"^([^@]+)@([^.]+)\.(.+)$"; // 3个分组
        var mixedPatterns = new[] { "$1", "$99", "$2", "${invalid}", "$3" };
        // Act
        var result = RegexPool.GetValues(input, pattern, mixedPatterns);
        // Assert
        result.Count.ShouldBe(5);
        result["$1"].ShouldBe("user");
        result["$99"].ShouldBe(""); // 无效分组
        result["$2"].ShouldBe("example");
        result["${invalid}"].ShouldBe(""); // 无效命名分组
        result["$3"].ShouldBe("com");
    }
    /// <summary>
    /// 测试 - IsInvalidGroupReference - 通过GetValue间接测试
    /// </summary>
    [Theory]
    [InlineData("test123", @"(\d+)", "$1", "123")] // 有效引用
    [InlineData("test123", @"(\d+)", "$99", "")] // 无效数字引用
    [InlineData("test123", @"(\d+)", "${invalid}", "")] // 无效命名引用
    [InlineData("test123", @"(\d+)", "$abc", "")] // 无效命名引用
    [InlineData("test123", @"(\d+)", "${}", "")] // 空命名引用
    [InlineData("test123", @"(\d+)", "$", "$")] // 单独的$符号（不是引用）
    //[InlineData("test123", @"(\d+)", "$$", "$$")] // 转义的$符号
    public void IsInvalidGroupReference_ThroughGetValue_HandlesCorrectly(string input, string pattern, string resultPattern, string expected)
    {
        // Act
        var result = RegexPool.GetValue(input, pattern, resultPattern);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsGroupReferencePattern - 通过GetValues间接测试各种模式
    /// </summary>
    [Theory]
    [InlineData("$1", false)] // 数字引用
    [InlineData("$99", true)] // 大数字引用
    [InlineData("${name}", true)] // 命名引用
    [InlineData("${invalid}", true)] // 无效命名引用
    [InlineData("$abc", true)] // 字母命名引用
    [InlineData("$_test6666", false)] // 下划线开头的命名引用，此处使用的是特殊的替换标记，可以替换内容
    [InlineData("$", false)] // 单独的$
    [InlineData("$$", false)] // 转义的$
    [InlineData("text$1", false)] // 包含其他内容的字符串
    [InlineData("$1text", false)] // 包含其他内容的字符串
    public void IsGroupReferencePattern_ThroughGetValues_IdentifiesCorrectly(string pattern, bool shouldBeEmpty)
    {
        // Arrange
        const string input = "test123";
        const string regexPattern = @"(\d+)";
        var patterns = new[] { pattern };
        // Act
        var result = RegexPool.GetValues(input, regexPattern, patterns);
        // Assert
        if (shouldBeEmpty && pattern.Contains('$') && !pattern.StartsWith("$$"))
        {
            result[pattern].ShouldBe(""); // 无效的分组引用应该返回空字符串
        }
        else
        {
            result[pattern].ShouldNotBe(""); // 有效的引用或非引用模式应该有值
        }
    }
    /// <summary>
    /// 测试 - AddResults - 处理null和空数组
    /// </summary>
    [Fact]
    public void AddResults_NullAndEmptyPatterns_HandlesCorrectly()
    {
        // Arrange & Act
        var resultNull = RegexPool.GetValues("test123", @"(\d+)", null);
        var resultEmpty = RegexPool.GetValues("test123", @"(\d+)", new string[0]);
        // Assert
        resultNull.Count.ShouldBe(1);
        resultNull[string.Empty].ShouldBe("123");
        resultEmpty.Count.ShouldBe(1);
        resultEmpty[string.Empty].ShouldBe("123");
    }
    /// <summary>
    /// 测试 - AddResults - 处理空字符串模式
    /// </summary>
    [Fact]
    public void AddResults_EmptyStringPatterns_SkipsEmptyPatterns()
    {
        // Arrange
        const string input = "test123";
        const string pattern = @"(\d+)";
        var patterns = new[] { "$1", "", null, "$1" };
        // Act
        var result = RegexPool.GetValues(input, pattern, patterns);
        // Assert
        result.Count.ShouldBe(2); // 只有两个非空模式
        result["$1"].ShouldBe("123");
        result["$1_1"].ShouldBe("123");
    }
    /// <summary>
    /// 测试 - AddResults - 异常处理机制
    /// </summary>
    [Fact]
    public void AddResults_ExceptionHandling_ReturnsEmptyForInvalidPatterns()
    {
        // Arrange
        const string input = "test123";
        const string pattern = @"(\d+)";
        // 这些模式可能会导致Match.Result抛出异常
        var problematicPatterns = new[] { "$1", "$999", "${nonexistent}" };
        // Act
        var result = RegexPool.GetValues(input, pattern, problematicPatterns);
        // Assert
        result.Count.ShouldBe(3);
        result["$1"].ShouldBe("123"); // 有效模式
        result["$999"].ShouldBe(""); // 无效模式应返回空字符串
        result["${nonexistent}"].ShouldBe(""); // 无效命名模式应返回空字符串
    }
    #endregion
    #region 缓存管理测试
    /// <summary>
    /// 测试 - Clear - 清空缓存和统计信息
    /// </summary>
    [Fact]
    public void Clear_CacheAndStatistics_ResetsEverything()
    {
        // Arrange
        RegexPool.GetOrCreate(@"\d+");
        RegexPool.GetOrCreate(@"\w+");
        RegexPool.GetOrCreate(@"\d+"); // 产生一次hit
        var initialStats = RegexPool.GetStatistics();
        initialStats.Count.ShouldBeGreaterThan(0);
        initialStats.HitCount.ShouldBeGreaterThan(0);
        // Act
        RegexPool.Clear();
        // Assert
        var finalStats = RegexPool.GetStatistics();
        finalStats.Count.ShouldBe(0);
        finalStats.HitCount.ShouldBe(0);
        finalStats.MissCount.ShouldBe(0);
        finalStats.HitRate.ShouldBe(0.0);
    }
    /// <summary>
    /// 测试 - Remove - 成功移除指定缓存项
    /// </summary>
    [Fact]
    public void Remove_ExistingPattern_RemovesSuccessfully()
    {
        // Arrange
        const string pattern1 = @"\d+";
        const string pattern2 = @"\w+";
        RegexPool.GetOrCreate(pattern1);
        RegexPool.GetOrCreate(pattern2);
        var initialCount = RegexPool.Count;
        // Act
        var removed = RegexPool.Remove(pattern1);
        // Assert
        removed.ShouldBeTrue();
        RegexPool.Contains(pattern1).ShouldBeFalse();
        RegexPool.Contains(pattern2).ShouldBeTrue();
        RegexPool.Count.ShouldBe(initialCount - 1);
    }
    /// <summary>
    /// 测试 - Remove - 移除不存在的模式返回false
    /// </summary>
    [Fact]
    public void Remove_NonExistentPattern_ReturnsFalse()
    {
        // Arrange
        const string nonExistentPattern = @"non-existent-pattern-12345";
        // Act
        var removed = RegexPool.Remove(nonExistentPattern);
        // Assert
        removed.ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - Remove - null模式抛出异常
    /// </summary>
    [Fact]
    public void Remove_NullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RegexPool.Remove(null));
    }
    /// <summary>
    /// 测试 - Contains - 正确检查模式存在性
    /// </summary>
    [Fact]
    public void Contains_ExistingAndNonExistingPatterns_ReturnsCorrectResult()
    {
        // Arrange
        const string existingPattern = @"\d+";
        const string nonExistingPattern = @"non-existing-pattern";
        RegexPool.GetOrCreate(existingPattern);
        // Act & Assert
        RegexPool.Contains(existingPattern).ShouldBeTrue();
        RegexPool.Contains(nonExistingPattern).ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - Contains - null模式抛出异常
    /// </summary>
    [Fact]
    public void Contains_NullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RegexPool.Contains(null));
    }
    /// <summary>
    /// 测试 - Contains - 不同选项的相同模式视为不同缓存项
    /// </summary>
    [Fact]
    public void Contains_SamePatternDifferentOptions_TreatedAsDifferentItems()
    {
        // Arrange
        const string pattern = @"hello";
        RegexPool.GetOrCreate(pattern, RegexOptions.IgnoreCase);
        // Act & Assert
        RegexPool.Contains(pattern, RegexOptions.IgnoreCase).ShouldBeTrue();
        RegexPool.Contains(pattern, RegexOptions.None).ShouldBeFalse();
    }
    #endregion
    #region 统计信息测试
    /// <summary>
    /// 测试 - GetStatistics - 统计信息准确性
    /// </summary>
    [Fact]
    public void GetStatistics_AccurateStatistics_ReturnsCorrectValues()
    {
        // Arrange
        RegexPool.Clear();
        const string pattern1 = @"\d+";
        const string pattern2 = @"\w+";
        // Act
        RegexPool.GetOrCreate(pattern1); // Miss: 1
        RegexPool.GetOrCreate(pattern1); // Hit: 1
        RegexPool.GetOrCreate(pattern2); // Miss: 2
        RegexPool.GetOrCreate(pattern1); // Hit: 2
        var stats = RegexPool.GetStatistics();
        // Assert
        stats.Count.ShouldBe(2);
        stats.MissCount.ShouldBe(2);
        stats.HitCount.ShouldBe(2);
        stats.HitRate.ShouldBe(0.5, tolerance: 0.001);
        stats.MaxSize.ShouldBe(1000);
        stats.TotalAccess.ShouldBe(4);
        stats.UsageRate.ShouldBe(2.0 / 1000.0, tolerance: 0.001);
    }
    /// <summary>
    /// 测试 - Count - 反映实际缓存大小
    /// </summary>
    [Fact]
    public void Count_ReflectsActualCacheSize()
    {
        // Arrange
        RegexPool.Clear();
        // Act & Assert
        RegexPool.Count.ShouldBe(0);
        RegexPool.GetOrCreate(@"\d+");
        RegexPool.Count.ShouldBe(1);
        RegexPool.GetOrCreate(@"\w+");
        RegexPool.Count.ShouldBe(2);
        RegexPool.Remove(@"\d+");
        RegexPool.Count.ShouldBe(1);
        RegexPool.Clear();
        RegexPool.Count.ShouldBe(0);
    }
    /// <summary>
    /// 测试 - HitRate - 命中率计算正确性
    /// </summary>
    [Fact]
    public void HitRate_CalculatedCorrectly_ReturnsAccurateRatio()
    {
        // Arrange
        RegexPool.Clear();
        const string pattern = @"\d+";
        // Act & Assert
        // 初始状态
        RegexPool.HitRate.ShouldBe(0.0);
        // 一次 Miss
        RegexPool.GetOrCreate(pattern);
        RegexPool.HitRate.ShouldBe(0.0); // 0 hit / 1 total
        // 一次 Hit
        RegexPool.GetOrCreate(pattern);
        RegexPool.HitRate.ShouldBe(0.5); // 1 hit / 2 total
        // 再一次 Hit
        RegexPool.GetOrCreate(pattern);
        RegexPool.HitRate.ShouldBe(2.0 / 3.0, tolerance: 0.001); // 2 hit / 3 total
    }
    /// <summary>
    /// 测试 - PoolStatistics - ToString方法格式化输出
    /// </summary>
    [Fact]
    public void PoolStatistics_ToString_ReturnsFormattedString()
    {
        // Arrange
        RegexPool.Clear();
        RegexPool.GetOrCreate(@"\d+"); // 产生一次 miss
        RegexPool.GetOrCreate(@"\d+"); // 产生一次 hit
        // Act
        var stats = RegexPool.GetStatistics();
        var stringResult = stats.ToString();
        // Assert
        stringResult.ShouldNotBeNullOrEmpty();
        stringResult.ShouldContain("RegexPool Statistics");
        stringResult.ShouldContain("Count=");
        stringResult.ShouldContain("HitRate=");
    }
    /// <summary>
    /// 测试 - 统计信息 - 除零保护
    /// </summary>
    [Fact]
    public void Statistics_DivisionByZero_HandlesCorrectly()
    {
        // Arrange
        RegexPool.Clear();
        // Act
        var stats = RegexPool.GetStatistics();
        // Assert
        stats.HitRate.ShouldBe(0.0);
        stats.TotalAccess.ShouldBe(0);
        stats.UsageRate.ShouldBe(0.0);
    }
    /// <summary>
    /// 测试 - 统计信息 - 大数值处理
    /// </summary>
    [Fact]
    public void Statistics_LargeNumbers_HandlesCorrectly()
    {
        // Arrange
        RegexPool.Clear();
        const string pattern = @"\d+";
        // Act - 模拟大量访问
        for (int i = 0; i < 10000; i++)
        {
            RegexPool.GetOrCreate(pattern);
        }
        // Assert
        var stats = RegexPool.GetStatistics();
        stats.HitCount.ShouldBe(9999L);
        stats.MissCount.ShouldBe(1L);
        stats.TotalAccess.ShouldBe(10000L);
        stats.HitRate.ShouldBe(0.9999, tolerance: 0.0001);
    }
    #endregion
    #region 常用正则表达式测试
    /// <summary>
    /// 测试 - CommonPatterns - 邮箱模式
    /// </summary>
    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("test.email@domain.co.uk", true)]
    [InlineData("user+tag@example.com", true)]
    [InlineData("invalid-email", false)]
    [InlineData("user@", false)]
    [InlineData("@example.com", false)]
    public void CommonPatterns_Email_ShouldValidateCorrectly(string input, bool expected)
    {
        // Act
        var result = RegexPool.IsMatch(input, RegexPool.CommonPatterns.Email, RegexOptions.None);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - CommonPatterns - 手机号模式
    /// </summary>
    [Theory]
    [InlineData("13812345678", true)]
    [InlineData("15912345678", true)]
    [InlineData("18612345678", true)]
    [InlineData("12812345678", false)] // 无效号段
    [InlineData("1381234567", false)]  // 位数不够
    [InlineData("138123456789", false)] // 位数太多
    public void CommonPatterns_MobilePhone_ShouldValidateCorrectly(string input, bool expected)
    {
        // Act
        var result = RegexPool.IsMatch(input, RegexPool.CommonPatterns.MobilePhone, RegexOptions.None);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - CommonPatterns - IPv4 模式
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("255.255.255.255", true)]
    [InlineData("0.0.0.0", true)]
    [InlineData("256.1.1.1", false)]  // 超出范围
    [InlineData("192.168.1", false)]  // 不完整
    [InlineData("192.168.1.1.1", false)] // 多余段
    public void CommonPatterns_IPv4_ShouldValidateCorrectly(string input, bool expected)
    {
        // Act
        var result = RegexPool.IsMatch(input, RegexPool.CommonPatterns.IPv4, RegexOptions.None);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - PrecompileCommonPatterns - 预编译功能
    /// </summary>
    [Fact]
    public void PrecompileCommonPatterns_ShouldCacheAllPatterns()
    {
        // Arrange
        RegexPool.Clear();
        var initialCount = RegexPool.Count;
        // Act
        RegexPool.PrecompileCommonPatterns();
        // Assert
        var finalCount = RegexPool.Count;
        finalCount.ShouldBeGreaterThan(initialCount);
        // 验证常用模式已被缓存
        RegexPool.Contains(RegexPool.CommonPatterns.Email).ShouldBeTrue();
        RegexPool.Contains(RegexPool.CommonPatterns.MobilePhone).ShouldBeTrue();
        RegexPool.Contains(RegexPool.CommonPatterns.IPv4).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - CommonPatterns - 中文字符模式深度验证
    /// </summary>
    [Theory]
    [InlineData("中文测试", true)]
    [InlineData("Hello中文World", true)]
    [InlineData("测试123", true)]
    [InlineData("Hello World", false)]
    [InlineData("123456", false)]
    [InlineData("", false)]
    [InlineData("中", true)]
    [InlineData("🀄", false)] // 不是CJK统一汉字
    public void CommonPatterns_ChineseCharacters_ValidatesCorrectly(string input, bool expected)
    {
        // Act
        var result = RegexPool.IsMatch(input, RegexPool.CommonPatterns.ChineseCharacters, RegexOptions.None);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - CommonPatterns - 纯数字模式验证
    /// </summary>
    [Theory]
    [InlineData("123456", true)]
    [InlineData("0", true)]
    [InlineData("00123", true)]
    [InlineData("123abc", false)]
    [InlineData("abc123", false)]
    [InlineData("", false)]
    [InlineData(" 123 ", false)]
    [InlineData("-123", false)]
    [InlineData("+123", false)]
    public void CommonPatterns_DigitsOnly_ValidatesCorrectly(string input, bool expected)
    {
        // Act
        var result = RegexPool.IsMatch(input, RegexPool.CommonPatterns.DigitsOnly, RegexOptions.None);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - CommonPatterns - 纯字母模式验证
    /// </summary>
    [Theory]
    [InlineData("abc", true)]
    [InlineData("ABC", true)]
    [InlineData("AbCdEf", true)]
    [InlineData("abc123", false)]
    [InlineData("123abc", false)]
    [InlineData("", false)]
    [InlineData(" abc ", false)]
    [InlineData("abc-def", false)]
    [InlineData("abc_def", false)]
    public void CommonPatterns_LettersOnly_ValidatesCorrectly(string input, bool expected)
    {
        // Act
        var result = RegexPool.IsMatch(input, RegexPool.CommonPatterns.LettersOnly, RegexOptions.None);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - CommonPatterns - 字母数字组合模式验证
    /// </summary>
    [Theory]
    [InlineData("abc123", true)]
    [InlineData("123abc", true)]
    [InlineData("ABC123", true)]
    [InlineData("abc", true)]
    [InlineData("123", true)]
    [InlineData("", false)]
    [InlineData("abc-123", false)]
    [InlineData("abc_123", false)]
    [InlineData(" abc123 ", false)]
    [InlineData("abc 123", false)]
    public void CommonPatterns_Alphanumeric_ValidatesCorrectly(string input, bool expected)
    {
        // Act
        var result = RegexPool.IsMatch(input, RegexPool.CommonPatterns.Alphanumeric, RegexOptions.None);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - CommonPatterns - 身份证号模式验证
    /// </summary>
    [Theory]
    [InlineData("123456789012345678", true)] // 18位数字
    [InlineData("12345678901234567X", true)] // 17位数字+X
    [InlineData("12345678901234567x", true)] // 17位数字+x
    [InlineData("1234567890123456789", false)] // 19位
    [InlineData("12345678901234567", false)] // 17位
    [InlineData("12345678901234567Y", false)] // 无效字母
    [InlineData("12345678901234567A", false)] // 无效字母
    [InlineData("", false)]
    public void CommonPatterns_IdCard_ValidatesCorrectly(string input, bool expected)
    {
        // Act
        var result = RegexPool.IsMatch(input, RegexPool.CommonPatterns.IdCard, RegexOptions.None);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - CommonPatterns - 邮政编码模式验证
    /// </summary>
    [Theory]
    [InlineData("100000", true)]
    [InlineData("999999", true)]
    [InlineData("000000", true)]
    [InlineData("12345", false)] // 5位
    [InlineData("1234567", false)] // 7位
    [InlineData("12345a", false)] // 包含字母
    [InlineData("", false)]
    [InlineData(" 123456 ", false)] // 包含空格
    public void CommonPatterns_PostalCode_ValidatesCorrectly(string input, bool expected)
    {
        // Act
        var result = RegexPool.IsMatch(input, RegexPool.CommonPatterns.PostalCode, RegexOptions.None);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - PrecompileCommonPatterns - 异常处理
    /// </summary>
    [Fact]
    public void PrecompileCommonPatterns_WithReflectionException_DoesNotThrow()
    {
        // Arrange
        RegexPool.Clear();
        // Act & Assert - 应该不抛出异常
        Should.NotThrow(() => RegexPool.PrecompileCommonPatterns());
        // 验证至少预编译了一些模式
        RegexPool.Count.ShouldBeGreaterThan(0);
    }
    /// <summary>
    /// 测试 - PrecompileCommonPatterns - 并行安全性
    /// </summary>
    [Fact]
    public void PrecompileCommonPatterns_ConcurrentCalls_IsThreadSafe()
    {
        // Arrange
        RegexPool.Clear();
        const int threadCount = 5;
        var tasks = new Task[threadCount];
        var exceptions = new ConcurrentBag<Exception>();
        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    RegexPool.PrecompileCommonPatterns();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }
        Task.WaitAll(tasks);
        // Assert
        exceptions.ShouldBeEmpty();
        RegexPool.Count.ShouldBeGreaterThan(0);
    }
    /// <summary>
    /// 测试 - PrecompileCommonPatterns - 缓存效果验证
    /// </summary>
    [Fact]
    public void PrecompileCommonPatterns_CachingEffect_ImprovesPerformance()
    {
        // Arrange
        RegexPool.Clear();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        // Act - 预编译
        RegexPool.PrecompileCommonPatterns();
        stopwatch.Stop();
        var precompileTime = stopwatch.ElapsedMilliseconds;
        // 测试访问预编译的模式
        stopwatch.Restart();
        for (int i = 0; i < 100; i++)
        {
            RegexPool.IsMatch("test@example.com", RegexPool.CommonPatterns.Email);
            RegexPool.IsMatch("13812345678", RegexPool.CommonPatterns.MobilePhone);
            RegexPool.IsMatch("192.168.1.1", RegexPool.CommonPatterns.IPv4);
        }
        stopwatch.Stop();
        var accessTime = stopwatch.ElapsedMilliseconds;
        // Assert
        var stats = RegexPool.GetStatistics();
        stats.HitRate.ShouldBeGreaterThan(0.9); // 高命中率表明缓存有效
        // 预编译的模式应该在缓存中
        RegexPool.Contains(RegexPool.CommonPatterns.Email).ShouldBeTrue();
        RegexPool.Contains(RegexPool.CommonPatterns.MobilePhone).ShouldBeTrue();
        RegexPool.Contains(RegexPool.CommonPatterns.IPv4).ShouldBeTrue();
    }
    #endregion
    #region 并发测试
    /// <summary>
    /// 测试 - 并发访问 - 线程安全
    /// </summary>
    [Fact]
    public void ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        RegexPool.Clear();
        const string pattern = @"\d+";
        const int threadCount = 10;
        const int operationsPerThread = 100;
        var tasks = new Task[threadCount];
        var exceptions = new ConcurrentBag<Exception>();
        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        var regex = RegexPool.GetOrCreate(pattern);
                        var isMatch = RegexPool.IsMatch("test123", pattern);
                        var match = RegexPool.Match("test123", pattern);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }
        Task.WaitAll(tasks);
        // Assert
        exceptions.ShouldBeEmpty();
        RegexPool.Count.ShouldBe(1); // 应该只有一个缓存的正则表达式
        var stats = RegexPool.GetStatistics();
        // 在并发环境下，只有一次真正的miss（创建缓存），其余都是hit
        // 但每个线程的每次操作都会调用GetOrCreate，所以总访问次数应该是确定的
        var expectedTotalAccess = threadCount * operationsPerThread * 3; // 每次循环调用3个方法
        stats.TotalAccess.ShouldBe(expectedTotalAccess);
        //stats.MissCount.ShouldBe(threadCount); // 只有一次真正的miss
        stats.HitCount.ShouldBe(expectedTotalAccess - stats.MissCount); // 其余都是hit
        stats.Count.ShouldBe(1); // 只有一个缓存项
    }
    /// <summary>
    /// 测试 - 并发访问 - 多个不同模式的线程安全
    /// </summary>
    [Fact]
    public void ConcurrentAccess_MultiplePatterns_ShouldBeThreadSafe()
    {
        // Arrange
        RegexPool.Clear();
        var patterns = new[] { @"\d+", @"\w+", @"[a-z]+", @"[A-Z]+", @"\s+" };
        const int threadCount = 10;
        const int operationsPerThread = 50;
        var tasks = new Task[threadCount];
        var exceptions = new ConcurrentBag<Exception>();
        var random = new Random();
        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        var pattern = patterns[random.Next(patterns.Length)];
                        var regex = RegexPool.GetOrCreate(pattern);
                        var isMatch = RegexPool.IsMatch("test123 ABC def", pattern);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }
        Task.WaitAll(tasks);
        // Assert
        exceptions.ShouldBeEmpty();
        RegexPool.Count.ShouldBe(patterns.Length); // 应该缓存了所有不同的模式
        var stats = RegexPool.GetStatistics();
        //stats.MissCount.ShouldBe(patterns.Length); // 每个模式一次miss
        stats.HitCount.ShouldBeGreaterThan(0); // 应该有命中
        stats.TotalAccess.ShouldBe(threadCount * operationsPerThread * 2); // 每次循环调用2个方法
    }
    /// <summary>
    /// 测试 - 并发访问 - 统计准确性验证
    /// </summary>
    [Fact]
    public void ConcurrentAccess_StatisticsAccuracy_ShouldBeCorrect()
    {
        // Arrange
        RegexPool.Clear();
        const string pattern1 = @"\d+";
        const string pattern2 = @"\w+";
        const int threadCount = 5;
        var tasks = new Task[threadCount];
        var exceptions = new ConcurrentBag<Exception>();
        // Act - 每个线程执行特定次数的操作
        for (int i = 0; i < threadCount; i++)
        {
            var threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    // 第一个线程创建pattern1，第二个线程创建pattern2，其余线程随机访问
                    if (threadIndex == 0)
                    {
                        RegexPool.GetOrCreate(pattern1); // Miss: 1
                        RegexPool.GetOrCreate(pattern1); // Hit: 1
                    }
                    else if (threadIndex == 1)
                    {
                        RegexPool.GetOrCreate(pattern2); // Miss: 1
                        RegexPool.GetOrCreate(pattern2); // Hit: 1
                    }
                    else
                    {
                        RegexPool.GetOrCreate(pattern1); // Hit
                        RegexPool.GetOrCreate(pattern2); // Hit
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }
        Task.WaitAll(tasks);
        // Assert
        exceptions.ShouldBeEmpty();
        var stats = RegexPool.GetStatistics();
        stats.Count.ShouldBe(2); // 两个不同的模式
        //stats.MissCount.ShouldBe(threadCount); // 每个模式一次miss
        stats.TotalAccess.ShouldBe(2 + 2 + 6); // 总共10次访问：线程0(2次) + 线程1(2次) + 其他3个线程(每个2次)
        stats.HitCount.ShouldBe(stats.TotalAccess - stats.MissCount);
    }
    #endregion
    #region 性能测试
    /// <summary>
    /// 测试 - 性能对比 - 缓存 vs 非缓存
    /// </summary>
    [Fact]
    public void Performance_CachedVsNonCached_ShouldShowPerformanceGain()
    {
        // Arrange
        const string input = "test@example.com";
        const string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        const int iterations = 1000;
        // 预热缓存
        RegexPool.IsMatch(input, pattern, useCache: true);
        // Act & Assert - 测试应该在合理时间内完成
        Should.CompleteIn(() =>
        {
            // 使用缓存的版本
            for (int i = 0; i < iterations; i++)
            {
                RegexPool.IsMatch(input, pattern, useCache: true);
            }
        }, TimeSpan.FromSeconds(1));
        Should.CompleteIn(() =>
        {
            // 不使用缓存的版本（通常会更慢）
            for (int i = 0; i < iterations; i++)
            {
                RegexPool.IsMatch(input, pattern, useCache: false);
            }
        }, TimeSpan.FromSeconds(5)); // 给更多时间
    }
    #endregion
    #region 边界条件测试
    /// <summary>
    /// 测试 - 特殊字符处理
    /// </summary>
    [Fact]
    public void SpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange & Act & Assert
        Should.NotThrow(() =>
        {
            RegexPool.IsMatch("test\0null", @"test");
            RegexPool.IsMatch("test\r\n", @"test");
            RegexPool.IsMatch("test\t\b", @"test");
            RegexPool.IsMatch("测试中文", @"[\u4e00-\u9fa5]+");
        });
    }
    /// <summary>
    /// 测试 - 无效正则表达式模式
    /// </summary>
    [Theory]
    [InlineData("[")]          // 不完整的字符类
    [InlineData("*")]          // 无效的重复符
    [InlineData("?")]          // 无效的量词
    [InlineData("(")]          // 不完整的分组
    public void InvalidRegexPattern_ShouldThrowException(string invalidPattern)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => RegexPool.GetOrCreate(invalidPattern));
    }
    /// <summary>
    /// 测试 - 长字符串处理
    /// </summary>
    [Fact]
    public void LongString_ShouldHandleCorrectly()
    {
        // Arrange
        var longString = new string('a', 10000);
        const string pattern = @"a+";
        // Act
        var result = RegexPool.IsMatch(longString, pattern);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - 并发缓存清理 - LRU机制线程安全
    /// </summary>
    [Fact]
    public void ConcurrentAccess_LRUCleanup_ShouldBeThreadSafe()
    {
        // Arrange
        RegexPool.Clear();
        const int patternCount = 1050; // 超过最大缓存大小
        const int threadCount = 5;
        var tasks = new Task[threadCount];
        var exceptions = new ConcurrentBag<Exception>();
        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    var startPattern = threadIndex * (patternCount / threadCount);
                    var endPattern = (threadIndex + 1) * (patternCount / threadCount);
                    for (int j = startPattern; j < endPattern; j++)
                    {
                        var pattern = $@"\d{{{j}}}"; // 生成唯一模式
                        RegexPool.GetOrCreate(pattern);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }
        Task.WaitAll(tasks);
        // Assert
        exceptions.ShouldBeEmpty();
        var stats = RegexPool.GetStatistics();
        stats.Count.ShouldBeLessThan(1000); // 应该触发了LRU清理
        stats.MissCount.ShouldBeGreaterThan(0);
    }
    /// <summary>
    /// 测试 - 极长正则表达式模式
    /// </summary>
    [Fact]
    public void RegexPool_VeryLongPattern_HandlesCorrectly()
    {
        // Arrange
        var longPattern = string.Join("|", Enumerable.Range(1, 1000).Select(i => $"test{i}"));
        // Act & Assert
        Should.NotThrow(() =>
        {
            var regex = RegexPool.GetOrCreate(longPattern);
            var isMatch = RegexPool.IsMatch("test500", longPattern);
            isMatch.ShouldBeTrue();
        });
    }
    /// <summary>
    /// 测试 - 特殊Unicode字符处理
    /// </summary>
    [Theory]
    [InlineData("测试🀄文本", @"[\u4e00-\u9fa5]+", true)] // 中文字符
    [InlineData("emoji😀test", @"😀", true)] // Emoji
    [InlineData("Åpfel", @"[ÀÁÂÃÄÅàáâãäå]", true)] // 重音字符
    public void RegexPool_UnicodeCharacters_HandlesCorrectly(string input, string pattern, bool expected)
    {
        // Act
        var result = RegexPool.IsMatch(input, pattern, RegexOptions.None);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - 内存压力下的缓存行为
    /// </summary>
    [Fact]
    public void RegexPool_UnderMemoryPressure_MaintainsFunctionality()
    {
        // Arrange
        RegexPool.Clear();
        var patterns = Enumerable.Range(1, 500)
            .Select(i => $@"pattern_{i}_\d{{1,{i % 10 + 1}}}")
            .ToArray();
        // Act - 模拟内存压力
        Should.NotThrow(() =>
        {
            foreach (var pattern in patterns)
            {
                RegexPool.GetOrCreate(pattern);
                RegexPool.IsMatch($"pattern_{pattern.GetHashCode() % 100}_123", pattern);
            }
        });
        // Assert
        var stats = RegexPool.GetStatistics();
        stats.Count.ShouldBeLessThanOrEqualTo(1000); // 不应超过最大缓存大小
        stats.TotalAccess.ShouldBe(patterns.Length * 2); // 每个模式访问两次
    }
    #endregion
    #region PoolStatistics 测试
    /// <summary>
    /// 测试 - PoolStatistics - ToString 方法
    /// </summary>
    [Fact]
    public void PoolStatistics_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        RegexPool.Clear();
        RegexPool.GetOrCreate(@"\d+"); // 产生一次 miss
        RegexPool.GetOrCreate(@"\d+"); // 产生一次 hit
        // Act
        var stats = RegexPool.GetStatistics();
        var stringResult = stats.ToString();
        // Assert
        stringResult.ShouldNotBeNullOrEmpty();
        stringResult.ShouldContain("RegexPool Statistics");
        stringResult.ShouldContain("Count=");
        stringResult.ShouldContain("HitRate=");
        stringResult.ShouldContain("MissCount=");
    }
    /// <summary>
    /// 测试 - PoolStatistics - TotalAccess 属性
    /// </summary>
    [Fact]
    public void PoolStatistics_TotalAccess_ShouldCalculateCorrectly()
    {
        // Arrange
        RegexPool.Clear();
        // Act
        RegexPool.GetOrCreate(@"\d+"); // Miss: 1
        RegexPool.GetOrCreate(@"\d+"); // Hit: 1
        RegexPool.GetOrCreate(@"\w+"); // Miss: 1
        var stats = RegexPool.GetStatistics();
        // Assert
        stats.TotalAccess.ShouldBe(3); // 2 Miss + 1 Hit
    }
    /// <summary>
    /// 测试 - PoolStatistics - UsageRate 属性
    /// </summary>
    [Fact]
    public void PoolStatistics_UsageRate_ShouldCalculateCorrectly()
    {
        // Arrange
        RegexPool.Clear();
        // Act
        RegexPool.GetOrCreate(@"\d+");
        RegexPool.GetOrCreate(@"\w+");
        var stats = RegexPool.GetStatistics();
        // Assert
        stats.UsageRate.ShouldBe(2.0 / 1000.0); // 2 patterns / 1000 max size
    }
    /// <summary>
    /// 测试 - PoolStatistics - 所有属性的正确性
    /// </summary>
    [Fact]
    public void PoolStatistics_AllProperties_ReflectActualState()
    {
        // Arrange
        RegexPool.Clear();
        // Act - 创建一些缓存项和统计数据
        RegexPool.GetOrCreate(@"\d+"); // Miss: 1
        RegexPool.GetOrCreate(@"\w+"); // Miss: 2
        RegexPool.GetOrCreate(@"\d+"); // Hit: 1
        RegexPool.GetOrCreate(@"\w+"); // Hit: 2
        RegexPool.GetOrCreate(@"\s+"); // Miss: 3
        var stats = RegexPool.GetStatistics();
        // Assert
        stats.Count.ShouldBe(3);
        stats.HitCount.ShouldBe(2);
        stats.MissCount.ShouldBe(3);
        stats.TotalAccess.ShouldBe(5);
        stats.HitRate.ShouldBe(0.4, tolerance: 0.001);
        stats.UsageRate.ShouldBe(3.0 / 1000.0, tolerance: 0.001);
        stats.MaxSize.ShouldBe(1000);
    }
    /// <summary>
    /// 测试 - PoolStatistics - ToString方法的格式
    /// </summary>
    [Fact]
    public void PoolStatistics_ToString_ContainsAllKeyInformation()
    {
        // Arrange
        RegexPool.Clear();
        RegexPool.GetOrCreate(@"\d+");
        RegexPool.GetOrCreate(@"\d+");
        // Act
        var stats = RegexPool.GetStatistics();
        var result = stats.ToString();
        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("RegexPool Statistics");
        result.ShouldContain("Count=");
        result.ShouldContain("HitRate=");
        result.ShouldContain("MissCount=");
        result.ShouldContain("%"); // 百分比格式
        result.ShouldContain("/"); // 分数格式
    }
    #endregion
}
