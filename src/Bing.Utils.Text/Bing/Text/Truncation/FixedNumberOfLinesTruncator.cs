namespace Bing.Text.Truncation;

/// <summary>
/// 固定行数的字符串截断器
/// </summary>
internal class FixedNumberOfLinesTruncator : IStringTruncator
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

        var numberOfLines = text.CountByLines();
        if (numberOfLines <= maxLength)
            return text;
        var builder = new StringBuilder();
        var counter = 0;
        foreach (var line in text.SplitByLines())
        {
            if (++counter == maxLength)
            {
                if (truncateFrom == StringTruncateFrom.Left)
                    builder.Insert(0, truncationString);
                else
                    builder.AppendLine(truncationString);
                break;
            }
            builder.AppendLine(line);
        }
        return builder.ToString();
    }

    /// <summary>
    /// 实例
    /// </summary>
    public static FixedNumberOfLinesTruncator Instance { get; } = new();
}