using Bing.Text.Formatting;

namespace Bing.Utils.Tests.Text.Formatting;

/// <summary>
/// 格式化字符串提取器测试
/// </summary>
public class FormattedStringValueExtractorTest
{
    /// <summary>
    /// 测试 - 匹配
    /// </summary>
    [Fact]
    public void Test_Matched()
    {
        Test_Matched_Internal(
            "My name is Andy.",
            "My name is {0}.",
            new NameValue("0", "Andy")
        );

        Test_Matched_Internal(
            "User jxb does not exist.",
            "User {0} does not exist.",
            new NameValue("0", "jxb")
        );

        Test_Matched_Internal(
            "bing.com",
            "{domain}",
            new NameValue("domain", "bing.com")
        );

        Test_Matched_Internal(
            "http://acme.bing.com/gaming/Index.html",
            "http://{TENANCY_NAME}.bing.com/{AREA}/Index.html",
            new NameValue("TENANCY_NAME", "acme"),
            new NameValue("AREA", "gaming")
        );
    }

    /// <summary>
    /// 测试 - 匹配 - 原始值为空
    /// </summary>
    [Fact]
    public void Test_Matched_1()
    {
        var result = FormattedStringValueExtractor.Extract("", "a");
        Assert.False(result.IsMatch);
        Assert.Empty(result.Matches);
    }

    /// <summary>
    /// 测试 - 匹配 - 格式字符串为空
    /// </summary>
    [Fact]
    public void Test_Matched_2()
    {
        var result = FormattedStringValueExtractor.Extract("a", "");
        Assert.False(result.IsMatch);
        Assert.Empty(result.Matches);
    }

    /// <summary>
    /// 测试 - 匹配 - 格式字符串未包含{}
    /// </summary>
    [Fact]
    public void Test_Matched_3()
    {
        var result = FormattedStringValueExtractor.Extract("a", "a");
        Assert.True(result.IsMatch);
        Assert.Empty(result.Matches);
    }

    /// <summary>
    /// 测试 - 匹配 - 格式字符串包含{}
    /// </summary>
    [Fact]
    public void Test_Matched_4()
    {
        Test_Matched_Internal("a", "{value}", new NameValue("value", "a"));
    }

    /// <summary>
    /// 测试 - 匹配 - 值前后有空格
    /// </summary>
    [Fact]
    public void Test_Matched_5()
    {
        Test_Matched_Internal(" a ", "{value}", new NameValue("value", "a"));
    }

    /// <summary>
    /// 测试 - 匹配 - 格式化字符串前后有空格
    /// </summary>
    [Fact]
    public void Test_Matched_6()
    {
        Test_Matched_Internal("a", "    {value}   ", new NameValue("value", "a"));
    }

    /// <summary>
    /// 测试 - 匹配 - 格式字符串左侧有文本
    /// </summary>
    [Fact]
    public void Test_Matched_7()
    {
        Test_Matched_Internal("Hello,World", "Hello,{value}", new NameValue("value", "World"));
    }

    /// <summary>
    /// 测试 - 匹配 - 格式字符串右侧有文本
    /// </summary>
    [Fact]
    public void Test_Matched_8()
    {
        Test_Matched_Internal("Hello,World", "{value},World", new NameValue("value", "Hello"));
    }

    /// <summary>
    /// 测试 - 匹配 - 格式字符串左右侧有文本
    /// </summary>
    [Fact]
    public void Test_Matched_9()
    {
        Test_Matched_Internal("Hello,Bing,World", "Hello,{value},World", new NameValue("value", "Bing"));
    }

    /// <summary>
    /// 测试 - 匹配 - 格式字符串包含两个变量 - 左侧文本
    /// </summary>
    [Fact]
    public void Test_Matched_10()
    {
        Test_Matched_Internal(
            "Hello,Bing,World", 
            "Hello,{a},{b}", 
            new NameValue("a", "Bing"),
            new NameValue("b", "World"));
    }

    /// <summary>
    /// 测试 - 匹配 - 格式字符串包含两个变量 - 右侧文本
    /// </summary>
    [Fact]
    public void Test_Matched_11()
    {
        Test_Matched_Internal(
            "Hello,Bing,World", 
            "{a},{b},World", 
            new NameValue("a", "Hello"),
            new NameValue("b", "Bing"));
    }

    /// <summary>
    /// 测试 - 匹配 - 格式字符串包含两个变量 - 重复文本
    /// </summary>
    [Fact]
    public void Test_Matched_12()
    {
        Test_Matched_Internal(
            "Hello,Bing,Hello,Test,Hello,World", 
            "Hello,{a},Hello,{b},Hello,World", 
            new NameValue("a", "Bing"),
            new NameValue("b", "Test"));
    }

    /// <summary>
    /// 测试 - 匹配 - 没有分隔符
    /// </summary>
    [Fact]
    public void Test_Matched_13()
    {
        Test_Matched_Internal("acababcabcd", "a{b}c{d}", 
            new NameValue("b", "cabab"),
            new NameValue("d", "abcd"));
    }

    /// <summary>
    /// 测试 - 匹配 - 未匹配到变量b
    /// </summary>
    [Fact]
    public void Test_Matched_14()
    {
        Test_Matched_Internal(
            "Hello,Bing,World", 
            "Hello,{a},{b},World", 
            new NameValue("a", "Bing"));
    }

    /// <summary>
    /// 测试 - 不匹配
    /// </summary>
    [Fact]
    public void Test_Not_Matched()
    {
        Test_Not_Matched_Internal(
            "My name is Andy.",
            "My name is Jam."
        );

        Test_Not_Matched_Internal(
            "Role {0} does not exist.",
            "User name {0} is invalid, can only contain letters or digits."
        );

        Test_Not_Matched_Internal(
            "{0} cannot be null or empty.",
            "Incorrect password."
        );

        Test_Not_Matched_Internal(
            "Incorrect password.",
            "{0} cannot be null or empty."
        );
    }

    /// <summary>
    /// 测试 - 是否完全匹配
    /// </summary>
    [Fact]
    public void Test_IsMatch()
    {
        FormattedStringValueExtractor.IsMatch("User jxb does not exist.", "User {0} does not exist.", out var values).ShouldBe(true);
        values[0].ShouldBe("jxb");
    }

    private static void Test_Matched_Internal(string str, string format, params NameValue[] expectedPairs)
    {
        var result = FormattedStringValueExtractor.Extract(str, format);
        result.IsMatch.ShouldBe(true);

        if (expectedPairs == null)
        {
            result.Matches.Count.ShouldBe(0);
            return;
        }

        result.Matches.Count.ShouldBe(expectedPairs.Length);

        for (var i = 0; i < expectedPairs.Length; i++)
        {
            var actualMatch = result.Matches[i];
            var expectedPair = expectedPairs[i];

            actualMatch.Name.ShouldBe(expectedPair.Name);
            actualMatch.Value.ShouldBe(expectedPair.Value);
        }
    }

    private void Test_Not_Matched_Internal(string str, string format)
    {
        var result = FormattedStringValueExtractor.Extract(str, format);
        result.IsMatch.ShouldBe(false);
    }
}