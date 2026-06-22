using System.Threading;
using Shouldly;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Bing.Threading;

// ─────────────────────────────────────────────────────────────────────────────
//  TaskCache Tests
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="TaskCache"/> — 预缓存 Task&lt;bool&gt; 结果。
/// </summary>
public class TaskCacheTests
{
    [Fact]
    public void TrueResult_IsCompletedTask()
    {
        TaskCache.TrueResult.IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public void TrueResult_ReturnsTrue()
    {
        TaskCache.TrueResult.Result.ShouldBeTrue();
    }

    [Fact]
    public void FalseResult_IsCompletedTask()
    {
        TaskCache.FalseResult.IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public void FalseResult_ReturnsFalse()
    {
        TaskCache.FalseResult.Result.ShouldBeFalse();
    }

    [Fact]
    public void TrueResult_SameInstance_WhenAccessedMultipleTimes()
    {
        var first = TaskCache.TrueResult;
        var second = TaskCache.TrueResult;
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    [Fact]
    public void FalseResult_SameInstance_WhenAccessedMultipleTimes()
    {
        var first = TaskCache.FalseResult;
        var second = TaskCache.FalseResult;
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    [Fact]
    public void TrueResult_And_FalseResult_AreDifferentInstances()
    {
        ReferenceEquals(TaskCache.TrueResult, TaskCache.FalseResult).ShouldBeFalse();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  ReaderWriteLockDisposable Tests
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="Locks.ReaderWriteLockDisposable"/> — 读写锁自动释放器。
/// </summary>
public class ReaderWriteLockDisposableTests
{
    // ── 构造函数参数校验 ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullLock_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(
            () => new Locks.ReaderWriteLockDisposable(null!));
    }

    // ── 写锁 ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_DefaultLockType_EntersWriteLock()
    {
        var rwLock = new ReaderWriterLockSlim();
        using (new Locks.ReaderWriteLockDisposable(rwLock))
        {
            rwLock.IsWriteLockHeld.ShouldBeTrue();
        }
        // 释放后锁应已退出
        rwLock.IsWriteLockHeld.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WriteLockType_EntersAndExitsWriteLock()
    {
        var rwLock = new ReaderWriterLockSlim();
        using (new Locks.ReaderWriteLockDisposable(rwLock, Locks.ReaderWriteLockType.Write))
        {
            rwLock.IsWriteLockHeld.ShouldBeTrue();
        }
        rwLock.IsWriteLockHeld.ShouldBeFalse();
    }

    // ── 读锁 ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ReadLockType_EntersAndExitsReadLock()
    {
        var rwLock = new ReaderWriterLockSlim();
        using (new Locks.ReaderWriteLockDisposable(rwLock, Locks.ReaderWriteLockType.Read))
        {
            rwLock.IsReadLockHeld.ShouldBeTrue();
        }
        rwLock.IsReadLockHeld.ShouldBeFalse();
    }

    // ── 升级读锁 ───────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_UpgradeableReadLockType_EntersAndExitsUpgradeableReadLock()
    {
        var rwLock = new ReaderWriterLockSlim();
        using (new Locks.ReaderWriteLockDisposable(rwLock, Locks.ReaderWriteLockType.UpgradeableRead))
        {
            rwLock.IsUpgradeableReadLockHeld.ShouldBeTrue();
        }
        rwLock.IsUpgradeableReadLockHeld.ShouldBeFalse();
    }

    // ── 多次 Dispose 安全 ──────────────────────────────────────────────────────

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var rwLock = new ReaderWriterLockSlim();
        var disposable = new Locks.ReaderWriteLockDisposable(rwLock, Locks.ReaderWriteLockType.Write);
        disposable.Dispose();
        // 第二次 Dispose 不应抛异常（_disposed 保护）
        Should.NotThrow(() => disposable.Dispose());
    }

    // ── 释放后可重新获取锁 ─────────────────────────────────────────────────────

    [Fact]
    public void Dispose_WriteLock_AllowsSubsequentWriteLock()
    {
        var rwLock = new ReaderWriterLockSlim();
        using (new Locks.ReaderWriteLockDisposable(rwLock, Locks.ReaderWriteLockType.Write))
        {
            // 持有写锁期间
            rwLock.IsWriteLockHeld.ShouldBeTrue();
        }
        // 释放后可以再次获取写锁
        using (new Locks.ReaderWriteLockDisposable(rwLock, Locks.ReaderWriteLockType.Write))
        {
            rwLock.IsWriteLockHeld.ShouldBeTrue();
        }
    }

    [Fact]
    public void Dispose_ReadLock_AllowsSubsequentReadLock()
    {
        var rwLock = new ReaderWriterLockSlim();
        using (new Locks.ReaderWriteLockDisposable(rwLock, Locks.ReaderWriteLockType.Read))
        {
            rwLock.IsReadLockHeld.ShouldBeTrue();
        }
        // 释放后可以再次获取读锁
        using (new Locks.ReaderWriteLockDisposable(rwLock, Locks.ReaderWriteLockType.Read))
        {
            rwLock.IsReadLockHeld.ShouldBeTrue();
        }
    }
}
