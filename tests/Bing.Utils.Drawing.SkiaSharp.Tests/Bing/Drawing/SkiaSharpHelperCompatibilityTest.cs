using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 Phase 1 兼容层 API。
/// </summary>
[Trait("Drawing", "SkiaSharp.Compatibility")]
public class SkiaSharpHelperCompatibilityTest
{
    #region ToStream

    /// <summary>
    /// 验证转换结果是位于起始位置的可读流。
    /// </summary>
    [Fact]
    public void ToStream_ReturnsReadableStreamAtPositionZero()
    {
        using var source = CreateSampleImage();
        using var stream = SkiaSharpHelper.ToStream(source);
        stream.Position.ShouldBe(0);
        stream.Length.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// 验证空图像参数抛出参数为空异常。
    /// </summary>
    [Fact]
    public void ToStream_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToStream(null!));
    }

    /// <summary>
    /// 验证图像转换为流后可通过流重新加载。
    /// </summary>
    [Fact]
    public void ToStream_CanRoundTripWithFromStream()
    {
        using var source = CreateSampleImage();
        using var stream = SkiaSharpHelper.ToStream(source);
        var restored = SkiaSharpHelper.FromStream(stream);
        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(source.Width);
        restored.Height.ShouldBe(source.Height);
        restored.Dispose();
    }

    /// <summary>
    /// 验证加载的 JPEG 图像默认转换为 PNG 流。
    /// </summary>
    [Fact]
    public void ToStream_LoadedJpegDefaultsToPng()
    {
        using var source = CreateSampleImage();
        var jpeg = SkiaSharpHelper.ToBytes(source, SKEncodedImageFormat.Jpeg);
        using var loaded = SkiaSharpHelper.FromBytes(jpeg);
        using var stream = SkiaSharpHelper.ToStream(loaded!);
        var header = new byte[8];
        stream.Read(header, 0, header.Length).ShouldBe(header.Length);

        header.ShouldBe(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
    }

    #endregion

    #region MakeThumbnail

    /// <summary>
    /// 验证固定宽高缩略图输出指定尺寸。
    /// </summary>
    [Fact]
    public void MakeThumbnail_FixedBoth_ExactDimensions()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = SkiaSharpHelper.MakeThumbnail(source, 4, 4, ThumbnailMode.FixedBoth);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证固定宽度缩略图按比例计算高度。
    /// </summary>
    [Fact]
    public void MakeThumbnail_FixedW_HeightAuto()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = SkiaSharpHelper.MakeThumbnail(source, 4, 10, ThumbnailMode.FixedW);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(2);
    }

    /// <summary>
    /// 验证固定高度缩略图按比例计算宽度。
    /// </summary>
    [Fact]
    public void MakeThumbnail_FixedH_WidthAuto()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = SkiaSharpHelper.MakeThumbnail(source, 10, 2, ThumbnailMode.FixedH);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(2);
    }

    /// <summary>
    /// 验证裁剪模式缩略图调整到指定宽高比。
    /// </summary>
    [Fact]
    public void MakeThumbnail_Cut_CropsToAspectRatio()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = SkiaSharpHelper.MakeThumbnail(source, 4, 4, ThumbnailMode.Cut);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证空源图像参数抛出参数为空异常。
    /// </summary>
    [Fact]
    public void MakeThumbnail_NullSource_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.MakeThumbnail((SKImage)null!, 4, 4, ThumbnailMode.FixedBoth));
    }

    #endregion

    #region ScaleImage

    /// <summary>
    /// 验证按宽度缩放时返回指定画布尺寸。
    /// </summary>
    [Fact]
    public void ScaleImage_FitWidth_ProducesExactCanvas()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = SkiaSharpHelper.ScaleImage(source, 4, 4);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证按高度缩放时返回指定画布尺寸。
    /// </summary>
    [Fact]
    public void ScaleImage_FitHeight_ProducesExactCanvas()
    {
        using var source = CreateSampleImage(4, 8);
        using var result = SkiaSharpHelper.ScaleImage(source, 4, 4);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证空图像参数抛出参数为空异常。
    /// </summary>
    [Fact]
    public void ScaleImage_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ScaleImage(null!, 4, 4));
    }

    #endregion

    #region Gray

    /// <summary>
    /// 验证灰度转换保持图像尺寸。
    /// </summary>
    [Fact]
    public void Gray_ReturnsSameSize()
    {
        using var source = CreateSampleImage();
        using var result = SkiaSharpHelper.Gray(source);
        result.Width.ShouldBe(source.Width);
        result.Height.ShouldBe(source.Height);
    }

    /// <summary>
    /// 验证空图像参数抛出参数为空异常。
    /// </summary>
    [Fact]
    public void Gray_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.Gray(null!));
    }

    /// <summary>
    /// 验证白色像素灰度转换后仍保持白色。
    /// </summary>
    [Fact]
    public void Gray_WhitePixel_PreservesWhite()
    {
        using var source = CreateSampleImage(1, 1, new SKColor(255, 255, 255));
        using var result = SkiaSharpHelper.Gray(source);
        using var bitmap = SKBitmap.FromImage(result);
        var pixel = bitmap.GetPixel(0, 0);
        pixel.Red.ShouldBe((byte)255);
        pixel.Green.ShouldBe((byte)255);
        pixel.Blue.ShouldBe((byte)255);
    }

    #endregion

    #region ToBlackWhiteImage

    /// <summary>
    /// 验证黑白转换保持图像尺寸。
    /// </summary>
    [Fact]
    public void ToBlackWhiteImage_ReturnsSameSize()
    {
        using var source = CreateSampleImage();
        using var result = SkiaSharpHelper.ToBlackWhiteImage(source);
        result.Width.ShouldBe(source.Width);
        result.Height.ShouldBe(source.Height);
    }

    /// <summary>
    /// 验证空图像参数抛出参数为空异常。
    /// </summary>
    [Fact]
    public void ToBlackWhiteImage_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToBlackWhiteImage(null!));
    }

    #endregion

    #region FilterColor

    /// <summary>
    /// 验证滤色处理移除红色通道。
    /// </summary>
    [Fact]
    public void FilterColor_RemovesRedChannel()
    {
        using var source = CreateSampleImage(1, 1, new SKColor(200, 100, 50));
        using var result = SkiaSharpHelper.FilterColor(source);
        using var bitmap = SKBitmap.FromImage(result);
        var pixel = bitmap.GetPixel(0, 0);
        pixel.Red.ShouldBe((byte)0);
        pixel.Green.ShouldBe((byte)100);
        pixel.Blue.ShouldBe((byte)50);
    }

    /// <summary>
    /// 验证空图像参数抛出参数为空异常。
    /// </summary>
    [Fact]
    public void FilterColor_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.FilterColor(null!));
    }

    #endregion

    #region Plate

    /// <summary>
    /// 验证底片处理反转 RGB 通道。
    /// </summary>
    [Fact]
    public void Plate_InvertsRgbChannels()
    {
        using var source = CreateSampleImage(1, 1, new SKColor(100, 150, 200));
        using var result = SkiaSharpHelper.Plate(source);
        using var bitmap = SKBitmap.FromImage(result);
        var pixel = bitmap.GetPixel(0, 0);
        pixel.Red.ShouldBe((byte)155);
        pixel.Green.ShouldBe((byte)105);
        pixel.Blue.ShouldBe((byte)55);
    }

    /// <summary>
    /// 验证空图像参数抛出参数为空异常。
    /// </summary>
    [Fact]
    public void Plate_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.Plate(null!));
    }

    #endregion

    #region PerPixelProcess

    /// <summary>
    /// 验证逐像素处理转换所有像素。
    /// </summary>
    [Fact]
    public void PerPixelProcess_TransformsAllPixels()
    {
        using var source = CreateSampleImage(2, 2, new SKColor(100, 100, 100));
        using var result = SkiaSharpHelper.PerPixelProcess(source, color => new SKColor(255, 0, 0, color.Alpha));
        using var bitmap = SKBitmap.FromImage(result);
        var pixel = bitmap.GetPixel(0, 0);
        pixel.Red.ShouldBe((byte)255);
        pixel.Green.ShouldBe((byte)0);
        pixel.Blue.ShouldBe((byte)0);
    }

    /// <summary>
    /// 验证逐像素处理不会修改源图像。
    /// </summary>
    [Fact]
    public void PerPixelProcess_DoesNotMutateSource()
    {
        using var source = CreateSampleImage(1, 1, new SKColor(100, 100, 100));
        using var result = SkiaSharpHelper.PerPixelProcess(source, color => new SKColor(255, 0, 0, color.Alpha));
        using var bitmap = SKBitmap.FromImage(source);
        bitmap.GetPixel(0, 0).Red.ShouldBe((byte)100);
    }

    /// <summary>
    /// 验证空图像参数抛出参数为空异常。
    /// </summary>
    [Fact]
    public void PerPixelProcess_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.PerPixelProcess(null!, c => c));
    }

    #endregion

    #region ColorExtensions

    /// <summary>
    /// 验证白色灰度值为 1。
    /// </summary>
    [Fact]
    public void GetGrayScale_White_ReturnsOne()
    {
        SkiaSharpHelper.GetGrayScale(SKColors.White).ShouldBe(1.0f, 0.01f);
    }

    /// <summary>
    /// 验证黑色灰度值为 0。
    /// </summary>
    [Fact]
    public void GetGrayScale_Black_ReturnsZero()
    {
        SkiaSharpHelper.GetGrayScale(SKColors.Black).ShouldBe(0.0f, 0.01f);
    }

    /// <summary>
    /// 验证相同颜色的差异值为 0。
    /// </summary>
    [Fact]
    public void ColorDifference_SameColor_ReturnsZero()
    {
        var c = new SKColor(100, 150, 200);
        SkiaSharpHelper.ColorDifference(c, c).ShouldBe(0.0, 0.001);
    }

    /// <summary>
    /// 验证颜色差异计算具有对称性。
    /// </summary>
    [Fact]
    public void ColorDifference_IsSymmetric()
    {
        var a = new SKColor(200, 100, 50);
        var b = new SKColor(50, 100, 200);
        SkiaSharpHelper.ColorDifference(a, b).ShouldBe(SkiaSharpHelper.ColorDifference(b, a), 0.001);
    }

    /// <summary>
    /// 验证相同颜色会被判定为相似。
    /// </summary>
    [Fact]
    public void IsSimilarColors_SameColor_ReturnsTrue()
    {
        var c = new SKColor(100, 150, 200);
        SkiaSharpHelper.IsSimilarColors(c, c).ShouldBeTrue();
    }

    /// <summary>
    /// 验证不同透明度的颜色不会被判定为相似。
    /// </summary>
    [Fact]
    public void IsSimilarColors_DifferentAlpha_ReturnsFalse()
    {
        var a = new SKColor(128, 128, 128, 100);
        var b = new SKColor(128, 128, 128, 200);
        SkiaSharpHelper.IsSimilarColors(a, b).ShouldBeFalse();
    }

    /// <summary>
    /// 验证混合比例为 1 时返回前景色。
    /// </summary>
    [Fact]
    public void Blend_Amount1_ReturnsOriginal()
    {
        var foreground = new SKColor(200, 100, 50);
        var background = new SKColor(50, 100, 200);
        var result = SkiaSharpHelper.Blend(foreground, background, 1.0);
        result.Red.ShouldBe((byte)200);
    }

    /// <summary>
    /// 验证混合比例为 0 时返回背景色。
    /// </summary>
    [Fact]
    public void Blend_Amount0_ReturnsBackground()
    {
        var foreground = new SKColor(200, 100, 50);
        var background = new SKColor(50, 100, 200);
        var result = SkiaSharpHelper.Blend(foreground, background, 0.0);
        result.Red.ShouldBe((byte)50);
    }

    #endregion

    #region ColorMatrices

    /// <summary>
    /// 验证亮度为 1 时颜色矩阵对角线为 1。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateBrightnessFilter_Amount1_DiagonalIs1()
    {
        var matrix = ColorMatrices.CreateBrightnessFilter(1f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[1, 1].ShouldBe(1f, 0.001f);
        matrix[2, 2].ShouldBe(1f, 0.001f);
        matrix[3, 3].ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 验证负亮度参数抛出范围异常。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateBrightnessFilter_NegativeAmount_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateBrightnessFilter(-0.1f));
    }

    /// <summary>
    /// 验证对比度为 1 时矩阵保持原始变换。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateContrastFilter_Amount1_DiagonalIs1()
    {
        var matrix = ColorMatrices.CreateContrastFilter(1f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[4, 0].ShouldBe(0f, 0.001f);
    }

    /// <summary>
    /// 验证灰度化参数为 0 时生成全灰度矩阵。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateGrayScaleFilter_Amount0_ReturnsIdentityLike()
    {
        var matrix = ColorMatrices.CreateGrayScaleFilter(0f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[3, 3].ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 验证饱和度参数为 1 时生成保持原色的矩阵。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateSaturationFilter_Amount1_ReturnsIdentityLike()
    {
        var matrix = ColorMatrices.CreateSaturationFilter(1f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[3, 3].ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 验证超出范围的灰度化参数抛出范围异常。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateGrayScaleFilter_OutOfRange_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateGrayScaleFilter(-0.1f));
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateGrayScaleFilter(1.1f));
    }

    /// <summary>
    /// 验证负对比度参数抛出范围异常。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateContrastFilter_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateContrastFilter(-0.1f));
    }

    /// <summary>
    /// 验证负饱和度参数抛出范围异常。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateSaturationFilter_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateSaturationFilter(-0.1f));
    }

    #endregion

    #region Helper

    /// <summary>
    /// 创建指定尺寸和填充色的示例图像。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <param name="fill">填充颜色；为空时使用默认灰色。</param>
    /// <returns>指定尺寸和颜色的示例图像。</returns>
    private static SKImage CreateSampleImage(int width = 4, int height = 4, SKColor? fill = null)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(fill ?? new SKColor(128, 128, 128));
        return SKImage.FromBitmap(bitmap);
    }

    #endregion
}
