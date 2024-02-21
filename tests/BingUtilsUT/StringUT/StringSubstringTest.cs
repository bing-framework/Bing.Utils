using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Substring")]
public class StringSubstringTest
{
    /// <summary>
    /// 测试 - 截断字符串 - 指定范围
    /// </summary>
    [Fact]
    public void Test_Substring()
    {
        var str = "abcderghigh";
        Strings.Substring(str, 3, 3).ShouldBe(string.Empty);
        Strings.Substring(str, 2, 3).ShouldBe("c");
        Strings.Substring(str, 2, -3).ShouldBe("cdergh");
        Strings.Substring(str, -5, str.Length).ShouldBe("ghigh");
    }

    /// <summary>
    /// 测试 - 截断字符串 - 指定长度
    /// </summary>
    [Fact]
    public void Test_Substring_With_Length()
    {
        var str = "A5E6005700000000000000000000000000000000000000090D0100000000000001003830";
        Strings.SubstringWithLength(str, -2, 2).ShouldBe("38");
    }

    /// <summary>
    /// 测试 - 截取字符串 - 分隔字符串之前部分
    /// </summary>
    [Fact]
    public void Test_SubstringBefore_1()
    {
        var str = "abcderghigh";
        Strings.SubstringBefore(null, "*", false).ShouldBe(null);
        Strings.SubstringBefore("", "*", false).ShouldBe("");
        Strings.SubstringBefore(str, "a", false).ShouldBe("");
        Strings.SubstringBefore(str, "b", false).ShouldBe("a");
        Strings.SubstringBefore(str, "c", false).ShouldBe("ab");
        Strings.SubstringBefore(str, "d", false).ShouldBe("abc");
        Strings.SubstringBefore(str, "", false).ShouldBe("");
        Strings.SubstringBefore(str, null, false).ShouldBe(str);

        // 找不到返回原字符串
        Strings.SubstringBefore(str, "k", false).ShouldBe(str);
        Strings.SubstringBefore(str, "k", true).ShouldBe(str);
    }

    /// <summary>
    /// 测试 - 截取字符串 - 分隔字符之前部分
    /// </summary>
    [Fact]
    public void Test_SubstringBefore_2()
    {
        var str = "abcderghigh";
        Strings.SubstringBefore(null, '*', false).ShouldBe(null);
        Strings.SubstringBefore("", '*', false).ShouldBe("");
        Strings.SubstringBefore(str, 'a', false).ShouldBe("");
        Strings.SubstringBefore(str, 'b', false).ShouldBe("a");
        Strings.SubstringBefore(str, 'c', false).ShouldBe("ab");
        Strings.SubstringBefore(str, 'd', false).ShouldBe("abc");

        // 找不到返回原字符串
        Strings.SubstringBefore(str, 'k', false).ShouldBe(str);
        Strings.SubstringBefore(str, 'k', true).ShouldBe(str);
    }

    /// <summary>
    /// 测试 - 截取字符串 - 分隔字符串之后部分
    /// </summary>
    [Fact]
    public void Test_SubstringAfter_1()
    {
        var str = "abcderghigh";
        Strings.SubstringAfter(null, "*", false).ShouldBe(null);
        Strings.SubstringAfter("", "*", false).ShouldBe("");
        Strings.SubstringAfter(str, null, false).ShouldBe("");
        Strings.SubstringAfter(str, "a", false).ShouldBe("bcderghigh");
        Strings.SubstringAfter(str, "b", false).ShouldBe("cderghigh");
        Strings.SubstringAfter(str, "d", false).ShouldBe("erghigh");
        Strings.SubstringAfter(str, "h", false).ShouldBe("igh");
        Strings.SubstringAfter(str, "h", true).ShouldBe("");
        Strings.SubstringAfter(str, "", true).ShouldBe("");

        // 找不到返回原字符串
        Strings.SubstringAfter(str, "k", false).ShouldBe("");
        Strings.SubstringAfter(str, "k", true).ShouldBe("");
    }

    /// <summary>
    /// 测试 - 截取字符串 - 分隔字符之后部分
    /// </summary>
    [Fact]
    public void Test_SubstringAfter_2()
    {
        var str = "abcderghigh";
        Strings.SubstringAfter(null, '*', false).ShouldBe(null);
        Strings.SubstringAfter("", '*', false).ShouldBe("");
        Strings.SubstringAfter(str, 'a', false).ShouldBe("bcderghigh");
        Strings.SubstringAfter(str, 'b', false).ShouldBe("cderghigh");
        Strings.SubstringAfter(str, 'd', false).ShouldBe("erghigh");
        Strings.SubstringAfter(str, 'h', false).ShouldBe("igh");
        Strings.SubstringAfter(str, 'h', true).ShouldBe("");

        // 找不到返回原字符串
        Strings.SubstringAfter(str, 'k', false).ShouldBe("");
        Strings.SubstringAfter(str, 'k', true).ShouldBe("");
    }

    /// <summary>
    /// 测试 - 截取字符串 - 指定字符串中间部分
    /// </summary>
    [Fact]
    public void Test_SubstringBetween_1()
    {
        Strings.SubstringBetween("wx[b]yz", "[", "]").ShouldBe("b");
        Strings.SubstringBetween(null, "[", "]").ShouldBe(string.Empty);
        Strings.SubstringBetween("abc", null, "]").ShouldBe(string.Empty);
        Strings.SubstringBetween("abc", "[", null).ShouldBe(string.Empty);
        Strings.SubstringBetween("", "", "").ShouldBe(string.Empty);
        Strings.SubstringBetween("", "", "]").ShouldBe(string.Empty);
        Strings.SubstringBetween("", "[", "]").ShouldBe(string.Empty);
        Strings.SubstringBetween("wang", "", "").ShouldBe(string.Empty);
        Strings.SubstringBetween("wang", "w", "g").ShouldBe("an");
        Strings.SubstringBetween("wangwang", "w", "g").ShouldBe("an");
    }

    /// <summary>
    /// 测试 - 截取字符串 - 指定字符串中间部分
    /// </summary>
    [Fact]
    public void Test_SubstringBetween_2()
    {
        Strings.SubstringBetween(null, ",").ShouldBe(string.Empty);
        Strings.SubstringBetween("","").ShouldBe(string.Empty);
        Strings.SubstringBetween("","tag").ShouldBe(string.Empty);
        Strings.SubstringBetween("tagabctag",null).ShouldBe(string.Empty);
        Strings.SubstringBetween("tagabctag","").ShouldBe(string.Empty);
        Strings.SubstringBetween("tagabctag","tag").ShouldBe("abc");
    }
}