using System.Security.Cryptography;
using System.Text;
using Bing.Security.Encoding;
using TextEncoding = System.Text.Encoding;

namespace Bing.Security.Hashing;

/// <summary>
/// 提供 SHA-2 摘要计算和验证操作。
/// </summary>
public static class Hashing
{
    /// <summary>
    /// 使用指定 SHA-2 算法计算字节序列摘要。
    /// </summary>
    /// <param name="value">要计算摘要的字节序列。</param>
    /// <param name="algorithm">摘要算法，默认使用 SHA-256。</param>
    /// <returns>摘要字节。</returns>
    public static byte[] Compute(ReadOnlySpan<byte> value, HashAlgorithmType algorithm = HashAlgorithmType.Sha256)
    {
        using var hasher = CreateAlgorithm(algorithm);
        return hasher.ComputeHash(value.ToArray());
    }

    /// <summary>
    /// 使用指定 SHA-2 算法计算文本的小写十六进制摘要。
    /// </summary>
    /// <param name="value">要计算摘要的文本。</param>
    /// <param name="algorithm">摘要算法，默认使用 SHA-256。</param>
    /// <param name="encoding">文本编码，未指定时使用 UTF-8。</param>
    /// <returns>小写十六进制摘要。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 <c>null</c> 时抛出。</exception>
    public static string ComputeHex(string value, HashAlgorithmType algorithm = HashAlgorithmType.Sha256, TextEncoding encoding = null)
    {
        return HexEncoding.Encode(Compute(GetBytes(value, encoding), algorithm));
    }

    /// <summary>
    /// 使用指定 SHA-2 算法计算文本的 Base64 摘要。
    /// </summary>
    /// <param name="value">要计算摘要的文本。</param>
    /// <param name="algorithm">摘要算法，默认使用 SHA-256。</param>
    /// <param name="encoding">文本编码，未指定时使用 UTF-8。</param>
    /// <returns>标准 Base64 摘要。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 <c>null</c> 时抛出。</exception>
    public static string ComputeBase64(string value, HashAlgorithmType algorithm = HashAlgorithmType.Sha256, TextEncoding encoding = null)
    {
        return Convert.ToBase64String(Compute(GetBytes(value, encoding), algorithm));
    }

    /// <summary>
    /// 验证文本是否匹配指定 SHA-2 小写或大写十六进制摘要。
    /// </summary>
    /// <param name="value">要验证的原始文本。</param>
    /// <param name="expectedHash">预期十六进制摘要。</param>
    /// <param name="algorithm">摘要算法，默认使用 SHA-256。</param>
    /// <param name="encoding">文本编码，未指定时使用 UTF-8。</param>
    /// <returns>摘要匹配时返回 <c>true</c>；格式非法或不匹配时返回 <c>false</c>。</returns>
    public static bool VerifyHex(string value, string expectedHash, HashAlgorithmType algorithm = HashAlgorithmType.Sha256, TextEncoding encoding = null)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (!HexEncoding.TryDecode(expectedHash, out var expected))
            return false;

        var actual = Compute(GetBytes(value, encoding), algorithm);
        try
        {
            return SecureComparison.FixedTimeEquals(actual, expected);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(actual);
            CryptographicOperationsCompat.ZeroMemory(expected);
        }
    }

    /// <summary>
    /// 异步计算流的 SHA-2 摘要，不关闭调用方提供的流。
    /// </summary>
    /// <param name="stream">要读取的流。</param>
    /// <param name="algorithm">摘要算法，默认使用 SHA-256。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步操作的任务，任务结果为摘要字节。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> 为 <c>null</c> 时抛出。</exception>
    public static async Task<byte[]> ComputeAsync(Stream stream, HashAlgorithmType algorithm = HashAlgorithmType.Sha256, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        using var hasher = CreateAlgorithm(algorithm);
        return await ComputeStreamAsync(stream, hasher, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 异步计算文件的小写十六进制 SHA-2 摘要。
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <param name="algorithm">摘要算法，默认使用 SHA-256。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步操作的任务，任务结果为小写十六进制摘要。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> 为空时抛出。</exception>
    public static async Task<string> ComputeFileHexAsync(string path, HashAlgorithmType algorithm = HashAlgorithmType.Sha256, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("文件路径不能为空。", nameof(path));

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await ComputeAsync(stream, algorithm, cancellationToken).ConfigureAwait(false);
        try
        {
            return HexEncoding.Encode(hash);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(hash);
        }
    }

    /// <summary>
    /// 创建指定类型的 SHA-2 算法实例。
    /// </summary>
    /// <param name="algorithm">摘要算法类型。</param>
    /// <returns>摘要算法实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="algorithm"/> 不受支持时抛出。</exception>
    internal static HashAlgorithm CreateAlgorithm(HashAlgorithmType algorithm)
    {
        return algorithm switch
        {
            HashAlgorithmType.Sha256 => SHA256.Create(),
            HashAlgorithmType.Sha384 => SHA384.Create(),
            HashAlgorithmType.Sha512 => SHA512.Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "不支持的 SHA-2 算法。")
        };
    }

    /// <summary>
    /// 异步读取流并计算摘要。
    /// </summary>
    /// <param name="stream">要读取的流。</param>
    /// <param name="hasher">摘要算法实例。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步操作的任务，任务结果为摘要字节。</returns>
    internal static async Task<byte[]> ComputeStreamAsync(Stream stream, HashAlgorithm hasher, CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        try
        {
            while (true)
            {
                var count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                if (count == 0)
                    break;
                hasher.TransformBlock(buffer, 0, count, null, 0);
            }
            hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return hasher.Hash ?? throw new CryptographicException("无法计算流摘要。");
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(buffer);
        }
    }

    /// <summary>
    /// 将文本转换为指定编码的字节。
    /// </summary>
    /// <param name="value">文本值。</param>
    /// <param name="encoding">文本编码。</param>
    /// <returns>文本字节。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 <c>null</c> 时抛出。</exception>
    internal static byte[] GetBytes(string value, TextEncoding encoding)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        return (encoding ?? TextEncoding.UTF8).GetBytes(value);
    }
}