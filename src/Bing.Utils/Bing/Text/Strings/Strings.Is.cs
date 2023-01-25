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
    public static bool IsUpper(string text)
    {
        return FilterForLetters(text).All(char.IsUpper);
    }

    /// <summary>
    /// 判断是否为小写。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLower(string text)
    {
        return FilterForLetters(text).All(char.IsLower);
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
    public static bool IsUpper(this string text)
    {
        return Strings.IsUpper(text);
    }

    /// <summary>
    /// 判断是否为小写。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLower(this string text)
    {
        return Strings.IsLower(text);
    }
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
    public static bool IsNullOrEmpty(this string text)
    {
        return string.IsNullOrEmpty(text);
    }

    /// <summary>
    /// 检查字符串是 null、空还是仅由空白字符组成。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrWhiteSpace(this string text)
    {
        return string.IsNullOrWhiteSpace(text);
    }
}