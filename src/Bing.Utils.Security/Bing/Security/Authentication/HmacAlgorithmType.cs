namespace Bing.Security.Authentication;

/// <summary>
/// 表示主包支持的 HMAC SHA-2 算法。
/// </summary>
public enum HmacAlgorithmType
{
    /// <summary>
    /// HMAC-SHA256 算法。
    /// </summary>
    Sha256,

    /// <summary>
    /// HMAC-SHA384 算法。
    /// </summary>
    Sha384,

    /// <summary>
    /// HMAC-SHA512 算法。
    /// </summary>
    Sha512
}