using System;
using System.Text;
namespace Bing.Text.Pinyin;

/// <summary>
/// 中文拼音转换工具。
/// </summary>
/// <remarks>
/// 原有接口使用固定 GBK 区码生成无调拼音；新增接口从离线词库读取带调和词组读音。
/// </remarks>
public static partial class PinyinUtil
{
    /// <summary>
    /// 获取文本的不带声调全拼。
    /// </summary>
    /// <param name="text">要转换的文本。</param>
    /// <param name="separator">相邻中文拼音音节之间的分隔符。</param>
    /// <returns>首字母大写的拼音文本；输入为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">separator 为 null。</exception>
    public static string GetPinyin(string text, string separator = "")
    {
        if (separator == null)
            throw new ArgumentNullException(nameof(separator));
        return Convert(text, separator, Bing.Helpers.Str.FullPinYin);
    }

    /// <summary>
    /// 获取文本的拼音首字母。
    /// </summary>
    /// <param name="text">要转换的文本。</param>
    /// <param name="separator">相邻中文拼音首字母之间的分隔符。</param>
    /// <returns>小写拼音首字母文本；输入为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">separator 为 null。</exception>
    public static string GetInitials(string text, string separator = "")
    {
        if (separator == null)
            throw new ArgumentNullException(nameof(separator));
        return Convert(text, separator, Bing.Helpers.Str.PinYin);
    }

    /// <summary>
    /// 判断字符是否属于 BMP 中的汉字区段。
    /// </summary>
    /// <param name="value">要判断的字符。</param>
    /// <returns>属于 CJK 扩展 A、基本区或兼容区时返回 true；否则返回 false。</returns>
    public static bool IsChinese(char value) =>
        value >= '\u3400' && value <= '\u4dbf' ||
        value >= '\u4e00' && value <= '\u9fff' ||
        value >= '\uf900' && value <= '\ufaff';

    /// <summary>
    /// 逐字符转换文本并插入拼音分隔符。
    /// </summary>
    /// <param name="text">要转换的文本。</param>
    /// <param name="separator">相邻拼音音节的分隔符。</param>
    /// <param name="converter">单字符拼音转换函数。</param>
    /// <returns>转换结果；输入为 null 时返回 null。</returns>
    private static string Convert(string text, string separator, Func<string, string> converter)
    {
        if (text == null)
            return null;
        if (text.Length == 0)
            return string.Empty;

        var result = new StringBuilder(text.Length);
        var previousWasPinyin = false;
        foreach (var value in text)
        {
            if (!IsChinese(value))
            {
                result.Append(value);
                previousWasPinyin = false;
                continue;
            }

            var source = value.ToString();
            var converted = converter(source);
            var wasConverted = !string.IsNullOrEmpty(converted) && !string.Equals(converted, source, StringComparison.Ordinal);
            if (!wasConverted)
            {
                result.Append(source);
                previousWasPinyin = false;
                continue;
            }

            if (previousWasPinyin && separator.Length > 0)
                result.Append(separator);
            result.Append(converted);
            previousWasPinyin = true;
        }

        return result.ToString();
    }
}
