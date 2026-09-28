// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 提供逐行处理文本的方法。
/// </summary>
public static partial class StringLines
{
    /// <summary>
    /// 统计文本行数。
    /// </summary>
    /// <param name="text">待统计文本。</param>
    /// <returns>文本中的行数。</returns>
    public static int CountByLines(string text)
    {
        int index = 0, lines = 0;
        while (true)
        {
            var lineBreakIndex = FindLineBreak(text, index);
            if (lineBreakIndex < 0)
            {
                if (text.Length > index)
                    lines++;
                return lines;
            }

            index = lineBreakIndex + GetLineBreakLength(text, lineBreakIndex);
            lines++;
        }
    }
}

/// <summary>
/// 提供逐行处理文本的扩展方法。
/// </summary>
public static partial class StringLinesExtensions
{
    /// <summary>
    /// 统计文本行数。
    /// </summary>
    /// <param name="text">待统计文本。</param>
    /// <returns>文本中的行数。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountByLines(this string text) => StringLines.CountByLines(text);
}
