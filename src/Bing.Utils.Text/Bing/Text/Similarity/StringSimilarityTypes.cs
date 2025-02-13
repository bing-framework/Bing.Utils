namespace Bing.Text.Similarity;

/// <summary>
/// 字符串相似度类型
/// </summary>
public enum StringSimilarityTypes
{
    /// <summary>
    /// 任意相似度类型，包括不同长度的字符串。
    /// </summary>
    Any,

    /// <summary>
    /// 完全相同的字符串。
    /// </summary>
    Same,

    /// <summary>
    /// 主要关注较长字符串的相似度。
    /// </summary>
    MayorLong,

    /// <summary>
    /// 主要关注较短字符串的相似度。
    /// </summary>
    MinorLong,
}