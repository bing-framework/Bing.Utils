namespace Bing.Text.Segmentation;

/// <summary>
/// 表示原文中的一个分词片段。
/// </summary>
public sealed class ChineseSegment
{
    /// <summary>
    /// 初始化一个 <see cref="ChineseSegment"/> 类型的实例。
    /// </summary>
    /// <param name="value">原文片段。</param>
    /// <param name="index">片段在原文中的 UTF-16 起点。</param>
    internal ChineseSegment(string value, int index)
    {
        Value = value;
        Index = index;
    }

    /// <summary>
    /// 原文片段。
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// 片段起始索引。
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// 片段长度。
    /// </summary>
    public int Length => Value.Length;
}
