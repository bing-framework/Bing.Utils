using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bing.Text.Matching;
using Shouldly;
using Xunit;

namespace Bing.Text.Matching;

/// <summary>
/// 测试 <see cref="KeywordMatcher" /> 的关键词匹配、替换、边界和并发契约。
/// </summary>
[Trait("TextUT", "KeywordMatcher")]
public class KeywordMatcherTests
{
    /// <summary>
    /// 测试目的：包含判断应区分大小写，并按原文精确查找关键词。
    /// </summary>
    [Fact]
    public void ContainsAny_UsesCaseSensitiveExactMatching()
    {
        var matcher = new KeywordMatcher(new[] { "Cat", "中文" });

        matcher.ContainsAny("cat").ShouldBeFalse();
        matcher.ContainsAny("A中文!").ShouldBeTrue();
        matcher.ContainsAny(null).ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：默认查询应从左到右选择同一起点的最长关键词，并跳过重叠命中。
    /// </summary>
    [Fact]
    public void FindAll_DefaultUsesLeftmostLongestSelection()
    {
        var matcher = new KeywordMatcher(new[] { "中国", "中国人", "国人", "人" });

        var matches = matcher.FindAll("中国人");

        matches.Count.ShouldBe(1);
        matches[0].Keyword.ShouldBe("中国人");
        matches[0].Value.ShouldBe("中国人");
        matches[0].Index.ShouldBe(0);
        matches[0].Length.ShouldBe(3);
    }

    /// <summary>
    /// 测试目的：启用重叠查询时应返回全部命中，并按起点升序、同起点长度降序排列。
    /// </summary>
    [Fact]
    public void FindAll_WithOverlapsReturnsAllMatchesInStableOrder()
    {
        var matcher = new KeywordMatcher(new[] { "中国", "中国人", "国人" });

        var matches = matcher.FindAll("中国人", includeOverlaps: true);

        matches.Select(match => match.Value).ShouldBe(new[] { "中国人", "中国", "国人" });
        matches.Select(match => match.Index).ShouldBe(new[] { 0, 0, 1 });
        matches.Select(match => match.Length).ShouldBe(new[] { 3, 2, 2 });
    }

    /// <summary>
    /// 测试目的：默认策略应选择最左命中，而不是在全文范围内选择长度最大的命中。
    /// </summary>
    [Fact]
    public void FindAll_DefaultIsLeftmostRatherThanGloballyLongest()
    {
        var matcher = new KeywordMatcher(new[] { "ab", "bcde" });

        var defaultMatches = matcher.FindAll("abcde");
        var allMatches = matcher.FindAll("abcde", includeOverlaps: true);

        defaultMatches.Select(match => match.Value).ShouldBe(new[] { "ab" });
        allMatches.Select(match => match.Value).ShouldBe(new[] { "ab", "bcde" });
    }

    /// <summary>
    /// 测试目的：不重叠的相邻关键词应按原文顺序全部返回。
    /// </summary>
    [Fact]
    public void FindAll_ReturnsAdjacentKeywordMatches()
    {
        var matcher = new KeywordMatcher(new[] { "ab", "cd" });

        var matches = matcher.FindAll("abcd");

        matches.Select(match => match.Value).ShouldBe(new[] { "ab", "cd" });
        matches.Select(match => match.Index).ShouldBe(new[] { 0, 2 });
    }

    /// <summary>
    /// 测试目的：同一关键词在文本中重复出现时应分别返回每个位置。
    /// </summary>
    [Fact]
    public void FindAll_ReturnsRepeatedOccurrencesOfSameKeyword()
    {
        var matcher = new KeywordMatcher(new[] { "go" });

        var matches = matcher.FindAll("go-go-go");

        matches.Select(match => match.Value).ShouldBe(new[] { "go", "go", "go" });
        matches.Select(match => match.Index).ShouldBe(new[] { 0, 3, 6 });
    }

    /// <summary>
    /// 测试目的：关键词中的标点符号应按原文精确匹配，不被特殊解释或规范化。
    /// </summary>
    [Fact]
    public void FindAll_MatchesKeywordsContainingPunctuationExactly()
    {
        var matcher = new KeywordMatcher(new[] { "C#", "a.b", "[x]" });

        var matches = matcher.FindAll("C# a.b [x]");

        matches.Select(match => match.Value).ShouldBe(new[] { "C#", "a.b", "[x]" });
        matches.Select(match => match.Index).ShouldBe(new[] { 0, 3, 7 });
    }

    /// <summary>
    /// 测试目的：自动机失败转移应保留不同后缀路径上的全部匹配。
    /// </summary>
    [Fact]
    public void FindAll_UsesFailureTransitionsForOverlappingSuffixes()
    {
        var matcher = new KeywordMatcher(new[] { "he", "she", "his", "hers" });

        var defaultMatches = matcher.FindAll("ushers");
        var allMatches = matcher.FindAll("ushers", includeOverlaps: true);

        defaultMatches.Select(match => match.Value).ShouldBe(new[] { "she" });
        allMatches.Select(match => match.Value).ShouldBe(new[] { "she", "hers", "he" });
        allMatches.Select(match => match.Index).ShouldBe(new[] { 1, 2, 2 });
    }

    /// <summary>
    /// 测试目的：空文本和空查询结果应返回空集合，且空词库仍可正常使用。
    /// </summary>
    [Fact]
    public void FindAll_EmptyInputsAndEmptyDictionaryReturnEmptyResults()
    {
        var matcher = new KeywordMatcher(Array.Empty<string>());

        matcher.ContainsAny("").ShouldBeFalse();
        matcher.FindAll(null).ShouldBeEmpty();
        matcher.FindAll("").ShouldBeEmpty();
        matcher.Replace("text", "replacement").ShouldBe("text");
    }

    /// <summary>
    /// 测试目的：匹配结果集合应对调用方保持只读。
    /// </summary>
    [Fact]
    public void FindAll_ReturnsReadOnlyCollection()
    {
        var matcher = new KeywordMatcher(new[] { "a" });
        var matches = matcher.FindAll("a");
        var list = (IList<KeywordMatch>)matches;

        list.IsReadOnly.ShouldBeTrue();
        Should.Throw<NotSupportedException>(() => list.Add(null));
    }

    /// <summary>
    /// 测试目的：词库为空、包含空条目或为 null 时应按契约抛出对应异常。
    /// </summary>
    [Fact]
    public void Constructor_ValidatesKeywordSource()
    {
        Should.Throw<ArgumentNullException>(() => new KeywordMatcher(null));
        Should.Throw<ArgumentException>(() => new KeywordMatcher(new string[] { null }));
        Should.Throw<ArgumentException>(() => new KeywordMatcher(new[] { "" }));
    }

    /// <summary>
    /// 测试目的：重复关键词应去重，纯空白关键词应按原文参与匹配。
    /// </summary>
    [Fact]
    public void Constructor_DeduplicatesExactKeywordsAndKeepsWhitespace()
    {
        var matcher = new KeywordMatcher(new[] { "foo", "foo", " " });

        var matches = matcher.FindAll("foo bar");

        matches.Select(match => match.Value).ShouldBe(new[] { "foo", " " });
    }

    /// <summary>
    /// 测试目的：匹配位置和长度应使用 UTF-16 单位，并能直接切回原文。
    /// </summary>
    [Fact]
    public void FindAll_ReportsUtf16PositionsForSupplementaryCharacters()
    {
        const string text = "A😀中👍🏽";
        var matcher = new KeywordMatcher(new[] { "😀", "👍🏽" });

        var matches = matcher.FindAll(text);

        matches.Count.ShouldBe(2);
        matches[0].Index.ShouldBe(1);
        matches[0].Length.ShouldBe(2);
        matches[1].Index.ShouldBe(4);
        matches[1].Length.ShouldBe(4);
        foreach (var match in matches)
            text.Substring(match.Index, match.Length).ShouldBe(match.Value);
    }

    /// <summary>
    /// 测试目的：孤立代理项应按普通 UTF-16 代码单元匹配且不触发解码异常。
    /// </summary>
    [Fact]
    public void FindAll_PreservesIsolatedSurrogateCodeUnits()
    {
        var isolatedSurrogate = "\uD800";
        var text = "A" + isolatedSurrogate + "B";
        var matcher = new KeywordMatcher(new[] { isolatedSurrogate });

        var matches = matcher.FindAll(text);

        matches.Count.ShouldBe(1);
        matches[0].Value.ShouldBe(isolatedSurrogate);
        matches[0].Index.ShouldBe(1);
        matches[0].Length.ShouldBe(1);
    }

    /// <summary>
    /// 测试目的：固定替换应使用最左最长结果，并保留未匹配文本。
    /// </summary>
    [Fact]
    public void Replace_WithFixedValueUsesDefaultSelection()
    {
        var matcher = new KeywordMatcher(new[] { "中国", "中国人", "国人" });

        var result = matcher.Replace("中国人和中国", "[词]");

        result.ShouldBe("[词]和[词]");
    }

    /// <summary>
    /// 测试目的：回调替换应能够读取匹配内容，并将 null 结果视为删除。
    /// </summary>
    [Fact]
    public void Replace_WithCallbackSupportsMatchDataAndDeletion()
    {
        var matcher = new KeywordMatcher(new[] { "cat", "dog" });

        var marked = matcher.Replace("cat dog", match => $"<{match.Keyword}:{match.Index}>");
        var deleted = matcher.Replace("cat dog", match => match.Keyword == "dog" ? null : $"[{match.Value}]");

        marked.ShouldBe("<cat:0> <dog:4>");
        deleted.ShouldBe("[cat] ");
    }

    /// <summary>
    /// 测试目的：替换结果不应再次作为输入递归匹配。
    /// </summary>
    [Fact]
    public void Replace_DoesNotReprocessReplacementText()
    {
        var matcher = new KeywordMatcher(new[] { "ab", "cd" });

        var result = matcher.Replace("ab", _ => "cd");

        result.ShouldBe("cd");
    }

    /// <summary>
    /// 测试目的：替换参数、空文本和回调异常应遵循公开契约。
    /// </summary>
    [Fact]
    public void Replace_ValidatesNullArgumentsAndPropagatesCallbackErrors()
    {
        var matcher = new KeywordMatcher(new[] { "a" });

        Should.Throw<ArgumentNullException>(() => matcher.Replace("", (string)null));
        Should.Throw<ArgumentNullException>(() => matcher.Replace("", (Func<KeywordMatch, string>)null));
        matcher.Replace(null, "replacement").ShouldBeNull();
        matcher.Replace(null, (Func<KeywordMatch, string>)(_ => "replacement")).ShouldBeNull();
        Should.Throw<InvalidOperationException>(() => matcher.Replace("a", _ => throw new InvalidOperationException("callback")));
    }

    /// <summary>
    /// 测试目的：构造后的匹配器应保存词库快照，不受原始集合后续修改影响。
    /// </summary>
    [Fact]
    public void Constructor_TakesSnapshotOfMutableKeywordSource()
    {
        var keywords = new[] { "alpha" };
        var matcher = new KeywordMatcher(keywords);

        keywords[0] = "beta";

        matcher.ContainsAny("alpha").ShouldBeTrue();
        matcher.ContainsAny("beta").ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：同一实例被并发查询时应返回一致结果且不共享可变扫描状态。
    /// </summary>
    [Fact]
    public async Task Matcher_CanBeReusedConcurrently()
    {
        var matcher = new KeywordMatcher(new[] { "hello", "中国", "中国人", "😀" });
        const string text = "hello中国人😀";
        const string expected = "hello:0:5|中国人:5:3|😀:8:2";

        var results = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() =>
        {
            var matches = matcher.FindAll(text);
            return string.Join("|", matches.Select(match => $"{match.Value}:{match.Index}:{match.Length}"));
        })));

        foreach (var result in results)
            result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：固定种子生成的 ASCII/中文样本应与独立朴素扫描在两种查询模式下保持一致。
    /// </summary>
    [Fact]
    public void FindAll_FixedSeedRandomCasesMatchIndependentNaiveScan()
    {
        var random = new Random(20260927);

        for (var caseIndex = 0; caseIndex < 256; caseIndex++)
        {
            var text = CreateRandomText(random, random.Next(0, 31));
            var keywords = CreateRandomKeywords(random, text);
            var matcher = new KeywordMatcher(keywords);

            AssertMatchesEqual(text, keywords, matcher.FindAll(text), includeOverlaps: false);
            AssertMatchesEqual(text, keywords, matcher.FindAll(text, includeOverlaps: true), includeOverlaps: true);
        }
    }

    /// <summary>
    /// 使用简单的逐起点扫描生成预期结果，避免复用自动机实现逻辑。
    /// </summary>
    /// <param name="text">待匹配文本。</param>
    /// <param name="keywords">关键词列表。</param>
    /// <param name="includeOverlaps">是否包含重叠匹配。</param>
    /// <returns>按选择规则排列的预期匹配。</returns>
    private static IReadOnlyList<ExpectedMatch> NaiveFindAll(string text, IReadOnlyList<string> keywords, bool includeOverlaps)
    {
        var allMatches = new List<ExpectedMatch>();
        for (var index = 0; index < text.Length; index++)
        {
            foreach (var keyword in keywords)
            {
                if (keyword.Length <= text.Length - index &&
                    string.Equals(text.Substring(index, keyword.Length), keyword, StringComparison.Ordinal))
                {
                    allMatches.Add(new ExpectedMatch(keyword, index, keyword.Length));
                }
            }
        }

        allMatches.Sort((left, right) =>
        {
            var indexComparison = left.Index.CompareTo(right.Index);
            return indexComparison != 0 ? indexComparison : right.Length.CompareTo(left.Length);
        });

        if (includeOverlaps)
            return allMatches;

        var selectedMatches = new List<ExpectedMatch>();
        var nextIndex = 0;
        foreach (var match in allMatches)
        {
            if (match.Index < nextIndex)
                continue;

            selectedMatches.Add(match);
            nextIndex = match.Index + match.Length;
        }

        return selectedMatches;
    }

    /// <summary>
    /// 将实际匹配逐项与朴素扫描结果进行比较。
    /// </summary>
    /// <param name="text">待匹配文本。</param>
    /// <param name="keywords">关键词列表。</param>
    /// <param name="actualMatches">实际匹配结果。</param>
    /// <param name="includeOverlaps">是否包含重叠匹配。</param>
    private static void AssertMatchesEqual(
        string text,
        IReadOnlyList<string> keywords,
        IReadOnlyList<KeywordMatch> actualMatches,
        bool includeOverlaps)
    {
        var expectedMatches = NaiveFindAll(text, keywords, includeOverlaps);

        actualMatches.Count.ShouldBe(expectedMatches.Count);
        for (var index = 0; index < expectedMatches.Count; index++)
        {
            var expected = expectedMatches[index];
            var actual = actualMatches[index];
            actual.Keyword.ShouldBe(expected.Value);
            actual.Value.ShouldBe(expected.Value);
            actual.Index.ShouldBe(expected.Index);
            actual.Length.ShouldBe(expected.Length);
            text.Substring(actual.Index, actual.Length).ShouldBe(actual.Value);
        }
    }

    /// <summary>
    /// 根据固定随机源生成由 ASCII 和中文字符组成的文本。
    /// </summary>
    /// <param name="random">随机数生成器。</param>
    /// <param name="length">文本长度。</param>
    /// <returns>随机测试文本。</returns>
    private static string CreateRandomText(Random random, int length)
    {
        const string alphabet = "abcz中文测试";
        var characters = new char[length];
        for (var index = 0; index < characters.Length; index++)
            characters[index] = alphabet[random.Next(alphabet.Length)];

        return new string(characters);
    }

    /// <summary>
    /// 生成去重关键词，并在非空文本中嵌入一个已知命中。
    /// </summary>
    /// <param name="random">随机数生成器。</param>
    /// <param name="text">用于嵌入关键词的文本。</param>
    /// <returns>去重后的关键词列表。</returns>
    private static IReadOnlyList<string> CreateRandomKeywords(Random random, string text)
    {
        var keywords = new HashSet<string>(StringComparer.Ordinal);
        var keywordCount = random.Next(1, 9);
        while (keywords.Count < keywordCount)
            keywords.Add(CreateRandomText(random, random.Next(1, 6)));

        if (text.Length > 0)
        {
            var embeddedLength = random.Next(1, Math.Min(5, text.Length) + 1);
            var embeddedIndex = random.Next(0, text.Length - embeddedLength + 1);
            keywords.Add(text.Substring(embeddedIndex, embeddedLength));
        }

        return keywords.ToArray();
    }

    /// <summary>
    /// 独立朴素扫描使用的匹配预期值。
    /// </summary>
    private readonly struct ExpectedMatch
    {
        /// <summary>
        /// 初始化一个 <see cref="ExpectedMatch"/> 类型的实例。
        /// </summary>
        /// <param name="value">匹配内容。</param>
        /// <param name="index">匹配起始索引。</param>
        /// <param name="length">匹配长度。</param>
        public ExpectedMatch(string value, int index, int length)
        {
            Value = value;
            Index = index;
            Length = length;
        }

        /// <summary>
        /// 匹配内容。
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// 匹配起始索引。
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// 匹配长度。
        /// </summary>
        public int Length { get; }
    }
}
