namespace Bing.OneTimePassword;

/// <summary>
/// 存储适配器使用的可变账号事务状态，不能作为客户端响应。
/// </summary>
public sealed class OtpAccountState
{
    /// <summary>
    /// 创建指定账号的空状态。
    /// </summary>
    /// <param name="subjectId">应用内稳定账号标识；不能为空且不超过 256 个字符。</param>
    public OtpAccountState(string subjectId) { SubjectId = OtpGuard.Text(subjectId, 256, nameof(subjectId)); }
    /// <summary>
    /// 获取事务所属账号。
    /// </summary>
    public string SubjectId { get; }
    /// <summary>
    /// 获取凭据记录集合。
    /// </summary>
    public List<OtpCredentialState> Credentials { get; } = new();
    /// <summary>
    /// 获取验证事务集合。
    /// </summary>
    public List<OtpTransactionState> Transactions { get; } = new();
    /// <summary>
    /// 获取或设置账号累计失败状态。
    /// </summary>
    public OtpFailureState Failure { get; set; } = new();
}

/// <summary>
/// 存储适配器使用的凭据记录，仅在隔离事务中修改。
/// </summary>
public sealed class OtpCredentialState
{
    /// <summary>
    /// 获取或设置随机凭据标识。
    /// </summary>
    public string CredentialId { get; set; } = "";
    /// <summary>
    /// 获取或设置受控设备名称。
    /// </summary>
    public string DisplayName { get; set; } = "";
    /// <summary>
    /// 获取或设置绑定时冻结的算法配置。
    /// </summary>
    public OtpParameters Parameters { get; set; } = new();
    /// <summary>
    /// 获取或设置密文字节，禁止发送给客户端。
    /// </summary>
    public byte[] ProtectedSecret { get; set; } = Array.Empty<byte>();
    /// <summary>
    /// 获取或设置生命周期状态。
    /// </summary>
    public OtpCredentialStatus Status { get; set; }
    /// <summary>
    /// 获取或设置下一个 HOTP 计数器。
    /// </summary>
    public ulong NextCounter { get; set; }
    /// <summary>
    /// 获取或设置计数器是否耗尽。
    /// </summary>
    public bool CounterExhausted { get; set; }
    /// <summary>
    /// 获取或设置 TOTP 最后消费步，未使用为 -1。
    /// </summary>
    public long LastAcceptedStep { get; set; } = -1;
    /// <summary>
    /// 获取或设置 UTC 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>
    /// 获取或设置最近成功使用时间。
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }
    /// <summary>
    /// 获取或设置待确认失效时间。
    /// </summary>
    public DateTimeOffset? PendingExpiresAt { get; set; }
    /// <summary>
    /// 获取或设置凭据累计失败状态。
    /// </summary>
    public OtpFailureState Failure { get; set; } = new();
}

/// <summary>
/// 与账号、会话、用途绑定的短时验证事务记录。
/// </summary>
public sealed class OtpTransactionState
{
    /// <summary>
    /// 获取或设置随机事务标识。
    /// </summary>
    public string TransactionId { get; set; } = "";
    /// <summary>
    /// 获取或设置服务端固定用途。
    /// </summary>
    public string Purpose { get; set; } = "";
    /// <summary>
    /// 获取或设置可信会话的 SHA256 摘要。
    /// </summary>
    public string BindingHash { get; set; } = "";
    /// <summary>
    /// 获取或设置绑定/重同步的指定凭据，普通验证为空。
    /// </summary>
    public string? CredentialId { get; set; }
    /// <summary>
    /// 获取或设置操作种类。
    /// </summary>
    public OtpTransactionKind Kind { get; set; }
    /// <summary>
    /// 获取或设置绝对过期时间。
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
    /// <summary>
    /// 获取或设置本事务累计失败次数。
    /// </summary>
    public int FailureCount { get; set; }
    /// <summary>
    /// 获取或设置事务是否已消费。
    /// </summary>
    public bool Consumed { get; set; }
}

/// <summary>
/// 账号或凭据的持久失败窗口与锁定状态。
/// </summary>
public sealed class OtpFailureState
{
    /// <summary>
    /// 获取或设置当前失败窗口的起点。
    /// </summary>
    public DateTimeOffset? WindowStart { get; set; }
    /// <summary>
    /// 获取或设置窗口内累计失败次数。
    /// </summary>
    public int Count { get; set; }
    /// <summary>
    /// 获取或设置锁定截止时间。
    /// </summary>
    public DateTimeOffset? LockedUntil { get; set; }
}
