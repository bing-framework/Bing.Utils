#if NET6_0_OR_GREATER
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using Bing.Security.Keys;
using Bing.Security.Randomness;

namespace Bing.Security.Cryptography;

/// <summary>
/// 实现 BSS2 分块 AES-GCM 认证流格式。
/// </summary>
internal static class AesGcmStreamV2
{
    /// <summary>
    /// BSS2 格式版本。
    /// </summary>
    private const byte Version = 2;

    /// <summary>
    /// AES-256-GCM 算法标识。
    /// </summary>
    private const byte AlgorithmAes256Gcm = 1;

    /// <summary>
    /// HKDF-SHA256 密钥派生标识。
    /// </summary>
    private const byte KdfHkdfSha256 = 1;

    /// <summary>
    /// 数据块记录类型。
    /// </summary>
    private const byte DataRecordType = 1;

    /// <summary>
    /// 认证终止记录类型。
    /// </summary>
    private const byte FinalRecordType = 2;

    /// <summary>
    /// BSS2 固定头长度。
    /// </summary>
    private const int HeaderLength = 52;

    /// <summary>
    /// 文件随机盐长度，单位为字节。
    /// </summary>
    private const int FileSaltLength = 32;

    /// <summary>
    /// 文件随机 Nonce 前缀长度，单位为字节。
    /// </summary>
    private const int NoncePrefixLength = 8;

    /// <summary>
    /// 数据记录头长度，包含 32 位块序号和明文长度。
    /// </summary>
    private const int DataRecordHeaderLength = 8;

    /// <summary>
    /// 认证终止记录数据长度，包含块数量和明文总长度。
    /// </summary>
    private const int FinalRecordDataLength = 12;

    /// <summary>
    /// 块认证数据的固定长度，不含终止记录数据。
    /// </summary>
    private const int AuthenticationDataLength = 43;

    /// <summary>
    /// 每文件 HKDF 派生所使用的固定用途上下文。
    /// </summary>
    private static readonly byte[] KeyDerivationInfo = System.Text.Encoding.ASCII.GetBytes("Bing.Utils.Security/AesGcmStream/v2");

    /// <summary>
    /// 使用 BSS2 格式加密输入流，不关闭调用方提供的流。
    /// </summary>
    /// <param name="plaintext">明文输入流。</param>
    /// <param name="ciphertext">认证密文输出流。</param>
    /// <param name="masterKey">32 字节 AES-256 主密钥。</param>
    /// <param name="blockSize">每个认证块的最大明文字节数。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步加密操作的任务。</returns>
    internal static async Task EncryptAsync(Stream plaintext, Stream ciphertext, ReadOnlyMemory<byte> masterKey, int blockSize, CancellationToken cancellationToken)
    {
        var fileSalt = SecurityRandom.GetBytes(FileSaltLength);
        var noncePrefix = SecurityRandom.GetBytes(NoncePrefixLength);
        var header = CreateHeader(blockSize, fileSalt, noncePrefix);
        var headerDigest = ComputeHeaderDigest(header);
        var fileKey = Hkdf.DeriveKeySha256(masterKey.Span, fileSalt, KeyDerivationInfo, 32);
        var plaintextBuffer = ArrayPool<byte>.Shared.Rent(blockSize);
        var ciphertextBuffer = ArrayPool<byte>.Shared.Rent(blockSize);
        var nonce = new byte[AesGcmPayload.NonceSize];
        var authenticationData = new byte[AuthenticationDataLength + FinalRecordDataLength];
        var tag = new byte[AesGcmPayload.TagSize];
        var recordHeader = new byte[DataRecordHeaderLength];
        var recordType = new byte[1];
        try
        {
            await ciphertext.WriteAsync(header, 0, header.Length, cancellationToken).ConfigureAwait(false);
            using var aes = CreateAesGcm(fileKey);
            uint index = 0;
            ulong totalLength = 0;
            while (true)
            {
                var length = await ReadChunkAsync(plaintext, plaintextBuffer, blockSize, cancellationToken).ConfigureAwait(false);
                if (length == 0)
                    break;

                CreateNonce(noncePrefix, index, nonce);
                var aadLength = WriteAuthenticationData(authenticationData, headerDigest, DataRecordType, index, (uint)length, false, ReadOnlySpan<byte>.Empty);
                aes.Encrypt(nonce, plaintextBuffer.AsSpan(0, length), ciphertextBuffer.AsSpan(0, length), tag, authenticationData.AsSpan(0, aadLength));
                BinaryPrimitives.WriteUInt32BigEndian(recordHeader.AsSpan(0, 4), index);
                BinaryPrimitives.WriteUInt32BigEndian(recordHeader.AsSpan(4, 4), (uint)length);
                await WriteByteAsync(ciphertext, DataRecordType, recordType, cancellationToken).ConfigureAwait(false);
                await ciphertext.WriteAsync(recordHeader, 0, recordHeader.Length, cancellationToken).ConfigureAwait(false);
                await ciphertext.WriteAsync(ciphertextBuffer, 0, length, cancellationToken).ConfigureAwait(false);
                await ciphertext.WriteAsync(tag, 0, tag.Length, cancellationToken).ConfigureAwait(false);
                checked { totalLength += (uint)length; }
                if (index == uint.MaxValue - 1)
                    throw new InvalidOperationException("BSS2 认证流已达到最大数据块数量，无法继续加密。 ");
                index++;
            }

            var finalData = new byte[FinalRecordDataLength];
            try
            {
                BinaryPrimitives.WriteUInt32BigEndian(finalData.AsSpan(0, 4), index);
                BinaryPrimitives.WriteUInt64BigEndian(finalData.AsSpan(4, 8), totalLength);
                CreateNonce(noncePrefix, uint.MaxValue, nonce);
                var aadLength = WriteAuthenticationData(authenticationData, headerDigest, FinalRecordType, uint.MaxValue, 0, true, finalData);
                aes.Encrypt(nonce, ReadOnlySpan<byte>.Empty, Span<byte>.Empty, tag, authenticationData.AsSpan(0, aadLength));
                await WriteByteAsync(ciphertext, FinalRecordType, recordType, cancellationToken).ConfigureAwait(false);
                await ciphertext.WriteAsync(finalData, 0, finalData.Length, cancellationToken).ConfigureAwait(false);
                await ciphertext.WriteAsync(tag, 0, tag.Length, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CryptographicOperationsCompat.ZeroMemory(finalData);
            }
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(fileSalt);
            CryptographicOperationsCompat.ZeroMemory(noncePrefix);
            CryptographicOperationsCompat.ZeroMemory(header);
            CryptographicOperationsCompat.ZeroMemory(headerDigest);
            CryptographicOperationsCompat.ZeroMemory(fileKey);
            CryptographicOperationsCompat.ZeroMemory(plaintextBuffer);
            CryptographicOperationsCompat.ZeroMemory(ciphertextBuffer);
            CryptographicOperationsCompat.ZeroMemory(nonce);
            CryptographicOperationsCompat.ZeroMemory(authenticationData);
            CryptographicOperationsCompat.ZeroMemory(tag);
            CryptographicOperationsCompat.ZeroMemory(recordHeader);
            CryptographicOperationsCompat.ZeroMemory(recordType);
            ArrayPool<byte>.Shared.Return(plaintextBuffer);
            ArrayPool<byte>.Shared.Return(ciphertextBuffer);
        }
    }

    /// <summary>
    /// 验证 BSS2 格式密文并以流式方式写出已认证块，不关闭调用方提供的流。
    /// </summary>
    /// <param name="ciphertext">认证密文输入流，已消费四字节 BSS2 魔数。</param>
    /// <param name="plaintext">已认证明文输出流。</param>
    /// <param name="masterKey">32 字节 AES-256 主密钥。</param>
    /// <param name="magic">已读取的四字节魔数。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步解密操作的任务。</returns>
    internal static async Task DecryptAsync(Stream ciphertext, Stream plaintext, ReadOnlyMemory<byte> masterKey, byte[] magic, CancellationToken cancellationToken)
    {
        var header = new byte[HeaderLength];
        Buffer.BlockCopy(magic, 0, header, 0, magic.Length);
        byte[] fileSalt = null;
        byte[] noncePrefix = null;
        byte[] headerDigest = null;
        byte[] fileKey = null;
        byte[] ciphertextBuffer = null;
        byte[] plaintextBuffer = null;
        try
        {
            await ReadExactlyAsync(ciphertext, header, magic.Length, HeaderLength - magic.Length, cancellationToken).ConfigureAwait(false);
            var blockSize = ParseHeader(header, out fileSalt, out noncePrefix);
            headerDigest = ComputeHeaderDigest(header);
            fileKey = Hkdf.DeriveKeySha256(masterKey.Span, fileSalt, KeyDerivationInfo, 32);
            ciphertextBuffer = ArrayPool<byte>.Shared.Rent(blockSize);
            plaintextBuffer = ArrayPool<byte>.Shared.Rent(blockSize);
            var nonce = new byte[AesGcmPayload.NonceSize];
            var authenticationData = new byte[AuthenticationDataLength + FinalRecordDataLength];
            var tag = new byte[AesGcmPayload.TagSize];
            var recordHeader = new byte[DataRecordHeaderLength];
            var recordTypeBuffer = new byte[1];
            try
            {
                using var aes = CreateAesGcm(fileKey);
                uint expectedIndex = 0;
                ulong totalLength = 0;
                while (true)
                {
                    var recordType = await ReadByteAsync(ciphertext, recordTypeBuffer, cancellationToken).ConfigureAwait(false);
                    if (recordType == DataRecordType)
                    {
                        await ReadExactlyAsync(ciphertext, recordHeader, 0, recordHeader.Length, cancellationToken).ConfigureAwait(false);
                        var index = BinaryPrimitives.ReadUInt32BigEndian(recordHeader.AsSpan(0, 4));
                        var length = BinaryPrimitives.ReadUInt32BigEndian(recordHeader.AsSpan(4, 4));
                        if (index != expectedIndex || length == 0 || length > blockSize || index == uint.MaxValue)
                            throw new CryptographicException("BSS2 数据块的序号或长度无效。 ");
                        await ReadExactlyAsync(ciphertext, ciphertextBuffer, 0, (int)length, cancellationToken).ConfigureAwait(false);
                        await ReadExactlyAsync(ciphertext, tag, 0, tag.Length, cancellationToken).ConfigureAwait(false);
                        CreateNonce(noncePrefix, index, nonce);
                        var aadLength = WriteAuthenticationData(authenticationData, headerDigest, DataRecordType, index, length, false, ReadOnlySpan<byte>.Empty);
                        try
                        {
                            aes.Decrypt(nonce, ciphertextBuffer.AsSpan(0, (int)length), tag, plaintextBuffer.AsSpan(0, (int)length), authenticationData.AsSpan(0, aadLength));
                            await plaintext.WriteAsync(plaintextBuffer, 0, (int)length, cancellationToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            CryptographicOperationsCompat.ZeroMemory(plaintextBuffer.AsSpan(0, (int)length));
                        }

                        checked { totalLength += length; }
                        if (expectedIndex == uint.MaxValue - 1)
                            throw new CryptographicException("BSS2 数据块数量超过格式上限。 ");
                        expectedIndex++;
                        continue;
                    }

                    if (recordType != FinalRecordType)
                        throw new InvalidDataException("BSS2 认证流包含未知记录类型。 ");

                    var finalData = new byte[FinalRecordDataLength];
                    try
                    {
                        await ReadExactlyAsync(ciphertext, finalData, 0, finalData.Length, cancellationToken).ConfigureAwait(false);
                        await ReadExactlyAsync(ciphertext, tag, 0, tag.Length, cancellationToken).ConfigureAwait(false);
                        var finalCount = BinaryPrimitives.ReadUInt32BigEndian(finalData.AsSpan(0, 4));
                        var finalLength = BinaryPrimitives.ReadUInt64BigEndian(finalData.AsSpan(4, 8));
                        if (finalCount != expectedIndex || finalLength != totalLength)
                            throw new CryptographicException("BSS2 认证终止记录中的长度或块数量无效。 ");
                        CreateNonce(noncePrefix, uint.MaxValue, nonce);
                        var aadLength = WriteAuthenticationData(authenticationData, headerDigest, FinalRecordType, uint.MaxValue, 0, true, finalData);
                        aes.Decrypt(nonce, ReadOnlySpan<byte>.Empty, tag, Span<byte>.Empty, authenticationData.AsSpan(0, aadLength));
                    }
                    finally
                    {
                        CryptographicOperationsCompat.ZeroMemory(finalData);
                    }

                    if (await HasTrailingDataAsync(ciphertext, recordTypeBuffer, cancellationToken).ConfigureAwait(false))
                        throw new InvalidDataException("BSS2 认证流在终止记录后包含尾随数据。 ");
                    return;
                }
            }
            finally
            {
                CryptographicOperationsCompat.ZeroMemory(nonce);
                CryptographicOperationsCompat.ZeroMemory(authenticationData);
                CryptographicOperationsCompat.ZeroMemory(tag);
                CryptographicOperationsCompat.ZeroMemory(recordHeader);
                CryptographicOperationsCompat.ZeroMemory(recordTypeBuffer);
            }
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(header);
            if (fileSalt != null)
                CryptographicOperationsCompat.ZeroMemory(fileSalt);
            if (noncePrefix != null)
                CryptographicOperationsCompat.ZeroMemory(noncePrefix);
            if (headerDigest != null)
                CryptographicOperationsCompat.ZeroMemory(headerDigest);
            if (fileKey != null)
                CryptographicOperationsCompat.ZeroMemory(fileKey);
            if (ciphertextBuffer != null)
            {
                CryptographicOperationsCompat.ZeroMemory(ciphertextBuffer);
                ArrayPool<byte>.Shared.Return(ciphertextBuffer);
            }
            if (plaintextBuffer != null)
            {
                CryptographicOperationsCompat.ZeroMemory(plaintextBuffer);
                ArrayPool<byte>.Shared.Return(plaintextBuffer);
            }
        }
    }

    /// <summary>
    /// 创建 BSS2 固定头。
    /// </summary>
    /// <param name="blockSize">块大小。</param>
    /// <param name="fileSalt">每文件随机盐。</param>
    /// <param name="noncePrefix">每文件随机 Nonce 前缀。</param>
    /// <returns>BSS2 固定头。</returns>
    private static byte[] CreateHeader(int blockSize, byte[] fileSalt, byte[] noncePrefix)
    {
        var header = new byte[HeaderLength];
        header[0] = (byte)'B';
        header[1] = (byte)'S';
        header[2] = (byte)'S';
        header[3] = (byte)'2';
        header[4] = Version;
        header[5] = AlgorithmAes256Gcm;
        header[6] = KdfHkdfSha256;
        header[7] = 0;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8, 4), (uint)blockSize);
        fileSalt.CopyTo(header, 12);
        noncePrefix.CopyTo(header, 12 + FileSaltLength);
        return header;
    }

    /// <summary>
    /// 解析并验证 BSS2 固定头。
    /// </summary>
    /// <param name="header">BSS2 固定头。</param>
    /// <param name="fileSalt">解析出的文件盐。</param>
    /// <param name="noncePrefix">解析出的 Nonce 前缀。</param>
    /// <returns>已验证的块大小。</returns>
    /// <exception cref="NotSupportedException">版本、算法或 KDF 不受支持时抛出。</exception>
    /// <exception cref="InvalidDataException">格式头或块大小无效时抛出。</exception>
    private static int ParseHeader(byte[] header, out byte[] fileSalt, out byte[] noncePrefix)
    {
        fileSalt = null;
        noncePrefix = null;
        if (header[0] != 'B' || header[1] != 'S' || header[2] != 'S' || header[3] != '2')
            throw new InvalidDataException("认证流不是 BSS2 格式。 ");
        if (header[4] != Version || header[5] != AlgorithmAes256Gcm || header[6] != KdfHkdfSha256)
            throw new NotSupportedException("BSS2 认证流使用了不受支持的版本、算法或密钥派生函数。 ");
        if (header[7] != 0)
            throw new InvalidDataException("BSS2 认证流保留字段无效。 ");
        var blockSize = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(8, 4));
        if (blockSize < 4096 || blockSize > 1048576)
            throw new InvalidDataException("BSS2 认证流块大小无效。 ");
        fileSalt = header.AsSpan(12, FileSaltLength).ToArray();
        noncePrefix = header.AsSpan(12 + FileSaltLength, NoncePrefixLength).ToArray();
        return (int)blockSize;
    }

    /// <summary>
    /// 计算固定头的 SHA-256 摘要。
    /// </summary>
    /// <param name="header">固定头。</param>
    /// <returns>32 字节摘要。</returns>
    private static byte[] ComputeHeaderDigest(byte[] header)
    {
#if NET8_0_OR_GREATER
        return SHA256.HashData(header);
#else
        using var hash = SHA256.Create();
        return hash.ComputeHash(header);
#endif
    }

    /// <summary>
    /// 使用文件前缀和块索引构建 12 字节唯一 Nonce。
    /// </summary>
    /// <param name="noncePrefix">8 字节文件随机前缀。</param>
    /// <param name="index">32 位块索引。</param>
    /// <param name="destination">12 字节 Nonce 缓冲区。</param>
    private static void CreateNonce(byte[] noncePrefix, uint index, byte[] destination)
    {
        noncePrefix.CopyTo(destination, 0);
        BinaryPrimitives.WriteUInt32BigEndian(destination.AsSpan(NoncePrefixLength, 4), index);
    }

    /// <summary>
    /// 写入块认证数据。
    /// </summary>
    /// <param name="destination">认证数据缓冲区。</param>
    /// <param name="headerDigest">固定头摘要。</param>
    /// <param name="recordType">记录类型。</param>
    /// <param name="index">块索引。</param>
    /// <param name="length">明文长度。</param>
    /// <param name="isFinal">是否为终止记录。</param>
    /// <param name="extra">终止记录绑定数据。</param>
    /// <returns>实际认证数据长度。</returns>
    private static int WriteAuthenticationData(byte[] destination, byte[] headerDigest, byte recordType, uint index, uint length, bool isFinal, ReadOnlySpan<byte> extra)
    {
        headerDigest.CopyTo(destination, 0);
        destination[32] = Version;
        destination[33] = recordType;
        BinaryPrimitives.WriteUInt32BigEndian(destination.AsSpan(34, 4), index);
        BinaryPrimitives.WriteUInt32BigEndian(destination.AsSpan(38, 4), length);
        destination[42] = isFinal ? (byte)1 : (byte)0;
        extra.CopyTo(destination.AsSpan(AuthenticationDataLength));
        return AuthenticationDataLength + extra.Length;
    }

    /// <summary>
    /// 循环读取数据，直到填满一个块或到达输入结尾。
    /// </summary>
    /// <param name="source">输入流。</param>
    /// <param name="buffer">块缓冲区。</param>
    /// <param name="blockSize">块大小。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>读取的字节数；输入结尾时为 0。</returns>
    private static async Task<int> ReadChunkAsync(Stream source, byte[] buffer, int blockSize, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < blockSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = await source.ReadAsync(buffer, offset, blockSize - offset, cancellationToken).ConfigureAwait(false);
            if (count == 0)
                return offset;
            offset += count;
        }
        return offset;
    }

    /// <summary>
    /// 从输入流读取固定长度数据。
    /// </summary>
    /// <param name="source">输入流。</param>
    /// <param name="buffer">目标缓冲区。</param>
    /// <param name="offset">目标偏移量。</param>
    /// <param name="count">需要读取的字节数。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步读取操作的任务。</returns>
    /// <exception cref="InvalidDataException">输入流在读取完成前结束时抛出。</exception>
    private static async Task ReadExactlyAsync(Stream source, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var remaining = count;
        while (remaining > 0)
        {
            var read = await source.ReadAsync(buffer, offset, remaining, cancellationToken).ConfigureAwait(false);
            if (read == 0)
                throw new InvalidDataException("BSS2 认证流已截断。 ");
            offset += read;
            remaining -= read;
        }
    }

    /// <summary>
    /// 读取单个记录类型字节。
    /// </summary>
    /// <param name="source">输入流。</param>
    /// <param name="buffer">复用的一字节读取缓冲区。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>记录类型。</returns>
    private static async Task<byte> ReadByteAsync(Stream source, byte[] buffer, CancellationToken cancellationToken)
    {
        await ReadExactlyAsync(source, buffer, 0, 1, cancellationToken).ConfigureAwait(false);
        return buffer[0];
    }

    /// <summary>
    /// 写入单个记录类型字节。
    /// </summary>
    /// <param name="destination">输出流。</param>
    /// <param name="value">要写入的值。</param>
    /// <param name="buffer">复用的一字节写入缓冲区。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步写入操作的任务。</returns>
    private static async Task WriteByteAsync(Stream destination, byte value, byte[] buffer, CancellationToken cancellationToken)
    {
        buffer[0] = value;
        await destination.WriteAsync(buffer, 0, 1, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 检查终止记录后是否存在尾随字节。
    /// </summary>
    /// <param name="source">输入流。</param>
    /// <param name="buffer">复用的一字节读取缓冲区。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>存在尾随数据时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    private static async Task<bool> HasTrailingDataAsync(Stream source, byte[] buffer, CancellationToken cancellationToken)
    {
        return await source.ReadAsync(buffer, 0, 1, cancellationToken).ConfigureAwait(false) != 0;
    }

    /// <summary>
    /// 创建标签长度固定的 AES-GCM 实例。
    /// </summary>
    /// <param name="key">32 字节文件子密钥。</param>
    /// <returns>AES-GCM 实例。</returns>
    private static AesGcm CreateAesGcm(byte[] key)
    {
#if NET8_0_OR_GREATER
        return new AesGcm(key, AesGcmPayload.TagSize);
#else
        return new AesGcm(key);
#endif
    }
}
#endif