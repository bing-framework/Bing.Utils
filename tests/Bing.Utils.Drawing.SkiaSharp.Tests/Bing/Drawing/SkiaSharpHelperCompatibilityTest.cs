using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 验证图像兼容 API 的流、缩略图、像素变换和颜色运算。
/// </summary>
[Trait("Drawing", "SkiaSharp.Compatibility")]
public class SkiaSharpHelperCompatibilityTest
{
    #region ToStream

    /// <summary>
    /// 验证编码后的流可读且位置从开头开始。
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
    /// 验证空图像会导致流导出抛出空参数异常。
    /// </summary>
    [Fact]
    public void ToStream_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToStream(null!));
    }

    /// <summary>
    /// 验证导出的图像流可重新加载并保留原始尺寸。
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

    #endregion

    #region MakeThumbnail

    /// <summary>
    /// 验证固定宽高模式生成指定尺寸的缩略图。
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
    /// 验证固定宽度模式按比例计算缩略图高度。
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
    /// 验证固定高度模式按比例计算缩略图宽度。
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
    /// 验证裁剪模式将缩略图裁成指定宽高比。
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
    /// 验证空源图像会导致缩略图生成抛出空参数异常。
    /// </summary>
    [Fact]
    public void MakeThumbnail_NullSource_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.MakeThumbnail((SKImage)null!, 4, 4, ThumbnailMode.FixedBoth));
    }

    #endregion

    #region ScaleImage

    /// <summary>
    /// 验证按宽度缩放后画布尺寸与目标尺寸一致。
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
    /// 验证按高度缩放后画布尺寸与目标尺寸一致。
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
    /// 验证空图像会导致缩放操作抛出空参数异常。
    /// </summary>
    [Fact]
    public void ScaleImage_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ScaleImage(null!, 4, 4));
    }

    #endregion

    #region Gray

    /// <summary>
    /// 验证灰度转换保留图像尺寸。
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
    /// 验证空图像会导致灰度转换抛出空参数异常。
    /// </summary>
    [Fact]
    public void Gray_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.Gray(null!));
    }

    /// <summary>
    /// 验证白色像素转换为灰度后仍保持白色。
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
    /// 验证黑白转换保留图像尺寸。
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
    /// 验证空图像会导致黑白转换抛出空参数异常。
    /// </summary>
    [Fact]
    public void ToBlackWhiteImage_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToBlackWhiteImage(null!));
    }

    #endregion

    #region FilterColor

    /// <summary>
    /// 验证颜色过滤将红色通道清零并保留其他通道。
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
    /// 验证空图像会导致颜色过滤抛出空参数异常。
    /// </summary>
    [Fact]
    public void FilterColor_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.FilterColor(null!));
    }

    #endregion

    #region Plate

    /// <summary>
    /// 验证底片效果反转 RGB 通道值。
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
    /// 验证空图像会导致底片效果抛出空参数异常。
    /// </summary>
    [Fact]
    public void Plate_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.Plate(null!));
    }

    #endregion

    #region PerPixelProcess

    /// <summary>
    /// 验证逐像素处理器转换图像中的像素颜色。
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
    /// 验证空图像会导致逐像素处理抛出空参数异常。
    /// </summary>
    [Fact]
    public void PerPixelProcess_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.PerPixelProcess(null!, c => c));
    }

    #endregion

    #region ColorExtensions

    /// <summary>
    /// 验证白色的灰度值接近 1。
    /// </summary>
    [Fact]
    public void GetGrayScale_White_ReturnsOne()
    {
        SkiaSharpHelper.GetGrayScale(SKColors.White).ShouldBe(1.0f, 0.01f);
    }

    /// <summary>
    /// 验证黑色的灰度值接近 0。
    /// </summary>
    [Fact]
    public void GetGrayScale_Black_ReturnsZero()
    {
        SkiaSharpHelper.GetGrayScale(SKColors.Black).ShouldBe(0.0f, 0.01f);
    }

    /// <summary>
    /// 验证相同颜色之间的差异为 0。
    /// </summary>
    [Fact]
    public void ColorDifference_SameColor_ReturnsZero()
    {
        var c = new SKColor(100, 150, 200);
        SkiaSharpHelper.ColorDifference(c, c).ShouldBe(0.0, 0.001);
    }

    /// <summary>
    /// 验证颜色差异计算满足对称性。
    /// </summary>
    [Fact]
    public void ColorDifference_IsSymmetric()
    {
        var a = new SKColor(200, 100, 50);
        var b = new SKColor(50, 100, 200);
        SkiaSharpHelper.ColorDifference(a, b).ShouldBe(SkiaSharpHelper.ColorDifference(b, a), 0.001);
    }

    /// <summary>
    /// 验证颜色与自身比较时判定为相似。
    /// </summary>
    [Fact]
    public void IsSimilarColors_SameColor_ReturnsTrue()
    {
        var c = new SKColor(100, 150, 200);
        SkiaSharpHelper.IsSimilarColors(c, c).ShouldBeTrue();
    }

    /// <summary>
    /// 验证 Alpha 通道不同的颜色不会被判定为相似。
    /// </summary>
    [Fact]
    public void IsSimilarColors_DifferentAlpha_ReturnsFalse()
    {
        var a = new SKColor(128, 128, 128, 100);
        var b = new SKColor(128, 128, 128, 200);
        SkiaSharpHelper.IsSimilarColors(a, b).ShouldBeFalse();
    }

    /// <summary>
    /// 验证混合比例为 1 时结果采用前景色。
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
    /// 验证混合比例为 0 时结果采用背景色。
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
    /// 验证亮度为 1 时颜色矩阵的 RGB 和 Alpha 对角线为 1。
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
    /// 验证负亮度参数会被拒绝。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateBrightnessFilter_NegativeAmount_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateBrightnessFilter(-0.1f));
    }

    /// <summary>
    /// 验证对比度为 1 时颜色矩阵保持单位变换。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateContrastFilter_Amount1_DiagonalIs1()
    {
        var matrix = ColorMatrices.CreateContrastFilter(1f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[4, 0].ShouldBe(0f, 0.001f);
    }

    /// <summary>
    /// 验证灰度强度为 0 时颜色矩阵等同于单位变换。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateGrayScaleFilter_Amount0_ReturnsIdentityLike()
    {
        var matrix = ColorMatrices.CreateGrayScaleFilter(0f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[3, 3].ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 验证饱和度为 1 时颜色矩阵等同于单位变换。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateSaturationFilter_Amount1_ReturnsIdentityLike()
    {
        var matrix = ColorMatrices.CreateSaturationFilter(1f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[3, 3].ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 验证超出范围的灰度强度会被拒绝。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateGrayScaleFilter_OutOfRange_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateGrayScaleFilter(-0.1f));
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateGrayScaleFilter(1.1f));
    }

    /// <summary>
    /// 验证负对比度参数会被拒绝。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateContrastFilter_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateContrastFilter(-0.1f));
    }

    /// <summary>
    /// 验证负饱和度参数会被拒绝。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateSaturationFilter_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateSaturationFilter(-0.1f));
    }

    #endregion

    #region Helper

    /// <summary>
    /// 创建指定尺寸和填充颜色的测试图像。
    /// </summary>
    /// <param name="width">图像宽度，默认值为 4。</param>
    /// <param name="height">图像高度，默认值为 4。</param>
    /// <param name="fill">可选填充颜色；省略时使用中灰色。</param>
    /// <returns>由指定颜色填充的图像。</returns>
    private static SKImage CreateSampleImage(int width = 4, int height = 4, SKColor? fill = null)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(fill ?? new SKColor(128, 128, 128));
        return SKImage.FromBitmap(bitmap);
    }

    #endregion
}
