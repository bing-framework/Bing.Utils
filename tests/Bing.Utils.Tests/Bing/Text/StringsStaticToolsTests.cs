using Bing.Text;

namespace Bing.Utils.Tests.Bing.Text;

/// <summary>
/// 验证 Strings 静态工具的字符串包含、字符分类、截取和替换行为。
/// </summary>
[Trait("Bing.Text", "StringsStaticTools")]
public class StringsStaticToolsTests
{
    #region Strings.Contains
    /// <summary>
    /// 验证 Strings.Contains 按字符串或字符候选项判断是否存在匹配项。
    /// </summary>

    [Fact]
    public void Strings_Contains_String_Found_ReturnsTrue()
    {
        Strings.Contains("Hello World", "World", "xyz").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 Strings.Contains 按字符串或字符候选项判断是否存在匹配项。
    /// </summary>

    [Fact]
    public void Strings_Contains_String_NullText_ReturnsFalse()
    {
        Strings.Contains(null, "hello").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 Strings.Contains 按字符串或字符候选项判断是否存在匹配项。
    /// </summary>

    [Fact]
    public void Strings_Contains_String_NotFound_ReturnsFalse()
    {
        Strings.Contains("Hello World", "abc", "xyz").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 Strings.Contains 按字符串或字符候选项判断是否存在匹配项。
    /// </summary>

    [Fact]
    public void Strings_Contains_Char_Found_ReturnsTrue()
    {
        Strings.Contains("Hello", 'e').ShouldBeTrue();
    }
    /// <summary>
    /// 验证 Strings.Contains 按字符串或字符候选项判断是否存在匹配项。
    /// </summary>

    [Fact]
    public void Strings_Contains_Char_NotFound_ReturnsFalse()
    {
        Strings.Contains("Hello", 'z').ShouldBeFalse();
    }
    /// <summary>
    /// 验证 Strings.Contains 按字符串或字符候选项判断是否存在匹配项。
    /// </summary>

    [Fact]
    public void Strings_Contains_MultipleChars_OneFound_ReturnsTrue()
    {
        Strings.Contains("Hello", 'x', 'e').ShouldBeTrue();
    }
    /// <summary>
    /// 验证 ContainsIgnoreCase 与 IgnoreCase 参数控制候选项的大小写匹配。
    /// </summary>

    [Fact]
    public void Strings_ContainsIgnoreCase_String_Found_ReturnsTrue()
    {
        Strings.ContainsIgnoreCase("Hello World", "hello").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 ContainsIgnoreCase 与 IgnoreCase 参数控制候选项的大小写匹配。
    /// </summary>

    [Fact]
    public void Strings_ContainsIgnoreCase_Char_Found_ReturnsTrue()
    {
        Strings.ContainsIgnoreCase("Hello", 'E').ShouldBeTrue();
    }
    /// <summary>
    /// 验证 ContainsIgnoreCase 与 IgnoreCase 参数控制候选项的大小写匹配。
    /// </summary>

    [Fact]
    public void Strings_ContainsIgnoreCase_MultipleChars_Found_ReturnsTrue()
    {
        Strings.ContainsIgnoreCase("Hello", 'E', 'O').ShouldBeTrue();
    }
    /// <summary>
    /// 验证 ContainsIgnoreCase 与 IgnoreCase 参数控制候选项的大小写匹配。
    /// </summary>

    [Fact]
    public void Strings_Contains_WithIgnoreCaseTrue_ReturnsTrue()
    {
        Strings.Contains("Hello World", new[] { "hello" }, IgnoreCase.True).ShouldBeTrue();
    }
    /// <summary>
    /// 验证 ContainsIgnoreCase 与 IgnoreCase 参数控制候选项的大小写匹配。
    /// </summary>

    [Fact]
    public void Strings_Contains_WithIgnoreCaseFalse_ReturnsFalse()
    {
        Strings.Contains("Hello World", new[] { "hello" }, IgnoreCase.False).ShouldBeFalse();
    }
    /// <summary>
    /// 验证 ContainsChinese 识别中文字符并处理不含中文或空输入。
    /// </summary>

    [Fact]
    public void Strings_ContainsChinese_WithChineseChars_ReturnsTrue()
    {
        Strings.ContainsChinese("Hello 你好").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 ContainsChinese 识别中文字符并处理不含中文或空输入。
    /// </summary>

    [Fact]
    public void Strings_ContainsChinese_NoChineseChars_ReturnsFalse()
    {
        Strings.ContainsChinese("Hello World").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 ContainsChinese 识别中文字符并处理不含中文或空输入。
    /// </summary>

    [Fact]
    public void Strings_ContainsChinese_NullOrWhitespace_ReturnsFalse()
    {
        Strings.ContainsChinese(null).ShouldBeFalse();
        Strings.ContainsChinese("").ShouldBeFalse();
    }

    #endregion

    #region Strings.Is
    /// <summary>
    /// 验证 IsUpper 和 IsLower 根据字母大小写判断字符串。
    /// </summary>

    [Fact]
    public void Strings_IsUpper_AllUpperLetters_ReturnsTrue()
    {
        Strings.IsUpper("HELLO").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsUpper 和 IsLower 根据字母大小写判断字符串。
    /// </summary>

    [Fact]
    public void Strings_IsUpper_MixedCase_ReturnsFalse()
    {
        Strings.IsUpper("Hello").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsUpper 和 IsLower 根据字母大小写判断字符串。
    /// </summary>

    [Fact]
    public void Strings_IsLower_AllLowerLetters_ReturnsTrue()
    {
        Strings.IsLower("hello").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsUpper 和 IsLower 根据字母大小写判断字符串。
    /// </summary>

    [Fact]
    public void Strings_IsLower_MixedCase_ReturnsFalse()
    {
        Strings.IsLower("Hello").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsChinese(char) 区分中文字符和非中文字符。
    /// </summary>

    [Fact]
    public void Strings_IsChinese_Char_ChineseChar_ReturnsTrue()
    {
        Strings.IsChinese('好').ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsChinese(char) 区分中文字符和非中文字符。
    /// </summary>

    [Fact]
    public void Strings_IsChinese_Char_NonChinese_ReturnsFalse()
    {
        Strings.IsChinese('A').ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsChinese(string) 判断字符串是否全由中文字符组成，并处理混合或空输入。
    /// </summary>

    [Fact]
    public void Strings_IsChinese_String_AllChinese_ReturnsTrue()
    {
        Strings.IsChinese("你好世界").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsChinese(string) 判断字符串是否全由中文字符组成，并处理混合或空输入。
    /// </summary>

    [Fact]
    public void Strings_IsChinese_String_Mixed_ReturnsFalse()
    {
        Strings.IsChinese("Hello你好").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsChinese(string) 判断字符串是否全由中文字符组成，并处理混合或空输入。
    /// </summary>

    [Fact]
    public void Strings_IsChinese_String_NullOrWhitespace_ReturnsFalse()
    {
        Strings.IsChinese((string)null).ShouldBeFalse();
        Strings.IsChinese("").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsAllUpperCase 要求所有字符均为大写字母。
    /// </summary>

    [Fact]
    public void Strings_IsAllUpperCase_OnlyLetters_ReturnsTrue()
    {
        Strings.IsAllUpperCase("HELLO").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsAllUpperCase 要求所有字符均为大写字母。
    /// </summary>

    [Fact]
    public void Strings_IsAllUpperCase_WithDigit_ReturnsFalse()
    {
        Strings.IsAllUpperCase("HELLO1").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsAllLowerCase 要求所有字符均为小写字母。
    /// </summary>

    [Fact]
    public void Strings_IsAllLowerCase_OnlyLetters_ReturnsTrue()
    {
        Strings.IsAllLowerCase("hello").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsAllLowerCase 要求所有字符均为小写字母。
    /// </summary>

    [Fact]
    public void Strings_IsAllLowerCase_MixedCase_ReturnsFalse()
    {
        Strings.IsAllLowerCase("Hello").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsAllLetters 仅接受由字母组成的字符串。
    /// </summary>

    [Fact]
    public void Strings_IsAllLetters_OnlyLetters_ReturnsTrue()
    {
        Strings.IsAllLetters("HelloWorld").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsAllLetters 仅接受由字母组成的字符串。
    /// </summary>

    [Fact]
    public void Strings_IsAllLetters_WithDigit_ReturnsFalse()
    {
        Strings.IsAllLetters("Hello1").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsAllDigits 仅接受由数字组成的字符串。
    /// </summary>

    [Fact]
    public void Strings_IsAllDigits_OnlyDigits_ReturnsTrue()
    {
        Strings.IsAllDigits("12345").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsAllDigits 仅接受由数字组成的字符串。
    /// </summary>

    [Fact]
    public void Strings_IsAllDigits_WithLetter_ReturnsFalse()
    {
        Strings.IsAllDigits("123a5").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsAlphanumeric 仅接受由字母和数字组成的字符串。
    /// </summary>

    [Fact]
    public void Strings_IsAlphanumeric_LettersAndDigits_ReturnsTrue()
    {
        Strings.IsAlphanumeric("Hello123").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsAlphanumeric 仅接受由字母和数字组成的字符串。
    /// </summary>

    [Fact]
    public void Strings_IsAlphanumeric_WithSpecialChar_ReturnsFalse()
    {
        Strings.IsAlphanumeric("Hello!").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsAsciiLetters 仅接受 ASCII 字母。
    /// </summary>

    [Fact]
    public void Strings_IsAsciiLetters_AsciiOnly_ReturnsTrue()
    {
        Strings.IsAsciiLetters("Hello").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsAsciiLetters 仅接受 ASCII 字母。
    /// </summary>

    [Fact]
    public void Strings_IsAsciiLetters_WithDigit_ReturnsFalse()
    {
        Strings.IsAsciiLetters("Hello1").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsAsciiDigits 仅接受 ASCII 数字。
    /// </summary>

    [Fact]
    public void Strings_IsAsciiDigits_DigitsOnly_ReturnsTrue()
    {
        Strings.IsAsciiDigits("12345").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsAsciiDigits 仅接受 ASCII 数字。
    /// </summary>

    [Fact]
    public void Strings_IsAsciiDigits_WithLetter_ReturnsFalse()
    {
        Strings.IsAsciiDigits("123a").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsAsciiAlphanumeric 仅接受 ASCII 字母和数字。
    /// </summary>

    [Fact]
    public void Strings_IsAsciiAlphanumeric_Mixed_ReturnsTrue()
    {
        Strings.IsAsciiAlphanumeric("Hello123").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsAsciiAlphanumeric 仅接受 ASCII 字母和数字。
    /// </summary>

    [Fact]
    public void Strings_IsAsciiAlphanumeric_WithSpecialChar_ReturnsFalse()
    {
        Strings.IsAsciiAlphanumeric("Hello!").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsValidIdentifier 识别合法标识符并拒绝数字开头或含特殊字符的内容。
    /// </summary>

    [Fact]
    public void Strings_IsValidIdentifier_ValidIdentifier_ReturnsTrue()
    {
        Strings.IsValidIdentifier("_myVar123").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsValidIdentifier 识别合法标识符并拒绝数字开头或含特殊字符的内容。
    /// </summary>

    [Fact]
    public void Strings_IsValidIdentifier_StartsWithDigit_ReturnsFalse()
    {
        Strings.IsValidIdentifier("1var").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsValidIdentifier 识别合法标识符并拒绝数字开头或含特殊字符的内容。
    /// </summary>

    [Fact]
    public void Strings_IsValidIdentifier_WithSpecialChar_ReturnsFalse()
    {
        Strings.IsValidIdentifier("my-var").ShouldBeFalse();
    }

    #endregion

    #region StringsShortcutExtensions (Is.*NullOrEmpty / WhiteSpace)
    /// <summary>
    /// 验证 IsNullOrEmpty 和 IsNotNullNorEmpty 对 null、空和非空字符串的判断。
    /// </summary>

    [Fact]
    public void IsNullOrEmpty_NullString_ReturnsTrue()
    {
        ((string)null).IsNullOrEmpty().ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsNullOrEmpty 和 IsNotNullNorEmpty 对 null、空和非空字符串的判断。
    /// </summary>

    [Fact]
    public void IsNullOrEmpty_EmptyString_ReturnsTrue()
    {
        "".IsNullOrEmpty().ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsNullOrEmpty 和 IsNotNullNorEmpty 对 null、空和非空字符串的判断。
    /// </summary>

    [Fact]
    public void IsNullOrEmpty_NonEmpty_ReturnsFalse()
    {
        "hello".IsNullOrEmpty().ShouldBeFalse();
    }
    /// <summary>
    /// 验证 IsNullOrEmpty 和 IsNotNullNorEmpty 对 null、空和非空字符串的判断。
    /// </summary>

    [Fact]
    public void IsNotNullNorEmpty_NonEmpty_ReturnsTrue()
    {
        "hello".IsNotNullNorEmpty().ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsNullOrWhiteSpace 和 IsNotNullNorWhiteSpace 对空白文本的判断。
    /// </summary>

    [Fact]
    public void IsNullOrWhiteSpace_Whitespace_ReturnsTrue()
    {
        "   ".IsNullOrWhiteSpace().ShouldBeTrue();
    }
    /// <summary>
    /// 验证 IsNullOrWhiteSpace 和 IsNotNullNorWhiteSpace 对空白文本的判断。
    /// </summary>

    [Fact]
    public void IsNotNullNorWhiteSpace_NonWhitespace_ReturnsTrue()
    {
        "hello".IsNotNullNorWhiteSpace().ShouldBeTrue();
    }

    #endregion

    #region Strings.Has
    /// <summary>
    /// 验证 HasNumbers 判断数字是否存在，并可要求达到最少数量。
    /// </summary>

    [Fact]
    public void Strings_HasNumbers_WithDigit_ReturnsTrue()
    {
        Strings.HasNumbers("abc1def").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 HasNumbers 判断数字是否存在，并可要求达到最少数量。
    /// </summary>

    [Fact]
    public void Strings_HasNumbers_NoDigit_ReturnsFalse()
    {
        Strings.HasNumbers("abcdef").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 HasNumbers 判断数字是否存在，并可要求达到最少数量。
    /// </summary>

    [Fact]
    public void Strings_HasNumbersAtLeast_EnoughDigits_ReturnsTrue()
    {
        Strings.HasNumbersAtLeast("abc123", 3).ShouldBeTrue();
    }
    /// <summary>
    /// 验证 HasNumbers 判断数字是否存在，并可要求达到最少数量。
    /// </summary>

    [Fact]
    public void Strings_HasNumbersAtLeast_NotEnoughDigits_ReturnsFalse()
    {
        Strings.HasNumbersAtLeast("abc1", 3).ShouldBeFalse();
    }
    /// <summary>
    /// 验证 HasLetters 判断字母是否存在，并可要求达到最少数量。
    /// </summary>

    [Fact]
    public void Strings_HasLetters_WithLetter_ReturnsTrue()
    {
        Strings.HasLetters("123a456").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 HasLetters 判断字母是否存在，并可要求达到最少数量。
    /// </summary>

    [Fact]
    public void Strings_HasLetters_NoLetter_ReturnsFalse()
    {
        Strings.HasLetters("12345").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 HasLetters 判断字母是否存在，并可要求达到最少数量。
    /// </summary>

    [Fact]
    public void Strings_HasLettersAtLeast_EnoughLetters_ReturnsTrue()
    {
        Strings.HasLettersAtLeast("abc123", 3).ShouldBeTrue();
    }
    /// <summary>
    /// 验证 HasLetters 判断字母是否存在，并可要求达到最少数量。
    /// </summary>

    [Fact]
    public void Strings_HasLettersAtLeast_NotEnoughLetters_ReturnsFalse()
    {
        Strings.HasLettersAtLeast("a1234", 3).ShouldBeFalse();
    }

    #endregion

    #region Strings.Take
    /// <summary>
    /// 验证 Take 返回指定长度的前缀，或在文本较短时返回全部内容。
    /// </summary>

    [Fact]
    public void Take_LongerThanLength_ReturnsPrefix()
    {
        Strings.Take("Hello World", 5).ShouldBe("Hello");
    }
    /// <summary>
    /// 验证 Take 返回指定长度的前缀，或在文本较短时返回全部内容。
    /// </summary>

    [Fact]
    public void Take_ShorterThanLength_ReturnsAll()
    {
        Strings.Take("Hi", 10).ShouldBe("Hi");
    }
    /// <summary>
    /// 验证 Take 对 null 和空字符串返回空字符串。
    /// </summary>

    [Fact]
    public void Take_NullOrWhitespace_ReturnsEmpty()
    {
        Strings.Take(null, 5).ShouldBe(string.Empty);
        Strings.Take("", 5).ShouldBe(string.Empty);
    }

    #endregion

    #region Strings.Substring
    /// <summary>
    /// 验证 Substring 按起止索引提取片段，并处理负索引、倒序、相同索引和 null。
    /// </summary>

    [Fact]
    public void Substring_PositiveIndices_ReturnsRange()
    {
        Strings.Substring("Hello World", 6, 11).ShouldBe("World");
    }
    /// <summary>
    /// 验证 Substring 按起止索引提取片段，并处理负索引、倒序、相同索引和 null。
    /// </summary>

    [Fact]
    public void Substring_NegativeFromIndex_CountsFromEnd()
    {
        Strings.Substring("Hello World", -5, 11).ShouldBe("World");
    }
    /// <summary>
    /// 验证 Substring 按起止索引提取片段，并处理负索引、倒序、相同索引和 null。
    /// </summary>

    [Fact]
    public void Substring_SwappedIndices_AutoSwaps()
    {
        Strings.Substring("Hello World", 11, 6).ShouldBe("World");
    }
    /// <summary>
    /// 验证 Substring 按起止索引提取片段，并处理负索引、倒序、相同索引和 null。
    /// </summary>

    [Fact]
    public void Substring_SameIndices_ReturnsEmpty()
    {
        Strings.Substring("Hello", 2, 2).ShouldBe(string.Empty);
    }
    /// <summary>
    /// 验证 Substring 按起止索引提取片段，并处理负索引、倒序、相同索引和 null。
    /// </summary>

    [Fact]
    public void Substring_NullInput_ReturnsEmpty()
    {
        Strings.Substring(null, 0, 5).ShouldBe(string.Empty);
    }
    /// <summary>
    /// 验证 SubstringWithLength 按起始索引和长度提取片段。
    /// </summary>

    [Fact]
    public void SubstringWithLength_PositiveLength_ReturnsSubstring()
    {
        Strings.SubstringWithLength("Hello World", 6, 5).ShouldBe("World");
    }

    #endregion

    #region Strings.SubstringBefore / SubstringAfter / SubstringBetween
    /// <summary>
    /// 验证 SubstringBefore 返回首个或最后一个分隔符之前的内容，并处理缺失、起始和字符分隔符。
    /// </summary>

    [Fact]
    public void SubstringBefore_FirstSeparator_ReturnsPrefix()
    {
        Strings.SubstringBefore("abcba", "b", false).ShouldBe("a");
    }
    /// <summary>
    /// 验证 SubstringBefore 返回首个或最后一个分隔符之前的内容，并处理缺失、起始和字符分隔符。
    /// </summary>

    [Fact]
    public void SubstringBefore_LastSeparator_ReturnsBeforeLast()
    {
        Strings.SubstringBefore("abcba", "b", true).ShouldBe("abc");
    }
    /// <summary>
    /// 验证 SubstringBefore 返回首个或最后一个分隔符之前的内容，并处理缺失、起始和字符分隔符。
    /// </summary>

    [Fact]
    public void SubstringBefore_SeparatorNotFound_ReturnsWhole()
    {
        Strings.SubstringBefore("abc", "d", false).ShouldBe("abc");
    }
    /// <summary>
    /// 验证 SubstringBefore 返回首个或最后一个分隔符之前的内容，并处理缺失、起始和字符分隔符。
    /// </summary>

    [Fact]
    public void SubstringBefore_SeparatorAtStart_ReturnsEmpty()
    {
        Strings.SubstringBefore("abc", "a", false).ShouldBe(string.Empty);
    }
    /// <summary>
    /// 验证 SubstringBefore 返回首个或最后一个分隔符之前的内容，并处理缺失、起始和字符分隔符。
    /// </summary>

    [Fact]
    public void SubstringBefore_Char_FirstSeparator_ReturnsPrefix()
    {
        Strings.SubstringBefore("abcba", 'b', false).ShouldBe("a");
    }
    /// <summary>
    /// 验证 SubstringAfter 返回首个或最后一个分隔符之后的内容，并处理缺失和字符分隔符。
    /// </summary>

    [Fact]
    public void SubstringAfter_FirstSeparator_ReturnsSuffix()
    {
        Strings.SubstringAfter("abcba", "b", false).ShouldBe("cba");
    }
    /// <summary>
    /// 验证 SubstringAfter 返回首个或最后一个分隔符之后的内容，并处理缺失和字符分隔符。
    /// </summary>

    [Fact]
    public void SubstringAfter_LastSeparator_ReturnsAfterLast()
    {
        Strings.SubstringAfter("abcba", "b", true).ShouldBe("a");
    }
    /// <summary>
    /// 验证 SubstringAfter 返回首个或最后一个分隔符之后的内容，并处理缺失和字符分隔符。
    /// </summary>

    [Fact]
    public void SubstringAfter_SeparatorNotFound_ReturnsEmpty()
    {
        Strings.SubstringAfter("abc", "d", false).ShouldBe(string.Empty);
    }
    /// <summary>
    /// 验证 SubstringAfter 返回首个或最后一个分隔符之后的内容，并处理缺失和字符分隔符。
    /// </summary>

    [Fact]
    public void SubstringAfter_Char_ReturnsSuffix()
    {
        Strings.SubstringAfter("abcba", 'b', false).ShouldBe("cba");
    }
    /// <summary>
    /// 验证 SubstringBetween 提取标记之间的内容，并处理单一标记或标记缺失。
    /// </summary>

    [Fact]
    public void SubstringBetween_TwoMarkers_ReturnsMiddle()
    {
        Strings.SubstringBetween("before[content]after", "[", "]").ShouldBe("content");
    }
    /// <summary>
    /// 验证 SubstringBetween 提取标记之间的内容，并处理单一标记或标记缺失。
    /// </summary>

    [Fact]
    public void SubstringBetween_SameMarker_ReturnsMiddle()
    {
        Strings.SubstringBetween("*hello*", "*").ShouldBe("hello");
    }
    /// <summary>
    /// 验证 SubstringBetween 提取标记之间的内容，并处理单一标记或标记缺失。
    /// </summary>

    [Fact]
    public void SubstringBetween_MarkerNotFound_ReturnsEmpty()
    {
        Strings.SubstringBetween("hello", "[", "]").ShouldBe(string.Empty);
    }

    #endregion

    #region Strings.Replace
    /// <summary>
    /// 验证 ReplaceIgnoreCase 忽略大小写替换匹配内容，无匹配时保留原字符串。
    /// </summary>

    [Fact]
    public void ReplaceIgnoreCase_MatchesIgnoringCase_ReturnsReplaced()
    {
        Strings.ReplaceIgnoreCase("Hello World", "world", "Earth").ShouldBe("Hello Earth");
    }
    /// <summary>
    /// 验证 ReplaceIgnoreCase 忽略大小写替换匹配内容，无匹配时保留原字符串。
    /// </summary>

    [Fact]
    public void ReplaceIgnoreCase_NoMatch_ReturnsOriginal()
    {
        Strings.ReplaceIgnoreCase("Hello World", "xyz", "Earth").ShouldBe("Hello World");
    }
    /// <summary>
    /// 验证 ReplaceFirstOccurrence 仅替换首个匹配项，无匹配时保留原字符串。
    /// </summary>

    [Fact]
    public void ReplaceFirstOccurrence_OnlyReplacesFirst()
    {
        Strings.ReplaceFirstOccurrence("aaa", "a", "b").ShouldBe("baa");
    }
    /// <summary>
    /// 验证 ReplaceFirstOccurrence 仅替换首个匹配项，无匹配时保留原字符串。
    /// </summary>

    [Fact]
    public void ReplaceFirstOccurrence_NoMatch_ReturnsOriginal()
    {
        Strings.ReplaceFirstOccurrence("abc", "x", "y").ShouldBe("abc");
    }
    /// <summary>
    /// 验证 ReplaceLastOccurrence 仅替换最后一个匹配项，无匹配时保留原字符串。
    /// </summary>

    [Fact]
    public void ReplaceLastOccurrence_OnlyReplacesLast()
    {
        Strings.ReplaceLastOccurrence("aaa", "a", "b").ShouldBe("aab");
    }
    /// <summary>
    /// 验证 ReplaceLastOccurrence 仅替换最后一个匹配项，无匹配时保留原字符串。
    /// </summary>

    [Fact]
    public void ReplaceLastOccurrence_NoMatch_ReturnsOriginal()
    {
        Strings.ReplaceLastOccurrence("abc", "x", "y").ShouldBe("abc");
    }
    /// <summary>
    /// 验证 ReplaceOnlyAtEndIgnoreCase 仅在忽略大小写匹配后缀时替换字符串末尾。
    /// </summary>

    [Fact]
    public void ReplaceOnlyAtEndIgnoreCase_EndsWithMatch_ReplacesEnd()
    {
        Strings.ReplaceOnlyAtEndIgnoreCase("Hello.CS", ".cs", ".txt").ShouldBe("Hello.txt");
    }
    /// <summary>
    /// 验证 ReplaceOnlyAtEndIgnoreCase 仅在忽略大小写匹配后缀时替换字符串末尾。
    /// </summary>

    [Fact]
    public void ReplaceOnlyAtEndIgnoreCase_NotAtEnd_ReturnsOriginal()
    {
        Strings.ReplaceOnlyAtEndIgnoreCase("Hello.cs World", ".cs", ".txt").ShouldBe("Hello.cs World");
    }

    #endregion
}
