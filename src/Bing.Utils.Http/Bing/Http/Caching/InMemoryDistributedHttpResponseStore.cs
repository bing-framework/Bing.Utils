namespace Bing.Http.Caching;

/// <summary>

/// 用于测试和单进程宿主的共享快照存储适配器。

/// </summary>
/// <remarks>此实现只在当前进程内提供锁和一致性，不能作为跨进程或跨节点的分布式实现。</remarks>
public sealed class InMemoryDistributedHttpResponseStore : IDistributedHttpResponseStore,
    IDistributedHttpResponseLeaseStore, IDistributedHttpResponseNamespaceStore
{
    /// <summary>
    /// 保护快照、租约和命名空间状态的锁。
    /// </summary>
    private readonly object _gate = new object();
    /// <summary>
    /// 按键保存的快照和过期时间。
    /// </summary>
    private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
    /// <summary>
    /// 按键保存的有效回源租约。
    /// </summary>
    private readonly Dictionary<string, LeaseEntry> _leases = new Dictionary<string, LeaseEntry>(StringComparer.Ordinal);
    /// <summary>
    /// 各缓存命名空间的递增版本号。
    /// </summary>
    private readonly Dictionary<string, long> _namespaceVersions = new Dictionary<string, long>(StringComparer.Ordinal);
    /// <summary>
    /// 为新快照分配版本令牌的计数器。
    /// </summary>
    private long _version;

    /// <inheritdoc />
    public Task<string> GetNamespaceVersionAsync(string namespaceName,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(namespaceName, nameof(namespaceName));
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_namespaceVersions.TryGetValue(namespaceName, out var version)
                ? version.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : "0");
        }
    }

    /// <inheritdoc />
    public Task<string> InvalidateNamespaceAsync(string namespaceName,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(namespaceName, nameof(namespaceName));
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _namespaceVersions.TryGetValue(namespaceName, out var version);
            version++;
            _namespaceVersions[namespaceName] = version;
            return Task.FromResult(version.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    /// <inheritdoc />
    public Task<int> CleanupNamespaceAsync(string namespaceName,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(namespaceName, nameof(namespaceName));
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var currentVersion = _namespaceVersions.TryGetValue(namespaceName, out var version)
                ? version.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : "0";
            var currentPrefix = namespaceName + ":" + currentVersion + ":";
            var namespacePrefix = namespaceName + ":";
            var oldKeys = _entries.Keys
                .Where(key => key.StartsWith(namespacePrefix, StringComparison.Ordinal) &&
                    !key.StartsWith(currentPrefix, StringComparison.Ordinal))
                .ToArray();
            foreach (var key in oldKeys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _entries.Remove(key);
            }
            return Task.FromResult(oldKeys.Length);
        }
    }

    /// <inheritdoc />
    public Task<IDistributedHttpResponseLease> TryAcquireAsync(string key, TimeSpan leaseTtl,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key, nameof(key));
        if (leaseTtl <= TimeSpan.Zero || leaseTtl == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(leaseTtl));
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_leases.TryGetValue(key, out var current) && current.ExpiresAt > DateTimeOffset.UtcNow)
                return Task.FromResult<IDistributedHttpResponseLease>(null);
            var token = Guid.NewGuid().ToString("N");
            _leases[key] = new LeaseEntry(token, DateTimeOffset.UtcNow + leaseTtl);
            return Task.FromResult<IDistributedHttpResponseLease>(new MemoryLease(this, key, token));
        }
    }

    /// <summary>

    /// 释放当前持有的分布式缓存租约。

    /// </summary>
    private Task ReleaseLeaseAsync(string key, string token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_leases.TryGetValue(key, out var current) &&
                string.Equals(current.Token, token, StringComparison.Ordinal))
                _leases.Remove(key);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<DistributedHttpResponseSnapshot> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_entries.TryGetValue(key, out var entry)) return Task.FromResult<DistributedHttpResponseSnapshot>(null);
            if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                _entries.Remove(key);
                return Task.FromResult<DistributedHttpResponseSnapshot>(null);
            }
            return Task.FromResult(Clone(entry.Snapshot));
        }
    }

    /// <inheritdoc />
    public Task<DistributedHttpResponseSnapshot> SetAsync(string key, DistributedHttpResponseSnapshot snapshot,
        TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        Validate(key, snapshot, ttl);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var stored = CloneWithVersion(snapshot, NextVersion());
            _entries[key] = new Entry(stored, DateTimeOffset.UtcNow + ttl);
            return Task.FromResult(Clone(stored));
        }
    }

    /// <inheritdoc />
    public Task<bool> CompareAndSetAsync(string key, string expectedVersion,
        DistributedHttpResponseSnapshot snapshot, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        Validate(key, snapshot, ttl);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var exists = _entries.TryGetValue(key, out var current) && current.ExpiresAt > DateTimeOffset.UtcNow;
            if (!exists && current != null) _entries.Remove(key);
            if ((exists ? current.Snapshot.Version : null) != expectedVersion) return Task.FromResult(false);
            _entries[key] = new Entry(CloneWithVersion(snapshot, NextVersion()), DateTimeOffset.UtcNow + ttl);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, string expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_entries.TryGetValue(key, out var current)) return Task.FromResult(false);
            if (current.ExpiresAt <= DateTimeOffset.UtcNow ||
                (expectedVersion != null && !string.Equals(current.Snapshot.Version, expectedVersion, StringComparison.Ordinal)))
            {
                if (current.ExpiresAt <= DateTimeOffset.UtcNow) _entries.Remove(key);
                return Task.FromResult(false);
            }
            _entries.Remove(key);
            return Task.FromResult(true);
        }
    }

    /// <summary>

    /// 生成下一个命名空间版本标识。

    /// </summary>
    private string NextVersion() => (++_version).ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>

    /// 复制分片清单及其中的分片状态。

    /// </summary>
    private static DistributedHttpResponseSnapshot Clone(DistributedHttpResponseSnapshot snapshot)
        => CloneWithVersion(snapshot, snapshot.Version);

    /// <summary>

    /// 复制响应快照并替换版本标识。

    /// </summary>
    private static DistributedHttpResponseSnapshot CloneWithVersion(DistributedHttpResponseSnapshot snapshot, string version)
        => new DistributedHttpResponseSnapshot(snapshot.StatusCode, snapshot.Headers, snapshot.ContentHeaders,
            snapshot.Body, snapshot.RawContent, version);

    /// <summary>

    /// 校验分布式响应缓存的存储、键和有效期。

    /// </summary>
    private static void Validate(string key, DistributedHttpResponseSnapshot snapshot, TimeSpan ttl)
    {
        ValidateKey(key);
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (ttl <= TimeSpan.Zero || ttl == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(ttl));
    }

    /// <summary>

    /// 验证缓存键非空且符合调用要求。

    /// </summary>
    private static void ValidateKey(string key, string parameterName = "key")
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A distributed cache key is required.", parameterName);
    }

    /// <summary>

    /// 存储快照及其过期时间的条目。

    /// </summary>
    private sealed class Entry
    {
        /// <summary>
        /// 初始化一个<see cref="Entry"/>类型的实例。
        /// </summary>
        /// <param name="snapshot">要保存的响应快照。</param>
        /// <param name="expiresAt">条目过期时间。</param>
        public Entry(DistributedHttpResponseSnapshot snapshot, DateTimeOffset expiresAt)
        { Snapshot = snapshot; ExpiresAt = expiresAt; }
        /// <summary>
        /// 条目保存的响应快照。
        /// </summary>
        public DistributedHttpResponseSnapshot Snapshot { get; }
        /// <summary>
        /// 条目过期时间。
        /// </summary>
        public DateTimeOffset ExpiresAt { get; }
    }

    /// <summary>

    /// 存储租约令牌及其有效期的条目。

    /// </summary>
    private sealed class LeaseEntry
    {
        /// <summary>
        /// 初始化一个<see cref="LeaseEntry"/>类型的实例。
        /// </summary>
        /// <param name="token">租约持有者令牌。</param>
        /// <param name="expiresAt">租约过期时间。</param>
        public LeaseEntry(string token, DateTimeOffset expiresAt)
        { Token = token; ExpiresAt = expiresAt; }
        /// <summary>
        /// 用于识别当前租约持有者的令牌。
        /// </summary>
        public string Token { get; }
        /// <summary>
        /// 条目过期时间。
        /// </summary>
        public DateTimeOffset ExpiresAt { get; }
    }

    /// <summary>

    /// 由当前存储实例管理的回源租约。

    /// </summary>
    private sealed class MemoryLease : IDistributedHttpResponseLease
    {
        /// <summary>
        /// 负责释放租约的存储实例。
        /// </summary>
        private readonly InMemoryDistributedHttpResponseStore _owner;
        /// <summary>
        /// 租约关联的缓存键。
        /// </summary>
        private readonly string _key;
        /// <summary>
        /// 创建租约时分配的持有者令牌。
        /// </summary>
        private readonly string _token;
        /// <summary>
        /// 标记租约是否已释放。
        /// </summary>
        private int _released;

        /// <summary>

        /// 初始化一个<see cref="MemoryLease"/>类型的实例。

        /// </summary>
        /// <param name="owner">负责释放租约的存储实例。</param>
        /// <param name="key">租约关联的缓存键。</param>
        /// <param name="token">租约持有者令牌。</param>
        public MemoryLease(InMemoryDistributedHttpResponseStore owner, string key, string token)
        { _owner = owner; _key = key; _token = token; }

        /// <inheritdoc />
        public Task ReleaseAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _released, 1) != 0) return Task.CompletedTask;
            return _owner.ReleaseLeaseAsync(_key, _token, cancellationToken);
        }
    }
}
