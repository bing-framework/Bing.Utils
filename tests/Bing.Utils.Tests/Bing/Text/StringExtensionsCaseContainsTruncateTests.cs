using Bing.Text;

namespace Bing.Utils.Tests.Bing.Text;

/// <summary>
/// 验证字符串大小写转换、包含判断、前后缀匹配、全半角转换和截断行为。
/// </summary>
[Trait("Bing.Text", "StringExtensionsCaseContainsTruncate")]
public class StringExtensionsCaseContainsTruncateTests
{
    #region ToCapitalCase

    /// <summary>
    /// 验证单个小写单词转换为首字母大写形式。
    /// </summary>
    [Fact]
    public void ToCapitalCase_SingleWord_ReturnsCapitalized()
    {
        "hello".ToCapitalCase().ShouldBe("Hello");
    }
    /// <summary>
    /// 验证 ToCapitalCase 将单词首字母转换为大写，并保留已有全大写单词。
    /// </summary>

    [Fact]
    public void ToCapitalCase_MultipleWords_ReturnsEachCapitalized()
    {
        "hello world".ToCapitalCase().ShouldBe("Hello World");
    }
    /// <summary>
    /// 验证 ToCapitalCase 将单词首字母转换为大写，并保留已有全大写单词。
    /// </summary>

    [Fact]
    public void ToCapitalCase_AllCaps_RetainsAllCaps()
    {
        // AllCapitals word stays unchanged
        "HELLO world".ToCapitalCase().ShouldBe("HELLO World");
    }
    /// <summary>
    /// 验证 ToCapitalCase 将单个小写字符转换为大写。
    /// </summary>

    [Fact]
    public void ToCapitalCase_SingleChar_ReturnsUppercase()
    {
        "a".ToCapitalCase().ShouldBe("A");
    }

    #endregion

    #region ToCamelCase (StringExtensions)
    /// <summary>
    /// 验证 ToCamelCase 将首字母转为小写，并保留本已符合形式的内容。
    /// </summary>

    [Fact]
    public void ToCamelCase_PascalCase_LowercasesFirstChar()
    {
        "UserName".ToCamelCase().ShouldBe("userName");
    }
    /// <summary>
    /// 验证 ToCamelCase 将首字母转为小写，并保留本已符合形式的内容。
    /// </summary>

    [Fact]
    public void ToCamelCase_SingleChar_ReturnsLower()
    {
        "A".ToCamelCase().ShouldBe("a");
    }
    /// <summary>
    /// 验证 ToCamelCase 将首字母转为小写，并保留本已符合形式的内容。
    /// </summary>

    [Fact]
    public void ToCamelCase_AlreadyLower_ReturnsSame()
    {
        "hello".ToCamelCase().ShouldBe("hello");
    }
    /// <summary>
    /// 验证 ToCamelCase 对空字符串返回空字符串。
    /// </summary>

    [Fact]
    public void ToCamelCase_Empty_ReturnsEmpty()
    {
        "".ToCamelCase().ShouldBe("");
    }

    #endregion

    #region Contains (StringExtensions)
    /// <summary>
    /// 验证 Contains 可按字符串候选项判断包含关系，并处理 null 输入。
    /// </summary>

    [Fact]
    public void Contains_StringWithMatch_ReturnsTrue()
    {
        "Hello World".Contains("World", "xyz").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 Contains 可按字符串候选项判断包含关系，并处理 null 输入。
    /// </summary>

    [Fact]
    public void Contains_StringNoMatch_ReturnsFalse()
    {
        "Hello World".Contains("abc", "xyz").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 Contains 可按字符串候选项判断包含关系，并处理 null 输入。
    /// </summary>

    [Fact]
    public void Contains_NullText_ReturnsFalse()
    {
        ((string)null).Contains("a", "b").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 Contains 可按单个字符或多个候选字符判断包含关系。
    /// </summary>

    [Fact]
    public void Contains_CharMatch_ReturnsTrue()
    {
        "Hello".Contains('e').ShouldBeTrue();
    }
    /// <summary>
    /// 验证 Contains 可按单个字符或多个候选字符判断包含关系。
    /// </summary>

    [Fact]
    public void Contains_CharNoMatch_ReturnsFalse()
    {
        "Hello".Contains('z').ShouldBeFalse();
    }
    /// <summary>
    /// 验证 Contains 可按单个字符或多个候选字符判断包含关系。
    /// </summary>

    [Fact]
    public void Contains_MultipleCharsOneMatch_ReturnsTrue()
    {
        "Hello".Contains('x', 'e').ShouldBeTrue();
    }
    /// <summary>
    /// 验证 Contains 可按单个字符或多个候选字符判断包含关系。
    /// </summary>

    [Fact]
    public void Contains_MultipleCharsNoMatch_ReturnsFalse()
    {
        "Hello".Contains('x', 'y', 'z').ShouldBeFalse();
    }
    /// <summary>
    /// 验证 ContainsIgnoreCase 对字符串和字符候选项忽略大小写进行匹配。
    /// </summary>

    [Fact]
    public void ContainsIgnoreCase_String_IgnoresCase()
    {
        "Hello World".ContainsIgnoreCase("hello").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 ContainsIgnoreCase 对字符串和字符候选项忽略大小写进行匹配。
    /// </summary>

    [Fact]
    public void ContainsIgnoreCase_StringNotFound_ReturnsFalse()
    {
        "Hello World".ContainsIgnoreCase("xyz").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 ContainsIgnoreCase 对字符串和字符候选项忽略大小写进行匹配。
    /// </summary>

    [Fact]
    public void ContainsIgnoreCase_Char_IgnoresCase()
    {
        "Hello".ContainsIgnoreCase('E').ShouldBeTrue();
    }
    /// <summary>
    /// 验证 ContainsIgnoreCase 对字符串和字符候选项忽略大小写进行匹配。
    /// </summary>

    [Fact]
    public void ContainsIgnoreCase_Char_NotFound_ReturnsFalse()
    {
        "Hello".ContainsIgnoreCase('x').ShouldBeFalse();
    }
    /// <summary>
    /// 验证 ContainsIgnoreCase 对字符串和字符候选项忽略大小写进行匹配。
    /// </summary>

    [Fact]
    public void ContainsIgnoreCase_MultipleChars_IgnoresCase()
    {
        "Hello".ContainsIgnoreCase('E', 'O').ShouldBeTrue();
    }
    /// <summary>
    /// 验证 Contains 的 IgnoreCase 参数控制字符串及字符候选项的大小写匹配。
    /// </summary>

    [Fact]
    public void Contains_StringArrayWithIgnoreCaseTrue_ReturnsTrue()
    {
        "Hello World".Contains(new[] { "hello" }, IgnoreCase.True).ShouldBeTrue();
    }
    /// <summary>
    /// 验证 Contains 的 IgnoreCase 参数控制字符串及字符候选项的大小写匹配。
    /// </summary>

    [Fact]
    public void Contains_StringArrayWithIgnoreCaseFalse_ReturnsFalse()
    {
        "Hello World".Contains(new[] { "hello" }, IgnoreCase.False).ShouldBeFalse();
    }
    /// <summary>
    /// 验证 Contains 的 IgnoreCase 参数控制字符串及字符候选项的大小写匹配。
    /// </summary>

    [Fact]
    public void Contains_CharWithIgnoreCaseTrue_ReturnsTrue()
    {
        "Hello".Contains('E', IgnoreCase.True).ShouldBeTrue();
    }
    /// <summary>
    /// 验证 Contains 的 IgnoreCase 参数控制字符串及字符候选项的大小写匹配。
    /// </summary>

    [Fact]
    public void Contains_CharWithIgnoreCaseFalse_ReturnsFalse()
    {
        "Hello".Contains('E', IgnoreCase.False).ShouldBeFalse();
    }
    /// <summary>
    /// 验证 Contains 的 IgnoreCase 参数控制字符串及字符候选项的大小写匹配。
    /// </summary>

    [Fact]
    public void Contains_CharArrayWithIgnoreCaseTrue_ReturnsTrue()
    {
        "Hello".Contains(new[] { 'E', 'O' }, IgnoreCase.True).ShouldBeTrue();
    }
    /// <summary>
    /// 验证 Contains 的 IgnoreCase 参数控制字符串及字符候选项的大小写匹配。
    /// </summary>

    [Fact]
    public void Contains_CharArrayWithIgnoreCaseFalse_ReturnsFalse()
    {
        "Hello".Contains(new[] { 'E', 'O' }, IgnoreCase.False).ShouldBeFalse();
    }

    #endregion

    #region MatchEmoji
    /// <summary>
    /// 验证 MatchEmoji 能识别包含 Emoji 的文本，并拒绝普通文本。
    /// </summary>

    [Fact]
    public void MatchEmoji_WithEmoji_ReturnsTrue()
    {
        "Hello 😊".MatchEmoji().ShouldBeTrue();
    }
    /// <summary>
    /// 验证 MatchEmoji 能识别包含 Emoji 的文本，并拒绝普通文本。
    /// </summary>

    [Fact]
    public void MatchEmoji_NoEmoji_ReturnsFalse()
    {
        "Hello World".MatchEmoji().ShouldBeFalse();
    }

    #endregion

    #region EndsWith (StringsShortcutExtensions)
    /// <summary>
    /// 验证 EndsWith 可按字符或多个字符串后缀判断匹配结果。
    /// </summary>

    [Fact]
    public void EndsWith_Char_Match_ReturnsTrue()
    {
        "hello".EndsWith('o').ShouldBeTrue();
    }
    /// <summary>
    /// 验证 EndsWith 可按字符或多个字符串后缀判断匹配结果。
    /// </summary>

    [Fact]
    public void EndsWith_Char_NoMatch_ReturnsFalse()
    {
        "hello".EndsWith('x').ShouldBeFalse();
    }
    /// <summary>
    /// 验证 EndsWith 可按字符或多个字符串后缀判断匹配结果。
    /// </summary>

    [Fact]
    public void EndsWith_ParamsStrings_OneMatch_ReturnsTrue()
    {
        "hello.cs".EndsWith(".cs", ".ts").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 EndsWith 可按字符或多个字符串后缀判断匹配结果。
    /// </summary>

    [Fact]
    public void EndsWith_ParamsStrings_NoMatch_ReturnsFalse()
    {
        "hello.cs".EndsWith(".ts", ".js").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 EndsWithIgnoreCase 忽略大小写匹配单个或多个后缀。
    /// </summary>

    [Fact]
    public void EndsWithIgnoreCase_IgnoresCase_ReturnsTrue()
    {
        "hello.CS".EndsWithIgnoreCase(".cs").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 EndsWithIgnoreCase 忽略大小写匹配单个或多个后缀。
    /// </summary>

    [Fact]
    public void EndsWithIgnoreCase_NotEnding_ReturnsFalse()
    {
        "hello.cs".EndsWithIgnoreCase(".ts").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 EndsWithIgnoreCase 忽略大小写匹配单个或多个后缀。
    /// </summary>

    [Fact]
    public void EndsWithIgnoreCase_Params_OneMatch_ReturnsTrue()
    {
        "hello.CS".EndsWithIgnoreCase(".ts", ".cs").ShouldBeTrue();
    }

    #endregion

    #region StartsWith (StringsShortcutExtensions)
    /// <summary>
    /// 验证 StartsWith 可按字符或多个字符串前缀判断匹配结果。
    /// </summary>

    [Fact]
    public void StartsWith_Char_Match_ReturnsTrue()
    {
        "hello".StartsWith('h').ShouldBeTrue();
    }
    /// <summary>
    /// 验证 StartsWith 可按字符或多个字符串前缀判断匹配结果。
    /// </summary>

    [Fact]
    public void StartsWith_Char_NoMatch_ReturnsFalse()
    {
        "hello".StartsWith('x').ShouldBeFalse();
    }
    /// <summary>
    /// 验证 StartsWith 可按字符或多个字符串前缀判断匹配结果。
    /// </summary>

    [Fact]
    public void StartsWith_ParamsStrings_OneMatch_ReturnsTrue()
    {
        "Hello World".StartsWith("Hello", "Bye").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 StartsWith 可按字符或多个字符串前缀判断匹配结果。
    /// </summary>

    [Fact]
    public void StartsWith_ParamsStrings_NoMatch_ReturnsFalse()
    {
        "Hello World".StartsWith("Bye", "See").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 StartsWithIgnoreCase 忽略大小写匹配单个或多个前缀。
    /// </summary>

    [Fact]
    public void StartsWithIgnoreCase_IgnoresCase_ReturnsTrue()
    {
        "HELLO world".StartsWithIgnoreCase("hello").ShouldBeTrue();
    }
    /// <summary>
    /// 验证 StartsWithIgnoreCase 忽略大小写匹配单个或多个前缀。
    /// </summary>

    [Fact]
    public void StartsWithIgnoreCase_NotStarting_ReturnsFalse()
    {
        "Hello World".StartsWithIgnoreCase("World").ShouldBeFalse();
    }
    /// <summary>
    /// 验证 StartsWithIgnoreCase 忽略大小写匹配单个或多个前缀。
    /// </summary>

    [Fact]
    public void StartsWithIgnoreCase_Params_OneMatch_ReturnsTrue()
    {
        "HELLO world".StartsWithIgnoreCase("bye", "hello").ShouldBeTrue();
    }

    #endregion

    #region ToSbcCase / ToDbcCase
    /// <summary>
    /// 验证 ToSbcCase 将 ASCII 字符和空格转换为全角形式。
    /// </summary>

    [Fact]
    public void ToSbcCase_AsciiChars_ConvertsToFullWidth()
    {
        var result = "AB".ToSbcCase();
        // 'A'(65) + 65248 = 65313 (full-width A)
        result[0].ShouldBe((char)65313);
        result[1].ShouldBe((char)65314);
    }
    /// <summary>
    /// 验证 ToSbcCase 将 ASCII 字符和空格转换为全角形式。
    /// </summary>

    [Fact]
    public void ToSbcCase_Space_ConvertsToFullWidthSpace()
    {
        var result = " ".ToSbcCase();
        result[0].ShouldBe((char)12288);
    }
    /// <summary>
    /// 验证 ToDbcCase 将全角字符和全角空格转换为半角形式。
    /// </summary>

    [Fact]
    public void ToDbcCase_FullWidthChars_ConvertsToHalfWidth()
    {
        var fullWidth = "Ａ"; // char 65313
        var result = fullWidth.ToDbcCase();
        result[0].ShouldBe('A');
    }
    /// <summary>
    /// 验证 ToDbcCase 将全角字符和全角空格转换为半角形式。
    /// </summary>

    [Fact]
    public void ToDbcCase_FullWidthSpace_ConvertsToHalfWidthSpace()
    {
        var fullWidthSpace = "\u3000"; // 12288
        var result = fullWidthSpace.ToDbcCase();
        result[0].ShouldBe(' ');
    }
    /// <summary>
    /// 验证 ToSbcCase 与 ToDbcCase 往返后还原原字符串。
    /// </summary>

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
    /// <summary>
    /// 验证 Truncate 对未超过最大长度的文本保留原内容。
    /// </summary>

    [Fact]
    public void Truncate_WithinMaxLength_ReturnsOriginal()
    {
        "hi".Truncate(10).ShouldBe("hi");
    }
    /// <summary>
    /// 验证 Truncate 超出最大长度时按约定追加省略号。
    /// </summary>

    [Fact]
    public void Truncate_ExceedsMaxLength_ReturnsWithEllipsis()
    {
        "Hello World".Truncate(8).ShouldBe("Hello...");
    }
    /// <summary>
    /// 验证 Truncate 超出最大长度时按约定追加省略号。
    /// </summary>

    [Fact]
    public void Truncate_MaxLengthLessThanOrEqualThree_ReturnsTwoCharsAndDot()
    {
        "Hello".Truncate(2).ShouldBe("He.");
    }
    /// <summary>
    /// 验证 Truncate 的最大长度为零时返回空字符串。
    /// </summary>

    [Fact]
    public void Truncate_ZeroMaxLength_ReturnsEmpty()
    {
        "Hello".Truncate(0).ShouldBe(string.Empty);
    }
    /// <summary>
    /// 验证 Truncate 对 null 或空字符串返回空字符串。
    /// </summary>

    [Fact]
    public void Truncate_NullOrEmpty_ReturnsEmpty()
    {
        "".Truncate(5).ShouldBe(string.Empty);
        ((string)null).Truncate(5).ShouldBe(string.Empty);
    }

    #endregion
}
