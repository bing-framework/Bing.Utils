// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串行工具
/// </summary>
public static partial class StringLines
{
    /// <summary>
    /// 计算行数
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>文本中的行数</returns>
    public static int CountByLines(string text)
    {
        int index = 0, lines = 0;
        while (true)
        {
            var newIndex = text.IndexOf(Environment.NewLine, index, StringComparison.Ordinal);
            if (newIndex < 0)
            {
                if (text.Length > index)
                    lines++;
                return lines;
            }

            index = newIndex + 2;
            lines++;
        }
    }
}

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringLinesExtensions
{
    /// <summary>
    /// 计算行数
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>文本中的行数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountByLines(this string text) => StringLines.CountByLines(text);
}