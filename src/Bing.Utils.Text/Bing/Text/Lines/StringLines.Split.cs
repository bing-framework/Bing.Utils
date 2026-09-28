// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 提供逐行处理文本的方法。
/// </summary>
public static partial class StringLines
{
    /// <summary>
    /// 按行分割文本。
    /// </summary>
    /// <param name="text">待分割文本。</param>
    /// <returns>单行文本集合。</returns>
    public static IEnumerable<string> SplitByLines(string text)
    {
        var index = 0;
        while (true)
        {
            var lineBreakIndex = FindLineBreak(text, index);
            if (lineBreakIndex < 0)
            {
                if (text.Length > index)
                    yield return text.Substring(index);
                yield break;
            }

            yield return text.Substring(index, lineBreakIndex - index);
            index = lineBreakIndex + GetLineBreakLength(text, lineBreakIndex);
        }
    }

    /// <summary>
    /// 查找文本中的下一个换行符。
    /// </summary>
    /// <param name="text">待扫描文本。</param>
    /// <param name="startIndex">扫描起始索引。</param>
    /// <returns>换行符索引；未找到时返回 -1。</returns>
    private static int FindLineBreak(string text, int startIndex)
    {
        for (var index = startIndex; index < text.Length; index++)
        {
            if (text[index] is '\r' or '\n')
                return index;
        }

        return -1;
    }

    /// <summary>
    /// 获取指定位置换行符的代码单元长度。
    /// </summary>
    /// <param name="text">待检查文本。</param>
    /// <param name="index">换行符索引。</param>
    /// <returns>CRLF 返回 2，其余换行符返回 1。</returns>
    private static int GetLineBreakLength(string text, int index)
        => text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n' ? 2 : 1;

    /// <summary>
    /// 逐行分割并转换文本。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="text">待分割文本。</param>
    /// <returns>转换后的目标类型数组。</returns>
    /// <remarks>先去除文本两端空白，再忽略空行。</remarks>
    public static T[] SplitInLinesTyped<T>(string text) where T : IComparable
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<T>();

        return SplitByLines(text.Trim())
            .Where(line => !string.IsNullOrEmpty(line))
            .Select(line => (T)Convert.ChangeType(line, typeof(T)))
            .ToArray();
    }

    /// <summary>
    /// 按行分割并移除空行。
    /// </summary>
    /// <param name="text">待分割文本。</param>
    /// <returns>非空单行文本数组。</returns>
    public static string[] SplitInLinesWithoutEmpty(string text)
        => SplitByLines(text)
            .Where(line => !string.IsNullOrEmpty(line))
            .ToArray();
}

/// <summary>
/// 提供逐行处理文本的扩展方法。
/// </summary>
public static partial class StringLinesExtensions
{
    /// <summary>
    /// 按行分割文本。
    /// </summary>
    /// <param name="text">待分割文本。</param>
    /// <returns>单行文本集合。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<string> SplitByLines(this string text) => StringLines.SplitByLines(text);

    /// <summary>
    /// 逐行分割并转换文本。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="text">待分割文本。</param>
    /// <returns>转换后的目标类型数组。</returns>
    /// <remarks>先去除文本两端空白，再忽略空行。</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] SplitInLinesTyped<T>(this string text) where T : IComparable
        => StringLines.SplitInLinesTyped<T>(text);

    /// <summary>
    /// 按行分割并移除空行。
    /// </summary>
    /// <param name="text">待分割文本。</param>
    /// <returns>非空单行文本数组。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] SplitInLinesWithoutEmpty(this string text) => StringLines.SplitInLinesWithoutEmpty(text);
}
