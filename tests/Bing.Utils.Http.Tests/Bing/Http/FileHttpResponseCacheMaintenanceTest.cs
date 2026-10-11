using Bing.Http;
using Bing.Http.Caching;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>

/// 文件缓存后台维护器的周期、停止和异常隔离测试。

/// </summary>
public class FileHttpResponseCacheMaintenanceTest
{
    /// <summary>
    /// 验证定期清理过期缓存项。
    /// </summary>
    [Fact]
    public async Task Start_PeriodicallyCleansExpiredEntries()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            await cache.SetAsync("expired", Response(), TimeSpan.FromMilliseconds(1));
            using (var maintenance = new FileHttpResponseCacheMaintenance(cache, TimeSpan.FromMilliseconds(20), TimeSpan.Zero))
            {
                maintenance.Start();
                await EventuallyAsync(async () => (await cache.GetAsync("expired")) == null);
                maintenance.IsRunning.ShouldBeTrue();
            }
        }
        finally { DeleteDirectory(directory); }
    }

    /// <summary>

    /// 验证维护器可重复停止且停止后不再执行清理。

    /// </summary>
    [Fact]
    public void StopAndDispose_AreIdempotentAndPreventFurtherCleanup()
    {
        var directory = CreateDirectory();
        try
        {
            var cache = new FileHttpResponseCache(directory);
            var maintenance = new FileHttpResponseCacheMaintenance(cache, TimeSpan.FromMilliseconds(20), TimeSpan.Zero);
            try
            {
                maintenance.Start();
                maintenance.Stop();
                maintenance.Stop();
                maintenance.IsRunning.ShouldBeFalse();
            }
            finally { maintenance.Dispose(); }
            Should.Throw<ObjectDisposedException>(() => maintenance.Start());
        }
        finally { DeleteDirectory(directory); }
    }

    /// <summary>

    /// 验证是否报告并是否不停止循环。

    /// </summary>
    [Fact]
    public async Task CleanupFailure_IsReportedAndDoesNotStopLoop()
    {
        var errors = new List<Exception>();
        var calls = 0;
        using (var maintenance = new FileHttpResponseCacheMaintenance(_ =>
        {
            calls++;
            throw new IOException("injected cleanup failure");
        }, TimeSpan.FromMilliseconds(20), errors.Add))
        {
            maintenance.Start();
            await EventuallyAsync(() => errors.Count > 0 && calls > 1);
            maintenance.IsRunning.ShouldBeTrue();
        }
    }

    /// <summary>

    /// 创建测试用 HTTP 响应。

    /// </summary>
    private static HttpResponseResult<byte[]> Response()
        => new HttpResponseResult<byte[]>(System.Net.HttpStatusCode.OK,
            new Dictionary<string, IReadOnlyList<string>>(), new Dictionary<string, IReadOnlyList<string>>(),
            new byte[] { 1 }, "value");

    /// <summary>

    /// 处理测试场景中的 EventuallyAsync。

    /// </summary>
    private static async Task EventuallyAsync(Func<Task<bool>> condition)
    {
        var timeout = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < timeout)
        {
            if (await condition()) return;
            await Task.Delay(20);
        }
        throw new TimeoutException("Condition was not met.");
    }

    /// <summary>

    /// 处理测试场景中的 EventuallyAsync。

    /// </summary>
    private static async Task EventuallyAsync(Func<bool> condition)
        => await EventuallyAsync(() => Task.FromResult(condition()));

    /// <summary>

    /// 创建测试所需的辅助对象或数据。

    /// </summary>
    private static string CreateDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "bing-http-maintenance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>

    /// 清理测试生成的临时资源。

    /// </summary>
    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
    }
}
