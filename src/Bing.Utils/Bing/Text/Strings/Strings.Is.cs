namespace Bing.Text;

/// <summary>
/// 字符串工具
/// </summary>
public static partial class Strings
{
    /// <summary>
    /// 判断是否为大写。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUpper(string text) => FilterForLetters(text).All(char.IsUpper);

    /// <summary>
    /// 判断是否为小写。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLower(string text) => FilterForLetters(text).All(char.IsLower);

    /// <summary>
    /// 判断是否为中文字符。
    /// </summary>
    /// <param name="value">字符</param>
    /// <returns>如果字符是中文字符，则返回 true；否则返回 false。</returns>
    public static bool IsChinese(char value) => (value >= 19968 && value <= 40869);

    /// <summary>
    /// 判断字符串是否全部为中文字符。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>如果字符串全部为中文字符，则返回 true；否则返回 false。</returns>
    public static bool IsChinese(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        return text.All(IsChinese);
    }
}

/// <summary>
/// 字符串扩展
/// </summary>
public static partial class StringsExtensions
{
    /// <summary>
    /// 判断是否为大写。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUpper(this string text) => Strings.IsUpper(text);

    /// <summary>
    /// 判断是否为小写。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLower(this string text) => Strings.IsLower(text);
}

/// <summary>
/// 字符串捷径扩展
/// </summary>
public static partial class StringsShortcutExtensions
{
    /// <summary>
    /// 检查字符串是 null 还是 <see cref="string.Empty"/> 字符串。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty(this string text) => string.IsNullOrEmpty(text);

    /// <summary>
    /// 检查字符串不是 null 或 <see cref="string.Empty"/> 字符串。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNullNorEmpty(this string text) => !text.IsNullOrEmpty();

    /// <summary>
    /// 检查字符串是 null、空还是仅由空白字符组成。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrWhiteSpace(this string text) => string.IsNullOrWhiteSpace(text);

    /// <summary>
    /// 检查字符串不是 null、空或由空白字符组成。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNullNorWhiteSpace(this string text) => !text.IsNullOrWhiteSpace();
}