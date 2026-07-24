#if NET6_0_OR_GREATER
using System.Security.Cryptography;
using Bing.Security.Keys;

namespace Bing.Security.Signatures;

/// <summary>
/// 提供 NIST P-256、P-384 和 P-521 曲线的 DER 格式 ECDSA 签名操作。
/// </summary>
public static class EcdsaSignature
{
    /// <summary>
    /// 使用私钥曲线对应的 SHA-2 算法创建 DER 格式 ECDSA 签名。
    /// </summary>
    /// <param name="data">要签名的数据。</param>
    /// <param name="privateKeyPem">ECDSA PKCS#8 私钥 PEM。</param>
    /// <returns>RFC 3279 DER 编码的 ECDSA 签名。</returns>
    /// <exception cref="ArgumentException">私钥无效或曲线不受支持时抛出。</exception>
    public static byte[] Sign(ReadOnlySpan<byte> data, string privateKeyPem)
    {
        using var ecdsa = PemKeySerializer.ImportEcdsaPrivateKey(privateKeyPem);
        return ecdsa.SignData(data.ToArray(), GetHashAlgorithm(ecdsa), DSASignatureFormat.Rfc3279DerSequence);
    }

    /// <summary>
    /// 使用公钥曲线对应的 SHA-2 算法验证 DER 格式 ECDSA 签名。
    /// </summary>
    /// <param name="data">原始数据。</param>
    /// <param name="signature">RFC 3279 DER 编码的签名。</param>
    /// <param name="publicKeyPem">ECDSA SubjectPublicKeyInfo 公钥 PEM。</param>
    /// <returns>签名有效时返回 <c>true</c>；签名无效时返回 <c>false</c>。</returns>
    /// <exception cref="ArgumentException">公钥无效或曲线不受支持时抛出。</exception>
    public static bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, string publicKeyPem)
    {
        using var ecdsa = PemKeySerializer.ImportEcdsaPublicKey(publicKeyPem);
        try
        {
            return ecdsa.VerifyData(data.ToArray(), signature.ToArray(), GetHashAlgorithm(ecdsa), DSASignatureFormat.Rfc3279DerSequence);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    /// <summary>
    /// 根据密钥中的标准命名曲线选择对应的 SHA-2 摘要算法。
    /// </summary>
    /// <param name="ecdsa">ECDSA 密钥实例。</param>
    /// <returns>与曲线安全强度匹配的摘要算法。</returns>
    /// <exception cref="ArgumentException">曲线不是支持的 NIST 曲线时抛出。</exception>
    private static HashAlgorithmName GetHashAlgorithm(ECDsa ecdsa)
    {
        var curveOid = ecdsa.ExportParameters(false).Curve.Oid.Value;
        return curveOid switch
        {
            "1.2.840.10045.3.1.7" => HashAlgorithmName.SHA256,
            "1.3.132.0.34" => HashAlgorithmName.SHA384,
            "1.3.132.0.35" => HashAlgorithmName.SHA512,
            _ => throw new ArgumentException("仅支持 NIST P-256、P-384 和 P-521 ECDSA 曲线。", nameof(ecdsa))
        };
    }
}
#endif