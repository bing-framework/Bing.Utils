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
    /// 允许验证和生成的最大 PBKDF2 迭代次数。
    /// </summary>
    public const int MaximumIterationCount = 1000000;

    /// <summary>
    /// 允许的最大密码盐字节长度。
    /// </summary>
    public const int MaximumSaltSize = 64;

    /// <summary>
    /// 允许的最大派生哈希字节长度。
    /// </summary>
    public const int MaximumHashSize = 64;

    /// <summary>
    /// 允许验证的最大编码密码记录字符数。
    /// </summary>
    public const int MaximumEncodedHashLength = 512;

    /// <summary>
    /// PBKDF2 迭代次数，必须介于 100000 和 1000000 之间。
    /// </summary>
    public int IterationCount { get; init; } = DefaultIterationCount;

    /// <summary>
    /// 随机盐字节长度，必须介于 16 和 64 之间。
    /// </summary>
    public int SaltSize { get; init; } = 16;

    /// <summary>
    /// 派生哈希字节长度，必须介于 32 和 64 之间。
    /// </summary>
    public int HashSize { get; init; } = 32;
}
#endif