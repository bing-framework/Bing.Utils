#if NET6_0_OR_GREATER
using System.Security.Cryptography;
using Bing.Security.Cryptography;
using Bing.Security.Keys;

namespace Bing.Security.Signatures;

/// <summary>
/// 提供 RSA-PSS-SHA256 签名和验证操作。
/// </summary>
public static class RsaSignature
{
    /// <summary>
    /// 使用 RSA-PSS-SHA256 为数据生成签名。
    /// </summary>
    /// <param name="data">要签名的数据。</param>
    /// <param name="privateKeyPem">RSA PKCS#8 私钥 PEM。</param>
    /// <returns>RSA-PSS-SHA256 签名字节。</returns>
    /// <exception cref="ArgumentException">私钥无效或长度不足时抛出。</exception>
    public static byte[] Sign(ReadOnlySpan<byte> data, string privateKeyPem)
    {
        using var rsa = PemKeySerializer.ImportRsaPrivateKey(privateKeyPem);
        RsaEncryption.EnsureSecureKeySize(rsa);
        return rsa.SignData(data.ToArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }

    /// <summary>
    /// 使用 RSA-PSS-SHA256 验证签名。
    /// </summary>
    /// <param name="data">原始数据。</param>
    /// <param name="signature">待验证的签名。</param>
    /// <param name="publicKeyPem">RSA SubjectPublicKeyInfo 公钥 PEM。</param>
    /// <returns>签名有效时返回 <c>true</c>；无效签名返回 <c>false</c>。</returns>
    /// <exception cref="ArgumentException">公钥无效或长度不足时抛出。</exception>
    public static bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, string publicKeyPem)
    {
        using var rsa = PemKeySerializer.ImportRsaPublicKey(publicKeyPem);
        RsaEncryption.EnsureSecureKeySize(rsa);
        return rsa.VerifyData(data.ToArray(), signature.ToArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }
}
#endif