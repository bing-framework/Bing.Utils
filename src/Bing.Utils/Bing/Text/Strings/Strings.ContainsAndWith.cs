using System.Text.RegularExpressions;

namespace Bing.Text;

/// <summary>
/// 字符串工具
/// </summary>
public static partial class Strings
{
    #region Contains

    /// <summary>
    /// 检查字符串中是否包含任意一个给定的子字符串
    /// </summary>
    /// <param name="text">要检查的源字符串</param>
    /// <param name="value">需要检查的第一个子字符串</param>
    /// <param name="values">需要检查的其他子字符串</param>
    /// <returns>
    /// 如果源字符串包含任意一个给定的子字符串，则返回 true；否则返回 false。
    /// 如果源字符串为 null 或空字符串，则始终返回 false。
    /// </returns>
    /// <example>
    /// <code>
    /// Strings.Contains("Hello World", "Hello") => true
    /// Strings.Contains("Hello World", "xyz", "World") => true
    /// Strings.Contains("Hello World", "xyz", "abc") => false
    /// Strings.Contains(null, "Hello") => false
    /// </code>
    /// </example>
    public static bool Contains(string text, string value, params string[] values)
    {
        if (string.IsNullOrEmpty(text))
            return false;
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
    /// 检查字符串中是否包含指定字符
    /// </summary>
    /// <param name="text">要检查的源字符串</param>
    /// <param name="character">需要检查的字符</param>
    /// <returns>
    /// 如果源字符串包含指定字符，则返回 true；否则返回 false。
    /// 如果源字符串为 null 或空字符串，则返回 false。
    /// </returns>
    /// <example>
    /// <code>
    /// Strings.Contains("Hello", 'e') => true
    /// Strings.Contains("Hello", 'x') => false
    /// Strings.Contains("", 'e') => false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(string text, char character)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        return text.IndexOf(character) >= 0;
    }

    /// <summary>
    /// 检查字符串中是否包含任意一个给定的字符
    /// </summary>
    /// <param name="text">要检查的源字符串</param>
    /// <param name="character">需要检查的第一个字符</param>
    /// <param name="characters">需要检查的其他字符</param>
    /// <returns>
    /// 如果源字符串包含任意一个给定的字符，则返回 true；否则返回 false。
    /// 如果源字符串为 null 或空字符串，则返回 false。
    /// </returns>
    /// <example>
    /// <code>
    /// Strings.Contains("Hello", 'e', 'o') => true
    /// Strings.Contains("Hello", 'x', 'y', 'z') => false
    /// Strings.Contains("Hello", 'x', 'e') => true
    /// </code>
    /// </example>
    public static bool Contains(string text, char character, params char[] characters)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        if (text.IndexOf(character) >= 0)
            return true;
        if (characters is null || characters.Length == 0)
            return false;
        foreach (var c in characters)
        {
            if (text.IndexOf(c) >= 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 检查字符串中是否包含任意一个给定的子字符串（忽略大小写）
    /// </summary>
    /// <param name="text">要检查的源字符串</param>
    /// <param name="value">需要检查的第一个子字符串</param>
    /// <param name="values">需要检查的其他子字符串</param>
    /// <returns>
    /// 如果源字符串包含任意一个给定的子字符串（忽略大小写），则返回 true；否则返回 false。
    /// 如果源字符串为 null 或空字符串，则返回 false。
    /// </returns>
    /// <example>
    /// <code>
    /// Strings.ContainsIgnoreCase("Hello World", "hello") => true
    /// Strings.ContainsIgnoreCase("Hello World", "WORLD") => true
    /// Strings.ContainsIgnoreCase("Hello World", "xyz", "world") => true
    /// </code>
    /// </example>
    public static bool ContainsIgnoreCase(string text, string value, params string[] values)
    {
        if (string.IsNullOrEmpty(text))
            return false;
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
    /// 检查字符串中是否包含指定字符（忽略大小写）
    /// </summary>
    /// <param name="text">要检查的源字符串</param>
    /// <param name="character">需要检查的字符</param>
    /// <returns>
    /// 如果源字符串包含指定字符（忽略大小写），则返回 true；否则返回 false。
    /// 如果源字符串为 null 或空字符串，则返回 false。
    /// </returns>
    /// <example>
    /// <code>
    /// Strings.ContainsIgnoreCase("Hello", 'E') => true
    /// Strings.ContainsIgnoreCase("Hello", 'x') => false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsIgnoreCase(string text, char character)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        return text.IndexOf(character.ToString(), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// 检查字符串中是否包含任意一个给定的字符（忽略大小写）
    /// </summary>
    /// <param name="text">要检查的源字符串</param>
    /// <param name="character">需要检查的第一个字符</param>
    /// <param name="characters">需要检查的其他字符</param>
    /// <returns>
    /// 如果源字符串包含任意一个给定的字符（忽略大小写），则返回 true；否则返回 false。
    /// 如果源字符串为 null 或空字符串，则返回 false。
    /// </returns>
    /// <example>
    /// <code>
    /// Strings.ContainsIgnoreCase("Hello", 'E', 'O') => true
    /// Strings.ContainsIgnoreCase("Hello", 'X', 'Y', 'Z') => false
    /// </code>
    /// </example>
    public static bool ContainsIgnoreCase(string text, char character, params char[] characters)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        if (text.IndexOf(character.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (characters is null || characters.Length == 0)
            return false;

        foreach (var c in characters)
        {
            if (text.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 检查字符串中是否包含任意一个给定的子字符串，根据忽略大小写选项决定比较方式
    /// </summary>
    /// <param name="text">要检查的源字符串</param>
    /// <param name="values">需要检查的子字符串数组</param>
    /// <param name="case">忽略大小写选项</param>
    /// <returns>
    /// 如果源字符串包含任意一个给定的子字符串，则返回 true；否则返回 false。
    /// 如果源字符串为 null 或空字符串，则返回 false。
    /// 如果 values 为 null 或空数组，则返回 false。
    /// </returns>
    /// <example>
    /// <code>
    /// Strings.Contains("Hello World", new[]{"hello"}, IgnoreCase.True) => true
    /// Strings.Contains("Hello World", new[]{"hello"}, IgnoreCase.False) => false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(string text, string[] values, IgnoreCase @case)
    {
        if (string.IsNullOrEmpty(text) || values == null || values.Length == 0)
            return false;
        return @case.X()
            ? values.Any(v => text.Contains(v, StringComparison.OrdinalIgnoreCase))
            : values.Any(text.Contains);
    }

    /// <summary>
    /// 检查字符串中是否包含指定字符，根据忽略大小写选项决定比较方式
    /// </summary>
    /// <param name="text">要检查的源字符串</param>
    /// <param name="character">需要检查的字符</param>
    /// <param name="case">忽略大小写选项</param>
    /// <returns>
    /// 如果源字符串包含指定字符，则返回 true；否则返回 false。
    /// 如果源字符串为 null 或空字符串，则返回 false。
    /// </returns>
    /// <example>
    /// <code>
    /// Strings.Contains("Hello", 'E', IgnoreCase.True) => true
    /// Strings.Contains("Hello", 'E', IgnoreCase.False) => false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(string text, char character, IgnoreCase @case)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        return @case.X()
            ? text.Any(c => c == char.ToUpperInvariant(character) || c == char.ToLowerInvariant(character))
            : text.Any(c => c == character);
    }

    /// <summary>
    /// 检查字符串中是否包含任意一个给定的字符，根据忽略大小写选项决定比较方式
    /// </summary>
    /// <param name="text">要检查的源字符串</param>
    /// <param name="characters">需要检查的字符数组</param>
    /// <param name="case">忽略大小写选项</param>
    /// <returns>
    /// 如果源字符串包含任意一个给定的字符，则返回 true；否则返回 false。
    /// 如果源字符串为 null 或空字符串，则返回 false。
    /// 如果 characters 为 null 或空数组，则返回 false。
    /// </returns>
    /// <example>
    /// <code>
    /// Strings.Contains("Hello", new[]{'E','O'}, IgnoreCase.True) => true
    /// Strings.Contains("Hello", new[]{'E','O'}, IgnoreCase.False) => false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(string text, char[] characters, IgnoreCase @case)
    {
        if (string.IsNullOrEmpty(text) || characters == null || characters.Length == 0)
            return false;

        if (@case.X())
        {
            foreach (var c in characters)
            {
                if (text.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }
        else
        {
            foreach (var c in characters)
            {
                if (text.IndexOf(c) >= 0)
                    return true;
            }
            return false;
        }
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
/// 字符串捷径扩展
/// </summary>
public static partial class StringsShortcutExtensions
{
    #region EndsWith

    /// <summary>
    /// 确定此字符串实例的结尾是否与指定的字符匹配。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="value">待检查的字符</param>
    /// <returns>如果匹配则返回 true，否则返回 false</returns>
    public static bool EndsWith(this string text, char value) => !string.IsNullOrEmpty(text) && text[^1] == value;

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
    /// 确定此字符串实例的开头是否与指定的字符匹配。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="value">待检查的字符</param>
    /// <returns>如果匹配则返回 true，否则返回 false</returns>
    public static bool StartsWith(this string text, char value) => !string.IsNullOrEmpty(text) && text![0] == value;

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