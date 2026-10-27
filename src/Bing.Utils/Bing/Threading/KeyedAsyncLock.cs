namespace Bing.Threading;

/// <summary>
/// 在同一实例内按键提供异步互斥。
/// </summary>
/// <typeparam name="TKey">标识互斥资源的键类型，键不可为 null。</typeparam>
/// <remarks>调用方必须复用实例。锁不可重入，不保证严格 FIFO；空闲键自动回收。</remarks>
public sealed class KeyedAsyncLock<TKey>
{
    /// <summary>
    /// 保护条目登记、引用计数和回收的同步对象。
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// 保存仍有持有者或等待者的键。
    /// </summary>
    private readonly Dictionary<TKey, Entry> _entries;

    /// <summary>
    /// 初始化按键互斥组件。
    /// </summary>
    /// <param name="comparer">键比较器；null 使用类型的默认比较器。</param>
    public KeyedAsyncLock(IEqualityComparer<TKey> comparer = null) => _entries = new Dictionary<TKey, Entry>(comparer);

    /// <summary>
    /// 获取仍被持有或等待的键数量。
    /// </summary>
    internal int ActiveKeyCount { get { lock (_gate) return _entries.Count; } }

    /// <summary>
    /// 等待取得指定键的互斥租约。
    /// </summary>
    /// <param name="key">需要互斥的键，不可为 null。</param>
    /// <param name="cancellationToken">只取消本次等待，不撤销已经取得的租约。</param>
    /// <returns>取得的租约；调用方必须释放。</returns>
    /// <exception cref="ArgumentNullException">键为 null。</exception>
    /// <exception cref="OperationCanceledException">等待被取消。</exception>
    /// <example><code>using (await locks.LockAsync(orderId, cancellationToken)) { await UpdateAsync(); }</code></example>
    public Task<KeyedLockLease> LockAsync(TKey key, CancellationToken cancellationToken = default) =>
        TryLockAsync(key, Timeout.InfiniteTimeSpan, cancellationToken);

    /// <summary>
    /// 在指定等待时间内尝试取得互斥租约。
    /// </summary>
    /// <param name="key">需要互斥的键，不可为 null。</param>
    /// <param name="timeout">等待时间；允许零或 InfiniteTimeSpan，其余须在 0 至 Int32.MaxValue 毫秒内。</param>
    /// <param name="cancellationToken">只取消本次等待。</param>
    /// <returns>取得的租约；等待超时返回 null，取消不会转换成超时。</returns>
    /// <exception cref="ArgumentNullException">键为 null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">等待时间无效。</exception>
    /// <exception cref="OperationCanceledException">等待被取消。</exception>
    public Task<KeyedLockLease> TryLockAsync(TKey key, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        if (timeout != Timeout.InfiniteTimeSpan && (timeout < TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(timeout), "等待时间必须为非负值或无限等待，且不超过 Int32.MaxValue 毫秒。");
        if (cancellationToken.IsCancellationRequested) return Task.FromCanceled<KeyedLockLease>(cancellationToken);

        Entry entry;
        lock (_gate)
        {
            if (!_entries.TryGetValue(key, out entry)) _entries.Add(key, entry = new Entry());
            entry.References++;
        }
        return AcquireAsync(key, entry, timeout, cancellationToken);
    }

    /// <summary>
    /// 将已登记引用转换为租约，失败时归还引用。
    /// </summary>
    /// <param name="key">已登记的键。</param>
    /// <param name="entry">本次登记的条目。</param>
    /// <param name="timeout">等待时间。</param>
    /// <param name="token">等待取消令牌。</param>
    /// <returns>租约，或超时返回 null。</returns>
    private async Task<KeyedLockLease> AcquireAsync(TKey key, Entry entry, TimeSpan timeout, CancellationToken token)
    {
        var acquired = false;
        try
        {
            acquired = await entry.Semaphore.WaitAsync(timeout, token).ConfigureAwait(false);
            if (!acquired) return null;
            return new KeyedLockLease(() => ReleaseReference(key, entry, true));
        }
        finally
        {
            if (!acquired) ReleaseReference(key, entry, false);
        }
    }

    /// <summary>
    /// 归还持有者或等待者引用，并回收不再使用的条目。
    /// </summary>
    /// <param name="key">条目的键。</param>
    /// <param name="entry">归还引用的实际条目。</param>
    /// <param name="releaseSemaphore">是否同时释放已取得的互斥权。</param>
    private void ReleaseReference(TKey key, Entry entry, bool releaseSemaphore)
    {
        // 释放时本次引用仍在，新的等待者不能提前移除本条目。
        if (releaseSemaphore) entry.Semaphore.Release();
        lock (_gate)
        {
            entry.References--;
            if (entry.References == 0 && _entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
            {
                _entries.Remove(key);
                entry.Semaphore.Dispose();
            }
        }
    }

    /// <summary>
    /// 保存一个键的互斥信号量及存活引用。
    /// </summary>
    private sealed class Entry
    {
        /// <summary>
        /// 仅允许一个持有者进入的信号量。
        /// </summary>
        internal readonly SemaphoreSlim Semaphore = new(1, 1);

        /// <summary>
        /// 在登记表锁内维护的持有者和等待者总数。
        /// </summary>
        internal int References;
    }
}

/// <summary>
/// 表示一次按键互斥权的可释放租约。
/// </summary>
/// <remarks>重复或并发释放均只归还一次互斥权。</remarks>
public sealed class KeyedLockLease : IDisposable
{
    /// <summary>
    /// 首次释放时交换为 null 的归还操作。
    /// </summary>
    private Action _release;

    /// <summary>
    /// 初始化已获取互斥权的租约。
    /// </summary>
    /// <param name="release">归还互斥权和条目引用的操作。</param>
    internal KeyedLockLease(Action release) => _release = release;

    /// <summary>
    /// 归还互斥权；重复调用不执行额外操作。
    /// </summary>
    public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
}
