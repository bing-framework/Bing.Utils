namespace Bing.Security.Hashing;

/// <summary>
/// 表示主包支持的 SHA-2 摘要算法。
/// </summary>
public enum HashAlgorithmType
{
    /// <summary>
    /// SHA-256 摘要算法。
    /// </summary>
    Sha256,

    /// <summary>
    /// SHA-384 摘要算法。
    /// </summary>
    Sha384,

    /// <summary>
    /// SHA-512 摘要算法。
    /// </summary>
    Sha512
}