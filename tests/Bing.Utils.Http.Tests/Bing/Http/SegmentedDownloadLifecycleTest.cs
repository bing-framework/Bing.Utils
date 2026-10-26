using System.Threading;
using Bing.Http;
using Moq;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>验证可选能力兼容、任务收尾及磁盘维护边界。</summary>
public class SegmentedDownloadLifecycleTest : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bing-lifecycle-" + Guid.NewGuid().ToString("N"));
    private string Target => Path.Combine(_root, "file.bin");
    public SegmentedDownloadLifecycleTest() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);

    [Fact]
    public async Task OptionalInterfaceDoesNotRequireLegacyImplementationChanges()
    {
        var request = new Mock<IHttpRequest<string>>().Object;
        await Assert.ThrowsAsync<NotSupportedException>(() => request.DownloadFileSegmentedAsync(Target, options: null, cancellationToken: default));
        Assert.Throws<NotSupportedException>(() => request.CreateSegmentedDownloadTask(Target));
        Assert.Throws<ArgumentNullException>(() => ((IHttpRequest<string>)null).CreateSegmentedDownloadTask(Target));
    }

    [Fact]
    public async Task PauseWaitsForCleanupAndResumeUsesSnapshot()
    {
        var entered = Signal(); var cancelObserved = Signal(); var release = Signal();
        var options = new HttpSegmentedDownloadOptions { SegmentSize = 8 };
        var calls = 0;
        var fake = new Capability(async (config, token) =>
        {
            Assert.Equal(8, config.SegmentSize);
            if (++calls == 2) return State(true);
            using var registration = token.Register(() => cancelObserved.TrySetResult(true));
            entered.TrySetResult(true); await release.Task; token.ThrowIfCancellationRequested(); return State(false);
        });
        await using var task = new HttpSegmentedDownloadTask<string>(fake, Target, options);
        options.SegmentSize = 1;
        var run = task.StartAsync(); await entered.Task;
        await Assert.ThrowsAsync<InvalidOperationException>(() => task.StartAsync());
        Assert.Throws<InvalidOperationException>(() => task.Discard());
        var pause = task.PauseAsync(); await cancelObserved.Task;
        Assert.Equal(HttpSegmentedDownloadTaskStatus.Pausing, task.Status); Assert.False(pause.IsCompleted);
        release.TrySetResult(true); await pause;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(HttpSegmentedDownloadTaskStatus.Paused, task.Status);
        await task.ResumeAsync(); Assert.Equal(HttpSegmentedDownloadTaskStatus.Completed, task.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => task.StartAsync());
    }

    [Fact]
    public async Task ConcurrentDisposeWaitsEvenWhenCancellationCallbackThrows()
    {
        var entered = Signal(); var release = Signal(); var canceled = Signal();
        var fake = new Capability(async (_, token) =>
        {
            using var registration = token.Register(() => { canceled.TrySetResult(true); throw new InvalidOperationException("callback"); });
            entered.TrySetResult(true); await release.Task; token.ThrowIfCancellationRequested(); return State(true);
        });
        var task = new HttpSegmentedDownloadTask<string>(fake, Target, null);
        var run = task.StartAsync(); await entered.Task;
        var first = task.DisposeAsync().AsTask(); await canceled.Task;
        var second = task.DisposeAsync().AsTask();
        Assert.Same(first, second); Assert.False(first.IsCompleted);
        release.TrySetResult(true);
        await Assert.ThrowsAsync<AggregateException>(() => first);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(HttpSegmentedDownloadTaskStatus.Disposed, task.Status); Assert.NotNull(task.LastError);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => task.StartAsync());
    }

    [Fact]
    public async Task FailureIncompleteRetryAndPreCancellationHaveDistinctStates()
    {
        var calls = 0; var failure = new IOException("probe failed");
        var fake = new Capability((_, _) => ++calls == 1 ? Task.FromException<HttpDownloadSegmentsState>(failure) : Task.FromResult(State(false)));
        await using var task = new HttpSegmentedDownloadTask<string>(fake, Target, null);
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.StartAsync(canceled.Token)); Assert.Equal(0, calls);
        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => task.StartAsync())); Assert.Same(failure, task.LastError);
        Assert.Equal(HttpSegmentedDownloadTaskStatus.Failed, task.Status);
        await task.ResumeAsync(); Assert.Equal(HttpSegmentedDownloadTaskStatus.Incomplete, task.Status); Assert.Null(task.LastError);
        task.Discard(); Assert.Equal(HttpSegmentedDownloadTaskStatus.Discarded, task.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => task.ResumeAsync());
    }

    [Fact]
    public async Task CheckpointMaintenanceRespectsLocksOwnershipAndPreview()
    {
        File.WriteAllText(Target, "keep-target", System.Text.Encoding.UTF8);
        var statePath = Target + ".binghttp.segments.json";
        await HttpDownloadSegmentStateStore.SaveAsync(statePath, State(false));
        var staging = Target + ".binghttp.segments"; Directory.CreateDirectory(staging);
        var part = Path.Combine(staging, "segment-0-3.part"); File.WriteAllBytes(part, new byte[4]);
        File.WriteAllText(Path.Combine(staging, "keep.txt"), "external", System.Text.Encoding.UTF8);
        var snapshot = HttpSegmentedDownloadFiles.Inspect(Target); Assert.Equal(4, snapshot.StagedBytes);
        snapshot.State.TotalLength = 100;
        Assert.Equal(4, HttpSegmentedDownloadFiles.Inspect(Target).State.TotalLength);
        using (var active = new FileStream(Target + ".binghttp.lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Throws<IOException>(() => HttpSegmentedDownloadFiles.Discard(Target));
            Assert.Equal(1, HttpSegmentedDownloadFiles.CleanupExpired(_root, DateTime.UtcNow.AddDays(1), false).SkippedTasks);
        }
        var preview = HttpSegmentedDownloadFiles.CleanupExpired(_root, DateTime.UtcNow.AddDays(1));
        Assert.Equal(2, preview.FileCount); Assert.True(File.Exists(part));
        var clean = HttpSegmentedDownloadFiles.Discard(Target);
        Assert.Empty(clean.Errors); Assert.Equal(2, clean.FileCount); Assert.Equal(1, clean.SkippedFiles);
        Assert.True(File.Exists(Path.Combine(staging, "keep.txt")));
        Assert.Equal("keep-target", File.ReadAllText(Target, System.Text.Encoding.UTF8)); Assert.False(File.Exists(statePath));
    }

    [Fact]
    public async Task PartDeletionFailureKeepsCheckpointForLaterCleanup()
    {
        var checkpoint = Target + ".binghttp.segments.json"; await HttpDownloadSegmentStateStore.SaveAsync(checkpoint, State(false));
        var directory = Target + ".binghttp.segments"; Directory.CreateDirectory(directory);
        var part = Path.Combine(directory, "segment-0-3.part"); File.WriteAllBytes(part, new byte[4]);
        // Windows 文件共享规则可复现删除失败；Linux 允许删除打开的文件，交由对应平台场景验证。
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) return;
        using var held = new FileStream(part, FileMode.Open, FileAccess.Read, FileShare.Read);
        var result = HttpSegmentedDownloadFiles.Discard(Target);
        Assert.Single(result.Errors); Assert.True(File.Exists(checkpoint)); Assert.True(File.Exists(part));
    }

    private static TaskCompletionSource<bool> Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static HttpDownloadSegmentsState State(bool complete) => new()
    {
        SchemaVersion = 1, TotalLength = 4,
        Segments = new List<HttpDownloadSegmentState> { new() { Start = 0, End = 3, Completed = complete, BytesWritten = complete ? 4 : 0 } }
    };
    private sealed class Capability : IHttpRequestSegmentedDownload<string>
    {
        private readonly Func<HttpSegmentedDownloadOptions, CancellationToken, Task<HttpDownloadSegmentsState>> _run;
        internal Capability(Func<HttpSegmentedDownloadOptions, CancellationToken, Task<HttpDownloadSegmentsState>> run) => _run = run;
        public Task<HttpDownloadSegmentsState> DownloadFileSegmentedAsync(string filePath, HttpSegmentedDownloadOptions options = null, CancellationToken cancellationToken = default) => _run(options, cancellationToken);
    }
}
