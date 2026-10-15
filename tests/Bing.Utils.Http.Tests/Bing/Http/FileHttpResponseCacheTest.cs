using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Bing.Http;
using Bing.Http.Caching;

namespace Bing.Utils.Http.Tests.Bing.Http;



/// <summary>
/// 文件 HTTP 响应缓存的持久化、损坏恢复和失效测试。
/// </summary>
public class FileHttpResponseCacheTest
{
    /// <summary>
    /// 验证持久化快照并过期。
    /// </summary>
    [Fact]
    public async Task SetAndGet_PersistsSnapshotAndExpires()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            await cache.SetAsync("orders-1", Response("value"), TimeSpan.FromMilliseconds(50));
            (await cache.GetAsync("orders-1")).Data.ShouldBe(new byte[] { 1, 2, 3 });
            await Task.Delay(100);
            (await cache.GetAsync("orders-1")).ShouldBeNull();
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证读取旧格式缓存并迁移到当前版本。
    /// </summary>
    [Fact]
    public async Task LegacyFormatIsReadAndMigratedToCurrentVersion()
    {
        var directory = CreateDirectory();
        try
        {
            const string key = "legacy-format";
            var response = Response("legacy");
            WriteLegacyCacheFile(directory, key, response, TimeSpan.FromMinutes(1));
            var cache = new FileHttpResponseCache(directory);

            (await cache.GetAsync(key)).Data.ShouldBe(new byte[] { 1, 2, 3 });
            var path = Directory.GetFiles(directory, "*.cache").Single();
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                reader.ReadString().ShouldBe("BINGHTTP2");
                reader.ReadInt32().ShouldBe(2);
            }
            var statistics = cache.GetStatistics();
            statistics.Hits.ShouldBe(1);
            statistics.LegacyMigrations.ShouldBe(1);
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证未知格式版本是否拒绝并已移除。
    /// </summary>
    [Fact]
    public async Task UnknownFormatVersionIsRejectedAndRemoved()
    {
        var directory = CreateDirectory();
        try
        {
            const string key = "unknown-format";
            var path = Path.Combine(directory, Hash(key) + ".cache");
            using (var stream = File.Create(path))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write("BINGHTTP2");
                writer.Write(999);
            }

            var cache = new FileHttpResponseCache(directory);
            (await cache.GetAsync(key)).ShouldBeNull();
            File.Exists(path).ShouldBeFalse();
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证拒绝空并路径穿越键。
    /// </summary>
    [Fact]
    public async Task RejectsEmptyAndPathTraversalKeys()
    {
        var cache = new FileHttpResponseCache(CreateDirectory());
        Should.Throw<ArgumentException>(() => cache.GetAsync(" "));
        Should.Throw<ArgumentException>(() => cache.GetAsync("../secret"));
        await Should.ThrowAsync<ArgumentException>(() => cache.SetAsync("a/b", Response("x"), TimeSpan.FromMinutes(1)));
    }



    /// <summary>
    /// 验证忽略损坏文件并按序号前缀移除缓存。
    /// </summary>
    [Fact]
    public async Task CorruptFileIsIgnoredAndPrefixRemovalIsOrdinal()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            await cache.SetAsync("orders-1", Response("one"), TimeSpan.FromMinutes(1));
            var corruptPath = Directory.GetFiles(directory, "*.cache").Single();
            await cache.SetAsync("Orders-2", Response("two"), TimeSpan.FromMinutes(1));
            File.WriteAllBytes(corruptPath, new byte[] { 1, 2, 3 });
            (await cache.GetAsync("orders-1")).ShouldBeNull();
            cache.RemoveByPrefix("Orders-").ShouldBe(1);
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证并发写入方保持可读原子缓存项。
    /// </summary>
    [Fact]
    public async Task ConcurrentWritersLeaveReadableAtomicEntry()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            await Task.WhenAll(Enumerable.Range(0, 8).Select(index =>
                cache.SetAsync("orders-1", Response(index.ToString()), TimeSpan.FromMinutes(1))));
            var result = await cache.GetAsync("orders-1");
            result.Data.ShouldNotBeNull();
            result.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证容量不足时淘汰最近最少访问项。
    /// </summary>
    [Fact]
    public async Task CapacityEvictsLeastRecentlyAccessedEntry()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory, new FileHttpResponseCacheOptions { MaxEntries = 2 });
            await cache.SetAsync("old", Response("old"), TimeSpan.FromMinutes(1));
            await Task.Delay(30);
            await cache.SetAsync("kept", Response("kept"), TimeSpan.FromMinutes(1));
            (await cache.GetAsync("old")).ShouldNotBeNull();
            await Task.Delay(30);
            await cache.SetAsync("new", Response("new"), TimeSpan.FromMinutes(1));

            (await cache.GetAsync("old")).ShouldNotBeNull();
            (await cache.GetAsync("kept")).ShouldBeNull();
            (await cache.GetAsync("new")).ShouldNotBeNull();
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证容量锁是否不占用用户缓存键缓存键。
    /// </summary>
    [Fact]
    public async Task CapacityLockDoesNotReserveAUserCacheKey()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory, new FileHttpResponseCacheOptions { MaxEntries = 2 });
            await cache.SetAsync("__bing_http_capacity__", Response("value"), TimeSpan.FromMinutes(1));
            (await cache.GetAsync("__bing_http_capacity__")).ShouldNotBeNull();
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证缓存容量淘汰过期项并拒绝过大的缓存项。
    /// </summary>
    [Fact]
    public async Task CapacityRemovesExpiredEntriesAndRejectsOversizedEntry()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory, new FileHttpResponseCacheOptions { MaxBytes = 1 });
            await cache.SetAsync("oversized", Response("value"), TimeSpan.FromMinutes(1));
            (await cache.GetAsync("oversized")).ShouldBeNull();

            await cache.SetAsync("expired", Response("value"), TimeSpan.FromMilliseconds(10));
            await Task.Delay(50);
            await cache.SetAsync("next", Response("value"), TimeSpan.FromMinutes(1));
            (await cache.GetAsync("expired")).ShouldBeNull();
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证容量选项要求正数值。
    /// </summary>
    [Fact]
    public void CapacityOptionsRequirePositiveValues()
    {
        var entriesDirectory = CreateDirectory();
        var bytesDirectory = CreateDirectory();
        try
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new FileHttpResponseCache(
                entriesDirectory, new FileHttpResponseCacheOptions { MaxEntries = 0 }));
            Should.Throw<ArgumentOutOfRangeException>(() => new FileHttpResponseCache(
                bytesDirectory, new FileHttpResponseCacheOptions { MaxBytes = 0 }));
        }
        finally
        {
            Directory.Delete(entriesDirectory, true);
            Directory.Delete(bytesDirectory, true);
        }
    }



    /// <summary>
    /// 验证串行化相同键工厂并是否不缓存失败。
    /// </summary>
    [Fact]
    public async Task GetOrCreate_SerializesSameKeyFactoryAndDoesNotCacheFailure()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            var calls = 0;
            var requests = Enumerable.Range(0, 4).Select(_ => cache.GetOrCreateAsync(
                "orders-1", TimeSpan.FromMinutes(1), async () =>
                {
                    Interlocked.Increment(ref calls);
                    await Task.Delay(20);
                    return Response("value");
                })).ToArray();
            var results = await Task.WhenAll(requests);
            calls.ShouldBe(1);
            results.ShouldAllBe(result => result.Data.SequenceEqual(new byte[] { 1, 2, 3 }));

            await Should.ThrowAsync<InvalidOperationException>(() => cache.GetOrCreateAsync(
                "failed", TimeSpan.FromMinutes(1), () => Task.FromException<HttpResponseResult<byte[]>>(
                    new InvalidOperationException("origin failed"))));
            (await cache.GetAsync("failed")).ShouldBeNull();
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证等待锁之前再次检查缓存项。
    /// </summary>
    [Fact]
    public async Task GetOrCreate_WaitsForOccupiedLockBeforeDoubleChecking()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            await cache.SetAsync("locked", Response("old"), TimeSpan.FromMinutes(1));
            await cache.RemoveAsync("locked");
            var lockPath = Directory.GetFiles(directory, "*.lock").Single();

            using (var heldLock = new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var calls = 0;
                var operation = Task.Run(() => cache.GetOrCreateAsync("locked", TimeSpan.FromMinutes(1), () =>
                {
                    Interlocked.Increment(ref calls);
                    return Task.FromResult(Response("new"));
                }));

                await Task.Delay(50);
                heldLock.Dispose();
                var result = await operation;
                calls.ShouldBe(1);
                result.Data.SequenceEqual(new byte[] { 1, 2, 3 }).ShouldBeTrue();
                cache.GetStatistics().LockWaits.ShouldBeGreaterThan(0);
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }



    /// <summary>
    /// 验证等待其他进程持有的锁并原子发布缓存项。
    /// </summary>
    [Fact]
    public async Task GetOrCreate_WaitsForLockHeldBySeparateProcessAndPublishesAtomically()
    {
        var directory = CreateDirectory();
        Process helper = null;
        try
        {
            var cache = new FileHttpResponseCache(directory);
            const string key = "cross-process";
            await cache.SetAsync(key, Response("old"), TimeSpan.FromMinutes(1));
            await cache.RemoveAsync(key);

            var lockPath = Path.Combine(directory, Hash(key) + ".lock");
            var readyPath = Path.Combine(directory, "helper.ready");
            var releasePath = Path.Combine(directory, "helper.release");
            helper = StartLockHelper(lockPath, readyPath, releasePath);
            await WaitForFileAsync(readyPath);

            var calls = 0;
            var operation = Task.Run(() => cache.GetOrCreateAsync(key, TimeSpan.FromMinutes(1), () =>
            {
                Interlocked.Increment(ref calls);
                return Task.FromResult(Response("new"));
            }));
            await Task.Delay(100);
            calls.ShouldBe(0);

            File.WriteAllText(releasePath, "release");
            var result = await operation;
            result.Data.ShouldBe(new byte[] { 1, 2, 3 });
            (await cache.GetAsync(key)).ShouldNotBeNull();
            helper.WaitForExit(5000).ShouldBeTrue();
        }
        finally
        {
            if (helper != null && !helper.HasExited)
            {
                helper.Kill();
                helper.WaitForExit();
            }
            Directory.Delete(directory, true);
        }
    }



    /// <summary>
    /// 验证取消期间锁等待停止之前工厂。
    /// </summary>
    [Fact]
    public async Task GetOrCreate_CancellationDuringLockWaitStopsBeforeFactory()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            await cache.SetAsync("cancel-lock", Response("old"), TimeSpan.FromMinutes(1));
            await cache.RemoveAsync("cancel-lock");
            var lockPath = Directory.GetFiles(directory, "*.lock").Single();
            using (var heldLock = new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            using (var cancellation = new CancellationTokenSource())
            {
                var calls = 0;
                var operation = Task.Run(() => cache.GetOrCreateAsync("cancel-lock", TimeSpan.FromMinutes(1), () =>
                {
                    Interlocked.Increment(ref calls);
                    return Task.FromResult(Response("unexpected"));
                }, cancellation.Token));
                await Task.Delay(50);
                cancellation.Cancel();
                await Should.ThrowAsync<OperationCanceledException>(() => operation);
                calls.ShouldBe(0);
            }
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证取消工厂是否不写入缓存。
    /// </summary>
    [Fact]
    public async Task GetOrCreate_CancelledFactoryDoesNotWriteCache()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            using var cancellation = new CancellationTokenSource();
            await Should.ThrowAsync<OperationCanceledException>(() => cache.GetOrCreateAsync(
                "cancel-factory", TimeSpan.FromMinutes(1), async () =>
                {
                    cancellation.Cancel();
                    await Task.Yield();
                    return Response("unused");
                }, cancellation.Token));
            (await cache.GetAsync("cancel-factory")).ShouldBeNull();
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证清理过期缓存和陈旧临时文件，同时保留锁文件。
    /// </summary>
    [Fact]
    public async Task CleanupAsync_RemovesExpiredCachesAndStaleTemporaryFilesButKeepsLocks()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            await cache.SetAsync("expired", Response("expired"), TimeSpan.FromMilliseconds(10));
            await Task.Delay(50);
            var temporaryPath = Path.Combine(directory, "orphan.tmp");
            File.WriteAllText(temporaryPath, "temporary");
            File.SetLastWriteTimeUtc(temporaryPath, DateTime.UtcNow.AddMinutes(-1));
            var lockPath = Directory.GetFiles(directory, "*.lock").Single();

            var result = await cache.CleanupAsync(TimeSpan.FromSeconds(1));

            result.ExpiredEntriesRemoved.ShouldBe(1);
            result.TemporaryFilesRemoved.ShouldBe(1);
            result.TotalRemoved.ShouldBe(2);
            var statistics = cache.GetStatistics();
            statistics.ExpiredEntriesRemoved.ShouldBe(1);
            statistics.TemporaryFilesRemoved.ShouldBe(1);
            File.Exists(temporaryPath).ShouldBeFalse();
            File.Exists(lockPath).ShouldBeTrue();
            (await cache.GetAsync("expired")).ShouldBeNull();
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证移除嵌套文件期间默认清理范围保持在顶层目录。
    /// </summary>
    [Fact]
    public async Task CleanupRecursiveAsync_RemovesNestedFiles_WhileDefaultCleanupStaysTopLevel()
    {
        var directory = CreateDirectory();
        var nested = Path.Combine(directory, "nested", "deeper");
        Directory.CreateDirectory(nested);
        try
        {
            var rootCache = new FileHttpResponseCache(directory);
            var nestedCache = new FileHttpResponseCache(nested);
            await nestedCache.SetAsync("expired", Response("expired"), TimeSpan.FromMilliseconds(10));
            await Task.Delay(50);
            var temporary = Path.Combine(nested, "orphan.tmp");
            File.WriteAllText(temporary, "temporary");
            File.SetLastWriteTimeUtc(temporary, DateTime.UtcNow.AddMinutes(-1));

            var topLevel = await rootCache.CleanupAsync(TimeSpan.Zero);
            topLevel.TotalRemoved.ShouldBe(0);
            Directory.GetFiles(nested, "*.cache").ShouldHaveSingleItem();

            var recursive = await rootCache.CleanupRecursiveAsync(TimeSpan.Zero);
            recursive.ExpiredEntriesRemoved.ShouldBe(1);
            recursive.TemporaryFilesRemoved.ShouldBe(1);
            File.Exists(temporary).ShouldBeFalse();
            Directory.GetFiles(nested, "*.cache").ShouldBeEmpty();
            Directory.GetFiles(nested, "*.lock").Length.ShouldBe(2);
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证遵循取消之前锁取得。
    /// </summary>
    [Fact]
    public async Task CleanupAsync_HonorsCancellationBeforeLockAcquisition()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                await Should.ThrowAsync<OperationCanceledException>(() =>
                    cache.CleanupAsync(TimeSpan.Zero, cancellation.Token));
            }
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证保留被锁定的临时文件且不计入删除失败数。
    /// </summary>
    [Fact]
    public async Task CleanupAsync_KeepsLockedTemporaryFileAndDoesNotCountFailedDelete()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            var temporaryPath = Path.Combine(directory, "occupied.tmp");
            File.WriteAllText(temporaryPath, "temporary");
            File.SetLastWriteTimeUtc(temporaryPath, DateTime.UtcNow.AddMinutes(-1));
            using (var held = new FileStream(temporaryPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var result = await cache.CleanupAsync(TimeSpan.Zero);
                result.TemporaryFilesRemoved.ShouldBe(0);
                cache.GetStatistics().TemporaryFilesRemoved.ShouldBe(0);
                File.Exists(temporaryPath).ShouldBeTrue();
            }
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证等待缓存项锁时遵循取消请求。
    /// </summary>
    [Fact]
    public async Task CleanupAsync_HonorsCancellationWhileWaitingForEntryLock()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            await cache.SetAsync("expired", Response("expired"), TimeSpan.FromMilliseconds(10));
            await Task.Delay(50);
            var lockPath = Directory.GetFiles(directory, "*.lock").Single();
            using (var heldLock = new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            using (var cancellation = new CancellationTokenSource())
            {
                var operation = Task.Run(() => cache.CleanupAsync(TimeSpan.Zero, cancellation.Token));
                await Task.Delay(50);
                cancellation.Cancel();
                await Should.ThrowAsync<OperationCanceledException>(() => operation);
            }
            Directory.GetFiles(directory, "*.cache").ShouldHaveSingleItem();
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证重新读取缓存项之后其键锁是否释放。
    /// </summary>
    [Fact]
    public async Task CleanupAsync_RereadsEntryAfterItsKeyLockIsReleased()
    {
        var directory = CreateDirectory();
        var sourceDirectory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            var sourceCache = new FileHttpResponseCache(sourceDirectory);
            await cache.SetAsync("orders-1", Response("expired"), TimeSpan.FromMilliseconds(10));
            await Task.Delay(50);
            await sourceCache.SetAsync("orders-1", Response("fresh"), TimeSpan.FromMinutes(1));

            var targetPath = Directory.GetFiles(directory, "*.cache").Single();
            var sourcePath = Directory.GetFiles(sourceDirectory, "*.cache").Single();
            var lockPath = Directory.GetFiles(directory, "*.lock").Single();
            using (var heldLock = new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var operation = Task.Run(() => cache.CleanupAsync(TimeSpan.Zero));
                await Task.Delay(50);
                File.Copy(sourcePath, targetPath, true);
                heldLock.Dispose();
                await operation;
            }

            (await cache.GetAsync("orders-1")).ShouldNotBeNull();
        }
        finally
        {
            Directory.Delete(directory, true);
            Directory.Delete(sourceDirectory, true);
        }
    }



    /// <summary>
    /// 验证仅清理根目录缓存文件，并忽略损坏文件和临时文件。
    /// </summary>
    [Fact]
    public async Task ClearAsync_RemovesOnlyRootCacheFilesAndIgnoresCorruptAndTemporaryFiles()
    {
        var directory = CreateDirectory();
        var outside = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            await cache.SetAsync("one", Response("one"), TimeSpan.FromMinutes(1));
            await cache.SetAsync("two", Response("two"), TimeSpan.FromMinutes(1));
            File.WriteAllBytes(Path.Combine(directory, "corrupt.cache"), new byte[] { 1, 2, 3 });
            File.WriteAllText(Path.Combine(directory, "pending.tmp"), "temporary");
            File.WriteAllText(Path.Combine(outside, "outside.cache"), "sentinel");

            (await cache.ClearAsync()).ShouldBe(2);
            Directory.GetFiles(directory, "*.cache").Length.ShouldBe(1);
            File.Exists(Path.Combine(directory, "pending.tmp")).ShouldBeTrue();
            File.Exists(Path.Combine(outside, "outside.cache")).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(directory, true);
            Directory.Delete(outside, true);
        }
    }



    /// <summary>
    /// 验证注入的删除操作失败时不计入已删除数量。
    /// </summary>
    [Fact]
    public async Task CleanupAsync_UsesInjectedDeleteFailureWithoutCountingRemoval()
    {
        var directory = CreateDirectory();
        try
        {
            var fileSystem = new TestMaintenanceFileSystem { DeleteReturnsFalse = true };
            var cache = new FileHttpResponseCache(directory, null, fileSystem);
            var temporaryPath = Path.Combine(directory, "injected.tmp");
            File.WriteAllText(temporaryPath, "temporary");
            File.SetLastWriteTimeUtc(temporaryPath, DateTime.UtcNow.AddMinutes(-1));

            var result = await cache.CleanupAsync(TimeSpan.Zero);

            result.TemporaryFilesRemoved.ShouldBe(0);
            File.Exists(temporaryPath).ShouldBeTrue();
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证注入的权限错误不会被计入已删除数量。
    /// </summary>
    [Fact]
    public async Task ClearAsync_UsesInjectedPermissionFailureWithoutCountingRemoval()
    {
        var directory = CreateDirectory();
        try
        {
            var fileSystem = new TestMaintenanceFileSystem { DeleteThrowsUnauthorized = true };
            var cache = new FileHttpResponseCache(directory, null, fileSystem);
            await cache.SetAsync("permission", Response("value"), TimeSpan.FromMinutes(1));

            (await cache.ClearAsync()).ShouldBe(0);
            Directory.GetFiles(directory, "*.cache").ShouldHaveSingleItem();
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证枚举操作异常会向上传播。
    /// </summary>
    [Fact]
    public async Task ClearAsync_PropagatesInjectedEnumerationFailure()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory, null,
                new TestMaintenanceFileSystem { EnumerationError = new IOException("injected enumeration failure") });

            await Should.ThrowAsync<IOException>(() => cache.ClearAsync());
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证截断文件是否被忽略获取并清理。
    /// </summary>
    [Fact]
    public async Task TruncatedFileIsIgnoredByGetAndClear()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            await cache.SetAsync("truncated", Response("value"), TimeSpan.FromMinutes(1));
            var path = Directory.GetFiles(directory, "*.cache").Single();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read))
                stream.SetLength(Math.Max(1, stream.Length - 1));

            (await cache.GetAsync("truncated")).ShouldBeNull();

            await cache.SetAsync("truncated", Response("value"), TimeSpan.FromMinutes(1));
            path = Directory.GetFiles(directory, "*.cache").Single();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read))
                stream.SetLength(Math.Max(1, stream.Length - 1));
            (await cache.ClearAsync()).ShouldBe(0);
            File.Exists(path).ShouldBeTrue();
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 验证是否不正在运行的回源操作不重复发布工厂。
    /// </summary>
    [Fact]
    public async Task ClearAsync_DoesNotRepublishAnAlreadyRunningFactory()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var operation = cache.GetOrCreateAsync("epoch", TimeSpan.FromMinutes(1), async () =>
            {
                started.SetResult(true);
                await release.Task;
                return Response("stale");
            });

            await started.Task;
            (await cache.ClearAsync()).ShouldBe(0);
            release.SetResult(true);
            (await operation).Data.ShouldBe(new byte[] { 1, 2, 3 });
            (await cache.GetAsync("epoch")).ShouldBeNull();
        }
        finally { Directory.Delete(directory, true); }
    }



    /// <summary>
    /// 按旧版缓存格式写入用于迁移测试的缓存文件。
    /// </summary>
    /// <param name="directory">保存缓存文件的目录。</param>
    /// <param name="key">写入文件并用于计算文件名的缓存键。</param>
    /// <param name="response">要序列化的响应状态、头及字节正文。</param>
    /// <param name="ttl">相对当前时间计算过期时刻的有效期。</param>
    private static void WriteLegacyCacheFile(string directory, string key,
        HttpResponseResult<byte[]> response, TimeSpan ttl)
    {
        var path = Path.Combine(directory, Hash(key) + ".cache");
        using (var stream = File.Create(path))
        using (var writer = new BinaryWriter(stream, Encoding.UTF8))
        {
            writer.Write("BINGHTTP1");
            writer.Write(key);
            writer.Write((DateTimeOffset.UtcNow + ttl).UtcTicks);
            writer.Write((int)response.StatusCode);
            WriteHeaders(writer, response.Headers);
            WriteHeaders(writer, response.ContentHeaders);
            writer.Write(response.Data.Length);
            writer.Write(response.Data);
        }
    }



    /// <summary>
    /// 将多值响应头写入旧版缓存的二进制结构。
    /// </summary>
    /// <param name="writer">接收响应头数据的二进制写入器。</param>
    /// <param name="headers">待写入的响应头集合；为 null 时写入零项。</param>
    private static void WriteHeaders(BinaryWriter writer,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
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
    /// 创建包含固定字节正文及实体标签的成功响应样例。
    /// </summary>
    /// <param name="value">响应的原始字符串正文。</param>
    /// <returns>实体标签为 <c>v1</c> 且数据为三个固定字节的响应。</returns>
    private static HttpResponseResult<byte[]> Response(string value)
        => new HttpResponseResult<byte[]>(HttpStatusCode.OK,
            new Dictionary<string, IReadOnlyList<string>> { ["ETag"] = new[] { "\"v1\"" } },
            new Dictionary<string, IReadOnlyList<string>>(), new byte[] { 1, 2, 3 }, value);



    /// <summary>
    /// 创建用于文件响应缓存测试的独立临时目录。
    /// </summary>
    /// <returns>已创建的唯一临时目录完整路径。</returns>
    private static string CreateDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "bing-file-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }



    /// <summary>
    /// 启动持有独占文件锁并通过标记文件同步的测试进程。
    /// </summary>
    /// <param name="lockPath">子进程独占打开的锁文件路径。</param>
    /// <param name="readyPath">获取锁后写入的就绪标记路径。</param>
    /// <param name="releasePath">指示子进程释放锁并退出的标记路径。</param>
    /// <returns>已启动的 PowerShell 进程，调用方负责同步退出并释放。</returns>
    /// <remarks>当前系统缺少可启动的 PowerShell 时，以跳过测试异常报告环境限制。</remarks>
    private static Process StartLockHelper(string lockPath, string readyPath, string releasePath)
    {
        var executable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell.exe" : "pwsh";
        const string script = "$lock=[IO.File]::Open($env:BING_CACHE_LOCK,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); [IO.File]::WriteAllText($env:BING_CACHE_READY,'ready'); while(-not [IO.File]::Exists($env:BING_CACHE_RELEASE)){ Start-Sleep -Milliseconds 10 }; $lock.Dispose()";
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = "-NoProfile -NonInteractive -EncodedCommand " + encoded,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        startInfo.EnvironmentVariables["BING_CACHE_LOCK"] = lockPath;
        startInfo.EnvironmentVariables["BING_CACHE_READY"] = readyPath;
        startInfo.EnvironmentVariables["BING_CACHE_RELEASE"] = releasePath;
        try
        {
            var process = Process.Start(startInfo);
            if (process == null) throw new InvalidOperationException("The lock helper process could not be started.");
            return process;
        }
        catch (System.ComponentModel.Win32Exception error)
        {
            throw Xunit.Sdk.SkipException.ForSkip("PowerShell is unavailable; cross-process cache locking is not tested on this target: " + error.Message);
        }
    }



    /// <summary>
    /// 异步等待跨进程锁辅助程序写入就绪标记。
    /// </summary>
    /// <param name="path">预期出现的就绪标记文件路径。</param>
    /// <remarks>最多等待五秒；超时后通过断言报告失败。</remarks>
    private static async Task WaitForFileAsync(string path)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!File.Exists(path) && DateTime.UtcNow < deadline)
            await Task.Delay(10);
        File.Exists(path).ShouldBeTrue("The cross-process lock helper did not signal readiness.");
    }



    /// <summary>
    /// 计算缓存键的 UTF-8 SHA-256 文件名摘要。
    /// </summary>
    /// <param name="key">需要映射为缓存文件名的原始键。</param>
    /// <returns>不含分隔符的小写十六进制摘要。</returns>
    private static string Hash(string key)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-", string.Empty).ToLowerInvariant();
    }



    /// <summary>
    /// 为缓存清理测试提供可控的文件和目录操作实现。
    /// </summary>
    private sealed class TestMaintenanceFileSystem : IFileHttpResponseCacheFileSystem
    {
        /// <summary>
        /// 获取或设置是否模拟文件删除返回失败。
        /// </summary>
        public bool DeleteReturnsFalse { get; set; }
        /// <summary>
        /// 获取或设置是否在文件删除时抛出模拟的权限异常。
        /// </summary>
        public bool DeleteThrowsUnauthorized { get; set; }
        /// <summary>
        /// 获取或设置文件枚举时抛出的模拟异常；为 null 时正常枚举。
        /// </summary>
        public Exception EnumerationError { get; set; }

        /// <inheritdoc />
        public IEnumerable<string> GetFiles(string directory, string pattern)
        {
            if (EnumerationError != null) throw EnumerationError;
            return Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
        }

        /// <inheritdoc />
        public IEnumerable<string> GetDirectories(string directory)
            => Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);

        /// <inheritdoc />
        public bool FileExists(string path) => File.Exists(path);

        /// <inheritdoc />
        public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);

        /// <inheritdoc />
        public FileStream OpenLock(string path, CancellationToken cancellationToken, Action onWait)
            => new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        /// <inheritdoc />
        public bool TryDelete(string path)
        {
            if (DeleteThrowsUnauthorized) throw new UnauthorizedAccessException("injected permission failure");
            if (DeleteReturnsFalse || !File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }
    }
}
