namespace Bing.Collections;
/// <summary>
/// 测试类：覆盖 `StringCollectionExtensions` 相关行为。
/// </summary>
[Trait("Bing.Text", "StringCollectionExtensions")]
public class StringCollectionExtensionsTest
{
    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `WithDefaultDelimiter` 场景下，结果为 `ShouldJoin`。
    /// </summary>
    [Fact]
    public void JoinToString_WithDefaultDelimiter_ShouldJoin()
    {
        var result = new[] { "a", "b", "c" }.JoinToString();
        result.ShouldBe("a,b,c");
    }
    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `WithPredicateAndReplace` 场景下，结果为 `ShouldFilterAndReplace`。
    /// </summary>
    [Fact]
    public void JoinToString_WithPredicateAndReplace_ShouldFilterAndReplace()
    {
        var result = new[] { "a", "b", "c" }
            .JoinToString("-", x => x != "b", x => x.ToUpperInvariant());
        result.ShouldBe("a-B-c");
    }
    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `WithNullList` 场景下，结果为 `ShouldReturnEmpty`。
    /// </summary>
    [Fact]
    public void JoinToString_WithNullList_ShouldReturnEmpty()
    {
        IEnumerable<string> source = null;
        source.JoinToString().ShouldBe(string.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `JoinOnePerLine` 在 `ShouldAppendTrailingNewLine` 场景下的行为。
    /// </summary>
    [Fact]
    public void JoinOnePerLine_ShouldAppendTrailingNewLine()
    {
        var result = new[] { 1, 2 }.JoinOnePerLine();
        result.ShouldBe($"1{Environment.NewLine}2{Environment.NewLine}");
    }
}

