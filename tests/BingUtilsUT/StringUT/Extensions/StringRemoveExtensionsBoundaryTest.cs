using Bing.Text;

namespace BingUtilsUT.StringUT.Extensions;

/// <summary>
/// 测试类：覆盖 StringRemove 扩展方法的边界输入与契约一致性。
/// </summary>
[Trait("StringUT.Extensions", "String.Remove.Boundary")]
public class StringRemoveExtensionsBoundaryTest
{
    /// <summary>
    /// 测试用例：RemoveSince 带大小写选项时，应返回与预期一致的结果。
    /// </summary>
    [Theory]
    [InlineData("Hello World", "world", IgnoreCase.True, "Hello ")]
    [InlineData("Hello World", "world", IgnoreCase.False, "Hello World")]
    [InlineData("Hello World", null, IgnoreCase.True, "Hello World")]
    [InlineData("", "world", IgnoreCase.True, "")]
    [InlineData(null, "world", IgnoreCase.True, null)]
    public void RemoveSince_WithIgnoreCaseOption_ReturnsExpected(
        string text,
        string removeFromThis,
        IgnoreCase @case,
        string expected)
    {
        var result = text.RemoveSince(removeFromThis, @case);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：RemoveFromIgnoreCase 的行为应等价于 RemoveSinceIgnoreCase。
    /// </summary>
    [Theory]
    [InlineData("HelloWorld", "o")]
    [InlineData("HelloWorld", "O")]
    [InlineData("HelloWorld", "x")]
    [InlineData("", "x")]
    [InlineData(null, "x")]
    public void RemoveFromIgnoreCase_AnyInput_IsConsistentWithRemoveSinceIgnoreCase(string text, string marker)
    {
        var fromIgnoreCase = text.RemoveFromIgnoreCase(marker);
        var sinceIgnoreCase = text.RemoveSinceIgnoreCase(marker);

        fromIgnoreCase.ShouldBe(sinceIgnoreCase);
    }

    /// <summary>
    /// 测试用例：RemoveStart 在 null/空白/大小写不匹配时，应保持约定行为。
    /// </summary>
    [Theory]
    [InlineData(null, "a", "")]
    [InlineData("", "a", "")]
    [InlineData("   ", "a", "")]
    [InlineData("abc", "a", "bc")]
    [InlineData("abc", "A", "abc")]
    public void RemoveStart_BoundaryInput_ReturnsExpected(string value, string start, string expected)
    {
        var result = value.RemoveStart(start);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：RemoveEnd 在 null/空白/大小写不匹配时，应保持约定行为。
    /// </summary>
    [Theory]
    [InlineData(null, "a", "")]
    [InlineData("", "a", "")]
    [InlineData("   ", "a", "")]
    [InlineData("abc", "c", "ab")]
    [InlineData("abc", "C", "abc")]
    public void RemoveEnd_BoundaryInput_ReturnsExpected(string value, string end, string expected)
    {
        var result = value.RemoveEnd(end);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：RemoveDuplicateSpaces 对混合空白应统一压缩为单个空格。
    /// </summary>
    [Fact]
    public void RemoveDuplicateSpaces_MixedWhitespaces_NormalizesToSingleSpace()
    {
        var result = "A\t \r\n  B   C".RemoveDuplicateSpaces();

        result.ShouldBe("A B C");
    }

    /// <summary>
    /// 测试用例：RemoveAccentsIgnoreCase 不处理 ñ，而 RemoveAccentsIgnoreCaseAndN 会处理。
    /// </summary>
    [Theory]
    [InlineData("niño", "niño", "nino")]
    [InlineData("SEÑOR", "SEÑOR", "SENOR")]
    public void RemoveAccents_MethodFamily_HasExpectedDifferenceForEnye(
        string input,
        string expectedIgnoreCase,
        string expectedIgnoreCaseAndN)
    {
        input.RemoveAccentsIgnoreCase().ShouldBe(expectedIgnoreCase);
        input.RemoveAccentsIgnoreCaseAndN().ShouldBe(expectedIgnoreCaseAndN);
    }

    /// <summary>
    /// 测试用例：RemoveChars 传入 null 字符数组时，应返回原字符串。
    /// </summary>
    [Fact]
    public void RemoveChars_NullCharsArray_ReturnsOriginalString()
    {
        var result = StringExtensions.RemoveChars("abc", (char[])null);

        result.ShouldBe("abc");
    }
}
