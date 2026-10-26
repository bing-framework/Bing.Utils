using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bing.CommandLine;

/// <summary>
/// 命令行执行器
/// </summary>
public class CommandLineExecutor
{
    /// <summary>
    /// 日志
    /// </summary>
    private ILogger _logger;

    /// <summary>
    /// 命令。范例：dotnet
    /// </summary>
    private string _command;

    /// <summary>
    /// 命令参数。范例：--info
    /// </summary>
    private string _arguments;

    /// <summary>
    /// 环境变量
    /// </summary>
    private readonly IDictionary<string, string> _environmentVariables;

    /// <summary>
    /// 是否重定向标准输出流
    /// </summary>
    private bool _redirectStandardOutput;

    /// <summary>
    /// 是否重定向标准错误流
    /// </summary>
    private bool _redirectStandardError;

    /// <summary>
    /// 输出流字符编码。默认值：Encoding.UTF8
    /// </summary>
    private Encoding _outputEncoding;

    /// <summary>
    /// 是否使用操作系统shell启动。默认值：false
    /// </summary>
    private bool _useShellExecute;

    /// <summary>
    /// 工作目录
    /// </summary>
    private string _workingDirectory;

    /// <summary>
    /// 启动进程是否不要创建新窗口
    /// </summary>
    private bool _createNoWindow;

    /// <summary>
    /// 命令执行超时间隔
    /// </summary>
    private TimeSpan _timeout;

    /// <summary>
    /// 预期完成输出消息
    /// </summary>
    private readonly List<string> _outputToMatch;

    /// <summary>
    /// 初始化一个 <see cref="CommandLineExecutor"/> 类型的实例。
    /// </summary>
    public CommandLineExecutor()
    {
        _logger = NullLogger.Instance;
        _redirectStandardOutput = true;
        _redirectStandardError = true;
        _outputEncoding = Encoding.UTF8;
        _useShellExecute = false;
        _createNoWindow = true;
        _environmentVariables = new Dictionary<string, string>();
        _timeout = TimeSpan.FromSeconds(30);
        _outputToMatch = new List<string>();
    }

    /// <summary>
    /// 创建命令行执行器
    /// </summary>
    /// <param name="command">命令。范例：dotnet</param>
    /// <param name="arguments">命令参数。范例：--info</param>
    /// <returns></returns>
    public static CommandLineExecutor Create(string command, string arguments = null) => new CommandLineExecutor().Command(command).Arguments(arguments);

    /// <summary>
    /// 设置日志
    /// </summary>
    /// <param name="logger">日志</param>
    public CommandLineExecutor Log(ILogger logger)
    {
        _logger = logger ?? NullLogger.Instance;
        return this;
    }

    /// <summary>
    /// 设置命令
    /// </summary>
    /// <param name="command">命令。范例：dotnet</param>
    public CommandLineExecutor Command(string command)
    {
        _command = command;
        return this;
    }

    /// <summary>
    /// 根据条件设置命令参数
    /// </summary>
    /// <param name="condition">条件。如果为false则不设置命令参数</param>
    /// <param name="arguments">命令参数。范例：--info</param>
    public CommandLineExecutor ArgumentsIf(bool condition, params string[] arguments)
    {
        if (condition == false)
            return this;
        return Arguments(arguments);
    }

    /// <summary>
    /// 根据条件设置命令参数
    /// </summary>
    /// <param name="condition">条件。如果为false则不设置命令参数</param>
    /// <param name="arguments">命令参数。范例：--info</param>
    public CommandLineExecutor ArgumentsIf(bool condition, IEnumerable<string> arguments)
    {
        if (condition == false)
            return this;
        return Arguments(arguments);
    }

    /// <summary>
    /// 设置命令参数
    /// </summary>
    /// <param name="arguments">命令参数</param>
    public CommandLineExecutor Arguments(params string[] arguments)
    {
        if (arguments == null)
            return this;
        return Arguments((IEnumerable<string>)arguments);
    }

    /// <summary>
    /// 设置命令参数
    /// </summary>
    /// <param name="arguments">命令参数</param>
    public CommandLineExecutor Arguments(IEnumerable<string> arguments)
    {
        if (arguments == null)
            return this;
        if (string.IsNullOrWhiteSpace(_arguments) == false)
            _arguments += " ";
        _arguments += string.Join(" ", arguments);
        return this;
    }

    /// <summary>
    /// 设置环境变量
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public CommandLineExecutor Env(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return this;
        if (_environmentVariables.ContainsKey(key))
            _environmentVariables.Remove(key);
        _environmentVariables.Add(key, value);
        return this;
    }

    /// <summary>
    /// 设置环境变量
    /// </summary>
    /// <param name="env">环境变量</param>
    public CommandLineExecutor Env(IDictionary<string, string> env)
    {
        if (env == null)
            return this;
        foreach (var item in env)
            Env(item.Key, item.Value);
        return this;
    }

    /// <summary>
    /// 设置重定向标准输出流
    /// </summary>
    /// <param name="value">是否重定向标准输出流。默认值：true</param>
    /// <remarks>启用重定向时禁用 Shell 启动；关闭重定向不会自动启用 Shell。</remarks>
    public CommandLineExecutor RedirectStandardOutput(bool value = true)
    {
        _redirectStandardOutput = value;
        if (value)
            _useShellExecute = false;
        return this;
    }

    /// <summary>
    /// 设置重定向标准错误流
    /// </summary>
    /// <param name="value">是否重定向标准错误流</param>
    public CommandLineExecutor RedirectStandardError(bool value = true)
    {
        _redirectStandardError = value;
        if (value)
            _useShellExecute = false;
        return this;
    }

    /// <summary>
    /// 设置输出流字符编码
    /// </summary>
    /// <param name="encoding">输出流字符编码。默认值：Encoding.UTF8</param>
    public CommandLineExecutor OutputEncoding(Encoding encoding)
    {
        _outputEncoding = encoding;
        return this;
    }

    /// <summary>
    /// 设置是否使用操作系统shell启动
    /// </summary>
    /// <param name="value">是否使用操作系统shell启动。默认值：false</param>
    public CommandLineExecutor UseShellExecute(bool value = true)
    {
        _useShellExecute = value;
        if (value)
        {
            _redirectStandardOutput = false;
            _redirectStandardError = false;
            return this;
        }

        _redirectStandardOutput = true;
        return this;
    }

    /// <summary>
    /// 设置工作目录
    /// </summary>
    /// <param name="value">工作目录</param>
    public CommandLineExecutor WorkingDirectory(string value)
    {
        _workingDirectory = value;
        return this;
    }

    /// <summary>
    /// 启动进程是否不要创建新窗口
    /// </summary>
    /// <param name="value">启动进程是否不要创建新窗口。默认值：true</param>
    public CommandLineExecutor CreateNoWindow(bool value)
    {
        _createNoWindow = value;
        return this;
    }

    /// <summary>
    /// 设置命令执行超时间隔。默认值：30秒
    /// </summary>
    /// <param name="timeout">命令执行超时间隔</param>
    public CommandLineExecutor Timeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// 设置命令执行完成的预期输出消息
    /// </summary>
    /// <param name="outputs">输出消息</param>
    public CommandLineExecutor OutputToMatch(params string[] outputs)
    {
        if (outputs == null)
            throw new ArgumentNullException(nameof(outputs));
        if (outputs.Any(output => output == null))
            throw new ArgumentException("输出匹配项不能为 null。", nameof(outputs));
        _outputToMatch.AddRange(outputs);
        return this;
    }

    /// <summary>
    /// 执行命令行。
    /// </summary>
    /// <remarks>
    /// 未设置输出条件时等待直接启动的进程退出；设置条件时匹配任意消息即返回，进程可能仍在运行。
    /// 进程退出后最多再等待 500 毫秒接收输出尾部，随后停止输出事件，避免后代进程持有管道导致无限等待。
    /// 返回的进程及其重定向读取资源由调用方释放。提前退出但未匹配时抛出异常；超时会终止本次启动的进程并释放资源。
    /// 已退出进程留下的后台服务由调用方管理，不再枚举其后代；netstandard2.0 超时仅终止直接进程。
    /// </remarks>
    /// <exception cref="ArgumentException">命令或等待时间无效。</exception>
    /// <exception cref="InvalidOperationException">进程退出时仍未匹配预期输出。</exception>
    /// <exception cref="TimeoutException">等待超时。</exception>
    public Process Execute()
    {
        if (string.IsNullOrWhiteSpace(_command))
            throw new ArgumentException("命令未设置");
        if (_timeout.TotalMilliseconds < -1 || _timeout.TotalMilliseconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(_timeout));
        _logger.LogDebug($"Running command: {GetDebugText()}");
        var process = new Process { StartInfo = CreateProcessStartInfo(), EnableRaisingEvents = true };
        // 每次调用独立记录退出、输出结束和匹配状态，重复执行不会继承上次的信号。
        var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var matched = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var outputEnded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorEnded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = _outputToMatch.ToArray();
        void Receive(DataReceivedEventArgs e, TaskCompletionSource<bool> ended)
        {
            if (e.Data == null)
            {
                ended.TrySetResult(true);
                return;
            }
            if (string.IsNullOrWhiteSpace(e.Data))
                return;
            try { _logger.LogDebug(e.Data); }
            catch (InvalidOperationException) { }
            if (expected.Any(e.Data.Contains))
                matched.TrySetResult(true);
        }
        process.Exited += (_, _) => exited.TrySetResult(true);
        process.OutputDataReceived += (_, e) => Receive(e, outputEnded);
        process.ErrorDataReceived += (_, e) => Receive(e, errorEnded);
        if (!process.StartInfo.RedirectStandardOutput) outputEnded.SetResult(true);
        if (!process.StartInfo.RedirectStandardError) errorEnded.SetResult(true);
        var drainedOutput = Task.WhenAll(outputEnded.Task, errorEnded.Task);
        Task completion = expected.Length == 0 ? exited.Task : Task.WhenAny(matched.Task, exited.Task);
        var started = false;
        try
        {
            started = process.Start();
            if (process.StartInfo.RedirectStandardOutput) process.BeginOutputReadLine();
            if (process.StartInfo.RedirectStandardError) process.BeginErrorReadLine();
            if (!completion.Wait(_timeout))
                throw new TimeoutException($"命令 \"{GetDebugText()}\" 超时");
            if (exited.Task.IsCompleted)
            {
                // Exited 与管道 EOF 是两个不同的事件；后代可能继续持有继承的写入端。
                Task tail = expected.Length == 0 ? drainedOutput : Task.WhenAny(matched.Task, drainedOutput);
                tail.Wait(TimeSpan.FromMilliseconds(500));
                StopRedirectedReaders(process);
            }
            if (expected.Length > 0 && !matched.Task.IsCompleted)
                throw new InvalidOperationException($"命令退出（退出码 {process.ExitCode}），但未匹配预期输出。");
            return process;
        }
        catch
        {
            try
            {
                if (started && !process.HasExited)
                {
#if NETSTANDARD2_0
                    process.Kill();
#else
                    process.Kill(entireProcessTree: true);
#endif
                    process.WaitForExit(5000);
                }
            }
            catch (InvalidOperationException) { } // 进程可能在检查后自行退出。
            catch (System.ComponentModel.Win32Exception) { }
            finally { process.Dispose(); }
            throw;
        }
    }

    /// <summary>停止输出事件；管道由 Process.Dispose 释放，成功返回时由调用方负责。</summary>
    private static void StopRedirectedReaders(Process process)
    {
        if (process.StartInfo.RedirectStandardOutput)
        {
            try { process.CancelOutputRead(); }
            catch (InvalidOperationException) { }
        }
        if (process.StartInfo.RedirectStandardError)
        {
            try { process.CancelErrorRead(); }
            catch (InvalidOperationException) { }
        }
    }

    /// <summary>
    /// 创建进程启动信息
    /// </summary>
    private ProcessStartInfo CreateProcessStartInfo()
    {
        var result = new ProcessStartInfo(_command, _arguments ?? string.Empty)
        {
            RedirectStandardOutput = _redirectStandardOutput,
            RedirectStandardError = _redirectStandardError,
            StandardOutputEncoding = _redirectStandardOutput ? _outputEncoding : null,
            StandardErrorEncoding = _redirectStandardError ? _outputEncoding : null,
            UseShellExecute = _useShellExecute,
            WorkingDirectory = _workingDirectory,
            CreateNoWindow = _createNoWindow,
        };
        foreach (var item in _environmentVariables)
            result.EnvironmentVariables[item.Key] = item.Value;
        return result;
    }

    /// <summary>
    /// 获取命令调试文本
    /// </summary>
    public string GetDebugText() => $"{_command} {_arguments}";

    /// <summary>
    /// 执行 PowerShell 命令
    /// </summary>
    /// <param name="command">PowerShell 命令。范例：Set-ExecutionPolicy RemoteSigned</param>
    /// <param name="workingDirectory">可选工作目录。</param>
    /// <remarks>
    /// 直接启动 Windows PowerShell 或跨平台 PowerShell，并无限等待进程结束；输出仍会读取但不保留。
    /// </remarks>
    public static void ExecutePowerShell(string command, string workingDirectory = null)
    {
        var fileName = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows) ? "powershell.exe" : "pwsh";
        var options = new CommandOptions
        {
            FileName = fileName,
            Arguments = new List<string> { "-NoProfile", "-NonInteractive", "-Command", command ?? string.Empty },
            WorkingDirectory = workingDirectory,
            Timeout = null,
            MaxStandardOutputChars = 0,
            MaxStandardErrorChars = 0,
        };
        CommandRunner.RunAsync(options).GetAwaiter().GetResult();
    }
}
