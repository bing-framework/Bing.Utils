using System.Globalization;
namespace Bing.Collections;
/// <summary>
/// 测试类：覆盖 `StringCollectionExtensionsAdvanced` 相关行为。
/// </summary>
[Trait("Bing.Text", "StringCollectionExtensions.Advanced")]
public class StringCollectionExtensionsAdvancedTest
{
    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `WithPredicateFalseAndNoReplace` 场景下，结果为 `SkipsItem`。
    /// </summary>
    [Fact]
    public void JoinToString_WithPredicateFalseAndNoReplace_SkipsItem()
    {
        var result = new[] { "a", "b", "c" }
            .JoinToString("-", x => x != "b", replaceFunc: null);
        result.ShouldBe("a-c");
    }
    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `WithIndexPredicateAndReplace` 场景下，结果为 `UsesReplacementOnFalse`。
    /// </summary>
    [Fact]
    public void JoinToString_WithIndexPredicateAndReplace_UsesReplacementOnFalse()
    {
        var result = new[] { "x", "y", "z" }
            .JoinToString(
                ",",
                (item, index) => index != 1,
                (item, index) => $"{item}{index}");
        result.ShouldBe("x,y1,z");
    }
    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `GenericWithToFunc` 场景下，结果为 `UsesCustomProjection`。
    /// </summary>
    [Fact]
    public void JoinToString_GenericWithToFunc_UsesCustomProjection()
    {
        var result = new[] { 1, 2, 3 }
            .JoinToString(";", x => $"[{x}]");
        result.ShouldBe("[1];[2];[3]");
    }
    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `GenericWithPredicateAndReplace` 场景下，结果为 `AppliesBothRules`。
    /// </summary>
    [Fact]
    public void JoinToString_GenericWithPredicateAndReplace_AppliesBothRules()
    {
        var result = new[] { 1, 2, 3, 4 }
            .JoinToString(
                "|",
                x => x % 2 == 1,
                x => x * 10);
        result.ShouldBe("1|20|3|40");
    }
    /// <summary>
    /// 测试用例：验证 `JoinToStringFormat` 在 `WithProvider` 场景下，结果为 `UsesProviderFormatting`。
    /// </summary>
    [Fact]
    public void JoinToStringFormat_WithProvider_UsesProviderFormatting()
    {
        var value = 1.5m;
        var provider = new CultureInfo("fr-FR");
        var result = new[] { value }.JoinToStringFormat(";", provider);
        result.ShouldContain(",");
    }
    /// <summary>
    /// 测试用例：验证 `JoinOnePerLine` 在 `WithEmptyCollection` 场景下，结果为 `CurrentlyReturnsTrailingNewLine`。
    /// </summary>
    [Fact]
    public void JoinOnePerLine_WithEmptyCollection_CurrentlyReturnsTrailingNewLine()
    {
        var result = Array.Empty<int>().JoinOnePerLine();
        result.ShouldBe(Environment.NewLine);
    }
    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `WithNullListAndCustomDelimiter` 场景下，结果为 `ReturnsEmptyString`。
    /// </summary>
    [Fact]
    public void JoinToString_WithNullListAndCustomDelimiter_ReturnsEmptyString()
    {
        IEnumerable<int> source = null;
        var result = source.JoinToString("|");
        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试用例：验证 `JoinOnePerLine` 在 `NullList` 场景下，结果为 `CurrentlyReturnsTrailingNewLine`。
    /// </summary>
    [Fact]
    public void JoinOnePerLine_NullList_CurrentlyReturnsTrailingNewLine()
    {
        IEnumerable<int> source = null;

        var result = source.JoinOnePerLine();

        result.ShouldBe(Environment.NewLine);
    }

    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `NullProjectionFunc` 场景下，结果为 `ThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void JoinToString_NullProjectionFunc_ThrowsNullReferenceException()
    {
        var source = new[] { 1, 2, 3 };

        Should.Throw<NullReferenceException>(() => source.JoinToString(",", to: null));
    }

    /// <summary>
    /// 测试用例：验证 `JoinToStringFormat` 在 `NullList` 场景下，结果为 `ReturnsEmptyString`。
    /// </summary>
    [Fact]
    public void JoinToStringFormat_NullList_ReturnsEmptyString()
    {
        IEnumerable<decimal> source = null;

        var result = source.JoinToStringFormat("|", CultureInfo.InvariantCulture);

        result.ShouldBe(string.Empty);
    }
}

