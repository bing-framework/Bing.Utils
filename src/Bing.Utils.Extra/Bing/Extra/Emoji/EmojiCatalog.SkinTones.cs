using System;
using System.Collections.Generic;

namespace Bing.Extra.Emoji;

/// <summary>
/// Emoji 序列前缀树及元数据索引。
/// </summary>
public sealed partial class EmojiCatalog
{
    /// <summary>
    /// 将目录中的单肤色表情应用为指定肤色。
    /// </summary>
    /// <param name="text">待处理文本。</param>
    /// <param name="tone">目标肤色等级。</param>
    /// <returns>处理后的文本；text 为 null 时返回 null。</returns>
    /// <exception cref="ArgumentOutOfRangeException">肤色等级无效。</exception>
    /// <remarks>只使用目录内已有变体；多肤色组合及没有基础项的家族保持原文。</remarks>
    public string ApplySkinTone(string text, EmojiSkinTone tone)
    {
        ValidateSkinTone(tone);
        return Replace(text, match =>
            TryGetSingleSkinToneVariant(match.Emoji.Unicode, tone, out var variant)
                ? variant.Unicode
                : match.Value);
    }

    /// <summary>
    /// 将目录中的表情应用为指定的多肤色序列。
    /// </summary>
    /// <param name="text">待处理文本。</param>
    /// <param name="tones">按出现顺序排列的目标肤色等级。</param>
    /// <returns>处理后的文本；text 为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">tones 为 null。</exception>
    /// <exception cref="ArgumentException">tones 为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">包含无效肤色等级。</exception>
    /// <remarks>仅替换目录中存在的规范变体；已有肤色输入要求目标数量一致。</remarks>
    public string ApplySkinTones(string text, IReadOnlyList<EmojiSkinTone> tones)
    {
        if (tones == null)
            throw new ArgumentNullException(nameof(tones));
        if (tones.Count == 0)
            throw new ArgumentException("至少指定一个肤色等级。", nameof(tones));

        var toneCopy = new EmojiSkinTone[tones.Count];
        for (var index = 0; index < tones.Count; index++)
        {
            ValidateSkinTone(tones[index]);
            toneCopy[index] = tones[index];
        }

        return Replace(text, match =>
            TryGetSkinToneVariant(match.Emoji.Unicode, toneCopy, out var variant)
                ? variant.Unicode
                : match.Value);
    }

    /// <summary>
    /// 移除目录表情中可还原的肤色修饰符。
    /// </summary>
    /// <param name="text">待处理文本。</param>
    /// <returns>处理后的文本；text 为 null 时返回 null。</returns>
    /// <remarks>目录中缺少无肤色基础项的序列保持原文。</remarks>
    public string RemoveSkinTones(string text) => Replace(text, match =>
        TryGetSkinTone(match.Emoji.Unicode, out var info) &&
        info.Tones.Count > 0 && info.BaseEmoji != null
            ? info.BaseEmoji.Unicode
            : match.Value);

    /// <summary>
    /// 验证肤色等级在定义范围内。
    /// </summary>
    /// <param name="tone">待验证的肤色等级。</param>
    private static void ValidateSkinTone(EmojiSkinTone tone)
    {
        if (tone < EmojiSkinTone.Light || tone > EmojiSkinTone.Dark)
            throw new ArgumentOutOfRangeException(nameof(tone));
    }
}
