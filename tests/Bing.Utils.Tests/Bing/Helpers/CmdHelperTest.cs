using System.ComponentModel;
namespace Bing.Helpers;
/// <summary>
/// 测试类：覆盖 `CmdHelper` 相关行为。
/// </summary>
[Trait("Bing.Helpers", "CmdHelper")]
public class CmdHelperTest
{
    /// <summary>
    /// 测试用例：验证 `Run` 在 `InvalidCmdPath` 场景下，结果为 `ThrowsWin32Exception`。
    /// </summary>
    [Fact]
    public void Run_InvalidCmdPath_ThrowsWin32Exception()
    {
        Should.Throw<Win32Exception>(() => CmdHelper.Run("echo test", "not-exists-cmd-path.exe"));
    }
    /// <summary>
    /// 测试用例：验证 `Run` 在 `OnWindows` 场景下，结果为 `EchoCommandContainsExpectedOutput`。
    /// </summary>
    [Fact]
    public void Run_OnWindows_EchoCommandContainsExpectedOutput()
    {
        if (!Env.IsWindows)
        {
            Env.IsWindows.ShouldBeFalse();
            return;
        }
        var output = CmdHelper.Run("echo bing-utils-cmd-test");
        output.ShouldContain("bing-utils-cmd-test");
    }
    /// <summary>
    /// 测试用例：验证 `Bash` 在 `Command_WhenNative` 场景下，结果为 `CanExecuteSimpleEcho`。
    /// </summary>
    [Fact]
    public void Bash_Command_WhenNative_CanExecuteSimpleEcho()
    {
        if (!CmdHelper.Bash.Native)
        {
            CmdHelper.Bash.Native.ShouldBeFalse();
            return;
        }
        var result = new CmdHelper.Bash().Command("echo bing-utils-bash-test");
        result.ExitCode.ShouldBe(0);
        result.Output.ShouldContain("bing-utils-bash-test");
    }
    /// <summary>
    /// 测试用例：验证 `Bash` 在 `Subsystem_WhenNative` 场景下，结果为 `ShouldAlwaysBeFalse`。
    /// </summary>
    [Fact]
    public void Bash_Subsystem_WhenNative_ShouldAlwaysBeFalse()
    {
        if (CmdHelper.Bash.Native)
            CmdHelper.Bash.Subsystem.ShouldBeFalse();
    }
}

