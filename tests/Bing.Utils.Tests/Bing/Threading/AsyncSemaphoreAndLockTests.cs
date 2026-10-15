using System.Threading;
using Bing.Threading.Asyncs;

namespace Bing.Utils.Tests.Bing.Threading;

/// <summary>
/// <see cref="AsyncSemaphore"/> 单元测试
/// </summary>
public class AsyncSemaphoreTests
{
    // ─────────────────────────────────────────────────────────────────
    // 构造函数守卫
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证异步信号量拒绝零初始计数。
    /// </summary>
    [Fact]
    public void Constructor_ZeroCount_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new AsyncSemaphore(0));
    }

    /// <summary>
    /// 验证异步信号量拒绝负初始计数。
    /// </summary>
    [Fact]
    public void Constructor_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new AsyncSemaphore(-1));
    }

    /// <summary>
    /// 验证异步信号量接受正初始计数。
    /// </summary>
    [Fact]
    public void Constructor_PositiveCount_DoesNotThrow()
    {
        Should.NotThrow(() => new AsyncSemaphore(1));
    }

    // ─────────────────────────────────────────────────────────────────
    // WaitAsync
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证信号量可用时等待任务立即完成。
    /// </summary>
    [Fact]
    public void WaitAsync_WhenAvailable_ReturnsCompletedTask()
    {
        var sem = new AsyncSemaphore(1);
        var task = sem.WaitAsync();
        task.IsCompleted.ShouldBeTrue();
    }

    /// <summary>
    /// 验证信号量耗尽后等待任务保持未完成。
    /// </summary>
    [Fact]
    public void WaitAsync_WhenUnavailable_ReturnsPendingTask()
    {
        var sem = new AsyncSemaphore(1);
        sem.WaitAsync(); // 耗尽计数
        var second = sem.WaitAsync();
        second.IsCompleted.ShouldBeFalse();
    }

    /// <summary>
    /// 验证计数为二的信号量仅允许前两次等待立即完成。
    /// </summary>
    [Fact]
    public void WaitAsync_InitialCount2_AllowsTwoWaits()
    {
        var sem = new AsyncSemaphore(2);
        var t1 = sem.WaitAsync();
        var t2 = sem.WaitAsync();
        t1.IsCompleted.ShouldBeTrue();
        t2.IsCompleted.ShouldBeTrue();
        var t3 = sem.WaitAsync();
        t3.IsCompleted.ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // Release
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证释放信号量使待处理的等待任务完成。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task Release_WithPendingWaiter_CompletesWaiter()
    {
        var sem = new AsyncSemaphore(1);
        sem.WaitAsync(); // 耗尽
        var waiter = sem.WaitAsync();
        waiter.IsCompleted.ShouldBeFalse();
        sem.Release();
        await waiter;
        waiter.IsCompleted.ShouldBeTrue();
    }

    /// <summary>
    /// 验证没有等待者时释放信号量不抛出异常。
    /// </summary>
    [Fact]
    public void Release_NoWaiter_DoesNotThrow()
    {
        var sem = new AsyncSemaphore(1);
        // Release 时无等待者，计数自增
        Should.NotThrow(() => sem.Release());
    }

    /// <summary>
    /// 验证计数耗尽后释放信号量唤醒下一等待者。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task WaitAndRelease_MultipleRounds_Work()
    {
        var sem = new AsyncSemaphore(2);
        var t1 = sem.WaitAsync();
        var t2 = sem.WaitAsync();
        var t3 = sem.WaitAsync(); // 需等待
        t3.IsCompleted.ShouldBeFalse();
        sem.Release();
        await t3;
        t3.IsCompleted.ShouldBeTrue();
    }
}

/// <summary>
/// <see cref="AsyncLock"/> 单元测试
/// </summary>
public class AsyncLockTests
{
    // ─────────────────────────────────────────────────────────────────
    // LockAsync
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证首次获取异步锁立即完成。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task LockAsync_FirstCall_CompletesImmediately()
    {
        var lk = new AsyncLock();
        var task = lk.LockAsync();
        task.IsCompleted.ShouldBeTrue();
        var releaser = await task;
        releaser.Dispose();
    }

    /// <summary>
    /// 验证释放异步锁后待处理的获取操作完成。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task LockAsync_Dispose_AllowsSecondLock()
    {
        var lk = new AsyncLock();
        var r1 = await lk.LockAsync();
        var t2 = lk.LockAsync();
        t2.IsCompleted.ShouldBeFalse();

        r1.Dispose(); // 释放第一个锁

        var r2 = await t2;
        r2.Dispose();
    }

    /// <summary>
    /// 验证异步锁保护的并发计数得到预期结果。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task LockAsync_SerializesAccess_CountIsCorrect()
    {
        var lk = new AsyncLock();
        var counter = 0;
        var tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            using var r = await lk.LockAsync();
            counter++;
        });
        await Task.WhenAll(tasks);
        counter.ShouldBe(5);
    }

    // ─────────────────────────────────────────────────────────────────
    // Releaser.Dispose
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证异步锁释放器重复释放不抛出异常。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task Releaser_Dispose_CalledTwice_DoesNotThrow()
    {
        var lk = new AsyncLock();
        var releaser = await lk.LockAsync();
        releaser.Dispose();
        // Dispose 时 _toRelease?._semaphore.Release() — 由 null 守护，不会抛
        Should.NotThrow(() => releaser.Dispose());
    }
}
