#if NET6_0_OR_GREATER
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Bing.Security.Certificates;

/// <summary>
/// 提供从 X.509 证书读取 RSA 和 ECDSA 公私钥的操作。
/// </summary>
public static class CertificateKeyReader
{
    /// <summary>
    /// 获取证书中的 RSA 公钥。
    /// </summary>
    /// <param name="certificate">X.509 证书。</param>
    /// <returns>调用方负责释放的 RSA 公钥实例。</returns>
    /// <exception cref="ArgumentException">证书不包含 RSA 公钥时抛出。</exception>
    public static RSA GetRsaPublicKey(X509Certificate2 certificate) => GetKey(certificate, static value => value.GetRSAPublicKey(), "RSA 公钥");

    /// <summary>
    /// 获取证书中的 RSA 私钥。
    /// </summary>
    /// <param name="certificate">X.509 证书。</param>
    /// <returns>调用方负责释放的 RSA 私钥实例。</returns>
    /// <exception cref="ArgumentException">证书不包含 RSA 私钥时抛出。</exception>
    public static RSA GetRsaPrivateKey(X509Certificate2 certificate) => GetKey(certificate, static value => value.GetRSAPrivateKey(), "RSA 私钥");

    /// <summary>
    /// 获取证书中的 ECDSA 公钥。
    /// </summary>
    /// <param name="certificate">X.509 证书。</param>
    /// <returns>调用方负责释放的 ECDSA 公钥实例。</returns>
    /// <exception cref="ArgumentException">证书不包含 ECDSA 公钥时抛出。</exception>
    public static ECDsa GetEcdsaPublicKey(X509Certificate2 certificate) => GetKey(certificate, static value => value.GetECDsaPublicKey(), "ECDSA 公钥");

    /// <summary>
    /// 获取证书中的 ECDSA 私钥。
    /// </summary>
    /// <param name="certificate">X.509 证书。</param>
    /// <returns>调用方负责释放的 ECDSA 私钥实例。</returns>
    /// <exception cref="ArgumentException">证书不包含 ECDSA 私钥时抛出。</exception>
    public static ECDsa GetEcdsaPrivateKey(X509Certificate2 certificate) => GetKey(certificate, static value => value.GetECDsaPrivateKey(), "ECDSA 私钥");

    /// <summary>
    /// 获取并验证指定类型的证书密钥。
    /// </summary>
    /// <typeparam name="TKey">密码学密钥类型。</typeparam>
    /// <param name="certificate">X.509 证书。</param>
    /// <param name="getKey">读取证书密钥的函数。</param>
    /// <param name="keyName">异常中使用的密钥名称。</param>
    /// <returns>调用方负责释放的密钥实例。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="certificate"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">证书不包含目标密钥时抛出。</exception>
    private static TKey GetKey<TKey>(X509Certificate2 certificate, Func<X509Certificate2, TKey> getKey, string keyName) where TKey : AsymmetricAlgorithm
    {
        if (certificate == null)
            throw new ArgumentNullException(nameof(certificate));
        var key = getKey(certificate);
        if (key == null)
            throw new ArgumentException("证书不包含" + keyName + "。", nameof(certificate));
        return key;
    }
}
#endif