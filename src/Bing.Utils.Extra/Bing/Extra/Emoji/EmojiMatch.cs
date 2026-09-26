namespace Bing.Extra.Emoji;

/// <summary>
/// 原文中的一次完整 Emoji 匹配。
/// </summary>
/// <remarks>位置与长度均以 UTF-16 代码单元为单位。</remarks>
public sealed class EmojiMatch
{
    /// <summary>
    /// 获取匹配的原文片段。
    /// </summary>
    /// <remarks>保留输入的变体选择符形式。</remarks>
    public string Value { get; }

    /// <summary>
    /// 获取从零开始的 UTF-16 索引。
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// 获取原文片段的 UTF-16 长度。
    /// </summary>
    /// <remarks>可与 <see cref="Index" /> 一同用于原字符串的 <see cref="string.Substring(int, int)" />。</remarks>
    public int Length { get; }

    /// <summary>
    /// 获取对应的规范表情元数据。
    /// </summary>
    public EmojiInfo Emoji { get; }

    /// <summary>
    /// 初始化 <see cref="EmojiMatch" /> 类的新实例。
    /// </summary>
    /// <param name="value">匹配的原文片段。</param>
    /// <param name="index">原文中从零开始的 UTF-16 索引。</param>
    /// <param name="length">原文片段的 UTF-16 长度。</param>
    /// <param name="emoji">对应的规范表情元数据。</param>
    internal EmojiMatch(string value, int index, int length, EmojiInfo emoji)
    {
        Value = value;
        Index = index;
        Length = length;
        Emoji = emoji;
    }
}
