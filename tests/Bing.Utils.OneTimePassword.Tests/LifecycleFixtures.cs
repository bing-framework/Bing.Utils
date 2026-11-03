using Bing.OneTimePassword;

namespace Bing.Utils.OneTimePassword.Tests;

/// <summary>
/// 可手动推进的测试时钟。
/// </summary>
internal sealed class TestClock : IOtpClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.FromUnixTimeSeconds(1700000010);
}

/// <summary>
/// 仅用于状态机单测的不限流替身。
/// </summary>
internal sealed class TestLimiter : IOtpRateLimiter
{
    public Task<bool> TryAcquireAsync(string partition, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(true); }
}

/// <summary>
/// 仅用于测试的字节复制保护器，不能用于应用注册。
/// </summary>
internal sealed class TestProtector : IOtpSecretProtector
{
    public bool Fail { get; set; }
    public byte[] Protect(OtpSecretContext context, byte[] secret) => (byte[])secret.Clone();
    public byte[] Unprotect(OtpSecretContext context, byte[] value) => Fail ? throw new OtpProtectionException("test") : (byte[])value.Clone();
}

/// <summary>
/// 使用复制后提交保证回滚的测试存储。
/// </summary>
internal sealed class TestStore : IOtpStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, OtpAccountState> _states = new();
    public Task<T> ExecuteAsync<T>(string subjectId, Func<OtpAccountState, T> operation, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = Clone(_states.TryGetValue(subjectId, out var prior) ? prior : new OtpAccountState(subjectId));
            var result = operation(state);
            cancellationToken.ThrowIfCancellationRequested();
            _states[subjectId] = Clone(state);
            return Task.FromResult(result);
        }
    }
    public Task<int> CleanupAsync(DateTimeOffset now, int maxRecords = 256, CancellationToken cancellationToken = default) => Task.FromResult(0);
    private static OtpFailureState Failure(OtpFailureState x) => new() { Count = x.Count, WindowStart = x.WindowStart, LockedUntil = x.LockedUntil };
    private static OtpAccountState Clone(OtpAccountState input)
    {
        var result = new OtpAccountState(input.SubjectId) { Failure = Failure(input.Failure) };
        result.Credentials.AddRange(input.Credentials.Select(x => new OtpCredentialState
        {
            CredentialId = x.CredentialId, DisplayName = x.DisplayName, Parameters = x.Parameters,
            ProtectedSecret = (byte[])x.ProtectedSecret.Clone(), Status = x.Status, NextCounter = x.NextCounter,
            CounterExhausted = x.CounterExhausted, LastAcceptedStep = x.LastAcceptedStep, CreatedAt = x.CreatedAt,
            LastUsedAt = x.LastUsedAt, PendingExpiresAt = x.PendingExpiresAt, Failure = Failure(x.Failure)
        }));
        result.Transactions.AddRange(input.Transactions.Select(x => new OtpTransactionState
        {
            TransactionId = x.TransactionId, Purpose = x.Purpose, BindingHash = x.BindingHash, CredentialId = x.CredentialId,
            Kind = x.Kind, ExpiresAt = x.ExpiresAt, FailureCount = x.FailureCount, Consumed = x.Consumed
        }));
        return result;
    }
}

/// <summary>
/// 建立无需外部服务的生命周期夹具。
/// </summary>
internal sealed class LifecycleFixture
{
    public TestClock Clock { get; } = new();
    public TestStore Store { get; } = new();
    public TestProtector Protector { get; } = new();
    public OtpService Service { get; }
    public OtpContext Manage => new("alice", "manage", "session", Clock.UtcNow);
    public OtpContext Login => new("alice", "login", "session");
    public LifecycleFixture(OtpServiceOptions? options = null) => Service = new OtpService(Store, Protector, new TestLimiter(), options, Clock);
    public async Task<OtpEnrollment> EnrollAsync(OtpType type = OtpType.Totp)
    {
        var e = await Service.BeginEnrollmentAsync(Manage, new OtpParameters(type), "alice@example.test");
        var code = type == OtpType.Totp ? Totp.Generate(OtpSecretEncoding.Decode(e.SecretBase32), Clock.UtcNow) : Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32), 0);
        var result = await Service.ConfirmEnrollmentAsync(Manage, e.CredentialId, e.TransactionId, code);
        if (!result.Succeeded) throw new InvalidOperationException("Fixture enrollment failed.");
        return e;
    }
}
