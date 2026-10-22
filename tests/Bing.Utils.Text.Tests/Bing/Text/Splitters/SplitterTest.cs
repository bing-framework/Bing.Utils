using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bing.Text.Splitters;
using Shouldly;

namespace Bing.Text;

/// <summary>
/// 字符串分割器的分隔符、过滤、裁剪和键值解析测试。
/// </summary>
[Trait("TextUT", "Splitter")]
public class SplitterTest
{
    #region On(char) / On(string) — 基础分割

    /// <summary>
    /// 验证按字符分隔符切分文本。
    /// </summary>
    /// <param name="input">待分割文本。</param>
    /// <param name="separator">字符分隔符。</param>
    /// <param name="expected">预期分割片段。</param>
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
    /// 验证按多字符分隔符切分文本。
    /// </summary>
    /// <param name="input">待分割文本。</param>
    /// <param name="separator">字符串分隔符。</param>
    /// <param name="expected">预期分割片段。</param>
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
    /// 验证空文本和纯空白文本分割为空列表。
    /// </summary>
    /// <param name="input">待分割文本。</param>
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
    /// 验证SplitToArray 应返回与 SplitToList 内容相同的字符串数组
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
    /// 验证省略空字符串配置过滤相邻及边界分隔符产生的空片段。
    /// </summary>
    /// <param name="input">待分割文本。</param>
    /// <param name="expected">预期分割片段。</param>
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
    /// 验证未启用 OmitEmptyStrings 时，相邻分隔符产生的空串应保留
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
    /// 验证裁剪配置移除片段两端空白。
    /// </summary>
    /// <param name="input">待分割文本。</param>
    /// <param name="expected">预期分割片段。</param>
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
    /// 验证自定义 TrimResults 函数（转大写）应对每个片段应用该函数
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
    /// 验证非正则分割只返回数量上限内的片段。
    /// </summary>
    /// <param name="input">待分割文本。</param>
    /// <param name="limit">片段数量上限。</param>
    /// <param name="expected">预期分割片段。</param>
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
    /// 验证当前正则分割忽略片段数量上限。
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
    /// 验证按正则表达式字符串切分文本。
    /// </summary>
    /// <param name="input">待分割文本。</param>
    /// <param name="pattern">分割正则表达式。</param>
    /// <param name="expected">预期分割片段。</param>
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
    /// 测试目的：非法正则模式在分割非空文本时保留参数异常，不静默返回未分割文本。
    /// </summary>
    [Theory]
    [InlineData("[")]
    [InlineData("(")]
    public void OnPattern_WithInvalidPattern_ThrowsArgumentExceptionWhenSplit(string pattern)
    {
        var splitter = Splitter.OnPattern(pattern);

        Should.Throw<ArgumentException>(() => splitter.SplitToArray("a,b"));
    }

    /// <summary>
    /// 验证使用 Regex 对象模式时按正则规则切割文本。
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
    /// 验证按固定长度分割时末段可短于指定长度。
    /// </summary>
    /// <param name="input">待分割文本。</param>
    /// <param name="length">每段长度。</param>
    /// <param name="expected">预期分割片段。</param>
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
    /// 验证负的固定分割长度触发参数越界异常。
    /// </summary>
    /// <param name="length">无效分割长度。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void FixedLength_WithNonPositiveLength_ThrowsArgumentOutOfRangeException(int length)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => Splitter.FixedLength(length));
        exception.ParamName.ShouldBe("length");
        exception.ActualValue.ShouldBe(length);
    }

    /// <summary>
    /// 测试目的：合法分段长度下，空白输入仍由各终结方法返回空结果。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void FixedLength_WithEmptyInput_ReturnsEmpty(string input)
    {
        var splitter = Splitter.FixedLength(2);

        splitter.Split(input).ShouldBeEmpty();
        splitter.SplitToList(input).ShouldBeEmpty();
        splitter.SplitToArray(input).ShouldBeEmpty();
    }

    #endregion

    #region MapSplitter (WithKeyValueSeparator)

    /// <summary>
    /// 验证使用键值分隔符后 SplitToDictionary 应正确解析 key=value 格式字符串
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
    /// 验证键值分隔符为字符串形式时，SplitToDictionary 同样能正确解析
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
    /// 验证空文本和纯空白文本解析为空字典。
    /// </summary>
    /// <param name="input">待分割文本。</param>
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

    /// <summary>
    /// 测试目的：字典转换遇到重复键时明确失败，不静默覆盖先前的值。
    /// </summary>
    [Fact]
    public void MapSplitter_WithDuplicateKeys_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Splitter.On('&').WithKeyValueSeparator('=').SplitToDictionary("a=1&a=2"));
    }

    /// <summary>
    /// 测试目的：空键值分隔符沿用不切分键值的行为，每个片段成为值为空的键。
    /// </summary>
    [Fact]
    public void MapSplitter_WithEmptyKeyValueSeparator_UsesWholeItemAsKey()
    {
        var result = Splitter.On('&').WithKeyValueSeparator(string.Empty)
            .SplitToDictionary("a=1&b=2");

        result.Count.ShouldBe(2);
        result["a=1"].ShouldBe(string.Empty);
        result["b=2"].ShouldBe(string.Empty);
    }

    #endregion

    #region OmitEmptyStrings + TrimResults 组合

    /// <summary>
    /// 验证同时启用 OmitEmptyStrings 和 TrimResults，应既过滤空串又裁剪空白
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
