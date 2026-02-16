namespace Bing.Collections;

[Trait("Bing.Text", "StringCollectionExtensions")]
public class StringCollectionExtensionsTest
{
    [Fact]
    public void JoinToString_WithDefaultDelimiter_ShouldJoin()
    {
        var result = new[] { "a", "b", "c" }.JoinToString();
        result.ShouldBe("a,b,c");
    }

    [Fact]
    public void JoinToString_WithPredicateAndReplace_ShouldFilterAndReplace()
    {
        var result = new[] { "a", "b", "c" }
            .JoinToString("-", x => x != "b", x => x.ToUpperInvariant());

        // 当前实现语义：predicate 为 false 时使用 replaceFunc 替换，而不是过滤元素
        result.ShouldBe("a-B-c");
    }

    [Fact]
    public void JoinToString_WithNullList_ShouldReturnEmpty()
    {
        IEnumerable<string> source = null;
        source.JoinToString().ShouldBe(string.Empty);
    }

    [Fact]
    public void JoinOnePerLine_ShouldAppendTrailingNewLine()
    {
        var result = new[] { 1, 2 }.JoinOnePerLine();
        result.ShouldBe($"1{Environment.NewLine}2{Environment.NewLine}");
    }
}
