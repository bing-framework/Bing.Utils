using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.RegularExpressions;
namespace Bing.Drawing;
/// <summary>
/// 测试类：覆盖 `ImageSharpHelper` 相关行为。
/// </summary>
public class ImageSharpHelperTest
{
    /// <summary>
    /// 测试用例：验证验证码文本 API 返回去歧义数字字母串。
    /// </summary>
    [Fact]
    public void GetCaptchaCode_ValidLength_ReturnsExpectedShape()
    {
        var code = ImageSharpHelper.GetCaptchaCode(6);

        code.Length.ShouldBe(6);
        Regex.IsMatch(code, "^[23456789A-HJ-NP-Za-km-z]+$").ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证验证码文本 API 在非法长度下抛出异常。
    /// </summary>
    [Fact]
    public void GetCaptchaCode_InvalidLength_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.GetCaptchaCode(0))
            .ParamName.ShouldBe("length");
    }

    /// <summary>
    /// 测试用例：验证 `ToBytes` 在 `And_FromBytes` 场景下，结果为 `RoundTrip`。
    /// </summary>
    [Fact]
    public void ToBytes_And_FromBytes_RoundTrip()
    {
        using var source = CreateSampleImage();
        var bytes = ImageSharpHelper.ToBytes(source, PngFormat.Instance);
        bytes.Length.ShouldBeGreaterThan(0);
        using var restored = ImageSharpHelper.FromBytes(bytes);
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
        var base64 = ImageSharpHelper.ToBase64String(source, PngFormat.Instance);
        base64.ShouldNotBeNullOrWhiteSpace();
        using var restored = ImageSharpHelper.FromBase64String(base64);
        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(2);
        restored.Height.ShouldBe(2);
    }
    /// <summary>
    /// 测试用例：验证 `ToDataUrl` 在 `And_FromDataUrl` 场景下，结果为 `RoundTrip`。
    /// </summary>
    [Fact]
    public void ToDataUrl_And_FromDataUrl_RoundTrip()
    {
        using var source = CreateSampleImage();
        var dataUrl = ImageSharpHelper.ToDataUrl(source, PngFormat.Instance);
        dataUrl.ShouldStartWith("data:image/png;base64,");
        using var restored = ImageSharpHelper.FromDataUrl(dataUrl);
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
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.FromBytes(null!))
            .ParamName.ShouldBe("bytes");
    }
    /// <summary>
    /// 测试用例：验证 `ToBytes` 在 `NullImage` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void ToBytes_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToBytes(null!))
            .ParamName.ShouldBe("image");
    }
    /// <summary>
    /// 测试用例：验证 `ToBase64String` 在 `NullImage` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void ToBase64String_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToBase64String(null!))
            .ParamName.ShouldBe("image");
    }
    /// <summary>
    /// 测试用例：验证 `ToDataUrl` 在 `NullImage` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void ToDataUrl_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToDataUrl(null!))
            .ParamName.ShouldBe("image");
    }
    /// <summary>
    /// 测试用例：验证 `FromBase64String` 在 `InvalidValue` 场景下，结果为 `ReturnsNull`。
    /// </summary>
    [Fact]
    public void FromBase64String_InvalidValue_ReturnsNull()
    {
        var restored = ImageSharpHelper.FromBase64String("not-base64");
        restored.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `FromDataUrl` 在 `InvalidFormat` 场景下，结果为 `ReturnsNull`。
    /// </summary>
    [Fact]
    public void FromDataUrl_InvalidFormat_ReturnsNull()
    {
        var restored = ImageSharpHelper.FromDataUrl("invalid-data-url");
        restored.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `FromFile` 在 `And_FromFileGeneric` 场景下，结果为 `LoadExpectedImage`。
    /// </summary>
    [Fact]
    public void FromFile_And_FromFileGeneric_LoadExpectedImage()
    {
        using var source = CreateSampleImage();
        var bytes = ImageSharpHelper.ToBytes(source, PngFormat.Instance);
        var tempPath = Path.Combine(Path.GetTempPath(), $"bing-utils-imagesharp-{Guid.NewGuid():N}.png");
        try
        {
            File.WriteAllBytes(tempPath, bytes);
            using var restored = ImageSharpHelper.FromFile(tempPath);
            restored.ShouldNotBeNull();
            restored!.Width.ShouldBe(2);
            restored.Height.ShouldBe(2);
            using var restoredGeneric = ImageSharpHelper.FromFile<Rgba32>(tempPath);
            restoredGeneric.ShouldNotBeNull();
            restoredGeneric!.Width.ShouldBe(2);
            restoredGeneric.Height.ShouldBe(2);
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
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.SetOpacity(source, -0.1f))
            .ParamName.ShouldBe("opacity");
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.SetOpacity(source, 1.1f))
            .ParamName.ShouldBe("opacity");
    }
    /// <summary>
    /// 测试用例：验证 `SetOpacity` 在 `ValidOpacity` 场景下，结果为 `ReturnsNewImageWithLowerAlpha`。
    /// </summary>
    [Fact]
    public void SetOpacity_ValidOpacity_ReturnsNewImageWithLowerAlpha()
    {
        using var source = CreateSampleImage();
        using var result = ImageSharpHelper.SetOpacity(source, 0.5f);
        result.ShouldNotBeNull();
        ReferenceEquals(source, result).ShouldBeFalse();
        result.Width.ShouldBe(source.Width);
        result.Height.ShouldBe(source.Height);
        using var resultRgba = result.CloneAs<Rgba32>();
        resultRgba[0, 0].A.ShouldBeLessThan((byte)255);
    }
    /// <summary>
    /// 测试用例：验证 `SetOpacity` 在 `NullImage` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void SetOpacity_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.SetOpacity(null!, 0.5f))
            .ParamName.ShouldBe("image");
    }

    /// <summary>
    /// 测试用例：验证图片信息 API 返回正确的 MIME 和扩展名。
    /// </summary>
    [Fact]
    public void GetMimeType_And_GetImageExtension_PngLoadedImage_ReturnExpectedValues()
    {
        using var source = CreateSampleImage();
        var bytes = ImageSharpHelper.ToBytes(source, PngFormat.Instance);
        using var restored = ImageSharpHelper.FromBytes(bytes);

        restored.ShouldNotBeNull();
        ImageSharpHelper.GetMimeType(restored!).ShouldBe("image/png");
        ImageSharpHelper.GetImageExtension(restored).ShouldBe("png");
    }

    /// <summary>
    /// 测试用例：验证透明图片会被识别为包含 Alpha。
    /// </summary>
    [Fact]
    public void HasAlpha_TransparentImage_ReturnsTrue()
    {
        using var image = new Image<Rgba32>(2, 2);
        image[0, 0] = new Rgba32(255, 0, 0, 0);

        ImageSharpHelper.HasAlpha(image).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证保持宽高比缩放时返回新图像且尺寸符合预期。
    /// </summary>
    [Fact]
    public void Resize_KeepAspectRatio_ReturnsExpectedSize()
    {
        using var source = CreatePatternImage(40, 20);
        using var result = ImageSharpHelper.Resize(source, 10, 10);

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
        using var result = ImageSharpHelper.Resize(source, 40, 40, allowEnlarge: false);

        ReferenceEquals(source, result).ShouldBeFalse();
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
        using var result = ImageSharpHelper.Crop(source, new Rectangle(8, 6, 10, 10));

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
        using var result = ImageSharpHelper.Rotate(source, 90);

        result.Width.ShouldBe(4);
        result.Height.ShouldBe(10);
    }

    /// <summary>
    /// 测试用例：验证水平翻转会镜像像素位置。
    /// </summary>
    [Fact]
    public void FlipHorizontal_MirrorsPixels()
    {
        using var source = new Image<Rgba32>(2, 1);
        source[0, 0] = new Rgba32(255, 0, 0, 255);
        source[1, 0] = new Rgba32(0, 0, 255, 255);

        using var result = ImageSharpHelper.FlipHorizontal(source);
        using var rgba = result.CloneAs<Rgba32>();

        rgba[0, 0].B.ShouldBe((byte)255);
        rgba[1, 0].R.ShouldBe((byte)255);
    }

    /// <summary>
    /// 测试用例：验证 JPEG 质量重载会影响输出大小。
    /// </summary>
    [Fact]
    public void ToBytes_JpegQualityOverload_LowQualitySmallerThanHighQuality()
    {
        using var source = CreateNoiseImage(128, 128);

        var highQuality = ImageSharpHelper.ToBytes(source, JpegFormat.Instance, 100);
        var lowQuality = ImageSharpHelper.ToBytes(source, JpegFormat.Instance, 25);

        lowQuality.Length.ShouldBeLessThan(highQuality.Length);
    }

    /// <summary>
    /// 测试用例：验证近似匹配像素会被替换为目标颜色。
    /// </summary>
    [Fact]
    public void ReplaceColor_MatchingPixel_ReplacesColor()
    {
        using var source = new Image<Rgba32>(1, 1);
        source[0, 0] = new Rgba32(255, 0, 0, 255);

        using var result = ImageSharpHelper.ReplaceColor(source, Color.FromRgb(255, 0, 0), Color.FromRgb(0, 0, 255));
        using var rgba = result.CloneAs<Rgba32>();

        rgba[0, 0].R.ShouldBe((byte)0);
        rgba[0, 0].B.ShouldBe((byte)255);
    }

    /// <summary>
    /// 测试用例：验证黑白效果保留 Alpha。
    /// </summary>
    [Fact]
    public void SetBlackWhiteEffect_PreservesAlpha()
    {
        using var source = new Image<Rgba32>(1, 1);
        source[0, 0] = new Rgba32(255, 255, 255, 128);

        using var result = ImageSharpHelper.SetBlackWhiteEffect(source, 0.5f);
        using var rgba = result.CloneAs<Rgba32>();

        rgba[0, 0].R.ShouldBe((byte)255);
        rgba[0, 0].A.ShouldBe((byte)128);
    }

    /// <summary>
    /// 测试用例：验证双色调效果在黑色源像素下返回颜色A。
    /// </summary>
    [Fact]
    public void SetDuotoneEffect_BlackPixel_ReturnsColorA()
    {
        using var source = new Image<Rgba32>(1, 1);
        source[0, 0] = new Rgba32(0, 0, 0, 255);

        using var result = ImageSharpHelper.SetDuotoneEffect(source, Color.FromRgb(255, 0, 0), Color.FromRgb(0, 0, 255));
        using var rgba = result.CloneAs<Rgba32>();

        rgba[0, 0].R.ShouldBe((byte)255);
        rgba[0, 0].B.ShouldBe((byte)0);
    }

    /// <summary>
    /// 测试用例：验证亮度 0 会得到全黑图像。
    /// </summary>
    [Fact]
    public void SetBrightness_AmountZero_ReturnsBlackPixels()
    {
        using var source = new Image<Rgba32>(1, 1);
        source[0, 0] = new Rgba32(20, 40, 60, 255);

        using var result = ImageSharpHelper.SetBrightness(source, 0f);
        using var rgba = result.CloneAs<Rgba32>();

        rgba[0, 0].R.ShouldBe((byte)0);
        rgba[0, 0].G.ShouldBe((byte)0);
        rgba[0, 0].B.ShouldBe((byte)0);
        rgba[0, 0].A.ShouldBe((byte)255);
    }

    /// <summary>
    /// 测试用例：验证对比度 0 会得到中灰图像。
    /// </summary>
    [Fact]
    public void SetContrast_AmountZero_ReturnsMidGrayPixels()
    {
        using var source = new Image<Rgba32>(1, 1);
        source[0, 0] = new Rgba32(255, 0, 0, 255);

        using var result = ImageSharpHelper.SetContrast(source, 0f);
        using var rgba = result.CloneAs<Rgba32>();

        rgba[0, 0].R.ShouldBe((byte)128);
        rgba[0, 0].G.ShouldBe((byte)128);
        rgba[0, 0].B.ShouldBe((byte)128);
    }

    /// <summary>
    /// 测试用例：验证饱和度 0 会得到灰度像素。
    /// </summary>
    [Fact]
    public void SetSaturation_AmountZero_ReturnsGrayscalePixels()
    {
        using var source = new Image<Rgba32>(1, 1);
        source[0, 0] = new Rgba32(200, 100, 50, 255);

        using var result = ImageSharpHelper.SetSaturation(source, 0f);
        using var rgba = result.CloneAs<Rgba32>();

        rgba[0, 0].R.ShouldBe(rgba[0, 0].G);
        rgba[0, 0].G.ShouldBe(rgba[0, 0].B);
    }

    /// <summary>
    /// 测试用例：验证应用单位矩阵不会改变像素。
    /// </summary>
    [Fact]
    public void ApplyColorMatrix_IdentityMatrix_ReturnsUnchangedPixels()
    {
        using var source = new Image<Rgba32>(1, 1);
        source[0, 0] = new Rgba32(12, 34, 56, 78);

        var matrix = new float[,]
        {
            { 1, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0 },
            { 0, 0, 1, 0, 0 },
            { 0, 0, 0, 1, 0 },
            { 0, 0, 0, 0, 1 }
        };

        using var result = ImageSharpHelper.ApplyColorMatrix(source, matrix);
        using var rgba = result.CloneAs<Rgba32>();

        rgba[0, 0].R.ShouldBe((byte)12);
        rgba[0, 0].G.ShouldBe((byte)34);
        rgba[0, 0].B.ShouldBe((byte)56);
        rgba[0, 0].A.ShouldBe((byte)78);
    }

    /// <summary>
    /// 测试用例：验证颜色 API 的边界参数会抛出异常。
    /// </summary>
    [Fact]
    public void ColorApis_InvalidParameters_Throw()
    {
        using var source = CreateSampleImage();

        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.ReplaceColor(source, Color.FromRgb(255, 0, 0), Color.FromRgb(0, 0, 255), -1))
            .ParamName.ShouldBe("accuracy");
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.SetBlackWhiteEffect(source, 1.1f))
            .ParamName.ShouldBe("threshold");
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.SetBrightness(source, -0.1f))
            .ParamName.ShouldBe("amount");
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.SetContrast(source, -0.1f))
            .ParamName.ShouldBe("amount");
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.SetSaturation(source, -0.1f))
            .ParamName.ShouldBe("amount");
        Should.Throw<ArgumentException>(() => ImageSharpHelper.ApplyColorMatrix(source, new float[4, 4]))
            .ParamName.ShouldBe("matrix");
    }

    /// <summary>
    /// 测试用例：验证柔化边缘会降低边缘 Alpha，同时保留中心区域。
    /// </summary>
    [Fact]
    public void SoftEdge_PositiveRadius_SoftensBorderAlpha()
    {
        using var source = CreateSolidImage(12, 12, new Rgba32(255, 255, 255, 255));

        using var result = ImageSharpHelper.SoftEdge(source, 4f);
        using var rgba = result.CloneAs<Rgba32>();

        rgba[0, 0].A.ShouldBeLessThan((byte)255);
        rgba[6, 6].A.ShouldBeGreaterThan(rgba[0, 0].A);
        rgba[6, 6].A.ShouldBeGreaterThan((byte)200);
    }

    /// <summary>
    /// 测试用例：验证半径为 0 时返回内容不变的新图像。
    /// </summary>
    [Fact]
    public void SoftEdge_ZeroRadius_ReturnsUnchangedClone()
    {
        using var source = new Image<Rgba32>(2, 1);
        source[0, 0] = new Rgba32(10, 20, 30, 128);
        source[1, 0] = new Rgba32(40, 50, 60, 255);

        using var result = ImageSharpHelper.SoftEdge(source, 0f);
        using var rgba = result.CloneAs<Rgba32>();

        ReferenceEquals(source, result).ShouldBeFalse();
        rgba[0, 0].ShouldBe(new Rgba32(10, 20, 30, 128));
        rgba[1, 0].ShouldBe(new Rgba32(40, 50, 60, 255));
    }

    /// <summary>
    /// 测试用例：验证负半径会抛出异常。
    /// </summary>
    [Fact]
    public void SoftEdge_NegativeRadius_ThrowsArgumentOutOfRangeException()
    {
        using var source = CreateSampleImage();

        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.SoftEdge(source, -1f))
            .ParamName.ShouldBe("radius");
    }

    /// <summary>
    /// 测试用例：验证柔化卷积会把亮点扩散到邻近像素。
    /// </summary>
    [Fact]
    public void Soften_SingleBrightPixel_SpreadsToNeighbors()
    {
        using var source = CreateSolidImage(3, 3, new Rgba32(0, 0, 0, 255));
        source[1, 1] = new Rgba32(255, 255, 255, 255);

        using var result = ImageSharpHelper.Soften(source);
        using var rgba = result.CloneAs<Rgba32>();

        ReferenceEquals(source, result).ShouldBeFalse();
        source[1, 1].R.ShouldBe((byte)255);
        rgba[1, 1].R.ShouldBeLessThan((byte)255);
        rgba[1, 1].R.ShouldBeGreaterThan(rgba[0, 1].R);
        rgba[0, 1].R.ShouldBeGreaterThan((byte)0);
    }

    /// <summary>
    /// 测试用例：验证锐化卷积对纯色图像保持不变。
    /// </summary>
    [Fact]
    public void Sharpen_UniformImage_PreservesPixels()
    {
        using var source = CreateSolidImage(3, 3, new Rgba32(50, 60, 70, 128));

        using var result = ImageSharpHelper.Sharpen(source);
        using var rgba = result.CloneAs<Rgba32>();

        rgba[1, 1].ShouldBe(new Rgba32(50, 60, 70, 128));
    }

    /// <summary>
    /// 测试用例：验证浮雕卷积对纯色图像产生中灰效果。
    /// </summary>
    [Fact]
    public void Emboss_UniformImage_ReturnsMidGrayLikePixels()
    {
        using var source = CreateSolidImage(3, 3, new Rgba32(50, 60, 70, 128));

        using var result = ImageSharpHelper.Emboss(source);
        using var rgba = result.CloneAs<Rgba32>();

        rgba[1, 1].R.ShouldBe((byte)128);
        rgba[1, 1].G.ShouldBe((byte)128);
        rgba[1, 1].B.ShouldBe((byte)128);
        rgba[1, 1].A.ShouldBe((byte)128);
    }

    /// <summary>
    /// 测试用例：验证雾化效果按局部前向采样生成新图像。
    /// </summary>
    [Fact]
    public void Atomizing_PatternImage_SamplesFromForwardNeighborhood()
    {
        using var source = CreatePatternImage(40, 40);

        using var result = ImageSharpHelper.Atomizing(source);
        using var rgba = result.CloneAs<Rgba32>();

        const int blockSize = 19;
        var offset00 = ((0 + 1) * 13 + (0 + 1) * 7 + source.Width * 5 + source.Height * 3) % blockSize;
        var expectedX00 = Math.Min(source.Width - 1, offset00);
        var expectedY00 = Math.Min(source.Height - 1, offset00);
        var offset102 = ((10 + 1) * 13 + (2 + 1) * 7 + source.Width * 5 + source.Height * 3) % blockSize;
        var expectedX102 = Math.Min(source.Width - 1, 10 + offset102);
        var expectedY102 = Math.Min(source.Height - 1, 2 + offset102);

        ReferenceEquals(source, result).ShouldBeFalse();
        rgba[0, 0].ShouldBe(source[expectedX00, expectedY00]);
        rgba[10, 2].ShouldBe(source[expectedX102, expectedY102]);
        rgba[0, 0].ShouldNotBe(source[0, 0]);
    }

    /// <summary>
    /// 测试用例：验证雾化效果 API 在空图片下抛出异常。
    /// </summary>
    [Fact]
    public void Atomizing_NullImage_Throw()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.Atomizing(null!))
            .ParamName.ShouldBe("image");
    }

    /// <summary>
    /// 测试用例：验证图片水印会按目标区域缩放并覆盖原图。
    /// </summary>
    [Fact]
    public void AddImageWatermark_FullOpacity_AppliesScaledOverlay()
    {
        using var source = CreateSolidImage(4, 4, new Rgba32(255, 255, 255, 255));
        using var watermark = CreateSolidImage(1, 1, new Rgba32(255, 0, 0, 255));

        using var result = ImageSharpHelper.AddImageWatermark(source, watermark, new Rectangle(1, 1, 2, 2));
        using var rgba = result.CloneAs<Rgba32>();

        ReferenceEquals(source, result).ShouldBeFalse();
        rgba[0, 0].ShouldBe(new Rgba32(255, 255, 255, 255));
        rgba[1, 1].ShouldBe(new Rgba32(255, 0, 0, 255));
        rgba[2, 2].ShouldBe(new Rgba32(255, 0, 0, 255));
        source[1, 1].ShouldBe(new Rgba32(255, 255, 255, 255));
    }

    /// <summary>
    /// 测试用例：验证透明度为 0 时返回内容不变的新图像。
    /// </summary>
    [Fact]
    public void AddImageWatermark_ZeroOpacity_ReturnsUnchangedClone()
    {
        using var source = CreateSolidImage(4, 4, new Rgba32(255, 255, 255, 255));
        using var watermark = CreateSolidImage(1, 1, new Rgba32(255, 0, 0, 255));

        using var result = ImageSharpHelper.AddImageWatermark(source, watermark, new Rectangle(1, 1, 2, 2), 0f);
        using var rgba = result.CloneAs<Rgba32>();

        ReferenceEquals(source, result).ShouldBeFalse();
        rgba[1, 1].ShouldBe(new Rgba32(255, 255, 255, 255));
        rgba[2, 2].ShouldBe(new Rgba32(255, 255, 255, 255));
    }

    /// <summary>
    /// 测试用例：验证图片水印 API 的异常参数。
    /// </summary>
    [Fact]
    public void AddImageWatermark_InvalidArguments_Throw()
    {
        using var source = CreateSolidImage(4, 4, new Rgba32(255, 255, 255, 255));
        using var watermark = CreateSolidImage(1, 1, new Rgba32(255, 0, 0, 255));

        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.AddImageWatermark(null!, watermark, new Rectangle(0, 0, 1, 1)))
            .ParamName.ShouldBe("image");
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.AddImageWatermark(source, null!, new Rectangle(0, 0, 1, 1)))
            .ParamName.ShouldBe("watermark");
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.AddImageWatermark(source, watermark, new Rectangle(0, 0, 0, 1)))
            .ParamName.ShouldBe("targetRectangle");
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.AddImageWatermark(source, watermark, new Rectangle(0, 0, 1, 1), -0.1f))
            .ParamName.ShouldBe("opacity");
    }

    /// <summary>
    /// 测试用例：验证卷积效果 API 在空图片下抛出异常。
    /// </summary>
    [Fact]
    public void ConvolutionApis_NullImage_Throw()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.Soften(null!))
            .ParamName.ShouldBe("image");
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.Sharpen(null!))
            .ParamName.ShouldBe("image");
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.Emboss(null!))
            .ParamName.ShouldBe("image");
    }

    private static Image<Rgba32> CreateSampleImage()
    {
        var image = new Image<Rgba32>(2, 2);
        image[0, 0] = new Rgba32(255, 0, 0, 255);
        image[1, 0] = new Rgba32(0, 255, 0, 255);
        image[0, 1] = new Rgba32(0, 0, 255, 255);
        image[1, 1] = new Rgba32(255, 255, 255, 255);
        return image;
    }

    private static Image<Rgba32> CreatePatternImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                image[x, y] = new Rgba32((byte)(x * 11 % 255), (byte)(y * 19 % 255), (byte)((x + y) * 7 % 255), 255);
            }
        }
        return image;
    }

    private static Image<Rgba32> CreateNoiseImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        var random = new Random(42);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                image[x, y] = new Rgba32((byte)random.Next(0, 256), (byte)random.Next(0, 256), (byte)random.Next(0, 256), 255);
            }
        }
        return image;
    }

    private static Image<Rgba32> CreateSolidImage(int width, int height, Rgba32 color)
    {
        var image = new Image<Rgba32>(width, height);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
                image[x, y] = color;
        }

        return image;
    }
}

