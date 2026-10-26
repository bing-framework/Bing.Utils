using System.Text.RegularExpressions;
using Bing.Text.RegularExpressions;

namespace Bing.Helpers;

public static partial class Regexs
{
    /// <summary>
    /// 在显式超时内判断匹配，超时传播 RegexMatchTimeoutException。
    /// </summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">正则模式。</param>
    /// <param name="matchTimeout">单次匹配超时。</param>
    /// <param name="options">正则选项。</param>
    /// <param name="useCache">是否复用含文化及超时的缓存。</param>
    /// <returns>是否存在匹配。</returns>
    public static bool IsMatchWithTimeout(string input, string pattern, TimeSpan matchTimeout, RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
        => RegexPool.IsMatch(input, pattern, options, matchTimeout, useCache);

    /// <summary>
    /// 在显式超时内获取首个匹配。
    /// </summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">正则模式。</param>
    /// <param name="matchTimeout">匹配超时。</param>
    /// <param name="options">正则选项。</param>
    /// <param name="useCache">是否缓存。</param>
    /// <returns>首个匹配；没有匹配时 Success 为 false。</returns>
    public static Match MatchWithTimeout(string input, string pattern, TimeSpan matchTimeout, RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
        => RegexPool.Match(input, pattern, options, matchTimeout, useCache);

    /// <summary>
    /// 获取受匹配超时约束的匹配集合，枚举时也可能超时。
    /// </summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">正则模式。</param>
    /// <param name="matchTimeout">匹配超时。</param>
    /// <param name="options">正则选项。</param>
    /// <param name="useCache">是否缓存。</param>
    /// <returns>匹配集合。</returns>
    public static MatchCollection MatchesWithTimeout(string input, string pattern, TimeSpan matchTimeout, RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
        => RegexPool.Matches(input, pattern, options, matchTimeout, useCache);

    /// <summary>
    /// 使用显式匹配超时替换文本。
    /// </summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">正则模式。</param>
    /// <param name="replacement">替换模板。</param>
    /// <param name="matchTimeout">匹配超时。</param>
    /// <param name="options">正则选项。</param>
    /// <param name="useCache">是否缓存。</param>
    /// <returns>替换结果。</returns>
    public static string ReplaceWithTimeout(string input, string pattern, string replacement, TimeSpan matchTimeout, RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
        => RegexPool.Replace(input, pattern, replacement, options, matchTimeout, useCache);

    /// <summary>
    /// 使用显式匹配超时拆分文本。
    /// </summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">分隔模式。</param>
    /// <param name="matchTimeout">匹配超时。</param>
    /// <param name="options">正则选项。</param>
    /// <param name="useCache">是否缓存。</param>
    /// <returns>拆分结果。</returns>
    public static string[] SplitWithTimeout(string input, string pattern, TimeSpan matchTimeout, RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
        => RegexPool.Split(input, pattern, options, matchTimeout, useCache);

    /// <summary>
    /// 使用显式匹配超时提取单个值。
    /// </summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">模式。</param>
    /// <param name="matchTimeout">匹配超时。</param>
    /// <param name="resultPattern">结果模板。</param>
    /// <param name="options">正则选项。</param>
    /// <param name="useCache">是否缓存。</param>
    /// <returns>提取结果。</returns>
    public static string GetValueWithTimeout(string input, string pattern, TimeSpan matchTimeout, string resultPattern = "", RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
        => RegexPool.GetValue(input, pattern, resultPattern, options, matchTimeout, useCache);

    /// <summary>
    /// 使用显式匹配超时提取多个模板值。
    /// </summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">模式。</param>
    /// <param name="resultPatterns">结果模板。</param>
    /// <param name="matchTimeout">匹配超时。</param>
    /// <param name="options">正则选项。</param>
    /// <param name="useCache">是否缓存。</param>
    /// <returns>模板与提取结果的映射。</returns>
    public static Dictionary<string, string> GetValuesWithTimeout(string input, string pattern, string[] resultPatterns, TimeSpan matchTimeout, RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
        => RegexPool.GetValues(input, pattern, resultPatterns, options, matchTimeout, useCache);
}
