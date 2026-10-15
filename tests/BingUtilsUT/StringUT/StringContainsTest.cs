using Bing.Text;
namespace BingUtilsUT.StringUT;

/// <summary>
/// 验证字符串、字符及字符数组的包含判断和忽略大小写行为。
/// </summary>
[Trait("StringUT", "Strings.Contains")]
public class StringContainsTest
{
    #region Contains
    /// <summary>
    /// 验证字符串包含判断的多值、大小写和空值边界行为。
    /// </summary>
    [Fact]
    public void Test_Contains_String()
    {
        // 基本功能测试
        Assert.True(Strings.Contains("Hello World", "Hello"));
        Assert.True(Strings.Contains("Hello World", "World"));
        Assert.False(Strings.Contains("Hello World", "world")); // 大小写敏感
        // 多参数测试
        Assert.True(Strings.Contains("Hello World", "xyz", "World"));
        Assert.False(Strings.Contains("Hello World", "xyz", "abc"));
        // 边界情况
        Assert.False(Strings.Contains(null, "Hello"));
        Assert.False(Strings.Contains("", "Hello"));
        Assert.True(Strings.Contains("Hello", ""));
        Assert.Throws<ArgumentNullException>(() => Strings.Contains("Hello", null));
    }
    /// <summary>
    /// 验证单个字符的包含判断及空值边界行为。
    /// </summary>
    [Fact]
    public void Test_Contains_Char()
    {
        // 基本功能测试
        Assert.True(Strings.Contains("Hello", 'H'));
        Assert.True(Strings.Contains("Hello", 'e'));
        Assert.False(Strings.Contains("Hello", 'x'));
        // 边界情况
        Assert.False(Strings.Contains(null, 'H'));
        Assert.False(Strings.Contains("", 'H'));
    }
    /// <summary>
    /// 验证多个候选字符的包含判断及空值边界行为。
    /// </summary>
    [Fact]
    public void Test_Contains_Chars()
    {
        // 基本功能测试
        Assert.True(Strings.Contains("Hello", 'H', 'e'));
        Assert.True(Strings.Contains("Hello", 'x', 'H'));
        Assert.False(Strings.Contains("Hello", 'x', 'y', 'z'));
        // 边界情况
        Assert.False(Strings.Contains(null, 'H', 'e'));
        Assert.False(Strings.Contains("", 'H', 'e'));
        Assert.True(Strings.Contains("Hello", 'H', null));
        Assert.True(Strings.Contains("Hello", 'H', new char[0]));
    }
    /// <summary>
    /// 验证忽略大小写的字符串包含判断及空值边界行为。
    /// </summary>
    [Fact]
    public void Test_ContainsIgnoreCase_String()
    {
        // 基本功能测试
        Assert.True(Strings.ContainsIgnoreCase("Hello World", "hello"));
        Assert.True(Strings.ContainsIgnoreCase("Hello World", "WORLD"));
        // 多参数测试
        Assert.True(Strings.ContainsIgnoreCase("Hello World", "xyz", "world"));
        Assert.False(Strings.ContainsIgnoreCase("Hello World", "xyz", "abc"));
        // 边界情况
        Assert.False(Strings.ContainsIgnoreCase(null, "Hello"));
        Assert.False(Strings.ContainsIgnoreCase("", "Hello"));
        Assert.True(Strings.ContainsIgnoreCase("Hello", ""));
        Assert.Throws<ArgumentNullException>(() => Strings.ContainsIgnoreCase("Hello", null));
    }
    /// <summary>
    /// 验证忽略大小写的单个字符包含判断。
    /// </summary>
    [Fact]
    public void Test_ContainsIgnoreCase_Char()
    {
        // 基本功能测试
        Assert.True(Strings.ContainsIgnoreCase("Hello", 'h'));
        Assert.True(Strings.ContainsIgnoreCase("Hello", 'E'));
        Assert.False(Strings.ContainsIgnoreCase("Hello", 'x'));
        // 边界情况
        Assert.False(Strings.ContainsIgnoreCase(null, 'H'));
        Assert.False(Strings.ContainsIgnoreCase("", 'H'));
    }
    /// <summary>
    /// 验证忽略大小写的多个候选字符包含判断。
    /// </summary>
    [Fact]
    public void Test_ContainsIgnoreCase_Chars()
    {
        // 基本功能测试
        Assert.True(Strings.ContainsIgnoreCase("Hello", 'h', 'e'));
        Assert.True(Strings.ContainsIgnoreCase("Hello", 'x', 'H'));
        Assert.False(Strings.ContainsIgnoreCase("Hello", 'x', 'y', 'z'));
        // 边界情况
        Assert.False(Strings.ContainsIgnoreCase(null, 'H', 'e'));
        Assert.False(Strings.ContainsIgnoreCase("", 'H', 'e'));
    }
    /// <summary>
    /// 验证字符串数组候选项遵循指定的大小写匹配方式。
    /// </summary>
    [Fact]
    public void Test_Contains_StringArray_IgnoreCase()
    {
        // 忽略大小写
        Assert.True(Strings.Contains("Hello World", new[] { "hello", "xyz" }, IgnoreCase.True));
        Assert.True(Strings.Contains("Hello World", new[] { "xyz", "WORLD" }, IgnoreCase.True));
        Assert.False(Strings.Contains("Hello World", new[] { "xyz", "abc" }, IgnoreCase.True));
        // 区分大小写
        Assert.False(Strings.Contains("Hello World", new[] { "hello", "xyz" }, IgnoreCase.False));
        Assert.True(Strings.Contains("Hello World", new[] { "Hello", "xyz" }, IgnoreCase.False));
        Assert.False(Strings.Contains("Hello World", new[] { "xyz", "abc" }, IgnoreCase.False));
        // 边界情况
        Assert.False(Strings.Contains(null, new[] { "Hello" }, IgnoreCase.True));
        Assert.False(Strings.Contains("Hello", (string[])null, IgnoreCase.True));
        Assert.False(Strings.Contains("Hello", new string[0], IgnoreCase.True));
    }
    /// <summary>
    /// 验证单个字符候选项遵循指定的大小写匹配方式。
    /// </summary>
    [Fact]
    public void Test_Contains_Char_IgnoreCase()
    {
        // 忽略大小写
        Assert.True(Strings.Contains("Hello", 'h', IgnoreCase.True));
        Assert.True(Strings.Contains("Hello", 'E', IgnoreCase.True));
        Assert.False(Strings.Contains("Hello", 'x', IgnoreCase.True));
        // 区分大小写
        Assert.False(Strings.Contains("Hello", 'h', IgnoreCase.False));
        Assert.True(Strings.Contains("Hello", 'H', IgnoreCase.False));
        Assert.False(Strings.Contains("Hello", 'x', IgnoreCase.False));
        // 边界情况
        Assert.False(Strings.Contains(null, 'H', IgnoreCase.True));
        Assert.False(Strings.Contains("", 'H', IgnoreCase.True));
    }
    /// <summary>
    /// 验证字符数组候选项遵循指定的大小写匹配方式。
    /// </summary>
    [Fact]
    public void Test_Contains_CharArray_IgnoreCase()
    {
        // 忽略大小写
        Assert.True(Strings.Contains("Hello", new[] { 'h', 'e' }, IgnoreCase.True));
        Assert.True(Strings.Contains("Hello", new[] { 'x', 'H' }, IgnoreCase.True));
        Assert.False(Strings.Contains("Hello", new[] { 'x', 'y', 'z' }, IgnoreCase.True));
        // 区分大小写
        Assert.True(Strings.Contains("Hello", new[] { 'h', 'e' }, IgnoreCase.False));
        Assert.True(Strings.Contains("Hello", new[] { 'H', 'e' }, IgnoreCase.False));
        Assert.False(Strings.Contains("Hello", new[] { 'x', 'y', 'z' }, IgnoreCase.False));
        // 边界情况
        Assert.False(Strings.Contains(null, new[] { 'H', 'e' }, IgnoreCase.True));
        Assert.False(Strings.Contains("", new[] { 'H', 'e' }, IgnoreCase.True));
        Assert.False(Strings.Contains("Hello", (char[])null, IgnoreCase.True));
        Assert.False(Strings.Contains("Hello", new char[0], IgnoreCase.True));
    }
    /// <summary>
    /// 验证较长字符串中的子串和字符包含判断结果。
    /// </summary>
    [Fact]
    public void Test_Performance()
    {
        // 创建一个较长的字符串
        var longText = new string('a', 1000) + "target" + new string('b', 1000);
        // Contains 性能
        Assert.True(Strings.Contains(longText, "target"));
        Assert.True(Strings.Contains(longText, 'a', 't'));
        // ContainsIgnoreCase 性能
        Assert.True(Strings.ContainsIgnoreCase(longText, "TARGET"));
        Assert.True(Strings.ContainsIgnoreCase(longText, 'T', 'A'));
    }
    #endregion
    #region MatchEmoji
    /// <summary>
    /// 验证包含 Emoji 的字符串会被识别为匹配。
    /// </summary>
    [Fact]
    public void Test_MatchEmoji_WithEmoji()
    {
        // 基本功能测试
        Assert.True(Strings.MatchEmoji("Hello, world! 😊"));
        Assert.True(Strings.MatchEmoji("😊👍🎉"));
        Assert.False(Strings.MatchEmoji("Hello, world!"));
        // 边界情况
        Assert.False(Strings.MatchEmoji(""));
        Assert.False(Strings.MatchEmoji("     "));
    }
    /// <summary>
    /// 验证不含 Emoji 的普通文本不会匹配。
    /// </summary>
    [Fact]
    public void Test_MatchEmoji_WithoutEmoji()
    {
        var input = "Hello, world!";
        var result = Strings.MatchEmoji(input);
        Assert.False(result);
    }
    /// <summary>
    /// 验证空字符串不会被识别为包含 Emoji。
    /// </summary>
    [Fact]
    public void Test_MatchEmoji_EmptyString()
    {
        var input = "";
        var result = Strings.MatchEmoji(input);
        Assert.False(result);
    }
    /// <summary>
    /// 验证仅含空格的字符串不会被识别为包含 Emoji。
    /// </summary>
    [Fact]
    public void Test_MatchEmoji_StringWithSpaces()
    {
        var input = "     ";
        var result = Strings.MatchEmoji(input);
        Assert.False(result);
    }
    /// <summary>
    /// 验证包含多个 Emoji 的字符串会被识别为匹配。
    /// </summary>
    [Fact]
    public void Test_MatchEmoji_MultipleEmojis()
    {
        var input = "😊👍🎉";
        var result = Strings.MatchEmoji(input);
        Assert.True(result);
    }
    #endregion
}
