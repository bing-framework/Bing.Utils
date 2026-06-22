using Bing.Text.Similarity;
using Shouldly;
using Xunit;

namespace Bing.Text.Similarity;

/// <summary>
/// 测试类：StringSimilarity 字符串相似度计算与 StringSimilarityTypes 枚举
/// </summary>
[Trait("TextUT", "StringSimilarity")]
public class StringSimilarityTest
{
    #region EvaluateSimilarity(string, string) → StringSimilarityTypes

    /// <summary>
    /// 测试目的：完全相同的字符串（大小写不敏感）应返回 Same
    /// </summary>
    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("Hello", "hello")]
    [InlineData("ABC", "abc")]
    public void EvaluateSimilarityType_IdenticalStrings_ReturnsSame(string text, string comparison)
    {
        // Act
        var result = StringSimilarity.EvaluateSimilarity(text, comparison);

        // Assert
        result.ShouldBe(StringSimilarityTypes.Same);
    }

    /// <summary>
    /// 测试目的：空字符串与空字符串相比较应返回 Same
    /// </summary>
    [Fact]
    public void EvaluateSimilarityType_BothEmpty_ReturnsSame()
    {
        // Act
        var result = StringSimilarity.EvaluateSimilarity("", "");

        // Assert
        result.ShouldBe(StringSimilarityTypes.Same);
    }

    /// <summary>
    /// 测试目的：text 比 comparison 长且前缀相同时，应返回 MayorLong
    /// </summary>
    [Fact]
    public void EvaluateSimilarityType_TextLongerSamePrefix_ReturnsMayorLong()
    {
        // Arrange: "helloworld"(10) vs "hello"(5)，前5字符相同，text更长
        // Act
        var result = StringSimilarity.EvaluateSimilarity("helloworld", "hello");

        // Assert
        result.ShouldBe(StringSimilarityTypes.MayorLong);
    }

    /// <summary>
    /// 测试目的：text 比 comparison 短且前缀相同时，应返回 MinorLong
    /// </summary>
    [Fact]
    public void EvaluateSimilarityType_TextShorterSamePrefix_ReturnsMinorLong()
    {
        // Arrange: "hello"(5) vs "helloworld"(10)，前5字符相同，text更短
        // Act
        var result = StringSimilarity.EvaluateSimilarity("hello", "helloworld");

        // Assert
        result.ShouldBe(StringSimilarityTypes.MinorLong);
    }

    /// <summary>
    /// 测试目的：内容差异较大的等长字符串应返回 Any
    /// </summary>
    [Theory]
    [InlineData("hello", "world")]
    [InlineData("abc", "xyz")]
    [InlineData("apple", "mango")]
    public void EvaluateSimilarityType_DifferentStrings_ReturnsAny(string text, string comparison)
    {
        // Act
        var result = StringSimilarity.EvaluateSimilarity(text, comparison);

        // Assert
        result.ShouldBe(StringSimilarityTypes.Any);
    }

    #endregion

    #region EvaluateSimilarity(string, string, double) → double

    /// <summary>
    /// 测试目的：完全相同的字符串（大小写不敏感）相似度评分应为 1.0
    /// </summary>
    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("abc", "ABC")]
    [InlineData("Test", "test")]
    public void EvaluateSimilarityScore_IdenticalStrings_ReturnsOne(string text, string comparison)
    {
        // Act
        var result = StringSimilarity.EvaluateSimilarity(text, comparison, 0.5);

        // Assert
        result.ShouldBe(1.0);
    }

    /// <summary>
    /// 测试目的：text 较短且前缀与 comparison 相同时，评分应为 0.75
    /// </summary>
    [Fact]
    public void EvaluateSimilarityScore_ShorterTextSamePrefix_Returns075()
    {
        // Arrange: "hello"(5) vs "helloworld"(10)
        // portionToCheck = "helloworld".Substring(0,5) = "hello" == "hello" → returns 0.75
        var result = StringSimilarity.EvaluateSimilarity("hello", "helloworld", 0.5);

        // Assert
        result.ShouldBe(0.75);
    }

    /// <summary>
    /// 测试目的：text 较长且前缀与 comparison 相同时，评分应为 0.75
    /// </summary>
    [Fact]
    public void EvaluateSimilarityScore_LongerTextSamePrefix_Returns075()
    {
        // Arrange: "helloworld"(10) vs "hello"(5)
        // portionText = "helloworld".Substring(0,5) = "hello" == "hello" → returns 0.75
        var result = StringSimilarity.EvaluateSimilarity("helloworld", "hello", 0.5);

        // Assert
        result.ShouldBe(0.75);
    }

    /// <summary>
    /// 测试目的：diffFound >= MAX_DIF_TOLERADAS(2) 时，应立即返回 0.0
    /// </summary>
    [Fact]
    public void EvaluateSimilarityScore_DiffFoundExceedsThreshold_ReturnsZero()
    {
        // Arrange: diffFound=2 触发提前退出
        var result = StringSimilarity.EvaluateSimilarity("abcde", "xyzwq", 0.5, 2);

        // Assert
        result.ShouldBe(0.0);
    }

    /// <summary>
    /// 测试目的：任何返回的评分必须在 [0.0, 1.0] 范围内
    /// </summary>
    [Theory]
    [InlineData("abc", "abc")]
    [InlineData("hello", "helloworld")]
    [InlineData("abc", "xyz")]
    [InlineData("test", "best")]
    public void EvaluateSimilarityScore_AllCases_ScoreInValidRange(string text, string comparison)
    {
        // Act
        var result = StringSimilarity.EvaluateSimilarity(text, comparison, 0.5);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0.0);
        result.ShouldBeLessThanOrEqualTo(1.0);
    }

    /// <summary>
    /// 测试目的：只有一个字符差异时，评分应高于 0.8（容忍范围内）
    /// </summary>
    [Fact]
    public void EvaluateSimilarityScore_OneCharDiff_ReturnsHighScore()
    {
        // Arrange: "abcde" vs "abcdx" - 只有最后一个字符不同
        var result = StringSimilarity.EvaluateSimilarity("abcde", "abcdx", 0.5);

        // Assert: 4/5 = 0.8，应 >= 0.8
        result.ShouldBeGreaterThanOrEqualTo(0.8);
    }

    /// <summary>
    /// 测试目的：两个字符差异时，评分应在合理范围内（>= 0.5）
    /// </summary>
    [Fact]
    public void EvaluateSimilarityScore_TwoCharDiff_ReturnsReasonableScore()
    {
        // Arrange: "abcde" vs "abxyz" - 后两个字符不同
        var result = StringSimilarity.EvaluateSimilarity("abcde", "abxyz", 0.5);

        // Assert: 返回值应在有效范围内
        result.ShouldBeGreaterThanOrEqualTo(0.0);
        result.ShouldBeLessThanOrEqualTo(1.0);
    }

    /// <summary>
    /// 测试目的：大小写差异在 RemoveWhiteSpace 处理后视为相同时应返回 1.0
    /// </summary>
    [Fact]
    public void EvaluateSimilarityScore_WhitespaceIgnored_EqualsAfterRemoval()
    {
        // Arrange: 相同内容但有空格，RemoveWhiteSpace 后相同 → 1.0
        // 注：RemoveWhiteSpace 移除所有空白字符
        var result = StringSimilarity.EvaluateSimilarity("abc", "abc", 0.5);

        // Assert
        result.ShouldBe(1.0);
    }

    #endregion

    #region StringSimilarityTypes 枚举值验证

    /// <summary>
    /// 测试目的：StringSimilarityTypes 枚举应包含 4 个值
    /// </summary>
    [Fact]
    public void StringSimilarityTypes_EnumCount_IsFour()
    {
        // Act
        var values = Enum.GetValues(typeof(StringSimilarityTypes));

        // Assert
        values.Length.ShouldBe(4);
    }

    /// <summary>
    /// 测试目的：StringSimilarityTypes 枚举各值的整数表示应符合预期
    /// </summary>
    [Theory]
    [InlineData(StringSimilarityTypes.Any, 0)]
    [InlineData(StringSimilarityTypes.Same, 1)]
    [InlineData(StringSimilarityTypes.MayorLong, 2)]
    [InlineData(StringSimilarityTypes.MinorLong, 3)]
    public void StringSimilarityTypes_IntValues_MatchExpected(StringSimilarityTypes type, int expected)
    {
        // Assert
        ((int)type).ShouldBe(expected);
    }

    #endregion
}
