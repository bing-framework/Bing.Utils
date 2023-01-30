using System.Text.RegularExpressions;

namespace Bing.Text;

/// <summary>
/// 字符串工具
/// </summary>
public static partial class Strings
{
    /// <summary>
    /// 从输入字符串中获取捕获的子字符串。
    /// </summary>
    /// <param name="match">正则匹配结果</param>
    /// <param name="group">分组</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static string GetGroupValue(Match match, string group)
    {
        if (match == null)
            throw new ArgumentNullException(nameof(match));
        if (string.IsNullOrWhiteSpace(group))
            throw new ArgumentNullException(nameof(group));
        var g = match.Groups[group];
        if (!match.Success || !g.Success)
            throw new InvalidOperationException($"未能在匹配结果中找到匹配分组({group})");
        return g.Value;
    }
}

/// <summary>
/// 字符串扩展
/// </summary>
public static partial class StringsExtensions
{
    /// <summary>
    /// 从输入字符串中获取捕获的子字符串。
    /// </summary>
    /// <param name="match">正则匹配结果</param>
    /// <param name="group">分组</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetGroupValue(this Match match, string group)
    {
        return Strings.GetGroupValue(match, group);
    }
}