using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Remove")]
public class StringRemoveTest
{
    /// <summary>
    /// 测试 - 移除子字符串
    /// </summary>
    [Fact]
    public void Test_Remove()
    {
        // 基本功能测试
        Strings.Remove("Hello World", "World").ShouldBe("Hello ");
        Strings.Remove("Hello Hello World", "Hello ").ShouldBe("World");
        Strings.Remove("Hello World Hello", "Hello").ShouldBe(" World ");

        // 忽略大小写
        Strings.Remove("Hello World", "world", IgnoreCase.True).ShouldBe("Hello ");
        Strings.Remove("Hello World", "HELLO", IgnoreCase.True).ShouldBe(" World");

        // 大小写敏感
        Strings.Remove("Hello World", "world", IgnoreCase.False).ShouldBe("Hello World");

        // 边界情况
        Strings.Remove(null, "World").ShouldBeNull();
        Strings.Remove("Hello World", null).ShouldBe("Hello World");
        Strings.Remove("Hello World", "").ShouldBe("Hello World");
        Strings.Remove("", "World").ShouldBe("");
    }

    /// <summary>
    /// 测试 - 移除字符串
    /// </summary>
    [Fact]
    public void Test_Remove_1()
    {
        var text = " abcdefghijkl mnopqrstuvwxyz ";

        Strings.RemoveChars(text, 'a', 'b', 'z').ShouldBe(" cdefghijkl mnopqrstuvwxy ");
        Strings.RemoveChars(text, ' ').ShouldBe("abcdefghijklmnopqrstuvwxyz");
        Strings.RemoveWhiteSpace(text).ShouldBe("abcdefghijklmnopqrstuvwxyz");
    }

    /// <summary>
    /// 测试 - 移除空格
    /// </summary>
    [Fact]
    public void Test_RemoveWhiteSpace()
    {
        // 基本功能测试
        Strings.RemoveWhiteSpace("Hello World").ShouldBe("HelloWorld");
        Strings.RemoveWhiteSpace("  Hello  World  ").ShouldBe("HelloWorld");

        // 边界情况
        Strings.RemoveWhiteSpace(null).ShouldBeNull();
        Strings.RemoveWhiteSpace("").ShouldBe("");
        Strings.RemoveWhiteSpace(" ").ShouldBe("");
    }

    /// <summary>
    /// 测试 - 移除字符
    /// </summary>
    [Fact]
    public void Test_RemoveChars()
    {
        // 基本功能测试
        Strings.RemoveChars("Hello World", 'e', 'o').ShouldBe("Hll Wrld");
        Strings.RemoveChars("Hello World", ' ').ShouldBe("HelloWorld");

        // 多次出现
        Strings.RemoveChars("Mississippi", 's', 'i').ShouldBe("Mpp");

        // 边界情况
        Strings.RemoveChars(null, 'e').ShouldBeNull();
        Strings.RemoveChars("Hello", null).ShouldBe("Hello");
        Strings.RemoveChars("Hello", new char[0]).ShouldBe("Hello");
        Strings.RemoveChars("", 'e').ShouldBe("");

        // 特殊字符
        Strings.RemoveChars("Hello\nWorld", '\n').ShouldBe("HelloWorld");
        Strings.RemoveChars("Hello\tWorld", '\t').ShouldBe("HelloWorld");
    }

    /// <summary>
    /// 测试 - 移除重复空白字符
    /// </summary>
    [Fact]
    public void Test_RemoveDuplicateWhiteSpaces()
    {
        // 基本功能测试
        Strings.RemoveDuplicateWhiteSpaces("Hello  World").ShouldBe("Hello World");
        Strings.RemoveDuplicateWhiteSpaces("Hello   World").ShouldBe("Hello World");

        // 各种空白字符
        Strings.RemoveDuplicateWhiteSpaces("Hello\t\nWorld").ShouldBe("Hello World");
        Strings.RemoveDuplicateWhiteSpaces("Hello \t \n World").ShouldBe("Hello World");

        // 边界情况
        Strings.RemoveDuplicateWhiteSpaces(null).ShouldBeNull();
        Strings.RemoveDuplicateWhiteSpaces("").ShouldBe("");
        Strings.RemoveDuplicateWhiteSpaces(" ").ShouldBe(" ");
        Strings.RemoveDuplicateWhiteSpaces("   ").ShouldBe(" ");
    }

    /// <summary>
    /// 测试 - 移除所有重复的空格
    /// </summary>
    [Fact]
    public void Test_RemoveDuplicateWhiteSpaces_1()
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
    /// 测试 - 移除重复字符
    /// </summary>
    [Fact]
    public void Test_RemoveDuplicateChar()
    {
        // 基本功能测试
        Strings.RemoveDuplicateChar("Hello", 'l').ShouldBe("Helo");
        Strings.RemoveDuplicateChar("aabbc", 'a').ShouldBe("abbc");

        // 区分大小写
        Strings.RemoveDuplicateChar("aAabBb", 'a', IgnoreCase.False).ShouldBe("aAabBb");
        Strings.RemoveDuplicateChar("aAabBb", 'A', IgnoreCase.False).ShouldBe("aAabBb");

        // 忽略大小写
        Strings.RemoveDuplicateChar("aAabBb", 'a', IgnoreCase.True).ShouldBe("abBb");
        Strings.RemoveDuplicateChar("aAabBb", 'A', IgnoreCase.True).ShouldBe("abBb");

        // 边界情况
        Strings.RemoveDuplicateChar(null, 'a').ShouldBeNull();
        Strings.RemoveDuplicateChar("", 'a').ShouldBe("");
        Strings.RemoveDuplicateChar("Hello", 'z').ShouldBe("Hello");

        // 复杂场景
        Strings.RemoveDuplicateChar("Mississippi", 's').ShouldBe("Misisippi");
        Strings.RemoveDuplicateChar("Mississippi", 'i').ShouldBe("Mississippi");
    }

    /// <summary>
    /// 测试 - 移除所有重复的字符
    /// </summary>
    [Fact]
    public void Test_RemoveDuplicateChar_1()
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
    /// 测试 - 从索引位置移除字符
    /// </summary>
    [Fact]
    public void Test_RemoveSince_Index()
    {
        // 基本功能测试
        Strings.RemoveSince("Hello World", 5).ShouldBe("Hello");
        Strings.RemoveSince("Hello World", 0).ShouldBe("Hello World");

        // 边界情况
        Strings.RemoveSince(null, 5).ShouldBeNull();
        Strings.RemoveSince("", 5).ShouldBe("");
        Strings.RemoveSince("Hello", -1).ShouldBe("Hello");
        Strings.RemoveSince("Hello", 10).ShouldBe("");
        Strings.RemoveSince("Hello", 5).ShouldBe("");
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
    /// 测试 - 从子字符串位置移除
    /// </summary>
    [Fact]
    public void Test_RemoveSince_String()
    {
        // 基本功能测试
        Strings.RemoveSince("Hello World", "World").ShouldBe("Hello ");
        Strings.RemoveSince("Hello World", "o").ShouldBe("Hell");

        // 边界情况
        Strings.RemoveSince(null, "World").ShouldBeNull();
        Strings.RemoveSince("Hello", null).ShouldBe("Hello");
        Strings.RemoveSince("Hello", "").ShouldBe("Hello");
        Strings.RemoveSince("", "World").ShouldBe("");
        Strings.RemoveSince("Hello", "xyz").ShouldBe("Hello");
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
    /// 测试 - 从子字符串位置移除（忽略大小写）
    /// </summary>
    [Fact]
    public void Test_RemoveSinceIgnoreCase()
    {
        // 基本功能测试
        Strings.RemoveSinceIgnoreCase("Hello World", "world").ShouldBe("Hello ");
        Strings.RemoveSinceIgnoreCase("Hello World", "WORLD").ShouldBe("Hello ");
        Strings.RemoveSinceIgnoreCase("Hello World", "O").ShouldBe("Hell");

        // 边界情况
        Strings.RemoveSinceIgnoreCase(null, "World").ShouldBeNull();
        Strings.RemoveSinceIgnoreCase("Hello", null).ShouldBe("Hello");
        Strings.RemoveSinceIgnoreCase("Hello", "").ShouldBe("Hello");
        Strings.RemoveSinceIgnoreCase("", "World").ShouldBe("");
        Strings.RemoveSinceIgnoreCase("Hello", "xyz").ShouldBe("Hello");
    }

    /// <summary>
    /// 测试 - 从子字符串位置移除（支持忽略大小写选项）
    /// </summary>
    [Fact]
    public void Test_RemoveSince_WithCase()
    {
        // 忽略大小写
        Strings.RemoveSince("Hello World", "world", IgnoreCase.True).ShouldBe("Hello ");
        Strings.RemoveSince("Hello World", "WORLD", IgnoreCase.True).ShouldBe("Hello ");

        // 区分大小写
        Strings.RemoveSince("Hello World", "world", IgnoreCase.False).ShouldBe("Hello World");
        Strings.RemoveSince("Hello World", "World", IgnoreCase.False).ShouldBe("Hello ");

        // 默认行为应与 IgnoreCase.False 相同
        Strings.RemoveSince("Hello World", "world").ShouldBe("Hello World");
        Strings.RemoveSince("Hello World", "World").ShouldBe("Hello ");
    }

    /// <summary>
    /// 测试 - 移除开头字符串
    /// </summary>
    [Fact]
    public void Test_RemoveStart()
    {
        // 基本功能测试
        Strings.RemoveStart("Hello World", "Hello").ShouldBe(" World");
        Strings.RemoveStart("Hello World Hello", "Hello").ShouldBe(" World Hello");

        // 大小写敏感
        Strings.RemoveStart("Hello World", "hello").ShouldBe("Hello World");

        // 边界情况
        Strings.RemoveStart((string)null, "Hello").ShouldBe("");
        Strings.RemoveStart("", "Hello").ShouldBe("");
        Strings.RemoveStart("Hello", null).ShouldBe("Hello");
        Strings.RemoveStart("Hello", "").ShouldBe("Hello");
        Strings.RemoveStart("Hello", "World").ShouldBe("Hello");
        Strings.RemoveStart("Hello", "HelloWorld").ShouldBe("Hello");
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
    public void Test_RemoveStart_1(string value, string removeValue, string result)
    {
        Assert.Equal(result, Strings.RemoveStart(value, removeValue));
    }

    /// <summary>
    /// 测试 - 移除末尾字符串
    /// </summary>
    [Fact]
    public void Test_RemoveEnd()
    {
        // 基本功能测试
        Strings.RemoveEnd("Hello World", "World").ShouldBe("Hello ");
        Strings.RemoveEnd("Hello World Hello", "Hello").ShouldBe("Hello World ");

        // 大小写敏感
        Strings.RemoveEnd("Hello World", "world").ShouldBe("Hello World");

        // 边界情况
        Strings.RemoveEnd((string)null, "World").ShouldBe("");
        Strings.RemoveEnd("", "World").ShouldBe("");
        Strings.RemoveEnd("Hello", null).ShouldBe("Hello");
        Strings.RemoveEnd("Hello", "").ShouldBe("Hello");
        Strings.RemoveEnd("Hello", "World").ShouldBe("Hello");
        Strings.RemoveEnd("Hello", "HelloWorld").ShouldBe("Hello");
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
    public void Test_RemoveEnd_1(string value, string removeValue, string result)
    {
        Assert.Equal(result, Strings.RemoveEnd(value, removeValue));
    }

    /// <summary>
    /// 测试 - 移除末尾字符串
    /// </summary>
    [Fact]
    public void Test_RemoveEnd_2()
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

    /// <summary>
    /// 测试 - 方法间一致性
    /// </summary>
    [Fact]
    public void Test_ConsistencyBetweenMethods()
    {
        // RemoveChars 和 RemoveWhiteSpace 应一致
        var text = "Hello World";
        Strings.RemoveWhiteSpace(text).ShouldBe(Strings.RemoveChars(text, ' '));

        // RemoveSince 和 RemoveSinceIgnoreCase 应一致(当大小写相同时)
        var text2 = "Hello World";
        Strings.RemoveSince(text2, "World").ShouldBe(Strings.RemoveSinceIgnoreCase(text2, "World"));

        // RemoveStart 和 Substring 方法结果应一致
        var text3 = "Hello World";
        var prefix = "Hello";
        Strings.RemoveStart(text3, prefix).ShouldBe(text3.Substring(prefix.Length));

        // RemoveEnd 和 Substring 方法结果应一致
        var text4 = "Hello World";
        var suffix = "World";
        Strings.RemoveEnd(text4, suffix).ShouldBe(text4.Substring(0, text4.LastIndexOf(suffix, StringComparison.Ordinal)));
    }

    /// <summary>
    /// 测试 - 性能表现
    /// </summary>
    [Fact]
    public void Test_Performance()
    {
        // 创建较长的字符串，测试处理性能
        var longText = new string('a', 1000) + new string('b', 1000);

        // RemoveDuplicateChar 性能
        var result1 = Strings.RemoveDuplicateChar(longText, 'a');
        result1.Length.ShouldBe(1001); // 只保留一个'a'加上1000个'b'

        // RemoveSince 性能
        var result2 = Strings.RemoveSince(longText, "b");
        result2.Length.ShouldBe(1000); // 移除所有'b'字符，只保留1000个'a'

        // RemoveDuplicateWhiteSpaces 性能
        var spacedText = string.Join("  ", Enumerable.Repeat("word", 1000));
        var result3 = Strings.RemoveDuplicateWhiteSpaces(spacedText);
        result3.ShouldNotContain("  "); // 不应包含连续空格
    }
}