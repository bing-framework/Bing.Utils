namespace Bing.Text;
/// <summary>
/// 测试类：覆盖 `CaseFormatter` 相关行为。
/// </summary>
[Trait("Bing.Text", "CaseFormatter")]
public class CaseFormatterTest
{
    /// <summary>
    /// 测试用例：验证 `To` 在 `UpperCamelInput` 场景下，结果为 `ReturnsPascalCase`。
    /// </summary>
    [Theory]
    [InlineData("first-name", "FirstName")]
    [InlineData("already_mixed_case", "AlreadyMixedCase")]
    [InlineData("user profile", "UserProfile")]
    public void To_UpperCamelInput_ReturnsPascalCase(string input, string expected)
    {
        var result = CaseFormatter.Instance.To(CaseFormatter.Style.UpperCamel, input);
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `To` 在 `LowerCamelInput` 场景下，结果为 `ReturnsCamelCase`。
    /// </summary>
    [Theory]
    [InlineData("FIRST_NAME", "firstName")]
    [InlineData("employee-id", "employeeId")]
    public void To_LowerCamelInput_ReturnsCamelCase(string input, string expected)
    {
        var result = CaseFormatter.Instance.To(CaseFormatter.Style.LowerCamel, input);
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `To` 在 `NullOrEmptySequence` 场景下，结果为 `ReturnsEmptyString`。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void To_NullOrEmptySequence_ReturnsEmptyString(string input)
    {
        var result = CaseFormatter.Instance.To(CaseFormatter.Style.LowerUnderscore, input);
        result.ShouldBe(string.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `To` 在 `InvalidStyle` 场景下，结果为 `ThrowsInvalidOperationException`。
    /// </summary>
    [Fact]
    public void To_InvalidStyle_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() =>
            CaseFormatter.Instance.To((CaseFormatter.Style)999, "first_name"));
    }
}

