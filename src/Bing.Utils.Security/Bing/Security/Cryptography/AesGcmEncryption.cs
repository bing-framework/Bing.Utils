#if !NETSTANDARD2_0
using System.Security.Cryptography;
using Bing.Security.Keys;
using Bing.Security.Randomness;

namespace Bing.Security.Cryptography;

/// <summary>
/// 提供 AES-GCM 认证加密操作。
/// </summary>
public static class AesGcmEncryption
{
    /// <summary>
    /// 生成随机 AES 加密密钥。
    /// </summary>
    /// <param name="keySize">AES 密钥位长度，默认使用 256 位。</param>
    /// <returns>随机 AES 密钥字节。</returns>
    public static byte[] GenerateKey(AesKeySize keySize = AesKeySize.Size256) => AesKeyGenerator.Generate(keySize);

    /// <summary>
    /// 使用自动生成的唯一随机 Nonce 执行 AES-GCM 认证加密。
    /// </summary>
    /// <param name="plaintext">要加密的明文。</param>
    /// <param name="key">16、24 或 32 字节 AES 密钥。</param>
    /// <param name="associatedData">需要认证但不加密的关联数据。</param>
    /// <returns>包含随机 Nonce、密文和认证标签的版本化载荷。</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> 长度无效时抛出。</exception>
    public static AesGcmPayload Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData = default)
    {
        var keyBytes = ValidateAndCopyKey(key);
        var nonce = SecurityRandom.GetBytes(AesGcmPayload.NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AesGcmPayload.TagSize];
        try
        {
            using var aes = CreateAesGcm(keyBytes);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
            return new AesGcmPayload(nonce, ciphertext, tag);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(keyBytes);
            CryptographicOperationsCompat.ZeroMemory(nonce);
            CryptographicOperationsCompat.ZeroMemory(ciphertext);
            CryptographicOperationsCompat.ZeroMemory(tag);
        }
    }

    /// <summary>
    /// 验证认证标签后解密 AES-GCM 载荷。
    /// </summary>
    /// <param name="payload">版本化 AES-GCM 载荷。</param>
    /// <param name="key">16、24 或 32 字节 AES 密钥。</param>
    /// <param name="associatedData">加密时使用的关联数据。</param>
    /// <returns>仅在认证成功后返回的明文字节。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> 长度无效时抛出。</exception>
    /// <exception cref="CryptographicException">认证标签、关联数据、Nonce、密文或密钥不匹配时抛出。</exception>
    public static byte[] Decrypt(AesGcmPayload payload, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData = default)
    {
        if (payload == null)
            throw new ArgumentNullException(nameof(payload));

        var keyBytes = ValidateAndCopyKey(key);
        var plaintext = new byte[payload.Ciphertext.Length];
        try
        {
            using var aes = CreateAesGcm(keyBytes);
            aes.Decrypt(payload.Nonce, payload.Ciphertext, payload.Tag, plaintext, associatedData);
            return plaintext;
        }
        catch
        {
            CryptographicOperationsCompat.ZeroMemory(plaintext);
            throw;
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(keyBytes);
        }
    }

    /// <summary>
    /// 复制并验证 AES 密钥长度。
    /// </summary>
    /// <param name="key">调用方提供的 AES 密钥。</param>
    /// <returns>可被内部清零的密钥副本。</returns>
    /// <exception cref="ArgumentException">密钥长度不是 AES 支持长度时抛出。</exception>
    private static byte[] ValidateAndCopyKey(ReadOnlySpan<byte> key)
    {
        if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            throw new ArgumentException("AES-GCM 密钥必须为 16、24 或 32 字节。", nameof(key));
        return key.ToArray();
    }

    /// <summary>
    /// 创建使用固定认证标签长度的 AES-GCM 实例。
    /// </summary>
    /// <param name="key">AES 密钥。</param>
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