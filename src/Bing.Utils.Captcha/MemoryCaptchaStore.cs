namespace Bing.Captcha;

/// <summary>有界内存存储的配置。</summary>
public sealed class MemoryCaptchaStoreOptions
{
    /// <summary>获取或设置最大记录数，默认 10000。</summary>
    public int Capacity { get; set; } = 10000;
    /// <summary>获取或设置清理周期，默认 30 秒。</summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromSeconds(30);
    /// <summary>获取或设置每轮最多检查的条数，默认 256。</summary>
    public int CleanupBatchSize { get; set; } = 256;
}

/// <summary>使用短锁实现原子替换、消费与容量淘汰的单实例存储；由创建者释放。</summary>
public sealed class MemoryCaptchaStore : ICaptchaStore, IDisposable
{
    private sealed class Item
    {
        internal Item(string key, CaptchaEntry entry) { Key = key; Entry = entry; }
        internal readonly string Key;
        internal readonly CaptchaEntry Entry;
    }
    private readonly object _gate = new();
    private readonly Dictionary<string, LinkedListNode<Item>> _items = new(StringComparer.Ordinal);
    private readonly LinkedList<Item> _lru = new();
    private LinkedListNode<Item>? _cursor;
    private readonly ICaptchaClock _clock;
    private readonly int _capacity;
    private readonly int _batch;
    private readonly Timer _timer;
    private bool _disposed;
    /// <summary>创建存储并启动有界清理；配置在构造时复制。</summary>
    /// <param name="options">容量、清理间隔及每批上限；null 使用默认值。</param>
    /// <param name="clock">与服务一致的 UTC 时钟，null 使用系统时间。</param>
    public MemoryCaptchaStore(MemoryCaptchaStoreOptions? options = null, ICaptchaClock? clock = null)
    {
        options ??= new MemoryCaptchaStoreOptions();
        if (options.Capacity < 1) throw new ArgumentOutOfRangeException(nameof(options.Capacity));
        if (options.CleanupBatchSize < 1) throw new ArgumentOutOfRangeException(nameof(options.CleanupBatchSize));
        if (options.CleanupInterval < TimeSpan.FromMilliseconds(10) || options.CleanupInterval > TimeSpan.FromDays(1)) throw new ArgumentOutOfRangeException(nameof(options.CleanupInterval));
        _capacity = options.Capacity; _batch = options.CleanupBatchSize; _clock = clock ?? new SystemCaptchaClock();
        _timer = new Timer(_ => Sweep(), null, options.CleanupInterval, options.CleanupInterval);
    }
    /// <summary>获取当前驻留记录数（可能包含尚未清理的过期记录）。</summary>
    public int Count { get { lock (_gate) return _items.Count; } }
    /// <inheritdoc />
    public Task ReplaceAsync(CaptchaContext context, CaptchaEntry entry, CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        cancellationToken.ThrowIfCancellationRequested(); var key = context.GetStorageKey();
        lock (_gate)
        {
            EnsureOpen(); cancellationToken.ThrowIfCancellationRequested();
            if (_clock.UtcNow >= entry.ExpiresAt) throw new ArgumentException("不能写入已过期的挑战。", nameof(entry));
            if (_items.TryGetValue(key, out var prior)) Remove(prior);
            if (_items.Count >= _capacity)
            {
                var node = _lru.First;
                while (node != null) { var next = node.Next; if (_clock.UtcNow >= node.Value.Entry.ExpiresAt) Remove(node); node = next; }
                if (_items.Count >= _capacity) Remove(_lru.First!);
            }
            _items.Add(key, _lru.AddLast(new Item(key, entry)));
        }
        return Task.CompletedTask;
    }
    /// <inheritdoc />
    public Task<CaptchaEntry?> TryTakeAsync(CaptchaContext context, string captchaId, CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        CaptchaContext.Check(captchaId, 64, nameof(captchaId));
        cancellationToken.ThrowIfCancellationRequested(); var key = context.GetStorageKey();
        lock (_gate)
        {
            EnsureOpen(); cancellationToken.ThrowIfCancellationRequested();
            if (!_items.TryGetValue(key, out var node)) return Task.FromResult<CaptchaEntry?>(null);
            if (_clock.UtcNow >= node.Value.Entry.ExpiresAt) { Remove(node); return Task.FromResult<CaptchaEntry?>(null); }
            if (!string.Equals(node.Value.Entry.CaptchaId, captchaId, StringComparison.Ordinal)) return Task.FromResult<CaptchaEntry?>(null);
            Remove(node); return Task.FromResult<CaptchaEntry?>(node.Value.Entry);
        }
    }
    private void Sweep()
    {
        lock (_gate)
        {
            if (_disposed) return;
            var remaining = Math.Min(_batch, _items.Count);
            while (remaining-- > 0 && _lru.First != null)
            {
                var node = _cursor ?? _lru.First;
                _cursor = node!.Next;
                if (_clock.UtcNow >= node.Value.Entry.ExpiresAt) Remove(node);
            }
        }
    }
    private void Remove(LinkedListNode<Item> node)
    {
        if (_cursor == node) _cursor = node.Next;
        _lru.Remove(node); _items.Remove(node.Value.Key);
    }
    private void EnsureOpen() { if (_disposed) throw new ObjectDisposedException(nameof(MemoryCaptchaStore)); }
    /// <summary>停止清理并清空记录；与访问互斥，可重复调用。</summary>
    public void Dispose()
    {
        lock (_gate) { if (_disposed) return; _disposed = true; _timer.Dispose(); _items.Clear(); _lru.Clear(); _cursor = null; }
    }
}
