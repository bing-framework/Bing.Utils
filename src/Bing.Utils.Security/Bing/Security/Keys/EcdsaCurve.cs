#if !NETSTANDARD2_0
namespace Bing.Security.Keys;

/// <summary>
/// 表示支持的 NIST ECDSA 曲线。
/// </summary>
public enum EcdsaCurve
{
    /// <summary>
    /// NIST P-256 曲线。
    /// </summary>
    P256,

    /// <summary>
    /// NIST P-384 曲线。
    /// </summary>
    P384,

    /// <summary>
    /// NIST P-521 曲线。
    /// </summary>
    P521
}
#endif