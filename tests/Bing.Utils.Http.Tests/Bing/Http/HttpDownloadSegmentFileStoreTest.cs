using System.Text;
using System.Threading;
using Bing.Http;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>

/// HTTP 分片临时文件写入、校验和原子合并测试。

/// </summary>
public class HttpDownloadSegmentFileStoreTest
{
    /// <summary>
    /// 验证生成有序目标文件。
    /// </summary>
    [Fact]
    public async Task WriteOutOfOrderAndMerge_ProducesOrderedTarget()
    {
        var root = CreateDirectory();
        try
        {
            var store = new HttpDownloadSegmentFileStore(Path.Combine(root, "segments"), Path.Combine(root, "out.bin"));
            var second = await Write(store, new HttpDownloadRange(4, 7), "EFGH");
            var first = await Write(store, new HttpDownloadRange(0, 3), "ABCD");
            await store.MergeAsync(State(8, first, second));
            File.ReadAllText(Path.Combine(root, "out.bin")).ShouldBe("ABCDEFGH");
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证写入拒绝较短并较长分段。

    /// </summary>
    [Fact]
    public async Task WriteRejectsShortAndLongSegments()
    {
        var root = CreateDirectory();
        try
        {
            var store = new HttpDownloadSegmentFileStore(root, Path.Combine(root, "out.bin"));
            await Should.ThrowAsync<InvalidDataException>(() => Write(store, new HttpDownloadRange(0, 3), "abc"));
            await Should.ThrowAsync<InvalidDataException>(() => Write(store, new HttpDownloadRange(0, 3), "abcde"));
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证合并拒绝缺少分段并保留现有目标。

    /// </summary>
    [Fact]
    public async Task MergeRejectsMissingSegmentAndKeepsExistingTarget()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "out.bin");
            File.WriteAllText(target, "old");
            var store = new HttpDownloadSegmentFileStore(Path.Combine(root, "segments"), target);
            var first = await Write(store, new HttpDownloadRange(0, 3), "ABCD");
            await Should.ThrowAsync<InvalidDataException>(() => store.MergeAsync(State(8, first,
                new HttpDownloadSegmentState { Start = 4, End = 7, BytesWritten = 4, Completed = true })));
            File.ReadAllText(target).ShouldBe("old");
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证合并取消保留现有目标并分段文件。

    /// </summary>
    [Fact]
    public async Task MergeCancellationKeepsExistingTargetAndSegmentFiles()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "out.bin");
            File.WriteAllText(target, "old");
            var store = new HttpDownloadSegmentFileStore(Path.Combine(root, "segments"), target);
            var first = await Write(store, new HttpDownloadRange(0, 3), "ABCD");
            var second = await Write(store, new HttpDownloadRange(4, 7), "EFGH");
            var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Should.ThrowAsync<OperationCanceledException>(() => store.MergeAsync(State(8, first, second), cancellation.Token));
            File.ReadAllText(target).ShouldBe("old");
            Directory.GetFiles(Path.Combine(root, "segments"), "*.part").Length.ShouldBe(2);
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task Reconcile_DowngradesOnlyInvalidCompletedSegmentsAndRemovesOrphans()
    {
        var root = CreateDirectory();
        try
        {
            var staging = Path.Combine(root, "segments");
            var store = new HttpDownloadSegmentFileStore(staging, Path.Combine(root, "out.bin"));
            var first = await Write(store, new HttpDownloadRange(0, 3), "ABCD");
            var second = await Write(store, new HttpDownloadRange(4, 7), "EFGH");
            File.WriteAllText(Path.Combine(staging, "segment-4-7.part"), "xxxx", Encoding.UTF8);
            File.WriteAllText(Path.Combine(staging, "orphan.part"), "orphan", Encoding.UTF8);
            File.WriteAllText(Path.Combine(staging, "stale.tmp"), "temp", Encoding.UTF8);
            var state = State(12, first, second,
                new HttpDownloadSegmentState { Start = 8, End = 11, BytesWritten = 4, Completed = true, Sha256 = new string('a', 64) });

            var invalidated = store.Reconcile(state);

            invalidated.ShouldBe(2);
            state.Segments[0].Completed.ShouldBeTrue();
            state.Segments[1].Completed.ShouldBeFalse();
            state.Segments[2].Completed.ShouldBeFalse();
            state.Segments[2].BytesWritten.ShouldBe(0);
            File.Exists(Path.Combine(staging, "orphan.part")).ShouldBeFalse();
            File.Exists(Path.Combine(staging, "stale.tmp")).ShouldBeFalse();
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 写入测试所需的数据。

    /// </summary>
    private static async Task<HttpDownloadSegmentState> Write(HttpDownloadSegmentFileStore store,
        HttpDownloadRange range, string text)
    {
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            return await store.WriteSegmentAsync(range, stream);
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

    /// 创建测试所需的辅助对象或数据。

    /// </summary>
    private static string CreateDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "bing-http-segment-files-" + Guid.NewGuid().ToString("N"));
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
