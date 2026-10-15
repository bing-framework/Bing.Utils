using System.Text;
using Shouldly;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Bing.Text;

// ─────────────────────────────────────────────────────────────────────────────
//  StringBuilderExtensions Tests
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="StringBuilderExtensions"/> — SubString / TrimStart / TrimEnd / Trim
/// </summary>
public class StringBuilderExtensionsTests
{
    // ════════════════════════════════════════════════════════════════
    //  SubString
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 验证 StringBuilder 为空时截取操作抛出参数空异常。
    /// </summary>
    [Fact]
    public void SubString_NullStringBuilder_ThrowsArgumentNullException()
    {
        StringBuilder sb = null!;
        Should.Throw<ArgumentNullException>(() => sb.SubString(0, 3));
    }

    /// <summary>
    /// 验证按指定起点和长度截取子字符串。
    /// </summary>
    [Fact]
    public void SubString_NormalRange_ReturnsSubstring()
    {
        var sb = new StringBuilder("Hello, World!");
        sb.SubString(7, 5).ShouldBe("World");
    }

    /// <summary>
    /// 验证从起始位置截取会返回字符串前缀。
    /// </summary>
    [Fact]
    public void SubString_StartAtZero_ReturnsPrefix()
    {
        var sb = new StringBuilder("ABCDEF");
        sb.SubString(0, 3).ShouldBe("ABC");
    }

    /// <summary>
    /// 验证截取范围覆盖全部内容时返回原字符串。
    /// </summary>
    [Fact]
    public void SubString_FullLength_ReturnsEntireString()
    {
        var sb = new StringBuilder("Test");
        sb.SubString(0, 4).ShouldBe("Test");
    }

    /// <summary>
    /// 验证截取范围超出内容长度时抛出索引越界异常。
    /// </summary>
    [Fact]
    public void SubString_ExceedsLength_ThrowsIndexOutOfRangeException()
    {
        var sb = new StringBuilder("Hi");
        Should.Throw<IndexOutOfRangeException>(() => sb.SubString(0, 10));
    }

    /// <summary>
    /// 验证截取长度为零时返回空字符串。
    /// </summary>
    [Fact]
    public void SubString_LengthZero_ReturnsEmptyString()
    {
        var sb = new StringBuilder("Test");
        sb.SubString(2, 0).ShouldBe(string.Empty);
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimStart (空格)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 验证默认 TrimStart 会移除前导空格。
    /// </summary>
    [Fact]
    public void TrimStart_Default_RemovesLeadingSpaces()
    {
        var sb = new StringBuilder("   Hello");
        sb.TrimStart().ToString().ShouldBe("Hello");
    }

    /// <summary>
    /// 验证没有前导空格时默认 TrimStart 不修改内容。
    /// </summary>
    [Fact]
    public void TrimStart_Default_NoLeadingSpaces_Unchanged()
    {
        var sb = new StringBuilder("Hello   ");
        sb.TrimStart().ToString().ShouldBe("Hello   ");
    }

    /// <summary>
    /// 验证空 StringBuilder 调用默认 TrimStart 后仍为空。
    /// </summary>
    [Fact]
    public void TrimStart_Default_EmptyStringBuilder_Unchanged()
    {
        var sb = new StringBuilder(string.Empty);
        sb.TrimStart().ToString().ShouldBe(string.Empty);
    }

    /// <summary>
    /// 验证 StringBuilder 为空时 TrimStart 抛出参数空异常。
    /// </summary>
    [Fact]
    public void TrimStart_Null_ThrowsArgumentNullException()
    {
        StringBuilder sb = null!;
        Should.Throw<ArgumentNullException>(() => sb.TrimStart());
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimStart (char)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 验证 TrimStart 会移除指定的连续前导字符。
    /// </summary>
    [Fact]
    public void TrimStart_Char_RemovesLeadingCharacter()
    {
        var sb = new StringBuilder("***Hello***");
        sb.TrimStart('*').ToString().ShouldBe("Hello***");
    }

    /// <summary>
    /// 验证空 StringBuilder 调用字符版 TrimStart 后仍为空。
    /// </summary>
    [Fact]
    public void TrimStart_Char_EmptyStringBuilder_Unchanged()
    {
        var sb = new StringBuilder(string.Empty);
        sb.TrimStart('x').ToString().ShouldBe(string.Empty);
    }

    /// <summary>
    /// 验证首字符不匹配时字符版 TrimStart 不修改内容。
    /// </summary>
    [Fact]
    public void TrimStart_Char_NoMatchAtStart_Unchanged()
    {
        var sb = new StringBuilder("Hello***");
        sb.TrimStart('*').ToString().ShouldBe("Hello***");
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimStart (char[])
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 验证字符数组版 TrimStart 会移除匹配的前导字符。
    /// </summary>
    [Fact]
    public void TrimStart_CharArray_RemovesMatchingLeadingChars()
    {
        var sb = new StringBuilder("---Hello---");
        sb.TrimStart(new[] { '-' }).ToString().ShouldBe("Hello---");
    }

    /// <summary>
    /// 验证字符数组为空时 TrimStart 抛出参数空异常。
    /// </summary>
    [Fact]
    public void TrimStart_CharArray_Null_ThrowsArgumentNullException()
    {
        var sb = new StringBuilder("Hello");
        Should.Throw<ArgumentNullException>(() => sb.TrimStart((char[])null!));
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimStart (string)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 验证字符串版 TrimStart 会移除重复的前导片段。
    /// </summary>
    [Fact]
    public void TrimStart_String_RemovesLeadingOccurrences()
    {
        var sb = new StringBuilder("abcabcHello");
        sb.TrimStart("abc").ToString().ShouldBe("Hello");
    }

    /// <summary>
    /// 验证移除片段为空时字符串版 TrimStart 不修改内容。
    /// </summary>
    [Fact]
    public void TrimStart_String_NullStr_Unchanged()
    {
        var sb = new StringBuilder("Hello");
        sb.TrimStart((string)null!).ToString().ShouldBe("Hello");
    }

    /// <summary>
    /// 验证移除片段为空字符串时 TrimStart 不修改内容。
    /// </summary>
    [Fact]
    public void TrimStart_String_EmptyStr_Unchanged()
    {
        var sb = new StringBuilder("Hello");
        sb.TrimStart(string.Empty).ToString().ShouldBe("Hello");
    }

    /// <summary>
    /// 验证移除片段长于内容时 TrimStart 不修改内容。
    /// </summary>
    [Fact]
    public void TrimStart_String_LongerThanSb_Unchanged()
    {
        var sb = new StringBuilder("Hi");
        sb.TrimStart("Hello World").ToString().ShouldBe("Hi");
    }

    /// <summary>
    /// 验证前导内容不匹配时字符串版 TrimStart 不修改内容。
    /// </summary>
    [Fact]
    public void TrimStart_String_NoMatch_Unchanged()
    {
        var sb = new StringBuilder("World");
        sb.TrimStart("abc").ToString().ShouldBe("World");
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimEnd (空格)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 验证默认 TrimEnd 会移除尾随空格。
    /// </summary>
    [Fact]
    public void TrimEnd_Default_RemovesTrailingSpaces()
    {
        var sb = new StringBuilder("Hello   ");
        sb.TrimEnd().ToString().ShouldBe("Hello");
    }

    /// <summary>
    /// 验证没有尾随空格时默认 TrimEnd 不修改内容。
    /// </summary>
    [Fact]
    public void TrimEnd_Default_NoTrailingSpaces_Unchanged()
    {
        var sb = new StringBuilder("   Hello");
        sb.TrimEnd().ToString().ShouldBe("   Hello");
    }

    /// <summary>
    /// 验证空 StringBuilder 调用默认 TrimEnd 后仍为空。
    /// </summary>
    [Fact]
    public void TrimEnd_Default_EmptyStringBuilder_Unchanged()
    {
        var sb = new StringBuilder(string.Empty);
        sb.TrimEnd().ToString().ShouldBe(string.Empty);
    }

    /// <summary>
    /// 验证 StringBuilder 为空时 TrimEnd 抛出参数空异常。
    /// </summary>
    [Fact]
    public void TrimEnd_Null_ThrowsArgumentNullException()
    {
        StringBuilder sb = null!;
        Should.Throw<ArgumentNullException>(() => sb.TrimEnd());
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimEnd (char)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 验证 TrimEnd 会移除指定的连续尾随字符。
    /// </summary>
    [Fact]
    public void TrimEnd_Char_RemovesTrailingCharacter()
    {
        var sb = new StringBuilder("***Hello***");
        sb.TrimEnd('*').ToString().ShouldBe("***Hello");
    }

    /// <summary>
    /// 验证空 StringBuilder 调用字符版 TrimEnd 后仍为空。
    /// </summary>
    [Fact]
    public void TrimEnd_Char_EmptyStringBuilder_Unchanged()
    {
        var sb = new StringBuilder(string.Empty);
        sb.TrimEnd('x').ToString().ShouldBe(string.Empty);
    }

    /// <summary>
    /// 验证末尾字符不匹配时字符版 TrimEnd 不修改内容。
    /// </summary>
    [Fact]
    public void TrimEnd_Char_NoMatchAtEnd_Unchanged()
    {
        var sb = new StringBuilder("***Hello");
        sb.TrimEnd('*').ToString().ShouldBe("***Hello");
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimEnd (char[])
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 验证字符数组版 TrimEnd 会移除匹配的尾随字符。
    /// </summary>
    [Fact]
    public void TrimEnd_CharArray_RemovesMatchingTrailingChars()
    {
        var sb = new StringBuilder("Hello---");
        sb.TrimEnd(new[] { '-' }).ToString().ShouldBe("Hello");
    }

    /// <summary>
    /// 验证字符数组为空时 TrimEnd 抛出参数空异常。
    /// </summary>
    [Fact]
    public void TrimEnd_CharArray_Null_ThrowsArgumentNullException()
    {
        var sb = new StringBuilder("Hello");
        Should.Throw<ArgumentNullException>(() => sb.TrimEnd((char[])null!));
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimEnd (string)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 验证字符串版 TrimEnd 会移除重复的尾随片段。
    /// </summary>
    [Fact]
    public void TrimEnd_String_RemovesTrailingOccurrences()
    {
        var sb = new StringBuilder("Helloabcabc");
        sb.TrimEnd("abc").ToString().ShouldBe("Hello");
    }

    /// <summary>
    /// 验证移除片段为空时字符串版 TrimEnd 不修改内容。
    /// </summary>
    [Fact]
    public void TrimEnd_String_NullStr_Unchanged()
    {
        var sb = new StringBuilder("Hello");
        sb.TrimEnd((string)null!).ToString().ShouldBe("Hello");
    }

    /// <summary>
    /// 验证移除片段为空字符串时 TrimEnd 不修改内容。
    /// </summary>
    [Fact]
    public void TrimEnd_String_EmptyStr_Unchanged()
    {
        var sb = new StringBuilder("Hello");
        sb.TrimEnd(string.Empty).ToString().ShouldBe("Hello");
    }

    /// <summary>
    /// 验证移除片段长于内容时 TrimEnd 不修改内容。
    /// </summary>
    [Fact]
    public void TrimEnd_String_LongerThanSb_Unchanged()
    {
        var sb = new StringBuilder("Hi");
        sb.TrimEnd("Hello World").ToString().ShouldBe("Hi");
    }

    /// <summary>
    /// 验证尾随内容不匹配时字符串版 TrimEnd 不修改内容。
    /// </summary>
    [Fact]
    public void TrimEnd_String_NoMatch_Unchanged()
    {
        var sb = new StringBuilder("World");
        sb.TrimEnd("abc").ToString().ShouldBe("World");
    }

    // ════════════════════════════════════════════════════════════════
    //  Trim (去除两端空格)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 验证 Trim 会同时移除两端空格。
    /// </summary>
    [Fact]
    public void Trim_RemovesBothEnds()
    {
        var sb = new StringBuilder("   Hello   ");
        sb.Trim().ToString().ShouldBe("Hello");
    }

    /// <summary>
    /// 验证空 StringBuilder 调用 Trim 后仍为空。
    /// </summary>
    [Fact]
    public void Trim_EmptyStringBuilder_Unchanged()
    {
        var sb = new StringBuilder(string.Empty);
        sb.Trim().ToString().ShouldBe(string.Empty);
    }

    /// <summary>
    /// 验证仅含空格的内容调用 Trim 后返回空字符串。
    /// </summary>
    [Fact]
    public void Trim_OnlySpaces_ReturnsEmptyString()
    {
        var sb = new StringBuilder("   ");
        sb.Trim().ToString().ShouldBe(string.Empty);
    }

    /// <summary>
    /// 验证不含空白字符时 Trim 不修改内容。
    /// </summary>
    [Fact]
    public void Trim_NoWhitespace_Unchanged()
    {
        var sb = new StringBuilder("NoSpaces");
        sb.Trim().ToString().ShouldBe("NoSpaces");
    }

    /// <summary>
    /// 验证 StringBuilder 为空时 Trim 抛出参数空异常。
    /// </summary>
    [Fact]
    public void Trim_NullStringBuilder_ThrowsArgumentNullException()
    {
        StringBuilder sb = null!;
        Should.Throw<ArgumentNullException>(() => sb.Trim());
    }

    // ════════════════════════════════════════════════════════════════
    //  综合：TrimStart + TrimEnd 组合
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 验证依次移除指定前后缀后保留中间内容。
    /// </summary>
    [Fact]
    public void TrimStartAndEnd_String_RemovesBothEndsCorrectly()
    {
        var sb = new StringBuilder("--Hello--");
        sb.TrimStart('-').TrimEnd('-').ToString().ShouldBe("Hello");
    }

    /// <summary>
    /// 验证不同输入调用 Trim 后得到预期结果。
    /// </summary>
    /// <param name="input">待处理的字符串。</param>
    /// <param name="expected">预期的修剪结果。</param>
    [Theory]
    [InlineData("  abc  ", "abc")]
    [InlineData("abc  ", "abc")]
    [InlineData("  abc", "abc")]
    [InlineData("abc", "abc")]
    [InlineData("", "")]
    public void Trim_Theory_VariousInputs(string input, string expected)
    {
        var sb = new StringBuilder(input);
        sb.Trim().ToString().ShouldBe(expected);
    }
}
