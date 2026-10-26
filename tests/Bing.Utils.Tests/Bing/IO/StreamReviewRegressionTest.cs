using Bing.Extensions;
using Bing.Helpers;
using System.Linq;
using System.Threading;

namespace Bing.IO;

/// <summary>验证短读、不可定位流及覆盖写入的数据完整性契约。</summary>
public class StreamReviewRegressionTest
{
    private static readonly byte[] Content = { 1, 2, 3, 4, 5, 6 };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadToBytes_HandlesShortReadsAndPreservesOwnership(bool seekable)
    {
        using var stream = new ShortReadStream(Content, 2, seekable);
        if (seekable) stream.Position = 3;
        FileHelper.ReadToBytes(stream).ShouldBe(Content);
        stream.Disposed.ShouldBeFalse();
        if (seekable) stream.Position.ShouldBe(0);
        using var asyncStream = new ShortReadStream(Content, 1, seekable);
        if (seekable) asyncStream.Position = 2;
        (await FileHelper.ReadToBytesAsync(asyncStream)).ShouldBe(Content);
        asyncStream.Disposed.ShouldBeFalse();
        if (seekable) asyncStream.Position.ShouldBe(0);
    }

    [Fact]
    public async Task ReadToBytesAsync_HonorsCancellationBetweenShortReads()
    {
        using var cancellation = new CancellationTokenSource();
        using var stream = new ShortReadStream(Content, 1, false, () => cancellation.Cancel());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileHelper.ReadToBytesAsync(stream, cancellation.Token));
        stream.Disposed.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void ToFile_TruncatesExistingFile(int newLength)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, Content);
            using var stream = new ShortReadStream(Content.Take(newLength).ToArray(), 1, false);
            stream.ToFile(path).ShouldBeTrue();
            File.ReadAllBytes(path).ShouldBe(Content.Take(newLength).ToArray());
            stream.Disposed.ShouldBeFalse();
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ContentsEqual_ComparesBytesAcrossDifferentChunkBoundaries()
    {
        var content = Enumerable.Range(0, 4101).Select(i => (byte)i).ToArray();
        using var left = new ShortReadStream(content, 1, false);
        using var right = new ShortReadStream(content, 2, false);
        left.ContentsEqual(right).ShouldBeTrue();
        left.Disposed.ShouldBeFalse();
        right.Disposed.ShouldBeFalse();
        using var shorter = new ShortReadStream(Content.Take(5).ToArray(), 2, false);
        using var longer = new ShortReadStream(Content, 1, false);
        shorter.ContentsEqual(longer).ShouldBeFalse();
        using var different = new ShortReadStream(new byte[] { 1, 2, 9 }, 1, false);
        using var original = new ShortReadStream(new byte[] { 1, 2, 3 }, 2, false);
        original.ContentsEqual(different).ShouldBeFalse();
    }

    [Fact]
    public void ContentsEqual_UsesCurrentPositions()
    {
        using var left = new MemoryStream(new byte[] { 0, 1, 2 });
        using var right = new MemoryStream(new byte[] { 1, 2 });
        left.Position = 1;
        left.ContentsEqual(right).ShouldBeTrue();
        left.Position.ShouldBe(3);
        right.Position.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Compression_HandlesNonSeekableShortReadStreams(bool async)
    {
        using var source = new ShortReadStream(Content, 1, false);
        var compressed = async ? await Compression.CompressAsync(source) : Compression.Compress(source);
        source.Disposed.ShouldBeFalse();
        using var compressedStream = new ShortReadStream(compressed, 2, false);
        var result = async ? await Compression.DecompressAsync(compressedStream) : Compression.Decompress(compressedStream);
        result.ShouldBe(Content);
        compressedStream.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task Compression_PreservesSeekablePositionAndCloseConventions()
    {
        using var source = new ShortReadStream(Content, 1, true);
        source.Position = 4;
        var compressed = await Compression.CompressAsync(source);
        source.Position.ShouldBe(Content.Length);
        source.Disposed.ShouldBeFalse();
        var withPrefix = new byte[] { 99 }.Concat(compressed).ToArray();
        using var encoded = new ShortReadStream(withPrefix, 2, true);
        encoded.Position = 1;
        Compression.Decompress(encoded).ShouldBe(Content);
        encoded.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task CompressionAsync_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var source = new ShortReadStream(Content, 1, false);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Compression.CompressAsync(source, cancellationToken: cancellation.Token));
        using var compressed = new ShortReadStream(Compression.Compress(Content), 1, false);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Compression.DecompressAsync(compressed, cancellation.Token));
    }

    /// <summary>只暴露 Stream 标准读取入口，避免 MemoryStream.CopyTo 的快速路径掩盖短读。</summary>
    private sealed class ShortReadStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly int _chunk;
        private readonly bool _seekable;
        private readonly Action _afterRead;
        public bool Disposed { get; private set; }
        public ShortReadStream(byte[] data, int chunk, bool seekable, Action afterRead = null)
        {
            _inner = new MemoryStream(data);
            _chunk = chunk;
            _seekable = seekable;
            _afterRead = afterRead;
        }
        public override bool CanRead => !Disposed;
        public override bool CanSeek => _seekable;
        public override bool CanWrite => false;
        public override long Length => _seekable ? _inner.Length : throw new NotSupportedException();
        public override long Position { get => _seekable ? _inner.Position : throw new NotSupportedException(); set { if (!_seekable) throw new NotSupportedException(); _inner.Position = value; } }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, Math.Min(count, _chunk));
            _afterRead?.Invoke();
            return read;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Read(buffer, offset, count));
        }
        public override long Seek(long offset, SeekOrigin origin) => _seekable ? _inner.Seek(offset, origin) : throw new NotSupportedException();
        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
