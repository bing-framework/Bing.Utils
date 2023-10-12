using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Bing.Helpers;

/// <summary>
/// 命令行帮助类
/// </summary>
public class CmdHelper
{
    /// <summary>
    /// Windows操作系统，执行cmd命令
    /// 多命令请使用批处理命令连接符：
    /// <![CDATA[
    /// &：同时执行两个命令
    /// |：将上一个命令的输出，作为下一个命令的输入
    /// &&：当&&前的命令成功时，才执行&&后的命令
    /// ||：当||前的命令失败时，才执行||后的命令
    /// ]]>
    /// </summary>
    /// <param name="cmdText">命令文本</param>
    /// <param name="cmdPath">命令行程序路径</param>
    public static string Run(string cmdText, string cmdPath = "cmd.exe")
    {
        // 说明：不管命令是否成功均执行exit命令，否则当调用ReadToEnd()方法时，会处于假死状态
        var cmd = $"{cmdText} &exit";
        using var process = new Process();
        InitProcess(process, cmdPath);

        // 写入命令
        process.StandardInput.WriteLine(cmd);
        process.StandardInput.AutoFlush = true;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(); // 等待程序执行完退出进程
        process.Close();

        return output;
    }

    /// <summary>
    /// 初始化处理程序
    /// </summary>
    private static void InitProcess(Process process, string cmdPath)
    {
        if (cmdPath == "cmd.exe")
            cmdPath = Path.Combine(System.Environment.SystemDirectory, cmdPath);
        process.StartInfo.FileName = cmdPath;
        process.StartInfo.UseShellExecute = false; // 是否使用操作系统shell启动
        process.StartInfo.RedirectStandardInput = true; // 是否接受来自调用程序的输入信息
        process.StartInfo.RedirectStandardOutput = true; // 是否由调用程序获取输出信息
        process.StartInfo.RedirectStandardError = true; // 是否重定向标准错误输出
        process.StartInfo.CreateNoWindow = true; // 是否不显示程序窗口
        process.Start();
    }

#if NET5_0
        /// <summary>
        /// Windows操作系统，执行cmd命令
        /// 多命令请使用批处理命令连接符：
        /// <![CDATA[
        /// &：同时执行两个命令
        /// |：将上一个命令的输出，作为下一个命令的输入
        /// &&：当&&前的命令成功时，才执行&&后的命令
        /// ||：当||前的命令失败时，才执行||后的命令
        /// ]]>
        /// </summary>
        /// <param name="cmdText">命令文本</param>
        /// <param name="cmdPath">命令行程序路径</param>
        public static async Task<string> RunAsync(string cmdText, string cmdPath = "cmd.exe")
        {
            // 说明：不管命令是否成功均执行exit命令，否则当调用ReadToEnd()方法时，会处于假死状态
            var cmd = $"{cmdText} &exit";
            using var process = new Process();
            InitProcess(process, cmdPath);

            // 写入命令
            await process.StandardInput.WriteLineAsync(cmd);
            process.StandardInput.AutoFlush = true;
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync(); // 等待程序执行完退出进程
            process.Close();

            return output;
        }
#endif

    /// <summary>
    /// Linux操作系统，执行Shell命令
    /// </summary>
    /// <param name="cmdText">命令文本</param>
    /// <remarks>
    /// using https://github.com/phil-harmoniq/Shell.NET
    /// </remarks>
    public static BashResult Shell(string cmdText) => new Bash().Command(cmdText);

    /// <summary>
    /// Bash执行结果
    /// </summary>
    public class BashResult
    {
        /// <summary>
        /// 命令的标准输出
        /// </summary>
        public string Output { get; private set; }

        /// <summary>
        /// 命令的错误输出
        /// </summary>
        public string ErrorMsg { get; private set; }

        /// <summary>
        /// 命令的退出代码
        /// </summary>
        public int ExitCode { get; private set; }

        /// <summary>
        /// 由换行符分割的命令输出数组
        /// </summary>
        public string[] Lines => Output?.Split(Environment.NewLine.ToCharArray());

        /// <summary>
        /// 初始化一个<see cref="BashResult"/>类型的实例
        /// </summary>
        /// <param name="output">命令的标准输出</param>
        /// <param name="errorMsg">命令的错误输出</param>
        /// <param name="exitCode">命令的退出代码</param>
        internal BashResult(string output, string errorMsg, int exitCode)
        {
            Output = output?.TrimEnd(Environment.NewLine.ToCharArray());
            ErrorMsg = errorMsg?.TrimEnd(Environment.NewLine.ToCharArray());
            ExitCode = exitCode;
        }
    }

    /// <summary>
    /// 执行
    /// </summary>
    public class Bash
    {
        /// <summary>
        /// 是否Linux平台
        /// </summary>
        private static bool _linux { get; }

        /// <summary>
        /// 是否Mac系统
        /// </summary>
        private static bool _mac { get; }

        /// <summary>
        /// 是否Windows系统
        /// </summary>
        private static bool _windows { get; }

        /// <summary>
        /// Bash路径
        /// </summary>
        private static string _bashPath { get; }

        /// <summary>
        /// 确定 bash 是否在本机操作系统中运行(Linux/MacOS)
        /// </summary>
        public static bool Native { get; }

        /// <summary>
        /// 子系统
        /// </summary>
        public static bool Subsystem => _windows && File.Exists(@"C:\Windows\System32\bash.exe");

        /// <summary>
        /// 命令的标准输出
        /// </summary>
        public string Output { get; private set; }

        /// <summary>
        /// 由换行符分割的命令输出数组
        /// </summary>
        public string[] Lines => Output?.Split(Environment.NewLine.ToCharArray());

        /// <summary>
        /// 命令的退出代码
        /// </summary>
        public int ExitCode { get; private set; }

        /// <summary>
        /// 命令的错误输出
        /// </summary>
        public string ErrorMsg { get; private set; }

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static Bash()
        {
            _linux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            _mac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            _windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            Native = _linux || _mac ? true : false;
            _bashPath = Native ? "bash" : "bash.exe";
        }

        /// <summary>
        /// 执行Bash命令
        /// </summary>
        /// <param name="input">输入命令文本</param>
        /// <param name="redirect">是否重定向</param>
        public BashResult Command(string input, bool redirect = true)
        {
            if (!Native && Subsystem)
                throw new PlatformNotSupportedException();
            using (var bash = new Process { StartInfo = BashInfo(input, redirect) })
            {
                bash.Start();
                if (redirect)
                {
                    Output = bash.StandardOutput.ReadToEnd().TrimEnd(Environment.NewLine.ToCharArray());
                    ErrorMsg = bash.StandardError.ReadToEnd().TrimEnd(Environment.NewLine.ToCharArray());
                }
                else
                {
                    Output = null;
                    ErrorMsg = null;
                }

                bash.WaitForExit();
                ExitCode = bash.ExitCode;
                bash.Close();
            }

            if (redirect)
                return new BashResult(Output, ErrorMsg, ExitCode);
            return new BashResult(null, null, ExitCode);
        }

        /// <summary>
        /// 获取Bash信息
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="redirectOutput">是否重定向输出</param>
        private ProcessStartInfo BashInfo(string input, bool redirectOutput) =>
            new ProcessStartInfo
            {
                FileName = _bashPath,
                Arguments = $"-c \"{input}\"",
                RedirectStandardInput = false,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectOutput,
                UseShellExecute = false,
                CreateNoWindow = true,
                ErrorDialog = false
            };
    }
}