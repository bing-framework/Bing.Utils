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
    /// 测试 - 返回字符串中所包含字母的数量
    /// </summary>
    [Fact]
    public void Test_CountForLetters()
    {
        Strings.CountForLetters("abcd1234").ShouldBe(4);
        Strings.CountForLetters("").ShouldBe(0);
        Strings.CountForLetters("1234").ShouldBe(0);
        Strings.CountForLetters(null).ShouldBe(0);
    }

    /// <summary>
    /// 测试 - 返回字符串中所包含大写字母的数量
    /// </summary>
    [Fact]
    public void Test_CountForLettersUpperCase()
    {
        Strings.CountForLettersUpperCase("abcdABCD1234").ShouldBe(4);
        Strings.CountForLettersUpperCase("").ShouldBe(0);
        Strings.CountForLettersUpperCase("1234").ShouldBe(0);
        Strings.CountForLettersUpperCase(null).ShouldBe(0);
    }

    /// <summary>
    /// 测试 - 返回字符串中所包含小写字母的数量
    /// </summary>
    [Fact]
    public void Test_CountForLettersLowerCase()
    {
        Strings.CountForLettersLowerCase("abcdABCD1234").ShouldBe(4);
        Strings.CountForLettersLowerCase("").ShouldBe(0);
        Strings.CountForLettersLowerCase("1234").ShouldBe(0);
        Strings.CountForLettersLowerCase(null).ShouldBe(0);
    }
}