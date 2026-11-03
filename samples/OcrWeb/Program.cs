using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Bing.Ocr;
using Bing.Ocr.PaddleServing;
using Bing.Ocr.SkiaSharp;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
    builder.WebHost.UseUrls("http://127.0.0.1:5086");
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 11 * 1024 * 1024);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN";
    o.Cookie.HttpOnly = true; o.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    // 上传阶段也有独立准入限制，避免在 OCR 队列外无限积累图片。
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext,string>(_ =>
        RateLimitPartition.GetConcurrencyLimiter("ocr", _ => new ConcurrencyLimiterOptions { PermitLimit = 9, QueueLimit = 0 }));
});
builder.Services.AddBingOcr();
builder.Services.AddSingleton(_ => new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan });
builder.Services.AddOcrProvider(sp => new PaddleServingOcrProvider(sp.GetRequiredService<HttpClient>(), new PaddleServingOptions
{
    Endpoint = new Uri(builder.Configuration["Ocr:Endpoint"] ?? "http://127.0.0.1:8080/"),
    ExpectedModelProfileId = builder.Configuration["Ocr:ModelProfileId"]
}));
builder.Services.AddOcrImagePreprocessor(_ => new SkiaOcrImagePreprocessor());
var app = builder.Build();
app.Services.GetRequiredService<IOcrService>();
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' blob:; object-src 'none'; frame-ancestors 'none'; base-uri 'self'";
    try { await next(context); }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
    catch (Exception ex) when (ex is OcrInputException || ex is OcrCapabilityException || ex is OcrBusyException ||
        ex is OcrTimeoutException || ex is OcrProviderException || ex is AntiforgeryValidationException ||
        ex is JsonException || ex is InvalidDataException || ex is ArgumentException || ex is BadHttpRequestException)
    {
        var (status, code, message) = ex switch
        {
            OcrBusyException => (429, "ocr_busy", "识别服务繁忙，请稍后重试。"),
            OcrTimeoutException => (504, "ocr_timeout", "识别超时，可缩小图片区域后重试。"),
            OcrProviderException => (503, "ocr_unavailable", "识别服务不可用，请检查服务配置。"),
            OcrInputException input => (400, input.Code, input.Message),
            OcrCapabilityException => (400, "unsupported_capability", "当前识别服务不支持该选项。"),
            _ => (400, "invalid_request", "请求无效，请重新选择图片和识别选项。")
        };
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new { error = code, message }, context.RequestAborted);
        }
    }
});
app.UseRateLimiter();
app.UseDefaultFiles(); app.UseStaticFiles();
app.MapGet("/api/session", (HttpContext context, IAntiforgery antiforgery) =>
    Results.Json(new { token = antiforgery.GetAndStoreTokens(context).RequestToken }));
app.MapPost("/api/ocr", async (HttpContext context, IAntiforgery antiforgery, IOcrService service) =>
{
    if (!context.Request.Headers.ContainsKey("X-CSRF-TOKEN")) return Results.BadRequest(new { error = "csrf_required", message = "请刷新页面后重试。" });
    await antiforgery.ValidateRequestAsync(context);
    if (context.Request.ContentLength > 11 * 1024 * 1024)
        return Results.Json(new { error = "input_too_large", message = "上传内容超过限制。" }, statusCode: 413);
    if (!MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var mediaType) ||
        !string.Equals(mediaType.MediaType.Value, "multipart/form-data", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "multipart_required", message = "请选择图片文件。" });
    var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
    if (string.IsNullOrEmpty(boundary) || boundary.Length > 128) throw new InvalidDataException("multipart boundary 无效。");
    var reader = new MultipartReader(boundary, context.Request.Body) { HeadersCountLimit = 8, BodyLengthLimit = 10 * 1024 * 1024 };
    byte[]? image = null; OcrOptions options = new();
    var seenOptions = false; var sections = 0;
    while (await reader.ReadNextSectionAsync(context.RequestAborted) is { } section)
    {
        if (++sections > 2 || !ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition))
            throw new InvalidDataException("multipart 字段无效。");
        var name = HeaderUtilities.RemoveQuotes(disposition.Name).Value;
        if (name == "image" && image == null)
            image = await OcrUpload.ReadAsync(section.Body, 10 * 1024 * 1024, context.RequestAborted);
        else if (name == "options" && !seenOptions)
        {
            seenOptions = true;
            var bytes = await OcrUpload.ReadAsync(section.Body, 4096, context.RequestAborted);
            options = JsonSerializer.Deserialize<OcrOptions>(bytes, OcrUpload.JsonOptions) ?? throw new JsonException("缺少选项。");
        }
        else throw new InvalidDataException("重复或未知字段。");
    }
    if (image == null || image.Length == 0) throw new OcrInputException("empty_input", "请选择非空图片。");
    using var input = new MemoryStream(image, false);
    var result = await service.RecognizeAsync(input, options, context.RequestAborted);
    return Results.Json(result);
});
app.Run();

/// <summary>上传流的有限读取；不落盘、不关闭请求流。</summary>
internal static class OcrUpload
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    internal static async Task<byte[]> ReadAsync(Stream input, int limit, CancellationToken token)
    {
        using var output = new MemoryStream(); var buffer = new byte[8192];
        while (true)
        {
            var count = (int)Math.Min(buffer.Length, (long)limit - output.Length + 1);
            var read = await input.ReadAsync(buffer.AsMemory(0, count), token);
            if (read == 0) return output.ToArray();
            if (output.Length + read > limit) throw new OcrInputException("input_too_large", "上传内容超过限制。");
            output.Write(buffer, 0, read);
        }
    }
}
/// <summary>供集成测试创建宿主。</summary>
public partial class Program { }
