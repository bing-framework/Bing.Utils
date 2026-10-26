using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Bing.CommandLine;

public class CommandLineExecutorRegressionTest
{
    private const string PidFileEnvironmentVariable = "BING_UTILS_TEST_PID_FILE";
    private const string MatchEnvironmentVariable = "BING_UTILS_TEST_MATCH";

    [Fact]
    public void Execute_WithoutOutputCondition_WaitsForSuccessfulExit()
    {
        using var process = CreateExecutor(GetExitScript()).Execute();

        Assert.True(process.HasExited);
        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public void Execute_WhenOutputMatches_ReturnsWhileProcessIsRunning()
    {
        var executor = CreateExecutor(GetReadyAndSleepScript())
            .OutputToMatch("READY");

        var process = executor.Execute();
        try
        {
            Assert.False(process.HasExited);
        }
        finally
        {
            StopProcess(process);
        }
    }

    [Fact]
    public void Execute_WhenProcessExitsWithoutMatchingOutput_FailsPromptly()
    {
        var executor = CreateExecutor(GetOtherOutputScript(), TimeSpan.FromSeconds(10))
            .OutputToMatch("EXPECTED");
        var stopwatch = Stopwatch.StartNew();

        Assert.Throws<InvalidOperationException>(() => executor.Execute());

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(8));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Execute_WhenExitedParentLeavesInheritedPipes_DoesNotWaitForChild(bool requireMatch)
    {
        var directory = CreateTemporaryDirectory();
        var pidFile = Path.Combine(directory, "descendant.pid");
        var executor = CreateExecutor(GetSpawnChildAndExitScript(), TimeSpan.FromSeconds(6))
            .Env(PidFileEnvironmentVariable, pidFile);
        if (requireMatch)
            executor.OutputToMatch("NEVER_MATCHED");
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (requireMatch)
            {
                var error = Assert.Throws<InvalidOperationException>(() => executor.Execute());
                Assert.Contains("未匹配预期输出", error.Message);
            }
            else
            {
                using var process = executor.Execute();
                Assert.True(process.HasExited);
                Assert.Equal(0, process.ExitCode);
            }
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5));
            // 后代进程由测试显式清理；父进程退出不代表它创建的服务也应被结束。
            Assert.True(IsProcessRunning(int.Parse(File.ReadAllText(pidFile, Encoding.UTF8))));
        }
        finally
        {
            StopProcessByPidFile(pidFile);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Execute_WhenMatchIsLastUnterminatedLine_ReceivesTailBeforeReportingExit()
    {
        var script = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "[Console]::Out.Write('TAIL_READY'); [Console]::Out.Flush(); [Environment]::Exit(0)"
            : "printf TAIL_READY; exit 0";
        using var process = CreateExecutor(script).OutputToMatch("TAIL_READY").Execute();
        Assert.True(process.WaitForExit(5000));
    }

    [Fact]
    public void Execute_WhenTimedOut_StopsTheStartedProcess()
    {
        var directory = CreateTemporaryDirectory();
        var pidFile = Path.Combine(directory, "child.pid");
        var executor = CreateExecutor(GetWritePidAndSleepScript(), TimeSpan.FromSeconds(2))
            .Env(PidFileEnvironmentVariable, pidFile);

        try
        {
            Assert.Throws<TimeoutException>(() => executor.Execute());

            Assert.True(File.Exists(pidFile));
            var processId = int.Parse(File.ReadAllText(pidFile, Encoding.UTF8));
            Assert.False(IsProcessRunning(processId));
        }
        finally
        {
            StopProcessByPidFile(pidFile);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void Execute_SucceedsWhenEitherOrBothOutputStreamsAreNotRedirected(
        bool redirectStandardOutput,
        bool redirectStandardError)
    {
        var executor = CreateExecutor(GetWriteBothStreamsAndExitScript())
            .RedirectStandardOutput(redirectStandardOutput)
            .RedirectStandardError(redirectStandardError);

        using var process = executor.Execute();

        Assert.True(process.HasExited);
        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public void Execute_ReusingExecutor_DoesNotReusePreviousMatchSignal()
    {
        var executor = CreateExecutor(GetConditionalReadyScript(), TimeSpan.FromSeconds(10))
            .OutputToMatch("READY")
            .Env(MatchEnvironmentVariable, "yes");

        var firstProcess = executor.Execute();
        try
        {
            Assert.False(firstProcess.HasExited);
        }
        finally
        {
            StopProcess(firstProcess);
        }

        executor.Env(MatchEnvironmentVariable, "no");
        var stopwatch = Stopwatch.StartNew();

        Assert.Throws<InvalidOperationException>(() => executor.Execute());

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(8));
    }

    [Fact]
    public void OutputToMatch_RejectsNullBeforeStartingAsyncCallbacks()
    {
        var executor = new CommandLineExecutor();
        Assert.Throws<ArgumentNullException>(() => executor.OutputToMatch(null));
        Assert.Throws<ArgumentException>(() => executor.OutputToMatch("ready", null));
    }

    private static CommandLineExecutor CreateExecutor(string script, TimeSpan? timeout = null)
    {
        var executor = new CommandLineExecutor()
            .Timeout(timeout ?? TimeSpan.FromSeconds(10));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            return executor
                .Command("powershell.exe")
                .Arguments("-NoLogo", "-NoProfile", "-NonInteractive", "-EncodedCommand", encodedScript);
        }

        return executor
            .Command("/bin/sh")
            .Arguments("-c", QuoteProcessArgument(script));
    }

    private static string QuoteProcessArgument(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private static string GetExitScript() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "[Environment]::Exit(0)"
        : "exit 0";

    private static string GetSpawnChildAndExitScript() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "$info = [Diagnostics.ProcessStartInfo]::new('powershell.exe', '-NoLogo -NoProfile -NonInteractive -Command Start-Sleep -Seconds 30'); " +
          "$info.UseShellExecute = $false; $info.CreateNoWindow = $true; " +
          "$child = [Diagnostics.Process]::Start($info); " +
          "[IO.File]::WriteAllText($env:BING_UTILS_TEST_PID_FILE, [string]$child.Id, [Text.Encoding]::UTF8); [Environment]::Exit(0)"
        : "sleep 30 & printf '%s' \"$!\" > \"$BING_UTILS_TEST_PID_FILE\"; exit 0";

    private static string GetReadyAndSleepScript() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "[Console]::Out.WriteLine('READY'); [Console]::Out.Flush(); Start-Sleep -Seconds 30"
        : "echo READY; exec sleep 30";

    private static string GetOtherOutputScript() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "[Console]::Out.WriteLine('OTHER')"
        : "echo OTHER";

    private static string GetWritePidAndSleepScript() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "[IO.File]::WriteAllText($env:BING_UTILS_TEST_PID_FILE, [string]$PID, [Text.Encoding]::UTF8); Start-Sleep -Seconds 30"
        : "printf '%s' \"$$\" > \"$BING_UTILS_TEST_PID_FILE\"; exec sleep 30";

    private static string GetWriteBothStreamsAndExitScript() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "[Console]::Out.WriteLine('stdout'); [Console]::Error.WriteLine('stderr'); [Environment]::Exit(0)"
        : "echo stdout; echo stderr >&2; exit 0";

    private static string GetConditionalReadyScript() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "if ($env:BING_UTILS_TEST_MATCH -eq 'yes') { [Console]::Out.WriteLine('READY'); [Console]::Out.Flush(); Start-Sleep -Seconds 30 } else { [Console]::Out.WriteLine('OTHER') }"
        : "if [ \"$BING_UTILS_TEST_MATCH\" = yes ]; then echo READY; exec sleep 30; else echo OTHER; fi";

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "Bing.Utils.CommandLineExecutor." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void StopProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            process.Dispose();
        }
    }

    private static void StopProcessByPidFile(string pidFile)
    {
        if (!File.Exists(pidFile))
            return;

        if (!int.TryParse(File.ReadAllText(pidFile, Encoding.UTF8), out var processId))
            return;

        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        catch (ArgumentException)
        {
        }
    }

    private static bool IsProcessRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
