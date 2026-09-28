using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Bing.Extra.Emoji;

/// <summary>
/// Emoji 识别、转换与查询工具。
/// </summary>
/// <remarks>
/// 使用 Unicode Emoji 18.0、gemoji v4.1.0 与 CLDR 48.2 内置数据。
/// 按完整序列最长匹配，接受官方清单中的省略表情选择符形式；不匹配显式文本样式。
/// 未收录的组合可能匹配其中的已知子序列；支持有限范围的 HTML 数字实体转换。
/// </remarks>
public static class EmojiUtil
{
    /// <summary>
    /// 首次访问时线程安全初始化的共享表情索引。
    /// </summary>
    private static readonly Lazy<EmojiCatalog> Catalog = new Lazy<EmojiCatalog>(() => new EmojiCatalog());

    /// <summary>
    /// 空的只读 Emoji 元数据列表。
    /// </summary>
    private static readonly IReadOnlyList<EmojiInfo> EmptyEmojis =
        new ReadOnlyCollection<EmojiInfo>(new List<EmojiInfo>());

    /// <summary>
    /// 空的只读元数据值列表。
    /// </summary>
    private static readonly IReadOnlyList<string> EmptyMetadataValues =
        new ReadOnlyCollection<string>(new List<string>());

    /// <summary>
    /// 判断整个字符串是否恰好为一个受支持的表情。
    /// </summary>
    /// <param name="text">待判断的完整字符串。</param>
    /// <returns>输入恰好匹配一个表情时为 true，否则为 false；null 或空字符串为 false。</returns>
    public static bool IsEmoji(string text) => TryGetByUnicode(text, out _);

    /// <summary>
    /// 判断文本中是否至少包含一个受支持的表情。
    /// </summary>
    /// <param name="text">待扫描文本。</param>
    /// <returns>存在匹配时为 true，否则为 false；null 或空字符串为 false。</returns>
    public static bool ContainsEmoji(string text)
    {
        var index = 0;
        return TryFindNext(text, ref index, out _, out _);
    }

    /// <summary>
    /// 统计完整表情的出现次数。
    /// </summary>
    /// <param name="text">待扫描文本。</param>
    /// <returns>表情数量；null 或空字符串为零。</returns>
    /// <remarks>每个完整组合表情计为一次，重复出现分别计数。</remarks>
    public static int Count(string text)
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
    /// 提取文本中的完整表情。
    /// </summary>
    /// <param name="text">待扫描文本。</param>
    /// <returns>只读原文片段列表；null 或空字符串返回空列表。</returns>
    /// <remarks>按原文顺序返回，保留重复项及原始选择符形式。</remarks>
    public static IReadOnlyList<string> ExtractEmojis(string text)
    {
        var result = new List<string>();
        var index = 0;
        while (TryFindNext(text, ref index, out var length, out _))
        {
            result.Add(text.Substring(index, length));
            index += length;
        }
        return result.AsReadOnly();
    }

    /// <summary>
    /// 查找文本中的全部表情匹配。
    /// </summary>
    /// <param name="text">待扫描文本。</param>
    /// <returns>按出现顺序排列的只读匹配列表；null 或空字符串返回空列表。</returns>
    /// <remarks>
    /// 从左到右进行非重叠的最长匹配。
    /// 索引及长度使用 UTF-16 代码单元，可直接用于原字符串的 <see cref="string.Substring(int, int)" />。
    /// </remarks>
    public static IReadOnlyList<EmojiMatch> FindAll(string text)
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
    /// 移除文本中的全部受支持表情。
    /// </summary>
    /// <param name="text">待清理文本。</param>
    /// <returns>清理后的文本；null 返回 null。</returns>
    /// <remarks>保留未匹配的字符及空白。</remarks>
    public static string RemoveAllEmojis(string text) => Replace(text, string.Empty);

    /// <summary>
    /// 替换文本中的完整表情。
    /// </summary>
    /// <param name="text">待替换文本。</param>
    /// <param name="replacement">替换字符串，空字符串表示删除。</param>
    /// <returns>替换后的文本；text 为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">replacement 为 null。</exception>
    /// <remarks>每个完整匹配替换一次，不递归扫描替换结果。</remarks>
    public static string Replace(string text, string replacement)
    {
        if (replacement == null)
            throw new ArgumentNullException(nameof(replacement));
        return Replace(text, match => replacement);
    }

    /// <summary>
    /// 替换文本中的完整表情。
    /// </summary>
    /// <param name="text">待替换文本。</param>
    /// <param name="replacement">每个匹配调用一次的回调，返回 null 表示删除。</param>
    /// <returns>替换后的文本；text 为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">replacement 为 null。</exception>
    /// <remarks>
    /// 按出现顺序为每个完整匹配调用一次回调，不递归扫描替换结果。
    /// 回调抛出的异常直接传递给调用方，不返回部分结果。
    /// </remarks>
    public static string Replace(string text, Func<EmojiMatch, string> replacement)
    {
        if (replacement == null)
            throw new ArgumentNullException(nameof(replacement));
        return ReplaceWhere(text, _ => true, replacement);
    }

    /// <summary>
    /// 移除满足条件的完整 Emoji 匹配。
    /// </summary>
    /// <param name="text">待处理文本。</param>
    /// <param name="predicate">判断是否移除当前匹配的条件。</param>
    /// <returns>处理后的文本；text 为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">predicate 为 null。</exception>
    /// <remarks>按原文顺序判断每个完整匹配，不递归扫描剩余文本或替换结果。</remarks>
    public static string RemoveWhere(string text, Func<EmojiMatch, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        return ReplaceWhere(text, predicate, string.Empty);
    }

    /// <summary>
    /// 将满足条件的完整 Emoji 匹配替换为固定文本。
    /// </summary>
    /// <param name="text">待处理文本。</param>
    /// <param name="predicate">判断是否替换当前匹配的条件。</param>
    /// <param name="replacement">替换字符串，空字符串表示删除。</param>
    /// <returns>处理后的文本；text 为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">predicate 或 replacement 为 null。</exception>
    /// <remarks>未满足条件的匹配保留原文，处理不会递归扫描替换结果。</remarks>
    public static string ReplaceWhere(string text, Func<EmojiMatch, bool> predicate, string replacement)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        if (replacement == null)
            throw new ArgumentNullException(nameof(replacement));
        return ReplaceWhere(text, predicate, _ => replacement);
    }

    /// <summary>
    /// 按条件动态替换完整 Emoji 匹配。
    /// </summary>
    /// <param name="text">待处理文本。</param>
    /// <param name="predicate">判断是否替换当前匹配的条件。</param>
    /// <param name="replacement">根据匹配生成替换内容的回调，返回 null 表示删除。</param>
    /// <returns>处理后的文本；text 为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">predicate 或 replacement 为 null。</exception>
    /// <remarks>
    /// 按原文顺序为每个完整匹配调用 predicate；仅对返回 true 的匹配调用 replacement。
    /// 回调异常直接传递，不递归扫描回调生成的文本。
    /// </remarks>
    public static string ReplaceWhere(string text, Func<EmojiMatch, bool> predicate, Func<EmojiMatch, string> replacement)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        if (replacement == null)
            throw new ArgumentNullException(nameof(replacement));
        if (string.IsNullOrEmpty(text))
            return text;
        StringBuilder builder = null;
        var index = 0;
        var copied = 0;
        while (TryFindNext(text, ref index, out var length, out var emoji))
        {
            var match = new EmojiMatch(text.Substring(index, length), index, length, emoji);
            if (predicate(match))
            {
                if (builder == null)
                    builder = new StringBuilder(text.Length);
                builder.Append(text, copied, index - copied);
                builder.Append(replacement(match));
                copied = index + length;
            }
            index += length;
        }
        return builder == null ? text : builder.Append(text, copied, text.Length - copied).ToString();
    }

    /// <summary>
    /// 将文本中的已知 :alias: 替换为规范 Unicode 表情。
    /// </summary>
    /// <param name="text">包含别名的文本。</param>
    /// <returns>转换后的文本；null 返回 null，未知或大小写不符的别名保留原样。</returns>
    /// <remarks>只处理冒号包围的完整别名，不解码 HTML，不递归解析替换结果。</remarks>
    public static string ToUnicode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        StringBuilder builder = null;
        var copied = 0;
        var index = 0;
        while (index < text.Length)
        {
            if (text[index] != ':')
            {
                index++;
                continue;
            }
            var end = index + 1;
            while (end < text.Length && IsAliasCharacter(text[end]))
                end++;
            if (end > index + 1 && end < text.Length && text[end] == ':')
            {
                if (Catalog.Value.TryGetAlias(text.Substring(index + 1, end - index - 1), out var emoji))
                {
                    if (builder == null)
                        builder = new StringBuilder(text.Length);
                    builder.Append(text, copied, index - copied).Append(emoji.Unicode);
                    copied = end + 1;
                }
                index = end + 1;
            }
            else
            {
                index = end;
            }
        }
        return builder == null ? text : builder.Append(text, copied, text.Length - copied).ToString();
    }

    /// <summary>
    /// 将完整表情转换为首选别名。
    /// </summary>
    /// <param name="text">包含 Unicode 表情的文本。</param>
    /// <returns>转换后的文本；null 返回 null。</returns>
    /// <remarks>
    /// 输出使用 <c>:alias:</c> 格式；没有完整映射时保留原文，不拆解肤色或 ZWJ 序列。
    /// 别名反向转换使用规范形式，可能补全表情选择符。
    /// </remarks>
    public static string ToAlias(string text) => Replace(text, match =>
        match.Emoji.Aliases.Count == 0 ? match.Value : ":" + match.Emoji.Aliases[0] + ":");

    /// <summary>
    /// 将文本中的已知 Emoji 转换为目录规范 Unicode 形式。
    /// </summary>
    /// <param name="text">包含 Emoji 的文本。</param>
    /// <returns>规范化后的文本；null 返回 null，未匹配文本保持原样。</returns>
    /// <remarks>仅规范化识别器匹配的完整序列，不处理显式文本样式或未知组合。</remarks>
    public static string Normalize(string text) => Replace(text, match => match.Emoji.Unicode);

    /// <summary>
    /// 将文本中的完整 Emoji 转换为 HTML 十六进制数字实体。
    /// </summary>
    /// <param name="text">包含 Emoji 的文本。</param>
    /// <returns>转换后的文本；null 返回 null，空字符串保持为空。</returns>
    /// <remarks>
    /// 仅转换识别器匹配的完整序列，使用大写十六进制并保留原始变体选择符、连接符及标签代码点。
    /// 未匹配的字符保持原样。
    /// </remarks>
    public static string ToHtmlEntities(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        StringBuilder builder = null;
        var copied = 0;
        var index = 0;
        while (TryFindNext(text, ref index, out var length, out _))
        {
            if (builder == null)
                builder = new StringBuilder(text.Length);
            builder.Append(text, copied, index - copied);
            AppendHtmlEntities(builder, text.Substring(index, length));
            index += length;
            copied = index;
        }

        return builder == null ? text : builder.Append(text, copied, text.Length - copied).ToString();
    }

    /// <summary>
    /// 将已知 Emoji 数字实体转换为规范 Unicode 表情。
    /// </summary>
    /// <param name="text">包含 HTML 数字实体的文本。</param>
    /// <returns>转换后的文本；null 返回 null，空字符串保持为空。</returns>
    /// <remarks>
    /// 接受十进制和十六进制实体，只解码能够组成内置完整 Emoji 序列的实体。
    /// 未知、格式错误、超出 Unicode 范围或落在代理项范围内的实体保持原文；识别到的序列输出规范形式。
    /// </remarks>
    public static string FromHtmlEntities(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        StringBuilder builder = null;
        var copied = 0;
        var index = 0;
        while (index < text.Length)
        {
            if (!TryParseNumericEntity(text, index, out var end, out _))
            {
                index++;
                continue;
            }

            var tokens = new List<NumericEntityToken>();
            var cursor = index;
            while (TryParseNumericEntity(text, cursor, out end, out var unicode))
            {
                tokens.Add(new NumericEntityToken(text.Substring(cursor, end - cursor), unicode));
                cursor = end;
            }

            if (builder == null)
                builder = new StringBuilder(text.Length);
            builder.Append(text, copied, index - copied);
            AppendDecodedEntityRun(builder, tokens);
            copied = cursor;
            index = cursor;
        }

        return builder == null ? text : builder.Append(text, copied, text.Length - copied).ToString();
    }

    /// <summary>
    /// 按英文或本地化名称、分组、子分组、别名或标签搜索 Emoji 元数据。
    /// </summary>
    /// <param name="query">不区分大小写的子串查询文本。</param>
    /// <returns>按目录顺序排列的只读元数据列表；query 为 null 或空字符串时返回空列表。</returns>
    public static IReadOnlyList<EmojiInfo> Search(string query)
    {
        if (string.IsNullOrEmpty(query))
            return EmptyEmojis;

        return FilterMetadata(item =>
            item.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            item.Group.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            item.Subgroup.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            ContainsAlias(item, query) ||
            ContainsTag(item, query) ||
            ContainsLocalization(item, query));
    }

    /// <summary>
    /// 按指定语言的本地化名称或关键词搜索 Emoji 元数据。
    /// </summary>
    /// <param name="query">不区分大小写的子串查询文本。</param>
    /// <param name="locale">语言标识，例如 <c>zh</c>。</param>
    /// <returns>按目录顺序排列的只读元数据列表；任一参数为空或语言未知时返回空列表。</returns>
    /// <remarks>语言标识按不区分大小写的序数比较精确匹配，不自动回退到英文或其他语言。</remarks>
    public static IReadOnlyList<EmojiInfo> Search(string query, string locale)
    {
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(locale))
            return EmptyEmojis;

        return FilterMetadata(item => ContainsLocalization(item, query, locale));
    }

    /// <summary>
    /// 按官方分组筛选 Emoji 元数据。
    /// </summary>
    /// <param name="group">不区分大小写的完整分组名称。</param>
    /// <returns>按目录顺序排列的只读元数据列表；group 为 null 或空字符串时返回空列表。</returns>
    public static IReadOnlyList<EmojiInfo> GetByGroup(string group) =>
        string.IsNullOrEmpty(group)
            ? EmptyEmojis
            : FilterMetadata(item => string.Equals(item.Group, group, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 按官方子分组筛选 Emoji 元数据。
    /// </summary>
    /// <param name="subgroup">不区分大小写的完整子分组名称。</param>
    /// <returns>按目录顺序排列的只读元数据列表；subgroup 为 null 或空字符串时返回空列表。</returns>
    public static IReadOnlyList<EmojiInfo> GetBySubgroup(string subgroup) =>
        string.IsNullOrEmpty(subgroup)
            ? EmptyEmojis
            : FilterMetadata(item => string.Equals(item.Subgroup, subgroup, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 按 Emoji 引入版本筛选元数据。
    /// </summary>
    /// <param name="version">不区分大小写的完整 Emoji 版本号，例如 1.0 或 15.1。</param>
    /// <returns>按目录顺序排列的只读元数据列表；version 为 null 或空字符串时返回空列表。</returns>
    public static IReadOnlyList<EmojiInfo> GetByVersion(string version) =>
        string.IsNullOrEmpty(version)
            ? EmptyEmojis
            : FilterMetadata(item => string.Equals(item.Version, version, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 按英文标签筛选 Emoji 元数据。
    /// </summary>
    /// <param name="tag">不区分大小写的完整标签。</param>
    /// <returns>按目录顺序排列的只读元数据列表；tag 为 null 或空字符串时返回空列表。</returns>
    public static IReadOnlyList<EmojiInfo> GetByTag(string tag) =>
        string.IsNullOrEmpty(tag)
            ? EmptyEmojis
            : FilterMetadata(item => HasTag(item, tag));

    /// <summary>
    /// 获取目录中出现过的官方分组名称。
    /// </summary>
    /// <returns>按首次出现顺序排列的只读分组名称列表。</returns>
    public static IReadOnlyList<string> GetGroups() =>
        GetDistinctMetadataValues(item => item.Group);

    /// <summary>
    /// 获取目录中出现过的官方子分组名称。
    /// </summary>
    /// <returns>按首次出现顺序排列的只读子分组名称列表。</returns>
    public static IReadOnlyList<string> GetSubgroups() =>
        GetDistinctMetadataValues(item => item.Subgroup);

    /// <summary>
    /// 获取目录中出现过的 Emoji 版本号。
    /// </summary>
    /// <returns>按官方目录首次出现顺序排列的只读版本号列表。</returns>
    public static IReadOnlyList<string> GetVersions() =>
        GetDistinctMetadataValues(item => item.Version);

    /// <summary>
    /// 获取目录中出现过的 gemoji 英文标签。
    /// </summary>
    /// <returns>按表情目录及标签首次出现顺序排列的只读标签列表。</returns>
    public static IReadOnlyList<string> GetTags()
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in Catalog.Value.Items)
        {
            foreach (var tag in item.Tags)
            {
                if (seen.Add(tag))
                    result.Add(tag);
            }
        }
        return result.Count == 0 ? EmptyMetadataValues : result.AsReadOnly();
    }

    /// <summary>
    /// 获取目录中出现过的本地化语言标识。
    /// </summary>
    /// <returns>按首次出现顺序排列的只读语言标识列表。</returns>
    /// <remarks>只返回本地化数据中的语言，不包含英文元数据隐含的默认语言。</remarks>
    public static IReadOnlyList<string> GetLocales()
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Catalog.Value.Items)
        {
            foreach (var localization in item.Localizations)
            {
                if (seen.Add(localization.Locale))
                    result.Add(localization.Locale);
            }
        }
        return result.Count == 0 ? EmptyMetadataValues : result.AsReadOnly();
    }

    /// <summary>
    /// 按别名查询表情元数据。
    /// </summary>
    /// <param name="alias">不带冒号或由一对冒号包围的别名，不自动裁剪空白。</param>
    /// <param name="emoji">成功时为元数据，失败时为 null。</param>
    /// <returns>找到映射时为 true；null、空字符串或未知别名为 false。</returns>
    /// <remarks>使用区分大小写的精确匹配。</remarks>
    public static bool TryGetByAlias(string alias, out EmojiInfo emoji)
    {
        emoji = null;
        if (string.IsNullOrEmpty(alias))
            return false;
        if (alias.Length >= 2 && alias[0] == ':' && alias[alias.Length - 1] == ':')
            alias = alias.Substring(1, alias.Length - 2);
        return Catalog.Value.TryGetAlias(alias, out emoji);
    }

    /// <summary>
    /// 按完整 Unicode 序列查询规范表情元数据。
    /// </summary>
    /// <param name="unicode">恰好一个完整表情的 Unicode 字符串。</param>
    /// <param name="emoji">成功时为元数据，失败时为 null。</param>
    /// <returns>找到序列时为 true；null、空字符串或非完整表情为 false。</returns>
    /// <remarks>接受官方清单内省略表情选择符的兼容形式。</remarks>
    public static bool TryGetByUnicode(string unicode, out EmojiInfo emoji)
    {
        emoji = null;
        return !string.IsNullOrEmpty(unicode) && Catalog.Value.TryGetUnicode(unicode, out emoji);
    }

    /// <summary>
    /// 按完整 Unicode 序列和语言查询本地化元数据。
    /// </summary>
    /// <param name="unicode">完整的 Unicode 表情序列。</param>
    /// <param name="locale">语言标识，例如 <c>zh</c>。</param>
    /// <param name="localization">成功时为本地化元数据，失败时为 null。</param>
    /// <returns>找到对应本地化数据时为 true，否则为 false。</returns>
    /// <remarks>接受规范序列及官方清单内的兼容形式；语言按不区分大小写的序数比较精确匹配，不自动回退。</remarks>
    public static bool TryGetLocalization(string unicode, string locale, out EmojiLocalization localization)
    {
        localization = null;
        if (string.IsNullOrEmpty(unicode) || string.IsNullOrEmpty(locale) ||
            !Catalog.Value.TryGetUnicode(unicode, out var emoji))
            return false;

        foreach (var value in emoji.Localizations)
        {
            if (string.Equals(value.Locale, locale, StringComparison.OrdinalIgnoreCase))
            {
                localization = value;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 查询完整 Emoji 的肤色变体信息。
    /// </summary>
    /// <param name="unicode">完整的 Unicode 表情序列。</param>
    /// <param name="info">成功时为肤色变体信息，失败时为 null。</param>
    /// <returns>序列属于受支持的肤色家族时为 true，否则为 false。</returns>
    /// <remarks>支持多个肤色修饰符的握手、人物和 ZWJ 序列；独立肤色组件不属于肤色家族。</remarks>
    public static bool TryGetSkinTone(string unicode, out EmojiSkinToneInfo info)
    {
        info = null;
        return !string.IsNullOrEmpty(unicode) && Catalog.Value.TryGetSkinTone(unicode, out info);
    }

    /// <summary>
    /// 获取完整 Emoji 所属肤色家族的规范序列。
    /// </summary>
    /// <param name="unicode">完整的 Unicode 表情序列。</param>
    /// <returns>包含基础项（如果存在）和全部肤色变体的只读列表；未找到时为空。</returns>
    public static IReadOnlyList<EmojiInfo> GetSkinToneVariants(string unicode)
    {
        if (string.IsNullOrEmpty(unicode))
            return EmptyEmojis;

        var variants = Catalog.Value.GetSkinToneVariants(unicode);
        return variants.Count == 0 ? EmptyEmojis : variants;
    }

    /// <summary>
    /// 按单一肤色等级筛选规范表情。
    /// </summary>
    /// <param name="tone">要筛选的肤色等级。</param>
    /// <returns>按官方目录顺序排列的只读表情列表。</returns>
    /// <exception cref="ArgumentOutOfRangeException">肤色等级不是 <see cref="EmojiSkinTone" /> 定义的五个等级之一。</exception>
    /// <remarks>仅返回恰好包含一个肤色修饰符的序列；包含多个肤色修饰符的组合不会纳入结果。</remarks>
    public static IReadOnlyList<EmojiInfo> GetBySkinTone(EmojiSkinTone tone)
    {
        ValidateSkinTone(tone);
        var result = Catalog.Value.GetBySkinTone(tone);
        return result.Count == 0 ? EmptyEmojis : result;
    }

    /// <summary>
    /// 将可安全映射的单肤色 Emoji 应用为指定肤色等级。
    /// </summary>
    /// <param name="text">待处理文本。</param>
    /// <param name="tone">目标肤色等级。</param>
    /// <returns>处理后的文本；text 为 null 时返回 null。</returns>
    /// <exception cref="ArgumentOutOfRangeException">肤色等级不是 <see cref="EmojiSkinTone" /> 定义的五个等级之一。</exception>
    /// <remarks>仅处理存在无肤色基础项且目标序列包含一个修饰符的家族；多修饰符组合、独立组件和未知文本保持原文。</remarks>
    public static string ApplySkinTone(string text, EmojiSkinTone tone)
    {
        ValidateSkinTone(tone);
        return Replace(text, match =>
            Catalog.Value.TryGetSingleSkinToneVariant(match.Emoji.Unicode, tone, out var variant)
                ? variant.Unicode
                : match.Value);
    }

    /// <summary>
    /// 将可安全映射的 Emoji 应用为指定的多肤色序列。
    /// </summary>
    /// <param name="text">待处理文本。</param>
    /// <param name="tones">目标序列中按出现顺序排列的肤色等级。</param>
    /// <returns>处理后的文本；text 为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">tones 为 null。</exception>
    /// <exception cref="ArgumentException">tones 为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">tones 包含未定义的肤色等级。</exception>
    /// <remarks>只替换官方目录中存在完全相同修饰符序列的匹配；不排序、去重或拼接新序列。已有肤色输入要求目标数量一致。</remarks>
    public static string ApplySkinTones(string text, IReadOnlyList<EmojiSkinTone> tones)
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
            Catalog.Value.TryGetSkinToneVariant(match.Emoji.Unicode, toneCopy, out var variant)
                ? variant.Unicode
                : match.Value);
    }

    /// <summary>
    /// 移除可还原到无肤色基础项的完整 Emoji 中的肤色修饰符。
    /// </summary>
    /// <param name="text">待处理文本。</param>
    /// <returns>处理后的文本；null 返回 null。</returns>
    /// <remarks>没有无肤色基础项的多人物或组合序列保持原文，独立肤色组件也保持原文。</remarks>
    public static string RemoveSkinTones(string text) => Replace(text, match =>
        Catalog.Value.TryGetSkinTone(match.Emoji.Unicode, out var info) &&
        info.Tones.Count > 0 && info.BaseEmoji != null
            ? info.BaseEmoji.Unicode
            : match.Value);

    /// <summary>
    /// 获取全部规范表情元数据。
    /// </summary>
    /// <returns>线程安全共享的只读元数据列表。</returns>
    /// <remarks>按官方清单顺序排列，不重复列出兼容形式。</remarks>
    public static IReadOnlyList<EmojiInfo> GetAll() => Catalog.Value.Items;

    /// <summary>
    /// 从已有 Emoji 元数据创建不可变的自定义目录。
    /// </summary>
    /// <param name="emojis">目录中的元数据快照。</param>
    /// <returns>只包含指定元数据的目录实例。</returns>
    /// <exception cref="ArgumentNullException">emojis 为 null。</exception>
    /// <exception cref="ArgumentException">序列包含 null、重复序列、重复别名或无效变体。</exception>
    /// <remarks>自定义目录不修改全局共享目录，也不会注册新的 Unicode 序列。</remarks>
    public static EmojiCatalog CreateCatalog(IEnumerable<EmojiInfo> emojis) => EmojiCatalog.Create(emojis);

    /// <summary>
    /// 定位下一处表情匹配。
    /// </summary>
    /// <param name="text">待扫描文本，可为 null 或空字符串。</param>
    /// <param name="index">扫描起点的 UTF-16 索引；成功时更新为匹配起点。</param>
    /// <param name="length">成功时为匹配的 UTF-16 长度。</param>
    /// <param name="emoji">成功时为匹配对应的规范元数据；失败时为 null。</param>
    /// <returns>找到匹配时为 true，否则为 false。</returns>
    /// <remarks>遇到显式文本样式时跳过整段候选；成功后由调用方推进索引。</remarks>
    private static bool TryFindNext(string text, ref int index, out int length, out EmojiInfo emoji)
    {
        length = 0;
        emoji = null;
        if (string.IsNullOrEmpty(text))
            return false;
        var catalog = Catalog.Value;
        while (index < text.Length)
        {
            length = catalog.Match(text, index, out emoji);
            if (length > 0)
                return true;
            index += length < 0 ? -length : 1;
        }
        return false;
    }

    /// <summary>
    /// 判断字符是否可用于表情别名。
    /// </summary>
    /// <param name="value">待判断的字符。</param>
    /// <returns>字符为 ASCII 字母、数字、下划线、加号或减号时为 true，否则为 false。</returns>
    private static bool IsAliasCharacter(char value) =>
        value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z' ||
        value >= '0' && value <= '9' || value == '_' || value == '+' || value == '-';

    /// <summary>
    /// 验证肤色等级枚举值。
    /// </summary>
    /// <param name="tone">待验证的肤色等级。</param>
    /// <exception cref="ArgumentOutOfRangeException">肤色等级不在定义范围内。</exception>
    private static void ValidateSkinTone(EmojiSkinTone tone)
    {
        if (tone < EmojiSkinTone.Light || tone > EmojiSkinTone.Dark)
            throw new ArgumentOutOfRangeException(nameof(tone), tone, "肤色等级无效。");
    }

    /// <summary>
    /// 按条件筛选共享 Emoji 元数据。
    /// </summary>
    /// <param name="predicate">元数据筛选条件。</param>
    /// <returns>按目录顺序排列的只读结果列表。</returns>
    private static IReadOnlyList<EmojiInfo> FilterMetadata(Func<EmojiInfo, bool> predicate)
    {
        var result = new List<EmojiInfo>();
        foreach (var item in Catalog.Value.Items)
        {
            if (predicate(item))
                result.Add(item);
        }
        return result.Count == 0 ? EmptyEmojis : result.AsReadOnly();
    }

    /// <summary>
    /// 获取按目录首次出现顺序去重的元数据值。
    /// </summary>
    /// <param name="selector">从元数据读取值的函数。</param>
    /// <returns>只读元数据值列表。</returns>
    private static IReadOnlyList<string> GetDistinctMetadataValues(Func<EmojiInfo, string> selector)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in Catalog.Value.Items)
        {
            var value = selector(item);
            if (!string.IsNullOrEmpty(value) && seen.Add(value))
                result.Add(value);
        }
        return result.Count == 0 ? EmptyMetadataValues : result.AsReadOnly();
    }

    /// <summary>
    /// 判断 Emoji 的别名是否包含查询文本。
    /// </summary>
    /// <param name="emoji">待查询的 Emoji 元数据。</param>
    /// <param name="query">不区分大小写的查询文本。</param>
    /// <returns>任一别名包含查询文本时为 true，否则为 false。</returns>
    private static bool ContainsAlias(EmojiInfo emoji, string query)
    {
        foreach (var alias in emoji.Aliases)
        {
            if (alias.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 判断 Emoji 的标签是否包含查询文本。
    /// </summary>
    /// <param name="emoji">待查询的 Emoji 元数据。</param>
    /// <param name="query">不区分大小写的查询文本。</param>
    /// <returns>任一标签包含查询文本时为 true，否则为 false。</returns>
    private static bool ContainsTag(EmojiInfo emoji, string query)
    {
        foreach (var tag in emoji.Tags)
        {
            if (tag.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 判断 Emoji 的本地化名称或关键词是否包含查询文本。
    /// </summary>
    /// <param name="emoji">待查询的 Emoji 元数据。</param>
    /// <param name="query">不区分大小写的查询文本。</param>
    /// <returns>任一本地化字段包含查询文本时为 true，否则为 false。</returns>
    private static bool ContainsLocalization(EmojiInfo emoji, string query)
    {
        return ContainsLocalization(emoji, query, null);
    }

    /// <summary>
    /// 判断指定语言的本地化字段是否包含查询文本。
    /// </summary>
    /// <param name="emoji">待查询的 Emoji 元数据。</param>
    /// <param name="query">不区分大小写的查询文本。</param>
    /// <param name="locale">可选的语言标识；为 null 时检查全部本地化数据。</param>
    /// <returns>匹配本地化字段时为 true，否则为 false。</returns>
    private static bool ContainsLocalization(EmojiInfo emoji, string query, string locale)
    {
        foreach (var localization in emoji.Localizations)
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
    /// 判断 Emoji 是否包含指定标签。
    /// </summary>
    /// <param name="emoji">待判断的 Emoji 元数据。</param>
    /// <param name="tag">不区分大小写的完整标签。</param>
    /// <returns>包含标签时为 true，否则为 false。</returns>
    private static bool HasTag(EmojiInfo emoji, string tag)
    {
        foreach (var value in emoji.Tags)
        {
            if (string.Equals(value, tag, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 将一个完整 Emoji 序列追加为 HTML 十六进制数字实体。
    /// </summary>
    /// <param name="builder">目标字符串构造器。</param>
    /// <param name="value">完整 Emoji 序列。</param>
    private static void AppendHtmlEntities(StringBuilder builder, string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var scalar = (int)value[index];
            if (char.IsHighSurrogate((char)scalar) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
            {
                scalar = char.ConvertToUtf32((char)scalar, value[++index]);
            }

            builder.Append("&#x")
                .Append(((int)scalar).ToString("X", CultureInfo.InvariantCulture))
                .Append(';');
        }
    }

    /// <summary>
    /// 将一段连续数字实体按最长 Emoji 序列追加到结果。
    /// </summary>
    /// <param name="builder">目标字符串构造器。</param>
    /// <param name="tokens">连续的数字实体令牌。</param>
    private static void AppendDecodedEntityRun(StringBuilder builder, IList<NumericEntityToken> tokens)
    {
        var decoded = new StringBuilder();
        var offsets = new int[tokens.Count];
        for (var index = 0; index < tokens.Count; index++)
        {
            offsets[index] = decoded.Length;
            decoded.Append(tokens[index].Unicode);
        }

        var value = decoded.ToString();
        var tokenIndex = 0;
        while (tokenIndex < tokens.Count)
        {
            var length = Catalog.Value.Match(value, offsets[tokenIndex], out var emoji);
            if (length > 0)
            {
                var matchEnd = offsets[tokenIndex] + length;
                var tokenEnd = tokenIndex;
                while (tokenEnd < tokens.Count && offsets[tokenEnd] < matchEnd)
                    tokenEnd++;

                if (tokenEnd > tokenIndex &&
                    offsets[tokenEnd - 1] + tokens[tokenEnd - 1].Unicode.Length == matchEnd)
                {
                    builder.Append(emoji.Unicode);
                    tokenIndex = tokenEnd;
                    continue;
                }
            }

            builder.Append(tokens[tokenIndex].Original);
            tokenIndex++;
        }
    }

    /// <summary>
    /// 尝试解析一个 Unicode 数字 HTML 实体。
    /// </summary>
    /// <param name="text">待解析文本。</param>
    /// <param name="index">实体起始索引。</param>
    /// <param name="end">成功时为实体结束后的索引。</param>
    /// <param name="unicode">成功时为对应的 UTF-16 字符串。</param>
    /// <returns>实体格式和 Unicode 标量均有效时为 true，否则为 false。</returns>
    private static bool TryParseNumericEntity(string text, int index, out int end, out string unicode)
    {
        end = index;
        unicode = null;
        if (index < 0 || index >= text.Length || text.Length - index < 4 ||
            text[index] != '&' || text[index + 1] != '#')
            return false;

        var cursor = index + 2;
        var hex = false;
        if (text[cursor] == 'x' || text[cursor] == 'X')
        {
            hex = true;
            cursor++;
        }

        var digitStart = cursor;
        var value = 0;
        var radix = hex ? 16 : 10;
        while (cursor < text.Length)
        {
            var digit = GetNumericEntityDigit(text[cursor], hex);
            if (digit < 0)
                break;
            if (value > (0x10FFFF - digit) / radix)
                return false;
            value = value * radix + digit;
            cursor++;
        }

        if (cursor == digitStart || cursor >= text.Length || text[cursor] != ';' ||
            value > 0x10FFFF || value >= 0xD800 && value <= 0xDFFF)
            return false;

        end = cursor + 1;
        unicode = char.ConvertFromUtf32(value);
        return true;
    }

    /// <summary>
    /// 将数字实体中的字符转换为数值。
    /// </summary>
    /// <param name="value">待转换字符。</param>
    /// <param name="hex">是否按十六进制解释。</param>
    /// <returns>有效数字值；字符不是当前进制数字时为 -1。</returns>
    private static int GetNumericEntityDigit(char value, bool hex)
    {
        if (value >= '0' && value <= '9')
            return value - '0';
        if (hex && value >= 'a' && value <= 'f')
            return value - 'a' + 10;
        if (hex && value >= 'A' && value <= 'F')
            return value - 'A' + 10;
        return -1;
    }

    /// <summary>
    /// 保存一个数字实体的原文和解码结果。
    /// </summary>
    private sealed class NumericEntityToken
    {
        /// <summary>
        /// 初始化一个 <see cref="NumericEntityToken" /> 类型的实例。
        /// </summary>
        /// <param name="original">数字实体原文。</param>
        /// <param name="unicode">对应的 Unicode 标量字符串。</param>
        internal NumericEntityToken(string original, string unicode)
        {
            Original = original;
            Unicode = unicode;
        }

        /// <summary>
        /// 获取数字实体原文。
        /// </summary>
        internal string Original { get; }

        /// <summary>
        /// 获取数字实体对应的 Unicode 标量字符串。
        /// </summary>
        internal string Unicode { get; }
    }
}
