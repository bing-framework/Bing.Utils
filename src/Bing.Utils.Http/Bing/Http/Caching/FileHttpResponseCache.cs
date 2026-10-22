using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Bing.Http;

namespace Bing.Http.Caching;

/// <summary>
/// 将二进制 HTTP 响应快照保存到本地文件的缓存。
/// </summary>
/// <remarks>文件名使用缓存键哈希，写入使用临时文件替换；该缓存不会自动接入请求管线。</remarks>
public sealed class FileHttpResponseCache : IHttpResponseCache<byte[]>, IHttpResponseCacheStatistics
{
    /// <summary>
    /// 旧版缓存记录的文件标记。
    /// </summary>
    private const string LegacyMagic = "BINGHTTP1";
    /// <summary>
    /// 当前缓存记录的文件标记。
    /// </summary>
    private const string Magic = "BINGHTTP2";
    /// <summary>
    /// 当前缓存文件格式版本。
    /// </summary>
    private const int CurrentFormatVersion = 2;
    /// <summary>
    /// 缓存记录允许的最大响应头数量。
    /// </summary>
    private const int MaxHeaderCount = 4096;
    /// <summary>
    /// 单个响应头允许的最大值数量。
    /// </summary>
    private const int MaxValueCount = 256;
    /// <summary>
    /// 缓存记录允许的最大正文字节数。
    /// </summary>
    private const int MaxContentBytes = 256 * 1024 * 1024;
    /// <summary>
    /// 缓存文件所在的规范化目录路径。
    /// </summary>
    private readonly string _directory;
    /// <summary>
    /// 清理和清空操作使用的文件系统边界。
    /// </summary>
    private readonly IFileHttpResponseCacheFileSystem _fileSystem;
    /// <summary>
    /// 可选的最大缓存条目数。
    /// </summary>
    private readonly int? _maxEntries;
    /// <summary>
    /// 可选的最大缓存文件总字节数。
    /// </summary>
    private readonly long? _maxBytes;
    /// <summary>
    /// 串行化容量统计和淘汰操作的锁。
    /// </summary>
    private readonly object _capacityGate = new object();
    /// <summary>
    /// 保护清空代次变更的锁。
    /// </summary>
    private readonly object _generationGate = new object();
    /// <summary>
    /// 用于阻止清空前开始的写入在清空后重新发布的代次。
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
    /// 损坏或不支持格式的记录累计数。
    /// </summary>
    private long _corruptEntries;
    /// <summary>
    /// 旧格式记录成功迁移累计数。
    /// </summary>
    private long _legacyMigrations;
    /// <summary>
    /// 旧格式记录迁移失败累计数。
    /// </summary>
    private long _migrationFailures;
    /// <summary>
    /// 等待缓存锁重试的累计次数。
    /// </summary>
    private long _lockWaits;
    /// <summary>
    /// 清理操作移除过期条目的累计数。
    /// </summary>
    private long _expiredEntriesRemoved;
    /// <summary>
    /// 清理操作移除临时文件的累计数。
    /// </summary>
    private long _temporaryFilesRemoved;

    /// <summary>
    /// 初始化一个 <see cref="FileHttpResponseCache"/> 类型的实例。
    /// </summary>
    /// <param name="directory">缓存目录。</param>
    public FileHttpResponseCache(string directory)
        : this(directory, null, DefaultFileHttpResponseCacheFileSystem.Instance)
    {
    }

    /// <summary>
    /// 初始化一个 <see cref="FileHttpResponseCache"/> 类型的实例。
    /// </summary>
    /// <param name="directory">缓存目录。</param>
    /// <param name="options">容量配置；为空表示不限制条目数和总字节数。</param>
    public FileHttpResponseCache(string directory, FileHttpResponseCacheOptions options)
        : this(directory, options, DefaultFileHttpResponseCacheFileSystem.Instance)
    {
    }

    /// <summary>
    /// 初始化一个 <see cref="FileHttpResponseCache"/> 类型的实例。
    /// </summary>
    /// <param name="directory">缓存目录。</param>
    /// <param name="options">容量配置。</param>
    /// <param name="fileSystem">仅用于清理和清空操作的文件系统边界。</param>
    internal FileHttpResponseCache(string directory, FileHttpResponseCacheOptions options,
        IFileHttpResponseCacheFileSystem fileSystem)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A cache directory is required.", nameof(directory));
        if (fileSystem == null) throw new ArgumentNullException(nameof(fileSystem));
        if (options?.MaxEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxEntries must be positive when specified.");
        if (options?.MaxBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxBytes must be positive when specified.");
        _directory = Path.GetFullPath(directory);
        _maxEntries = options?.MaxEntries;
        _maxBytes = options?.MaxBytes;
        _fileSystem = fileSystem;
        Directory.CreateDirectory(_directory);
    }

    /// <inheritdoc />
    public HttpResponseCacheStatistics GetStatistics()
        => new HttpResponseCacheStatistics(
            Interlocked.Read(ref _hits), Interlocked.Read(ref _misses),
            Interlocked.Read(ref _evictions), Interlocked.Read(ref _corruptEntries),
            Interlocked.Read(ref _legacyMigrations), Interlocked.Read(ref _migrationFailures),
            Interlocked.Read(ref _lockWaits), Interlocked.Read(ref _expiredEntriesRemoved),
            Interlocked.Read(ref _temporaryFilesRemoved));

    /// <inheritdoc />
    public Task<HttpResponseResult<byte[]>> GetAsync(string key)
    {
        ValidateKey(key);
        using (AcquireLock(key))
        {
            return Task.FromResult(TryRead(CachePath(key), key));
        }
    }

    /// <inheritdoc />
    public Task SetAsync(string key, HttpResponseResult<byte[]> response, TimeSpan ttl,
        bool requestContainsAuthorization = false, bool allowAuthenticated = false)
    {
        ValidateKey(key);
        ValidateTtl(ttl);
        if (requestContainsAuthorization && !allowAuthenticated)
            throw new InvalidOperationException("Caching authenticated requests requires explicit allowAuthenticated=true.");
        if (response == null) throw new ArgumentNullException(nameof(response));
        if (response.Data == null) throw new ArgumentException("A byte[] response body is required.", nameof(response));

        var path = CachePath(key);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using (AcquireLock(key))
        {
            try
            {
                Write(temp, new FileCacheEntry(key, DateTimeOffset.UtcNow + ttl, response));
                Replace(temp, path);
                EnforceCapacity();
            }
            finally { TryDelete(temp); }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key)
    {
        ValidateKey(key);
        using (AcquireLock(key)) TryDelete(CachePath(key));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public int RemoveByPrefix(string prefix)
    {
        ValidateKey(prefix);
        var removed = 0;
        foreach (var path in Directory.GetFiles(_directory, "*.cache", SearchOption.TopDirectoryOnly))
        {
            FileCacheEntry entry;
            try { entry = Read(path); }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is EndOfStreamException)
            {
                Interlocked.Increment(ref _corruptEntries);
                continue;
            }
            if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal)) continue;
            using (AcquireLock(entry.Key))
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    removed++;
                }
            }
        }
        return removed;
    }

    /// <inheritdoc />
    public Task<int> ClearAsync()
    {
        lock (_generationGate) _clearGeneration++;
        var removed = 0;
        var root = Path.GetFullPath(_directory);
        foreach (var path in _fileSystem.GetFiles(root, "*.cache"))
        {
            FileCacheEntry entry;
            try { entry = Read(path); }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is EndOfStreamException)
            {
                Interlocked.Increment(ref _corruptEntries);
                Trace.TraceWarning("Ignoring corrupt HTTP cache file '{0}': {1}", path, error.Message);
                continue;
            }

            using (AcquireLock(entry.Key, CancellationToken.None, _fileSystem))
            {
                if (!_fileSystem.FileExists(path) ||
                    !string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), root, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    _ = Read(path);
                    if (TryDelete(_fileSystem, path))
                    {
                        removed++;
                    }
                }
                catch (Exception error) when (error is IOException || error is InvalidDataException || error is EndOfStreamException)
                {
                    Interlocked.Increment(ref _corruptEntries);
                    Trace.TraceWarning("Ignoring invalid HTTP cache file '{0}' during clear: {1}", path, error.Message);
                }
            }
        }
        return Task.FromResult(removed);
    }

    /// <summary>
    /// 清理过期缓存和超过保留时间的临时文件。
    /// </summary>
    /// <param name="staleAfter">临时文件至少应保留的时间；必须为非负有限值。</param>
    /// <param name="cancellationToken">取消清理和容量锁等待的令牌。</param>
    /// <returns>本次清理实际移除的文件统计。</returns>
    /// <remarks>
    /// 清理只处理当前目录的过期 <c>.cache</c> 和遗留 <c>.tmp</c> 文件。
    /// 为避免跨进程锁竞态，<c>.lock</c> 文件由调用方或操作系统生命周期保留，不会被此方法删除。
    /// 清理是显式操作，不会启动后台定时器。
    /// </remarks>
    public Task<FileHttpResponseCacheCleanupResult> CleanupAsync(
        TimeSpan staleAfter, CancellationToken cancellationToken = default)
        => CleanupAsync(staleAfter, cancellationToken, false);

    /// <summary>
    /// 递归清理当前缓存目录树中的过期缓存和遗留临时文件。
    /// </summary>
    /// <param name="staleAfter">临时文件至少应保留的时间；必须为非负有限值。</param>
    /// <param name="cancellationToken">取消清理和容量锁等待的令牌。</param>
    /// <returns>本次清理实际移除的文件统计。</returns>
    /// <remarks>这是显式的一次性维护操作；默认 CleanupAsync 仍只扫描当前目录顶层。每个目录使用同目录容量锁和按键锁，锁文件本身不会删除。</remarks>
    public Task<FileHttpResponseCacheCleanupResult> CleanupRecursiveAsync(
        TimeSpan staleAfter, CancellationToken cancellationToken = default)
        => CleanupAsync(staleAfter, cancellationToken, true);

    /// <summary>
    /// 清理文件缓存中过期条目和陈旧临时文件。
    /// </summary>
    private Task<FileHttpResponseCacheCleanupResult> CleanupAsync(
        TimeSpan staleAfter, CancellationToken cancellationToken, bool recursive)
    {
        if (staleAfter < TimeSpan.Zero || staleAfter == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(staleAfter));
        cancellationToken.ThrowIfCancellationRequested();

        var expiredEntriesRemoved = 0;
        var temporaryFilesRemoved = 0;
        var now = DateTime.UtcNow;
        var cutoff = staleAfter.Ticks >= now.Ticks - DateTime.MinValue.Ticks
            ? DateTime.MinValue
            : now.Subtract(staleAfter);

        foreach (var directory in FindDirectories(_directory, recursive, cancellationToken, _fileSystem))
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_capacityGate)
                using (AcquireCapacityLock(directory, cancellationToken, _fileSystem))
                {
                    foreach (var path in _fileSystem.GetFiles(directory, "*.cache"))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        FileCacheEntry entry;
                        try
                        {
                            entry = Read(path);
                        }
                        catch (Exception error) when (error is IOException || error is InvalidDataException || error is EndOfStreamException)
                        {
                            Interlocked.Increment(ref _corruptEntries);
                            Trace.TraceWarning("Ignoring invalid HTTP cache file '{0}' during cleanup: {1}", path, error.Message);
                            continue;
                        }

                        if (entry.ExpiresAt > DateTimeOffset.UtcNow) continue;
                        using (AcquireLock(directory, entry.Key, cancellationToken, _fileSystem))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (!_fileSystem.FileExists(path)) continue;
                            try
                            {
                                var current = Read(path);
                                if (current.ExpiresAt <= DateTimeOffset.UtcNow && TryDelete(_fileSystem, path))
                                {
                                    expiredEntriesRemoved++;
                                    Interlocked.Increment(ref _expiredEntriesRemoved);
                                }
                            }
                            catch (Exception error) when (error is IOException || error is InvalidDataException || error is EndOfStreamException)
                            {
                                Interlocked.Increment(ref _corruptEntries);
                                Trace.TraceWarning("Ignoring invalid HTTP cache file '{0}' during cleanup: {1}", path, error.Message);
                            }
                        }
                    }

                    foreach (var path in _fileSystem.GetFiles(directory, "*.tmp"))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            if (_fileSystem.GetLastWriteTimeUtc(path) <= cutoff && TryDelete(_fileSystem, path))
                            {
                                temporaryFilesRemoved++;
                                Interlocked.Increment(ref _temporaryFilesRemoved);
                            }
                        }
                        catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                    }
                }
        }

        return Task.FromResult(new FileHttpResponseCacheCleanupResult(
            expiredEntriesRemoved, temporaryFilesRemoved));
    }

    /// <inheritdoc />
    public async Task<HttpResponseResult<byte[]>> GetOrCreateAsync(string key, TimeSpan ttl,
        Func<Task<HttpResponseResult<byte[]>>> factory,
        bool requestContainsAuthorization = false, bool allowAuthenticated = false)
        => await GetOrCreateAsync(key, ttl, factory, CancellationToken.None,
            requestContainsAuthorization, allowAuthenticated).ConfigureAwait(false);

    /// <summary>
    /// 读取或创建文件缓存，并允许取消锁等待、文件阶段和回源后的提交。
    /// </summary>
    /// <param name="key">调用方定义的稳定缓存键。</param>
    /// <param name="ttl">回源结果的缓存有效期。</param>
    /// <param name="factory">缓存未命中时执行的回源委托。</param>
    /// <param name="cancellationToken">用于取消锁等待、文件阶段和回源提交的令牌。</param>
    /// <param name="requestContainsAuthorization">请求是否带有认证信息。</param>
    /// <param name="allowAuthenticated">是否明确允许缓存带认证信息的请求。</param>
    /// <returns>缓存或回源得到的响应快照。</returns>
    public async Task<HttpResponseResult<byte[]>> GetOrCreateAsync(string key, TimeSpan ttl,
        Func<Task<HttpResponseResult<byte[]>>> factory, CancellationToken cancellationToken,
        bool requestContainsAuthorization = false, bool allowAuthenticated = false)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        ValidateKey(key);
        ValidateTtl(ttl);
        if (requestContainsAuthorization && !allowAuthenticated)
            throw new InvalidOperationException("Caching authenticated requests requires explicit allowAuthenticated=true.");
        cancellationToken.ThrowIfCancellationRequested();
        long generation;
        lock (_generationGate) generation = _clearGeneration;
        using (AcquireLock(key, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cached = TryRead(CachePath(key), key, cancellationToken);
            if (cached != null) return cached;
            var response = await factory().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (response == null) return null;
            if (response.Data == null) throw new ArgumentException("A byte[] response body is required.", nameof(factory));
            var path = CachePath(key);
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                lock (_generationGate)
                {
                    if (generation != _clearGeneration) return response;
                    cancellationToken.ThrowIfCancellationRequested();
                    Write(temp, new FileCacheEntry(key, DateTimeOffset.UtcNow + ttl, response));
                    cancellationToken.ThrowIfCancellationRequested();
                    Replace(temp, path);
                    EnforceCapacity();
                }
            }
            finally { TryDelete(temp); }
            return response;
        }
    }

    /// <summary>
    /// 根据缓存键生成缓存数据文件路径。
    /// </summary>
    private string CachePath(string key) => Path.Combine(_directory, Hash(key) + ".cache");
    /// <summary>
    /// 根据缓存键生成条目锁文件路径。
    /// </summary>
    private string LockPath(string key) => Path.Combine(_directory, Hash(key) + ".lock");

    /// <summary>
    /// 按配置容量移除过期或最久未访问的缓存条目。
    /// </summary>
    private void EnforceCapacity()
    {
        if (!_maxEntries.HasValue && !_maxBytes.HasValue) return;
        lock (_capacityGate)
            using (AcquireCapacityLock())
            {
                var files = Directory.GetFiles(_directory, "*.cache", SearchOption.TopDirectoryOnly)
                    .Select(path => new FileInfo(path))
                    .Where(file => file.Exists)
                    .OrderBy(file => file.LastWriteTimeUtc)
                    .ToList();
                var now = DateTimeOffset.UtcNow;
                var valid = new List<FileInfo>(files.Count);
                long totalBytes = 0;
                foreach (var file in files)
                {
                    try
                    {
                        var entry = Read(file.FullName);
                        if (entry.ExpiresAt <= now)
                        {
                            if (TryDelete(file.FullName))
                                Interlocked.Increment(ref _evictions);
                            continue;
                        }
                        valid.Add(file);
                        totalBytes += file.Length;
                    }
                    catch (Exception error) when (error is IOException || error is InvalidDataException || error is EndOfStreamException)
                    {
                        Interlocked.Increment(ref _corruptEntries);
                        Trace.TraceWarning("Ignoring invalid HTTP cache file '{0}' during capacity enforcement: {1}", file.FullName, error.Message);
                    }
                }

                foreach (var file in valid.ToArray())
                {
                    if ((!_maxEntries.HasValue || valid.Count <= _maxEntries.Value) &&
                        (!_maxBytes.HasValue || totalBytes <= _maxBytes.Value)) break;
                    if (!TryDelete(file.FullName)) continue;
                    totalBytes -= file.Length;
                    valid.Remove(file);
                    Interlocked.Increment(ref _evictions);
                }
            }
    }

    /// <summary>
    /// 读取并验证指定缓存文件的响应内容。
    /// </summary>
    private HttpResponseResult<byte[]> TryRead(string path, string key)
        => TryRead(path, key, CancellationToken.None);

    /// <summary>
    /// 读取并验证指定缓存文件的响应内容。
    /// </summary>
    private HttpResponseResult<byte[]> TryRead(string path, string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(path))
        {
            Interlocked.Increment(ref _misses);
            return null;
        }
        try
        {
            var entry = Read(path);
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(entry.Key, key, StringComparison.Ordinal) || entry.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                File.Delete(path);
                return null;
            }
            if (entry.IsLegacy) TryMigrateLegacy(path, entry);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
            Interlocked.Increment(ref _hits);
            return entry.Response;
        }
        catch (Exception error) when (error is IOException || error is InvalidDataException || error is EndOfStreamException)
        {
            Interlocked.Increment(ref _corruptEntries);
            Interlocked.Increment(ref _misses);
            TryDelete(path);
            return null;
        }
    }

    /// <summary>
    /// 获取指定缓存条目的跨进程锁。
    /// </summary>
    private FileStream AcquireLock(string key)
        => AcquireLock(key, CancellationToken.None);

    /// <summary>
    /// 获取指定缓存条目的跨进程锁。
    /// </summary>
    private FileStream AcquireLock(string key, CancellationToken cancellationToken)
        => AcquireLockPath(LockPath(key), cancellationToken,
            () => Interlocked.Increment(ref _lockWaits));

    /// <summary>
    /// 获取指定缓存条目的跨进程锁。
    /// </summary>
    private FileStream AcquireLock(string key, CancellationToken cancellationToken,
        IFileHttpResponseCacheFileSystem fileSystem)
        => fileSystem.OpenLock(LockPath(key), cancellationToken,
            () => Interlocked.Increment(ref _lockWaits));

    /// <summary>
    /// 获取指定缓存条目的跨进程锁。
    /// </summary>
    private FileStream AcquireLock(string directory, string key, CancellationToken cancellationToken,
        IFileHttpResponseCacheFileSystem fileSystem)
        => fileSystem.OpenLock(Path.Combine(directory, Hash(key) + ".lock"), cancellationToken,
            () => Interlocked.Increment(ref _lockWaits));

    /// <summary>
    /// 获取文件缓存容量维护所需的跨进程锁。
    /// </summary>
    private FileStream AcquireCapacityLock()
        => AcquireCapacityLock(CancellationToken.None);

    /// <summary>
    /// 获取文件缓存容量维护所需的跨进程锁。
    /// </summary>
    private FileStream AcquireCapacityLock(CancellationToken cancellationToken)
        => AcquireLockPath(Path.Combine(_directory, ".binghttp.capacity.lock"), cancellationToken,
            () => Interlocked.Increment(ref _lockWaits));

    /// <summary>
    /// 获取文件缓存容量维护所需的跨进程锁。
    /// </summary>
    private FileStream AcquireCapacityLock(CancellationToken cancellationToken,
        IFileHttpResponseCacheFileSystem fileSystem)
        => fileSystem.OpenLock(Path.Combine(_directory, ".binghttp.capacity.lock"), cancellationToken,
            () => Interlocked.Increment(ref _lockWaits));

    /// <summary>
    /// 获取文件缓存容量维护所需的跨进程锁。
    /// </summary>
    private FileStream AcquireCapacityLock(string directory, CancellationToken cancellationToken,
        IFileHttpResponseCacheFileSystem fileSystem)
        => fileSystem.OpenLock(Path.Combine(directory, ".binghttp.capacity.lock"), cancellationToken,
            () => Interlocked.Increment(ref _lockWaits));

    /// <summary>
    /// 枚举根目录及按需递归发现的子目录。
    /// </summary>
    private static IEnumerable<string> FindDirectories(string root, bool recursive,
        CancellationToken cancellationToken, IFileHttpResponseCacheFileSystem fileSystem)
    {
        var queue = new Queue<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fullRoot = Path.GetFullPath(root);
        queue.Enqueue(fullRoot);
        visited.Add(fullRoot);
        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = queue.Dequeue();
            yield return current;
            if (!recursive) continue;
            foreach (var child in fileSystem.GetDirectories(current))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fullChild = Path.GetFullPath(child);
                if (visited.Add(fullChild)) queue.Enqueue(fullChild);
            }
        }
    }

    /// <summary>
    /// 打开指定路径对应的独占锁文件。
    /// </summary>
    private static FileStream AcquireLockPath(string path, CancellationToken cancellationToken,
        Action onWait)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException)
            {
                onWait?.Invoke();
                cancellationToken.ThrowIfCancellationRequested();
                if (DateTime.UtcNow >= deadline) throw;
                cancellationToken.WaitHandle.WaitOne(10);
            }
        }
    }

    /// <summary>
    /// 将缓存键转换为稳定的文件名摘要。
    /// </summary>
    private static string Hash(string key)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-", string.Empty).ToLowerInvariant();
    }

    /// <summary>
    /// 验证缓存键非空且符合调用要求。
    /// </summary>
    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key == "." || key == ".." ||
            key.IndexOfAny(new[] { '\\', '/', ':' }) >= 0 || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Cache key must be non-empty and path-safe.", nameof(key));
    }

    /// <summary>
    /// 验证缓存有效期为正且有限。
    /// </summary>
    private static void ValidateTtl(TimeSpan ttl)
    {
        if (ttl <= TimeSpan.Zero || ttl == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(ttl));
    }

    /// <summary>
    /// 读取文件缓存条目及其响应快照。
    /// </summary>
    private static FileCacheEntry Read(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var reader = new BinaryReader(stream, Encoding.UTF8))
        {
            var magic = reader.ReadString();
            var isLegacy = string.Equals(magic, LegacyMagic, StringComparison.Ordinal);
            if (!isLegacy && !string.Equals(magic, Magic, StringComparison.Ordinal))
                throw new InvalidDataException("Invalid file cache header.");
            if (!isLegacy && reader.ReadInt32() != CurrentFormatVersion)
                throw new InvalidDataException("Unsupported file cache format version.");
            var key = reader.ReadString();
            var expiresAt = new DateTimeOffset(reader.ReadInt64(), TimeSpan.Zero);
            var statusCode = (HttpStatusCode)reader.ReadInt32();
            var headers = ReadHeaders(reader);
            var contentHeaders = ReadHeaders(reader);
            var length = reader.ReadInt32();
            if (length < 0 || length > MaxContentBytes) throw new InvalidDataException("Invalid cached content length.");
            var data = reader.ReadBytes(length);
            if (data.Length != length) throw new InvalidDataException("Truncated cached content.");
            return new FileCacheEntry(key, expiresAt,
                new HttpResponseResult<byte[]>(statusCode, headers, contentHeaders, data, null), isLegacy);
        }
    }

    /// <summary>
    /// 将旧格式缓存条目迁移为当前格式。
    /// </summary>
    private bool TryMigrateLegacy(string path, FileCacheEntry entry)
    {
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Write(temp, entry);
            Replace(temp, path);
            Interlocked.Increment(ref _legacyMigrations);
            return true;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Interlocked.Increment(ref _migrationFailures);
            Trace.TraceWarning("Could not migrate legacy HTTP cache file '{0}': {1}", path, error.Message);
            return false;
        }
        finally { TryDelete(temp); }
    }

    /// <summary>
    /// 将缓存条目写入文件。
    /// </summary>
    private static void Write(string path, FileCacheEntry entry)
    {
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        using (var writer = new BinaryWriter(stream, Encoding.UTF8))
        {
            writer.Write(Magic);
            writer.Write(CurrentFormatVersion);
            writer.Write(entry.Key);
            writer.Write(entry.ExpiresAt.UtcTicks);
            writer.Write((int)entry.Response.StatusCode);
            WriteHeaders(writer, entry.Response.Headers);
            WriteHeaders(writer, entry.Response.ContentHeaders);
            writer.Write(entry.Response.Data.Length);
            writer.Write(entry.Response.Data);
        }
    }

    /// <summary>
    /// 从缓存文件读取响应头集合。
    /// </summary>
    private static Dictionary<string, IReadOnlyList<string>> ReadHeaders(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if (count < 0 || count > MaxHeaderCount) throw new InvalidDataException("Invalid cached header count.");
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < count; i++)
        {
            var name = reader.ReadString();
            var valueCount = reader.ReadInt32();
            if (valueCount < 0 || valueCount > MaxValueCount) throw new InvalidDataException("Invalid cached header value count.");
            var values = new string[valueCount];
            for (var j = 0; j < valueCount; j++) values[j] = reader.ReadString();
            result[name] = values;
        }
        return result;
    }

    /// <summary>
    /// 将响应头集合写入缓存文件。
    /// </summary>
    private static void WriteHeaders(BinaryWriter writer, IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
    {
        writer.Write(headers?.Count ?? 0);
        if (headers == null) return;
        foreach (var header in headers)
        {
            writer.Write(header.Key);
            writer.Write(header.Value?.Count ?? 0);
            if (header.Value != null)
                foreach (var value in header.Value) writer.Write(value ?? string.Empty);
        }
    }

    /// <summary>
    /// 以临时文件替换目标缓存文件。
    /// </summary>
    private static void Replace(string temp, string target)
    {
        if (File.Exists(target))
        {
            try { File.Replace(temp, target, null); return; }
            catch (PlatformNotSupportedException) { File.Delete(target); }
        }
        File.Move(temp, target);
    }

    /// <summary>
    /// 尝试删除缓存或临时文件。
    /// </summary>
    private static bool TryDelete(string path)
    {
        try
        {
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// 尝试删除缓存或临时文件。
    /// </summary>
    private static bool TryDelete(IFileHttpResponseCacheFileSystem fileSystem, string path)
    {
        try { return fileSystem.TryDelete(path); }
        catch { return false; }
    }

    /// <summary>
    /// 文件缓存记录的反序列化数据。
    /// </summary>
    private sealed class FileCacheEntry
    {
        /// <summary>
        /// 初始化一个 <see cref="FileCacheEntry"/> 类型的实例。
        /// </summary>
        public FileCacheEntry(string key, DateTimeOffset expiresAt, HttpResponseResult<byte[]> response,
            bool isLegacy = false)
        { Key = key; ExpiresAt = expiresAt; Response = response; IsLegacy = isLegacy; }
        /// <summary>
        /// 获取记录关联的缓存键。
        /// </summary>
        public string Key { get; }
        /// <summary>
        /// 获取记录的过期时间。
        /// </summary>
        public DateTimeOffset ExpiresAt { get; }
        /// <summary>
        /// 获取记录的响应快照。
        /// </summary>
        public HttpResponseResult<byte[]> Response { get; }
        /// <summary>
        /// 获取记录是否来自旧版文件格式。
        /// </summary>
        public bool IsLegacy { get; }
    }
}
