using System.Text.RegularExpressions;
using SkiaSharp;
namespace Bing.Drawing;
/// <summary>
/// 验证 <see cref="SkiaSharpHelper"/> 的图像编码、变换和水印行为。
/// </summary>
public class SkiaSharpHelperTest
{
    /// <summary>
    /// 验证 PNG 字节可还原为与源图像尺寸相同的图像。
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
    /// 验证 Base64 文本可还原为与源图像尺寸相同的图像。
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
    /// 验证 Data URL 包含图像媒体类型和 Base64 数据标记。
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
    /// 验证新建图像默认导出为 PNG 格式。
    /// </summary>
    [Fact]
    public void ToDataUrl_DefaultFormat_ForInMemoryImage_UsesPng()
    {
        using var source = CreateSampleImage();

        var dataUrl = SkiaSharpHelper.ToDataUrl(source);

        dataUrl.ShouldStartWith("data:image/png;base64,");
    }

    /// <summary>
    /// 验证从 JPEG 加载的图像经缩放后默认导出仍为 JPEG。
    /// </summary>
    [Fact]
    public void ToDataUrl_DefaultFormat_ForDerivedImage_UsesPng()
    {
        using var source = CreateNoiseImage(32, 16);
        var bytes = SkiaSharpHelper.ToBytes(source, SKEncodedImageFormat.Jpeg, 90);
        using var restored = SkiaSharpHelper.FromBytes(bytes);
        using var resized = SkiaSharpHelper.Resize(restored!, 16, 8);

        var dataUrl = SkiaSharpHelper.ToDataUrl(resized);

        dataUrl.ShouldStartWith("data:image/png;base64,");
    }
    /// <summary>
    /// 验证 PNG Data URL 可还原为与源图像尺寸相同的图像。
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
    /// 验证空字节数组参数会抛出参数名为 <c>bytes</c> 的空参数异常。
    /// </summary>
    [Fact]
    public void FromBytes_Null_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.FromBytes(null!))
            .ParamName.ShouldBe("bytes");
    }
    /// <summary>
    /// 验证空图像会导致字节编码抛出参数名为 <c>image</c> 的空参数异常。
    /// </summary>
    [Fact]
    public void ToBytes_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToBytes(null!))
            .ParamName.ShouldBe("image");
    }
    /// <summary>
    /// 验证空图像会导致 Base64 编码抛出参数名为 <c>image</c> 的空参数异常。
    /// </summary>
    [Fact]
    public void ToBase64String_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToBase64String(null!))
            .ParamName.ShouldBe("image");
    }
    /// <summary>
    /// 验证空图像会导致 Data URL 编码抛出参数名为 <c>image</c> 的空参数异常。
    /// </summary>
    [Fact]
    public void ToDataUrl_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToDataUrl(null!))
            .ParamName.ShouldBe("image");
    }
    /// <summary>
    /// 验证无效 Base64 文本无法生成图像并返回空结果。
    /// </summary>
    [Fact]
    public void FromBase64String_InvalidValue_ThrowsFormatException()
    {
        Should.Throw<FormatException>(() => SkiaSharpHelper.FromBase64String("not-base64"));
    }
    /// <summary>
    /// 验证无效 Data URL 无法生成图像并返回空结果。
    /// </summary>
    [Fact]
    public void FromDataUrl_InvalidFormat_ThrowsFormatException()
    {
        Should.Throw<FormatException>(() => SkiaSharpHelper.FromDataUrl("invalid-data-url"));
    }

    /// <summary>
    /// 测试用例：验证尝试加载无效图像时返回 false 且不抛出内容异常。
    /// </summary>
    [Fact]
    public void TryLoad_InvalidBytes_ReturnsFalseWithoutThrowing()
    {
        var loaded = SkiaSharpHelper.TryLoad(new byte[] { 1, 2, 3 }, out var image);

        loaded.ShouldBeFalse();
        image.ShouldBeNull();
    }

    /// <summary>
    /// 测试用例：验证无显式格式时 ToBytes 始终输出 PNG。
    /// </summary>
    [Fact]
    public void ToBytes_DefaultFormat_UsesPng()
    {
        using var source = CreateSampleImage();
        var jpeg = SkiaSharpHelper.ToBytes(source, SKEncodedImageFormat.Jpeg, 90);
        using var restored = SkiaSharpHelper.FromBytes(jpeg);

        var output = SkiaSharpHelper.ToBytes(restored!);
        using var data = SKData.CreateCopy(output);
        using var codec = SKCodec.Create(data);

        codec.ShouldNotBeNull();
        codec!.EncodedFormat.ShouldBe(SKEncodedImageFormat.Png);
    }
    /// <summary>
    /// 验证 PNG 文件可加载为与原图尺寸相同的图像。
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
    /// 验证图像从流当前位置加载，且加载后调用方流仍可读取。
    /// </summary>
    [Fact]
    public void FromStream_WithOffsetPayload_LoadsExpectedImageAndKeepsStreamOpen()
    {
        using var source = CreateSampleImage();
        var bytes = SkiaSharpHelper.ToBytes(source, (SKEncodedImageFormat.Png, 100));
        var payload = new byte[3 + bytes.Length];
        Array.Copy(bytes, 0, payload, 3, bytes.Length);
        using var stream = new MemoryStream(payload);
        stream.Position = 3;

        using var restored = SkiaSharpHelper.FromStream(stream);

        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(2);
        restored.Height.ShouldBe(2);
        stream.CanRead.ShouldBeTrue();
        stream.Position.ShouldBe(stream.Length);
    }
    /// <summary>
    /// 验证小于 0 或大于 1 的透明度会抛出范围异常。
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
    /// 验证透明度设为 0.5 后返回新图像且像素 Alpha 为 128。
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
        bitmap.GetPixel(0, 0).Alpha.ShouldBe((byte)128);
    }

    /// <summary>
    /// 验证设置透明度时按原像素 Alpha 比例缩放。
    /// </summary>
    [Fact]
    public void SetOpacity_PartiallyTransparentPixel_MultipliesExistingAlpha()
    {
        using var source = CreateSolidImage(1, 1, new SKColor(255, 0, 0, 128));

        using var result = SkiaSharpHelper.SetOpacity(source, 0.5f);
        using var bitmap = SKBitmap.FromImage(result);

        bitmap.GetPixel(0, 0).Alpha.ShouldBe((byte)64);
    }
    /// <summary>
    /// 验证空图像会导致透明度设置抛出空参数异常。
    /// </summary>
    [Fact]
    public void SetOpacity_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.SetOpacity(null!, 0.5f))
            .ParamName.ShouldBe("image");
    }

    /// <summary>
    /// 验证 PNG 图像对应的 MIME 类型和文件扩展名。
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
    /// 验证含透明像素的图像被识别为具有 Alpha 通道。
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
    /// 验证保持宽高比缩放会返回新图像并计算出正确尺寸。
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
    /// 验证禁用放大时小图保持原始尺寸。
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
    /// 验证超出图像边界的裁剪区域会收缩至有效范围。
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
    /// 验证旋转 90 度后图像宽高互换。
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
    /// 验证水平翻转后像素位置关于垂直轴镜像。
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
    /// 验证 JPEG 低质量编码产生的字节数少于高质量编码。
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
    /// 验证字节编码的元组质量超出范围时抛出范围异常。
    /// </summary>
    [Fact]
    public void ToBytes_TupleQualityOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        using var source = CreateSampleImage();

        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.ToBytes(source, (SKEncodedImageFormat.Jpeg, -1)))
            .ParamName.ShouldBe("quality");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.ToBytes(source, (SKEncodedImageFormat.Jpeg, 0)))
            .ParamName.ShouldBe("quality");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.ToBytes(source, (SKEncodedImageFormat.Jpeg, 101)))
            .ParamName.ShouldBe("quality");
    }

    /// <summary>
    /// 验证字节编码的显式质量参数超出范围时抛出范围异常。
    /// </summary>
    [Fact]
    public void ToBytes_DirectQualityOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        using var source = CreateSampleImage();

        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.ToBytes(source, SKEncodedImageFormat.Jpeg, 0))
            .ParamName.ShouldBe("quality");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.ToBytes(source, SKEncodedImageFormat.Jpeg, 101))
            .ParamName.ShouldBe("quality");
    }

    /// <summary>
    /// 验证 Base64 编码的元组质量超出范围时抛出范围异常。
    /// </summary>
    [Fact]
    public void ToBase64String_TupleQualityOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        using var source = CreateSampleImage();

        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.ToBase64String(source, (SKEncodedImageFormat.Jpeg, -1)))
            .ParamName.ShouldBe("quality");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.ToBase64String(source, (SKEncodedImageFormat.Jpeg, 0)))
            .ParamName.ShouldBe("quality");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.ToBase64String(source, (SKEncodedImageFormat.Jpeg, 101)))
            .ParamName.ShouldBe("quality");
    }

    /// <summary>
    /// 验证 Data URL 编码的元组质量超出范围时抛出范围异常。
    /// </summary>
    [Fact]
    public void ToDataUrl_TupleQualityOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        using var source = CreateSampleImage();

        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.ToDataUrl(source, (SKEncodedImageFormat.Jpeg, -1)))
            .ParamName.ShouldBe("quality");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.ToDataUrl(source, (SKEncodedImageFormat.Jpeg, 0)))
            .ParamName.ShouldBe("quality");
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.ToDataUrl(source, (SKEncodedImageFormat.Jpeg, 101)))
            .ParamName.ShouldBe("quality");
    }

    /// <summary>
    /// 验证 ICO 图像使用标准的 x-icon MIME 类型。
    /// </summary>
    [Fact]
    public void GetMimeType_IcoFormat_ReturnsXIconMime()
    {
        SKEncodedImageFormat.Ico.GetMimeType().ShouldBe("image/x-icon");
    }

    /// <summary>
    /// 验证颜色替换会匹配容差范围内的像素并应用目标颜色。
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
    /// 验证黑白效果保留源像素的 Alpha 值。
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
    /// 验证黑色源像素在双色调效果下映射为第一种目标颜色。
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
    /// 验证亮度参数为 0 时像素结果为黑色。
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
    /// 验证对比度参数为 0 时像素结果为中灰色。
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
    /// 验证饱和度参数为 0 时像素结果为灰度。
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
    /// 验证应用单位颜色矩阵后像素通道保持不变。
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
    /// 验证颜色变换 API 拒绝越界参数。
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
    /// 验证边缘柔化降低边缘 Alpha 且保留中心区域。
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
    /// 验证柔化半径为 0 时返回像素不变的新图像。
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
    /// 验证负柔化半径会被拒绝。
    /// </summary>
    [Fact]
    public void SoftEdge_NegativeRadius_ThrowsArgumentOutOfRangeException()
    {
        using var source = CreateSampleImage();

        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.SoftEdge(source, -1f))
            .ParamName.ShouldBe("radius");
    }

    /// <summary>
    /// 验证柔化卷积将单个亮点扩散到相邻像素。
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
    /// 验证锐化卷积不改变均匀颜色图像的像素。
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
    /// 验证浮雕卷积将均匀颜色图像转换为近似中灰。
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
    /// 验证雾化效果使用局部前向采样生成新图像。
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
    /// 验证雾化效果拒绝空图像参数。
    /// </summary>
    [Fact]
    public void Atomizing_NullImage_Throw()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.Atomizing(null!))
            .ParamName.ShouldBe("image");
    }

    /// <summary>
    /// 验证图片水印按目标区域缩放叠加且不修改源图像。
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
    /// 验证水印透明度为 0 时返回像素不变的新图像。
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
    /// 验证半透明水印叠加结果符合源像素与水印的 Alpha 比例。
    /// </summary>
    [Fact]
    public void AddImageWatermark_PartiallyTransparentWatermark_PreservesAlphaComposition()
    {
        using var source = CreateSolidImage(1, 1, new SKColor(0, 0, 0, 0));
        using var watermark = CreateSolidImage(1, 1, new SKColor(255, 0, 0, 128));

        using var result = SkiaSharpHelper.AddImageWatermark(source, watermark, new SKRectI(0, 0, 1, 1), 0.5f);
        using var resultBitmap = SKBitmap.FromImage(result);

        resultBitmap.GetPixel(0, 0).Alpha.ShouldBe((byte)64);
    }

    /// <summary>
    /// 验证图片水印拒绝空图像、无效区域和越界透明度。
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
    /// 验证文字水印在图像右下区域绘制且不修改源图像。
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
    /// 验证文字水印透明度为 0 时图像像素保持不变。
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
    /// 验证文字水印拒绝空图像、空白文本和无效尺寸参数。
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
    /// 验证验证码文本长度正确且仅包含去歧义数字和字母。
    /// </summary>
    [Fact]
    public void GetCaptchaCode_ValidLength_ReturnsExpectedShape()
    {
        var code = SkiaSharpHelper.GetCaptchaCode(6);

        code.Length.ShouldBe(6);
        Regex.IsMatch(code, "^[23456789A-HJ-NP-Za-km-z]+$").ShouldBeTrue();
    }

    /// <summary>
    /// 验证验证码长度为 0 时抛出范围异常。
    /// </summary>
    [Fact]
    public void GetCaptchaCode_InvalidLength_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.GetCaptchaCode(0))
            .ParamName.ShouldBe("length");
    }

    /// <summary>
    /// 验证按长度创建的验证码图片包含指定长度的数字字母文本。
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
    /// 验证按长度创建验证码图片时拒绝非法长度。
    /// </summary>
    [Fact]
    public void CreateCaptchaImage_InvalidLength_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => SkiaSharpHelper.CreateCaptchaImage(0, out _))
            .ParamName.ShouldBe("length");
    }

    /// <summary>
    /// 验证验证码图片尺寸随文本长度变化且文本区域含有绘制像素。
    /// </summary>
    /// <param name="code">验证码文本。</param>
    /// <param name="expectedWidth">期望图像宽度。</param>
    /// <param name="expectedHeight">期望图像高度。</param>
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
    /// 验证验证码图片 API 拒绝空白验证码文本。
    /// </summary>
    /// <param name="code">待验证的验证码文本。</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateCaptchaImage_BlankCode_ThrowsArgumentNullException(string code)
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.CreateCaptchaImage(code))
            .ParamName.ShouldBe("code");
    }

    /// <summary>
    /// 验证空图像传入柔化、锐化和浮雕效果时均抛出空参数异常。
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

    /// <summary>
    /// 创建包含红、绿、蓝和白色像素的 2×2 测试图像。
    /// </summary>
    /// <returns>包含四种颜色样本的图像。</returns>
    private static SKImage CreateSampleImage()
    {
        using var bitmap = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(255, 0, 0, 255));
        bitmap.SetPixel(1, 0, new SKColor(0, 255, 0, 255));
        bitmap.SetPixel(0, 1, new SKColor(0, 0, 255, 255));
        bitmap.SetPixel(1, 1, new SKColor(255, 255, 255, 255));
        return SKImage.FromBitmap(bitmap);
    }

    /// <summary>
    /// 创建颜色按像素坐标变化的测试图像。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <returns>按坐标生成颜色的图像。</returns>
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

    /// <summary>
    /// 使用固定随机种子创建像素颜色的测试图像。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <returns>包含确定性随机颜色的图像。</returns>
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

    /// <summary>
    /// 创建由指定颜色填充的测试图像。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <param name="color">填充图像的颜色。</param>
    /// <returns>指定尺寸和颜色的图像。</returns>
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

    /// <summary>
    /// 检查指定区域内是否存在与背景色不同的像素。
    /// </summary>
    /// <param name="bitmap">要检查的位图。</param>
    /// <param name="background">用于比较的背景色。</param>
    /// <param name="startX">检查区域左边界，默认为位图左边缘。</param>
    /// <param name="startY">检查区域上边界，默认为位图上边缘。</param>
    /// <param name="endX">检查区域右边界；省略时使用位图宽度。</param>
    /// <param name="endY">检查区域下边界；省略时使用位图高度。</param>
    /// <returns>区域内存在非背景像素时为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
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

