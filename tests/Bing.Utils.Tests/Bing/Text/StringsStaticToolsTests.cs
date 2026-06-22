using Bing.Text;

namespace Bing.Utils.Tests.Bing.Text;

/// <summary>
/// Strings 静态工具类测试 — ContainsAndWith / Is / Has / Substring / Replace
/// </summary>
[Trait("Bing.Text", "StringsStaticTools")]
public class StringsStaticToolsTests
{
    #region Strings.Contains

    [Fact]
    public void Strings_Contains_String_Found_ReturnsTrue()
    {
        Strings.Contains("Hello World", "World", "xyz").ShouldBeTrue();
    }

    [Fact]
    public void Strings_Contains_String_NullText_ReturnsFalse()
    {
        Strings.Contains(null, "hello").ShouldBeFalse();
    }

    [Fact]
    public void Strings_Contains_String_NotFound_ReturnsFalse()
    {
        Strings.Contains("Hello World", "abc", "xyz").ShouldBeFalse();
    }

    [Fact]
    public void Strings_Contains_Char_Found_ReturnsTrue()
    {
        Strings.Contains("Hello", 'e').ShouldBeTrue();
    }

    [Fact]
    public void Strings_Contains_Char_NotFound_ReturnsFalse()
    {
        Strings.Contains("Hello", 'z').ShouldBeFalse();
    }

    [Fact]
    public void Strings_Contains_MultipleChars_OneFound_ReturnsTrue()
    {
        Strings.Contains("Hello", 'x', 'e').ShouldBeTrue();
    }

    [Fact]
    public void Strings_ContainsIgnoreCase_String_Found_ReturnsTrue()
    {
        Strings.ContainsIgnoreCase("Hello World", "hello").ShouldBeTrue();
    }

    [Fact]
    public void Strings_ContainsIgnoreCase_Char_Found_ReturnsTrue()
    {
        Strings.ContainsIgnoreCase("Hello", 'E').ShouldBeTrue();
    }

    [Fact]
    public void Strings_ContainsIgnoreCase_MultipleChars_Found_ReturnsTrue()
    {
        Strings.ContainsIgnoreCase("Hello", 'E', 'O').ShouldBeTrue();
    }

    [Fact]
    public void Strings_Contains_WithIgnoreCaseTrue_ReturnsTrue()
    {
        Strings.Contains("Hello World", new[] { "hello" }, IgnoreCase.True).ShouldBeTrue();
    }

    [Fact]
    public void Strings_Contains_WithIgnoreCaseFalse_ReturnsFalse()
    {
        Strings.Contains("Hello World", new[] { "hello" }, IgnoreCase.False).ShouldBeFalse();
    }

    [Fact]
    public void Strings_ContainsChinese_WithChineseChars_ReturnsTrue()
    {
        Strings.ContainsChinese("Hello 你好").ShouldBeTrue();
    }

    [Fact]
    public void Strings_ContainsChinese_NoChineseChars_ReturnsFalse()
    {
        Strings.ContainsChinese("Hello World").ShouldBeFalse();
    }

    [Fact]
    public void Strings_ContainsChinese_NullOrWhitespace_ReturnsFalse()
    {
        Strings.ContainsChinese(null).ShouldBeFalse();
        Strings.ContainsChinese("").ShouldBeFalse();
    }

    #endregion

    #region Strings.Is

    [Fact]
    public void Strings_IsUpper_AllUpperLetters_ReturnsTrue()
    {
        Strings.IsUpper("HELLO").ShouldBeTrue();
    }

    [Fact]
    public void Strings_IsUpper_MixedCase_ReturnsFalse()
    {
        Strings.IsUpper("Hello").ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsLower_AllLowerLetters_ReturnsTrue()
    {
        Strings.IsLower("hello").ShouldBeTrue();
    }

    [Fact]
    public void Strings_IsLower_MixedCase_ReturnsFalse()
    {
        Strings.IsLower("Hello").ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsChinese_Char_ChineseChar_ReturnsTrue()
    {
        Strings.IsChinese('好').ShouldBeTrue();
    }

    [Fact]
    public void Strings_IsChinese_Char_NonChinese_ReturnsFalse()
    {
        Strings.IsChinese('A').ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsChinese_String_AllChinese_ReturnsTrue()
    {
        Strings.IsChinese("你好世界").ShouldBeTrue();
    }

    [Fact]
    public void Strings_IsChinese_String_Mixed_ReturnsFalse()
    {
        Strings.IsChinese("Hello你好").ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsChinese_String_NullOrWhitespace_ReturnsFalse()
    {
        Strings.IsChinese((string)null).ShouldBeFalse();
        Strings.IsChinese("").ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsAllUpperCase_OnlyLetters_ReturnsTrue()
    {
        Strings.IsAllUpperCase("HELLO").ShouldBeTrue();
    }

    [Fact]
    public void Strings_IsAllUpperCase_WithDigit_ReturnsFalse()
    {
        Strings.IsAllUpperCase("HELLO1").ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsAllLowerCase_OnlyLetters_ReturnsTrue()
    {
        Strings.IsAllLowerCase("hello").ShouldBeTrue();
    }

    [Fact]
    public void Strings_IsAllLowerCase_MixedCase_ReturnsFalse()
    {
        Strings.IsAllLowerCase("Hello").ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsAllLetters_OnlyLetters_ReturnsTrue()
    {
        Strings.IsAllLetters("HelloWorld").ShouldBeTrue();
    }

    [Fact]
    public void Strings_IsAllLetters_WithDigit_ReturnsFalse()
    {
        Strings.IsAllLetters("Hello1").ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsAllDigits_OnlyDigits_ReturnsTrue()
    {
        Strings.IsAllDigits("12345").ShouldBeTrue();
    }

    [Fact]
    public void Strings_IsAllDigits_WithLetter_ReturnsFalse()
    {
        Strings.IsAllDigits("123a5").ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsAlphanumeric_LettersAndDigits_ReturnsTrue()
    {
        Strings.IsAlphanumeric("Hello123").ShouldBeTrue();
    }

    [Fact]
    public void Strings_IsAlphanumeric_WithSpecialChar_ReturnsFalse()
    {
        Strings.IsAlphanumeric("Hello!").ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsAsciiLetters_AsciiOnly_ReturnsTrue()
    {
        Strings.IsAsciiLetters("Hello").ShouldBeTrue();
    }

    [Fact]
    public void Strings_IsAsciiLetters_WithDigit_ReturnsFalse()
    {
        Strings.IsAsciiLetters("Hello1").ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsAsciiDigits_DigitsOnly_ReturnsTrue()
    {
        Strings.IsAsciiDigits("12345").ShouldBeTrue();
    }

    [Fact]
    public void Strings_IsAsciiDigits_WithLetter_ReturnsFalse()
    {
        Strings.IsAsciiDigits("123a").ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsAsciiAlphanumeric_Mixed_ReturnsTrue()
    {
        Strings.IsAsciiAlphanumeric("Hello123").ShouldBeTrue();
    }

    [Fact]
    public void Strings_IsAsciiAlphanumeric_WithSpecialChar_ReturnsFalse()
    {
        Strings.IsAsciiAlphanumeric("Hello!").ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsValidIdentifier_ValidIdentifier_ReturnsTrue()
    {
        Strings.IsValidIdentifier("_myVar123").ShouldBeTrue();
    }

    [Fact]
    public void Strings_IsValidIdentifier_StartsWithDigit_ReturnsFalse()
    {
        Strings.IsValidIdentifier("1var").ShouldBeFalse();
    }

    [Fact]
    public void Strings_IsValidIdentifier_WithSpecialChar_ReturnsFalse()
    {
        Strings.IsValidIdentifier("my-var").ShouldBeFalse();
    }

    #endregion

    #region StringsShortcutExtensions (Is.*NullOrEmpty / WhiteSpace)

    [Fact]
    public void IsNullOrEmpty_NullString_ReturnsTrue()
    {
        ((string)null).IsNullOrEmpty().ShouldBeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_EmptyString_ReturnsTrue()
    {
        "".IsNullOrEmpty().ShouldBeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_NonEmpty_ReturnsFalse()
    {
        "hello".IsNullOrEmpty().ShouldBeFalse();
    }

    [Fact]
    public void IsNotNullNorEmpty_NonEmpty_ReturnsTrue()
    {
        "hello".IsNotNullNorEmpty().ShouldBeTrue();
    }

    [Fact]
    public void IsNullOrWhiteSpace_Whitespace_ReturnsTrue()
    {
        "   ".IsNullOrWhiteSpace().ShouldBeTrue();
    }

    [Fact]
    public void IsNotNullNorWhiteSpace_NonWhitespace_ReturnsTrue()
    {
        "hello".IsNotNullNorWhiteSpace().ShouldBeTrue();
    }

    #endregion

    #region Strings.Has

    [Fact]
    public void Strings_HasNumbers_WithDigit_ReturnsTrue()
    {
        Strings.HasNumbers("abc1def").ShouldBeTrue();
    }

    [Fact]
    public void Strings_HasNumbers_NoDigit_ReturnsFalse()
    {
        Strings.HasNumbers("abcdef").ShouldBeFalse();
    }

    [Fact]
    public void Strings_HasNumbersAtLeast_EnoughDigits_ReturnsTrue()
    {
        Strings.HasNumbersAtLeast("abc123", 3).ShouldBeTrue();
    }

    [Fact]
    public void Strings_HasNumbersAtLeast_NotEnoughDigits_ReturnsFalse()
    {
        Strings.HasNumbersAtLeast("abc1", 3).ShouldBeFalse();
    }

    [Fact]
    public void Strings_HasLetters_WithLetter_ReturnsTrue()
    {
        Strings.HasLetters("123a456").ShouldBeTrue();
    }

    [Fact]
    public void Strings_HasLetters_NoLetter_ReturnsFalse()
    {
        Strings.HasLetters("12345").ShouldBeFalse();
    }

    [Fact]
    public void Strings_HasLettersAtLeast_EnoughLetters_ReturnsTrue()
    {
        Strings.HasLettersAtLeast("abc123", 3).ShouldBeTrue();
    }

    [Fact]
    public void Strings_HasLettersAtLeast_NotEnoughLetters_ReturnsFalse()
    {
        Strings.HasLettersAtLeast("a1234", 3).ShouldBeFalse();
    }

    #endregion

    #region Strings.Take

    [Fact]
    public void Take_LongerThanLength_ReturnsPrefix()
    {
        Strings.Take("Hello World", 5).ShouldBe("Hello");
    }

    [Fact]
    public void Take_ShorterThanLength_ReturnsAll()
    {
        Strings.Take("Hi", 10).ShouldBe("Hi");
    }

    [Fact]
    public void Take_NullOrWhitespace_ReturnsEmpty()
    {
        Strings.Take(null, 5).ShouldBe(string.Empty);
        Strings.Take("", 5).ShouldBe(string.Empty);
    }

    #endregion

    #region Strings.Substring

    [Fact]
    public void Substring_PositiveIndices_ReturnsRange()
    {
        Strings.Substring("Hello World", 6, 11).ShouldBe("World");
    }

    [Fact]
    public void Substring_NegativeFromIndex_CountsFromEnd()
    {
        Strings.Substring("Hello World", -5, 11).ShouldBe("World");
    }

    [Fact]
    public void Substring_SwappedIndices_AutoSwaps()
    {
        Strings.Substring("Hello World", 11, 6).ShouldBe("World");
    }

    [Fact]
    public void Substring_SameIndices_ReturnsEmpty()
    {
        Strings.Substring("Hello", 2, 2).ShouldBe(string.Empty);
    }

    [Fact]
    public void Substring_NullInput_ReturnsEmpty()
    {
        Strings.Substring(null, 0, 5).ShouldBe(string.Empty);
    }

    [Fact]
    public void SubstringWithLength_PositiveLength_ReturnsSubstring()
    {
        Strings.SubstringWithLength("Hello World", 6, 5).ShouldBe("World");
    }

    #endregion

    #region Strings.SubstringBefore / SubstringAfter / SubstringBetween

    [Fact]
    public void SubstringBefore_FirstSeparator_ReturnsPrefix()
    {
        Strings.SubstringBefore("abcba", "b", false).ShouldBe("a");
    }

    [Fact]
    public void SubstringBefore_LastSeparator_ReturnsBeforeLast()
    {
        Strings.SubstringBefore("abcba", "b", true).ShouldBe("abc");
    }

    [Fact]
    public void SubstringBefore_SeparatorNotFound_ReturnsWhole()
    {
        Strings.SubstringBefore("abc", "d", false).ShouldBe("abc");
    }

    [Fact]
    public void SubstringBefore_SeparatorAtStart_ReturnsEmpty()
    {
        Strings.SubstringBefore("abc", "a", false).ShouldBe(string.Empty);
    }

    [Fact]
    public void SubstringBefore_Char_FirstSeparator_ReturnsPrefix()
    {
        Strings.SubstringBefore("abcba", 'b', false).ShouldBe("a");
    }

    [Fact]
    public void SubstringAfter_FirstSeparator_ReturnsSuffix()
    {
        Strings.SubstringAfter("abcba", "b", false).ShouldBe("cba");
    }

    [Fact]
    public void SubstringAfter_LastSeparator_ReturnsAfterLast()
    {
        Strings.SubstringAfter("abcba", "b", true).ShouldBe("a");
    }

    [Fact]
    public void SubstringAfter_SeparatorNotFound_ReturnsEmpty()
    {
        Strings.SubstringAfter("abc", "d", false).ShouldBe(string.Empty);
    }

    [Fact]
    public void SubstringAfter_Char_ReturnsSuffix()
    {
        Strings.SubstringAfter("abcba", 'b', false).ShouldBe("cba");
    }

    [Fact]
    public void SubstringBetween_TwoMarkers_ReturnsMiddle()
    {
        Strings.SubstringBetween("before[content]after", "[", "]").ShouldBe("content");
    }

    [Fact]
    public void SubstringBetween_SameMarker_ReturnsMiddle()
    {
        Strings.SubstringBetween("*hello*", "*").ShouldBe("hello");
    }

    [Fact]
    public void SubstringBetween_MarkerNotFound_ReturnsEmpty()
    {
        Strings.SubstringBetween("hello", "[", "]").ShouldBe(string.Empty);
    }

    #endregion

    #region Strings.Replace

    [Fact]
    public void ReplaceIgnoreCase_MatchesIgnoringCase_ReturnsReplaced()
    {
        Strings.ReplaceIgnoreCase("Hello World", "world", "Earth").ShouldBe("Hello Earth");
    }

    [Fact]
    public void ReplaceIgnoreCase_NoMatch_ReturnsOriginal()
    {
        Strings.ReplaceIgnoreCase("Hello World", "xyz", "Earth").ShouldBe("Hello World");
    }

    [Fact]
    public void ReplaceFirstOccurrence_OnlyReplacesFirst()
    {
        Strings.ReplaceFirstOccurrence("aaa", "a", "b").ShouldBe("baa");
    }

    [Fact]
    public void ReplaceFirstOccurrence_NoMatch_ReturnsOriginal()
    {
        Strings.ReplaceFirstOccurrence("abc", "x", "y").ShouldBe("abc");
    }

    [Fact]
    public void ReplaceLastOccurrence_OnlyReplacesLast()
    {
        Strings.ReplaceLastOccurrence("aaa", "a", "b").ShouldBe("aab");
    }

    [Fact]
    public void ReplaceLastOccurrence_NoMatch_ReturnsOriginal()
    {
        Strings.ReplaceLastOccurrence("abc", "x", "y").ShouldBe("abc");
    }

    [Fact]
    public void ReplaceOnlyAtEndIgnoreCase_EndsWithMatch_ReplacesEnd()
    {
        Strings.ReplaceOnlyAtEndIgnoreCase("Hello.CS", ".cs", ".txt").ShouldBe("Hello.txt");
    }

    [Fact]
    public void ReplaceOnlyAtEndIgnoreCase_NotAtEnd_ReturnsOriginal()
    {
        Strings.ReplaceOnlyAtEndIgnoreCase("Hello.cs World", ".cs", ".txt").ShouldBe("Hello.cs World");
    }

    #endregion
}
