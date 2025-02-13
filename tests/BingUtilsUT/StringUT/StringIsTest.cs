using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Is")]
public class StringIsTest
{
    /// <summary>
    /// 测试 - 是否为大写
    /// </summary>
    [Fact]
    public void Test_IsUpper()
    {
        Strings.IsUpper("").ShouldBeTrue();
        Strings.IsUpper("a").ShouldBeFalse();
        Strings.IsUpper("A").ShouldBeTrue();
        Strings.IsUpper("aa").ShouldBeFalse();
        Strings.IsUpper("AA").ShouldBeTrue();
        Strings.IsUpper("aA").ShouldBeFalse();
        Strings.IsUpper("Aa").ShouldBeFalse();
        Strings.IsUpper("a123").ShouldBeFalse();
        Strings.IsUpper("A123").ShouldBeTrue();
        Strings.IsUpper("aa123").ShouldBeFalse();
        Strings.IsUpper("AA123").ShouldBeTrue();
        Strings.IsUpper("aA123").ShouldBeFalse();
        Strings.IsUpper("Aa123").ShouldBeFalse();
        Strings.IsUpper("a°").ShouldBeFalse();
        Strings.IsUpper("A°").ShouldBeTrue();
        Strings.IsUpper("aa°").ShouldBeFalse();
        Strings.IsUpper("AA°").ShouldBeTrue();
        Strings.IsUpper("aA°").ShouldBeFalse();
        Strings.IsUpper("Aa°").ShouldBeFalse();
        Strings.IsUpper("a ").ShouldBeFalse();
        Strings.IsUpper("A ").ShouldBeTrue();
        Strings.IsUpper("a a").ShouldBeFalse();
        Strings.IsUpper("A A").ShouldBeTrue();
        Strings.IsUpper("a A").ShouldBeFalse();
        Strings.IsUpper("A a").ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 是否为小写
    /// </summary>
    [Fact]
    public void Test_IsLower()
    {
        Strings.IsLower("").ShouldBeTrue();
        Strings.IsLower("a").ShouldBeTrue();
        Strings.IsLower("A").ShouldBeFalse();
        Strings.IsLower("aa").ShouldBeTrue();
        Strings.IsLower("AA").ShouldBeFalse();
        Strings.IsLower("aA").ShouldBeFalse();
        Strings.IsLower("Aa").ShouldBeFalse();
        Strings.IsLower("a123").ShouldBeTrue();
        Strings.IsLower("A123").ShouldBeFalse();
        Strings.IsLower("aa123").ShouldBeTrue();
        Strings.IsLower("AA123").ShouldBeFalse();
        Strings.IsLower("aA123").ShouldBeFalse();
        Strings.IsLower("Aa123").ShouldBeFalse();
        Strings.IsLower("a°").ShouldBeTrue();
        Strings.IsLower("A°").ShouldBeFalse();
        Strings.IsLower("aa°").ShouldBeTrue();
        Strings.IsLower("AA°").ShouldBeFalse();
        Strings.IsLower("aA°").ShouldBeFalse();
        Strings.IsLower("Aa°").ShouldBeFalse();
        Strings.IsLower("a ").ShouldBeTrue();
        Strings.IsLower("A ").ShouldBeFalse();
        Strings.IsLower("a a").ShouldBeTrue();
        Strings.IsLower("A A").ShouldBeFalse();
        Strings.IsLower("a A").ShouldBeFalse();
        Strings.IsLower("A a").ShouldBeFalse();
    }

    /// <summary>
    /// 测试 IsChinese(char value) 方法，当字符是中文时应返回 True
    /// </summary>
    [Fact]
    public void IsChineseChar_Should_Return_True_For_Chinese_Character()
    {
        // Arrange
        var character = '你';

        // Act
        var result = Strings.IsChinese(character);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 IsChinese(char value) 方法，当字符不是中文时应返回 False
    /// </summary>
    [Fact]
    public void IsChineseChar_Should_Return_False_For_Non_Chinese_Character()
    {
        // Arrange
        var character = 'A';

        // Act
        var result = Strings.IsChinese(character);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 IsChinese(string text) 方法，当字符串全部由中文字符组成时应返回 True
    /// </summary>
    [Fact]
    public void IsChineseString_Should_Return_True_For_Chinese_String()
    {
        // Arrange
        var text = "你好世界";

        // Act
        var result = Strings.IsChinese(text);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 IsChinese(string text) 方法，当字符串含有非中文字符时应返回 False
    /// </summary>
    [Fact]
    public void IsChineseString_Should_Return_False_For_Non_Chinese_String()
    {
        // Arrange
        var text = "Hello";

        // Act
        var result = Strings.IsChinese(text);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 IsChinese(string text) 方法，当字符串包含中英文混合时应返回 False
    /// </summary>
    [Fact]
    public void IsChineseString_Should_Return_False_For_Mixed_String()
    {
        // Arrange
        var text = "你好World";

        // Act
        var result = Strings.IsChinese(text);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 IsChinese(string text) 方法，当字符串为空时应返回 False
    /// </summary>
    [Fact]
    public void IsChineseString_Should_Return_False_For_Empty_String()
    {
        // Arrange
        var text = string.Empty;

        // Act
        var result = Strings.IsChinese(text);

        // Assert
        Assert.False(result);
    }
}