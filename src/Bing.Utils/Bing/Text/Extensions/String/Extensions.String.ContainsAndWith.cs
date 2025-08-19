// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    #region Contains

#if NETSTANDARD2_0

    /// <summary>
    /// 确定输入字符串是否包含指定字符串
    /// </summary>
    /// <param name="inputValue">输入字符串</param>
    /// <param name="comparisonValue">包含字符串</param>
    /// <param name="comparisonType">区域</param>
    public static bool Contains(this string inputValue, string comparisonValue, StringComparison comparisonType)
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