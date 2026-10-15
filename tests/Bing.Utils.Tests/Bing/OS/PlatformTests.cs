#pragma warning disable CS0618
using System.Runtime.InteropServices;
using Bing.OS;

namespace Bing.Utils.Tests.Bing.OS;

/// <summary>
/// <see cref="Platform"/>（已过时）单元测试
/// </summary>
public class PlatformTests
{
    // ─────────────────────────────────────────────────────────────────
    // GetOSPlatform
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证操作系统标识属于已知名称或空文本。
    /// </summary>
    [Fact]
    public void GetOSPlatform_ReturnsKnownStringOrEmpty()
    {
        var result = Platform.GetOSPlatform();
        var valid = new[] { "Windows", "Linux", "OSX", string.Empty };
        valid.ShouldContain(result);
    }

    /// <summary>
    /// 验证 Windows 环境返回对应系统标识。
    /// </summary>
    [Fact]
    public void GetOSPlatform_OnWindows_ReturnsWindows()
    {
        if (!Platform.IsWindows) return; // 仅 Windows 上验证
        Platform.GetOSPlatform().ShouldBe("Windows");
    }

    // ─────────────────────────────────────────────────────────────────
    // IsXxx 平台属性
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证 Windows 平台标记为布尔值。
    /// </summary>
    [Fact]
    public void IsWindows_ReturnsBool()
    {
        // 只验证返回布尔类型，不假设具体值
        Platform.IsWindows.ShouldBeOfType<bool>();
    }

    /// <summary>
    /// 验证 Linux 平台标记为布尔值。
    /// </summary>
    [Fact]
    public void IsLinux_ReturnsBool()
    {
        Platform.IsLinux.ShouldBeOfType<bool>();
    }

    /// <summary>
    /// 验证 macOS 平台标记为布尔值。
    /// </summary>
    [Fact]
    public void IsOSX_ReturnsBool()
    {
        Platform.IsOSX.ShouldBeOfType<bool>();
    }

    /// <summary>
    /// 验证三个受支持平台标记中恰有一个为真。
    /// </summary>
    [Fact]
    public void ExactlyOnePlatformFlag_IsTrue()
    {
        var trueCount = new[] { Platform.IsWindows, Platform.IsLinux, Platform.IsOSX }
            .Count(x => x);
        trueCount.ShouldBe(1);
    }

    // ─────────────────────────────────────────────────────────────────
    // 系统信息属性
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证计算机名称不为空。
    /// </summary>
    [Fact]
    public void MachineName_IsNotEmpty()
    {
        Platform.MachineName.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证操作系统描述不为空。
    /// </summary>
    [Fact]
    public void OSDescription_IsNotEmpty()
    {
        Platform.OSDescription.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证操作系统版本不为空。
    /// </summary>
    [Fact]
    public void OSVersion_IsNotEmpty()
    {
        Platform.OSVersion.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证运行时框架描述不为空。
    /// </summary>
    [Fact]
    public void FrameworkDescription_IsNotEmpty()
    {
        Platform.FrameworkDescription.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证操作系统体系结构属于测试列举的架构。
    /// </summary>
    [Fact]
    public void OSArchitecture_IsValidArchitecture()
    {
        var valid = new[] { Architecture.X86, Architecture.X64, Architecture.Arm, Architecture.Arm64 };
        valid.ShouldContain(Platform.OSArchitecture);
    }

    /// <summary>
    /// 验证进程体系结构属于测试列举的架构。
    /// </summary>
    [Fact]
    public void ProcessArchitecture_IsValidArchitecture()
    {
        var valid = new[] { Architecture.X86, Architecture.X64, Architecture.Arm, Architecture.Arm64 };
        valid.ShouldContain(Platform.ProcessArchitecture);
    }

    /// <summary>
    /// 验证当前目录不为空。
    /// </summary>
    [Fact]
    public void CurrentDirectory_IsNotEmpty()
    {
        Platform.CurrentDirectory.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证应用根目录不为空。
    /// </summary>
    [Fact]
    public void AppRoot_IsNotEmpty()
    {
        Platform.AppRoot.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证应用名称不为空。
    /// </summary>
    [Fact]
    public void ApplicationName_IsNotEmpty()
    {
        Platform.ApplicationName.ShouldNotBeNullOrEmpty();
    }

    // ─────────────────────────────────────────────────────────────────
    // GetPhysicalPath
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证物理路径转换拒绝空路径。
    /// </summary>
    [Fact]
    public void GetPhysicalPath_NullPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Platform.GetPhysicalPath(null!));
    }

    /// <summary>
    /// 验证相对路径转换为根路径。
    /// </summary>
    [Fact]
    public void GetPhysicalPath_RelativePath_ReturnsRootedPath()
    {
        var result = Platform.GetPhysicalPath("test/file.txt");
        Path.IsPathRooted(result).ShouldBeTrue();
    }

    /// <summary>
    /// 验证波浪号前缀不改变相对路径的转换结果。
    /// </summary>
    [Fact]
    public void GetPhysicalPath_TildePrefix_StripsTildeAndReturnsSamePath()
    {
        var withTilde = Platform.GetPhysicalPath("~/sub/file.txt");
        var without = Platform.GetPhysicalPath("sub/file.txt");
        withTilde.ShouldBe(without);
    }

    /// <summary>
    /// 验证斜杠前缀不改变相对路径的转换结果。
    /// </summary>
    [Fact]
    public void GetPhysicalPath_SlashPrefix_StripsSlashAndReturnsSamePath()
    {
        var withSlash = Platform.GetPhysicalPath("/sub/file.txt");
        var without = Platform.GetPhysicalPath("sub/file.txt");
        withSlash.ShouldBe(without);
    }

    /// <summary>
    /// 验证物理路径采用指定基准路径。
    /// </summary>
    [Fact]
    public void GetPhysicalPath_CustomBasePath_UsesThatBase()
    {
        var customBase = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var result = Platform.GetPhysicalPath("file.txt", customBase);
        result.ShouldStartWith(customBase);
    }

    /// <summary>
    /// 验证空相对路径仍转换为根路径。
    /// </summary>
    [Fact]
    public void GetPhysicalPath_EmptyRelativePath_ReturnsCombinedPath()
    {
        var result = Platform.GetPhysicalPath(string.Empty);
        Path.IsPathRooted(result).ShouldBeTrue();
    }
}
