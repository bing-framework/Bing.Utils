// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// 计算行数
    /// </summary>
    /// <param name="str">字符串</param>
    public static int CountLines(this string str)
    {
        int index = 0, lines = 0;
        while (true)
        {
            var newIndex = str.IndexOf(Environment.NewLine, index, StringComparison.Ordinal);
            if (newIndex < 0)
            {
                if (str.Length > index)
                    lines++;
                return lines;
            }

            index = newIndex + 2;
            lines++;
        }
    }

    /// <summary>
    /// 返回字符串中所包含字母的数量。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForLetters(this string text) => Strings.CountForLetters(text);

    /// <summary>
    /// 返回字符串中所包含大写字母的数量。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForLettersUpperCase(this string text) => Strings.CountForLettersUpperCase(text);

    /// <summary>
    /// 返回字符串中所包含小写字母的数量。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForLettersLowerCase(this string text) => Strings.CountForLettersLowerCase(text);

    /// <summary>
    /// 返回字符串中所包含数字的数量。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForNumbers(this string text) => Strings.CountForNumbers(text);

    /// <summary>
    /// 计算给定字符串中有多少个指定的字符。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrences(this string text, char toCheck) => Strings.CountOccurrences(text, toCheck);

    /// <summary>
    /// 计算给定字符串中有多少个指定的子字符串。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrences(this string text, string toCheck) => Strings.CountOccurrences(text, toCheck);

    /// <summary>
    /// 计算给定字符串中有多少个指定的字符，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrencesIgnoreCase(this string text, char toCheck) => Strings.CountOccurrencesIgnoreCase(text, toCheck);

    /// <summary>
    /// 计算给定字符串中有多少个指定的子字符串，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrencesIgnoreCase(this string text, string toCheck) => Strings.CountOccurrencesIgnoreCase(text, toCheck);

    /// <summary>
    /// 计算给定字符串中有多少个指定的字符，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符</param>
    /// <param name="case">忽略大小写开关</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrences(this string text, char toCheck, IgnoreCase @case) => Strings.CountOccurrences(text, toCheck, @case);

    /// <summary>
    /// 计算给定字符串中有多少个指定的子字符串，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    /// <param name="case">忽略大小写开关</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrences(this string text, string toCheck, IgnoreCase @case) => Strings.CountOccurrences(text, toCheck, @case);

    /// <summary>
    /// 比较字符串，获取不相等字符的数量。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForDiffChars(this string text, string toCheck) => Strings.CountForDiffChars(text, toCheck);

    /// <summary>
    /// 比较字符串，获取不相等字符的数量，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForDiffCharsIgnoreCase(this string text, string toCheck) => Strings.CountForDiffCharsIgnoreCase(text, toCheck);

    /// <summary>
    /// 比较字符串，获取不相等字符的数量，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    /// <param name="case">忽略大小写开关</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForDiffChars(this string text, string toCheck, IgnoreCase @case) => Strings.CountForDiffChars(text, toCheck, @case);

    /// <summary>
    /// 计算字符串中的字符数。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>字符数</returns>
    /// <remarks>
    /// 注意：<br />
    /// netcore3.1 遇到组合emoji时，会被拆成单个emoji去计算。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CharacterCount(this string text) => Strings.CharacterCount(text);

    /// <summary>
    /// 计算字符串的字节大小（使用UTF-8编码）
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>字节大小</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BytesCount(this string text) => Strings.BytesCount(text);
}