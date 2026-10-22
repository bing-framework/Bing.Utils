using System;
using System.Collections.Generic;
using System.Text;

namespace Bing.Extra.Emoji;

/// <summary>
/// Emoji 序列前缀树及元数据索引。
/// </summary>
/// <remarks>索引在构造阶段完成构建，实例发布后只供读取。</remarks>
public sealed partial class EmojiCatalog
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
    /// 将肤色家族键映射到基础表情和规范变体列表。
    /// </summary>
    private readonly Dictionary<string, SkinToneFamily> _skinToneFamilies = new Dictionary<string, SkinToneFamily>(StringComparer.Ordinal);
    /// <summary>
    /// 将单一肤色等级映射到按目录顺序排列的规范表情列表。
    /// </summary>
    private readonly Dictionary<EmojiSkinTone, IReadOnlyList<EmojiInfo>> _singleSkinToneItems = new Dictionary<EmojiSkinTone, IReadOnlyList<EmojiInfo>>();
    /// <summary>
    /// 在构造阶段按官方清单顺序收集的规范表情列表。
    /// </summary>
    private readonly List<EmojiInfo> _items = new List<EmojiInfo>();

    /// <summary>
    /// 获取按官方顺序排列的只读规范表情列表。
    /// </summary>
    public IReadOnlyList<EmojiInfo> Items { get; }

    /// <summary>
    /// 初始化一个 <see cref="EmojiCatalog" /> 类型的实例。
    /// </summary>
    /// <remarks>在实例发布前加载内置数据并完成全部索引构建。</remarks>
    internal EmojiCatalog() : this(true)
    {
    }

    /// <summary>
    /// 初始化一个 <see cref="EmojiCatalog" /> 类型的实例。
    /// </summary>
    /// <param name="loadGeneratedData">是否加载内置元数据。</param>
    private EmojiCatalog(bool loadGeneratedData)
    {
        if (loadGeneratedData)
            LoadGeneratedData();
        BuildSkinToneIndex();
        Items = _items.AsReadOnly();
    }

    /// <summary>
    /// 从已有 Emoji 元数据创建不可变的自定义目录。
    /// </summary>
    /// <param name="emojis">目录中的元数据快照。</param>
    /// <returns>只包含指定元数据的目录实例。</returns>
    /// <exception cref="ArgumentNullException">emojis 为 null。</exception>
    /// <exception cref="ArgumentException">序列包含 null、重复序列、重复别名或无效变体。</exception>
    /// <remarks>目录只接受已有 <see cref="EmojiInfo" />，不会注册新的 Unicode 序列，也不会修改全局 <see cref="EmojiUtil" /> 目录。</remarks>
    public static EmojiCatalog Create(IEnumerable<EmojiInfo> emojis)
    {
        if (emojis == null)
            throw new ArgumentNullException(nameof(emojis));

        var catalog = new EmojiCatalog(false);
        foreach (var emoji in emojis)
        {
            if (emoji == null)
                throw new ArgumentException("目录不能包含 null 元数据。", nameof(emojis));
            catalog.Add(emoji);
        }
        catalog.BuildSkinToneIndex();
        return catalog;
    }

    /// <summary>
    /// 判断整个字符串是否恰好为目录中的一个表情。
    /// </summary>
    /// <param name="text">待判断的完整字符串。</param>
    /// <returns>输入恰好匹配一个目录表情时为 true，否则为 false。</returns>
    public bool IsEmoji(string text) => !string.IsNullOrEmpty(text) && TryGetUnicode(text, out _);

    /// <summary>
    /// 判断文本中是否包含目录表情。
    /// </summary>
    /// <param name="text">待扫描文本。</param>
    /// <returns>包含目录表情时为 true，否则为 false。</returns>
    public bool ContainsEmoji(string text)
    {
        var index = 0;
        return TryFindNext(text, ref index, out _, out _);
    }

    /// <summary>
    /// 统计文本中的目录表情数量。
    /// </summary>
    /// <param name="text">待扫描文本。</param>
    /// <returns>完整表情数量；null 或空字符串为零。</returns>
    public int Count(string text)
    {
        var count = 0;
        var index = 0;
        while (TryFindNext(text, ref index, out var length, out _))
        {
            count++;
            index += length;
        }
        return count;
    }

    /// <summary>
    /// 查找文本中的目录表情匹配。
    /// </summary>
    /// <param name="text">待扫描文本。</param>
    /// <returns>按出现顺序排列的只读匹配列表。</returns>
    /// <remarks>使用从左到右的非重叠最长匹配，位置和长度使用 UTF-16 代码单元。</remarks>
    public IReadOnlyList<EmojiMatch> FindAll(string text)
    {
        var result = new List<EmojiMatch>();
        var index = 0;
        while (TryFindNext(text, ref index, out var length, out var emoji))
        {
            result.Add(new EmojiMatch(text.Substring(index, length), index, length, emoji));
            index += length;
        }
        return result.AsReadOnly();
    }

    /// <summary>
    /// 移除文本中的目录表情。
    /// </summary>
    /// <param name="text">待清理文本。</param>
    /// <returns>清理后的文本；null 返回 null。</returns>
    public string RemoveAllEmojis(string text) => Replace(text, string.Empty);

    /// <summary>
    /// 将文本中的目录表情替换为指定字符串。
    /// </summary>
    /// <param name="text">待替换文本。</param>
    /// <param name="replacement">替换字符串。</param>
    /// <returns>替换后的文本；text 为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">replacement 为 null。</exception>
    public string Replace(string text, string replacement)
    {
        if (replacement == null)
            throw new ArgumentNullException(nameof(replacement));
        return Replace(text, match => replacement);
    }

    /// <summary>
    /// 根据回调替换文本中的目录表情。
    /// </summary>
    /// <param name="text">待替换文本。</param>
    /// <param name="replacement">每个匹配调用一次的回调，返回 null 表示删除。</param>
    /// <returns>替换后的文本；text 为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">replacement 为 null。</exception>
    public string Replace(string text, Func<EmojiMatch, string> replacement)
    {
        if (replacement == null)
            throw new ArgumentNullException(nameof(replacement));
        if (text == null)
            return null;

        var matches = FindAll(text);
        if (matches.Count == 0)
            return text;

        var builder = new StringBuilder(text.Length);
        var copied = 0;
        foreach (var match in matches)
        {
            builder.Append(text, copied, match.Index - copied);
            builder.Append(replacement(match));
            copied = match.Index + match.Length;
        }
        return builder.Append(text, copied, text.Length - copied).ToString();
    }

    /// <summary>
    /// 按英文或本地化元数据搜索目录表情。
    /// </summary>
    /// <param name="query">不区分大小写的子串查询文本。</param>
    /// <returns>按目录顺序排列的只读结果列表。</returns>
    public IReadOnlyList<EmojiInfo> Search(string query) => Search(query, null);

    /// <summary>
    /// 按指定语言的本地化元数据搜索目录表情。
    /// </summary>
    /// <param name="query">不区分大小写的子串查询文本。</param>
    /// <param name="locale">语言标识。</param>
    /// <returns>按目录顺序排列的只读结果列表。</returns>
    public IReadOnlyList<EmojiInfo> Search(string query, string locale)
    {
        if (string.IsNullOrEmpty(query) || locale != null && string.IsNullOrEmpty(locale))
            return new EmojiInfo[0];

        var result = new List<EmojiInfo>();
        foreach (var item in Items)
        {
            if (locale == null
                ? ContainsSearchValue(item, query)
                : ContainsLocalization(item, query, locale))
                result.Add(item);
        }
        return result.AsReadOnly();
    }

    /// <summary>
    /// 按分组筛选目录表情。
    /// </summary>
    /// <param name="group">不区分大小写的完整分组名称。</param>
    /// <returns>按目录顺序排列的只读结果列表。</returns>
    public IReadOnlyList<EmojiInfo> GetByGroup(string group) => Filter(item =>
        !string.IsNullOrEmpty(group) && string.Equals(item.Group, group, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 按子分组筛选目录表情。
    /// </summary>
    /// <param name="subgroup">不区分大小写的完整子分组名称。</param>
    /// <returns>按目录顺序排列的只读结果列表。</returns>
    public IReadOnlyList<EmojiInfo> GetBySubgroup(string subgroup) => Filter(item =>
        !string.IsNullOrEmpty(subgroup) && string.Equals(item.Subgroup, subgroup, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 按 Emoji 版本筛选目录表情。
    /// </summary>
    /// <param name="version">不区分大小写的完整版本号。</param>
    /// <returns>按目录顺序排列的只读结果列表。</returns>
    public IReadOnlyList<EmojiInfo> GetByVersion(string version) => Filter(item =>
        !string.IsNullOrEmpty(version) && string.Equals(item.Version, version, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 按英文标签筛选目录表情。
    /// </summary>
    /// <param name="tag">不区分大小写的完整标签。</param>
    /// <returns>按目录顺序排列的只读结果列表。</returns>
    public IReadOnlyList<EmojiInfo> GetByTag(string tag) => Filter(item =>
        !string.IsNullOrEmpty(tag) && HasTag(item, tag));

    /// <summary>
    /// 按别名查询目录表情。
    /// </summary>
    /// <param name="alias">不带冒号或由一对冒号包围的别名。</param>
    /// <param name="emoji">找到的元数据；未找到时为 null。</param>
    /// <returns>找到映射时为 true，否则为 false。</returns>
    public bool TryGetByAlias(string alias, out EmojiInfo emoji)
    {
        emoji = null;
        if (string.IsNullOrEmpty(alias))
            return false;
        if (alias.Length >= 2 && alias[0] == ':' && alias[alias.Length - 1] == ':')
            alias = alias.Substring(1, alias.Length - 2);
        return TryGetAlias(alias, out emoji);
    }

    /// <summary>
    /// 按完整 Unicode 序列查询目录表情。
    /// </summary>
    /// <param name="unicode">完整表情序列。</param>
    /// <param name="emoji">找到的元数据；未找到时为 null。</param>
    /// <returns>找到序列时为 true，否则为 false。</returns>
    public bool TryGetByUnicode(string unicode, out EmojiInfo emoji)
    {
        emoji = null;
        return !string.IsNullOrEmpty(unicode) && TryGetUnicode(unicode, out emoji);
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
        var info = new EmojiInfo(unicode, variants, name, group, subgroup, version, aliases, EmojiTags.Get(unicode), EmojiLocalizations.Get(unicode));
        Add(info);
    }

    /// <summary>
    /// 将已有元数据加入目录索引。
    /// </summary>
    /// <param name="info">待加入的不可变元数据。</param>
    private void Add(EmojiInfo info)
    {
        if (info == null || string.IsNullOrEmpty(info.Unicode) || info.Variants.Count == 0)
            throw new ArgumentException("目录元数据或表情变体无效。", nameof(info));

        var variants = new HashSet<string>(StringComparer.Ordinal);
        var hasCanonical = false;
        foreach (var value in info.Variants)
        {
            if (string.IsNullOrEmpty(value) || !variants.Add(value) || _unicode.ContainsKey(value))
                throw new ArgumentException("目录包含重复或无效的表情变体。", nameof(info));
            if (value == info.Unicode)
                hasCanonical = true;
        }
        if (!hasCanonical)
            throw new ArgumentException("目录元数据的变体必须包含规范序列。", nameof(info));

        var aliases = new HashSet<string>(StringComparer.Ordinal);
        foreach (var alias in info.Aliases)
        {
            if (string.IsNullOrEmpty(alias) || !aliases.Add(alias) || _aliases.ContainsKey(alias))
                throw new ArgumentException("目录包含重复或无效的别名。", nameof(info));
        }

        _items.Add(info);
        foreach (var alias in info.Aliases)
            _aliases.Add(alias, info);
        foreach (var value in info.Variants)
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
    /// 定位下一处目录表情匹配。
    /// </summary>
    /// <param name="text">待扫描文本。</param>
    /// <param name="index">扫描起点；命中时更新为匹配起点。</param>
    /// <param name="length">命中序列的 UTF-16 长度。</param>
    /// <param name="emoji">命中的表情元数据。</param>
    /// <returns>找到匹配返回 true，否则返回 false。</returns>
    private bool TryFindNext(string text, ref int index, out int length, out EmojiInfo emoji)
    {
        length = 0;
        emoji = null;
        if (string.IsNullOrEmpty(text))
            return false;
        while (index < text.Length)
        {
            length = Match(text, index, out emoji);
            if (length > 0)
                return true;
            index += length < 0 ? -length : 1;
        }
        return false;
    }

    /// <summary>
    /// 按条件筛选目录元数据。
    /// </summary>
    /// <param name="predicate">筛选条件。</param>
    /// <returns>满足条件的只读元数据列表。</returns>
    private IReadOnlyList<EmojiInfo> Filter(Func<EmojiInfo, bool> predicate)
    {
        var result = new List<EmojiInfo>();
        foreach (var item in Items)
        {
            if (predicate(item))
                result.Add(item);
        }
        return result.AsReadOnly();
    }

    /// <summary>
    /// 判断目录元数据是否包含搜索文本。
    /// </summary>
    /// <param name="item">待检查元数据。</param>
    /// <param name="query">搜索文本。</param>
    /// <returns>包含搜索文本返回 true，否则返回 false。</returns>
    private static bool ContainsSearchValue(EmojiInfo item, string query)
    {
        if (item.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            item.Group.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            item.Subgroup.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        foreach (var alias in item.Aliases)
        {
            if (alias.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        foreach (var tag in item.Tags)
        {
            if (tag.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return ContainsLocalization(item, query, null);
    }

    /// <summary>
    /// 判断本地化名称或关键词是否包含搜索文本。
    /// </summary>
    /// <param name="item">待检查元数据。</param>
    /// <param name="query">搜索文本。</param>
    /// <param name="locale">指定语言；null 表示检查所有语言。</param>
    /// <returns>包含搜索文本返回 true，否则返回 false。</returns>
    private static bool ContainsLocalization(EmojiInfo item, string query, string locale)
    {
        foreach (var localization in item.Localizations)
        {
            if (locale != null && !string.Equals(localization.Locale, locale, StringComparison.OrdinalIgnoreCase))
                continue;
            if (localization.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            foreach (var keyword in localization.Keywords)
            {
                if (keyword.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 判断元数据是否包含指定标签。
    /// </summary>
    /// <param name="item">待检查元数据。</param>
    /// <param name="tag">目标标签。</param>
    /// <returns>包含标签返回 true，否则返回 false。</returns>
    private static bool HasTag(EmojiInfo item, string tag)
    {
        foreach (var value in item.Tags)
        {
            if (string.Equals(value, tag, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
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
    /// 查询完整 Emoji 的肤色变体信息。
    /// </summary>
    /// <param name="unicode">完整的 Unicode 表情序列。</param>
    /// <param name="info">找到的肤色变体信息；查询失败时为 null。</param>
    /// <returns>序列属于目录内的肤色家族时为 true，否则为 false。</returns>
    public bool TryGetSkinTone(string unicode, out EmojiSkinToneInfo info)
    {
        info = null;
        if (string.IsNullOrEmpty(unicode) || !_unicode.TryGetValue(unicode, out var emoji))
            return false;

        var key = EmojiSkinToneUtilities.GetFamilyKey(emoji.Unicode, out var tones);
        if (!_skinToneFamilies.TryGetValue(key, out var family))
            return false;

        info = new EmojiSkinToneInfo(emoji, family.BaseEmoji, family.Variants, tones);
        return true;
    }

    /// <summary>
    /// 获取完整 Emoji 所属肤色家族的规范序列。
    /// </summary>
    /// <param name="unicode">完整的 Unicode 表情序列。</param>
    /// <returns>同一家族的只读规范序列列表；未找到肤色家族时为空。</returns>
    public IReadOnlyList<EmojiInfo> GetSkinToneVariants(string unicode)
    {
        if (string.IsNullOrEmpty(unicode) || !_unicode.TryGetValue(unicode, out var emoji))
            return new EmojiInfo[0];

        var key = EmojiSkinToneUtilities.GetFamilyKey(emoji.Unicode, out _);
        return _skinToneFamilies.TryGetValue(key, out var family)
            ? family.Variants
            : new EmojiInfo[0];
    }

    /// <summary>
    /// 获取只包含一个指定肤色等级的规范表情。
    /// </summary>
    /// <param name="tone">肤色等级。</param>
    /// <returns>按目录顺序排列的只读表情列表。</returns>
    /// <exception cref="ArgumentOutOfRangeException">肤色等级无效。</exception>
    /// <remarks>仅返回恰好包含一个肤色修饰符的序列。</remarks>
    public IReadOnlyList<EmojiInfo> GetBySkinTone(EmojiSkinTone tone)
    {
        ValidateSkinTone(tone);
        return _singleSkinToneItems.TryGetValue(tone, out var items) ? items : new EmojiInfo[0];
    }

    /// <summary>
    /// 查询同一家族中指定的单肤色规范序列。
    /// </summary>
    /// <param name="unicode">完整的规范或兼容 Emoji 序列。</param>
    /// <param name="tone">目标肤色等级。</param>
    /// <param name="variant">找到的单肤色规范序列；未找到时为 null。</param>
    /// <returns>输入可安全映射到目标序列时为 true，否则为 false。</returns>
    internal bool TryGetSingleSkinToneVariant(string unicode, EmojiSkinTone tone, out EmojiInfo variant)
    {
        variant = null;
        if (!_unicode.TryGetValue(unicode, out var emoji))
            return false;

        var key = EmojiSkinToneUtilities.GetFamilyKey(emoji.Unicode, out var sourceTones);
        if (sourceTones.Length > 1 || !_skinToneFamilies.TryGetValue(key, out var family) || family.BaseEmoji == null)
            return false;

        foreach (var item in family.Variants)
        {
            EmojiSkinTone[] itemTones;
            EmojiSkinToneUtilities.GetFamilyKey(item.Unicode, out itemTones);
            if (itemTones.Length == 1 && itemTones[0] == tone)
            {
                variant = item;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 查询同一家族中指定多肤色顺序的规范序列。
    /// </summary>
    /// <param name="unicode">完整的规范或兼容 Emoji 序列。</param>
    /// <param name="tones">按出现顺序排列的目标肤色等级。</param>
    /// <param name="variant">找到的规范序列；未找到时为 null。</param>
    /// <returns>输入可安全映射到目标序列时为 true，否则为 false。</returns>
    internal bool TryGetSkinToneVariant(string unicode, IReadOnlyList<EmojiSkinTone> tones, out EmojiInfo variant)
    {
        variant = null;
        if (!_unicode.TryGetValue(unicode, out var emoji))
            return false;

        var key = EmojiSkinToneUtilities.GetFamilyKey(emoji.Unicode, out var sourceTones);
        if (!_skinToneFamilies.TryGetValue(key, out var family) ||
            sourceTones.Length > 0 && sourceTones.Length != tones.Count)
            return false;

        foreach (var item in family.Variants)
        {
            EmojiSkinTone[] itemTones;
            EmojiSkinToneUtilities.GetFamilyKey(item.Unicode, out itemTones);
            if (itemTones.Length != tones.Count)
                continue;

            var matched = true;
            for (var index = 0; index < itemTones.Length; index++)
            {
                if (itemTones[index] != tones[index])
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                variant = item;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 构建只读肤色家族索引。
    /// </summary>
    private void BuildSkinToneIndex()
    {
        var builders = new Dictionary<string, SkinToneFamilyBuilder>(StringComparer.Ordinal);
        var singleToneBuilders = new Dictionary<EmojiSkinTone, List<EmojiInfo>>();
        foreach (var item in _items)
        {
            var key = EmojiSkinToneUtilities.GetFamilyKey(item.Unicode, out var tones);
            if (!builders.TryGetValue(key, out var builder))
            {
                builder = new SkinToneFamilyBuilder();
                builders.Add(key, builder);
            }

            builder.Items.Add(item);
            if (tones.Length == 0)
                builder.BaseEmoji = item;
            else
                builder.HasVariants = true;

            if (tones.Length == 1)
            {
                if (!singleToneBuilders.TryGetValue(tones[0], out var toneItems))
                {
                    toneItems = new List<EmojiInfo>();
                    singleToneBuilders.Add(tones[0], toneItems);
                }
                toneItems.Add(item);
            }
        }

        foreach (var pair in builders)
        {
            var builder = pair.Value;
            if (builder.HasVariants)
                _skinToneFamilies.Add(pair.Key, new SkinToneFamily(builder.BaseEmoji, builder.Items.ToArray()));
        }

        foreach (var pair in singleToneBuilders)
            _singleSkinToneItems.Add(pair.Key, Array.AsReadOnly(pair.Value.ToArray()));
    }

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

    /// <summary>
    /// 构建肤色家族索引时收集的临时数据。
    /// </summary>
    private sealed class SkinToneFamilyBuilder
    {
        /// <summary>
        /// 家族中的基础 Emoji 元数据。
        /// </summary>
        internal EmojiInfo BaseEmoji;
        /// <summary>
        /// 按官方目录顺序收集的家族成员。
        /// </summary>
        internal readonly List<EmojiInfo> Items = new List<EmojiInfo>();
        /// <summary>
        /// 家族是否包含至少一个肤色变体。
        /// </summary>
        internal bool HasVariants;
    }

    /// <summary>
    /// 已完成构建的只读肤色家族。
    /// </summary>
    private sealed class SkinToneFamily
    {
        /// <summary>
        /// 初始化一个 <see cref="SkinToneFamily" /> 类型的实例。
        /// </summary>
        /// <param name="baseEmoji">家族中的基础 Emoji 元数据。</param>
        /// <param name="variants">按官方目录顺序排列的家族成员。</param>
        internal SkinToneFamily(EmojiInfo baseEmoji, EmojiInfo[] variants)
        {
            BaseEmoji = baseEmoji;
            Variants = Array.AsReadOnly(variants);
        }

        /// <summary>
        /// 家族中的无肤色基础 Emoji 元数据。
        /// </summary>
        internal EmojiInfo BaseEmoji { get; }
        /// <summary>
        /// 家族成员的只读列表。
        /// </summary>
        internal IReadOnlyList<EmojiInfo> Variants { get; }
    }
}
