using System.Threading;
using Bing.Http;
using Bing.Http.Caching;

namespace Bing.Utils.Http.Tests.Bing.Http;



/// <summary>
/// 文件缓存后台维护器的周期、停止和异常隔离测试。
/// </summary>
[Collection("TimingSensitive")]
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
    /// 验证维护器报告清理异常后仍继续后续清理。
    /// </summary>
    [Fact]
    public async Task CleanupFailure_IsReportedAndDoesNotStopLoop()
    {
        var calls = 0;
        var errorReported = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var nextCleanupStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var maintenance = new FileHttpResponseCacheMaintenance(_ =>
        {
            if (Interlocked.Increment(ref calls) > 1) nextCleanupStarted.TrySetResult(true);
            throw new IOException("injected cleanup failure");
        }, TimeSpan.FromMilliseconds(20), error => errorReported.TrySetResult(error)))
        {
            maintenance.Start();
            var observed = Task.WhenAll(errorReported.Task, nextCleanupStarted.Task);
            var completed = await Task.WhenAny(observed, Task.Delay(TimeSpan.FromSeconds(3)));
            completed.ShouldBe(observed);
            var error = await errorReported.Task;
            (await nextCleanupStarted.Task).ShouldBeTrue();
            Assert.IsType<IOException>(error);
            Assert.Equal("injected cleanup failure", error.Message);
            calls.ShouldBeGreaterThan(1);
            maintenance.IsRunning.ShouldBeTrue();
        }
    }



    /// <summary>
    /// 创建包含固定正文的成功缓存响应样例。
    /// </summary>
    /// <returns>正文为单字节且原始文本为 <c>value</c> 的成功响应。</returns>
    private static HttpResponseResult<byte[]> Response()
        => new HttpResponseResult<byte[]>(System.Net.HttpStatusCode.OK,
            new Dictionary<string, IReadOnlyList<string>>(), new Dictionary<string, IReadOnlyList<string>>(),
            new byte[] { 1 }, "value");



    /// <summary>
    /// 异步等待测试条件在限定时间内满足。
    /// </summary>
    /// <param name="condition">异步检测条件的委托，返回 true 时结束等待。</param>
    /// <exception cref="TimeoutException">三秒内条件仍未满足。</exception>
    /// <remarks>每次未满足条件后等待二十毫秒。</remarks>
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
    /// 异步等待测试条件在限定时间内满足。
    /// </summary>
    /// <param name="condition">同步检测条件的委托，返回 true 时结束等待。</param>
    /// <exception cref="TimeoutException">三秒内条件仍未满足。</exception>
    private static async Task EventuallyAsync(Func<bool> condition)
        => await EventuallyAsync(() => Task.FromResult(condition()));



    /// <summary>
    /// 创建用于文件缓存后台维护测试的独立临时目录。
    /// </summary>
    /// <returns>已创建的唯一临时目录完整路径。</returns>
    private static string CreateDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "bing-http-maintenance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }



    /// <summary>
    /// 递归删除测试创建的临时目录。
    /// </summary>
    /// <param name="path">待清理的目录完整路径；目录不存在时不执行操作。</param>
    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
    }
}
