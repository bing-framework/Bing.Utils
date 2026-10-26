using System.Text;
using Bing.Http;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>
/// 验证分片合并提交和检查点快照的回归边界。
/// </summary>
public class SegmentRefinementTest
{
    /// <summary>
    /// 后续分片校验失败时，已写入临时目标的数据不能替换原文件。
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LaterCorruptionKeepsTargetAndCleansTemporaryFile(bool truncate)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var staging = Path.Combine(root, "parts");
        var target = Path.Combine(root, "target");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(target, "original", Encoding.UTF8);
            var store = new HttpDownloadSegmentFileStore(staging, target);
            var state = CreateState();
            for (var i = 0; i < 2; i++)
                state.Segments[i] = await store.WriteSegmentAsync(state.Segments[i].Range, new MemoryStream(new byte[] { 1, 2 }));
            File.WriteAllBytes(Path.Combine(staging, "segment-2-3.part"), truncate ? new byte[] { 9 } : new byte[] { 9, 9 });
            await Assert.ThrowsAsync<InvalidDataException>(() => store.MergeAsync(state));
            Assert.Equal("original", File.ReadAllText(target, Encoding.UTF8));
            Assert.Empty(Directory.GetFiles(root, "*.tmp"));
            Assert.Equal(2, Directory.GetFiles(staging, "*.part").Length);
        }
        finally { Directory.Delete(root, true); }
    }

    /// <summary>
    /// 检查点持有或修改快照不会影响调度中的状态，失败分片后仍会继续领取。
    /// </summary>
    [Fact]
    public async Task CheckpointSnapshotsAreIndependentAndFailuresDoNotStopScheduling()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var state = CreateState();
            var snapshots = new List<HttpDownloadSegmentsState>();
            var scheduler = new HttpDownloadSegmentScheduler(new HttpDownloadSegmentFileStore(root, Path.Combine(root, "out")), 1);
            var result = await scheduler.DownloadAsync(state, (range, _) => range.Start == 0
                ? Task.FromException<Stream>(new IOException("failed"))
                : Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2 })), checkpoint: (snapshot, _) =>
                {
                    snapshots.Add(snapshot);
                    snapshot.Segments[1].Completed = false;
                    return Task.CompletedTask;
                });
            Assert.False(result.Segments[0].Completed);
            Assert.True(result.Segments[1].Completed);
            Assert.All(state.Segments, segment => Assert.False(segment.Completed));
            Assert.Single(snapshots);
        }
        finally { Directory.Delete(root, true); }
    }

    /// <summary>
    /// 创建两个连续分片的独立状态。
    /// </summary>
    private static HttpDownloadSegmentsState CreateState() => new()
    {
        SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
        TotalLength = 4,
        Segments = new List<HttpDownloadSegmentState>
        {
            new() { Start = 0, End = 1 }, new() { Start = 2, End = 3 }
        }
    };
}
