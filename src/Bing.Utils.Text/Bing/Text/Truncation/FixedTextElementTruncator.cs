using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bing.Text.Truncation;

/// <summary>
/// 按 Unicode 文本元素安全截断字符串。
/// </summary>
internal sealed class FixedTextElementTruncator : IStringTruncator
{
    /// <inheritdoc />
    public string Truncate(string text, int maxLength, string truncationString = "...", string shortTruncationString = ".", StringTruncateFrom truncateFrom = StringTruncateFrom.Right, bool extraSpace = false)
    {
        if (string.IsNullOrEmpty(text) || maxLength < 0)
            return string.Empty;
        if (maxLength == 0)
            return text;

        if (string.IsNullOrEmpty(truncationString))
            truncationString = "...";
        if (string.IsNullOrEmpty(shortTruncationString))
            shortTruncationString = ".";
        if (GetTextElementCount(truncationString) < GetTextElementCount(shortTruncationString))
            (shortTruncationString, truncationString) = (truncationString, shortTruncationString);

        var elements = GetTextElements(text);
        if (elements.Count <= maxLength)
            return text;

        var separatorCount = extraSpace ? 1 : 0;
        var marker = truncationString;
        var markerCount = GetTextElementCount(marker);
        if (markerCount + separatorCount > maxLength)
        {
            marker = shortTruncationString;
            markerCount = GetTextElementCount(marker);
        }

        if (markerCount + separatorCount > maxLength)
            return TakeElements(elements, maxLength, truncateFrom);

        var sourceCount = maxLength - markerCount - separatorCount;
        var source = TakeElements(elements, sourceCount, truncateFrom);
        var separator = extraSpace ? " " : string.Empty;
        return truncateFrom == StringTruncateFrom.Left
            ? marker + separator + source
            : source + separator + marker;
    }

    /// <summary>
    /// 获取字符串包含的 Unicode 文本元素数量。
    /// </summary>
    /// <param name="text">要统计的文本。</param>
    /// <returns>文本元素数量。</returns>
    private static int GetTextElementCount(string text)
    {
        var count = 0;
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
            count++;
        return count;
    }

    /// <summary>
    /// 将字符串拆分为 Unicode 文本元素。
    /// </summary>
    /// <param name="text">要拆分的文本。</param>
    /// <returns>按原文顺序排列的文本元素列表。</returns>
    private static List<string> GetTextElements(string text)
    {
        var elements = new List<string>();
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
            elements.Add(enumerator.GetTextElement());
        return elements;
    }

    /// <summary>
    /// 从文本元素列表的一侧截取指定数量的元素。
    /// </summary>
    /// <param name="elements">原文本元素列表。</param>
    /// <param name="count">要保留的元素数量。</param>
    /// <param name="truncateFrom">保留文本的方向。</param>
    /// <returns>截取后的文本。</returns>
    private static string TakeElements(IReadOnlyList<string> elements, int count, StringTruncateFrom truncateFrom)
    {
        if (count <= 0)
            return string.Empty;

        var builder = new StringBuilder();
        if (truncateFrom == StringTruncateFrom.Left)
        {
            var start = elements.Count - count;
            for (var index = start; index < elements.Count; index++)
                builder.Append(elements[index]);
        }
        else
        {
            for (var index = 0; index < count && index < elements.Count; index++)
                builder.Append(elements[index]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// 文本元素截断器的共享实例。
    /// </summary>
    public static FixedTextElementTruncator Instance { get; } = new FixedTextElementTruncator();
}
