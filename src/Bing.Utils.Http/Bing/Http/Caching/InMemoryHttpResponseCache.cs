using Bing.Http;

namespace Bing.Http.Caching;

/// <summary>

/// 无外部依赖的进程内 HTTP 响应缓存。

/// </summary>
/// <typeparam name="TResult">响应转换后的数据类型。</typeparam>
public sealed class InMemoryHttpResponseCache<TResult> : IHttpResponseCache<TResult>, IHttpResponseCacheStatistics where TResult : class
{
    /// <summary>
    /// 保护缓存条目、容量和请求状态的锁。
    /// </summary>
    private readonly object _gate = new object();
    /// <summary>
    /// 按缓存键保存的有效条目。
    /// </summary>
    private readonly Dictionary<string, CacheItem> _items = new Dictionary<string, CacheItem>(StringComparer.Ordinal);
    /// <summary>
    /// 按缓存键共享中的回源任务。
    /// </summary>
    private readonly Dictionary<string, Task<HttpResponseResult<TResult>>> _inFlight =
        new Dictionary<string, Task<HttpResponseResult<TResult>>>(StringComparer.Ordinal);
    /// <summary>
    /// 可选的最大缓存条目数。
    /// </summary>
    private readonly int? _maxEntries;
    /// <summary>
    /// 可选的最大估算缓存字节数。
    /// </summary>
    private readonly long? _maxBytes;
    /// <summary>
    /// 估算缓存结果大小的委托。
    /// </summary>
    private readonly Func<TResult, long> _sizeEstimator;
    /// <summary>
    /// 当前已缓存条目的估算总字节数。
    /// </summary>
    private long _estimatedBytes;
    /// <summary>
    /// 阻止清空前启动的回源任务在清空后重新发布的代次。
    /// </summary>
    private long _clearGeneration;
    /// <summary>
    /// 缓存命中累计数。
    /// </summary>
    private long _hits;
    /// <summary>
    /// 缓存未命中累计数。
    /// </summary>
    private long _misses;
    /// <summary>
    /// 因容量限制淘汰的条目累计数。
    /// </summary>
    private long _evictions;

    /// <summary>

    /// 初始化一个<see cref="InMemoryHttpResponseCache{TResult}"/>类型的实例。

    /// </summary>
    /// <param name="options">容量配置；为空表示不限制条目数。</param>
    public InMemoryHttpResponseCache(HttpResponseCacheOptions options = null)
    {
        if (options?.MaxEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxEntries must be positive when specified.");
        if (options?.MaxBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxBytes must be positive when specified.");
        if (options?.MaxBytes.HasValue == true && options.SizeEstimator == null)
            throw new ArgumentException("MaxBytes requires SizeEstimator.", nameof(options));
        _maxEntries = options?.MaxEntries;
        _maxBytes = options?.MaxBytes;
        _sizeEstimator = options?.SizeEstimator == null ? null : value => options.SizeEstimator(value);
    }

    /// <inheritdoc />
    public HttpResponseCacheStatistics GetStatistics()
        => new HttpResponseCacheStatistics(
            Interlocked.Read(ref _hits), Interlocked.Read(ref _misses),
            Interlocked.Read(ref _evictions), 0, 0, 0, 0, 0, 0);

    /// <inheritdoc />
    public Task<HttpResponseResult<TResult>> GetAsync(string key)
    {
        ValidateKey(key);
        lock (_gate)
        {
            if (_items.TryGetValue(key, out var item))
            {
                if (item.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    item.LastAccess = DateTimeOffset.UtcNow;
                    Interlocked.Increment(ref _hits);
                    return Task.FromResult(item.Response);
                }
                _estimatedBytes -= item.SizeBytes;
                _items.Remove(key);
            }
        }
        Interlocked.Increment(ref _misses);
        return Task.FromResult<HttpResponseResult<TResult>>(null);
    }

    /// <inheritdoc />
    public Task SetAsync(string key, HttpResponseResult<TResult> response, TimeSpan ttl,
        bool requestContainsAuthorization = false, bool allowAuthenticated = false)
    {
        ValidateKey(key);
        ValidateTtl(ttl);
        ValidateAuthorization(requestContainsAuthorization, allowAuthenticated);
        if (response == null) throw new ArgumentNullException(nameof(response));
        lock (_gate) Store(key, response, ttl);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key)
    {
        ValidateKey(key);
        lock (_gate)
        {
            if (_items.TryGetValue(key, out var item))
            {
                _estimatedBytes -= item.SizeBytes;
                _items.Remove(key);
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public int RemoveByPrefix(string prefix)
    {
        ValidateKey(prefix);
        lock (_gate)
        {
            var keys = _items.Keys
                .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
                .ToArray();
            foreach (var key in keys)
            {
                _estimatedBytes -= _items[key].SizeBytes;
                _items.Remove(key);
            }
            return keys.Length;
        }
    }

    /// <inheritdoc />
    public Task<int> ClearAsync()
    {
        lock (_gate)
        {
            var removed = _items.Count;
            _clearGeneration++;
            _items.Clear();
            _estimatedBytes = 0;
            return Task.FromResult(removed);
        }
    }

    /// <inheritdoc />
    public Task<HttpResponseResult<TResult>> GetOrCreateAsync(string key, TimeSpan ttl,
        Func<Task<HttpResponseResult<TResult>>> factory,
        bool requestContainsAuthorization = false, bool allowAuthenticated = false)
    {
        ValidateKey(key);
        ValidateTtl(ttl);
        ValidateAuthorization(requestContainsAuthorization, allowAuthenticated);
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        lock (_gate)
        {
            if (_items.TryGetValue(key, out var item))
            {
                if (item.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    item.LastAccess = DateTimeOffset.UtcNow;
                    Interlocked.Increment(ref _hits);
                    return Task.FromResult(item.Response);
                }
                _estimatedBytes -= item.SizeBytes;
                _items.Remove(key);
            }
            if (_inFlight.TryGetValue(key, out var existing))
            {
                Interlocked.Increment(ref _misses);
                return existing;
            }
            Interlocked.Increment(ref _misses);

            var completion = new TaskCompletionSource<HttpResponseResult<TResult>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _inFlight[key] = completion.Task;
            _ = RunFactoryAsync(key, ttl, factory, completion, _clearGeneration);
            return completion.Task;
        }
    }

    /// <summary>

    /// 执行单次回源工厂并向等待者发布结果。

    /// </summary>
    private async Task RunFactoryAsync(string key, TimeSpan ttl,
        Func<Task<HttpResponseResult<TResult>>> factory,
        TaskCompletionSource<HttpResponseResult<TResult>> completion,
        long generation)
    {
        try
        {
            var request = factory();
            if (request == null)
                throw new InvalidOperationException("The response cache factory returned null.");
            var response = await request.ConfigureAwait(false);
            if (response != null)
                lock (_gate)
                {
                    if (generation == _clearGeneration) Store(key, response, ttl);
                }
            completion.TrySetResult(response);
        }
        catch (Exception error)
        {
            // 回源失败或取消不会写入缓存；所有等待者仍接收原始异常。
            completion.TrySetException(error);
        }
        finally
        {
            lock (_gate)
            {
                if (_inFlight.TryGetValue(key, out var active) && ReferenceEquals(active, completion.Task))
                    _inFlight.Remove(key);
            }
        }
    }

    /// <summary>

    /// 验证缓存键非空且符合调用要求。

    /// </summary>
    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A cache key is required.", nameof(key));
    }

    /// <summary>

    /// 验证缓存有效期为正且有限。

    /// </summary>
    private static void ValidateTtl(TimeSpan ttl)
    {
        if (ttl <= TimeSpan.Zero || ttl == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(ttl), "Cache TTL must be positive and finite.");
    }

    /// <summary>

    /// 阻止未显式允许的认证请求进入缓存。

    /// </summary>
    private static void ValidateAuthorization(bool requestContainsAuthorization, bool allowAuthenticated)
    {
        if (requestContainsAuthorization && !allowAuthenticated)
            throw new InvalidOperationException("Caching authenticated requests requires explicit allowAuthenticated=true.");
    }

    /// <summary>

    /// 保存响应并按容量策略淘汰旧缓存条目。

    /// </summary>
    private void Store(string key, HttpResponseResult<TResult> response, TimeSpan ttl)
    {
        var size = EstimateSize(response);
        if (_items.TryGetValue(key, out var previous))
        {
            _estimatedBytes -= previous.SizeBytes;
            _items.Remove(key);
        }
        if (_maxBytes.HasValue && size > _maxBytes.Value)
        {
            Interlocked.Increment(ref _evictions);
            return;
        }
        _items[key] = new CacheItem(response, DateTimeOffset.UtcNow + ttl, size);
        _estimatedBytes += size;
        if (!_maxEntries.HasValue && !_maxBytes.HasValue) return;

        while ((_maxEntries.HasValue && _items.Count > _maxEntries.Value) ||
               (_maxBytes.HasValue && _estimatedBytes > _maxBytes.Value))
        {
            var now = DateTimeOffset.UtcNow;
            var candidate = _items
                .Where(item => item.Value.ExpiresAt <= now)
                .OrderBy(item => item.Value.ExpiresAt)
                .Select(item => item.Key)
                .FirstOrDefault();
            if (candidate == null)
                candidate = _items.OrderBy(item => item.Value.LastAccess)
                    .Select(item => item.Key).First();
            _estimatedBytes -= _items[candidate].SizeBytes;
            _items.Remove(candidate);
            Interlocked.Increment(ref _evictions);
        }
    }

    /// <summary>

    /// 根据配置估算缓存响应占用的字节数。

    /// </summary>
    private long EstimateSize(HttpResponseResult<TResult> response)
    {
        if (!_maxBytes.HasValue) return 0;
        var size = _sizeEstimator(response.Data);
        if (size < 0) throw new ArgumentOutOfRangeException(nameof(response), "SizeEstimator must return a non-negative value.");
        return size;
    }

    /// <summary>

    /// 保存响应及容量淘汰元数据的缓存条目。

    /// </summary>
    private sealed class CacheItem
    {
        /// <summary>
        /// 初始化一个<see cref="CacheItem"/>类型的实例。
        /// </summary>
        public CacheItem(HttpResponseResult<TResult> response, DateTimeOffset expiresAt, long sizeBytes)
        {
            Response = response;
            ExpiresAt = expiresAt;
            LastAccess = DateTimeOffset.UtcNow;
            SizeBytes = sizeBytes;
        }

        /// <summary>

        /// 缓存的响应快照。

        /// </summary>
        public HttpResponseResult<TResult> Response { get; }
        /// <summary>
        /// 条目过期时间。
        /// </summary>
        public DateTimeOffset ExpiresAt { get; }
        /// <summary>
        /// 最近一次命中时间，用于淘汰顺序。
        /// </summary>
        public DateTimeOffset LastAccess { get; set; }
        /// <summary>
        /// 条目的估算字节数。
        /// </summary>
        public long SizeBytes { get; }
    }
}
