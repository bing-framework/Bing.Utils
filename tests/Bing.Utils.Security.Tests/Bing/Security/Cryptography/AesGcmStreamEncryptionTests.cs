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
    /// 验证空流、多块数据及不可定位流均可往返，且调用方流保持打开。
    /// </summary>
    /// <param name="length">要往返处理的输入数据长度（字节）。</param>
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
    /// 验证加密使用 BSS2 格式，且解密拒绝 BSS1 格式。
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
    /// 验证重复使用主密钥加密时，每个 BSS2 文件具有不同盐和 Nonce 前缀。
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
    /// 验证加密和解密均能处理短读流。
    /// </summary>
    /// <param name="maximumReadLength">每次读取最多返回的字节数。</param>
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
    /// 验证篡改、截断或删除密文数据时拒绝解密。
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
    /// 验证解密拒绝重排、重复、插入记录以及终止记录异常的密文。
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
    /// 验证格式头、盐、Nonce 前缀及终止记录的认证，并拒绝无效块长度和尾随数据。
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
    /// 验证解密拒绝不支持的格式版本、算法和密钥派生函数。
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
    /// 验证后续记录认证失败前，已认证的数据块仍会写入目标流。
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
    /// 验证文件加密成功后替换目标文件，取消时保留原目标内容。
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
    /// 验证错误密钥和取消操作均不会写出明文。
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
    /// 使用指定 AES 密钥加密测试明文。
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
    /// 使用指定 AES 密钥解密测试密文。
    /// </summary>
    /// <param name="ciphertext">认证密文。</param>
    /// <param name="key">AES-256 密钥。</param>
    private static async Task DecryptAsync(byte[] ciphertext, byte[] key)
    {
        using var input = new MemoryStream(ciphertext);
        using var output = new MemoryStream();
        await AesGcmStreamEncryption.DecryptAsync(input, output, key);
    }

    /// <summary>
    /// BSS2 固定格式头的长度，单位为字节。
    /// </summary>
    private const int HeaderLength = 52;

    /// <summary>
    /// 获取 BSS2 密文中终止记录的起始偏移量。
    /// </summary>
    /// <param name="ciphertext">包含格式头及记录的 BSS2 密文。</param>
    /// <returns>终止记录在密文中的字节偏移量。</returns>
    private static int GetFinalRecordOffset(byte[] ciphertext)
    {
        var offset = HeaderLength;
        while (ciphertext[offset] == 1)
            offset += GetRecord(ciphertext, offset).Length;
        return offset;
    }

    /// <summary>
    /// 从密文指定偏移处提取一条完整记录。
    /// </summary>
    /// <param name="ciphertext">包含记录的密文。</param>
    /// <param name="offset">记录起始位置的字节偏移量。</param>
    /// <returns>记录类型字节、记录头、数据和认证标签组成的字节序列。</returns>
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

    /// <summary>
    /// 按给定顺序连接多个内存片段。
    /// </summary>
    /// <param name="parts">要连接的内存片段。</param>
    /// <returns>按输入顺序拼接后的字节数组。</returns>
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
        /// 初始化一个 <see cref="NonSeekableReadStream"/> 类型的实例。
        /// </summary>
        /// <param name="data">作为流内容的数据。</param>
        /// <param name="maximumReadLength">每次读取允许返回的最大字节数，必须大于零。</param>
        /// <remarks>省略此参数时单次读取不额外受限。</remarks>
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
