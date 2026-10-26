using System.Runtime.CompilerServices;
using System.Threading;
using Bing.Collections;

namespace Bing.Utils.Tests.Collections;

public class AsyncBufferingTest
{
    [Fact]
    public async Task PrefetchIsBoundedOrderedAndDisposesOnEarlyExit()
    {
        var read = 0; var disposed = false; var filled = Signal();
        async IAsyncEnumerable<int> Source([EnumeratorCancellation] CancellationToken token = default)
        {
            try { for (var i = 0; ; i++) { token.ThrowIfCancellationRequested(); if (++read == 4) filled.TrySetResult(true); yield return i; await Task.Yield(); } }
            finally { disposed = true; }
        }
        var iterator = Source().PrefetchAsync(3).GetAsyncEnumerator();
        Assert.True(await iterator.MoveNextAsync()); Assert.Equal(0, iterator.Current); await filled.Task;
        Assert.InRange(read, 3, 4); await iterator.DisposeAsync(); Assert.True(disposed); Assert.InRange(read, 3, 4);
    }
    [Fact]
    public async Task PrefetchDrainsThenPropagatesFailure()
    {
        async IAsyncEnumerable<int> Source() { yield return 1; await Task.Yield(); yield return 2; throw new IOException("source"); }
        var seen = new List<int>();
        await Assert.ThrowsAsync<IOException>(async () => { await foreach (var value in Source().PrefetchAsync(2)) seen.Add(value); });
        Assert.Equal(new[] { 1, 2 }, seen);
    }
    [Fact]
    public async Task CountBatchesPreserveOrderAndFinalPartial()
    {
        async IAsyncEnumerable<int> Source(int count) { for (var i = 0; i < count; i++) { yield return i; await Task.Yield(); } }
        var batches = new List<IReadOnlyList<int>>(); await foreach (var batch in Source(5).BatchAsync(2)) batches.Add(batch);
        Assert.Equal(new[] { 2, 2, 1 }, batches.Select(x => x.Count)); Assert.Equal(Enumerable.Range(0, 5), batches.SelectMany(x => x));
        await foreach (var unused in Source(0).BatchAsync(2)) Assert.True(false);
    }
    [Fact]
    public async Task TimedBatchKeepsOnePendingReadAndCancelsItOnDispose()
    {
        var reading = Signal(); var disposed = false;
        async IAsyncEnumerable<int> Source([EnumeratorCancellation] CancellationToken token = default)
        {
            try { yield return 1; reading.TrySetResult(true); await Task.Delay(Timeout.Infinite, token); yield return 2; }
            finally { disposed = true; }
        }
        var iterator = Source().BatchAsync(10, TimeSpan.FromMilliseconds(25)).GetAsyncEnumerator();
        var next = iterator.MoveNextAsync().AsTask(); await reading.Task;
        Assert.True(await next); Assert.Equal(new[] { 1 }, iterator.Current); await iterator.DisposeAsync(); Assert.True(disposed);
    }
    [Fact]
    public async Task ExpiredBatchWinsWhenSourceAlsoCompletesSynchronously()
    {
        var disposed = false;
        async IAsyncEnumerable<int> Source()
        {
            try
            {
                yield return 1;
                // 模拟 MoveNextAsync 内的同步工作，使读取返回时批次已到期。
                Thread.Sleep(50);
                yield return 2;
                yield return 3;
                await Task.CompletedTask;
            }
            finally { disposed = true; }
        }
        await using var iterator = Source().BatchAsync(10, TimeSpan.FromMilliseconds(1)).GetAsyncEnumerator();
        Assert.True(await iterator.MoveNextAsync());
        Assert.Equal(new[] { 1 }, iterator.Current);
        var rest = new List<int>();
        while (await iterator.MoveNextAsync()) rest.AddRange(iterator.Current);
        Assert.Equal(new[] { 2, 3 }, rest);
        Assert.True(disposed);
    }

    [Fact]
    public async Task PreCancellationDoesNotEnumerate()
    {
        var touched = false;
        async IAsyncEnumerable<int> Source() { touched = true; await Task.Yield(); yield return 1; }
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await foreach (var _ in Source().PrefetchAsync(1, cts.Token)) { } });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await foreach (var _ in Source().BatchAsync(1, cancellationToken: cts.Token)) { } });
        Assert.False(touched);
        Assert.Throws<ArgumentOutOfRangeException>(() => Source().BatchAsync(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Source().PrefetchAsync(0));
    }
    private static TaskCompletionSource<bool> Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
