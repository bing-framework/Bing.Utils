using System;
using System.Threading.Tasks;
using Bing.Text.Chinese;
using Shouldly;
using Xunit;

namespace Bing.Text.Chinese;

/// <summary>
/// 测试固定 OpenCC 词表的简繁中文转换契约。
/// </summary>
[Trait("TextUT", "ChineseConverter")]
public class ChineseConverterTests
{
    /// <summary>
    /// 验证常用简体词语可以转换为固定繁体结果，并支持反向转换。
    /// </summary>
    [Fact]
    public void CommonWords_ConvertBetweenSimplifiedAndTraditional()
    {
        ChineseConverter.ToTraditional("汉字软件").ShouldBe("漢字軟件");
        ChineseConverter.ToSimplified("漢字軟件").ShouldBe("汉字软件");
        ChineseConverter.ToTraditional("中国计算机").ShouldBe("中國計算機");
        ChineseConverter.ToSimplified("中國計算機").ShouldBe("中国计算机");
    }

    /// <summary>
    /// 验证词组匹配优先于单字映射，并保留词表首个候选结果。
    /// </summary>
    [Fact]
    public void PhraseMapping_PrefersLongestPhraseAndFirstCharacterCandidate()
    {
        ChineseConverter.ToTraditional("干").ShouldBe("幹");
        ChineseConverter.ToTraditional("干净").ShouldBe("乾淨");
        ChineseConverter.ToTraditional("发型").ShouldBe("髮型");
        ChineseConverter.ToSimplified("乾").ShouldBe("干");
        ChineseConverter.ToSimplified("乾坤").ShouldBe("乾坤");
    }

    /// <summary>
    /// 验证非中文、Emoji 和孤立代理项在转换过程中保持原样。
    /// </summary>
    [Fact]
    public void MixedUnicode_PreservesNonChineseEmojiAndIsolatedSurrogates()
    {
        var text = "A汉😀\uD83D|\uDE00字B";

        ChineseConverter.ToTraditional(text).ShouldBe("A漢😀\uD83D|\uDE00字B");
        ChineseConverter.ToSimplified("A漢😀\uD83D|\uDE00字B")
            .ShouldBe(text);
    }

    /// <summary>
    /// 验证 null 和空文本保持原值。
    /// </summary>
    [Fact]
    public void NullAndEmptyText_PreserveInput()
    {
        ChineseConverter.ToTraditional(null).ShouldBeNull();
        ChineseConverter.ToSimplified(null).ShouldBeNull();
        ChineseConverter.ToTraditional(string.Empty).ShouldBe(string.Empty);
        ChineseConverter.ToSimplified(string.Empty).ShouldBe(string.Empty);
    }

    /// <summary>
    /// 验证两个方向的首次词表加载支持并发调用且结果稳定。
    /// </summary>
    [Fact]
    public void FirstUse_ConcurrentConversionsRemainStable()
    {
        Parallel.For(0, 32, _ =>
        {
            ChineseConverter.ToTraditional("干净发型汉字").ShouldBe("乾淨髮型漢字");
            ChineseConverter.ToSimplified("乾淨髮型漢字").ShouldBe("干净发型汉字");
        });
    }
}
