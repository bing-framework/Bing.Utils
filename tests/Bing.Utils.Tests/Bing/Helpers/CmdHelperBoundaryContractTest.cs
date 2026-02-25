namespace Bing.Helpers;
/// <summary>
/// 测试类：覆盖 `CmdHelperBoundaryContract` 相关行为。
/// </summary>
[Trait("Bing.Helpers", "CmdHelper.Boundary")]
public class CmdHelperBoundaryContractTest
{
    /// <summary>
    /// 测试用例：验证 `Bash` 在 `Command_RedirectFalse` 场景下，结果为 `ReturnsNullOutputAndError`。
    /// </summary>
    [Fact]
    public void Bash_Command_RedirectFalse_ReturnsNullOutputAndError()
    {
        if (!CmdHelper.Bash.Native)
        {
            CmdHelper.Bash.Native.ShouldBeFalse();
            return;
        }
        var result = new CmdHelper.Bash().Command("echo bing-utils-bash-redirect-false", redirect: false);
        result.ExitCode.ShouldBe(0);
        result.Output.ShouldBeNull();
        result.ErrorMsg.ShouldBeNull();
        result.Lines.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `Bash` 在 `Command_InvalidInput` 场景下，结果为 `ReturnsNonZeroExitCode`。
    /// </summary>
    [Fact]
    public void Bash_Command_InvalidInput_ReturnsNonZeroExitCode()
    {
        if (!CmdHelper.Bash.Native)
        {
            CmdHelper.Bash.Native.ShouldBeFalse();
            return;
        }
        var result = new CmdHelper.Bash().Command("command_that_should_not_exist_bing_utils");
        result.ExitCode.ShouldNotBe(0);
        result.ErrorMsg.ShouldNotBeNullOrWhiteSpace();
    }
    /// <summary>
    /// 测试用例：验证 `Shell` 在 `Command_MultiLineOutput` 场景下，结果为 `LinesContainsAllItems`。
    /// </summary>
    [Fact]
    public void Shell_Command_MultiLineOutput_LinesContainsAllItems()
    {
        if (!CmdHelper.Bash.Native)
        {
            CmdHelper.Bash.Native.ShouldBeFalse();
            return;
        }
        var result = CmdHelper.Shell("printf 'line1\\nline2\\n'");
        result.ExitCode.ShouldBe(0);
        result.Lines.ShouldNotBeNull();
        result.Lines.ShouldContain("line1");
        result.Lines.ShouldContain("line2");
    }
}

