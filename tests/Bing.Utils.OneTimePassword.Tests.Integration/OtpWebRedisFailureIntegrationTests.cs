using System.Net;
using System.Security.Cryptography;
using Bing.OneTimePassword;
using StackExchange.Redis;
using Xunit;

namespace Bing.Utils.OneTimePassword.Tests.Integration;

/// <summary>
/// 真实 Redis 未配置时明确跳过；已配置但不可达时由测试报告失败。
/// </summary>
public sealed class RedisTheoryAttribute : TheoryAttribute
{
    /// <summary>
    /// 使用与 Redis 适配器集成测试一致的连接环境变量。
    /// </summary>
    public RedisTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BING_OTP_REDIS")))
            Skip = "BLOCKED_EXTERNAL: Set BING_OTP_REDIS to run real Redis HTTP integration tests.";
    }
}

/// <summary>
/// 通过断开真实 Redis 的 TCP 通道验证 HTTP 故障映射与认证状态保护。
/// </summary>
public sealed class OtpWebRedisFailureIntegrationTests
{
    /// <summary>
    /// Redis 连接中断后拒绝验证，不消费验证码或事务，也不签发 MFA 会话。
    /// </summary>
    /// <param name="type">需要验证的 OTP 类型。</param>
    [RedisTheory]
    [InlineData("totp")]
    [InlineData("hotp")]
    public async Task Verification_WhenRedisConnectionBreaks_ShouldReturn503WithoutConsumingOtp(string type)
    {
        var connection = Environment.GetEnvironmentVariable("BING_OTP_REDIS");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Redis configuration disappeared after test discovery.");

        var configuration = ConfigurationOptions.Parse(connection);
        var upstream = Assert.Single(configuration.EndPoints);
        await using var proxy = new RedisFaultProxy(upstream);
        // 仅切断当前用例的网络通道，不关闭 Redis 服务或影响其他客户端。
        if (configuration.Ssl && string.IsNullOrEmpty(configuration.SslHost))
            configuration.SslHost = upstream is DnsEndPoint dns ? dns.Host : ((IPEndPoint)upstream).Address.ToString();
        configuration.EndPoints.Clear();
        configuration.EndPoints.Add(proxy.EndPoint);
        configuration.AsyncTimeout = 1000;
        configuration.SyncTimeout = 1000;
        configuration.ConnectTimeout = 3000;
        configuration.ConnectRetry = 0;
        configuration.AbortOnConnectFail = true;

        using var host = new OtpWebTestHost(redisConnection: configuration.ToString());
        using var client = host.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        var csrfToken = await OtpWebHttp.SignInAsync(client);
        EnrollmentResponse enrollment;
        using (var response = await OtpWebHttp.PostJsonAsync(client, "/api/otp/enroll", csrfToken, new
        {
            type,
            displayName = "redis-fault-integration"
        }))
        {
            OtpWebHttp.AssertNoStore(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            enrollment = await OtpWebHttp.ReadJsonAsync<EnrollmentResponse>(response);
        }

        var secret = OtpSecretEncoding.Decode(enrollment.SecretBase32);
        try
        {
            using (var confirm = await OtpWebHttp.PostJsonAsync(client, "/api/otp/confirm", csrfToken, new
            {
                credentialId = enrollment.CredentialId,
                transactionId = enrollment.TransactionId,
                code = type == "hotp" ? Hotp.Generate(secret, 0) : Totp.Generate(secret, host.Clock.UtcNow)
            }))
            {
                OtpWebHttp.AssertNoStore(confirm);
                Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
            }

            TransactionResponse transaction;
            using (var response = await OtpWebHttp.PostEmptyAsync(client, "/api/otp/transactions", csrfToken))
            {
                OtpWebHttp.AssertNoStore(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                transaction = await OtpWebHttp.ReadJsonAsync<TransactionResponse>(response);
            }

            var before = await ReadStateAsync(host.Store, enrollment.CredentialId, transaction.TransactionId);
            Assert.False(before.Consumed);
            if (type == "totp")
                host.Clock.UtcNow = DateTimeOffset.FromUnixTimeSeconds((before.Step + 1) * 30);
            var code = type == "hotp" ? Hotp.Generate(secret, before.Counter) : Totp.Generate(secret, host.Clock.UtcNow);

            // 绑定和创建事务已通过真实 Lua 限流；现在关闭已有连接及重连入口。
            await proxy.DisposeAsync();
            using (var unavailable = await OtpWebHttp.PostJsonAsync(client, "/api/otp/verify", csrfToken, new
            {
                credentialId = enrollment.CredentialId,
                transactionId = transaction.TransactionId,
                code
            }))
            {
                OtpWebHttp.AssertNoStore(unavailable);
                await OtpWebHttp.AssertErrorAsync(unavailable, HttpStatusCode.ServiceUnavailable, "otp_unavailable");
            }

            var after = await ReadStateAsync(host.Store, enrollment.CredentialId, transaction.TransactionId);
            Assert.Equal(before, after);
            using var protectedResponse = await client.GetAsync("/api/protected");
            OtpWebHttp.AssertNoStore(protectedResponse);
            Assert.Equal(HttpStatusCode.Forbidden, protectedResponse.StatusCode);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    private static Task<(ulong Counter, long Step, bool Consumed, int AccountFailures, int CredentialFailures, int TransactionFailures)>
        ReadStateAsync(IOtpStore store, string credentialId, string transactionId) =>
        store.ExecuteAsync("development-demo", state =>
        {
            var credential = Assert.Single(state.Credentials, item => item.CredentialId == credentialId);
            var transaction = Assert.Single(state.Transactions, item => item.TransactionId == transactionId);
            Assert.Equal(OtpCredentialStatus.Active, credential.Status);
            return (credential.NextCounter, credential.LastAcceptedStep, transaction.Consumed,
                state.Failure.Count, credential.Failure.Count, transaction.FailureCount);
        });
}
