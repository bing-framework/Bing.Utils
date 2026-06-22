using System;
using System.Text;
using Bing.Text;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Bing.Text;

/// <summary>
/// StringExtensions — Digits / Equals / Fill / From / Is / Letters /
///                    Mask / Remove / Safe / Substring / Count
/// </summary>
public class StringExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Digits
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void OnlyDigits_MixedString_ReturnsDigitsOnly() => "abc123def456".OnlyDigits().ShouldBe("123456");
    [Fact] public void OnlyDigits_NoDigits_ReturnsEmpty() => "abc".OnlyDigits().ShouldBe(string.Empty);
    [Fact] public void OnlyDigits_WithExceptions_IncludesExceptionChars() => "12.34".OnlyDigits(['.', '-']).ShouldBe("12.34");
    [Fact] public void TotalDigits_MixedString_ReturnsCount() => "a1b2c3".TotalDigits().ShouldBe(3);
    [Fact] public void TotalDigits_EmptyString_ReturnsZero() => "".TotalDigits().ShouldBe(0);
    [Fact] public void ContainsOnlyDigits_AllDigits_ReturnsTrue() => "12345".ContainsOnlyDigits().ShouldBeTrue();
    [Fact] public void ContainsOnlyDigits_Mixed_ReturnsFalse() => "123a".ContainsOnlyDigits().ShouldBeFalse();
    [Fact] public void NonContainsDigits_NoDigits_ReturnsTrue() => "abc".NonContainsDigits().ShouldBeTrue();
    [Fact] public void NonContainsDigits_HasDigits_ReturnsFalse() => "abc1".NonContainsDigits().ShouldBeFalse();
    [Fact] public void ContainsDigits_HasDigit_ReturnsTrue() => "a1b".ContainsDigits().ShouldBeTrue();
    [Fact] public void ContainsDigits_NoDigit_ReturnsFalse() => "abc".ContainsDigits().ShouldBeFalse();
    [Fact] public void IncludeDigits_HasDigit_ReturnsTrue() => "a1".IncludeDigits().ShouldBeTrue();
    [Fact] public void IncludeDigits_EmptyString_ReturnsFalse() => "".IncludeDigits(1).ShouldBeFalse();
    [Fact] public void IncludeDigits_WithMinCount_2_Returns() => "a12b".IncludeDigits(2).ShouldBeTrue();
    [Fact] public void IncludeDigits_WithMinCount_3_ReturnsFalse() => "a12b".IncludeDigits(3).ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Equals
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void EqualsAnyOf_Matching_ReturnsTrue()
        => "hello".EqualsAnyOf(StringComparison.OrdinalIgnoreCase, "Hello", "World").ShouldBeTrue();
    [Fact] public void EqualsAnyOf_NotMatching_ReturnsFalse()
        => "foo".EqualsAnyOf(StringComparison.Ordinal, "bar", "baz").ShouldBeFalse();
    [Fact] public void EqualsAnyOf_EmptyArray_ReturnsFalse()
        => "hello".EqualsAnyOf(StringComparison.Ordinal).ShouldBeFalse();

    [Fact] public void EqualsAnyOfIgnoreCase_Matching_ReturnsTrue()
        => "Hello".EqualsAnyOfIgnoreCase("hello", "world").ShouldBeTrue();
    [Fact] public void EqualsAnyOfIgnoreCase_NotMatching_ReturnsFalse()
        => "Hello".EqualsAnyOfIgnoreCase("foo", "bar").ShouldBeFalse();

    [Fact] public void EqualsIgnoreCase_SameCase_ReturnsTrue() => "hello".EqualsIgnoreCase("hello").ShouldBeTrue();
    [Fact] public void EqualsIgnoreCase_DifferentCase_ReturnsTrue() => "Hello".EqualsIgnoreCase("HELLO").ShouldBeTrue();
    [Fact] public void EqualsIgnoreCase_Different_ReturnsFalse() => "Hello".EqualsIgnoreCase("world").ShouldBeFalse();

    [Fact] public void EqualsInvariant_WithSpaces_TrimAndIgnoreCase() => "  Hello  ".EqualsInvariant("hello").ShouldBeTrue();
    [Fact] public void EqualsInvariant_BothNull_ReturnsTrue() => ((string)null).EqualsInvariant(null).ShouldBeTrue();
    [Fact] public void EqualsInvariant_OneNull_ReturnsFalse() => "hello".EqualsInvariant(null).ShouldBeFalse();

    [Fact] public void EqualsTo_IgnoreCaseByDefault_ReturnsTrue() => "Hello".EqualsTo("hello").ShouldBeTrue();

    [Fact] public void EqualsWithOptions_TrimAndIgnoreCase_ReturnsTrue()
        => "  Hello  ".EqualsWithOptions("hello", trim: true).ShouldBeTrue();
    [Fact] public void EqualsWithOptions_CaseSensitive_ReturnsFalse()
        => "Hello".EqualsWithOptions("hello", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] public void EqualsWithOptions_HandleNull_BothNull_ReturnsTrue()
        => ((string)null).EqualsWithOptions(null, handleNull: true).ShouldBeTrue();

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Fill
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void Fill_Format_ReplacesPlaceholders() => "Hello {0}!".Fill("World").ShouldBe("Hello World!");
    [Fact] public void Fill_MultipleArgs_ReplacesAll() => "{0} + {1} = {2}".Fill(1, 2, 3).ShouldBe("1 + 2 = 3");
    [Fact] public void Fill_WithBrTag_ReplacesWithNewLine() => "a<br>b".Fill().ShouldContain(Environment.NewLine);

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.From (Base64)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void FromBase64StringToBytes_ValidBase64_ReturnsBytes()
    {
        var original = "Hello World";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(original));
        var bytes = base64.FromBase64StringToBytes();
        Encoding.UTF8.GetString(bytes).ShouldBe(original);
    }

    [Fact]
    public void FromBase64String_ValidBase64_ReturnsOriginal()
    {
        var original = "Hello World";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(original));
        base64.FromBase64String(Encoding.UTF8).ShouldBe(original);
    }

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Is — IsUpper / IsLower / IsLike / IsLikeAny
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void IsUpper_AllUpper_ReturnsTrue() => "HELLO".IsUpper().ShouldBeTrue();
    [Fact] public void IsUpper_Mixed_ReturnsFalse() => "Hello".IsUpper().ShouldBeFalse();
    [Fact] public void IsLower_AllLower_ReturnsTrue() => "hello".IsLower().ShouldBeTrue();
    [Fact] public void IsLower_Mixed_ReturnsFalse() => "Hello".IsLower().ShouldBeFalse();

    [Fact] public void IsLike_StarPrefix_Matches() => "hello".IsLike("*llo").ShouldBeTrue();
    [Fact] public void IsLike_StarSuffix_Matches() => "hello".IsLike("h*").ShouldBeTrue();
    [Fact] public void IsLike_StarOnly_MatchesAnything() => "hello".IsLike("*").ShouldBeTrue();
    [Fact] public void IsLike_ExactMatch_ReturnsTrue() => "hello".IsLike("hello").ShouldBeTrue();
    [Fact] public void IsLike_NoMatch_ReturnsFalse() => "hello".IsLike("ha*").ShouldBeFalse();
    [Fact] public void IsLike_EmptyPattern_EmptyString_ReturnsTrue() => "".IsLike("").ShouldBeTrue();
    [Fact] public void IsLikeAny_AnyMatch_ReturnsTrue() => "hello".IsLikeAny("h*", "world").ShouldBeTrue();
    [Fact] public void IsLikeAny_NoMatch_ReturnsFalse() => "hello".IsLikeAny("a*", "b*").ShouldBeFalse();
    [Fact] public void IsLikeAny_EmptyPatterns_ReturnsFalse() => "hello".IsLikeAny().ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Letters
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void TotalLetters_MixedString_ReturnsLetterCount() => "a1b2C3".TotalLetters().ShouldBe(3);
    [Fact] public void TotalLetters_EmptyString_ReturnsZero() => "".TotalLetters().ShouldBe(0);
    [Fact] public void TotalLowerLetters_ReturnsLowerCount() => "aAbBcC".TotalLowerLetters().ShouldBe(3);
    [Fact] public void TotalUpperLetters_ReturnsUpperCount() => "aAbBcC".TotalUpperLetters().ShouldBe(3);
    [Fact] public void IncludeLetters_HasLetter_ReturnsTrue() => "a1".IncludeLetters().ShouldBeTrue();
    [Fact] public void IncludeLetters_EmptyString_ReturnsFalse() => "".IncludeLetters(1).ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Mask
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Mask_LongString_MasksMiddle()
    {
        var result = "13812345678".Mask();
        result.ShouldStartWith("138");
        result.ShouldEndWith("5678");
        result.ShouldContain("*");
    }

    [Fact] public void Mask_ShortString_MasksEnd() => "hello".Mask().ShouldStartWith("h");
    [Fact] public void Mask_Null_ReturnsNull() => ((string)null).Mask().ShouldBeNull();
    [Fact] public void Mask_Empty_ReturnsEmpty() => "".Mask().ShouldBe(string.Empty);

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Remove
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void RemoveFromIgnoreCase_IgnoresCase() => "HelloWorld".RemoveFromIgnoreCase("O").ShouldBe("Hell");
    [Fact] public void RemoveDuplicateSpaces_MultipleSP_ReturnsOneSP() => "Hello  World".RemoveDuplicateSpaces().ShouldBe("Hello World");
    [Fact] public void RemoveAccentsIgnoreCase_Accents_Removed() => "café".RemoveAccentsIgnoreCase().ShouldBe("cafe");
    [Fact] public void RemoveAccentsIgnoreCaseAndN_NTilde_Replaced() => "niño".RemoveAccentsIgnoreCaseAndN().ShouldBe("nino");
    [Fact] public void RemoveChars_RemovesSpecifiedChars() => "Hello World".RemoveChars(' ').ShouldBe("HelloWorld");
    [Fact] public void RemoveWhiteSpace_AllSpacesRemoved() => "  Hello  World  ".RemoveWhiteSpace().ShouldBe("HelloWorld");
    [Fact] public void RemoveDuplicateWhiteSpaces_TabsAndSpaces_Normalized() => "Hello\t\nWorld".RemoveDuplicateWhiteSpaces().ShouldBe("Hello World");
    [Fact] public void RemoveDuplicateChar_RemovesDuplicates() => "Mississippi".RemoveDuplicateChar('s').ShouldBe("Misisippi");
    [Fact] public void RemoveSince_ByIndex_RemovesFromIndex() => "Hello World".RemoveSince(5).ShouldBe("Hello");
    [Fact] public void RemoveSince_ByString_RemovesFromSubstring() => "Hello World".RemoveSince("World").ShouldBe("Hello ");
    [Fact] public void RemoveSinceIgnoreCase_IgnoresCase() => "Hello World".RemoveSinceIgnoreCase("world").ShouldBe("Hello ");
    [Fact] public void RemoveStart_RemovesPrefix() => "Hello World".RemoveStart("Hello").ShouldBe(" World");
    [Fact] public void RemoveStart_NotPresent_ReturnsOriginal() => "Hello World".RemoveStart("Foo").ShouldBe("Hello World");
    [Fact] public void RemoveEnd_RemovesSuffix() => "Hello World".RemoveEnd("World").ShouldBe("Hello ");
    [Fact] public void RemoveEnd_NotPresent_ReturnsOriginal() => "Hello World".RemoveEnd("Foo").ShouldBe("Hello World");
    [Fact] public void Remove_SubString_AllOccurrences() => "Hello Hello World".Remove("Hello ").ShouldBe("World");

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Safe — UrlEncode / UrlDecode
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void UrlEncode_SpecialChars_Encoded()
    {
        var encoded = "Hello World&test=1".UrlEncode();
        encoded.ShouldContain("%");
    }

    [Fact]
    public void UrlDecode_EncodedString_Decoded()
    {
        var original = "Hello World&test=1";
        var encoded = original.UrlEncode();
        encoded.UrlDecode().ShouldBe(original);
    }

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Substring — Take
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void Take_LengthLessThanString_ReturnsTruncated() => "Hello World".Take(5).ShouldBe("Hello");
    [Fact] public void Take_LengthGreaterThanString_ReturnsOriginal() => "Hello".Take(100).ShouldBe("Hello");

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Count
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void CharacterCount_AsciiString_ReturnsLength() => "Hello".CharacterCount().ShouldBe(5);
    [Fact] public void BytesCount_AsciiString_ReturnsByteLength() => "Hello".BytesCount().ShouldBe(5);
    [Fact] public void BytesCount_ChineseString_ReturnsByteLength() => "你好".BytesCount().ShouldBeGreaterThan(2);
}
