namespace Bing.Text;

/// <summary>
/// 字符串工具
/// </summary>
public static partial class Strings
{
    #region Has Numbers

    /// <summary>
    /// 返回是否包含数字。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasNumbers(string text) => HasNumbersAtLeast(text, 1);

    /// <summary>
    /// 至少包含指定数量的数字。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="minCount">最小数量</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasNumbersAtLeast(string text, int minCount)
    {
        minCount = minCount <= 0 ? 1 : minCount;
        return FilterForNumbers(text).Count() >= minCount;
    }

    #endregion

    #region Has Letters

    /// <summary>
    /// 返回是否包含字母。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasLetters(string text) => HasLettersAtLeast(text, 1);

    /// <summary>
    /// 至少包含指定数量的字母。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="minCount">最小数量</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasLettersAtLeast(string text, int minCount)
    {
        minCount = minCount <= 0 ? 1 : minCount;
        return FilterForLetters(text).Count() >= minCount;
    }

    #endregion
}

/// <summary>
/// 字符串扩展
/// </summary>
public static partial class StringsExtensions
{
    #region Has Numbers

    /// <summary>
    /// 返回是否包含数字。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasNumbers(this string text) => Strings.HasNumbers(text);

    /// <summary>
    /// 至少包含指定数量的数字。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="minCount">最小数量</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasNumbersAtLeast(this string text, int minCount)=> Strings.HasNumbersAtLeast(text, minCount);

    #endregion

    #region Has Letters

    /// <summary>
    /// 返回是否包含字母。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasLetters(this string text)  => Strings.HasLetters(text);

    /// <summary>
    /// 至少包含指定数量的字母。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="minCount">最小数量</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasLettersAtLeast(this string text, int minCount) => Strings.HasLettersAtLeast(text, minCount);

    #endregion
}