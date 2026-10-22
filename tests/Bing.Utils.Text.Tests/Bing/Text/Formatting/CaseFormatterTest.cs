using Shouldly;
using static Bing.Text.CaseFormatter;

namespace Bing.Text;

/// <summary>
/// 命名风格格式化器的分隔符和大小写转换测试。
/// </summary>
[Trait("TextUT", "CaseFormatter")]
public class CaseFormatterTest
{
    #region LowerUnderscore → 各种 Style

    /// <summary>
    /// 验证下划线命名转换为小驼峰命名。
    /// </summary>
    /// <param name="input">待格式化文本。</param>
    /// <param name="expected">预期格式化文本。</param>
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
    /// 验证下划线命名转换为大驼峰命名。
    /// </summary>
    /// <param name="input">待格式化文本。</param>
    /// <param name="expected">预期格式化文本。</param>
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
    /// 验证下划线命名转换为小写连字符命名。
    /// </summary>
    /// <param name="input">待格式化文本。</param>
    /// <param name="expected">预期格式化文本。</param>
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
    /// 验证下划线命名转换为大写下划线命名。
    /// </summary>
    /// <param name="input">待格式化文本。</param>
    /// <param name="expected">预期格式化文本。</param>
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
    /// 验证LowerUnderscore 配合 LowerCamelWithWhiteSpace，应转换为首字母大写（除首词）的空格分隔格式
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
    /// 验证LowerUnderscore 配合 UpperCamelWithWhiteSpace，应转换为每词首字母大写的空格分隔格式
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
    /// 验证连字符命名转换为小驼峰命名。
    /// </summary>
    /// <param name="input">待格式化文本。</param>
    /// <param name="expected">预期格式化文本。</param>
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
    /// 验证LowerHyphen 分割器配合 LowerUnderscore style，应转换为 snake_case 格式
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
    /// 验证通用格式化器处理空格、连字符和下划线分隔符。
    /// </summary>
    /// <param name="input">待格式化文本。</param>
    /// <param name="style">目标命名风格。</param>
    /// <param name="expected">预期格式化文本。</param>
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
    /// 验证人类可读文本转换为指定命名风格。
    /// </summary>
    /// <param name="input">待格式化文本。</param>
    /// <param name="style">目标命名风格。</param>
    /// <param name="expected">预期格式化文本。</param>
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
    /// 验证无分隔符的单词转换为指定命名风格。
    /// </summary>
    /// <param name="input">待格式化文本。</param>
    /// <param name="style">目标命名风格。</param>
    /// <param name="expected">预期格式化文本。</param>
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

    /// <summary>
    /// 测试目的：其余格式化分支处理 null、空串和空白时，与已有下划线格式契约一致返回空串。
    /// </summary>
    [Theory]
    [InlineData(Style.LowerCamel)]
    [InlineData(Style.LowerCamelWithWhiteSpace)]
    [InlineData(Style.LowerHyphen)]
    [InlineData(Style.UpperCamel)]
    [InlineData(Style.UpperCamelWithWhiteSpace)]
    [InlineData(Style.UpperUnderscore)]
    public void To_WithNullOrEmptySequence_ReturnsEmptyString(Style style)
    {
        var formatter = CaseFormatter.Instance;

        formatter.To(style, null).ShouldBe(string.Empty);
        formatter.To(style, string.Empty).ShouldBe(string.Empty);
        formatter.To(style, " \t\r\n").ShouldBe(string.Empty);
    }

    #endregion
}
