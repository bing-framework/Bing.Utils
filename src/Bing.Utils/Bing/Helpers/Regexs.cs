using System.Text.RegularExpressions;
using Bing.Text.RegularExpressions;

namespace Bing.Helpers;

/// <summary>
/// 正则表达式 操作
/// </summary>
public static partial class Regexs
{
    #region IsMatch

    /// <summary>
    /// 判断字符串是否匹配指定的正则表达式模式
    /// </summary>
    /// <param name="input">字符串</param>
    /// <param name="pattern">模式字符串</param>
    /// <param name="options">正则表达式选项</param>
    /// <returns>如果找到匹配项，则为 true；否则为 false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMatch(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase) =>
        RegexPool.IsMatch(input, pattern, options, false);

    /// <summary>
    /// 使用缓存的正则表达式检查字符串是否匹配指定模式
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="pattern">模式字符串</param>
    /// <param name="options">正则表达式选项</param>
    /// <returns>如果找到匹配项，则为 true；否则为 false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMatchCached(string str, string pattern, RegexOptions options = RegexOptions.IgnoreCase) =>
        RegexPool.IsMatch(str, pattern, options, useCache: true);

    #endregion

    #region Match

    /// <summary>
    /// 获取第一个匹配项
    /// </summary>
    /// <param name="input">要搜索匹配项的字符串</param>
    /// <param name="pattern">要匹配的正则表达式模式</param>
    /// <param name="options">正则表达式选项</param>
    /// <returns>匹配结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Match Match(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Match(input, pattern, options, useCache: false);

    /// <summary>
    /// 使用缓存获取第一个匹配项
    /// </summary>
    /// <param name="input">要搜索匹配项的字符串</param>
    /// <param name="pattern">要匹配的正则表达式模式</param>
    /// <param name="options">正则表达式选项</param>
    /// <returns>匹配结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Match MatchCached(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Match(input, pattern, options, useCache: true);

    #endregion

    #region Matches

    /// <summary>
    /// 获取所有匹配项
    /// </summary>
    /// <param name="input">要搜索匹配项的字符串</param>
    /// <param name="pattern">要匹配的正则表达式模式</param>
    /// <param name="options">正则表达式选项</param>
    /// <returns>匹配结果集合</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MatchCollection Matches(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Matches(input, pattern, options, useCache: false);

    /// <summary>
    /// 使用缓存获取所有匹配项
    /// </summary>
    /// <param name="input">要搜索匹配项的字符串</param>
    /// <param name="pattern">要匹配的正则表达式模式</param>
    /// <param name="options">正则表达式选项</param>
    /// <returns>匹配结果集合</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MatchCollection MatchesCached(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Matches(input, pattern, options, useCache: true);

    #endregion

    #region Replace

    /// <summary>
    /// 替换匹配的字符串
    /// </summary>
    /// <param name="input">要搜索匹配项的字符串</param>
    /// <param name="pattern">要匹配的正则表达式模式</param>
    /// <param name="replacement">替换字符串</param>
    /// <param name="options">正则表达式选项</param>
    /// <returns>替换后的字符串</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Replace(string input, string pattern, string replacement, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Replace(input, pattern, replacement, options, useCache: false);

    /// <summary>
    /// 使用缓存替换匹配的字符串
    /// </summary>
    /// <param name="input">要搜索匹配项的字符串</param>
    /// <param name="pattern">要匹配的正则表达式模式</param>
    /// <param name="replacement">替换字符串</param>
    /// <param name="options">正则表达式选项</param>
    /// <returns>替换后的字符串</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceCached(string input, string pattern, string replacement, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Replace(input, pattern, replacement, options, useCache: true);

    #endregion

    #region Split

    /// <summary>
    /// 使用正则表达式分割字符串
    /// </summary>
    /// <param name="input">要拆分的字符串</param>
    /// <param name="pattern">要匹配的正则表达式模式</param>
    /// <param name="options">正则表达式选项</param>
    /// <returns>分割后的字符串数组</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] Split(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Split(input, pattern, options, useCache: false);

    /// <summary>
    /// 使用缓存的正则表达式分割字符串
    /// </summary>
    /// <param name="input">要拆分的字符串</param>
    /// <param name="pattern">要匹配的正则表达式模式</param>
    /// <param name="options">正则表达式选项</param>
    /// <returns>分割后的字符串数组</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] SplitCached(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Split(input, pattern, options, useCache: true);

    #endregion

    #region GetValues

    /// <summary>
    /// 获取匹配值集合
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="pattern">模式字符串</param>
    /// <param name="resultPatterns">结果模式字符串数组，例如：new[]{"$1","$2"}</param>
    /// <param name="options">正则表达式选项</param>
    /// <returns>匹配值字典</returns>
    public static Dictionary<string, string> GetValues(string input, string pattern, string[] resultPatterns, RegexOptions options = RegexOptions.IgnoreCase) =>
        RegexPool.GetValues(input, pattern, resultPatterns, options, useCache: true);

    #endregion

    #region GetValue

    /// <summary>
    /// 获取匹配值
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="pattern">模式字符串</param>
    /// <param name="resultPattern">结果模式字符串，范例："$1"用来获取第一个()内的值</param>
    /// <param name="options">选项</param>
    /// <returns>匹配的值</returns>
    public static string GetValue(string input, string pattern, string resultPattern = "", RegexOptions options = RegexOptions.IgnoreCase) => 
        RegexPool.GetValue(input, pattern, resultPattern, options, useCache: true);

    #endregion
}