namespace Bing.Text.Similarity;

public class StringSimilarityTest
{
    [Fact]
    public void Test_EvaluateSimilarity()
    {
        var a = "我是一个文本，独一无二的文本";
        var b = "一个文本，独一无二的文本";

        var result = StringSimilarity.EvaluateSimilarity(a, b, 1);
        Assert.Equal(1, result);
    }
}