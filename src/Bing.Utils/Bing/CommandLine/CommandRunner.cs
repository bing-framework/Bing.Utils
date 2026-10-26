using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bing.CommandLine;

/// <summary>
/// 通过结构化参数启动并监控外部命令。
/// </summary>
/// <remarks>
/// 进程不会经过 shell；在 .NET Standard 2.0 上由本类按 Windows argv 规则转义参数，并直接启动目标程序。
/// 超时或取消时会终止本次启动的进程；.NET Standard 2.0 只能直接终止该进程，不能保证终止其后代进程。
/// </remarks>
public static class CommandRunner
{
    /// <summary>
    /// 表示未配置执行超时时持续等待的任务。
    /// </summary>
    private static readonly Task NeverTask = new TaskCompletionSource<bool>().Task;

    /// <summary>
    /// 保存异常清理时等待进程退出的最长时限。
    /// </summary>
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 异步启动命令并捕获其输出。
    /// </summary>
    /// <param name="options">可执行文件、参数、环境和资源限制配置。</param>
    /// <param name="cancellationToken">用于取消本次执行的令牌。</param>
    /// <returns>进程退出码、捕获输出和执行耗时。</returns>
    /// <exception cref="ArgumentNullException">选项或必需的参数集合为空。</exception>
    /// <exception cref="ArgumentException">命令参数、环境变量或限制值无效。</exception>
    /// <exception cref="OperationCanceledException">调用方取消执行，并携带对应的取消令牌。</exception>
    /// <exception cref="TimeoutException">执行超过配置的超时时间。</exception>
    public static async Task<CommandResult> RunAsync(CommandOptions options, CancellationToken cancellationToken = default)
    {
        var snapshot = Snapshot(options);
        var inputWriter = options.StandardInput;
        var outputObserver = options.OnStandardOutput;
        var errorObserver = options.OnStandardError;
        var interactive = inputWriter != null || outputObserver != null || errorObserver != null;
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = CreateStartInfo(snapshot);
        startInfo.RedirectStandardInput = inputWriter != null;
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Exited += (_, _) => exited.TrySetResult(true);
        var cancellationSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellationRegistration = cancellationToken.Register(() => cancellationSignal.TrySetResult(true));

        var started = false;
        var standardOutput = new CapturedOutput(snapshot.MaxStandardOutputChars);
        var standardError = new CapturedOutput(snapshot.MaxStandardErrorChars);
        Task standardOutputTask = Task.CompletedTask;
        Task standardErrorTask = Task.CompletedTask;
        Task standardInputTask = Task.CompletedTask;
        using var ioLifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ioFailure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopwatch = Stopwatch.StartNew();
        using var timerLifetime = new CancellationTokenSource();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!process.Start())
                throw new InvalidOperationException($"无法启动命令：{snapshot.FileName}");
            started = true;
            if (process.HasExited)
                exited.TrySetResult(true);

            standardOutputTask = ObserveAsync(() => ReadOutputAsync(process.StandardOutput, standardOutput, outputObserver, ioLifetime.Token));
            standardErrorTask = ObserveAsync(() => ReadOutputAsync(process.StandardError, standardError, errorObserver, ioLifetime.Token));
            if (inputWriter != null) standardInputTask = ObserveAsync(async () =>
            {
                using var writer = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false), 4096) { AutoFlush = true };
                await inputWriter(writer, ioLifetime.Token).ConfigureAwait(false);
                ioLifetime.Token.ThrowIfCancellationRequested();
                await writer.FlushAsync().ConfigureAwait(false);
            });
            var readersTask = Task.WhenAll(standardOutputTask, standardErrorTask, standardInputTask);
            var timeoutTask = CreateTimeoutTask(snapshot.Timeout, timerLifetime.Token);

            var first = await Task.WhenAny(exited.Task, cancellationSignal.Task, timeoutTask, ioFailure.Task).ConfigureAwait(false);
            ThrowIfCancelled(cancellationToken);
            ThrowIoFailure();
            if (first == timeoutTask && !exited.Task.IsCompleted)
                throw new TimeoutException($"命令执行超时：{snapshot.FileName}");

            await exited.Task.ConfigureAwait(false);
            ThrowIfCancelled(cancellationToken);
            ThrowIoFailure();
            if (snapshot.Timeout.HasValue && timeoutTask.IsCompleted && stopwatch.Elapsed >= snapshot.Timeout.Value)
                throw new TimeoutException($"命令执行超时：{snapshot.FileName}");

            var drainTimeoutTask = DelayForTimeoutAsync(snapshot.OutputDrainTimeout, timerLifetime.Token);
            await Task.WhenAny(readersTask, drainTimeoutTask, cancellationSignal.Task, timeoutTask, ioFailure.Task)
                .ConfigureAwait(false);
            ThrowIfCancelled(cancellationToken);
            ThrowIoFailure();
            if (snapshot.Timeout.HasValue && timeoutTask.IsCompleted && stopwatch.Elapsed >= snapshot.Timeout.Value)
                throw new TimeoutException($"命令执行超时：{snapshot.FileName}");

            var outputComplete = readersTask.IsCompleted;
            if (outputComplete)
            {
                await readersTask.ConfigureAwait(false);
                outputComplete = standardOutput.IsComplete && standardError.IsComplete;
            }
            else
            {
                if (interactive) CancelIo();
                CloseReaders(process);
                CloseInput();
                if (interactive) await readersTask.ConfigureAwait(false);
                else await Task.WhenAny(readersTask, Task.Delay(TimeSpan.FromMilliseconds(500))).ConfigureAwait(false);
            }

            ThrowIfCancelled(cancellationToken);
            ThrowIoFailure();
            if (snapshot.Timeout.HasValue && timeoutTask.IsCompleted && stopwatch.Elapsed >= snapshot.Timeout.Value)
                throw new TimeoutException($"命令执行超时：{snapshot.FileName}");

            stopwatch.Stop();
            return new CommandResult(process.ExitCode,
                standardOutput.GetText(),
                standardError.GetText(),
                standardOutput.IsTruncated,
                standardError.IsTruncated,
                outputComplete, stopwatch.Elapsed);
        }
        catch
        {
            if (started)
            {
                CancelIo();
                await StopProcessAsync(process, exited.Task).ConfigureAwait(false);
                CloseReaders(process);
                CloseInput();
                var cleanup = Task.WhenAll(standardOutputTask, standardErrorTask, standardInputTask);
                if (interactive) await cleanup.ConfigureAwait(false);
                else await Task.WhenAny(cleanup, Task.Delay(500)).ConfigureAwait(false);
            }
            throw;
        }
        finally { timerLifetime.Cancel(); }

        async Task ObserveAsync(Func<Task> operation)
        {
            try { await operation().ConfigureAwait(false); }
            catch (OperationCanceledException) when (ioLifetime.IsCancellationRequested) { }
            catch (Exception error) { ioFailure.TrySetResult(error); }
        }
        void ThrowIoFailure()
        { if (ioFailure.Task.IsCompleted) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ioFailure.Task.Result).Throw(); }
        void CancelIo() { try { ioLifetime.Cancel(); } catch (Exception error) { ioFailure.TrySetResult(error); } }
        void CloseInput()
        {
            if (inputWriter == null) return;
            try { process.StandardInput.Dispose(); } catch (IOException) { } catch (InvalidOperationException) { }
        }
    }

    /// <summary>
    /// 在所有目标框架上创建等价的进程参数表示。
    /// </summary>
    /// <param name="options">经过校验的执行配置快照。</param>
    /// <returns>已配置为重定向输出且不经 shell 启动的进程信息。</returns>
    private static ProcessStartInfo CreateStartInfo(CommandOptionsSnapshot options)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = options.FileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        if (!string.IsNullOrEmpty(options.WorkingDirectory))
            startInfo.WorkingDirectory = options.WorkingDirectory;

        foreach (var variable in options.EnvironmentVariables)
            startInfo.EnvironmentVariables[variable.Key] = variable.Value;

#if NETSTANDARD2_0
        startInfo.Arguments = JoinEscapedArguments(options.Arguments);
#else
        foreach (var argument in options.Arguments)
            startInfo.ArgumentList.Add(argument);
#endif
        return startInfo;
    }

    /// <summary>
    /// 验证并复制配置，避免进程运行期间受到调用方修改影响。
    /// </summary>
    /// <param name="options">调用方提供的可变配置。</param>
    /// <returns>验证后的独立配置快照。</returns>
    private static CommandOptionsSnapshot Snapshot(CommandOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        var fileName = options.FileName;
        var arguments = options.Arguments;
        var workingDirectory = options.WorkingDirectory;
        var environmentVariables = options.EnvironmentVariables;
        var timeout = options.Timeout;
        var maxStandardOutputChars = options.MaxStandardOutputChars;
        var maxStandardErrorChars = options.MaxStandardErrorChars;
        var outputDrainTimeout = options.OutputDrainTimeout;

        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOf('\0') >= 0)
            throw new ArgumentException("必须指定可执行文件。", nameof(options));
        if (arguments == null)
            throw new ArgumentNullException(nameof(options.Arguments));
        if (environmentVariables == null)
            throw new ArgumentNullException(nameof(options.EnvironmentVariables));
        if (workingDirectory != null && workingDirectory.IndexOf('\0') >= 0)
            throw new ArgumentException("工作目录不能包含空字符。", nameof(options.WorkingDirectory));
        var argumentSnapshot = arguments.ToArray();
        if (argumentSnapshot.Any(argument => argument == null || argument.IndexOf('\0') >= 0))
            throw new ArgumentException("命令参数不能为 null 或包含空字符。", nameof(options.Arguments));
        if (maxStandardOutputChars < 0 || maxStandardErrorChars < 0)
            throw new ArgumentOutOfRangeException(nameof(options), "输出字符上限不能为负数。");
        if (timeout.HasValue && timeout.Value < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options.Timeout), "超时时间不能为负数；无限等待请使用 null。");
        if (outputDrainTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options.OutputDrainTimeout), "输出管道收尾等待时间必须大于零。");

        var environment = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var variable in environmentVariables)
        {
            if (string.IsNullOrEmpty(variable.Key) || variable.Key.IndexOf('=') >= 0 || variable.Key.IndexOf('\0') >= 0)
                throw new ArgumentException("环境变量名称不能为空，且不能包含等号或空字符。", nameof(options.EnvironmentVariables));
            if (variable.Value == null || variable.Value.IndexOf('\0') >= 0)
                throw new ArgumentException("环境变量值不能为 null 或包含空字符。", nameof(options.EnvironmentVariables));
            environment[variable.Key] = variable.Value;
        }

        return new CommandOptionsSnapshot(fileName, argumentSnapshot, workingDirectory,
            environment, timeout, maxStandardOutputChars, maxStandardErrorChars, outputDrainTimeout);
    }

    /// <summary>
    /// 持续读取管道，只将字符上限内的内容保存在内存中。
    /// </summary>
    /// <param name="reader">要持续读取的输出管道。</param>
    /// <param name="capture">保存有限输出及完成状态的缓冲区。</param>
    private static async Task ReadOutputAsync(StreamReader reader, CapturedOutput capture,
        Func<string, CancellationToken, Task> observer, CancellationToken token)
    {
        var buffer = new char[4096];
        while (true)
        {
            int count;
            try { count = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false); }
            catch (IOException) { return; }
            catch (ObjectDisposedException) { return; }
            if (count == 0) { capture.MarkComplete(); return; }
            capture.Append(buffer, count);
            if (observer != null)
            { token.ThrowIfCancellationRequested(); await observer(new string(buffer, 0, count), token).ConfigureAwait(false); }
        }
    }

    /// <summary>
    /// 在指定时限内终止并等待已经启动的进程。
    /// </summary>
    /// <param name="process">本次启动的进程。</param>
    /// <param name="exited">进程退出通知任务。</param>
    private static async Task StopProcessAsync(Process process, Task exited)
    {
        try
        {
            if (!process.HasExited)
            {
#if NETSTANDARD2_0
                process.Kill();
#else
                process.Kill(entireProcessTree: true);
#endif
            }
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }

        try
        {
            if (!process.HasExited)
                await Task.WhenAny(exited, Task.Delay(CleanupTimeout)).ConfigureAwait(false);
            if (process.HasExited)
                process.WaitForExit();
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
    }

    /// <summary>
    /// 关闭输出读取端以解除仍被后代进程持有的管道等待。
    /// </summary>
    /// <param name="process">输出读取端所属进程。</param>
    private static void CloseReaders(Process process)
    {
        try { process.StandardOutput.Dispose(); }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
        catch (IOException) { }
        try { process.StandardError.Dispose(); }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
        catch (IOException) { }
    }

    /// <summary>
    /// 为较长超时分段等待，兼容 .NET Standard 2.0 的 Task.Delay 时间范围。
    /// </summary>
    /// <param name="timeout">完整等待时长。</param>
    /// <param name="token">执行结束后取消后台计时。</param>
    /// <returns>等待时长结束后完成的异步任务。</returns>
    private static Task CreateTimeoutTask(TimeSpan? timeout, CancellationToken token) => timeout.HasValue
        ? DelayForTimeoutAsync(timeout.Value, token)
        : NeverTask;

    /// <summary>
    /// 以平台允许的最大单次延迟分段等待完整时限。
    /// </summary>
    /// <param name="timeout">完整等待时长。</param>
    /// <param name="token">执行结束后取消后台计时。</param>
    private static async Task DelayForTimeoutAsync(TimeSpan timeout, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        var maxDelay = TimeSpan.FromMilliseconds(int.MaxValue - 1);
        while (stopwatch.Elapsed < timeout)
        {
            var remaining = timeout - stopwatch.Elapsed;
            await Task.Delay(remaining > maxDelay ? maxDelay : remaining, token).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 优先传播调用方令牌对应的取消状态。
    /// </summary>
    /// <param name="cancellationToken">调用方的取消令牌。</param>
    private static void ThrowIfCancelled(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            throw new OperationCanceledException(cancellationToken);
    }

#if NETSTANDARD2_0
    /// <summary>
    /// 使用 Windows argv 转义规则构造进程参数字符串。
    /// </summary>
    /// <param name="arguments">需要按顺序传递的参数。</param>
    /// <returns>传递给 <see cref="ProcessStartInfo.Arguments"/> 的转义参数文本。</returns>
    private static string JoinEscapedArguments(IReadOnlyList<string> arguments) =>
        string.Join(" ", arguments.Select(QuoteArgument));

    /// <summary>
    /// 转义引号及其前置反斜杠，保持单个参数在目标进程中的原值。
    /// </summary>
    /// <param name="argument">需要转义的单个参数。</param>
    /// <returns>包含完整参数边界的进程参数文本。</returns>
    private static string QuoteArgument(string argument)
    {
        var builder = new StringBuilder(argument.Length + 2);
        builder.Append('"');
        var backslashes = 0;
        foreach (var character in argument)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }
            if (character == '"')
            {
                builder.Append('\\', backslashes * 2 + 1);
                builder.Append('"');
                backslashes = 0;
                continue;
            }
            builder.Append('\\', backslashes);
            builder.Append(character);
            backslashes = 0;
        }
        builder.Append('\\', backslashes * 2);
        builder.Append('"');
        return builder.ToString();
    }
#endif

    /// <summary>
    /// 保存启动时复制的不可变执行配置。
    /// </summary>
    private sealed class CommandOptionsSnapshot
    {
        /// <summary>
        /// 使用复制后的执行配置初始化快照。
        /// </summary>
        /// <param name="fileName">可执行文件路径。</param>
        /// <param name="arguments">独立参数副本。</param>
        /// <param name="workingDirectory">工作目录。</param>
        /// <param name="environmentVariables">环境变量副本。</param>
        /// <param name="timeout">执行超时时间。</param>
        /// <param name="maxStandardOutputChars">标准输出捕获上限。</param>
        /// <param name="maxStandardErrorChars">标准错误捕获上限。</param>
        /// <param name="outputDrainTimeout">进程退出后的管道收尾时限。</param>
        public CommandOptionsSnapshot(string fileName, string[] arguments, string workingDirectory,
            IDictionary<string, string> environmentVariables, TimeSpan? timeout, int maxStandardOutputChars,
            int maxStandardErrorChars, TimeSpan outputDrainTimeout)
        {
            FileName = fileName;
            Arguments = arguments;
            WorkingDirectory = workingDirectory;
            EnvironmentVariables = environmentVariables;
            Timeout = timeout;
            MaxStandardOutputChars = maxStandardOutputChars;
            MaxStandardErrorChars = maxStandardErrorChars;
            OutputDrainTimeout = outputDrainTimeout;
        }

        /// <summary>
        /// 获取可执行文件路径。
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// 获取进程参数副本。
        /// </summary>
        public string[] Arguments { get; }

        /// <summary>
        /// 获取工作目录。
        /// </summary>
        public string WorkingDirectory { get; }

        /// <summary>
        /// 获取环境变量副本。
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; }

        /// <summary>
        /// 获取超时时间。
        /// </summary>
        public TimeSpan? Timeout { get; }

        /// <summary>
        /// 获取标准输出捕获上限。
        /// </summary>
        public int MaxStandardOutputChars { get; }

        /// <summary>
        /// 获取标准错误捕获上限。
        /// </summary>
        public int MaxStandardErrorChars { get; }

        /// <summary>
        /// 获取输出收尾时限。
        /// </summary>
        public TimeSpan OutputDrainTimeout { get; }
    }

    /// <summary>
    /// 保存单条输出管道的捕获内容和完成状态。
    /// </summary>
    private sealed class CapturedOutput
    {
        /// <summary>
        /// 保护输出缓冲及其状态字段的锁。
        /// </summary>
        private readonly object _sync = new object();

        /// <summary>
        /// 限制保留字符数量的上限。
        /// </summary>
        private readonly int _maxChars;

        /// <summary>
        /// 保存字符上限内的输出内容。
        /// </summary>
        private readonly StringBuilder _builder;

        /// <summary>
        /// 指示是否至少有一个字符因达到上限而被丢弃。
        /// </summary>
        private bool _truncated;

        /// <summary>
        /// 指示读取是否自然到达管道末尾。
        /// </summary>
        private bool _complete;

        /// <summary>
        /// 初始化受字符上限约束的输出缓冲。
        /// </summary>
        /// <param name="maxChars">允许保留的最大字符数。</param>
        public CapturedOutput(int maxChars)
        {
            _maxChars = maxChars;
            _builder = new StringBuilder(Math.Min(maxChars, 4096));
        }

        /// <summary>
        /// 获取当前已保留的输出文本。
        /// </summary>
        /// <returns>截至快照时捕获的输出。</returns>
        public string GetText()
        {
            lock (_sync)
                return _builder.ToString();
        }

        /// <summary>
        /// 获取是否超过捕获上限。
        /// </summary>
        public bool IsTruncated
        {
            get
            {
                lock (_sync)
                    return _truncated;
            }
        }

        /// <summary>
        /// 获取读取端是否自然到达结尾。
        /// </summary>
        public bool IsComplete
        {
            get
            {
                lock (_sync)
                    return _complete;
            }
        }

        /// <summary>
        /// 将新读出的字符写入受限缓冲。
        /// </summary>
        /// <param name="buffer">输出读取缓冲区。</param>
        /// <param name="count">本次读取的字符数。</param>
        public void Append(char[] buffer, int count)
        {
            lock (_sync)
            {
                var remaining = _maxChars - _builder.Length;
                if (remaining > 0)
                    _builder.Append(buffer, 0, Math.Min(remaining, count));
                if (count > remaining)
                    _truncated = true;
            }
        }

        /// <summary>
        /// 标记管道已自然读取至结束。
        /// </summary>
        public void MarkComplete()
        {
            lock (_sync)
                _complete = true;
        }
    }
}
