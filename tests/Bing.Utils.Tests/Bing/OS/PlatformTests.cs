#pragma warning disable CS0618
using Bing.OS;
using System.Runtime.InteropServices;

namespace Bing.Utils.Tests.Bing.OS;

/// <summary>
/// <see cref="Platform"/>（已过时）单元测试
/// </summary>
public class PlatformTests
{
    // ─────────────────────────────────────────────────────────────────
    // GetOSPlatform
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GetOSPlatform_ReturnsKnownStringOrEmpty()
    {
        var result = Platform.GetOSPlatform();
        var valid = new[] { "Windows", "Linux", "OSX", string.Empty };
        valid.ShouldContain(result);
    }

    [Fact]
    public void GetOSPlatform_OnWindows_ReturnsWindows()
    {
        if (!Platform.IsWindows) return; // 仅 Windows 上验证
        Platform.GetOSPlatform().ShouldBe("Windows");
    }

    // ─────────────────────────────────────────────────────────────────
    // IsXxx 平台属性
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IsWindows_ReturnsBool()
    {
        // 只验证返回布尔类型，不假设具体值
        Platform.IsWindows.ShouldBeOfType<bool>();
    }

    [Fact]
    public void IsLinux_ReturnsBool()
    {
        Platform.IsLinux.ShouldBeOfType<bool>();
    }

    [Fact]
    public void IsOSX_ReturnsBool()
    {
        Platform.IsOSX.ShouldBeOfType<bool>();
    }

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

    [Fact]
    public void MachineName_IsNotEmpty()
    {
        Platform.MachineName.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void OSDescription_IsNotEmpty()
    {
        Platform.OSDescription.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void OSVersion_IsNotEmpty()
    {
        Platform.OSVersion.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void FrameworkDescription_IsNotEmpty()
    {
        Platform.FrameworkDescription.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void OSArchitecture_IsValidArchitecture()
    {
        var valid = new[] { Architecture.X86, Architecture.X64, Architecture.Arm, Architecture.Arm64 };
        valid.ShouldContain(Platform.OSArchitecture);
    }

    [Fact]
    public void ProcessArchitecture_IsValidArchitecture()
    {
        var valid = new[] { Architecture.X86, Architecture.X64, Architecture.Arm, Architecture.Arm64 };
        valid.ShouldContain(Platform.ProcessArchitecture);
    }

    [Fact]
    public void CurrentDirectory_IsNotEmpty()
    {
        Platform.CurrentDirectory.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void AppRoot_IsNotEmpty()
    {
        Platform.AppRoot.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ApplicationName_IsNotEmpty()
    {
        Platform.ApplicationName.ShouldNotBeNullOrEmpty();
    }

    // ─────────────────────────────────────────────────────────────────
    // GetPhysicalPath
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GetPhysicalPath_NullPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Platform.GetPhysicalPath(null!));
    }

    [Fact]
    public void GetPhysicalPath_RelativePath_ReturnsRootedPath()
    {
        var result = Platform.GetPhysicalPath("test/file.txt");
        Path.IsPathRooted(result).ShouldBeTrue();
    }

    [Fact]
    public void GetPhysicalPath_TildePrefix_StripsTildeAndReturnsSamePath()
    {
        var withTilde = Platform.GetPhysicalPath("~/sub/file.txt");
        var without = Platform.GetPhysicalPath("sub/file.txt");
        withTilde.ShouldBe(without);
    }

    [Fact]
    public void GetPhysicalPath_SlashPrefix_StripsSlashAndReturnsSamePath()
    {
        var withSlash = Platform.GetPhysicalPath("/sub/file.txt");
        var without = Platform.GetPhysicalPath("sub/file.txt");
        withSlash.ShouldBe(without);
    }

    [Fact]
    public void GetPhysicalPath_CustomBasePath_UsesThatBase()
    {
        var customBase = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var result = Platform.GetPhysicalPath("file.txt", customBase);
        result.ShouldStartWith(customBase);
    }

    [Fact]
    public void GetPhysicalPath_EmptyRelativePath_ReturnsCombinedPath()
    {
        var result = Platform.GetPhysicalPath(string.Empty);
        Path.IsPathRooted(result).ShouldBeTrue();
    }
}
