namespace Bing.Text.Truncation;

/// <summary>
/// 固定长度的字符串截断器
/// </summary>
internal class FixedLengthTruncator : IStringTruncator
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

        if (text.Length <= maxLength)
            return text;

        if (shortTruncationString.Length > maxLength)
            return truncateFrom == StringTruncateFrom.Left
                ? Strings.Right(text, maxLength)
                : Strings.Left(text, maxLength);

        var spacePlaceholder = extraSpace ? " " : "";

        if (shortTruncationString.Length < maxLength && truncationString.Length > maxLength)
            return truncateFrom == StringTruncateFrom.Left
                ? $"{shortTruncationString}{spacePlaceholder}{Strings.Right(text, truncationString.Length - shortTruncationString.Length)}"
                : $"{Strings.Left(text, truncationString.Length - shortTruncationString.Length)}{spacePlaceholder}{shortTruncationString}";

        if (text.Length <= truncationString.Length)
            return truncateFrom == StringTruncateFrom.Left
                ? $"{shortTruncationString}{spacePlaceholder}{Strings.Right(text, truncationString.Length - shortTruncationString.Length)}"
                : $"{Strings.Left(text, truncationString.Length - shortTruncationString.Length)}{spacePlaceholder}{shortTruncationString}";
        return truncateFrom == StringTruncateFrom.Left
            ? $"{truncationString}{spacePlaceholder}{Strings.Right(text, maxLength - truncationString.Length)}"
            : $"{Strings.Left(text, maxLength - truncationString.Length)}{spacePlaceholder}{truncationString}";
    }

    /// <summary>
    /// 实例
    /// </summary>
    public static FixedLengthTruncator Instance { get; } = new();
}