using SkiaSharp;

namespace Bing.Drawing;

public class SkiaSharpHelperTest
{
    [Fact]
    public void ToBytes_And_FromBytes_RoundTrip()
    {
        using var source = CreateSampleImage();
        var bytes = SkiaSharpHelper.ToBytes(source, (SKEncodedImageFormat.Png, 100));
        bytes.Length.ShouldBeGreaterThan(0);

        using var restored = SkiaSharpHelper.FromBytes(bytes);
        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(2);
        restored.Height.ShouldBe(2);
    }

    [Fact]
    public void ToBase64String_And_FromBase64String_RoundTrip()
    {
        using var source = CreateSampleImage();
        var base64 = SkiaSharpHelper.ToBase64String(source, (SKEncodedImageFormat.Png, 100));
        base64.ShouldNotBeNullOrWhiteSpace();

        using var restored = SkiaSharpHelper.FromBase64String(base64);
        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(2);
        restored.Height.ShouldBe(2);
    }

    [Fact]
    public void ToDataUrl_ContainsDataUrlShape()
    {
        using var source = CreateSampleImage();
        var dataUrl = SkiaSharpHelper.ToDataUrl(source, (SKEncodedImageFormat.Png, 100));
        dataUrl.ShouldStartWith("data:image/");
        dataUrl.ShouldContain(";base64,");
    }

    [Fact]
    public void FromDataUrl_WithValidPngDataUrl_RoundTrip()
    {
        using var source = CreateSampleImage();
        var base64 = SkiaSharpHelper.ToBase64String(source, (SKEncodedImageFormat.Png, 100));
        var dataUrl = $"data:image/png;base64,{base64}";

        using var restored = SkiaSharpHelper.FromDataUrl(dataUrl);
        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(2);
        restored.Height.ShouldBe(2);
    }

    [Fact]
    public void FromBytes_Null_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.FromBytes(null!))
            .ParamName.ShouldBe("bytes");
    }

    [Fact]
    public void ToBytes_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToBytes(null!))
            .ParamName.ShouldBe("image");
    }

    [Fact]
    public void ToBase64String_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToBase64String(null!))
            .ParamName.ShouldBe("image");
    }

    [Fact]
    public void ToDataUrl_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToDataUrl(null!))
            .ParamName.ShouldBe("image");
    }

    [Fact]
    public void FromBase64String_InvalidValue_ReturnsNull()
    {
        var restored = SkiaSharpHelper.FromBase64String("not-base64");
        restored.ShouldBeNull();
    }

    [Fact]
    public void FromDataUrl_InvalidFormat_ReturnsNull()
    {
        var restored = SkiaSharpHelper.FromDataUrl("invalid-data-url");
        restored.ShouldBeNull();
    }

    [Fact]
    public void FromFile_LoadExpectedImage()
    {
        using var source = CreateSampleImage();
        var bytes = SkiaSharpHelper.ToBytes(source, (SKEncodedImageFormat.Png, 100));
        var tempPath = Path.Combine(Path.GetTempPath(), $"bing-utils-skiasharp-{Guid.NewGuid():N}.png");
        try
        {
            File.WriteAllBytes(tempPath, bytes);
            using var restored = SkiaSharpHelper.FromFile(tempPath);
            restored.ShouldNotBeNull();
            restored!.Width.ShouldBe(2);
            restored.Height.ShouldBe(2);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public void SetOpacity_OutOfRange_ThrowsArgumentOutOfRangeException()
    {
        using var source = CreateSampleImage();
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.SetOpacity(source, -0.1f))
            .ParamName.ShouldBe("opacity");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.SetOpacity(source, 1.1f))
            .ParamName.ShouldBe("opacity");
    }

    [Fact]
    public void SetOpacity_ValidOpacity_ReturnsImageWithExpectedAlpha()
    {
        using var source = CreateSampleImage();
        using var result = SkiaSharpHelper.SetOpacity(source, 0.5f);

        result.ShouldNotBeNull();
        ReferenceEquals(source, result).ShouldBeFalse();
        result.Width.ShouldBe(source.Width);
        result.Height.ShouldBe(source.Height);

        using var bitmap = SKBitmap.FromImage(result);
        bitmap.GetPixel(0, 0).Alpha.ShouldBe((byte)127);
    }

    [Fact]
    public void SetOpacity_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.SetOpacity(null!, 0.5f))
            .ParamName.ShouldBe("image");
    }

    private static SKImage CreateSampleImage()
    {
        using var bitmap = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(255, 0, 0, 255));
        bitmap.SetPixel(1, 0, new SKColor(0, 255, 0, 255));
        bitmap.SetPixel(0, 1, new SKColor(0, 0, 255, 255));
        bitmap.SetPixel(1, 1, new SKColor(255, 255, 255, 255));
        return SKImage.FromBitmap(bitmap);
    }
}
