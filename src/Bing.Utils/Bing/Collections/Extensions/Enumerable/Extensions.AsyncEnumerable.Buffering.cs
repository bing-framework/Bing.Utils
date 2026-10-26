using System.Runtime.ExceptionServices;
using System.Diagnostics;

namespace Bing.Collections;

/// <summary>
/// 异步序列的有界预取与批处理扩展。
/// </summary>
public static class AsyncSequenceBufferingExtensions
{
    /// <summary>
    /// 以固定队列容量预取源，维持原始顺序；容量不包含已经交付给消费者的元素。
    /// </summary>
    /// <remarks>提前结束时取消生产者并等待其收尾，因此源应响应取消。不丢弃错误前已入队的元素；队列排空后传播源错误。
    /// 每个枚举器仅有一次在途 MoveNextAsync，不为每个元素创建工作任务。</remarks>
    /// <returns>有界预取后的异步序列。</returns>
    public static IAsyncEnumerable<T> PrefetchAsync<T>(this IAsyncEnumerable<T> source, int capacity, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (capacity <= 0 || capacity == int.MaxValue) throw new ArgumentOutOfRangeException(nameof(capacity));
        return PrefetchCore(source, capacity, cancellationToken);
    }

    /// <summary>
    /// 在达到数量或首条记录等待时长时输出非空批次；结束时交付剩余记录。
    /// </summary>
    /// <remarks>maxDelay 为 null 时只按数量。计时从每批第一条记录开始；消费者处理批次的耗时不计入下一批。
    /// 时间触发时允许保留一次在途源读取；提前结束会取消并等待该读取，要求源响应取消。
    /// 到期批次优先于同时完成的源读取；同步阻塞的 MoveNextAsync 无法被抢占，返回后立即检查到期时间。</remarks>
    /// <returns>独立的只读批次，每批最多 maxCount 条。</returns>
    public static IAsyncEnumerable<IReadOnlyList<T>> BatchAsync<T>(this IAsyncEnumerable<T> source, int maxCount,
        TimeSpan? maxDelay = null, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (maxCount <= 0) throw new ArgumentOutOfRangeException(nameof(maxCount));
        if (maxDelay.HasValue && (maxDelay <= TimeSpan.Zero || maxDelay.Value.TotalMilliseconds > int.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(maxDelay));
        return BatchCore(source, maxCount, maxDelay, cancellationToken);
    }

    /// <summary>
    /// 生产者取得空位后才读取源，完成信号额外唤醒消费者一次。
    /// </summary>
    private static async IAsyncEnumerable<T> PrefetchCore<T>(IAsyncEnumerable<T> source, int capacity,
        [EnumeratorCancellation] CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
        using var spaces = new SemaphoreSlim(capacity, capacity);
        using var ready = new SemaphoreSlim(0, capacity + 1);
        var queue = new Queue<T>(); var gate = new object(); ExceptionDispatchInfo failure = null; var done = false;
        async Task Produce()
        {
            try
            {
                await using var iterator = source.GetAsyncEnumerator(lifetime.Token);
                while (true)
                {
                    await spaces.WaitAsync(lifetime.Token).ConfigureAwait(false);
                    if (!await iterator.MoveNextAsync().ConfigureAwait(false)) break;
                    lifetime.Token.ThrowIfCancellationRequested();
                    lock (gate) queue.Enqueue(iterator.Current);
                    ready.Release();
                }
            }
            catch (Exception error) { failure = ExceptionDispatchInfo.Capture(error); }
            finally { lock (gate) done = true; ready.Release(); }
        }
        var producer = Produce();
        try
        {
            while (true)
            {
                await ready.WaitAsync(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                T item = default; bool found; bool ended;
                lock (gate) { found = queue.Count != 0; if (found) item = queue.Dequeue(); ended = done; }
                if (found) { spaces.Release(); yield return item; }
                else if (ended) { failure?.Throw(); break; }
            }
        }
        finally { try { lifetime.Cancel(); } finally { await producer.ConfigureAwait(false); } }
    }

    /// <summary>
    /// 在单一源读取与批次计时器之间选择，不并发访问枚举器。
    /// </summary>
    private static async IAsyncEnumerable<IReadOnlyList<T>> BatchCore<T>(IAsyncEnumerable<T> source, int maximum,
        TimeSpan? delay, [EnumeratorCancellation] CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
        var iterator = source.GetAsyncEnumerator(lifetime.Token);
        Task<bool> pending = null; CancellationTokenSource timerCancellation = null;
        try
        {
            var batch = new List<T>(Math.Min(maximum, 256)); Task timer = null; long batchStarted = 0;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (batch.Count != 0 && timer != null)
                {
                    // 单调时钟同时覆盖计时器尚未获调度及读取同步完成的情况。
                    bool Expired() => timer.IsCompleted ||
                        (Stopwatch.GetTimestamp() - batchStarted) / (double)Stopwatch.Frequency >= delay.Value.TotalSeconds;
                    if (!Expired())
                    {
                        pending ??= MoveNext();
                        await Task.WhenAny(timer, pending).ConfigureAwait(false);
                    }
                    token.ThrowIfCancellationRequested();
                    if (Expired())
                    {
                        timerCancellation.Cancel(); timerCancellation.Dispose(); timerCancellation = null; timer = null;
                        var output = batch.AsReadOnly(); batch = new List<T>(Math.Min(maximum, 256));
                        yield return output; continue;
                    }
                }
                pending ??= MoveNext();
                var next = await pending.ConfigureAwait(false); pending = null;
                token.ThrowIfCancellationRequested();
                if (!next)
                {
                    if (batch.Count != 0) yield return batch.AsReadOnly();
                    break;
                }
                batch.Add(iterator.Current);
                if (batch.Count == maximum)
                {
                    timerCancellation?.Cancel(); timerCancellation?.Dispose(); timerCancellation = null; timer = null;
                    var output = batch.AsReadOnly(); batch = new List<T>(Math.Min(maximum, 256)); yield return output;
                }
                else if (batch.Count == 1 && delay.HasValue)
                {
                    batchStarted = Stopwatch.GetTimestamp();
                    timerCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                    timer = Task.Delay(delay.Value, timerCancellation.Token);
                }
            }
        }
        finally
        {
            timerCancellation?.Cancel(); timerCancellation?.Dispose();
            try { lifetime.Cancel(); }
            finally
            {
                try { if (pending != null) try { await pending.ConfigureAwait(false); } catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { } }
                finally { await iterator.DisposeAsync().ConfigureAwait(false); }
            }
        }

        async Task<bool> MoveNext()
        {
            try { return await iterator.MoveNextAsync().ConfigureAwait(false); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw new OperationCanceledException(token); }
        }
    }
}
