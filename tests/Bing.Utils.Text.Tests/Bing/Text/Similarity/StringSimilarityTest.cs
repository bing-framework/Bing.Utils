using Bing.Text.Similarity;
using Shouldly;
using Xunit;

namespace Bing.Text.Similarity;

/// <summary>
/// 字符串相似度类型、评分和枚举值测试。
/// </summary>
[Trait("TextUT", "StringSimilarity")]
public class StringSimilarityTest
{
    #region EvaluateSimilarity(string, string) → StringSimilarityTypes

    /// <summary>
    /// 验证忽略大小写的相同文本被判为相同。
    /// </summary>
    /// <param name="text">待比较文本。</param>
    /// <param name="comparison">比较文本。</param>
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
    /// 验证空字符串与空字符串相比较应返回 Same
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
    /// 验证text 比 comparison 长且前缀相同时，应返回 MayorLong
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
    /// 验证text 比 comparison 短且前缀相同时，应返回 MinorLong
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
    /// 验证明显不同的等长文本被判为无特定相似类型。
    /// </summary>
    /// <param name="text">待比较文本。</param>
    /// <param name="comparison">比较文本。</param>
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
    /// 验证忽略大小写的相同文本相似度为一。
    /// </summary>
    /// <param name="text">待比较文本。</param>
    /// <param name="comparison">比较文本。</param>
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
    /// 验证text 较短且前缀与 comparison 相同时，评分应为 0.75
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
    /// 验证text 较长且前缀与 comparison 相同时，评分应为 0.75
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
    /// 验证diffFound >= MAX_DIF_TOLERADAS(2) 时，应立即返回 0.0
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
    /// 验证各组文本的相似度评分位于零至一之间。
    /// </summary>
    /// <param name="text">待比较文本。</param>
    /// <param name="comparison">比较文本。</param>
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
    /// 验证单字符差异的所选文本评分至少为零点八。
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
    /// 验证所选不同文本的评分位于零至一之间。
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
    /// 验证完全相同文本的相似度评分为一。
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
    /// 验证StringSimilarityTypes 枚举应包含 4 个值
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
    /// 验证相似度枚举值的整数表示。
    /// </summary>
    /// <param name="type">相似度枚举值。</param>
    /// <param name="expected">预期整数值。</param>
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
