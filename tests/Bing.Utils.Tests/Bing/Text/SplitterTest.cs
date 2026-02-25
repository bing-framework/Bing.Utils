using System.Text.RegularExpressions;
using Bing.Text.Splitters;
namespace Bing.Text;
/// <summary>
/// 测试类：覆盖 `Splitter` 相关行为。
/// </summary>
[Trait("StringUT", "Splitter")]
public class SplitterTest
{
    /// <summary>
    /// 测试用例：验证 `On` 在 `WithCharSeparatorAndTrimAndOmitEmpty` 场景下，结果为 `ReturnsExpectedParts`。
    /// </summary>
    [Fact]
    public void On_WithCharSeparatorAndTrimAndOmitEmpty_ReturnsExpectedParts()
    {
        var result = Splitter.On(',')
            .TrimResults()
            .OmitEmptyStrings()
            .SplitToArray(" a, b ,, c ");
        result.ShouldBe(["a", "b", "c"]);
    }
    /// <summary>
    /// 测试用例：验证 `On` 在 `WithLimit` 场景下，结果为 `ReturnsLimitedParts`。
    /// </summary>
    [Fact]
    public void On_WithLimit_ReturnsLimitedParts()
    {
        var result = Splitter.On(',')
            .Limit(2)
            .SplitToArray("a,b,c,d");
        result.ShouldBe(["a", "b"]);
    }
    /// <summary>
    /// 测试用例：验证 `OnPattern` 在 `WithRegex` 场景下，结果为 `ReturnsExpectedParts`。
    /// </summary>
    [Fact]
    public void OnPattern_WithRegex_ReturnsExpectedParts()
    {
        var result = Splitter.OnPattern(@"\s+")
            .OmitEmptyStrings()
            .SplitToArray("a  b\tc");
        result.ShouldBe(["a", "b", "c"]);
    }
    /// <summary>
    /// 测试用例：验证 `On` 在 `WithRegexInstance` 场景下，结果为 `ReturnsExpectedParts`。
    /// </summary>
    [Fact]
    public void On_WithRegexInstance_ReturnsExpectedParts()
    {
        var result = Splitter.On(new Regex(@"\s+"))
            .OmitEmptyStrings()
            .SplitToArray("a  b\tc");
        result.ShouldBe(["a", "b", "c"]);
    }
    /// <summary>
    /// 测试用例：验证 `FixedLength` 在 `WithNegativeLength` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void FixedLength_WithNegativeLength_ThrowsArgumentOutOfRangeException()
    {
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => Splitter.FixedLength(-1));
        ex.ParamName.ShouldBe("length");
    }
    /// <summary>
    /// 测试用例：验证 `FixedLength` 在 `SplitAndLimit` 场景下，结果为 `ReturnsExpectedParts`。
    /// </summary>
    [Fact]
    public void FixedLength_SplitAndLimit_ReturnsExpectedParts()
    {
        var result = Splitter.FixedLength(2)
            .Limit(2)
            .SplitToArray("abcdef");
        result.ShouldBe(["ab", "cd"]);
    }
    /// <summary>
    /// 测试用例：验证 `WithKeyValueSeparator` 在 `SplitToDictionary` 场景下，结果为 `ReturnsExpectedMap`。
    /// </summary>
    [Fact]
    public void WithKeyValueSeparator_SplitToDictionary_ReturnsExpectedMap()
    {
        var result = Splitter.On('&')
            .WithKeyValueSeparator('=')
            .SplitToDictionary("a=1&b=2&empty=");
        result.Count.ShouldBe(3);
        result["a"].ShouldBe("1");
        result["b"].ShouldBe("2");
        result["empty"].ShouldBe(string.Empty);
    }
}

