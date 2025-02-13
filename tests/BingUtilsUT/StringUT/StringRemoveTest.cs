using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Remove")]
public class StringRemoveTest
{
    /// <summary>
    /// 测试 - 移除字符串
    /// </summary>
    [Fact]
    public void Test_Remove()
    {
        var text = " abcdefghijkl mnopqrstuvwxyz ";

        Strings.RemoveChars(text, 'a', 'b', 'z').ShouldBe(" cdefghijkl mnopqrstuvwxy ");
        Strings.RemoveChars(text, ' ').ShouldBe("abcdefghijklmnopqrstuvwxyz");
        Strings.RemoveWhiteSpace(text).ShouldBe("abcdefghijklmnopqrstuvwxyz");
    }

    /// <summary>
    /// 测试 - 移除所有重复的空格
    /// </summary>
    [Fact]
    public void Test_RemoveDuplicateWhiteSpaces()
    {
        Strings.RemoveDuplicateWhiteSpaces("  ").ShouldBe(" ");
        Strings.RemoveDuplicateWhiteSpaces("  1").ShouldBe(" 1");
        Strings.RemoveDuplicateWhiteSpaces("1  ").ShouldBe("1 ");
        Strings.RemoveDuplicateWhiteSpaces("1  1").ShouldBe("1 1");
        Strings.RemoveDuplicateWhiteSpaces(" 11 ").ShouldBe(" 11 ");
        Strings.RemoveDuplicateWhiteSpaces("    ").ShouldBe(" ");
        Strings.RemoveDuplicateWhiteSpaces("  1  ").ShouldBe(" 1 ");
    }

    /// <summary>
    /// 测试 - 移除所有重复的字符
    /// </summary>
    [Fact]
    public void Test_RemoveDuplicateChar()
    {
        Strings.RemoveDuplicateChar("zz", 'z').ShouldBe("z");
        Strings.RemoveDuplicateChar("zz1", 'z').ShouldBe("z1");
        Strings.RemoveDuplicateChar("1zz", 'z').ShouldBe("1z");
        Strings.RemoveDuplicateChar("1zz1", 'z').ShouldBe("1z1");
        Strings.RemoveDuplicateChar("z11z", 'z').ShouldBe("z11z");
        Strings.RemoveDuplicateChar("zzz", 'z').ShouldBe("z");
        Strings.RemoveDuplicateChar("zz1zz", 'z').ShouldBe("z1z");
    }

    /// <summary>
    /// 测试 - 移除指定位置后的字符串
    /// </summary>
    [Fact]
    public void Test_RemoveSince()
    {
        Strings.RemoveSince("ABCDE", 3).ShouldBe("ABC");
    }

    /// <summary>
    /// 测试 - 移除给定字符串后的字符串
    /// </summary>
    [Fact]
    public void Test_RemoveSince_WithGivenText()
    {
        Strings.RemoveSince("ABCDE", "D").ShouldBe("ABC");
    }

    /// <summary>
    /// 测试 - 移除给定字符串后的字符串 - 忽略大小写
    /// </summary>
    [Fact]
    public void Test_RemoveSinceIgnoreCase_WithGivenText()
    {
        Strings.RemoveSinceIgnoreCase("ABCDE", "d").ShouldBe("ABC");
    }

    /// <summary>
    /// 测试 - 移除起始字符串
    /// </summary>
    [Theory]
    [InlineData(null, null, "")]
    [InlineData(null, "a", "")]
    [InlineData("", "", "")]
    [InlineData("a", "b", "a")]
    [InlineData("ab", "b", "ab")]
    [InlineData("ab", "a", "b")]
    [InlineData("abc", "ab", "c")]
    [InlineData("abc", "Ab", "abc")]
    [InlineData("abc", "abc", "")]
    [InlineData("ab", "abc", "ab")]
    [InlineData("a.cs.cshtml", "a.cs", ".cshtml")]
    [InlineData("\r\na", "\r\n", "a")]
    public void Test_RemoveStart(string value, string removeValue, string result)
    {
        Assert.Equal(result, Strings.RemoveStart(value, removeValue));
    }

    /// <summary>
    /// 测试 - 移除末尾字符串
    /// </summary>
    [Theory]
    [InlineData(null, null, "")]
    [InlineData(null, "a", "")]
    [InlineData("", "", "")]
    [InlineData("a", "b", "a")]
    [InlineData("ab", "a", "ab")]
    [InlineData("ab", "b", "a")]
    [InlineData("abc", "abc", "")]
    [InlineData("bc", "abc", "bc")]
    [InlineData("ab", "abc", "ab")]
    [InlineData("a.cs.cshtml", ".cshtml", "a.cs")]
    [InlineData("a\r\n", "\r\n", "a")]
    public void Test_RemoveEnd(string value, string removeValue, string result)
    {
        Assert.Equal(result, Strings.RemoveEnd(value, removeValue));
    }

    /// <summary>
    /// 测试 - 移除末尾字符串
    /// </summary>
    [Fact]
    public void Test_RemoveEnd_1()
    {
        // null case
        (null as string).RemoveEnd("Test").ShouldBe(string.Empty);

        // empty case
        string.Empty.RemoveEnd("Test").ShouldBe(string.Empty);

        // Simple case
        "MyTestAppService".RemoveEnd("AppService").ShouldBe("MyTest");
        "MyTestAppService".RemoveEnd("Service").ShouldBe("MyTestApp");
    }

    /// <summary>
    /// 测试 - 清理空白字符
    /// </summary>
    [Fact]
    public void Test_CleanBlank()
    {
        var str = "	 你 好　";
        Strings.CleanBlank(str).ShouldBe("你好");
    }
}