using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using Bing.OneTimePassword;
using Xunit;

namespace Bing.Utils.OneTimePassword.Tests.Integration;

/// <summary>
/// 验证 TOTP 绑定、会话绑定、重放拒绝及 MFA 会话签发。
/// </summary>
public sealed class OtpWebTotpIntegrationTests
{
    /// <summary>
    /// 验证跨会话绑定和重放统一失败，只有新时间步验证后可访问受保护端点。
    /// </summary>
    [Fact]
    public async Task TotpLifecycle_WhenBoundAndVerified_ShouldRequireFreshCodeForMfa()
    {
        using var host = new OtpWebTestHost();
        using var primaryClient = host.CreateClient();
        using var secondClient = host.CreateClient();

        using (var unauthenticated = await primaryClient.GetAsync("/api/otp/credentials"))
        {
            OtpWebHttp.AssertNoStore(unauthenticated);
            Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        }
        var primaryCsrf = await OtpWebHttp.SignInAsync(primaryClient);
        using (var beforeOtp = await primaryClient.GetAsync("/api/protected"))
        {
            OtpWebHttp.AssertNoStore(beforeOtp);
            Assert.Equal(HttpStatusCode.Forbidden, beforeOtp.StatusCode);
        }

        var enrollment = await EnrollTotpAsync(primaryClient, primaryCsrf);
        var secret = OtpSecretEncoding.Decode(enrollment.SecretBase32);
        try
        {
            var secondCsrf = await OtpWebHttp.SignInAsync(secondClient);
            var enrollmentCode = Totp.Generate(secret, host.Clock.UtcNow);
            using (var crossSession = await OtpWebHttp.PostJsonAsync(secondClient, "/api/otp/confirm", secondCsrf, new
            {
                credentialId = enrollment.CredentialId,
                transactionId = enrollment.TransactionId,
                code = enrollmentCode
            }))
            {
                OtpWebHttp.AssertNoStore(crossSession);
                await OtpWebHttp.AssertErrorAsync(crossSession, HttpStatusCode.BadRequest, "otp_invalid");
            }

            var wrongCode = FindWrongTotpCode(secret, host.Clock.UtcNow);
            using (var invalidCode = await OtpWebHttp.PostJsonAsync(primaryClient, "/api/otp/confirm", primaryCsrf, new
            {
                credentialId = enrollment.CredentialId,
                transactionId = enrollment.TransactionId,
                code = wrongCode
            }))
            {
                OtpWebHttp.AssertNoStore(invalidCode);
                await OtpWebHttp.AssertErrorAsync(invalidCode, HttpStatusCode.BadRequest, "otp_invalid");
            }

            using (var confirm = await OtpWebHttp.PostJsonAsync(primaryClient, "/api/otp/confirm", primaryCsrf, new
            {
                credentialId = enrollment.CredentialId,
                transactionId = enrollment.TransactionId,
                code = enrollmentCode
            }))
            {
                OtpWebHttp.AssertNoStore(confirm);
                Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
            }

            using (var beforeVerification = await primaryClient.GetAsync("/api/protected"))
            {
                OtpWebHttp.AssertNoStore(beforeVerification);
                Assert.Equal(HttpStatusCode.Forbidden, beforeVerification.StatusCode);
            }

            using (var credentials = await primaryClient.GetAsync("/api/otp/credentials"))
            {
                OtpWebHttp.AssertNoStore(credentials);
                Assert.Equal(HttpStatusCode.OK, credentials.StatusCode);
                var json = await credentials.Content.ReadAsStringAsync();
                OtpWebHttp.AssertNoSecretOrUri(json, enrollment.SecretBase32, enrollment.ProvisioningUri);
            }

            var replayTransaction = await BeginLoginTransactionAsync(primaryClient, primaryCsrf);
            using (var replay = await OtpWebHttp.PostJsonAsync(primaryClient, "/api/otp/verify", primaryCsrf, new
            {
                credentialId = enrollment.CredentialId,
                transactionId = replayTransaction.TransactionId,
                code = enrollmentCode
            }))
            {
                OtpWebHttp.AssertNoStore(replay);
                await OtpWebHttp.AssertErrorAsync(replay, HttpStatusCode.BadRequest, "otp_invalid");
            }

            var currentStep = host.Clock.UtcNow.ToUnixTimeSeconds() / 30;
            host.Clock.UtcNow = DateTimeOffset.FromUnixTimeSeconds((currentStep + 1) * 30);
            var freshCode = Totp.Generate(secret, host.Clock.UtcNow);
            var freshTransaction = await BeginLoginTransactionAsync(primaryClient, primaryCsrf);
            using (var verify = await OtpWebHttp.PostJsonAsync(primaryClient, "/api/otp/verify", primaryCsrf, new
            {
                credentialId = enrollment.CredentialId,
                transactionId = freshTransaction.TransactionId,
                code = freshCode
            }))
            {
                OtpWebHttp.AssertNoStore(verify);
                Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
            }

            using var protectedResponse = await primaryClient.GetAsync("/api/protected");
            OtpWebHttp.AssertNoStore(protectedResponse);
            Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    private static async Task<EnrollmentResponse> EnrollTotpAsync(HttpClient client, string csrfToken)
    {
        using var response = await OtpWebHttp.PostJsonAsync(client, "/api/otp/enroll", csrfToken, new
        {
            type = "totp",
            displayName = "integration-test"
        });
        OtpWebHttp.AssertNoStore(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await OtpWebHttp.ReadJsonAsync<EnrollmentResponse>(response);
    }

    private static async Task<TransactionResponse> BeginLoginTransactionAsync(HttpClient client, string csrfToken)
    {
        using var response = await OtpWebHttp.PostEmptyAsync(client, "/api/otp/transactions", csrfToken);
        OtpWebHttp.AssertNoStore(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await OtpWebHttp.ReadJsonAsync<TransactionResponse>(response);
    }

    private static string FindWrongTotpCode(byte[] secret, DateTimeOffset timestamp)
    {
        var currentStep = timestamp.ToUnixTimeSeconds() / 30;
        var validCodes = new HashSet<string>(StringComparer.Ordinal);
        for (var step = currentStep - 1; step <= currentStep + 1; step++)
        {
            if (step >= 0)
                validCodes.Add(Hotp.Generate(secret, (ulong)step));
        }

        for (var candidate = 0; candidate < 1_000_000; candidate++)
        {
            var code = candidate.ToString("D6", CultureInfo.InvariantCulture);
            if (!validCodes.Contains(code))
                return code;
        }
        throw new InvalidOperationException("无法生成非匹配 TOTP 测试输入。");
    }
}
