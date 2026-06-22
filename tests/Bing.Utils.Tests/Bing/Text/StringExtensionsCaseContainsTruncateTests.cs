using Bing.Text;

namespace Bing.Utils.Tests.Bing.Text;

/// <summary>
/// StringExtensions — Case / ContainsAndWith / Convert(SbcDbc) / Truncate 扩展测试
/// </summary>
[Trait("Bing.Text", "StringExtensionsCaseContainsTruncate")]
public class StringExtensionsCaseContainsTruncateTests
{
    #region ToCapitalCase

    [Fact]
    public void ToCapitalCase_SingleWord_ReturnsCapitalized()
    {
        "hello".ToCapitalCase().ShouldBe("Hello");
    }

    [Fact]
    public void ToCapitalCase_MultipleWords_ReturnsEachCapitalized()
    {
        "hello world".ToCapitalCase().ShouldBe("Hello World");
    }

    [Fact]
    public void ToCapitalCase_AllCaps_RetainsAllCaps()
    {
        // AllCapitals word stays unchanged
        "HELLO world".ToCapitalCase().ShouldBe("HELLO World");
    }

    [Fact]
    public void ToCapitalCase_SingleChar_ReturnsUppercase()
    {
        "a".ToCapitalCase().ShouldBe("A");
    }

    #endregion

    #region ToCamelCase (StringExtensions)

    [Fact]
    public void ToCamelCase_PascalCase_LowercasesFirstChar()
    {
        "UserName".ToCamelCase().ShouldBe("userName");
    }

    [Fact]
    public void ToCamelCase_SingleChar_ReturnsLower()
    {
        "A".ToCamelCase().ShouldBe("a");
    }

    [Fact]
    public void ToCamelCase_AlreadyLower_ReturnsSame()
    {
        "hello".ToCamelCase().ShouldBe("hello");
    }

    [Fact]
    public void ToCamelCase_Empty_ReturnsEmpty()
    {
        "".ToCamelCase().ShouldBe("");
    }

    #endregion

    #region Contains (StringExtensions)

    [Fact]
    public void Contains_StringWithMatch_ReturnsTrue()
    {
        "Hello World".Contains("World", "xyz").ShouldBeTrue();
    }

    [Fact]
    public void Contains_StringNoMatch_ReturnsFalse()
    {
        "Hello World".Contains("abc", "xyz").ShouldBeFalse();
    }

    [Fact]
    public void Contains_NullText_ReturnsFalse()
    {
        ((string)null).Contains("a", "b").ShouldBeFalse();
    }

    [Fact]
    public void Contains_CharMatch_ReturnsTrue()
    {
        "Hello".Contains('e').ShouldBeTrue();
    }

    [Fact]
    public void Contains_CharNoMatch_ReturnsFalse()
    {
        "Hello".Contains('z').ShouldBeFalse();
    }

    [Fact]
    public void Contains_MultipleCharsOneMatch_ReturnsTrue()
    {
        "Hello".Contains('x', 'e').ShouldBeTrue();
    }

    [Fact]
    public void Contains_MultipleCharsNoMatch_ReturnsFalse()
    {
        "Hello".Contains('x', 'y', 'z').ShouldBeFalse();
    }

    [Fact]
    public void ContainsIgnoreCase_String_IgnoresCase()
    {
        "Hello World".ContainsIgnoreCase("hello").ShouldBeTrue();
    }

    [Fact]
    public void ContainsIgnoreCase_StringNotFound_ReturnsFalse()
    {
        "Hello World".ContainsIgnoreCase("xyz").ShouldBeFalse();
    }

    [Fact]
    public void ContainsIgnoreCase_Char_IgnoresCase()
    {
        "Hello".ContainsIgnoreCase('E').ShouldBeTrue();
    }

    [Fact]
    public void ContainsIgnoreCase_Char_NotFound_ReturnsFalse()
    {
        "Hello".ContainsIgnoreCase('x').ShouldBeFalse();
    }

    [Fact]
    public void ContainsIgnoreCase_MultipleChars_IgnoresCase()
    {
        "Hello".ContainsIgnoreCase('E', 'O').ShouldBeTrue();
    }

    [Fact]
    public void Contains_StringArrayWithIgnoreCaseTrue_ReturnsTrue()
    {
        "Hello World".Contains(new[] { "hello" }, IgnoreCase.True).ShouldBeTrue();
    }

    [Fact]
    public void Contains_StringArrayWithIgnoreCaseFalse_ReturnsFalse()
    {
        "Hello World".Contains(new[] { "hello" }, IgnoreCase.False).ShouldBeFalse();
    }

    [Fact]
    public void Contains_CharWithIgnoreCaseTrue_ReturnsTrue()
    {
        "Hello".Contains('E', IgnoreCase.True).ShouldBeTrue();
    }

    [Fact]
    public void Contains_CharWithIgnoreCaseFalse_ReturnsFalse()
    {
        "Hello".Contains('E', IgnoreCase.False).ShouldBeFalse();
    }

    [Fact]
    public void Contains_CharArrayWithIgnoreCaseTrue_ReturnsTrue()
    {
        "Hello".Contains(new[] { 'E', 'O' }, IgnoreCase.True).ShouldBeTrue();
    }

    [Fact]
    public void Contains_CharArrayWithIgnoreCaseFalse_ReturnsFalse()
    {
        "Hello".Contains(new[] { 'E', 'O' }, IgnoreCase.False).ShouldBeFalse();
    }

    #endregion

    #region MatchEmoji

    [Fact]
    public void MatchEmoji_WithEmoji_ReturnsTrue()
    {
        "Hello 😊".MatchEmoji().ShouldBeTrue();
    }

    [Fact]
    public void MatchEmoji_NoEmoji_ReturnsFalse()
    {
        "Hello World".MatchEmoji().ShouldBeFalse();
    }

    #endregion

    #region EndsWith (StringsShortcutExtensions)

    [Fact]
    public void EndsWith_Char_Match_ReturnsTrue()
    {
        "hello".EndsWith('o').ShouldBeTrue();
    }

    [Fact]
    public void EndsWith_Char_NoMatch_ReturnsFalse()
    {
        "hello".EndsWith('x').ShouldBeFalse();
    }

    [Fact]
    public void EndsWith_ParamsStrings_OneMatch_ReturnsTrue()
    {
        "hello.cs".EndsWith(".cs", ".ts").ShouldBeTrue();
    }

    [Fact]
    public void EndsWith_ParamsStrings_NoMatch_ReturnsFalse()
    {
        "hello.cs".EndsWith(".ts", ".js").ShouldBeFalse();
    }

    [Fact]
    public void EndsWithIgnoreCase_IgnoresCase_ReturnsTrue()
    {
        "hello.CS".EndsWithIgnoreCase(".cs").ShouldBeTrue();
    }

    [Fact]
    public void EndsWithIgnoreCase_NotEnding_ReturnsFalse()
    {
        "hello.cs".EndsWithIgnoreCase(".ts").ShouldBeFalse();
    }

    [Fact]
    public void EndsWithIgnoreCase_Params_OneMatch_ReturnsTrue()
    {
        "hello.CS".EndsWithIgnoreCase(".ts", ".cs").ShouldBeTrue();
    }

    #endregion

    #region StartsWith (StringsShortcutExtensions)

    [Fact]
    public void StartsWith_Char_Match_ReturnsTrue()
    {
        "hello".StartsWith('h').ShouldBeTrue();
    }

    [Fact]
    public void StartsWith_Char_NoMatch_ReturnsFalse()
    {
        "hello".StartsWith('x').ShouldBeFalse();
    }

    [Fact]
    public void StartsWith_ParamsStrings_OneMatch_ReturnsTrue()
    {
        "Hello World".StartsWith("Hello", "Bye").ShouldBeTrue();
    }

    [Fact]
    public void StartsWith_ParamsStrings_NoMatch_ReturnsFalse()
    {
        "Hello World".StartsWith("Bye", "See").ShouldBeFalse();
    }

    [Fact]
    public void StartsWithIgnoreCase_IgnoresCase_ReturnsTrue()
    {
        "HELLO world".StartsWithIgnoreCase("hello").ShouldBeTrue();
    }

    [Fact]
    public void StartsWithIgnoreCase_NotStarting_ReturnsFalse()
    {
        "Hello World".StartsWithIgnoreCase("World").ShouldBeFalse();
    }

    [Fact]
    public void StartsWithIgnoreCase_Params_OneMatch_ReturnsTrue()
    {
        "HELLO world".StartsWithIgnoreCase("bye", "hello").ShouldBeTrue();
    }

    #endregion

    #region ToSbcCase / ToDbcCase

    [Fact]
    public void ToSbcCase_AsciiChars_ConvertsToFullWidth()
    {
        var result = "AB".ToSbcCase();
        // 'A'(65) + 65248 = 65313 (full-width A)
        result[0].ShouldBe((char)65313);
        result[1].ShouldBe((char)65314);
    }

    [Fact]
    public void ToSbcCase_Space_ConvertsToFullWidthSpace()
    {
        var result = " ".ToSbcCase();
        result[0].ShouldBe((char)12288);
    }

    [Fact]
    public void ToDbcCase_FullWidthChars_ConvertsToHalfWidth()
    {
        var fullWidth = "Ａ"; // char 65313
        var result = fullWidth.ToDbcCase();
        result[0].ShouldBe('A');
    }

    [Fact]
    public void ToDbcCase_FullWidthSpace_ConvertsToHalfWidthSpace()
    {
        var fullWidthSpace = "\u3000"; // 12288
        var result = fullWidthSpace.ToDbcCase();
        result[0].ShouldBe(' ');
    }

    [Fact]
    public void SbcDbc_RoundTrip_ReturnsOriginal()
    {
        var original = "Hello123";
        var fullWidth = original.ToSbcCase();
        var halfWidth = fullWidth.ToDbcCase();
        halfWidth.ShouldBe(original);
    }

    #endregion

    #region Truncate (Extensions.String.Truncate.cs)

    [Fact]
    public void Truncate_WithinMaxLength_ReturnsOriginal()
    {
        "hi".Truncate(10).ShouldBe("hi");
    }

    [Fact]
    public void Truncate_ExceedsMaxLength_ReturnsWithEllipsis()
    {
        "Hello World".Truncate(8).ShouldBe("Hello...");
    }

    [Fact]
    public void Truncate_MaxLengthLessThanOrEqualThree_ReturnsTwoCharsAndDot()
    {
        "Hello".Truncate(2).ShouldBe("He.");
    }

    [Fact]
    public void Truncate_ZeroMaxLength_ReturnsEmpty()
    {
        "Hello".Truncate(0).ShouldBe(string.Empty);
    }

    [Fact]
    public void Truncate_NullOrEmpty_ReturnsEmpty()
    {
        "".Truncate(5).ShouldBe(string.Empty);
        ((string)null).Truncate(5).ShouldBe(string.Empty);
    }

    #endregion
}
