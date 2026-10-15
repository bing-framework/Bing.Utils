using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Bing.Security.Gm;

/// <summary>
/// 验证 SM3 与 HMAC-SM3 的流式、文件和取消行为。
/// </summary>
public class GmStreamApiTests
{
    /// <summary>
    /// 验证空输入和短读流的摘要与一次性计算一致，且流保持打开。
    /// </summary>
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

    /// <summary>
    /// 验证文件摘要与相同内容的一次性摘要一致。
    /// </summary>
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

    /// <summary>
    /// 验证预先取消的令牌会使异步计算立即终止且不读取输入流。
    /// </summary>
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

    /// <summary>
    /// 验证 HMAC-SM3 拒绝空密钥和空签名，并对长度错误的签名返回 <see langword="false"/>。
    /// </summary>
    [Fact]
    public void HmacVerify_InvalidInputs_ShouldBeExplicit()
    {
        Should.Throw<ArgumentException>(() => HmacSm3.Compute(Array.Empty<byte>(), Array.Empty<byte>()));
        Should.Throw<ArgumentNullException>(() => HmacSm3.Verify(new byte[] { 1 }, new byte[] { 1 }, null));
        HmacSm3.Verify(new byte[] { 1 }, new byte[] { 1 }, new byte[31]).ShouldBeFalse();
    }

    /// <summary>
    /// 将每次读取限制为指定字节数的只读流。
    /// </summary>
    private sealed class ShortReadStream : Stream
    {
        /// <summary>
        /// 保存实际承载数据的内存流。
        /// </summary>
        private readonly MemoryStream _inner;

        /// <summary>
        /// 限制每次读取的最大字节数。
        /// </summary>
        private readonly int _maximum;

        /// <summary>
        /// 初始化一个 <see cref="ShortReadStream"/> 类型的实例。
        /// </summary>
        /// <param name="data">流读取的数据。</param>
        /// <param name="maximum">每次读取允许返回的最大字节数。</param>
        public ShortReadStream(byte[] data, int maximum) { _inner = new MemoryStream(data, false); _maximum = maximum; }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        /// <inheritdoc />
        public override void Flush() { }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, Math.Min(count, _maximum));

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) =>
            _inner.ReadAsync(buffer, offset, Math.Min(count, _maximum), token);

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        protected override void Dispose(bool disposing) { if (disposing) _inner.Dispose(); base.Dispose(disposing); }
    }
}
