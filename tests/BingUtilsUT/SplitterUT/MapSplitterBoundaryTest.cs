using Bing.Text.Splitters;
using System.Linq;

namespace BingUtilsUT.SplitterUT;

/// <summary>
/// 测试类：覆盖 MapSplitter 的边界输入与契约行为。
/// </summary>
[Trait("SplitterUT", "MapSplitter.Boundary")]
public class MapSplitterBoundaryTest
{
    /// <summary>
    /// 测试用例：当输入为 null/空白字符串时，应返回空结果。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Split_NullOrWhiteSpaceInput_ReturnsEmpty(string input)
    {
        var result = Splitter.On("&").WithKeyValueSeparator("=").Split(input);
        var dictionary = Splitter.On("&").WithKeyValueSeparator("=").SplitToDictionary(input);

        result.ShouldBeEmpty();
        dictionary.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试用例：当键值分隔符不存在时，值应为空字符串。
    /// </summary>
    [Fact]
    public void Split_ItemWithoutValueSeparator_ReturnsEmptyValue()
    {
        var result = Splitter.On("&").WithKeyValueSeparator("=").Split("a&b=2").ToList();

        result.Count.ShouldBe(2);
        result[0].Key.ShouldBe("a");
        result[0].Value.ShouldBe(string.Empty);
        result[1].Key.ShouldBe("b");
        result[1].Value.ShouldBe("2");
    }

    /// <summary>
    /// 测试用例：当值中包含多个分隔符时，仅保留首段值（当前实现行为）。
    /// </summary>
    [Fact]
    public void Split_ValueContainsMultipleSeparators_UsesFirstValueSegment()
    {
        var result = Splitter.On("&").WithKeyValueSeparator("=").Split("a=1=2&b=3").ToList();

        result.Count.ShouldBe(2);
        result[0].Key.ShouldBe("a");
        result[0].Value.ShouldBe("1");
        result[1].Key.ShouldBe("b");
        result[1].Value.ShouldBe("3");
    }

    /// <summary>
    /// 测试用例：TrimResults 传入 null 函数时，应回退到默认 Trim 逻辑。
    /// </summary>
    [Fact]
    public void TrimResults_NullTrimFunctions_FallsBackToDefaultTrim()
    {
        var result = Splitter.On("&")
            .WithKeyValueSeparator("=")
            .TrimResults(null, null)
            .Split(" a = 1 & b = 2 ")
            .ToList();

        result.Count.ShouldBe(2);
        result[0].Key.ShouldBe("a");
        result[0].Value.ShouldBe("1");
        result[1].Key.ShouldBe("b");
        result[1].Value.ShouldBe("2");
    }

    /// <summary>
    /// 测试用例：Limit 小于等于 0 时，应按无限制处理。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Limit_NonPositiveValue_BehavesAsUnlimited(int limit)
    {
        var result = Splitter.On("&")
            .WithKeyValueSeparator("=")
            .Limit(limit)
            .Split("a=1&b=2&c=3")
            .ToList();

        result.Count.ShouldBe(3);
    }

    /// <summary>
    /// 测试用例：SplitToDictionary 遇到重复键时，应抛出 ArgumentException。
    /// </summary>
    [Fact]
    public void SplitToDictionary_DuplicateKeys_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Splitter.On("&").WithKeyValueSeparator("=").SplitToDictionary("a=1&a=2"));
    }

    /// <summary>
    /// 测试用例：FixedLength 传入负数时，应抛出参数越界异常。
    /// </summary>
    [Fact]
    public void FixedLength_NegativeLength_ThrowsArgumentOutOfRangeException()
    {
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => Splitter.FixedLength(-1));

        ex.ParamName.ShouldBe("length");
    }
}
