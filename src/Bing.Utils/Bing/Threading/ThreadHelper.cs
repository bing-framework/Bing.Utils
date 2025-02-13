namespace Bing.Threading;

/// <summary>
/// 线程帮助类
/// </summary>
public static class ThreadHelper
{
    /// <summary>
    /// 执行多个任务，仅需要其中一个任务完成即可。
    /// </summary>
    /// <typeparam name="T">任务返回的类型</typeparam>
    /// <param name="functions">需要执行的任务集合</param>
    /// <returns>第一个完成的任务结果</returns>
    /// <remarks>
    /// 该方法会启动所有传入的任务，等待其中任何一个任务完成后，立即取消其他任务的执行。
    /// </remarks>
    public static async Task<T> NeedOnlyOne<T>(params Func<CancellationToken, Task<T>>[] functions)
    {
        if (functions == null || functions.Length == 0)
            throw new ArgumentNullException("至少需要提供一个任务函数", nameof(functions));
        using var cts = new CancellationTokenSource();
        var tasks = functions.Select(func => func(cts.Token)).ToList();
        var exceptions = new List<Exception>();

        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
            tasks.Remove(completedTask);
            if (completedTask.IsCanceled)
            {
                // 任务被取消，记录异常
                exceptions.Add(new OperationCanceledException("任务已被取消"));
            }
            else if (completedTask.IsFaulted)
            {
                // 任务发生异常，记录异常
                exceptions.Add(completedTask.Exception?.InnerException ?? new Exception("任务发生未知异常"));
            }
            else if (completedTask.Status == TaskStatus.RanToCompletion)
            {
                // 任务成功完成，取消其他任务病返回结果
                cts.Cancel();
                return await completedTask.ConfigureAwait(false);
            }
        }
        // 所有任务都失败或被取消，抛出包含所有异常的 AggregateException
        throw new AggregateException("所有任务都已失败或被取消。", exceptions);
    }
}