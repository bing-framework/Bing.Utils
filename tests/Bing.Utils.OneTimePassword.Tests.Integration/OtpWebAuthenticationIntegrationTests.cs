using System.Net;
using Xunit;

namespace Bing.Utils.OneTimePassword.Tests.Integration;

/// <summary>
/// 验证 OTP Web 的认证入口、防伪保护、Cookie 属性与环境边界。
/// </summary>
public sealed class OtpWebAuthenticationIntegrationTests
{
    /// <summary>
    /// 验证 OTP 管理端点要求认证且开发登录需要有效防伪令牌。
    /// </summary>
    [Fact]
    public async Task Endpoints_WhenUnauthenticatedOrMissingCsrf_ShouldRejectRequest()
    {
        using var host = new OtpWebTestHost();
        using var client = host.CreateClient();

        using (var unauthenticated = await client.GetAsync("/api/otp/credentials"))
        {
            OtpWebHttp.AssertNoStore(unauthenticated);
            Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        }

        var csrfToken = await OtpWebHttp.GetCsrfTokenAsync(client);
        using (var missingToken = await OtpWebHttp.PostEmptyAsync(client, "/api/development/sign-in", null))
        {
            OtpWebHttp.AssertNoStore(missingToken);
            await OtpWebHttp.AssertErrorAsync(missingToken, HttpStatusCode.BadRequest, "csrf_invalid");
        }

        using var signedIn = await OtpWebHttp.PostEmptyAsync(client, "/api/development/sign-in", csrfToken);
        OtpWebHttp.AssertNoStore(signedIn);
        Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
        AssertAuthCookieAttributes(signedIn);
    }

    /// <summary>
    /// 验证开发登录 Cookie 强制使用 Secure、HttpOnly 和 SameSite=Strict。
    /// </summary>
    [Fact]
    public async Task DevelopmentSignIn_WhenCsrfIsValid_ShouldIssueProtectedCookie()
    {
        using var host = new OtpWebTestHost();
        using var client = host.CreateClient();

        await OtpWebHttp.SignInAsync(client);

        using var denied = await client.GetAsync("/api/protected");
        OtpWebHttp.AssertNoStore(denied);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    /// <summary>
    /// 验证生产环境不注册开发登录端点。
    /// </summary>
    [Fact]
    public async Task ProductionHost_WhenCallingDevelopmentSignIn_ShouldReturnNotFound()
    {
        using var host = new OtpWebTestHost("Production");
        using var client = host.CreateClient();
        var csrfToken = await OtpWebHttp.GetCsrfTokenAsync(client);

        using var response = await OtpWebHttp.PostEmptyAsync(client, "/api/development/sign-in", csrfToken);
        OtpWebHttp.AssertNoStore(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static void AssertAuthCookieAttributes(HttpResponseMessage response)
    {
        var cookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value => value.StartsWith("__Host-bing-otp=", StringComparison.Ordinal))
            : null;
        Assert.False(cookie == null, "Authentication cookie was not issued.");
        var attributes = cookie!.Split(';').Skip(1).Select(value => value.Trim()).ToArray();
        Assert.Contains(attributes, value => value.Equals("Secure", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(attributes, value => value.Equals("HttpOnly", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(attributes, value => value.Equals("SameSite=Strict", StringComparison.OrdinalIgnoreCase));
    }
}
