using System.Net;
using System.Threading;
using Bing.Http.Caching;

namespace Bing.Http.Tests.Bing.Http;



/// <summary>
/// HTTP 响应缓存的显式启用、TTL 和并发一致性测试。
/// </summary>
public class HttpResponseCacheTest
{
    /// <summary>
    /// 验证保留响应快照直到生存时间到期。
    /// </summary>
    [Fact]
    public async Task SetAndGet_PreservesResponseSnapshotUntilTtlExpires()
    {
        var cache = new InMemoryHttpResponseCache<string>();
        var response = Response("value");

        await cache.SetAsync("orders:1", response, TimeSpan.FromMilliseconds(50));
        (await cache.GetAsync("orders:1")).ShouldBeSameAs(response);
        await Task.Delay(100);
        (await cache.GetAsync("orders:1")).ShouldBeNull();

        var statistics = ((IHttpResponseCacheStatistics)cache).GetStatistics();
        statistics.Hits.ShouldBe(1);
        statistics.Misses.ShouldBe(1);
    }



    /// <summary>
    /// 验证并发调用方共用同一个进行中的回源操作。
    /// </summary>
    [Fact]
    public async Task GetOrCreate_SharesOneInFlightFactoryForConcurrentCallers()
    {
        var cache = new InMemoryHttpResponseCache<string>();
        var calls = 0;
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var requests = Enumerable.Range(0, 8).Select(_ => cache.GetOrCreateAsync(
            "orders:1", TimeSpan.FromMinutes(1), async () =>
            {
                Interlocked.Increment(ref calls);
                await gate.Task;
                return Response("value");
            })).ToArray();
        gate.SetResult(true);

        var responses = await Task.WhenAll(requests);
        calls.ShouldBe(1);
        responses.ShouldAllBe(item => item.Data == "value");
        (await cache.GetAsync("orders:1")).ShouldBeSameAs(responses[0]);
    }



    /// <summary>
    /// 验证认证请求需要显式启用缓存。
    /// </summary>
    [Fact]
    public async Task AuthenticatedRequestRequiresExplicitOptIn()
    {
        var cache = new InMemoryHttpResponseCache<string>();

        await Should.ThrowAsync<InvalidOperationException>(() => cache.GetOrCreateAsync(
            "private:1", TimeSpan.FromMinutes(1), () => Task.FromResult(Response("private")),
            requestContainsAuthorization: true));

        var response = await cache.GetOrCreateAsync(
            "private:1", TimeSpan.FromMinutes(1), () => Task.FromResult(Response("private")),
            requestContainsAuthorization: true, allowAuthenticated: true);
        response.Data.ShouldBe("private");
    }



    /// <summary>
    /// 验证无效键并生存时间失败之前缓存。
    /// </summary>
    [Fact]
    public async Task InvalidKeyAndTtlFailBeforeCaching()
    {
        var cache = new InMemoryHttpResponseCache<string>();
        await Should.ThrowAsync<ArgumentException>(() => cache.GetAsync(" "));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            cache.SetAsync("orders:1", Response("value"), TimeSpan.Zero));
    }



    /// <summary>
    /// 验证容量不足时先淘汰过期项，再淘汰最近最少使用项。
    /// </summary>
    [Fact]
    public async Task CapacityEvictsExpiredThenLeastRecentlyUsedEntry()
    {
        var cache = new InMemoryHttpResponseCache<string>(new HttpResponseCacheOptions { MaxEntries = 2 });
        await cache.SetAsync("expired", Response("expired"), TimeSpan.FromMilliseconds(10));
        await Task.Delay(30);
        await cache.SetAsync("old", Response("old"), TimeSpan.FromMinutes(1));
        await cache.SetAsync("new", Response("new"), TimeSpan.FromMinutes(1));
        await cache.SetAsync("latest", Response("latest"), TimeSpan.FromMinutes(1));

        (await cache.GetAsync("expired")).ShouldBeNull();
        (await cache.GetAsync("old")).ShouldBeNull();
        (await cache.GetAsync("new")).ShouldNotBeNull();
        (await cache.GetAsync("latest")).ShouldNotBeNull();
    }



    /// <summary>
    /// 验证缓存容量必须为正数。
    /// </summary>
    [Fact]
    public void CapacityMustBePositive()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new InMemoryHttpResponseCache<string>(new HttpResponseCacheOptions { MaxEntries = 0 }));
    }



    /// <summary>
    /// 验证字节容量不足时淘汰旧缓存项并拒绝过大的缓存项。
    /// </summary>
    [Fact]
    public async Task ByteCapacityEvictsOldEntriesAndRejectsOversizedEntry()
    {
        var cache = new InMemoryHttpResponseCache<string>(new HttpResponseCacheOptions
        {
            MaxBytes = 5,
            SizeEstimator = value => (value as string)?.Length ?? 0
        });
        await cache.SetAsync("a", Response("abc"), TimeSpan.FromMinutes(1));
        await cache.SetAsync("b", Response("def"), TimeSpan.FromMinutes(1));
        (await cache.GetAsync("a")).ShouldBeNull();
        (await cache.GetAsync("b")).ShouldNotBeNull();

        await cache.SetAsync("large", Response("123456"), TimeSpan.FromMinutes(1));
        (await cache.GetAsync("large")).ShouldBeNull();
    }



    /// <summary>
    /// 验证移除缓存项后可为后续项释放字节容量。
    /// </summary>
    [Fact]
    public async Task RemoveAsync_ReleasesByteCapacityForTheNextEntry()
    {
        var cache = new InMemoryHttpResponseCache<string>(new HttpResponseCacheOptions
        {
            MaxBytes = 5,
            SizeEstimator = value => (value as string)?.Length ?? 0
        });

        await cache.SetAsync("old", Response("12345"), TimeSpan.FromMinutes(1));
        await cache.RemoveAsync("old");
        await cache.SetAsync("new", Response("67890"), TimeSpan.FromMinutes(1));

        (await cache.GetAsync("old")).ShouldBeNull();
        (await cache.GetAsync("new")).ShouldNotBeNull();
    }



    /// <summary>
    /// 验证移除缓存项后重置已用字节容量。
    /// </summary>
    [Fact]
    public async Task ClearAsync_RemovesEntriesAndResetsByteCapacity()
    {
        var cache = new InMemoryHttpResponseCache<string>(new HttpResponseCacheOptions
        {
            MaxBytes = 5,
            SizeEstimator = value => (value as string)?.Length ?? 0
        });
        await cache.SetAsync("old", Response("12345"), TimeSpan.FromMinutes(1));

        (await cache.ClearAsync()).ShouldBe(1);
        await cache.SetAsync("new", Response("67890"), TimeSpan.FromMinutes(1));
        (await cache.GetAsync("new")).ShouldNotBeNull();
    }



    /// <summary>
    /// 验证是否安全期间写入方执行。
    /// </summary>
    [Fact]
    public async Task ClearAsync_IsSafeWhileWritersRun()
    {
        var cache = new InMemoryHttpResponseCache<string>();
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var writers = Enumerable.Range(0, 16).Select(index => Task.Run(async () =>
        {
            await start.Task;
            await cache.SetAsync("item:" + index, Response(index.ToString()), TimeSpan.FromMinutes(1));
        })).ToArray();
        var clear = Task.Run(async () =>
        {
            await start.Task;
            return await cache.ClearAsync();
        });
        start.SetResult(true);
        await Task.WhenAll(writers.Append(clear));
    }



    /// <summary>
    /// 验证是否不正在运行的回源操作不重复发布工厂。
    /// </summary>
    [Fact]
    public async Task ClearAsync_DoesNotRepublishAnAlreadyRunningFactory()
    {
        var cache = new InMemoryHttpResponseCache<string>();
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
        (await operation).Data.ShouldBe("stale");
        (await cache.GetAsync("epoch")).ShouldBeNull();
    }



    /// <summary>
    /// 验证按字节限制容量时要求有效的大小估算器。
    /// </summary>
    [Fact]
    public async Task ByteCapacityRequiresValidEstimator()
    {
        Should.Throw<ArgumentException>(() => new InMemoryHttpResponseCache<string>(
            new HttpResponseCacheOptions { MaxBytes = 10 }));
        var cache = new InMemoryHttpResponseCache<string>(new HttpResponseCacheOptions
        {
            MaxBytes = 10,
            SizeEstimator = _ => -1
        });
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            cache.SetAsync("negative", Response("value"), TimeSpan.FromMinutes(1)));
    }



    /// <summary>
    /// 验证移除按前缀使用序号匹配并返回数量。
    /// </summary>
    [Fact]
    public async Task RemoveByPrefixUsesOrdinalMatchingAndReturnsCount()
    {
        var cache = new InMemoryHttpResponseCache<string>();
        await cache.SetAsync("orders:1", Response("1"), TimeSpan.FromMinutes(1));
        await cache.SetAsync("orders:2", Response("2"), TimeSpan.FromMinutes(1));
        await cache.SetAsync("Orders:3", Response("3"), TimeSpan.FromMinutes(1));

        cache.RemoveByPrefix("orders:").ShouldBe(2);
        (await cache.GetAsync("orders:1")).ShouldBeNull();
        (await cache.GetAsync("Orders:3")).ShouldNotBeNull();
        Should.Throw<ArgumentException>(() => cache.RemoveByPrefix(""));
    }



    /// <summary>
    /// 验证移除按前缀是否安全期间其他任务读取并写入。
    /// </summary>
    [Fact]
    public async Task RemoveByPrefixIsSafeWhileOtherTasksReadAndWrite()
    {
        var cache = new InMemoryHttpResponseCache<string>();
        var operations = Enumerable.Range(0, 32).Select(index => Task.Run(async () =>
        {
            var key = "orders:" + index;
            await cache.SetAsync(key, Response(index.ToString()), TimeSpan.FromMinutes(1));
            if (index % 2 == 0) cache.RemoveByPrefix("orders:");
            await cache.GetAsync(key);
        }));

        await Task.WhenAll(operations);
        cache.RemoveByPrefix("orders:").ShouldBeGreaterThanOrEqualTo(0);
    }



    /// <summary>
    /// 创建包含固定实体标签的字符串响应样例。
    /// </summary>
    /// <param name="value">同时用作结果数据及原始正文的字符串。</param>
    /// <returns>实体标签为 <c>v1</c> 的成功响应快照。</returns>
    private static HttpResponseResult<string> Response(string value)
        => new HttpResponseResult<string>(HttpStatusCode.OK,
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["ETag"] = new[] { "\"v1\"" }
            },
            new Dictionary<string, IReadOnlyList<string>>(), value, value);
}
