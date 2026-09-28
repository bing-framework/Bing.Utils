using Bing.Text.Truncation;

// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 提供常用字符串截断器。
/// </summary>
public static class StringTruncators
{
    /// <summary>
    /// 按字符串长度截断的实例。
    /// </summary>
    public static IStringTruncator ByLength => FixedLengthTruncator.Instance;

    /// <summary>
    /// 按 Unicode 文本元素截断的实例。
    /// </summary>
    /// <remarks>文本元素边界由当前运行时的 <see cref="System.Globalization.StringInfo" /> 判定。</remarks>
    public static IStringTruncator ByTextElements => FixedTextElementTruncator.Instance;

    /// <summary>
    /// 按字符数量截断的实例。
    /// </summary>
    public static IStringTruncator ByNumberOfCharacters => FixedNumberOfCharactersTruncator.Instance;

    /// <summary>
    /// 按单词数量截断的实例。
    /// </summary>
    public static IStringTruncator ByNumberOfWords => FixedNumberOfWordsTruncator.Instance;

    /// <summary>
    /// 按行数截断的实例。
    /// </summary>
    public static IStringTruncator ByNumberOfLines => FixedNumberOfLinesTruncator.Instance;
}

/// <summary>
/// 提供字符串截断扩展方法。
/// </summary>
public static class StringTruncateExtensions
{
    /// <summary>
    /// 按 Unicode 文本元素截断字符串。
    /// </summary>
    /// <param name="text">要截断的字符串。</param>
    /// <param name="maxLength">最多保留的文本元素数量。</param>
    /// <param name="truncationString">截断后追加的字符串，默认为 "..."。</param>
    /// <param name="shortTruncationString">无法放置完整截断字符串时使用的短截断字符串，默认为 "."。</param>
    /// <param name="from">截断位置，默认为从右侧截断。</param>
    /// <param name="extraSpace">是否在正文和截断字符串之间添加一个空格。</param>
    /// <returns>按文本元素截断后的字符串。</returns>
    /// <remarks>文本元素边界由当前运行时的 <see cref="System.Globalization.StringInfo" /> 判定；不同运行时的 Unicode 数据可能导致边界差异。</remarks>
    public static string TruncateByTextElements(
        this string text, int maxLength, string truncationString = "...", string shortTruncationString = ".",
        StringTruncateFrom from = StringTruncateFrom.Right, bool extraSpace = false) =>
        text.Truncate(maxLength, truncationString, shortTruncationString, StringTruncators.ByTextElements, from, extraSpace);

    /// <summary>
    /// 截断字符串。
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
    /// 截断字符串。
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
    /// 截断字符串。
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
