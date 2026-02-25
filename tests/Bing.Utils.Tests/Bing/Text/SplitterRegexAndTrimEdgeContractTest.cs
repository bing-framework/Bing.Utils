using System.Text.RegularExpressions;
using Bing.Text.Splitters;

namespace Bing.Text;

/// <summary>
/// 测试类：覆盖 Splitter 在正则与裁剪组合下的边界契约。
/// </summary>
[Trait("Bing.Text", "Splitter.RegexTrim.EdgeContract")]
public class SplitterRegexAndTrimEdgeContractTest
{
    /// <summary>
    /// 测试用例：OnPattern(string) + Limit 在当前实现中不会应用 limit（字符串正则路径）。
    /// </summary>
    [Fact]
    public void OnPatternString_WithLimit_CurrentlyIgnoresLimit()
    {
        var result = Splitter.OnPattern(@"\s+")
            .Limit(2)
            .SplitToArray("a b c d");

        result.ShouldBe(["a", "b", "c", "d"]);
    }

    /// <summary>
    /// 测试用例：On(Regex) + Limit 在当前实现中会应用 limit（Regex 实例路径）。
    /// </summary>
    [Fact]
    public void OnRegexInstance_WithLimit_AppliesLimit()
    {
        var result = Splitter.On(new Regex(@"\s+"))
            .Limit(2)
            .SplitToArray("a b c d");

        result.ShouldBe(["a", "b c d"]);
    }

    /// <summary>
    /// 测试用例：OmitEmptyStrings 与 TrimResults 组合时，空白项会先保留后裁剪为空串。
    /// </summary>
    [Fact]
    public void OmitEmptyThenTrim_WhitespaceTokensRemainAsEmptyStrings()
    {
        var result = Splitter.On(',')
            .OmitEmptyStrings()
            .TrimResults()
            .SplitToArray(" , a ,, ");

        result.ShouldBe(["", "a", ""]);
    }

    /// <summary>
    /// 测试用例：TrimResults(func) 应使用传入的自定义裁剪函数。
    /// </summary>
    [Fact]
    public void TrimResults_WithCustomFunc_UsesProvidedFunction()
    {
        var result = Splitter.On(',')
            .TrimResults(x => $"[{x}]")
            .SplitToArray("a,b");

        result.ShouldBe(["[a]", "[b]"]);
    }
}
