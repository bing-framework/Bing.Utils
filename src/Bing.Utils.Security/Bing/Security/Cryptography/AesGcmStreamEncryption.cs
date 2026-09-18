#if NET6_0_OR_GREATER
using System.Security.Cryptography;
using Bing.Security.Randomness;

namespace Bing.Security.Cryptography;

/// <summary>
/// 提供使用分块 AES-GCM 认证格式的异步流加密和解密操作。
/// </summary>
public static class AesGcmStreamEncryption
{
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
        var destinationDirectory = Path.GetDirectoryName(Path.GetFullPath(destinationPath));
        var temporaryPath = Path.Combine(destinationDirectory, "." + Path.GetRandomFileName());
        try
        {
            using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var temporary = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await EncryptAsync(source, temporary, key, options, cancellationToken).ConfigureAwait(false);
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

        await AesGcmStreamV2.EncryptAsync(plaintext, ciphertext, key, options.BlockSize, cancellationToken).ConfigureAwait(false);
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
    /// <exception cref="ArgumentException"><paramref name="key"/> 不是 32 字节时抛出。</exception>
    /// <exception cref="NotSupportedException">输入流使用不受支持的格式版本、算法或 KDF 时抛出。</exception>
    /// <exception cref="InvalidDataException">输入流截断、格式无效或包含尾随数据时抛出。</exception>
    /// <exception cref="CryptographicException">认证标签、块顺序、块长度或密钥无效时抛出。</exception>
    public static async Task DecryptAsync(Stream ciphertext, Stream plaintext, ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        if (ciphertext == null)
            throw new ArgumentNullException(nameof(ciphertext));
        if (plaintext == null)
            throw new ArgumentNullException(nameof(plaintext));
        ValidateKey(key.Span);

        var magic = new byte[4];
        try
        {
            await ReadExactlyAsync(ciphertext, magic, cancellationToken).ConfigureAwait(false);
            if (magic[0] == 'B' && magic[1] == 'S' && magic[2] == 'S' && magic[3] == '1')
                throw new NotSupportedException("BSS1 认证流不受支持。请使用已完成 BSS2 迁移的密文。 ");
            if (magic[0] != 'B' || magic[1] != 'S' || magic[2] != 'S' || magic[3] != '2')
                throw new InvalidDataException("认证流不是受支持的 BSS2 格式。 ");
            await AesGcmStreamV2.DecryptAsync(ciphertext, plaintext, key, magic, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(magic);
        }
    }

    /// <summary>
    /// 异步读取固定长度数据，检测截断流。
    /// </summary>
    /// <param name="source">输入流。</param>
    /// <param name="buffer">目标缓冲区。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步读取操作的任务。</returns>
    /// <exception cref="InvalidDataException">流在读取完成前结束时抛出。</exception>
    private static async Task ReadExactlyAsync(Stream source, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var count = await source.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken).ConfigureAwait(false);
            if (count == 0)
                throw new InvalidDataException("认证流已截断。 ");
            offset += count;
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
        var fullSourcePath = Path.GetFullPath(sourcePath);
        var fullDestinationPath = Path.GetFullPath(destinationPath);
        if (PathsReferToSameLocation(fullSourcePath, fullDestinationPath))
            throw new ArgumentException("源文件和目标文件不能相同。", nameof(destinationPath));
    }

    /// <summary>
    /// 根据目标目录的实际文件系统语义判断两个规范化路径是否指向同一位置。
    /// </summary>
    private static bool PathsReferToSameLocation(string sourcePath, string destinationPath)
    {
        if (string.Equals(sourcePath, destinationPath, StringComparison.Ordinal))
            return true;
        if (!string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
            return false;
        return !IsCaseSensitiveDirectory(Path.GetDirectoryName(destinationPath));
    }

    /// <summary>
    /// 使用临时探测文件判断目录是否区分大小写，避免按操作系统猜测卷配置。
    /// </summary>
    private static bool IsCaseSensitiveDirectory(string directory)
    {
        var probeName = ".bing-case-probe-" + Guid.NewGuid().ToString("N") + "a";
        var probePath = Path.Combine(directory, probeName);
        var alternatePath = Path.Combine(directory, probeName.ToUpperInvariant());
        try
        {
            using (new FileStream(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
            }
            return !File.Exists(alternatePath);
        }
        finally
        {
            if (File.Exists(probePath))
                File.Delete(probePath);
        }
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

}
#endif