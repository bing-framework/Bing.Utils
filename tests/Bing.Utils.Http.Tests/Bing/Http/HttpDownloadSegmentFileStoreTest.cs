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
    /// 验证乱序写入的分片按字节范围顺序合并为完整正文。
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
    /// 验证分片写入拒绝短于或长于预期范围的正文。
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
    /// 验证缺少分片文件时合并失败且保留现有目标文件。
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
    /// 验证取消合并保留现有目标文件及全部分片文件。
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

    /// <summary>
    /// 验证状态对账仅撤销无效已完成分片，并删除孤立文件及临时文件。
    /// </summary>
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
    /// 异步将 UTF-8 文本写入指定范围的分片文件。
    /// </summary>
    /// <param name="store">接收分片数据的文件存储。</param>
    /// <param name="range">要写入的分片字节范围。</param>
    /// <param name="text">编码为 UTF-8 后写入分片的文本。</param>
    /// <returns>包含写入字节数及校验结果的分片状态。</returns>
    private static async Task<HttpDownloadSegmentState> Write(HttpDownloadSegmentFileStore store,
        HttpDownloadRange range, string text)
    {
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            return await store.WriteSegmentAsync(range, stream);
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
    /// 创建用于分片写入与合并测试的独立临时目录。
    /// </summary>
    /// <returns>已创建的唯一临时目录完整路径。</returns>
    private static string CreateDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "bing-http-segment-files-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }



    /// <summary>
    /// 递归删除测试创建的临时目录。
    /// </summary>
    /// <param name="path">待清理的目录完整路径；目录不存在时不执行操作。</param>
    private static void Delete(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
    }
}
