using System;
using System.Collections.Generic;

namespace Bing.Extra.Emoji;

/// <summary>
/// 不可变的 Emoji 元数据。
/// </summary>
/// <remarks>Unicode 使用规范表情形式，名称与分组来自 Unicode 清单，标签来自固定版本的 gemoji 快照，本地化数据来自内置简繁中文快照或调用方离线导入。</remarks>
public sealed class EmojiInfo
{
    /// <summary>
    /// 获取规范的 Unicode 表情序列。
    /// </summary>
    public string Unicode { get; }

    /// <summary>
    /// 获取受支持的规范及兼容序列列表。
    /// </summary>
    /// <remarks>第一项为规范序列，后续项为官方清单中的兼容形式。</remarks>
    public IReadOnlyList<string> Variants { get; }

    /// <summary>
    /// 获取官方英文名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 获取官方分组名称。
    /// </summary>
    public string Group { get; }

    /// <summary>
    /// 获取官方子分组名称。
    /// </summary>
    public string Subgroup { get; }

    /// <summary>
    /// 获取表情引入时的 Emoji 版本。
    /// </summary>
    /// <remarks>此版本不是组成字符首次编码时的 Unicode 版本。</remarks>
    public string Version { get; }

    /// <summary>
    /// 获取只读别名列表。
    /// </summary>
    /// <remarks>别名不带冒号，第一项为首选别名；没有映射时列表为空。</remarks>
    public IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// 获取只读英文标签列表。
    /// </summary>
    /// <remarks>标签来自固定版本的 gemoji 数据；没有标签映射时列表为空。</remarks>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// 获取只读本地化元数据列表。
    /// </summary>
    /// <remarks>包含所属目录的内置及离线导入数据；没有本地化映射时列表为空。</remarks>
    public IReadOnlyList<EmojiLocalization> Localizations { get; }

    /// <summary>
    /// 初始化一个 <see cref="EmojiInfo" /> 类型的实例。
    /// </summary>
    /// <param name="unicode">规范的完整 Unicode 表情序列。</param>
    /// <param name="variants">规范及兼容序列数组，第一项应为规范序列。</param>
    /// <param name="name">官方英文名称。</param>
    /// <param name="group">官方分组名称。</param>
    /// <param name="subgroup">官方子分组名称。</param>
    /// <param name="version">表情引入时的 Emoji 版本。</param>
    /// <param name="aliases">不带冒号的别名数组，第一项为首选别名。</param>
    /// <param name="tags">英文标签数组。</param>
    /// <param name="localizations">本地化元数据数组。</param>
    /// <remarks>复制数组并提供只读视图，避免外部修改元数据。</remarks>
    internal EmojiInfo(string unicode, string[] variants, string name, string group, string subgroup, string version, string[] aliases, string[] tags, EmojiLocalization[] localizations)
    {
        Unicode = unicode;
        Variants = Array.AsReadOnly((string[])variants.Clone());
        Name = name;
        Group = group;
        Subgroup = subgroup;
        Version = version;
        Aliases = Array.AsReadOnly((string[])aliases.Clone());
        Tags = Array.AsReadOnly((string[])tags.Clone());
        Localizations = Array.AsReadOnly((EmojiLocalization[])localizations.Clone());
    }
}
