using Bing.Captcha;
using Bing.Captcha.Redis;
using Bing.Captcha.SkiaSharp;
using Microsoft.Extensions.DependencyInjection;

// 只引用 NuGet 包，验证包内资产与传递依赖，不引用仓库项目。
internal static class Program
{
    public static async Task Main()
    {
        var services = new ServiceCollection();
        services.AddBingCaptcha(new CaptchaServiceOptions { BackendName = "memory" });
        services.AddMemoryCaptchaStore();
        services.AddSingleton<ICaptchaKeyProvider>(new StaticCaptchaKeyProvider("smoke",
            new[] { new CaptchaKey("smoke", Bing.Security.Randomness.SecurityRandom.GetBytes(32)) }));
        services.AddCaptchaRenderer(_ => new CapturingRenderer());
        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ICaptchaService>();
        var renderer = (CapturingRenderer)provider.GetRequiredService<ICaptchaRenderer>();
        if (!renderer.Validated)
            throw new InvalidOperationException("Consumer renderer configuration was not validated.");
        var context = new CaptchaContext("login", "consumer-session", "form");
        var challenge = await service.CreateAsync(context);
        if (!(await service.VerifyAsync(context, challenge.CaptchaId, renderer.Answer)).Succeeded)
            throw new InvalidOperationException("Consumer verification failed.");
        if ((await service.VerifyAsync(context, challenge.CaptchaId, renderer.Answer)).Succeeded)
            throw new InvalidOperationException("Consumer replay unexpectedly succeeded.");
        Console.WriteLine($"PASS: PNG {challenge.Image.Width}x{challenge.Image.Height}, {challenge.Image.ByteLength} bytes; {typeof(CaptchaRedisStore).Assembly.GetName().Name}");
    }

    private sealed class CapturingRenderer : ICaptchaRenderer, ICaptchaRendererValidator, IDisposable
    {
        private readonly SkiaSharpCaptchaRenderer _inner = new();
        internal string Answer = "";
        internal bool Validated;
        public void ValidateAlphabet(string alphabet)
        {
            _inner.ValidateAlphabet(alphabet);
            Validated = true;
        }
        public CaptchaImage Render(string text, CancellationToken cancellationToken = default)
        {
            Answer = text;
            return _inner.Render(text, cancellationToken);
        }
        public void Dispose() => _inner.Dispose();
    }
}
