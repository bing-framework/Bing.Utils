using System.IO;
using System.Net;
using System.Security.Cryptography;
using Bing.OneTimePassword;
using Xunit;

namespace Bing.Utils.OneTimePassword.Tests.Integration;

/// <summary>
/// 验证 OTP 管理、限流、近期认证及基础设施故障的 HTTP 映射。
/// </summary>
public sealed class OtpWebOperationsIntegrationTests
{
    /// <summary>
    /// 验证 HOTP 双码重同步、后续计数器验证和设备撤销。
    /// </summary>
    [Fact]
    public async Task HotpLifecycle_WhenResynchronized_ShouldAdvanceAndRevokeCredential()
    {
        using var host = new OtpWebTestHost();
        using var client = host.CreateClient();
        var csrfToken = await OtpWebHttp.SignInAsync(client);
        var enrollment = await EnrollAsync(client, csrfToken, "hotp");
        var secret = OtpSecretEncoding.Decode(enrollment.SecretBase32);
        try
        {
            using (var confirm = await OtpWebHttp.PostJsonAsync(client, "/api/otp/confirm", csrfToken, new
            {
                credentialId = enrollment.CredentialId,
                transactionId = enrollment.TransactionId,
                code = Hotp.Generate(secret, 0)
            }))
            {
                OtpWebHttp.AssertNoStore(confirm);
                Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
            }

            var pair = FindUniqueResynchronizationPair(secret, 1, 100);
            using (var resyncTransaction = await OtpWebHttp.PostJsonAsync(client, "/api/otp/resync/transactions", csrfToken, new
            {
                credentialId = enrollment.CredentialId
            }))
            {
                OtpWebHttp.AssertNoStore(resyncTransaction);
                Assert.Equal(HttpStatusCode.OK, resyncTransaction.StatusCode);
                var transaction = await OtpWebHttp.ReadJsonAsync<TransactionResponse>(resyncTransaction);

                using var resync = await OtpWebHttp.PostJsonAsync(client, "/api/otp/resync", csrfToken, new
                {
                    credentialId = enrollment.CredentialId,
                    transactionId = transaction.TransactionId,
                    firstCode = pair.FirstCode,
                    secondCode = pair.SecondCode
                });
                OtpWebHttp.AssertNoStore(resync);
                Assert.Equal(HttpStatusCode.OK, resync.StatusCode);
            }

            using (var transactionResponse = await OtpWebHttp.PostEmptyAsync(client, "/api/otp/transactions", csrfToken))
            {
                OtpWebHttp.AssertNoStore(transactionResponse);
                Assert.Equal(HttpStatusCode.OK, transactionResponse.StatusCode);
                var transaction = await OtpWebHttp.ReadJsonAsync<TransactionResponse>(transactionResponse);
                using var verify = await OtpWebHttp.PostJsonAsync(client, "/api/otp/verify", csrfToken, new
                {
                    credentialId = enrollment.CredentialId,
                    transactionId = transaction.TransactionId,
                    code = Hotp.Generate(secret, pair.Counter + 2)
                });
                OtpWebHttp.AssertNoStore(verify);
                Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
            }

            using (var revoke = await OtpWebHttp.PostJsonAsync(client, "/api/otp/revoke", csrfToken, new
            {
                credentialId = enrollment.CredentialId
            }))
            {
                OtpWebHttp.AssertNoStore(revoke);
                Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
            }

            using var credentials = await client.GetAsync("/api/otp/credentials");
            OtpWebHttp.AssertNoStore(credentials);
            Assert.Equal(HttpStatusCode.OK, credentials.StatusCode);
            var credentialsJson = await credentials.Content.ReadAsStringAsync();
            OtpWebHttp.AssertNoSecretOrUri(credentialsJson, enrollment.SecretBase32, enrollment.ProvisioningUri);
            OtpWebHttp.AssertCredentialNotListed(credentialsJson, enrollment.CredentialId);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    /// <summary>
    /// 验证主认证时间超过窗口后管理绑定被拒绝。
    /// </summary>
    [Fact]
    public async Task Enrollment_WhenPrimaryAuthenticationIsTooOld_ShouldReturnForbidden()
    {
        using var host = new OtpWebTestHost();
        using var client = host.CreateClient();
        var csrfToken = await OtpWebHttp.SignInAsync(client);
        host.Clock.UtcNow = host.Clock.UtcNow.AddMinutes(6);

        using var response = await OtpWebHttp.PostJsonAsync(client, "/api/otp/enroll", csrfToken, new
        {
            type = "totp",
            displayName = "expired-primary"
        });
        OtpWebHttp.AssertNoStore(response);
        await OtpWebHttp.AssertErrorAsync(response, HttpStatusCode.Forbidden, "otp_invalid");
    }

    /// <summary>
    /// 验证限流拒绝映射为 429。
    /// </summary>
    [Fact]
    public async Task Credentials_WhenRateLimiterRejects_ShouldReturnTooManyRequests()
    {
        using var host = new OtpWebTestHost();
        using var client = host.CreateClient();
        await OtpWebHttp.SignInAsync(client);
        host.RateLimiter.RejectAll = true;

        using var response = await client.GetAsync("/api/otp/credentials");
        OtpWebHttp.AssertNoStore(response);
        await OtpWebHttp.AssertErrorAsync(response, HttpStatusCode.TooManyRequests, "otp_rate_limited");
    }

    /// <summary>
    /// 验证密钥解密故障映射为 503。
    /// </summary>
    [Fact]
    public async Task Verification_WhenProtectorFails_ShouldReturnServiceUnavailable()
    {
        using var host = new OtpWebTestHost();
        using var client = host.CreateClient();
        var csrfToken = await OtpWebHttp.SignInAsync(client);
        var enrollment = await EnrollAsync(client, csrfToken, "totp");
        var secret = OtpSecretEncoding.Decode(enrollment.SecretBase32);
        try
        {
            using (var confirm = await OtpWebHttp.PostJsonAsync(client, "/api/otp/confirm", csrfToken, new
            {
                credentialId = enrollment.CredentialId,
                transactionId = enrollment.TransactionId,
                code = Totp.Generate(secret, host.Clock.UtcNow)
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

            host.Protector.FailUnprotect = true;
            using var unavailable = await OtpWebHttp.PostJsonAsync(client, "/api/otp/verify", csrfToken, new
            {
                credentialId = enrollment.CredentialId,
                transactionId = transaction.TransactionId,
                code = Totp.Generate(secret, host.Clock.UtcNow)
            });
            OtpWebHttp.AssertNoStore(unavailable);
            await OtpWebHttp.AssertErrorAsync(unavailable, HttpStatusCode.ServiceUnavailable, "otp_unavailable");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    /// <summary>
    /// 验证 SQLite 存储故障映射为 503。
    /// </summary>
    [Fact]
    public async Task Credentials_WhenSqliteFileCannotBeOpened_ShouldReturnServiceUnavailable()
    {
        using var host = new OtpWebTestHost();
        using var client = host.CreateClient();
        await OtpWebHttp.SignInAsync(client);
        var databasePath = Path.Combine(host.DataDirectory, "otp.db");
        var retainedPath = databasePath + ".retained";

        File.Move(databasePath, retainedPath);
        Directory.CreateDirectory(databasePath);

        using var response = await client.GetAsync("/api/otp/credentials");
        OtpWebHttp.AssertNoStore(response);
        await OtpWebHttp.AssertErrorAsync(response, HttpStatusCode.ServiceUnavailable, "otp_unavailable");
    }

    private static async Task<EnrollmentResponse> EnrollAsync(HttpClient client, string csrfToken, string type)
    {
        using var response = await OtpWebHttp.PostJsonAsync(client, "/api/otp/enroll", csrfToken, new
        {
            type,
            displayName = "integration-test"
        });
        OtpWebHttp.AssertNoStore(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await OtpWebHttp.ReadJsonAsync<EnrollmentResponse>(response);
    }

    private static (ulong Counter, string FirstCode, string SecondCode) FindUniqueResynchronizationPair(byte[] secret, ulong start, int maxOffset)
    {
        for (var offset = 0; offset <= maxOffset; offset++)
        {
            var counter = start + (ulong)offset;
            var first = Hotp.Generate(secret, counter);
            var second = Hotp.Generate(secret, counter + 1);
            var matches = 0;
            for (var candidateOffset = 0; candidateOffset <= maxOffset; candidateOffset++)
            {
                var candidate = start + (ulong)candidateOffset;
                if (Hotp.Generate(secret, candidate) == first && Hotp.Generate(secret, candidate + 1) == second)
                    matches++;
            }
            if (matches == 1)
                return (counter, first, second);
        }
        throw new InvalidOperationException("无法生成唯一 HOTP 重同步测试对。");
    }
}
