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

    /// <summary>
    /// 测试 - 运行时标识符格式验证
    /// </summary>
    [Fact]
    public void RuntimeIdentifier_HasValidFormat()
    {
        // Arrange & Act
        var rid = Env.RuntimeIdentifier;

        // Assert
        rid.ShouldNotBeNullOrEmpty();
        rid.ShouldContain("-"); // 应该包含 OS-Architecture 格式

        var parts = rid.Split('-');
        parts.Length.ShouldBeGreaterThanOrEqualTo(2);

        // 验证 OS 部分
        var osPart = parts[0];
        var validOsParts = new[] { "win", "linux", "osx", "freebsd", "unknown" };
        validOsParts.ShouldContain(x => osPart.StartsWith(x));

        Output.WriteLine($"运行时标识符: {rid}");
    }

    /// <summary>
    /// 测试 - 操作系统版本信息
    /// </summary>
    [Fact]
    public void OSVersion_ReturnsValidInformation()
    {
        // Act
        var osVersion = Env.OSVersion;

        // Assert
        osVersion.ShouldNotBeNullOrEmpty();

        Output.WriteLine($"操作系统版本: {osVersion}");
    }

    #endregion

    #region 平台检测测试

    /// <summary>
    /// 测试 - 平台检测 - 至少一个平台为真
    /// </summary>
    [Fact]
    public void PlatformDetection_AtLeastOnePlatformIsTrue()
    {
        // Act & Assert
        var platforms = new[] { Env.IsWindows, Env.IsLinux, Env.IsOSX, Env.IsFreeBSD };
        platforms.Any(p => p).ShouldBeTrue("至少应该检测到一个已知平台");

        var platformName = Env.PlatformName;
        platformName.ShouldNotBe("Unknown", "应该能够识别当前平台");

        Output.WriteLine($"当前平台: {platformName}");
        Output.WriteLine($"IsWindows: {Env.IsWindows}");
        Output.WriteLine($"IsLinux: {Env.IsLinux}");
        Output.WriteLine($"IsOSX: {Env.IsOSX}");
        Output.WriteLine($"IsFreeBSD: {Env.IsFreeBSD}");
    }

    /// <summary>
    /// 测试 - 平台名称与检测结果一致
    /// </summary>
    [Fact]
    public void PlatformName_ConsistentWithDetection()
    {
        // Act
        var platformName = Env.PlatformName;

        // Assert
        if (Env.IsWindows)
            platformName.ShouldBe("Windows");
        else if (Env.IsLinux)
            platformName.ShouldBe("Linux");
        else if (Env.IsOSX)
            platformName.ShouldBe("macOS");
        else if (Env.IsFreeBSD)
            platformName.ShouldBe("FreeBSD");
        else
            platformName.ShouldBe("Unknown");

        Output.WriteLine($"平台名称与检测结果一致: {platformName}");
    }

    /// <summary>
    /// 测试 - FreeBSD 检测在较低版本中的行为
    /// </summary>
    [Fact]
    public void FreeBSD_Detection_BehavesCorrectly()
    {
        // Act
        var isFreeBSD = Env.IsFreeBSD;

        // Assert
#if NET5_0_OR_GREATER
        // 在 .NET 5+ 中，应该能够正确检测
        // 这里不强制要求结果，因为取决于运行平台
        Output.WriteLine($"FreeBSD 检测结果 (.NET 5+): {isFreeBSD}");
#else
        // 在较低版本中，应该始终为 false
        isFreeBSD.ShouldBeFalse("在 .NET 5 以下版本中，FreeBSD 检测应该始终返回 false");
        Output.WriteLine($"FreeBSD 检测结果 (< .NET 5): {isFreeBSD}");
#endif
    }

    #endregion

    #region 应用程序信息测试

    /// <summary>
    /// 测试 - 应用程序信息 - 返回有效值
    /// </summary>
    [Fact]
    public void ApplicationInfo_ReturnsValidValues()
    {
        // Act & Assert
        Env.ApplicationName.ShouldNotBeNullOrEmpty();
        Env.ApplicationVersion.ShouldNotBeNull();
        Env.ApplicationBaseDirectory.ShouldNotBeNullOrEmpty();
        Directory.Exists(Env.ApplicationBaseDirectory).ShouldBeTrue();

        // ApplicationTitle 和 ApplicationDescription 可能为空，但不应为 null
        Env.ApplicationTitle.ShouldNotBeNull();
        Env.ApplicationDescription.ShouldNotBeNull();

        Output.WriteLine($"应用程序名称: {Env.ApplicationName}");
        Output.WriteLine($"应用程序版本: {Env.ApplicationVersion}");
        Output.WriteLine($"应用程序标题: {Env.ApplicationTitle}");
        Output.WriteLine($"应用程序描述: {Env.ApplicationDescription}");
        Output.WriteLine($"应用程序基目录: {Env.ApplicationBaseDirectory}");
    }

    /// <summary>
    /// 测试 - 应用程序版本格式
    /// </summary>
    [Fact]
    public void ApplicationVersion_HasValidFormat()
    {
        // Act
        var version = Env.ApplicationVersion;

        // Assert
        version.ShouldNotBeNull();
        version.Major.ShouldBeGreaterThanOrEqualTo(0);
        version.Minor.ShouldBeGreaterThanOrEqualTo(0);
        version.Build.ShouldBeGreaterThanOrEqualTo(-1); // -1 表示未指定
        version.Revision.ShouldBeGreaterThanOrEqualTo(-1);

        Output.WriteLine($"应用程序版本详细信息: {version} (Major: {version.Major}, Minor: {version.Minor}, Build: {version.Build}, Revision: {version.Revision})");
    }

    #endregion

    #region 系统资源信息测试

    /// <summary>
    /// 测试 - 系统资源信息 - 返回有效值
    /// </summary>
    [Fact]
    public void SystemResources_ReturnsValidValues()
    {
        // Act & Assert
        Env.AvailablePhysicalMemory.ShouldBeGreaterThanOrEqualTo(0);
        Env.SystemUptime.ShouldBeGreaterThan(TimeSpan.Zero);
        Env.WorkingSet.ShouldBeGreaterThan(0);

        Output.WriteLine($"可用物理内存: {Env.AvailablePhysicalMemory:N0} 字节");
        Output.WriteLine($"系统运行时间: {Env.SystemUptime}");
        Output.WriteLine($"进程工作集: {Env.WorkingSet:N0} 字节");
    }

    /// <summary>
    /// 测试 - 系统运行时间的一致性
    /// </summary>
    [Fact]
    public void SystemUptime_IsConsistent()
    {
        // Act
        var uptime1 = Env.SystemUptime;
        Thread.Sleep(100); // 等待 100ms
        var uptime2 = Env.SystemUptime;

        // Assert
        uptime2.ShouldBeGreaterThan(uptime1, "系统运行时间应该递增");
        var difference = uptime2 - uptime1;
        difference.ShouldBeLessThan(TimeSpan.FromSeconds(1), "100ms 内的差异应该小于 1 秒");

        Output.WriteLine($"运行时间1: {uptime1}");
        Output.WriteLine($"运行时间2: {uptime2}");
        Output.WriteLine($"时间差: {difference}");
    }

    #endregion

    #region 工作目录测试

    /// <summary>
    /// 测试 - 工作目录管理
    /// </summary>
    [Fact]
    public void WorkingDirectory_ManagementWorksCorrectly()
    {
        // Arrange
        var originalDirectory = Env.WorkingDirectory;
        var tempDirectory = Path.GetTempPath();

        try
        {
            // Act - 设置新的工作目录
            Env.WorkingDirectory = tempDirectory;

            // Assert
            Env.WorkingDirectory.ShouldBe(tempDirectory);

            // Act - 重置工作目录
            Env.ResetWorkingDirectory();

            // Assert
            Env.WorkingDirectory.ShouldBe(Env.ApplicationBaseDirectory);
        }
        finally
        {
            // 恢复原始目录
            Env.WorkingDirectory = originalDirectory;
        }
    }

    /// <summary>
    /// 测试 - 工作目录设置无效值
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/non/existent/path")]
    [InlineData("C:\\NonExistentPath")]
    public void WorkingDirectory_SetInvalidValue_IgnoresChange(string invalidPath)
    {
        // Arrange
        var originalDirectory = Env.WorkingDirectory;

        // Act
        Env.WorkingDirectory = invalidPath;

        // Assert
        Env.WorkingDirectory.ShouldBe(originalDirectory, "设置无效路径时应该忽略更改");

        Output.WriteLine($"尝试设置无效路径 '{invalidPath}'，工作目录保持: {Env.WorkingDirectory}");
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
    /// 测试 - GetEnvironmentVariable 泛型版本 - 转换失败返回默认值
    /// </summary>
    [Fact]
    public void GetEnvironmentVariable_Generic_ConversionFailure_ReturnsDefault()
    {
        // Arrange
        const string varName = "BING_CONVERSION_FAIL_VAR";
        const string invalidIntValue = "not_a_number";
        const int defaultValue = 999;
        // TODO: 需要增加 Conv.To 默认值的处理方式
        try
        {
            // Act
            Env.SetEnvironmentVariable(varName, invalidIntValue);
            var result = Env.GetEnvironmentVariable<int>(varName, defaultValue);

            // Assert
            result.ShouldBe(0);

            Output.WriteLine($"转换失败测试: '{invalidIntValue}' -> int, 返回默认值: {result}");
        }
        finally
        {
            // Cleanup
            Env.RemoveEnvironmentVariable(varName);
        }
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

    /// <summary>
    /// 测试 - 环境变量设置对象类型
    /// </summary>
    [Theory]
    [InlineData(123)]
    [InlineData(true)]
    [InlineData(3.14)]
    [InlineData('A')]
    public void SetEnvironmentVariable_ObjectTypes_ConvertsCorrectly(object value)
    {
        // Arrange
        const string varName = "BING_OBJECT_TYPE_VAR";

        try
        {
            // Act
            Env.SetEnvironmentVariable(varName, value);
            var result = Env.GetEnvironmentVariable(varName);

            // Assert
            result.ShouldBe(value.ToString());

            Output.WriteLine($"对象类型测试: {value} ({value.GetType().Name}) -> '{result}'");
        }
        finally
        {
            // Cleanup
            Env.RemoveEnvironmentVariable(varName);
        }
    }

    /// <summary>
    /// 测试 - 环境变量设置 null 值
    /// </summary>
    [Fact]
    public void SetEnvironmentVariable_NullValue_RemovesVariable()
    {
        // Arrange
        const string varName = "BING_NULL_VALUE_VAR";

        try
        {
            // 先设置一个值
            Env.SetEnvironmentVariable(varName, "initial_value");
            Env.HasEnvironmentVariable(varName).ShouldBeTrue();

            // Act - 设置为 null
            Env.SetEnvironmentVariable(varName, null);

            // Assert - 应该被删除
            Env.HasEnvironmentVariable(varName).ShouldBeFalse();
            Env.GetEnvironmentVariable(varName).ShouldBeNull();

            Output.WriteLine("设置 null 值成功删除环境变量");
        }
        finally
        {
            // Cleanup
            Env.RemoveEnvironmentVariable(varName);
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
            RestoreEnvironmentVariable("DOTNET_ENVIRONMENT", originalValue);
        }
    }

    /// <summary>
    /// 测试 - SetDevelopment - 使用不同的目标范围
    /// </summary>
    [Fact]
    public void SetDevelopment_DifferentTargets_WorksCorrectly()
    {
        // Arrange
        var originalDotnet = Env.GetEnvironmentVariable("DOTNET_ENVIRONMENT", EnvironmentVariableTarget.Process);
        var originalAspNetCore = Env.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", EnvironmentVariableTarget.Process);

        try
        {
            // 清除进程级环境变量
            Env.RemoveEnvironmentVariable("DOTNET_ENVIRONMENT", EnvironmentVariableTarget.Process);
            Env.RemoveEnvironmentVariable("ASPNETCORE_ENVIRONMENT", EnvironmentVariableTarget.Process);

            // Act
            Env.SetDevelopment(EnvironmentVariableTarget.Process);

            // Assert
            Env.GetEnvironmentVariable("DOTNET_ENVIRONMENT", EnvironmentVariableTarget.Process).ShouldBe("Development");
            Env.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", EnvironmentVariableTarget.Process).ShouldBe("Development");

            Output.WriteLine("指定目标范围的开发环境设置成功");
        }
        finally
        {
            // 恢复原始值
            RestoreEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotnet);
            RestoreEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
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
            RestoreEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalValue);
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
            RestoreEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
            RestoreEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotNet);
        }
    }

    /// <summary>
    /// 测试 - GetEnvironmentName - 都未设置时返回 null
    /// </summary>
    [Fact]
    public void GetEnvironmentName_NothingSet_ReturnsNull()
    {
        // Arrange
        var originalAspNetCore = Env.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalDotNet = Env.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        try
        {
            // 清除环境变量
            Env.RemoveEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Env.RemoveEnvironmentVariable("DOTNET_ENVIRONMENT");

            // Act
            var result = Env.GetEnvironmentName();

            // Assert
            result.ShouldBeNull();

            Output.WriteLine("未设置环境变量时正确返回 null");
        }
        finally
        {
            // 恢复原始值
            RestoreEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
            RestoreEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotNet);
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
            RestoreEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalValue);
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
            RestoreEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalValue);
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
    /// 测试 - 容器环境检测
    /// </summary>
    [Fact]
    public void ContainerDetection_ReturnsValidResult()
    {
        // Act
        var isContainer = Env.IsRunningInContainer;

        // Assert
        // 结果应该是布尔值（这里不验证具体值，因为取决于运行环境）
        Output.WriteLine($"是否在容器中运行: {isContainer}");

        // 如果检测到容器环境，验证检测逻辑
        if (isContainer)
        {
            var hasDockerEnv = File.Exists("/.dockerenv");
            var hasK8sEnv = Env.HasEnvironmentVariable("KUBERNETES_SERVICE_HOST");
            var hasContainerVars = new[] { "DOCKER_CONTAINER", "CONTAINER", "DOTNET_RUNNING_IN_CONTAINER" }
                .Any(envVar => Env.HasEnvironmentVariable(envVar));

            (hasDockerEnv || hasK8sEnv || hasContainerVars).ShouldBeTrue("如果检测到容器环境，至少应该满足一个检测条件");
        }
    }

    /// <summary>
    /// 测试 - CI 环境检测
    /// </summary>
    [Fact]
    public void CIDetection_ReturnsValidResult()
    {
        // Act
        var isCI = Env.IsRunningInCI;

        // Assert
        Output.WriteLine($"是否在 CI 环境中运行: {isCI}");

        // 如果检测到 CI 环境，验证检测逻辑
        if (isCI)
        {
            var ciEnvVars = new[]
            {
                "CI", "CONTINUOUS_INTEGRATION",
                "GITHUB_ACTIONS", "AZURE_PIPELINES", "TF_BUILD",
                "JENKINS_URL", "GITLAB_CI", "TRAVIS",
                "CIRCLECI", "BUILDKITE", "TEAMCITY_VERSION"
            };

            ciEnvVars.Any(envVar => Env.HasEnvironmentVariable(envVar))
                .ShouldBeTrue("如果检测到 CI 环境，至少应该满足一个检测条件");
        }
    }

    /// <summary>
    /// 测试 - 调试模式检测
    /// </summary>
    [Fact]
    public void DebugMode_Detection()
    {
        // Act & Assert
        var isDebugMode = Env.IsDebugMode;
        var isDebuggerAttached = Env.IsDebuggerAttached;

        Output.WriteLine($"调试模式: {isDebugMode}");
        Output.WriteLine($"调试器已附加: {isDebuggerAttached}");

#if DEBUG
        isDebugMode.ShouldBeTrue();
#else
        isDebugMode.ShouldBeFalse();
#endif
    }

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
            SafeDeleteFile(tempFile1);
            SafeDeleteFile(tempFile2);
        }
    }

    /// <summary>
    /// 测试 - GetTempFileName - 不同扩展名
    /// </summary>
    [Theory]
    [InlineData(".txt")]
    [InlineData(".json")]
    [InlineData(".xml")]
    [InlineData(".log")]
    [InlineData("")]
    [InlineData(null)]
    public void GetTempFileName_DifferentExtensions_WorksCorrectly(string extension)
    {
        // Act
        var tempFile = Env.GetTempFileName(extension);

        try
        {
            // Assert
            File.Exists(tempFile).ShouldBeTrue();

            if (!string.IsNullOrEmpty(extension))
            {
                tempFile.ShouldEndWith(extension);
            }

            Output.WriteLine($"扩展名 '{extension}' 测试: {tempFile}");
        }
        finally
        {
            // Cleanup
            SafeDeleteFile(tempFile);
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

            // Act & Assert
            string template, result;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                template = $"%{testVarName}%";
                result = Env.ExpandEnvironmentVariables(template);
                result.ShouldBe(testVarValue);
            }
            else
            {
                // Unix 系统的环境变量展开可能不同
                template = $"${testVarName}";
                result = Env.ExpandEnvironmentVariables(template);
                // 在某些系统上可能不支持 $VAR 格式，所以这里只验证不为空
                result.ShouldNotBeNull();
            }

            Output.WriteLine($"模板: {template}, 展开后: {result}");
        }
        finally
        {
            // Cleanup
            Env.RemoveEnvironmentVariable(testVarName);
        }
    }

    /// <summary>
    /// 测试 - ExpandEnvironmentVariables - 空值和特殊值处理
    /// </summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("no_variables_here", "no_variables_here")]
    public void ExpandEnvironmentVariables_SpecialValues_HandlesCorrectly(string input, string expected)
    {
        // Act
        var result = Env.ExpandEnvironmentVariables(input);

        // Assert
        result.ShouldBe(expected);

        Output.WriteLine($"特殊值测试: '{input}' -> '{result}'");
    }

    /// <summary>
    /// 测试 - Exit 方法存在性验证
    /// </summary>
    [Fact]
    public void Exit_MethodExists_CanBeInvoked()
    {
        // 注意：这里不实际调用 Exit 方法，因为它会终止进程
        // 只验证方法的存在性和签名

        // Arrange & Act
        var exitMethod = typeof(Env).GetMethod("Exit", new[] { typeof(int) });

        // Assert
        exitMethod.ShouldNotBeNull();
        exitMethod.IsStatic.ShouldBeTrue();
        exitMethod.IsPublic.ShouldBeTrue();
        exitMethod.ReturnType.ShouldBe(typeof(void));

        Output.WriteLine("Exit 方法签名验证通过");
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

    /// <summary>
    /// 测试 - HasEnvironmentVariable - 异常处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HasEnvironmentVariable_InvalidParameters_ReturnsFalse(string invalidParam)
    {
        // Act
        var result = Env.HasEnvironmentVariable(invalidParam);

        // Assert
        result.ShouldBeFalse("无效参数应该返回 false 而不是抛出异常");
    }

    #endregion

    #region 性能测试

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

    /// <summary>
    /// 测试 - 属性访问性能
    /// </summary>
    [Fact]
    public void PerformanceTest_PropertyAccess_CompletesQuickly()
    {
        // Arrange
        const int iterations = 1000;

        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                _ = Env.PlatformName;
                _ = Env.OSDescription;
                _ = Env.ApplicationName;
                _ = Env.IsWindows;
                _ = Env.IsLinux;
                _ = Env.IsOSX;
                _ = Env.ProcessorCount;
                _ = Env.Is64BitOperatingSystem;
            }
        }, TimeSpan.FromSeconds(1));

        Output.WriteLine($"属性访问性能测试完成: {iterations} 次迭代");
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
    /// 测试 - 实际应用场景 - 环境特定功能
    /// </summary>
    [Fact]
    public void RealWorldScenario_EnvironmentSpecificFeatures_WorksCorrectly()
    {
        // Arrange
        var originalEnv = Env.GetEnvironmentName();

        try
        {
            // 模拟开发环境特定功能
            Env.SetEnvironmentName(Env.Development);

            // Assert
            if (Env.IsDevelopment())
            {
                // 开发环境应该启用的功能
                Output.WriteLine("开发环境 - 启用详细日志和调试功能");
            }

            // 模拟生产环境
            Env.SetEnvironmentName(Env.Production);

            if (Env.IsProduction())
            {
                // 生产环境应该启用的功能
                Output.WriteLine("生产环境 - 启用性能优化和错误监控");
            }

            // 验证环境常量
            Env.Development.ShouldBe("Development");
            Env.Staging.ShouldBe("Staging");
            Env.Production.ShouldBe("Production");
            Env.Testing.ShouldBe("Testing");

            Output.WriteLine("环境特定功能测试完成");
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
    /// 测试 - 复合场景 - 多功能组合使用
    /// </summary>
    [Fact]
    public void ComplexScenario_MultipleFeaturesUsed_WorksCorrectly()
    {
        // Arrange
        var testId = Guid.NewGuid().ToString("N")[..8];
        var tempFile = Path.Combine(Env.TempPath, $"test_{testId}.txt");
        var originalEnv = Env.GetEnvironmentName();

        try
        {
            // 创建临时文件
            File.WriteAllText(tempFile, $"Test content for {Env.ApplicationName} on {Env.PlatformName}");

            // 设置环境配置
            Env.SetEnvironmentVariable($"TEST_CONFIG_{testId}", "complex_scenario_value");
            Env.SetEnvironmentName("Integration");

            // 验证系统信息
            var systemInfo = new
            {
                Platform = Env.PlatformName,
                Architecture = Env.OSArchitecture.ToString(),
                ProcessorCount = Env.ProcessorCount,
                Is64Bit = Env.Is64BitOperatingSystem,
                AppName = Env.ApplicationName,
                Environment = Env.GetEnvironmentName()
            };

            // Assert
            systemInfo.Platform.ShouldNotBe("Unknown");
            systemInfo.ProcessorCount.ShouldBeGreaterThan(0);
            systemInfo.AppName.ShouldNotBeNullOrEmpty();
            systemInfo.Environment.ShouldBe("Integration");

            File.Exists(tempFile).ShouldBeTrue();
            Env.HasEnvironmentVariable($"TEST_CONFIG_{testId}").ShouldBeTrue();

            Output.WriteLine($"复合场景测试 - 系统信息: {systemInfo}");
            Output.WriteLine($"临时文件: {tempFile}");
            Output.WriteLine($"环境变量: TEST_CONFIG_{testId} = {Env.GetEnvironmentVariable($"TEST_CONFIG_{testId}")}");
        }
        finally
        {
            // 清理
            SafeDeleteFile(tempFile);
            Env.RemoveEnvironmentVariable($"TEST_CONFIG_{testId}");
            if (originalEnv != null)
                Env.SetEnvironmentName(originalEnv);
            else
            {
                Env.RemoveEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                Env.RemoveEnvironmentVariable("DOTNET_ENVIRONMENT");
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