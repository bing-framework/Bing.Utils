namespace Bing.Text.Truncation;

/// <summary>
/// 字符串截断器
/// </summary>
public interface IStringTruncator
{
    /// <summary>
    /// 截断字符串
    /// </summary>
    /// <param name="text">要截断的字符串</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="truncationString">截断后追加的字符串，默认为"..."</param>
    /// <param name="shortTruncationString">短截断字符串，默认为"."</param>
    /// <param name="truncateFrom">截断位置，默认为从右侧截断</param>
    /// <param name="extraSpace">是否添加额外的空格，默认为false</param>
    /// <returns>截断后的字符串</returns>
    string Truncate(string text, int maxLength, string truncationString = "...", string shortTruncationString = ".", StringTruncateFrom truncateFrom = StringTruncateFrom.Right, bool extraSpace = false);
}