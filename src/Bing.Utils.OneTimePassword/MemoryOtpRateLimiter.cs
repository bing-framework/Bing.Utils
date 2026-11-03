namespace Bing.OneTimePassword;

/// <summary>
/// 固定窗口的有界单实例限流器，满容量时拒绝新增身份。
/// </summary>
public sealed class MemoryOtpRateLimiter : IOtpRateLimiter, IDisposable
{
    /// <summary>
    /// 窗口计费记录。
    /// </summary>
    private sealed class Bucket
    {
        /// <summary>
        /// 窗口绝对过期时间。
        /// </summary>
        internal DateTimeOffset ExpiresAt;
        /// <summary>
        /// 已计费请求数。
        /// </summary>
        internal int Count;
    }
    /// <summary>
    /// 保护窗口计费及释放的短锁。
    /// </summary>
    private readonly object _gate = new();
    /// <summary>
    /// 受容量限制的窗口集合。
    /// </summary>
    private readonly Dictionary<string, Bucket> _buckets = new(StringComparer.Ordinal);
    /// <summary>
    /// 使用的 UTC 时钟。
    /// </summary>
    private readonly IOtpClock _clock;
    /// <summary>
    /// 窗口身份容量。
    /// </summary>
    private readonly int _capacity;
    /// <summary>
    /// 是否已经释放。
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 创建限流器；容量不足时不淘汰有效计费记录。
    /// </summary>
    /// <param name="capacity">最多驻留的身份数，1 至 1000000。</param>
    /// <param name="clock">可测试时钟。</param>
    public MemoryOtpRateLimiter(int capacity = 10000, IOtpClock? clock = null)
    {
        if (capacity < 1 || capacity > 1000000) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity; _clock = clock ?? new SystemOtpClock();
    }
    /// <inheritdoc />
    public Task<bool> TryAcquireAsync(string partition, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        OtpGuard.Text(partition, 512, nameof(partition));
        if (limit < 1 || limit > 10000) throw new ArgumentOutOfRangeException(nameof(limit));
        if (window < TimeSpan.FromSeconds(1) || window > TimeSpan.FromDays(1)) throw new ArgumentOutOfRangeException(nameof(window));
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MemoryOtpRateLimiter));
            cancellationToken.ThrowIfCancellationRequested();
            var now = _clock.UtcNow;
            if (_buckets.TryGetValue(partition, out var existing) && existing.ExpiresAt <= now) { _buckets.Remove(partition); existing = null; }
            if (existing == null)
            {
                if (_buckets.Count >= _capacity)
                    foreach (var expired in _buckets.Where(x => x.Value.ExpiresAt <= now).Select(x => x.Key).ToArray()) _buckets.Remove(expired);
                if (_buckets.Count >= _capacity) return Task.FromResult(false);
                existing = new Bucket { ExpiresAt = now.Add(window) };
                _buckets.Add(partition, existing);
            }
            existing.Count = Math.Min(existing.Count + 1, 10001);
            return Task.FromResult(existing.Count <= limit);
        }
    }
    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate) { _disposed = true; _buckets.Clear(); }
    }
}
