using Bing.Tests;
using System.Runtime.InteropServices;

namespace Bing.Helpers;

/// <summary>
/// 环境操作工具类测试
/// </summary>
[Trait("Bing.Helpers", "Env")]
public class EnvTest : TestBase
{
    /// <inheritdoc />
    public EnvTest(ITestOutputHelper output) : base(output)
    {
    }

    #region 系统信息属性测试

    /// <summary>
    /// 测试 - 系统信息属性 - 返回有效值
    /// </summary>
    [Fact]
    public void SystemProperties_ReturnsValidValues()
    {
        // Act & Assert
        Env.NewLine.ShouldNotBeNullOrEmpty();
        Env.OSDescription.ShouldNotBeNullOrEmpty();
        Env.RuntimeIdentifier.ShouldNotBeNullOrEmpty();
        Env.FrameworkDescription.ShouldNotBeNullOrEmpty();
        Env.MachineName.ShouldNotBeNullOrEmpty();
        Env.UserName.ShouldNotBeNullOrEmpty();
        Env.CurrentDirectory.ShouldNotBeNullOrEmpty();
        Env.SystemDirectory.ShouldNotBeNullOrEmpty();

        Env.ProcessorCount.ShouldBeGreaterThan(0);
        Env.TickCount.ShouldBeGreaterThan(0);

        Output.WriteLine($"操作系统: {Env.OSDescription}");
        Output.WriteLine($"OS架构: {Env.OSArchitecture}");
        Output.WriteLine($"进程架构: {Env.ProcessArchitecture}");
        Output.WriteLine($"运行时标识符: {Env.RuntimeIdentifier}");
        Output.WriteLine($"框架描述: {Env.FrameworkDescription}");
        Output.WriteLine($"机器名: {Env.MachineName}");
        Output.WriteLine($"用户名: {Env.UserName}");
        Output.WriteLine($"用户域: {Env.UserDomainName}");
        Output.WriteLine($"处理器数量: {Env.ProcessorCount}");
        Output.WriteLine($"64位OS: {Env.Is64BitOperatingSystem}");
        Output.WriteLine($"64位进程: {Env.Is64BitProcess}");
    }

    #endregion

    #region 环境变量操作测试

    /// <summary>
    /// 测试 - SetEnvironmentVariable 和 GetEnvironmentVariable - 基本功能
    /// </summary>
    [Fact]
    public void SetAndGetEnvironmentVariable_BasicFunctionality_WorksCorrectly()
    {
        // Arrange
        const string varName = "BING_TEST_VAR";
        const string varValue = "test_value_123";

        try
        {
            // Act - 设置环境变量
            Env.SetEnvironmentVariable(varName, varValue);

            // Assert - 获取环境变量
            var result = Env.GetEnvironmentVariable(varName);
            result.ShouldBe(varValue);

            Output.WriteLine($"设置并获取环境变量: {varName} = {result}");
        }
        finally
        {
            // Cleanup
            Env.RemoveEnvironmentVariable(varName);
        }
    }

    /// <summary>
    /// 测试 - GetEnvironmentVariable 泛型版本 - 类型转换
    /// </summary>
    [Theory]
    [InlineData("123", 123)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("3.14", 3.14)]
    public void GetEnvironmentVariable_Generic_ConvertsTypesCorrectly<T>(string value, T expected)
    {
        // Arrange
        const string varName = "BING_TEST_TYPE_VAR";

        try
        {
            // Act
            Env.SetEnvironmentVariable(varName, value);
            var result = Env.GetEnvironmentVariable<T>(varName);

            // Assert
            result.ShouldBe(expected);

            Output.WriteLine($"类型转换测试: {value} -> {typeof(T).Name} = {result}");
        }
        finally
        {
            // Cleanup
            Env.RemoveEnvironmentVariable(varName);
        }
    }

    /// <summary>
    /// 测试 - GetEnvironmentVariable 泛型版本 - 默认值处理
    /// </summary>
    [Fact]
    public void GetEnvironmentVariable_Generic_ReturnsDefaultWhenNotExists()
    {
        // Arrange
        const string nonExistentVar = "BING_NON_EXISTENT_VAR_" + nameof(GetEnvironmentVariable_Generic_ReturnsDefaultWhenNotExists);
        const int defaultValue = 42;

        // Act
        var result = Env.GetEnvironmentVariable(nonExistentVar, defaultValue);

        // Assert
        result.ShouldBe(defaultValue);
    }

    /// <summary>
    /// 测试 - HasEnvironmentVariable - 存在性检查
    /// </summary>
    [Fact]
    public void HasEnvironmentVariable_ExistenceCheck_WorksCorrectly()
    {
        // Arrange
        const string existingVar = "BING_EXISTING_VAR";
        const string nonExistentVar = "BING_NON_EXISTENT_VAR_" + nameof(HasEnvironmentVariable_ExistenceCheck_WorksCorrectly);

        try
        {
            // Act & Assert - 不存在的变量
            Env.HasEnvironmentVariable(nonExistentVar).ShouldBeFalse();

            // Act & Assert - 存在的变量
            Env.SetEnvironmentVariable(existingVar, "some_value");
            Env.HasEnvironmentVariable(existingVar).ShouldBeTrue();

            // Act & Assert - 删除后不存在
            Env.RemoveEnvironmentVariable(existingVar);
            Env.HasEnvironmentVariable(existingVar).ShouldBeFalse();
        }
        finally
        {
            // Cleanup
            Env.RemoveEnvironmentVariable(existingVar);
        }
    }

    /// <summary>
    /// 测试 - GetEnvironmentVariables - 获取所有环境变量
    /// </summary>
    [Fact]
    public void GetEnvironmentVariables_ReturnsAllVariables()
    {
        // Act
        var variables = Env.GetEnvironmentVariables();

        // Assert
        variables.ShouldNotBeNull();
        variables.Count.ShouldBeGreaterThan(0);

        // PATH 变量在所有系统上都应该存在
        variables.Keys.ShouldContain(key =>
            key.Equals("PATH", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Path", StringComparison.OrdinalIgnoreCase));

        Output.WriteLine($"环境变量总数: {variables.Count}");
        Output.WriteLine("前5个环境变量:");
        var count = 0;
        foreach (var kvp in variables)
        {
            if (count >= 5) break;
            Output.WriteLine($"  {kvp.Key} = {kvp.Value?[..Math.Min(50, kvp.Value.Length)]}...");
            count++;
        }
    }

    /// <summary>
    /// 测试 - 环境变量参数验证
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnvironmentVariable_InvalidNames_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Env.SetEnvironmentVariable(invalidName, "value"));
        Should.Throw<ArgumentException>(() => Env.GetEnvironmentVariable(invalidName));
    }

    /// <summary>
    /// 测试 - 环境变量目标范围
    /// </summary>
    [Fact]
    public void EnvironmentVariable_DifferentTargets_WorkCorrectly()
    {
        // Arrange
        const string varName = "BING_TARGET_TEST_VAR";
        const string processValue = "process_value";

        try
        {
            // Act - 设置进程级变量
            Env.SetEnvironmentVariable(varName, processValue, EnvironmentVariableTarget.Process);

            // Assert
            var processResult = Env.GetEnvironmentVariable(varName, EnvironmentVariableTarget.Process);
            processResult.ShouldBe(processValue);

            // 验证其他目标范围没有这个变量
            var userResult = Env.GetEnvironmentVariable(varName, EnvironmentVariableTarget.User);
            userResult.ShouldBeNull();

            Output.WriteLine($"进程级环境变量测试通过: {processResult}");
        }
        finally
        {
            // Cleanup
            Env.RemoveEnvironmentVariable(varName, EnvironmentVariableTarget.Process);
        }
    }

    #endregion

    #region .NET 环境管理测试

    /// <summary>
    /// 测试 - SetDevelopment - 设置开发环境
    /// </summary>
    [Fact]
    public void SetDevelopment_SetsCorrectEnvironmentVariables()
    {
        // Arrange - 备份当前环境变量
        var originalDotnet = Env.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var originalAspNetCore = Env.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        try
        {
            // 清除环境变量
            Env.RemoveEnvironmentVariable("DOTNET_ENVIRONMENT");
            Env.RemoveEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            // Act
            Env.SetDevelopment();

            // Assert
            Env.GetEnvironmentVariable("DOTNET_ENVIRONMENT").ShouldBe("Development");
            Env.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ShouldBe("Development");
            Env.IsDevelopment().ShouldBeTrue();

            Output.WriteLine("开发环境设置成功");
        }
        finally
        {
            // 恢复原始环境变量
            RestoreEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotnet);
            RestoreEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
        }
    }

    /// <summary>
    /// 测试 - SetDevelopment - 不覆盖已存在的环境变量
    /// </summary>
    [Fact]
    public void SetDevelopment_DoesNotOverrideExistingVariables()
    {
        // Arrange
        const string existingValue = "Production";
        var originalValue = Env.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        try
        {
            // 设置一个现有值
            Env.SetEnvironmentVariable("DOTNET_ENVIRONMENT", existingValue);

            // Act
            Env.SetDevelopment();

            // Assert - 不应该被覆盖
            Env.GetEnvironmentVariable("DOTNET_ENVIRONMENT").ShouldBe(existingValue);

            Output.WriteLine("已存在的环境变量未被覆盖");
        }
        finally
        {
            // 恢复原始值
            if (originalValue != null)
                Env.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalValue);
            else
                Env.RemoveEnvironmentVariable("DOTNET_ENVIRONMENT");
        }
    }

    /// <summary>
    /// 测试 - 环境检查方法
    /// </summary>
    [Theory]
    [InlineData("Development", true, false, false, false)]
    [InlineData("Staging", false, true, false, false)]
    [InlineData("Production", false, false, true, false)]
    [InlineData("Testing", false, false, false, true)]
    [InlineData("Custom", false, false, false, false)]
    public void EnvironmentChecks_DifferentEnvironments_ReturnCorrectResults(
        string environmentName,
        bool expectedIsDev,
        bool expectedIsStaging,
        bool expectedIsProduction,
        bool expectedIsTesting)
    {
        // Arrange
        var originalValue = Env.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        try
        {
            // Act
            Env.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environmentName);

            // Assert
            Env.IsDevelopment().ShouldBe(expectedIsDev);
            Env.IsStaging().ShouldBe(expectedIsStaging);
            Env.IsProduction().ShouldBe(expectedIsProduction);
            Env.IsTesting().ShouldBe(expectedIsTesting);
            Env.IsEnvironment(environmentName).ShouldBeTrue();

            Output.WriteLine($"环境 '{environmentName}' 检查通过");
        }
        finally
        {
            // 恢复原始值
            if (originalValue != null)
                Env.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalValue);
            else
                Env.RemoveEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        }
    }

    /// <summary>
    /// 测试 - GetEnvironmentName - 优先级处理
    /// </summary>
    [Fact]
    public void GetEnvironmentName_PriorityHandling_WorksCorrectly()
    {
        // Arrange
        var originalAspNetCore = Env.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalDotNet = Env.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        try
        {
            // 清除环境变量
            Env.RemoveEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Env.RemoveEnvironmentVariable("DOTNET_ENVIRONMENT");

            // Test 1: 只设置 DOTNET_ENVIRONMENT
            Env.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "TestDotNet");
            Env.GetEnvironmentName().ShouldBe("TestDotNet");

            // Test 2: 设置 ASPNETCORE_ENVIRONMENT，应该优先
            Env.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "TestAspNetCore");
            Env.GetEnvironmentName().ShouldBe("TestAspNetCore");

            // Test 3: 清除 ASPNETCORE_ENVIRONMENT，应该回到 DOTNET_ENVIRONMENT
            Env.RemoveEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Env.GetEnvironmentName().ShouldBe("TestDotNet");

            Output.WriteLine("环境变量优先级测试通过");
        }
        finally
        {
            // 恢复原始值
            if (originalAspNetCore != null)
                Env.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
            else
                Env.RemoveEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (originalDotNet != null)
                Env.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotNet);
            else
                Env.RemoveEnvironmentVariable("DOTNET_ENVIRONMENT");
        }
    }

    /// <summary>
    /// 测试 - SetEnvironmentName - 设置环境名称
    /// </summary>
    [Fact]
    public void SetEnvironmentName_WorksCorrectly()
    {
        // Arrange
        var originalValue = Env.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        try
        {
            // Act - 设置单个环境变量
            Env.SetEnvironmentName("CustomEnvironment");
            Env.GetEnvironmentName().ShouldBe("CustomEnvironment");

            // Act - 设置两个环境变量
            Env.SetEnvironmentName("AnotherEnvironment", setBothVariables: true);
            Env.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ShouldBe("AnotherEnvironment");
            Env.GetEnvironmentVariable("DOTNET_ENVIRONMENT").ShouldBe("AnotherEnvironment");

            Output.WriteLine("环境名称设置测试通过");
        }
        finally
        {
            // 恢复原始值
            if (originalValue != null)
                Env.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalValue);
            else
                Env.RemoveEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            Env.RemoveEnvironmentVariable("DOTNET_ENVIRONMENT");
        }
    }

    /// <summary>
    /// 测试 - IsEnvironment - 大小写不敏感
    /// </summary>
    [Theory]
    [InlineData("Development", "development", true)]
    [InlineData("Production", "PRODUCTION", true)]
    [InlineData("Staging", "staging", true)]
    [InlineData("Development", "Production", false)]
    public void IsEnvironment_CaseInsensitive_WorksCorrectly(string setEnvironment, string checkEnvironment, bool expected)
    {
        // Arrange
        var originalValue = Env.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        try
        {
            // Act
            Env.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", setEnvironment);
            var result = Env.IsEnvironment(checkEnvironment);

            // Assert
            result.ShouldBe(expected);

            Output.WriteLine($"环境比较: '{setEnvironment}' vs '{checkEnvironment}' = {result}");
        }
        finally
        {
            // 恢复原始值
            if (originalValue != null)
                Env.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalValue);
            else
                Env.RemoveEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        }
    }

    #endregion

    #region 特殊文件夹路径测试

    /// <summary>
    /// 测试 - 特殊文件夹路径 - 返回有效路径
    /// </summary>
    [Fact]
    public void SpecialFolderPaths_ReturnsValidPaths()
    {
        // Act & Assert
        Env.TempPath.ShouldNotBeNullOrEmpty();
        Directory.Exists(Env.TempPath).ShouldBeTrue();

        Env.UserProfilePath.ShouldNotBeNullOrEmpty();
        Directory.Exists(Env.UserProfilePath).ShouldBeTrue();

        Env.ApplicationDataPath.ShouldNotBeNullOrEmpty();
        Directory.Exists(Env.ApplicationDataPath).ShouldBeTrue();

        Env.LocalApplicationDataPath.ShouldNotBeNullOrEmpty();
        Directory.Exists(Env.LocalApplicationDataPath).ShouldBeTrue();

        Output.WriteLine($"临时路径: {Env.TempPath}");
        Output.WriteLine($"用户配置路径: {Env.UserProfilePath}");
        Output.WriteLine($"应用数据路径: {Env.ApplicationDataPath}");
        Output.WriteLine($"本地应用数据路径: {Env.LocalApplicationDataPath}");
    }

    /// <summary>
    /// 测试 - GetFolderPath - 特殊文件夹
    /// </summary>
    [Theory]
    [InlineData(Environment.SpecialFolder.Desktop)]
    [InlineData(Environment.SpecialFolder.MyDocuments)]
    [InlineData(Environment.SpecialFolder.ApplicationData)]
    [InlineData(Environment.SpecialFolder.LocalApplicationData)]
    public void GetFolderPath_SpecialFolders_ReturnsValidPaths(Environment.SpecialFolder folder)
    {
        // Act
        var path = Env.GetFolderPath(folder);

        // Assert
        path.ShouldNotBeNullOrEmpty();
        // 注意：某些特殊文件夹在某些系统上可能不存在

        Output.WriteLine($"{folder}: {path}");
    }

    #endregion

    #region 实用工具方法测试

    /// <summary>
    /// 测试 - GetTempFileName - 创建临时文件
    /// </summary>
    [Fact]
    public void GetTempFileName_CreatesValidTempFile()
    {
        // Act
        var tempFile1 = Env.GetTempFileName();
        var tempFile2 = Env.GetTempFileName(".txt");

        try
        {
            // Assert
            File.Exists(tempFile1).ShouldBeTrue();
            File.Exists(tempFile2).ShouldBeTrue();

            tempFile1.ShouldNotBe(tempFile2);
            tempFile2.ShouldEndWith(".txt");

            Output.WriteLine($"临时文件1: {tempFile1}");
            Output.WriteLine($"临时文件2: {tempFile2}");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile1)) File.Delete(tempFile1);
            if (File.Exists(tempFile2)) File.Delete(tempFile2);
        }
    }

    /// <summary>
    /// 测试 - ExpandEnvironmentVariables - 环境变量展开
    /// </summary>
    [Fact]
    public void ExpandEnvironmentVariables_ExpandsCorrectly()
    {
        // Arrange
        const string testVarName = "BING_TEST_EXPAND_VAR";
        const string testVarValue = "expanded_value";

        try
        {
            // 设置测试环境变量
            Env.SetEnvironmentVariable(testVarName, testVarValue);

            // Act
            string template, result;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                template = $"%{testVarName}%";
                result = Env.ExpandEnvironmentVariables(template);
            }
            else
            {
                template = $"${testVarName}";
                result = Env.ExpandEnvironmentVariables(template);
            }

            // Assert
            result.ShouldContain(testVarValue);

            Output.WriteLine($"模板: {template}");
            Output.WriteLine($"展开后: {result}");
        }
        finally
        {
            // Cleanup
            Env.RemoveEnvironmentVariable(testVarName);
        }
    }

    #endregion

    #region 错误处理测试

    /// <summary>
    /// 测试 - 参数验证 - 空参数抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParameterValidation_EmptyParameters_ThrowsArgumentException(string invalidParam)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Env.SetEnvironmentVariable(invalidParam, "value"));
        Should.Throw<ArgumentException>(() => Env.GetEnvironmentVariable(invalidParam));
        Should.Throw<ArgumentException>(() => Env.IsEnvironment(invalidParam));
        Should.Throw<ArgumentException>(() => Env.SetEnvironmentName(invalidParam));
    }

    #endregion

    #region 集成测试

    /// <summary>
    /// 测试 - 实际应用场景 - 配置管理
    /// </summary>
    [Fact]
    public void RealWorldScenario_ConfigurationManagement_WorksCorrectly()
    {
        // Arrange
        var originalEnv = Env.GetEnvironmentName();

        try
        {
            // 模拟不同环境的配置
            var configurations = new Dictionary<string, string>
            {
                ["Development"] = "debug=true;logging=verbose",
                ["Production"] = "debug=false;logging=error",
                ["Staging"] = "debug=false;logging=warning"
            };

            foreach (var config in configurations)
            {
                // Act - 设置环境
                Env.SetEnvironmentName(config.Key);

                // Assert - 验证环境设置
                Env.GetEnvironmentName().ShouldBe(config.Key);
                Env.IsEnvironment(config.Key).ShouldBeTrue();

                // 模拟基于环境的配置逻辑
                string currentConfig = config.Value;
                currentConfig.ShouldNotBeNullOrEmpty();

                Output.WriteLine($"环境: {config.Key}, 配置: {currentConfig}");
            }
        }
        finally
        {
            // 恢复原始环境
            if (originalEnv != null)
                Env.SetEnvironmentName(originalEnv);
            else
            {
                Env.RemoveEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                Env.RemoveEnvironmentVariable("DOTNET_ENVIRONMENT");
            }
        }
    }

    /// <summary>
    /// 测试 - 性能测试 - 大量环境变量操作
    /// </summary>
    [Fact]
    public void PerformanceTest_ManyEnvironmentVariableOperations_CompletesQuickly()
    {
        // Arrange
        const int operationCount = 1000;
        var variableNames = new List<string>();

        try
        {
            // Act & Assert
            Should.CompleteIn(() =>
            {
                for (int i = 0; i < operationCount; i++)
                {
                    var varName = $"BING_PERF_TEST_VAR_{i}";
                    var varValue = $"value_{i}";

                    variableNames.Add(varName);

                    // 设置、获取、检查存在性
                    Env.SetEnvironmentVariable(varName, varValue);
                    var retrieved = Env.GetEnvironmentVariable(varName);
                    var exists = Env.HasEnvironmentVariable(varName);

                    retrieved.ShouldBe(varValue);
                    exists.ShouldBeTrue();
                }
            }, TimeSpan.FromSeconds(5));

            Output.WriteLine($"性能测试完成: {operationCount} 次环境变量操作");
        }
        finally
        {
            // Cleanup
            foreach (var varName in variableNames)
            {
                Env.RemoveEnvironmentVariable(varName);
            }
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 恢复环境变量值
    /// </summary>
    private static void RestoreEnvironmentVariable(string name, string originalValue)
    {
        if (originalValue != null)
            Env.SetEnvironmentVariable(name, originalValue);
        else
            Env.RemoveEnvironmentVariable(name);
    }

    /// <summary>
    /// 安全删除文件
    /// </summary>
    private static void SafeDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch
        {
            // 忽略删除失败
        }
    }

    #endregion
}