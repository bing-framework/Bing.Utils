#if !NETSTANDARD2_0
namespace Bing.Security.Keys;

/// <summary>
/// 表示使用标准 PEM 格式导出的 RSA 密钥对。
/// </summary>
public sealed class RsaKeyPair
{
    /// <summary>
    /// 使用 SubjectPublicKeyInfo 格式导出的 RSA 公钥 PEM。
    /// </summary>
    public string PublicKeyPem { get; }

    /// <summary>
    /// 使用 PKCS#8 格式导出的 RSA 私钥 PEM。
    /// </summary>
    public string PrivateKeyPem { get; }

    /// <summary>
    /// 使用标准 PEM 表示初始化 RSA 密钥对。
    /// </summary>
    /// <param name="publicKeyPem">SubjectPublicKeyInfo 公钥 PEM。</param>
    /// <param name="privateKeyPem">PKCS#8 私钥 PEM。</param>
    /// <exception cref="ArgumentException">任一 PEM 文本为空时抛出。</exception>
    public RsaKeyPair(string publicKeyPem, string privateKeyPem)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPem))
            throw new ArgumentException("RSA 公钥 PEM 不能为空。", nameof(publicKeyPem));
        if (string.IsNullOrWhiteSpace(privateKeyPem))
            throw new ArgumentException("RSA 私钥 PEM 不能为空。", nameof(privateKeyPem));
        PublicKeyPem = publicKeyPem;
        PrivateKeyPem = privateKeyPem;
    }
}
#endif