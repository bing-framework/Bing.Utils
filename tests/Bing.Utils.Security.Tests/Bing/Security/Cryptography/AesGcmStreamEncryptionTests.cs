using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Bing.Security.Randomness;
using Shouldly;
using Xunit;

namespace Bing.Security.Cryptography;

/// <summary>
/// 验证 AES-GCM 分块认证流加密格式。
/// </summary>
public class AesGcmStreamEncryptionTests
{
    /// <summary>
    /// 测试目的：空流、多块数据和不可 Seek 输入应能够往返，且不关闭调用方流。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9000)]
    public async Task EncryptAndDecryptAsync_WhenInputIsValid_ShouldRoundTripWithoutClosingStreams(int length)
    {
        // Arrange
        var key = SecurityRandom.GetBytes(32);
        var input = SecurityRandom.GetBytes(length);
        using var plaintext = new NonSeekableReadStream(input);
        using var ciphertext = new MemoryStream();
        using var decrypted = new MemoryStream();

        // Act
        await AesGcmStreamEncryption.EncryptAsync(plaintext, ciphertext, key, new AesGcmStreamOptions { BlockSize = 4096 });
        ciphertext.Position = 0;
        await AesGcmStreamEncryption.DecryptAsync(ciphertext, decrypted, key);

        // Assert
        decrypted.ToArray().ShouldBe(input);
        ciphertext.CanWrite.ShouldBeTrue();
        decrypted.CanWrite.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：新写入流必须使用 BSS2，且默认解密入口不得无声读取存在 Nonce 风险的 BSS1 流。
    /// </summary>
    [Fact]
    public async Task DecryptAsync_WhenStreamUsesLegacyBss1_ShouldRequireExplicitMigrationApi()
    {
        // Arrange
        var key = SecurityRandom.GetBytes(32);
        var ciphertext = await EncryptAsync(new byte[] { 1, 2, 3 }, key);
        var legacy = (byte[])ciphertext.Clone();
        legacy[3] = (byte)'1';
        legacy[4] = 1;
        using var source = new MemoryStream(legacy);
        using var destination = new MemoryStream();

        // Act
        var action = new Func<Task>(() => AesGcmStreamEncryption.DecryptAsync(source, destination, key));

        // Assert
        await Should.ThrowAsync<NotSupportedException>(action);
        ciphertext[0].ShouldBe((byte)'B');
        ciphertext[1].ShouldBe((byte)'S');
        ciphertext[2].ShouldBe((byte)'S');
        ciphertext[3].ShouldBe((byte)'2');
    }

    /// <summary>
    /// 测试目的：使用同一主密钥加密多个文件时，BSS2 必须生成不同的文件盐和 Nonce 前缀。
    /// </summary>
    [Fact]
    public async Task EncryptAsync_WhenMasterKeyIsReused_ShouldUseDistinctFileSaltAndNoncePrefix()
    {
        // Arrange
        var key = SecurityRandom.GetBytes(32);

        // Act
        var first = await EncryptAsync(new byte[] { 1 }, key);
        var second = await EncryptAsync(new byte[] { 1 }, key);

        // Assert
        first.AsSpan(0, 4).SequenceEqual(new byte[] { (byte)'B', (byte)'S', (byte)'S', (byte)'2' }).ShouldBeTrue();
        first.AsSpan(12, 32).SequenceEqual(second.AsSpan(12, 32)).ShouldBeFalse();
        first.AsSpan(44, 8).SequenceEqual(second.AsSpan(44, 8)).ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：BSS2 读写不得假定单次 ReadAsync 可以填满请求缓冲区。
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public async Task EncryptAndDecryptAsync_WhenStreamsReturnShortReads_ShouldRoundTrip(int maximumReadLength)
    {
        // Arrange
        var key = SecurityRandom.GetBytes(32);
        var input = SecurityRandom.GetBytes(9000);
        using var plaintext = new NonSeekableReadStream(input, maximumReadLength);
        using var ciphertext = new MemoryStream();
        using var decrypted = new MemoryStream();

        // Act
        await AesGcmStreamEncryption.EncryptAsync(plaintext, ciphertext, key, new AesGcmStreamOptions { BlockSize = 4096 });
        using var encryptedInput = new NonSeekableReadStream(ciphertext.ToArray(), maximumReadLength);
        await AesGcmStreamEncryption.DecryptAsync(encryptedInput, decrypted, key);

        // Assert
        decrypted.ToArray().ShouldBe(input);
    }

    /// <summary>
    /// 测试目的：密文、终止标签和完整数据块遭到篡改、截断或删除时必须拒绝解密。
    /// </summary>
    [Fact]
    public async Task DecryptAsync_WhenCiphertextIntegrityIsViolated_ShouldThrowCryptographicException()
    {
        // Arrange
        var key = SecurityRandom.GetBytes(32);
        var source = SecurityRandom.GetBytes(9000);
        var ciphertext = await EncryptAsync(source, key);
        const int headerLength = 52;
        const int firstRecordLength = 1 + 8 + 4096 + 16;
        var tampered = (byte[])ciphertext.Clone();
        tampered[headerLength + 1 + 8] ^= 1;
        var truncated = ciphertext[..^1];
        var deletedBlock = new byte[ciphertext.Length - firstRecordLength];
        Buffer.BlockCopy(ciphertext, 0, deletedBlock, 0, headerLength);
        Buffer.BlockCopy(ciphertext, headerLength + firstRecordLength, deletedBlock, headerLength, ciphertext.Length - (headerLength + firstRecordLength));

        // Act
        var tamperedAction = new Func<Task>(() => DecryptAsync(tampered, key));
        var truncatedAction = new Func<Task>(() => DecryptAsync(truncated, key));
        var deletedAction = new Func<Task>(() => DecryptAsync(deletedBlock, key));

        // Assert
        await Should.ThrowAsync<CryptographicException>(tamperedAction);
        await Should.ThrowAsync<InvalidDataException>(truncatedAction);
        await Should.ThrowAsync<CryptographicException>(deletedAction);
    }

    /// <summary>
    /// 测试目的：错误密钥和已取消操作不得返回明文。
    /// </summary>
    [Fact]
    public async Task EncryptAndDecryptAsync_WhenKeyIsWrongOrCancelled_ShouldRejectOperation()
    {
        // Arrange
        var key = SecurityRandom.GetBytes(32);
        var ciphertext = await EncryptAsync(SecurityRandom.GetBytes(64), key);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var input = new MemoryStream(new byte[1]);
        using var destination = new MemoryStream();
        var wrongKeyAction = new Func<Task>(() => DecryptAsync(ciphertext, SecurityRandom.GetBytes(32)));
        var cancelledAction = new Func<Task>(() => AesGcmStreamEncryption.EncryptAsync(input, destination, key, cancellationToken: cancellation.Token));

        // Act
        var wrongKeyException = await Should.ThrowAsync<CryptographicException>(wrongKeyAction);
        var cancelledException = await Should.ThrowAsync<OperationCanceledException>(cancelledAction);

        // Assert
        wrongKeyException.ShouldNotBeNull();
        cancelledException.ShouldNotBeNull();
        destination.Length.ShouldBe(0);
    }

    /// <summary>
    /// 使用指定密钥加密测试数据。
    /// </summary>
    /// <param name="plaintext">测试明文。</param>
    /// <param name="key">AES-256 密钥。</param>
    /// <returns>认证密文。</returns>
    private static async Task<byte[]> EncryptAsync(byte[] plaintext, byte[] key)
    {
        using var input = new MemoryStream(plaintext);
        using var output = new MemoryStream();
        await AesGcmStreamEncryption.EncryptAsync(input, output, key, new AesGcmStreamOptions { BlockSize = 4096 });
        return output.ToArray();
    }

    /// <summary>
    /// 使用指定密钥解密测试密文。
    /// </summary>
    /// <param name="ciphertext">认证密文。</param>
    /// <param name="key">AES-256 密钥。</param>
    /// <returns>表示异步解密操作的任务。</returns>
    private static async Task DecryptAsync(byte[] ciphertext, byte[] key)
    {
        using var input = new MemoryStream(ciphertext);
        using var output = new MemoryStream();
        await AesGcmStreamEncryption.DecryptAsync(input, output, key);
    }

    /// <summary>
    /// 表示不支持定位的只读内存流。
    /// </summary>
    private sealed class NonSeekableReadStream : Stream
    {
        /// <summary>
        /// 内部只读流。
        /// </summary>
        private readonly MemoryStream _stream;

        /// <summary>
        /// 单次读取允许返回的最大字节数。
        /// </summary>
        private readonly int _maximumReadLength;

        /// <summary>
        /// 初始化不支持定位的只读内存流。
        /// </summary>
        /// <param name="data">只读数据。</param>
        public NonSeekableReadStream(byte[] data, int maximumReadLength = int.MaxValue)
        {
            if (maximumReadLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumReadLength));
            _stream = new MemoryStream(data, writable: false);
            _maximumReadLength = maximumReadLength;
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, Math.Min(count, _maximumReadLength));

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _stream.ReadAsync(buffer, offset, Math.Min(count, _maximumReadLength), cancellationToken);

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _stream.Dispose();
            base.Dispose(disposing);
        }
    }
}
