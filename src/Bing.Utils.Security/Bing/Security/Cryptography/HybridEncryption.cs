#if NET6_0_OR_GREATER
using System.Security.Cryptography;
using Bing.Security.Keys;

namespace Bing.Security.Cryptography;

/// <summary>
/// 提供 RSA-OAEP-SHA256 与 AES-256-GCM 组合的混合加密操作。
/// </summary>
public static class HybridEncryption
{
    /// <summary>
    /// 使用随机 AES-256-GCM 密钥加密数据，并使用 RSA-OAEP-SHA256 包装该密钥。
    /// </summary>
    /// <param name="plaintext">要加密的数据。</param>
    /// <param name="publicKeyPem">RSA SubjectPublicKeyInfo 公钥 PEM。</param>
    /// <param name="associatedData">需要认证但不加密的关联数据。</param>
    /// <returns>版本化混合加密载荷。</returns>
    public static HybridEncryptedPayload Encrypt(ReadOnlySpan<byte> plaintext, string publicKeyPem, ReadOnlySpan<byte> associatedData = default)
    {
        var key = AesGcmEncryption.GenerateKey(AesKeySize.Size256);
        try
        {
            var encryptedData = AesGcmEncryption.Encrypt(plaintext, key, associatedData);
            var encryptedKey = RsaEncryption.Encrypt(key, publicKeyPem);
            try
            {
                return new HybridEncryptedPayload(encryptedKey, encryptedData);
            }
            finally
            {
                CryptographicOperationsCompat.ZeroMemory(encryptedKey);
            }
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(key);
        }
    }

    /// <summary>
    /// 解密 RSA 包装的 AES 密钥，并验证 AES-GCM 认证标签后返回明文。
    /// </summary>
    /// <param name="payload">版本化混合加密载荷。</param>
    /// <param name="privateKeyPem">RSA PKCS#8 私钥 PEM。</param>
    /// <param name="associatedData">加密时使用的关联数据。</param>
    /// <returns>仅在 RSA 解密与 AES-GCM 认证均成功后返回的明文字节。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="CryptographicException">密钥包装、私钥、认证标签或关联数据无效时抛出。</exception>
    public static byte[] Decrypt(HybridEncryptedPayload payload, string privateKeyPem, ReadOnlySpan<byte> associatedData = default)
    {
        if (payload == null)
            throw new ArgumentNullException(nameof(payload));

        var key = RsaEncryption.Decrypt(payload.EncryptedKey.Span, privateKeyPem);
        try
        {
            if (key.Length != 32)
                throw new CryptographicException("混合加密载荷包含无效的 AES-256 密钥。");
            return AesGcmEncryption.Decrypt(payload.EncryptedData, key, associatedData);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(key);
        }
    }
}
#endif