using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Count")]
public class StringCountTest
{
    /// <summary>
    /// 测试 - 计算字符数 - 当文本为英文，返回正确的计数
    /// </summary>
    [Fact]
    public void Test_CharacterCount_ShouldReturnCorrectCount_WhenTextIsEnglish()
    {
        var text = "Hello, World!";
        var result = Strings.CharacterCount(text);
        Assert.Equal(13, result);
    }

    /// <summary>
    /// 测试 - 计算字符数 - 当文本为中文，返回正确的计数
    /// </summary>
    [Fact]
    public void Test_CharacterCount_ShouldReturnCorrectCount_WhenTextIsChinese()
    {
        var text = "你好，世界！";
        var result = Strings.CharacterCount(text);
        Assert.Equal(6, result);
    }

    /// <summary>
    /// 测试 - 计算字符数 - 当文本为空时，应返回零
    /// </summary>
    [Fact]
    public void Test_CharacterCount_ShouldReturnZero_WhenTextIsEmpty()
    {
        var text = "";
        var result = Strings.CharacterCount(text);
        Assert.Equal(0, result);
    }

    /// <summary>
    /// 测试 - 计算字符数 - 当文本包含表情符号时，返回正确的计数
    /// </summary>
    [Fact]
    public void Test_CharacterCount_ShouldReturnCorrectCount_WhenTextContainsEmoji()
    {
        var text = "Hello, World! 👋";
        var result = Strings.CharacterCount(text);
        Assert.Equal(15, result);
    }

    /// <summary>
    /// 测试 - 计算字符数 - 当文本包含组合表情符号时，返回正确的计数
    /// </summary>
    [Fact]
    public void Test_CharacterCount_ShouldReturnCorrectCount_WhenTextContainsEmoji_1()
    {
        var text = "🤔1🥳a👨‍👩‍👧‍👦啊";
        var result = Strings.CharacterCount(text);
#if NETCOREAPP3_1
        Assert.Equal(6, result);
#else
        Assert.Equal(6, result);
#endif
        
    }

    /// <summary>
    /// 测试 - 计算字符数 - 当文本为null时，应抛出ArgumentNullException
    /// </summary>
    [Fact]
    public void Test_CharacterCount_ShouldThrowArgumentNullException_WhenTextIsNull()
    {
        string text = null;
        Assert.Throws<ArgumentNullException>(() => Strings.CharacterCount(text));
    }
}