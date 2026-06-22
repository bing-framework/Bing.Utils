using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 测试类：StringCollectionExtensions 字符串集合扩展方法（JoinToString 系列）
/// </summary>
[Trait("TextUT", "StringCollectionExtensions")]
public class StringCollectionExtensionsTest
{
    #region JoinToString — null 防护

    /// <summary>
    /// 测试目的：对 null 集合调用 JoinToString 应返回空字符串而非抛出异常
    /// </summary>
    [Fact]
    public void JoinToString_NullList_ReturnsEmpty()
    {
        // Arrange
        IEnumerable<string> list = null;

        // Act
        var result = list.JoinToString();

        // Assert
        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试目的：对 null 泛型集合调用 JoinToString&lt;T&gt; 应返回空字符串
    /// </summary>
    [Fact]
    public void JoinToStringGeneric_NullList_ReturnsEmpty()
    {
        // Arrange
        IEnumerable<int> list = null;

        // Act
        var result = list.JoinToString();

        // Assert
        result.ShouldBe(string.Empty);
    }

    #endregion

    #region JoinToString — 基础字符串集合

    /// <summary>
    /// 测试目的：JoinToString() 默认以逗号分隔连接字符串集合
    /// </summary>
    [Fact]
    public void JoinToString_DefaultDelimiter_JoinsWithComma()
    {
        // Act
        var result = new[] { "a", "b", "c" }.JoinToString();

        // Assert
        result.ShouldBe("a,b,c");
    }

    /// <summary>
    /// 测试目的：JoinToString 对空集合应返回空字符串
    /// </summary>
    [Fact]
    public void JoinToString_EmptyList_ReturnsEmpty()
    {
        // Act
        var result = Array.Empty<string>().JoinToString();

        // Assert
        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试目的：JoinToString 对单元素集合应返回该元素本身
    /// </summary>
    [Fact]
    public void JoinToString_SingleElement_ReturnsSingleElement()
    {
        // Act
        var result = new[] { "hello" }.JoinToString();

        // Assert
        result.ShouldBe("hello");
    }

    /// <summary>
    /// 测试目的：JoinToString(delimiter) 应使用指定分隔符连接
    /// </summary>
    [Theory]
    [InlineData(";", "a;b;c")]
    [InlineData("-", "a-b-c")]
    [InlineData("", "abc")]
    [InlineData(" | ", "a | b | c")]
    public void JoinToString_WithDelimiter_JoinsCorrectly(string delimiter, string expected)
    {
        // Act
        var result = new[] { "a", "b", "c" }.JoinToString(delimiter);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region JoinToString — 泛型集合

    /// <summary>
    /// 测试目的：JoinToString&lt;T&gt;() 对整数集合应转为字符串并以逗号分隔
    /// </summary>
    [Fact]
    public void JoinToStringGeneric_IntList_JoinsWithComma()
    {
        // Act
        var result = new[] { 1, 2, 3 }.JoinToString();

        // Assert
        result.ShouldBe("1,2,3");
    }

    /// <summary>
    /// 测试目的：JoinToString&lt;T&gt;(delimiter) 对整数集合应使用指定分隔符
    /// </summary>
    [Theory]
    [InlineData("|", "1|2|3")]
    [InlineData(" -> ", "1 -> 2 -> 3")]
    public void JoinToStringGeneric_WithDelimiter_JoinsCorrectly(string delimiter, string expected)
    {
        // Act
        var result = new[] { 1, 2, 3 }.JoinToString(delimiter);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region JoinToString — 带条件过滤

    /// <summary>
    /// 测试目的：JoinToString(predicate) 应仅连接满足条件的字符串
    /// </summary>
    [Fact]
    public void JoinToString_WithPredicate_FiltersItems()
    {
        // Arrange: 只保留不等于 "b" 的元素
        var list = new[] { "a", "b", "c" };

        // Act
        var result = list.JoinToString(s => s != "b");

        // Assert
        result.ShouldBe("a,c");
    }

    /// <summary>
    /// 测试目的：JoinToString(predicate) 当所有元素都被过滤时应返回空字符串
    /// </summary>
    [Fact]
    public void JoinToString_WithPredicateAllFiltered_ReturnsEmpty()
    {
        // Act
        var result = new[] { "a", "b", "c" }.JoinToString(_ => false);

        // Assert
        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试目的：JoinToString(indexPredicate) 应按索引条件过滤（仅保留偶数索引）
    /// </summary>
    [Fact]
    public void JoinToString_WithIndexPredicate_FiltersCorrectly()
    {
        // Arrange: 偶数索引(0,2)→ "a", "c"
        var list = new[] { "a", "b", "c" };

        // Act
        var result = list.JoinToString((s, i) => i % 2 == 0);

        // Assert
        result.ShouldBe("a,c");
    }

    /// <summary>
    /// 测试目的：JoinToString(delimiter, predicate) 应同时使用自定义分隔符和过滤条件
    /// </summary>
    [Fact]
    public void JoinToString_WithDelimiterAndPredicate_FiltersAndJoins()
    {
        // Arrange: 分隔符 "-"，只保留长度 > 1 的字符串
        var list = new[] { "a", "bb", "c", "dd" };

        // Act
        var result = list.JoinToString("-", s => s.Length > 1);

        // Assert
        result.ShouldBe("bb-dd");
    }

    #endregion

    #region JoinToString — 带转换函数

    /// <summary>
    /// 测试目的：JoinToString&lt;T&gt;(delimiter, to) 应使用自定义转换函数格式化元素
    /// </summary>
    [Fact]
    public void JoinToStringGeneric_WithToFunc_AppliesTransformation()
    {
        // Arrange: 将每个字符串转为大写
        var list = new[] { "hello", "world" };

        // Act
        var result = list.JoinToString(",", s => s.ToUpper());

        // Assert
        result.ShouldBe("HELLO,WORLD");
    }

    #endregion

    #region JoinOnePerLine

    /// <summary>
    /// 测试目的：JoinOnePerLine 应将每个元素单独占一行，并以换行符结尾
    /// </summary>
    [Fact]
    public void JoinOnePerLine_WithNumbers_EachOnSeparateLine()
    {
        // Act
        var result = new[] { 1, 2, 3 }.JoinOnePerLine();

        // Assert
        result.ShouldContain("1");
        result.ShouldContain("2");
        result.ShouldContain("3");
        result.ShouldEndWith(Environment.NewLine);
    }

    #endregion

    #region JoinToStringFormat

    /// <summary>
    /// 测试目的：JoinToStringFormat() 对实现 IFormattable 的集合应返回非空字符串
    /// </summary>
    [Fact]
    public void JoinToStringFormat_WithDoubles_ReturnsNonEmpty()
    {
        // Act
        var result = new[] { 1.0, 2.0, 3.0 }.JoinToStringFormat();

        // Assert
        result.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 测试目的：JoinToStringFormat(delimiter) 应使用指定分隔符连接格式化后的元素
    /// </summary>
    [Fact]
    public void JoinToStringFormat_WithDelimiter_ContainsDelimiter()
    {
        // Act
        var result = new[] { 1, 2, 3 }.JoinToStringFormat("|");

        // Assert
        result.ShouldContain("|");
    }

    /// <summary>
    /// 测试目的：JoinToStringFormat 对整数集合应返回正确的逗号分隔字符串
    /// </summary>
    [Fact]
    public void JoinToStringFormat_IntList_DefaultDelimiter()
    {
        // Act
        var result = new[] { 1, 2, 3 }.JoinToStringFormat();

        // Assert
        result.ShouldBe("1,2,3");
    }

    #endregion
}
