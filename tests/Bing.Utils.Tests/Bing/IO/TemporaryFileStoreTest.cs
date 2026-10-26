using System.Threading;

namespace Bing.IO;

/// <summary>验证临时存储的额度、活动锁、跨实例统计和清理边界。</summary>
public class TemporaryFileStoreTest : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bing-temp-test-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }

    [Fact]
    public async Task ReservationAndWriteLimitsIncludeSeekAndSetLength()
    {
        var store = new TemporaryFileStore(_root, 5);
        using var lease = store.CreateFile(5);
        Assert.Throws<IOException>(() => store.CreateFile(1));
        lease.Stream.Position = 4; lease.Stream.WriteByte(42);
        Assert.Throws<IOException>(() => lease.Stream.WriteByte(1));
        Assert.Throws<IOException>(() => lease.Stream.SetLength(6));
        Assert.Throws<IOException>(() => lease.Stream.Position = 6);
        Assert.Equal(5, lease.Stream.Length);
        lease.Stream.Position = 0;
        await Assert.ThrowsAsync<IOException>(() => lease.Stream.WriteAsync(new byte[6], 0, 6));
        lease.Stream.Flush();
        var bytes = new byte[5]; Assert.Equal(5, lease.Stream.Read(bytes, 0, 5));
        Assert.Equal(new byte[] { 0, 0, 0, 0, 42 }, bytes); lease.Stream.Position = 0;
        var statistics = new TemporaryFileStore(_root, 5).GetStatistics();
        Assert.Equal(5, statistics.ReservedBytes); Assert.Equal(5, statistics.ActualBytes);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => lease.Stream.WriteAsync(new byte[1], 0, 1, cancellation.Token));
        Assert.Equal(0, lease.Stream.Position);
    }

    [Fact]
    public void CleanupSkipsActivePreviewsAndReleasesQuotaOnlyAfterDeletion()
    {
        var store = new TemporaryFileStore(_root, 10); var other = new TemporaryFileStore(_root, 10);
        var lease = store.CreateFile(10); lease.Stream.WriteByte(2); lease.Stream.Flush();
        var cutoff = DateTime.UtcNow.AddDays(1);
        Assert.Equal(1, other.CleanupExpired(cutoff, false).SkippedFiles);
        lease.Dispose(); lease.Dispose(); Assert.True(File.Exists(lease.FilePath));
        Assert.Throws<IOException>(() => other.CreateFile(1));
        var preview = other.CleanupExpired(cutoff);
        Assert.Equal(1, preview.FileCount); Assert.Equal(10, preview.ReservedBytes); Assert.True(File.Exists(lease.FilePath));
        var deleted = other.CleanupExpired(cutoff, false);
        Assert.Empty(deleted.Errors); Assert.Equal(1, deleted.Bytes); Assert.False(File.Exists(lease.FilePath));
        Assert.Equal(0, store.GetStatistics().ReservedBytes);
        using var replacement = store.CreateFile(10, true);
    }

    [Fact]
    public void AutoDeleteAndZeroQuota()
    {
        var store = new TemporaryFileStore(_root, 0); string path;
        using (var lease = store.CreateFile(0, true))
        {
            path = lease.FilePath; Assert.Throws<IOException>(() => lease.Stream.WriteByte(1));
        }
        Assert.False(File.Exists(path)); Assert.Equal(0, store.GetStatistics().FileCount);
        Assert.Throws<ArgumentException>(() => store.CleanupExpired(DateTime.Now));
    }

    [Fact]
    public void CleanupDoesNotDeleteUnmanagedOrMalformedEntries()
    {
        var store = new TemporaryFileStore(_root, 10);
        var lease = store.CreateFile(5); var entry = Path.GetDirectoryName(lease.FilePath); lease.Dispose();
        File.WriteAllText(Path.Combine(entry, "keep.txt"), "keep", Encoding.UTF8);
        var result = store.CleanupExpired(DateTime.UtcNow.AddDays(1), false);
        Assert.Equal(1, result.SkippedFiles); Assert.True(File.Exists(lease.FilePath));
        File.WriteAllText(Path.Combine(entry, "reservation.txt"), "broken", Encoding.UTF8);
        Assert.Throws<InvalidDataException>(() => store.GetStatistics());
        Assert.Throws<InvalidDataException>(() => store.CreateFile(1));
        result = store.CleanupExpired(DateTime.UtcNow.AddDays(1), false);
        Assert.Single(result.Errors); Assert.True(File.Exists(lease.FilePath));
    }

    [Fact]
    public async Task ConcurrentCreationOnSameStoreCannotOverReserve()
    {
        var store = new TemporaryFileStore(_root, 8);
        var leases = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => store.CreateFile(1))));
        try { Assert.Equal(8, store.GetStatistics().ReservedBytes); Assert.Throws<IOException>(() => store.CreateFile(1)); }
        finally { foreach (var lease in leases) lease.Dispose(); }
        Assert.Equal(8, store.CleanupExpired(DateTime.UtcNow.AddDays(1), false).FileCount);
    }
}
