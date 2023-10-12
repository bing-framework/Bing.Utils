﻿using System.Text.RegularExpressions;

namespace Bing.Helpers;

/// <summary>
/// 正则表达式 操作
/// </summary>
public static partial class Regexs
{
    #region GetValues(获取匹配值集合)

    /// <summary>
    /// 获取匹配值集合
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="pattern">模式字符串</param>
    /// <param name="resultPatterns">结果模式字符串数组，范例：new[]{"$1","$2"}</param>
    /// <param name="options">选项</param>
    public static Dictionary<string, string> GetValues(string input, string pattern, string[] resultPatterns,
        RegexOptions options = RegexOptions.IgnoreCase)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input))
            return result;
        var match = System.Text.RegularExpressions.Regex.Match(input, pattern, options);
        if (match.Success == false)
            return result;
        AddResults(result, match, resultPatterns);
        return result;
    }

    /// <summary>
    /// 添加匹配结果
    /// </summary>
    /// <param name="result">匹配值字典</param>
    /// <param name="match">匹配结果</param>
    /// <param name="resultPatterns">结果模式字符串数组，范例：new[]{"$1","$2"}</param>
    private static void AddResults(Dictionary<string, string> result, Match match, string[] resultPatterns)
    {
        if (resultPatterns == null)
        {
            result.Add(string.Empty, match.Value);
            return;
        }
        foreach (var resultPattern in resultPatterns)
            result.Add(resultPattern, match.Result(resultPattern));
    }

    #endregion

    #region GetValue(获取匹配值)

    /// <summary>
    /// 获取匹配值
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="pattern">模式字符串</param>
    /// <param name="resultPattern">结果模式字符串，范例："$1"用来获取第一个()内的值</param>
    /// <param name="options">选项</param>
    public static string GetValue(string input, string pattern, string resultPattern = "",
        RegexOptions options = RegexOptions.IgnoreCase)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        var match = System.Text.RegularExpressions.Regex.Match(input, pattern, options);
        if (match.Success == false)
            return string.Empty;
        return string.IsNullOrWhiteSpace(resultPattern) ? match.Value : match.Result(resultPattern);
    }

    #endregion

    #region Split(分割成字符串数组)

    /// <summary>
    /// 分割成字符串数组
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="pattern">模式字符串</param>
    /// <param name="options">选项</param>
    public static string[] Split(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase) =>
        string.IsNullOrWhiteSpace(input)
            ? new string[] { }
            : System.Text.RegularExpressions.Regex.Split(input, pattern, options);

    #endregion

    #region Replace(替换)

    /// <summary>
    /// 替换
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="pattern">模式字符串</param>
    /// <param name="replacement">替换字符串</param>
    /// <param name="options">选项</param>
    public static string Replace(string input, string pattern, string replacement,
        RegexOptions options = RegexOptions.IgnoreCase) => string.IsNullOrWhiteSpace(input)
        ? string.Empty
        : System.Text.RegularExpressions.Regex.Replace(input, pattern, replacement, options);

    #endregion

    #region IsMatch(验证输入与模式是否匹配)

    /// <summary>
    /// 验证输入与模式是否匹配
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="pattern">模式字符串</param>
    public static bool IsMatch(string input, string pattern) => IsMatch(input, pattern, RegexOptions.IgnoreCase);

    /// <summary>
    /// 验证输入与模式是否匹配
    /// </summary>
    /// <param name="input">输入的字符串</param>
    /// <param name="pattern">模式字符串</param>
    /// <param name="options">选项</param>
    public static bool IsMatch(string input, string pattern, RegexOptions options) => Regex.IsMatch(input, pattern, options);

    #endregion

}