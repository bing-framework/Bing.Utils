using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Bing.Security.Gm;

public class GmStreamApiTests
{
    [Fact]
    public async Task StreamApis_EmptyAndShortReads_ShouldMatchOneShotAndLeaveStreamsOpen()
    {
        var data = new byte[10000];
        for (var index = 0; index < data.Length; index++) data[index] = (byte)index;
        var key = new byte[] { 1, 2, 3, 4 };
        using var empty = new MemoryStream();
        Sm3.Compute(empty).ShouldBe(Sm3.Compute(Array.Empty<byte>()));
        using var sm3Stream = new ShortReadStream(data, 7);
        using var hmacStream = new ShortReadStream(data, 5);
        (await Sm3.ComputeAsync(sm3Stream)).ShouldBe(Sm3.Compute(data));
        (await HmacSm3.ComputeAsync(key, hmacStream)).ShouldBe(HmacSm3.Compute(key, data));
        sm3Stream.CanRead.ShouldBeTrue();
        hmacStream.CanRead.ShouldBeTrue();
    }

    [Fact]
    public async Task FileApis_ShouldMatchOneShot()
    {
        var path = Path.GetTempFileName();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var key = new byte[] { 9, 8, 7 };
        try
        {
            await File.WriteAllBytesAsync(path, data);
            (await Sm3.ComputeFileHexAsync(path)).ShouldBe(Sm3.ComputeHex(data));
            (await HmacSm3.ComputeFileHexAsync(path, key)).ShouldBe(HmacSm3.ComputeHex(key, data));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task AsyncApis_PreCancelledToken_ShouldThrowWithoutConsumingInput()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var sm3Stream = new MemoryStream(new byte[] { 1 });
        using var hmacStream = new MemoryStream(new byte[] { 1 });
        await Should.ThrowAsync<OperationCanceledException>(() => Sm3.ComputeAsync(sm3Stream, cancellation.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => HmacSm3.ComputeAsync(new byte[] { 1 }, hmacStream, cancellation.Token));
        sm3Stream.Position.ShouldBe(0);
        hmacStream.Position.ShouldBe(0);
    }

    [Fact]
    public void HmacVerify_InvalidInputs_ShouldBeExplicit()
    {
        Should.Throw<ArgumentException>(() => HmacSm3.Compute(Array.Empty<byte>(), Array.Empty<byte>()));
        Should.Throw<ArgumentNullException>(() => HmacSm3.Verify(new byte[] { 1 }, new byte[] { 1 }, null));
        HmacSm3.Verify(new byte[] { 1 }, new byte[] { 1 }, new byte[31]).ShouldBeFalse();
    }

    private sealed class ShortReadStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly int _maximum;
        public ShortReadStream(byte[] data, int maximum) { _inner = new MemoryStream(data, false); _maximum = maximum; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, Math.Min(count, _maximum));
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) =>
            _inner.ReadAsync(buffer, offset, Math.Min(count, _maximum), token);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) _inner.Dispose(); base.Dispose(disposing); }
    }
}
