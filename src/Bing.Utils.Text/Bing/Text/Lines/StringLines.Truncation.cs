using Bing.Text.Truncation;

// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串行工具
/// </summary>
public static partial class StringLines
{
    /// <summary>
    /// 将多行文本分割为单行文本集合，并截取其中的一部分，其余部分用指定的占位符代替
    /// </summary>
    /// <param name="text">多行文本</param>
    /// <param name="maxLines">最大行数</param>
    /// <param name="placeholder">占位符，默认为"..."</param>
    /// <param name="extraSpace">是否添加额外的空格，默认为false</param>
    /// <returns>截取后的文本</returns>
    public static string TruncateByLines(string text, int maxLines, string placeholder = "...", bool extraSpace = false)
        => StringTruncators.ByNumberOfLines.Truncate(text, maxLines, placeholder, extraSpace: extraSpace);

    /// <summary>
    /// 将多行文本分割为单行文本集合，并截取其中的一部分，其余部分用指定的占位符代替
    /// </summary>
    /// <param name="text">多行文本</param>
    /// <param name="maxLines">最大行数</param>
    /// <param name="truncateFrom">截断位置</param>
    /// <param name="placeholder">占位符，默认为"..."</param>
    /// <param name="extraSpace">是否添加额外的空格，默认为false</param>
    /// <returns>截取后的文本</returns>
    public static string TruncateByLines(string text, int maxLines, StringTruncateFrom truncateFrom, string placeholder = "...", bool extraSpace = false)
        => StringTruncators.ByNumberOfLines.Truncate(text, maxLines, placeholder, truncateFrom: truncateFrom, extraSpace: extraSpace);
}

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringLinesExtensions
{
    /// <summary>
    /// 将多行文本分割为单行文本集合，并截取其中的一部分，其余部分用指定的占位符代替
    /// </summary>
    /// <param name="text">多行文本</param>
    /// <param name="maxLines">最大行数</param>
    /// <param name="placeholder">占位符，默认为"..."</param>
    /// <param name="extraSpace">是否添加额外的空格，默认为false</param>
    /// <returns>截取后的文本</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TruncateByLines(this string text, int maxLines, string placeholder = "...", bool extraSpace = false)
        => StringLines.TruncateByLines(text, maxLines, placeholder, extraSpace);

    /// <summary>
    /// 将多行文本分割为单行文本集合，并截取其中的一部分，其余部分用指定的占位符代替
    /// </summary>
    /// <param name="text">多行文本</param>
    /// <param name="maxLines">最大行数</param>
    /// <param name="truncateFrom">截断位置</param>
    /// <param name="placeholder">占位符，默认为"..."</param>
    /// <param name="extraSpace">是否添加额外的空格，默认为false</param>
    /// <returns>截取后的文本</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TruncateByLines(this string text, int maxLines, StringTruncateFrom truncateFrom, string placeholder = "...", bool extraSpace = false)
        => StringLines.TruncateByLines(text, maxLines, truncateFrom, placeholder, extraSpace);
}