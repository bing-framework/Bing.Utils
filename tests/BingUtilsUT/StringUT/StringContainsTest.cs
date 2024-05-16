using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Contains")]
public class StringContainsTest
{
    #region MatchEmoji

    /// <summary>
    /// 测试 - 匹配字符串中是否包含 Emoji 表情 - 包含 Emoji 表情
    /// </summary>
    [Fact]
    public void Test_MatchEmoji_WithEmoji()
    {
        var input = "Hello, world! 😊";
        var result = Strings.MatchEmoji(input);
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - 匹配字符串中是否包含 Emoji 表情 - 排除 Emoji 表情
    /// </summary>
    [Fact]
    public void Test_MatchEmoji_WithoutEmoji()
    {
        var input = "Hello, world!";
        var result = Strings.MatchEmoji(input);
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - 匹配字符串中是否包含 Emoji 表情 - 空字符串
    /// </summary>
    [Fact]
    public void Test_MatchEmoji_EmptyString()
    {
        var input = "";
        var result = Strings.MatchEmoji(input);
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - 匹配字符串中是否包含 Emoji 表情 - 全空格
    /// </summary>
    [Fact]
    public void Test_MatchEmoji_StringWithSpaces()
    {
        var input = "     ";
        var result = Strings.MatchEmoji(input);
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - 匹配字符串中是否包含 Emoji 表情 - 包含多个 Emoji 表情
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