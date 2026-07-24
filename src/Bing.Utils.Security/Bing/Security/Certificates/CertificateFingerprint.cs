#if NET6_0_OR_GREATER
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Bing.Security.Encoding;

namespace Bing.Security.Certificates;

/// <summary>
/// 提供 X.509 证书 SHA-256 指纹计算操作。
/// </summary>
public static class CertificateFingerprint
{
    /// <summary>
    /// 计算证书 DER 原始数据的 SHA-256 小写十六进制指纹。
    /// </summary>
    /// <param name="certificate">X.509 证书。</param>
    /// <returns>小写十六进制 SHA-256 指纹。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="certificate"/> 为 <c>null</c> 时抛出。</exception>
    public static string ComputeSha256(X509Certificate2 certificate)
    {
        if (certificate == null)
            throw new ArgumentNullException(nameof(certificate));

        var rawData = certificate.RawData;
        try
        {
            using var hasher = SHA256.Create();
            return HexEncoding.Encode(hasher.ComputeHash(rawData));
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(rawData);
        }
    }
}
#endif