namespace Bing.Text.Matching;

/// <summary>
/// 关键词匹配结果。
/// </summary>
/// <remarks>
/// 索引和长度使用 .NET 字符串的 UTF-16 代码单元，可直接用于原文切片。
/// </remarks>
public sealed class KeywordMatch
{
    /// <summary>
    /// 初始化一个 <see cref="KeywordMatch"/> 类型的实例。
    /// </summary>
    /// <param name="keyword">词库中的关键词。</param>
    /// <param name="value">原文中的匹配内容。</param>
    /// <param name="index">匹配内容在原文中的 UTF-16 起始索引。</param>
    /// <param name="length">匹配内容的 UTF-16 长度。</param>
    internal KeywordMatch(string keyword, string value, int index, int length)
    {
        Keyword = keyword;
        Value = value;
        Index = index;
        Length = length;
    }

    /// <summary>
    /// 词库关键词。
    /// </summary>
    public string Keyword { get; }

    /// <summary>
    /// 原文匹配内容。
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// 匹配起始索引。
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// 匹配长度。
    /// </summary>
    public int Length { get; }
}
