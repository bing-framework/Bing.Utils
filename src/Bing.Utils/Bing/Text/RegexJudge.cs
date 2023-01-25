using System.Text.RegularExpressions;

namespace Bing.Text;

/// <summary>
/// 正则表达式(<see cref="Regex"/>) 检查器
/// </summary>
public static class RegexJudge
{
    /// <summary>
    /// 指示在 <see cref="Regex"/> 构造函数中指定的正则表达式是否在指定的输入字符串中找到匹配项。
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="pattern">模式字符串</param>
    /// <param name="options">选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMatch(string str, string pattern, RegexOptions options = RegexOptions.IgnoreCase) => Regex.IsMatch(str, pattern, options);
}