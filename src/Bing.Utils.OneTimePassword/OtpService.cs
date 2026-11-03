using System.Diagnostics.Metrics;
using Bing.Security.Randomness;

namespace Bing.OneTimePassword;

/// <summary>
/// 使用原子账号事务执行 OTP 生命周期的线程安全服务。
/// </summary>
/// <remarks>不拥有注入资源；所有账号与认证上下文必须来自可信宿主。</remarks>
public sealed class OtpService : IOtpService
{
    /// <summary>
    /// 固定名称的诊断源。
    /// </summary>
    private static readonly Meter Meter = new("Bing.Utils.OneTimePassword", "1.0");
    /// <summary>
    /// 不含账号或凭据标识的事件计数。
    /// </summary>
    private static readonly Counter<long> Events = Meter.CreateCounter<long>("otp.events");
    /// <summary>
    /// 持久事务存储。
    /// </summary>
    private readonly IOtpStore _store;
    /// <summary>
    /// 本地密钥保护器。
    /// </summary>
    private readonly IOtpSecretProtector _protector;
    /// <summary>
    /// 明确选择的限流器。
    /// </summary>
    private readonly IOtpRateLimiter _limiter;
    /// <summary>
    /// UTC 时钟。
    /// </summary>
    private readonly IOtpClock _clock;
    /// <summary>
    /// 已验证的配置快照。
    /// </summary>
    private readonly OtpServiceOptions _options;

    /// <summary>
    /// 创建服务并验证所有必要组件。
    /// </summary>
    /// <param name="store">账号隔离事务存储；由调用方或 DI 容器管理生命周期，本服务不释放它。</param>
    /// <param name="protector">用于绑定并保护共享密钥的本地适配器；由调用方或 DI 容器管理生命周期。</param>
    /// <param name="limiter">请求限流适配器；由调用方或 DI 容器管理生命周期。</param>
    /// <param name="options">服务配置；构造时复制并验证，后续修改原对象不会影响此服务。</param>
    /// <param name="clock">提供 UTC 时间的时钟；为 null 时使用系统 UTC 时钟，本服务不会释放注入的时钟。</param>
    public OtpService(IOtpStore store, IOtpSecretProtector protector, IOtpRateLimiter limiter, OtpServiceOptions? options = null, IOtpClock? clock = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _limiter = limiter ?? throw new ArgumentNullException(nameof(limiter));
        _clock = clock ?? new SystemOtpClock();
        _options = (options ?? new OtpServiceOptions()).Snapshot();
    }

    /// <inheritdoc />
    public async Task<OtpEnrollment> BeginEnrollmentAsync(OtpContext context, OtpParameters parameters, string accountName, string displayName = "Authenticator", CancellationToken cancellationToken = default)
    {
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        OtpGuard.Text(accountName, 256, nameof(accountName));
        OtpGuard.Text(displayName, 128, nameof(displayName));
        await RateAsync(context, true, null, null, cancellationToken).ConfigureAwait(false);
        var secret = SecurityRandom.GetBytes(parameters.Algorithm == OtpHashAlgorithm.Sha512 ? 64 : 32);
        try
        {
            var id = SecurityRandom.GetBase64Url(16);
            var uri = OtpProvisioningUri.Create(parameters, _options.Issuer, accountName, secret);
            var protectedSecret = Protect(context, id, parameters, secret);
            var result = await StoreAsync(context, state =>
            {
                var now = _clock.UtcNow;
                Prune(state, now);
                if (IsLocked(state.Failure, now)) throw new OtpRateLimitException();
                if (state.Credentials.Count(x => x.Status == OtpCredentialStatus.Active) >= _options.MaxCredentials) throw new OtpRateLimitException();
                var pending = state.Credentials.Where(x => x.Status == OtpCredentialStatus.Pending).Select(x => x.CredentialId).ToArray();
                state.Credentials.RemoveAll(x => x.Status == OtpCredentialStatus.Pending);
                state.Transactions.RemoveAll(x => x.Kind == OtpTransactionKind.Enrollment || pending.Contains(x.CredentialId));
                var expires = now.Add(_options.EnrollmentLifetime);
                var transaction = NewTransaction(state, context, OtpTransactionKind.Enrollment, id, expires);
                state.Credentials.Add(new OtpCredentialState
                {
                    CredentialId = id, DisplayName = displayName, Parameters = parameters,
                    ProtectedSecret = protectedSecret, Status = OtpCredentialStatus.Pending,
                    CreatedAt = now, PendingExpiresAt = expires
                });
                return new OtpEnrollment(id, transaction.TransactionId, OtpSecretEncoding.Encode(secret), uri, expires);
            }, cancellationToken).ConfigureAwait(false);
            Metric(context, "enrollment_started");
            return result;
        }
        finally { Array.Clear(secret, 0, secret.Length); }
    }

    /// <inheritdoc />
    public Task<OtpVerifyResult> ConfirmEnrollmentAsync(OtpContext context, string credentialId, string transactionId, string? code, CancellationToken cancellationToken = default)
        => VerifyCoreAsync(context, credentialId, transactionId, code, null, OtpTransactionKind.Enrollment, cancellationToken);

    /// <inheritdoc />
    public async Task<OtpTransaction> BeginVerificationAsync(OtpContext context, OtpTransactionKind kind = OtpTransactionKind.Verification, string? credentialId = null, CancellationToken cancellationToken = default)
    {
        if (kind != OtpTransactionKind.Verification && kind != OtpTransactionKind.Resynchronization) throw new ArgumentOutOfRangeException(nameof(kind));
        if (kind == OtpTransactionKind.Resynchronization && !OtpGuard.IsId(credentialId)) throw new ArgumentException("重同步需要目标凭据。", nameof(credentialId));
        if (kind == OtpTransactionKind.Verification && credentialId != null) throw new ArgumentException("普通事务不预绑定设备。", nameof(credentialId));
        await RateAsync(context, kind == OtpTransactionKind.Resynchronization, credentialId, null, cancellationToken).ConfigureAwait(false);
        return await StoreAsync(context, state =>
        {
            var now = _clock.UtcNow;
            Prune(state, now);
            if (IsLocked(state.Failure, now)) throw new OtpRateLimitException();
            if (!state.Credentials.Any(x => x.Status == OtpCredentialStatus.Active && (kind == OtpTransactionKind.Verification || x.CredentialId == credentialId && x.Parameters.Type == OtpType.Hotp)))
                throw new OtpAuthorizationException();
            var expires = now.Add(_options.TransactionLifetime);
            var record = NewTransaction(state, context, kind, credentialId, expires);
            return new OtpTransaction(record.TransactionId, expires);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<OtpVerifyResult> VerifyAsync(OtpContext context, string credentialId, string transactionId, string? code, CancellationToken cancellationToken = default)
        => VerifyCoreAsync(context, credentialId, transactionId, code, null, OtpTransactionKind.Verification, cancellationToken);

    /// <inheritdoc />
    public Task<OtpVerifyResult> ResynchronizeHotpAsync(OtpContext context, string credentialId, string transactionId, string? firstCode, string? secondCode, CancellationToken cancellationToken = default)
        => VerifyCoreAsync(context, credentialId, transactionId, firstCode, secondCode, OtpTransactionKind.Resynchronization, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<OtpCredentialInfo>> GetCredentialsAsync(OtpContext context, CancellationToken cancellationToken = default)
    {
        await RateAsync(context, false, null, null, cancellationToken).ConfigureAwait(false);
        return await StoreAsync<IReadOnlyList<OtpCredentialInfo>>(context, state =>
        {
            Prune(state, _clock.UtcNow);
            return Array.AsReadOnly(state.Credentials.Select(x => new OtpCredentialInfo(x)).ToArray());
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RevokeAsync(OtpContext context, string credentialId, CancellationToken cancellationToken = default)
    {
        await RateAsync(context, true, credentialId, null, cancellationToken).ConfigureAwait(false);
        if (!OtpGuard.IsId(credentialId)) return false;
        var revoked = await StoreAsync(context, state =>
        {
            var credential = state.Credentials.FirstOrDefault(x => x.CredentialId == credentialId && x.Status != OtpCredentialStatus.Revoked);
            if (credential == null) return false;
            credential.Status = OtpCredentialStatus.Revoked;
            Array.Clear(credential.ProtectedSecret, 0, credential.ProtectedSecret.Length);
            credential.ProtectedSecret = Array.Empty<byte>();
            // 凭据集合改变后，先前建立的所有验证事务失效。
            state.Transactions.Clear();
            return true;
        }, cancellationToken).ConfigureAwait(false);
        Metric(context, revoked ? "revoked" : "revoke_invalid");
        return revoked;
    }

    /// <summary>
    /// 在同一个隔离事务中验证、推进状态并消费事务。
    /// </summary>
    /// <param name="context">宿主提供的可信账号、用途和会话上下文。</param>
    /// <param name="credentialId">当前账号待操作凭据的标识。</param>
    /// <param name="transactionId">此前创建的绑定事务标识。</param>
    /// <param name="code">第一条敏感验证码；不得记录或写入日志。</param>
    /// <param name="secondCode">可选的第二条敏感验证码，仅重同步时使用；不得记录或写入日志。</param>
    /// <param name="kind">绑定、普通验证或 HOTP 重同步事务类型。</param>
    /// <param name="token">用于取消存储操作的令牌。</param>
    /// <returns>提交后的验证结果。</returns>
    private async Task<OtpVerifyResult> VerifyCoreAsync(OtpContext context, string credentialId, string transactionId, string? code, string? secondCode, OtpTransactionKind kind, CancellationToken token)
    {
        await RateAsync(context, kind != OtpTransactionKind.Verification, credentialId, transactionId, token).ConfigureAwait(false);
        if (!OtpGuard.IsId(credentialId) || !OtpGuard.IsId(transactionId))
        {
            Metric(context, "invalid");
            return new OtpVerifyResult(OtpVerifyStatus.Invalid);
        }
        OtpParameters? matchedParameters = null;
        var result = await StoreAsync(context, state =>
        {
            var now = _clock.UtcNow;
            Prune(state, now);
            var transaction = state.Transactions.FirstOrDefault(x => x.TransactionId == transactionId);
            var credential = state.Credentials.FirstOrDefault(x => x.CredentialId == credentialId);
            matchedParameters = credential?.Parameters;
            if (transaction == null || credential == null || transaction.Kind != kind || transaction.Purpose != context.Purpose ||
                transaction.BindingHash != OtpGuard.Digest(context.BindingId) || transaction.Consumed ||
                transaction.ExpiresAt <= now || transaction.FailureCount >= _options.TransactionFailureLimit ||
                transaction.CredentialId != null && transaction.CredentialId != credentialId)
                return new OtpVerifyResult(OtpVerifyStatus.Invalid);
            var expectedStatus = kind == OtpTransactionKind.Enrollment ? OtpCredentialStatus.Pending : OtpCredentialStatus.Active;
            if (credential.Status != expectedStatus || kind == OtpTransactionKind.Resynchronization && credential.Parameters.Type != OtpType.Hotp)
                return new OtpVerifyResult(OtpVerifyStatus.Invalid);
            if (IsLocked(state.Failure, now) || IsLocked(credential.Failure, now)) return new OtpVerifyResult(OtpVerifyStatus.Locked);
            if (credential.CounterExhausted) return new OtpVerifyResult(OtpVerifyStatus.CounterExhausted);
            var secret = Unprotect(context.SubjectId, credential);
            OtpVerifyStatus status;
            try { status = MatchAndAdvance(credential, secret, code, secondCode, kind, now); }
            finally { Array.Clear(secret, 0, secret.Length); }
            if (status != OtpVerifyStatus.Success)
            {
                transaction.FailureCount++;
                RegisterFailure(state.Failure, now);
                RegisterFailure(credential.Failure, now);
                return new OtpVerifyResult(status);
            }
            credential.Status = OtpCredentialStatus.Active;
            credential.PendingExpiresAt = null;
            credential.LastUsedAt = now;
            transaction.Consumed = true;
            return new OtpVerifyResult(OtpVerifyStatus.Success);
        }, token).ConfigureAwait(false);
        Metric(context, kind.ToString() + "_" + result.Status, matchedParameters);
        return result;
    }

    /// <summary>
    /// 只在存储事务持有排他访问时更新匹配因子。
    /// </summary>
    /// <param name="credential">本次隔离事务中的凭据状态。</param>
    /// <param name="secret">解密后的共享密钥；调用方负责在本方法返回后清理。</param>
    /// <param name="code">第一条候选验证码；不得记录或写入日志。</param>
    /// <param name="secondCode">可选的连续第二条验证码，仅用于 HOTP 重同步。</param>
    /// <param name="kind">当前事务的操作类型。</param>
    /// <param name="now">用于计算 TOTP 时间步及更新使用时间的 UTC 当前时间。</param>
    /// <returns>算法验证、重放或计数器状态结果。</returns>
    private OtpVerifyStatus MatchAndAdvance(OtpCredentialState credential, byte[] secret, string? code, string? secondCode, OtpTransactionKind kind, DateTimeOffset now)
    {
        var p = credential.Parameters;
        if (p.Type == OtpType.Totp)
        {
            var matched = Totp.FindMatch(secret, code, now, _options.TotpWindow, p.Algorithm, p.Digits, p.PeriodSeconds);
            if (matched == null) return OtpVerifyStatus.Invalid;
            if (matched.Value <= credential.LastAcceptedStep) return OtpVerifyStatus.Replay;
            credential.LastAcceptedStep = matched.Value;
            return OtpVerifyStatus.Success;
        }
        ulong? counter;
        if (kind == OtpTransactionKind.Resynchronization)
        {
            counter = FindHotpResynchronizationCounter(credential.NextCounter, _options.HotpResynchronizationWindow, candidate =>
            {
                var first = Hotp.FindMatch(secret, code, candidate, 0, p.Algorithm, p.Digits);
                var second = Hotp.FindMatch(secret, secondCode, candidate + 1, 0, p.Algorithm, p.Digits);
                return first.HasValue && second.HasValue;
            });
        }
        else counter = Hotp.FindMatch(secret, code, credential.NextCounter, _options.HotpLookAhead, p.Algorithm, p.Digits);
        if (!counter.HasValue) return OtpVerifyStatus.Invalid;
        if (counter.Value == ulong.MaxValue) credential.CounterExhausted = true;
        else credential.NextCounter = counter.Value + 1;
        return OtpVerifyStatus.Success;
    }

    /// <summary>
    /// 完整扫描双码窗口，只有唯一匹配时才返回第二个计数器，供调用方推进状态。
    /// </summary>
    /// <param name="nextCounter">当前尚未消费的起始计数器。</param>
    /// <param name="maxOffset">已通过服务配置校验的最大前瞻偏移。</param>
    /// <param name="matchesPair">同时验证候选计数器及其下一计数器的同步匹配函数。</param>
    /// <returns>唯一匹配对的第二个计数器；无匹配或存在歧义时为 null。</returns>
    internal static ulong? FindHotpResynchronizationCounter(ulong nextCounter, int maxOffset, Func<ulong, bool> matchesPair)
    {
        ulong? counter = null;
        var matches = 0;
        for (var offset = 0; offset <= maxOffset; offset++)
        {
            if (nextCounter > ulong.MaxValue - (ulong)offset) break;
            var candidate = nextCounter + (ulong)offset;
            if (candidate == ulong.MaxValue) break;
            if (matchesPair(candidate)) { matches++; counter = candidate + 1; }
        }
        return matches == 1 ? counter : null;
    }

    /// <summary>
    /// 顺序执行各维度计费，不在持久事务内调用限流器。
    /// </summary>
    /// <param name="context">宿主提供的可信上下文，且用途须在配置允许列表中。</param>
    /// <param name="management">是否为需近期主认证的管理操作。</param>
    /// <param name="credentialId">可选凭据限流维度；无对应维度时为 null。</param>
    /// <param name="transactionId">可选事务限流维度；无对应维度时为 null。</param>
    /// <param name="token">传递给各限流器的取消令牌。</param>
    private async Task RateAsync(OtpContext context, bool management, string? credentialId, string? transactionId, CancellationToken token)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (!_options.AllowedPurposes.Contains(context.Purpose, StringComparer.Ordinal)) throw new ArgumentException("用途未获配置允许。", nameof(context));
        token.ThrowIfCancellationRequested();
        if (management)
        {
            var now = _clock.UtcNow;
            if (context.Purpose != _options.ManagementPurpose || !context.AuthenticatedAt.HasValue ||
                context.AuthenticatedAt.Value > now || now - context.AuthenticatedAt.Value > _options.RecentAuthenticationLifetime)
                throw new OtpAuthorizationException();
        }
        var scopes = new List<KeyValuePair<string, int>>
        {
            new("ip:" + OtpGuard.Digest(context.ClientAddress), _options.IpRequestsPerMinute),
            new("subject:" + OtpGuard.Digest(context.SubjectId), _options.SubjectRequestsPerMinute)
        };
        if (OtpGuard.IsId(credentialId)) scopes.Add(new("credential:" + credentialId, _options.CredentialRequestsPerMinute));
        if (OtpGuard.IsId(transactionId)) scopes.Add(new("transaction:" + transactionId, _options.TransactionRequestsPerMinute));
        if (management) scopes.Add(new("manage:" + OtpGuard.Digest(context.SubjectId), _options.ManagementRequestsPerMinute));
        foreach (var scope in scopes)
        {
            bool allowed;
            try { allowed = await _limiter.TryAcquireAsync(_options.ApplicationId + ":" + scope.Key, scope.Value, TimeSpan.FromMinutes(1), token).ConfigureAwait(false); }
            catch (OtpStoreException) { Metric(context, "rate_limiter_fault"); throw; }
            if (!allowed)
            {
                Metric(context, "rate_limited");
                throw new OtpRateLimitException();
            }
        }
    }

    /// <summary>
    /// 创建绑定当前可信上下文的事务。
    /// </summary>
    /// <param name="state">本次隔离事务中的账号状态。</param>
    /// <param name="context">用于绑定账号、用途和会话摘要的可信上下文。</param>
    /// <param name="kind">要创建的事务类型。</param>
    /// <param name="credentialId">需要绑定的凭据标识；普通验证事务为 null。</param>
    /// <param name="expires">事务绝对失效时间。</param>
    /// <returns>新建且已加入账号状态集合的事务记录。</returns>
    private OtpTransactionState NewTransaction(OtpAccountState state, OtpContext context, OtpTransactionKind kind, string? credentialId, DateTimeOffset expires)
    {
        if (state.Transactions.Count >= _options.MaxTransactions) throw new OtpRateLimitException();
        var transaction = new OtpTransactionState
        {
            TransactionId = SecurityRandom.GetBase64Url(32), Purpose = context.Purpose,
            BindingHash = OtpGuard.Digest(context.BindingId), Kind = kind, CredentialId = credentialId, ExpiresAt = expires
        };
        state.Transactions.Add(transaction);
        return transaction;
    }

    /// <summary>
    /// 清理当前账号已失效的短期状态。
    /// </summary>
    /// <param name="state">本次隔离事务中的可变账号状态。</param>
    /// <param name="now">用于判定过期的 UTC 当前时间。</param>
    private static void Prune(OtpAccountState state, DateTimeOffset now)
    {
        var removed = state.Credentials.Where(x => x.Status == OtpCredentialStatus.Revoked || x.Status == OtpCredentialStatus.Pending && x.PendingExpiresAt <= now).ToArray();
        foreach (var credential in removed)
        {
            Array.Clear(credential.ProtectedSecret, 0, credential.ProtectedSecret.Length);
            state.Credentials.Remove(credential);
        }
        state.Transactions.RemoveAll(x => x.Consumed || x.ExpiresAt <= now || x.CredentialId != null && removed.Any(c => c.CredentialId == x.CredentialId));
    }

    /// <summary>
    /// 判断锁定状态并在到期后重新开始失败窗口。
    /// </summary>
    /// <param name="failure">账号或凭据的失败窗口状态；过期锁定会在此对象中清除。</param>
    /// <param name="now">用于比较锁定截止时间的 UTC 当前时间。</param>
    /// <returns>锁定仍有效时为 true，否则为 false。</returns>
    private static bool IsLocked(OtpFailureState failure, DateTimeOffset now)
    {
        if (failure.LockedUntil > now) return true;
        if (failure.LockedUntil.HasValue) { failure.LockedUntil = null; failure.WindowStart = null; failure.Count = 0; }
        return false;
    }

    /// <summary>
    /// 累计失败，不因新建验证事务或成功验证而清空当前窗口。
    /// </summary>
    /// <param name="failure">要更新的账号或凭据失败窗口状态。</param>
    /// <param name="now">本次失败发生的 UTC 时间。</param>
    private void RegisterFailure(OtpFailureState failure, DateTimeOffset now)
    {
        if (!failure.WindowStart.HasValue || now >= failure.WindowStart.Value.Add(_options.FailureWindow))
        { failure.WindowStart = now; failure.Count = 0; }
        failure.Count = Math.Min(_options.FailureLimit, failure.Count + 1);
        if (failure.Count >= _options.FailureLimit) failure.LockedUntil = now.Add(_options.LockoutDuration);
    }

    /// <summary>
    /// 保护新密钥，验证适配器输出上限。
    /// </summary>
    /// <param name="context">用于绑定密文的应用、账号、凭据和算法上下文。</param>
    /// <param name="id">新凭据标识。</param>
    /// <param name="parameters">该凭据绑定时使用的固定算法参数。</param>
    /// <param name="secret">调用期间借用的明文密钥；本方法不获取其所有权。</param>
    /// <returns>独立分配且长度不超过 16384 字节的密文。</returns>
    private byte[] Protect(OtpContext context, string id, OtpParameters parameters, byte[] secret)
    {
        try
        {
            var value = _protector.Protect(new OtpSecretContext(_options.ApplicationId, context.SubjectId, id, parameters), secret);
            if (value == null || value.Length < 1 || value.Length > 16384) throw new OtpProtectionException("保护器返回了无效密文。");
            return value;
        }
        catch (OtpProtectionException) { Metric(context, "protection_fault", parameters); throw; }
        catch (Exception e) { Metric(context, "protection_fault", parameters); throw new OtpProtectionException("OTP 密钥保护失败。", e); }
    }

    /// <summary>
    /// 校验绑定后解密密钥，适配器故障不能放行。
    /// </summary>
    /// <param name="subjectId">凭据所属的应用内账号标识。</param>
    /// <param name="credential">包含密文和固定算法参数的凭据状态。</param>
    /// <returns>由保护器新分配的明文密钥；调用方必须在使用后清理。</returns>
    private byte[] Unprotect(string subjectId, OtpCredentialState credential)
    {
        try
        {
            var value = _protector.Unprotect(new OtpSecretContext(_options.ApplicationId, subjectId, credential.CredentialId, credential.Parameters), credential.ProtectedSecret);
            if (value == null || value.Length < 16 || value.Length > 128)
            {
                if (value != null) Array.Clear(value, 0, value.Length);
                throw new OtpProtectionException("保护器返回了无效密钥。");
            }
            return value;
        }
        catch (OtpProtectionException) { throw; }
        catch (Exception e) { throw new OtpProtectionException("OTP 密钥解密失败。", e); }
    }

    /// <summary>
    /// 输出固定标签指标；订阅者错误不能改变已经提交的认证结果。
    /// </summary>
    /// <param name="context">提供已验证用途标签的可信上下文。</param>
    /// <param name="result">固定结果分类，不含账号、凭据、验证码或密钥值。</param>
    /// <param name="parameters">可选算法配置，用于输出类型和算法标签。</param>
    private static void Metric(OtpContext context, string result, OtpParameters? parameters = null)
    {
        try
        {
            Events.Add(1, new KeyValuePair<string, object?>("purpose", context.Purpose),
                new KeyValuePair<string, object?>("result", result),
                new KeyValuePair<string, object?>("type", parameters?.Type.ToString() ?? "none"),
                new KeyValuePair<string, object?>("algorithm", parameters?.Algorithm.ToString() ?? "none"));
        }
        catch (Exception) { /* 诊断订阅者不能中断认证状态机。 */ }
    }

    /// <summary>
    /// 统一记录存储与解密故障，状态仍由存储事务负责回滚。
    /// </summary>
    /// <typeparam name="T">同步存储回调的返回类型。</typeparam>
    /// <param name="context">提供账号标识并传递到存储的可信上下文。</param>
    /// <param name="operation">隔离事务中的同步状态操作。</param>
    /// <param name="token">提交前取消存储事务的令牌。</param>
    /// <returns>存储事务提交后产生的回调结果。</returns>
    private async Task<T> StoreAsync<T>(OtpContext context, Func<OtpAccountState, T> operation, CancellationToken token)
    {
        try { return await _store.ExecuteAsync(context.SubjectId, operation, token).ConfigureAwait(false); }
        catch (OtpProtectionException) { Metric(context, "protection_fault"); throw; }
        catch (OtpStoreException) { Metric(context, "storage_fault"); throw; }
    }
}
