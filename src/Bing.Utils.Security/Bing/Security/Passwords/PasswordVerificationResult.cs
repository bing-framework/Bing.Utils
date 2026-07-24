#if !NETSTANDARD2_0
namespace Bing.Security.Passwords;

/// <summary>
/// 表示密码验证结果。
/// </summary>
public enum PasswordVerificationResult
{
    /// <summary>
    /// 密码或已编码哈希无效。
    /// </summary>
    Failed,

    /// <summary>
    /// 密码验证成功且参数符合当前安全策略。
    /// </summary>
    Success,

    /// <summary>
    /// 密码验证成功，但应使用当前参数重新生成哈希。
    /// </summary>
    SuccessRehashNeeded
}
#endif