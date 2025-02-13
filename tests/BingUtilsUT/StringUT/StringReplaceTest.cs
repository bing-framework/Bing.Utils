using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Replace")]
public class StringReplaceTest
{
    /// <summary>
    /// 测试 - 替换文本 - 忽略大小写
    /// </summary>
    [Fact]
    public void Test_ReplaceIgnoreCase()
    {
        Strings.ReplaceIgnoreCase("AABBCC", "a", "_").ShouldBe("__BBCC");
        Strings.ReplaceIgnoreCase("AABBCC", "aa", "__").ShouldBe("__BBCC");
        Strings.ReplaceIgnoreCase("AABBCC", "Aa", "__").ShouldBe("__BBCC");
    }

    /// <summary>
    /// 测试 - 替换文本 - 自定义字符串比较
    /// </summary>
    [Fact]
    public void Test_Replace_WithStringComparison()
    {
        Strings.Replace("AABBCC", "a", "_", StringComparison.OrdinalIgnoreCase).ShouldBe("__BBCC");
        Strings.Replace("AABBCC", "aa", "__", StringComparison.OrdinalIgnoreCase).ShouldBe("__BBCC");
        Strings.Replace("AABBCC", "Aa", "__", StringComparison.OrdinalIgnoreCase).ShouldBe("__BBCC");
        Strings.Replace("AABBCC", "a", "_", StringComparison.Ordinal).ShouldBe("AABBCC");
        Strings.Replace("AABBCC", "aa", "__", StringComparison.Ordinal).ShouldBe("AABBCC");
        Strings.Replace("AABBCC", "Aa", "__", StringComparison.Ordinal).ShouldBe("AABBCC");
    }

    /// <summary>
    /// 测试 - 替换文本 - 仅替换完成单词
    /// </summary>
    [Fact]
    public void Test_ReplaceOnlyWholePhrase()
    {
        var text = "AA BB CC";
        Strings.ReplaceOnlyWholePhrase(text, "AA", "DD").ShouldBe("DD BB CC");
        Strings.ReplaceOnlyWholePhrase(text, "BB", "DD").ShouldBe("AA DD CC");
        Strings.ReplaceOnlyWholePhrase(text, "A", "DD").ShouldBe("AA BB CC");
        Strings.ReplaceOnlyWholePhrase(text, "B", "DD").ShouldBe("AA BB CC");

        text = "AABBCC";
        Strings.ReplaceOnlyWholePhrase(text, "AA", "DD").ShouldBe("AABBCC");
        Strings.ReplaceOnlyWholePhrase(text, "BB", "DD").ShouldBe("AABBCC");
        Strings.ReplaceOnlyWholePhrase(text, "A", "DD").ShouldBe("AABBCC");
        Strings.ReplaceOnlyWholePhrase(text, "B", "DD").ShouldBe("AABBCC");
    }

    /// <summary>
    /// 测试 - 替换文本 - 仅替换首个命中的值
    /// </summary>
    [Fact]
    public void Test_ReplaceFirstOccurrence()
    {
        Strings.ReplaceFirstOccurrence("AABBCCAABBCC", "AA", "00").ShouldBe("00BBCCAABBCC");
    }

    /// <summary>
    /// 测试 - 替换文本 - 仅替换最后一个命中的值
    /// </summary>
    [Fact]
    public void Test_ReplaceLastOccurrence()
    {
        Strings.ReplaceLastOccurrence("AABBCCAABBCC", "AA", "00").ShouldBe("AABBCC00BBCC");
    }

    /// <summary>
    /// 测试 - 替换文本 - 仅替换结尾命中的结果，并忽略大小写
    /// </summary>
    [Fact]
    public void Test_ReplaceOnlyAtEndIgnoreCase()
    {
        Strings.ReplaceOnlyAtEndIgnoreCase("AABBCCAABBCCAA", "AA", "00").ShouldBe("AABBCCAABBCC00");
        Strings.ReplaceOnlyAtEndIgnoreCase("AABBCCAABBCCAA", "CC", "00").ShouldBe("AABBCCAABBCCAA");
        Strings.ReplaceOnlyAtEndIgnoreCase("AABBCCAABBCCAA", "aa", "00").ShouldBe("AABBCCAABBCC00");
        Strings.ReplaceOnlyAtEndIgnoreCase("AABBCCAABBCCAA", "cc", "00").ShouldBe("AABBCCAABBCCAA");
        Strings.ReplaceOnlyAtEndIgnoreCase("AABBCCAABBCCAA", "Aa", "00").ShouldBe("AABBCCAABBCC00");
        Strings.ReplaceOnlyAtEndIgnoreCase("AABBCCAABBCCAA", "Cc", "00").ShouldBe("AABBCCAABBCCAA");
    }

    /// <summary>
    /// 测试 - 替换文本 - 递归替换
    /// </summary>
    [Fact]
    public void Test_ReplaceRecursive()
    {
        Strings.ReplaceRecursive("ZZAAAAAABBCC", "AA", "A").ShouldBe("ZZABBCC");
    }

    /// <summary>
    /// 测试 - 替换文本 - 用空格来替换所有命中的字符
    /// </summary>
    [Fact]
    public void Test_ReplaceCharsWithWhiteSpace()
    {
        Strings.ReplaceCharsWithWhiteSpace("AABBCCDD", 'A', 'B').ShouldBe("    CCDD");
    }

    /// <summary>
    /// 测试 - 替换文本 - 用给定的字符来替换数字
    /// </summary>
    [Fact]
    public void Test_ReplaceNumbersWith()
    {
        Strings.ReplaceNumbersWith("AABB1234567890CC", '*').ShouldBe("AABB**********CC");
    }
}