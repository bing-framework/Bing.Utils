using System;
using System.IO;
using System.Linq;
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
    /// 测试目的：新写入流必须使用 BSS2，且默认解密入口必须拒绝已删除兼容支持的 BSS1 流。
    /// </summary>
    [Fact]
    public async Task DecryptAsync_WhenStreamUsesBss1_ShouldRejectUnsupportedFormat()
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
    /// 测试目的：BSS2 必须拒绝重排、重复、插入数据记录，以及缺少或重复终止记录的密文。
    /// </summary>
    [Fact]
    public async Task DecryptAsync_WhenRecordsAreReorderedDuplicatedOrTerminatedIncorrectly_ShouldRejectStream()
    {
        // Arrange
        var key = SecurityRandom.GetBytes(32);
        var ciphertext = await EncryptAsync(SecurityRandom.GetBytes(9000), key);
        var first = GetRecord(ciphertext, HeaderLength);
        var second = GetRecord(ciphertext, HeaderLength + first.Length);
        var final = GetRecord(ciphertext, HeaderLength + first.Length + second.Length + GetRecord(ciphertext, HeaderLength + first.Length + second.Length).Length);
        var reordered = Combine(ciphertext.AsMemory(0, HeaderLength), second, first, ciphertext.AsMemory(HeaderLength + first.Length + second.Length));
        var duplicated = Combine(ciphertext.AsMemory(0, HeaderLength + first.Length), first, ciphertext.AsMemory(HeaderLength + first.Length));
        var inserted = Combine(ciphertext.AsMemory(0, HeaderLength), new byte[] { 9 }, ciphertext.AsMemory(HeaderLength));
        var missingFinal = ciphertext[..^final.Length];
        var duplicateFinal = Combine(ciphertext, final);

        // Act
        var reorderedAction = new Func<Task>(() => DecryptAsync(reordered, key));
        var duplicatedAction = new Func<Task>(() => DecryptAsync(duplicated, key));
        var insertedAction = new Func<Task>(() => DecryptAsync(inserted, key));
        var missingFinalAction = new Func<Task>(() => DecryptAsync(missingFinal, key));
        var duplicateFinalAction = new Func<Task>(() => DecryptAsync(duplicateFinal, key));

        // Assert
        await Should.ThrowAsync<CryptographicException>(reorderedAction);
        await Should.ThrowAsync<CryptographicException>(duplicatedAction);
        await Should.ThrowAsync<InvalidDataException>(insertedAction);
        await Should.ThrowAsync<InvalidDataException>(missingFinalAction);
        await Should.ThrowAsync<InvalidDataException>(duplicateFinalAction);
    }

    /// <summary>
    /// 测试目的：BSS2 必须认证终止记录、格式头、盐和 Nonce 前缀，并拒绝无效块长度与尾随数据。
    /// </summary>
    [Fact]
    public async Task DecryptAsync_WhenProtocolFieldsAreTampered_ShouldRejectStream()
    {
        // Arrange
        var key = SecurityRandom.GetBytes(32);
        var ciphertext = await EncryptAsync(SecurityRandom.GetBytes(9000), key);
        var finalOffset = GetFinalRecordOffset(ciphertext);
        var invalidLength = (byte[])ciphertext.Clone();
        Array.Clear(invalidLength, HeaderLength + 5, 4);
        var finalCount = (byte[])ciphertext.Clone();
        finalCount[finalOffset + 1] ^= 1;
        var finalLength = (byte[])ciphertext.Clone();
        finalLength[finalOffset + 5] ^= 1;
        var salt = (byte[])ciphertext.Clone();
        salt[12] ^= 1;
        var noncePrefix = (byte[])ciphertext.Clone();
        noncePrefix[44] ^= 1;
        var trailing = Combine(ciphertext, new byte[] { 0 });

        // Act
        var invalidLengthAction = new Func<Task>(() => DecryptAsync(invalidLength, key));
        var finalCountAction = new Func<Task>(() => DecryptAsync(finalCount, key));
        var finalLengthAction = new Func<Task>(() => DecryptAsync(finalLength, key));
        var saltAction = new Func<Task>(() => DecryptAsync(salt, key));
        var noncePrefixAction = new Func<Task>(() => DecryptAsync(noncePrefix, key));
        var trailingAction = new Func<Task>(() => DecryptAsync(trailing, key));

        // Assert
        await Should.ThrowAsync<CryptographicException>(invalidLengthAction);
        await Should.ThrowAsync<CryptographicException>(finalCountAction);
        await Should.ThrowAsync<CryptographicException>(finalLengthAction);
        await Should.ThrowAsync<CryptographicException>(saltAction);
        await Should.ThrowAsync<CryptographicException>(noncePrefixAction);
        await Should.ThrowAsync<InvalidDataException>(trailingAction);
    }

    /// <summary>
    /// 测试目的：BSS2 必须拒绝不支持的版本、算法和密钥派生函数。
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task DecryptAsync_WhenHeaderUsesUnsupportedProtocolIdentifier_ShouldThrowNotSupportedException(int offset)
    {
        // Arrange
        var key = SecurityRandom.GetBytes(32);
        var ciphertext = await EncryptAsync(new byte[] { 1 }, key);
        ciphertext[offset]++;

        // Act
        var action = new Func<Task>(() => DecryptAsync(ciphertext, key));

        // Assert
        await Should.ThrowAsync<NotSupportedException>(action);
    }

    /// <summary>
    /// 测试目的：流 API 允许在后续认证失败前写出已经独立认证的数据块。
    /// </summary>
    [Fact]
    public async Task DecryptAsync_WhenLaterRecordAuthenticationFails_ShouldOnlyExposeAuthenticatedPrefix()
    {
        // Arrange
        var key = SecurityRandom.GetBytes(32);
        var source = SecurityRandom.GetBytes(9000);
        var ciphertext = await EncryptAsync(source, key);
        var first = GetRecord(ciphertext, HeaderLength);
        var tampered = (byte[])ciphertext.Clone();
        tampered[HeaderLength + first.Length + 9] ^= 1;
        using var input = new MemoryStream(tampered);
        using var output = new MemoryStream();

        // Act
        var action = new Func<Task>(() => AesGcmStreamEncryption.DecryptAsync(input, output, key));

        // Assert
        await Should.ThrowAsync<CryptographicException>(action);
        output.ToArray().ShouldBe(source[..4096]);
    }

    /// <summary>
    /// 测试目的：文件加密仅在全部成功后替换目标文件，取消时不得破坏已有目标文件。
    /// </summary>
    [Fact]
    public async Task EncryptFileAsync_WhenSuccessfulOrCancelled_ShouldReplaceDestinationAtomically()
    {
        // Arrange
        var directory = Path.Combine(Path.GetTempPath(), "Bing.Utils.Security.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        var sourcePath = Path.Combine(directory, "source.bin");
        var destinationPath = Path.Combine(directory, "destination.bss2");
        var source = SecurityRandom.GetBytes(9000);
        var existing = new byte[] { 7, 8, 9 };
        var key = SecurityRandom.GetBytes(32);
        await File.WriteAllBytesAsync(sourcePath, source);
        await File.WriteAllBytesAsync(destinationPath, existing);

        try
        {
            // Act
            await AesGcmStreamEncryption.EncryptFileAsync(sourcePath, destinationPath, key, new AesGcmStreamOptions { BlockSize = 4096 });
            var encrypted = await File.ReadAllBytesAsync(destinationPath);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var cancelledAction = new Func<Task>(() => AesGcmStreamEncryption.EncryptFileAsync(sourcePath, destinationPath, key, cancellationToken: cancellation.Token));

            // Assert
            encrypted.AsSpan(0, 4).SequenceEqual(new byte[] { (byte)'B', (byte)'S', (byte)'S', (byte)'2' }).ShouldBeTrue();
            await Should.ThrowAsync<OperationCanceledException>(cancelledAction);
            (await File.ReadAllBytesAsync(destinationPath)).ShouldBe(encrypted);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
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

    private const int HeaderLength = 52;

    private static int GetFinalRecordOffset(byte[] ciphertext)
    {
        var offset = HeaderLength;
        while (ciphertext[offset] == 1)
            offset += GetRecord(ciphertext, offset).Length;
        return offset;
    }

    private static byte[] GetRecord(byte[] ciphertext, int offset)
    {
        var recordType = ciphertext[offset];
        if (recordType == 1)
        {
            var length = (ciphertext[offset + 5] << 24) | (ciphertext[offset + 6] << 16) | (ciphertext[offset + 7] << 8) | ciphertext[offset + 8];
            return ciphertext.AsSpan(offset, 1 + 8 + length + 16).ToArray();
        }
        if (recordType == 2)
            return ciphertext.AsSpan(offset, 1 + 12 + 16).ToArray();
        throw new InvalidDataException("测试密文包含未知记录类型。 ");
    }

    private static byte[] Combine(params ReadOnlyMemory<byte>[] parts)
    {
        var length = parts.Sum(part => part.Length);
        var result = new byte[length];
        var offset = 0;
        foreach (var part in parts)
        {
            part.CopyTo(result.AsMemory(offset));
            offset += part.Length;
        }
        return result;
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
