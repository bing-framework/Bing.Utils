using System.Threading;
using Bing.Http;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>

/// HTTP 分片状态格式、覆盖校验和原子保存测试。

/// </summary>
public class HttpDownloadSegmentStateTest
{
    /// <summary>
    /// 验证保留已校验的状态。
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

    /// 验证拒绝重叠缺失和越界范围。

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

    /// 验证拒绝后续版本并无效完成。

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

    /// 验证接受匹配范围并拒绝不同分段大小。

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

    /// 验证拒绝非正数分段大小。

    /// </summary>
    [Fact]
    public void ValidatePlan_RejectsNonPositiveSegmentSize()
    {
        var state = State(0);

        Should.Throw<ArgumentOutOfRangeException>(() => HttpDownloadSegmentStateStore.ValidatePlan(state, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => HttpDownloadSegmentStateStore.ValidatePlan(state, -1));
    }

    /// <summary>

    /// 验证取消保持现有状态未修改。

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

    /// 处理测试场景中的 State。

    /// </summary>
    private static HttpDownloadSegmentsState State(long totalLength, params HttpDownloadSegmentState[] segments)
        => new HttpDownloadSegmentsState
        {
            SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
            TotalLength = totalLength,
            Segments = segments.ToList()
        };

    /// <summary>

    /// 清理测试生成的临时资源。

    /// </summary>
    private static void Delete(string directory)
    {
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
