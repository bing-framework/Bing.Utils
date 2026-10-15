using System.Collections.Generic;
using System.Text;
using Bing.Text.Joiners;
using Shouldly;

namespace Bing.Text;

/// <summary>
/// 字符串连接器的分隔符、空值策略和追加测试。
/// </summary>
[Trait("TextUT", "Joiner")]
public class JoinerTest
{
    #region On — 基础连接

    /// <summary>
    /// 验证指定字符串分隔符连接全部元素。
    /// </summary>
    /// <param name="sep">连接分隔符。</param>
    /// <param name="items">待连接元素。</param>
    /// <param name="expected">预期连接文本。</param>
    [Theory]
    [InlineData(",", new[] { "a", "b", "c" }, "a,b,c")]
    [InlineData("-", new[] { "x", "y" }, "x-y")]
    [InlineData("", new[] { "a", "b", "c" }, "abc")]
    [InlineData(", ", new[] { "hello", "world" }, "hello, world")]
    public void Join_WithStringSeparator_JoinsCorrectly(string sep, string[] items, string expected)
    {
        // Act
        var result = Joiner.On(sep).Join(items);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 验证字符分隔符下 Join 应与字符串分隔符结果一致
    /// </summary>
    [Fact]
    public void Join_WithCharSeparator_MatchesStringSeparator()
    {
        // Arrange
        var items = new[] { "a", "b", "c" };

        // Act
        var charResult = Joiner.On(',').Join(items);
        var strResult = Joiner.On(",").Join(items);

        // Assert
        charResult.ShouldBe(strResult);
    }

    /// <summary>
    /// 验证使用可变参数形式的 Join 应与列表形式结果一致
    /// </summary>
    [Fact]
    public void Join_WithVarArgs_MatchesListJoin()
    {
        // Act
        var listResult = Joiner.On(",").Join(new[] { "a", "b", "c" });
        var varArgResult = Joiner.On(",").Join("a", "b", "c");

        // Assert
        varArgResult.ShouldBe(listResult);
    }

    /// <summary>
    /// 验证空集合调用 Join 应返回空字符串
    /// </summary>
    [Fact]
    public void Join_WithEmptyList_ReturnsEmptyString()
    {
        // Act
        var result = Joiner.On(",").Join(Array.Empty<string>());

        // Assert
        result.ShouldBe(string.Empty);
    }

    #endregion

    #region SkipNulls

    /// <summary>
    /// 验证启用 SkipNulls 后，null 元素应从连接结果中过滤掉
    /// </summary>
    [Fact]
    public void SkipNulls_FiltersNullElements()
    {
        // Arrange
        var items = new[] { "a", null, "b", null, "c" };

        // Act
        var result = Joiner.On(",").SkipNulls().Join(items);

        // Assert
        result.ShouldBe("a,b,c");
    }

    /// <summary>
    /// 验证所有元素均为 null 时启用 SkipNulls，应返回空字符串
    /// </summary>
    [Fact]
    public void SkipNulls_AllNullElements_ReturnsEmptyString()
    {
        // Arrange
        var items = new string[] { null, null, null };

        // Act
        var result = Joiner.On(",").SkipNulls().Join(items);

        // Assert
        result.ShouldBe(string.Empty);
    }

    #endregion

    #region UseForNull

    /// <summary>
    /// 验证UseForNull 指定固定替代值时，null 元素应被该值替代
    /// </summary>
    [Fact]
    public void UseForNull_WithFixedValue_ReplacesNullElements()
    {
        // Arrange
        var items = new[] { "a", null, "c" };

        // Act
        var result = Joiner.On(",").UseForNull("N/A").Join(items);

        // Assert
        result.ShouldBe("a,N/A,c");
    }

    /// <summary>
    /// 验证UseForNull 使用函数替代时，null 元素应被函数返回值替代
    /// </summary>
    [Fact]
    public void UseForNull_WithFunc_ReplacesNullWithFuncResult()
    {
        // Arrange
        var items = new[] { "hello", null, "world" };

        // Act
        var result = Joiner.On(",").UseForNull(_ => "replaced").Join(items);

        // Assert
        result.ShouldBe("hello,replaced,world");
    }

    #endregion

    #region Join<T> with Converter

    /// <summary>
    /// 验证泛型连接先转换元素再按分隔符连接。
    /// </summary>
    [Fact]
    public void Join_WithGenericTypeAndConverter_ConvertsAndJoins()
    {
        // Arrange
        var numbers = new[] { 1, 2, 3 };

        // Act
        var result = Joiner.On(", ").Join(numbers, n => n.ToString());

        // Assert
        result.ShouldBe("1, 2, 3");
    }

    /// <summary>
    /// 验证泛型连接在转换前跳过空元素。
    /// </summary>
    [Fact]
    public void Join_GenericWithSkipNulls_SkipsNullObjects()
    {
        // Arrange
        var items = new[] { "a", null, "c" };

        // Act
        var result = Joiner.On(",").SkipNulls().Join(items, s => s!.ToUpperInvariant());

        // Assert
        result.ShouldBe("A,C");
    }

    #endregion

    #region AppendTo(StringBuilder)

    /// <summary>
    /// 验证AppendTo 应将连接结果追加到已有的 StringBuilder 中
    /// </summary>
    [Fact]
    public void AppendTo_AppendsJoinedStringToBuilder()
    {
        // Arrange
        var builder = new StringBuilder("prefix:");
        var items = new[] { "a", "b", "c" };

        // Act
        Joiner.On(",").AppendTo(builder, items);

        // Assert
        builder.ToString().ShouldBe("prefix:a,b,c");
    }

    #endregion

    #region MapJoiner (WithKeyValueSeparator)

    /// <summary>
    /// 验证使用 WithKeyValueSeparator 后，Join 接受交替键值列表，应生成 key=value 格式字符串
    /// </summary>
    [Fact]
    public void WithKeyValueSeparator_JoinsAlternatingKeyValueList()
    {
        // Arrange — 交替排列：key, value, key, value …
        var kvList = new[] { "a", "1", "b", "2" };

        // Act
        var result = Joiner.On(",").WithKeyValueSeparator("=").Join(kvList);

        // Assert
        result.ShouldBe("a=1,b=2");
    }

    /// <summary>
    /// 验证字符形式的键值分隔符应与字符串形式效果相同
    /// </summary>
    [Fact]
    public void WithKeyValueSeparator_CharSeparator_SameAsStringSeparator()
    {
        // Arrange — 交替排列：key, value
        var kvList = new[] { "k", "v" };

        // Act
        var charResult = Joiner.On(",").WithKeyValueSeparator(':').Join(kvList);
        var strResult = Joiner.On(",").WithKeyValueSeparator(":").Join(kvList);

        // Assert
        charResult.ShouldBe(strResult);
    }

    #endregion
}
