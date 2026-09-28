using System;
using System.Collections.Generic;
using System.Text;

namespace Bing.Extra.Emoji;

/// <summary>
/// Unicode Emoji 肤色修饰符等级。
/// </summary>
public enum EmojiSkinTone
{
    /// <summary>
    /// 浅色。
    /// </summary>
    Light = 1,

    /// <summary>
    /// 中浅色。
    /// </summary>
    MediumLight = 2,

    /// <summary>
    /// 中等肤色。
    /// </summary>
    Medium = 3,

    /// <summary>
    /// 中深色。
    /// </summary>
    MediumDark = 4,

    /// <summary>
    /// 深色。
    /// </summary>
    Dark = 5
}

/// <summary>
/// 一个完整 Emoji 的肤色变体信息。
/// </summary>
/// <remarks>多人物或多组件序列按原文顺序保存多个肤色等级。</remarks>
public sealed class EmojiSkinToneInfo
{
    /// <summary>
    /// 获取输入对应的规范 Emoji 元数据。
    /// </summary>
    public EmojiInfo Emoji { get; }

    /// <summary>
    /// 获取无肤色基础 Emoji 元数据。
    /// </summary>
    /// <remarks>官方清单没有基础项时返回 null。</remarks>
    public EmojiInfo BaseEmoji { get; }

    /// <summary>
    /// 获取输入中按出现顺序排列的肤色等级。
    /// </summary>
    /// <remarks>输入没有肤色修饰符时为空；多人物或多组件序列按原文顺序返回多个等级。</remarks>
    public IReadOnlyList<EmojiSkinTone> Tones { get; }

    /// <summary>
    /// 获取同一肤色家族的规范序列列表。
    /// </summary>
    /// <remarks>列表包含无肤色基础项（如果存在）及所有受支持的肤色变体。</remarks>
    public IReadOnlyList<EmojiInfo> Variants { get; }

    /// <summary>
    /// 初始化一个 <see cref="EmojiSkinToneInfo" /> 类型的实例。
    /// </summary>
    /// <param name="emoji">输入对应的规范 Emoji 元数据。</param>
    /// <param name="baseEmoji">无肤色基础 Emoji 元数据。</param>
    /// <param name="variants">同一家族的规范序列。</param>
    /// <param name="tones">输入中的肤色等级。</param>
    /// <remarks>复制集合并提供只读视图，避免外部修改内置元数据。</remarks>
    internal EmojiSkinToneInfo(EmojiInfo emoji, EmojiInfo baseEmoji, IReadOnlyList<EmojiInfo> variants, EmojiSkinTone[] tones)
    {
        Emoji = emoji;
        BaseEmoji = baseEmoji;
        var variantCopy = new EmojiInfo[variants.Count];
        for (var index = 0; index < variants.Count; index++)
            variantCopy[index] = variants[index];
        Variants = Array.AsReadOnly(variantCopy);
        Tones = Array.AsReadOnly((EmojiSkinTone[])tones.Clone());
    }
}

/// <summary>
/// 肤色修饰符的内部解析工具。
/// </summary>
internal static class EmojiSkinToneUtilities
{
    /// <summary>
    /// 获取用于家族索引的基础序列，并返回其中的肤色等级。
    /// </summary>
    /// <param name="value">规范 Emoji 序列。</param>
    /// <param name="tones">按原文顺序排列的肤色等级。</param>
    /// <returns>移除肤色修饰符和表情选择符后的家族键。</returns>
    internal static string GetFamilyKey(string value, out EmojiSkinTone[] tones)
    {
        List<EmojiSkinTone> toneValues = null;
        StringBuilder builder = null;
        var index = 0;
        while (index < value.Length)
        {
            var codePoint = GetCodePoint(value, index, out var codeUnitLength);
            if (TryGetTone(codePoint, out var tone))
            {
                if (toneValues == null)
                    toneValues = new List<EmojiSkinTone>();
                toneValues.Add(tone);
                if (builder == null)
                    builder = new StringBuilder(value.Length).Append(value, 0, index);
            }
            else if (codePoint == 0xFE0F)
            {
                if (builder == null)
                    builder = new StringBuilder(value.Length).Append(value, 0, index);
            }
            else if (builder != null)
            {
                builder.Append(value, index, codeUnitLength);
            }
            index += codeUnitLength;
        }

        tones = toneValues == null ? new EmojiSkinTone[0] : toneValues.ToArray();
        return builder == null ? value : builder.ToString();
    }

    /// <summary>
    /// 将 Unicode 代码点转换为肤色等级。
    /// </summary>
    /// <param name="codePoint">Unicode 代码点。</param>
    /// <param name="tone">对应的肤色等级。</param>
    /// <returns>代码点是肤色修饰符时为 true，否则为 false。</returns>
    internal static bool TryGetTone(int codePoint, out EmojiSkinTone tone)
    {
        if (codePoint >= 0x1F3FB && codePoint <= 0x1F3FF)
        {
            tone = (EmojiSkinTone)(codePoint - 0x1F3FB + 1);
            return true;
        }

        tone = default(EmojiSkinTone);
        return false;
    }

    /// <summary>
    /// 获取指定位置代码点占用的 UTF-16 单位数。
    /// </summary>
    /// <param name="value">待读取的字符串。</param>
    /// <param name="index">代码点起始位置。</param>
    /// <param name="length">代码点占用的 UTF-16 单位数。</param>
    /// <returns>代码点值；孤立代理项按单个代码单元返回。</returns>
    private static int GetCodePoint(string value, int index, out int length)
    {
        if (index + 1 < value.Length && char.IsHighSurrogate(value[index]) && char.IsLowSurrogate(value[index + 1]))
        {
            length = 2;
            return char.ConvertToUtf32(value, index);
        }

        length = 1;
        return value[index];
    }
}
