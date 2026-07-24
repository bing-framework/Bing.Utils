#if !NETSTANDARD2_0
namespace Bing.Security.Passwords;

/// <summary>
/// 配置 PBKDF2-HMAC-SHA256 密码哈希参数。
/// </summary>
public sealed class Pbkdf2PasswordHasherOptions
{
    /// <summary>
    /// 默认 PBKDF2 迭代次数。
    /// </summary>
    public const int DefaultIterationCount = 310000;

    /// <summary>
    /// PBKDF2 迭代次数，必须不小于 100000。
    /// </summary>
    public int IterationCount { get; init; } = DefaultIterationCount;

    /// <summary>
    /// 随机盐字节长度，必须不小于 16。
    /// </summary>
    public int SaltSize { get; init; } = 16;

    /// <summary>
    /// 派生哈希字节长度，必须不小于 32。
    /// </summary>
    public int HashSize { get; init; } = 32;
}
#endif