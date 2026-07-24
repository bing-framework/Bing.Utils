#if NET6_0_OR_GREATER
using System.Security.Cryptography;
using Bing.Security.Keys;

namespace Bing.Security.Cryptography;

/// <summary>
/// 提供 RSA-OAEP-SHA256 短数据加密和解密操作。
/// </summary>
public static class RsaEncryption
{
    /// <summary>
    /// 使用 RSA-OAEP-SHA256 加密短数据。
    /// </summary>
    /// <param name="plaintext">要加密的短数据。</param>
    /// <param name="publicKeyPem">RSA SubjectPublicKeyInfo 公钥 PEM。</param>
    /// <returns>RSA 密文。</returns>
    /// <exception cref="ArgumentException">公钥无效、密钥长度不足或明文超过 OAEP 最大长度时抛出。</exception>
    public static byte[] Encrypt(ReadOnlySpan<byte> plaintext, string publicKeyPem)
    {
        using var rsa = PemKeySerializer.ImportRsaPublicKey(publicKeyPem);
        EnsureSecureKeySize(rsa);
        var maximumLength = GetMaximumPlaintextLength(rsa);
        if (plaintext.Length > maximumLength)
            throw new ArgumentException("明文超过 RSA-OAEP-SHA256 可加密的最大长度；请使用混合加密处理较大数据。", nameof(plaintext));

        return rsa.Encrypt(plaintext.ToArray(), RSAEncryptionPadding.OaepSHA256);
    }

    /// <summary>
    /// 使用 RSA-OAEP-SHA256 解密密文。
    /// </summary>
    /// <param name="ciphertext">要解密的 RSA 密文。</param>
    /// <param name="privateKeyPem">RSA PKCS#8 私钥 PEM。</param>
    /// <returns>仅在 OAEP 验证成功后返回的明文字节。</returns>
    /// <exception cref="ArgumentException">私钥无效、密钥长度不足或密文长度不匹配时抛出。</exception>
    /// <exception cref="CryptographicException">密文、私钥或 OAEP 填充无效时抛出。</exception>
    public static byte[] Decrypt(ReadOnlySpan<byte> ciphertext, string privateKeyPem)
    {
        using var rsa = PemKeySerializer.ImportRsaPrivateKey(privateKeyPem);
        EnsureSecureKeySize(rsa);
        if (ciphertext.Length != rsa.KeySize / 8)
            throw new ArgumentException("RSA 密文长度与私钥长度不匹配。", nameof(ciphertext));

        return rsa.Decrypt(ciphertext.ToArray(), RSAEncryptionPadding.OaepSHA256);
    }

    /// <summary>
    /// 确保 RSA 密钥达到最低安全长度。
    /// </summary>
    /// <param name="rsa">RSA 密钥实例。</param>
    /// <exception cref="ArgumentException">密钥不足 2048 位时抛出。</exception>
    internal static void EnsureSecureKeySize(RSA rsa)
    {
        if (rsa.KeySize < 2048)
            throw new ArgumentException("RSA 密钥长度必须不小于 2048 位。", nameof(rsa));
    }

    /// <summary>
    /// 获取 RSA-OAEP-SHA256 的最大明文长度。
    /// </summary>
    /// <param name="rsa">RSA 密钥实例。</param>
    /// <returns>可加密的最大明文字节数。</returns>
    private static int GetMaximumPlaintextLength(RSA rsa)
    {
        const int sha256DigestSize = 32;
        return rsa.KeySize / 8 - 2 * sha256DigestSize - 2;
    }
}
#endif