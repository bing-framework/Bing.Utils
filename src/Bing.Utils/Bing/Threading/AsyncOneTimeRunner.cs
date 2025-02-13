namespace Bing.Threading;

/// <summary>
/// 异步一次性运行器，确保指定的操作只运行一次。<br />
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
    /// 是否已经运行过
    /// </summary>
    private volatile bool _runBefore;

    /// <summary>
    /// 用于同步的信号量
    /// </summary>
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    /// <summary>
    /// 运行指定的操作，确保操作只运行一次。
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