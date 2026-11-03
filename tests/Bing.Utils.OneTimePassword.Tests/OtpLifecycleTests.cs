using Bing.OneTimePassword;
using Xunit;

namespace Bing.Utils.OneTimePassword.Tests;

/// <summary>
/// 验证生命周期和原子消费，不依赖网络或真实时钟等待。
/// </summary>
public sealed class OtpLifecycleTests
{
    [Theory]
    [InlineData(OtpType.Totp)]
    [InlineData(OtpType.Hotp)]
    public async Task Enrollment_ConsumesConfirmationFactor(OtpType type)
    {
        var f = new LifecycleFixture();
        var e = await f.EnrollAsync(type);
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        var secret = OtpSecretEncoding.Decode(e.SecretBase32);
        var code = type == OtpType.Totp ? Totp.Generate(secret, f.Clock.UtcNow) : Hotp.Generate(secret, 0);
        Assert.False((await f.Service.VerifyAsync(f.Login, e.CredentialId, tx.TransactionId, code)).Succeeded);
        f.Clock.UtcNow = f.Clock.UtcNow.AddSeconds(30);
        code = type == OtpType.Totp ? Totp.Generate(secret, f.Clock.UtcNow) : Hotp.Generate(secret, 1);
        Assert.True((await f.Service.VerifyAsync(f.Login, e.CredentialId, tx.TransactionId, code)).Succeeded);
        Assert.False((await f.Service.VerifyAsync(f.Login, e.CredentialId, tx.TransactionId, code)).Succeeded);
    }

    [Fact]
    public async Task Totp_ReplayIsGlobalAcrossPurposesAndTransactions()
    {
        var f = new LifecycleFixture();
        var e = await f.EnrollAsync();
        f.Clock.UtcNow = f.Clock.UtcNow.AddSeconds(30);
        var code = Totp.Generate(OtpSecretEncoding.Decode(e.SecretBase32), f.Clock.UtcNow);
        var a = await f.Service.BeginVerificationAsync(f.Login);
        Assert.True((await f.Service.VerifyAsync(f.Login, e.CredentialId, a.TransactionId, code)).Succeeded);
        var context = new OtpContext("alice", "step-up", "another-session");
        var b = await f.Service.BeginVerificationAsync(context);
        Assert.Equal(OtpVerifyStatus.Replay, (await f.Service.VerifyAsync(context, e.CredentialId, b.TransactionId, code)).Status);
    }

    [Fact]
    public async Task WrongContext_DoesNotConsumeCorrectTransaction()
    {
        var f = new LifecycleFixture();
        var e = await f.EnrollAsync(OtpType.Hotp);
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        var code = Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32), 1);
        foreach (var wrong in new[] { new OtpContext("bob","login","session"), new OtpContext("alice","step-up","session"), new OtpContext("alice","login","wrong") })
            Assert.False((await f.Service.VerifyAsync(wrong,e.CredentialId,tx.TransactionId,code)).Succeeded);
        Assert.True((await f.Service.VerifyAsync(f.Login,e.CredentialId,tx.TransactionId,code)).Succeeded);
    }

    [Fact]
    public async Task PendingReplacement_AndExpiryInvalidateOldEnrollment()
    {
        var f = new LifecycleFixture();
        var first = await f.Service.BeginEnrollmentAsync(f.Manage,new OtpParameters(),"alice");
        await Assert.ThrowsAsync<OtpAuthorizationException>(() => f.Service.BeginVerificationAsync(f.Login));
        var latest = await f.Service.BeginEnrollmentAsync(f.Manage,new OtpParameters(),"alice");
        Assert.False((await f.Service.ConfirmEnrollmentAsync(f.Manage,first.CredentialId,first.TransactionId,Totp.Generate(OtpSecretEncoding.Decode(first.SecretBase32),f.Clock.UtcNow))).Succeeded);
        f.Clock.UtcNow = latest.ExpiresAt;
        Assert.False((await f.Service.ConfirmEnrollmentAsync(f.Manage,latest.CredentialId,latest.TransactionId,Totp.Generate(OtpSecretEncoding.Decode(latest.SecretBase32),f.Clock.UtcNow))).Succeeded);
    }

    [Fact]
    public async Task InvalidFormats_CountTowardPersistentAccountLock()
    {
        var f = new LifecycleFixture();
        var e = await f.EnrollAsync(OtpType.Hotp);
        for (var i=0;i<5;i++)
        {
            var tx = await f.Service.BeginVerificationAsync(f.Login);
            Assert.False((await f.Service.VerifyAsync(f.Login,e.CredentialId,tx.TransactionId,"１２３４５６")).Succeeded);
        }
        await Assert.ThrowsAsync<OtpRateLimitException>(() => f.Service.BeginVerificationAsync(f.Login));
        await Assert.ThrowsAsync<OtpRateLimitException>(() => f.Service.BeginEnrollmentAsync(f.Manage,new OtpParameters(),"alice"));
        Assert.Equal(5,await f.Store.ExecuteAsync("alice",s=>s.Failure.Count));
        f.Clock.UtcNow = f.Clock.UtcNow.AddMinutes(15);
        var next = await f.Service.BeginVerificationAsync(f.Login);
        Assert.True((await f.Service.VerifyAsync(f.Login,e.CredentialId,next.TransactionId,Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32),1))).Succeeded);
    }

    [Fact]
    public async Task ConcurrentFailures_DoNotLoseCounts()
    {
        var f = new LifecycleFixture();
        var e = await f.EnrollAsync(OtpType.Hotp);
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        await Task.WhenAll(Enumerable.Range(0,20).Select(_=>Task.Run(()=>f.Service.VerifyAsync(f.Login,e.CredentialId,tx.TransactionId,"bad"))));
        Assert.Equal(5,await f.Store.ExecuteAsync("alice",s=>s.Failure.Count));
    }

    [Fact]
    public async Task ConcurrentSuccess_OnlyOneTransactionWins()
    {
        var f = new LifecycleFixture();
        var e = await f.EnrollAsync(OtpType.Hotp);
        var transactions = new[] { await f.Service.BeginVerificationAsync(f.Login), await f.Service.BeginVerificationAsync(f.Login) };
        var code = Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32),1);
        var results = await Task.WhenAll(transactions.Select(t=>Task.Run(()=>f.Service.VerifyAsync(f.Login,e.CredentialId,t.TransactionId,code))));
        Assert.Single(results.Where(x=>x.Succeeded));
    }

    [Fact]
    public async Task SameTransaction_DifferentDevicesCannotBothSucceed()
    {
        var f = new LifecycleFixture();
        var a = await f.EnrollAsync(OtpType.Hotp);
        var b = await f.EnrollAsync(OtpType.Hotp);
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        var results = await Task.WhenAll(new[]{a,b}.Select(e=>Task.Run(()=>f.Service.VerifyAsync(f.Login,e.CredentialId,tx.TransactionId,Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32),1)))));
        Assert.Single(results.Where(x=>x.Succeeded));
    }

    [Fact]
    public async Task Revoke_InvalidatesTransactionsAndClearsCiphertext()
    {
        var f = new LifecycleFixture();
        var e = await f.EnrollAsync(OtpType.Hotp);
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        Assert.True(await f.Service.RevokeAsync(f.Manage,e.CredentialId));
        Assert.Equal(0,await f.Store.ExecuteAsync("alice",s=>s.Credentials.Single().ProtectedSecret.Length));
        Assert.False((await f.Service.VerifyAsync(f.Login,e.CredentialId,tx.TransactionId,Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32),1))).Succeeded);
        Assert.Empty(await f.Service.GetCredentialsAsync(f.Manage));
    }

    [Fact]
    public async Task Resynchronize_RequiresTwoConsecutiveCodesAndAdvancesBeyondBoth()
    {
        var f = new LifecycleFixture();
        var e = await f.EnrollAsync(OtpType.Hotp);
        var key = OtpSecretEncoding.Decode(e.SecretBase32);
        var tx = await f.Service.BeginVerificationAsync(f.Manage,OtpTransactionKind.Resynchronization,e.CredentialId);
        Assert.False((await f.Service.ResynchronizeHotpAsync(f.Manage,e.CredentialId,tx.TransactionId,Hotp.Generate(key,12),"bad")).Succeeded);
        Assert.Equal(1UL,await f.Store.ExecuteAsync("alice",s=>s.Credentials.Single().NextCounter));
        Assert.True((await f.Service.ResynchronizeHotpAsync(f.Manage,e.CredentialId,tx.TransactionId,Hotp.Generate(key,12),Hotp.Generate(key,13))).Succeeded);
        Assert.Equal(14UL,await f.Store.ExecuteAsync("alice",s=>s.Credentials.Single().NextCounter));
        var next = await f.Service.BeginVerificationAsync(f.Login);
        Assert.True((await f.Service.VerifyAsync(f.Login,e.CredentialId,next.TransactionId,Hotp.Generate(key,14))).Succeeded);
    }

    [Fact]
    public async Task CounterMaximum_IsAcceptedOnceThenExhausted()
    {
        var f = new LifecycleFixture();
        var e = await f.EnrollAsync(OtpType.Hotp);
        await f.Store.ExecuteAsync("alice",s=>s.Credentials.Single().NextCounter=ulong.MaxValue);
        var code = Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32),ulong.MaxValue);
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        Assert.True((await f.Service.VerifyAsync(f.Login,e.CredentialId,tx.TransactionId,code)).Succeeded);
        var next = await f.Service.BeginVerificationAsync(f.Login);
        Assert.Equal(OtpVerifyStatus.CounterExhausted,(await f.Service.VerifyAsync(f.Login,e.CredentialId,next.TransactionId,code)).Status);
    }

    [Fact]
    public async Task ProtectionFailure_RollsBackAndNeverAccepts()
    {
        var f = new LifecycleFixture();
        var e = await f.EnrollAsync(OtpType.Hotp);
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        var code = Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32),1);
        f.Protector.Fail = true;
        await Assert.ThrowsAsync<OtpProtectionException>(()=>f.Service.VerifyAsync(f.Login,e.CredentialId,tx.TransactionId,code));
        f.Protector.Fail = false;
        Assert.True((await f.Service.VerifyAsync(f.Login,e.CredentialId,tx.TransactionId,code)).Succeeded);
    }

    [Fact]
    public async Task TransactionExpiryAndCancellation_DoNotAdvanceCounter()
    {
        var f = new LifecycleFixture();
        var e = await f.EnrollAsync(OtpType.Hotp);
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        var code = Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32),1);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>f.Service.VerifyAsync(f.Login,e.CredentialId,tx.TransactionId,code,new CancellationToken(true)));
        f.Clock.UtcNow = tx.ExpiresAt;
        Assert.False((await f.Service.VerifyAsync(f.Login,e.CredentialId,tx.TransactionId,code)).Succeeded);
        Assert.Equal(1UL,await f.Store.ExecuteAsync("alice",s=>s.Credentials.Single().NextCounter));
    }

    [Fact]
    public async Task ManagementRequiresRecentTrustedAuthenticationAndPurpose()
    {
        var f = new LifecycleFixture();
        var contexts = new[] { f.Login, new OtpContext("alice","manage","session"), new OtpContext("alice","manage","session",f.Clock.UtcNow.AddMinutes(-6)), new OtpContext("alice","manage","session",f.Clock.UtcNow.AddSeconds(1)) };
        foreach (var context in contexts)
            await Assert.ThrowsAsync<OtpAuthorizationException>(()=>f.Service.BeginEnrollmentAsync(context,new OtpParameters(),"alice"));
    }

    [Fact]
    public async Task LimitsAndMetadata_DoNotLeakOrReplaceActiveCredentials()
    {
        var options = new OtpServiceOptions { MaxCredentials=1,MaxTransactions=1 };
        var f = new LifecycleFixture(options);
        options.MaxCredentials=100;
        var e = await f.EnrollAsync();
        await Assert.ThrowsAsync<OtpRateLimitException>(()=>f.Service.BeginEnrollmentAsync(f.Manage,new OtpParameters(),"alice"));
        await f.Service.BeginVerificationAsync(f.Login);
        await Assert.ThrowsAsync<OtpRateLimitException>(()=>f.Service.BeginVerificationAsync(f.Login));
        Assert.DoesNotContain(e.SecretBase32,e.ToString());
        Assert.DoesNotContain("Secret",System.Text.Json.JsonSerializer.Serialize(await f.Service.GetCredentialsAsync(f.Manage)));
        Assert.False(new OtpVerifyResult(default).Succeeded);
    }
}
