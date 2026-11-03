using System.Globalization;
using System.Security.Cryptography;
using Bing.OneTimePassword;
using Microsoft.AspNetCore.DataProtection;

namespace Bing.OneTimePassword.DataProtection;

/// <summary>
/// 借用宿主 Data Protection 密钥环保护 OTP 共享密钥。
/// </summary>
/// <remarks>宿主必须持久化并保护密钥环；本实现不创建或释放密钥管理服务。</remarks>
public sealed class DataProtectionOtpSecretProtector : IOtpSecretProtector
{
    /// <summary>
    /// 宿主持有的保护提供器。
    /// </summary>
    private readonly IDataProtectionProvider _provider;

    /// <summary>
    /// 创建密钥保护适配器。
    /// </summary>
    /// <param name="provider">宿主管理的提供器。</param>
    public DataProtectionOtpSecretProtector(IDataProtectionProvider provider)
    { _provider = provider ?? throw new ArgumentNullException(nameof(provider)); }

    /// <inheritdoc />
    public byte[] Protect(OtpSecretContext context, byte[] secret)
    {
        if (secret == null) throw new ArgumentNullException(nameof(secret));
        if (secret.Length < 16 || secret.Length > 128) throw new ArgumentOutOfRangeException(nameof(secret));
        try { return Create(context).Protect(secret); }
        catch (CryptographicException e) { throw new OtpProtectionException("OTP 密钥保护失败。", e); }
    }

    /// <inheritdoc />
    public byte[] Unprotect(OtpSecretContext context, byte[] protectedSecret)
    {
        if (protectedSecret == null) throw new ArgumentNullException(nameof(protectedSecret));
        if (protectedSecret.Length < 1 || protectedSecret.Length > 16384) throw new OtpProtectionException("OTP 密文长度无效。");
        try
        {
            var secret = Create(context).Unprotect(protectedSecret);
            if (secret.Length < 16 || secret.Length > 128)
            {
                Array.Clear(secret, 0, secret.Length);
                throw new OtpProtectionException("OTP 明文密钥长度无效。");
            }
            return secret;
        }
        catch (CryptographicException e) { throw new OtpProtectionException("OTP 密钥或绑定上下文不匹配。", e); }
    }

    /// <summary>
    /// 使用分层用途避免拼接歧义，认证所有不可变凭据字段。
    /// </summary>
    private IDataProtector Create(OtpSecretContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        var p = context.Parameters;
        return _provider.CreateProtector("Bing.OneTimePassword.Secret.v1")
            .CreateProtector(context.ApplicationId).CreateProtector(context.SubjectId).CreateProtector(context.CredentialId)
            .CreateProtector(((int)p.Type).ToString(CultureInfo.InvariantCulture))
            .CreateProtector(((int)p.Algorithm).ToString(CultureInfo.InvariantCulture))
            .CreateProtector(p.Digits.ToString(CultureInfo.InvariantCulture))
            .CreateProtector(p.PeriodSeconds.ToString(CultureInfo.InvariantCulture));
    }
}
