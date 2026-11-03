using System;
using System.Threading.Tasks;
using Bing.Captcha;
using Bing.Captcha.SkiaSharp;
using Bing.Drawing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bing.Utils.Captcha.SkiaSharp.Tests.Integration;

public sealed class ConfigurationIntegrationTests
{
    [Theory]
    [InlineData(CaptchaType.ChineseChar, true)]
    [InlineData(CaptchaType.Number, true)]
    [InlineData(CaptchaType.NumberAndLetter, false)]
    public void ResolveService_RejectsIncompatibleGeneratorBeforeFirstRequest(CaptchaType type, bool ignoreCase)
    {
        var services = Services(new CaptchaServiceOptions { Type = type, IgnoreAsciiCase = ignoreCase });
        services.AddCaptchaRenderer(_ => new SkiaSharpCaptchaRenderer());
        using var provider = services.BuildServiceProvider();
        Assert.Throws<ArgumentException>(() => provider.GetRequiredService<ICaptchaService>());
        Assert.Equal(0, ((MemoryCaptchaStore)provider.GetRequiredService<ICaptchaStore>()).Count);
    }

    [Theory]
    [InlineData(CaptchaType.Number, true, true)]
    [InlineData(CaptchaType.NumberAndLetter, true, true)]
    [InlineData(CaptchaType.NumberAndLetter, false, false)]
    [InlineData(CaptchaType.NumberAndLetter, true, false)]
    public async Task ResolveService_AcceptsCompatibleAlphabetAndCanGenerate(CaptchaType type, bool generatorIgnoreCase, bool rendererIgnoreCase)
    {
        var services = Services(new CaptchaServiceOptions { Type = type, IgnoreAsciiCase = generatorIgnoreCase });
        services.AddCaptchaRenderer(_ => new SkiaSharpCaptchaRenderer(new SkiaSharpCaptchaRendererOptions { Type = type, IgnoreAsciiCase = rendererIgnoreCase }));
        using var provider = services.BuildServiceProvider();
        var challenge = await provider.GetRequiredService<ICaptchaService>().CreateAsync(new CaptchaContext("login", "session", "form"));
        Assert.Equal("image/png", challenge.Image.ContentType);
    }

    [Fact]
    public void ValidateAlphabet_RejectsNullAndEmpty()
    {
        using var renderer = new SkiaSharpCaptchaRenderer();
        Assert.Throws<ArgumentNullException>(() => renderer.ValidateAlphabet(null!));
        Assert.Throws<ArgumentException>(() => renderer.ValidateAlphabet(""));
    }

    private static IServiceCollection Services(CaptchaServiceOptions options)
    {
        var services = new ServiceCollection();
        services.AddBingCaptcha(options);
        services.AddMemoryCaptchaStore();
        services.AddSingleton<ICaptchaKeyProvider>(new StaticCaptchaKeyProvider("test", new[] { new CaptchaKey("test", new byte[32]) }));
        return services;
    }
}
