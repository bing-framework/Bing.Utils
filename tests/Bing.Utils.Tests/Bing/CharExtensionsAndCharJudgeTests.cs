using Bing.Text;

namespace Bing.Utils.Tests.Bing;

/// <summary>
/// <see cref="CharExtensions"/> + <see cref="CharJudge"/> 单元测试
/// </summary>
public class CharExtensionsAndCharJudgeTests
{
    // ─────────────────────────────────────────────────────────────────
    // GetNumericValue
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GetNumericValue_Digit_ReturnsCorrectValue() =>
        '5'.GetNumericValue().ShouldBe(5.0);

    [Fact]
    public void GetNumericValue_Letter_ReturnsNegativeOne() =>
        'a'.GetNumericValue().ShouldBe(-1.0);

    // ─────────────────────────────────────────────────────────────────
    // Repeat
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Repeat_MultipleTimers_ReturnsRepeatedString() =>
        'a'.Repeat(3).ShouldBe("aaa");

    [Fact]
    public void Repeat_Zero_ReturnsEmpty() =>
        'x'.Repeat(0).ShouldBe(string.Empty);

    // ─────────────────────────────────────────────────────────────────
    // IsBetween
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IsBetween_Inside_ReturnsTrue() =>
        'c'.IsBetween('a', 'z').ShouldBeTrue();

    [Fact]
    public void IsBetween_AtMin_ReturnsTrue() =>
        'a'.IsBetween('a', 'z').ShouldBeTrue();

    [Fact]
    public void IsBetween_AtMax_ReturnsTrue() =>
        'z'.IsBetween('a', 'z').ShouldBeTrue();

    [Fact]
    public void IsBetween_Outside_ReturnsFalse() =>
        'A'.IsBetween('a', 'z').ShouldBeFalse();

    [Fact]
    public void IsBetween_ReversedMinMax_StillWorks() =>
        'c'.IsBetween('z', 'a').ShouldBeTrue();

    // ─────────────────────────────────────────────────────────────────
    // In / NotIn
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void In_CharInArray_ReturnsTrue() =>
        'a'.In('a', 'b', 'c').ShouldBeTrue();

    [Fact]
    public void In_CharNotInArray_ReturnsFalse() =>
        'd'.In('a', 'b', 'c').ShouldBeFalse();

    [Fact]
    public void NotIn_CharNotInArray_ReturnsTrue() =>
        'd'.NotIn('a', 'b', 'c').ShouldBeTrue();

    [Fact]
    public void NotIn_CharInArray_ReturnsFalse() =>
        'a'.NotIn('a', 'b', 'c').ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // Is* predicates
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IsWhiteSpace_Space_ReturnsTrue() => ' '.IsWhiteSpace().ShouldBeTrue();

    [Fact]
    public void IsWhiteSpace_Letter_ReturnsFalse() => 'a'.IsWhiteSpace().ShouldBeFalse();

    [Fact]
    public void IsDigit_Digit_ReturnsTrue() => '5'.IsDigit().ShouldBeTrue();

    [Fact]
    public void IsDigit_Letter_ReturnsFalse() => 'a'.IsDigit().ShouldBeFalse();

    [Fact]
    public void IsLetter_Letter_ReturnsTrue() => 'a'.IsLetter().ShouldBeTrue();

    [Fact]
    public void IsLetter_Digit_ReturnsFalse() => '1'.IsLetter().ShouldBeFalse();

    [Fact]
    public void IsLetterOrDigit_Digit_ReturnsTrue() => '3'.IsLetterOrDigit().ShouldBeTrue();

    [Fact]
    public void IsLetterOrDigit_Letter_ReturnsTrue() => 'z'.IsLetterOrDigit().ShouldBeTrue();

    [Fact]
    public void IsLetterOrDigit_Symbol_ReturnsFalse() => '#'.IsLetterOrDigit().ShouldBeFalse();

    [Fact]
    public void IsLower_Lower_ReturnsTrue() => 'a'.IsLower().ShouldBeTrue();

    [Fact]
    public void IsLower_Upper_ReturnsFalse() => 'A'.IsLower().ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // Case conversion
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToLower_Upper_ReturnsLower() => 'A'.ToLower().ShouldBe('a');

    [Fact]
    public void ToUpper_Lower_ReturnsUpper() => 'a'.ToUpper().ShouldBe('A');

    [Fact]
    public void ToLowerInvariant_Upper_ReturnsLower() => 'Z'.ToLowerInvariant().ShouldBe('z');

    [Fact]
    public void ToUpperInvariant_Lower_ReturnsUpper() => 'z'.ToUpperInvariant().ShouldBe('Z');

    // ─────────────────────────────────────────────────────────────────
    // EqualsIgnoreCase
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void EqualsIgnoreCase_SameLetterDifferentCase_ReturnsTrue() =>
        'A'.EqualsIgnoreCase('a').ShouldBeTrue();

    [Fact]
    public void EqualsIgnoreCase_SameCase_ReturnsTrue() =>
        'a'.EqualsIgnoreCase('a').ShouldBeTrue();

    [Fact]
    public void EqualsIgnoreCase_DifferentLetters_ReturnsFalse() =>
        'a'.EqualsIgnoreCase('b').ShouldBeFalse();

    [Fact]
    public void EqualsIgnoreCase_NullableChar_Null_ReturnsFalse() =>
        ((char?)null).EqualsIgnoreCase('a').ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // To (range enumeration)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void To_AscendingRange_ReturnsCorrectChars() =>
        'a'.To('e').ShouldBe(new[] { 'a', 'b', 'c', 'd', 'e' });

    [Fact]
    public void To_DescendingRange_ReturnsReversedChars() =>
        'e'.To('a').ShouldBe(new[] { 'e', 'd', 'c', 'b', 'a' });

    // ─────────────────────────────────────────────────────────────────
    // AsString
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AsString_ReturnsCharAsString() =>
        'X'.AsString().ShouldBe("X");

    // ─────────────────────────────────────────────────────────────────
    // CharJudge — IsBlankChar
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void CharJudge_IsBlankChar_Space_ReturnsTrue() =>
        CharJudge.IsBlankChar(' ').ShouldBeTrue();

    [Fact]
    public void CharJudge_IsBlankChar_Tab_ReturnsTrue() =>
        CharJudge.IsBlankChar('\t').ShouldBeTrue();

    [Fact]
    public void CharJudge_IsBlankChar_BOM_ReturnsTrue() =>
        CharJudge.IsBlankChar('\ufeff').ShouldBeTrue();

    [Fact]
    public void CharJudge_IsBlankChar_Null_ReturnsTrue() =>
        CharJudge.IsBlankChar('\u0000').ShouldBeTrue();

    [Fact]
    public void CharJudge_IsBlankChar_Letter_ReturnsFalse() =>
        CharJudge.IsBlankChar('a').ShouldBeFalse();

    [Fact]
    public void CharJudge_IsBlankChar_Digit_ReturnsFalse() =>
        CharJudge.IsBlankChar('1').ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // CharJudge — IsEmoji
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void CharJudge_IsEmoji_HeavyBlackHeart_ReturnsTrue() =>
        // U+2764 ❤
        CharJudge.IsEmoji('\u2764').ShouldBeTrue();

    [Fact]
    public void CharJudge_IsEmoji_Sun_ReturnsTrue() =>
        // U+2600 ☀
        CharJudge.IsEmoji('\u2600').ShouldBeTrue();

    [Fact]
    public void CharJudge_IsEmoji_Letter_ReturnsFalse() =>
        CharJudge.IsEmoji('a').ShouldBeFalse();

    [Fact]
    public void CharJudge_IsEmoji_Digit_ReturnsFalse() =>
        CharJudge.IsEmoji('5').ShouldBeFalse();
}
