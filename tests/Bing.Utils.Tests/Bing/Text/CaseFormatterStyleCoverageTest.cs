namespace Bing.Text;
/// <summary>
/// 测试类：覆盖 `CaseFormatterStyleCoverage` 相关行为。
/// </summary>
[Trait("Bing.Text", "CaseFormatter.StyleCoverage")]
public class CaseFormatterStyleCoverageTest
{
    /// <summary>
    /// 测试用例：验证 `To` 在 `LowerCamelWithWhiteSpace` 场景下，结果为 `ReturnsExpected`。
    /// </summary>
    [Fact]
    public void To_LowerCamelWithWhiteSpace_ReturnsExpected()
    {
        var result = CaseFormatter.Instance.To(CaseFormatter.Style.LowerCamelWithWhiteSpace, "FIRST_name-value");
        result.ShouldBe("first Name Value");
    }
    /// <summary>
    /// 测试用例：验证 `To` 在 `UpperCamelWithWhiteSpace` 场景下，结果为 `ReturnsExpected`。
    /// </summary>
    [Fact]
    public void To_UpperCamelWithWhiteSpace_ReturnsExpected()
    {
        var result = CaseFormatter.Instance.To(CaseFormatter.Style.UpperCamelWithWhiteSpace, "first_name-value");
        result.ShouldBe("First Name Value");
    }
    /// <summary>
    /// 测试用例：验证 `To` 在 `UpperUnderscore` 场景下，结果为 `ReturnsExpected`。
    /// </summary>
    [Fact]
    public void To_UpperUnderscore_ReturnsExpected()
    {
        var result = CaseFormatter.Instance.To(CaseFormatter.Style.UpperUnderscore, "first-name user");
        result.ShouldBe("FIRST_NAME_USER");
    }
    /// <summary>
    /// 测试用例：验证 `LowerHyphenFormatter` 在 `WithUnderscoreInput` 场景下，结果为 `SplitsByConfiguredSeparatorOnly`。
    /// </summary>
    [Fact]
    public void LowerHyphenFormatter_WithUnderscoreInput_SplitsByConfiguredSeparatorOnly()
    {
        var result = CaseFormatter.LowerHyphen.To(CaseFormatter.Style.UpperCamel, "first_name");
        // LowerHyphen formatter only splits by '-', so underscore remains in one token.
        result.ShouldBe("First_name");
    }
    /// <summary>
    /// 测试用例：验证 `HumanizerFormatter` 在 `WithMixedSeparators` 场景下，结果为 `CurrentBehaviorMatchesInstance`。
    /// </summary>
    [Fact]
    public void HumanizerFormatter_WithMixedSeparators_CurrentBehaviorMatchesInstance()
    {
        var input = "first_name-last value";
        var resultFromHumanizer = CaseFormatter.Humanizer.To(CaseFormatter.Style.UpperCamelWithWhiteSpace, input);
        var resultFromInstance = CaseFormatter.Instance.To(CaseFormatter.Style.UpperCamelWithWhiteSpace, input);
        resultFromHumanizer.ShouldBe(resultFromInstance);
        resultFromHumanizer.ShouldBe("First Name Last Value");
    }
}

