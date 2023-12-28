// ReSharper disable once CheckNamespace
namespace Bing.Threading;

/// <summary>
/// 任务(<see cref="Task"/>) 扩展
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// 等待结果
    /// </summary>
    /// <param name="task">异步操作</param>
    /// <param name="timeout">超时时间。单位：毫秒</param>
    /// <exception cref="TimeoutException"></exception>
    public static void WaitResult(this Task task, int timeout)
    {
        if(!task.Wait(timeout))
            throw new TimeoutException("任务超时未能在指定时间内完成。");
    }

    /// <summary>
    /// 等待结果
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="task">异步操作</param>
    /// <param name="timeout">超时时间。单位：毫秒</param>
    public static TResult WaitResult<TResult>(this Task<TResult> task, int timeout) => task.Wait(timeout) ? task.Result : default;

    /// <summary>
    /// 设置Task过期时间
    /// </summary>
    /// <param name="task">异步操作</param>
    /// <param name="millisecondsDelay">超时时间。单位：毫秒</param>
    public static async Task TimeoutAfter(this Task task, int millisecondsDelay)
    {
        var timeoutCancellationTokenSource = new CancellationTokenSource();
        var completedTask = await Task.WhenAny(task, Task.Delay(millisecondsDelay, timeoutCancellationTokenSource.Token));
        if (completedTask == task)
            timeoutCancellationTokenSource.Cancel();
        else
            throw new TimeoutException("操作已超时。");
    }

    /// <summary>
    /// 设置Task过期时间
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="task">异步操作</param>
    /// <param name="millisecondsDelay">超时时间。单位：毫秒</param>
    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, int millisecondsDelay)
    {
        using var timeoutCancellationTokenSource = new CancellationTokenSource();
        var completedTask = await Task.WhenAny(task, Task.Delay(millisecondsDelay, timeoutCancellationTokenSource.Token));
        if (completedTask == task)
        {
            timeoutCancellationTokenSource.Cancel();
            return await task;
        }
        throw new TimeoutException("操作已超时。");
    }
}