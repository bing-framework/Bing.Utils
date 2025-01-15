namespace Bing.Text.Truncation;

/// <summary>
/// 固定数量单词的字符串截断器
/// </summary>
internal class FixedNumberOfWordsTruncator : IStringTruncator
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
        if (truncationString.Length < shortTruncationString.Length)
            (shortTruncationString, truncationString) = (truncationString, shortTruncationString);

        var numberOfWords = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (numberOfWords <= maxLength)
            return text;

        var spacePlaceholder = extraSpace ? " " : "";
        var empty = string.Empty;

        if (shortTruncationString.Length > maxLength)
            return truncateFrom == StringTruncateFrom.Left
                ? TruncateFromLeft(ref text, ref maxLength, ref empty, ref spacePlaceholder)
                : TruncateFromRight(ref text, ref maxLength, ref empty, ref spacePlaceholder);

        if (shortTruncationString.Length < maxLength && truncationString.Length > maxLength)
            return truncateFrom == StringTruncateFrom.Left
                ? TruncateFromLeft(ref text, ref maxLength, ref shortTruncationString, ref spacePlaceholder)
                : TruncateFromRight(ref text, ref maxLength, ref shortTruncationString, ref spacePlaceholder);

        return truncateFrom == StringTruncateFrom.Left
            ? TruncateFromLeft(ref text, ref maxLength, ref truncationString, ref spacePlaceholder)
            : TruncateFromRight(ref text, ref maxLength, ref truncationString, ref spacePlaceholder);
    }

    /// <summary>
    /// 从右侧截断字符串
    /// </summary>
    /// <param name="text">要截断的字符串</param>
    /// <param name="length">长度</param>
    /// <param name="truncationString">截断后追加的字符串</param>
    /// <param name="spacePlaceholder">额外的空格占位符</param>
    /// <returns>截断后的字符串</returns>
    private static string TruncateFromRight(ref string text, ref int length, ref string truncationString, ref string spacePlaceholder)
    {
        var lastCharactersWasWhiteSpace = true;
        var numberOfWordsProcessed = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                if (!lastCharactersWasWhiteSpace)
                    numberOfWordsProcessed++;
                lastCharactersWasWhiteSpace = true;
                if (numberOfWordsProcessed == length)
                    return $"{text.Substring(0, i)}{spacePlaceholder}{truncationString}";
            }
            else
            {
                lastCharactersWasWhiteSpace = false;
            }
        }
        return $"{text}{spacePlaceholder}{truncationString}";
    }

    /// <summary>
    /// 从左侧截断字符串
    /// </summary>
    /// <param name="text">要截断的字符串</param>
    /// <param name="length">长度</param>
    /// <param name="truncationString">截断后追加的字符串</param>
    /// <param name="spacePlaceholder">额外的空格占位符</param>
    /// <returns>截断后的字符串</returns>
    private static string TruncateFromLeft(ref string text, ref int length, ref string truncationString, ref string spacePlaceholder)
    {
        var lastCharactersWasWhiteSpace = true;
        var numberOfWordsProcessed = 0;
        for (var i = text.Length - 1; i > 0; i--)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                if (!lastCharactersWasWhiteSpace)
                    numberOfWordsProcessed++;
                lastCharactersWasWhiteSpace = true;
                if (numberOfWordsProcessed == length)
                    return $"{truncationString}{spacePlaceholder}{text.Substring(i + 1).TrimEnd()}";
            }
            else
            {
                lastCharactersWasWhiteSpace = false;
            }
        }
        return $"{truncationString}{spacePlaceholder}{text}";
    }

    /// <summary>
    /// 实例
    /// </summary>
    public static FixedNumberOfWordsTruncator Instance { get; } = new();
}