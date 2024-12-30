using System.Text.RegularExpressions;

namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 检查器
/// </summary>
public static class StringJudge
{
    #region StartsWith/EndsWith

    /// <summary>
    /// 判断字符串是否以指定的字符开头。
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="value">字符</param>
    /// <returns>如果匹配则返回 true，否则返回 false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithThese(string str, char value) => str.StartsWith(value);

    /// <summary>
    /// 判断字符串是否以指定的字符串开头。
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="values">字符串集合</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithThese(string str, params string[] values) => str.StartsWith(values);

    /// <summary>
    /// 判断字符串是否以指定的字符串开头。
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="values">字符串集合</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithThese(string str, ICollection<string> values) => str.StartsWith(values);

    /// <summary>
    /// 判断字符串是否以指定的字符结尾。
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="value">字符</param>
    /// <returns>如果匹配则返回 true，否则返回 false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithThese(string str, char value) => str.StartsWith(value);

    /// <summary>
    /// 判断字符串是否以指定的字符串结尾。
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="values">字符串集合</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithThese(string str, params string[] values) => str.EndsWith(values);

    /// <summary>
    /// 判断字符串是否以指定的字符串结尾。
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="values">字符串集合</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithThese(string str, ICollection<string> values) => str.EndsWith(values);

    #endregion

    #region Contains

    /// <summary>
    /// 判断字符串是否包含汉字
    /// </summary>
    /// <param name="str">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsChineseCharacters(string str) => !string.IsNullOrWhiteSpace(str) && RegexJudge.IsMatch(str, "[\u4e00-\u9fa5]+");

    /// <summary>
    /// 判断字符串是否包含数字。
    /// </summary>
    /// <param name="str">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsNumber(string str) => !string.IsNullOrWhiteSpace(str) && RegexJudge.IsMatch(str, "[0-9]+");

    #endregion

    #region Is

    /// <summary>
    /// 正则表达式选项
    /// </summary>
    private const RegexOptions INTERNAL_SCHEMA = RegexOptions.Singleline | RegexOptions.Compiled;

    /// <summary>
    /// WebUrl 正则表达式
    /// </summary>
    private static readonly Regex WebUrlExpressionSchema = new Regex(@"(http|https)://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?", INTERNAL_SCHEMA);

    /// <summary>
    /// 判断字符串是否为 Web Url 地址。
    /// </summary>
    /// <param name="str">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWebUrl(string str)
    {
        return !string.IsNullOrWhiteSpace(str) && WebUrlExpressionSchema.IsMatch(str);
    }

    /// <summary>
    /// Email 正则表达式
    /// </summary>
    private static readonly Regex EmailExpressionSchema = new Regex(@"^([0-9a-zA-Z]+[-._+&])*[0-9a-zA-Z]+@([-0-9a-zA-Z]+[.])+[a-zA-Z]{2,6}$", INTERNAL_SCHEMA);

    /// <summary>
    /// 判断字符串是否为电子邮件。
    /// </summary>
    /// <param name="str">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmail(string str)
    {
        return !string.IsNullOrWhiteSpace(str) && EmailExpressionSchema.IsMatch(str);
    }

    #endregion
}