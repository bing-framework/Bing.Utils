using Bing.Threading;
using System.Threading;

namespace Bing.Utils.Tests.Bing.Threading;

/// <summary>
/// <see cref="SemaphoreSlimExtensions"/> 单元测试
/// </summary>
public class SemaphoreSlimExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // LockAsync（无参）
    // ─────────────────────────────────────────────────────────────────

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

    [Fact]
    public void Lock_TimeSpan_ThrowsWhenNotAcquired()
    {
        var sem = new SemaphoreSlim(1, 1);
        sem.Wait();
        Should.Throw<TimeoutException>(() => sem.Lock(TimeSpan.FromMilliseconds(1)));
        sem.Release();
    }
}
