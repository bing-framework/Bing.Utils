namespace Bing.Text.Formatting;

/// <summary>
/// 测试类：FormattedStringValueExtractor 回归测试
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class FormattedStringValueExtractorRegressionTest
{
    /// <summary>
    /// 测试用例：无分隔符且静态段为字母时，应提取完整动态值，不能错误匹配首字符
    /// </summary>
    [Fact]
    [Trait("DefectPattern", "Text.FormattedString.NoSeparatorLetterMatch")]
    public void Extract_NoSeparatorAndLetterTokens_ShouldKeepDynamicValueComplete()
    {
        var result = FormattedStringValueExtractor.Extract("acababcabcd", "a{b}c{d}");

        result.IsMatch.ShouldBeTrue();
        result.Matches.Count.ShouldBe(2);
        result.Matches[0].Name.ShouldBe("b");
        result.Matches[0].Value.ShouldBe("cabab");
        result.Matches[1].Name.ShouldBe("d");
        result.Matches[1].Value.ShouldBe("abcd");
    }
}
