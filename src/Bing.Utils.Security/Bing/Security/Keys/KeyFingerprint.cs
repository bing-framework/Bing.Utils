#if !NETSTANDARD2_0
using System.Security.Cryptography;
using Bing.Security.Encoding;

namespace Bing.Security.Keys;

/// <summary>
/// 提供标准公钥表示的 SHA-256 指纹计算操作。
/// </summary>
public static class KeyFingerprint
{
    /// <summary>
    /// 计算 RSA 或 ECDSA SubjectPublicKeyInfo 公钥 PEM 的 SHA-256 小写十六进制指纹。
    /// </summary>
    /// <param name="publicKeyPem">标准 SubjectPublicKeyInfo 公钥 PEM。</param>
    /// <returns>基于 DER 公钥表示计算的小写十六进制指纹。</returns>
    /// <exception cref="ArgumentException">PEM 格式错误或不是 RSA/ECDSA 公钥时抛出。</exception>
    public static string ComputeSha256(string publicKeyPem)
    {
        try
        {
            using var rsa = PemKeySerializer.ImportRsaPublicKey(publicKeyPem);
            return Compute(rsa.ExportSubjectPublicKeyInfo());
        }
        catch (ArgumentException)
        {
            using var ecdsa = PemKeySerializer.ImportEcdsaPublicKey(publicKeyPem);
            return Compute(ecdsa.ExportSubjectPublicKeyInfo());
        }
    }

    /// <summary>
    /// 计算标准 DER 公钥的 SHA-256 指纹。
    /// </summary>
    /// <param name="publicKeyDer">SubjectPublicKeyInfo DER 公钥。</param>
    /// <returns>小写十六进制 SHA-256 指纹。</returns>
    private static string Compute(byte[] publicKeyDer)
    {
        try
        {
            using var hasher = SHA256.Create();
            return HexEncoding.Encode(hasher.ComputeHash(publicKeyDer));
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(publicKeyDer);
        }
    }
}
#endif