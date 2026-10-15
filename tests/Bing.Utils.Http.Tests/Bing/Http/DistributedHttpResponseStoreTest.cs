using System.Net;
using System.Net.Http;
using System.Threading;
using Bing.Http;
using Bing.Http.Caching;

namespace Bing.Utils.Http.Tests.Bing.Http;



/// <summary>
/// 共享 HTTP 响应快照存储契约的进程内适配器测试。
/// </summary>
public class DistributedHttpResponseStoreTest
{
    /// <summary>
    /// 验证使用分布式快照并显式认证显式启用。
    /// </summary>
    [Fact]
    public async Task GetCached_UsesDistributedSnapshotAndExplicitAuthenticationOptIn()
    {
        var store = new InMemoryDistributedHttpResponseStore();
        var calls = 0;
        var first = await store.ExecuteCachedAsync("orders:1", TimeSpan.FromMinutes(1), () =>
        {
            calls++;
            return Task.FromResult(new HttpResponseResult<byte[]>(HttpStatusCode.OK,
                new Dictionary<string, IReadOnlyList<string>> { ["ETag"] = new[] { "\"v1\"" } },
                new Dictionary<string, IReadOnlyList<string>>(), new byte[] { 1, 2 }, "body"));
        }, requestContainsAuthorization: true, allowAuthenticated: true);
        var second = await store.ExecuteCachedAsync("orders:1", TimeSpan.FromMinutes(1), () =>
            Task.FromResult<HttpResponseResult<byte[]>>(null));

        calls.ShouldBe(1);
        second.Data.ShouldBe(new byte[] { 1, 2 });
        second.Headers["ETag"].Single().ShouldBe("\"v1\"");
        second.RawContent.ShouldBe("body");
    }



    /// <summary>
    /// 验证拒绝认证请求未使用显式显式启用。
    /// </summary>
    [Fact]
    public async Task GetCached_RejectsAuthenticatedRequestWithoutExplicitOptIn()
    {
        var store = new InMemoryDistributedHttpResponseStore();
        await Should.ThrowAsync<InvalidOperationException>(() => store.ExecuteCachedAsync(
            "private:1", TimeSpan.FromMinutes(1),
            () => Task.FromResult<HttpResponseResult<byte[]>>(null),
            requestContainsAuthorization: true));
    }






    /// <summary>
    /// 验证设置获取并比较并设置使用不透明版本。
    /// </summary>
    [Fact]
    public async Task SetGetAndCompareAndSetUseOpaqueVersions()
    {
        IDistributedHttpResponseStore store = new InMemoryDistributedHttpResponseStore();
        var first = await store.SetAsync("orders:1", Snapshot("one"), TimeSpan.FromMinutes(1));
        first.Version.ShouldNotBeNullOrWhiteSpace();
        (await store.GetAsync("orders:1")).Body.ShouldBe(new byte[] { 1 });

        (await store.CompareAndSetAsync("orders:1", "stale", Snapshot("two"), TimeSpan.FromMinutes(1))).ShouldBeFalse();
        (await store.CompareAndSetAsync("orders:1", first.Version, Snapshot("two"), TimeSpan.FromMinutes(1))).ShouldBeTrue();
        (await store.GetAsync("orders:1")).Body.ShouldBe(new byte[] { 2 });
    }



    /// <summary>
    /// 验证过期时间并按版本移除是否强制执行。
    /// </summary>
    [Fact]
    public async Task ExpirationAndVersionedRemovalAreEnforced()
    {
        IDistributedHttpResponseStore store = new InMemoryDistributedHttpResponseStore();
        var stored = await store.SetAsync("orders:1", Snapshot("one"), TimeSpan.FromMilliseconds(20));
        (await store.RemoveAsync("orders:1", "stale")).ShouldBeFalse();
        await Task.Delay(50);
        (await store.GetAsync("orders:1")).ShouldBeNull();
        (await store.RemoveAsync("orders:1", stored.Version)).ShouldBeFalse();
    }



    /// <summary>
    /// 验证快照数据是否已复制并取消是否遵循。
    /// </summary>
    [Fact]
    public async Task SnapshotDataIsCopiedAndCancellationIsHonored()
    {
        IDistributedHttpResponseStore store = new InMemoryDistributedHttpResponseStore();
        var body = new byte[] { 1, 2 };
        var stored = await store.SetAsync("orders:1", Snapshot(body), TimeSpan.FromMinutes(1));
        body[0] = 9;
        (await store.GetAsync("orders:1")).Body.ShouldBe(new byte[] { 1, 2 });
        var read = await store.GetAsync("orders:1");
        read.Body[0] = 9;
        (await store.GetAsync("orders:1")).Body.ShouldBe(new byte[] { 1, 2 });

        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            await Should.ThrowAsync<OperationCanceledException>(() => store.GetAsync("orders:1", cancellation.Token));
        }
        (await store.RemoveAsync("orders:1", stored.Version)).ShouldBeTrue();
    }



    /// <summary>
    /// 验证缓存执行会使用租约存储协调并发未命中请求。
    /// </summary>
    [Fact]
    public async Task ExecuteCachedCoordinatesConcurrentMissesWithTheLeaseStore()
    {
        var store = new InMemoryDistributedHttpResponseStore();
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var first = store.ExecuteCachedAsync("orders:lease", TimeSpan.FromSeconds(2), async () =>
        {
            Interlocked.Increment(ref calls);
            started.SetResult(true);
            await release.Task;
            return new HttpResponseResult<byte[]>(HttpStatusCode.OK,
                new Dictionary<string, IReadOnlyList<string>>(),
                new Dictionary<string, IReadOnlyList<string>>(), new byte[] { 7 }, null);
        });
        await started.Task;
        var second = store.ExecuteCachedAsync("orders:lease", TimeSpan.FromSeconds(2),
            () => Task.FromResult<HttpResponseResult<byte[]>>(null));
        release.SetResult(true);

        var results = await Task.WhenAll(first, second);
        calls.ShouldBe(1);
        results[0].Data.ShouldBe(new byte[] { 7 });
        results[1].Data.ShouldBe(new byte[] { 7 });
    }



    /// <summary>
    /// 验证内存中租约过期并可以再次取得。
    /// </summary>
    [Fact]
    public async Task InMemoryLeaseExpiresAndCanBeAcquiredAgain()
    {
        var store = new InMemoryDistributedHttpResponseStore();
        var first = await store.TryAcquireAsync("orders:lease", TimeSpan.FromMilliseconds(30));
        (await store.TryAcquireAsync("orders:lease", TimeSpan.FromMilliseconds(30))).ShouldBeNull();
        await Task.Delay(60);
        var second = await store.TryAcquireAsync("orders:lease", TimeSpan.FromMilliseconds(30));
        second.ShouldNotBeNull();
        await first.ReleaseAsync();
        await second.ReleaseAsync();
    }



    /// <summary>
    /// 验证缓存执行不会隐藏租约释放失败。
    /// </summary>
    [Fact]
    public async Task ExecuteCachedDoesNotHideLeaseReleaseFailure()
    {
        var store = new ThrowingLeaseStore();
        await Should.ThrowAsync<InvalidOperationException>(() => store.ExecuteCachedAsync(
            "orders:lease-error", TimeSpan.FromSeconds(1),
            () => Task.FromResult(SnapshotResponse("value"))));
    }



    /// <summary>
    /// 验证快照读取失败时缓存执行仍会调用回源工厂。
    /// </summary>
    [Fact]
    public async Task ExecuteCachedPropagatesSnapshotReadFailureBeforeFactory()
    {
        var store = new FaultInjectionStore { ThrowOnRead = true };
        var calls = 0;

        await Should.ThrowAsync<IOException>(() => store.ExecuteCachedAsync(
            "orders:read-error", TimeSpan.FromSeconds(1), () =>
            {
                Interlocked.Increment(ref calls);
                return Task.FromResult(SnapshotResponse("unexpected"));
            }));

        calls.ShouldBe(0);
    }



    /// <summary>
    /// 验证回源工厂失败后会释放租约，并允许下一个调用方取得租约。
    /// </summary>
    [Fact]
    public async Task ExecuteCachedReleasesLeaseWhenFactoryFailsAndNextCallerTakesOver()
    {
        var store = new InMemoryDistributedHttpResponseStore();
        var calls = 0;

        await Should.ThrowAsync<InvalidOperationException>(() => store.ExecuteCachedAsync(
            "orders:factory-error", TimeSpan.FromSeconds(1), () =>
            {
                Interlocked.Increment(ref calls);
                throw new InvalidOperationException("injected origin failure");
            }));

        (await store.GetAsync("orders:factory-error")).ShouldBeNull();
        var result = await store.ExecuteCachedAsync("orders:factory-error", TimeSpan.FromSeconds(1), () =>
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult(SnapshotResponse("recovered"));
        });

        calls.ShouldBe(2);
        result.RawContent.ShouldBe("recovered");
    }



    /// <summary>
    /// 验证租约超时后回退读取缓存，且不会永久等待。
    /// </summary>
    [Fact]
    public async Task ExecuteCachedLeaseTimeoutFallsBackWithoutPermanentWait()
    {
        var store = new FaultInjectionStore { LeaseUnavailable = true };
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var result = await store.ExecuteCachedAsync("orders:lease-timeout", TimeSpan.FromMilliseconds(50),
            () => Task.FromResult(SnapshotResponse("fallback")));

        watch.Stop();
        watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
        result.RawContent.ShouldBe("fallback");
    }



    /// <summary>
    /// 验证取消缓存执行会停止等待租约。
    /// </summary>
    [Fact]
    public async Task ExecuteCachedCancellationStopsLeaseWait()
    {
        var store = new FaultInjectionStore { LeaseUnavailable = true };
        using var cancellation = new CancellationTokenSource(50);
        var calls = 0;

        await Should.ThrowAsync<OperationCanceledException>(() => store.ExecuteCachedAsync(
            "orders:lease-cancel", TimeSpan.FromMinutes(1), () =>
            {
                Interlocked.Increment(ref calls);
                return Task.FromResult(SnapshotResponse("unexpected"));
            }, cancellationToken: cancellation.Token));

        calls.ShouldBe(0);
    }



    /// <summary>
    /// 验证多个节点共用一个租约并发布快照。
    /// </summary>
    [Fact]
    public async Task IndependentNodesShareOneLeaseAndPublishedSnapshot()
    {
        var state = new SharedDistributedState();
        var firstNode = new SharedNodeStore(state);
        var secondNode = new SharedNodeStore(state);
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;

        var first = firstNode.ExecuteCachedAsync("orders:cross-node", TimeSpan.FromSeconds(2), async () =>
        {
            Interlocked.Increment(ref calls);
            started.SetResult(true);
            await release.Task;
            return SnapshotResponse("published-by-first-node");
        });
        await started.Task;
        var second = secondNode.ExecuteCachedAsync("orders:cross-node", TimeSpan.FromSeconds(2),
            () => Task.FromResult<HttpResponseResult<byte[]>>(null));
        release.SetResult(true);

        var results = await Task.WhenAll(first, second);
        calls.ShouldBe(1);
        results[1].RawContent.ShouldBe("published-by-first-node");
        (await secondNode.GetAsync("orders:cross-node")).RawContent.ShouldBe("published-by-first-node");
    }



    /// <summary>
    /// 验证独立节点取得超时之后租约持有方失败。
    /// </summary>
    [Fact]
    public async Task IndependentNodeTakesOverAfterLeaseHolderFails()
    {
        var state = new SharedDistributedState();
        var firstNode = new SharedNodeStore(state);
        var secondNode = new SharedNodeStore(state);
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fail = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;

        var first = firstNode.ExecuteCachedAsync("orders:takeover", TimeSpan.FromSeconds(2), async () =>
        {
            Interlocked.Increment(ref calls);
            started.SetResult(true);
            await fail.Task;
            throw new InvalidOperationException("injected node failure");
        });
        await started.Task;
        var second = secondNode.ExecuteCachedAsync("orders:takeover", TimeSpan.FromSeconds(2), () =>
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult(SnapshotResponse("published-by-second-node"));
        });
        fail.SetResult(true);

        await Should.ThrowAsync<InvalidOperationException>(() => first);
        var result = await second;
        calls.ShouldBe(2);
        result.RawContent.ShouldBe("published-by-second-node");
        (await firstNode.GetAsync("orders:takeover")).RawContent.ShouldBe("published-by-second-node");
    }



    /// <summary>
    /// 验证独立节点可取得过期租约，且不会永久阻塞。
    /// </summary>
    [Fact]
    public async Task IndependentNodeCanTakeOverExpiredLeaseWithoutPermanentBlocking()
    {
        var state = new SharedDistributedState();
        var firstNode = new SharedNodeStore(state);
        var secondNode = new SharedNodeStore(state);
        var held = await firstNode.TryAcquireAsync("orders:expired-lease", TimeSpan.FromMilliseconds(40));
        held.ShouldNotBeNull();
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var result = await secondNode.ExecuteCachedAsync("orders:expired-lease", TimeSpan.FromMilliseconds(40),
            () => Task.FromResult(SnapshotResponse("after-expiry")));

        watch.Stop();
        watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
        result.RawContent.ShouldBe("after-expiry");
    }



    /// <summary>
    /// 验证独立节点保留键并版本相互隔离。
    /// </summary>
    [Fact]
    public async Task IndependentNodesKeepKeysAndVersionsIsolated()
    {
        var state = new SharedDistributedState();
        var firstNode = new SharedNodeStore(state);
        var secondNode = new SharedNodeStore(state);
        var first = await firstNode.SetAsync("orders:a", Snapshot("one"), TimeSpan.FromMinutes(1));
        var second = await secondNode.SetAsync("orders:b", Snapshot("two"), TimeSpan.FromMinutes(1));

        first.Version.ShouldNotBe(second.Version);
        (await secondNode.GetAsync("orders:a")).Body.ShouldBe(new byte[] { 1 });
        (await firstNode.GetAsync("orders:b")).Body.ShouldBe(new byte[] { 2 });
        (await secondNode.GetAsync("orders:missing")).ShouldBeNull();
    }



    /// <summary>
    /// 验证命名空间失效状态共享并隐藏旧快照。
    /// </summary>
    [Fact]
    public async Task NamespaceInvalidationIsSharedAndHidesOldSnapshots()
    {
        var state = new SharedDistributedState();
        var firstNode = new SharedNodeStore(state);
        var secondNode = new SharedNodeStore(state);
        var calls = 0;

        var first = await firstNode.ExecuteCachedAsync("orders:1", TimeSpan.FromMinutes(1),
            () => Task.FromResult(SnapshotResponse("before")), namespaceName: "orders");
        (await secondNode.GetCachedAsync("orders:1", TimeSpan.FromMinutes(1),
            () => Task.FromResult<HttpResponseResult<byte[]>>(null), namespaceName: "orders")).RawContent.ShouldBe("before");

        var oldVersion = await firstNode.GetNamespaceVersionAsync("orders");
        var newVersion = await secondNode.InvalidateNamespaceAsync("orders");
        newVersion.ShouldNotBe(oldVersion);

        var refreshed = await firstNode.ExecuteCachedAsync("orders:1", TimeSpan.FromMinutes(1), () =>
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult(SnapshotResponse("after"));
        }, namespaceName: "orders");

        calls.ShouldBe(1);
        first.RawContent.ShouldBe("before");
        refreshed.RawContent.ShouldBe("after");
    }



    /// <summary>
    /// 验证命名空间租约键不串行化不同命名空间。
    /// </summary>
    [Fact]
    public async Task NamespaceLeaseKeysDoNotSerializeDifferentNamespaces()
    {
        var state = new SharedDistributedState();
        var firstNode = new SharedNodeStore(state);
        var secondNode = new SharedNodeStore(state);
        var calls = 0;

        var first = firstNode.ExecuteCachedAsync("same-key", TimeSpan.FromSeconds(1), () =>
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult(SnapshotResponse("namespace-a"));
        }, namespaceName: "namespace-a");
        var second = secondNode.ExecuteCachedAsync("same-key", TimeSpan.FromSeconds(1), () =>
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult(SnapshotResponse("namespace-b"));
        }, namespaceName: "namespace-b");

        var results = await Task.WhenAll(first, second);
        calls.ShouldBe(2);
        results.Select(item => item.RawContent).ShouldBe(new[] { "namespace-a", "namespace-b" });
    }



    /// <summary>
    /// 验证清理命名空间时移除旧版本并保留当前快照。
    /// </summary>
    [Fact]
    public async Task NamespaceCleanupRemovesOldVersionsAndRetainsCurrentSnapshot()
    {
        var store = new InMemoryDistributedHttpResponseStore();
        await store.ExecuteCachedAsync("orders:1", TimeSpan.FromMinutes(1),
            () => Task.FromResult(SnapshotResponse("old")), namespaceName: "orders");
        await store.InvalidateNamespaceAsync("orders");
        await store.ExecuteCachedAsync("orders:1", TimeSpan.FromMinutes(1),
            () => Task.FromResult(SnapshotResponse("current")), namespaceName: "orders");

        (await store.CleanupNamespaceAsync("orders")).ShouldBe(1);
        var current = await store.ExecuteCachedAsync("orders:1", TimeSpan.FromMinutes(1),
            () => Task.FromResult<HttpResponseResult<byte[]>>(null), namespaceName: "orders");
        current.RawContent.ShouldBe("current");
    }



    /// <summary>
    /// 验证多个节点可以并发失效并清理共享命名空间。
    /// </summary>
    [Fact]
    public async Task IndependentNodesCanInvalidateAndCleanupSharedNamespaceConcurrently()
    {
        var state = new SharedDistributedState();
        var firstNode = new SharedNodeStore(state);
        var secondNode = new SharedNodeStore(state);
        await firstNode.ExecuteCachedAsync("orders:1", TimeSpan.FromMinutes(1),
            () => Task.FromResult(SnapshotResponse("old")), namespaceName: "orders");
        await secondNode.InvalidateNamespaceAsync("orders");

        var cleanup = firstNode.CleanupNamespaceAsync("orders");
        var current = secondNode.ExecuteCachedAsync("orders:1", TimeSpan.FromMinutes(1),
            () => Task.FromResult(SnapshotResponse("current")), namespaceName: "orders");
        await Task.WhenAll(cleanup, current);

        (await firstNode.ExecuteCachedAsync("orders:1", TimeSpan.FromMinutes(1),
            () => Task.FromResult<HttpResponseResult<byte[]>>(null), namespaceName: "orders"))
            .RawContent.ShouldBe("current");
    }



    /// <summary>
    /// 创建测试用分布式响应快照。
    /// </summary>
    /// <param name="value">用于选择字节载荷的标识；为 <c>one</c> 时载荷为 1，否则为 2。</param>
    /// <returns>包含选定单字节正文的成功响应快照。</returns>
    private static DistributedHttpResponseSnapshot Snapshot(string value)
        => Snapshot(new byte[] { value == "one" ? (byte)1 : (byte)2 });



    /// <summary>
    /// 创建测试用分布式响应快照。
    /// </summary>
    /// <param name="body">快照保存的字节正文。</param>
    /// <returns>包含指定正文且无响应头的成功响应快照。</returns>
    private static DistributedHttpResponseSnapshot Snapshot(byte[] body)
        => new DistributedHttpResponseSnapshot(HttpStatusCode.OK,
            new Dictionary<string, IReadOnlyList<string>>(),
            new Dictionary<string, IReadOnlyList<string>>(), body);



    /// <summary>
    /// 创建包含固定字节数据及指定原始正文的响应样例。
    /// </summary>
    /// <param name="value">响应的原始字符串正文。</param>
    /// <returns>数据为单字节且状态为成功的响应结果。</returns>
    private static HttpResponseResult<byte[]> SnapshotResponse(string value)
        => new HttpResponseResult<byte[]>(HttpStatusCode.OK,
            new Dictionary<string, IReadOnlyList<string>>(),
            new Dictionary<string, IReadOnlyList<string>>(), new byte[] { 1 }, value);



    /// <summary>
    /// 使用内存存储处理缓存操作并模拟租约释放失败的存储。
    /// </summary>
    private sealed class ThrowingLeaseStore : IDistributedHttpResponseStore, IDistributedHttpResponseLeaseStore
    {
        /// <summary>
        /// 测试中使用的存储状态。
        /// </summary>
        private readonly InMemoryDistributedHttpResponseStore _store = new InMemoryDistributedHttpResponseStore();

        /// <inheritdoc />
        public Task<DistributedHttpResponseSnapshot> GetAsync(string key, CancellationToken cancellationToken = default)
            => _store.GetAsync(key, cancellationToken);

        /// <inheritdoc />
        public Task<DistributedHttpResponseSnapshot> SetAsync(string key, DistributedHttpResponseSnapshot snapshot,
            TimeSpan ttl, CancellationToken cancellationToken = default)
            => _store.SetAsync(key, snapshot, ttl, cancellationToken);

        /// <inheritdoc />
        public Task<bool> CompareAndSetAsync(string key, string expectedVersion,
            DistributedHttpResponseSnapshot snapshot, TimeSpan ttl, CancellationToken cancellationToken = default)
            => _store.CompareAndSetAsync(key, expectedVersion, snapshot, ttl, cancellationToken);

        /// <inheritdoc />
        public Task<bool> RemoveAsync(string key, string expectedVersion = null,
            CancellationToken cancellationToken = default)
            => _store.RemoveAsync(key, expectedVersion, cancellationToken);

        /// <inheritdoc />
        public Task<IDistributedHttpResponseLease> TryAcquireAsync(string key, TimeSpan leaseTtl,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IDistributedHttpResponseLease>(new ThrowingLease());
    }



    /// <summary>
    /// 在释放操作中返回异常的测试租约。
    /// </summary>
    private sealed class ThrowingLease : IDistributedHttpResponseLease
    {
        /// <inheritdoc />
        public Task ReleaseAsync(CancellationToken cancellationToken = default)
            => Task.FromException(new InvalidOperationException("injected lease release failure"));
    }



    /// <summary>
    /// 可按测试标志模拟读失败或租约不可用的分布式存储。
    /// </summary>
    private sealed class FaultInjectionStore : IDistributedHttpResponseStore, IDistributedHttpResponseLeaseStore
    {
        /// <summary>
        /// 测试中使用的存储状态。
        /// </summary>
        private readonly InMemoryDistributedHttpResponseStore _store = new InMemoryDistributedHttpResponseStore();



        /// <summary>
        /// 获取或设置是否在读取快照时抛出模拟的 I/O 异常。
        /// </summary>
        public bool ThrowOnRead { get; set; }
        /// <summary>
        /// 获取或设置是否模拟无法取得租约的状态。
        /// </summary>
        public bool LeaseUnavailable { get; set; }

        /// <inheritdoc />
        public Task<DistributedHttpResponseSnapshot> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (ThrowOnRead) throw new IOException("injected snapshot read failure");
            return _store.GetAsync(key, cancellationToken);
        }

        /// <inheritdoc />
        public Task<DistributedHttpResponseSnapshot> SetAsync(string key, DistributedHttpResponseSnapshot snapshot,
            TimeSpan ttl, CancellationToken cancellationToken = default)
            => _store.SetAsync(key, snapshot, ttl, cancellationToken);

        /// <inheritdoc />
        public Task<bool> CompareAndSetAsync(string key, string expectedVersion,
            DistributedHttpResponseSnapshot snapshot, TimeSpan ttl, CancellationToken cancellationToken = default)
            => _store.CompareAndSetAsync(key, expectedVersion, snapshot, ttl, cancellationToken);

        /// <inheritdoc />
        public Task<bool> RemoveAsync(string key, string expectedVersion = null,
            CancellationToken cancellationToken = default)
            => _store.RemoveAsync(key, expectedVersion, cancellationToken);

        /// <inheritdoc />
        public Task<IDistributedHttpResponseLease> TryAcquireAsync(string key, TimeSpan leaseTtl,
            CancellationToken cancellationToken = default)
        {
            if (LeaseUnavailable) return Task.FromResult<IDistributedHttpResponseLease>(null);
            return _store.TryAcquireAsync(key, leaseTtl, cancellationToken);
        }
    }



    /// <summary>
    /// 保存多个测试存储节点共享的快照、租约和命名空间版本。
    /// </summary>
    private sealed class SharedDistributedState
    {
        /// <summary>
        /// 获取同步多个测试节点共享状态访问的锁对象。
        /// </summary>
        public object Gate { get; } = new object();
        /// <summary>
        /// 获取保存共享响应快照及过期时间的字典。
        /// </summary>
        public Dictionary<string, SharedEntry> Entries { get; } = new Dictionary<string, SharedEntry>(StringComparer.Ordinal);
        /// <summary>
        /// 获取保存共享租约持有者及过期时间的字典。
        /// </summary>
        public Dictionary<string, SharedLeaseEntry> Leases { get; } = new Dictionary<string, SharedLeaseEntry>(StringComparer.Ordinal);
        /// <summary>
        /// 获取记录各命名空间当前版本号的字典。
        /// </summary>
        public Dictionary<string, long> NamespaceVersions { get; } = new Dictionary<string, long>(StringComparer.Ordinal);
        /// <summary>
        /// 记录共享存储使用的递增版本号。
        /// </summary>
        public long Version;
    }



    /// <summary>
    /// 基于共享状态实现响应快照、租约和命名空间版本存取。
    /// </summary>
    private sealed class SharedNodeStore : IDistributedHttpResponseStore, IDistributedHttpResponseLeaseStore,
        IDistributedHttpResponseNamespaceStore
    {
        /// <summary>
        /// 保存当前存储节点访问的共享快照、租约及版本状态。
        /// </summary>
        private readonly SharedDistributedState _state;



        /// <summary>
        /// 初始化一个 <see cref="SharedNodeStore"/> 类型的实例。
        /// </summary>
        /// <param name="state">共享的分布式存储状态。</param>
        public SharedNodeStore(SharedDistributedState state) => _state = state;

        /// <inheritdoc />
        public Task<string> GetNamespaceVersionAsync(string namespaceName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_state.Gate)
            {
                return Task.FromResult(_state.NamespaceVersions.TryGetValue(namespaceName, out var version)
                    ? version.ToString()
                    : "0");
            }
        }

        /// <inheritdoc />
        public Task<string> InvalidateNamespaceAsync(string namespaceName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_state.Gate)
            {
                _state.NamespaceVersions.TryGetValue(namespaceName, out var version);
                version++;
                _state.NamespaceVersions[namespaceName] = version;
                return Task.FromResult(version.ToString());
            }
        }

        /// <inheritdoc />
        public Task<int> CleanupNamespaceAsync(string namespaceName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_state.Gate)
            {
                var currentVersion = _state.NamespaceVersions.TryGetValue(namespaceName, out var version)
                    ? version.ToString()
                    : "0";
                var currentPrefix = namespaceName + ":" + currentVersion + ":";
                var namespacePrefix = namespaceName + ":";
                var oldKeys = _state.Entries.Keys
                    .Where(key => key.StartsWith(namespacePrefix, StringComparison.Ordinal) &&
                        !key.StartsWith(currentPrefix, StringComparison.Ordinal))
                    .ToArray();
                foreach (var key in oldKeys) _state.Entries.Remove(key);
                return Task.FromResult(oldKeys.Length);
            }
        }

        /// <inheritdoc />
        public Task<DistributedHttpResponseSnapshot> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_state.Gate)
            {
                if (!_state.Entries.TryGetValue(key, out var entry)) return Task.FromResult<DistributedHttpResponseSnapshot>(null);
                if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    _state.Entries.Remove(key);
                    return Task.FromResult<DistributedHttpResponseSnapshot>(null);
                }
                return Task.FromResult(Clone(entry.Snapshot));
            }
        }

        /// <inheritdoc />
        public Task<DistributedHttpResponseSnapshot> SetAsync(string key, DistributedHttpResponseSnapshot snapshot,
            TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_state.Gate)
            {
                var stored = CloneWithVersion(snapshot, (++_state.Version).ToString());
                _state.Entries[key] = new SharedEntry(stored, DateTimeOffset.UtcNow + ttl);
                return Task.FromResult(Clone(stored));
            }
        }

        /// <inheritdoc />
        public Task<bool> CompareAndSetAsync(string key, string expectedVersion,
            DistributedHttpResponseSnapshot snapshot, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_state.Gate)
            {
                var current = GetCurrent(key);
                if (!string.Equals(current?.Version, expectedVersion, StringComparison.Ordinal))
                    return Task.FromResult(false);
                var stored = CloneWithVersion(snapshot, (++_state.Version).ToString());
                _state.Entries[key] = new SharedEntry(stored, DateTimeOffset.UtcNow + ttl);
                return Task.FromResult(true);
            }
        }

        /// <inheritdoc />
        public Task<bool> RemoveAsync(string key, string expectedVersion = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_state.Gate)
            {
                var current = GetCurrent(key);
                if (current == null || expectedVersion != null && !string.Equals(current.Version, expectedVersion, StringComparison.Ordinal))
                    return Task.FromResult(false);
                return Task.FromResult(_state.Entries.Remove(key));
            }
        }

        /// <inheritdoc />
        public Task<IDistributedHttpResponseLease> TryAcquireAsync(string key, TimeSpan leaseTtl,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_state.Gate)
            {
                if (_state.Leases.TryGetValue(key, out var current) && current.ExpiresAt > DateTimeOffset.UtcNow)
                    return Task.FromResult<IDistributedHttpResponseLease>(null);
                var token = Guid.NewGuid().ToString("N");
                _state.Leases[key] = new SharedLeaseEntry(token, DateTimeOffset.UtcNow + leaseTtl);
                return Task.FromResult<IDistributedHttpResponseLease>(new SharedLease(_state, key, token));
            }
        }



        /// <summary>
        /// 读取测试存储中当前有效的快照。
        /// </summary>
        /// <param name="key">待查询的共享存储键。</param>
        /// <returns>当前快照；条目不存在或已过期时返回 null。</returns>
        /// <remarks>发现过期条目时将其从共享字典移除；调用方应持有共享状态锁。</remarks>
        private DistributedHttpResponseSnapshot GetCurrent(string key)
        {
            if (!_state.Entries.TryGetValue(key, out var entry)) return null;
            if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                _state.Entries.Remove(key);
                return null;
            }
            return entry.Snapshot;
        }



        /// <summary>
        /// 复制测试使用的响应快照。
        /// </summary>
        /// <param name="snapshot">要复制的源响应快照。</param>
        /// <returns>保留源版本号的独立快照。</returns>
        private static DistributedHttpResponseSnapshot Clone(DistributedHttpResponseSnapshot snapshot)
            => CloneWithVersion(snapshot, snapshot.Version);



        /// <summary>
        /// 复制测试使用的响应快照。
        /// </summary>
        /// <param name="snapshot">要复制的源响应快照。</param>
        /// <param name="version">赋予副本的版本标识。</param>
        /// <returns>包含指定版本号的独立快照。</returns>
        private static DistributedHttpResponseSnapshot CloneWithVersion(DistributedHttpResponseSnapshot snapshot, string version)
            => new DistributedHttpResponseSnapshot(snapshot.StatusCode, snapshot.Headers,
                snapshot.ContentHeaders, snapshot.Body, snapshot.RawContent, version);
    }



    /// <summary>
    /// 保存响应快照及其过期时间的记录。
    /// </summary>
    private sealed class SharedEntry
    {
        /// <summary>
        /// 初始化一个 <see cref="SharedEntry"/> 类型的实例。
        /// </summary>
        /// <param name="snapshot">要存储的 HTTP 响应快照。</param>
        /// <param name="expiresAt">条目或租约的过期时间。</param>
        public SharedEntry(DistributedHttpResponseSnapshot snapshot, DateTimeOffset expiresAt)
        { Snapshot = snapshot; ExpiresAt = expiresAt; }
        /// <summary>
        /// 获取当前缓存条目保存的响应快照。
        /// </summary>
        public DistributedHttpResponseSnapshot Snapshot { get; }
        /// <summary>
        /// 获取当前缓存条目的过期时刻。
        /// </summary>
        public DateTimeOffset ExpiresAt { get; }
    }



    /// <summary>
    /// 保存租约持有者标识及过期时间的记录。
    /// </summary>
    private sealed class SharedLeaseEntry
    {
        /// <summary>
        /// 初始化一个 <see cref="SharedLeaseEntry"/> 类型的实例。
        /// </summary>
        /// <param name="token">租约持有者标识。</param>
        /// <param name="expiresAt">条目或租约的过期时间。</param>
        public SharedLeaseEntry(string token, DateTimeOffset expiresAt)
        { Token = token; ExpiresAt = expiresAt; }
        /// <summary>
        /// 获取用于区分当前租约持有者的标识。
        /// </summary>
        public string Token { get; }
        /// <summary>
        /// 获取当前租约的过期时刻。
        /// </summary>
        public DateTimeOffset ExpiresAt { get; }
    }



    /// <summary>
    /// 释放共享状态中由当前持有者登记的租约。
    /// </summary>
    private sealed class SharedLease : IDistributedHttpResponseLease
    {
        /// <summary>
        /// 保存释放租约时需要校验及修改的共享状态。
        /// </summary>
        private readonly SharedDistributedState _state;
        /// <summary>
        /// 标识当前租约在共享租约字典中的存储键。
        /// </summary>
        private readonly string _key;
        /// <summary>
        /// 标识当前持有者，释放时用于防止删除后续持有者的租约。
        /// </summary>
        private readonly string _token;



        /// <summary>
        /// 初始化一个 <see cref="SharedLease"/> 类型的实例。
        /// </summary>
        /// <param name="state">共享的分布式存储状态。</param>
        /// <param name="key">租约对应的存储键。</param>
        /// <param name="token">租约持有者标识。</param>
        public SharedLease(SharedDistributedState state, string key, string token)
        { _state = state; _key = key; _token = token; }

        /// <inheritdoc />
        public Task ReleaseAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_state.Gate)
            {
                if (_state.Leases.TryGetValue(_key, out var current) &&
                    string.Equals(current.Token, _token, StringComparison.Ordinal))
                    _state.Leases.Remove(_key);
            }
            return Task.CompletedTask;
        }
    }
}
