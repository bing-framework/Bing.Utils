namespace Bing.Helpers;

/// <summary>
/// 测试类：Env 环境变量相关缺陷回归测试。
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
[Collection("EnvSerial")]
public class EnvRegressionTest
{
    /// <summary>
    /// 测试用例：SetDevelopment 在已有环境变量时不应覆盖现有值（回归保护）。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetSetDevelopmentNoOverrideCases))]
    [Trait("DefectPattern", "Helpers.Env.SetDevelopment.NoOverride")]
    public void SetDevelopment_EnvironmentAlreadySet_ShouldKeepOriginalValue(
        string initialDotnet,
        string initialAspnet,
        string expectedDotnet,
        string expectedAspnet)
    {
        const string dotnetKey = "DOTNET_ENVIRONMENT";
        const string aspnetKey = "ASPNETCORE_ENVIRONMENT";
        var originDotnet = Env.GetEnvironmentVariable(dotnetKey, EnvironmentVariableTarget.Process);
        var originAspnet = Env.GetEnvironmentVariable(aspnetKey, EnvironmentVariableTarget.Process);

        try
        {
            SetOrRemoveProcessVariable(dotnetKey, initialDotnet);
            SetOrRemoveProcessVariable(aspnetKey, initialAspnet);

            Env.SetDevelopment(EnvironmentVariableTarget.Process);

            Env.GetEnvironmentVariable(dotnetKey, EnvironmentVariableTarget.Process).ShouldBe(expectedDotnet);
            Env.GetEnvironmentVariable(aspnetKey, EnvironmentVariableTarget.Process).ShouldBe(expectedAspnet);
        }
        finally
        {
            RestoreVariable(dotnetKey, originDotnet);
            RestoreVariable(aspnetKey, originAspnet);
        }
    }

    /// <summary>
    /// 测试数据：SetDevelopment 不覆盖既有环境变量的代表场景。
    /// </summary>
    public static IEnumerable<object[]> GetSetDevelopmentNoOverrideCases()
    {
        yield return new object[] { "Staging", null, "Staging", null };
        yield return new object[] { null, "Production", null, "Production" };
        yield return new object[] { "Testing", "Staging", "Testing", "Staging" };
    }

    private static void SetOrRemoveProcessVariable(string name, string value)
    {
        if (value == null)
            Env.RemoveEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        else
            Env.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
    }

    private static void RestoreVariable(string name, string value)
    {
        if (value == null)
            Env.RemoveEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        else
            Env.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
    }
}