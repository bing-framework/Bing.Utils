using System;
using System.Collections.Generic;
using Bing.Extensions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// 测试 <see cref="StringExtensions"/> Bases 命名空间下的工具方法
/// （ExtractXxx / Remove / ReverseString / Split / GetTextLength /
///   TrimToMaxLength / PadBoth / EnsureXxx / ConcatWith / Join /
///   GetBefore/Between/After / WordCase / ToPlural / ReplaceAll /
///   ParseCommandlineParams / ParseStringToEnum /
///   RepairZero / ReplaceFirst / ReplaceLast / ReplacePath）
/// </summary>
public class StringExtensionsBasesTests
{
    private readonly ITestOutputHelper _output;

    public StringExtensionsBasesTests(ITestOutputHelper output) => _output = output;

    // ──────────────────────────────────────────
    //  ExtractAround
    // ──────────────────────────────────────────

    [Fact]
    public void ExtractAround_MiddleChar_ReturnsSurrounding()
    {
        // index=2 ('c'), leftLen=1, rightLen=2
        // startIndex=1, length=Min(4, 1-1+2+1)=Min(4,3)=3 → Substring(1,3)="bcd"
        var result = "abcde".ExtractAround(2, 1, 2);
        result.ShouldBe("bcd");
    }

    [Fact]
    public void ExtractAround_StartBoundary_ClampsLeft()
    {
        // index=0, left=3, right=2 → startIndex=0, length=Min(5, 0+2)=2 → "ab"
        var result = "abcde".ExtractAround(0, 3, 2);
        result.ShouldBe("ab");
    }

    // ──────────────────────────────────────────
    //  ExtractLettersNumbers / ExtractNumbers / ExtractLetters / ExtractChinese / FilterChars
    // ──────────────────────────────────────────

    [Fact]
    public void ExtractLettersNumbers_MixedInput_RemovesNonAlphanumeric()
    {
        "abc中文123!".ExtractLettersNumbers().ShouldBe("abc123");
    }

    [Fact]
    public void ExtractNumbers_LettersAndDigits_ReturnsDigitsOnly()
    {
        "abc123def456".ExtractNumbers().ShouldBe("123456");
    }

    [Fact]
    public void ExtractNumbers_NullOrEmpty_ReturnsEmpty()
    {
        "".ExtractNumbers().ShouldBe(string.Empty);
    }

    [Fact]
    public void ExtractLetters_LettersAndDigits_ReturnsLettersOnly()
    {
        "abc123def".ExtractLetters().ShouldBe("abcdef");
    }

    [Fact]
    public void ExtractChinese_MixedInput_ReturnsChineseOnly()
    {
        "abc中文123".ExtractChinese().ShouldBe("中文");
    }

    [Fact]
    public void FilterChars_ByLetter_ReturnsLettersOnly()
    {
        "abc123".FilterChars(char.IsLetter).ShouldBe("abc");
    }

    [Fact]
    public void FilterChars_ByDigit_ReturnsDigitsOnly()
    {
        "abc123".FilterChars(char.IsDigit).ShouldBe("123");
    }

    // ──────────────────────────────────────────
    //  Remove
    // ──────────────────────────────────────────

    [Fact]
    public void Remove_CharArray_RemovesAllOccurrences()
    {
        // Must pass char[] explicitly to avoid conflict with string.Remove(int startIndex)
        "hello".Remove(new char[] { 'l' }).ShouldBe("heo");
    }

    [Fact]
    public void Remove_StringArray_RemovesAllOccurrences()
    {
        "hello world".Remove("world").ShouldBe("hello ");
    }

    [Fact]
    public void Remove_IndexFromLeft_RemovesLeadingChars()
    {
        // Must pass bool to distinguish from string.Remove(int startIndex)
        "hello".Remove(2, true).ShouldBe("llo");
    }

    [Fact]
    public void Remove_IndexFromRight_RemovesTrailingChars()
    {
        "hello".Remove(2, false).ShouldBe("hel");
    }

    [Fact]
    public void RemoveAllSpecialCharacters_MixedInput_ReturnsAlphanumericOnly()
    {
        "hello!@#123".RemoveAllSpecialCharacters().ShouldBe("hello123");
    }

    // ──────────────────────────────────────────
    //  ReverseString
    // ──────────────────────────────────────────

    [Fact]
    public void ReverseString_Normal_ReturnsReversed()
    {
        "hello".ReverseString().ShouldBe("olleh");
    }

    [Fact]
    public void ReverseString_SingleChar_ReturnsSame()
    {
        "a".ReverseString().ShouldBe("a");
    }

    // ──────────────────────────────────────────
    //  Split
    // ──────────────────────────────────────────

    [Fact]
    public void Split_CommaSeparated_ReturnsCorrectParts()
    {
        var parts = "a,b,c".Split(",");
        parts.ShouldBe(new[] { "a", "b", "c" });
    }

    [Fact]
    public void Split_RemoveEmptyEntries_FiltersEmpties()
    {
        var parts = "a,,b,,c".Split(",", true);
        parts.ShouldBe(new[] { "a", "b", "c" });
    }

    [Fact]
    public void Split_KeepEmptyEntries_PreservesEmpties()
    {
        var parts = "a,,b".Split(",", false);
        parts.Length.ShouldBe(3);
        parts[1].ShouldBe(string.Empty);
    }

    // ──────────────────────────────────────────
    //  GetTextLength
    // ──────────────────────────────────────────

    [Fact]
    public void GetTextLength_AsciiOnly_EqualsStringLength()
    {
        "abc".GetTextLength().ShouldBe(3);
    }

    [Fact]
    public void GetTextLength_ChineseChars_CountsAsTwo()
    {
        // "汉字" = 2 Chinese chars, each counts as 2 → 4
        "汉字".GetTextLength().ShouldBe(4);
    }

    [Fact]
    public void GetTextLength_Mixed_CombinesCorrectly()
    {
        // "abc" (3) + "汉字" (4) = 7
        "abc汉字".GetTextLength().ShouldBe(7);
    }

    // ──────────────────────────────────────────
    //  TrimToMaxLength
    // ──────────────────────────────────────────

    [Fact]
    public void TrimToMaxLength_LongerThanMax_Truncates()
    {
        "hello world".TrimToMaxLength(5).ShouldBe("hello");
    }

    [Fact]
    public void TrimToMaxLength_ShorterThanMax_ReturnsOriginal()
    {
        "hi".TrimToMaxLength(10).ShouldBe("hi");
    }

    [Fact]
    public void TrimToMaxLength_WithSuffix_AppendsSuffix()
    {
        // maxLength=5 applies to the base string, then adds suffix
        var result = "hello world".TrimToMaxLength(5, "...");
        result.ShouldBe("hello...");
    }

    [Fact]
    public void TrimToMaxLength_WithSuffix_FitsWithinMax_NoSuffix()
    {
        "hi".TrimToMaxLength(10, "...").ShouldBe("hi");
    }

    // ──────────────────────────────────────────
    //  PadBoth
    // ──────────────────────────────────────────

    [Fact]
    public void PadBoth_EvenPadding_SymmetricPad()
    {
        "abc".PadBoth(7, '-').ShouldBe("--abc--");
    }

    [Fact]
    public void PadBoth_OddPadding_ExtraOnLeft()
    {
        // diff=5, diff/2=2 (integer), width-diff/2=6 → PadLeft(6,'-')="---abc", PadRight(8,'-')="---abc--"
        "abc".PadBoth(8, '-').ShouldBe("---abc--");
    }

    [Fact]
    public void PadBoth_AlreadyFits_ReturnsSame()
    {
        "hello".PadBoth(3, '-').ShouldBe("hello");
    }

    // ──────────────────────────────────────────
    //  EnsureStartsWith / EnsureEndWith
    // ──────────────────────────────────────────

    [Fact]
    public void EnsureStartsWith_NotStarting_PrependPrefix()
    {
        "world".EnsureStartsWith("hello ").ShouldBe("hello world");
    }

    [Fact]
    public void EnsureStartsWith_AlreadyStarting_NoPrepend()
    {
        "hello world".EnsureStartsWith("hello").ShouldBe("hello world");
    }

    [Fact]
    public void EnsureEndWith_NotEnding_AppendSuffix()
    {
        "hello".EnsureEndWith("!").ShouldBe("hello!");
    }

    [Fact]
    public void EnsureEndWith_AlreadyEnding_NoAppend()
    {
        "hello!".EnsureEndWith("!").ShouldBe("hello!");
    }

    // ──────────────────────────────────────────
    //  ConcatWith
    // ──────────────────────────────────────────

    [Fact]
    public void ConcatWith_TwoValues_ConcatsWithSeparator()
    {
        "hello".ConcatWith(" ", "world").ShouldBe("hello world");
    }

    [Fact]
    public void ConcatWith_MultipleValues_ConcatsAll()
    {
        // ConcatWith(params string[] values) — no separator; just concatenates all
        "a".ConcatWith("-", "b", "c").ShouldBe("a-bc");
    }

    // ──────────────────────────────────────────
    //  JoinNotNullOrEmpty
    // ──────────────────────────────────────────

    [Fact]
    public void JoinNotNullOrEmpty_FiltersNullAndEmpty()
    {
        var arr = new[] { "a", "", null, "b" };
        arr.JoinNotNullOrEmpty(",").ShouldBe("a,b");
    }

    [Fact]
    public void JoinNotNullOrEmpty_AllEmpty_ReturnsEmpty()
    {
        new[] { "", null, "" }.JoinNotNullOrEmpty(",").ShouldBe(string.Empty);
    }

    // ──────────────────────────────────────────
    //  GetBefore / GetBetween / GetAfter / SubstringFrom
    // ──────────────────────────────────────────

    [Fact]
    public void GetBefore_DelimiterPresent_ReturnsBeforeText()
    {
        "before[middle]after".GetBefore("[").ShouldBe("before");
    }

    [Fact]
    public void GetBefore_NoDelimiter_ReturnsEmpty()
    {
        "hello".GetBefore("[").ShouldBe(string.Empty);
    }

    [Fact]
    public void GetBetween_DelimitersPresent_ReturnsMiddleText()
    {
        "before[middle]after".GetBetween("[", "]").ShouldBe("middle");
    }

    [Fact]
    public void GetAfter_DelimiterPresent_ReturnsAfterText()
    {
        "before[middle]after".GetAfter("]").ShouldBe("after");
    }

    [Fact]
    public void GetAfter_NoDelimiter_ReturnsEmpty()
    {
        "hello".GetAfter("]").ShouldBe(string.Empty);
    }

    [Fact]
    public void SubstringFrom_ValidIndex_ReturnsSubstring()
    {
        // SubstringFrom(int index) — index 6 is the 'w' of "world"
        "hello world".SubstringFrom(6).ShouldBe("world");
    }

    // ──────────────────────────────────────────
    //  ToUpperFirstLetter / ToLowerFirstLetter
    // ──────────────────────────────────────────

    [Fact]
    public void ToUpperFirstLetter_LowerStart_CapitalizesFirst()
    {
        "hello".ToUpperFirstLetter().ShouldBe("Hello");
    }

    [Fact]
    public void ToUpperFirstLetter_AlreadyUpper_NoChange()
    {
        "Hello".ToUpperFirstLetter().ShouldBe("Hello");
    }

    [Fact]
    public void ToLowerFirstLetter_UpperStart_LowercasesFirst()
    {
        "Hello".ToLowerFirstLetter().ShouldBe("hello");
    }

    [Fact]
    public void ToLowerFirstLetter_AlreadyLower_NoChange()
    {
        "hello".ToLowerFirstLetter().ShouldBe("hello");
    }

    // ──────────────────────────────────────────
    //  ToTitleCase
    // ──────────────────────────────────────────

    [Fact]
    public void ToTitleCase_AllLower_CapitalizesEachWord()
    {
        "hello world".ToTitleCase().ShouldBe("Hello World");
    }

    [Fact]
    public void ToTitleCase_MixedCase_CapitalizesEachWord()
    {
        "hello world foo".ToTitleCase().ShouldBe("Hello World Foo");
    }

    // ──────────────────────────────────────────
    //  ToPlural (English pluralization)
    // ──────────────────────────────────────────

    [Fact]
    public void ToPlural_SimpleWord_AddsS()
    {
        "cat".ToPlural().ShouldBe("cats");
    }

    [Fact]
    public void ToPlural_EndsWithCh_AddsEs()
    {
        "church".ToPlural().ShouldBe("churches");
    }

    [Fact]
    public void ToPlural_EndsWithSh_AddsEs()
    {
        "brush".ToPlural().ShouldBe("brushes");
    }

    [Fact]
    public void ToPlural_EndsWithY_ReplacesWithIes()
    {
        "lady".ToPlural().ShouldBe("ladies");
    }

    // ──────────────────────────────────────────
    //  ReplaceAll
    // ──────────────────────────────────────────

    [Fact]
    public void ReplaceAll_StringList_ReplacesAllMatches()
    {
        "White Black Gray".ReplaceAll(new[] { "White", "Black" }, "[C]").ShouldBe("[C] [C] Gray");
    }

    [Fact]
    public void ReplaceAll_FuncOverload_TransformsMatches()
    {
        var result = "White Black".ReplaceAll(new[] { "White", "Black" }, v => $"[{v}]");
        result.ShouldBe("[White] [Black]");
    }

    [Fact]
    public void ReplaceAll_PairEnumerables_ReplacesEachPair()
    {
        // replaces "a"→"X", "b"→"Y"
        var result = "a-b-c".ReplaceAll(new[] { "a", "b" }, new[] { "X", "Y" });
        result.ShouldBe("X-Y-c");
    }

    // ──────────────────────────────────────────
    //  ParseCommandlineParams
    // ──────────────────────────────────────────

    [Fact]
    public void ParseCommandlineParams_KeyValuePairs_ReturnsDictionary()
    {
        // ParseCommandlineParams extends string[], not string
        var args = new[] { "/key1=val1", "/key2=val2" };
        var d = args.ParseCommandlineParams();
        d["key1"].ShouldBe("val1");
        d["key2"].ShouldBe("val2");
    }

    // ──────────────────────────────────────────
    //  ParseStringToEnum
    // ──────────────────────────────────────────

    [Fact]
    public void ParseStringToEnum_ValidName_ReturnsEnum()
    {
        var result = "Monday".ParseStringToEnum<DayOfWeek>();
        result.ShouldBe(DayOfWeek.Monday);
    }

    [Fact]
    public void ParseStringToEnum_InvalidName_ReturnsDefault()
    {
        var result = "NotADay".ParseStringToEnum<DayOfWeek>();
        result.ShouldBe(default(DayOfWeek));
    }

    // ──────────────────────────────────────────
    //  EncodeEmailAddress
    // ──────────────────────────────────────────

    [Fact]
    public void EncodeEmailAddress_ValidEmail_EncodesCharacters()
    {
        var result = "test@example.com".EncodeEmailAddress();
        // Result should contain HTML entities for @, ., etc.
        result.ShouldNotBeNullOrWhiteSpace();
        result.ShouldNotBe("test@example.com"); // should be encoded
    }

    // ──────────────────────────────────────────
    //  RepairZero
    // ──────────────────────────────────────────

    [Fact]
    public void RepairZero_ShortNumber_PadsWithZeros()
    {
        "123".RepairZero(6).ShouldBe("000123");
    }

    [Fact]
    public void RepairZero_ExactLength_NoChange()
    {
        "123456".RepairZero(6).ShouldBe("123456");
    }

    [Fact]
    public void RepairZero_LongerThanWidth_ReturnsOriginal()
    {
        "1234567".RepairZero(6).ShouldBe("1234567");
    }

    // ──────────────────────────────────────────
    //  ReplaceFirst / ReplaceLast
    // ──────────────────────────────────────────

    [Fact]
    public void ReplaceFirst_StringOverload_ReplacesOnlyFirst()
    {
        "aaa".ReplaceFirst("a", "b").ShouldBe("baa");
    }

    [Fact]
    public void ReplaceFirst_NumberOverload_ReplacesFirstNOccurrences()
    {
        // ReplaceFirst(1, "a", "b") replaces the first 1 occurrence
        "aaaa".ReplaceFirst(1, "a", "b").ShouldBe("baaa");
    }

    [Fact]
    public void ReplaceLast_StringOverload_ReplacesOnlyLast()
    {
        "aaa".ReplaceLast("a", "b").ShouldBe("aab");
    }

    [Fact]
    public void ReplaceLast_NumberOverload_ReplacesLastNOccurrences()
    {
        // ReplaceLast(1, "a", "b") — replaces last 1 occurrence
        "aaaa".ReplaceLast(1, "a", "b").ShouldBe("aaab");
    }

    // ──────────────────────────────────────────
    //  ReplacePath
    // ──────────────────────────────────────────

    [Fact]
    public void ReplacePath_MixedSlashes_NormalizesToPlatformSlash()
    {
        var path = "a/b\\c/d";
        var result = path.ReplacePath();
        // On Windows → backslash; on Linux/Mac → forward slash
        var expected = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows)
            ? "a\\b\\c\\d"
            : "a/b/c/d";
        result.ShouldBe(expected);
    }
}

/// <summary>
/// 测试 <see cref="StringExtensions"/> 中的 Convert / Validation 部分
/// (ToXDocument / ToXElement / HexStringToBytes / ToUnicodeString /
///  ToSecureString / EncodeBase64 / DecodeBase64 /
///  IsRangeLength / Contains / ContainsEquivalenceTo / ContainsAny / ContainsAll)
/// </summary>
public class StringExtensionsConvertValidationTests
{
    // ──────────────────────────────────────────
    //  ToXDocument / ToXElement
    // ──────────────────────────────────────────

    [Fact]
    public void ToXDocument_ValidXml_ReturnsXDocument()
    {
        var doc = "<root><child/></root>".ToXDocument();
        doc.ShouldNotBeNull();
        doc.Root!.Name.LocalName.ShouldBe("root");
    }

    [Fact]
    public void ToXElement_ValidXml_ReturnsXElement()
    {
        var el = "<item id=\"1\"/>".ToXElement();
        el.ShouldNotBeNull();
        el!.Attribute("id")!.Value.ShouldBe("1");
    }

    // ──────────────────────────────────────────
    //  HexStringToBytes
    // ──────────────────────────────────────────

    [Fact]
    public void HexStringToBytes_ValidHex_ReturnsByteArray()
    {
        // "41" = 65 = 'A', "42" = 66 = 'B'
        var bytes = "4142".HexStringToBytes();
        bytes.ShouldBe(new byte[] { 0x41, 0x42 });
    }

    [Fact]
    public void HexStringToBytes_WithSpaces_StripsSpaces()
    {
        var bytes = "41 42".HexStringToBytes();
        bytes.ShouldBe(new byte[] { 0x41, 0x42 });
    }

    [Fact]
    public void HexStringToBytes_EmptyString_ReturnsEmptyArray()
    {
        var bytes = "".HexStringToBytes();
        bytes.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────
    //  ToUnicodeString
    // ──────────────────────────────────────────

    [Fact]
    public void ToUnicodeString_SingleChar_ReturnsEscapeSequence()
    {
        // Uses ((int)t).ToString("x") without padding — 'A'=0x41 → "\u41"
        "A".ToUnicodeString().ShouldBe(@"\u41");
    }

    [Fact]
    public void ToUnicodeString_MultiChar_ReturnsAllEscaped()
    {
        var result = "AB".ToUnicodeString();
        result.ShouldBe(@"\u41\u42");
    }

    // ──────────────────────────────────────────
    //  ToSecureString / ToUnSecureString (round-trip)
    // ──────────────────────────────────────────

    [Fact]
    public void ToSecureString_ValidString_ReturnsSecureString()
    {
        var secure = "secret".ToSecureString();
        secure.ShouldNotBeNull();
        secure!.Length.ShouldBe(6);
    }

    [Fact]
    public void ToSecureString_EmptyString_ReturnsNull()
    {
        "".ToSecureString().ShouldBeNull();
    }

    [Fact]
    public void ToSecureString_RoundTrip_MatchesOriginal()
    {
        var original = "my_password";
        var secure = original.ToSecureString(markReadOnly: false);
        var plain = secure!.ToUnSecureString();
        plain.ShouldBe(original);
    }

    // ──────────────────────────────────────────
    //  EncodeBase64 / DecodeBase64
    // ──────────────────────────────────────────

    [Fact]
    public void EncodeBase64_RoundTrip_MatchesOriginal()
    {
        var original = "Hello, World!";
        var encoded = original.EncodeBase64();
        var decoded = encoded.DecodeBase64();
        decoded.ShouldBe(original);
    }

    [Fact]
    public void EncodeBase64_SimpleString_ProducesBase64()
    {
        // "Man" → "TWFu" in standard Base64
        "Man".EncodeBase64().ShouldBe("TWFu");
    }

    // ──────────────────────────────────────────
    //  IsRangeLength
    // ──────────────────────────────────────────

    [Fact]
    public void IsRangeLength_WithinRange_ReturnsTrue()
    {
        "hello".IsRangeLength(3, 10).ShouldBeTrue();
    }

    [Fact]
    public void IsRangeLength_TooShort_ReturnsFalse()
    {
        "hi".IsRangeLength(3, 10).ShouldBeFalse();
    }

    [Fact]
    public void IsRangeLength_TooLong_ReturnsFalse()
    {
        "hello world".IsRangeLength(3, 8).ShouldBeFalse();
    }

    [Fact]
    public void IsRangeLength_ExactMin_ReturnsTrue()
    {
        "abc".IsRangeLength(3, 10).ShouldBeTrue();
    }

    [Fact]
    public void IsRangeLength_ExactMax_ReturnsTrue()
    {
        "abcdefghij".IsRangeLength(3, 10).ShouldBeTrue();
    }

    // ──────────────────────────────────────────
    //  Contains (StringComparison)
    // ──────────────────────────────────────────

    [Fact]
    public void Contains_CaseInsensitive_FindsMatch()
    {
        "Hello World".Contains("hello", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public void Contains_CaseSensitive_NoMatchOnDifferentCase()
    {
        "Hello World".Contains("hello", StringComparison.Ordinal).ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  ContainsEquivalenceTo
    // ──────────────────────────────────────────

    [Fact]
    public void ContainsEquivalenceTo_IgnoresCase_ReturnsTrue()
    {
        "Hello World".ContainsEquivalenceTo("HELLO").ShouldBeTrue();
    }

    [Fact]
    public void ContainsEquivalenceTo_NotPresent_ReturnsFalse()
    {
        "Hello World".ContainsEquivalenceTo("MISSING").ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  ContainsAny / ContainsAll
    // ──────────────────────────────────────────

    [Fact]
    public void ContainsAny_OneMatchAmongMany_ReturnsTrue()
    {
        "hello world".ContainsAny("xyz", "world").ShouldBeTrue();
    }

    [Fact]
    public void ContainsAny_NoMatchAtAll_ReturnsFalse()
    {
        "hello world".ContainsAny("xyz", "abc").ShouldBeFalse();
    }

    [Fact]
    public void ContainsAny_CaseInsensitive_FindsMatch()
    {
        "hello world".ContainsAny(StringComparison.OrdinalIgnoreCase, "HELLO").ShouldBeTrue();
    }

    [Fact]
    public void ContainsAll_AllPresent_ReturnsTrue()
    {
        "hello world".ContainsAll("hello", "world").ShouldBeTrue();
    }

    [Fact]
    public void ContainsAll_OneMissing_ReturnsFalse()
    {
        "hello world".ContainsAll("hello", "xyz").ShouldBeFalse();
    }
}
