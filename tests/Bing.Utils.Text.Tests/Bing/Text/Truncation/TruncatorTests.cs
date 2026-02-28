using Bing.Text.Truncation;
using Shouldly;

namespace Bing.Text;

/// <summary>
/// 测试类：StringTruncators 的各截断策略（ByLength / ByNumberOfCharacters / ByNumberOfWords / ByNumberOfLines）
/// 及 StringTruncateExtensions 扩展方法
/// </summary>
[Trait("TextUT", "Truncator")]
public class TruncatorTests
{
    #region ByLength (FixedLengthTruncator)

    /// <summary>
    /// 测试目的：ByLength 对超长字符串截断后应以截断字符串结尾且总长度等于 maxLength
    /// </summary>
    [Theory]
    [InlineData("hello world", 8, "hello...")]
    [InlineData("abcdefgh", 5, "ab...")]
    [InlineData("short", 10, "short")]   // 不超长，原样返回
    public void ByLength_TruncatesFromRightWithEllipsis(string text, int maxLength, string expected)
    {
        // Act
        var result = StringTruncators.ByLength.Truncate(text, maxLength);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：ByLength maxLength 为负数时应返回空字符串（非直觉行为，文档规范）
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ByLength_WithNegativeMaxLength_ReturnsEmptyString(int maxLength)
    {
        // Act
        var result = StringTruncators.ByLength.Truncate("hello world", maxLength);

        // Assert
        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试目的：ByLength maxLength 为 0 时应原样返回文本
    /// </summary>
    [Fact]
    public void ByLength_WithZeroMaxLength_ReturnsOriginalText()
    {
        // Act
        var result = StringTruncators.ByLength.Truncate("hello", 0);

        // Assert
        result.ShouldBe("hello");
    }

    /// <summary>
    /// 测试目的：ByLength 从左侧截断（TruncateFrom.Left）时，结果应以截断符开头
    /// </summary>
    [Fact]
    public void ByLength_TruncateFromLeft_StartsWithEllipsis()
    {
        // Act
        var result = StringTruncators.ByLength.Truncate(
            "hello world", 8, "...", ".", StringTruncateFrom.Left);

        // Assert
        result.ShouldStartWith("...");
        result.Length.ShouldBe(8);
    }

    /// <summary>
    /// 测试目的：文本为 null 或空时应返回空字符串
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ByLength_WithNullOrEmptyText_ReturnsEmptyString(string text)
    {
        // Act
        var result = StringTruncators.ByLength.Truncate(text!, 10);

        // Assert
        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试目的：扩展方法 Truncate() 默认使用 ByLength 截断器
    /// </summary>
    [Fact]
    public void StringExtension_Truncate_UsesLengthTruncatorByDefault()
    {
        // Act
        var extensionResult = "hello world".Truncate(8);
        var directResult = StringTruncators.ByLength.Truncate("hello world", 8);

        // Assert
        extensionResult.ShouldBe(directResult);
    }

    #endregion

    #region ByNumberOfWords (FixedNumberOfWordsTruncator)

    /// <summary>
    /// 测试目的：ByNumberOfWords 按单词数截断，超出词数时以截断符结尾
    /// 注：当 shortTruncationString.Length &lt; maxLength &amp;&amp; truncationString.Length &gt; maxLength 时，实现选用 shortTruncationString
    /// </summary>
    [Theory]
    [InlineData("one two three four", 2, "one two.")]  // maxWords=2: short="."(1) < 2 && full="..."(3) > 2 → 用 "."
    [InlineData("hello world", 1, "hello...")]        // maxWords=1: short="."(1) == 1 → 不满足 < 1, 用 "..."
    [InlineData("hello", 5, "hello")]                 // 不超过单词数，原样返回
    public void ByNumberOfWords_TruncatesAtWordBoundary(string text, int maxWords, string expected)
    {
        // Act
        var result = StringTruncators.ByNumberOfWords.Truncate(text, maxWords);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：ByNumberOfWords maxLength 为负数时应返回空字符串
    /// </summary>
    [Fact]
    public void ByNumberOfWords_WithNegativeMaxLength_ReturnsEmptyString()
    {
        // Act
        var result = StringTruncators.ByNumberOfWords.Truncate("hello world", -1);

        // Assert
        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试目的：ByNumberOfWords maxLength 为 0 时原样返回文本
    /// </summary>
    [Fact]
    public void ByNumberOfWords_WithZeroMaxLength_ReturnsOriginalText()
    {
        // Act
        var result = StringTruncators.ByNumberOfWords.Truncate("hello world", 0);

        // Assert
        result.ShouldBe("hello world");
    }

    /// <summary>
    /// 测试目的：ByNumberOfWords 从左侧截断，结果应以截断符开头
    /// 注：maxLength=3 时 truncationString.Length(3) == maxLength，不满足 "short < max && full > max" 条件，故用完整 "..."
    /// </summary>
    [Fact]
    public void ByNumberOfWords_TruncateFromLeft_StartsWithTruncationString()
    {
        // Act
        var result = StringTruncators.ByNumberOfWords.Truncate(
            "one two three four", 3, "...", ".", StringTruncateFrom.Left);

        // Assert
        result.ShouldStartWith("...");
        result.ShouldEndWith("four");
    }

    #endregion

    #region ByNumberOfLines (FixedNumberOfLinesTruncator)

    /// <summary>
    /// 测试目的： ByNumberOfLines 按行数截断，实现在 counter==maxLength 时替换为截断符，保留前 maxLength-1 行
    /// </summary>
    [Fact]
    public void ByNumberOfLines_TruncatesAtLineBoundary()
    {
        // Arrange — 使用 Environment.NewLine 保证跨平台一致性
        var text = $"line1{Environment.NewLine}line2{Environment.NewLine}line3{Environment.NewLine}line4";

        // Act — maxLength=3: 循环到 counter==3 时输出截断符，实际保留 2 行 (line1, line2)
        var result = StringTruncators.ByNumberOfLines.Truncate(text, 3);

        // Assert
        result.ShouldContain("line1");
        result.ShouldContain("line2");
        result.ShouldNotContain("line3");
    }

    /// <summary>
    /// 测试目的：ByNumberOfLines 文本行数未超过 maxLength 时原样返回
    /// </summary>
    [Fact]
    public void ByNumberOfLines_WithTextUnderLimit_ReturnsOriginal()
    {
        // Arrange
        var text = $"line1{Environment.NewLine}line2";

        // Act
        var result = StringTruncators.ByNumberOfLines.Truncate(text, 5);

        // Assert
        result.ShouldBe(text);
    }

    #endregion

    #region ByNumberOfCharacters (FixedNumberOfCharactersTruncator)

    /// <summary>
    /// 测试目的：ByNumberOfCharacters 按字符数（Unicode 字符）截断
    /// </summary>
    [Theory]
    [InlineData("hello world", 5, "he...")]
    [InlineData("abc", 10, "abc")]  // 不超长，原样返回
    public void ByNumberOfCharacters_TruncatesAtCharBoundary(string text, int maxChars, string expected)
    {
        // Act
        var result = StringTruncators.ByNumberOfCharacters.Truncate(text, maxChars);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region 扩展方法 Truncate 重载

    /// <summary>
    /// 测试目的：带自定义截断器的扩展方法 Truncate 应使用指定截断器
    /// </summary>
    [Fact]
    public void StringExtension_Truncate_WithCustomTruncator_UsesProvidedTruncator()
    {
        // Act
        var byWordsResult = "one two three".Truncate(1, StringTruncators.ByNumberOfWords);
        var byLengthResult = "one two three".Truncate(5, StringTruncators.ByLength);

        // Assert
        byWordsResult.ShouldContain("one");
        byLengthResult.ShouldStartWith("on");
    }

    /// <summary>
    /// 测试目的：完整参数的扩展方法 Truncate 应正确传递所有参数
    /// </summary>
    [Fact]
    public void StringExtension_Truncate_WithFullParams_PassesAllParameters()
    {
        // Act
        var result = "hello world".Truncate(
            8, "---", "-", StringTruncators.ByLength, StringTruncateFrom.Right);

        // Assert
        result.ShouldEndWith("---");
        result.Length.ShouldBe(8);
    }

    #endregion
}
