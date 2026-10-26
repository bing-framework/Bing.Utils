using System.Threading;

namespace Bing.IO;

public class TemporaryLifecycleTest : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bing-lifecycle-" + Guid.NewGuid().ToString("N"));
    private TemporaryFileStore Store(long max = 100) => new(Path.Combine(_root, "store"), max);
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }

    [Fact]
    public void BatchReservesAsAWholeAndRetainedEntriesCanBeExplicitlyDeleted()
    {
        var store = Store(10);
        Assert.Throws<IOException>(() => store.CreateFiles(new long[] { 6, 5 })); Assert.Empty(store.ListFiles());
        var leases = store.CreateFiles(new long[] { 2, 3, 5 });
        Assert.Equal(10, store.GetStatistics().ReservedBytes);
        leases[0].SetRetention("external-task"); var id = leases[0].Id;
        Assert.Throws<IOException>(() => store.DeleteFile(id));
        foreach (var lease in leases) lease.Dispose();
        Assert.Equal(1, store.CleanupExpired(DateTime.UtcNow.AddDays(1), false).SkippedFiles);
        store.DeleteFile(id); Assert.Empty(store.ListFiles());
    }

    [Fact]
    public void ReopenListsPreservesContentAndHoldsExclusiveLease()
    {
        var store = Store(); string id;
        using (var first = store.CreateFile(5))
        {
            id = first.Id; first.Stream.WriteByte(7); first.Stream.Flush();
            Assert.Throws<IOException>(() => store.OpenFile(id));
            Assert.Equal(1, Assert.Single(store.ListFiles()).Length);
        }
        using var second = Store().OpenFile(id, true);
        Assert.Equal(0, second.Stream.Position); Assert.Equal(7, second.Stream.ReadByte());
        Assert.Throws<ArgumentException>(() => store.OpenFile("../bad"));
    }

    [Fact]
    public void InterruptedReservationCanResizeAndBeCleaned()
    {
        var store = Store(); string id; string pending;
        using (var lease = store.CreateFile(5))
        {
            id = lease.Id; pending = Path.Combine(Path.GetDirectoryName(lease.FilePath), "reservation.pending");
            File.WriteAllText(pending, "incomplete update", System.Text.Encoding.UTF8);
        }
        using (var reopened = store.OpenFile(id))
        {
            reopened.Resize(10); Assert.Equal(10, reopened.MaxBytes); Assert.False(File.Exists(pending));
            File.WriteAllText(pending, "another interrupted update", System.Text.Encoding.UTF8);
        }
        store.DeleteFile(id); Assert.Empty(store.ListFiles());
        using (var lease = store.CreateFile(5))
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(lease.FilePath), "reservation.pending"), "interrupted", System.Text.Encoding.UTF8);
        var result = store.CleanupExpired(DateTime.UtcNow.AddDays(1), false);
        Assert.Empty(result.Errors); Assert.Equal(1, result.FileCount); Assert.Empty(store.ListFiles());
    }

    [Fact]
    public void ResizeIsPersistentAndQuotaFailureKeepsOldLimit()
    {
        var store = Store(10); var first = store.CreateFile(5);
        first.Stream.Write(new byte[3], 0, 3);
        Assert.Throws<ArgumentException>(() => first.Resize(2));
        first.Resize(8); Assert.Equal(8, store.GetStatistics().ReservedBytes);
        Assert.Throws<IOException>(() => first.Resize(11)); Assert.Equal(8, first.MaxBytes);
        first.Resize(3); string id = first.Id; first.Dispose();
        using var again = store.OpenFile(id, true);
        Assert.Equal(3, again.MaxBytes); again.Stream.Position = 3;
        Assert.Throws<IOException>(() => again.Stream.WriteByte(1));
    }

    [Fact]
    public void PromoteIsAtomicAndFailedCommitCanBeRetried()
    {
        var store = Store(); using var lease = store.CreateFile(10);
        var target = Path.Combine(_root, "final.bin"); File.WriteAllBytes(target, new byte[] { 1, 2, 3 });
        lease.Stream.WriteByte(9);
        Assert.Throws<IOException>(() => lease.Promote(target));
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(target));
        Assert.Equal(1, lease.Stream.Position);
        lease.Stream.WriteByte(8); lease.Promote(target, true);
        Assert.Equal(new byte[] { 9, 8 }, File.ReadAllBytes(target));
        Assert.Equal(0, store.GetStatistics().ReservedBytes);
        Assert.Throws<ObjectDisposedException>(() => lease.Resize(4));
    }

    [Fact]
    public void PromoteNewTargetPublishesCompleteBody()
    {
        var store = Store(); using var lease = store.CreateFile(10);
        lease.Stream.WriteByte(42);
        var target = Path.Combine(_root, "new-final.bin");
        lease.Promote(target);
        Assert.Equal(new byte[] { 42 }, File.ReadAllBytes(target));
        Assert.False(File.Exists(lease.FilePath)); Assert.Empty(store.ListFiles());
    }

    [Fact]
    public void CrossVolumePromotionCannotFallBackToCopy()
    {
        // Windows 本机的临时目录与测试产物目录可位于不同卷；相同卷环境由常规提交测试覆盖。
        if (Path.DirectorySeparatorChar != '\\' || string.Equals(Path.GetPathRoot(_root), Path.GetPathRoot(AppContext.BaseDirectory), StringComparison.OrdinalIgnoreCase)) return;
        var store = Store(); using var lease = store.CreateFile(10, true);
        lease.Stream.WriteByte(42);
        var target = Path.Combine(AppContext.BaseDirectory, "promote-cross-volume-" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.Throws<IOException>(() => lease.Promote(target));
            Assert.False(File.Exists(target)); Assert.True(File.Exists(lease.FilePath));
            lease.Stream.Position = 0; Assert.Equal(42, lease.Stream.ReadByte());
        }
        finally { if (File.Exists(target)) File.Delete(target); }
    }

    [Fact]
    public async Task CancellationDoesNotAcquireOrDelete()
    {
        var store = Store(); using var lease = store.CreateFile(10);
        using var cts = new CancellationTokenSource(); cts.Cancel();
        var error = Assert.Throws<OperationCanceledException>(() => lease.Promote(Path.Combine(_root, "target"), cancellationToken: cts.Token));
        Assert.Equal(cts.Token, error.CancellationToken); Assert.True(lease.Stream.CanWrite);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.CleanupExpiredAsync(DateTime.UtcNow.AddDays(1), false, cts.Token));
        Assert.Single(store.ListFiles());
    }

    [Fact]
    public async Task SpillCopiesExistingBytesAndPositionThenDeletes()
    {
        var store = Store();
        await using (var stream = new SpilloverStream(store, 3, 10))
        {
            stream.Write(new byte[] { 1, 2, 3 }, 0, 3); Assert.False(stream.HasSpilled);
            stream.Position = 1;
            await stream.WriteAsync(new byte[] { 4, 5, 6 }, 0, 3);
            Assert.True(stream.HasSpilled); Assert.Equal(4, stream.Position);
            stream.Position = 0; var copy = new MemoryStream(); await stream.CopyToAsync(copy);
            Assert.Equal(new byte[] { 1, 4, 5, 6 }, copy.ToArray());
            stream.SetLength(2); stream.SetLength(6); stream.Position = 2;
            Assert.Equal(new byte[] { 0, 0, 0, 0 }, ReadRemaining(stream));
            Assert.Throws<IOException>(() => stream.SetLength(11));
        }
        Assert.Equal(0, store.GetStatistics().FileCount);
    }

    [Fact]
    public async Task SpillQuotaAndCanceledWritePreserveMemory()
    {
        var store = Store(5); using var stream = new SpilloverStream(store, 2, 10);
        stream.WriteByte(42); stream.WriteByte(43);
        Assert.Throws<IOException>(() => stream.WriteByte(44));
        Assert.False(stream.HasSpilled); Assert.Equal(2, stream.Length); Assert.Equal(2, stream.Position);
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stream.WriteAsync(new byte[1], 0, 1, cts.Token));
        stream.Position = 0; Assert.Equal(new byte[] { 42, 43 }, ReadRemaining(stream));
    }

    private static byte[] ReadRemaining(Stream source) { using var output = new MemoryStream(); source.CopyTo(output); return output.ToArray(); }
}
