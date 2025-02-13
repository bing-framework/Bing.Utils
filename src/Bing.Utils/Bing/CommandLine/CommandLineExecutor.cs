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
    /// 事件等待句柄
    /// </summary>
    private readonly EventWaitHandle _outputReceived;

    /// <summary>
    /// 预期完成输出消息
    /// </summary>
    private readonly List<string> _outputToMatch;

    /// <summary>
    /// 初始化一个<see cref="CommandLineExecutor"/>类型的实例
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
        _outputReceived = new EventWaitHandle(false, EventResetMode.ManualReset);
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
    /// <remarks>注意：如果要设置 UseShellExecute 为 false，则必须将 RedirectStandardOutput 设置为 true。</remarks>
    public CommandLineExecutor RedirectStandardOutput(bool value = true)
    {
        _redirectStandardOutput = value;
        if (value)
        {
            _useShellExecute = false;
            return this;
        }
        _useShellExecute = true;
        _outputEncoding = null;
        return this;
    }

    /// <summary>
    /// 设置重定向标准错误流
    /// </summary>
    /// <param name="value">是否重定向标准错误流</param>
    public CommandLineExecutor RedirectStandardError(bool value = true)
    {
        _redirectStandardError = value;
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
            _outputEncoding = null;
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
    /// 设置命令执行超时间隔。默认值：60秒
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
        _outputToMatch.AddRange(outputs);
        return this;
    }

    /// <summary>
    /// 执行命令行
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="TimeoutException"></exception>
    public Process Execute()
    {
        if (string.IsNullOrWhiteSpace(_command))
            throw new ArgumentException("命令未设置");
        _logger.LogDebug($"Running command: {GetDebugText()}");
        var process = new Process
        {
            StartInfo = CreateProcessStartInfo()
        };
        process.OutputDataReceived += OnOutput;
        process.ErrorDataReceived += OnOutput;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        var done = _outputReceived.WaitOne(_timeout);
        if (!done)
            throw new TimeoutException($"命令 \"{GetDebugText()}\" 超时");
        return process;
    }

    /// <summary>
    /// 创建进程启动信息
    /// </summary>
    private ProcessStartInfo CreateProcessStartInfo()
    {
        var escapedArgs = _arguments.Replace("\"", "\\\"");
        var result = new ProcessStartInfo(_command, escapedArgs)
        {
            RedirectStandardOutput = _redirectStandardOutput,
            RedirectStandardError = _redirectStandardError,
            StandardOutputEncoding = _outputEncoding,
            StandardErrorEncoding = _outputEncoding,
            UseShellExecute = _useShellExecute,
            WorkingDirectory = _workingDirectory,
            CreateNoWindow = _createNoWindow,
        };
        foreach (var item in _environmentVariables)
            result.EnvironmentVariables.Add(item.Key, item.Value);
        return result;
    }

    /// <summary>
    /// 接收输出消息事件处理
    /// </summary>
    private void OnOutput(object sendingProcess, DataReceivedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Data))
            return;
        try
        {
            _logger.LogDebug(e.Data);
        }
        catch (InvalidOperationException)
        {
        }
        if (_outputToMatch.Any(e.Data.Contains))
            _outputReceived.Set();

    }

    /// <summary>
    /// 获取命令调试文本
    /// </summary>
    public string GetDebugText() => $"{_command} {_arguments}";

    /// <summary>
    /// 执行 PowerShell 命令
    /// </summary>
    /// <param name="command">PowerShell 命令。范例：Set-ExecutionPolicy RemoteSigned</param>
    /// <param name="workingDirectory">工作目录</param>
    public static void ExecutePowerShell(string command, string workingDirectory = null)
    {
        using Process process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.CreateNoWindow = false;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.Start();
        process.StandardInput.WriteLine($"powershell {command}");
        process.StandardInput.WriteLine("exit");
        process.StandardInput.AutoFlush = true;
        process.WaitForExit();
    }
}