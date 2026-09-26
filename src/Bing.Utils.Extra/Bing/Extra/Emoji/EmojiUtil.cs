using System;
using System.Collections.Generic;
using System.Text;

namespace Bing.Extra.Emoji;

/// <summary>
/// Emoji 识别、转换与查询工具。
/// </summary>
/// <remarks>
/// 使用 Unicode Emoji 18.0 与 gemoji v4.1.0 内置数据。
/// 按完整序列最长匹配，接受官方清单中的省略表情选择符形式；不匹配显式文本样式。
/// 未收录的组合可能匹配其中的已知子序列。本工具不负责字体渲染或 HTML 实体解码。
/// </remarks>
public static class EmojiUtil
{
    /// <summary>
    /// 首次访问时线程安全初始化的共享表情索引。
    /// </summary>
    private static readonly Lazy<EmojiCatalog> Catalog = new Lazy<EmojiCatalog>(() => new EmojiCatalog());

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
        if (string.IsNullOrEmpty(text))
            return text;
        StringBuilder builder = null;
        var index = 0;
        var copied = 0;
        while (TryFindNext(text, ref index, out var length, out var emoji))
        {
            if (builder == null)
                builder = new StringBuilder(text.Length);
            builder.Append(text, copied, index - copied);
            builder.Append(replacement(new EmojiMatch(text.Substring(index, length), index, length, emoji)));
            index += length;
            copied = index;
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
    /// 获取全部规范表情元数据。
    /// </summary>
    /// <returns>线程安全共享的只读元数据列表。</returns>
    /// <remarks>按官方清单顺序排列，不重复列出兼容形式。</remarks>
    public static IReadOnlyList<EmojiInfo> GetAll() => Catalog.Value.Items;

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
}
