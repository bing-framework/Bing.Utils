using Shouldly;

namespace Bing.Text;

/// <summary>
/// 测试类：StringLines 多行文本分割、计数与截断工具方法
/// </summary>
[Trait("TextUT", "StringLines")]
public class StringLinesTest
{
    #region SplitByLines

    /// <summary>
    /// 测试目的：SplitByLines 使用 Environment.NewLine 分割多行文本，应正确返回每行内容
    /// </summary>
    [Fact]
    public void SplitByLines_WithMultipleLines_ReturnsEachLine()
    {
        // Arrange — 使用 Environment.NewLine 保证跨平台一致性
        var text = $"line1{Environment.NewLine}line2{Environment.NewLine}line3";

        // Act
        var result = StringLines.SplitByLines(text).ToList();

        // Assert
        result.Count.ShouldBe(3);
        result[0].ShouldBe("line1");
        result[1].ShouldBe("line2");
        result[2].ShouldBe("line3");
    }

    /// <summary>
    /// 测试目的：单行文本（无换行符）调用 SplitByLines 应返回仅含该行的集合
    /// </summary>
    [Fact]
    public void SplitByLines_WithSingleLine_ReturnsSingleElement()
    {
        // Act
        var result = StringLines.SplitByLines("hello world").ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBe("hello world");
    }

    /// <summary>
    /// 测试目的：空字符串调用 SplitByLines 应返回空集合
    /// </summary>
    [Fact]
    public void SplitByLines_WithEmptyString_ReturnsEmptyCollection()
    {
        // Act
        var result = StringLines.SplitByLines(string.Empty).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试目的：文本末尾有换行符时，SplitByLines 不应产生多余的空行
    /// </summary>
    [Fact]
    public void SplitByLines_WithTrailingNewLine_DoesNotProduceExtraEmptyLine()
    {
        // Arrange
        var text = $"line1{Environment.NewLine}line2{Environment.NewLine}";

        // Act
        var result = StringLines.SplitByLines(text).ToList();

        // Assert
        result.Count.ShouldBe(2);
    }

    /// <summary>
    /// 测试目的：SplitByLines 应支持混合使用 CRLF、LF 和 CR 换行符。
    /// </summary>
    [Fact]
    public void SplitByLines_WithMixedLineEndings_ReturnsEachLine()
    {
        var result = StringLines.SplitByLines("line1\r\nline2\nline3\rline4").ToList();

        result.ShouldBe(new[] { "line1", "line2", "line3", "line4" });
    }

    /// <summary>
    /// 测试目的：SplitByLines 遇到 LF 后的内容时不应跳过首字符。
    /// </summary>
    [Fact]
    public void SplitByLines_WithLf_DoesNotSkipCharacterAfterLineBreak()
    {
        var result = StringLines.SplitByLines("line1\nline2").ToList();

        result.ShouldBe(new[] { "line1", "line2" });
    }

    /// <summary>
    /// 测试目的：扩展方法 SplitByLines() 应与静态方法结果一致
    /// </summary>
    [Fact]
    public void SplitByLines_ExtensionMethod_MatchesStaticMethod()
    {
        // Arrange
        var text = $"a{Environment.NewLine}b{Environment.NewLine}c";

        // Act
        var staticResult = StringLines.SplitByLines(text).ToList();
        var extensionResult = text.SplitByLines().ToList();

        // Assert
        extensionResult.ShouldBe(staticResult);
    }

    #endregion

    #region SplitInLinesWithoutEmpty

    /// <summary>
    /// 测试目的：SplitInLinesWithoutEmpty 应过滤空行，只返回有内容的行
    /// </summary>
    [Fact]
    public void SplitInLinesWithoutEmpty_FiltersEmptyLines()
    {
        // Arrange
        var text = $"line1{Environment.NewLine}{Environment.NewLine}line3{Environment.NewLine}";

        // Act
        var result = StringLines.SplitInLinesWithoutEmpty(text);

        // Assert
        result.Length.ShouldBe(2);
        result[0].ShouldBe("line1");
        result[1].ShouldBe("line3");
    }

    #endregion

    #region SplitInLinesTyped<T>

    /// <summary>
    /// 测试目的：SplitInLinesTyped 应将每行转换为目标类型（如 int）
    /// </summary>
    [Fact]
    public void SplitInLinesTyped_WithIntegers_ConvertsEachLine()
    {
        // Arrange
        var text = $"1{Environment.NewLine}2{Environment.NewLine}3";

        // Act
        var result = StringLines.SplitInLinesTyped<int>(text);

        // Assert
        result.ShouldBe(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// 测试目的：SplitInLinesTyped 应识别混合的 CRLF、LF 和 CR 换行符。
    /// </summary>
    [Fact]
    public void SplitInLinesTyped_WithMixedLineEndings_ConvertsEachLine()
    {
        var result = StringLines.SplitInLinesTyped<int>("1\r\n2\n3\r4");

        result.ShouldBe(new[] { 1, 2, 3, 4 });
    }

    /// <summary>
    /// 测试目的：SplitInLinesWithoutEmpty 应在混合换行文本中移除空行并保留其他内容。
    /// </summary>
    [Fact]
    public void SplitInLinesWithoutEmpty_WithMixedLineEndings_RemovesEmptyLines()
    {
        var result = StringLines.SplitInLinesWithoutEmpty("A\r\n\nB\rC\n");

        result.ShouldBe(new[] { "A", "B", "C" });
    }

    #endregion

    #region CountByLines

    /// <summary>
    /// 验证文本行数统计。
    /// </summary>
    /// <param name="lineCount">预期行数。</param>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void CountByLines_ReturnsCorrectLineCount(int lineCount)
    {
        // Arrange
        var lines = Enumerable.Range(1, lineCount).Select(i => $"line{i}");
        var text = string.Join(Environment.NewLine, lines);

        // Act
        var result = StringLines.CountByLines(text);

        // Assert
        result.ShouldBe(lineCount);
    }

    /// <summary>
    /// 测试目的：扩展方法 CountByLines() 应与静态方法结果一致
    /// </summary>
    [Fact]
    public void CountByLines_ExtensionMethod_MatchesStaticMethod()
    {
        // Arrange
        var text = $"a{Environment.NewLine}b{Environment.NewLine}c";

        // Act
        var staticResult = StringLines.CountByLines(text);
        var extensionResult = text.CountByLines();

        // Assert
        extensionResult.ShouldBe(staticResult);
    }

    /// <summary>
    /// 验证行数统计与逐行分割一致。
    /// </summary>
    /// <param name="text">包含混合换行符的文本。</param>
    [Theory]
    [InlineData("line1\r\nline2\nline3\rline4")]
    [InlineData("line1\nline2")]
    [InlineData("line1\r\n")]
    [InlineData("\r\n")]
    public void CountByLines_MatchesSplitByLines(string text)
    {
        var splitLines = StringLines.SplitByLines(text).ToList();

        StringLines.CountByLines(text).ShouldBe(splitLines.Count);
    }

    #endregion
}
