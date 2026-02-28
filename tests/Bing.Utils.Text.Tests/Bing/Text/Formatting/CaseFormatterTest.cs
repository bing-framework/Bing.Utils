using Shouldly;
using static Bing.Text.CaseFormatter;

namespace Bing.Text;

/// <summary>
/// 测试类：CaseFormatter 大小写格式化器的全部 Style 枚举和工厂属性
/// </summary>
[Trait("TextUT", "CaseFormatter")]
public class CaseFormatterTest
{
    #region LowerUnderscore → 各种 Style

    /// <summary>
    /// 测试目的：LowerUnderscore 分割器配合 LowerCamel style，应转换为小驼峰格式
    /// </summary>
    [Theory]
    [InlineData("hello_world", "helloWorld")]
    [InlineData("my_variable_name", "myVariableName")]
    [InlineData("a_b", "aB")]
    public void LowerUnderscore_ToLowerCamel_ConvertsToLowerCamelCase(string input, string expected)
    {
        // Act
        var result = CaseFormatter.LowerUnderscore.To(Style.LowerCamel, input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：LowerUnderscore 分割器配合 UpperCamel style，应转换为大驼峰（PascalCase）格式
    /// </summary>
    [Theory]
    [InlineData("hello_world", "HelloWorld")]
    [InlineData("my_variable", "MyVariable")]
    public void LowerUnderscore_ToUpperCamel_ConvertsToPascalCase(string input, string expected)
    {
        // Act
        var result = CaseFormatter.LowerUnderscore.To(Style.UpperCamel, input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：LowerUnderscore 分割器配合 LowerHyphen style，应转换为 kebab-case 格式
    /// </summary>
    [Theory]
    [InlineData("hello_world", "hello-world")]
    [InlineData("my_variable_name", "my-variable-name")]
    public void LowerUnderscore_ToLowerHyphen_ConvertsToKebabCase(string input, string expected)
    {
        // Act
        var result = CaseFormatter.LowerUnderscore.To(Style.LowerHyphen, input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：LowerUnderscore 分割器配合 UpperUnderscore style，应转换为全大写下划线格式
    /// </summary>
    [Theory]
    [InlineData("hello_world", "HELLO_WORLD")]
    [InlineData("my_var", "MY_VAR")]
    public void LowerUnderscore_ToUpperUnderscore_ConvertsToUpperSnakeCase(string input, string expected)
    {
        // Act
        var result = CaseFormatter.LowerUnderscore.To(Style.UpperUnderscore, input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：LowerUnderscore 配合 LowerCamelWithWhiteSpace，应转换为首字母大写（除首词）的空格分隔格式
    /// </summary>
    [Fact]
    public void LowerUnderscore_ToLowerCamelWithWhiteSpace_ConvertsCorrectly()
    {
        // Act
        var result = CaseFormatter.LowerUnderscore.To(Style.LowerCamelWithWhiteSpace, "hello_world");

        // Assert
        result.ShouldBe("hello World");
    }

    /// <summary>
    /// 测试目的：LowerUnderscore 配合 UpperCamelWithWhiteSpace，应转换为每词首字母大写的空格分隔格式
    /// </summary>
    [Fact]
    public void LowerUnderscore_ToUpperCamelWithWhiteSpace_ConvertsCorrectly()
    {
        // Act
        var result = CaseFormatter.LowerUnderscore.To(Style.UpperCamelWithWhiteSpace, "hello_world");

        // Assert
        result.ShouldBe("Hello World");
    }

    #endregion

    #region LowerHyphen → 各种 Style

    /// <summary>
    /// 测试目的：LowerHyphen 分割器配合 LowerCamel style，应正确转换 kebab-case 为小驼峰
    /// </summary>
    [Theory]
    [InlineData("hello-world", "helloWorld")]
    [InlineData("my-variable-name", "myVariableName")]
    public void LowerHyphen_ToLowerCamel_ConvertsToLowerCamelCase(string input, string expected)
    {
        // Act
        var result = CaseFormatter.LowerHyphen.To(Style.LowerCamel, input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：LowerHyphen 分割器配合 LowerUnderscore style，应转换为 snake_case 格式
    /// </summary>
    [Fact]
    public void LowerHyphen_ToLowerUnderscore_ConvertsToSnakeCase()
    {
        // Act
        var result = CaseFormatter.LowerHyphen.To(Style.LowerUnderscore, "hello-world");

        // Assert
        result.ShouldBe("hello_world");
    }

    #endregion

    #region Instance (Regex 分割器 [-_ ]+)

    /// <summary>
    /// 测试目的：Instance 分割器支持连字符、下划线、空格混合输入的格式转换
    /// </summary>
    [Theory]
    [InlineData("hello world", Style.UpperCamel, "HelloWorld")]
    [InlineData("hello-world", Style.UpperCamel, "HelloWorld")]
    [InlineData("hello_world", Style.UpperCamel, "HelloWorld")]
    [InlineData("hello world", Style.LowerHyphen, "hello-world")]
    public void Instance_WithMixedSeparators_ConvertsCorrectly(string input, Style style, string expected)
    {
        // Act
        var result = CaseFormatter.Instance.To(style, input);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region Humanizer

    /// <summary>
    /// 测试目的：Humanizer 模式下使用正则 [-_ ]+ 切分单词，转换各种 Style
    /// </summary>
    [Theory]
    [InlineData("Hello World", Style.LowerHyphen, "hello-world")]
    [InlineData("Hello World", Style.LowerUnderscore, "hello_world")]
    [InlineData("Hello World", Style.UpperCamel, "HelloWorld")]
    public void Humanizer_ConvertsVariousStyles(string input, Style style, string expected)
    {
        // Act
        var result = CaseFormatter.Humanizer.To(style, input);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region 边界：单词/空值

    /// <summary>
    /// 测试目的：单词输入（无分隔符）时，格式转换应正确处理单词
    /// </summary>
    [Theory]
    [InlineData("hello", Style.UpperCamel, "Hello")]
    [InlineData("hello", Style.LowerCamel, "hello")]
    [InlineData("hello", Style.LowerHyphen, "hello")]
    [InlineData("hello", Style.UpperUnderscore, "HELLO")]
    public void SingleWord_AllStyles_ConvertsCorrectly(string input, Style style, string expected)
    {
        // Act
        var result = CaseFormatter.LowerUnderscore.To(style, input);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion
}
