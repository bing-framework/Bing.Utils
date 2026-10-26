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

    /// <summary>对序列执行异步操作；取消或处理器拒绝继续时停止调度，并等待已启动操作收尾。</summary>
    /// <param name="enumerable">只枚举一次的源序列。</param>
    /// <param name="action">逐项操作。</param>
    /// <param name="maxDegreeOfParallelism">并发上限，须大于零；null 保留无限并发。</param>
    /// <param name="errorHandler">返回 true 忽略当前错误；返回 false 或抛错时停止领取后续元素。</param>
    /// <param name="cancellationToken">外部取消令牌；取消不撤回已启动操作的副作用。</param>
    /// <remarks>有限并发聚合未处理错误，无限并发传播首个未处理错误。外部取消在收尾后优先传播。</remarks>
    public static Task ForEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> action, int? maxDegreeOfParallelism = null, Func<T, Exception, bool> errorHandler = null, CancellationToken cancellationToken = default)
    {
        if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));
        if (action == null) throw new ArgumentNullException(nameof(action));
        ValidateParallelism(maxDegreeOfParallelism);
        return ForEachCoreAsync(enumerable, (item, _) => action(item), maxDegreeOfParallelism, errorHandler, cancellationToken);
    }

    /// <summary>逐项执行接收取消令牌的异步操作，调度及异常语义与 ForEachAsync 相同。</summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="source">只枚举一次的源序列。</param>
    /// <param name="action">接收元素与外部取消令牌的操作。</param>
    /// <param name="maxDegreeOfParallelism">大于零的并发上限；null 表示无限并发。</param>
    /// <param name="errorHandler">返回 true 继续；否则停止领取后续元素，已启动操作仍会被等待。</param>
    /// <param name="cancellationToken">外部取消令牌。</param>
    /// <returns>全部已启动操作收尾后完成的任务。</returns>
    public static Task ForEachWithCancellationAsync<T>(this IEnumerable<T> source, Func<T, CancellationToken, Task> action,
        int? maxDegreeOfParallelism = null, Func<T, Exception, bool> errorHandler = null, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (action == null) throw new ArgumentNullException(nameof(action));
        ValidateParallelism(maxDegreeOfParallelism);
        return ForEachCoreAsync(source, action, maxDegreeOfParallelism, errorHandler, cancellationToken);
    }

    /// <summary>统一验证并发参数，避免不同执行分支接受不同的无效值。</summary>
    private static void ValidateParallelism(int? maxDegreeOfParallelism)
    {
        if (maxDegreeOfParallelism <= 0) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
    }

    /// <summary>有限并发仅保留固定工作任务；无限并发保留既有同时启动语义。</summary>
    private static async Task ForEachCoreAsync<T>(IEnumerable<T> source, Func<T, CancellationToken, Task> action,
        int? parallelism, Func<T, Exception, bool> errorHandler, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var iterator = source.GetEnumerator();
        var gate = new object();
        var stopped = false;
        Exception enumerationError = null;
        var errors = new ConcurrentQueue<Exception>();

        bool TakeNext(out T item)
        {
            lock (gate)
            {
                item = default;
                if (stopped || cancellationToken.IsCancellationRequested) return false;
                try
                {
                    if (!iterator.MoveNext()) { stopped = true; return false; }
                    item = iterator.Current;
                    return true;
                }
                catch (Exception error)
                {
                    enumerationError = error;
                    stopped = true;
                    return false;
                }
            }
        }

        async Task InvokeAsync(T item)
        {
            try { await action(item, cancellationToken).ConfigureAwait(false); }
            catch (Exception error)
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (errorHandler != null)
                {
                    try { if (errorHandler(item, error)) return; }
                    catch (Exception handlerError) { error = handlerError; }
                    lock (gate) stopped = true;
                }
                errors.Enqueue(parallelism.HasValue
                    ? new InvalidOperationException($"处理元素 {item} 时出错: {error.Message}", error)
                    : error);
            }
        }

        async Task WorkerAsync()
        {
            while (TakeNext(out var item)) await InvokeAsync(item).ConfigureAwait(false);
        }

        Exception executionError = null;
        Exception disposalError = null;
        try
        {
            if (parallelism.HasValue)
            {
                var workers = new List<Task>();
                // 只为实际领取到的元素启动工作任务，避免空序列或极大上限分配巨型任务表。
                // 每个工作任务只入队一次；不把令牌传给 Task.Run，以确保已领取操作能够收尾。
                while (workers.Count < parallelism.Value && TakeNext(out var initialItem))
                {
                    var firstItem = initialItem;
                    workers.Add(Task.Run(async () =>
                    {
                        await InvokeAsync(firstItem).ConfigureAwait(false);
                        await WorkerAsync().ConfigureAwait(false);
                    }));
                }
                await Task.WhenAll(workers).ConfigureAwait(false);
            }
            else
            {
                var tasks = new List<Task>();
                while (TakeNext(out var item)) tasks.Add(InvokeAsync(item));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }
        catch (Exception error) { executionError = error; }
        finally
        {
            // 清理失败不能覆盖外部取消或已经观察到的操作/枚举失败。
            try { iterator.Dispose(); }
            catch (Exception error) { disposalError = error; }
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (enumerationError != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(enumerationError).Throw();
        if (executionError != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(executionError).Throw();
        if (parallelism.HasValue && !errors.IsEmpty) throw new AggregateException("在处理集合元素时发生一个或多个错误。", errors);
        if (errors.TryPeek(out var firstError)) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(firstError).Throw();
        if (disposalError != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(disposalError).Throw();
    }
}
