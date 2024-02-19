namespace Bing.Text;

/// <summary>
/// 字符串工具
/// </summary>
public static partial class Strings
{
    /// <summary>
    /// 替换，忽略大小写。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceIgnoreCase(string text, string oldValue, string newValue)
    {
        return Replace(text, oldValue, newValue, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 仅替换完整单词。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    public static string ReplaceOnlyWholePhrase(string text, string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(oldValue))
            return text;
        var index = text.IndexOfWholePhrase(oldValue);
        var lastIndex = 0;
        var buffer = new StringBuilder(text.Length);
        while (index >= 0)
        {
            buffer.Append(text, startIndex: lastIndex, count: index - lastIndex);
            buffer.Append(newValue);
            lastIndex = index + oldValue.Length;
            index = text.IndexOfWholePhrase(oldValue, startIndex: index + 1);
        }

        buffer.Append(text, lastIndex, text.Length - lastIndex);
        return buffer.ToString();
    }

    /// <summary>
    /// 仅替换首个命中的值。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    public static string ReplaceFirstOccurrence(string text, string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(oldValue))
            return text;
        var index = text.IndexOfIgnoreCase(oldValue);
        return index switch
        {
            < 0 => text,
            0 => $"{newValue}{text[oldValue.Length..]}",
            _ => $"{text[..index]}{newValue}{text[(index + oldValue.Length)..]}"
        };
    }

    /// <summary>
    /// 仅替换最后一个命中的值。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    public static string ReplaceLastOccurrence(string text, string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(oldValue))
            return text;
        var index = text.LastIndexOfIgnoreCase(oldValue);
        return index switch
        {
            < 0 => text,
            0 => $"{newValue}{text[oldValue.Length..]}",
            _ => $"{text[..index]}{newValue}{text[(index + oldValue.Length)..]}"
        };
    }

    /// <summary>
    /// 仅替换结尾命中的结果，并忽略大小写。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    public static string ReplaceOnlyAtEndIgnoreCase(string text, string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(oldValue))
            return text;
        if (text.EndsWithIgnoreCase(oldValue))
            return $"{text[..^oldValue.Length]}{newValue}";
        return text;
    }

    /// <summary>
    /// 替换。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    /// <param name="comparisonType">字符串比较类型</param>
    public static string Replace(string text, string oldValue, string newValue, StringComparison comparisonType)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(oldValue))
            return text;
        int index = -1, lastIndex = 0;
        var buffer = new StringBuilder(text.Length);
        while ((index = text.IndexOf(oldValue, index + 1, comparisonType)) >= 0)
        {
            buffer.Append(text, lastIndex, index - lastIndex);
            buffer.Append(newValue);
            lastIndex = index + oldValue.Length;
        }

        buffer.Append(text, lastIndex, text.Length - lastIndex);
        return buffer.ToString();
    }

    /// <summary>
    /// 递归替换。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    public static string ReplaceRecursive(string text, string oldValue, string newValue)
    {
        const int maxTries = 1000;
        string temp, ret;
        ret = text.Replace(oldValue, newValue);
        var i = 0;
        do
        {
            i++;
            temp = ret;
            ret = temp.Replace(oldValue, newValue);
        } while (temp != ret || i > maxTries);
        return ret;
    }

    /// <summary>
    /// 用空格来替换所有命中的字符。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="toReplace">替换字符</param>
    public static string ReplaceCharsWithWhiteSpace(string text, params char[] toReplace)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;
        if (toReplace is null || toReplace.Length == 0)
            return text;
        var holder = new char[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            var @char = text[i];
            var matched = false;
            for (var j = 0; j < toReplace.Length; j++)
            {
                if (@char == toReplace[j])
                {
                    holder[i] = ' ';
                    matched = true;
                    break;
                }
            }

            if (!matched)
                holder[i] = @char;
        }
        return new string(holder);
    }

    /// <summary>
    /// 用给定的字符来替换数字。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="toReplace">替换字符</param>
    public static string ReplaceNumbersWith(string text, char toReplace)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;
        var holder = new char[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            var @char = text[i];
            if (@char >= '0' && @char <= '9')
                holder[i] = toReplace;
            else
                holder[i] = @char;
        }
        return new string(holder);
    }

    /// <summary>
    /// 替换指定字符串的指定区间内字符为固定字符。
    /// </summary>
    /// <param name="text">待处理的字符串。</param>
    /// <param name="startInclude">替换起始位置（包含该位置字符）。</param>
    /// <param name="endExclude">替换结束位置（不包含该位置字符）。</param>
    /// <param name="replacedChar">用于替换的字符。</param>
    /// <returns>处理后的字符串，指定范围内的字符被<paramref name="replacedChar"/>替换。</returns>
    /// <remarks>
    /// 如果输入的字符串为空或仅包含空白字符，或者<paramref name="startInclude"/>大于等于字符串的长度，
    /// 或<paramref name="startInclude"/>大于<paramref name="endExclude"/>，方法将返回原始字符串。
    /// <paramref name="endExclude"/>如果超过字符串长度，将被调整为字符串的实际长度，以避免越界访问。
    /// </remarks>
    public static string Replace(string text, int startInclude, int endExclude, char replacedChar)
    {
        if (text is null)
            return null;
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        if (startInclude >= text.Length || startInclude > endExclude)
            return text;
        endExclude = Math.Min(endExclude, text.Length);
        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
            sb.Append(i >= startInclude && i < endExclude ? replacedChar : text[i]);
        return sb.ToString();
    }
}

/// <summary>
/// 字符串扩展
/// </summary>
public static partial class StringsExtensions
{
    /// <summary>
    /// 替换，忽略大小写。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceIgnoreCase(this string text, string oldValue, string newValue)
    {
        return Strings.ReplaceIgnoreCase(text, oldValue, newValue);
    }

    /// <summary>
    /// 仅替换完整单词。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceOnlyWholePhrase(this string text, string oldValue, string newValue)
    {
        return Strings.ReplaceOnlyWholePhrase(text, oldValue, newValue);
    }

    /// <summary>
    /// 仅替换首个命中的值。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceFirstOccurrence(this string text, string oldValue, string newValue)
    {
        return Strings.ReplaceFirstOccurrence(text, oldValue, newValue);
    }

    /// <summary>
    /// 仅替换最后一个命中的值。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceLastOccurrence(this string text, string oldValue, string newValue)
    {
        return Strings.ReplaceLastOccurrence(text, oldValue, newValue);
    }

    /// <summary>
    /// 仅替换结尾命中的结果，并忽略大小写。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceOnlyAtEndIgnoreCase(this string text, string oldValue, string newValue)
    {
        return Strings.ReplaceOnlyAtEndIgnoreCase(text, oldValue, newValue);
    }

    /// <summary>
    /// 替换。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    /// <param name="comparisonType">字符串比较类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Replace(this string text, string oldValue, string newValue, StringComparison comparisonType)
    {
        return Strings.Replace(text, oldValue, newValue, comparisonType);
    }

    /// <summary>
    /// 递归替换。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceRecursive(this string text, string oldValue, string newValue)
    {
        return Strings.ReplaceRecursive(text, oldValue, newValue);
    }

    /// <summary>
    /// 用空格来替换所有命中的字符。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="toReplace">替换字符</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceCharsWithWhiteSpace(this string text, params char[] toReplace)
    {
        return Strings.ReplaceCharsWithWhiteSpace(text, toReplace);
    }

    /// <summary>
    /// 用给定的字符来替换数字。
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="toReplace">替换字符</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceNumbersWith(this string text, char toReplace)
    {
        return Strings.ReplaceNumbersWith(text, toReplace);
    }
}