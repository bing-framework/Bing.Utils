namespace Bing.OneTimePassword;

/// <summary>
/// 生命周期和限流配置，服务构造时复制并验证。
/// </summary>
public sealed class OtpServiceOptions
{
    /// <summary>
    /// 获取或设置应用隔离标识。
    /// </summary>
    public string ApplicationId { get; set; } = "Bing.OneTimePassword";
    /// <summary>
    /// 获取或设置注册 URI 的发行者。
    /// </summary>
    public string Issuer { get; set; } = "Bing";
    /// <summary>
    /// 获取或设置允许的服务端用途。
    /// </summary>
    public string[] AllowedPurposes { get; set; } = new[] { "login", "step-up", "manage" };
    /// <summary>
    /// 获取或设置管理用途。
    /// </summary>
    public string ManagementPurpose { get; set; } = "manage";
    /// <summary>
    /// 获取或设置待确认有效期。
    /// </summary>
    public TimeSpan EnrollmentLifetime { get; set; } = TimeSpan.FromMinutes(10);
    /// <summary>
    /// 获取或设置验证事务有效期。
    /// </summary>
    public TimeSpan TransactionLifetime { get; set; } = TimeSpan.FromMinutes(5);
    /// <summary>
    /// 获取或设置管理认证最大年龄。
    /// </summary>
    public TimeSpan RecentAuthenticationLifetime { get; set; } = TimeSpan.FromMinutes(5);
    /// <summary>
    /// 获取或设置失败计数窗口。
    /// </summary>
    public TimeSpan FailureWindow { get; set; } = TimeSpan.FromMinutes(15);
    /// <summary>
    /// 获取或设置临时锁定时长。
    /// </summary>
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
    /// <summary>
    /// 获取或设置账号和凭据失败锁定阈值。
    /// </summary>
    public int FailureLimit { get; set; } = 5;
    /// <summary>
    /// 获取或设置每事务的最大失败次数。
    /// </summary>
    public int TransactionFailureLimit { get; set; } = 5;
    /// <summary>
    /// 获取或设置有效设备上限。
    /// </summary>
    public int MaxCredentials { get; set; } = 5;
    /// <summary>
    /// 获取或设置同时有效的事务上限。
    /// </summary>
    public int MaxTransactions { get; set; } = 20;
    /// <summary>
    /// 获取或设置 TOTP 前后窗口，仅允许 0 或 1。
    /// </summary>
    public int TotpWindow { get; set; } = 1;
    /// <summary>
    /// 获取或设置 HOTP 前瞻偏移，范围 0 至 100。
    /// </summary>
    public int HotpLookAhead { get; set; } = 5;
    /// <summary>
    /// 获取或设置 HOTP 双码重同步最大偏移，范围 1 至 100。
    /// </summary>
    public int HotpResynchronizationWindow { get; set; } = 100;
    /// <summary>
    /// 获取或设置每 IP 每分钟请求数。
    /// </summary>
    public int IpRequestsPerMinute { get; set; } = 60;
    /// <summary>
    /// 获取或设置每账号每分钟请求数。
    /// </summary>
    public int SubjectRequestsPerMinute { get; set; } = 10;
    /// <summary>
    /// 获取或设置每凭据每分钟请求数。
    /// </summary>
    public int CredentialRequestsPerMinute { get; set; } = 10;
    /// <summary>
    /// 获取或设置每事务每分钟请求数。
    /// </summary>
    public int TransactionRequestsPerMinute { get; set; } = 10;
    /// <summary>
    /// 获取或设置每账号每分钟管理请求数。
    /// </summary>
    public int ManagementRequestsPerMinute { get; set; } = 5;

    /// <summary>
    /// 返回已验证的独立配置快照。
    /// </summary>
    /// <returns>配置快照。</returns>
    public OtpServiceOptions Snapshot()
    {
        var copy = (OtpServiceOptions)MemberwiseClone();
        copy.ApplicationId = OtpGuard.Text(ApplicationId, 128, nameof(ApplicationId));
        copy.Issuer = OtpGuard.Text(Issuer, 128, nameof(Issuer));
        if (Issuer.Contains(":")) throw new ArgumentException("发行者不能包含冒号。", nameof(Issuer));
        copy.ManagementPurpose = OtpGuard.Text(ManagementPurpose, 64, nameof(ManagementPurpose));
        if (AllowedPurposes == null || AllowedPurposes.Length == 0 || AllowedPurposes.Length > 32) throw new ArgumentException("用途数量必须为 1 至 32。");
        copy.AllowedPurposes = AllowedPurposes.Select(x => OtpGuard.Text(x, 64, nameof(AllowedPurposes))).Distinct(StringComparer.Ordinal).ToArray();
        if (!copy.AllowedPurposes.Contains(ManagementPurpose, StringComparer.Ordinal)) throw new ArgumentException("管理用途必须列入允许清单。");
        foreach (var duration in new[] { EnrollmentLifetime, TransactionLifetime, RecentAuthenticationLifetime, FailureWindow, LockoutDuration })
            if (duration < TimeSpan.FromSeconds(1) || duration > TimeSpan.FromDays(1)) throw new ArgumentOutOfRangeException(nameof(duration));
        foreach (var limit in new[] { FailureLimit, TransactionFailureLimit, MaxCredentials, MaxTransactions, IpRequestsPerMinute, SubjectRequestsPerMinute, CredentialRequestsPerMinute, TransactionRequestsPerMinute, ManagementRequestsPerMinute })
            if (limit < 1 || limit > 10000) throw new ArgumentOutOfRangeException(nameof(limit));
        if (MaxCredentials > 100 || MaxTransactions > 100) throw new ArgumentOutOfRangeException(nameof(MaxCredentials));
        if (TotpWindow < 0 || TotpWindow > 1 || HotpLookAhead < 0 || HotpLookAhead > 100 || HotpResynchronizationWindow < 1 || HotpResynchronizationWindow > 100) throw new ArgumentOutOfRangeException(nameof(TotpWindow));
        return copy;
    }
}
