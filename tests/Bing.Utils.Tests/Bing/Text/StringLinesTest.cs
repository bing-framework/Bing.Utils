namespace Bing.Text;
/// <summary>
/// 测试类：覆盖 `StringLines` 相关行为。
/// </summary>
[Trait("Bing.Text", "StringLines")]
public class StringLinesTest
{
    /// <summary>
    /// 测试用例：验证 `SplitByLines` 在 `MultiLineInput` 场景下，结果为 `PreservesOrder`。
    /// </summary>
    [Fact]
    public void SplitByLines_MultiLineInput_PreservesOrder()
    {
        var text = $"A{Environment.NewLine}B{Environment.NewLine}C";
        var result = StringLines.SplitByLines(text).ToList();
        result.ShouldBe(new[] { "A", "B", "C" });
    }
    /// <summary>
    /// 测试用例：验证 `SplitByLines` 在 `TrailingNewLine` 场景下，结果为 `DoesNotEmitTrailingEmptyLine`。
    /// </summary>
    [Fact]
    public void SplitByLines_TrailingNewLine_DoesNotEmitTrailingEmptyLine()
    {
        var text = $"A{Environment.NewLine}B{Environment.NewLine}";
        var result = StringLines.SplitByLines(text).ToList();
        result.ShouldBe(new[] { "A", "B" });
    }
    /// <summary>
    /// 测试用例：验证 `SplitInLinesTyped` 在 `IntInput` 场景下，结果为 `ReturnsTypedArray`。
    /// </summary>
    [Fact]
    public void SplitInLinesTyped_IntInput_ReturnsTypedArray()
    {
        var text = $"1{Environment.NewLine}2{Environment.NewLine}3";
        var result = StringLines.SplitInLinesTyped<int>(text);
        result.ShouldBe(new[] { 1, 2, 3 });
    }
    /// <summary>
    /// 测试用例：验证 `SplitInLinesWithoutEmpty` 在 `InputWithBlankLines` 场景下，结果为 `RemovesBlankLines`。
    /// </summary>
    [Fact]
    public void SplitInLinesWithoutEmpty_InputWithBlankLines_RemovesBlankLines()
    {
        var text = $"A{Environment.NewLine}{Environment.NewLine}B{Environment.NewLine}";
        var result = StringLines.SplitInLinesWithoutEmpty(text);
        result.ShouldBe(new[] { "A", "B" });
    }
    /// <summary>
    /// 测试用例：验证 `CountByLines` 在 `MultiLineInput` 场景下，结果为 `ReturnsLineCount`。
    /// </summary>
    [Fact]
    public void CountByLines_MultiLineInput_ReturnsLineCount()
    {
        var text = $"A{Environment.NewLine}B{Environment.NewLine}C";
        StringLines.CountByLines(text).ShouldBe(3);
    }
    /// <summary>
    /// 测试用例：验证 `TruncateByLines` 在 `TextLongerThanMaxLines` 场景下，结果为 `ReturnsTruncatedText`。
    /// </summary>
    [Fact]
    public void TruncateByLines_TextLongerThanMaxLines_ReturnsTruncatedText()
    {
        var text = $"A{Environment.NewLine}B{Environment.NewLine}C";
        var result = StringLines.TruncateByLines(text, 2);
        result.ShouldBe($"A{Environment.NewLine}...{Environment.NewLine}");
    }
}

