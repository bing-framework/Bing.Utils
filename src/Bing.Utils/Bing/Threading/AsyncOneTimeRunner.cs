namespace Bing.Threading;

/// <summary>
/// 确保 action 只执行一次。<br />
/// <see cref="AsyncOneTimeRunner"/> 通常作为类的 私有、静态、只读 对象实例，确保在类的声明周期中，某个操作只执行一次。<br />
/// <example>
/// <code>
/// private static readonly AsyncOneTimeRunner runner = new AsyncOneTimeRunner();
/// </code>
/// </example>
/// </summary>
public class AsyncOneTimeRunner
{
    /// <summary>
    /// 是否已运行
    /// </summary>
    private volatile bool _runBefore;

    /// <summary>
    /// 信号量
    /// </summary>
    private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

    /// <summary>
    /// 运行
    /// </summary>
    /// <param name="action">操作</param>
    public async Task RunAsync(Func<Task> action)
    {
        if (_runBefore)
            return;
        using (await _semaphoreSlim.LockAsync())
        {
            if (_runBefore)
                return;
            await action();
            _runBefore = true;
        }
    }
}