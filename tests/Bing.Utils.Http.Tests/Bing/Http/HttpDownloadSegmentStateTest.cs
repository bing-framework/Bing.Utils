using System.Threading;
using Bing.Http;

namespace Bing.Utils.Http.Tests.Bing.Http;



/// <summary>
/// HTTP 分片状态格式、覆盖校验和原子保存测试。
/// </summary>
public class HttpDownloadSegmentStateTest
{
    /// <summary>
    /// 验证状态保存及加载保留正文长度、分片数量和已写入字节数。
    /// </summary>
    [Fact]
    public async Task SaveAndLoad_PreservesValidatedState()
    {
        var path = Path.Combine(Path.GetTempPath(), "bing-http-segments-" + Guid.NewGuid().ToString("N"), "state.json");
        try
        {
            var state = State(10, new HttpDownloadSegmentState { Start = 0, End = 4, BytesWritten = 5, Completed = true },
                new HttpDownloadSegmentState { Start = 5, End = 9, BytesWritten = 2 });
            await HttpDownloadSegmentStateStore.SaveAsync(path, state);
            var loaded = HttpDownloadSegmentStateStore.Load(path);
            loaded.TotalLength.ShouldBe(10);
            loaded.Segments.Count.ShouldBe(2);
            loaded.Segments[1].BytesWritten.ShouldBe(2);
        }
        finally { Delete(Path.GetDirectoryName(path)); }
    }



    /// <summary>
    /// 验证状态校验拒绝范围重叠、覆盖缺口及越界分片。
    /// </summary>
    [Fact]
    public void Validate_RejectsOverlapMissingAndOutOfBoundsRanges()
    {
        Should.Throw<InvalidDataException>(() => HttpDownloadSegmentStateStore.Validate(State(10,
            new HttpDownloadSegmentState { Start = 0, End = 5 },
            new HttpDownloadSegmentState { Start = 5, End = 9 })));
        Should.Throw<InvalidDataException>(() => HttpDownloadSegmentStateStore.Validate(State(10,
            new HttpDownloadSegmentState { Start = 0, End = 3 },
            new HttpDownloadSegmentState { Start = 5, End = 9 })));
        Should.Throw<InvalidDataException>(() => HttpDownloadSegmentStateStore.Validate(State(10,
            new HttpDownloadSegmentState { Start = 0, End = 10 })));
    }



    /// <summary>
    /// 验证状态校验拒绝未来格式版本及写入长度不足的完成标记。
    /// </summary>
    [Fact]
    public void Validate_RejectsFutureVersionAndInvalidCompletion()
    {
        var future = State(1, new HttpDownloadSegmentState { Start = 0, End = 0 });
        future.SchemaVersion = 2;
        Should.Throw<NotSupportedException>(() => HttpDownloadSegmentStateStore.Validate(future));

        var incomplete = State(1, new HttpDownloadSegmentState { Start = 0, End = 0, Completed = true });
        Should.Throw<InvalidDataException>(() => HttpDownloadSegmentStateStore.Validate(incomplete));
    }



    /// <summary>
    /// 验证分片计划校验接受匹配范围并拒绝不同分片大小。
    /// </summary>
    [Fact]
    public void ValidatePlan_AcceptsMatchingRangesAndRejectsDifferentSegmentSize()
    {
        var matching = State(10,
            new HttpDownloadSegmentState { Start = 0, End = 3 },
            new HttpDownloadSegmentState { Start = 4, End = 7 },
            new HttpDownloadSegmentState { Start = 8, End = 9 });
        HttpDownloadSegmentStateStore.ValidatePlan(matching, 4);

        Should.Throw<InvalidDataException>(() => HttpDownloadSegmentStateStore.ValidatePlan(matching, 3));
    }



    /// <summary>
    /// 验证分片计划校验拒绝零或负分片大小。
    /// </summary>
    [Fact]
    public void ValidatePlan_RejectsNonPositiveSegmentSize()
    {
        var state = State(0);

        Should.Throw<ArgumentOutOfRangeException>(() => HttpDownloadSegmentStateStore.ValidatePlan(state, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => HttpDownloadSegmentStateStore.ValidatePlan(state, -1));
    }



    /// <summary>
    /// 验证取消保存不会改写已有下载状态。
    /// </summary>
    [Fact]
    public async Task SaveAsync_CancellationLeavesExistingStateUntouched()
    {
        var path = Path.Combine(Path.GetTempPath(), "bing-http-segments-" + Guid.NewGuid().ToString("N"), "state.json");
        try
        {
            await HttpDownloadSegmentStateStore.SaveAsync(path, State(1,
                new HttpDownloadSegmentState { Start = 0, End = 0, BytesWritten = 1, Completed = true }));
            var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Should.ThrowAsync<OperationCanceledException>(() =>
                HttpDownloadSegmentStateStore.SaveAsync(path, State(1,
                    new HttpDownloadSegmentState { Start = 0, End = 0 }), cancellation.Token));
            HttpDownloadSegmentStateStore.Load(path).Segments[0].Completed.ShouldBeTrue();
        }
        finally { Delete(Path.GetDirectoryName(path)); }
    }



    /// <summary>
    /// 创建包含指定分片的下载状态样例。
    /// </summary>
    /// <param name="totalLength">待下载正文的总字节数。</param>
    /// <param name="segments">按范围组织的分片状态样例。</param>
    /// <returns>使用当前格式版本并包含指定分片的下载状态。</returns>
    private static HttpDownloadSegmentsState State(long totalLength, params HttpDownloadSegmentState[] segments)
        => new HttpDownloadSegmentsState
        {
            SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
            TotalLength = totalLength,
            Segments = segments.ToList()
        };



    /// <summary>
    /// 递归删除状态文件测试创建的临时目录。
    /// </summary>
    /// <param name="directory">待清理的目录；为 null、空字符串或不存在时不执行操作。</param>
    private static void Delete(string directory)
    {
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
