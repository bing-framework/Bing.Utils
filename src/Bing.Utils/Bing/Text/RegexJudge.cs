using System.Text.RegularExpressions;
using Bing.Text.RegularExpressions;

namespace Bing.Text;

/// <summary>
/// 正则表达式(<see cref="Regex"/>) 检查器
/// </summary>
public static class RegexJudge
{
    #region IsMatch

    /// <summary>
    /// 判断字符串是否匹配指定的正则表达式模式
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="pattern">模式字符串</param>
    /// <param name="options">正则表达式选项</param>
    /// <returns>如果找到匹配项，则为 true；否则为 false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMatch(string str, string pattern, RegexOptions options = RegexOptions.IgnoreCase) =>
        RegexPool.IsMatch(str, pattern, options, useCache: false);

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
}