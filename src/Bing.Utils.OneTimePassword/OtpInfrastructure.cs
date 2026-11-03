using System.Security.Cryptography;
using System.Text;

namespace Bing.OneTimePassword;

/// <summary>
/// 密文保护的不可变绑定上下文。
/// </summary>
public sealed class OtpSecretContext
{
    /// <summary>
    /// 创建保护上下文，所有字段都必须与解密时一致。
    /// </summary>
    /// <param name="applicationId">宿主应用的隔离标识；不能为空且不超过 128 个字符。</param>
    /// <param name="subjectId">应用内稳定账号标识；不能为空且不超过 256 个字符。</param>
    /// <param name="credentialId">目标 OTP 凭据标识；不能为空且不超过 64 个字符。</param>
    /// <param name="parameters">该凭据绑定时冻结的非空算法配置。</param>
    public OtpSecretContext(string applicationId, string subjectId, string credentialId, OtpParameters parameters)
    {
        ApplicationId = OtpGuard.Text(applicationId, 128, nameof(applicationId));
        SubjectId = OtpGuard.Text(subjectId, 256, nameof(subjectId));
        CredentialId = OtpGuard.Text(credentialId, 64, nameof(credentialId));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }
    /// <summary>
    /// 获取应用隔离标识。
    /// </summary>
    public string ApplicationId { get; }
    /// <summary>
    /// 获取账号标识。
    /// </summary>
    public string SubjectId { get; }
    /// <summary>
    /// 获取凭据标识。
    /// </summary>
    public string CredentialId { get; }
    /// <summary>
    /// 获取不可变算法参数。
    /// </summary>
    public OtpParameters Parameters { get; }
}

/// <summary>
/// 存储基础设施故障。
/// </summary>
public sealed class OtpStoreException : Exception
{
    /// <summary>
    /// 创建不含验证码或密钥的故障。
    /// </summary>
    /// <param name="message">不包含验证码或明文密钥的故障说明。</param>
    /// <param name="inner">导致当前存储故障的原始异常；没有内部异常时为 null。</param>
    public OtpStoreException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// 密钥保护或解密失败，不能当作普通错误验证码。
/// </summary>
public sealed class OtpProtectionException : Exception
{
    /// <summary>
    /// 创建保护器故障。
    /// </summary>
    /// <param name="message">不包含验证码或明文密钥的故障说明。</param>
    /// <param name="inner">导致当前保护故障的原始异常；没有内部异常时为 null。</param>
    public OtpProtectionException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// 达到请求速率或有界状态配额。
/// </summary>
public sealed class OtpRateLimitException : Exception
{
    /// <summary>
    /// 创建不暴露限流身份的拒绝结果。
    /// </summary>
    public OtpRateLimitException() : base("OTP 请求达到限制。") { }
}

/// <summary>
/// 管理操作缺少近期可信认证。
/// </summary>
public sealed class OtpAuthorizationException : Exception
{
    /// <summary>
    /// 创建管理认证拒绝。
    /// </summary>
    public OtpAuthorizationException() : base("OTP 管理操作需要近期可信认证。") { }
}

/// <summary>
/// 服务内部有界文本与摘要辅助函数。
/// </summary>
internal static class OtpGuard
{
    /// <summary>
    /// 校验非空且不含控制字符或 UTF16 代理项的有界字段。
    /// </summary>
    /// <param name="value">待校验文本。</param>
    /// <param name="max">允许的最大字符数。</param>
    /// <param name="name">用于异常参数信息的字段名称。</param>
    /// <returns>通过校验的原始文本。</returns>
    internal static string Text(string value, int max, string name)
    {
        if (value == null) throw new ArgumentNullException(name);
        if (string.IsNullOrWhiteSpace(value) || value.Length > max || value.Any(c => char.IsControl(c) || char.IsSurrogate(c)))
            throw new ArgumentException("字段为空、过长或包含不支持的字符。", name);
        return value;
    }
    /// <summary>
    /// 判断客户端标识是否满足有界格式。
    /// </summary>
    /// <param name="value">待校验标识；null 视为无效。</param>
    /// <returns>标识非空、不超过 64 个字符且只含 ASCII 字母、数字、连字符或下划线时为 true。</returns>
    internal static bool IsId(string? value) => value != null && value.Length > 0 && value.Length <= 64 && value.All(c => char.IsLetterOrDigit(c) && c <= 127 || c == '-' || c == '_');
    /// <summary>
    /// 计算 UTF8 文本的不可逆定位摘要，不承担身份认证。
    /// </summary>
    /// <param name="value">要计算摘要的文本。</param>
    /// <returns>小写十六进制 SHA-256 摘要。</returns>
    internal static string Digest(string value)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
    }
}
