using System;
using Bing.Text.Pinyin;
using Shouldly;
using Xunit;

namespace Bing.Text.Pinyin;

/// <summary>
/// 测试 <see cref="PinyinUtil" /> 的拼音转换与边界契约。
/// </summary>
[Trait("TextUT", "Pinyin")]
public class PinyinUtilTests
{
    /// <summary>
    /// 测试目的：全拼转换应支持连续中文、分隔符和空输入。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <param name="separator">音节分隔符。</param>
    /// <param name="expected">预期拼音。</param>
    [Theory]
    [InlineData(null, "", null)]
    [InlineData("", "", "")]
    [InlineData("中国", "", "ZhongGuo")]
    [InlineData("中国", " ", "Zhong Guo")]
    public void GetPinyin_ConvertsChineseSyllables(string text, string separator, string expected)
    {
        PinyinUtil.GetPinyin(text, separator).ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：拼音首字母转换应返回小写结果并支持分隔符。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <param name="separator">音节分隔符。</param>
    /// <param name="expected">预期首字母。</param>
    [Theory]
    [InlineData("中国", "", "zg")]
    [InlineData("中国", "-", "z-g")]
    public void GetInitials_ConvertsChineseInitials(string text, string separator, string expected)
    {
        PinyinUtil.GetInitials(text, separator).ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：非中文内容应保持原样，分隔符仅插入相邻的已转换中文音节之间。
    /// </summary>
    [Fact]
    public void Convert_PreservesMixedTextAndSupplementaryCharacters()
    {
        PinyinUtil.GetPinyin("A中国!😀中文", "-").ShouldBe("AZhong-Guo!😀Zhong-Wen");
        PinyinUtil.GetInitials("A中国!😀中文", "-").ShouldBe("Az-g!😀z-w");
    }

    /// <summary>
    /// 测试目的：无法按现有区码表转换的汉字应保留原字符。
    /// </summary>
    [Fact]
    public void Convert_PreservesUnsupportedChineseCharacters()
    {
        PinyinUtil.GetPinyin("㐀中", "-").ShouldBe("㐀Zhong");
        PinyinUtil.GetInitials("㐀中", "-").ShouldBe("㐀z");
    }

    /// <summary>
    /// 测试目的：分隔符为 null 时应始终抛出参数异常。
    /// </summary>
    [Fact]
    public void Convert_ThrowsWhenSeparatorIsNull()
    {
        Should.Throw<ArgumentNullException>(() => PinyinUtil.GetPinyin(null, null));
        Should.Throw<ArgumentNullException>(() => PinyinUtil.GetInitials(null, null));
    }

    /// <summary>
    /// 测试目的：汉字判断应覆盖 BMP 中的常用、扩展 A 和兼容区字符。
    /// </summary>
    /// <param name="value">待判断字符。</param>
    /// <param name="expected">预期判断结果。</param>
    [Theory]
    [InlineData('中', true)]
    [InlineData('㐀', true)]
    [InlineData('豈', true)]
    [InlineData('A', false)]
    [InlineData('\ud83d', false)]
    public void IsChinese_RecognizesBmpChineseRanges(char value, bool expected)
    {
        PinyinUtil.IsChinese(value).ShouldBe(expected);
    }
}
