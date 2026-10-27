using System.Threading;
using Bing.Threading;

namespace Bing.Utils.Tests.Bing.Threading;

/// <summary>
/// 验证按键互斥、取消隔离和条目回收。
/// </summary>
public class KeyedAsyncLockTest
{
    /// <summary>
    /// 同键等待与异键获取相互独立，重复释放不会增加许可数。
    /// </summary>
    [Fact]
    public async Task Keys_AreIsolated_AndLeaseIsIdempotent()
    {
        var locks = new KeyedAsyncLock<string>();
        var first = await locks.LockAsync("a");
        var waiting = locks.LockAsync("a");
        waiting.IsCompleted.ShouldBeFalse();
        var other = await locks.LockAsync("b");
        locks.ActiveKeyCount.ShouldBe(2);
        first.Dispose();
        first.Dispose();
        using var second = await Guard(waiting);
        (await locks.TryLockAsync("a", TimeSpan.Zero)).ShouldBeNull();
        other.Dispose();
        second.Dispose();
        locks.ActiveKeyCount.ShouldBe(0);
    }

    /// <summary>
    /// 超时和取消不会遗留引用或影响持有者。
    /// </summary>
    [Fact]
    public async Task CanceledAndTimedOutWaiters_AreReclaimed()
    {
        var locks = new KeyedAsyncLock<int>();
        using var held = await locks.LockAsync(1);
        using var cancellation = new CancellationTokenSource();
        var canceled = locks.LockAsync(1, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        canceled.IsCanceled.ShouldBeTrue();
        (await locks.TryLockAsync(1, TimeSpan.Zero)).ShouldBeNull();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => locks.LockAsync(2, cancellation.Token));
        locks.ActiveKeyCount.ShouldBe(1);
        held.Dispose();
        locks.ActiveKeyCount.ShouldBe(0);
        using var reacquired = await locks.LockAsync(1);
    }

    /// <summary>
    /// 使用自定义键比较器并验证参数边界。
    /// </summary>
    [Fact]
    public async Task ComparerAndArguments_AreRespected()
    {
        var locks = new KeyedAsyncLock<string>(StringComparer.OrdinalIgnoreCase);
        using var held = await locks.LockAsync("中文A");
        (await locks.TryLockAsync("中文a", TimeSpan.Zero)).ShouldBeNull();
        Should.Throw<ArgumentNullException>(() => locks.LockAsync(null));
        Should.Throw<ArgumentOutOfRangeException>(() => locks.TryLockAsync("x", TimeSpan.FromTicks(-1)));
        Should.Throw<ArgumentOutOfRangeException>(() => locks.TryLockAsync("x", TimeSpan.FromMilliseconds((double)int.MaxValue + 1)));
    }

    /// <summary>
    /// 在释放、重新获取及条目删除竞争中保持同键最大并发为一。
    /// </summary>
    [Fact]
    public async Task ReleaseAndReacquire_RaceMaintainsMutualExclusion()
    {
        var locks = new KeyedAsyncLock<string>();
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = 0;
        var maximum = 0;
        var executions = 0;
        var workers = Enumerable.Range(0, 24).Select(_ => Task.Run(async () =>
        {
            await start.Task;
            for (var iteration = 0; iteration < 50; iteration++)
            {
                using (await locks.LockAsync("same"))
                {
                    var current = Interlocked.Increment(ref active);
                    maximum = Math.Max(maximum, current);
                    current.ShouldBe(1);
                    await Task.Yield();
                    Interlocked.Increment(ref executions);
                    Interlocked.Decrement(ref active);
                }
            }
        })).ToArray();
        start.SetResult(true);
        await Guard(Task.WhenAll(workers).ContinueWith(t => { t.GetAwaiter().GetResult(); return true; }));
        maximum.ShouldBe(1);
        executions.ShouldBe(1200);
        locks.ActiveKeyCount.ShouldBe(0);
    }

    /// <summary>
    /// 用截止保护避免实现缺陷使测试无限等待。
    /// </summary>
    /// <typeparam name="T">任务结果类型。</typeparam>
    /// <param name="task">待验证任务。</param>
    /// <returns>任务结果。</returns>
    private static async Task<T> Guard<T>(Task<T> task)
    {
        (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(15)))).ShouldBeSameAs(task);
        return await task;
    }
}
