using System.Text;
using Bing.Security.Authentication;
using Bing.Security.Encoding;
#if NET6_0_OR_GREATER
using Bing.Security.Signatures;
#endif

namespace Bing.Security.Canonicalization;

/// <summary>
/// 提供规范化请求的 HMAC、RSA-PSS 和 ECDSA 签名操作。
/// </summary>
public static class CanonicalRequestSigner
{
    /// <summary>
    /// 使用 HMAC-SHA256 签名规范化参数。
    /// </summary>
    /// <param name="parameters">显式参数集合。</param>
    /// <param name="key">高熵 HMAC 密钥。</param>
    /// <param name="options">规范化选项。</param>
    /// <returns>小写十六进制 HMAC-SHA256 签名。</returns>
    public static string SignHmacSha256(IEnumerable<CanonicalParameter> parameters, ReadOnlySpan<byte> key, CanonicalParameterOptions options = null)
    {
        var canonical = CanonicalParameterSerializer.Serialize(parameters, options);
        return Hmac.ComputeHex(canonical, key, HmacAlgorithmType.Sha256, System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// 验证 HMAC-SHA256 规范化请求签名。
    /// </summary>
    /// <param name="parameters">显式参数集合。</param>
    /// <param name="signature">小写或大写十六进制 HMAC-SHA256 签名。</param>
    /// <param name="key">高熵 HMAC 密钥。</param>
    /// <param name="options">规范化选项。</param>
    /// <returns>签名有效时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool VerifyHmacSha256(IEnumerable<CanonicalParameter> parameters, string signature, ReadOnlySpan<byte> key, CanonicalParameterOptions options = null)
    {
        var canonical = CanonicalParameterSerializer.Serialize(parameters, options);
        return Hmac.Verify(System.Text.Encoding.UTF8.GetBytes(canonical), signature, key, HmacAlgorithmType.Sha256);
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// 使用 RSA-PSS-SHA256 签名规范化参数。
    /// </summary>
    /// <param name="parameters">显式参数集合。</param>
    /// <param name="privateKeyPem">RSA PKCS#8 私钥 PEM。</param>
    /// <param name="options">规范化选项。</param>
    /// <returns>RSA-PSS-SHA256 签名字节。</returns>
    public static byte[] SignRsaPssSha256(IEnumerable<CanonicalParameter> parameters, string privateKeyPem, CanonicalParameterOptions options = null)
    {
        return RsaSignature.Sign(System.Text.Encoding.UTF8.GetBytes(CanonicalParameterSerializer.Serialize(parameters, options)), privateKeyPem);
    }

    /// <summary>
    /// 验证 RSA-PSS-SHA256 规范化请求签名。
    /// </summary>
    /// <param name="parameters">显式参数集合。</param>
    /// <param name="signature">RSA-PSS-SHA256 签名字节。</param>
    /// <param name="publicKeyPem">RSA SubjectPublicKeyInfo 公钥 PEM。</param>
    /// <param name="options">规范化选项。</param>
    /// <returns>签名有效时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool VerifyRsaPssSha256(IEnumerable<CanonicalParameter> parameters, ReadOnlySpan<byte> signature, string publicKeyPem, CanonicalParameterOptions options = null)
    {
        return RsaSignature.Verify(System.Text.Encoding.UTF8.GetBytes(CanonicalParameterSerializer.Serialize(parameters, options)), signature, publicKeyPem);
    }

    /// <summary>
    /// 使用私钥曲线对应的 SHA-2 算法签名规范化参数。
    /// </summary>
    /// <param name="parameters">显式参数集合。</param>
    /// <param name="privateKeyPem">ECDSA PKCS#8 私钥 PEM。</param>
    /// <param name="options">规范化选项。</param>
    /// <returns>DER 格式 ECDSA 签名字节。</returns>
    public static byte[] SignEcdsa(IEnumerable<CanonicalParameter> parameters, string privateKeyPem, CanonicalParameterOptions options = null)
    {
        return EcdsaSignature.Sign(System.Text.Encoding.UTF8.GetBytes(CanonicalParameterSerializer.Serialize(parameters, options)), privateKeyPem);
    }

    /// <summary>
    /// 验证 DER 格式 ECDSA 规范化请求签名。
    /// </summary>
    /// <param name="parameters">显式参数集合。</param>
    /// <param name="signature">DER 格式 ECDSA 签名字节。</param>
    /// <param name="publicKeyPem">ECDSA SubjectPublicKeyInfo 公钥 PEM。</param>
    /// <param name="options">规范化选项。</param>
    /// <returns>签名有效时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool VerifyEcdsa(IEnumerable<CanonicalParameter> parameters, ReadOnlySpan<byte> signature, string publicKeyPem, CanonicalParameterOptions options = null)
    {
        return EcdsaSignature.Verify(System.Text.Encoding.UTF8.GetBytes(CanonicalParameterSerializer.Serialize(parameters, options)), signature, publicKeyPem);
    }
#endif
}