using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Convert")]
public class StringConvertTest
{
    /// <summary>
    /// 测试 - 转换成全角字符串 - 包含空格
    /// </summary>
    [Fact]
    public void Test_ToSbcCase_WithSpace()
    {
        // Arrange
        var input = "Hello World";
        var expected = "Ｈｅｌｌｏ　Ｗｏｒｌｄ";

        // Act
        var result = Strings.ToSbcCase(input);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 转换成全角字符串 - 排除空格
    /// </summary>
    [Fact]
    public void Test_ToSbcCase_WithoutSpace()
    {
        // Arrange
        var input = "HelloWorld";
        var expected = "ＨｅｌｌｏＷｏｒｌｄ";

        // Act
        var result = Strings.ToSbcCase(input);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 转换成全角字符串 - 包含特殊符号
    /// </summary>
    [Fact]
    public void Test_ToSbcCase_WithSpecialCharacters()
    {
        // Arrange
        var input = "Hello!@#$%^&*()_+-=";
        var expected = "Ｈｅｌｌｏ！＠＃＄％＾＆＊（）＿＋－＝";

        // Act
        var result = Strings.ToSbcCase(input);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 转换成半角字符串 - 包含空格
    /// </summary>
    [Fact]
    public void Test_ToDbcCase_WithSpaces()
    {
        var input = "ａｂｃ　１２３　";
        var expected = "abc 123 ";
        var result = Strings.ToDbcCase(input);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 转换成半角字符串 - 排除空格
    /// </summary>
    [Fact]
    public void Test_ToDbcCase_WithoutSpaces()
    {
        var input = "ａｂｃ１２３";
        var expected = "abc123";
        var result = Strings.ToDbcCase(input);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 转换成半角字符串 - 包含特殊符号
    /// </summary>
    [Fact]
    public void Test_ToDbcCase_WithSpecialCharacters()
    {
        var input = "ａｂｃ！＠＃＄％＾＆＊（）";
        var expected = "abc!@#$%^&*()";
        var result = Strings.ToDbcCase(input);
        Assert.Equal(expected, result);
    }
}