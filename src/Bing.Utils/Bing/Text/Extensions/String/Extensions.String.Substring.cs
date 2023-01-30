

// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// 截断字符串。子字符串从指定字符串之后开始
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="startText">起始字符串</param>
    public static string Substring(this string text, string startText)
    {
        var index = text.IndexOf(startText, StringComparison.Ordinal);
        if (index == -1)
            throw new ArgumentException($"Not found: '{startText}'.");
        return text.Substring(index);
    }

}