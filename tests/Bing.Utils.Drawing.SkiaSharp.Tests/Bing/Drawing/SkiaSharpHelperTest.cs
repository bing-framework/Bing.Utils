using SkiaSharp;
using System.Text.RegularExpressions;
namespace Bing.Drawing;
/// <summary>
/// 测试类：覆盖 `SkiaSharpHelper` 相关行为。
/// </summary>
public class SkiaSharpHelperTest
{
    /// <summary>
    /// 测试用例：验证 `ToBytes` 在 `And_FromBytes` 场景下，结果为 `RoundTrip`。
    /// </summary>
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
    /// <summary>
    /// 测试用例：验证 `ToBase64String` 在 `And_FromBase64String` 场景下，结果为 `RoundTrip`。
    /// </summary>
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
    /// <summary>
    /// 测试用例：验证 `ToDataUrl` 在 `ContainsDataUrlShape` 场景下的行为。
    /// </summary>
    [Fact]
    public void ToDataUrl_ContainsDataUrlShape()
    {
        using var source = CreateSampleImage();
        var dataUrl = SkiaSharpHelper.ToDataUrl(source, (SKEncodedImageFormat.Png, 100));
        dataUrl.ShouldStartWith("data:image/");
        dataUrl.ShouldContain(";base64,");
    }
    /// <summary>
    /// 测试用例：验证 `FromDataUrl` 在 `WithValidPngDataUrl` 场景下，结果为 `RoundTrip`。
    /// </summary>
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
    /// <summary>
    /// 测试用例：验证 `FromBytes` 在 `Null` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void FromBytes_Null_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.FromBytes(null!))
            .ParamName.ShouldBe("bytes");
    }
    /// <summary>
    /// 测试用例：验证 `ToBytes` 在 `NullImage` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void ToBytes_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToBytes(null!))
            .ParamName.ShouldBe("image");
    }
    /// <summary>
    /// 测试用例：验证 `ToBase64String` 在 `NullImage` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void ToBase64String_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToBase64String(null!))
            .ParamName.ShouldBe("image");
    }
    /// <summary>
    /// 测试用例：验证 `ToDataUrl` 在 `NullImage` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void ToDataUrl_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToDataUrl(null!))
            .ParamName.ShouldBe("image");
    }
    /// <summary>
    /// 测试用例：验证 `FromBase64String` 在 `InvalidValue` 场景下，结果为 `ReturnsNull`。
    /// </summary>
    [Fact]
    public void FromBase64String_InvalidValue_ReturnsNull()
    {
        var restored = SkiaSharpHelper.FromBase64String("not-base64");
        restored.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `FromDataUrl` 在 `InvalidFormat` 场景下，结果为 `ReturnsNull`。
    /// </summary>
    [Fact]
    public void FromDataUrl_InvalidFormat_ReturnsNull()
    {
        var restored = SkiaSharpHelper.FromDataUrl("invalid-data-url");
        restored.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `FromFile` 在 `LoadExpectedImage` 场景下的行为。
    /// </summary>
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
    /// <summary>
    /// 测试用例：验证 `SetOpacity` 在 `OutOfRange` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void SetOpacity_OutOfRange_ThrowsArgumentOutOfRangeException()
    {
        using var source = CreateSampleImage();
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.SetOpacity(source, -0.1f))
            .ParamName.ShouldBe("opacity");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.SetOpacity(source, 1.1f))
            .ParamName.ShouldBe("opacity");
    }
    /// <summary>
    /// 测试用例：验证 `SetOpacity` 在 `ValidOpacity` 场景下，结果为 `ReturnsImageWithExpectedAlpha`。
    /// </summary>
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
    /// <summary>
    /// 测试用例：验证 `SetOpacity` 在 `NullImage` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void SetOpacity_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.SetOpacity(null!, 0.5f))
            .ParamName.ShouldBe("image");
    }

    /// <summary>
    /// 测试用例：验证图片信息 API 返回正确的 MIME 和扩展名。
    /// </summary>
    [Fact]
    public void GetMimeType_And_GetImageExtension_PngLoadedImage_ReturnExpectedValues()
    {
        using var source = CreateSampleImage();
        var bytes = SkiaSharpHelper.ToBytes(source, SKEncodedImageFormat.Png, 100);
        using var restored = SkiaSharpHelper.FromBytes(bytes);

        restored.ShouldNotBeNull();
        SkiaSharpHelper.GetMimeType(restored!).ShouldBe("image/png");
        SkiaSharpHelper.GetImageExtension(restored).ShouldBe("png");
    }

    /// <summary>
    /// 测试用例：验证透明图片会被识别为包含 Alpha。
    /// </summary>
    [Fact]
    public void HasAlpha_TransparentImage_ReturnsTrue()
    {
        using var bitmap = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(255, 0, 0, 0));
        using var image = SKImage.FromBitmap(bitmap);

        SkiaSharpHelper.HasAlpha(image).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证保持宽高比缩放时返回新图像且尺寸符合预期。
    /// </summary>
    [Fact]
    public void Resize_KeepAspectRatio_ReturnsExpectedSize()
    {
        using var source = CreatePatternImage(40, 20);
        using var result = SkiaSharpHelper.Resize(source, 10, 10);

        ReferenceEquals(source, result).ShouldBeFalse();
        result.Width.ShouldBe(10);
        result.Height.ShouldBe(5);
        source.Width.ShouldBe(40);
        source.Height.ShouldBe(20);
    }

    /// <summary>
    /// 测试用例：验证禁止放大时保留原始尺寸。
    /// </summary>
    [Fact]
    public void Resize_AllowEnlargeFalse_DoesNotGrowImage()
    {
        using var source = CreatePatternImage(4, 2);
        using var result = SkiaSharpHelper.Resize(source, 40, 40, allowEnlarge: false);

        result.Width.ShouldBe(4);
        result.Height.ShouldBe(2);
    }

    /// <summary>
    /// 测试用例：验证越界裁剪会按边界收缩。
    /// </summary>
    [Fact]
    public void Crop_OutOfBoundsRectangle_ClampsToImageBounds()
    {
        using var source = CreatePatternImage(10, 8);
        using var result = SkiaSharpHelper.Crop(source, new SKRectI(8, 6, 18, 16));

        result.Width.ShouldBe(2);
        result.Height.ShouldBe(2);
    }

    /// <summary>
    /// 测试用例：验证旋转 90 度后宽高互换。
    /// </summary>
    [Fact]
    public void Rotate_90_SwapsDimensions()
    {
        using var source = CreatePatternImage(10, 4);
        using var result = SkiaSharpHelper.Rotate(source, 90);

        result.Width.ShouldBe(4);
        result.Height.ShouldBe(10);
    }

    /// <summary>
    /// 测试用例：验证水平翻转会镜像像素位置。
    /// </summary>
    [Fact]
    public void FlipHorizontal_MirrorsPixels()
    {
        using var bitmap = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(255, 0, 0, 255));
        bitmap.SetPixel(1, 0, new SKColor(0, 0, 255, 255));
        using var source = SKImage.FromBitmap(bitmap);

        using var result = SkiaSharpHelper.FlipHorizontal(source);
        using var resultBitmap = SKBitmap.FromImage(result);

        resultBitmap.GetPixel(0, 0).Blue.ShouldBe((byte)255);
        resultBitmap.GetPixel(1, 0).Red.ShouldBe((byte)255);
    }

    /// <summary>
    /// 测试用例：验证 JPEG 质量重载会影响输出大小。
    /// </summary>
    [Fact]
    public void ToBytes_JpegQualityOverload_LowQualitySmallerThanHighQuality()
    {
        using var source = CreateNoiseImage(128, 128);

        var highQuality = SkiaSharpHelper.ToBytes(source, SKEncodedImageFormat.Jpeg, 100);
        var lowQuality = SkiaSharpHelper.ToBytes(source, SKEncodedImageFormat.Jpeg, 25);

        lowQuality.Length.ShouldBeLessThan(highQuality.Length);
    }

    /// <summary>
    /// 测试用例：验证近似匹配像素会被替换为目标颜色。
    /// </summary>
    [Fact]
    public void ReplaceColor_MatchingPixel_ReplacesColor()
    {
        using var bitmap = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(255, 0, 0, 255));
        using var source = SKImage.FromBitmap(bitmap);

        using var result = SkiaSharpHelper.ReplaceColor(source, new SKColor(255, 0, 0, 255), new SKColor(0, 0, 255, 255));
        using var resultBitmap = SKBitmap.FromImage(result);

        resultBitmap.GetPixel(0, 0).Red.ShouldBe((byte)0);
        resultBitmap.GetPixel(0, 0).Blue.ShouldBe((byte)255);
    }

    /// <summary>
    /// 测试用例：验证黑白效果保留 Alpha。
    /// </summary>
    [Fact]
    public void SetBlackWhiteEffect_PreservesAlpha()
    {
        using var bitmap = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(255, 255, 255, 128));
        using var source = SKImage.FromBitmap(bitmap);

        using var result = SkiaSharpHelper.SetBlackWhiteEffect(source, 0.5f);
        using var resultBitmap = SKBitmap.FromImage(result);

        resultBitmap.GetPixel(0, 0).Red.ShouldBe((byte)255);
        resultBitmap.GetPixel(0, 0).Alpha.ShouldBe((byte)128);
    }

    /// <summary>
    /// 测试用例：验证双色调效果在黑色源像素下返回颜色A。
    /// </summary>
    [Fact]
    public void SetDuotoneEffect_BlackPixel_ReturnsColorA()
    {
        using var bitmap = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(0, 0, 0, 255));
        using var source = SKImage.FromBitmap(bitmap);

        using var result = SkiaSharpHelper.SetDuotoneEffect(source, new SKColor(255, 0, 0, 255), new SKColor(0, 0, 255, 255));
        using var resultBitmap = SKBitmap.FromImage(result);

        resultBitmap.GetPixel(0, 0).Red.ShouldBe((byte)255);
        resultBitmap.GetPixel(0, 0).Blue.ShouldBe((byte)0);
    }

    /// <summary>
    /// 测试用例：验证亮度 0 会得到全黑图像。
    /// </summary>
    [Fact]
    public void SetBrightness_AmountZero_ReturnsBlackPixels()
    {
        using var bitmap = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(20, 40, 60, 255));
        using var source = SKImage.FromBitmap(bitmap);

        using var result = SkiaSharpHelper.SetBrightness(source, 0f);
        using var resultBitmap = SKBitmap.FromImage(result);

        resultBitmap.GetPixel(0, 0).Red.ShouldBe((byte)0);
        resultBitmap.GetPixel(0, 0).Green.ShouldBe((byte)0);
        resultBitmap.GetPixel(0, 0).Blue.ShouldBe((byte)0);
    }

    /// <summary>
    /// 测试用例：验证对比度 0 会得到中灰图像。
    /// </summary>
    [Fact]
    public void SetContrast_AmountZero_ReturnsMidGrayPixels()
    {
        using var bitmap = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(255, 0, 0, 255));
        using var source = SKImage.FromBitmap(bitmap);

        using var result = SkiaSharpHelper.SetContrast(source, 0f);
        using var resultBitmap = SKBitmap.FromImage(result);

        resultBitmap.GetPixel(0, 0).Red.ShouldBe((byte)128);
        resultBitmap.GetPixel(0, 0).Green.ShouldBe((byte)128);
        resultBitmap.GetPixel(0, 0).Blue.ShouldBe((byte)128);
    }

    /// <summary>
    /// 测试用例：验证饱和度 0 会得到灰度像素。
    /// </summary>
    [Fact]
    public void SetSaturation_AmountZero_ReturnsGrayscalePixels()
    {
        using var bitmap = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(200, 100, 50, 255));
        using var source = SKImage.FromBitmap(bitmap);

        using var result = SkiaSharpHelper.SetSaturation(source, 0f);
        using var resultBitmap = SKBitmap.FromImage(result);
        var pixel = resultBitmap.GetPixel(0, 0);

        pixel.Red.ShouldBe(pixel.Green);
        pixel.Green.ShouldBe(pixel.Blue);
    }

    /// <summary>
    /// 测试用例：验证应用单位矩阵不会改变像素。
    /// </summary>
    [Fact]
    public void ApplyColorMatrix_IdentityMatrix_ReturnsUnchangedPixels()
    {
        using var bitmap = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(12, 34, 56, 78));
        using var source = SKImage.FromBitmap(bitmap);

        var matrix = new float[,]
        {
            { 1, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0 },
            { 0, 0, 1, 0, 0 },
            { 0, 0, 0, 1, 0 },
            { 0, 0, 0, 0, 1 }
        };

        using var result = SkiaSharpHelper.ApplyColorMatrix(source, matrix);
        using var resultBitmap = SKBitmap.FromImage(result);
        var pixel = resultBitmap.GetPixel(0, 0);

        pixel.Red.ShouldBe((byte)12);
        pixel.Green.ShouldBe((byte)34);
        pixel.Blue.ShouldBe((byte)56);
        pixel.Alpha.ShouldBe((byte)78);
    }

    /// <summary>
    /// 测试用例：验证颜色 API 的边界参数会抛出异常。
    /// </summary>
    [Fact]
    public void ColorApis_InvalidParameters_Throw()
    {
        using var source = CreateSampleImage();

        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.ReplaceColor(source, new SKColor(255, 0, 0, 255), new SKColor(0, 0, 255, 255), -1))
            .ParamName.ShouldBe("accuracy");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.SetBlackWhiteEffect(source, 1.1f))
            .ParamName.ShouldBe("threshold");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.SetBrightness(source, -0.1f))
            .ParamName.ShouldBe("amount");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.SetContrast(source, -0.1f))
            .ParamName.ShouldBe("amount");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.SetSaturation(source, -0.1f))
            .ParamName.ShouldBe("amount");
        Should.Throw<ArgumentException>(() => SkiaSharpHelper.ApplyColorMatrix(source, new float[4, 4]))
            .ParamName.ShouldBe("matrix");
    }

    /// <summary>
    /// 测试用例：验证柔化边缘会降低边缘 Alpha，同时保留中心区域。
    /// </summary>
    [Fact]
    public void SoftEdge_PositiveRadius_SoftensBorderAlpha()
    {
        using var source = CreateSolidImage(12, 12, new SKColor(255, 255, 255, 255));

        using var result = SkiaSharpHelper.SoftEdge(source, 4f);
        using var resultBitmap = SKBitmap.FromImage(result);

        resultBitmap.GetPixel(0, 0).Alpha.ShouldBeLessThan((byte)255);
        resultBitmap.GetPixel(6, 6).Alpha.ShouldBeGreaterThan(resultBitmap.GetPixel(0, 0).Alpha);
        resultBitmap.GetPixel(6, 6).Alpha.ShouldBeGreaterThan((byte)200);
    }

    /// <summary>
    /// 测试用例：验证半径为 0 时返回内容不变的新图像。
    /// </summary>
    [Fact]
    public void SoftEdge_ZeroRadius_ReturnsUnchangedClone()
    {
        using var bitmap = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(10, 20, 30, 128));
        bitmap.SetPixel(1, 0, new SKColor(40, 50, 60, 255));
        using var source = SKImage.FromBitmap(bitmap);

        using var result = SkiaSharpHelper.SoftEdge(source, 0f);
        using var resultBitmap = SKBitmap.FromImage(result);

        ReferenceEquals(source, result).ShouldBeFalse();
        resultBitmap.GetPixel(0, 0).ShouldBe(new SKColor(10, 20, 30, 128));
        resultBitmap.GetPixel(1, 0).ShouldBe(new SKColor(40, 50, 60, 255));
    }

    /// <summary>
    /// 测试用例：验证负半径会抛出异常。
    /// </summary>
    [Fact]
    public void SoftEdge_NegativeRadius_ThrowsArgumentOutOfRangeException()
    {
        using var source = CreateSampleImage();

        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.SoftEdge(source, -1f))
            .ParamName.ShouldBe("radius");
    }

    /// <summary>
    /// 测试用例：验证柔化卷积会把亮点扩散到邻近像素。
    /// </summary>
    [Fact]
    public void Soften_SingleBrightPixel_SpreadsToNeighbors()
    {
        using var source = CreateSolidImage(3, 3, new SKColor(0, 0, 0, 255));
        using var sourceBitmap = SKBitmap.FromImage(source);
        sourceBitmap.SetPixel(1, 1, new SKColor(255, 255, 255, 255));
        using var prepared = SKImage.FromBitmap(sourceBitmap);

        using var result = SkiaSharpHelper.Soften(prepared);
        using var resultBitmap = SKBitmap.FromImage(result);
        using var originalBitmap = SKBitmap.FromImage(prepared);

        ReferenceEquals(prepared, result).ShouldBeFalse();
        originalBitmap.GetPixel(1, 1).Red.ShouldBe((byte)255);
        resultBitmap.GetPixel(1, 1).Red.ShouldBeLessThan((byte)255);
        resultBitmap.GetPixel(1, 1).Red.ShouldBeGreaterThan(resultBitmap.GetPixel(0, 1).Red);
        resultBitmap.GetPixel(0, 1).Red.ShouldBeGreaterThan((byte)0);
    }

    /// <summary>
    /// 测试用例：验证锐化卷积对纯色图像保持不变。
    /// </summary>
    [Fact]
    public void Sharpen_UniformImage_PreservesPixels()
    {
        using var source = CreateSolidImage(3, 3, new SKColor(50, 60, 70, 128));

        using var result = SkiaSharpHelper.Sharpen(source);
        using var resultBitmap = SKBitmap.FromImage(result);

        resultBitmap.GetPixel(1, 1).ShouldBe(new SKColor(50, 60, 70, 128));
    }

    /// <summary>
    /// 测试用例：验证浮雕卷积对纯色图像产生中灰效果。
    /// </summary>
    [Fact]
    public void Emboss_UniformImage_ReturnsMidGrayLikePixels()
    {
        using var source = CreateSolidImage(3, 3, new SKColor(50, 60, 70, 128));

        using var result = SkiaSharpHelper.Emboss(source);
        using var resultBitmap = SKBitmap.FromImage(result);
        var pixel = resultBitmap.GetPixel(1, 1);

        pixel.Red.ShouldBe((byte)128);
        pixel.Green.ShouldBe((byte)128);
        pixel.Blue.ShouldBe((byte)128);
        pixel.Alpha.ShouldBe((byte)128);
    }

    /// <summary>
    /// 测试用例：验证雾化效果按局部前向采样生成新图像。
    /// </summary>
    [Fact]
    public void Atomizing_PatternImage_SamplesFromForwardNeighborhood()
    {
        using var source = CreatePatternImage(40, 40);
        using var sourceBitmap = SKBitmap.FromImage(source);

        using var result = SkiaSharpHelper.Atomizing(source);
        using var resultBitmap = SKBitmap.FromImage(result);

        const int blockSize = 19;
        var offset00 = ((0 + 1) * 13 + (0 + 1) * 7 + source.Width * 5 + source.Height * 3) % blockSize;
        var expectedX00 = Math.Min(source.Width - 1, offset00);
        var expectedY00 = Math.Min(source.Height - 1, offset00);
        var offset102 = ((10 + 1) * 13 + (2 + 1) * 7 + source.Width * 5 + source.Height * 3) % blockSize;
        var expectedX102 = Math.Min(source.Width - 1, 10 + offset102);
        var expectedY102 = Math.Min(source.Height - 1, 2 + offset102);

        ReferenceEquals(source, result).ShouldBeFalse();
        resultBitmap.GetPixel(0, 0).ShouldBe(sourceBitmap.GetPixel(expectedX00, expectedY00));
        resultBitmap.GetPixel(10, 2).ShouldBe(sourceBitmap.GetPixel(expectedX102, expectedY102));
        resultBitmap.GetPixel(0, 0).ShouldNotBe(sourceBitmap.GetPixel(0, 0));
    }

    /// <summary>
    /// 测试用例：验证雾化效果 API 在空图片下抛出异常。
    /// </summary>
    [Fact]
    public void Atomizing_NullImage_Throw()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.Atomizing(null!))
            .ParamName.ShouldBe("image");
    }

    /// <summary>
    /// 测试用例：验证图片水印会按目标区域缩放并覆盖原图。
    /// </summary>
    [Fact]
    public void AddImageWatermark_FullOpacity_AppliesScaledOverlay()
    {
        using var source = CreateSolidImage(4, 4, new SKColor(255, 255, 255, 255));
        using var watermark = CreateSolidImage(1, 1, new SKColor(255, 0, 0, 255));

        using var result = SkiaSharpHelper.AddImageWatermark(source, watermark, new SKRectI(1, 1, 3, 3));
        using var resultBitmap = SKBitmap.FromImage(result);
        using var sourceBitmap = SKBitmap.FromImage(source);

        ReferenceEquals(source, result).ShouldBeFalse();
        resultBitmap.GetPixel(0, 0).ShouldBe(new SKColor(255, 255, 255, 255));
        resultBitmap.GetPixel(1, 1).ShouldBe(new SKColor(255, 0, 0, 255));
        resultBitmap.GetPixel(2, 2).ShouldBe(new SKColor(255, 0, 0, 255));
        sourceBitmap.GetPixel(1, 1).ShouldBe(new SKColor(255, 255, 255, 255));
    }

    /// <summary>
    /// 测试用例：验证透明度为 0 时返回内容不变的新图像。
    /// </summary>
    [Fact]
    public void AddImageWatermark_ZeroOpacity_ReturnsUnchangedClone()
    {
        using var source = CreateSolidImage(4, 4, new SKColor(255, 255, 255, 255));
        using var watermark = CreateSolidImage(1, 1, new SKColor(255, 0, 0, 255));

        using var result = SkiaSharpHelper.AddImageWatermark(source, watermark, new SKRectI(1, 1, 3, 3), 0f);
        using var resultBitmap = SKBitmap.FromImage(result);

        ReferenceEquals(source, result).ShouldBeFalse();
        resultBitmap.GetPixel(1, 1).ShouldBe(new SKColor(255, 255, 255, 255));
        resultBitmap.GetPixel(2, 2).ShouldBe(new SKColor(255, 255, 255, 255));
    }

    /// <summary>
    /// 测试用例：验证图片水印 API 的异常参数。
    /// </summary>
    [Fact]
    public void AddImageWatermark_InvalidArguments_Throw()
    {
        using var source = CreateSolidImage(4, 4, new SKColor(255, 255, 255, 255));
        using var watermark = CreateSolidImage(1, 1, new SKColor(255, 0, 0, 255));

        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.AddImageWatermark(null!, watermark, new SKRectI(0, 0, 1, 1)))
            .ParamName.ShouldBe("image");
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.AddImageWatermark(source, null!, new SKRectI(0, 0, 1, 1)))
            .ParamName.ShouldBe("watermark");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.AddImageWatermark(source, watermark, new SKRectI(0, 0, 0, 1)))
            .ParamName.ShouldBe("targetRectangle");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.AddImageWatermark(source, watermark, new SKRectI(0, 0, 1, 1), 1.1f))
            .ParamName.ShouldBe("opacity");
    }

    /// <summary>
    /// 测试用例：验证文字水印会在右下区域绘制文本。
    /// </summary>
    [Fact]
    public void AddTextWatermark_FullOpacity_DrawsInBottomRightRegion()
    {
        var background = new SKColor(255, 255, 255, 255);
        using var source = CreateSolidImage(200, 80, background);

        using var result = SkiaSharpHelper.AddTextWatermark(source, "WM", fontSize: 20, opacity: 1f);
        using var resultBitmap = SKBitmap.FromImage(result);
        using var sourceBitmap = SKBitmap.FromImage(source);

        ReferenceEquals(source, result).ShouldBeFalse();
        resultBitmap.GetPixel(0, 0).ShouldBe(background);
        sourceBitmap.GetPixel(150, 60).ShouldBe(background);
        ContainsNonBackgroundPixel(resultBitmap, background, resultBitmap.Width / 2, resultBitmap.Height / 2, resultBitmap.Width, resultBitmap.Height).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证透明度为 0 时文字水印不改变图像内容。
    /// </summary>
    [Fact]
    public void AddTextWatermark_ZeroOpacity_ReturnsUnchangedClone()
    {
        var background = new SKColor(255, 255, 255, 255);
        using var source = CreateSolidImage(200, 80, background);

        using var result = SkiaSharpHelper.AddTextWatermark(source, "WM", fontSize: 20, opacity: 0f);
        using var resultBitmap = SKBitmap.FromImage(result);

        ReferenceEquals(source, result).ShouldBeFalse();
        ContainsNonBackgroundPixel(resultBitmap, background).ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：验证文字水印 API 的异常参数。
    /// </summary>
    [Fact]
    public void AddTextWatermark_InvalidArguments_Throw()
    {
        using var source = CreateSolidImage(200, 80, new SKColor(255, 255, 255, 255));

        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.AddTextWatermark(null!, "WM"))
            .ParamName.ShouldBe("image");
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.AddTextWatermark(source, " "))
            .ParamName.ShouldBe("text");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.AddTextWatermark(source, "WM", fontSize: 0))
            .ParamName.ShouldBe("fontSize");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.AddTextWatermark(source, "WM", opacity: 1.1f))
            .ParamName.ShouldBe("opacity");
    }

    /// <summary>
    /// 测试用例：验证验证码文本 API 返回去歧义数字字母串。
    /// </summary>
    [Fact]
    public void GetCaptchaCode_ValidLength_ReturnsExpectedShape()
    {
        var code = SkiaSharpHelper.GetCaptchaCode(6);

        code.Length.ShouldBe(6);
        Regex.IsMatch(code, "^[23456789A-HJ-NP-Za-km-z]+$").ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证验证码文本 API 在非法长度下抛出异常。
    /// </summary>
    [Fact]
    public void GetCaptchaCode_InvalidLength_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.GetCaptchaCode(0))
            .ParamName.ShouldBe("length");
    }

    /// <summary>
    /// 测试用例：验证按长度创建验证码图片会返回指定长度的数字字母验证码。
    /// </summary>
    [Fact]
    public void CreateCaptchaImage_WithLengthOutCode_ReturnsExpectedCodeShapeAndImageSize()
    {
        using var image = SkiaSharpHelper.CreateCaptchaImage(6, out var code);
        using var bitmap = SKBitmap.FromImage(image);
        var background = new SKColor(240, 240, 240, 255);

        code.Length.ShouldBe(6);
        Regex.IsMatch(code, "^[23456789A-HJ-NP-Za-km-z]+$").ShouldBeTrue();
        image.Width.ShouldBe(140);
        image.Height.ShouldBe(30);
        ContainsNonBackgroundPixel(bitmap, background).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证按长度创建验证码图片在非法长度下抛出异常。
    /// </summary>
    [Fact]
    public void CreateCaptchaImage_InvalidLength_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.CreateCaptchaImage(0, out _))
            .ParamName.ShouldBe("length");
    }

    /// <summary>
    /// 测试用例：验证验证码图片会按文本长度生成预期尺寸并包含非背景像素。
    /// </summary>
    [Theory]
    [InlineData("A", 40, 30)]
    [InlineData("AB12", 100, 30)]
    public void CreateCaptchaImage_ValidCode_ReturnsExpectedSize(string code, int expectedWidth, int expectedHeight)
    {
        using var image = SkiaSharpHelper.CreateCaptchaImage(code);
        using var bitmap = SKBitmap.FromImage(image);
        var background = new SKColor(240, 240, 240, 255);

        image.Width.ShouldBe(expectedWidth);
        image.Height.ShouldBe(expectedHeight);
        ContainsNonBackgroundPixel(bitmap, background).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证验证码图片 API 在空白文本下抛出异常。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateCaptchaImage_BlankCode_ThrowsArgumentNullException(string code)
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.CreateCaptchaImage(code))
            .ParamName.ShouldBe("code");
    }

    /// <summary>
    /// 测试用例：验证卷积效果 API 在空图片下抛出异常。
    /// </summary>
    [Fact]
    public void ConvolutionApis_NullImage_Throw()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.Soften(null!))
            .ParamName.ShouldBe("image");
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.Sharpen(null!))
            .ParamName.ShouldBe("image");
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.Emboss(null!))
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

    private static SKImage CreatePatternImage(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                bitmap.SetPixel(x, y, new SKColor((byte)(x * 11 % 255), (byte)(y * 19 % 255), (byte)((x + y) * 7 % 255), 255));
            }
        }
        return SKImage.FromBitmap(bitmap);
    }

    private static SKImage CreateNoiseImage(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var random = new Random(42);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                bitmap.SetPixel(x, y, new SKColor((byte)random.Next(0, 256), (byte)random.Next(0, 256), (byte)random.Next(0, 256), 255));
            }
        }
        return SKImage.FromBitmap(bitmap);
    }

    private static SKImage CreateSolidImage(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
                bitmap.SetPixel(x, y, color);
        }

        return SKImage.FromBitmap(bitmap);
    }

    private static bool ContainsNonBackgroundPixel(SKBitmap bitmap, SKColor background, int startX = 0, int startY = 0, int? endX = null, int? endY = null)
    {
        var actualEndX = Math.Min(endX ?? bitmap.Width, bitmap.Width);
        var actualEndY = Math.Min(endY ?? bitmap.Height, bitmap.Height);

        for (var x = Math.Max(0, startX); x < actualEndX; x++)
        {
            for (var y = Math.Max(0, startY); y < actualEndY; y++)
            {
                if (bitmap.GetPixel(x, y) != background)
                    return true;
            }
        }

        return false;
    }
}

