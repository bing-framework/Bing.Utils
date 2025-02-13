
// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// 从字符串开始处获取指定长度的子字符串
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="length">需要获取的子字符串的长度</param>
    /// <returns>如果原始字符串的长度大于指定长度，则返回原始字符串的前length个字符组成的子字符串；否则，返回原始字符串。如果原始字符串为空或仅包含空格，则返回空字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Take(this string text, int length) => Strings.Take(text, length);
}