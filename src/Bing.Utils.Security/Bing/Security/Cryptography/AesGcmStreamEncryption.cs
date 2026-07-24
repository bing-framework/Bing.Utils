#if NET6_0_OR_GREATER
using System.Buffers.Binary;
using System.Security.Cryptography;
using Bing.Security.Randomness;

namespace Bing.Security.Cryptography;

/// <summary>
/// 提供使用分块 AES-GCM 认证格式的异步流加密和解密操作。
/// </summary>
public static class AesGcmStreamEncryption
{
    /// <summary>
    /// 当前认证流格式版本。
    /// </summary>
    private const byte CurrentVersion = 1;

    /// <summary>
    /// AES-256-GCM 算法标识。
    /// </summary>
    private const byte AlgorithmAes256Gcm = 1;

    /// <summary>
    /// 数据块记录类型。
    /// </summary>
    private const byte DataRecordType = 1;

    /// <summary>
    /// 认证终止记录类型。
    /// </summary>
    private const byte FinalRecordType = 2;

    /// <summary>
    /// 固定头长度，包含魔数、版本、算法、块大小、基础 Nonce 和可选 AAD 摘要长度。
    /// </summary>
    private const int HeaderLength = 23;

    /// <summary>
    /// 使用 AES-256-GCM 将源文件写入认证密文文件。
    /// </summary>
    /// <param name="sourcePath">要加密的源文件路径。</param>
    /// <param name="destinationPath">认证密文文件路径。</param>
    /// <param name="key">32 字节 AES-256 密钥。</param>
    /// <param name="options">分块加密选项，未指定时使用默认块大小。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步文件加密操作的任务。</returns>
    public static async Task EncryptFileAsync(string sourcePath, string destinationPath, ReadOnlyMemory<byte> key, AesGcmStreamOptions options = null, CancellationToken cancellationToken = default)
    {
        ValidateFilePaths(sourcePath, destinationPath);
        using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await EncryptAsync(source, destination, key, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 验证认证密文文件并在全部认证成功后原子替换目标文件。
    /// </summary>
    /// <param name="sourcePath">认证密文源文件路径。</param>
    /// <param name="destinationPath">已认证明文文件路径。</param>
    /// <param name="key">32 字节 AES-256 密钥。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步文件解密操作的任务。</returns>
    public static async Task DecryptFileAsync(string sourcePath, string destinationPath, ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        ValidateFilePaths(sourcePath, destinationPath);
        var destinationDirectory = Path.GetDirectoryName(Path.GetFullPath(destinationPath));
        var temporaryPath = Path.Combine(destinationDirectory, "." + Path.GetRandomFileName());
        try
        {
            using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var temporary = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await DecryptAsync(source, temporary, key, cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, destinationPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    /// <summary>
    /// 使用 AES-256-GCM 将输入流写入分块认证密文流；不会关闭调用方提供的流。
    /// </summary>
    /// <param name="plaintext">要加密的输入流。</param>
    /// <param name="ciphertext">写入认证密文的输出流。</param>
    /// <param name="key">32 字节 AES-256 密钥。</param>
    /// <param name="options">分块加密选项，未指定时使用默认块大小。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步加密操作的任务。</returns>
    /// <exception cref="ArgumentNullException">任一流为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> 不是 32 字节或块大小无效时抛出。</exception>
    public static async Task EncryptAsync(Stream plaintext, Stream ciphertext, ReadOnlyMemory<byte> key, AesGcmStreamOptions options = null, CancellationToken cancellationToken = default)
    {
        if (plaintext == null)
            throw new ArgumentNullException(nameof(plaintext));
        if (ciphertext == null)
            throw new ArgumentNullException(nameof(ciphertext));
        ValidateKey(key.Span);
        options ??= new AesGcmStreamOptions();
        ValidateBlockSize(options.BlockSize);

        var keyBytes = key.ToArray();
        var baseNonce = SecurityRandom.GetBytes(AesGcmPayload.NonceSize);
        var header = CreateHeader(options.BlockSize, baseNonce);
        var headerHash = ComputeHash(header);
        var buffer = new byte[options.BlockSize];
        try
        {
            await ciphertext.WriteAsync(header, 0, header.Length, cancellationToken).ConfigureAwait(false);
            ulong blockIndex = 0;
            ulong totalLength = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = await plaintext.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                if (count == 0)
                    break;

                checked { totalLength += (uint)count; }
                await WriteDataRecordAsync(ciphertext, keyBytes, baseNonce, headerHash, blockIndex, buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                checked { blockIndex++; }
            }

            await WriteFinalRecordAsync(ciphertext, keyBytes, baseNonce, headerHash, blockIndex, totalLength, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(keyBytes);
            CryptographicOperationsCompat.ZeroMemory(baseNonce);
            CryptographicOperationsCompat.ZeroMemory(header);
            CryptographicOperationsCompat.ZeroMemory(headerHash);
            CryptographicOperationsCompat.ZeroMemory(buffer);
        }
    }

    /// <summary>
    /// 验证分块认证密文流并将每个已认证明文块写入输出流；不会关闭调用方提供的流。
    /// </summary>
    /// <param name="ciphertext">认证密文输入流。</param>
    /// <param name="plaintext">写入已认证明文的输出流。</param>
    /// <param name="key">32 字节 AES-256 密钥。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步解密操作的任务。</returns>
    /// <exception cref="ArgumentNullException">任一流为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> 不是 32 字节或格式头无效时抛出。</exception>
    /// <exception cref="CryptographicException">块、顺序、认证终止记录、密钥或流完整性无效时抛出。</exception>
    public static async Task DecryptAsync(Stream ciphertext, Stream plaintext, ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        if (ciphertext == null)
            throw new ArgumentNullException(nameof(ciphertext));
        if (plaintext == null)
            throw new ArgumentNullException(nameof(plaintext));
        ValidateKey(key.Span);

        var keyBytes = key.ToArray();
        var header = new byte[HeaderLength];
        try
        {
            await ReadExactlyAsync(ciphertext, header, cancellationToken).ConfigureAwait(false);
            var blockSize = ValidateAndGetBlockSize(header);
            var baseNonce = header.AsSpan(10, AesGcmPayload.NonceSize).ToArray();
            var headerHash = ComputeHash(header);
            try
            {
                var expectedIndex = 0UL;
                var totalLength = 0UL;
                while (true)
                {
                    var recordType = await ReadRecordTypeAsync(ciphertext, cancellationToken).ConfigureAwait(false);
                    if (recordType == DataRecordType)
                    {
                        var recordHeader = new byte[12];
                        try
                        {
                            await ReadExactlyAsync(ciphertext, recordHeader, cancellationToken).ConfigureAwait(false);
                            var index = BinaryPrimitives.ReadUInt64BigEndian(recordHeader.AsSpan(0, 8));
                            var length = BinaryPrimitives.ReadUInt32BigEndian(recordHeader.AsSpan(8, 4));
                            if (index != expectedIndex || length == 0 || length > blockSize)
                                throw new CryptographicException("认证数据块的顺序或长度无效。 ");

                            var encrypted = new byte[length];
                            var tag = new byte[AesGcmPayload.TagSize];
                            try
                            {
                                await ReadExactlyAsync(ciphertext, encrypted, cancellationToken).ConfigureAwait(false);
                                await ReadExactlyAsync(ciphertext, tag, cancellationToken).ConfigureAwait(false);
                                var decrypted = DecryptRecord(keyBytes, baseNonce, headerHash, DataRecordType, index, length, encrypted, tag);
                                try
                                {
                                    await plaintext.WriteAsync(decrypted, 0, decrypted.Length, cancellationToken).ConfigureAwait(false);
                                }
                                finally
                                {
                                    CryptographicOperationsCompat.ZeroMemory(decrypted);
                                }
                            }
                            finally
                            {
                                CryptographicOperationsCompat.ZeroMemory(encrypted);
                                CryptographicOperationsCompat.ZeroMemory(tag);
                            }

                            checked { totalLength += length; expectedIndex++; }
                            continue;
                        }
                        finally
                        {
                            CryptographicOperationsCompat.ZeroMemory(recordHeader);
                        }
                    }

                    if (recordType != FinalRecordType)
                        throw new CryptographicException("认证流包含未知记录类型。 ");

                    await VerifyFinalRecordAsync(ciphertext, keyBytes, baseNonce, headerHash, expectedIndex, totalLength, cancellationToken).ConfigureAwait(false);
                    if (await ReadTrailingByteAsync(ciphertext, cancellationToken).ConfigureAwait(false))
                        throw new CryptographicException("认证流在终止记录后包含尾随数据。 ");
                    return;
                }
            }
            finally
            {
                CryptographicOperationsCompat.ZeroMemory(baseNonce);
                CryptographicOperationsCompat.ZeroMemory(headerHash);
            }
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(keyBytes);
            CryptographicOperationsCompat.ZeroMemory(header);
        }
    }

    /// <summary>
    /// 创建格式头。
    /// </summary>
    /// <param name="blockSize">块大小。</param>
    /// <param name="baseNonce">随机基础 Nonce。</param>
    /// <returns>认证流格式头。</returns>
    private static byte[] CreateHeader(int blockSize, byte[] baseNonce)
    {
        var header = new byte[HeaderLength];
        header[0] = (byte)'B';
        header[1] = (byte)'S';
        header[2] = (byte)'S';
        header[3] = (byte)'1';
        header[4] = CurrentVersion;
        header[5] = AlgorithmAes256Gcm;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(6, 4), (uint)blockSize);
        baseNonce.CopyTo(header, 10);
        header[22] = 0;
        return header;
    }

    /// <summary>
    /// 验证格式头并返回块大小。
    /// </summary>
    /// <param name="header">认证流格式头。</param>
    /// <returns>已验证的块大小。</returns>
    /// <exception cref="ArgumentException">格式头不受支持时抛出。</exception>
    private static int ValidateAndGetBlockSize(byte[] header)
    {
        if (header[0] != 'B' || header[1] != 'S' || header[2] != 'S' || header[3] != '1' || header[4] != CurrentVersion || header[5] != AlgorithmAes256Gcm || header[22] != 0)
            throw new ArgumentException("认证流格式头不受支持。", nameof(header));
        var blockSize = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(6, 4));
        if (blockSize > int.MaxValue)
            throw new ArgumentException("认证流块大小无效。", nameof(header));
        ValidateBlockSize((int)blockSize);
        return (int)blockSize;
    }

    /// <summary>
    /// 写入单个认证数据块记录。
    /// </summary>
    /// <param name="destination">密文目标流。</param>
    /// <param name="key">AES-256 密钥。</param>
    /// <param name="baseNonce">基础 Nonce。</param>
    /// <param name="headerHash">格式头摘要。</param>
    /// <param name="index">块序号。</param>
    /// <param name="plaintext">块明文。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步写入操作的任务。</returns>
    private static async Task WriteDataRecordAsync(Stream destination, byte[] key, byte[] baseNonce, byte[] headerHash, ulong index, ReadOnlyMemory<byte> plaintext, CancellationToken cancellationToken)
    {
        var recordHeader = new byte[12];
        var encrypted = new byte[plaintext.Length];
        var tag = new byte[AesGcmPayload.TagSize];
        try
        {
            BinaryPrimitives.WriteUInt64BigEndian(recordHeader.AsSpan(0, 8), index);
            BinaryPrimitives.WriteUInt32BigEndian(recordHeader.AsSpan(8, 4), (uint)plaintext.Length);
            EncryptRecord(key, baseNonce, headerHash, DataRecordType, index, (uint)plaintext.Length, plaintext.Span, encrypted, tag);
            await destination.WriteAsync(new[] { DataRecordType }, 0, 1, cancellationToken).ConfigureAwait(false);
            await destination.WriteAsync(recordHeader, 0, recordHeader.Length, cancellationToken).ConfigureAwait(false);
            await destination.WriteAsync(encrypted, 0, encrypted.Length, cancellationToken).ConfigureAwait(false);
            await destination.WriteAsync(tag, 0, tag.Length, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(recordHeader);
            CryptographicOperationsCompat.ZeroMemory(encrypted);
            CryptographicOperationsCompat.ZeroMemory(tag);
        }
    }

    /// <summary>
    /// 写入认证终止记录，绑定块数量和明文总长度。
    /// </summary>
    /// <param name="destination">密文目标流。</param>
    /// <param name="key">AES-256 密钥。</param>
    /// <param name="baseNonce">基础 Nonce。</param>
    /// <param name="headerHash">格式头摘要。</param>
    /// <param name="blockCount">数据块总数。</param>
    /// <param name="totalLength">明文总字节数。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步写入操作的任务。</returns>
    private static async Task WriteFinalRecordAsync(Stream destination, byte[] key, byte[] baseNonce, byte[] headerHash, ulong blockCount, ulong totalLength, CancellationToken cancellationToken)
    {
        var finalData = new byte[16];
        var tag = new byte[AesGcmPayload.TagSize];
        try
        {
            BinaryPrimitives.WriteUInt64BigEndian(finalData.AsSpan(0, 8), blockCount);
            BinaryPrimitives.WriteUInt64BigEndian(finalData.AsSpan(8, 8), totalLength);
            EncryptRecord(key, baseNonce, headerHash, FinalRecordType, ulong.MaxValue, 0, ReadOnlySpan<byte>.Empty, Array.Empty<byte>(), tag, finalData);
            await destination.WriteAsync(new[] { FinalRecordType }, 0, 1, cancellationToken).ConfigureAwait(false);
            await destination.WriteAsync(finalData, 0, finalData.Length, cancellationToken).ConfigureAwait(false);
            await destination.WriteAsync(tag, 0, tag.Length, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(finalData);
            CryptographicOperationsCompat.ZeroMemory(tag);
        }
    }

    /// <summary>
    /// 验证认证终止记录。
    /// </summary>
    /// <param name="source">密文输入流。</param>
    /// <param name="key">AES-256 密钥。</param>
    /// <param name="baseNonce">基础 Nonce。</param>
    /// <param name="headerHash">格式头摘要。</param>
    /// <param name="expectedBlockCount">预期数据块数。</param>
    /// <param name="expectedTotalLength">预期明文总长度。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步验证操作的任务。</returns>
    private static async Task VerifyFinalRecordAsync(Stream source, byte[] key, byte[] baseNonce, byte[] headerHash, ulong expectedBlockCount, ulong expectedTotalLength, CancellationToken cancellationToken)
    {
        var finalData = new byte[16];
        var tag = new byte[AesGcmPayload.TagSize];
        try
        {
            await ReadExactlyAsync(source, finalData, cancellationToken).ConfigureAwait(false);
            await ReadExactlyAsync(source, tag, cancellationToken).ConfigureAwait(false);
            var blockCount = BinaryPrimitives.ReadUInt64BigEndian(finalData.AsSpan(0, 8));
            var totalLength = BinaryPrimitives.ReadUInt64BigEndian(finalData.AsSpan(8, 8));
            if (blockCount != expectedBlockCount || totalLength != expectedTotalLength)
                throw new CryptographicException("认证终止记录中的长度或块数量无效。 ");

            DecryptRecord(key, baseNonce, headerHash, FinalRecordType, ulong.MaxValue, 0, Array.Empty<byte>(), tag, finalData);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(finalData);
            CryptographicOperationsCompat.ZeroMemory(tag);
        }
    }

    /// <summary>
    /// 使用块专属 Nonce 和 AAD 加密记录。
    /// </summary>
    /// <param name="key">AES-256 密钥。</param>
    /// <param name="baseNonce">基础 Nonce。</param>
    /// <param name="headerHash">格式头摘要。</param>
    /// <param name="recordType">记录类型。</param>
    /// <param name="index">块序号。</param>
    /// <param name="length">明文长度。</param>
    /// <param name="plaintext">明文。</param>
    /// <param name="ciphertext">密文目标。</param>
    /// <param name="tag">认证标签目标。</param>
    /// <param name="authenticationData">可选附加认证数据输出缓冲区。</param>
    private static void EncryptRecord(byte[] key, byte[] baseNonce, byte[] headerHash, byte recordType, ulong index, uint length, ReadOnlySpan<byte> plaintext, byte[] ciphertext, byte[] tag, byte[] authenticationData = null)
    {
        var nonce = CreateNonce(baseNonce, index);
        var aad = CreateAuthenticationData(headerHash, recordType, index, length, authenticationData);
        try
        {
            using var aes = CreateAesGcm(key);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(nonce);
            CryptographicOperationsCompat.ZeroMemory(aad);
        }
    }

    /// <summary>
    /// 验证并解密记录。
    /// </summary>
    /// <param name="key">AES-256 密钥。</param>
    /// <param name="baseNonce">基础 Nonce。</param>
    /// <param name="headerHash">格式头摘要。</param>
    /// <param name="recordType">记录类型。</param>
    /// <param name="index">块序号。</param>
    /// <param name="length">明文长度。</param>
    /// <param name="ciphertext">密文。</param>
    /// <param name="tag">认证标签。</param>
    /// <param name="authenticationData">可选附加认证数据。</param>
    /// <returns>认证成功后的明文。</returns>
    private static byte[] DecryptRecord(byte[] key, byte[] baseNonce, byte[] headerHash, byte recordType, ulong index, uint length, byte[] ciphertext, byte[] tag, byte[] authenticationData = null)
    {
        var nonce = CreateNonce(baseNonce, index);
        var aad = CreateAuthenticationData(headerHash, recordType, index, length, authenticationData);
        var plaintext = new byte[ciphertext.Length];
        try
        {
            using var aes = CreateAesGcm(key);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, aad);
            return plaintext;
        }
        catch
        {
            CryptographicOperationsCompat.ZeroMemory(plaintext);
            throw;
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(nonce);
            CryptographicOperationsCompat.ZeroMemory(aad);
        }
    }

    /// <summary>
    /// 构建块唯一 Nonce；前四字节保持随机前缀，后八字节为大端块序号。
    /// </summary>
    /// <param name="baseNonce">随机基础 Nonce。</param>
    /// <param name="index">块序号。</param>
    /// <returns>块唯一 Nonce。</returns>
    private static byte[] CreateNonce(byte[] baseNonce, ulong index)
    {
        var nonce = baseNonce.ToArray();
        BinaryPrimitives.WriteUInt64BigEndian(nonce.AsSpan(4, 8), index);
        return nonce;
    }

    /// <summary>
    /// 构建认证数据，绑定格式头、记录类型、块序号和长度。
    /// </summary>
    /// <param name="headerHash">格式头摘要。</param>
    /// <param name="recordType">记录类型。</param>
    /// <param name="index">块序号。</param>
    /// <param name="length">明文长度。</param>
    /// <param name="extra">可选附加认证数据。</param>
    /// <returns>认证数据字节。</returns>
    private static byte[] CreateAuthenticationData(byte[] headerHash, byte recordType, ulong index, uint length, byte[] extra)
    {
        var result = new byte[45 + (extra?.Length ?? 0)];
        headerHash.CopyTo(result, 0);
        result[32] = recordType;
        BinaryPrimitives.WriteUInt64BigEndian(result.AsSpan(33, 8), index);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(41, 4), length);
        if (extra != null)
            extra.CopyTo(result, 45);
        return result;
    }

    /// <summary>
    /// 计算字节序列的 SHA-256 摘要。
    /// </summary>
    /// <param name="data">输入数据。</param>
    /// <returns>SHA-256 摘要。</returns>
    private static byte[] ComputeHash(byte[] data)
    {
        using var hasher = SHA256.Create();
        return hasher.ComputeHash(data);
    }

    /// <summary>
    /// 异步读取固定长度数据，检测截断流。
    /// </summary>
    /// <param name="source">输入流。</param>
    /// <param name="buffer">目标缓冲区。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步读取操作的任务。</returns>
    /// <exception cref="CryptographicException">流在读取完成前结束时抛出。</exception>
    private static async Task ReadExactlyAsync(Stream source, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var count = await source.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken).ConfigureAwait(false);
            if (count == 0)
                throw new CryptographicException("认证流已截断。 ");
            offset += count;
        }
    }

    /// <summary>
    /// 异步读取记录类型，流结束表示格式截断。
    /// </summary>
    /// <param name="source">输入流。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>记录类型。</returns>
    /// <exception cref="CryptographicException">流中缺少记录类型时抛出。</exception>
    private static async Task<byte> ReadRecordTypeAsync(Stream source, CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        try
        {
            await ReadExactlyAsync(source, buffer, cancellationToken).ConfigureAwait(false);
            return buffer[0];
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(buffer);
        }
    }

    /// <summary>
    /// 判断终止记录后是否仍存在尾随数据。
    /// </summary>
    /// <param name="source">输入流。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>存在尾随数据时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    private static async Task<bool> ReadTrailingByteAsync(Stream source, CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        try
        {
            return await source.ReadAsync(buffer, 0, 1, cancellationToken).ConfigureAwait(false) != 0;
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(buffer);
        }
    }

    /// <summary>
    /// 验证 AES-256 密钥长度。
    /// </summary>
    /// <param name="key">AES 密钥。</param>
    /// <exception cref="ArgumentException">密钥不是 32 字节时抛出。</exception>
    private static void ValidateKey(ReadOnlySpan<byte> key)
    {
        if (key.Length != 32)
            throw new ArgumentException("认证流加密仅支持 32 字节 AES-256 密钥。", nameof(key));
    }

    /// <summary>
    /// 验证文件路径并拒绝使用同一文件作为输入和输出。
    /// </summary>
    /// <param name="sourcePath">源文件路径。</param>
    /// <param name="destinationPath">目标文件路径。</param>
    /// <exception cref="ArgumentException">路径为空或解析为同一文件时抛出。</exception>
    private static void ValidateFilePaths(string sourcePath, string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("源文件路径不能为空。", nameof(sourcePath));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("目标文件路径不能为空。", nameof(destinationPath));
        if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("源文件和目标文件不能相同。", nameof(destinationPath));
    }

    /// <summary>
    /// 验证认证流块大小。
    /// </summary>
    /// <param name="blockSize">块大小。</param>
    /// <exception cref="ArgumentOutOfRangeException">块大小超出安全范围时抛出。</exception>
    private static void ValidateBlockSize(int blockSize)
    {
        if (blockSize < 4096 || blockSize > 1048576)
            throw new ArgumentOutOfRangeException(nameof(blockSize), "认证流块大小必须介于 4096 和 1048576 字节之间。 ");
    }

    /// <summary>
    /// 创建使用固定标签长度的 AES-GCM 实例。
    /// </summary>
    /// <param name="key">AES-256 密钥。</param>
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