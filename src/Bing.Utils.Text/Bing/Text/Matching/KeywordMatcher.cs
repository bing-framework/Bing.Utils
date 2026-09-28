using System.Collections.ObjectModel;
using System.Text;

namespace Bing.Text.Matching;

/// <summary>
/// 基于 Aho–Corasick 自动机的多关键词匹配器。
/// </summary>
/// <remarks>
/// 关键词按区分大小写的序数比较进行精确匹配。实例在构造后不再修改，可由多个线程并发查询。
/// 默认查询和替换采用最左最长、不重叠策略；查询可选择返回全部重叠结果。
/// </remarks>
public sealed class KeywordMatcher
{
    /// <summary>
    /// 空匹配结果集合。
    /// </summary>
    private static readonly IReadOnlyList<KeywordMatch> EmptyMatches =
        new ReadOnlyCollection<KeywordMatch>(new List<KeywordMatch>());

    /// <summary>
    /// 关键词自动机的根节点。
    /// </summary>
    private readonly MatchNode _root;

    /// <summary>
    /// 初始化一个 <see cref="KeywordMatcher"/> 类型的实例。
    /// </summary>
    /// <param name="keywords">要加入词库的关键词序列。</param>
    /// <exception cref="ArgumentNullException">关键词序列为 null。</exception>
    /// <exception cref="ArgumentException">关键词序列包含 null 或空字符串。</exception>
    public KeywordMatcher(IEnumerable<string> keywords)
    {
        if (keywords == null)
            throw new ArgumentNullException(nameof(keywords));

        _root = BuildAutomaton(keywords);
    }

    /// <summary>
    /// 判断文本中是否包含任一关键词。
    /// </summary>
    /// <param name="text">要检查的文本。</param>
    /// <returns>包含关键词时返回 true；否则返回 false。</returns>
    public bool ContainsAny(string text)
    {
        if (text == null)
            return false;

        var state = _root;
        for (var index = 0; index < text.Length; index++)
        {
            state = Advance(state, text[index]);
            if (state.Outputs.Count > 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 查找文本中的关键词。
    /// </summary>
    /// <param name="text">要检查的文本。</param>
    /// <param name="includeOverlaps">是否返回重叠匹配；false 表示采用最左最长、不重叠策略。</param>
    /// <returns>按原文位置排序的只读匹配结果。</returns>
    /// <remarks>
    /// 返回结果的索引和长度使用 UTF-16 代码单元。返回重叠结果时，同一起点的较长匹配排在前面。
    /// </remarks>
    public IReadOnlyList<KeywordMatch> FindAll(string text, bool includeOverlaps = false)
    {
        if (string.IsNullOrEmpty(text))
            return EmptyMatches;

        var rawMatches = new List<RawMatch>();
        Scan(text, rawMatches);
        if (rawMatches.Count == 0)
            return EmptyMatches;

        rawMatches.Sort(CompareRawMatches);
        var matches = new List<KeywordMatch>(rawMatches.Count);
        var nextIndex = 0;
        foreach (var rawMatch in rawMatches)
        {
            if (!includeOverlaps && rawMatch.Index < nextIndex)
                continue;

            matches.Add(CreateMatch(text, rawMatch));
            if (!includeOverlaps)
                nextIndex = rawMatch.Index + rawMatch.Length;
        }

        return matches.Count == 0 ? EmptyMatches : matches.AsReadOnly();
    }

    /// <summary>
    /// 替换文本中的关键词。
    /// </summary>
    /// <param name="text">要处理的文本。</param>
    /// <param name="replacement">用于替换每个匹配的字符串。</param>
    /// <returns>替换后的文本；输入文本为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">replacement 为 null。</exception>
    /// <remarks>
    /// 替换采用最左最长、不重叠策略，并且不会递归处理替换结果。
    /// </remarks>
    public string Replace(string text, string replacement)
    {
        if (replacement == null)
            throw new ArgumentNullException(nameof(replacement));

        if (text == null)
            return null;

        return ReplaceMatches(text, FindAll(text), _ => replacement);
    }

    /// <summary>
    /// 替换文本中的关键词。
    /// </summary>
    /// <param name="text">要处理的文本。</param>
    /// <param name="replacement">根据匹配结果生成替换内容的回调；返回 null 表示删除匹配内容。</param>
    /// <returns>替换后的文本；输入文本为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">replacement 为 null。</exception>
    /// <remarks>
    /// 替换采用最左最长、不重叠策略，不会递归处理回调生成的文本，回调异常会直接传播。
    /// </remarks>
    public string Replace(string text, Func<KeywordMatch, string> replacement)
    {
        if (replacement == null)
            throw new ArgumentNullException(nameof(replacement));

        if (text == null)
            return null;

        return ReplaceMatches(text, FindAll(text), replacement);
    }

    /// <summary>
    /// 构建关键词自动机。
    /// </summary>
    /// <param name="keywords">关键词序列。</param>
    /// <returns>构建完成的根节点。</returns>
    private static MatchNode BuildAutomaton(IEnumerable<string> keywords)
    {
        var root = new MatchNode();
        var uniqueKeywords = new HashSet<string>(StringComparer.Ordinal);
        foreach (var keyword in keywords)
        {
            if (keyword == null)
                throw new ArgumentException("关键词序列不能包含 null。", nameof(keywords));
            if (keyword.Length == 0)
                throw new ArgumentException("关键词不能是空字符串。", nameof(keywords));
            if (!uniqueKeywords.Add(keyword))
                continue;

            AddKeyword(root, keyword);
        }

        BuildFailureLinks(root);
        return root;
    }

    /// <summary>
    /// 将单个关键词加入自动机前缀树。
    /// </summary>
    /// <param name="root">自动机根节点。</param>
    /// <param name="keyword">要加入的关键词。</param>
    private static void AddKeyword(MatchNode root, string keyword)
    {
        var node = root;
        for (var index = 0; index < keyword.Length; index++)
        {
            var character = keyword[index];
            if (!node.Children.TryGetValue(character, out var next))
            {
                next = new MatchNode();
                node.Children.Add(character, next);
            }

            node = next;
        }

        node.Outputs.Add(new KeywordEntry(keyword));
    }

    /// <summary>
    /// 为自动机节点建立失败链接及继承的输出。
    /// </summary>
    /// <param name="root">自动机根节点。</param>
    private static void BuildFailureLinks(MatchNode root)
    {
        var queue = new Queue<MatchNode>();
        foreach (var child in root.Children.Values)
        {
            child.Failure = root;
            queue.Enqueue(child);
        }

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            foreach (var transition in node.Children)
            {
                var child = transition.Value;
                var fallback = node.Failure;
                while (!ReferenceEquals(fallback, root) && !fallback.Children.ContainsKey(transition.Key))
                    fallback = fallback.Failure;

                if (fallback.Children.TryGetValue(transition.Key, out var failure) && !ReferenceEquals(failure, child))
                    child.Failure = failure;
                else
                    child.Failure = root;

                if (child.Failure.Outputs.Count > 0)
                    child.Outputs.AddRange(child.Failure.Outputs);

                queue.Enqueue(child);
            }
        }
    }

    /// <summary>
    /// 将自动机状态推进到下一个节点。
    /// </summary>
    /// <param name="state">当前节点。</param>
    /// <param name="character">待处理的 UTF-16 字符。</param>
    /// <returns>处理字符后的节点。</returns>
    private MatchNode Advance(MatchNode state, char character)
    {
        while (true)
        {
            if (state.Children.TryGetValue(character, out var next))
                return next;
            if (ReferenceEquals(state, _root))
                return _root;

            state = state.Failure;
        }
    }

    /// <summary>
    /// 扫描文本并收集所有原始匹配。
    /// </summary>
    /// <param name="text">要扫描的文本。</param>
    /// <param name="matches">用于接收匹配结果的列表。</param>
    private void Scan(string text, ICollection<RawMatch> matches)
    {
        var state = _root;
        for (var index = 0; index < text.Length; index++)
        {
            state = Advance(state, text[index]);
            foreach (var entry in state.Outputs)
                matches.Add(new RawMatch(entry, index - entry.Length + 1));
        }
    }

    /// <summary>
    /// 比较两个原始匹配的排序位置。
    /// </summary>
    /// <param name="left">第一个匹配。</param>
    /// <param name="right">第二个匹配。</param>
    /// <returns>按起点升序、长度降序返回比较结果。</returns>
    private static int CompareRawMatches(RawMatch left, RawMatch right)
    {
        var indexComparison = left.Index.CompareTo(right.Index);
        return indexComparison != 0 ? indexComparison : right.Length.CompareTo(left.Length);
    }

    /// <summary>
    /// 创建公开的匹配结果。
    /// </summary>
    /// <param name="text">原文。</param>
    /// <param name="rawMatch">内部匹配。</param>
    /// <returns>包含原文内容的匹配结果。</returns>
    private static KeywordMatch CreateMatch(string text, RawMatch rawMatch) =>
        new KeywordMatch(rawMatch.Entry.Keyword, text.Substring(rawMatch.Index, rawMatch.Length), rawMatch.Index, rawMatch.Length);

    /// <summary>
    /// 按匹配结果执行替换。
    /// </summary>
    /// <param name="text">原文。</param>
    /// <param name="matches">不重叠的匹配结果。</param>
    /// <param name="replacement">替换回调。</param>
    /// <returns>替换后的文本。</returns>
    private static string ReplaceMatches(string text, IReadOnlyList<KeywordMatch> matches, Func<KeywordMatch, string> replacement)
    {
        if (matches.Count == 0)
            return text;

        var builder = new StringBuilder(text.Length);
        var previousIndex = 0;
        foreach (var match in matches)
        {
            builder.Append(text, previousIndex, match.Index - previousIndex);
            var replacementValue = replacement(match);
            if (replacementValue != null)
                builder.Append(replacementValue);
            previousIndex = match.Index + match.Length;
        }

        builder.Append(text, previousIndex, text.Length - previousIndex);
        return builder.ToString();
    }

    /// <summary>
    /// 自动机节点。
    /// </summary>
    private sealed class MatchNode
    {
        /// <summary>
        /// 当前节点的字符转移。
        /// </summary>
        public readonly Dictionary<char, MatchNode> Children = new Dictionary<char, MatchNode>();

        /// <summary>
        /// 当前节点的失败链接。
        /// </summary>
        public MatchNode Failure;

        /// <summary>
        /// 到达当前节点时结束的关键词及其失败链接上的关键词。
        /// </summary>
        public readonly List<KeywordEntry> Outputs = new List<KeywordEntry>();
    }

    /// <summary>
    /// 自动机中的关键词记录。
    /// </summary>
    private sealed class KeywordEntry
    {
        /// <summary>
        /// 初始化一个 <see cref="KeywordEntry"/> 类型的实例。
        /// </summary>
        /// <param name="keyword">关键词文本。</param>
        public KeywordEntry(string keyword)
        {
            Keyword = keyword;
            Length = keyword.Length;
        }

        /// <summary>
        /// 关键词文本。
        /// </summary>
        public readonly string Keyword;

        /// <summary>
        /// 关键词的 UTF-16 长度。
        /// </summary>
        public readonly int Length;
    }

    /// <summary>
    /// 自动机扫描产生的内部匹配。
    /// </summary>
    private sealed class RawMatch
    {
        /// <summary>
        /// 初始化一个 <see cref="RawMatch"/> 类型的实例。
        /// </summary>
        /// <param name="entry">匹配到的关键词记录。</param>
        /// <param name="index">匹配起始索引。</param>
        public RawMatch(KeywordEntry entry, int index)
        {
            Entry = entry;
            Index = index;
        }

        /// <summary>
        /// 匹配到的关键词记录。
        /// </summary>
        public readonly KeywordEntry Entry;

        /// <summary>
        /// 匹配起始索引。
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// 获取匹配的 UTF-16 长度。
        /// </summary>
        public int Length => Entry.Length;
    }
}
