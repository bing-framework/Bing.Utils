using System.Threading;
using Bing.Threading;

namespace Bing.Utils.Tests.Bing.Threading;

/// <summary>
/// <see cref="SemaphoreSlimExtensions"/> 单元测试
/// </summary>
public class SemaphoreSlimExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // LockAsync（无参）
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证异步锁定作用域内持有信号量且退出后释放。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task LockAsync_NoArgs_HoldsLockInsideScope()
    {
        var sem = new SemaphoreSlim(1, 1);
        using (await sem.LockAsync())
        {
            sem.CurrentCount.ShouldBe(0);
        }
        sem.CurrentCount.ShouldBe(1);
    }

    /// <summary>
    /// 验证释放异步锁定对象归还信号量计数。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task LockAsync_Dispose_ReleasesLock()
    {
        var sem = new SemaphoreSlim(1, 1);
        var disposable = await sem.LockAsync();
        sem.CurrentCount.ShouldBe(0);
        disposable.Dispose();
        sem.CurrentCount.ShouldBe(1);
    }

    // ─────────────────────────────────────────────────────────────────
    // LockAsync（CancellationToken）
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证异步锁定接受未取消的令牌。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task LockAsync_WithCancellationToken_AcquiresLock()
    {
        var sem = new SemaphoreSlim(1, 1);
        using (await sem.LockAsync(CancellationToken.None))
        {
            sem.CurrentCount.ShouldBe(0);
        }
        sem.CurrentCount.ShouldBe(1);
    }

    // ─────────────────────────────────────────────────────────────────
    // LockAsync（millisecondsTimeout）
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证信号量可用时在毫秒超时内获取锁。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task LockAsync_MillisecondsTimeout_AcquiresWhenAvailable()
    {
        var sem = new SemaphoreSlim(1, 1);
        using (await sem.LockAsync(5000))
        {
            sem.CurrentCount.ShouldBe(0);
        }
        sem.CurrentCount.ShouldBe(1);
    }

    /// <summary>
    /// 验证毫秒超时内未获取信号量时抛出超时异常。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task LockAsync_MillisecondsTimeout_ThrowsWhenNotAcquired()
    {
        var sem = new SemaphoreSlim(1, 1);
        await sem.WaitAsync(); // 耗尽
        await Should.ThrowAsync<TimeoutException>(async () =>
        {
            await sem.LockAsync(1); // 1ms 超时
        });
        sem.Release();
    }

    // ─────────────────────────────────────────────────────────────────
    // LockAsync（TimeSpan）
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证信号量可用时在时间间隔超时内获取锁。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task LockAsync_TimeSpan_AcquiresWhenAvailable()
    {
        var sem = new SemaphoreSlim(1, 1);
        using (await sem.LockAsync(TimeSpan.FromSeconds(5)))
        {
            sem.CurrentCount.ShouldBe(0);
        }
        sem.CurrentCount.ShouldBe(1);
    }

    /// <summary>
    /// 验证时间间隔超时内未获取信号量时抛出超时异常。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task LockAsync_TimeSpan_ThrowsWhenNotAcquired()
    {
        var sem = new SemaphoreSlim(1, 1);
        await sem.WaitAsync();
        await Should.ThrowAsync<TimeoutException>(async () =>
        {
            await sem.LockAsync(TimeSpan.FromMilliseconds(1));
        });
        sem.Release();
    }

    // ─────────────────────────────────────────────────────────────────
    // Lock（无参，同步版本）
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证同步锁定作用域内持有信号量且退出后释放。
    /// </summary>
    [Fact]
    public void Lock_NoArgs_HoldsLockInsideScope()
    {
        var sem = new SemaphoreSlim(1, 1);
        using (sem.Lock())
        {
            sem.CurrentCount.ShouldBe(0);
        }
        sem.CurrentCount.ShouldBe(1);
    }

    /// <summary>
    /// 验证释放同步锁定对象归还信号量计数。
    /// </summary>
    [Fact]
    public void Lock_Dispose_ReleasesLock()
    {
        var sem = new SemaphoreSlim(1, 1);
        var disposable = sem.Lock();
        sem.CurrentCount.ShouldBe(0);
        disposable.Dispose();
        sem.CurrentCount.ShouldBe(1);
    }

    // ─────────────────────────────────────────────────────────────────
    // Lock（CancellationToken）
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证同步锁定接受未取消的令牌。
    /// </summary>
    [Fact]
    public void Lock_WithCancellationToken_AcquiresLock()
    {
        var sem = new SemaphoreSlim(1, 1);
        using (sem.Lock(CancellationToken.None))
        {
            sem.CurrentCount.ShouldBe(0);
        }
        sem.CurrentCount.ShouldBe(1);
    }

    // ─────────────────────────────────────────────────────────────────
    // Lock（millisecondsTimeout）
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证同步毫秒等待超时后抛出超时异常。
    /// </summary>
    [Fact]
    public void Lock_MillisecondsTimeout_ThrowsWhenNotAcquired()
    {
        var sem = new SemaphoreSlim(1, 1);
        sem.Wait();
        Should.Throw<TimeoutException>(() => sem.Lock(1));
        sem.Release();
    }

    // ─────────────────────────────────────────────────────────────────
    // Lock（TimeSpan）
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证同步时间间隔等待超时后抛出超时异常。
    /// </summary>
    [Fact]
    public void Lock_TimeSpan_ThrowsWhenNotAcquired()
    {
        var sem = new SemaphoreSlim(1, 1);
        sem.Wait();
        Should.Throw<TimeoutException>(() => sem.Lock(TimeSpan.FromMilliseconds(1)));
        sem.Release();
    }
}
