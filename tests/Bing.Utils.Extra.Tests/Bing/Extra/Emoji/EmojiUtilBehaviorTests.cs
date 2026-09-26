using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bing.Extra.Emoji;
using Shouldly;
using Xunit;

namespace Bing.Utils.Extra.Tests.Bing.Extra.Emoji;

/// <summary>
/// 验证 Emoji 工具的匹配、转换和只读集合契约。
/// </summary>
[Trait("Category", "Emoji")]
public sealed class EmojiUtilBehaviorTests
{
    /// <summary>
    /// 验证常见 Emoji 形式可被识别。
    /// </summary>
    /// <param name="value">待判断的 Emoji 字符串。</param>
    [Theory]
    [InlineData("😀")]
    [InlineData("❤")]
    [InlineData("©")]
    public void IsEmoji_QualifiedVariants_ReturnsTrue(string value)
    {
        EmojiUtil.IsEmoji(value).ShouldBeTrue();
        EmojiUtil.ContainsEmoji($"prefix {value} suffix").ShouldBeTrue();
        EmojiUtil.Count($"prefix {value} suffix").ShouldBe(1);
    }

    /// <summary>
    /// 验证完整 Emoji 序列与普通文本的识别结果。
    /// </summary>
    [Fact]
    public void IsEmoji_EmojiSequenceAndText_ReturnsExpectedResult()
    {
        EmojiUtil.IsEmoji("👨‍👩‍👧‍👦").ShouldBeTrue();
        EmojiUtil.IsEmoji("plain text").ShouldBeFalse();
        EmojiUtil.IsEmoji("😀 text").ShouldBeFalse();
        EmojiUtil.ContainsEmoji("plain text").ShouldBeFalse();
        EmojiUtil.Count("😀😃🎉").ShouldBe(3);
    }

    /// <summary>
    /// 验证皮肤色、国旗、键帽、职业和标签序列可被识别。
    /// </summary>
    [Fact]
    public void IsEmoji_SkinToneFlagKeycapProfessionAndTagSequences_ReturnsTrue()
    {
        var englandFlag = "\U0001F3F4\U000E0067\U000E0062\U000E0065\U000E006E\U000E0067\U000E007F";
        var examples = new[] { "👍🏽", "🇨🇳", "1️⃣", "👩‍💻", englandFlag };

        foreach (var value in examples)
        {
            EmojiUtil.IsEmoji(value).ShouldBeTrue(value);
            EmojiUtil.Count(value).ShouldBe(1, value);
            EmojiUtil.FindAll(value).Single().Length.ShouldBe(value.Length, value);
        }

        var text = "A😀中👍🏽";
        var matches = EmojiUtil.FindAll(text);
        matches.Select(match => match.Value).ShouldBe(new[] { "😀", "👍🏽" });
        matches[0].Index.ShouldBe(1);
        matches[1].Index.ShouldBe(4);
    }

    /// <summary>
    /// 验证显式文本呈现选择符不会被识别为 Emoji。
    /// </summary>
    /// <param name="textPresentation">包含文本呈现选择符的字符序列。</param>
    [Theory]
    [InlineData("❤\uFE0E")]
    [InlineData("☀\uFE0E")]
    [InlineData("©\uFE0E")]
    [InlineData("👨‍👩‍👧‍👦\uFE0E")]
    public void ExplicitTextPresentationSelector_IsNotMatched(string textPresentation)
    {
        EmojiUtil.IsEmoji(textPresentation).ShouldBeFalse();
        EmojiUtil.ContainsEmoji($"before{textPresentation}after").ShouldBeFalse();
        EmojiUtil.FindAll($"before{textPresentation}after").ShouldBeEmpty();
        EmojiUtil.RemoveAllEmojis(textPresentation + "😀").ShouldBe(textPresentation);
        EmojiUtil.ToAlias(textPresentation).ShouldBe(textPresentation);
    }

    /// <summary>
    /// 验证匹配结果保留原文并使用 UTF-16 索引和长度。
    /// </summary>
    [Fact]
    public void FindAll_UsesUtf16IndexAndLength_AndPreservesOriginalValue()
    {
        var text = "A👨‍👩‍👧‍👦B😀C";

        var matches = EmojiUtil.FindAll(text);

        matches.Count.ShouldBe(2);
        matches[0].Value.ShouldBe("👨‍👩‍👧‍👦");
        matches[0].Index.ShouldBe(1);
        matches[0].Length.ShouldBe("👨‍👩‍👧‍👦".Length);
        text.Substring(matches[0].Index, matches[0].Length).ShouldBe(matches[0].Value);
        matches[0].Emoji.ShouldNotBeNull();
        matches[1].Value.ShouldBe("😀");
        matches[1].Index.ShouldBe(1 + matches[0].Length + 1);
        matches[1].Length.ShouldBe(2);
    }

    /// <summary>
    /// 验证提取和移除操作保持出现顺序及普通文本。
    /// </summary>
    [Fact]
    public void ExtractAndRemove_PreserveTextAndOccurrenceOrder()
    {
        var text = "left😀middle👨‍👩‍👧‍👦right😀";

        var extracted = EmojiUtil.ExtractEmojis(text);

        extracted.ShouldBe(new[] { "😀", "👨‍👩‍👧‍👦", "😀" });
        EmojiUtil.RemoveAllEmojis(text).ShouldBe("leftmiddleright");
    }

    /// <summary>
    /// 验证字符串和回调替换逐个处理原始匹配。
    /// </summary>
    [Fact]
    public void Replace_StringAndCallback_ReplaceEachOriginalMatchOnce()
    {
        EmojiUtil.Replace("hello 😀 and 😃", "[emoji]").ShouldBe("hello [emoji] and [emoji]");

        var calls = new List<EmojiMatch>();
        var result = EmojiUtil.Replace("😀😃", match =>
        {
            calls.Add(match);
            return "😀";
        });

        result.ShouldBe("😀😀");
        calls.Count.ShouldBe(2);
        calls.Select(match => match.Value).ShouldBe(new[] { "😀", "😃" });
    }

    /// <summary>
    /// 验证替换回调异常传播及空回调拒绝行为。
    /// </summary>
    [Fact]
    public void Replace_CallbackExceptionPropagates_AndNullCallbackIsRejected()
    {
        var expected = new InvalidOperationException("replacement failed");
        Should.Throw<InvalidOperationException>(() => EmojiUtil.Replace("😀", _ => throw expected))
            .ShouldBeSameAs(expected);

        Func<EmojiMatch, string> callback = null;
        Should.Throw<ArgumentNullException>(() => EmojiUtil.Replace("😀", callback));
        EmojiUtil.Replace("a😀b", _ => null).ShouldBe("ab");
    }

    /// <summary>
    /// 验证 Emoji 与别名之间的规范化转换和查询。
    /// </summary>
    [Fact]
    public void AliasAndUnicodeConversion_UseCanonicalValues()
    {
        EmojiUtil.ToUnicode("Say :grinning:!").ShouldBe("Say 😀!");
        EmojiUtil.ToAlias("Say 😀!").ShouldBe("Say :grinning:!");
        EmojiUtil.ToUnicode(":GRINNING:").ShouldBe(":GRINNING:");
        EmojiUtil.ToUnicode(":unknown_alias:").ShouldBe(":unknown_alias:");
        EmojiUtil.ToUnicode("a :missing::grinning::smile: b").ShouldBe("a :missing:😀😄 b");
        EmojiUtil.ToUnicode("Time: 12 :smile: &#128516;").ShouldBe("Time: 12 😄 &#128516;");

        EmojiUtil.TryGetByAlias("grinning", out var byAlias).ShouldBeTrue();
        byAlias.ShouldNotBeNull();
        byAlias.Unicode.ShouldBe("😀");
        byAlias.Aliases.ShouldContain("grinning");
        EmojiUtil.TryGetByAlias(":grinning:", out var byDelimitedAlias).ShouldBeTrue();
        byDelimitedAlias.ShouldBeSameAs(byAlias);

        EmojiUtil.TryGetByAlias("Grinning", out _).ShouldBeFalse();
        EmojiUtil.TryGetByAlias("missing_alias", out _).ShouldBeFalse();
        EmojiUtil.TryGetByUnicode("😀", out var byUnicode).ShouldBeTrue();
        byUnicode.ShouldBeSameAs(byAlias);
        EmojiUtil.TryGetByUnicode("missing", out _).ShouldBeFalse();

        EmojiUtil.TryGetByUnicode("❤", out var normalizedHeart).ShouldBeTrue();
        normalizedHeart.Unicode.ShouldBe("❤️");
        EmojiUtil.ToUnicode(":heart:").ShouldBe("❤️");
        EmojiUtil.ToAlias("❤").ShouldBe(":heart:");
        EmojiUtil.ToAlias("👍🏽").ShouldBe("👍🏽");
        EmojiUtil.TryGetByUnicode("👍🏽", out var skinTone).ShouldBeTrue();
        skinTone.Aliases.ShouldBeEmpty();
    }

    /// <summary>
    /// 验证孤立代理项不会误判且有效代理项保持为一个 Emoji。
    /// </summary>
    [Fact]
    public void StandaloneSurrogates_AreNotEmoji_AndValidPairIsMatchedAsOneValue()
    {
        var highSurrogate = "\uD83D";
        var lowSurrogate = "\uDE00";

        EmojiUtil.IsEmoji(highSurrogate).ShouldBeFalse();
        EmojiUtil.IsEmoji(lowSurrogate).ShouldBeFalse();
        EmojiUtil.ContainsEmoji(highSurrogate + lowSurrogate).ShouldBeTrue();
        EmojiUtil.FindAll(highSurrogate + lowSurrogate).Single().Value.ShouldBe("😀");

        var ordinaryText = "123#*" + highSurrogate + "|" + lowSurrogate;
        EmojiUtil.ContainsEmoji(ordinaryText).ShouldBeFalse();
        EmojiUtil.RemoveAllEmojis(ordinaryText).ShouldBe(ordinaryText);
    }

    /// <summary>
    /// 验证独立 Emoji 组件和普通符号保持原样。
    /// </summary>
    /// <param name="component">待验证的独立 Emoji 组件。</param>
    [Theory]
    [InlineData("🏽")]
    [InlineData("🦰")]
    public void StandaloneComponents_ArePreserved(string component)
    {
        EmojiUtil.IsEmoji(component).ShouldBeFalse();
        EmojiUtil.Count(component).ShouldBe(0);
        EmojiUtil.RemoveAllEmojis(component).ShouldBe(component);
    }

    /// <summary>
    /// 验证空值和空字符串遵循约定的返回或异常行为。
    /// </summary>
    [Fact]
    public void NullAndEmptyInputs_FollowEmptyOrNullPreservingContracts()
    {
        EmojiUtil.IsEmoji(null).ShouldBeFalse();
        EmojiUtil.ContainsEmoji(null).ShouldBeFalse();
        EmojiUtil.Count(null).ShouldBe(0);
        EmojiUtil.ExtractEmojis(null).ShouldBeEmpty();
        EmojiUtil.FindAll(null).ShouldBeEmpty();
        EmojiUtil.RemoveAllEmojis(null).ShouldBeNull();
        EmojiUtil.Replace(null, "replacement").ShouldBeNull();
        Should.Throw<ArgumentNullException>(() => EmojiUtil.Replace("😀", (string)null));
        Should.Throw<ArgumentNullException>(() => EmojiUtil.Replace(string.Empty, (string)null));
        Should.Throw<ArgumentNullException>(() => EmojiUtil.Replace(null, (string)null));
        Should.Throw<ArgumentNullException>(() => EmojiUtil.Replace(null, (Func<EmojiMatch, string>)null));
        Should.Throw<ArgumentNullException>(() => EmojiUtil.Replace(string.Empty, (Func<EmojiMatch, string>)null));
        EmojiUtil.Replace(null, _ => "replacement").ShouldBeNull();
        EmojiUtil.ToUnicode(null).ShouldBeNull();
        EmojiUtil.ToAlias(null).ShouldBeNull();
        EmojiUtil.TryGetByAlias(null, out var alias).ShouldBeFalse();
        EmojiUtil.TryGetByUnicode(null, out var unicode).ShouldBeFalse();
        alias.ShouldBeNull();
        unicode.ShouldBeNull();

        EmojiUtil.ExtractEmojis(string.Empty).ShouldBeEmpty();
        EmojiUtil.FindAll(string.Empty).ShouldBeEmpty();
        EmojiUtil.RemoveAllEmojis(string.Empty).ShouldBe(string.Empty);
    }

    /// <summary>
    /// 验证公开列表和元数据别名列表不支持写入。
    /// </summary>
    [Fact]
    public void PublicLists_AndMetadataAliases_AreReadOnly()
    {
        var extracted = EmojiUtil.ExtractEmojis("😀");
        var matches = EmojiUtil.FindAll("😀");
        var all = EmojiUtil.GetAll();

        AssertReadOnly(extracted);
        AssertReadOnly(matches);
        AssertReadOnly(all);
        AssertReadOnly(all[0].Aliases);
    }

    /// <summary>
    /// 异步验证并发访问结果的一致性。
    /// </summary>
    /// <remarks>需在独立进程中单独筛选本测试，才能验证首次初始化场景。</remarks>
    [Fact]
    [Trait("Category", "Emoji.Concurrency")]
    public async Task FirstUse_ConcurrentCalls_ReturnConsistentResults()
    {
        var tasks = Enumerable.Range(0, 64)
            .Select(_ => Task.Run(() =>
            {
                var all = EmojiUtil.GetAll();
                return (Count: all.Count, Unicode: EmojiUtil.ToUnicode(":grinning:"), Found: EmojiUtil.ContainsEmoji("x😀y"));
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        results.Select(result => result.Count).Distinct().ShouldHaveSingleItem();
        results.Select(result => result.Unicode).Distinct().ShouldBe(new[] { "😀" });
        results.Select(result => result.Found).Distinct().ShouldBe(new[] { true });
    }

    /// <summary>
    /// 验证指定只读列表实现不支持写入。
    /// </summary>
    /// <typeparam name="T">列表元素的类型。</typeparam>
    /// <param name="values">待验证的只读列表。</param>
    private static void AssertReadOnly<T>(IReadOnlyList<T> values)
    {
        values.ShouldNotBeNull();
        if (values is IList<T> mutable)
            Should.Throw<NotSupportedException>(() => mutable.Add(default(T)));
    }
}
