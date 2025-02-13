using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Count")]
public class StringCountTest
{
    /// <summary>
    /// 测试 - 计算字符串所包含的字母数量
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
    /// 测试 - 计算字符串所包含的大写字母数量
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
    /// 测试 - 计算字符串所包含的小写字母数量
    /// </summary>
    [Fact]
    public void Test_CountForLetterLowerCase()
    {
        Strings.CountForLettersLowerCase("abcdABCD1234").ShouldBe(4);
        Strings.CountForLettersLowerCase("").ShouldBe(0);
        Strings.CountForLettersLowerCase("1234").ShouldBe(0);
        Strings.CountForLettersLowerCase(null).ShouldBe(0);
    }

    /// <summary>
    /// 测试 - 计算字符串所包含的数字数量
    /// </summary>
    [Fact]
    public void Test_CountForNumbers()
    {
        Strings.CountForNumbers("abcd1234").ShouldBe(4);
        Strings.CountForNumbers("").ShouldBe(0);
        Strings.CountForNumbers("abcd").ShouldBe(0);
        Strings.CountForNumbers(null).ShouldBe(0);
    }

    /// <summary>
    /// 测试 - 计算给定字符串中有多少个指定的字符
    /// </summary>
    [Fact]
    public void Test_CountOccurrences()
    {
        var text = "AABBCCDDAABBCCDD";
        Strings.CountOccurrences(text,"AA").ShouldBe(2);
        Strings.CountOccurrences(text,"aa").ShouldBe(0);
            
        Strings.CountOccurrences(text,'A').ShouldBe(4);
        Strings.CountOccurrences(text,'a').ShouldBe(0);

        Strings.CountOccurrences(text,"AA", IgnoreCase.False).ShouldBe(2);
        Strings.CountOccurrences(text,"aa", IgnoreCase.False).ShouldBe(0);
            
        Strings.CountOccurrences(text,'A', IgnoreCase.False).ShouldBe(4);
        Strings.CountOccurrences(text,'a', IgnoreCase.False).ShouldBe(0);
            
        Strings.CountOccurrences(text,"AA", IgnoreCase.True).ShouldBe(2);
        Strings.CountOccurrences(text,"aa", IgnoreCase.True).ShouldBe(2);
            
        Strings.CountOccurrences(text,'A', IgnoreCase.True).ShouldBe(4);
        Strings.CountOccurrences(text,'a', IgnoreCase.True).ShouldBe(4);
    }

    /// <summary>
    /// 测试 - 计算给定字符串中有多少个指定的字符 - 忽略大小写
    /// </summary>
    [Fact]
    public void Test_CountOccurrences_IgnoreCase()
    {
        var text = "AABBCCDDAABBCCDD";
        Strings.CountOccurrencesIgnoreCase(text,"AA").ShouldBe(2);
        Strings.CountOccurrencesIgnoreCase(text,"aa").ShouldBe(2);
            
        Strings.CountOccurrencesIgnoreCase(text,'A').ShouldBe(4);
        Strings.CountOccurrencesIgnoreCase(text,'a').ShouldBe(4);
    }

    /// <summary>
    /// 测试 - 计算给定字符串中有多少个指定的子字符串
    /// </summary>
    [Fact]
    public void Test_CountOccurrencesWithCase()
    {
        Assert.Equal(4, Strings.CountOccurrences("Hello Hello", 'L', IgnoreCase.True));
        Assert.Equal(0, Strings.CountOccurrences("Hello Hello", 'L', IgnoreCase.False));
    }

    /// <summary>
    /// 测试 - 比较字符串，获取不相等字符的数量
    /// </summary>
    [Fact]
    public void Test_CountForDiffChars()
    {
        Assert.Equal(1, Strings.CountForDiffChars("Hello", "Hella"));
        Assert.Equal(4, Strings.CountForDiffChars("Hello", "World"));
    }

    /// <summary>
    /// 测试 - 比较字符串，获取不相等字符的数量，忽略大小写
    /// </summary>
    [Fact]
    public void TestCountForDiffCharsIgnoreCase()
    {
        Assert.Equal(0, Strings.CountForDiffCharsIgnoreCase("Hello", "HELLO"));
        Assert.Equal(4, Strings.CountForDiffCharsIgnoreCase("Hello", "WORLD"));
    }

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
        Assert.Equal(12, result);
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

    /// <summary>
    /// 测试 - 计算字符串的字节大小
    /// </summary>
    [Fact]
    public void Test_BytesCount()
    {
        var text = "🤔1🥳a👨‍👩‍👧‍👦啊";
        var result = Strings.BytesCount(text);
        Assert.Equal(38, result);
    }
}