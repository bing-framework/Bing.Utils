using System.Runtime.ExceptionServices;

namespace Bing.Collections;

/// <summary>
/// 为异步序列提供有界的并发消费和转换。
/// </summary>
public static class BingAsyncEnumerableExtensions
{
    /// <summary>
    /// 有界并发处理异步序列，失败或取消后停止调度并等待已启动操作。
    /// </summary>
    /// <typeparam name="T">源元素类型。</typeparam>
    /// <param name="source">只枚举一次的异步源。</param>
    /// <param name="action">操作；令牌同时响应外部取消与内部停止。</param>
    /// <param name="maxDegreeOfParallelism">必须大于零的最大在途操作数。</param>
    /// <param name="cancellationToken">外部取消令牌。</param>
    public static async Task ForEachAwaitAsync<T>(this IAsyncEnumerable<T> source, Func<T, CancellationToken, Task> action,
        int maxDegreeOfParallelism, CancellationToken cancellationToken = default)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        await foreach (var unused in SelectConcurrentAsync(source, async (item, token) =>
        {
            await action(item, token).ConfigureAwait(false);
            return true;
        }, maxDegreeOfParallelism, false, cancellationToken).ConfigureAwait(false)) { }
    }

    /// <summary>
    /// 使用有界窗口并发转换异步序列，可选择源顺序或完成顺序输出。
    /// </summary>
    /// <typeparam name="T">源元素类型。</typeparam>
    /// <typeparam name="TResult">结果类型。</typeparam>
    /// <param name="source">异步源；MoveNext 不会并发调用。</param>
    /// <param name="selector">转换；令牌响应外部取消和消费者提前停止。</param>
    /// <param name="maxDegreeOfParallelism">最大在途操作数，必须大于零。</param>
    /// <param name="preserveOrder">true 保持源顺序；false 按已完成任务输出，同批完成次序不保证。</param>
    /// <param name="cancellationToken">外部取消令牌。</param>
    /// <returns>逐项结果；活动任务与重排结果最多保留并发度个，另允许一个在途源读取。</returns>
    /// <remarks>首个观察到的失败停止调度并传播；收尾失败不覆盖主失败。外部取消优先传播外部令牌。委托须合作响应取消，无法撤回已发生副作用。</remarks>
    public static IAsyncEnumerable<TResult> SelectConcurrentAsync<T, TResult>(this IAsyncEnumerable<T> source,
        Func<T, CancellationToken, Task<TResult>> selector, int maxDegreeOfParallelism,
        bool preserveOrder = true, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (maxDegreeOfParallelism <= 0) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
        return SelectCore(source, selector, maxDegreeOfParallelism, preserveOrder, cancellationToken);
    }

    /// <summary>
    /// 在单个源读取和有界任务窗口之间调度，慢速源不阻塞已就绪结果。
    /// </summary>
    private static async IAsyncEnumerable<TResult> SelectCore<T, TResult>(IAsyncEnumerable<T> source,
        Func<T, CancellationToken, Task<TResult>> selector, int degree, bool ordered,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pending = new List<Task<TResult>>(degree);
        IAsyncEnumerator<T> enumerator = null;
        Task<bool> moving = null;
        Exception primaryError = null;
        var ended = false;
        try
        {
            enumerator = source.GetAsyncEnumerator(stop.Token);
            while (true)
            {
                var step = await NextAsync().ConfigureAwait(false);
                if (!step.HasValue) break;
                yield return step.Value;
            }
        }
        finally
        {
            Exception cleanupError = null;
            try { stop.Cancel(); }
            catch (Exception ex) { cleanupError = ex; }
            if (moving != null)
            {
                try { await moving.ConfigureAwait(false); }
                catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
                catch (Exception ex) { cleanupError ??= ex; }
            }
            try { await Task.WhenAll(pending).ConfigureAwait(false); }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            catch (Exception ex) { cleanupError ??= ex; }
            if (enumerator != null)
            {
                try { await enumerator.DisposeAsync().ConfigureAwait(false); }
                catch (Exception ex) { cleanupError ??= ex; }
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (primaryError == null && cleanupError != null) ExceptionDispatchInfo.Capture(cleanupError).Throw();
        }

        async Task<(bool HasValue, TResult Value)> NextAsync()
        {
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var failed = pending.FirstOrDefault(t => t.IsFaulted || t.IsCanceled);
                    if (failed != null) await failed.ConfigureAwait(false);
                    if (!ended && moving == null && pending.Count < degree)
                        moving = enumerator.MoveNextAsync().AsTask();
                    if (moving != null && moving.IsCompleted)
                    {
                        var hasNext = await moving.ConfigureAwait(false);
                        moving = null;
                        if (!hasNext) ended = true;
                        else
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            failed = pending.FirstOrDefault(t => t.IsFaulted || t.IsCanceled);
                            if (failed != null) await failed.ConfigureAwait(false);
                            pending.Add(InvokeAsync(enumerator.Current));
                        }
                        continue;
                    }
                    if (pending.Count != 0)
                    {
                        var ready = ordered ? pending[0] : pending.FirstOrDefault(t => t.IsCompleted);
                        if (ready != null && ready.IsCompleted)
                        {
                            var value = await ready.ConfigureAwait(false);
                            pending.Remove(ready);
                            return (true, value);
                        }
                    }
                    else if (ended) return (false, default);
                    var wait = pending.Where(t => !t.IsCompleted).Cast<Task>().ToList();
                    if (moving != null) wait.Add(moving);
                    await Task.WhenAny(wait).ConfigureAwait(false);
                }
            }
            catch (Exception ex) { primaryError = ex; throw; }
        }

        async Task<TResult> InvokeAsync(T item)
        {
            var task = selector(item, stop.Token);
            if (task == null) throw new InvalidOperationException("异步转换委托返回了 null 任务。");
            return await task.ConfigureAwait(false);
        }
    }
}
