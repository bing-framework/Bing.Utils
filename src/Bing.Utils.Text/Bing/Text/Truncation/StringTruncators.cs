using Bing.Text.Truncation;

// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串截断器实例
/// </summary>
public static class StringTruncators
{
    /// <summary>
    /// 固定长度截断器
    /// </summary>
    public static IStringTruncator ByLength => FixedLengthTruncator.Instance;

    /// <summary>
    /// 固定数量字符截断器
    /// </summary>
    public static IStringTruncator ByNumberOfCharacters => FixedNumberOfCharactersTruncator.Instance;

    /// <summary>
    /// 固定数量单词截断器
    /// </summary>
    public static IStringTruncator ByNumberOfWords => FixedNumberOfWordsTruncator.Instance;

    /// <summary>
    /// 固定数量行截断器
    /// </summary>
    public static IStringTruncator ByNumberOfLines => FixedNumberOfLinesTruncator.Instance;
}

/// <summary>
/// 字符串 - 截断(<see cref="string"/>) 扩展
/// </summary>
public static class StringTruncateExtensions
{
    /// <summary>
    /// 截断，并用给定的字符串开头或结尾。
    /// </summary>
    /// <param name="text">要截断的字符串</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="truncationString">截断后追加的字符串，默认为"..."</param>
    /// <param name="shortTruncationString">短截断字符串，默认为"."</param>
    /// <param name="from">截断位置，默认为从右侧截断</param>
    /// <param name="extraSpace">是否添加额外的空格，默认为false</param>
    /// <returns>截断后的字符串</returns>
    public static string Truncate(
        this string text, int maxLength, string truncationString = "...", string shortTruncationString = ".",
        StringTruncateFrom from = StringTruncateFrom.Right, bool extraSpace = false) =>
        text.Truncate(maxLength, truncationString, shortTruncationString, StringTruncators.ByLength, from, extraSpace);

    /// <summary>
    /// 截断，并用给定的字符串开头或结尾。
    /// </summary>
    /// <param name="text">要截断的字符串</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="truncator">截断器</param>
    /// <param name="from">截断位置，默认为从右侧截断</param>
    /// <param name="extraSpace">是否添加额外的空格，默认为false</param>
    /// <returns>截断后的字符串</returns>
    public static string Truncate(
        this string text, int maxLength,
        IStringTruncator truncator, StringTruncateFrom from = StringTruncateFrom.Right, bool extraSpace = false) =>
        text.Truncate(maxLength, "...", ".", truncator, from, extraSpace);

    /// <summary>
    /// 截断，并用给定的字符串开头或结尾。
    /// </summary>
    /// <param name="text">要截断的字符串</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="truncationString">截断后追加的字符串</param>
    /// <param name="shortTruncationString">短截断字符串</param>
    /// <param name="truncator">截断器</param>
    /// <param name="from">截断位置，默认为从右侧截断</param>
    /// <param name="extraSpace">是否添加额外的空格，默认为false</param>
    /// <returns>截断后的字符串</returns>
    public static string Truncate(
        this string text, int maxLength, string truncationString, string shortTruncationString,
        IStringTruncator truncator, StringTruncateFrom from = StringTruncateFrom.Right, bool extraSpace = false)
    {
        truncator ??= StringTruncators.ByLength;
        return truncator.Truncate(text, maxLength, truncationString, shortTruncationString, from, extraSpace);
    }
}