using System.IO.Compression;
using System.Text;
using System.Threading;

namespace Bing.Helpers;

public class CompressionLimitTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4096)]
    [InlineData(8193)]
    public async Task GZip_ExactLimitSucceeds_OneByteLessFails(int length)
    {
        var data = Enumerable.Repeat((byte)42, length).ToArray();
        var compressed = CreateGZip(data);
        Assert.Equal(data, Compression.DecompressWithLimit(compressed, length));
        Assert.Equal(data, await Compression.DecompressWithLimitAsync(compressed, length));
        if (length == 0) return;
        Assert.Throws<InvalidDataException>(() => Compression.DecompressWithLimit(compressed, length - 1));
        await Assert.ThrowsAsync<InvalidDataException>(() => Compression.DecompressWithLimitAsync(compressed, length - 1));
    }

    [Fact]
    public async Task GZip_ShortNonSeekableInput_IsSupportedAndClosed()
    {
        var data = Encoding.UTF8.GetBytes("短读和不可定位流");
        var compressed = CreateGZip(data);
        var sync = new ShortReadStream(compressed);
        Assert.Equal(data, Compression.DecompressWithLimit(sync, data.Length));
        Assert.True(sync.IsDisposed);
        var asyncInput = new ShortReadStream(compressed);
        Assert.Equal(data, await Compression.DecompressWithLimitAsync(asyncInput, data.Length));
        Assert.True(asyncInput.IsDisposed);
        var overLimit = new ShortReadStream(compressed);
        Assert.Throws<InvalidDataException>(() => Compression.DecompressWithLimit(overLimit, 0));
        Assert.True(overLimit.IsDisposed);
        var asyncOverLimit = new ShortReadStream(compressed);
        await Assert.ThrowsAsync<InvalidDataException>(() => Compression.DecompressWithLimitAsync(asyncOverLimit, 0));
        Assert.True(asyncOverLimit.IsDisposed);
    }

    [Fact]
    public async Task GZip_UsesCurrentPosition_AndKeepsSeekableEmptyStreamOpen()
    {
        var compressed = CreateGZip(new byte[] { 1, 2 });
        var input = new MemoryStream(new byte[] { 99 }.Concat(compressed).ToArray()) { Position = 1 };
        Assert.Equal(new byte[] { 1, 2 }, Compression.DecompressWithLimit(input, long.MaxValue));
        Assert.False(input.CanRead);
        using var empty = new MemoryStream();
        Assert.Empty(Compression.DecompressWithLimit(empty, 0));
        Assert.Empty(await Compression.DecompressWithLimitAsync(empty, 0));
        Assert.True(empty.CanRead);
    }

    [Fact]
    public async Task GZip_StringBudgetCountsUtf8Bytes()
    {
        const string text = "中文";
        var compressed = Compression.Compress(text);
        Assert.Equal(text, Compression.DecompressWithLimit(compressed, 6));
        Assert.Equal(text, await Compression.DecompressWithLimitAsync(compressed, 6));
        Assert.Throws<InvalidDataException>(() => Compression.DecompressWithLimit(compressed, 2));
        await Assert.ThrowsAsync<InvalidDataException>(() => Compression.DecompressWithLimitAsync(compressed, 2));
        Assert.Equal(string.Empty, Compression.DecompressWithLimit(" ", 0));
        Assert.Equal(string.Empty, await Compression.DecompressWithLimitAsync(" ", 0));
        Assert.Equal(text, Compression.Decompress(compressed));
    }

    [Fact]
    public async Task GZip_RejectsInvalidLimitsAndPreservesCancellation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Compression.DecompressWithLimit(Array.Empty<byte>(), -1));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Compression.DecompressWithLimitAsync(Array.Empty<byte>(), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Compression.DecompressWithLimit("", -1));
        Assert.Throws<InvalidDataException>(() => Compression.DecompressWithLimit(new byte[] { 1, 2, 3, 4 }, 100));
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Compression.DecompressWithLimitAsync(Array.Empty<byte>(), 0, canceled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Compression.DecompressWithLimitAsync("", 0, cancellationToken: canceled.Token));
        using var midRead = new CancellationTokenSource();
        var stream = new ShortReadStream(CreateGZip(new byte[4096]), midRead.Cancel);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Compression.DecompressWithLimitAsync(stream, 4096, midRead.Token));
        Assert.True(stream.IsDisposed);
    }

    [Fact]
    public void Zip_UsesAggregateLimit_AndPrechecksBeforeCreatingDirectories()
    {
        WithArchive(new[] { ("nested/a.bin", new byte[] { 1, 2 }), ("b.bin", new byte[] { 3, 4 }) }, (zip, target) =>
        {
            Assert.Throws<InvalidDataException>(() => Compression.UnZipWithLimit(zip, target, 3));
            Assert.False(Directory.Exists(target));
            Compression.UnZipWithLimit(zip, target, 4);
            Assert.Equal(new byte[] { 1, 2 }, File.ReadAllBytes(Path.Combine(target, "nested/a.bin")));
            Assert.Equal(new byte[] { 3, 4 }, File.ReadAllBytes(Path.Combine(target, "b.bin")));
        });
    }

    [Fact]
    public void Zip_RejectsUnsafePathsBeforeWritingAnyEntry()
    {
        WithArchive(new[] { ("safe.txt", new byte[] { 1 }), ("../outside.txt", new byte[] { 2 }) }, (zip, target) =>
        {
            Assert.Throws<InvalidDataException>(() => Compression.UnZipWithLimit(zip, target, 2));
            Assert.False(Directory.Exists(target));
            Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(target), "outside.txt")));
        });
    }

    [Fact]
    public void Zip_OverwritePolicyAndZeroBudgetEmptyFiles()
    {
        WithArchive(new[] { ("empty.bin", Array.Empty<byte>()) }, (zip, target) =>
        {
            Directory.CreateDirectory(target);
            var destination = Path.Combine(target, "empty.bin");
            File.WriteAllBytes(destination, new byte[] { 8, 9 });
            Assert.Throws<IOException>(() => Compression.UnZipWithLimit(zip, target, 0, false));
            Assert.Equal(new byte[] { 8, 9 }, File.ReadAllBytes(destination));
            Compression.UnZipWithLimit(zip, target, 0);
            Assert.Empty(File.ReadAllBytes(destination));
            Assert.Single(Directory.GetFiles(target));
        });
    }

    [Fact]
    public void Zip_ForgedDeclaredSizesCannotBypassActualAggregateBudget()
    {
        WithArchive(new[] { ("first.bin", new byte[] { 1, 2, 3 }), ("second.bin", new byte[] { 4, 5, 6 }) }, (zip, target) =>
        {
            // 同时缩小本地文件头和中央目录中的声明长度；预算必须检查实际解压量。
            var bytes = File.ReadAllBytes(zip);
            for (var i = 0; i < bytes.Length - 28; i++)
            {
                var signature = BitConverter.ToUInt32(bytes, i);
                var sizeOffset = signature == 0x04034b50 ? 22 : signature == 0x02014b50 ? 24 : -1;
                if (sizeOffset >= 0)
                    Array.Copy(BitConverter.GetBytes(1), 0, bytes, i + sizeOffset, 4);
            }
            File.WriteAllBytes(zip, bytes);
            Directory.CreateDirectory(target);
            var destination = Path.Combine(target, "second.bin");
            File.WriteAllBytes(destination, new byte[] { 99 });
            var error = Assert.Throws<InvalidDataException>(() => Compression.UnZipWithLimit(zip, target, 5));
            Assert.Contains("超过", error.Message);
            Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(Path.Combine(target, "first.bin")));
            Assert.Equal(new byte[] { 99 }, File.ReadAllBytes(destination));
            Assert.Empty(Directory.GetFiles(target, ".bing-unzip-*.tmp"));
        });
    }

    private static byte[] CreateGZip(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(data, 0, data.Length);
        return output.ToArray();
    }

    private static void WithArchive((string Name, byte[] Data)[] entries, Action<string, string> action)
    {
        var root = Path.Combine(Path.GetTempPath(), "Bing.Utils.Limits." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var zip = Path.Combine(root, "input.zip");
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
                foreach (var entry in entries)
                {
                    using var output = archive.CreateEntry(entry.Name, CompressionLevel.NoCompression).Open();
                    output.Write(entry.Data, 0, entry.Data.Length);
                }
            action(zip, Path.Combine(root, "output"));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private sealed class ShortReadStream : Stream
    {
        private readonly MemoryStream _source;
        private readonly Action _onRead;
        public bool IsDisposed { get; private set; }
        public ShortReadStream(byte[] data, Action onRead = null) { _source = new MemoryStream(data); _onRead = onRead; }
        public override bool CanRead => !IsDisposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, Math.Min(count, 2));
            _onRead?.Invoke();
            return read;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Read(buffer, offset, count));
        }
        protected override void Dispose(bool disposing) { IsDisposed = true; if (disposing) _source.Dispose(); base.Dispose(disposing); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
