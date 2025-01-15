// ReSharper disable once CheckNamespace
namespace Bing.Threading;

/// <summary>
/// 任务(<see cref="Task"/>) 扩展
/// </summary>
public static class TaskExtensions
{
    #region WaitResult(等待结果)

    /// <summary>
    /// 等待结果。
    /// </summary>
    /// <param name="task">异步操作</param>
    /// <param name="timeout">超时时间。单位：毫秒</param>
    /// <exception cref="TimeoutException"></exception>
    public static void WaitResult(this Task task, int timeout)
    {
        if (!task.Wait(timeout))
            throw new TimeoutException("任务超时未能在指定时间内完成。");
    }

    /// <summary>
    /// 等待结果。
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="task">异步操作</param>
    /// <param name="timeout">超时时间。单位：毫秒</param>
    public static TResult WaitResult<TResult>(this Task<TResult> task, int timeout) => task.Wait(timeout) ? task.Result : default;

    #endregion

    #region TimeoutAfter(设置Task过期时间)

    /// <summary>
    /// 设置Task过期时间。
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
    /// 设置Task过期时间。
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

    #endregion

    #region ToCancellationTokenSource(转换为CancellationTokenSource)

    /// <summary>
    /// 将<see cref="Task"/>转换为<see cref="CancellationTokenSource"/>。
    /// </summary>
    /// <param name="task">异步操作</param>
    /// <returns>一个新的<see cref="CancellationTokenSource"/>实例，当提供的<paramref name="task"/>完成时，它将向<see cref="CancellationToken"/>发出取消信号。</returns>
    /// <exception cref="ArgumentNullException">当<paramref name="task"/>为null时抛出.</exception>
    public static CancellationTokenSource ToCancellationTokenSource(this Task task)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));
        var cancellationTokenSource = new CancellationTokenSource();
        task.ContinueWithSynchronously((_, state) => ((CancellationTokenSource)state!).Cancel(), cancellationTokenSource);
        return cancellationTokenSource;
    }

    #endregion

    #region ContinueWithSynchronously(继续执行同步任务)

    /// <summary>
    /// 继续执行同步任务。
    /// </summary>
    /// <param name="task">异步操作</param>
    /// <param name="continuationAction">继续执行的操作</param>
    /// <returns>继续执行的任务</returns>
    /// <exception cref="ArgumentNullException">当<paramref name="task"/>或<paramref name="continuationAction"/>为null时抛出.</exception>
    public static Task ContinueWithSynchronously(this Task task, Action<Task> continuationAction)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));
        if (continuationAction is null)
            throw new ArgumentNullException(nameof(continuationAction));
        return task.ContinueWith(continuationAction, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>
    /// 继续执行同步任务。
    /// </summary>
    /// <param name="task">异步操作</param>
    /// <param name="continuationAction">继续执行的操作</param>
    /// <param name="state">传递给继续执行操作的状态对象</param>
    /// <returns>继续执行的任务</returns>
    /// <exception cref="ArgumentNullException">当<paramref name="task"/>或<paramref name="continuationAction"/>为null时抛出.</exception>
    public static Task ContinueWithSynchronously(this Task task, Action<Task, object> continuationAction, object state)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));
        if (continuationAction is null)
            throw new ArgumentNullException(nameof(continuationAction));
        return task.ContinueWith(continuationAction, state, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>
    /// 继续执行同步任务。
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="task">异步操作</param>
    /// <param name="continuationFunction">继续执行的函数</param>
    /// <returns>继续执行的任务</returns>
    /// <exception cref="ArgumentNullException">当<paramref name="task"/>或<paramref name="continuationFunction"/>为null时抛出.</exception>
    public static Task<TResult> ContinueWithSynchronously<TResult>(this Task task, Func<Task, TResult> continuationFunction)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));
        if (continuationFunction is null)
            throw new ArgumentNullException(nameof(continuationFunction));
        return task.ContinueWith(continuationFunction, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>
    /// 继续执行同步任务。
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="task">异步操作</param>
    /// <param name="continuationFunction">继续执行的函数</param>
    /// <param name="state">传递给继续执行操作的状态对象</param>
    /// <returns>继续执行的任务</returns>
    /// <exception cref="ArgumentNullException">当<paramref name="task"/>或<paramref name="continuationFunction"/>为null时抛出.</exception>
    public static Task<TResult> ContinueWithSynchronously<TResult>(this Task task, Func<Task, object, TResult> continuationFunction, object state)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));
        if (continuationFunction is null)
            throw new ArgumentNullException(nameof(continuationFunction));
        return task.ContinueWith(continuationFunction, state, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>
    /// 继续执行同步任务。
    /// </summary>
    /// <typeparam name="TResult">原始任务的结果类型</typeparam>
    /// <param name="task">异步操作</param>
    /// <param name="continuationAction">继续执行的操作</param>
    /// <returns>继续执行的任务</returns>
    /// <exception cref="ArgumentNullException">当<paramref name="task"/>或<paramref name="continuationAction"/>为null时抛出.</exception>
    public static Task ContinueWithSynchronously<TResult>(this Task<TResult> task, Action<Task<TResult>> continuationAction)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));
        if (continuationAction is null)
            throw new ArgumentNullException(nameof(continuationAction));
        return task.ContinueWith(continuationAction, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>
    /// 继续执行同步任务。
    /// </summary>
    /// <typeparam name="TResult">原始任务的结果类型</typeparam>
    /// <param name="task">异步操作</param>
    /// <param name="continuationAction">继续执行的操作</param>
    /// <param name="state">传递给继续执行操作的状态对象</param>
    /// <returns>继续执行的任务</returns>
    /// <exception cref="ArgumentNullException">当<paramref name="task"/>或<paramref name="continuationAction"/>为null时抛出.</exception>
    public static Task ContinueWithSynchronously<TResult>(this Task<TResult> task, Action<Task<TResult>, object> continuationAction, object state)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));
        if (continuationAction is null)
            throw new ArgumentNullException(nameof(continuationAction));
        return task.ContinueWith(continuationAction, state, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>
    /// 继续执行同步任务。
    /// </summary>
    /// <typeparam name="TResult">原始任务的结果类型</typeparam>
    /// <typeparam name="TNewResult">>继续执行任务的结果类型</typeparam>
    /// <param name="task">异步操作</param>
    /// <param name="continuationFunction">继续执行的函数</param>
    /// <returns>继续执行的任务</returns>
    /// <exception cref="ArgumentNullException">当<paramref name="task"/>或<paramref name="continuationFunction"/>为null时抛出.</exception>
    public static Task<TNewResult> ContinueWithSynchronously<TResult, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TNewResult> continuationFunction)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));
        if (continuationFunction is null)
            throw new ArgumentNullException(nameof(continuationFunction));
        return task.ContinueWith(continuationFunction, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>
    /// 继续执行同步任务。
    /// </summary>
    /// <typeparam name="TResult">原始任务的结果类型</typeparam>
    /// <typeparam name="TNewResult">>继续执行任务的结果类型</typeparam>
    /// <param name="task">异步操作</param>
    /// <param name="continuationFunction">继续执行的函数</param>
    /// <param name="state">传递给继续执行操作的状态对象</param>
    /// <returns>继续执行的任务</returns>
    /// <exception cref="ArgumentNullException">当<paramref name="task"/>或<paramref name="continuationFunction"/>为null时抛出.</exception>
    public static Task<TNewResult> ContinueWithSynchronously<TResult, TNewResult>(this Task<TResult> task, Func<Task<TResult>, object, TNewResult> continuationFunction, object state)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));
        if (continuationFunction is null)
            throw new ArgumentNullException(nameof(continuationFunction));
        return task.ContinueWith(continuationFunction, state, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    #endregion

}