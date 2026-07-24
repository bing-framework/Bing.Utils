#if !NETSTANDARD2_0
using System.Security.Cryptography;

namespace Bing.Security.Keys;

/// <summary>
/// 提供标准 PKCS#8 和 SubjectPublicKeyInfo PEM 密钥导入操作。
/// </summary>
public static class PemKeySerializer
{
    /// <summary>
    /// 导入 RSA SubjectPublicKeyInfo 公钥 PEM。
    /// </summary>
    /// <param name="pem">RSA 公钥 PEM。</param>
    /// <returns>调用方负责释放的 RSA 实例。</returns>
    public static RSA ImportRsaPublicKey(string pem) => ImportRsa(pem, false);

    /// <summary>
    /// 导入 RSA PKCS#8 私钥 PEM。
    /// </summary>
    /// <param name="pem">RSA 私钥 PEM。</param>
    /// <returns>调用方负责释放的 RSA 实例。</returns>
    public static RSA ImportRsaPrivateKey(string pem) => ImportRsa(pem, true);

    /// <summary>
    /// 导入 ECDSA SubjectPublicKeyInfo 公钥 PEM。
    /// </summary>
    /// <param name="pem">ECDSA 公钥 PEM。</param>
    /// <returns>调用方负责释放的 ECDSA 实例。</returns>
    public static ECDsa ImportEcdsaPublicKey(string pem) => ImportEcdsa(pem, false);

    /// <summary>
    /// 导入 ECDSA PKCS#8 私钥 PEM。
    /// </summary>
    /// <param name="pem">ECDSA 私钥 PEM。</param>
    /// <returns>调用方负责释放的 ECDSA 实例。</returns>
    public static ECDsa ImportEcdsaPrivateKey(string pem) => ImportEcdsa(pem, true);

    /// <summary>
    /// 导入 RSA PEM 并确保其密钥用途符合调用方预期。
    /// </summary>
    /// <param name="pem">PEM 文本。</param>
    /// <param name="privateKey">是否需要私钥。</param>
    /// <returns>RSA 实例。</returns>
    /// <exception cref="ArgumentException">PEM 为空、格式错误或密钥用途不匹配时抛出。</exception>
    private static RSA ImportRsa(string pem, bool privateKey)
    {
        ValidatePem(pem, privateKey ? "PRIVATE KEY" : "PUBLIC KEY");
        var rsa = RSA.Create();
        try
        {
            if (privateKey)
                rsa.ImportFromPem(pem);
            else
                rsa.ImportFromPem(pem);
            var parameters = rsa.ExportParameters(privateKey);
            if (parameters.Modulus == null || (privateKey && parameters.D == null))
                throw new ArgumentException("PEM 不包含所需的 RSA 密钥用途。", nameof(pem));
            return rsa;
        }
        catch (Exception exception) when (exception is CryptographicException || exception is ArgumentException)
        {
            rsa.Dispose();
            throw new ArgumentException("PEM 不是有效的 RSA 密钥，或其用途与请求不匹配。", nameof(pem), exception);
        }
    }

    /// <summary>
    /// 导入 ECDSA PEM 并确保其密钥用途符合调用方预期。
    /// </summary>
    /// <param name="pem">PEM 文本。</param>
    /// <param name="privateKey">是否需要私钥。</param>
    /// <returns>ECDSA 实例。</returns>
    /// <exception cref="ArgumentException">PEM 为空、格式错误或密钥用途不匹配时抛出。</exception>
    private static ECDsa ImportEcdsa(string pem, bool privateKey)
    {
        ValidatePem(pem, privateKey ? "PRIVATE KEY" : "PUBLIC KEY");
        var ecdsa = ECDsa.Create();
        try
        {
            ecdsa.ImportFromPem(pem);
            var parameters = ecdsa.ExportParameters(privateKey);
            if (parameters.Q.X == null || (privateKey && parameters.D == null))
                throw new ArgumentException("PEM 不包含所需的 ECDSA 密钥用途。", nameof(pem));
            return ecdsa;
        }
        catch (Exception exception) when (exception is CryptographicException || exception is ArgumentException)
        {
            ecdsa.Dispose();
            throw new ArgumentException("PEM 不是有效的 ECDSA 密钥，或其用途与请求不匹配。", nameof(pem), exception);
        }
    }

    /// <summary>
    /// 验证 PEM 文本基本格式。
    /// </summary>
    /// <param name="pem">PEM 文本。</param>
    /// <param name="expectedLabel">期望的 PEM 标签。</param>
    /// <exception cref="ArgumentException">PEM 为空或标签不匹配时抛出。</exception>
    private static void ValidatePem(string pem, string expectedLabel)
    {
        if (string.IsNullOrWhiteSpace(pem))
            throw new ArgumentException("PEM 文本不能为空。", nameof(pem));
        if (pem.IndexOf("-----BEGIN " + expectedLabel + "-----", StringComparison.Ordinal) < 0 ||
            pem.IndexOf("-----END " + expectedLabel + "-----", StringComparison.Ordinal) < 0)
            throw new ArgumentException("PEM 标签与请求的密钥用途不匹配。", nameof(pem));
    }
}
#endif