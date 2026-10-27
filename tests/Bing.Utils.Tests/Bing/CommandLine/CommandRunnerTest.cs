using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bing.CommandLine;

/// <summary>
/// 结构化命令运行器回归测试。
/// </summary>
public class CommandRunnerTest
{
    /// <summary>
    /// 验证两个管道并行读取且输出按各自上限截断。
    /// </summary>
    [Fact]
    public async Task RunAsync_DrainsBothPipesAndMarksCapturedOutputTruncated()
    {
        var options = CreateScriptOptions(GetLargeOutputScript(), TimeSpan.FromSeconds(15));
        options.MaxStandardOutputChars = options.MaxStandardErrorChars = 2048;
        var result = await CommandRunner.RunAsync(options);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(2048, result.StandardOutput.Length);
        Assert.Equal(2048, result.StandardError.Length);
        Assert.True(result.StandardOutputTruncated);
        Assert.True(result.StandardErrorTruncated);
        Assert.True(result.OutputComplete);
        Assert.True(result.Duration >= TimeSpan.Zero);
    }

    /// <summary>
    /// 验证零捕获上限仍会持续读取，避免子进程因管道写满而阻塞。
    /// </summary>
    [Fact]
    public async Task RunAsync_WithZeroCaptureLimitStillDrainsOutput()
    {
        var options = CreateScriptOptions(GetLargeOutputScript(), TimeSpan.FromSeconds(15));
        options.MaxStandardOutputChars = 0;
        options.MaxStandardErrorChars = 0;

        var result = await CommandRunner.RunAsync(options);

        Assert.Equal(string.Empty, result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.True(result.StandardOutputTruncated);
        Assert.True(result.StandardErrorTruncated);
        Assert.True(result.OutputComplete);
    }

    /// <summary>
    /// 验证无输出的零退出进程返回完整结果。
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenNoOutputAndZeroExitCode_ReturnsCompleteResult()
    {
        var result = await CommandRunner.RunAsync(CreateScriptOptions(GetExitScript(0), TimeSpan.FromSeconds(10)));

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StandardOutput);
        Assert.Empty(result.StandardError);
        Assert.True(result.OutputComplete);
        Assert.Same(result, result.EnsureSuccess());
    }

    /// <summary>
    /// 验证非零退出码由结果承载，并由显式成功检查抛出。
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenExitCodeIsNonZero_ReturnsResultUntilEnsureSuccessIsCalled()
    {
        // 此用例仅验证退出码，使用系统轻量 shell，避免 PowerShell 冷启动影响断言。
        var windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var options = new CommandOptions
        {
            FileName = windows ? "cmd.exe" : "/bin/sh",
            // 由 stdin 提交内置命令，兼容 netstandard2.0 参数转义与 cmd 的特殊 /c 解析规则。
            Arguments = windows ? new[] { "/d", "/q" } : Array.Empty<string>(),
            StandardInput = (writer, _) => writer.WriteLineAsync("exit 7"),
            Timeout = TimeSpan.FromSeconds(10)
        };
        var result = await CommandRunner.RunAsync(options);

        Assert.Equal(7, result.ExitCode);
        Assert.Throws<InvalidOperationException>(() => result.EnsureSuccess());
    }

    /// <summary>
    /// 验证参数边界、空参数、引号、反斜杠和 Unicode 值不被合并或丢失。
    /// </summary>
    [Fact]
    public async Task RunAsync_PreservesArgumentBoundariesAndSpecialCharacters()
    {
        var arguments = new[] { "two words", string.Empty, "slash\\quote\"", "tail\\\\", "中文" };
        var options = CreateArgumentEchoOptions(arguments, out var temporaryScript);
        try
        {
            var result = await CommandRunner.RunAsync(options);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(arguments, DecodeArgumentLines(result.StandardOutput));
            Assert.True(result.OutputComplete);
        }
        finally
        {
            if (temporaryScript != null && File.Exists(temporaryScript))
                File.Delete(temporaryScript);
        }
    }

    /// <summary>
    /// 验证开始前取消会携带调用方的取消令牌。
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenTokenWasCancelledBeforeStart_ThrowsWithThatToken()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CommandRunner.RunAsync(CreateScriptOptions(GetSleepScript(), TimeSpan.FromSeconds(10)), cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    /// <summary>
    /// 验证执行中取消会停止子进程并传播调用方令牌。
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenCancelledDuringExecution_StopsProcessAndPropagatesToken()
    {
        using var cancellation = new CancellationTokenSource();
        var run = CommandRunner.RunAsync(CreateScriptOptions(GetSleepScript(), TimeSpan.FromSeconds(20)), cancellation.Token);
        await Task.Delay(150);
        cancellation.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await run);

        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    /// <summary>
    /// 验证达到超时时间会抛出超时异常。
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenTimedOut_ThrowsTimeoutException()
    {
        var options = CreateScriptOptions(GetSleepScript(), TimeSpan.FromMilliseconds(150));

        await Assert.ThrowsAsync<TimeoutException>(() => CommandRunner.RunAsync(options));
    }

    /// <summary>
    /// 验证可执行文件无法启动时保留平台启动异常。
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenExecutableCannotStart_PropagatesStartFailure()
    {
        var options = new CommandOptions
        {
            FileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing-executable"),
            Timeout = TimeSpan.FromSeconds(1),
        };

        await Assert.ThrowsAnyAsync<System.ComponentModel.Win32Exception>(() => CommandRunner.RunAsync(options));
    }

    /// <summary>
    /// 验证后代进程持有输出管道时按收尾时限返回不完整标记。
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenDescendantKeepsPipeOpen_StopsDrainingAtConfiguredLimit()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var directory = CreateTemporaryDirectory();
        var pidFile = Path.Combine(directory, "descendant.pid");
        var options = CreateScriptOptions("sleep 30 & printf '%s' \"$!\" > \"$BING_UTILS_COMMAND_RUNNER_PID_FILE\"; exit 0",
            TimeSpan.FromSeconds(10));
        options.EnvironmentVariables["BING_UTILS_COMMAND_RUNNER_PID_FILE"] = pidFile;
        options.OutputDrainTimeout = TimeSpan.FromMilliseconds(100);
        try
        {
            var result = await CommandRunner.RunAsync(options);

            Assert.Equal(0, result.ExitCode);
            Assert.False(result.OutputComplete);
            Assert.True(File.Exists(pidFile));
        }
        finally
        {
            StopProcessByPidFile(pidFile);
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// 验证无效限制在启动任何进程前被拒绝。
    /// </summary>
    [Fact]
    public async Task RunAsync_ValidatesOptionsBeforeStartingProcess()
    {
        var options = CreateScriptOptions(GetExitScript(0), TimeSpan.FromSeconds(10));
        options.MaxStandardOutputChars = -1;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CommandRunner.RunAsync(options));
    }

    /// <summary>
    /// 为当前平台创建执行固定脚本的受控命令配置。
    /// </summary>
    /// <param name="script">要执行的固定测试脚本。</param>
    /// <param name="timeout">测试进程的超时时间。</param>
    /// <returns>通过本机命令解释器执行脚本的配置。</returns>
    private static CommandOptions CreateScriptOptions(string script, TimeSpan timeout)
    {
        var options = new CommandOptions
        {
            Timeout = timeout,
            MaxStandardOutputChars = 65536,
            MaxStandardErrorChars = 65536,
        };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            options.FileName = "powershell.exe";
            options.Arguments = new List<string>
            {
                "-NoLogo", "-NoProfile", "-NonInteractive", "-EncodedCommand",
                Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
            };
        }
        else
        {
            options.FileName = "/bin/sh";
            options.Arguments = new List<string> { "-c", script };
        }
        return options;
    }

    /// <summary>
    /// 创建逐个回显参数的进程配置，以验证参数向量边界。
    /// </summary>
    /// <param name="values">需要回显的原始参数。</param>
    /// <param name="temporaryScript">Windows 平台创建的脚本路径；其他平台为 <see langword="null"/>。</param>
    /// <returns>用于参数回显的命令配置。</returns>
    private static CommandOptions CreateArgumentEchoOptions(string[] values, out string temporaryScript)
    {
        var options = new CommandOptions
        {
            Timeout = TimeSpan.FromSeconds(10),
            MaxStandardOutputChars = 65536,
        };
        temporaryScript = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            temporaryScript = Path.Combine(Path.GetTempPath(), "Bing.Utils.CommandRunner." + Guid.NewGuid().ToString("N") + ".ps1");
            File.WriteAllText(temporaryScript,
                "foreach ($value in $args) { [Console]::WriteLine('V:' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes([string]$value))) }",
                new UTF8Encoding(false));
            options.FileName = "powershell.exe";
            options.Arguments = new List<string> { "-NoLogo", "-NoProfile", "-NonInteractive", "-File", temporaryScript };
            foreach (var value in values)
                options.Arguments.Add(value);
        }
        else
        {
            options.FileName = "/bin/sh";
            options.Arguments = new List<string>
            {
                "-c",
                "for value do printf 'V:'; printf '%s' \"$value\" | base64 | tr -d '\\n'; printf '\\n'; done",
                "argument-echo",
            };
            foreach (var value in values)
                options.Arguments.Add(value);
        }
        return options;
    }

    /// <summary>
    /// 解码回显脚本输出的 Base64 参数行。
    /// </summary>
    /// <param name="output">进程标准输出。</param>
    /// <returns>按原顺序解码的参数数组。</returns>
    private static string[] DecodeArgumentLines(string output) => output
        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Where(line => line.StartsWith("V:", StringComparison.Ordinal))
        .Select(line => Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring(2))))
        .ToArray();

    /// <summary>
    /// 获取同时大量写入标准输出和标准错误的脚本。
    /// </summary>
    /// <returns>适用于当前操作系统的脚本文本。</returns>
    private static string GetLargeOutputScript() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "[Console]::Out.Write('o' * 200000); [Console]::Error.Write('e' * 200000); [Environment]::Exit(0)"
        : "head -c 200000 /dev/zero | tr '\\000' o; head -c 200000 /dev/zero | tr '\\000' e >&2; exit 0";

    /// <summary>
    /// 获取以指定退出码结束的脚本。
    /// </summary>
    /// <param name="code">脚本退出码。</param>
    /// <returns>适用于当前操作系统的脚本文本。</returns>
    private static string GetExitScript(int code) => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? $"[Environment]::Exit({code})"
        : $"exit {code}";

    /// <summary>
    /// 获取持续运行直至超时或取消的脚本。
    /// </summary>
    /// <returns>适用于当前操作系统的脚本文本。</returns>
    private static string GetSleepScript() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "Start-Sleep -Seconds 30"
        : "exec sleep 30";

    /// <summary>
    /// 创建隔离测试用的临时目录。
    /// </summary>
    /// <returns>已创建的临时目录绝对路径。</returns>
    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "Bing.Utils.CommandRunner." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// 按测试进程记录的 PID 清理后台后代进程。
    /// </summary>
    /// <param name="pidFile">记录进程 ID 的文件路径。</param>
    private static void StopProcessByPidFile(string pidFile)
    {
        if (!File.Exists(pidFile) || !int.TryParse(File.ReadAllText(pidFile, Encoding.UTF8), out var processId))
            return;
        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
        }
        catch (ArgumentException)
        {
        }
    }
}
