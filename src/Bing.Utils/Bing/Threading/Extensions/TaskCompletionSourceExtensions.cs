// ReSharper disable once CheckNamespace
namespace Bing.Threading;

/// <summary>
/// 任务完成源(<see cref="TaskCompletionSource{TResult}"/>) 扩展
/// </summary>
public static class TaskCompletionSourceExtensions
{
    /// <summary>
    /// 尝试从已完成任务中完成任务完成源
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="taskCompletionSource">任务完成源</param>
    /// <param name="completedTask">已完成的任务</param>
    /// <returns>如果成功完成任务，则返回true；否则返回false。</returns>
    /// <exception cref="ArgumentNullException">当<paramref name="taskCompletionSource"/>或<paramref name="completedTask"/>为null时抛出</exception>
    /// <exception cref="ArgumentException">当<paramref name="completedTask"/>不是已完成的任务时抛出</exception>
    public static bool TryCompleteFromCompletedTask<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Task<TResult> completedTask)
    {
        if (taskCompletionSource is null)
            throw new ArgumentNullException(nameof(taskCompletionSource));
        if (completedTask is null)
            throw new ArgumentNullException(nameof(completedTask));

        return completedTask.Status switch
        {
            TaskStatus.RanToCompletion => taskCompletionSource.TrySetResult(completedTask.Result),
            TaskStatus.Canceled => taskCompletionSource.TrySetCanceled(),
            TaskStatus.Faulted => taskCompletionSource.TrySetException(completedTask.Exception!.InnerExceptions),
            _ => throw new ArgumentException("Argument must be a completed task", nameof(completedTask))
        };
    }
}