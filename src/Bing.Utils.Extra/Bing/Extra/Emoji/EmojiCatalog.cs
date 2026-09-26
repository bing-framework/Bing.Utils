using System;
using System.Collections.Generic;

namespace Bing.Extra.Emoji;

/// <summary>
/// Emoji 序列前缀树及元数据索引。
/// </summary>
/// <remarks>索引在构造阶段完成构建，实例发布后只供读取。</remarks>
internal sealed partial class EmojiCatalog
{
    /// <summary>
    /// 按 UTF-16 代码单元定位表情序列的根节点。
    /// </summary>
    private readonly Node _root = new Node();
    /// <summary>
    /// 使用序数比较区分别名大小写的元数据索引。
    /// </summary>
    private readonly Dictionary<string, EmojiInfo> _aliases = new Dictionary<string, EmojiInfo>(StringComparer.Ordinal);
    /// <summary>
    /// 将规范序列及兼容形式映射到规范元数据的索引。
    /// </summary>
    private readonly Dictionary<string, EmojiInfo> _unicode = new Dictionary<string, EmojiInfo>(StringComparer.Ordinal);
    /// <summary>
    /// 在构造阶段按官方清单顺序收集的规范表情列表。
    /// </summary>
    private readonly List<EmojiInfo> _items = new List<EmojiInfo>();

    /// <summary>
    /// 获取按官方顺序排列的只读规范表情列表。
    /// </summary>
    internal IReadOnlyList<EmojiInfo> Items { get; }

    /// <summary>
    /// 初始化 <see cref="EmojiCatalog" /> 类的新实例。
    /// </summary>
    /// <remarks>在实例发布前加载内置数据并完成全部索引构建。</remarks>
    internal EmojiCatalog()
    {
        LoadGeneratedData();
        Items = _items.AsReadOnly();
    }

    /// <summary>
    /// 添加规范表情及其兼容序列。
    /// </summary>
    /// <param name="unicode">规范的完整 Unicode 表情序列。</param>
    /// <param name="name">官方英文名称。</param>
    /// <param name="group">官方分组名称。</param>
    /// <param name="subgroup">官方子分组名称。</param>
    /// <param name="version">表情引入时的 Emoji 版本。</param>
    /// <param name="aliases">不带冒号的别名数组，第一项为首选别名。</param>
    /// <param name="variants">指向同一规范表情的全部受支持序列，包含规范形式。</param>
    /// <remarks>仅在构造期间调用，同时更新前缀树、查询索引和元数据列表。</remarks>
    private void Add(string unicode, string name, string group, string subgroup, string version, string[] aliases, string[] variants)
    {
        var info = new EmojiInfo(unicode, name, group, subgroup, version, aliases);
        _items.Add(info);
        foreach (var alias in aliases)
            _aliases.Add(alias, info);
        foreach (var value in variants)
        {
            _unicode.Add(value, info);
            var node = _root;
            foreach (var character in value)
            {
                if (!node.Children.TryGetValue(character, out var child))
                {
                    child = new Node();
                    node.Children.Add(character, child);
                }
                node = child;
            }
            node.Emoji = info;
        }
    }

    /// <summary>
    /// 精确查询不带冒号的别名。
    /// </summary>
    /// <param name="alias">不带冒号且区分大小写的别名。</param>
    /// <param name="emoji">找到的规范元数据；未找到时为 null。</param>
    /// <returns>找到别名时为 true，否则为 false。</returns>
    internal bool TryGetAlias(string alias, out EmojiInfo emoji) => _aliases.TryGetValue(alias, out emoji);

    /// <summary>
    /// 精确查询规范或兼容形式的完整序列。
    /// </summary>
    /// <param name="unicode">完整的 Unicode 表情序列。</param>
    /// <param name="emoji">找到的规范元数据；未找到时为 null。</param>
    /// <returns>找到序列时为 true，否则为 false。</returns>
    internal bool TryGetUnicode(string unicode, out EmojiInfo emoji) => _unicode.TryGetValue(unicode, out emoji);

    /// <summary>
    /// 从指定位置寻找最长表情匹配。
    /// </summary>
    /// <param name="text">待匹配文本。</param>
    /// <param name="index">匹配起点的 UTF-16 索引。</param>
    /// <param name="emoji">最长匹配对应的元数据；未匹配或遇到文本选择符时为 null。</param>
    /// <returns>匹配的 UTF-16 长度；未匹配时为零，遇到文本选择符时为应跳过的代码单元数量的负值。</returns>
    /// <remarks>遇到 U+FE0E 时取消当前候选，包括已经找到的较短表情前缀。</remarks>
    internal int Match(string text, int index, out EmojiInfo emoji)
    {
        emoji = null;
        var length = 0;
        var node = _root;
        for (var position = index; position < text.Length; position++)
        {
            // 文本选择符使当前候选失效，包括此前找到的较短表情前缀。
            if (text[position] == '\uFE0E')
            {
                emoji = null;
                return -(position - index + 1);
            }
            if (!node.Children.TryGetValue(text[position], out node))
                break;
            if (node.Emoji != null)
            {
                emoji = node.Emoji;
                length = position - index + 1;
            }
        }
        return length;
    }

    /// <summary>
    /// Emoji 序列前缀树节点。
    /// </summary>
    /// <remarks>节点仅在索引构造期间修改，不向调用方公开。</remarks>
    private sealed class Node
    {
        /// <summary>
        /// 按下一 UTF-16 代码单元索引的子节点集合。
        /// </summary>
        internal readonly Dictionary<char, Node> Children = new Dictionary<char, Node>();
        /// <summary>
        /// 当前序列终点对应的元数据；非完整表情终点时为 null。
        /// </summary>
        internal EmojiInfo Emoji;
    }
}
