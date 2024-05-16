namespace Bing.Text;

/// <summary>
/// 字符串工具
/// </summary>
public static partial class Strings
{
    /// <summary>
    /// 从字符串开始处获取指定长度的子字符串
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="length">需要获取的子字符串的长度</param>
    /// <returns>如果原始字符串的长度大于指定长度，则返回原始字符串的前length个字符组成的子字符串；否则，返回原始字符串。如果原始字符串为空或仅包含空格，则返回空字符串。</returns>
    public static string Take(string text, int length)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        return text.Length > length ? text.Substring(0, length) : text;
    }

    /// <summary>
    /// 截断字符串。从字符串中提取指定范围的子字符串。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="fromIndexInclude">子字符串开始的索引（包含）。正值从字符串开头计数，负值从字符串末尾反向计数。</param>
    /// <param name="toIndexExclude">子字符串结束的索引（不包含）。正值从字符串开头计数，负值从字符串末尾反向计数。</param>
    /// <returns>
    /// 根据指定的开始和结束索引提取的子字符串。
    /// 如果输入字符串为空或仅包含空白字符，则返回空字符串。
    /// 如果计算后的开始和结束索引相同，也返回空字符串。
    /// </returns>
    /// <remarks>
    /// 该方法自动调整索引值以确保它们在有效范围内，并且如果结束索引小于开始索引，则自动交换这两个索引。
    /// </remarks>
    public static string Substring(string text, int fromIndexInclude, int toIndexExclude)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        var len = text.Length;

        // 调整fromIndexInclude为有效范围
        if (fromIndexInclude < 0)
        {
            fromIndexInclude = len + fromIndexInclude;
            if (fromIndexInclude < 0)
                fromIndexInclude = 0;
        }
        else if (fromIndexInclude > len)
        {
            fromIndexInclude = len;
        }

        // 调整toIndexExclude为有效范围
        if (toIndexExclude < 0)
        {
            toIndexExclude = len + toIndexExclude;
            if (toIndexExclude < 0)
                toIndexExclude = len;
        }
        else if (toIndexExclude > len)
        {
            toIndexExclude = len;
        }

        // 如果toIndexExclude小于fromIndexInclude，交换它们
        if (toIndexExclude < fromIndexInclude)
            (fromIndexInclude, toIndexExclude) = (toIndexExclude, fromIndexInclude);

        // 如果fromIndexInclude等于toIndexExclude，返回空字符串
        if (fromIndexInclude == toIndexExclude)
            return string.Empty;

        // 返回子字符串
        return text.Substring(fromIndexInclude, toIndexExclude - fromIndexInclude);
    }

    /// <summary>
    /// 截断字符串。从指定位置开始，截取指定长度的子字符串。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="fromIndex">开始截取的索引位置（包含）。</param>
    /// <param name="length">截取的长度。</param>
    /// <returns>截取后的字符串。</returns>
    /// <remarks>
    /// 如果fromIndex为负，则表示从字符串的末尾开始计算的索引。
    /// 长度为正值时，表示从fromIndex开始向字符串末尾截取；为负值时，表示从fromIndex开始向字符串开头截取。
    /// </remarks>
    public static string SubstringWithLength(string text, int fromIndex, int length)
    {
        int toIndex;
        if (fromIndex < 0)
            toIndex = fromIndex - length;
        else
            toIndex = fromIndex + length;
        return Substring(text, fromIndex, toIndex);
    }

    /// <summary>
    /// 截断字符串。截取分隔字符串之前的子字符串，不包括分隔字符串。
    /// </summary>
    /// <remarks>
    /// 例如：
    /// <para>1. Strings.SubstringBefore(null, *, false)        = null</para>
    /// <para>2. Strings.SubstringBefore("", *, false)          = ""</para>
    /// <para>3. Strings.SubstringBefore("abc", "a", false)     = ""</para>
    /// <para>4. Strings.SubstringBefore("abcba", "b", false)   = "a"</para>
    /// <para>5. Strings.SubstringBefore("abc", "c", false)     = "ab"</para>
    /// <para>6. Strings.SubstringBefore("abc", "d", false)     = "abc"</para>
    /// <para>7. Strings.SubstringBefore("abc", "", false)      = ""</para>
    /// <para>8. Strings.SubstringBefore("abc", null, false)    = "abc"</para>
    /// </remarks>
    /// <param name="text">原始字符串</param>
    /// <param name="separator">分隔字符串（不包含）</param>
    /// <param name="isLastSeparator">是否查找最后一个分隔字符串（多次出现分隔字符串时选取最后一个），true为选取最后一个</param>
    /// <param name="comparison">字符串比较规则，允许忽略大小写等比较选项，默认为不区分大小写。</param>
    /// <returns>返回从字符串开头到指定分隔符之前的部分。如果找不到分隔符，返回整个字符串。如果分隔符位于字符串开头，返回空字符串。</returns>
    public static string SubstringBefore(string text, string separator, bool isLastSeparator, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (string.IsNullOrEmpty(text) || separator == null)
            return text;
        if (string.IsNullOrEmpty(separator))
            return string.Empty;
        // 根据isLastSeparator标志选择查找最后一个还是第一个分隔符的位置
        var pos = isLastSeparator ? text.LastIndexOf(separator, comparison) : text.IndexOf(separator, comparison);
        return pos switch
        {
            // 如果没找到分隔符，返回原字符串
            -1 => text,
            // 如果分隔符位于字符串的开头，返回空字符串
            0 => string.Empty,
            _ => text[..pos]
        };
    }

    /// <summary>
    /// 截断字符串。截取分隔字符之前的子字符串，不包括分隔字符。
    /// </summary>
    /// <remarks>
    /// 例如：
    /// <para>1. Strings.SubstringBefore(null, *, false)        = null</para>
    /// <para>2. Strings.SubstringBefore("", *, false)          = ""</para>
    /// <para>3. Strings.SubstringBefore("abc", 'a', false)     = ""</para>
    /// <para>4. Strings.SubstringBefore("abcba", 'b', false)   = "a"</para>
    /// <para>5. Strings.SubstringBefore("abc", 'c', false)     = "ab"</para>
    /// <para>6. Strings.SubstringBefore("abc", 'd', false)     = "abc"</para>
    /// </remarks>
    /// <param name="text">原始字符串</param>
    /// <param name="separator">分隔字符（不包含）</param>
    /// <param name="isLastSeparator">是否查找最后一个分隔字符串（多次出现分隔字符串时选取最后一个），true为选取最后一个</param>
    /// <returns>返回从字符串开头到指定分隔符之前的部分。如果找不到分隔符，返回整个字符串。如果分隔符位于字符串开头，返回空字符串。</returns>
    public static string SubstringBefore(string text, char separator, bool isLastSeparator)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        // 根据isLastSeparator标志选择查找最后一个还是第一个分隔符的位置
        var pos = isLastSeparator ? text.LastIndexOf(separator) : text.IndexOf(separator);
        return pos switch
        {
            // 如果没找到分隔符，返回原字符串
            -1 => text,
            // 如果分隔符位于字符串的开头，返回空字符串
            0 => string.Empty,
            _ => text[..pos]
        };
    }

    /// <summary>
    /// 截断字符串。截取分隔字符串之后的子字符串，不包括分隔字符串。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="separator">分隔字符串（不包含）</param>
    /// <param name="isLastSeparator">是否查找最后一个分隔字符串（多次出现分隔字符串时选取最后一个），true为选取最后一个</param>
    /// <param name="comparison">字符串比较规则，允许忽略大小写等比较选项，默认为不区分大小写。</param>
    /// <returns>返回从指定分隔符之后到字符串末尾的部分。如果输入字符串为空，返回原字符串。如果分隔符为空，或未找到分隔符，或分隔符位于字符串的末尾，返回空字符串。</returns>
    public static string SubstringAfter(string text, string separator, bool isLastSeparator, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        if (separator == null)
            return string.Empty;
        // 根据isLastSeparator标志选择查找最后一个还是第一个分隔符的位置
        var pos = isLastSeparator ? text.LastIndexOf(separator, comparison) : text.IndexOf(separator, comparison);
        // 如果找不到分隔符，或分隔符位于字符串的末尾，返回空字符串
        if (pos == -1 || text.Length - 1 == pos)
            return string.Empty;
        return text[(pos + separator.Length)..];
    }

    /// <summary>
    /// 截断字符串。截取分隔字符之后的子字符串，不包括分隔字符。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="separator">分隔字符（不包含）</param>
    /// <param name="isLastSeparator">是否查找最后一个分隔字符串（多次出现分隔字符串时选取最后一个），true为选取最后一个</param>
    /// <returns>返回从指定分隔符之后到字符串末尾的部分。如果输入字符串为空，返回原字符串。如果分隔符为空，或未找到分隔符，或分隔符位于字符串的末尾，返回空字符串。</returns>
    public static string SubstringAfter(string text, char separator, bool isLastSeparator)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        // 根据isLastSeparator标志选择查找最后一个还是第一个分隔符的位置
        var pos = isLastSeparator ? text.LastIndexOf(separator) : text.IndexOf(separator);
        // 如果找不到分隔符，或分隔符位于字符串的末尾，返回空字符串
        if (pos == -1 || text.Length - 1 == pos)
            return string.Empty;
        return text[(pos + 1)..];
    }

    /// <summary>
    /// 截断字符串。截取指定字符串中间部分的子字符串，不包含标识字符串。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="before">截取开始的字符串标识</param>
    /// <param name="after">截取到的字符串标识</param>
    /// <param name="comparison">字符串比较规则，允许忽略大小写等比较选项，默认为不区分大小写。</param>
    /// <returns>如果找到了<paramref name="before"/>和<paramref name="after"/>，并且它们之间有内容，则返回这部分内容；否则返回空字符串。</returns>
    public static string SubstringBetween(string text, string before, string after, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(before) || string.IsNullOrEmpty(after))
            return string.Empty;
        var start = text.IndexOf(before, comparison);
        if (start != -1)
        {
            // 在找到的前缀字符串之后查找后缀字符串的位置
            start += before.Length;
            var end = text.IndexOf(after, start, comparison);
            if (end != -1)
            {
                // 截取并返回两个字符串之间的子字符串
                return text.Substring(start, end - start);
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// 截断字符串。截取指定字符串中间部分的子字符串，不包含标识字符串。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="beforeAndAfter">截取开始和结束的字符串标识</param>
    /// <param name="comparison">字符串比较规则，允许忽略大小写等比较选项，默认为不区分大小写。</param>
    /// <returns>如果找到了界定字符串，并且它们之间有内容，则返回这部分内容；否则返回空字符串。</returns>
    public static string SubstringBetween(string text, string beforeAndAfter, StringComparison comparison = StringComparison.OrdinalIgnoreCase) => 
        SubstringBetween(text, beforeAndAfter, beforeAndAfter, comparison);
}