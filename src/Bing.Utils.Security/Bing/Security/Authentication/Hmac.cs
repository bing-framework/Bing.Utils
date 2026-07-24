using System.Security.Cryptography;
using System.Text;
using Bing.Security.Encoding;
using Bing.Security.Hashing;
using HashingOperations = Bing.Security.Hashing.Hashing;
using TextEncoding = System.Text.Encoding;

namespace Bing.Security.Authentication;

/// <summary>
/// 提供 HMAC SHA-2 消息认证码计算和验证操作。
/// </summary>
public static class Hmac
{
    /// <summary>
    /// 使用指定 HMAC SHA-2 算法计算消息认证码。
    /// </summary>
    /// <param name="value">要认证的数据。</param>
    /// <param name="key">高熵随机密钥，不能为空。</param>
    /// <param name="algorithm">HMAC 算法，默认使用 HMAC-SHA256。</param>
    /// <returns>消息认证码字节。</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为空时抛出。</exception>
    public static byte[] Compute(ReadOnlySpan<byte> value, ReadOnlySpan<byte> key, HmacAlgorithmType algorithm = HmacAlgorithmType.Sha256)
    {
        var keyBytes = GetKeyBytes(key);
        try
        {
            using var hmac = CreateAlgorithm(keyBytes, algorithm);
            return hmac.ComputeHash(value.ToArray());
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(keyBytes);
        }
    }

    /// <summary>
    /// 使用指定 HMAC SHA-2 算法计算文本的小写十六进制消息认证码。
    /// </summary>
    /// <param name="value">要认证的文本。</param>
    /// <param name="key">高熵随机密钥，不能为空。</param>
    /// <param name="algorithm">HMAC 算法，默认使用 HMAC-SHA256。</param>
    /// <param name="encoding">文本编码，未指定时使用 UTF-8。</param>
    /// <returns>小写十六进制消息认证码。</returns>
    public static string ComputeHex(string value, ReadOnlySpan<byte> key, HmacAlgorithmType algorithm = HmacAlgorithmType.Sha256, TextEncoding encoding = null)
    {
        return HexEncoding.Encode(Compute(HashingOperations.GetBytes(value, encoding), key, algorithm));
    }

    /// <summary>
    /// 使用指定 HMAC SHA-2 算法计算文本的标准 Base64 消息认证码。
    /// </summary>
    /// <param name="value">要认证的文本。</param>
    /// <param name="key">高熵随机密钥，不能为空。</param>
    /// <param name="algorithm">HMAC 算法，默认使用 HMAC-SHA256。</param>
    /// <param name="encoding">文本编码，未指定时使用 UTF-8。</param>
    /// <returns>标准 Base64 消息认证码。</returns>
    public static string ComputeBase64(string value, ReadOnlySpan<byte> key, HmacAlgorithmType algorithm = HmacAlgorithmType.Sha256, TextEncoding encoding = null)
    {
        return Convert.ToBase64String(Compute(HashingOperations.GetBytes(value, encoding), key, algorithm));
    }

    /// <summary>
    /// 使用固定时间比较验证十六进制 HMAC。
    /// </summary>
    /// <param name="value">原始数据。</param>
    /// <param name="expectedMac">预期十六进制 HMAC。</param>
    /// <param name="key">高熵随机密钥，不能为空。</param>
    /// <param name="algorithm">HMAC 算法，默认使用 HMAC-SHA256。</param>
    /// <returns>HMAC 匹配时返回 <c>true</c>；格式非法或不匹配时返回 <c>false</c>。</returns>
    public static bool Verify(ReadOnlySpan<byte> value, string expectedMac, ReadOnlySpan<byte> key, HmacAlgorithmType algorithm = HmacAlgorithmType.Sha256)
    {
        if (!HexEncoding.TryDecode(expectedMac, out var expected))
            return false;

        var actual = Compute(value, key, algorithm);
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
    /// 异步计算流的 HMAC，不关闭调用方提供的流。
    /// </summary>
    /// <param name="stream">要读取的流。</param>
    /// <param name="key">高熵随机密钥，不能为空。</param>
    /// <param name="algorithm">HMAC 算法，默认使用 HMAC-SHA256。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步操作的任务，任务结果为消息认证码字节。</returns>
    public static async Task<byte[]> ComputeAsync(Stream stream, ReadOnlyMemory<byte> key, HmacAlgorithmType algorithm = HmacAlgorithmType.Sha256, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        var keyBytes = GetKeyBytes(key.Span);
        try
        {
            using var hmac = CreateAlgorithm(keyBytes, algorithm);
            return await HashingOperations.ComputeStreamAsync(stream, hmac, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(keyBytes);
        }
    }

    /// <summary>
    /// 异步计算文件的小写十六进制 HMAC。
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <param name="key">高熵随机密钥，不能为空。</param>
    /// <param name="algorithm">HMAC 算法，默认使用 HMAC-SHA256。</param>
    /// <param name="cancellationToken">取消异步操作的令牌。</param>
    /// <returns>表示异步操作的任务，任务结果为小写十六进制消息认证码。</returns>
    public static async Task<string> ComputeFileHexAsync(string path, ReadOnlyMemory<byte> key, HmacAlgorithmType algorithm = HmacAlgorithmType.Sha256, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("文件路径不能为空。", nameof(path));

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var mac = await ComputeAsync(stream, key, algorithm, cancellationToken).ConfigureAwait(false);
        try
        {
            return HexEncoding.Encode(mac);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(mac);
        }
    }

    /// <summary>
    /// 根据算法类型创建 HMAC 实例。
    /// </summary>
    /// <param name="key">HMAC 密钥。</param>
    /// <param name="algorithm">HMAC 算法类型。</param>
    /// <returns>HMAC 实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="algorithm"/> 不受支持时抛出。</exception>
    private static HMAC CreateAlgorithm(byte[] key, HmacAlgorithmType algorithm)
    {
        return algorithm switch
        {
            HmacAlgorithmType.Sha256 => new HMACSHA256(key),
            HmacAlgorithmType.Sha384 => new HMACSHA384(key),
            HmacAlgorithmType.Sha512 => new HMACSHA512(key),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "不支持的 HMAC 算法。")
        };
    }

    /// <summary>
    /// 复制并验证 HMAC 密钥。
    /// </summary>
    /// <param name="key">调用方提供的密钥。</param>
    /// <returns>可被内部清零的密钥副本。</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为空时抛出。</exception>
    private static byte[] GetKeyBytes(ReadOnlySpan<byte> key)
    {
        if (key.IsEmpty)
            throw new ArgumentException("HMAC 密钥不能为空，应使用高熵随机密钥。", nameof(key));
        return key.ToArray();
    }
}