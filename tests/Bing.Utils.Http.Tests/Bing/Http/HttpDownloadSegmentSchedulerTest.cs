using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using Bing.Http;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>

/// 分片调度器的并发、重试、取消和跳过测试。

/// </summary>
public class HttpDownloadSegmentSchedulerTest
{
    /// <summary>
    /// 验证遵循并发并完成乱序。
    /// </summary>
    [Fact]
    public async Task DownloadAsync_RespectsConcurrencyAndCompletesOutOfOrder()
    {
        var root = CreateDirectory();
        try
        {
            var store = new HttpDownloadSegmentFileStore(Path.Combine(root, "segments"), Path.Combine(root, "out.bin"));
            var state = State(8, 4);
            var current = 0;
            var peak = 0;
            var scheduler = new HttpDownloadSegmentScheduler(store, 2);
            var result = await scheduler.DownloadAsync(state, async (range, token) =>
            {
                var value = Interlocked.Increment(ref current);
                while (true)
                {
                    var old = Volatile.Read(ref peak);
                    if (value <= old || Interlocked.CompareExchange(ref peak, value, old) == old) break;
                }
                await Task.Delay(range.Start == 0 ? 50 : 10, token);
                Interlocked.Decrement(ref current);
                return new MemoryStream(Encoding.UTF8.GetBytes(new string((char)('A' + range.Start), (int)range.Length)));
            });
            result.Segments.All(segment => segment.Completed).ShouldBeTrue();
            peak.ShouldBe(2);
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证重试失败操作分段。

    /// </summary>
    [Fact]
    public async Task DownloadAsync_RetriesFailedSegment()
    {
        var root = CreateDirectory();
        try
        {
            var store = new HttpDownloadSegmentFileStore(Path.Combine(root, "segments"), Path.Combine(root, "out.bin"));
            var attempts = 0;
            var scheduler = new HttpDownloadSegmentScheduler(store, 1, 2);
            var result = await scheduler.DownloadAsync(State(4, 4), (range, token) =>
            {
                if (Interlocked.Increment(ref attempts) < 3) throw new IOException("transient");
                return Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3, 4 }));
            });
            attempts.ShouldBe(3);
            result.Segments.Single().Completed.ShouldBeTrue();
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证保持失败分段未完成。

    /// </summary>
    [Fact]
    public async Task DownloadAsync_LeavesFailedSegmentIncomplete()
    {
        var root = CreateDirectory();
        try
        {
            var store = new HttpDownloadSegmentFileStore(Path.Combine(root, "segments"), Path.Combine(root, "out.bin"));
            var scheduler = new HttpDownloadSegmentScheduler(store, 1, 1);
            var result = await scheduler.DownloadAsync(State(4, 4), (range, token) =>
                throw new IOException("permanent"));
            result.Segments.Single().Completed.ShouldBeFalse();
            File.Exists(Path.Combine(root, "out.bin")).ShouldBeFalse();
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证取消停止工作线程。

    /// </summary>
    [Fact]
    public async Task DownloadAsync_CancellationStopsWorkers()
    {
        var root = CreateDirectory();
        try
        {
            var store = new HttpDownloadSegmentFileStore(Path.Combine(root, "segments"), Path.Combine(root, "out.bin"));
            var scheduler = new HttpDownloadSegmentScheduler(store, 1);
            using (var cancellation = new CancellationTokenSource())
            {
                var operation = scheduler.DownloadAsync(State(4, 4), async (range, token) =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                    return new MemoryStream(new byte[] { 1, 2, 3, 4 });
                }, cancellation.Token);
                await Task.Delay(50);
                cancellation.Cancel();
                await Should.ThrowAsync<OperationCanceledException>(() => operation);
            }
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证跳过已完成分段。

    /// </summary>
    [Fact]
    public async Task DownloadAsync_SkipsCompletedSegments()
    {
        var root = CreateDirectory();
        try
        {
            var store = new HttpDownloadSegmentFileStore(Path.Combine(root, "segments"), Path.Combine(root, "out.bin"));
            var state = State(8, 4);
            state.Segments[0].BytesWritten = 4;
            state.Segments[0].Completed = true;
            var calls = 0;
            var result = await new HttpDownloadSegmentScheduler(store, 1).DownloadAsync(state, (range, token) =>
            {
                Interlocked.Increment(ref calls);
                return Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3, 4 }));
            });
            calls.ShouldBe(1);
            result.Segments[0].Completed.ShouldBeTrue();
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证报告完成字节一次并总数长度。

    /// </summary>
    [Fact]
    public async Task DownloadAsync_ReportsCompletedBytesOnceAndTotalLength()
    {
        var root = CreateDirectory();
        try
        {
            var progress = new List<HttpDownloadSegmentProgress>();
            var store = new HttpDownloadSegmentFileStore(Path.Combine(root, "segments"), Path.Combine(root, "out.bin"));
            var result = await new HttpDownloadSegmentScheduler(store, 2, progressObserver: progress.Add)
                .DownloadAsync(State(8, 4), (range, token) =>
                    Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3, 4 })));
            result.Segments.All(segment => segment.Completed).ShouldBeTrue();
            progress.Select(item => item.CompletedBytes).OrderBy(value => value).ShouldBe(new long[] { 4, 8 });
            progress.All(item => item.TotalBytes == 8).ShouldBeTrue();
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证报告仅最终失败之后重试。

    /// </summary>
    [Fact]
    public async Task DownloadAsync_ReportsOnlyFinalFailureAfterRetries()
    {
        var root = CreateDirectory();
        try
        {
            var failures = new List<HttpDownloadSegmentFailure>();
            var store = new HttpDownloadSegmentFileStore(Path.Combine(root, "segments"), Path.Combine(root, "out.bin"));
            var result = await new HttpDownloadSegmentScheduler(store, 1, 2,
                    failureObserver: failures.Add)
                .DownloadAsync(State(4, 4), (range, token) =>
                    throw new IOException("permanent"));
            result.Segments.Single().Completed.ShouldBeFalse();
            failures.Count.ShouldBe(1);
            failures.Single().Attempts.ShouldBe(3);
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证进度观察器失败传播未使用重试。

    /// </summary>
    [Fact]
    public async Task DownloadAsync_ProgressObserverFailurePropagatesWithoutRetry()
    {
        var root = CreateDirectory();
        try
        {
            var attempts = 0;
            var store = new HttpDownloadSegmentFileStore(Path.Combine(root, "segments"), Path.Combine(root, "out.bin"));
            await Should.ThrowAsync<InvalidOperationException>(() =>
                new HttpDownloadSegmentScheduler(store, 1, 3,
                        progressObserver: _ => throw new InvalidOperationException("observer"))
                    .DownloadAsync(State(4, 4), (range, token) =>
                    {
                        attempts++;
                        return Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3, 4 }));
                    }));
            attempts.ShouldBe(1);
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 处理测试场景中的 State。

    /// </summary>
    private static HttpDownloadSegmentsState State(long totalLength, long segmentSize)
        => new HttpDownloadSegmentsState
        {
            SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
            TotalLength = totalLength,
            Segments = HttpDownloadRangePlanner.Create(totalLength, segmentSize).Select(range =>
                new HttpDownloadSegmentState { Start = range.Start, End = range.End }).ToList()
        };

    /// <summary>

    /// 创建测试所需的辅助对象或数据。

    /// </summary>
    private static string CreateDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "bing-http-segment-scheduler-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>

    /// 清理测试生成的临时资源。

    /// </summary>
    private static void Delete(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
    }
}
