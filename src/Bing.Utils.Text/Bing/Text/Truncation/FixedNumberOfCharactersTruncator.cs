namespace Bing.Text.Truncation;

/// <summary>
/// 固定数量字母或数字的字符截断器
/// </summary>
internal class FixedNumberOfCharactersTruncator : IStringTruncator
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

        var numberOfLetterOrDigit = text.ToCharArray().Count(char.IsLetterOrDigit);
        if (numberOfLetterOrDigit <= maxLength)
            return text;

        if (shortTruncationString.Length > maxLength)
            return truncateFrom == StringTruncateFrom.Left
                ? text.Substring(text.Length - maxLength)
                : text.Substring(0, maxLength);

        var spacePlaceholder = extraSpace ? " " : "";
        if (shortTruncationString.Length < maxLength && truncationString.Length > maxLength)
            return truncateFrom == StringTruncateFrom.Left
                ? TruncateFromLeft(ref text, ref maxLength, ref shortTruncationString, ref spacePlaceholder)
                : TruncateFromRight(ref text, ref maxLength, ref shortTruncationString, ref spacePlaceholder);

        if (numberOfLetterOrDigit <= truncationString.Length)
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
    /// <param name="maxLength">最大长度</param>
    /// <param name="truncationString">截断后追加的字符串</param>
    /// <param name="spacePlaceholder">额外的空格占位符</param>
    /// <returns>截断后的字符串</returns>
    private static string TruncateFromRight(ref string text, ref int maxLength, ref string truncationString, ref string spacePlaceholder)
    {
        var alphaNumericalCharactersProcessed = 0;
        for (var i = 0; i < text.Length - truncationString.Length; i++)
        {
            if (char.IsLetterOrDigit(text[i]))
                alphaNumericalCharactersProcessed++;
            if (alphaNumericalCharactersProcessed + truncationString.Length == maxLength)
                return $"{text.Substring(0, i + 1)}{spacePlaceholder}{truncationString}";
        }
        return text;
    }

    /// <summary>
    /// 从左侧截断字符串
    /// </summary>
    /// <param name="text">要截断的字符串</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="truncationString">截断后追加的字符串</param>
    /// <param name="spacePlaceholder">额外的空格占位符</param>
    /// <returns>截断后的字符串</returns>
    private static string TruncateFromLeft(ref string text, ref int maxLength, ref string truncationString, ref string spacePlaceholder)
    {
        var alphaNumericalCharactersProcessed = 0;
        for (var i = text.Length - 1; i > 0; i--)
        {
            if (char.IsLetterOrDigit(text[i]))
                alphaNumericalCharactersProcessed++;
            if (alphaNumericalCharactersProcessed + truncationString.Length == maxLength)
                return $"{truncationString}{spacePlaceholder}{text.Substring(i)}";
        }
        return text;
    }

    /// <summary>
    /// 实例
    /// </summary>
    public static FixedNumberOfCharactersTruncator Instance { get; } = new();
}