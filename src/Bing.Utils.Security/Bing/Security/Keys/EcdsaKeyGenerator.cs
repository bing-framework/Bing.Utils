#if !NETSTANDARD2_0
using System.Security.Cryptography;

namespace Bing.Security.Keys;

/// <summary>
/// 提供 NIST ECDSA 密钥对生成操作。
/// </summary>
public static class EcdsaKeyGenerator
{
    /// <summary>
    /// 生成指定 NIST 曲线的 ECDSA 密钥对。
    /// </summary>
    /// <param name="curve">ECDSA 曲线，默认使用 P-256。</param>
    /// <returns>使用 PKCS#8 私钥和 SubjectPublicKeyInfo 公钥表示的密钥对。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="curve"/> 不受支持时抛出。</exception>
    public static EcdsaKeyPair Generate(EcdsaCurve curve = EcdsaCurve.P256)
    {
        using var ecdsa = ECDsa.Create(GetCurve(curve));
        return new EcdsaKeyPair(
            new string(PemEncoding.Write("PUBLIC KEY", ecdsa.ExportSubjectPublicKeyInfo())),
            new string(PemEncoding.Write("PRIVATE KEY", ecdsa.ExportPkcs8PrivateKey())));
    }

    /// <summary>
    /// 将强类型曲线枚举转换为命名曲线。
    /// </summary>
    /// <param name="curve">ECDSA 曲线。</param>
    /// <returns>命名椭圆曲线。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="curve"/> 不受支持时抛出。</exception>
    internal static ECCurve GetCurve(EcdsaCurve curve)
    {
        return curve switch
        {
            EcdsaCurve.P256 => ECCurve.NamedCurves.nistP256,
            EcdsaCurve.P384 => ECCurve.NamedCurves.nistP384,
            EcdsaCurve.P521 => ECCurve.NamedCurves.nistP521,
            _ => throw new ArgumentOutOfRangeException(nameof(curve), curve, "不支持的 ECDSA 曲线。")
        };
    }
}
#endif