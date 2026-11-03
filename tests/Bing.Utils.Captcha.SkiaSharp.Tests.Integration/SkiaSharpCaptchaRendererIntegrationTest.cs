using System;
using System.IO;
using Bing.Captcha;
using Bing.Captcha.SkiaSharp;
using Bing.Drawing;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace Bing.Utils.Captcha.SkiaSharp.Tests.Integration;

public sealed class CjkFontFactAttribute : FactAttribute
{
    public CjkFontFactAttribute()
    {
        var path = Environment.GetEnvironmentVariable("BING_CAPTCHA_TEST_FONT");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            Skip = "BLOCKED_EXTERNAL: Set BING_CAPTCHA_TEST_FONT to a CJK font file to run this test.";
    }
}

public sealed class LatinFontFactAttribute : FactAttribute
{
    public LatinFontFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BING_CAPTCHA_TEST_LATIN_FONT")))
            Skip = "BLOCKED_EXTERNAL: Set BING_CAPTCHA_TEST_LATIN_FONT to a Latin-only font file to run this test.";
    }
}

public sealed class SkiaSharpCaptchaRendererIntegrationTest
{
    [Fact]
    public void Render_ReturnsBoundedPngWithConfiguredDimensions()
    {
        using var renderer = new SkiaSharpCaptchaRenderer(new SkiaSharpCaptchaRendererOptions
        {
            DrawingOptions = new CaptchaOptions { Width = 180, Height = 64, FontSize = 30, FontWidth = 24 }
        });

        var image = renderer.Render("AB23");

        image.ContentType.ShouldBe("image/png");
        image.Width.ShouldBe(180);
        image.Height.ShouldBe(64);
        image.ByteLength.ShouldBeLessThanOrEqualTo(64 * 1024);
        using var decoded = SKImage.FromEncodedData(image.Bytes);
        decoded.ShouldNotBeNull();
        decoded!.Width.ShouldBe(180);
        decoded.Height.ShouldBe(64);
    }

    [Fact]
    public void Constructor_RejectsUnsupportedOrUnboundedSettings()
    {
        Should.Throw<NotSupportedException>(() => new SkiaSharpCaptchaRenderer(
            new SkiaSharpCaptchaRendererOptions { DrawingOptions = new CaptchaOptions { RandomItalic = true } }));
        Should.Throw<NotSupportedException>(() => new SkiaSharpCaptchaRenderer(
            new SkiaSharpCaptchaRendererOptions { DrawingOptions = new CaptchaOptions { CharacterSpacing = 1 } }));
        Should.Throw<ArgumentOutOfRangeException>(() => new SkiaSharpCaptchaRenderer(
            new SkiaSharpCaptchaRendererOptions { DrawingOptions = new CaptchaOptions { Width = 513 } }));
        Should.Throw<ArgumentException>(() => new SkiaSharpCaptchaRenderer(
            new SkiaSharpCaptchaRendererOptions { Type = CaptchaType.ChineseChar }));
    }

    [Fact]
    public void Render_RejectsPngBeyondConfiguredByteLimit()
    {
        using var renderer = new SkiaSharpCaptchaRenderer(new SkiaSharpCaptchaRendererOptions
        {
            MaxImageBytes = 1
        });

        Should.Throw<CaptchaRendererException>(() => renderer.Render("AB23"));
    }

    [Fact]
    public void Render_UsesConfigurationSnapshotAfterCallerMutatesOptions()
    {
        var options = new SkiaSharpCaptchaRendererOptions
        {
            DrawingOptions = new CaptchaOptions { Width = 180, Height = 64, FontSize = 30, FontWidth = 24 }
        };
        using var renderer = new SkiaSharpCaptchaRenderer(options);
        options.DrawingOptions.Width = 300;
        options.DrawingOptions.Height = 100;

        var image = renderer.Render("AB23");

        image.Width.ShouldBe(180);
        image.Height.ShouldBe(64);
    }

    [Fact]
    public void Render_RejectsCharactersOutsideConfiguredAlphabet()
    {
        using var renderer = new SkiaSharpCaptchaRenderer();

        Should.Throw<ArgumentException>(() => renderer.Render("AB!2"));
        Should.Throw<ArgumentOutOfRangeException>(() => renderer.Render("ABC"));
    }

    [Fact]
    public void Dispose_IsIdempotentAndRejectsLaterRendering()
    {
        var renderer = new SkiaSharpCaptchaRenderer();
        using var survivor = new SkiaSharpCaptchaRenderer();

        renderer.Dispose();
        renderer.Dispose();

        Should.Throw<ObjectDisposedException>(() => renderer.Render("AB23"));
        survivor.Render("CD45").Width.ShouldBeGreaterThan(0);
    }

    [CjkFontFact]
    public void Render_ValidatesCompleteChineseAlphabetAndKeepsCallerFontOwned()
    {
        var fontPath = Environment.GetEnvironmentVariable("BING_CAPTCHA_TEST_FONT")!;
        var options = new SkiaSharpCaptchaRendererOptions
        {
            Type = CaptchaType.ChineseChar,
            FontPath = fontPath,
            DrawingOptions = new CaptchaOptions { Width = 220, Height = 72, FontSize = 32, FontWidth = 30 }
        };
        using (var renderer = new SkiaSharpCaptchaRenderer(options))
        {
            var text = CaptchaAlphabet.GetCharacters(CaptchaType.ChineseChar).Substring(0, 4);
            var rendered = renderer.Render(text);
            rendered.Width.ShouldBe(220);
        }

        using var typeface = SKTypeface.FromFile(fontPath);
        using var first = SkiaSharpHelper.CreateCaptchaImage("AB23", null, typeface);
        using var second = SkiaSharpHelper.CreateCaptchaImage("CD45", null, typeface);
        second.Width.ShouldBeGreaterThan(0);
    }

    [LatinFontFact]
    public void Constructor_RejectsExplicitFontWithoutChineseAlphabetCoverage()
    {
        var fontPath = Environment.GetEnvironmentVariable("BING_CAPTCHA_TEST_LATIN_FONT")!;

        Should.Throw<CaptchaRendererException>(() => new SkiaSharpCaptchaRenderer(new SkiaSharpCaptchaRendererOptions
        {
            Type = CaptchaType.ChineseChar,
            FontPath = fontPath
        }));
    }
}
