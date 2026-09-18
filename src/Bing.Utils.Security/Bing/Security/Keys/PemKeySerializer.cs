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
            rsa.ImportFromPem(pem);
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
        // 只允许单个、用途匹配的 PEM 块，避免从混合文本导入其他用途的密钥。
        var text = pem.AsSpan().Trim();
        if (!PemEncoding.TryFind(text, out var fields) ||
            !text[fields.Label].SequenceEqual(expectedLabel.AsSpan()) ||
            fields.Location.Start.GetOffset(text.Length) != 0 ||
            fields.Location.End.GetOffset(text.Length) != text.Length)
            throw new ArgumentException("PEM 必须包含单个用途匹配的密钥块。", nameof(pem));
    }
}
#endif