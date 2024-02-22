using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// 字符串掩码[俗称:脱敏]
    /// </summary>
    /// <param name="value">需要进行脱敏处理的字符串。</param>
    /// <param name="mask">用于脱敏的掩码字符，默认为'*'。</param>
    /// <returns>脱敏后的字符串。</returns>
    /// <remarks>
    /// 此方法根据字符串的长度，保留字符串的部分内容，其余部分使用指定的掩码字符替换。
    /// - 如果字符串长度大于等于11，保留前3位和后4位，其余部分使用掩码字符替换。
    /// - 如果字符串长度为10，保留前3位和后3位，其余部分使用掩码字符替换。
    /// - 依此类推，长度递减时，保留的字符数量相应减少。
    /// - 如果字符串长度小于或等于5，仅保留第一位，其余部分使用掩码字符替换。
    /// 如果输入字符串为空或仅包含空白字符，则直接返回原字符串。
    /// </remarks>
    public static string Mask(this string value, char mask = '*')
    {
        if (string.IsNullOrWhiteSpace(value?.Trim()))
            return value;
        value = value.Trim();
        var masks = mask.ToString().PadLeft(4, mask);
        return value.Length switch
        {
            >= 11 => Regex.Replace(value, "(.{3}).*(.{4})", $"$1{masks}$2"),
            10 => Regex.Replace(value, "(.{3}).*(.{3})", $"$1{masks}$2"),
            9 => Regex.Replace(value, "(.{2}).*(.{3})", $"$1{masks}$2"),
            8 => Regex.Replace(value, "(.{2}).*(.{2})", $"$1{masks}$2"),
            7 => Regex.Replace(value, "(.{1}).*(.{2})", $"$1{masks}$2"),
            6 => Regex.Replace(value, "(.{1}).*(.{1})", $"$1{masks}$2"),
            _ => Regex.Replace(value, "(.{1}).*", $"$1{masks}")
        };
    }
}