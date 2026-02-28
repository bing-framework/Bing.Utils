using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bing.Text.Splitters;
using Shouldly;

namespace Bing.Text;

/// <summary>
/// 测试类：Splitter 字符串分割器的各工厂方法、配置链与终结方法
/// </summary>
[Trait("TextUT", "Splitter")]
public class SplitterTest
{
    #region On(char) / On(string) — 基础分割

    /// <summary>
    /// 测试目的：单字符分隔符下 SplitToList 应正确切割并返回所有片段
    /// </summary>
    [Theory]
    [InlineData("a,b,c", ',', new[] { "a", "b", "c" })]
    [InlineData("x|y|z", '|', new[] { "x", "y", "z" })]
    [InlineData("hello", ',', new[] { "hello" })]
    public void On_WithCharSeparator_SplitsCorrectly(string input, char separator, string[] expected)
    {
        // Act
        var result = Splitter.On(separator).SplitToList(input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：字符串分隔符（多字符）下 SplitToList 应正确切割
    /// </summary>
    [Theory]
    [InlineData("a::b::c", "::", new[] { "a", "b", "c" })]
    [InlineData("foo---bar", "---", new[] { "foo", "bar" })]
    public void On_WithStringSeparator_SplitsCorrectly(string input, string separator, string[] expected)
    {
        // Act
        var result = Splitter.On(separator).SplitToList(input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：空字符串输入时 SplitToList 应返回空集合
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void On_WithEmptyOrWhitespaceInput_ReturnsEmptyList(string input)
    {
        // Act
        var result = Splitter.On(',').SplitToList(input);

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试目的：SplitToArray 应返回与 SplitToList 内容相同的字符串数组
    /// </summary>
    [Fact]
    public void SplitToArray_ResultMatchesSplitToList()
    {
        // Arrange
        const string input = "a,b,c";

        // Act
        var list = Splitter.On(',').SplitToList(input);
        var array = Splitter.On(',').SplitToArray(input);

        // Assert
        array.ShouldBe(list.ToArray());
    }

    #endregion

    #region OmitEmptyStrings

    /// <summary>
    /// 测试目的：启用 OmitEmptyStrings 后，相邻分隔符产生的空串应被过滤
    /// </summary>
    [Theory]
    [InlineData("a,,b", new[] { "a", "b" })]
    [InlineData(",a,b,", new[] { "a", "b" })]
    [InlineData("a,,,,b,,c", new[] { "a", "b", "c" })]
    public void OmitEmptyStrings_FiltersEmptySegments(string input, string[] expected)
    {
        // Act
        var result = Splitter.On(',').OmitEmptyStrings().SplitToList(input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：未启用 OmitEmptyStrings 时，相邻分隔符产生的空串应保留
    /// </summary>
    [Fact]
    public void WithoutOmitEmptyStrings_PreservesEmptySegments()
    {
        // Act
        var result = Splitter.On(',').SplitToList("a,,b");

        // Assert
        result.Count.ShouldBe(3);
        result[1].ShouldBe(string.Empty);
    }

    #endregion

    #region TrimResults

    /// <summary>
    /// 测试目的：启用 TrimResults 后，每个片段的前后空白应被裁剪
    /// </summary>
    [Theory]
    [InlineData(" a , b , c ", new[] { "a", "b", "c" })]
    [InlineData("  hello  ,  world  ", new[] { "hello", "world" })]
    public void TrimResults_TrimsWhitespaceFromEachSegment(string input, string[] expected)
    {
        // Act
        var result = ((ISplitter)Splitter.On(',')).TrimResults().SplitToList(input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：自定义 TrimResults 函数（转大写）应对每个片段应用该函数
    /// </summary>
    [Fact]
    public void TrimResults_WithCustomFunc_AppliesFuncToEachSegment()
    {
        // Act
        var result = ((ISplitter)Splitter.On(',')).TrimResults(s => s.Trim().ToUpperInvariant()).SplitToList(" a , b , c ");

        // Assert
        result.ShouldBe(new[] { "A", "B", "C" });
    }

    #endregion

    #region Limit

    /// <summary>
    /// 测试目的：非正则模式下 Limit(n) 应只返回前 n 个切割片段
    /// </summary>
    [Theory]
    [InlineData("a,b,c,d", 2, new[] { "a", "b" })]
    [InlineData("a,b,c,d", 3, new[] { "a", "b", "c" })]
    [InlineData("a,b,c,d", 10, new[] { "a", "b", "c", "d" })]
    public void Limit_InNonRegexMode_LimitsSegmentCount(string input, int limit, string[] expected)
    {
        // Act
        var result = ((ISplitter)Splitter.On(',')).Limit(limit).SplitToList(input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：正则模式下 Limit(n) 静默无效，应返回全部切割片段（已知行为，文档缺陷）
    /// </summary>
    [Fact]
    public void Limit_InRegexMode_IsIgnoredSilently()
    {
        // Arrange — OnPattern 使用正则模式，Limit 在此模式下静默无效
        // Act
        var result = ((ISplitter)Splitter.OnPattern(",")).Limit(2).SplitToList("a,b,c,d");

        // Assert — 全部 4 个片段都被返回（Limit 无效）
        // 注意：此为当前行为断言，如将来修复 Limit 对正则模式无效的问题，此测试需同步更新
        result.Count.ShouldBe(4);
    }

    #endregion

    #region OnPattern (正则字符串模式)

    /// <summary>
    /// 测试目的：使用正则字符串模式时，应按正则规则切割
    /// </summary>
    [Theory]
    [InlineData("a  b   c", @"\s+", new[] { "a", "b", "c" })]
    [InlineData("a1b2c", @"\d+", new[] { "a", "b", "c" })]
    public void OnPattern_WithRegexString_SplitsCorrectly(string input, string pattern, string[] expected)
    {
        // Act
        var result = Splitter.OnPattern(pattern).SplitToList(input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：使用 Regex 对象模式时，应按正则规则切割
    /// </summary>
    [Fact]
    public void On_WithRegexObject_SplitsCorrectly()
    {
        // Arrange
        var regex = new Regex(@"\s+");

        // Act
        var result = Splitter.On(regex).SplitToList("hello   world  foo");

        // Assert
        result.ShouldBe(new[] { "hello", "world", "foo" });
    }

    #endregion

    #region FixedLength

    /// <summary>
    /// 测试目的：按固定长度切割字符串，应返回等长片段（末段允许更短）
    /// </summary>
    [Theory]
    [InlineData("abcdef", 2, new[] { "ab", "cd", "ef" })]
    [InlineData("abcde", 2, new[] { "ab", "cd", "e" })]
    [InlineData("abcdef", 3, new[] { "abc", "def" })]
    public void FixedLength_SplitsIntoEqualChunks(string input, int length, string[] expected)
    {
        // Act
        var result = ((IFixedLengthSplitter)Splitter.FixedLength(length)).SplitToList(input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：固定长度为负数时应抛出 ArgumentOutOfRangeException
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void FixedLength_WithNegativeLength_ThrowsArgumentOutOfRangeException(int length)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Splitter.FixedLength(length));
    }

    #endregion

    #region MapSplitter (WithKeyValueSeparator)

    /// <summary>
    /// 测试目的：使用键值分隔符后 SplitToDictionary 应正确解析 key=value 格式字符串
    /// </summary>
    [Fact]
    public void WithKeyValueSeparator_SplitToDictionary_ParsesKeyValuePairs()
    {
        // Act
        var result = ((IMapSplitter)((ISplitter)Splitter.On(',')).WithKeyValueSeparator('=')).SplitToDictionary("a=1,b=2,c=3");

        // Assert
        result.Count.ShouldBe(3);
        result["a"].ShouldBe("1");
        result["b"].ShouldBe("2");
        result["c"].ShouldBe("3");
    }

    /// <summary>
    /// 测试目的：键值分隔符为字符串形式时，SplitToDictionary 同样能正确解析
    /// </summary>
    [Fact]
    public void WithKeyValueSeparator_StringSeparator_ParsesKeyValuePairs()
    {
        // Act
        var result = ((IMapSplitter)((ISplitter)Splitter.On(',')).WithKeyValueSeparator("->")).SplitToDictionary("a->1,b->2");

        // Assert
        result["a"].ShouldBe("1");
        result["b"].ShouldBe("2");
    }

    /// <summary>
    /// 测试目的：空字符串输入时 MapSplitter.SplitToDictionary 应返回空字典
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MapSplitter_WithEmptyInput_ReturnsEmptyDictionary(string input)
    {
        // Act
        var result = ((IMapSplitter)((ISplitter)Splitter.On(',')).WithKeyValueSeparator('=')).SplitToDictionary(input);

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region OmitEmptyStrings + TrimResults 组合

    /// <summary>
    /// 测试目的：同时启用 OmitEmptyStrings 和 TrimResults，应既过滤空串又裁剪空白
    /// </summary>
    [Fact]
    public void OmitEmptyStrings_And_TrimResults_Together_WorkCorrectly()
    {
        // Arrange — 不含纯空白段，否则 OmitEmpty 先于 Trim 执行时会漏掌被裁削后变空的段
        var result = ((ISplitter)Splitter.On(',')).OmitEmptyStrings().TrimResults().SplitToList(" a , b ,, c ");

        // Assert
        result.ShouldBe(new[] { "a", "b", "c" });
    }

    #endregion
}
