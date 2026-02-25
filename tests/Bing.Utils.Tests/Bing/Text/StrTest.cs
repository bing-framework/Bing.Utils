namespace Bing.Text;
/// <summary>
/// 字符串操作 测试
/// </summary>
public class StrTest
{
    [Theory]
    [InlineData("userName", "UserName")]
    [InlineData("URLValue", "URLValue")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void ToPascalCase_InputValue_ReturnsExpectedResult(string input, string expected)
    {
        var result = Str.ToPascalCase(input);
        result.ShouldBe(expected);
    }
    [Theory]
    [InlineData("UserName", "userName")]
    [InlineData("URLValue", "urlValue")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void ToCamelCase_InputValue_ReturnsExpectedResult(string input, string expected)
    {
        var result = Str.ToCamelCase(input);
        result.ShouldBe(expected);
    }
    [Theory]
    [InlineData("UserName", "user_name")]
    [InlineData("HTTPServerError", "http_server_error")]
    [InlineData("user name", "user_name")]
    public void ToSnakeCase_InputValue_ReturnsExpectedResult(string input, string expected)
    {
        var result = Str.ToSnakeCase(input);
        result.ShouldBe(expected);
    }
    [Theory]
    [InlineData("UserName", "user-name")]
    [InlineData("HTTPServerError", "http-server-error")]
    [InlineData("user name", "user-name")]
    public void ToKebabCase_InputValue_ReturnsExpectedResult(string input, string expected)
    {
        var result = Str.ToKebabCase(input);
        result.ShouldBe(expected);
    }
    [Fact]
    public void Repeat_TimesLessThanOrEqualToZero_ReturnsEmptyString()
    {
        Str.Repeat("ab", 0).ShouldBeEmpty();
        Str.Repeat("ab", -1).ShouldBeEmpty();
        Str.Repeat('a', 0).ShouldBeEmpty();
    }
    /// <summary>
    /// 测试 - 将Unicode转换为字符串
    /// </summary>
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("\\u4e2d\\u56fd", "中国")]
    [InlineData("\\u4F60\\u597D\\u4E16\\u754C", "你好世界")]
    [InlineData("Hello\\u4E16\\u754CWorld", "Hello世界World")]
    [InlineData("\\u0041\\u0042\\u0043", "ABC")]
    [InlineData("混合\\u6D4B\\u8BD5字符串", "混合测试字符串")]
    [InlineData("非Unicode字符串", "非Unicode字符串")]
    public void Test_UnicodeToStr(string input, string result)
    {
        Assert.Equal(result, Str.UnicodeToStr(input));
    }
}
