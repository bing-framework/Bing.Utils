namespace Bing.Text.Similarity;
/// <summary>
/// 测试类：覆盖 `StringSimilarityBoundaryContract` 相关行为。
/// </summary>
[Trait("Bing.Text", "StringSimilarity.Boundary")]
public class StringSimilarityBoundaryContractTest
{
    /// <summary>
    /// 测试用例：验证 `EvaluateSimilarity` 在 `BothNull` 场景下，结果为 `ReturnsOneAndSame`。
    /// </summary>
    [Fact]
    public void EvaluateSimilarity_BothNull_ReturnsOneAndSame()
    {
        var score = StringSimilarity.EvaluateSimilarity(null, null, 0.2);
        var type = StringSimilarity.EvaluateSimilarity(null, null);
        score.ShouldBe(1d);
        type.ShouldBe(StringSimilarityTypes.Same);
    }
    /// <summary>
    /// 测试用例：验证 `EvaluateSimilarity` 在 `NullAndNonNull` 场景下，结果为 `CurrentBehaviorThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void EvaluateSimilarity_NullAndNonNull_CurrentBehaviorThrowsNullReferenceException()
    {
        Should.Throw<NullReferenceException>(() => StringSimilarity.EvaluateSimilarity(null, "abc", 0.2));
        Should.Throw<NullReferenceException>(() => StringSimilarity.EvaluateSimilarity(null, "abc"));
    }
    /// <summary>
    /// 测试用例：验证 `EvaluateSimilarity` 在 `LengthPrefixMatch` 场景下，结果为 `Returns075AndLengthType`。
    /// </summary>
    [Fact]
    public void EvaluateSimilarity_LengthPrefixMatch_Returns075AndLengthType()
    {
        var longText = "abcdef";
        var shortText = "abc";
        var scoreWhenLeftLonger = StringSimilarity.EvaluateSimilarity(longText, shortText, 0.2);
        var typeWhenLeftLonger = StringSimilarity.EvaluateSimilarity(longText, shortText);
        var scoreWhenRightLonger = StringSimilarity.EvaluateSimilarity(shortText, longText, 0.2);
        var typeWhenRightLonger = StringSimilarity.EvaluateSimilarity(shortText, longText);
        scoreWhenLeftLonger.ShouldBe(0.75d);
        scoreWhenRightLonger.ShouldBe(0.75d);
        typeWhenLeftLonger.ShouldBe(StringSimilarityTypes.MayorLong);
        typeWhenRightLonger.ShouldBe(StringSimilarityTypes.MinorLong);
    }
    /// <summary>
    /// 测试用例：验证 `EvaluateSimilarity` 在 `DiffFoundAtTolerance` 场景下，结果为 `ReturnsZero`。
    /// </summary>
    [Fact]
    public void EvaluateSimilarity_DiffFoundAtTolerance_ReturnsZero()
    {
        var score = StringSimilarity.EvaluateSimilarity("abcdef", "abcxyz", 0.2, diffFound: 2);
        score.ShouldBe(0d);
    }
    /// <summary>
    /// 测试用例：验证 `EvaluateSimilarity` 在 `IgnoresWhitespaceAndCase` 场景下，结果为 `ReturnsOneAndSame`。
    /// </summary>
    [Fact]
    public void EvaluateSimilarity_IgnoresWhitespaceAndCase_ReturnsOneAndSame()
    {
        var score = StringSimilarity.EvaluateSimilarity(" Ab C ", "aBc", 0.1);
        var type = StringSimilarity.EvaluateSimilarity(" Ab C ", "aBc");
        score.ShouldBe(1d);
        type.ShouldBe(StringSimilarityTypes.Same);
    }
}

