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

    [Fact]
    public void SubString_NullStringBuilder_ThrowsArgumentNullException()
    {
        StringBuilder sb = null!;
        Should.Throw<ArgumentNullException>(() => sb.SubString(0, 3));
    }

    [Fact]
    public void SubString_NormalRange_ReturnsSubstring()
    {
        var sb = new StringBuilder("Hello, World!");
        sb.SubString(7, 5).ShouldBe("World");
    }

    [Fact]
    public void SubString_StartAtZero_ReturnsPrefix()
    {
        var sb = new StringBuilder("ABCDEF");
        sb.SubString(0, 3).ShouldBe("ABC");
    }

    [Fact]
    public void SubString_FullLength_ReturnsEntireString()
    {
        var sb = new StringBuilder("Test");
        sb.SubString(0, 4).ShouldBe("Test");
    }

    [Fact]
    public void SubString_ExceedsLength_ThrowsIndexOutOfRangeException()
    {
        var sb = new StringBuilder("Hi");
        Should.Throw<IndexOutOfRangeException>(() => sb.SubString(0, 10));
    }

    [Fact]
    public void SubString_LengthZero_ReturnsEmptyString()
    {
        var sb = new StringBuilder("Test");
        sb.SubString(2, 0).ShouldBe(string.Empty);
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimStart (空格)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TrimStart_Default_RemovesLeadingSpaces()
    {
        var sb = new StringBuilder("   Hello");
        sb.TrimStart().ToString().ShouldBe("Hello");
    }

    [Fact]
    public void TrimStart_Default_NoLeadingSpaces_Unchanged()
    {
        var sb = new StringBuilder("Hello   ");
        sb.TrimStart().ToString().ShouldBe("Hello   ");
    }

    [Fact]
    public void TrimStart_Default_EmptyStringBuilder_Unchanged()
    {
        var sb = new StringBuilder(string.Empty);
        sb.TrimStart().ToString().ShouldBe(string.Empty);
    }

    [Fact]
    public void TrimStart_Null_ThrowsArgumentNullException()
    {
        StringBuilder sb = null!;
        Should.Throw<ArgumentNullException>(() => sb.TrimStart());
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimStart (char)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TrimStart_Char_RemovesLeadingCharacter()
    {
        var sb = new StringBuilder("***Hello***");
        sb.TrimStart('*').ToString().ShouldBe("Hello***");
    }

    [Fact]
    public void TrimStart_Char_EmptyStringBuilder_Unchanged()
    {
        var sb = new StringBuilder(string.Empty);
        sb.TrimStart('x').ToString().ShouldBe(string.Empty);
    }

    [Fact]
    public void TrimStart_Char_NoMatchAtStart_Unchanged()
    {
        var sb = new StringBuilder("Hello***");
        sb.TrimStart('*').ToString().ShouldBe("Hello***");
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimStart (char[])
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TrimStart_CharArray_RemovesMatchingLeadingChars()
    {
        var sb = new StringBuilder("---Hello---");
        sb.TrimStart(new[] { '-' }).ToString().ShouldBe("Hello---");
    }

    [Fact]
    public void TrimStart_CharArray_Null_ThrowsArgumentNullException()
    {
        var sb = new StringBuilder("Hello");
        Should.Throw<ArgumentNullException>(() => sb.TrimStart((char[])null!));
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimStart (string)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TrimStart_String_RemovesLeadingOccurrences()
    {
        var sb = new StringBuilder("abcabcHello");
        sb.TrimStart("abc").ToString().ShouldBe("Hello");
    }

    [Fact]
    public void TrimStart_String_NullStr_Unchanged()
    {
        var sb = new StringBuilder("Hello");
        sb.TrimStart((string)null!).ToString().ShouldBe("Hello");
    }

    [Fact]
    public void TrimStart_String_EmptyStr_Unchanged()
    {
        var sb = new StringBuilder("Hello");
        sb.TrimStart(string.Empty).ToString().ShouldBe("Hello");
    }

    [Fact]
    public void TrimStart_String_LongerThanSb_Unchanged()
    {
        var sb = new StringBuilder("Hi");
        sb.TrimStart("Hello World").ToString().ShouldBe("Hi");
    }

    [Fact]
    public void TrimStart_String_NoMatch_Unchanged()
    {
        var sb = new StringBuilder("World");
        sb.TrimStart("abc").ToString().ShouldBe("World");
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimEnd (空格)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TrimEnd_Default_RemovesTrailingSpaces()
    {
        var sb = new StringBuilder("Hello   ");
        sb.TrimEnd().ToString().ShouldBe("Hello");
    }

    [Fact]
    public void TrimEnd_Default_NoTrailingSpaces_Unchanged()
    {
        var sb = new StringBuilder("   Hello");
        sb.TrimEnd().ToString().ShouldBe("   Hello");
    }

    [Fact]
    public void TrimEnd_Default_EmptyStringBuilder_Unchanged()
    {
        var sb = new StringBuilder(string.Empty);
        sb.TrimEnd().ToString().ShouldBe(string.Empty);
    }

    [Fact]
    public void TrimEnd_Null_ThrowsArgumentNullException()
    {
        StringBuilder sb = null!;
        Should.Throw<ArgumentNullException>(() => sb.TrimEnd());
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimEnd (char)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TrimEnd_Char_RemovesTrailingCharacter()
    {
        var sb = new StringBuilder("***Hello***");
        sb.TrimEnd('*').ToString().ShouldBe("***Hello");
    }

    [Fact]
    public void TrimEnd_Char_EmptyStringBuilder_Unchanged()
    {
        var sb = new StringBuilder(string.Empty);
        sb.TrimEnd('x').ToString().ShouldBe(string.Empty);
    }

    [Fact]
    public void TrimEnd_Char_NoMatchAtEnd_Unchanged()
    {
        var sb = new StringBuilder("***Hello");
        sb.TrimEnd('*').ToString().ShouldBe("***Hello");
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimEnd (char[])
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TrimEnd_CharArray_RemovesMatchingTrailingChars()
    {
        var sb = new StringBuilder("Hello---");
        sb.TrimEnd(new[] { '-' }).ToString().ShouldBe("Hello");
    }

    [Fact]
    public void TrimEnd_CharArray_Null_ThrowsArgumentNullException()
    {
        var sb = new StringBuilder("Hello");
        Should.Throw<ArgumentNullException>(() => sb.TrimEnd((char[])null!));
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimEnd (string)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TrimEnd_String_RemovesTrailingOccurrences()
    {
        var sb = new StringBuilder("Helloabcabc");
        sb.TrimEnd("abc").ToString().ShouldBe("Hello");
    }

    [Fact]
    public void TrimEnd_String_NullStr_Unchanged()
    {
        var sb = new StringBuilder("Hello");
        sb.TrimEnd((string)null!).ToString().ShouldBe("Hello");
    }

    [Fact]
    public void TrimEnd_String_EmptyStr_Unchanged()
    {
        var sb = new StringBuilder("Hello");
        sb.TrimEnd(string.Empty).ToString().ShouldBe("Hello");
    }

    [Fact]
    public void TrimEnd_String_LongerThanSb_Unchanged()
    {
        var sb = new StringBuilder("Hi");
        sb.TrimEnd("Hello World").ToString().ShouldBe("Hi");
    }

    [Fact]
    public void TrimEnd_String_NoMatch_Unchanged()
    {
        var sb = new StringBuilder("World");
        sb.TrimEnd("abc").ToString().ShouldBe("World");
    }

    // ════════════════════════════════════════════════════════════════
    //  Trim (去除两端空格)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Trim_RemovesBothEnds()
    {
        var sb = new StringBuilder("   Hello   ");
        sb.Trim().ToString().ShouldBe("Hello");
    }

    [Fact]
    public void Trim_EmptyStringBuilder_Unchanged()
    {
        var sb = new StringBuilder(string.Empty);
        sb.Trim().ToString().ShouldBe(string.Empty);
    }

    [Fact]
    public void Trim_OnlySpaces_ReturnsEmptyString()
    {
        var sb = new StringBuilder("   ");
        sb.Trim().ToString().ShouldBe(string.Empty);
    }

    [Fact]
    public void Trim_NoWhitespace_Unchanged()
    {
        var sb = new StringBuilder("NoSpaces");
        sb.Trim().ToString().ShouldBe("NoSpaces");
    }

    [Fact]
    public void Trim_NullStringBuilder_ThrowsArgumentNullException()
    {
        StringBuilder sb = null!;
        Should.Throw<ArgumentNullException>(() => sb.Trim());
    }

    // ════════════════════════════════════════════════════════════════
    //  综合：TrimStart + TrimEnd 组合
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TrimStartAndEnd_String_RemovesBothEndsCorrectly()
    {
        var sb = new StringBuilder("--Hello--");
        sb.TrimStart('-').TrimEnd('-').ToString().ShouldBe("Hello");
    }

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
