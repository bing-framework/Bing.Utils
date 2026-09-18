#if NET6_0_OR_GREATER
using System.Security.Cryptography;
using Bing.Security.Cryptography;
namespace Bing.Security.Canonicalization;

/// <summary>提供 BRS1 版本化 RSA-PSS-SHA256 签名，不缓存或释放调用方密钥。</summary>
public static class VersionedRequestSigner
{
    /// <summary>唯一支持的版本和算法标识，同时纳入签名。</summary>
    public const string Protocol = "BRS1.RSA-PSS-SHA256";

    /// <summary>使用调用期提供的 RSA 私钥签名规范化参数。</summary>
    /// <param name="parameters">显式参数集合。</param>
    /// <param name="privateKey">至少 2048 位的 RSA 私钥，由调用方释放。</param>
    /// <returns>协议标识及 Base64 签名。</returns>
    public static string Sign(IEnumerable<CanonicalParameter> parameters, RSA privateKey)
    {
        if (privateKey == null) throw new ArgumentNullException(nameof(privateKey));
        RsaEncryption.EnsureSecureKeySize(privateKey);
        return Protocol + "." + Convert.ToBase64String(privateKey.SignData(
            GetData(parameters), HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
    }

    /// <summary>验证版本化签名，不回退旧协议，不释放密钥。</summary>
    /// <param name="parameters">显式参数集合。</param>
    /// <param name="signature">版本化签名文本。</param>
    /// <param name="publicKey">至少 2048 位的 RSA 公钥，由调用方释放。</param>
    /// <returns>版本、格式或签名不匹配返回 false；无效参数或密钥抛出异常。</returns>
    public static bool Verify(IEnumerable<CanonicalParameter> parameters, string signature, RSA publicKey)
    {
        if (publicKey == null) throw new ArgumentNullException(nameof(publicKey));
        if (signature == null) throw new ArgumentNullException(nameof(signature));
        RsaEncryption.EnsureSecureKeySize(publicKey);
        var data = GetData(parameters);
        var prefix = Protocol + ".";
        if (!signature.StartsWith(prefix, StringComparison.Ordinal)) return false;
        // 在解码前限制不可信输入长度。
        var byteLength = (publicKey.KeySize + 7) / 8;
        if (signature.Length - prefix.Length != ((byteLength + 2) / 3) * 4) return false;
        byte[] decoded;
        try { decoded = Convert.FromBase64String(signature.Substring(prefix.Length)); }
        catch (FormatException) { return false; }
        if (decoded.Length != byteLength) return false;
        try { return publicKey.VerifyData(data, decoded, HashAlgorithmName.SHA256, RSASignaturePadding.Pss); }
        catch (CryptographicException) { return false; }
    }

    private static byte[] GetData(IEnumerable<CanonicalParameter> parameters) =>
        System.Text.Encoding.UTF8.GetBytes(Protocol + "\n" + CanonicalParameterSerializer.Serialize(parameters));
}
#endif
