namespace Bing.Text.Similarity;

[Trait("StringUT", "Strings.StringSimilarity")]
public class StringSimilarityTest
{
    /// <summary>
    /// 测试 - 字符串相似度 - 完全相同
    /// </summary>
    [Fact]
    public void Test_EvaluateSimilarity_Same()
    {
        var a = "全世界无产阶级联合起来";
        var b = "全世界无产阶级联合起来";
        var array = new int[10];
        var value = StringSimilarity.EvaluateSimilarity(a, b, 0.1);
        var type = StringSimilarity.EvaluateSimilarity(a, b);

        value.ShouldBeLessThan(1.00000001);
        value.ShouldBeGreaterThan(0.9999999999);
        type.ShouldBe(StringSimilarityTypes.Same);
    }

    /// <summary>
    /// 测试 - 字符串相似度 - 任意相似
    /// </summary>
    [Fact]
    public void Test_EvaluateSimilarity_Any()
    {
        var a = "全世界无产阶级联合起来";
        var b = "巴拉巴拉小魔仙";

        var value = StringSimilarity.EvaluateSimilarity(a, b, 0.1);
        var type = StringSimilarity.EvaluateSimilarity(a, b);

        value.ShouldBeLessThan(0.00001);
        value.ShouldBe(0);
        type.ShouldBe(StringSimilarityTypes.Any);
    }

    /// <summary>
    /// 测试 - 字符串相似度 - 任意相似 60%
    /// </summary>
    [Fact]
    public void Test_EvaluateSimilarity_Any60()
    {
        var a = "全世界无产阶级联合起来";
        var b = "全世界无产阶级跳起舞来";

        var value = StringSimilarity.EvaluateSimilarity(a, b, 0.1);
        var type = StringSimilarity.EvaluateSimilarity(a, b);

        value.ShouldBeLessThan(1);
        value.ShouldBeGreaterThan(0.60);
        type.ShouldBe(StringSimilarityTypes.Any);
    }

    /// <summary>
    /// 测试 - 字符串相似度 - 任意相似 75%
    /// </summary>
    [Fact]
    public void Test_EvaluateSimilarity_Any75()
    {
        var a = "知错能改";
        var b = "知错不改";

        var value = StringSimilarity.EvaluateSimilarity(a, b, 0.1);
        var type = StringSimilarity.EvaluateSimilarity(a, b);

        value.ShouldBeLessThan(0.750000001);
        value.ShouldBeGreaterThan(0.749999999);
        type.ShouldBe(StringSimilarityTypes.Any);
    }

    /// <summary>
    /// 测试 - 字符串相似度 - 任意相似 50%
    /// </summary>
    [Fact]
    public void Test_EvaluateSimilarity_Any50()
    {
        var a = "乾坤无敌";
        var b = "宇宙无敌";

        var value = StringSimilarity.EvaluateSimilarity(a, b, 0.1);
        var type = StringSimilarity.EvaluateSimilarity(a, b);

        value.ShouldBeLessThan(0.50000001);
        value.ShouldBeGreaterThan(0.49999999);
        type.ShouldBe(StringSimilarityTypes.Any);
    }
}