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

    /// <summary>
    /// 验证数字字符返回对应数值。
    /// </summary>
    [Fact]
    public void GetNumericValue_Digit_ReturnsCorrectValue() =>
        '5'.GetNumericValue().ShouldBe(5.0);

    /// <summary>
    /// 验证字母字符的数值为负一。
    /// </summary>
    [Fact]
    public void GetNumericValue_Letter_ReturnsNegativeOne() =>
        'a'.GetNumericValue().ShouldBe(-1.0);

    // ─────────────────────────────────────────────────────────────────
    // Repeat
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证字符按指定次数重复。
    /// </summary>
    [Fact]
    public void Repeat_MultipleTimers_ReturnsRepeatedString() =>
        'a'.Repeat(3).ShouldBe("aaa");

    /// <summary>
    /// 验证重复零次返回空文本。
    /// </summary>
    [Fact]
    public void Repeat_Zero_ReturnsEmpty() =>
        'x'.Repeat(0).ShouldBe(string.Empty);

    // ─────────────────────────────────────────────────────────────────
    // IsBetween
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证范围内字符满足区间判断。
    /// </summary>
    [Fact]
    public void IsBetween_Inside_ReturnsTrue() =>
        'c'.IsBetween('a', 'z').ShouldBeTrue();

    /// <summary>
    /// 验证字符区间包含下界。
    /// </summary>
    [Fact]
    public void IsBetween_AtMin_ReturnsTrue() =>
        'a'.IsBetween('a', 'z').ShouldBeTrue();

    /// <summary>
    /// 验证字符区间包含上界。
    /// </summary>
    [Fact]
    public void IsBetween_AtMax_ReturnsTrue() =>
        'z'.IsBetween('a', 'z').ShouldBeTrue();

    /// <summary>
    /// 验证范围外字符不满足区间判断。
    /// </summary>
    [Fact]
    public void IsBetween_Outside_ReturnsFalse() =>
        'A'.IsBetween('a', 'z').ShouldBeFalse();

    /// <summary>
    /// 验证颠倒字符边界仍可判断区间。
    /// </summary>
    [Fact]
    public void IsBetween_ReversedMinMax_StillWorks() =>
        'c'.IsBetween('z', 'a').ShouldBeTrue();

    // ─────────────────────────────────────────────────────────────────
    // In / NotIn
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证字符存在于候选数组。
    /// </summary>
    [Fact]
    public void In_CharInArray_ReturnsTrue() =>
        'a'.In('a', 'b', 'c').ShouldBeTrue();

    /// <summary>
    /// 验证字符不在候选数组时返回否定结果。
    /// </summary>
    [Fact]
    public void In_CharNotInArray_ReturnsFalse() =>
        'd'.In('a', 'b', 'c').ShouldBeFalse();

    /// <summary>
    /// 验证字符不在候选数组时满足排除判断。
    /// </summary>
    [Fact]
    public void NotIn_CharNotInArray_ReturnsTrue() =>
        'd'.NotIn('a', 'b', 'c').ShouldBeTrue();

    /// <summary>
    /// 验证候选数组内字符不满足排除判断。
    /// </summary>
    [Fact]
    public void NotIn_CharInArray_ReturnsFalse() =>
        'a'.NotIn('a', 'b', 'c').ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // Is* predicates
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证空格是空白字符。
    /// </summary>
    [Fact]
    public void IsWhiteSpace_Space_ReturnsTrue() => ' '.IsWhiteSpace().ShouldBeTrue();

    /// <summary>
    /// 验证字母不是空白字符。
    /// </summary>
    [Fact]
    public void IsWhiteSpace_Letter_ReturnsFalse() => 'a'.IsWhiteSpace().ShouldBeFalse();

    /// <summary>
    /// 验证数字字符满足数字判断。
    /// </summary>
    [Fact]
    public void IsDigit_Digit_ReturnsTrue() => '5'.IsDigit().ShouldBeTrue();

    /// <summary>
    /// 验证字母不满足数字判断。
    /// </summary>
    [Fact]
    public void IsDigit_Letter_ReturnsFalse() => 'a'.IsDigit().ShouldBeFalse();

    /// <summary>
    /// 验证字母满足字母判断。
    /// </summary>
    [Fact]
    public void IsLetter_Letter_ReturnsTrue() => 'a'.IsLetter().ShouldBeTrue();

    /// <summary>
    /// 验证数字不满足字母判断。
    /// </summary>
    [Fact]
    public void IsLetter_Digit_ReturnsFalse() => '1'.IsLetter().ShouldBeFalse();

    /// <summary>
    /// 验证数字满足字母或数字判断。
    /// </summary>
    [Fact]
    public void IsLetterOrDigit_Digit_ReturnsTrue() => '3'.IsLetterOrDigit().ShouldBeTrue();

    /// <summary>
    /// 验证字母满足字母或数字判断。
    /// </summary>
    [Fact]
    public void IsLetterOrDigit_Letter_ReturnsTrue() => 'z'.IsLetterOrDigit().ShouldBeTrue();

    /// <summary>
    /// 验证符号不满足字母或数字判断。
    /// </summary>
    [Fact]
    public void IsLetterOrDigit_Symbol_ReturnsFalse() => '#'.IsLetterOrDigit().ShouldBeFalse();

    /// <summary>
    /// 验证小写字母满足小写判断。
    /// </summary>
    [Fact]
    public void IsLower_Lower_ReturnsTrue() => 'a'.IsLower().ShouldBeTrue();

    /// <summary>
    /// 验证大写字母不满足小写判断。
    /// </summary>
    [Fact]
    public void IsLower_Upper_ReturnsFalse() => 'A'.IsLower().ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // Case conversion
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证大写字符转换为小写。
    /// </summary>
    [Fact]
    public void ToLower_Upper_ReturnsLower() => 'A'.ToLower().ShouldBe('a');

    /// <summary>
    /// 验证小写字符转换为大写。
    /// </summary>
    [Fact]
    public void ToUpper_Lower_ReturnsUpper() => 'a'.ToUpper().ShouldBe('A');

    /// <summary>
    /// 验证使用固定区域性将字符转换为小写。
    /// </summary>
    [Fact]
    public void ToLowerInvariant_Upper_ReturnsLower() => 'Z'.ToLowerInvariant().ShouldBe('z');

    /// <summary>
    /// 验证使用固定区域性将字符转换为大写。
    /// </summary>
    [Fact]
    public void ToUpperInvariant_Lower_ReturnsUpper() => 'z'.ToUpperInvariant().ShouldBe('Z');

    // ─────────────────────────────────────────────────────────────────
    // EqualsIgnoreCase
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证不同大小写的同一字母相等。
    /// </summary>
    [Fact]
    public void EqualsIgnoreCase_SameLetterDifferentCase_ReturnsTrue() =>
        'A'.EqualsIgnoreCase('a').ShouldBeTrue();

    /// <summary>
    /// 验证相同字符在忽略大小写比较下相等。
    /// </summary>
    [Fact]
    public void EqualsIgnoreCase_SameCase_ReturnsTrue() =>
        'a'.EqualsIgnoreCase('a').ShouldBeTrue();

    /// <summary>
    /// 验证不同字母在忽略大小写比较下不相等。
    /// </summary>
    [Fact]
    public void EqualsIgnoreCase_DifferentLetters_ReturnsFalse() =>
        'a'.EqualsIgnoreCase('b').ShouldBeFalse();

    /// <summary>
    /// 验证空字符值与实际字符不相等。
    /// </summary>
    [Fact]
    public void EqualsIgnoreCase_NullableChar_Null_ReturnsFalse() =>
        ((char?)null).EqualsIgnoreCase('a').ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // To (range enumeration)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证升序字符范围包含两端及中间字符。
    /// </summary>
    [Fact]
    public void To_AscendingRange_ReturnsCorrectChars() =>
        'a'.To('e').ShouldBe(new[] { 'a', 'b', 'c', 'd', 'e' });

    /// <summary>
    /// 验证降序字符范围按逆序返回两端及中间字符。
    /// </summary>
    [Fact]
    public void To_DescendingRange_ReturnsReversedChars() =>
        'e'.To('a').ShouldBe(new[] { 'e', 'd', 'c', 'b', 'a' });

    // ─────────────────────────────────────────────────────────────────
    // AsString
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证字符转换为对应的单字符文本。
    /// </summary>
    [Fact]
    public void AsString_ReturnsCharAsString() =>
        'X'.AsString().ShouldBe("X");

    // ─────────────────────────────────────────────────────────────────
    // CharJudge — IsBlankChar
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证空格属于空白字符。
    /// </summary>
    [Fact]
    public void CharJudge_IsBlankChar_Space_ReturnsTrue() =>
        CharJudge.IsBlankChar(' ').ShouldBeTrue();

    /// <summary>
    /// 验证制表符属于空白字符。
    /// </summary>
    [Fact]
    public void CharJudge_IsBlankChar_Tab_ReturnsTrue() =>
        CharJudge.IsBlankChar('\t').ShouldBeTrue();

    /// <summary>
    /// 验证字节顺序标记属于空白字符。
    /// </summary>
    [Fact]
    public void CharJudge_IsBlankChar_BOM_ReturnsTrue() =>
        CharJudge.IsBlankChar('\ufeff').ShouldBeTrue();

    /// <summary>
    /// 验证零字符属于空白字符。
    /// </summary>
    [Fact]
    public void CharJudge_IsBlankChar_Null_ReturnsTrue() =>
        CharJudge.IsBlankChar('\u0000').ShouldBeTrue();

    /// <summary>
    /// 验证字母不属于空白字符。
    /// </summary>
    [Fact]
    public void CharJudge_IsBlankChar_Letter_ReturnsFalse() =>
        CharJudge.IsBlankChar('a').ShouldBeFalse();

    /// <summary>
    /// 验证数字不属于空白字符。
    /// </summary>
    [Fact]
    public void CharJudge_IsBlankChar_Digit_ReturnsFalse() =>
        CharJudge.IsBlankChar('1').ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // CharJudge — IsEmoji
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证实心心形字符属于表情符号。
    /// </summary>
    [Fact]
    public void CharJudge_IsEmoji_HeavyBlackHeart_ReturnsTrue() =>
        // U+2764 ❤
        CharJudge.IsEmoji('\u2764').ShouldBeTrue();

    /// <summary>
    /// 验证太阳字符属于表情符号。
    /// </summary>
    [Fact]
    public void CharJudge_IsEmoji_Sun_ReturnsTrue() =>
        // U+2600 ☀
        CharJudge.IsEmoji('\u2600').ShouldBeTrue();

    /// <summary>
    /// 验证字母不属于表情符号。
    /// </summary>
    [Fact]
    public void CharJudge_IsEmoji_Letter_ReturnsFalse() =>
        CharJudge.IsEmoji('a').ShouldBeFalse();

    /// <summary>
    /// 验证数字不属于表情符号。
    /// </summary>
    [Fact]
    public void CharJudge_IsEmoji_Digit_ReturnsFalse() =>
        CharJudge.IsEmoji('5').ShouldBeFalse();
}
