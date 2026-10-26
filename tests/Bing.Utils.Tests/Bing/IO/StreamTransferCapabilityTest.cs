using System.Security.Cryptography;
using System.Threading;

namespace Bing.IO;

/// <summary>
/// 验证流预算、资源归属和文件原子提交。
/// </summary>
public class StreamTransferCapabilityTest
{
    /// <summary>
    /// 同步和异步复制均正确处理短读、位置、校验及限额边界。
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShortReadsAndExactBudget(bool asynchronous)
    {
        using var source = new ShortStream(new byte[] { 1, 2, 3, 4, 5 });
        using var target = new MemoryStream(); target.WriteByte(42);
        var progress = new List<long>();
        var config = new StreamTransferOptions { MaxBytes = 5, BufferSize = 3, ComputeSha256 = true, Progress = new CallbackProgress(progress.Add) };
        var result = asynchronous ? await StreamTransfer.CopyAsync(source, target, config) : StreamTransfer.Copy(source, target, config);
        Assert.Equal(new byte[] { 42, 1, 2, 3, 4, 5 }, target.ToArray());
        Assert.Equal(5, result.BytesTransferred); Assert.Equal(5, progress.Last());
        using var hash = SHA256.Create();
        Assert.Equal(BitConverter.ToString(hash.ComputeHash(new byte[] { 1, 2, 3, 4, 5 })).Replace("-", "").ToLowerInvariant(), result.Sha256);
        Assert.True(source.CanRead); Assert.True(target.CanWrite);
    }

    /// <summary>
    /// 超限或取消不能写入超额字节，参数错误和开始前取消不接管流。
    /// </summary>
    [Fact]
    public async Task LimitsAndOwnership()
    {
        using var source = new MemoryStream(new byte[] { 1, 2, 3 }); using var target = new MemoryStream();
        await Assert.ThrowsAsync<InvalidDataException>(() => StreamTransfer.CopyAsync(source, target, new StreamTransferOptions { MaxBytes = 2 }));
        Assert.True(target.Length <= 2);
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => StreamTransfer.CopyAsync(source, target, leaveOpen: false, cancellationToken: canceled.Token));
        Assert.Equal(canceled.Token, error.CancellationToken); Assert.True(source.CanRead); Assert.True(target.CanWrite);
        Assert.Throws<ArgumentException>(() => StreamTransfer.Copy(source, source, leaveOpen: false)); Assert.True(source.CanRead);
        source.Position = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => StreamTransfer.CopyAsync(source, target, new StreamTransferOptions { MaxBytes = 0 }, leaveOpen: false));
        Assert.False(source.CanRead); Assert.False(target.CanWrite);
        using var empty = new MemoryStream(); using var output = new MemoryStream();
        Assert.Equal(0, StreamTransfer.Copy(empty, output, new StreamTransferOptions { MaxBytes = 0 }).BytesTransferred);
    }

    /// <summary>
    /// 校验失败和中途取消保留旧文件，成功覆盖截断旧尾部并清理临时文件。
    /// </summary>
    [Fact]
    public async Task AtomicCommitPreservesOldFileOnFailure()
    {
        var directory = Path.Combine(Path.GetTempPath(), "bing-atomic-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory); var path = Path.Combine(directory, "target.txt");
        try
        {
            File.WriteAllText(path, "original-long-content", Encoding.UTF8);
            using var source = new MemoryStream(new byte[] { 1, 2, 3 });
            await Assert.ThrowsAsync<InvalidDataException>(() => AtomicFile.WriteAsync(path, source, new StreamTransferOptions { ExpectedSha256 = new string('0', 64) }));
            Assert.Equal("original-long-content", File.ReadAllText(path, Encoding.UTF8)); Assert.True(source.CanRead);
            source.Position = 0; using var canceled = new CancellationTokenSource();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AtomicFile.WriteAsync(path, source,
                new StreamTransferOptions { BufferSize = 1, Progress = new CallbackProgress(_ => canceled.Cancel()) }, cancellationToken: canceled.Token));
            Assert.Equal("original-long-content", File.ReadAllText(path, Encoding.UTF8));
            await AtomicFile.WriteTextAsync(path, "短");
            Assert.Equal(Encoding.UTF8.GetBytes("短"), File.ReadAllBytes(path));
            Assert.Single(Directory.GetFiles(directory));
            await AtomicFile.WriteTextAsync(Path.Combine(directory, "empty"), "");
            Assert.Empty(File.ReadAllBytes(Path.Combine(directory, "empty")));
        }
        finally { Directory.Delete(directory, true); }
    }

    /// <summary>
    /// 严格读取传播缺失、预算和原令牌，成功返回独立内容。
    /// </summary>
    [Fact]
    public async Task StrictReadingPropagatesErrors()
    {
        var path = Path.Combine(Path.GetTempPath(), "bing-strict-" + Guid.NewGuid().ToString("N"));
        Assert.Throws<FileNotFoundException>(() => FileHelper.ReadBytesStrict(path));
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileHelper.ReadBytesStrictAsync(path, cancellationToken: canceled.Token));
        Assert.Equal(canceled.Token, error.CancellationToken);
        try
        {
            File.WriteAllBytes(path, new byte[] { 1, 2 });
            Assert.Equal(new byte[] { 1, 2 }, await FileHelper.ReadBytesStrictAsync(path, 2));
            Assert.Throws<InvalidDataException>(() => FileHelper.ReadBytesStrict(path, 1));
        }
        finally { File.Delete(path); }
    }

    /// <summary>
    /// 大流分段只能包含有效字节，且不改变读取位置。
    /// </summary>
    [Fact]
    public void LargeStreamSegmentsExcludeCapacity()
    {
        using var stream = new LargeMemoryStream(3);
        stream.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
        var segments = stream.GetWrittenSegments();
        Assert.Equal(new[] { 3, 2 }, segments.Select(s => s.Length));
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, segments.SelectMany(s => s.ToArray()));
        Assert.Equal(5, stream.Position); stream.SetLength(1); Assert.Equal(1, stream.GetWrittenSegments().Single().Length);
        stream.SetLength(0); Assert.Empty(stream.GetWrittenSegments());
        stream.Dispose(); Assert.Throws<ObjectDisposedException>(() => stream.GetWrittenSegments());
    }

    /// <summary>
    /// 以内联方式执行进度回调，确定性观察取消。
    /// </summary>
    private sealed class CallbackProgress : IProgress<long>
    {
        private readonly Action<long> _action;
        public CallbackProgress(Action<long> action) => _action = action;
        public void Report(long value) => _action(value);
    }

    /// <summary>
    /// 模拟每次最多返回两个字节的不可定位流。
    /// </summary>
    private sealed class ShortStream : MemoryStream
    {
        public ShortStream(byte[] bytes) : base(bytes) { }
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(2, count));
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Read(buffer, offset, count));
        }
    }
}
