namespace Bing.OneTimePassword;

/// <summary>
/// 宿主认证后建立的可信验证上下文。
/// </summary>
/// <remarks>不能从 HTTP 请求体直接绑定本类型；认证时间、账号和网络地址必须由宿主确定。</remarks>
public sealed class OtpContext
{
    /// <summary>
    /// 创建可信上下文。
    /// </summary>
    /// <param name="subjectId">宿主认证得到的应用内稳定账号标识；不能为空且不超过 256 字符。</param>
    /// <param name="purpose">服务端用途标识，最多 64 个字符且必须列入服务配置的允许用途。</param>
    /// <param name="bindingId">宿主生成且在事务生命周期内稳定的会话绑定值；不得直接取自客户端请求，最多 256 个字符。</param>
    /// <param name="authenticatedAt">宿主确认的主认证时间；管理操作需提供，值会转换为 UTC。</param>
    /// <param name="clientAddress">可信代理解析后的客户端地址，用于限流；最多 128 个字符，缺省为 unknown。</param>
    public OtpContext(string subjectId, string purpose, string bindingId, DateTimeOffset? authenticatedAt = null, string clientAddress = "unknown")
    {
        SubjectId = OtpGuard.Text(subjectId, 256, nameof(subjectId));
        Purpose = OtpGuard.Text(purpose, 64, nameof(purpose));
        BindingId = OtpGuard.Text(bindingId, 256, nameof(bindingId));
        ClientAddress = OtpGuard.Text(clientAddress, 128, nameof(clientAddress));
        AuthenticatedAt = authenticatedAt?.ToUniversalTime();
    }
    /// <summary>
    /// 获取可信账号。
    /// </summary>
    public string SubjectId { get; }
    /// <summary>
    /// 获取服务端用途。
    /// </summary>
    public string Purpose { get; }
    /// <summary>
    /// 获取可信会话绑定值。
    /// </summary>
    public string BindingId { get; }
    /// <summary>
    /// 获取最近认证时间。
    /// </summary>
    public DateTimeOffset? AuthenticatedAt { get; }
    /// <summary>
    /// 获取可信网络地址。
    /// </summary>
    public string ClientAddress { get; }
}

/// <summary>
/// 持久化凭据的生命周期状态。
/// </summary>
public enum OtpCredentialStatus
{
    /// <summary>
    /// 尚未确认。
    /// </summary>
    Pending = 1,
    /// <summary>
    /// 已激活。
    /// </summary>
    Active = 2,
    /// <summary>
    /// 已撤销且不再持有密文。
    /// </summary>
    Revoked = 3
}

/// <summary>
/// 限定验证事务的操作种类。
/// </summary>
public enum OtpTransactionKind
{
    /// <summary>
    /// 普通二次验证。
    /// </summary>
    Verification = 1,
    /// <summary>
    /// 凭据绑定确认。
    /// </summary>
    Enrollment = 2,
    /// <summary>
    /// HOTP 双码重同步。
    /// </summary>
    Resynchronization = 3
}

/// <summary>
/// 服务内部验证状态，默认值表示拒绝。
/// </summary>
public enum OtpVerifyStatus
{
    /// <summary>
    /// 请求、凭据或验证码无效。
    /// </summary>
    Invalid = 0,
    /// <summary>
    /// 已原子消费并成功验证。
    /// </summary>
    Success = 1,
    /// <summary>
    /// 时间步已使用。
    /// </summary>
    Replay = 2,
    /// <summary>
    /// 账号或凭据被临时锁定。
    /// </summary>
    Locked = 3,
    /// <summary>
    /// 计数器已经耗尽。
    /// </summary>
    CounterExhausted = 4
}

/// <summary>
/// 不包含密钥或验证码的校验结果。
/// </summary>
public sealed class OtpVerifyResult
{
    /// <summary>
    /// 创建结果。
    /// </summary>
    /// <param name="status">服务内部校验结果；<see cref="IOtpService"/> 仅在对应持久状态提交后返回 Success。</param>
    public OtpVerifyResult(OtpVerifyStatus status) { Status = status; }
    /// <summary>
    /// 获取内部状态；Web 应统一隐藏普通失败原因。
    /// </summary>
    public OtpVerifyStatus Status { get; }
    /// <summary>
    /// 获取是否已经成功消费。
    /// </summary>
    public bool Succeeded => Status == OtpVerifyStatus.Success;
}

/// <summary>
/// 单次绑定响应，含只能在绑定时展示的敏感信息。
/// </summary>
public sealed class OtpEnrollment
{
    /// <summary>
    /// 创建绑定响应。
    /// </summary>
    /// <param name="credentialId">新建凭据的标识。</param>
    /// <param name="transactionId">绑定确认使用的一次性事务标识。</param>
    /// <param name="secret">只在绑定流程中展示的 Base32 明文密钥。</param>
    /// <param name="uri">包含明文密钥的注册 URI，属于敏感信息。</param>
    /// <param name="expiresAt">待确认凭据的绝对失效时间。</param>
    internal OtpEnrollment(string credentialId, string transactionId, string secret, string uri, DateTimeOffset expiresAt)
    { CredentialId = credentialId; TransactionId = transactionId; SecretBase32 = secret; ProvisioningUri = uri; ExpiresAt = expiresAt; }
    /// <summary>
    /// 获取公开凭据 ID。
    /// </summary>
    public string CredentialId { get; }
    /// <summary>
    /// 获取绑定确认事务 ID。
    /// </summary>
    public string TransactionId { get; }
    /// <summary>
    /// 获取一次性展示的 Base32 共享密钥，禁止记录日志。
    /// </summary>
    public string SecretBase32 { get; }
    /// <summary>
    /// 获取含共享密钥的注册 URI，禁止记录日志。
    /// </summary>
    public string ProvisioningUri { get; }
    /// <summary>
    /// 获取待确认凭据的绝对失效时间。
    /// </summary>
    public DateTimeOffset ExpiresAt { get; }
}

/// <summary>
/// 不携带密钥的验证事务句柄。
/// </summary>
public sealed class OtpTransaction
{
    /// <summary>
    /// 创建验证事务句柄。
    /// </summary>
    /// <param name="id">随机生成的事务标识。</param>
    /// <param name="expiresAt">事务的绝对失效时间。</param>
    internal OtpTransaction(string id, DateTimeOffset expiresAt) { TransactionId = id; ExpiresAt = expiresAt; }
    /// <summary>
    /// 获取随机事务 ID。
    /// </summary>
    public string TransactionId { get; }
    /// <summary>
    /// 获取绝对过期时间。
    /// </summary>
    public DateTimeOffset ExpiresAt { get; }
}

/// <summary>
/// 可返回给账号持有者的凭据元数据。
/// </summary>
public sealed class OtpCredentialInfo
{
    /// <summary>
    /// 从存储记录复制公开字段。
    /// </summary>
    /// <param name="state">当前账号隔离事务中的凭据状态。</param>
    internal OtpCredentialInfo(OtpCredentialState state)
    { CredentialId = state.CredentialId; DisplayName = state.DisplayName; Parameters = state.Parameters; Status = state.Status; CreatedAt = state.CreatedAt; LastUsedAt = state.LastUsedAt; }
    /// <summary>
    /// 获取凭据 ID。
    /// </summary>
    public string CredentialId { get; }
    /// <summary>
    /// 获取设备显示名。
    /// </summary>
    public string DisplayName { get; }
    /// <summary>
    /// 获取固定算法配置。
    /// </summary>
    public OtpParameters Parameters { get; }
    /// <summary>
    /// 获取生命周期状态。
    /// </summary>
    public OtpCredentialStatus Status { get; }
    /// <summary>
    /// 获取创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; }
    /// <summary>
    /// 获取最近使用时间；未使用时为 null。
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; }
}

/// <summary>
/// 一次性口令的完整生命周期入口。
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// 生成新密钥并创建待确认凭据；要求近期管理认证。
    /// </summary>
    /// <param name="context">由宿主认证层构造的可信上下文；必须使用管理用途和近期认证时间。</param>
    /// <param name="parameters">绑定后固定不变的 OTP 类型、算法、位数和周期配置。</param>
    /// <param name="accountName">用于注册 URI 标签的账号名称；不得为空或超过 256 个字符。</param>
    /// <param name="displayName">设备显示名称；不得为空或超过 128 个字符。</param>
    /// <param name="cancellationToken">用于取消操作；提交前观察到取消会回滚持久状态，提交后的状态不会恢复。</param>
    /// <returns>异步返回仅本次展示的密钥、注册 URI 和确认句柄。</returns>
    Task<OtpEnrollment> BeginEnrollmentAsync(OtpContext context, OtpParameters parameters, string accountName, string displayName = "Authenticator", CancellationToken cancellationToken = default);
    /// <summary>
    /// 原子确认绑定并消费首次时间步或计数器。
    /// </summary>
    /// <param name="context">由宿主认证层构造的可信管理上下文。</param>
    /// <param name="credentialId">当前账号待确认凭据的标识。</param>
    /// <param name="transactionId">创建待确认凭据时返回的事务标识。</param>
    /// <param name="code">认证器生成的敏感验证码；必须是凭据配置位数的 ASCII 数字，null 或格式错误按失败处理且不得记录。</param>
    /// <param name="cancellationToken">用于取消操作；提交前观察到取消会回滚持久状态，提交后的状态不会恢复。</param>
    /// <returns>异步返回绑定确认的校验结果。</returns>
    Task<OtpVerifyResult> ConfirmEnrollmentAsync(OtpContext context, string credentialId, string transactionId, string? code, CancellationToken cancellationToken = default);
    /// <summary>
    /// 在主认证后创建验证事务；重同步事务要求近期管理认证与目标凭据。
    /// </summary>
    /// <param name="context">由宿主认证层构造的可信上下文；重同步用途需提供近期管理认证。</param>
    /// <param name="kind">事务类型；普通验证使用上下文用途，重同步创建 HOTP 专用事务。</param>
    /// <param name="credentialId">仅重同步时必填的当前账号 HOTP 凭据标识；普通验证必须为 null。</param>
    /// <param name="cancellationToken">用于取消事务创建；提交前观察到取消时不会保存新事务。</param>
    /// <returns>异步返回绑定当前账号、用途和会话的事务句柄。</returns>
    Task<OtpTransaction> BeginVerificationAsync(OtpContext context, OtpTransactionKind kind = OtpTransactionKind.Verification, string? credentialId = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// 验证当前凭据并原子消费对应事务。
    /// </summary>
    /// <param name="context">由宿主认证层构造的可信上下文，必须与创建事务时的账号、用途和会话一致。</param>
    /// <param name="credentialId">当前账号待验证凭据的标识。</param>
    /// <param name="transactionId">此前创建且尚未过期或消费的验证事务标识。</param>
    /// <param name="code">认证器生成的敏感验证码；必须是凭据配置位数的 ASCII 数字，null 或格式错误按失败处理且不得记录。</param>
    /// <param name="cancellationToken">用于取消操作；提交前观察到取消会回滚持久状态，提交后的状态不会恢复。</param>
    /// <returns>异步返回校验结果；只有持久提交成功时结果才为成功。</returns>
    Task<OtpVerifyResult> VerifyAsync(OtpContext context, string credentialId, string transactionId, string? code, CancellationToken cancellationToken = default);
    /// <summary>
    /// 返回当前账号的设备元数据。
    /// </summary>
    /// <param name="context">由宿主认证层构造的可信账号上下文。</param>
    /// <param name="cancellationToken">用于取消查询。</param>
    /// <returns>异步返回不包含密钥、密文或计数器的只读列表。</returns>
    Task<IReadOnlyList<OtpCredentialInfo>> GetCredentialsAsync(OtpContext context, CancellationToken cancellationToken = default);
    /// <summary>
    /// 撤销账号所属凭据并使未完成事务失效；要求近期管理认证。
    /// </summary>
    /// <param name="context">由宿主认证层构造的可信管理上下文；必须包含近期认证时间。</param>
    /// <param name="credentialId">当前账号所属凭据的标识。</param>
    /// <param name="cancellationToken">用于取消操作；提交前观察到取消会回滚撤销，提交后的状态不会恢复。</param>
    /// <returns>异步返回是否找到并撤销凭据；找到并撤销时为 true，否则为 false。</returns>
    Task<bool> RevokeAsync(OtpContext context, string credentialId, CancellationToken cancellationToken = default);
    /// <summary>
    /// 使用连续两码确认 HOTP 重同步，匹配有歧义时拒绝。
    /// </summary>
    /// <param name="context">由宿主认证层构造的可信管理上下文；必须与重同步事务的账号、用途和会话一致，并包含近期认证时间。</param>
    /// <param name="credentialId">当前账号待重同步的 HOTP 凭据标识。</param>
    /// <param name="transactionId">此前创建且绑定该凭据的重同步事务标识。</param>
    /// <param name="firstCode">认证器连续生成的第一条敏感验证码；须为凭据配置位数的 ASCII 数字，null 或格式错误按失败处理且不得记录。</param>
    /// <param name="secondCode">紧随第一条之后生成的第二条敏感验证码；须为凭据配置位数的 ASCII 数字，null 或格式错误按失败处理且不得记录。</param>
    /// <param name="cancellationToken">用于取消操作；提交前观察到取消会回滚计数器更新，提交后的状态不会恢复。</param>
    /// <returns>异步返回重同步校验结果；成功时计数器已原子推进。</returns>
    Task<OtpVerifyResult> ResynchronizeHotpAsync(OtpContext context, string credentialId, string transactionId, string? firstCode, string? secondCode, CancellationToken cancellationToken = default);
}

/// <summary>
/// 可注入的 UTC 时钟。
/// </summary>
public interface IOtpClock
{
    /// <summary>
    /// 获取当前 UTC 时间。
    /// </summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// 使用系统 UTC 的时钟。
/// </summary>
public sealed class SystemOtpClock : IOtpClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>
/// 本地共享密钥保护器，事务内调用不得执行网络操作。
/// </summary>
public interface IOtpSecretProtector
{
    /// <summary>
    /// 按凭据不可变上下文保护明文密钥。
    /// </summary>
    /// <param name="context">包含应用、账号、凭据及固定算法参数的保护上下文。</param>
    /// <param name="secret">调用期间借用的 OTP 明文字节，长度为 16 至 128 字节；适配器不得在调用结束后保留该数组引用。</param>
    /// <returns>由调用方拥有的独立密文字节数组，长度不得超过 16384 字节。</returns>
    byte[] Protect(OtpSecretContext context, byte[] secret);
    /// <summary>
    /// 验证上下文后解密；调用方负责清理返回的密钥。
    /// </summary>
    /// <param name="context">必须与保护密钥时完全一致的应用、账号、凭据及算法上下文。</param>
    /// <param name="protectedSecret">待解密的密文字节；调用期间借用，适配器不得保留其数组引用。</param>
    /// <returns>新分配且由调用方拥有的明文字节数组，长度为 16 至 128 字节；调用方应在使用后清理。</returns>
    byte[] Unprotect(OtpSecretContext context, byte[] protectedSecret);
}

/// <summary>
/// 提供账号级串行化持久事务的存储。
/// </summary>
/// <remarks>回调只允许同步本地运算，不得发起网络操作；异常或提交前取消必须回滚；提交未知不自动重试。</remarks>
public interface IOtpStore
{
    /// <summary>
    /// 在隔离事务中加载账号状态，执行同步回调并原子保存。
    /// </summary>
    /// <param name="subjectId">本次隔离和持久化使用的应用内账号标识。</param>
    /// <param name="operation">接收账号状态并同步修改或读取的本地回调；不得执行网络操作或返回异步任务。</param>
    /// <param name="cancellationToken">取消令牌；提交前观察到取消时必须回滚事务。</param>
    /// <typeparam name="T">回调返回值，不得是异步操作。</typeparam>
    /// <returns>仅在存储事务提交后完成并返回回调结果的任务。</returns>
    Task<T> ExecuteAsync<T>(string subjectId, Func<OtpAccountState, T> operation, CancellationToken cancellationToken = default);
    /// <summary>
    /// 有界清理过期绑定、事务及撤销记录，不移除有效凭据的重放状态。
    /// </summary>
    /// <param name="now">用于判定过期状态的 UTC 当前时间。</param>
    /// <param name="maxRecords">本次清理最多处理的记录数，范围为 1 至 10000。</param>
    /// <param name="cancellationToken">取消令牌；提交前观察到取消时必须回滚清理事务。</param>
    /// <returns>清理任务完成后返回已处理的记录数量。</returns>
    Task<int> CleanupAsync(DateTimeOffset now, int maxRecords = 256, CancellationToken cancellationToken = default);
}

/// <summary>
/// 固定窗口单维度限流器，不拥有宿主连接。
/// </summary>
public interface IOtpRateLimiter
{
    /// <summary>
    /// 原子计费；被其他维度拒绝的请求不退还本维度额度。
    /// </summary>
    /// <param name="partition">区分独立限流维度的键；不得为空或超过 512 个字符，也不得使用验证码或密钥。</param>
    /// <param name="limit">固定窗口内允许的最多请求数，范围为 1 至 10000。</param>
    /// <param name="window">计费窗口时长，范围为 1 秒至 1 天。</param>
    /// <param name="cancellationToken">取消令牌；基础设施操作取消时传播取消异常。</param>
    /// <returns>任务完成后，额度内为 true、超额为 false；基础设施故障抛异常。</returns>
    Task<bool> TryAcquireAsync(string partition, int limit, TimeSpan window, CancellationToken cancellationToken = default);
}
