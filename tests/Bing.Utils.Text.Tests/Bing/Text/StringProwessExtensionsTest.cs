using System;
using System.Collections.Generic;
using Bing.Collections;
using Shouldly;
using Xunit;

namespace Bing.Text;

/// <summary>
/// 测试类：StringProwessExtensions 字符串扩展方法（Join/Split 系列）
/// </summary>
[Trait("TextUT", "StringProwessExtensions")]
public class StringProwessExtensionsTest
{
    #region JoinStringFor

    /// <summary>
    /// 测试目的：JoinStringFor 应将集合元素以给定分隔符连接成字符串
    /// </summary>
    [Theory]
    [InlineData(",", new[] { "a", "b", "c" }, "a,b,c")]
    [InlineData("-", new[] { "x", "y" }, "x-y")]
    [InlineData("", new[] { "a", "b" }, "ab")]
    [InlineData("|", new[] { "hello", "world" }, "hello|world")]
    public void JoinStringFor_WithSeparator_JoinsCorrectly(string separator, string[] items, string expected)
    {
        // Act
        var result = separator.JoinStringFor(items);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：对空集合调用 JoinStringFor 应返回空字符串
    /// </summary>
    [Fact]
    public void JoinStringFor_EmptyList_ReturnsEmptyString()
    {
        // Act
        var result = ",".JoinStringFor(Array.Empty<string>());

        // Assert
        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试目的：JoinStringFor 对整数集合（泛型）应正常转换并连接
    /// </summary>
    [Fact]
    public void JoinStringFor_IntCollection_JoinsCorrectly()
    {
        // Arrange
        var numbers = new[] { 1, 2, 3 };

        // Act
        var result = ",".JoinStringFor(numbers);

        // Assert
        result.ShouldBe("1,2,3");
    }

    #endregion

    #region SplitByIndex

    /// <summary>
    /// 测试目的：正常索引下 SplitByIndex 应在指定位置（1-based）分割字符串
    /// </summary>
    [Theory]
    [InlineData("abcdef", 3, "ab", "cdef")]
    [InlineData("abcdef", 4, "abc", "def")]
    [InlineData("hello", 2, "h", "ello")]
    public void SplitByIndex_ValidIndex_SplitsCorrectly(string input, int index, string expectedFirst, string expectedSecond)
    {
        // Act
        var result = input.SplitByIndex(index);

        // Assert
        result.Item1.ShouldBe(expectedFirst);
        result.Item2.ShouldBe(expectedSecond);
    }

    /// <summary>
    /// 测试目的：对空字符串调用 SplitByIndex 应返回两个空字符串
    /// </summary>
    [Fact]
    public void SplitByIndex_EmptyString_ReturnsBothEmpty()
    {
        // Act
        var result = "".SplitByIndex(3);

        // Assert
        result.Item1.ShouldBe("");
        result.Item2.ShouldBe("");
    }

    /// <summary>
    /// 测试目的：索引超过或等于字符串长度时，整个字符串在 Item1，Item2 为空
    /// </summary>
    [Theory]
    [InlineData("hello", 5)]
    [InlineData("hello", 100)]
    public void SplitByIndex_IndexExceedsLength_ReturnsAllInFirst(string input, int index)
    {
        // Act
        var result = input.SplitByIndex(index);

        // Assert
        result.Item1.ShouldBe(input);
        result.Item2.ShouldBe("");
    }

    /// <summary>
    /// 测试目的：索引为 0 或负数时，Item1 为空，Item2 为整个字符串
    /// </summary>
    [Theory]
    [InlineData("hello", 0)]
    [InlineData("hello", -1)]
    public void SplitByIndex_IndexZeroOrNegative_ReturnsAllInSecond(string input, int index)
    {
        // Act
        var result = input.SplitByIndex(index);

        // Assert
        result.Item1.ShouldBe("");
        result.Item2.ShouldBe(input);
    }

    #endregion

    #region SplitTyped<T>

    /// <summary>
    /// 测试目的：SplitTyped&lt;int&gt;(char) 应按字符分隔符分割并转换为整数数组
    /// </summary>
    [Fact]
    public void SplitTyped_CharDelimiter_Int_ReturnsIntArray()
    {
        // Act
        var result = "1,2,3".SplitTyped<int>(',');

        // Assert
        result.ShouldBe(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// 测试目的：SplitTyped&lt;string&gt;(char) 应按字符分隔符分割并返回字符串数组
    /// </summary>
    [Fact]
    public void SplitTyped_CharDelimiter_String_ReturnsStringArray()
    {
        // Act
        var result = "a,b,c".SplitTyped<string>(',');

        // Assert
        result.ShouldBe(new[] { "a", "b", "c" });
    }

    /// <summary>
    /// 测试目的：SplitTyped&lt;int&gt;(string) 应按字符串分隔符分割并转换为整数数组
    /// </summary>
    [Fact]
    public void SplitTyped_StringDelimiter_Int_ReturnsIntArray()
    {
        // Act
        var result = "10|20|30".SplitTyped<int>("|");

        // Assert
        result.ShouldBe(new[] { 10, 20, 30 });
    }

    /// <summary>
    /// 测试目的：SplitTyped 对空字符串应返回空数组
    /// </summary>
    [Fact]
    public void SplitTyped_EmptyString_ReturnsEmptyArray()
    {
        // Act
        var result = "".SplitTyped<int>(',');

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试目的：SplitTyped 对纯空白字符串应返回空数组
    /// </summary>
    [Fact]
    public void SplitTyped_WhitespaceString_ReturnsEmptyArray()
    {
        // Act
        var result = "   ".SplitTyped<int>(',');

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试目的：SplitTyped 应跳过连续分隔符产生的空项（RemoveEmptyEntries）
    /// </summary>
    [Fact]
    public void SplitTyped_ConsecutiveDelimiters_SkipsEmpty()
    {
        // Arrange: "1,,3" 中有连续逗号
        var result = "1,,3".SplitTyped<string>(',');

        // Assert: 空项被跳过，只有 "1" 和 "3"
        result.ShouldBe(new[] { "1", "3" });
    }

    /// <summary>
    /// 测试目的：单个元素字符串 SplitTyped 应返回包含该元素的数组
    /// </summary>
    [Fact]
    public void SplitTyped_SingleElement_ReturnsSingleItemArray()
    {
        // Act
        var result = "42".SplitTyped<int>(',');

        // Assert
        result.ShouldBe(new[] { 42 });
    }

    #endregion
}
