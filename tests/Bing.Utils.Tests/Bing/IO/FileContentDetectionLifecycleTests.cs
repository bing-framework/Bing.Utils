using Bing.IO;
using System.Threading;

namespace Bing.Utils.Tests.Bing.IO;

/// <summary>验证公开入口参数、重放流所有重载及释放后的访问。</summary>
public class FileContentDetectionLifecycleTests
{
    /// <summary>无效参数和开始前取消不消费输入，路径入口不抢先触发 IO。</summary>
    [Fact]
    public async Task EntryPoints_ValidateArgumentsBeforeReading()
    {
        Assert.Throws<ArgumentNullException>(() => FileContentDetector.Detect((byte[])null));
        Assert.Throws<ArgumentNullException>(() => FileContentDetector.Detect((Stream)null));
        Assert.Throws<ArgumentNullException>(() => FileContentDetector.Probe(null));
        Assert.Throws<ArgumentNullException>(() => FileContentDetector.DetectFile(null));
        Assert.Throws<ArgumentException>(() => FileContentDetector.DetectFile(" "));
        Assert.Throws<ArgumentNullException>(() => FileContentDetector.CompareExtension(null, "jpg"));
        using var unreadable = new MemoryStream();
        unreadable.Dispose();
        Assert.Throws<ArgumentException>(() => FileContentDetector.Probe(unreadable));
        using var source = new MemoryStream(new byte[] { 0x89, 0x50 });
        var token = new CancellationToken(true);
        Assert.ThrowsAny<OperationCanceledException>(() => FileContentDetector.Detect(source, cancellationToken: token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileContentDetector.DetectFileAsync("does-not-exist", cancellationToken: token));
        Assert.Equal(0, source.Position);
    }

    /// <summary>已观察 EOF 与恰好达到预算的未知 EOF 使用不同原因，均可完整重放。</summary>
    [Fact]
    public void Probe_TruncatedAndBudgetLimitedPrefixes_KeepDistinctReasons()
    {
        using var source = new MemoryStream(new byte[] { 0x89, 0x50 });
        using (var probe = FileContentDetector.Probe(source))
        {
            Assert.Equal(FileDetectionReason.Truncated, probe.Result.ReasonCode);
            Assert.Equal(0x89, probe.Content.ReadByte());
            Assert.Equal(0x50, probe.Content.ReadByte());
            Assert.Equal(-1, probe.Content.ReadByte());
        }
        source.Position = 0;
        using var limited = FileContentDetector.Probe(source, new FileDetectionOptions { MaxHeaderBytes = 2 });
        Assert.Equal(FileDetectionReason.HeaderLimitReached, limited.Result.ReasonCode);
        using var empty = new MemoryStream();
        using var emptyProbe = FileContentDetector.Probe(empty);
        Assert.Equal(FileDetectionStatus.Empty, emptyProbe.Result.Status);
        Assert.Equal(-1, emptyProbe.Content.ReadByte());
    }

    /// <summary>字节、Span、Memory 和数组异步入口可以交错消费，取消和零长度不前进。</summary>
    [Fact]
    public async Task Replay_AllReadOverloads_PreserveContentAndHonorCancellation()
    {
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.7 content after the header");
        using var source = new MemoryStream(bytes);
        using var probe = await FileContentDetector.ProbeAsync(source);
        var readback = new byte[bytes.Length];
        var token = new CancellationToken(true);
        Assert.Equal(0, await probe.Content.ReadAsync(readback, 0, 0, CancellationToken.None));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => probe.Content.ReadAsync(readback, 0, 1, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await probe.Content.ReadAsync(readback.AsMemory(0, 1), token));
        readback[0] = (byte)probe.Content.ReadByte();
        Assert.Equal(2, probe.Content.Read(readback.AsSpan(1, 2)));
        Assert.Equal(3, await probe.Content.ReadAsync(readback.AsMemory(3, 3)));
        var offset = 6;
        int count;
        while ((count = await probe.Content.ReadAsync(readback, offset, readback.Length - offset, CancellationToken.None)) > 0)
            offset += count;
        Assert.Equal(bytes.Length, offset);
        Assert.Equal(bytes, readback);
        Assert.Equal(-1, probe.Content.ReadByte());
    }

    /// <summary>Content 和租约可重复释放；源只关闭一次，释放后不允许读取已清理的前缀。</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Replay_DisposeIsIdempotentAndEnforcesOwnership(bool leaveOpen)
    {
        var source = new DisposalStream(Encoding.ASCII.GetBytes("%PDF-1.7"));
        var probe = FileContentDetector.Probe(source, leaveOpen: leaveOpen);
        probe.Content.Dispose();
        probe.Dispose();
        Assert.Equal(leaveOpen ? 0 : 1, source.DisposeCount);
        Assert.False(probe.Content.CanRead);
        Assert.Throws<ObjectDisposedException>(() => probe.Content.ReadByte());
        Assert.Throws<ObjectDisposedException>(() => probe.Content.Read(new byte[1], 0, 1));
        Assert.Equal("pdf", probe.Result.Format.Id);
        if (leaveOpen) source.Dispose();
    }

    /// <summary>记录底层源释放次数。</summary>
    private sealed class DisposalStream : MemoryStream
    {
        internal int DisposeCount { get; private set; }
        internal DisposalStream(byte[] content) : base(content) { }
        protected override void Dispose(bool disposing)
        {
            if (disposing) DisposeCount++;
            base.Dispose(disposing);
        }
    }
}
