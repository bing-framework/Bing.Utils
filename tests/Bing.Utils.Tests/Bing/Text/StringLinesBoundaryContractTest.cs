namespace Bing.Text;
/// <summary>
/// 测试类：覆盖 `StringLinesBoundaryContract` 相关行为。
/// </summary>
[Trait("Bing.Text", "StringLines.Boundary")]
public class StringLinesBoundaryContractTest
{
    /// <summary>
    /// 测试用例：验证 `SplitByLines` 在 `NullText` 场景下，结果为 `CurrentBehaviorThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void SplitByLines_NullText_CurrentBehaviorThrowsNullReferenceException()
    {
        Should.Throw<NullReferenceException>(() => StringLines.SplitByLines(null).ToList());
    }
    /// <summary>
    /// 测试用例：验证 `SplitInLinesWithoutEmpty` 在 `NullText` 场景下，结果为 `CurrentBehaviorThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void SplitInLinesWithoutEmpty_NullText_CurrentBehaviorThrowsNullReferenceException()
    {
        Should.Throw<NullReferenceException>(() => StringLines.SplitInLinesWithoutEmpty(null));
    }
    /// <summary>
    /// 测试用例：验证 `CountByLines` 在 `EmptyText` 场景下，结果为 `ReturnsZero`。
    /// </summary>
    [Fact]
    public void CountByLines_EmptyText_ReturnsZero()
    {
        var count = StringLines.CountByLines(string.Empty);
        count.ShouldBe(0);
    }
    /// <summary>
    /// 测试用例：验证 `TruncateByLines` 在 `MaxLinesIsZero` 场景下，结果为 `ReturnsOriginalText`。
    /// </summary>
    [Fact]
    public void TruncateByLines_MaxLinesIsZero_ReturnsOriginalText()
    {
        var text = $"A{Environment.NewLine}B{Environment.NewLine}C";
        var result = StringLines.TruncateByLines(text, 0);
        result.ShouldBe(text);
    }
    /// <summary>
    /// 测试用例：验证 `TruncateByLines` 在 `NegativeMaxLines` 场景下，结果为 `ReturnsEmptyString`。
    /// </summary>
    [Fact]
    public void TruncateByLines_NegativeMaxLines_ReturnsEmptyString()
    {
        var result = StringLines.TruncateByLines("A", -1);
        result.ShouldBe(string.Empty);
    }
}

