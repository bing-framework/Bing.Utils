using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Bing.OneTimePassword;
using Bing.OneTimePassword.DataProtection;
using Bing.OneTimePassword.Redis;
using Bing.OneTimePassword.Sqlite;
using Bing.Security.Randomness;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 8192);
var directory = Path.GetFullPath(builder.Configuration["Otp:DataDirectory"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
Directory.CreateDirectory(directory);
var protection = builder.Services.AddDataProtection().SetApplicationName("Bing.OtpWeb")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(directory, "keys")));
var certificateFile = builder.Configuration["Otp:KeyCertificateFile"];
if (!string.IsNullOrWhiteSpace(certificateFile))
{
    var certificate = new X509Certificate2(certificateFile, builder.Configuration["Otp:KeyCertificatePassword"],
        X509KeyStorageFlags.EphemeralKeySet);
    // 证书作为容器单例注册，随宿主一同释放。
    builder.Services.AddSingleton(_ => certificate);
    protection.ProtectKeysWithCertificate(certificate);
}
else if (OperatingSystem.IsWindows())
    protection.ProtectKeysWithDpapi();
else if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("生产环境必须配置 Otp:KeyCertificateFile 保护持久密钥环。");

builder.Services.AddBingOneTimePassword(new OtpServiceOptions { ApplicationId = "Bing.OtpWeb", Issuer = "Bing OTP Demo" });
builder.Services.AddOtpStore(_ => new SqliteOtpStore(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
    { DataSource = Path.Combine(directory, "otp.db") }.ToString()));
builder.Services.AddOtpSecretProtector(sp => new DataProtectionOtpSecretProtector(sp.GetRequiredService<IDataProtectionProvider>()));
switch (builder.Configuration["Otp:RateLimiter"])
{
    case "Memory": builder.Services.AddMemoryOtpRateLimiter(); break;
    case "Redis":
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(
            builder.Configuration["Otp:Redis"] ?? throw new InvalidOperationException("缺少 Otp:Redis。")));
        builder.Services.AddOtpRateLimiter(sp => new OtpRedisRateLimiter(sp.GetRequiredService<IConnectionMultiplexer>(), "bing:otp-web:rate:v1:"));
        break;
    default: throw new InvalidOperationException("必须显式选择 Memory 或 Redis 限流。");
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.Cookie.Name = "__Host-bing-otp";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    o.SlidingExpiration = false;
    o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("primary", p => p.RequireAuthenticatedUser().RequireClaim(ClaimTypes.NameIdentifier)
        .RequireClaim("otp_session").RequireClaim("primary_at"));
    o.AddPolicy("mfa", p => p.RequireAuthenticatedUser().RequireClaim("auth_level", "mfa"));
});
builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN";
    o.Cookie.Name = "__Host-bing-otp-csrf";
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
});

var app = builder.Build();
if (!string.IsNullOrWhiteSpace(certificateFile)) _ = app.Services.GetRequiredService<X509Certificate2>();
await ((SqliteOtpStore)app.Services.GetRequiredService<IOtpStore>()).InitializeAsync();
_ = app.Services.GetRequiredService<IOtpService>(); // 在接收请求前验证唯一组件。
app.Use(async (http, next) =>
{
    http.Response.Headers.CacheControl = "no-store";
    http.Response.Headers.Pragma = "no-cache";
    http.Response.Headers["X-Content-Type-Options"] = "nosniff";
    http.Response.Headers.ContentSecurityPolicy = "default-src 'self'; script-src 'self'; style-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
    try { await next(); }
    catch (AntiforgeryValidationException) { await OtpWebSupport.Error(http, 400, "csrf_invalid"); }
    catch (OtpRateLimitException) { await OtpWebSupport.Error(http, 429, "otp_rate_limited"); }
    catch (OtpAuthorizationException) { await OtpWebSupport.Error(http, 403, "otp_invalid"); }
    catch (Exception e) when (e is OtpStoreException or OtpProtectionException)
    { await OtpWebSupport.Error(http, 503, "otp_unavailable"); }
    catch (Exception e) when (e is ArgumentException or BadHttpRequestException or System.Text.Json.JsonException)
    { await OtpWebSupport.Error(http, 400, "otp_invalid"); }
});
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (http, next) =>
{
    if (http.Request.Path.StartsWithSegments("/api") && !HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method))
        await http.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(http);
    await next();
});

app.MapGet("/api/session", (HttpContext http, IAntiforgery csrf) => Results.Ok(new
{
    authenticated = http.User.Identity?.IsAuthenticated == true,
    mfa = http.User.HasClaim("auth_level", "mfa"),
    developmentLogin = app.Environment.IsDevelopment(),
    csrfToken = csrf.GetAndStoreTokens(http).RequestToken
}));

// 演示入口只在 Development 注册，账号和主认证时间由服务端固定创建。
if (app.Environment.IsDevelopment())
    app.MapPost("/api/development/sign-in", async (HttpContext http, IOtpClock clock) =>
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "development-demo"),
            new Claim(ClaimTypes.Email, "demo@example.test"),
            new Claim("otp_session", SecurityRandom.GetBase64Url(32)),
            new Claim("primary_at", clock.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)),
            new Claim("auth_level", "primary")
        };
        await http.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return Results.Ok(new { signedIn = true });
    });

var api = app.MapGroup("/api/otp").RequireAuthorization("primary");
api.MapGet("/credentials", (HttpContext http, IOtpService service) => service.GetCredentialsAsync(OtpWebSupport.Context(http, "login"), http.RequestAborted));
api.MapPost("/enroll", (OtpEnrollmentRequest request, HttpContext http, IOtpService service) =>
{
    var type = request.Type switch { "totp" => OtpType.Totp, "hotp" => OtpType.Hotp, _ => throw new ArgumentException("不支持的类型。") };
    return service.BeginEnrollmentAsync(OtpWebSupport.Context(http, "manage"), new OtpParameters(type),
        http.User.FindFirstValue(ClaimTypes.Email) ?? "account", request.DisplayName ?? "Authenticator", http.RequestAborted);
});
api.MapPost("/confirm", async (OtpCodeRequest request, HttpContext http, IOtpService service) =>
    OtpWebSupport.Result(await service.ConfirmEnrollmentAsync(OtpWebSupport.Context(http, "manage"), request.CredentialId ?? "", request.TransactionId ?? "", request.Code, http.RequestAborted)));
api.MapPost("/transactions", (HttpContext http, IOtpService service) =>
    service.BeginVerificationAsync(OtpWebSupport.Context(http, "login"), cancellationToken: http.RequestAborted));
api.MapPost("/verify", async (OtpCodeRequest request, HttpContext http, IOtpService service) =>
{
    var result = await service.VerifyAsync(OtpWebSupport.Context(http, "login"), request.CredentialId ?? "", request.TransactionId ?? "", request.Code, http.RequestAborted);
    if (!result.Succeeded) return OtpWebSupport.Result(result);
    // 只有持久提交后的成功结果才签发 MFA 会话；保留主认证时间，OTP 不延长管理权限。
    var claims = http.User.Claims.Where(c => c.Type != "auth_level").Append(new Claim("auth_level", "mfa"));
    await http.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
    return OtpWebSupport.Result(result);
});
api.MapPost("/revoke", async (OtpCredentialRequest request, HttpContext http, IOtpService service) =>
    await service.RevokeAsync(OtpWebSupport.Context(http, "manage"), request.CredentialId ?? "", http.RequestAborted)
        ? Results.Ok(new { succeeded = true }) : Results.BadRequest(new { error = "otp_invalid" }));
api.MapPost("/resync/transactions", (OtpCredentialRequest request, HttpContext http, IOtpService service) =>
    service.BeginVerificationAsync(OtpWebSupport.Context(http, "manage"), OtpTransactionKind.Resynchronization, request.CredentialId, http.RequestAborted));
api.MapPost("/resync", async (OtpResyncRequest request, HttpContext http, IOtpService service) =>
    OtpWebSupport.Result(await service.ResynchronizeHotpAsync(OtpWebSupport.Context(http, "manage"), request.CredentialId ?? "", request.TransactionId ?? "",
        request.FirstCode, request.SecondCode, http.RequestAborted)));
app.MapGet("/api/protected", () => Results.Ok(new { message = "此请求已通过主认证及 OTP 验证。" })).RequireAuthorization("mfa");
app.MapPost("/api/sign-out", async (HttpContext http) =>
{ await http.SignOutAsync(); return Results.Ok(); }).RequireAuthorization("primary");
app.Run();

/// <summary>
/// 供真实 HTTP 集成测试定位示例宿主。
/// </summary>
public partial class Program;

/// <summary>
/// HTTP 边界映射；任何请求模型都不接受账号、用途或会话绑定。
/// </summary>
internal static class OtpWebSupport
{
    /// <summary>
    /// 从受保护身份票据创建可信上下文。
    /// </summary>
    internal static OtpContext Context(HttpContext http, string purpose)
    {
        if (!long.TryParse(http.User.FindFirstValue("primary_at"), NumberStyles.None, CultureInfo.InvariantCulture, out var time))
            throw new OtpAuthorizationException();
        return new OtpContext(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new OtpAuthorizationException(),
            purpose, http.User.FindFirstValue("otp_session") ?? throw new OtpAuthorizationException(),
            DateTimeOffset.FromUnixTimeMilliseconds(time), http.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }
    /// <summary>
    /// 对外隐藏重放、锁定、账号和设备细节。
    /// </summary>
    internal static IResult Result(OtpVerifyResult result) => result.Succeeded
        ? Results.Ok(new { succeeded = true }) : Results.BadRequest(new { error = "otp_invalid" });
    /// <summary>
    /// 只返回固定错误码，禁止向响应复制敏感异常。
    /// </summary>
    internal static Task Error(HttpContext http, int status, string error)
    { http.Response.StatusCode = status; return http.Response.WriteAsJsonAsync(new { error }); }
}

/// <summary>
/// 注册请求只携带设备类型和显示名。
/// </summary>
public sealed class OtpEnrollmentRequest
{
    /// <summary>
    /// 获取或设置 totp/hotp。
    /// </summary>
    public string? Type { get; set; }
    /// <summary>
    /// 获取或设置设备显示名。
    /// </summary>
    public string? DisplayName { get; set; }
}

/// <summary>
/// 请求中的凭据选择器，不代表归属证明。
/// </summary>
public class OtpCredentialRequest
{
    /// <summary>
    /// 获取或设置待操作凭据 ID。
    /// </summary>
    public string? CredentialId { get; set; }
}

/// <summary>
/// 含敏感验证码的请求；默认 ToString 不展开属性。
/// </summary>
public sealed class OtpCodeRequest : OtpCredentialRequest
{
    /// <summary>
    /// 获取或设置验证事务 ID。
    /// </summary>
    public string? TransactionId { get; set; }
    /// <summary>
    /// 获取或设置验证码；禁止记录日志。
    /// </summary>
    public string? Code { get; set; }
}

/// <summary>
/// HOTP 重同步的连续两码请求。
/// </summary>
public sealed class OtpResyncRequest : OtpCredentialRequest
{
    /// <summary>
    /// 获取或设置专用事务 ID。
    /// </summary>
    public string? TransactionId { get; set; }
    /// <summary>
    /// 获取或设置第一个验证码。
    /// </summary>
    public string? FirstCode { get; set; }
    /// <summary>
    /// 获取或设置紧随其后的验证码。
    /// </summary>
    public string? SecondCode { get; set; }
}
