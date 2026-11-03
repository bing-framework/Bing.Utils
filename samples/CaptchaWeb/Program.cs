using Bing.Captcha;
using Bing.Captcha.SkiaSharp;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDataProtection();
builder.Services.AddAntiforgery(o => { o.HeaderName = "X-CSRF-TOKEN"; o.Cookie.SecurePolicy = CookieSecurePolicy.Always; o.Cookie.SameSite = SameSiteMode.Strict; });
builder.Services.AddBingCaptcha(new CaptchaServiceOptions { AllowedPurposes = new[] { "demo" }, BackendName = "memory" });
builder.Services.AddMemoryCaptchaStore();
builder.Services.AddSingleton<ICaptchaKeyProvider>(_ =>
{
    var secret = builder.Configuration["Captcha:Key"] ?? throw new InvalidOperationException("请通过 Captcha__Key 配置至少 32 字节的 Base64 密钥。");
    return new StaticCaptchaKeyProvider("v1", new[] { new CaptchaKey("v1", Convert.FromBase64String(secret)) });
});
builder.Services.AddCaptchaRenderer(_ => new SkiaSharpCaptchaRenderer());
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
        PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })),
        PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitPartition.GetFixedWindowLimiter(
            context.Items["binding"] as string ?? "anonymous", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
});
var app = builder.Build();
// 启动时解析，配置、字体或组件缺失不能延迟到第一次请求。
app.Services.GetRequiredService<ICaptchaService>();
var protector = app.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("CaptchaWeb.Binding.v1");
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    string? binding = null;
    if (context.Request.Cookies.TryGetValue("captcha.binding", out var value))
    {
        try { binding = protector.Unprotect(value); } catch (System.Security.Cryptography.CryptographicException) { }
    }
    if (binding == null)
    {
        binding = Bing.Security.Randomness.SecurityRandom.GetBase64Url(32);
        context.Response.Cookies.Append("captcha.binding", protector.Protect(binding), new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, IsEssential = true });
    }
    context.Items["binding"] = binding;
    try { await next(context); }
    catch (AntiforgeryValidationException) { context.Response.StatusCode = 400; }
    catch (ArgumentException) { context.Response.StatusCode = 400; }
    catch (CaptchaStoreException) { context.Response.StatusCode = 503; await context.Response.WriteAsJsonAsync(new { error = "captcha_unavailable" }); }
    catch (CaptchaRendererException) { context.Response.StatusCode = 503; await context.Response.WriteAsJsonAsync(new { error = "captcha_unavailable" }); }
});
app.UseRateLimiter();
app.UseDefaultFiles(); app.UseStaticFiles();
app.MapGet("/api/session", (HttpContext context, IAntiforgery antiforgery) => Results.Json(new { token = antiforgery.GetAndStoreTokens(context).RequestToken }));
app.MapPost("/api/captchas", async (HttpContext http, ChallengeRequest request, IAntiforgery antiforgery, ICaptchaService service) =>
{
    await antiforgery.ValidateRequestAsync(http);
    var challenge = await service.CreateAsync(new CaptchaContext("demo", (string)http.Items["binding"]!, request.FlowId), http.RequestAborted);
    return Results.Json(new { challenge.CaptchaId, imageBase64 = Convert.ToBase64String(challenge.Image.Bytes), challenge.Image.ContentType, challenge.Image.Width, challenge.Image.Height, challenge.ExpiresAt });
});
app.MapPost("/api/demo/submit", async (HttpContext http, Submission request, IAntiforgery antiforgery, ICaptchaService service) =>
{
    await antiforgery.ValidateRequestAsync(http);
    var result = await service.VerifyAsync(new CaptchaContext("demo", (string)http.Items["binding"]!, request.FlowId), request.CaptchaId, request.Answer, http.RequestAborted);
    return result.Succeeded ? Results.Json(new { message = "验证码已消费，示例操作完成。" }) : Results.Json(new { error = "captcha_invalid" }, statusCode: 400);
});
app.Run();
/// <summary>创建挑战的表单标识。</summary>
public sealed record ChallengeRequest(string FlowId);
/// <summary>提交当前挑战和答案。</summary>
public sealed record Submission(string FlowId, string CaptchaId, string Answer);
/// <summary>供集成测试创建应用宿主。</summary>
public partial class Program { }
