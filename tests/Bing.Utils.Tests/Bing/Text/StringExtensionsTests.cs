using System;
using System.Text;
using Bing.Text;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Bing.Text;

/// <summary>
/// 验证字符串扩展的数字、比较、格式、字符分类、遮蔽、替换与截取行为。
/// </summary>
public class StringExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Digits
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证 OnlyDigits 仅保留数字，并可保留指定例外字符。
    /// </summary>
    [Fact] public void OnlyDigits_MixedString_ReturnsDigitsOnly() => "abc123def456".OnlyDigits().ShouldBe("123456");
    /// <summary>
    /// 验证 OnlyDigits 仅保留数字，并可保留指定例外字符。
    /// </summary>
    [Fact] public void OnlyDigits_NoDigits_ReturnsEmpty() => "abc".OnlyDigits().ShouldBe(string.Empty);
    /// <summary>
    /// 验证 OnlyDigits 会保留调用方指定的例外字符。
    /// </summary>
    [Fact] public void OnlyDigits_WithExceptions_IncludesExceptionChars() => "12.34".OnlyDigits(['.', '-']).ShouldBe("12.34");
    /// <summary>
    /// 验证 TotalDigits 返回文本中的数字字符数量。
    /// </summary>
    [Fact] public void TotalDigits_MixedString_ReturnsCount() => "a1b2c3".TotalDigits().ShouldBe(3);
    /// <summary>
    /// 验证空字符串包含零个数字字符。
    /// </summary>
    [Fact] public void TotalDigits_EmptyString_ReturnsZero() => "".TotalDigits().ShouldBe(0);
    /// <summary>
    /// 验证 ContainsOnlyDigits 判断文本是否全部由数字组成。
    /// </summary>
    [Fact] public void ContainsOnlyDigits_AllDigits_ReturnsTrue() => "12345".ContainsOnlyDigits().ShouldBeTrue();
    /// <summary>
    /// 验证包含非数字字符时 ContainsOnlyDigits 返回 false。
    /// </summary>
    [Fact] public void ContainsOnlyDigits_Mixed_ReturnsFalse() => "123a".ContainsOnlyDigits().ShouldBeFalse();
    /// <summary>
    /// 验证 NonContainsDigits 判断文本是否不含数字。
    /// </summary>
    [Fact] public void NonContainsDigits_NoDigits_ReturnsTrue() => "abc".NonContainsDigits().ShouldBeTrue();
    /// <summary>
    /// 验证包含数字时 NonContainsDigits 返回 false。
    /// </summary>
    [Fact] public void NonContainsDigits_HasDigits_ReturnsFalse() => "abc1".NonContainsDigits().ShouldBeFalse();
    /// <summary>
    /// 验证 ContainsDigits 判断文本中是否含有数字。
    /// </summary>
    [Fact] public void ContainsDigits_HasDigit_ReturnsTrue() => "a1b".ContainsDigits().ShouldBeTrue();
    /// <summary>
    /// 验证不含数字时 ContainsDigits 返回 false。
    /// </summary>
    [Fact] public void ContainsDigits_NoDigit_ReturnsFalse() => "abc".ContainsDigits().ShouldBeFalse();
    /// <summary>
    /// 验证 IncludeDigits 判断数字是否存在并满足指定最少数量。
    /// </summary>
    [Fact] public void IncludeDigits_HasDigit_ReturnsTrue() => "a1".IncludeDigits().ShouldBeTrue();
    /// <summary>
    /// 验证空字符串调用 IncludeDigits 时返回 false。
    /// </summary>
    [Fact] public void IncludeDigits_EmptyString_ReturnsFalse() => "".IncludeDigits(1).ShouldBeFalse();
    /// <summary>
    /// 验证数字数量达到指定最少数量时 IncludeDigits 返回 true。
    /// </summary>
    [Fact] public void IncludeDigits_WithMinCount_2_Returns() => "a12b".IncludeDigits(2).ShouldBeTrue();
    /// <summary>
    /// 验证数字数量未达到指定最少数量时 IncludeDigits 返回 false。
    /// </summary>
    [Fact] public void IncludeDigits_WithMinCount_3_ReturnsFalse() => "a12b".IncludeDigits(3).ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Equals
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证 EqualsAnyOf 按指定比较方式与多个候选值比较。
    /// </summary>
    [Fact]
    public void EqualsAnyOf_Matching_ReturnsTrue()
        => "hello".EqualsAnyOf(StringComparison.OrdinalIgnoreCase, "Hello", "World").ShouldBeTrue();
    /// <summary>
    /// 验证 EqualsAnyOf 按指定比较方式与多个候选值比较。
    /// </summary>
    [Fact]
    public void EqualsAnyOf_NotMatching_ReturnsFalse()
        => "foo".EqualsAnyOf(StringComparison.Ordinal, "bar", "baz").ShouldBeFalse();
    /// <summary>
    /// 验证 EqualsAnyOf 按指定比较方式与多个候选值比较。
    /// </summary>
    [Fact]
    public void EqualsAnyOf_EmptyArray_ReturnsFalse()
        => "hello".EqualsAnyOf(StringComparison.Ordinal).ShouldBeFalse();

    /// <summary>
    /// 验证 EqualsAnyOfIgnoreCase 忽略大小写比较多个候选值。
    /// </summary>
    [Fact]
    public void EqualsAnyOfIgnoreCase_Matching_ReturnsTrue()
        => "Hello".EqualsAnyOfIgnoreCase("hello", "world").ShouldBeTrue();
    /// <summary>
    /// 验证 EqualsAnyOfIgnoreCase 忽略大小写比较多个候选值。
    /// </summary>
    [Fact]
    public void EqualsAnyOfIgnoreCase_NotMatching_ReturnsFalse()
        => "Hello".EqualsAnyOfIgnoreCase("foo", "bar").ShouldBeFalse();

    /// <summary>
    /// 验证 EqualsIgnoreCase 忽略大小写比较两个字符串。
    /// </summary>
    [Fact] public void EqualsIgnoreCase_SameCase_ReturnsTrue() => "hello".EqualsIgnoreCase("hello").ShouldBeTrue();
    /// <summary>
    /// 验证 EqualsIgnoreCase 忽略大小写比较两个字符串。
    /// </summary>
    [Fact] public void EqualsIgnoreCase_DifferentCase_ReturnsTrue() => "Hello".EqualsIgnoreCase("HELLO").ShouldBeTrue();
    /// <summary>
    /// 验证 EqualsIgnoreCase 忽略大小写比较两个字符串。
    /// </summary>
    [Fact] public void EqualsIgnoreCase_Different_ReturnsFalse() => "Hello".EqualsIgnoreCase("world").ShouldBeFalse();

    /// <summary>
    /// 验证 EqualsInvariant 会修剪空白并忽略大小写。
    /// </summary>
    [Fact] public void EqualsInvariant_WithSpaces_TrimAndIgnoreCase() => "  Hello  ".EqualsInvariant("hello").ShouldBeTrue();
    /// <summary>
    /// 验证双方均为 null 时 EqualsInvariant 返回 true。
    /// </summary>
    [Fact] public void EqualsInvariant_BothNull_ReturnsTrue() => ((string)null).EqualsInvariant(null).ShouldBeTrue();
    /// <summary>
    /// 验证仅一方为 null 时 EqualsInvariant 返回 false。
    /// </summary>
    [Fact] public void EqualsInvariant_OneNull_ReturnsFalse() => "hello".EqualsInvariant(null).ShouldBeFalse();

    /// <summary>
    /// 验证 EqualsTo 默认忽略大小写比较字符串。
    /// </summary>
    [Fact] public void EqualsTo_IgnoreCaseByDefault_ReturnsTrue() => "Hello".EqualsTo("hello").ShouldBeTrue();

    /// <summary>
    /// 验证 EqualsWithOptions 按选项处理修剪、大小写和 null。
    /// </summary>
    [Fact]
    public void EqualsWithOptions_TrimAndIgnoreCase_ReturnsTrue()
        => "  Hello  ".EqualsWithOptions("hello", trim: true).ShouldBeTrue();
    /// <summary>
    /// 验证 EqualsWithOptions 按选项处理修剪、大小写和 null。
    /// </summary>
    [Fact]
    public void EqualsWithOptions_CaseSensitive_ReturnsFalse()
        => "Hello".EqualsWithOptions("hello", StringComparison.Ordinal).ShouldBeFalse();
    /// <summary>
    /// 验证 EqualsWithOptions 按选项处理修剪、大小写和 null。
    /// </summary>
    [Fact]
    public void EqualsWithOptions_HandleNull_BothNull_ReturnsTrue()
        => ((string)null).EqualsWithOptions(null, handleNull: true).ShouldBeTrue();

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Fill
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证 Fill 会使用参数替换格式占位符。
    /// </summary>
    [Fact] public void Fill_Format_ReplacesPlaceholders() => "Hello {0}!".Fill("World").ShouldBe("Hello World!");
    /// <summary>
    /// 验证 Fill 会替换格式字符串中的多个占位符。
    /// </summary>
    [Fact] public void Fill_MultipleArgs_ReplacesAll() => "{0} + {1} = {2}".Fill(1, 2, 3).ShouldBe("1 + 2 = 3");
    /// <summary>
    /// 验证 Fill 会将 br 标记替换为换行符。
    /// </summary>
    [Fact] public void Fill_WithBrTag_ReplacesWithNewLine() => "a<br>b".Fill().ShouldContain(Environment.NewLine);

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.From (Base64)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证 FromBase64StringToBytes 解码 Base64 并还原原始 UTF-8 字节。
    /// </summary>
    [Fact]
    public void FromBase64StringToBytes_ValidBase64_ReturnsBytes()
    {
        var original = "Hello World";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(original));
        var bytes = base64.FromBase64StringToBytes();
        Encoding.UTF8.GetString(bytes).ShouldBe(original);
    }

    /// <summary>
    /// 验证 FromBase64String 使用指定编码解码 Base64 文本。
    /// </summary>
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

    /// <summary>
    /// 验证 IsUpper 和 IsLower 按字母大小写判断字符串。
    /// </summary>
    [Fact] public void IsUpper_AllUpper_ReturnsTrue() => "HELLO".IsUpper().ShouldBeTrue();
    /// <summary>
    /// 验证 IsUpper 和 IsLower 按字母大小写判断字符串。
    /// </summary>
    [Fact] public void IsUpper_Mixed_ReturnsFalse() => "Hello".IsUpper().ShouldBeFalse();
    /// <summary>
    /// 验证 IsUpper 和 IsLower 按字母大小写判断字符串。
    /// </summary>
    [Fact] public void IsLower_AllLower_ReturnsTrue() => "hello".IsLower().ShouldBeTrue();
    /// <summary>
    /// 验证 IsUpper 和 IsLower 按字母大小写判断字符串。
    /// </summary>
    [Fact] public void IsLower_Mixed_ReturnsFalse() => "Hello".IsLower().ShouldBeFalse();

    /// <summary>
    /// 验证 IsLike 按星号通配模式匹配字符串。
    /// </summary>
    [Fact] public void IsLike_StarPrefix_Matches() => "hello".IsLike("*llo").ShouldBeTrue();
    /// <summary>
    /// 验证 IsLike 按星号通配模式匹配字符串。
    /// </summary>
    [Fact] public void IsLike_StarSuffix_Matches() => "hello".IsLike("h*").ShouldBeTrue();
    /// <summary>
    /// 验证 IsLike 按星号通配模式匹配字符串。
    /// </summary>
    [Fact] public void IsLike_StarOnly_MatchesAnything() => "hello".IsLike("*").ShouldBeTrue();
    /// <summary>
    /// 验证 IsLike 按星号通配模式匹配字符串。
    /// </summary>
    [Fact] public void IsLike_ExactMatch_ReturnsTrue() => "hello".IsLike("hello").ShouldBeTrue();
    /// <summary>
    /// 验证 IsLike 按星号通配模式匹配字符串。
    /// </summary>
    [Fact] public void IsLike_NoMatch_ReturnsFalse() => "hello".IsLike("ha*").ShouldBeFalse();
    /// <summary>
    /// 验证空字符串与空 IsLike 模式匹配。
    /// </summary>
    [Fact] public void IsLike_EmptyPattern_EmptyString_ReturnsTrue() => "".IsLike("").ShouldBeTrue();
    /// <summary>
    /// 验证 IsLikeAny 在任一通配模式匹配时返回 true。
    /// </summary>
    [Fact] public void IsLikeAny_AnyMatch_ReturnsTrue() => "hello".IsLikeAny("h*", "world").ShouldBeTrue();
    /// <summary>
    /// 验证没有通配模式匹配时 IsLikeAny 返回 false。
    /// </summary>
    [Fact] public void IsLikeAny_NoMatch_ReturnsFalse() => "hello".IsLikeAny("a*", "b*").ShouldBeFalse();
    /// <summary>
    /// 验证没有候选模式时 IsLikeAny 返回 false。
    /// </summary>
    [Fact] public void IsLikeAny_EmptyPatterns_ReturnsFalse() => "hello".IsLikeAny().ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Letters
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证 TotalLetters 统计字符串中的字母字符数量。
    /// </summary>
    [Fact] public void TotalLetters_MixedString_ReturnsLetterCount() => "a1b2C3".TotalLetters().ShouldBe(3);
    /// <summary>
    /// 验证空字符串包含零个字母字符。
    /// </summary>
    [Fact] public void TotalLetters_EmptyString_ReturnsZero() => "".TotalLetters().ShouldBe(0);
    /// <summary>
    /// 验证 TotalLowerLetters 统计小写字母数量。
    /// </summary>
    [Fact] public void TotalLowerLetters_ReturnsLowerCount() => "aAbBcC".TotalLowerLetters().ShouldBe(3);
    /// <summary>
    /// 验证 TotalUpperLetters 统计大写字母数量。
    /// </summary>
    [Fact] public void TotalUpperLetters_ReturnsUpperCount() => "aAbBcC".TotalUpperLetters().ShouldBe(3);
    /// <summary>
    /// 验证 IncludeLetters 判断文本是否含字母并遵循最少数量。
    /// </summary>
    [Fact] public void IncludeLetters_HasLetter_ReturnsTrue() => "a1".IncludeLetters().ShouldBeTrue();
    /// <summary>
    /// 验证 IncludeLetters 判断文本是否含字母并遵循最少数量。
    /// </summary>
    [Fact] public void IncludeLetters_EmptyString_ReturnsFalse() => "".IncludeLetters(1).ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Mask
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证 Mask 会遮蔽长字符串的中间部分。
    /// </summary>
    [Fact]
    public void Mask_LongString_MasksMiddle()
    {
        var result = "13812345678".Mask();
        result.ShouldStartWith("138");
        result.ShouldEndWith("5678");
        result.ShouldContain("*");
    }

    /// <summary>
    /// 验证 Mask 会遮蔽短字符串的末尾部分。
    /// </summary>
    [Fact] public void Mask_ShortString_MasksEnd() => "hello".Mask().ShouldStartWith("h");
    /// <summary>
    /// 验证 Mask 接收 null 时返回 null。
    /// </summary>
    [Fact] public void Mask_Null_ReturnsNull() => ((string)null).Mask().ShouldBeNull();
    /// <summary>
    /// 验证 Mask 接收空字符串时返回空字符串。
    /// </summary>
    [Fact] public void Mask_Empty_ReturnsEmpty() => "".Mask().ShouldBe(string.Empty);

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Remove
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证 RemoveFromIgnoreCase 忽略大小写定位并删除匹配内容。
    /// </summary>
    [Fact] public void RemoveFromIgnoreCase_IgnoresCase() => "HelloWorld".RemoveFromIgnoreCase("O").ShouldBe("Hell");
    /// <summary>
    /// 验证 RemoveDuplicateSpaces 将连续普通空格合并为一个空格。
    /// </summary>
    [Fact] public void RemoveDuplicateSpaces_MultipleSP_ReturnsOneSP() => "Hello  World".RemoveDuplicateSpaces().ShouldBe("Hello World");
    /// <summary>
    /// 验证 RemoveAccentsIgnoreCase 去除拉丁字母重音符号。
    /// </summary>
    [Fact] public void RemoveAccentsIgnoreCase_Accents_Removed() => "café".RemoveAccentsIgnoreCase().ShouldBe("cafe");
    /// <summary>
    /// 验证 RemoveAccentsIgnoreCaseAndN 将带波浪号的 n 转换为普通 n。
    /// </summary>
    [Fact] public void RemoveAccentsIgnoreCaseAndN_NTilde_Replaced() => "niño".RemoveAccentsIgnoreCaseAndN().ShouldBe("nino");
    /// <summary>
    /// 验证 RemoveChars 删除指定字符。
    /// </summary>
    [Fact] public void RemoveChars_RemovesSpecifiedChars() => "Hello World".RemoveChars(' ').ShouldBe("HelloWorld");
    /// <summary>
    /// 验证 RemoveWhiteSpace 删除字符串中的空白字符。
    /// </summary>
    [Fact] public void RemoveWhiteSpace_AllSpacesRemoved() => "  Hello  World  ".RemoveWhiteSpace().ShouldBe("HelloWorld");
    /// <summary>
    /// 验证 RemoveDuplicateWhiteSpaces 将空格、制表符和换行归一为单个空格。
    /// </summary>
    [Fact] public void RemoveDuplicateWhiteSpaces_TabsAndSpaces_Normalized() => "Hello\t\nWorld".RemoveDuplicateWhiteSpaces().ShouldBe("Hello World");
    /// <summary>
    /// 验证 RemoveDuplicateChar 删除连续重复的指定字符。
    /// </summary>
    [Fact] public void RemoveDuplicateChar_RemovesDuplicates() => "Mississippi".RemoveDuplicateChar('s').ShouldBe("Misisippi");
    /// <summary>
    /// 验证 RemoveSince 按索引或匹配子串截断字符串。
    /// </summary>
    [Fact] public void RemoveSince_ByIndex_RemovesFromIndex() => "Hello World".RemoveSince(5).ShouldBe("Hello");
    /// <summary>
    /// 验证 RemoveSince 按索引或匹配子串截断字符串。
    /// </summary>
    [Fact] public void RemoveSince_ByString_RemovesFromSubstring() => "Hello World".RemoveSince("World").ShouldBe("Hello ");
    /// <summary>
    /// 验证 RemoveSinceIgnoreCase 忽略大小写查找截断位置。
    /// </summary>
    [Fact] public void RemoveSinceIgnoreCase_IgnoresCase() => "Hello World".RemoveSinceIgnoreCase("world").ShouldBe("Hello ");
    /// <summary>
    /// 验证 RemoveStart 仅在前缀匹配时删除前缀。
    /// </summary>
    [Fact] public void RemoveStart_RemovesPrefix() => "Hello World".RemoveStart("Hello").ShouldBe(" World");
    /// <summary>
    /// 验证前缀不存在时 RemoveStart 保留原字符串。
    /// </summary>
    [Fact] public void RemoveStart_NotPresent_ReturnsOriginal() => "Hello World".RemoveStart("Foo").ShouldBe("Hello World");
    /// <summary>
    /// 验证 RemoveEnd 仅在后缀匹配时删除后缀。
    /// </summary>
    [Fact] public void RemoveEnd_RemovesSuffix() => "Hello World".RemoveEnd("World").ShouldBe("Hello ");
    /// <summary>
    /// 验证后缀不存在时 RemoveEnd 保留原字符串。
    /// </summary>
    [Fact] public void RemoveEnd_NotPresent_ReturnsOriginal() => "Hello World".RemoveEnd("Foo").ShouldBe("Hello World");
    /// <summary>
    /// 验证 Remove 删除字符串中的所有匹配子串。
    /// </summary>
    [Fact] public void Remove_SubString_AllOccurrences() => "Hello Hello World".Remove("Hello ").ShouldBe("World");

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Safe — UrlEncode / UrlDecode
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证 UrlEncode 对特殊字符进行 URL 编码。
    /// </summary>
    [Fact]
    public void UrlEncode_SpecialChars_Encoded()
    {
        var encoded = "Hello World&test=1".UrlEncode();
        encoded.ShouldContain("%");
    }

    /// <summary>
    /// 验证 UrlDecode 将 URL 编码文本还原为原字符串。
    /// </summary>
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

    /// <summary>
    /// 验证 Take 按指定长度截取前缀，长度足够时保留原文本。
    /// </summary>
    [Fact] public void Take_LengthLessThanString_ReturnsTruncated() => "Hello World".Take(5).ShouldBe("Hello");
    /// <summary>
    /// 验证 Take 按指定长度截取前缀，长度足够时保留原文本。
    /// </summary>
    [Fact] public void Take_LengthGreaterThanString_ReturnsOriginal() => "Hello".Take(100).ShouldBe("Hello");

    // ─────────────────────────────────────────────────────────────────
    // Extensions.String.Count
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证 CharacterCount 返回字符串的字符数。
    /// </summary>
    [Fact] public void CharacterCount_AsciiString_ReturnsLength() => "Hello".CharacterCount().ShouldBe(5);
    /// <summary>
    /// 验证 BytesCount 返回 ASCII 字符串的 UTF-8 字节数。
    /// </summary>
    [Fact] public void BytesCount_AsciiString_ReturnsByteLength() => "Hello".BytesCount().ShouldBe(5);
    /// <summary>
    /// 验证 BytesCount 按 UTF-8 编码返回中文字符串的字节数。
    /// </summary>
    [Fact] public void BytesCount_ChineseString_ReturnsByteLength() => "你好".BytesCount().ShouldBeGreaterThan(2);
}
