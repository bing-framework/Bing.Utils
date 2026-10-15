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
    /// <summary>
    /// 验证缓存的真结果任务已经完成。
    /// </summary>
    [Fact]
    public void TrueResult_IsCompletedTask()
    {
        TaskCache.TrueResult.IsCompleted.ShouldBeTrue();
    }

    /// <summary>
    /// 验证缓存的真结果任务返回真。
    /// </summary>
    [Fact]
    public void TrueResult_ReturnsTrue()
    {
        TaskCache.TrueResult.Result.ShouldBeTrue();
    }

    /// <summary>
    /// 验证缓存的假结果任务已经完成。
    /// </summary>
    [Fact]
    public void FalseResult_IsCompletedTask()
    {
        TaskCache.FalseResult.IsCompleted.ShouldBeTrue();
    }

    /// <summary>
    /// 验证缓存的假结果任务返回假。
    /// </summary>
    [Fact]
    public void FalseResult_ReturnsFalse()
    {
        TaskCache.FalseResult.Result.ShouldBeFalse();
    }

    /// <summary>
    /// 验证重复访问真结果返回相同任务实例。
    /// </summary>
    [Fact]
    public void TrueResult_SameInstance_WhenAccessedMultipleTimes()
    {
        var first = TaskCache.TrueResult;
        var second = TaskCache.TrueResult;
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    /// <summary>
    /// 验证重复访问假结果返回相同任务实例。
    /// </summary>
    [Fact]
    public void FalseResult_SameInstance_WhenAccessedMultipleTimes()
    {
        var first = TaskCache.FalseResult;
        var second = TaskCache.FalseResult;
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    /// <summary>
    /// 验证真假结果采用不同任务实例。
    /// </summary>
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

    /// <summary>
    /// 验证读写锁释放器拒绝空锁。
    /// </summary>
    [Fact]
    public void Constructor_NullLock_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(
            () => new Locks.ReaderWriteLockDisposable(null!));
    }

    // ── 写锁 ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证默认进入写锁并在作用域退出后释放。
    /// </summary>
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

    /// <summary>
    /// 验证指定写锁类型时进入并释放写锁。
    /// </summary>
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

    /// <summary>
    /// 验证指定读锁类型时进入并释放读锁。
    /// </summary>
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

    /// <summary>
    /// 验证指定可升级读锁时进入并释放对应锁。
    /// </summary>
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

    /// <summary>
    /// 验证读写锁释放器重复释放不抛出异常。
    /// </summary>
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

    /// <summary>
    /// 验证写锁释放后可以再次获取写锁。
    /// </summary>
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

    /// <summary>
    /// 验证读锁释放后可以再次获取读锁。
    /// </summary>
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
