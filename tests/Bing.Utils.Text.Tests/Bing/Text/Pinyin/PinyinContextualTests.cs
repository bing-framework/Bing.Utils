using System;
using Bing.Text.Pinyin;
using Shouldly;
using Xunit;

namespace Bing.Text.Pinyin;

/// <summary>
/// 测试上下文拼音和带声调拼音的词组、多音字及输入边界契约。
/// </summary>
[Trait("TextUT", "Pinyin")]
public class PinyinContextualTests
{
    /// <summary>
    /// 验证多音字词组按上下文选择读音并优先使用词组结果。
    /// </summary>
    [Fact]
    public void ContextualPinyin_UsesLongestPhraseForPolyphonicWords()
    {
        PinyinUtil.GetPinyinWithTone("重庆银行", " ")
            .ShouldBe("Chóng Qìng Yín Háng");
        PinyinUtil.GetContextualPinyin("重庆银行", " ")
            .ShouldBe("Chong Qing Yin Hang");
    }

    /// <summary>
    /// 验证普通汉字可以生成稳定的带声调和无声调拼音。
    /// </summary>
    [Fact]
    public void ContextualPinyin_ConvertsOrdinaryChineseText()
    {
        PinyinUtil.GetPinyinWithTone("中国工具", " ")
            .ShouldBe("Zhōng Guó Gōng Jù");
        PinyinUtil.GetContextualPinyin("中国工具", " ")
            .ShouldBe("Zhong Guo Gong Ju");
    }

    /// <summary>
    /// 验证词组读音优先于单字首选读音。
    /// </summary>
    [Fact]
    public void ContextualPinyin_PrefersPhraseReadingOverCharacterReading()
    {
        PinyinUtil.GetPinyinWithTone("一").ShouldBe("Yī");
        PinyinUtil.GetPinyinWithTone("一个", " ").ShouldBe("Yí Gè");
        PinyinUtil.GetContextualPinyin("一个", " ").ShouldBe("Yi Ge");
    }

    /// <summary>
    /// 验证无调格式保留 ü 且能转换补充平面汉字。
    /// </summary>
    [Fact]
    public void ContextualPinyin_HandlesUmlautAndSupplementaryHanzi()
    {
        PinyinUtil.GetPinyinWithTone("绿𠀀", " ").ShouldBe("Lǜ Hē");
        PinyinUtil.GetContextualPinyin("绿𠀀", " ").ShouldBe("Lü He");
        PinyinUtil.GetPinyin("𠀀").ShouldBe("𠀀");
    }

    /// <summary>
    /// 验证非中文、Emoji 和孤立代理项保持原样且不错误连接分隔符。
    /// </summary>
    [Fact]
    public void ContextualPinyin_PreservesMixedTextEmojiAndIsolatedSurrogates()
    {
        var text = "A中国!😀\uD83D|\uDE00中文";

        PinyinUtil.GetPinyinWithTone(text, " ")
            .ShouldBe("AZhōng Guó!😀\uD83D|\uDE00Zhōng Wén");
        PinyinUtil.GetContextualPinyin(text, " ")
            .ShouldBe("AZhong Guo!😀\uD83D|\uDE00Zhong Wen");
    }

    /// <summary>
    /// 验证空文本返回空结果，null 文本保持 null 结果。
    /// </summary>
    [Fact]
    public void ContextualPinyin_PreservesNullAndEmptyText()
    {
        PinyinUtil.GetPinyinWithTone(null).ShouldBeNull();
        PinyinUtil.GetContextualPinyin(null).ShouldBeNull();
        PinyinUtil.GetPinyinWithTone(string.Empty).ShouldBe(string.Empty);
        PinyinUtil.GetContextualPinyin(string.Empty).ShouldBe(string.Empty);
    }

    /// <summary>
    /// 验证 null 分隔符会被两个新入口拒绝。
    /// </summary>
    [Fact]
    public void ContextualPinyin_RejectsNullSeparator()
    {
        Should.Throw<ArgumentNullException>(() => PinyinUtil.GetPinyinWithTone("中国", null));
        Should.Throw<ArgumentNullException>(() => PinyinUtil.GetContextualPinyin("中国", null));
        Should.Throw<ArgumentNullException>(() => PinyinUtil.GetPinyinWithTone(null, null));
        Should.Throw<ArgumentNullException>(() => PinyinUtil.GetContextualPinyin(null, null));
    }

    /// <summary>
    /// 验证重复调用不会改变上下文词库或转换结果。
    /// </summary>
    [Fact]
    public void ContextualPinyin_RepeatedCallsRemainStable()
    {
        const string text = "重庆银行中国工具";
        const string toneExpected = "Chóng Qìng Yín Háng Zhōng Guó Gōng Jù";
        const string contextualExpected = "Chong Qing Yin Hang Zhong Guo Gong Ju";

        for (var index = 0; index < 100; index++)
        {
            PinyinUtil.GetPinyinWithTone(text, " ").ShouldBe(toneExpected);
            PinyinUtil.GetContextualPinyin(text, " ").ShouldBe(contextualExpected);
        }
    }
}
