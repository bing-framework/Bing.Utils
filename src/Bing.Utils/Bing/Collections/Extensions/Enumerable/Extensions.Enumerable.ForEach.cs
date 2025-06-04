using System.Collections.Concurrent;

// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 可枚举类型(<see cref="IEnumerable{T}"/>) 扩展
/// </summary>
public static partial class BingEnumerableExtensions
{
    /// <summary>
    /// 对指定集合中的每个元素执行指定操作
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="enumerable">值</param>
    /// <param name="action">操作</param>
    /// <exception cref="ArgumentNullException">源集合对象为空、操作表达式为空</exception>
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        if (enumerable == null)
            throw new ArgumentNullException(nameof(enumerable), $@"源{typeof(T).Name}集合对象不可为空！");
        if (action == null)
            throw new ArgumentNullException(nameof(action), @"操作表达式不可为空！");
        foreach (var item in enumerable)
            action(item);
    }

    /// <summary>
    /// 对指定集合中的每个元素执行指定操作
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="enumerable">值</param>
    /// <param name="action">操作</param>
    /// <exception cref="ArgumentNullException">源集合对象为空、操作表达式为空</exception>
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)
    {
        if (enumerable == null)
            throw new ArgumentNullException(nameof(enumerable), $@"源{typeof(T).Name}集合对象不可为空！");
        if (action == null)
            throw new ArgumentNullException(nameof(action), @"操作表达式不可为空！");

        var index = 0;
        foreach (var item in enumerable)
            action(item, index++);
    }

    /// <summary>
    /// 对指定集合中的每个元素执行指定操作
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="enumerable">值</param>
    /// <param name="action">操作</param>
    /// <param name="maxDegreeOfParallelism">最大并行度，默认为无限制</param>
    /// <param name="errorHandler">错误处理委托，返回 true 继续执行，返回 false 终止后续操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="ArgumentNullException">源集合对象为空、操作表达式为空</exception>
    public static Task ForEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> action, int? maxDegreeOfParallelism = null, Func<T, Exception, bool> errorHandler = null, CancellationToken cancellationToken = default)
    {
        if (enumerable == null)
            throw new ArgumentNullException(nameof(enumerable), $@"源{typeof(T).Name}集合对象不可为空！");
        if (action == null)
            throw new ArgumentNullException(nameof(action), @"操作表达式不可为空！");
        // 如果未指定并行度，直接并行执行所有任务
        if (!maxDegreeOfParallelism.HasValue)
        {
            return errorHandler == null
                ? Task.WhenAll(enumerable.Select(action))
                : HandleErrorsWithoutParallelismLimit(enumerable, action, errorHandler, cancellationToken);
        }

        // 有并行度限制时，使用 SemaphoreSlim 控制并行度
        return ParallelForEachAsync(enumerable, action, maxDegreeOfParallelism.Value, errorHandler, cancellationToken);
    }

    /// <summary>
    /// 不限制并行度的情况下处理错误
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">源</param>
    /// <param name="action">操作</param>
    /// <param name="errorHandler">错误处理委托</param>
    /// <param name="cancellationToken">取消令牌</param>
    private static async Task HandleErrorsWithoutParallelismLimit<T>(IEnumerable<T> source, Func<T, Task> action, Func<T, Exception, bool> errorHandler, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        var items = source.ToList(); // 物化集合以便错误处理时能获取对应的项

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var index = i; // 捕获循环变量

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await action(item);
                }
                catch (Exception ex) when (errorHandler != null)
                {
                    if (!errorHandler(item, ex))
                    {
                        throw; // 如果错误处理器返回 false，重新抛出异常
                    }
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 对指定集合中的每个元素执行指定操作，并提供错误处理
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">源</param>
    /// <param name="action">操作</param>
    /// <param name="maxDegreeOfParallelism">最大并行度</param>
    /// <param name="errorHandler">错误处理委托</param>
    /// <param name="cancellationToken">取消令牌</param>
    private static async Task ParallelForEachAsync<T>(IEnumerable<T> source, Func<T, Task> action, int maxDegreeOfParallelism, Func<T, Exception, bool> errorHandler, CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
        var tasks = new List<Task>();
        var exceptions = new ConcurrentBag<(T Item, Exception Exception)>();
        var itemsList = source.ToList();
        foreach (var item in itemsList)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            await semaphore.WaitAsync(cancellationToken);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await action(item);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    if (errorHandler != null)
                    {
                        if (!errorHandler(item, ex))
                            exceptions.Add((item, ex));
                    }
                    else
                    {
                        exceptions.Add((item, ex));
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }
        await Task.WhenAll(tasks);

        if (exceptions.Count > 0)
            throw new AggregateException("在处理集合元素时发生一个或多个错误。", exceptions.Select(e => new InvalidOperationException($"处理元素 {e.Item} 时出错: {e.Exception.Message}", e.Exception)));
    }
}