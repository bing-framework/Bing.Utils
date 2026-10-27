using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bing.CommandLine;

[Collection("TimingSensitive")]
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

    /// <summary>先确认父子进程就绪，再验证父进程退出后无需等待子进程释放继承的管道。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_WhenExitedParentLeavesInheritedPipes_DoesNotWaitForChild(bool requireMatch)
    {
        var directory = CreateTemporaryDirectory();
        var pidFile = Path.Combine(directory, "descendant.pid");
        var parentPidFile = Path.Combine(directory, "parent.pid");
        var readyFile = Path.Combine(directory, "ready");
        var childReadyFile = Path.Combine(directory, "child-ready");
        var exitFile = Path.Combine(directory, "exit-parent");
        var releaseFile = Path.Combine(directory, "release-child");
        // 45 秒仅为整个夹具的 watchdog；待测退出行为仍受原有 5 秒限制。
        var executor = CreateExecutor(GetSpawnChildAndExitScript(), TimeSpan.FromSeconds(45))
            .Env(PidFileEnvironmentVariable, pidFile)
            .Env("BING_TEST_PARENT_PID", parentPidFile)
            .Env("BING_TEST_READY", readyFile)
            .Env("BING_TEST_CHILD_READY", childReadyFile)
            .Env("BING_TEST_EXIT", exitFile)
            .Env("BING_TEST_RELEASE", releaseFile);
        if (requireMatch)
            executor.OutputToMatch("NEVER_MATCHED");
        // Execute 是同步 API，使用独立线程避免占用测试框架的线程池工作线程。
        long completedTimestamp = 0;
        var execution = Task.Factory.StartNew(() =>
        {
            try { return executor.Execute(); }
            finally { Interlocked.Exchange(ref completedTimestamp, Stopwatch.GetTimestamp()); }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        try
        {
            var startup = Stopwatch.StartNew();
            while (!File.Exists(readyFile))
            {
                if (execution.IsCompleted)
                {
                    using var unexpected = await execution;
                    Assert.Fail("夹具在报告就绪之前退出。");
                }
                Assert.True(startup.Elapsed < TimeSpan.FromSeconds(30), "父子进程未在启动预算内就绪。");
                await Task.Delay(20);
            }
            var childId = int.Parse(File.ReadAllText(pidFile, Encoding.UTF8));
            Assert.True(IsProcessRunning(childId));
            var exitTimestamp = Stopwatch.GetTimestamp();
            File.WriteAllText(exitFile, string.Empty, Encoding.UTF8);
            var completed = await Task.WhenAny(execution, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.True(ReferenceEquals(execution, completed),
                $"父进程收到退出许可后未在 5 秒内完成：任务状态 {execution.Status}，" +
                $"父进程存活 {IsProcessRunning(int.Parse(File.ReadAllText(parentPidFile, Encoding.UTF8)))}，" +
                $"子进程存活 {IsProcessRunning(childId)}。");
            if (requireMatch)
            {
                var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    using var unexpected = await execution;
                });
                Assert.Contains("未匹配预期输出", error.Message);
            }
            else
            {
                using var process = await execution;
                Assert.True(process.HasExited);
                Assert.Equal(0, process.ExitCode);
            }
            // 以执行线程的完成时刻计量，不把测试 continuation 恢复延迟算入退出耗时。
            var exitSeconds = (Interlocked.Read(ref completedTimestamp) - exitTimestamp) / (double)Stopwatch.Frequency;
            Assert.InRange(exitSeconds, 0, 5);
            // 子进程尚未收到释放信号，退出结果必须独立于其管道 EOF。
            Assert.True(IsProcessRunning(childId));
        }
        finally
        {
            File.WriteAllText(exitFile, string.Empty, Encoding.UTF8);
            File.WriteAllText(releaseFile, string.Empty, Encoding.UTF8);
            StopProcessByPidFile(parentPidFile);
            StopProcessByPidFile(pidFile);
            // 回收正在执行的调用并观察异常；不能让夹具越过本测试继续运行。
            var finished = await Task.WhenAny(execution, Task.Delay(TimeSpan.FromSeconds(45)));
            Assert.Same(execution, finished);
            if (execution.Status == TaskStatus.RanToCompletion)
                execution.Result.Dispose();
            else
                await Record.ExceptionAsync(async () => { await execution; });
            // 处理启动较慢、在首次清理之后才写出的 PID。
            StopProcessByPidFile(parentPidFile);
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

    /// <summary>子进程显式持有 stdout/stderr，收到释放信号才退出；父进程就绪后等待退出许可。</summary>
    private static string GetSpawnChildAndExitScript()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "printf '%s' \"$$\" > \"$BING_TEST_PARENT_PID\"; " +
                "(printf 'CHILD_STDOUT\\n'; printf 'CHILD_STDERR\\n' >&2; " +
                ": > \"$BING_TEST_CHILD_READY\"; while [ ! -f \"$BING_TEST_RELEASE\" ]; do sleep 0.05; done) & " +
                "printf '%s' \"$!\" > \"$BING_UTILS_TEST_PID_FILE\"; " +
                "while [ ! -f \"$BING_TEST_CHILD_READY\" ]; do sleep 0.05; done; " +
                ": > \"$BING_TEST_READY\"; while [ ! -f \"$BING_TEST_EXIT\" ]; do sleep 0.05; done; exit 0";
        const string child = "[Console]::Out.WriteLine('CHILD_STDOUT'); [Console]::Out.Flush(); " +
            "[Console]::Error.WriteLine('CHILD_STDERR'); [Console]::Error.Flush(); " +
            "[IO.File]::WriteAllText($env:BING_TEST_CHILD_READY, '', [Text.Encoding]::UTF8); " +
            "while (!(Test-Path -LiteralPath $env:BING_TEST_RELEASE)) { Start-Sleep -Milliseconds 20 }";
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(child));
        return "[IO.File]::WriteAllText($env:BING_TEST_PARENT_PID, [string]$PID, [Text.Encoding]::UTF8); " +
            "$info = [Diagnostics.ProcessStartInfo]::new('powershell.exe', " +
            "'-NoLogo -NoProfile -NonInteractive -EncodedCommand " + encoded + "'); " +
            "$info.UseShellExecute = $false; $info.CreateNoWindow = $true; " +
            "$child = [Diagnostics.Process]::Start($info); " +
            "[IO.File]::WriteAllText($env:BING_UTILS_TEST_PID_FILE, [string]$child.Id, [Text.Encoding]::UTF8); " +
            "while (!(Test-Path -LiteralPath $env:BING_TEST_CHILD_READY)) { Start-Sleep -Milliseconds 20 }; " +
            "[IO.File]::WriteAllText($env:BING_TEST_READY, '', [Text.Encoding]::UTF8); " +
            "while (!(Test-Path -LiteralPath $env:BING_TEST_EXIT)) { Start-Sleep -Milliseconds 20 }; [Environment]::Exit(0)";
    }

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
        catch (InvalidOperationException)
        {
            // 进程可能在 HasExited 检查之后、Kill 之前自行退出。
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
