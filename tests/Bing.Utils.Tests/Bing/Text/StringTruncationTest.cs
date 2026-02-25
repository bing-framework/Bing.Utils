using Bing.Text.Truncation;
namespace Bing.Text;
/// <summary>
/// 测试类：覆盖 `StringTruncation` 相关行为。
/// </summary>
[Trait("Bing.Text", "StringTruncation")]
public class StringTruncationTest
{
    /// <summary>
    /// 测试用例：验证 `Truncate` 在 `RightMode` 场景下，结果为 `ReturnsExpectedTailPlaceholder`。
    /// </summary>
    [Fact]
    public void Truncate_RightMode_ReturnsExpectedTailPlaceholder()
    {
        var result = StringTruncateExtensions.Truncate("abcdef", 4);
        result.ShouldBe("a...");
    }
    /// <summary>
    /// 测试用例：验证 `Truncate` 在 `LeftMode` 场景下，结果为 `ReturnsExpectedHeadPlaceholder`。
    /// </summary>
    [Fact]
    public void Truncate_LeftMode_ReturnsExpectedHeadPlaceholder()
    {
        var result = StringTruncateExtensions.Truncate("abcdef", 4, from: StringTruncateFrom.Left);
        result.ShouldBe("...f");
    }
    /// <summary>
    /// 测试用例：验证 `Truncate` 在 `WithNegativeMaxLength` 场景下，结果为 `ReturnsEmptyString`。
    /// </summary>
    [Fact]
    public void Truncate_WithNegativeMaxLength_ReturnsEmptyString()
    {
        var result = StringTruncateExtensions.Truncate("abcdef", -1);
        result.ShouldBe(string.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `Truncate` 在 `ByNumberOfWordsRight` 场景下，结果为 `ReturnsExpectedWordCount`。
    /// </summary>
    [Fact]
    public void Truncate_ByNumberOfWordsRight_ReturnsExpectedWordCount()
    {
        var result = StringTruncateExtensions.Truncate("alpha beta gamma delta", 2, StringTruncators.ByNumberOfWords);
        result.ShouldBe("alpha beta.");
    }
    /// <summary>
    /// 测试用例：验证 `Truncate` 在 `ByNumberOfWordsLeft` 场景下，结果为 `ReturnsExpectedWordCount`。
    /// </summary>
    [Fact]
    public void Truncate_ByNumberOfWordsLeft_ReturnsExpectedWordCount()
    {
        var result = StringTruncateExtensions.Truncate("alpha beta gamma delta", 2, StringTruncators.ByNumberOfWords, from: StringTruncateFrom.Left);
        result.ShouldBe(".gamma delta");
    }
    /// <summary>
    /// 测试用例：验证 `Truncate` 在 `ByNumberOfCharacters` 场景下，结果为 `IgnoresNonAlphanumericCount`。
    /// </summary>
    [Fact]
    public void Truncate_ByNumberOfCharacters_IgnoresNonAlphanumericCount()
    {
        var result = StringTruncateExtensions.Truncate("abcdefghi", 5, StringTruncators.ByNumberOfCharacters);
        result.ShouldBe("ab...");
    }
}

