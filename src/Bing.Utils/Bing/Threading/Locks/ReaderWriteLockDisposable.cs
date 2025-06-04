namespace Bing.Threading.Locks;

/// <summary>
/// 读写锁释放器
/// </summary>
public class ReaderWriteLockDisposable : IDisposable
{
    /// <summary>
    /// 是否已释放
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 读写锁
    /// </summary>
    private readonly ReaderWriterLockSlim _rwLock;

    /// <summary>
    /// 读写锁类型
    /// </summary>
    private readonly ReaderWriteLockType _readerWriteLockType;

    /// <summary>
    /// 初始化一个<see cref="ReaderWriteLockDisposable"/>类型的实例
    /// </summary>
    /// <param name="rwLock">读写锁</param>
    /// <param name="readerWriteLockType">读写锁类型</param>
    public ReaderWriteLockDisposable(ReaderWriterLockSlim rwLock, ReaderWriteLockType readerWriteLockType = ReaderWriteLockType.Write)
    {
        _rwLock = rwLock ?? throw new ArgumentNullException(nameof(rwLock), "读写锁不能为空");
        _readerWriteLockType = readerWriteLockType;

        switch (_readerWriteLockType)
        {
            case ReaderWriteLockType.Read:
                _rwLock.EnterReadLock();
                break;

            case ReaderWriteLockType.Write:
                _rwLock.EnterWriteLock();
                break;

            case ReaderWriteLockType.UpgradeableRead:
                _rwLock.EnterUpgradeableReadLock();
                break;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="disposing">是否释放中</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            switch (_readerWriteLockType)
            {
                case ReaderWriteLockType.Read:
                    _rwLock.ExitReadLock();
                    break;
                case ReaderWriteLockType.Write:
                    _rwLock.ExitWriteLock();
                    break;
                case ReaderWriteLockType.UpgradeableRead:
                    _rwLock.ExitUpgradeableReadLock();
                    break;
            }
        }

        // 标记为已释放
        _disposed = true;
    }
}