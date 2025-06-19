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

    #region Filter

    /// <summary>
    /// 测试 - 过滤字符 - 正常情况
    /// </summary>
    [Fact]
    public void Test_FilterByChar_Normal()
    {
        // 过滤字母
        string input1 = "Hello123World";
        var result1 = Strings.FilterByChar(input1, c => char.IsLetter(c)).ToArray();
        Assert.Equal("HelloWorld", new string(result1));

        // 过滤数字
        string input2 = "Hello123World";
        var result2 = Strings.FilterByChar(input2, c => char.IsDigit(c)).ToArray();
        Assert.Equal("123", new string(result2));

        // 过滤大写字母
        string input3 = "HelloWorld";
        var result3 = Strings.FilterByChar(input3, c => char.IsUpper(c)).ToArray();
        Assert.Equal("HW", new string(result3));

        // 过滤小写字母
        string input4 = "HelloWorld";
        var result4 = Strings.FilterByChar(input4, c => char.IsLower(c)).ToArray();
        Assert.Equal("elloorld", new string(result4));

        // 自定义条件
        string input5 = "aeiouAEIOU12345";
        var result5 = Strings.FilterByChar(input5, c => "aeiouAEIOU".Contains(c)).ToArray();
        Assert.Equal("aeiouAEIOU", new string(result5));
    }

    /// <summary>
    /// 测试 - 过滤字符 - 空字符串
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Test_FilterByChar_Empty(string input)
    {
        var result = Strings.FilterByChar(input, c => true).ToArray();
        Assert.Empty(result);
    }

    /// <summary>
    /// 测试 - 过滤字符 - 所有字符都匹配
    /// </summary>
    [Fact]
    public void Test_FilterByChar_AllMatch()
    {
        string input = "Hello123";
        var result = Strings.FilterByChar(input, c => true).ToArray();
        Assert.Equal(input, new string(result));
    }

    /// <summary>
    /// 测试 - 过滤字符 - 所有字符都不匹配
    /// </summary>
    [Fact]
    public void Test_FilterByChar_NoMatch()
    {
        string input = "Hello123";
        var result = Strings.FilterByChar(input, c => false).ToArray();
        Assert.Empty(result);
    }

    /// <summary>
    /// 测试 - 过滤字符 - 空谓词参数
    /// </summary>
    [Fact]
    public void Test_FilterByChar_NullPredicate()
    {
        string input = "Hello123";
        Assert.Throws<ArgumentNullException>(() => Strings.FilterByChar(input, null).ToArray());
    }

    /// <summary>
    /// 测试 - 过滤字符 - 中文字符
    /// </summary>
    [Fact]
    public void Test_FilterByChar_ChineseChars()
    {
        string input = "你好Hello123世界";

        // 过滤中文字符
        var result1 = Strings.FilterByChar(input, c => c >= 0x4E00 && c <= 0x9FFF).ToArray();
        Assert.Equal("你好世界", new string(result1));

        // 过滤非中文字符
        var result2 = Strings.FilterByChar(input, c => !(c >= 0x4E00 && c <= 0x9FFF)).ToArray();
        Assert.Equal("Hello123", new string(result2));
    }

    /// <summary>
    /// 测试 - 过滤字符 - 特殊字符
    /// </summary>
    [Fact]
    public void Test_FilterByChar_SpecialChars()
    {
        string input = "Hello!@#$%^&*()_+World";

        // 过滤特殊字符
        var result = Strings.FilterByChar(input, c => !char.IsLetterOrDigit(c)).ToArray();
        Assert.Equal("!@#$%^&*()_+", new string(result));
    }

    /// <summary>
    /// 测试 - 过滤字符 - 与其他字符串方法组合
    /// </summary>
    [Fact]
    public void Test_FilterByChar_Combination()
    {
        string input = "  Hello  World  ";

        // 过滤非空格字符，然后合并
        var result1 = Strings.Merge(Strings.FilterByChar(input, c => !char.IsWhiteSpace(c)));
        Assert.Equal("HelloWorld", result1);

        // 过滤出数字，转为字符串
        string input2 = "The price is $123.45";
        var result2 = Strings.Merge(Strings.FilterByChar(input2, c => char.IsDigit(c) || c == '.'));
        Assert.Equal("123.45", result2);
    }

    /// <summary>
    /// 测试 - 只过滤字母和数字
    /// </summary>
    [Theory]
    [InlineData("Hello123!@#", "Hello123")]
    [InlineData("测试ABC123", "ABC123")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("!@#$%^", "")]
    public void Test_FilterForNumbersAndLetters(string input, string expected)
    {
        var result = Strings.Merge(Strings.FilterForNumbersAndLetters(input));
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 只过滤数字
    /// </summary>
    [Theory]
    [InlineData("Hello123!@#", "123")]
    [InlineData("测试ABC123", "123")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("!@#$%^", "")]
    public void Test_FilterForNumbers(string input, string expected)
    {
        var result = Strings.Merge(Strings.FilterForNumbers(input));
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 只过滤字母
    /// </summary>
    [Theory]
    [InlineData("Hello123!@#", "Hello")]
    [InlineData("测试ABC123", "ABC")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("!@#$%^", "")]
    public void Test_FilterForLetters(string input, string expected)
    {
        var result = Strings.Merge(Strings.FilterForLetters(input));
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - GetNumbers方法
    /// </summary>
    [Theory]
    [InlineData("Hello123!@#", "123")]
    [InlineData("测试ABC123", "123")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("!@#$%^", "")]
    public void Test_GetNumbers(string input, string expected)
    {
        var result = Strings.GetNumbers(input);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - GetLetters方法
    /// </summary>
    [Theory]
    [InlineData("Hello123!@#", "Hello")]
    [InlineData("测试ABC123", "ABC")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("!@#$%^", "")]
    public void Test_GetLetters(string input, string expected)
    {
        var result = Strings.GetLetters(input);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - GetNumbersAndLetters方法
    /// </summary>
    [Theory]
    [InlineData("Hello123!@#", "Hello123")]
    [InlineData("测试ABC123", "ABC123")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("!@#$%^", "")]
    public void Test_GetNumbersAndLetters(string input, string expected)
    {
        var result = Strings.GetNumbersAndLetters(input);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 字符串扩展方法 Where
    /// </summary>
    [Fact]
    public void Test_StringExtension_Where()
    {
        string input = "Hello123";
        var result = input.Where(c => char.IsLetter(c)).ToArray();
        Assert.Equal("Hello", new string(result));
    }

    #endregion

    #region Repeat

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
    /// 测试 - 重复字符串 - 正常情况
    /// </summary>
    [Theory]
    [InlineData("a", 3, "aaa")]
    [InlineData("abc", 2, "abcabc")]
    [InlineData("测试", 2, "测试测试")]
    [InlineData("abc123", 3, "abc123abc123abc123")]
    public void Test_Repeat_Normal(string input, int times, string expected)
    {
        var result = Strings.Repeat(input, times);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 重复字符串 - 单个字符优化
    /// </summary>
    [Theory]
    [InlineData("a", 5, "aaaaa")]
    [InlineData("#", 3, "###")]
    [InlineData("好", 2, "好好")]
    public void Test_Repeat_SingleChar(string input, int times, string expected)
    {
        var result = Strings.Repeat(input, times);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 重复字符串 - 优化场景 (1-4次)
    /// </summary>
    [Theory]
    [InlineData("abc", 1, "abc")]
    [InlineData("abc", 2, "abcabc")]
    [InlineData("abc", 3, "abcabcabc")]
    [InlineData("abc", 4, "abcabcabcabc")]
    public void Test_Repeat_OptimizedCases(string input, int times, string expected)
    {
        var result = Strings.Repeat(input, times);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 重复字符串 - 边界情况
    /// </summary>
    [Theory]
    [InlineData(null, 3, "")]
    [InlineData("", 3, "")]
    [InlineData("abc", 0, "")]
    [InlineData("abc", -1, "")]
    [InlineData(" ", 5, "     ")]
    public void Test_Repeat_Edge(string input, int times, string expected)
    {
        var result = Strings.Repeat(input, times);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 重复字符串 - 性能测试
    /// </summary>
    [Fact]
    public void Test_Repeat_Performance()
    {
        // 大量重复的性能测试
        const int repeatTimes = 10000;
        const string testString = "test";

        var result = Strings.Repeat(testString, repeatTimes);

        // 确保结果长度正确
        Assert.Equal(testString.Length * repeatTimes, result.Length);

        // 验证结果的前部分和后部分
        Assert.Equal(testString, result.Substring(0, testString.Length));
        Assert.Equal(testString, result.Substring(result.Length - testString.Length));
    }

    /// <summary>
    /// 测试 - 重复字符串 - 扩展方法
    /// </summary>
    [Fact]
    public void Test_Repeat_Extension()
    {
        // 测试扩展方法
        const string testString = "xyz";
        const int repeatTimes = 3;

        var resultFromMethod = Strings.Repeat(testString, repeatTimes);
        var resultFromExtension = testString.Repeat(repeatTimes);

        // 验证扩展方法与静态方法结果一致
        Assert.Equal(resultFromMethod, resultFromExtension);
    }

    #endregion

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