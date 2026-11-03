using System.Net;
using Xunit;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Net.Http.Json;
using Bing.OneTimePassword;
using Bing.OneTimePassword.DataProtection;
using Bing.OneTimePassword.Redis;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Bing.Utils.OneTimePassword.Tests.Integration;

/// <summary>
/// 为每个 HTTP 用例创建独立的 SQLite 文件和 Data Protection 密钥环。
/// </summary>
internal sealed class OtpWebTestHost : IDisposable
{
    private readonly string _rootDirectory;
    private readonly OtpWebApplicationFactory _factory;

    public OtpWebTestHost(string environment = "Development", string? redisConnection = null)
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "bing-otp-web-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootDirectory);
        DataDirectory = Path.Combine(_rootDirectory, "data");
        Clock = new MutableOtpClock(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));
        RateLimiter = new SwitchableOtpRateLimiter();

        string? certificatePath = null;
        string? certificatePassword = null;
        if (!environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            certificatePath = Path.Combine(_rootDirectory, "test-key-ring.pfx");
            certificatePassword = Guid.NewGuid().ToString("N");
            CreateTemporaryCertificate(certificatePath, certificatePassword);
        }

        _factory = new OtpWebApplicationFactory(
            environment,
            FindSampleContentRoot(),
            DataDirectory,
            certificatePath,
            certificatePassword,
            Clock,
            RateLimiter,
            redisConnection);
    }

    public string DataDirectory { get; }

    public MutableOtpClock Clock { get; }

    public SwitchableOtpRateLimiter RateLimiter { get; }

    public SwitchableOtpSecretProtector Protector => _factory.Services.GetRequiredService<SwitchableOtpSecretProtector>();

    public IOtpStore Store => _factory.Services.GetRequiredService<IOtpStore>();

    public HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost/"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    public void Dispose()
    {
        _factory.Dispose();
        if (Directory.Exists(_rootDirectory))
            Directory.Delete(_rootDirectory, recursive: true);
    }

    private static void CreateTemporaryCertificate(string path, string password)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=OTP Web Integration Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
    }

    private static string FindSampleContentRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "samples", "OtpWeb");
            if (File.Exists(Path.Combine(candidate, "OtpWeb.csproj")))
                return candidate;
        }
        throw new DirectoryNotFoundException("无法定位 OtpWeb 示例目录。");
    }
}

internal sealed class OtpWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;
    private readonly string _contentRoot;
    private readonly string _dataDirectory;
    private readonly string? _certificatePath;
    private readonly string? _certificatePassword;
    private readonly MutableOtpClock _clock;
    private readonly SwitchableOtpRateLimiter _rateLimiter;
    private readonly string? _redisConnection;
    private readonly string _redisKeyPrefix = "bing:otp-web:integration:" + Guid.NewGuid().ToString("N") + ":";

    public OtpWebApplicationFactory(
        string environment,
        string contentRoot,
        string dataDirectory,
        string? certificatePath,
        string? certificatePassword,
        MutableOtpClock clock,
        SwitchableOtpRateLimiter rateLimiter,
        string? redisConnection)
    {
        _environment = environment;
        _contentRoot = contentRoot;
        _dataDirectory = dataDirectory;
        _certificatePath = certificatePath;
        _certificatePassword = certificatePassword;
        _clock = clock;
        _rateLimiter = rateLimiter;
        _redisConnection = redisConnection;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.UseContentRoot(_contentRoot);
        // 最小宿主在 CreateBuilder 后立即读取配置，需要在宿主设置中提前注入。
        builder.UseSetting("Otp:DataDirectory", _dataDirectory);
        var rateLimiterKind = _redisConnection == null ? "Memory" : "Redis";
        builder.UseSetting("Otp:RateLimiter", rateLimiterKind);
        if (_redisConnection != null)
            builder.UseSetting("Otp:Redis", _redisConnection);
        if (_certificatePath != null)
        {
            builder.UseSetting("Otp:KeyCertificateFile", _certificatePath);
            builder.UseSetting("Otp:KeyCertificatePassword", _certificatePassword);
        }
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Otp:DataDirectory"] = _dataDirectory,
                ["Otp:RateLimiter"] = rateLimiterKind
            };
            if (_redisConnection != null)
                settings["Otp:Redis"] = _redisConnection;
            if (_certificatePath != null)
            {
                settings["Otp:KeyCertificateFile"] = _certificatePath;
                settings["Otp:KeyCertificatePassword"] = _certificatePassword;
            }
            configuration.AddInMemoryCollection(settings);
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IOtpClock>();
            services.AddSingleton<IOtpClock>(_clock);

            services.RemoveAll<IOtpRateLimiter>();
            if (_redisConnection == null)
            {
                services.AddSingleton(_rateLimiter);
                services.AddSingleton<IOtpRateLimiter>(sp => sp.GetRequiredService<SwitchableOtpRateLimiter>());
            }
            else
            {
                // 使用示例创建的真实连接，仅隔离测试键前缀，键按正常限流窗口过期。
                services.AddOtpRateLimiter(sp => new OtpRedisRateLimiter(
                    sp.GetRequiredService<IConnectionMultiplexer>(), _redisKeyPrefix));
            }

            services.RemoveAll<IOtpSecretProtector>();
            services.AddSingleton<SwitchableOtpSecretProtector>(sp => new SwitchableOtpSecretProtector(
                new DataProtectionOtpSecretProtector(sp.GetRequiredService<IDataProtectionProvider>())));
            services.AddSingleton<IOtpSecretProtector>(sp => sp.GetRequiredService<SwitchableOtpSecretProtector>());
        });
    }
}

internal sealed class MutableOtpClock : IOtpClock
{
    public MutableOtpClock(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; set; }
}

internal sealed class SwitchableOtpRateLimiter : IOtpRateLimiter
{
    public bool RejectAll { get; set; }

    public Task<bool> TryAcquireAsync(string partition, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(!RejectAll);
    }
}

internal sealed class SwitchableOtpSecretProtector : IOtpSecretProtector
{
    private readonly IOtpSecretProtector _inner;

    public SwitchableOtpSecretProtector(IOtpSecretProtector inner) => _inner = inner;

    public bool FailUnprotect { get; set; }

    public byte[] Protect(OtpSecretContext context, byte[] secret) => _inner.Protect(context, secret);

    public byte[] Unprotect(OtpSecretContext context, byte[] protectedSecret)
    {
        if (FailUnprotect)
            throw new OtpProtectionException("集成测试注入的保护器故障。");
        return _inner.Unprotect(context, protectedSecret);
    }
}

internal static class OtpWebHttp
{
    public static async Task<string> SignInAsync(HttpClient client)
    {
        var token = await GetCsrfTokenAsync(client);
        using (var missingTokenResponse = await PostEmptyAsync(client, "/api/development/sign-in", null))
        {
            AssertNoStore(missingTokenResponse);
            await AssertErrorAsync(missingTokenResponse, HttpStatusCode.BadRequest, "csrf_invalid");
        }

        using var response = await PostEmptyAsync(client, "/api/development/sign-in", token);
        AssertNoStore(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertAuthCookieAttributes(response);
        // 防伪令牌绑定身份；登录后匿名令牌立即失效。
        return await GetCsrfTokenAsync(client);
    }

    public static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/session");
        AssertNoStore(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await ReadJsonAsync<SessionResponse>(response);
        Assert.False(string.IsNullOrWhiteSpace(session.CsrfToken));
        return session.CsrfToken;
    }

    public static async Task<HttpResponseMessage> PostEmptyAsync(HttpClient client, string path, string? csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (csrfToken != null)
            request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", csrfToken);
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, string csrfToken, object value)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(value)
        };
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", csrfToken);
        return await client.SendAsync(request);
    }

    public static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response) where T : class
    {
        var value = await response.Content.ReadFromJsonAsync<T>();
        Assert.NotNull(value);
        return value!;
    }

    public static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string error)
    {
        Assert.Equal(status, response.StatusCode);
        var body = await ReadJsonAsync<ErrorResponse>(response);
        Assert.Equal(error, body.Error);
    }

    public static void AssertNoStore(HttpResponseMessage response)
    {
        Assert.True(response.Headers.CacheControl?.NoStore == true, "API response must set Cache-Control: no-store.");
        var hasNoCache = response.Headers.TryGetValues("Pragma", out var values)
            && values.SelectMany(value => value.Split(',')).Any(value => value.Trim().Equals("no-cache", StringComparison.OrdinalIgnoreCase));
        Assert.True(hasNoCache, "API response must set Pragma: no-cache.");
    }

    public static void AssertNoSecretOrUri(string json, string secretBase32, string provisioningUri)
    {
        using var document = JsonDocument.Parse(json);
        var hasSensitiveProperty = HasSensitiveProperty(document.RootElement);
        var hasSensitiveValue = json.Contains(secretBase32, StringComparison.Ordinal)
            || json.Contains(provisioningUri, StringComparison.Ordinal);
        Assert.False(hasSensitiveProperty || hasSensitiveValue, "设备列表响应包含密钥或注册 URI。");
    }

    public static void AssertCredentialNotListed(string json, string credentialId)
    {
        using var document = JsonDocument.Parse(json);
        var isListed = document.RootElement.EnumerateArray()
            .Any(item => item.GetProperty("credentialId").GetString() == credentialId);
        Assert.False(isListed, "已撤销凭据仍出现在设备列表中。");
    }

    private static void AssertAuthCookieAttributes(HttpResponseMessage response)
    {
        var cookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value => value.StartsWith("__Host-bing-otp=", StringComparison.Ordinal))
            : null;
        Assert.False(cookie == null, "Authentication cookie was not issued.");
        var attributes = cookie!.Split(';').Skip(1).Select(value => value.Trim()).ToArray();
        Assert.True(attributes.Any(value => value.Equals("Secure", StringComparison.OrdinalIgnoreCase)), "Authentication cookie must be Secure.");
        Assert.True(attributes.Any(value => value.Equals("HttpOnly", StringComparison.OrdinalIgnoreCase)), "Authentication cookie must be HttpOnly.");
        Assert.True(attributes.Any(value => value.Equals("SameSite=Strict", StringComparison.OrdinalIgnoreCase)), "Authentication cookie must use SameSite=Strict.");
    }

    private static bool HasSensitiveProperty(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals("secret", StringComparison.OrdinalIgnoreCase)
                    || property.Name.Equals("secretBase32", StringComparison.OrdinalIgnoreCase)
                    || property.Name.Equals("protectedSecret", StringComparison.OrdinalIgnoreCase)
                    || property.Name.Equals("uri", StringComparison.OrdinalIgnoreCase)
                    || property.Name.Equals("provisioningUri", StringComparison.OrdinalIgnoreCase)
                    || property.Name.Equals("code", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (HasSensitiveProperty(property.Value))
                    return true;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
                if (HasSensitiveProperty(child))
                    return true;
        }
        return false;
    }
}

internal sealed class SessionResponse
{
    public string CsrfToken { get; set; } = "";
}

internal sealed class ErrorResponse
{
    public string Error { get; set; } = "";
}

internal sealed class EnrollmentResponse
{
    public string CredentialId { get; set; } = "";
    public string TransactionId { get; set; } = "";
    public string SecretBase32 { get; set; } = "";
    public string ProvisioningUri { get; set; } = "";
}

internal sealed class TransactionResponse
{
    public string TransactionId { get; set; } = "";
}
