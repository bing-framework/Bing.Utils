namespace Bing.Helpers;

/// <summary>
/// 测试类：覆盖 Env 的 .NET 环境管理优先级、设置策略与幂等契约。
/// </summary>
[Trait("Bing.Helpers", "Env.EnvironmentManagement.Contract")]
[Collection("EnvSerial")]
public class EnvEnvironmentManagementContractTest
{
    private const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";
    private const string DotNetEnvironment = "DOTNET_ENVIRONMENT";

    /// <summary>
    /// 测试用例：GetEnvironmentName 在不同环境变量组合下应遵循优先级与回退规则。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetEnvironmentNameCases))]
    public void GetEnvironmentName_VariableCombinations_ReturnsExpectedResult(
        string aspNetCoreValue,
        string dotNetValue,
        string expected)
    {
        using var _ = new EnvironmentNameScope();
        SetProcessEnv(AspNetCoreEnvironment, aspNetCoreValue);
        SetProcessEnv(DotNetEnvironment, dotNetValue);

        var result = Env.GetEnvironmentName();

        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：SetEnvironmentName 根据 setBothVariables 开关更新目标环境变量。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetSetEnvironmentNameCases))]
    public void SetEnvironmentName_FlagCombinations_UpdatesExpectedVariables(
        string existingDotNetValue,
        string environmentName,
        bool setBothVariables,
        string expectedAspNetCoreValue,
        string expectedDotNetValue)
    {
        using var _ = new EnvironmentNameScope();
        SetProcessEnv(DotNetEnvironment, existingDotNetValue);

        Env.SetEnvironmentName(environmentName, setBothVariables: setBothVariables);

        GetProcessEnv(AspNetCoreEnvironment).ShouldBe(expectedAspNetCoreValue);
        GetProcessEnv(DotNetEnvironment).ShouldBe(expectedDotNetValue);
    }

    /// <summary>
    /// 测试用例：SetDevelopment 在不同初始状态下应保持幂等且不错误覆盖。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetSetDevelopmentCases))]
    public void SetDevelopment_DifferentInitialStates_ProducesExpectedValues(
        string initialAspNetCoreValue,
        string initialDotNetValue,
        string expectedAspNetCoreValue,
        string expectedDotNetValue)
    {
        using var _ = new EnvironmentNameScope();
        SetProcessEnv(AspNetCoreEnvironment, initialAspNetCoreValue);
        SetProcessEnv(DotNetEnvironment, initialDotNetValue);

        Env.SetDevelopment();

        GetProcessEnv(AspNetCoreEnvironment).ShouldBe(expectedAspNetCoreValue);
        GetProcessEnv(DotNetEnvironment).ShouldBe(expectedDotNetValue);
    }

    /// <summary>
    /// 测试用例：IsEnvironment 比较应忽略大小写。
    /// </summary>
    [Fact]
    public void IsEnvironment_CaseInsensitiveComparison_ReturnsTrue()
    {
        using var _ = new EnvironmentNameScope();
        SetProcessEnv(AspNetCoreEnvironment, "dEvElOpMeNt");

        var result = Env.IsEnvironment("development");

        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：IsEnvironment 传入空白环境名时，应抛出参数异常并包含参数名。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetInvalidEnvironmentNames))]
    public void IsEnvironment_InvalidEnvironmentName_ThrowsArgumentExceptionWithParamName(string environmentName)
    {
        var ex = Should.Throw<ArgumentException>(() => Env.IsEnvironment(environmentName));

        ex.ParamName.ShouldBe("environmentName");
    }

    /// <summary>
    /// 测试数据：GetEnvironmentName 优先级与回退场景。
    /// </summary>
    public static IEnumerable<object[]> GetEnvironmentNameCases()
    {
        yield return new object[] { Env.Staging, Env.Production, Env.Staging };
        yield return new object[] { " ", Env.Testing, Env.Testing };
        yield return new object[] { null, Env.Development, Env.Development };
        yield return new object[] { null, null, null };
    }

    /// <summary>
    /// 测试数据：SetEnvironmentName 的 setBothVariables 开关场景。
    /// </summary>
    public static IEnumerable<object[]> GetSetEnvironmentNameCases()
    {
        yield return new object[] { null, Env.Production, true, Env.Production, Env.Production };
        yield return new object[] { Env.Staging, Env.Development, false, Env.Development, Env.Staging };
    }

    /// <summary>
    /// 测试数据：SetDevelopment 在不同初始环境变量状态下的期望结果。
    /// </summary>
    public static IEnumerable<object[]> GetSetDevelopmentCases()
    {
        yield return new object[] { null, null, Env.Development, Env.Development };
        yield return new object[] { null, Env.Production, null, Env.Production };
        yield return new object[] { Env.Staging, null, Env.Staging, null };
    }

    /// <summary>
    /// 测试数据：非法环境名称输入。
    /// </summary>
    public static IEnumerable<object[]> GetInvalidEnvironmentNames()
    {
        yield return new object[] { null };
        yield return new object[] { string.Empty };
        yield return new object[] { " " };
        yield return new object[] { "\t" };
    }

    private static void SetProcessEnv(string name, string value) =>
        Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);

    private static string GetProcessEnv(string name) =>
        Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

    /// <summary>
    /// 作用域：保存并恢复进程级环境变量，避免测试污染。
    /// </summary>
    private sealed class EnvironmentNameScope : IDisposable
    {
        private readonly string _aspNetCore;
        private readonly string _dotNet;

        public EnvironmentNameScope()
        {
            _aspNetCore = GetProcessEnv(AspNetCoreEnvironment);
            _dotNet = GetProcessEnv(DotNetEnvironment);
        }

        public void Dispose()
        {
            SetProcessEnv(AspNetCoreEnvironment, _aspNetCore);
            SetProcessEnv(DotNetEnvironment, _dotNet);
        }
    }
}