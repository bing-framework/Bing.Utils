
// ReSharper disable once CheckNamespace
using System.Text.RegularExpressions;

namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// 移除字符串中指定的子字符串之后的内容（忽略大小写）
    /// </summary>
    /// <param name="text">源字符串</param>
    /// <param name="removeFromThis">要移除内容的起始子字符串</param>
    /// <returns>
    /// 处理后的字符串。如果源字符串中不包含指定的子字符串，则返回原字符串。
    /// 忽略子字符串的大小写。
    /// </returns>
    /// <example>
    /// <code>
    /// "HelloWorld".RemoveFromIgnoreCase("o") => "Hell"
    /// "HelloWorld".RemoveFromIgnoreCase("O") => "Hell"
    /// "HelloWorld".RemoveFromIgnoreCase("x") => "HelloWorld"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveFromIgnoreCase(this string text, string removeFromThis) =>
        Strings.RemoveSinceIgnoreCase(text, removeFromThis);

    /// <summary>
    /// 移除字符串中的重复空格，将多个连续空格替换为单个空格
    /// </summary>
    /// <param name="text">源字符串</param>
    /// <returns>
    /// 处理后的字符串，连续的空格被替换为单个空格。
    /// 如果源字符串为 null 或空字符串，则返回原字符串。
    /// </returns>
    /// <example>
    /// <code>
    /// "Hello  World".RemoveDuplicateSpaces() => "Hello World"
    /// "Hello   World  Test".RemoveDuplicateSpaces() => "Hello World Test"
    /// </code>
    /// </example>
    public static string RemoveDuplicateSpaces(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        // 重构实现，更高效且不会引入循环
        return Regex.Replace(text, @"\s+", " ");
    }

    /// <summary>
    /// 移除字符串中的音调字符，包括 'Ñ'/'ñ' 字符，保留基本字母
    /// </summary>
    /// <param name="text">源字符串</param>
    /// <returns>
    /// 移除音调字符后的字符串。如果源字符串为 null 或空字符串，则返回原字符串。
    /// </returns>
    /// <example>
    /// <code>
    /// "café".RemoveAccentsIgnoreCaseAndN() => "cafe"
    /// "El niño".RemoveAccentsIgnoreCaseAndN() => "El nino"
    /// </code>
    /// </example>
    public static string RemoveAccentsIgnoreCaseAndN(this string text) =>
        string.IsNullOrEmpty(text)
            ? text
            : RemoveAccentsIgnoreCase(text).Replace('Ñ', 'N').Replace('ñ', 'n');

    /// <summary>
    /// 移除字符串中的音调字符，保留基本字母
    /// </summary>
    /// <param name="text">源字符串</param>
    /// <returns>
    /// 移除音调字符后的字符串。如果源字符串为 null 或空字符串，则返回原字符串。
    /// </returns>
    /// <example>
    /// <code>
    /// "café".RemoveAccentsIgnoreCase() => "cafe"
    /// "Hélló Wórld".RemoveAccentsIgnoreCase() => "Hello World"
    /// </code>
    /// </example>
    public static string RemoveAccentsIgnoreCase(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        return text.Replace('Á', 'A')
            .Replace('É', 'E')
            .Replace('Í', 'I')
            .Replace('Ó', 'O')
            .Replace('Ú', 'U')
            .Replace('ü', 'u')
            .Replace('Ü', 'U')
            .Replace('á', 'a')
            .Replace('é', 'e')
            .Replace('í', 'i')
            .Replace('ó', 'o')
            .Replace('ú', 'u');
    }

    /// <summary>
    /// 从字符串中移除给定的子字符串，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写
    /// </summary>
    /// <param name="text">源文本</param>
    /// <param name="removeText">需要移除的子字符串</param>
    /// <param name="case">忽略大小写选项，默认为区分大小写</param>
    /// <returns>
    /// 处理后的字符串，其中所有匹配的子字符串都被移除。
    /// 如果源文本为 null，则返回 null；如果 removeText 为 null 或空，则返回原字符串。
    /// </returns>
    /// <example>
    /// <code>
    /// "Hello World".Remove("World") => "Hello "
    /// "Hello World".Remove("world", IgnoreCase.True) => "Hello "
    /// "Hello Hello World".Remove("Hello ") => "World"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Remove(this string text, string removeText, IgnoreCase @case = IgnoreCase.False) => Strings.Remove(text, removeText, @case);

    /// <summary>
    /// 从字符串中移除指定的一组字符
    /// </summary>
    /// <param name="text">源文本</param>
    /// <param name="toRemove">需要移除的字符数组</param>
    /// <returns>
    /// 处理后的字符串，不包含指定的任何字符。如果 <paramref name="text"/> 为 null，则返回 null；
    /// 如果 <paramref name="toRemove"/> 为 null 或空数组，则返回原字符串。
    /// </returns>
    /// <example>
    /// <code>
    /// "Hello World".RemoveChars('e', 'o') => "Hll Wrld"
    /// "Hello World".RemoveChars(' ') => "HelloWorld"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveChars(this string text, params char[] toRemove) => Strings.RemoveChars(text, toRemove);

    /// <summary>
    /// 移除字符串中的所有空格
    /// </summary>
    /// <param name="text">源文本</param>
    /// <returns>
    /// 处理后的字符串，不包含任何空格。
    /// 如果源文本为 null，则返回 null；如果为空字符串，则返回空字符串。
    /// </returns>
    /// <example>
    /// <code>
    /// "Hello World".RemoveWhiteSpace() => "HelloWorld"
    /// "  Hello  World  ".RemoveWhiteSpace() => "HelloWorld"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveWhiteSpace(this string text) => Strings.RemoveWhiteSpace(text);

    /// <summary>
    /// 移除字符串中所有连续的空白字符，将多个空白字符替换为单个空格
    /// </summary>
    /// <param name="text">源文本</param>
    /// <returns>
    /// 处理后的字符串，连续的空白字符被替换为单个空格。
    /// 如果 <paramref name="text"/> 为 null 或空字符串，则返回原值。
    /// </returns>
    /// <remarks>
    /// 此方法使用正则表达式 <c>\s+</c> 匹配任意连续的空白字符，包括空格、制表符、换行符等。
    /// </remarks>
    /// <example>
    /// <code>
    /// "Hello  World".RemoveDuplicateWhiteSpaces() => "Hello World"
    /// "Hello\t\nWorld".RemoveDuplicateWhiteSpaces() => "Hello World"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveDuplicateWhiteSpaces(this string text) => Strings.RemoveDuplicateWhiteSpaces(text);

    /// <summary>
    /// 移除字符串中指定字符的重复出现，只保留首次出现
    /// </summary>
    /// <param name="text">源文本</param>
    /// <param name="charRemove">需要处理重复的字符</param>
    /// <param name="case">忽略大小写选项，默认为区分大小写</param>
    /// <returns>
    /// 处理后的字符串，其中指定字符的重复出现被移除。
    /// 如果源文本为 null 或空字符串，则返回原值。
    /// </returns>
    /// <remarks>
    /// 当 <paramref name="case"/> 设为 <see cref="IgnoreCase.True"/> 时，将忽略大小写；
    /// 例如，如果移除重复的 'a'，那么 'A' 也会被视为重复并移除。
    /// </remarks>
    /// <example>
    /// <code>
    /// "Hello".RemoveDuplicateChar('l') => "Helo"
    /// "aAabBb".RemoveDuplicateChar('a', IgnoreCase.True) => "abBb"
    /// "Mississippi".RemoveDuplicateChar('s') => "Misisippi"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveDuplicateChar(this string text, char charRemove, IgnoreCase @case = IgnoreCase.False) => Strings.RemoveDuplicateChar(text, charRemove, @case);

    /// <summary>
    /// 从指定索引位置开始移除后续所有字符
    /// </summary>
    /// <param name="text">源文本</param>
    /// <param name="indexOfStartToRemove">开始移除的索引位置（包含该位置的字符）</param>
    /// <returns>
    /// 处理后的字符串，不包含指定索引之后的任何字符。
    /// 如果 <paramref name="indexOfStartToRemove"/> 小于或等于 0，则返回原字符串；
    /// 如果大于字符串长度，则返回空字符串。
    /// </returns>
    /// <example>
    /// <code>
    /// "Hello World".RemoveSince(5) => "Hello"
    /// "ABCDE".RemoveSince(3) => "ABC"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveSince(this string text, int indexOfStartToRemove) => Strings.RemoveSince(text, indexOfStartToRemove);

    /// <summary>
    /// 从指定子字符串首次出现的位置开始移除后续所有字符
    /// </summary>
    /// <param name="text">源文本</param>
    /// <param name="removeFromThis">标记移除起始位置的子字符串</param>
    /// <returns>
    /// 处理后的字符串，不包含标记字符串首次出现位置及之后的任何字符。
    /// 如果 <paramref name="text"/> 为 null，则返回 null；
    /// 如果 <paramref name="removeFromThis"/> 为 null、空字符串或在源文本中未找到，则返回原字符串。
    /// </returns>
    /// <example>
    /// <code>
    /// "Hello World".RemoveSince("World") => "Hello "
    /// "Hello World".RemoveSince("o") => "Hell"
    /// "ABCDE".RemoveSince("D") => "ABC"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveSince(this string text, string removeFromThis) => Strings.RemoveSince(text, removeFromThis);

    /// <summary>
    /// 从指定子字符串首次出现的位置开始移除后续所有字符（忽略大小写）
    /// </summary>
    /// <param name="text">源文本</param>
    /// <param name="removeFromThis">标记移除起始位置的子字符串</param>
    /// <returns>
    /// 处理后的字符串，不包含标记字符串首次出现位置及之后的任何字符。
    /// 如果 <paramref name="text"/> 为 null，则返回 null；
    /// 如果 <paramref name="removeFromThis"/> 为 null、空字符串或在源文本中未找到，则返回原字符串。
    /// </returns>
    /// <example>
    /// <code>
    /// "Hello World".RemoveSinceIgnoreCase("world") => "Hello "
    /// "ABCDE".RemoveSinceIgnoreCase("d") => "ABC"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveSinceIgnoreCase(this string text, string removeFromThis) => Strings.RemoveSinceIgnoreCase(text, removeFromThis);

    /// <summary>
    /// 从指定子字符串首次出现的位置开始移除后续所有字符，支持选择是否忽略大小写
    /// </summary>
    /// <param name="text">源文本</param>
    /// <param name="removeFromThis">标记移除起始位置的子字符串</param>
    /// <param name="case">忽略大小写选项</param>
    /// <returns>
    /// 处理后的字符串，根据 <paramref name="case"/> 参数确定查找子字符串的匹配规则。
    /// </returns>
    /// <example>
    /// <code>
    /// "Hello World".RemoveSince("world", IgnoreCase.True) => "Hello "
    /// "Hello World".RemoveSince("world", IgnoreCase.False) => "Hello World"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveSince(this string text, string removeFromThis, IgnoreCase @case) => Strings.RemoveSince(text, removeFromThis, @case);

    /// <summary>
    /// 移除字符串开头指定的子字符串
    /// </summary>
    /// <param name="value">源文本</param>
    /// <param name="start">需要移除的起始子字符串</param>
    /// <returns>
    /// 处理后的字符串，如果源字符串以指定子字符串开头，则移除该部分。
    /// 如果 <paramref name="value"/> 为 null 或空白字符串，则返回空字符串；
    /// 如果 <paramref name="start"/> 为 null 或空字符串，或源文本不以此开头，则返回源文本。
    /// </returns>
    /// <example>
    /// <code>
    /// "Hello World".RemoveStart("Hello") => " World"
    /// "a.cs.cshtml".RemoveStart("a.cs") => ".cshtml"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveStart(this string value, string start) => Strings.RemoveStart(value, start);

    /// <summary>
    /// 移除字符串末尾指定的子字符串
    /// </summary>
    /// <param name="value">源文本</param>
    /// <param name="end">需要移除的末尾子字符串</param>
    /// <returns>
    /// 处理后的字符串，如果源字符串以指定子字符串结尾，则移除该部分。
    /// 如果 <paramref name="value"/> 为 null 或空白字符串，则返回空字符串；
    /// 如果 <paramref name="end"/> 为 null 或空字符串，或源文本不以此结尾，则返回源文本。
    /// </returns>
    /// <example>
    /// <code>
    /// "Hello World".RemoveEnd("World") => "Hello "
    /// "a.cs.cshtml".RemoveEnd(".cshtml") => "a.cs"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveEnd(this string value, string end) => Strings.RemoveEnd(value, end);
}