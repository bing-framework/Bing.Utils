namespace Bing.Helpers;

/// <summary>
/// 测试类：Check 参数校验缺陷回归测试。
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class CheckRegressionTest
{
    /// <summary>
    /// 测试用例：NotNull 在空值场景应抛出包含正确参数名的 ArgumentNullException。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetNotNullParamNameCases))]
    [Trait("DefectPattern", "Helpers.Check.NotNull.ParamNameContract")]
    public void NotNull_NullValue_ShouldThrowArgumentNullExceptionWithParamName(string parameterName)
    {
        var exception = Should.Throw<ArgumentNullException>(() => Check.NotNull<object>(null, parameterName));

        exception.ParamName.ShouldBe(parameterName);
    }

    /// <summary>
    /// 测试数据：NotNull 参数名回归场景。
    /// </summary>
    public static IEnumerable<object[]> GetNotNullParamNameCases()
    {
        yield return new object[] { "userId" };
        yield return new object[] { "order_id" };
        yield return new object[] { "Value" };
    }
}