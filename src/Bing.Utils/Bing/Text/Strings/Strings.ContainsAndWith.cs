using System.Text.RegularExpressions;

namespace Bing.Text;

/// <summary>
/// 字符串工具
/// </summary>
public static partial class Strings
{
	#region Contains

    /// <summary>
    /// 在字符串中是否包含任意一个给定的子字符串。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="value">包含字符串</param>
    /// <param name="values">包含字符串数组</param>
    public static bool Contains(string text, string value, params string[] values)
    {
        return YieldReturnStrings().Any(text.Contains);

        IEnumerable<string> YieldReturnStrings()
        {
            yield return value;
            if (value is null)
                yield break;
            foreach (var val in values)
                yield return val;
        }
    }

    /// <summary>
    /// 在字符串中是否包含给定的字符。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="character">包含字符</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(string text, char character) => text.Any(c => c == character);

    /// <summary>
    /// 在字符串中是否包含任意一个给定的字符。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="character">包含字符</param>
    /// <param name="characters">包含字符数组</param>
    public static bool Contains(string text, char character, params char[] characters)
    {
        return YieldReturnCharacters().Any(text.Contains);

        IEnumerable<char> YieldReturnCharacters()
        {
            yield return character;
            if (characters is null)
                yield break;
            foreach (var val in characters)
                yield return val;
        }
    }

    /// <summary>
    /// 在字符串中是否包含任意一个给定的子字符串，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="value">包含字符串</param>
    /// <param name="values">包含字符串数组</param>
    public static bool ContainsIgnoreCase(string text, string value, params string[] values)
    {
        return YieldReturnStrings().Any(v => text.Contains(v, StringComparison.OrdinalIgnoreCase));

        IEnumerable<string> YieldReturnStrings()
        {
            yield return value;
            if (value is null)
                yield break;
            foreach (var val in values)
                yield return val;
        }
    }

    /// <summary>
    /// 在字符串中是否包含给定的字符，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="character">包含字符</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsIgnoreCase(string text, char character)
    {
        return text.Any(c => c == char.ToUpperInvariant(character) || c == char.ToLowerInvariant(character));
    }

    /// <summary>
    /// 在字符串中是否包含任意一个给定的字符，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="character">包含字符</param>
    /// <param name="characters">包含字符数组</param>
    public static bool ContainsIgnoreCase(string text, char character, params char[] characters)
    {
        return YieldReturnCharacters().Any(v => text.Contains(v, StringComparison.OrdinalIgnoreCase));

        IEnumerable<char> YieldReturnCharacters()
        {
            yield return character;
            if (characters is null)
                yield break;
            foreach (var val in characters)
                yield return val;
        }
    }

    /// <summary>
    /// 在字符串中是否包含任意一个给定的子字符串，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="values">包含字符串数组</param>
    /// <param name="case">忽略大小写选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(string text, string[] values, IgnoreCase @case)
    {
        return @case.X()
            ? values.Any(v => text.Contains(v, StringComparison.OrdinalIgnoreCase))
            : values.Any(text.Contains);
    }

    /// <summary>
    /// 在字符串中是否包含给定的字符，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="character">包含字符</param>
    /// <param name="case">忽略大小写选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(string text, char character, IgnoreCase @case)
    {
        return @case.X()
            ? text.Any(c => c == char.ToUpperInvariant(character) || c == char.ToLowerInvariant(character))
            : text.Any(c => c == character);
    }

    /// <summary>
    /// 在字符串中是否包含任意一个给定的字符，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="characters">包含字符数组</param>
    /// <param name="case">忽略大小写选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(string text, char[] characters, IgnoreCase @case)
    {
        return @case.X()
            ? characters.Any(v => text.Contains(v, StringComparison.OrdinalIgnoreCase))
            : characters.Any(text.Contains);
    }

    #endregion

    #region Match

    /// <summary>
    /// 匹配字符串中是否包含 Emoji 表情
    /// </summary>
    /// <param name="text">需要检查的字符串</param>
    /// <returns>如果字符串中包含 Emoji 表情，则返回 true，否则返回 false</returns>
    public static bool MatchEmoji(string text) => Regex.IsMatch(text, @"(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])");

    #endregion
}

/// <summary>
/// 字符串扩展
/// </summary>
public static partial class StringsExtensions
{
    #region Contains

#if NETSTANDARD2_0

    /// <summary>
    /// 确定输入字符串是否包含指定字符串
    /// </summary>
    /// <param name="inputValue">输入字符串</param>
    /// <param name="comparisonValue">包含字符串</param>
    /// <param name="comparisonType">区域</param>
    internal static bool Contains(this string inputValue, string comparisonValue, StringComparison comparisonType)
    {
        return (inputValue.IndexOf(comparisonValue, comparisonType) != -1);
    }

    /// <summary>
    /// 确定输入字符串是否包含指定字符
    /// </summary>
    /// <param name="inputValue">输入字符串</param>
    /// <param name="comparisonValue">包含字符</param>
    /// <param name="comparisonType">区域</param>
    internal static bool Contains(this string inputValue, char comparisonValue, StringComparison comparisonType)
    {
        return (inputValue.IndexOf(comparisonValue.ToString(), comparisonType) != -1);
    }

#endif

    /// <summary>
    /// 在字符串中是否包含任意一个给定的子字符串。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="value">包含字符串</param>
    /// <param name="values">包含字符串数组</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string text, string value, params string[] values)
    {
        return Strings.Contains(text, value, values);
    }

    /// <summary>
    /// 在字符串中是否包含给定的字符。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="character">包含字符</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string text, char character)
    {
        return Strings.Contains(text, character);
    }

    /// <summary>
    /// 在字符串中是否包含任意一个给定的字符。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="character">包含字符</param>
    /// <param name="characters">包含字符数组</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string text, char character, params char[] characters)
    {
        return Strings.Contains(text, character, characters);
    }

    /// <summary>
    /// 在字符串中是否包含任意一个给定的子字符串，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="value">包含字符串</param>
    /// <param name="values">包含字符串数组</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsIgnoreCase(this string text, string value, params string[] values)
    {
        return Strings.ContainsIgnoreCase(text, value, values);
    }

    /// <summary>
    /// 在字符串中是否包含给定的字符，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="character">包含字符</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsIgnoreCase(this string text, char character)
    {
        return Strings.ContainsIgnoreCase(text, character);
    }

    /// <summary>
    /// 在字符串中是否包含任意一个给定的字符，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="character">包含字符</param>
    /// <param name="characters">包含字符数组</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsIgnoreCase(this string text, char character, params char[] characters)
    {
        return Strings.ContainsIgnoreCase(text, character, characters);
    }

    /// <summary>
    /// 在字符串中是否包含任意一个给定的子字符串，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="values">包含字符串数组</param>
    /// <param name="case">忽略大小写选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string text, string[] values, IgnoreCase @case)
    {
        return Strings.Contains(text, values, @case);
    }

    /// <summary>
    /// 在字符串中是否包含给定的字符，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="character">包含字符</param>
    /// <param name="case">忽略大小写选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string text, char character, IgnoreCase @case)
    {
        return Strings.Contains(text, character, @case);
    }

    /// <summary>
    /// 在字符串中是否包含任意一个给定的字符，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="characters">包含字符数组</param>
    /// <param name="case">忽略大小写选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string text, char[] characters, IgnoreCase @case)
    {
        return Strings.Contains(text, characters, @case);
    }

    #endregion

    #region Match

    /// <summary>
    /// 匹配字符串中是否包含 Emoji 表情
    /// </summary>
    /// <param name="text">需要检查的字符串</param>
    /// <returns>如果字符串中包含 Emoji 表情，则返回 true，否则返回 false</returns>
    public static bool MatchEmoji(this string text) => Strings.MatchEmoji(text);

    #endregion
}

/// <summary>
/// 字符串捷径扩展
/// </summary>
public static partial class StringsShortcutExtensions
{
    #region EndsWith

    /// <summary>
    /// 确定此字符串实例的结尾是否与指定的字符串数组中的某一成员匹配。
    /// <para>只要有一个匹配，则返回 True，不然返回 False</para>
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="values">字符串数组</param>
    public static bool EndsWith(this string text, params string[] values)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (values is null || values.Length == 0 || values.Any(string.IsNullOrWhiteSpace))
            return true;
        return values.Any(text.EndsWith);
    }

    /// <summary>
    /// 确定此字符串实例的结尾是否与指定的字符串数组中的某一成员匹配。
    /// <para>只要有一个匹配，则返回 True，不然返回 False</para>
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="values">字符串数组</param>
    public static bool EndsWith(this string text, ICollection<string> values)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (values is null || !values.Any())
            return true;
        return EndsWith(text, values.ToArray());
    }

    /// <summary>
    /// 确定此字符串实例的结尾是否与指定的字符串匹配，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="values">待检查的字符串</param>
    public static bool EndsWithIgnoreCase(this string text, string values)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (values is null || string.IsNullOrEmpty(values))
            return true;
        return text.EndsWith(values, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 确定此字符串实例的结尾是否与指定的字符串数组中的某一个成员匹配，忽略大小写。
    /// <para>只要有一个匹配，则返回 True，不然返回 False</para>
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="values">待检查的字符串</param>
    public static bool EndsWithIgnoreCase(this string text, params string[] values)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (values is null || values.Length == 0 || values.Any(string.IsNullOrWhiteSpace))
            return true;
        return EndsWithIgnoreCase(text, (IEnumerable<string>)values);
    }

    /// <summary>
    /// 确定此字符串实例的结尾是否与指定的字符串数组中的某一个成员匹配，忽略大小写。
    /// <para>只要有一个匹配，则返回 True，不然返回 False</para>
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="values">待检查字符串</param>
    public static bool EndsWithIgnoreCase(this string text, IEnumerable<string> values)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (values is null || !values.Any())
            return true;
        return values.Any(check => text.EndsWith(check, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region StartsWith

    /// <summary>
    /// 确定此字符串实例的开头是否与指定的字符串数组中的某一成员匹配。
    /// <para>只要有一个匹配，则返回 True，不然返回 False</para>
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="values">字符串数组</param>
    public static bool StartsWith(this string text, params string[] values)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (values is null || values.Length == 0 || values.Any(string.IsNullOrWhiteSpace))
            return true;
        return values.Any(text.StartsWith);
    }

    /// <summary>
    /// 确定此字符串实例的开头是否与指定的字符串数组中的某一成员匹配。
    /// <para>只要有一个匹配，则返回 True，不然返回 False</para>
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="values">字符串数组</param>
    public static bool StartsWith(this string text, ICollection<string> values)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (values is null || !values.Any())
            return true;
        return StartsWith(text, values.ToArray());
    }

    /// <summary>
    /// 确定此字符串实例的开头是否与指定的字符串匹配，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="values">待检查字符串</param>
    public static bool StartsWithIgnoreCase(this string text, string values)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (values is null || string.IsNullOrEmpty(values))
            return true;
        return text.StartsWith(values, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 确定此字符串实例的开头是否与指定的字符串数组中的某一个成员匹配，忽略大小写。
    /// <para>只要有一个匹配，则返回 True，不然返回 False</para>
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="values">待检查字符串</param>
    public static bool StartsWithIgnoreCase(this string text, params string[] values)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (values is null || values.Length == 0 || values.Any(string.IsNullOrWhiteSpace))
            return true;
        return StartsWithIgnoreCase(text, (IEnumerable<string>)values);
    }

    /// <summary>
    /// 确定此字符串实例的开头是否与指定的字符串数组中的某一个成员匹配，忽略大小写。
    /// <para>只要有一个匹配，则返回 True，不然返回 False</para>
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="values">待检查字符串</param>
    public static bool StartsWithIgnoreCase(this string text, IEnumerable<string> values)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (values is null || !values.Any())
            return true;
        return values.Any(check => text.StartsWith(check, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}