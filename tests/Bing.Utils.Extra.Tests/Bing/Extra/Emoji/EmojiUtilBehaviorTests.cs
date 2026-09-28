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
        EmojiUtil.ToHtmlEntities(textPresentation).ShouldBe(textPresentation);
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
    /// 验证肤色家族查询保留基础项、修饰符顺序和多肤色组合。
    /// </summary>
    [Fact]
    public void SkinToneQueries_KeepFamilyAndModifierInformation()
    {
        EmojiUtil.TryGetSkinTone("👍🏽", out var medium).ShouldBeTrue();
        medium.Emoji.Unicode.ShouldBe("👍🏽");
        medium.BaseEmoji.Unicode.ShouldBe("👍");
        medium.Tones.ShouldBe(new[] { EmojiSkinTone.Medium });
        medium.Variants.Count.ShouldBe(6);
        medium.Variants.ShouldContain(info => info.Unicode == "👍🏽");
        AssertReadOnly(medium.Tones);
        AssertReadOnly(medium.Variants);

        EmojiUtil.GetSkinToneVariants("👍").Count.ShouldBe(6);
        EmojiUtil.GetSkinToneVariants("🖐🏽").ShouldContain(info => info.Unicode == "🖐️");

        EmojiUtil.TryGetSkinTone("🫱🏽‍🫲🏼", out var handshake).ShouldBeTrue();
        handshake.BaseEmoji.ShouldBeNull();
        handshake.Tones.ShouldBe(new[] { EmojiSkinTone.Medium, EmojiSkinTone.MediumLight });
        handshake.Variants.Count.ShouldBe(20);

        EmojiUtil.TryGetSkinTone("🏽", out _).ShouldBeFalse();
        EmojiUtil.GetSkinToneVariants("plain").ShouldBeEmpty();
    }

    /// <summary>
    /// 验证肤色移除只还原存在无肤色基础项的序列。
    /// </summary>
    [Fact]
    public void RemoveSkinTones_PreservesUnsupportedAndExistingContracts()
    {
        EmojiUtil.RemoveSkinTones("A👍🏽B🖐🏽C🫱🏽‍🫲🏼D🏽")
            .ShouldBe("A👍B🖐️C🫱🏽‍🫲🏼D🏽");
        EmojiUtil.RemoveSkinTones(null).ShouldBeNull();
        EmojiUtil.RemoveSkinTones(string.Empty).ShouldBe(string.Empty);

        EmojiUtil.Normalize("👍🏽").ShouldBe("👍🏽");
        EmojiUtil.ToAlias("👍🏽").ShouldBe("👍🏽");
        EmojiUtil.GetSkinToneVariants(null).ShouldBeEmpty();
        EmojiUtil.GetSkinToneVariants(string.Empty).ShouldBeEmpty();
        EmojiUtil.TryGetSkinTone(null, out var nullInfo).ShouldBeFalse();
        nullInfo.ShouldBeNull();
    }

    /// <summary>
    /// 验证按单一肤色等级筛选时排除多修饰符组合和独立肤色组件。
    /// </summary>
    [Fact]
    public void GetBySkinTone_ReturnsOnlySingleToneSequences()
    {
        var tones = new[]
        {
            EmojiSkinTone.Light,
            EmojiSkinTone.MediumLight,
            EmojiSkinTone.Medium,
            EmojiSkinTone.MediumDark,
            EmojiSkinTone.Dark
        };

        foreach (var tone in tones)
        {
            var values = EmojiUtil.GetBySkinTone(tone);

            values.ShouldNotBeEmpty();
            AssertReadOnly(values);
            foreach (var value in values)
            {
                EmojiUtil.TryGetSkinTone(value.Unicode, out var info).ShouldBeTrue();
                info.Tones.Count.ShouldBe(1);
                info.Tones[0].ShouldBe(tone);
            }
        }

        var medium = EmojiUtil.GetBySkinTone(EmojiSkinTone.Medium);
        medium.ShouldContain(info => info.Unicode == "👍🏽");
        medium.ShouldNotContain(info => info.Unicode == "🫱🏽‍🫲🏼");
        medium.ShouldNotContain(info => info.Unicode == "🏽");

        Should.Throw<ArgumentOutOfRangeException>(() => EmojiUtil.GetBySkinTone((EmojiSkinTone)0));
        Should.Throw<ArgumentOutOfRangeException>(() => EmojiUtil.GetBySkinTone((EmojiSkinTone)6));
    }

    /// <summary>
    /// 验证单肤色序列可以应用目标等级并保留不可安全映射的文本。
    /// </summary>
    [Fact]
    public void ApplySkinTone_ChangesSingleToneFamiliesOnly()
    {
        EmojiUtil.ApplySkinTone("A👍B👩‍💻C👍🏽D🤝🏽E🫱🏽‍🫲🏼F🏽G", EmojiSkinTone.Dark)
            .ShouldBe("A👍🏿B👩🏿‍💻C👍🏿D🤝🏿E🫱🏽‍🫲🏼F🏽G");
        EmojiUtil.ApplySkinTone("👍🏽", EmojiSkinTone.Light).ShouldBe("👍🏻");
        EmojiUtil.ApplySkinTone("❤", EmojiSkinTone.Medium).ShouldBe("❤");
        EmojiUtil.ApplySkinTone(null, EmojiSkinTone.Medium).ShouldBeNull();
        EmojiUtil.ApplySkinTone(string.Empty, EmojiSkinTone.Medium).ShouldBe(string.Empty);

        Should.Throw<ArgumentOutOfRangeException>(() => EmojiUtil.ApplySkinTone(null, (EmojiSkinTone)0));
        Should.Throw<ArgumentOutOfRangeException>(() => EmojiUtil.ApplySkinTone(string.Empty, (EmojiSkinTone)6));
    }

    /// <summary>
    /// 验证多肤色序列按调用方提供的顺序应用官方变体。
    /// </summary>
    [Fact]
    public void ApplySkinTones_UsesExactOrderedVariants()
    {
        EmojiUtil.ApplySkinTones("A👍B👍🏽C🫱🏻‍🫲🏿D🫱🏽‍🫲🏼E🫱🏻‍🫲🏿F🏽G", new[]
            { EmojiSkinTone.Medium, EmojiSkinTone.Dark })
            .ShouldBe("A👍B👍🏽C🫱🏽‍🫲🏿D🫱🏽‍🫲🏿E🫱🏽‍🫲🏿F🏽G");
        EmojiUtil.ApplySkinTones("🫱🏻‍🫲🏿", new[]
            { EmojiSkinTone.Dark, EmojiSkinTone.Medium }).ShouldBe("🫱🏿‍🫲🏽");
        EmojiUtil.ApplySkinTones("🫱🏻‍🫲🏿", new[] { EmojiSkinTone.Medium }).ShouldBe("🫱🏻‍🫲🏿");
        EmojiUtil.ApplySkinTones("A👍B👍🏽", new[] { EmojiSkinTone.Dark }).ShouldBe("A👍🏿B👍🏿");
        EmojiUtil.ApplySkinTones(null, new[] { EmojiSkinTone.Medium }).ShouldBeNull();
        EmojiUtil.ApplySkinTones(string.Empty, new[] { EmojiSkinTone.Medium }).ShouldBe(string.Empty);

        Should.Throw<ArgumentNullException>(() => EmojiUtil.ApplySkinTones("😀", null));
        Should.Throw<ArgumentException>(() => EmojiUtil.ApplySkinTones("😀", new EmojiSkinTone[0]));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            EmojiUtil.ApplySkinTones(null, new[] { EmojiSkinTone.Medium, (EmojiSkinTone)6 }));
    }

    /// <summary>
    /// 验证普通 Emoji、肤色、ZWJ 和键帽序列可以转换为 HTML 实体并还原。
    /// </summary>
    [Fact]
    public void HtmlEntities_RoundTrip_PreservesComplexEmojiSequences()
    {
        var text = "A😀👍🏽👩‍💻1️⃣B";

        var hexadecimalEntities = EmojiUtil.ToHtmlEntities(text);

        hexadecimalEntities.ShouldNotBe(text);
        hexadecimalEntities.ShouldContain("&#x");
        EmojiUtil.ToHtmlEntities("😀👍🏽").ShouldBe("&#x1F600;&#x1F44D;&#x1F3FD;");
        EmojiUtil.FromHtmlEntities(hexadecimalEntities).ShouldBe(text);
    }

    /// <summary>
    /// 验证十进制和十六进制 HTML 数字实体可以转换为 Emoji。
    /// </summary>
    [Fact]
    public void FromHtmlEntities_SupportsDecimalAndHexadecimalEntities()
    {
        EmojiUtil.FromHtmlEntities("A&#128512;B").ShouldBe("A😀B");
        EmojiUtil.FromHtmlEntities("A&#x1F600;B").ShouldBe("A😀B");
        EmojiUtil.FromHtmlEntities("A&#X1f600;B").ShouldBe("A😀B");
    }

    /// <summary>
    /// 验证未知、非法和超出 Unicode 范围的实体保持原样。
    /// </summary>
    [Fact]
    public void FromHtmlEntities_PreservesUnknownAndInvalidEntities()
    {
        var text = "&unknown; &#; &#x; &#xZZZZ; &#x110000; &#99999999;";

        EmojiUtil.FromHtmlEntities(text).ShouldBe(text);
        EmojiUtil.FromHtmlEntities("&#x2764;").ShouldBe("❤️");
        EmojiUtil.FromHtmlEntities("&#x2764;&#xFE0F;").ShouldBe("❤️");
        EmojiUtil.FromHtmlEntities("&#x2764;&#xFE0E;").ShouldBe("&#x2764;&#xFE0E;");
    }

    /// <summary>
    /// 验证按分组、子分组和 Emoji 版本筛选元数据，并返回只读结果。
    /// </summary>
    [Fact]
    public void GroupSubgroupAndVersionQueries_ReturnMatchingReadOnlyResults()
    {
        var byGroup = EmojiUtil.GetByGroup("Smileys & Emotion");
        var bySubgroup = EmojiUtil.GetBySubgroup("face-smiling");
        var byVersion = EmojiUtil.GetByVersion("1.0");

        byGroup.ShouldNotBeEmpty();
        byGroup.ShouldAllBe(info => info.Group == "Smileys & Emotion");
        bySubgroup.ShouldNotBeEmpty();
        bySubgroup.ShouldAllBe(info => info.Subgroup == "face-smiling");
        byVersion.ShouldNotBeEmpty();
        byVersion.ShouldAllBe(info => info.Version == "1.0");

        AssertReadOnly(byGroup);
        AssertReadOnly(bySubgroup);
        AssertReadOnly(byVersion);
    }

    /// <summary>
    /// 验证英文、中文名称、标签和别名搜索返回匹配元数据，并对未知关键词返回空结果。
    /// </summary>
    [Fact]
    public void Search_MatchesMetadata_AndReturnsReadOnlyResults()
    {
        var byName = EmojiUtil.Search("grinning face");
        var byAlias = EmojiUtil.Search("grinning");
        var byTag = EmojiUtil.Search("happy");
        var byChinese = EmojiUtil.Search("笑脸");

        byName.ShouldNotBeEmpty();
        byName.ShouldContain(info => info.Name.IndexOf("grinning face", StringComparison.OrdinalIgnoreCase) >= 0);
        byAlias.ShouldNotBeEmpty();
        byAlias.ShouldContain(info => info.Aliases.Contains("grinning"));
        byTag.ShouldNotBeEmpty();
        byTag.ShouldContain(info => info.Tags.Any(tag => string.Equals(tag, "happy", StringComparison.OrdinalIgnoreCase)));
        byChinese.ShouldNotBeEmpty();
        byChinese.ShouldContain(info => info.Unicode == "😀");
        EmojiUtil.Search("not-an-emoji-query").ShouldBeEmpty();

        AssertReadOnly(byName);
        AssertReadOnly(byAlias);
        AssertReadOnly(byTag);
        AssertReadOnly(byChinese);
    }

    /// <summary>
    /// 验证内置中文本地化名称、关键词和只读集合。
    /// </summary>
    [Fact]
    public void Localizations_ExposeNamesAndKeywordsAsReadOnlyData()
    {
        EmojiUtil.TryGetByUnicode("😀", out var grinning).ShouldBeTrue();

        var chinese = grinning.Localizations.Single(localization => localization.Locale == "zh");
        var traditionalChinese = grinning.Localizations.Single(localization => localization.Locale == "zh-Hant");

        chinese.Name.ShouldNotBeNullOrEmpty();
        chinese.Keywords.ShouldContain("笑脸");
        traditionalChinese.Name.ShouldBe("笑臉");
        traditionalChinese.Keywords.ShouldNotBeEmpty();
        grinning.Localizations.Count.ShouldBe(2);
        AssertReadOnly(grinning.Localizations);
        AssertReadOnly(chinese.Keywords);
        AssertReadOnly(traditionalChinese.Keywords);
        EmojiUtil.Search(chinese.Name).ShouldContain(info => info.Unicode == "😀");
    }

    /// <summary>
    /// 验证内置语言枚举、大小写匹配和区域标识不回退。
    /// </summary>
    [Fact]
    public void LocaleQueries_MatchCaseInsensitively_WithoutImplicitFallback()
    {
        var locales = EmojiUtil.GetLocales();
        locales.ShouldBe(new[] { "zh", "zh-Hant" });
        AssertReadOnly(locales);

        EmojiUtil.Search("笑脸", "zh").ShouldContain(info => info.Unicode == "😀");
        EmojiUtil.Search("笑脸", "ZH").ShouldContain(info => info.Unicode == "😀");
        EmojiUtil.Search("笑臉", "zh-Hant").ShouldContain(info => info.Unicode == "😀");
        EmojiUtil.Search("笑臉", "ZH-HANT").ShouldContain(info => info.Unicode == "😀");

        EmojiUtil.TryGetLocalization("😀", "zh", out var chinese).ShouldBeTrue();
        chinese.Locale.ShouldBe("zh");
        chinese.Name.ShouldNotBeNullOrEmpty();
        EmojiUtil.TryGetLocalization("😀", "ZH-HANT", out var traditionalChinese).ShouldBeTrue();
        traditionalChinese.Locale.ShouldBe("zh-Hant");
        traditionalChinese.Name.ShouldNotBeNullOrEmpty();

        EmojiUtil.Search("笑脸", "ja").ShouldBeEmpty();
        EmojiUtil.Search("grinning face").ShouldContain(info => info.Unicode == "😀");
        EmojiUtil.Search("grinning face", "en").ShouldBeEmpty();
        EmojiUtil.Search("笑顔", "ja-JP").ShouldBeEmpty();
        EmojiUtil.TryGetLocalization("😀", "zh-TW", out var regionalFallback).ShouldBeFalse();
        regionalFallback.ShouldBeNull();
        EmojiUtil.TryGetLocalization("😀", "missing", out var unknownLocale).ShouldBeFalse();
        unknownLocale.ShouldBeNull();
    }

    /// <summary>
    /// 验证自定义目录只识别选入的表情并保留兼容序列。
    /// </summary>
    [Fact]
    public void CustomCatalog_RestrictsMatchingAndPreservesMetadata()
    {
        EmojiUtil.TryGetByUnicode("😀", out var grinning).ShouldBeTrue();
        EmojiUtil.TryGetByUnicode("👍🏽", out var thumbsUp).ShouldBeTrue();

        var catalog = EmojiUtil.CreateCatalog(new[] { grinning, thumbsUp });

        catalog.Items.Count.ShouldBe(2);
        catalog.IsEmoji("😀").ShouldBeTrue();
        catalog.IsEmoji("😃").ShouldBeFalse();
        catalog.ContainsEmoji("A😀中👍🏽B").ShouldBeTrue();
        catalog.Count("A😀中👍🏽B").ShouldBe(2);
        catalog.FindAll("A😀中👍🏽B").Select(match => match.Value).ShouldBe(new[] { "😀", "👍🏽" });
        catalog.TryGetByUnicode("👍🏽", out var matchedThumbs).ShouldBeTrue();
        matchedThumbs.ShouldBeSameAs(thumbsUp);
        catalog.TryGetByAlias(":grinning:", out var matchedGrinning).ShouldBeTrue();
        matchedGrinning.ShouldBeSameAs(grinning);
        catalog.Search("grinning").ShouldContain(info => info.Unicode == "😀");
        catalog.Search("笑脸", "zh").ShouldContain(info => info.Unicode == "😀");
        catalog.GetByTag("happy").ShouldContain(info => info.Unicode == "😀");
        catalog.Replace("😀😃👍🏽", match => "[" + match.Emoji.Name + "]")
            .ShouldBe("[grinning face]😃[" + thumbsUp.Name + "]");
        AssertReadOnly(catalog.Items);
        AssertReadOnly(grinning.Variants);
        EmojiUtil.IsEmoji("😃").ShouldBeTrue();
    }

    /// <summary>
    /// 验证自定义目录拒绝空源和重复元数据。
    /// </summary>
    [Fact]
    public void CustomCatalog_RejectsNullAndDuplicateEntries()
    {
        Should.Throw<ArgumentNullException>(() => EmojiUtil.CreateCatalog(null));
        EmojiUtil.TryGetByUnicode("😀", out var grinning).ShouldBeTrue();
        Should.Throw<ArgumentException>(() => EmojiUtil.CreateCatalog(new[] { grinning, grinning }));

        var empty = EmojiUtil.CreateCatalog(Array.Empty<EmojiInfo>());
        empty.Items.ShouldBeEmpty();
        empty.FindAll("😀").ShouldBeEmpty();
        empty.Search("grinning").ShouldBeEmpty();
    }

    /// <summary>
    /// 验证 Emoji 标签元数据、大小写不敏感精确筛选和只读结果。
    /// </summary>
    [Fact]
    public void Tags_AndTagQueries_ReturnExactReadOnlyResults()
    {
        EmojiUtil.TryGetByAlias("grinning", out var grinning).ShouldBeTrue();
        grinning.Tags.ShouldContain("smile");
        AssertReadOnly(grinning.Tags);

        var byTag = EmojiUtil.GetByTag("SMILE");

        byTag.ShouldNotBeEmpty();
        byTag.ShouldContain(info => info.Unicode == "😀");
        byTag.ShouldAllBe(info => info.Tags.Any(tag => string.Equals(tag, "smile", StringComparison.OrdinalIgnoreCase)));
        AssertReadOnly(byTag);

        EmojiUtil.GetByTag(null).ShouldBeEmpty();
        EmojiUtil.GetByTag(string.Empty).ShouldBeEmpty();
        EmojiUtil.GetByTag("tag-that-does-not-exist").ShouldBeEmpty();
    }

    /// <summary>
    /// 验证兼容形式可以规范化为目录中的 Unicode 形式。
    /// </summary>
    [Fact]
    public void Normalize_ReplacesKnownVariants_AndPreservesTextStyle()
    {
        EmojiUtil.Normalize("A❤B❤️C\u2764\uFE0E").ShouldBe("A❤️B❤️C\u2764\uFE0E");
        EmojiUtil.Normalize(null).ShouldBeNull();
        EmojiUtil.Normalize(string.Empty).ShouldBe(string.Empty);
        EmojiUtil.Normalize("plain text").ShouldBe("plain text");
    }

    /// <summary>
    /// 验证分组、版本和标签枚举按目录顺序去重并保持只读。
    /// </summary>
    [Fact]
    public void MetadataValueEnumerations_ReturnDistinctReadOnlyValues()
    {
        var groups = EmojiUtil.GetGroups();
        var subgroups = EmojiUtil.GetSubgroups();
        var versions = EmojiUtil.GetVersions();
        var tags = EmojiUtil.GetTags();

        groups.ShouldContain("Smileys & Emotion");
        subgroups.ShouldContain("face-smiling");
        versions.ShouldContain("1.0");
        tags.ShouldContain("smile");
        groups.Count.ShouldBe(groups.Distinct(StringComparer.Ordinal).Count());
        subgroups.Count.ShouldBe(subgroups.Distinct(StringComparer.Ordinal).Count());
        versions.Count.ShouldBe(versions.Distinct(StringComparer.Ordinal).Count());
        tags.Count.ShouldBe(tags.Distinct(StringComparer.Ordinal).Count());

        AssertReadOnly(groups);
        AssertReadOnly(subgroups);
        AssertReadOnly(versions);
        AssertReadOnly(tags);
    }

    /// <summary>
    /// 验证按条件进行固定字符串替换、回调替换和删除时只处理原始匹配。
    /// </summary>
    [Fact]
    public void ConditionalReplacement_SupportsFixedCallbackAndRemovalWithoutRecursion()
    {
        var text = "A😀B😃C";
        Func<EmojiMatch, bool> firstOnly = match => match.Value == "😀";

        EmojiUtil.RemoveWhere(text, firstOnly).ShouldBe("AB😃C");
        EmojiUtil.ReplaceWhere(text, firstOnly, "[grinning]").ShouldBe("A[grinning]B😃C");

        var calls = new List<EmojiMatch>();
        var result = EmojiUtil.ReplaceWhere("😀😃", _ => true, match =>
        {
            calls.Add(match);
            return match.Value == "😀" ? "😀😃" : null;
        });

        result.ShouldBe("😀😃");
        calls.Count.ShouldBe(2);
        calls.Select(match => match.Value).ShouldBe(new[] { "😀", "😃" });
    }

    /// <summary>
    /// 验证条件替换的谓词、回调异常原样传播，并拒绝空参数。
    /// </summary>
    [Fact]
    public void ConditionalReplacement_PropagatesExceptionsAndRejectsNullArguments()
    {
        var predicateException = new InvalidOperationException("predicate failed");
        Should.Throw<InvalidOperationException>(() => EmojiUtil.RemoveWhere("😀", _ => throw predicateException))
            .ShouldBeSameAs(predicateException);

        var callbackException = new InvalidOperationException("callback failed");
        Should.Throw<InvalidOperationException>(() => EmojiUtil.ReplaceWhere("😀", _ => true, _ => throw callbackException))
            .ShouldBeSameAs(callbackException);

        Func<EmojiMatch, bool> nullPredicate = null;
        Func<EmojiMatch, string> nullCallback = null;

        Should.Throw<ArgumentNullException>(() => EmojiUtil.RemoveWhere("😀", nullPredicate));
        Should.Throw<ArgumentNullException>(() => EmojiUtil.RemoveWhere(null, nullPredicate));
        Should.Throw<ArgumentNullException>(() => EmojiUtil.ReplaceWhere("😀", nullPredicate, "replacement"));
        Should.Throw<ArgumentNullException>(() => EmojiUtil.ReplaceWhere("😀", _ => true, (string)null));
        Should.Throw<ArgumentNullException>(() => EmojiUtil.ReplaceWhere("😀", _ => true, nullCallback));
        Should.Throw<ArgumentNullException>(() => EmojiUtil.ReplaceWhere(string.Empty, nullPredicate, "replacement"));
        Should.Throw<ArgumentNullException>(() => EmojiUtil.ReplaceWhere(null, _ => true, nullCallback));
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
        EmojiUtil.ToHtmlEntities(component).ShouldBe(component);
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
        EmojiUtil.ToHtmlEntities(null).ShouldBeNull();
        EmojiUtil.FromHtmlEntities(null).ShouldBeNull();
        EmojiUtil.ToHtmlEntities(string.Empty).ShouldBe(string.Empty);
        EmojiUtil.FromHtmlEntities(string.Empty).ShouldBe(string.Empty);
        EmojiUtil.Search(null).ShouldBeEmpty();
        EmojiUtil.Search(string.Empty).ShouldBeEmpty();
        EmojiUtil.Search(null, "zh").ShouldBeEmpty();
        EmojiUtil.Search("笑脸", null).ShouldBeEmpty();
        EmojiUtil.Search("笑脸", string.Empty).ShouldBeEmpty();
        EmojiUtil.Search("笑顔", "ja-JP").ShouldBeEmpty();
        EmojiUtil.GetByGroup(null).ShouldBeEmpty();
        EmojiUtil.GetBySubgroup(string.Empty).ShouldBeEmpty();
        EmojiUtil.GetByVersion("missing").ShouldBeEmpty();
        EmojiUtil.TryGetByAlias(null, out var alias).ShouldBeFalse();
        EmojiUtil.TryGetByUnicode(null, out var unicode).ShouldBeFalse();
        EmojiUtil.TryGetLocalization(null, "zh", out var nullUnicodeLocalization).ShouldBeFalse();
        EmojiUtil.TryGetLocalization("😀", null, out var nullLocaleLocalization).ShouldBeFalse();
        alias.ShouldBeNull();
        unicode.ShouldBeNull();
        nullUnicodeLocalization.ShouldBeNull();
        nullLocaleLocalization.ShouldBeNull();

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
