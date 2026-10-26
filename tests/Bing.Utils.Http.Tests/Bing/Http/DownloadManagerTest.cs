using System.Threading;
using Bing.Http;
using Bing.IO;
using Moq;

namespace Bing.Utils.Http.Tests.Bing.Http;

public class DownloadManagerTest : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bing-manager-" + Guid.NewGuid().ToString("N"));
    public DownloadManagerTest() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);
    private string Target(string name) => Path.Combine(_root, name);

    [Fact]
    public async Task HostLimitsDoNotBlockOtherHostsAndQueueIsBounded()
    {
        await using var manager = new HttpDownloadManager(2, 1, 2);
        var release = Signal(); var first = Signal(); var other = Signal(); var queuedStarted = false;
        var request1 = Request(async (options, token) => { first.SetResult(true); await release.Task; options.Progress?.Invoke(new HttpDownloadSegmentProgress(new HttpDownloadRange(0, 3), 1, 4, 4)); return State(true); });
        var firstRun = manager.EnqueueAsync(new Uri("https://a.test"), request1, Target("one")); await first.Task;
        var secondRun = manager.EnqueueAsync(new Uri("https://a.test"), Request((_, _) => { queuedStarted = true; return Task.FromResult(State(true)); }), Target("two"));
        var thirdRun = manager.EnqueueAsync(new Uri("https://b.test"), Request(async (_, _) => { other.SetResult(true); await release.Task; return State(true); }), Target("three"));
        await other.Task; Assert.False(queuedStarted); Assert.Equal(2, manager.GetProgress().Running); Assert.Equal(1, manager.GetProgress().Queued);
        using var cts = new CancellationTokenSource();
        var canceled = manager.EnqueueAsync(new Uri("https://a.test"), Request((_, _) => throw new Exception("must not start")), Target("cancel"), cancellationToken: cts.Token);
        cts.Cancel(); Assert.Equal(cts.Token, (await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled)).CancellationToken);
        release.SetResult(true); Assert.All(await Task.WhenAll(firstRun, secondRun, thirdRun), x => Assert.True(x.IsComplete));
        Assert.True(queuedStarted); Assert.Equal(12, manager.GetProgress().CompletedBytes);
    }

    [Fact]
    public async Task DisposeWaitsForRunningCleanupAndCancelsWaitingCallers()
    {
        var manager = new HttpDownloadManager(1, 1, 1); var entered = Signal(); var canceled = Signal(); var release = Signal();
        var first = manager.EnqueueAsync(new Uri("https://a.test"), Request(async (_, token) =>
        { using var registration = token.Register(() => canceled.TrySetResult(true)); entered.SetResult(true); await release.Task; token.ThrowIfCancellationRequested(); return State(true); }), Target("first"));
        await entered.Task;
        var second = manager.EnqueueAsync(new Uri("https://a.test"), Request((_, _) => throw new Exception()), Target("second"));
        var third = manager.EnqueueAsync(new Uri("https://a.test"), Request((_, _) => throw new Exception()), Target("third"));
        var disposal = manager.DisposeAsync().AsTask(); await canceled.Task; Assert.False(disposal.IsCompleted);
        release.SetResult(true); await disposal;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => third);
    }

    [Fact]
    public async Task ExplicitResultAndQuotaRetentionAreRecoverable()
    {
        var result = await Request((_, _) => Task.FromResult(State(false))).DownloadFileSegmentedResultAsync(Target("result"));
        Assert.False(result.IsComplete); Assert.Single(result.FailedSegments); Assert.EndsWith(".binghttp.segments.json", result.RecoveryPath);
        var store = new TemporaryFileStore(Path.Combine(_root, "quota"), 8);
        var target = Target("quota-target");
        using (var held = await HttpDownloadDiskReservation.AcquireAsync(target, store, 4, CancellationToken.None))
            Assert.Equal(8, store.GetStatistics().ReservedBytes);
        Assert.Equal(1, store.CleanupExpired(DateTime.UtcNow.AddDays(1), false).SkippedFiles);
        Assert.Equal(8, store.GetStatistics().ReservedBytes);
        await Assert.ThrowsAsync<IOException>(() => HttpDownloadDiskReservation.AcquireAsync(Target("other"), store, 1, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => HttpDownloadDiskReservation.AcquireAsync(target, null, 4, CancellationToken.None));
        using (var reopened = await HttpDownloadDiskReservation.AcquireAsync(target, store, 4, CancellationToken.None)) reopened.Release();
        Assert.Equal(0, store.GetStatistics().ReservedBytes);
    }
    [Fact]
    public async Task QuotaCountsResiduesAndDiscardCleansMergeBodies()
    {
        var target = Target("residues"); var staging = target + ".binghttp.segments";
        Directory.CreateDirectory(staging);
        File.WriteAllBytes(Path.Combine(staging, "segment-0-3.part"), new byte[20]);
        var merge = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllBytes(merge, new byte[30]);
        var unrelated = target + ".tmp"; File.WriteAllBytes(unrelated, new byte[1]);
        var files = new HttpDownloadSegmentFileStore(staging, target) { EnforceDiskReservation = true };
        Assert.Equal(50, files.GetReservedWorkspaceLength());
        var store = new TemporaryFileStore(Path.Combine(_root, "residue-quota"), 50);
        using (var reservation = await HttpDownloadDiskReservation.AcquireAsync(target, store, 4, CancellationToken.None, files.GetReservedWorkspaceLength()))
        {
            Assert.Equal(50, store.GetStatistics().ReservedBytes);
            files.Reconcile(State(false)); files.PrepareReservedWorkspace(State(false));
            Assert.Equal(0, files.GetReservedWorkspaceLength());
            Assert.True(File.Exists(unrelated));
            File.WriteAllBytes(merge, new byte[4]);
        }
        await HttpSegmentedDownloadFiles.DiscardAsync(target, store);
        Assert.False(File.Exists(merge)); Assert.True(File.Exists(unrelated));
        Assert.Equal(0, store.GetStatistics().ReservedBytes);
    }

    private static IHttpRequest<string> Request(Func<HttpSegmentedDownloadOptions, CancellationToken, Task<HttpDownloadSegmentsState>> run)
    {
        var mock = new Mock<IHttpRequest<string>>();
        mock.As<IHttpRequestSegmentedDownload<string>>().Setup(x => x.DownloadFileSegmentedAsync(It.IsAny<string>(), It.IsAny<HttpSegmentedDownloadOptions>(), It.IsAny<CancellationToken>()))
            .Returns((string _, HttpSegmentedDownloadOptions options, CancellationToken token) => run(options, token));
        return mock.Object;
    }
    private static HttpDownloadSegmentsState State(bool complete) => new() { SchemaVersion = 1, TotalLength = 4,
        Segments = new List<HttpDownloadSegmentState> { new() { Start = 0, End = 3, Completed = complete, BytesWritten = complete ? 4 : 0 } } };
    private static TaskCompletionSource<bool> Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
