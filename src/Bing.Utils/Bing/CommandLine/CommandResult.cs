using System;

namespace Bing.CommandLine;

/// <summary>
/// 保存命令进程的退出状态和捕获输出。
/// </summary>
public sealed class CommandResult
{
    /// <summary>
    /// 使用命令执行数据初始化结果。
    /// </summary>
    /// <param name="exitCode">进程退出码。</param>
    /// <param name="standardOutput">捕获的标准输出。</param>
    /// <param name="standardError">捕获的标准错误。</param>
    /// <param name="standardOutputTruncated">标准输出是否超过捕获上限。</param>
    /// <param name="standardErrorTruncated">标准错误是否超过捕获上限。</param>
    /// <param name="outputComplete">两个输出管道是否均读取到结束。</param>
    /// <param name="duration">从启动到完成输出处理所用的时间。</param>
    internal CommandResult(int exitCode, string standardOutput, string standardError,
        bool standardOutputTruncated, bool standardErrorTruncated, bool outputComplete, TimeSpan duration)
    {
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
        StandardOutputTruncated = standardOutputTruncated;
        StandardErrorTruncated = standardErrorTruncated;
        OutputComplete = outputComplete;
        Duration = duration;
    }

    /// <summary>
    /// 获取进程退出码；零通常表示成功。
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// 获取捕获到的标准输出文本。
    /// </summary>
    public string StandardOutput { get; }

    /// <summary>
    /// 获取捕获到的标准错误文本。
    /// </summary>
    public string StandardError { get; }

    /// <summary>
    /// 获取标准输出是否因达到字符上限而截断。
    /// </summary>
    public bool StandardOutputTruncated { get; }

    /// <summary>
    /// 获取标准错误是否因达到字符上限而截断。
    /// </summary>
    public bool StandardErrorTruncated { get; }

    /// <summary>
    /// 获取两个输出管道是否都读取到了结束；后代进程仍持有管道时可能为 <see langword="false"/>。
    /// </summary>
    public bool OutputComplete { get; }

    /// <summary>
    /// 获取从启动进程到完成输出处理所用的时间。
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// 在退出码非零时抛出异常，否则返回当前结果。
    /// </summary>
    /// <returns>退出码为零时的当前结果。</returns>
    /// <exception cref="InvalidOperationException">进程退出码非零。</exception>
    public CommandResult EnsureSuccess()
    {
        if (ExitCode != 0)
            throw new InvalidOperationException($"命令执行失败，退出码 {ExitCode}。{StandardError}");
        return this;
    }
}
