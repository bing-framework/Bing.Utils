using System.IO.Compression;
using System.Threading;

namespace Bing.Helpers;

[Trait("Category", "PerformanceApiReview")]
public class CompressionStreamingTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8193)]
    public async Task RoundTrip_ShortReads_CurrentPositions_AndOpenStreams(int length)
    {
        var data = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();
        foreach (var asynchronous in new[] { false, true })
        {
            using var source = new ShortStream(new byte[] { 99 }.Concat(data).ToArray());
            source.SkipPrefix();
            using var compressed = new MemoryStream();
            compressed.WriteByte(77);
            if (asynchronous) await Compression.CompressToAsync(source, compressed);
            else Compression.CompressTo(source, compressed);
            Assert.True(compressed.Length > 1);
            Assert.Equal(77, compressed.ToArray()[0]);
            Assert.True(source.CanRead);
            using var gzipSource = new ShortStream(compressed.ToArray().Skip(1).ToArray());
            using var destination = new MemoryStream();
            destination.WriteByte(66);
            if (asynchronous) await Compression.DecompressToAsync(gzipSource, destination, length);
            else Compression.DecompressTo(gzipSource, destination, length);
            Assert.Equal(new byte[] { 66 }.Concat(data), destination.ToArray());
            Assert.True(gzipSource.CanRead);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OverLimit_LeavesOnlyBoundedPartialOutput_AndClosesWhenRequested(bool asynchronous)
    {
        var bytes = Compression.Compress(new byte[10000]);
        var input = new MemoryStream(bytes);
        var output = new MemoryStream();
        if (asynchronous)
            await Assert.ThrowsAsync<InvalidDataException>(() => Compression.DecompressToAsync(input, output, 5000, false));
        else
            Assert.Throws<InvalidDataException>(() => Compression.DecompressTo(input, output, 5000, false));
        Assert.InRange(output.ToArray().Length, 1, 5000);
        Assert.False(input.CanRead);
        Assert.False(output.CanWrite);
    }

    [Fact]
    public async Task ValidationAndPreCancellation_DoNotTakeOwnership()
    {
        using var input = new MemoryStream(new byte[] { 1 });
        using var output = new MemoryStream();
        Assert.Throws<ArgumentException>(() => Compression.CompressTo(input, input, leaveOpen: false));
        Assert.Throws<ArgumentOutOfRangeException>(() => Compression.DecompressTo(input, output, -1, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => Compression.CompressTo(input, output, (CompressionLevel)99, false));
        Assert.Throws<ArgumentNullException>(() => Compression.CompressTo(null, output));
        using var readOnly = new MemoryStream(new byte[] { 1 }, false);
        Assert.Throws<ArgumentException>(() => Compression.CompressTo(input, readOnly, leaveOpen: false));
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Compression.CompressToAsync(input, output, leaveOpen: false, cancellationToken: cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Compression.DecompressToAsync(input, output, 100, false, cts.Token));
        Assert.True(input.CanRead);
        Assert.True(output.CanWrite);
        Assert.Equal(0, input.Position);
        Assert.Equal(0, output.Length);
    }

    [Fact]
    public async Task WriteFailure_ClosesBothStreams_AndPropagatesIoError()
    {
        var input = new MemoryStream(new byte[8192]);
        var output = new FailingOutput();
        await Assert.ThrowsAsync<IOException>(() => Compression.CompressToAsync(input, output, leaveOpen: false));
        Assert.False(input.CanRead);
        Assert.False(output.CanWrite);
        input = new MemoryStream(Compression.Compress(new byte[8192]));
        output = new FailingOutput();
        Assert.Throws<IOException>(() => Compression.DecompressTo(input, output, 8192, false));
        Assert.False(input.CanRead);
        Assert.False(output.CanWrite);
    }

    [Fact]
    public async Task CancellationDuringRead_Propagates_AndClosesBoth()
    {
        using var cts = new CancellationTokenSource();
        var input = new CancelingInput(cts);
        var output = new MemoryStream();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Compression.CompressToAsync(input, output, leaveOpen: false, cancellationToken: cts.Token));
        Assert.Equal(cts.Token, error.CancellationToken);
        Assert.False(input.CanRead);
        Assert.False(output.CanWrite);
    }

    [Fact]
    public void InvalidData_DoesNotCloseBorrowedStreams()
    {
        using var input = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        using var output = new MemoryStream();
        Assert.Throws<InvalidDataException>(() => Compression.DecompressTo(input, output, 100));
        Assert.True(input.CanRead);
        Assert.True(output.CanWrite);
    }

    [Fact]
    public async Task LegacyCompression_PreservesRewindEmptyAndOwnership()
    {
        using var input = new MemoryStream(new byte[] { 1, 2, 3 }) { Position = 2 };
        Assert.Equal(new byte[] { 1, 2, 3 }, Compression.Decompress(Compression.Compress(input)));
        input.Position = 2;
        Assert.Equal(new byte[] { 1, 2, 3 }, Compression.Decompress(await Compression.CompressAsync(input)));
        Assert.True(input.CanRead);
        using var empty = new ShortStream(Array.Empty<byte>());
        Assert.Empty(Compression.Compress(empty));
        Assert.Empty(await Compression.CompressAsync(empty));
    }

    private sealed class ShortStream : MemoryStream
    {
        public ShortStream(byte[] bytes) : base(bytes) { }
        public void SkipPrefix() => base.Position = 1;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(2, count));
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(Read(buffer, offset, count));
        }
#if NET6_0_OR_GREATER
        public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(2, buffer.Length)]);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return new ValueTask<int>(Read(buffer.Span));
        }
#endif
        // 禁止 MemoryStream.CopyTo 绕过短读接缝。
        public override void CopyTo(Stream destination, int bufferSize)
        {
            var buffer = new byte[bufferSize]; int count;
            while ((count = Read(buffer, 0, buffer.Length)) != 0) destination.Write(buffer, 0, count);
        }
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken token)
        {
            var buffer = new byte[bufferSize]; int count;
            while ((count = await ReadAsync(buffer, 0, buffer.Length, token)) != 0) await destination.WriteAsync(buffer, 0, count, token);
        }
    }
    private sealed class FailingOutput : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("destination");
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token) => Task.FromException(new IOException("destination"));
#if NET6_0_OR_GREATER
        public override void Write(ReadOnlySpan<byte> buffer) => throw new IOException("destination");
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default) => new(Task.FromException(new IOException("destination")));
#endif
    }
    private sealed class CancelingInput : MemoryStream
    {
        private readonly CancellationTokenSource _cts;
        public CancelingInput(CancellationTokenSource cts) : base(new byte[10]) => _cts = cts;
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken token)
        {
            _cts.Cancel();
            return Task.FromCanceled(token);
        }
    }
}
