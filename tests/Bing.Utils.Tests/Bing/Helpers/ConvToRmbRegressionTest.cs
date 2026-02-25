namespace Bing.Helpers;

/// <summary>
/// 测试类：`Conv.ToRMB` 回归测试
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class ConvToRmbRegressionTest
{
    /// <summary>
    /// 测试用例：输入为 `null` 时应返回 `null`（回归：空输入导致异常）
    /// </summary>
    [Fact]
    [Trait("DefectPattern", "Helpers.Conv.ToRMB.NullInput")]
    public void ToRMB_NullInput_ShouldReturnNull()
    {
        var result = Conv.ToRMB(null);

        result.ShouldBeNull();
    }

    /// <summary>
    /// 测试用例：无法解析为金额的字符串应原样返回（回归：错误地抛异常或返回空值）
    /// </summary>
    [Theory]
    [MemberData(nameof(GetInvalidTextInputs))]
    [Trait("DefectPattern", "Helpers.Conv.ToRMB.InvalidTextPassthrough")]
    public void ToRMB_InvalidTextInput_ShouldReturnOriginalText(string input)
    {
        var result = Conv.ToRMB(input);

        result.ShouldBe(input);
    }

    /// <summary>
    /// 测试数据：无法转换为人民币金额的文本输入
    /// </summary>
    public static IEnumerable<object[]> GetInvalidTextInputs()
    {
        yield return new object[] { string.Empty };
        yield return new object[] { "invalid" };
        yield return new object[] { "12a.34" };
        yield return new object[] { "￥123" };
    }
}
