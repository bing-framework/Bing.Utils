using System.Diagnostics;
using Bing.OneTimePassword.DataProtection;
using Bing.OneTimePassword.Sqlite;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Bing.OneTimePassword.Tests.Integration;

/// <summary>
/// 使用真实本地数据库和文件密钥环检查持久化事务边界。
/// </summary>
public sealed class SqliteLifecycleTests
{
    [Fact]
    public async Task Initialization_IsExplicitAndRejectsUnknownSchema()
    {
        using var f = new DatabaseFixture(initialize: false);
        await Assert.ThrowsAsync<OtpStoreException>(() => f.Store.ExecuteAsync("alice", _ => true));
        await f.Store.InitializeAsync();
        await f.Store.InitializeAsync();
        f.Sql("UPDATE otp_schema SET version=2;");
        await Assert.ThrowsAsync<OtpStoreException>(() => f.Store.InitializeAsync());
        await Assert.ThrowsAsync<OtpStoreException>(() => f.Store.ExecuteAsync("alice", _ => true));
    }

    [Theory]
    [InlineData(OtpType.Totp)]
    [InlineData(OtpType.Hotp)]
    public async Task Restart_PreservesConsumedFactorAndProtectedSecret(OtpType type)
    {
        using var f = new DatabaseFixture();
        var enrollment = await f.Enroll(type);
        var secret = OtpSecretEncoding.Decode(enrollment.SecretBase32);
        f.Clock.UtcNow = f.Clock.UtcNow.AddSeconds(30);
        var code = type == OtpType.Totp ? Totp.Generate(secret, f.Clock.UtcNow) : Hotp.Generate(secret, 1);
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        Assert.True((await f.Service.VerifyAsync(f.Login, enrollment.CredentialId, tx.TransactionId, code)).Succeeded);
        var restarted = f.CreateService();
        var retry = await restarted.BeginVerificationAsync(f.Login);
        Assert.False((await restarted.VerifyAsync(f.Login, enrollment.CredentialId, retry.TransactionId, code)).Succeeded);
        var cipher = await f.Store.ExecuteAsync("alice", s => s.Credentials.Single().ProtectedSecret);
        Assert.NotEqual(secret, cipher);
        Assert.DoesNotContain(enrollment.SecretBase32, System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(f.Database)));
    }

    [Fact]
    public async Task CounterBlob_PreservesUnsignedMaximumAcrossRestart()
    {
        using var f = new DatabaseFixture();
        var e = await f.Enroll(OtpType.Hotp);
        await f.Store.ExecuteAsync("alice", s => { s.Credentials.Single().NextCounter = ulong.MaxValue; return true; });
        Assert.Equal(ulong.MaxValue, await new SqliteOtpStore(f.ConnectionString).ExecuteAsync("alice", s => s.Credentials.Single().NextCounter));
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        Assert.True((await f.Service.VerifyAsync(f.Login, e.CredentialId, tx.TransactionId, Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32), ulong.MaxValue))).Succeeded);
        Assert.True(await f.Store.ExecuteAsync("alice", s => s.Credentials.Single().CounterExhausted));
    }

    [Fact]
    public async Task SeparateConnections_OnlyOneConcurrentCodeSucceeds()
    {
        using var f = new DatabaseFixture();
        var e = await f.Enroll(OtpType.Hotp);
        var transactions = new List<OtpTransaction>();
        for (var i = 0; i < 8; i++) transactions.Add(await f.Service.BeginVerificationAsync(f.Login));
        var code = Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32), 1);
        var results = await Task.WhenAll(transactions.Select(t => Task.Run(() => f.CreateService().VerifyAsync(f.Login, e.CredentialId, t.TransactionId, code))));
        Assert.Single(results.Where(x => x.Succeeded));
        Assert.Equal(2UL, await f.Store.ExecuteAsync("alice", s => s.Credentials.Single().NextCounter));
    }

    [Fact]
    public async Task SeparateDevices_OnlyOneCanConsumeSameTransaction()
    {
        using var f = new DatabaseFixture();
        var first = await f.Enroll(OtpType.Hotp);
        var second = await f.Enroll(OtpType.Hotp);
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        var results = await Task.WhenAll(new[] { first, second }.Select(e => Task.Run(() => f.CreateService().VerifyAsync(
            f.Login, e.CredentialId, tx.TransactionId, Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32), 1)))));
        Assert.Single(results.Where(x => x.Succeeded));
    }

    [Fact]
    public async Task ConcurrentFailures_PersistLockAcrossNewServiceAndTransactions()
    {
        using var f = new DatabaseFixture();
        var e = await f.Enroll(OtpType.Hotp);
        var transactions = new List<OtpTransaction>();
        for (var i = 0; i < 10; i++) transactions.Add(await f.Service.BeginVerificationAsync(f.Login));
        await Task.WhenAll(transactions.Select(t => Task.Run(() => f.CreateService().VerifyAsync(f.Login, e.CredentialId, t.TransactionId, "invalid"))));
        Assert.Equal(5, await f.Store.ExecuteAsync("alice", s => s.Failure.Count));
        Assert.Equal(5, await f.Store.ExecuteAsync("alice", s => s.Credentials.Single().Failure.Count));
        await Assert.ThrowsAsync<OtpRateLimitException>(() => f.CreateService().BeginVerificationAsync(f.Login));
        f.Clock.UtcNow = f.Clock.UtcNow.AddMinutes(15);
        var fresh = await f.CreateService().BeginVerificationAsync(f.Login);
        Assert.True((await f.Service.VerifyAsync(f.Login, e.CredentialId, fresh.TransactionId, Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32), 1))).Succeeded);
    }

    [Fact]
    public async Task RevokeRace_AllAttemptsAfterCommitAreRejected()
    {
        using var f = new DatabaseFixture();
        var e = await f.Enroll(OtpType.Hotp);
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        var code = Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32), 1);
        var verify = Task.Run(() => f.CreateService().VerifyAsync(f.Login, e.CredentialId, tx.TransactionId, code));
        var revoke = Task.Run(() => f.CreateService().RevokeAsync(f.Manage, e.CredentialId));
        await Task.WhenAll(verify, revoke);
        Assert.True(revoke.Result);
        Assert.False((await f.CreateService().VerifyAsync(f.Login, e.CredentialId, tx.TransactionId, code)).Succeeded);
        Assert.Empty(await f.Service.GetCredentialsAsync(f.Login));
    }

    [Fact]
    public async Task ExceptionCancellationAndAsyncCallback_AllRollback()
    {
        using var f = new DatabaseFixture();
        await f.Store.ExecuteAsync("alice", s => { s.Failure.Count = 1; return true; });
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ExecuteAsync<int>("alice", s =>
        { s.Failure.Count = 2; throw new InvalidOperationException("rollback"); }));
        using var cancellation = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Store.ExecuteAsync("alice", s =>
        { s.Failure.Count = 3; cancellation.Cancel(); return true; }, cancellation.Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ExecuteAsync("alice", s =>
        { s.Failure.Count = 4; return Task.CompletedTask; }));
        Assert.Equal(1, await f.Store.ExecuteAsync("alice", s => s.Failure.Count));
    }

    [Fact]
    public async Task DatabaseLock_HasBoundedWaitAndFailsClosed()
    {
        using var f = new DatabaseFixture();
        using var blocker = new SqliteConnection(f.ConnectionString);
        blocker.Open();
        using var transaction = blocker.BeginTransaction(deferred: false);
        var watch = Stopwatch.StartNew();
        await Assert.ThrowsAsync<OtpStoreException>(() => f.Store.ExecuteAsync("alice", _ => true));
        Assert.InRange(watch.Elapsed, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(12));
    }

    [Fact]
    public async Task CorruptCounterRecord_IsRejected()
    {
        using var f = new DatabaseFixture();
        await f.Enroll(OtpType.Hotp);
        f.Sql("PRAGMA ignore_check_constraints=ON; UPDATE otp_credentials SET next_counter=X'01';");
        await Assert.ThrowsAsync<OtpStoreException>(() => f.Store.ExecuteAsync("alice", _ => true));
    }

    [Fact]
    public async Task CiphertextSwap_FailsWithoutConsumingTransaction()
    {
        using var f = new DatabaseFixture();
        var first = await f.Enroll(OtpType.Hotp);
        var second = await f.Enroll(OtpType.Hotp);
        await f.Store.ExecuteAsync("alice", s =>
        { s.Credentials[0].ProtectedSecret = s.Credentials[1].ProtectedSecret.ToArray(); return true; });
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        await Assert.ThrowsAsync<OtpProtectionException>(() => f.Service.VerifyAsync(f.Login, first.CredentialId, tx.TransactionId,
            Hotp.Generate(OtpSecretEncoding.Decode(second.SecretBase32), 1)));
        Assert.False(await f.Store.ExecuteAsync("alice", s => s.Transactions.Single(t => t.TransactionId == tx.TransactionId).Consumed));
    }

    [Fact]
    public async Task BoundedCleanup_NeverRemovesActiveReplayState()
    {
        using var f = new DatabaseFixture();
        var active = await f.Enroll(OtpType.Totp);
        var accepted = await f.Store.ExecuteAsync("alice", s => s.Credentials.Single().LastAcceptedStep);
        await f.Service.BeginEnrollmentAsync(f.Manage, new OtpParameters(), "alice@example.test");
        f.Clock.UtcNow = f.Clock.UtcNow.AddMinutes(20);
        Assert.InRange(await f.Store.CleanupAsync(f.Clock.UtcNow, 1), 0, 1);
        await f.Store.CleanupAsync(f.Clock.UtcNow, 100);
        var credential = await f.Store.ExecuteAsync("alice", s => s.Credentials.Single());
        Assert.Equal(active.CredentialId, credential.CredentialId);
        Assert.Equal(accepted, credential.LastAcceptedStep);
    }

    /// <summary>
    /// 每个用例独立拥有真实数据库、限流器和测试密钥环。
    /// </summary>
    private sealed class DatabaseFixture : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "bing-otp-sqlite-" + Guid.NewGuid().ToString("N"));
        private readonly MemoryOtpRateLimiter _limiter;
        private readonly IOtpSecretProtector _protector;
        public TestClock Clock { get; } = new();
        public string Database => Path.Combine(_directory, "otp.db");
        public string ConnectionString => new SqliteConnectionStringBuilder { DataSource = Database, Pooling = false }.ToString();
        public SqliteOtpStore Store { get; }
        public IOtpService Service { get; }
        public OtpContext Login => new("alice", "login", "session");
        public OtpContext Manage => new("alice", "manage", "session", Clock.UtcNow);
        public DatabaseFixture(bool initialize = true)
        {
            Directory.CreateDirectory(_directory);
            _limiter = new MemoryOtpRateLimiter(clock: Clock);
            _protector = new DataProtectionOtpSecretProtector(DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(_directory, "keys"))));
            Store = new SqliteOtpStore(ConnectionString);
            if (initialize) Store.InitializeAsync().GetAwaiter().GetResult();
            Service = CreateService();
        }
        public IOtpService CreateService() => new OtpService(new SqliteOtpStore(ConnectionString), _protector, _limiter, new OtpServiceOptions
        {
            IpRequestsPerMinute = 1000, SubjectRequestsPerMinute = 1000, CredentialRequestsPerMinute = 1000,
            TransactionRequestsPerMinute = 1000, ManagementRequestsPerMinute = 1000, MaxTransactions = 100
        }, Clock);
        public async Task<OtpEnrollment> Enroll(OtpType type)
        {
            var e = await Service.BeginEnrollmentAsync(Manage, new OtpParameters(type), "alice@example.test");
            var secret = OtpSecretEncoding.Decode(e.SecretBase32);
            var code = type == OtpType.Totp ? Totp.Generate(secret, Clock.UtcNow) : Hotp.Generate(secret, 0);
            Assert.True((await Service.ConfirmEnrollmentAsync(Manage, e.CredentialId, e.TransactionId, code)).Succeeded);
            return e;
        }
        public void Sql(string sql)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
        public void Dispose()
        {
            _limiter.Dispose();
            Directory.Delete(_directory, true);
        }
    }

    /// <summary>
    /// 以固定 UTC 时间重现时间窗口和锁定到期。
    /// </summary>
    private sealed class TestClock : IOtpClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.FromUnixTimeSeconds(1700000010);
    }
}
