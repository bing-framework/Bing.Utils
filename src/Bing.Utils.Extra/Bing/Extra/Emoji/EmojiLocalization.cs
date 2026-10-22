using System;
using System.Collections.Generic;

namespace Bing.Extra.Emoji;

/// <summary>
/// 一个 Emoji 的本地化元数据。
/// </summary>
/// <remarks>数据来自内置简繁中文 CLDR 快照或调用方的离线 JSON；可通过所属目录的 GetLocales 查询语言集合。</remarks>
public sealed class EmojiLocalization
{
    /// <summary>
    /// 获取本地化语言标识。
    /// </summary>
    public string Locale { get; }

    /// <summary>
    /// 获取本地化名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 获取本地化关键词列表。
    /// </summary>
    public IReadOnlyList<string> Keywords { get; }

    /// <summary>
    /// 初始化一个 <see cref="EmojiLocalization" /> 类型的实例。
    /// </summary>
    /// <param name="locale">语言标识。</param>
    /// <param name="name">本地化名称。</param>
    /// <param name="keywords">本地化关键词数组。</param>
    /// <remarks>复制关键词数组并提供只读视图，避免外部修改内置元数据。</remarks>
    internal EmojiLocalization(string locale, string name, string[] keywords)
    {
        Locale = locale;
        Name = name;
        Keywords = Array.AsReadOnly((string[])keywords.Clone());
    }
}
