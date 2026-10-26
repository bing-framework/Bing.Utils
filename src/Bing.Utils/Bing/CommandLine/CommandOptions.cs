using System;
using System.Collections.Generic;

namespace Bing.CommandLine;

/// <summary>
/// 描述一次结构化命令行执行。
/// </summary>
/// <remarks>
/// 执行开始时会复制参数和环境变量；运行期间修改此对象不会影响已经启动的进程。
/// </remarks>
public sealed class CommandOptions
{
    /// <summary>
    /// 获取或设置标准输入写入器；配置后才重定向 stdin，委托完成后刷新并关闭管道以发送 EOF。
    /// </summary>
    /// <remarks>写入器使用无 BOM UTF-8，由运行器拥有。委托应响应令牌，不能在返回后继续使用写入器。</remarks>
    public Func<StreamWriter, CancellationToken, Task> StandardInput { get; set; }

    /// <summary>
    /// 获取或设置标准输出的实时分块消费者；每块最多 4096 个 UTF-16 字符，不承诺行边界。
    /// </summary>
    /// <remarks>每个管道串行等待消费者，形成背压；与 stderr 消费者可并发。即使捕获达到上限仍交付全部块。
    /// 委托应响应令牌，异常会终止本次进程，运行器等待已启动委托收尾。</remarks>
    public Func<string, CancellationToken, Task> OnStandardOutput { get; set; }

    /// <summary>
    /// 获取或设置标准错误的实时分块消费者，采用与标准输出相同的背压和生命周期规则。
    /// </summary>
    public Func<string, CancellationToken, Task> OnStandardError { get; set; }
    /// <summary>
    /// 获取或设置要启动的可执行文件名称或路径。
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 获取或设置按原顺序传递给进程的独立参数。
    /// </summary>
    public IList<string> Arguments { get; set; } = new List<string>();

    /// <summary>
    /// 获取或设置进程的工作目录；为 <see langword="null"/> 时使用当前目录。
    /// </summary>
    public string WorkingDirectory { get; set; }

    /// <summary>
    /// 获取或设置仅应用于本次进程的环境变量覆盖值。
    /// </summary>
    public IDictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// 获取或设置执行超时时间；默认 30 秒，设为 <see langword="null"/> 表示无限等待。
    /// </summary>
    public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 获取或设置最多保留的标准输出字符数；设为 0 时丢弃内容但仍持续读取。
    /// </summary>
    public int MaxStandardOutputChars { get; set; } = 65536;

    /// <summary>
    /// 获取或设置最多保留的标准错误字符数；设为 0 时丢弃内容但仍持续读取。
    /// </summary>
    public int MaxStandardErrorChars { get; set; } = 65536;

    /// <summary>
    /// 获取或设置进程退出后等待输出管道结束的最长时间。
    /// </summary>
    /// <remarks>
    /// 该值必须大于零；超时后关闭读取端，并在结果中将 <c>OutputComplete</c> 设为 <see langword="false"/>。
    /// </remarks>
    public TimeSpan OutputDrainTimeout { get; set; } = TimeSpan.FromMilliseconds(500);
}
