using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Basic")]
public class StringsTest
{
    /// <summary>
    /// 测试 - 合并字符串 - 字符集
    /// </summary>
    [Fact]
    public void Test_Merge_For_AllChars()
    {
        var chars = new List<char> {'a', 'b', 'c'};
        Strings.Merge(chars).ShouldBe("abc");
    }

    /// <summary>
    /// 测试 - 合并字符串 - 字符串集合
    /// </summary>
    [Fact]
    public void Test_Merge_For_Strings()
    {
        var strings = new List<string> {"00", "11", "22"};
        Strings.Merge("AA", strings.ToArray()).ShouldBe("AA001122");
    }

    /// <summary>
    /// 测试 - 合并字符串 - 字符串+字符串数组
    /// </summary>
    [Fact]
    public void Test_Merge_For_StringAndStrings()
    {
        Strings.Merge("AA", "00", "11", "22").ShouldBe("AA001122");
    }

    /// <summary>
    /// 测试 - 合并字符串 - 字符串+字符数组
    /// </summary>
    [Fact]
    public void Test_Merge_For_StringAndChars()
    {
        Strings.Merge("AA", '0', '0', '1', '1', '2', '2').ShouldBe("AA001122");
    }

    /// <summary>
    /// 测试 - 相等判断，忽略大小写 - 2个字符串
    /// </summary>
    [Fact]
    public void Test_EqualsIgnoreCase()
    {
        "AAA".EqualsIgnoreCase("aaa").ShouldBeTrue();
        "aaa".EqualsIgnoreCase("AAA").ShouldBeTrue();
        "AaA".EqualsIgnoreCase("aAa").ShouldBeTrue();
        "".EqualsIgnoreCase("").ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 相等判断，忽略大小写 - 多个字符串
    /// </summary>
    [Fact]
    public void Test_EqualsToAnyIgnoreCase()
    {
        "AAA".EqualsToAnyIgnoreCase("a", "aa", "aaa").ShouldBeTrue();
        "aaa".EqualsToAnyIgnoreCase("b", "a", "bb", "AA", "AAA").ShouldBeTrue();
        "ZZZ".EqualsToAnyIgnoreCase().ShouldBeFalse();
        "ZZZ".EqualsToAnyIgnoreCase(null).ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 返回是否包含字母
    /// </summary>
    [Fact]
    public void Test_HasLetters()
    {
        Strings.HasLetters("").ShouldBeFalse();
        Strings.HasLetters("1234567890").ShouldBeFalse();
        Strings.HasLetters("1234567890a").ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 至少包含指定数量的字母
    /// </summary>
    [Fact]
    public void Test_HasLettersAtLeast()
    {
        Strings.HasLettersAtLeast("1234567890", 0).ShouldBeFalse();
        Strings.HasLettersAtLeast("1234567890", -1).ShouldBeFalse();
        Strings.HasLettersAtLeast("1234567890a", 0).ShouldBeTrue();
        Strings.HasLettersAtLeast("1234567890a", 1).ShouldBeTrue();
        Strings.HasLettersAtLeast("1234567890a", 2).ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 重复指定次数的字符
    /// </summary>
    [Fact]
    public void Test_Repeat()
    {
        Strings.Repeat("ABC", -1).ShouldBeEmpty();
        Strings.Repeat("ABC", 0).ShouldBeEmpty();
        Strings.Repeat("ABC", 1).ShouldBe("ABC");
        Strings.Repeat("ABC", 2).ShouldBe("ABCABC");
    }

    /// <summary>
    /// 测试 - 从左向右截取字符串
    /// </summary>
    [Fact]
    public void Test_Left()
    {
        Strings.Left("ABCDEFG", 0).ShouldBeEmpty();
        Strings.Left("ABCDEFG", 1).ShouldBe("A");
        Strings.Left("ABCDEFG", 2).ShouldBe("AB");
        Strings.Left("ABCDEFG", 3).ShouldBe("ABC");
        Strings.Left("ABCDEFG", 4).ShouldBe("ABCD");
        Strings.Left("ABCDEFG", 5).ShouldBe("ABCDE");
        Strings.Left("ABCDEFG", 6).ShouldBe("ABCDEF");
        Strings.Left("ABCDEFG", 7).ShouldBe("ABCDEFG");
        Strings.Left("ABCDEFG", 8).ShouldBe("ABCDEFG");
    }

    /// <summary>
    /// 测试 - 从右向左截取字符串
    /// </summary>
    [Fact]
    public void Test_Right()
    {
        Strings.Right("ABCDEFG", 0).ShouldBeEmpty();
        Strings.Right("ABCDEFG", 1).ShouldBe("G");
        Strings.Right("ABCDEFG", 2).ShouldBe("FG");
        Strings.Right("ABCDEFG", 3).ShouldBe("EFG");
        Strings.Right("ABCDEFG", 4).ShouldBe("DEFG");
        Strings.Right("ABCDEFG", 5).ShouldBe("CDEFG");
        Strings.Right("ABCDEFG", 6).ShouldBe("BCDEFG");
        Strings.Right("ABCDEFG", 7).ShouldBe("ABCDEFG");
        Strings.Right("ABCDEFG", 8).ShouldBe("ABCDEFG");
    }

    /// <summary>
    /// 测试 - 获取通用前缀
    /// </summary>
    [Fact]
    public void Test_CommonPrefix()
    {
        var textOne = "AAABBBCCC";
        var textTwo = "AABBCC";

        Strings.CommonPrefix(textOne, textTwo).ShouldBe("AA");
        Strings.CommonPrefix(textOne, textTwo, out var v1).ShouldBe("AA");
        v1.ShouldBe(2);
    }

    /// <summary>
    /// 测试 - 获取通用后缀
    /// </summary>
    [Fact]
    public void Test_CommonSuffix()
    {
        var textOne = "AAABBBCCC";
        var textTwo = "AABBCC";

        Strings.CommonSuffix(textOne, textTwo).ShouldBe("CC");
        Strings.CommonSuffix(textOne, textTwo, out var v1).ShouldBe("CC");
        v1.ShouldBe(2);
    }
}