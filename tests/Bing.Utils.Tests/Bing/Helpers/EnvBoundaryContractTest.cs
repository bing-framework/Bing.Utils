namespace Bing.Helpers;

/// <summary>
/// 测试类：覆盖 <see cref="Env"/> 环境变量与展开逻辑的边界契约。
/// </summary>
[Trait("Bing.Helpers", "Env.BoundaryContract")]
[Collection("EnvSerial")]
public class EnvBoundaryContractTest
{
    /// <summary>
    /// 测试用例：环境变量名称为 `null`/空白时，应抛出 <see cref="ArgumentException"/> 且参数名为 `name`。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetInvalidVariableNames))]
    public void EnvironmentVariable_InvalidName_ThrowsArgumentExceptionWithParamName(string name)
    {
        var setEx = Should.Throw<ArgumentException>(() => Env.SetEnvironmentVariable(name, "v"));
        setEx.ParamName.ShouldBe("name");

        var getEx = Should.Throw<ArgumentException>(() => Env.GetEnvironmentVariable(name));
        getEx.ParamName.ShouldBe("name");

        var removeEx = Should.Throw<ArgumentException>(() => Env.RemoveEnvironmentVariable(name));
        removeEx.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// 测试用例：`GetEnvironmentVariable<T>` 在名称非法时返回默认值，不抛异常。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetInvalidVariableNames))]
    public void GetEnvironmentVariable_Generic_InvalidName_ReturnsDefault(string name)
    {
        var result = Env.GetEnvironmentVariable(name, 123);

        result.ShouldBe(123);
    }

    /// <summary>
    /// 测试用例：`HasEnvironmentVariable` 在名称非法时返回 `false`（吞掉参数异常）。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetInvalidVariableNames))]
    public void HasEnvironmentVariable_InvalidName_ReturnsFalse(string name)
    {
        var result = Env.HasEnvironmentVariable(name);

        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：环境变量值为空字符串时，`HasEnvironmentVariable` 返回 `false`，泛型读取返回默认值。
    /// </summary>
    [Fact]
    public void EnvironmentVariable_EmptyValue_HasReturnsFalseAndGenericReturnsDefault()
    {
        var name = CreateVarName();
        try
        {
            Env.SetEnvironmentVariable(name, string.Empty);

            Env.GetEnvironmentVariable(name).ShouldBeNullOrEmpty();
            Env.HasEnvironmentVariable(name).ShouldBeFalse();
            Env.GetEnvironmentVariable(name, 77).ShouldBe(77);
        }
        finally
        {
            Env.RemoveEnvironmentVariable(name);
        }
    }

    /// <summary>
    /// 测试用例：设置 `null` 值等价于删除环境变量，重复删除应幂等。
    /// </summary>
    [Fact]
    public void SetEnvironmentVariable_NullValue_RemovesVariableAndRemoveIsIdempotent()
    {
        var name = CreateVarName();
        Env.SetEnvironmentVariable(name, "v1");
        Env.HasEnvironmentVariable(name).ShouldBeTrue();

        Env.SetEnvironmentVariable(name, null);
        Env.HasEnvironmentVariable(name).ShouldBeFalse();
        Env.GetEnvironmentVariable(name).ShouldBeNull();

        Should.NotThrow(() => Env.RemoveEnvironmentVariable(name));
    }

    /// <summary>
    /// 测试用例：`GetEnvironmentVariables(Process)` 应包含当前进程刚设置的变量。
    /// </summary>
    [Fact]
    public void GetEnvironmentVariables_ProcessTarget_ContainsRecentlySetVariable()
    {
        var name = CreateVarName();
        const string value = "boundary_value";
        try
        {
            Env.SetEnvironmentVariable(name, value);

            var all = Env.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

            all.ContainsKey(name).ShouldBeTrue();
            all[name].ShouldBe(value);
        }
        finally
        {
            Env.RemoveEnvironmentVariable(name);
        }
    }

    /// <summary>
    /// 测试用例：`ExpandEnvironmentVariables` 对 `null`/空字符串/纯空白应原样返回。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetRawPassThroughInputs))]
    public void ExpandEnvironmentVariables_NullOrEmpty_ReturnsInput(string input)
    {
        var result = Env.ExpandEnvironmentVariables(input);

        result.ShouldBe(input);
    }

    /// <summary>
    /// 测试用例：`ExpandEnvironmentVariables` 对 `%VAR%` 模式应正确展开当前进程变量。
    /// </summary>
    [Fact]
    public void ExpandEnvironmentVariables_PercentPattern_ExpandsProcessVariable()
    {
        var name = CreateVarName();
        const string value = "expanded";
        try
        {
            Env.SetEnvironmentVariable(name, value);
            var template = $"%{name}%";

            var result = Env.ExpandEnvironmentVariables(template);

            result.ShouldBe(value);
        }
        finally
        {
            Env.RemoveEnvironmentVariable(name);
        }
    }

    /// <summary>
    /// 测试用例：`GetEnvironmentVariable<T>` 在转换失败时，当前实现返回类型默认值（而非调用方传入默认值）。
    /// </summary>
    [Fact]
    public void GetEnvironmentVariable_Generic_WhenConversionFails_CurrentlyReturnsTypeDefault()
    {
        var name = CreateVarName();
        try
        {
            Env.SetEnvironmentVariable(name, "not-a-number");

            var intResult = Env.GetEnvironmentVariable(name, 999);
            var boolResult = Env.GetEnvironmentVariable(name, true);
            var doubleResult = Env.GetEnvironmentVariable(name, 1.23d);

            intResult.ShouldBe(0);
            boolResult.ShouldBeFalse();
            doubleResult.ShouldBe(0d);
        }
        finally
        {
            Env.RemoveEnvironmentVariable(name);
        }
    }

    /// <summary>
    /// 测试用例：`SetEnvironmentVariable` 支持对象值并按 `ToString()` 存储。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetObjectValuesForEnvironmentVariableCases))]
    public void SetEnvironmentVariable_ObjectValue_StoresStringRepresentation(object value, string expected)
    {
        var name = CreateVarName();
        try
        {
            Env.SetEnvironmentVariable(name, value);

            var result = Env.GetEnvironmentVariable(name);
            result.ShouldBe(expected);
        }
        finally
        {
            Env.RemoveEnvironmentVariable(name);
        }
    }

    public static IEnumerable<object[]> GetInvalidVariableNames()
    {
        yield return new object[] { null };
        yield return new object[] { string.Empty };
        yield return new object[] { " " };
        yield return new object[] { "\t" };
        yield return new object[] { "\r\n" };
    }

    public static IEnumerable<object[]> GetRawPassThroughInputs()
    {
        yield return new object[] { null };
        yield return new object[] { string.Empty };
        yield return new object[] { "   " };
    }

    public static IEnumerable<object[]> GetObjectValuesForEnvironmentVariableCases()
    {
        yield return new object[] { true, bool.TrueString };
        yield return new object[] { 123, "123" };
        yield return new object[] { 3.14, "3.14" };
        yield return new object[] { 'A', "A" };
    }

    private static string CreateVarName() => $"BING_ENV_BOUNDARY_{Guid.NewGuid():N}";
}
