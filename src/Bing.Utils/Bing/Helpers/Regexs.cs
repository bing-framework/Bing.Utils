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
    /// 在输入字符串中搜索第一个匹配指定模式的子字符串
    /// </summary>
    /// <param name="input">要搜索的输入字符串</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <returns>表示第一个匹配项的 Match 对象，如果未找到匹配项则返回失败的 Match 对象</returns>
    /// <exception cref="ArgumentNullException">当输入字符串或模式为 null 时抛出</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Match Match(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Match(input, pattern, options, useCache: false);

    /// <summary>
    /// 使用缓存在输入字符串中搜索第一个匹配指定模式的子字符串
    /// </summary>
    /// <param name="input">要搜索的输入字符串</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <returns>表示第一个匹配项的 Match 对象，如果未找到匹配项则返回失败的 Match 对象</returns>
    /// <exception cref="ArgumentNullException">当输入字符串或模式为 null 时抛出</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Match MatchCached(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Match(input, pattern, options, useCache: true);

    #endregion

    #region Matches

    /// <summary>
    /// 在输入字符串中搜索所有匹配指定模式的子字符串
    /// </summary>
    /// <param name="input">要搜索的输入字符串</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <returns>包含所有匹配项的 MatchCollection 集合</returns>
    /// <exception cref="ArgumentNullException">当输入字符串或模式为 null 时抛出</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MatchCollection Matches(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Matches(input, pattern, options, useCache: false);

    /// <summary>
    /// 使用缓存在输入字符串中搜索所有匹配指定模式的子字符串
    /// </summary>
    /// <param name="input">要搜索的输入字符串</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <returns>包含所有匹配项的 MatchCollection 集合</returns>
    /// <exception cref="ArgumentNullException">当输入字符串或模式为 null 时抛出</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MatchCollection MatchesCached(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Matches(input, pattern, options, useCache: true);

    #endregion

    #region Replace

    /// <summary>
    /// 使用指定的替换字符串替换输入字符串中匹配正则表达式模式的所有子字符串
    /// </summary>
    /// <param name="input">要执行替换操作的输入字符串</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="replacement">替换字符串，可包含捕获组引用如 $1、$2 等</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <returns>替换完成后的新字符串</returns>
    /// <exception cref="ArgumentNullException">当模式或替换字符串为 null 时抛出</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Replace(string input, string pattern, string replacement, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Replace(input, pattern, replacement, options, useCache: false);

    /// <summary>
    /// 使用缓存和指定的替换字符串替换输入字符串中匹配正则表达式模式的所有子字符串
    /// </summary>
    /// <param name="input">要执行替换操作的输入字符串</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="replacement">替换字符串，可包含捕获组引用如 $1、$2 等</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <returns>替换完成后的新字符串</returns>
    /// <exception cref="ArgumentNullException">当模式或替换字符串为 null 时抛出</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceCached(string input, string pattern, string replacement, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Replace(input, pattern, replacement, options, useCache: true);

    #endregion

    #region Split

    /// <summary>
    /// 使用正则表达式模式作为分隔符将输入字符串分割成子字符串数组
    /// </summary>
    /// <param name="input">要分割的输入字符串</param>
    /// <param name="pattern">作为分隔符的正则表达式模式</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <returns>分割后的字符串数组</returns>
    /// <exception cref="ArgumentNullException">当模式为 null 时抛出</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] Split(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Split(input, pattern, options, useCache: false);

    /// <summary>
    /// 使用缓存和正则表达式模式作为分隔符将输入字符串分割成子字符串数组
    /// </summary>
    /// <param name="input">要分割的输入字符串</param>
    /// <param name="pattern">作为分隔符的正则表达式模式</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <returns>分割后的字符串数组</returns>
    /// <exception cref="ArgumentNullException">当模式为 null 时抛出</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] SplitCached(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        => RegexPool.Split(input, pattern, options, useCache: true);

    #endregion

    #region GetValues

    /// <summary>
    /// 获取正则表达式匹配的多个值，将结果组织为字典形式
    /// </summary>
    /// <param name="input">要搜索的输入字符串</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="resultPatterns">结果模式字符串数组，例如 new[]{"$1","$2"} 用于获取多个捕获组</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <returns>包含匹配值的字典，键为结果模式，值为对应的匹配结果</returns>
    /// <exception cref="ArgumentNullException">当模式为 null 时抛出</exception>
    public static Dictionary<string, string> GetValues(string input, string pattern, string[] resultPatterns, RegexOptions options = RegexOptions.IgnoreCase) =>
        RegexPool.GetValues(input, pattern, resultPatterns, options, useCache: true);

    #endregion

    #region GetValue

    /// <summary>
    /// 获取正则表达式匹配的单个值，支持结果模式转换
    /// </summary>
    /// <param name="input">要搜索的输入字符串</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="resultPattern">结果模式字符串，例如 "$1" 用来获取第一个捕获组内的值，空字符串表示获取整个匹配结果</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <returns>匹配的值，如果未匹配或输入为空则返回空字符串</returns>
    /// <exception cref="ArgumentNullException">当模式为 null 时抛出</exception>
    public static string GetValue(string input, string pattern, string resultPattern = "", RegexOptions options = RegexOptions.IgnoreCase) => 
        RegexPool.GetValue(input, pattern, resultPattern, options, useCache: true);

    #endregion
}