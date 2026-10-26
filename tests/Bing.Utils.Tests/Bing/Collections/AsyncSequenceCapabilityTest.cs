using System.Runtime.CompilerServices;
using System.Threading;
using Bing.Collections;

namespace Bing.Utils.Tests.Collections;

/// <summary>
/// 验证异步源的窗口、取消、顺序和收尾。
/// </summary>
public class AsyncSequenceCapabilityTest
{
    /// <summary>
    /// 只允许固定窗口内操作并发，消费者退出后取消并收尾剩余操作。
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BoundedWindowAndEarlyExit(bool ordered)
    {
        var read = 0; var active = 0; var finished = 0; var disposed = false;
        var gates = Enumerable.Range(0, 3).Select(_ => new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        async IAsyncEnumerable<int> Source([EnumeratorCancellation] CancellationToken token = default)
        {
            try { for (var i = 0; i < 100; i++) { token.ThrowIfCancellationRequested(); read++; yield return i; await Task.Yield(); } }
            finally { disposed = true; }
        }
        var results = Source().SelectConcurrentAsync(async (i, token) =>
        {
            if (Interlocked.Increment(ref active) == 3) started.TrySetResult(true);
            try { using (token.Register(() => gates[i].TrySetCanceled())) return await gates[i].Task; }
            finally { Interlocked.Decrement(ref active); Interlocked.Increment(ref finished); }
        }, 3, ordered);
        var iterator = results.GetAsyncEnumerator();
        var moving = iterator.MoveNextAsync().AsTask();
        await started.Task;
        Assert.Equal(3, read); Assert.Equal(3, active);
        gates[ordered ? 0 : 2].SetResult(42);
        Assert.True(await moving); Assert.Equal(42, iterator.Current);
        await iterator.DisposeAsync();
        Assert.True(disposed); Assert.Equal(0, active); Assert.Equal(3, finished); Assert.Equal(3, read);
    }

    /// <summary>
    /// 预取消不启动源，执行取消保留调用方令牌，失败会释放源。
    /// </summary>
    [Fact]
    public async Task CancellationAndFailureDisposeSource()
    {
        var enumerated = false; var disposed = false;
        async IAsyncEnumerable<int> Source([EnumeratorCancellation] CancellationToken token = default)
        {
            enumerated = true;
            try { yield return 1; await Task.Yield(); throw new IOException("source"); }
            finally { disposed = true; }
        }
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Source().ForEachAwaitAsync((_, _) => Task.CompletedTask, 2, canceled.Token));
        Assert.Equal(canceled.Token, ex.CancellationToken); Assert.False(enumerated);
        await Assert.ThrowsAsync<IOException>(() => Source().ForEachAwaitAsync((_, _) => Task.CompletedTask, 2));
        Assert.True(disposed);
        using var during = new CancellationTokenSource();
        ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Source().ForEachAwaitAsync((_, _) => { during.Cancel(); return Task.CompletedTask; }, 1, during.Token));
        Assert.Equal(during.Token, ex.CancellationToken);
        Assert.Throws<ArgumentOutOfRangeException>(() => Source().SelectConcurrentAsync((i, _) => Task.FromResult(i), 0));
    }

    /// <summary>
    /// 有序路径按源顺序输出，空源不会调用委托。
    /// </summary>
    [Fact]
    public async Task PreservesOrderAndSupportsEmptySource()
    {
        async IAsyncEnumerable<int> Source(int count) { for (var i = 0; i < count; i++) { yield return i; await Task.Yield(); } }
        var values = new List<int>();
        await foreach (var value in Source(30).SelectConcurrentAsync(async (i, _) => { await Task.Yield(); return i; }, 4)) values.Add(value);
        Assert.Equal(Enumerable.Range(0, 30), values);
        await Source(0).ForEachAwaitAsync((_, _) => throw new Exception(), 1);
    }
}
