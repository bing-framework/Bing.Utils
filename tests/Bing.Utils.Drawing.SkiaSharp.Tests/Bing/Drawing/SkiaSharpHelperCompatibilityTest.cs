using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 Phase 1 兼容层 API。
/// </summary>
[Trait("Drawing", "SkiaSharp.Compatibility")]
public class SkiaSharpHelperCompatibilityTest
{
    #region ToStream

    [Fact]
    public void ToStream_ReturnsReadableStreamAtPositionZero()
    {
        using var source = CreateSampleImage();
        using var stream = SkiaSharpHelper.ToStream(source);
        stream.Position.ShouldBe(0);
        stream.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ToStream_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToStream(null!));
    }

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

    [Fact]
    public void MakeThumbnail_FixedBoth_ExactDimensions()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = SkiaSharpHelper.MakeThumbnail(source, 4, 4, ThumbnailMode.FixedBoth);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    [Fact]
    public void MakeThumbnail_FixedW_HeightAuto()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = SkiaSharpHelper.MakeThumbnail(source, 4, 10, ThumbnailMode.FixedW);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(2);
    }

    [Fact]
    public void MakeThumbnail_FixedH_WidthAuto()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = SkiaSharpHelper.MakeThumbnail(source, 10, 2, ThumbnailMode.FixedH);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(2);
    }

    [Fact]
    public void MakeThumbnail_Cut_CropsToAspectRatio()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = SkiaSharpHelper.MakeThumbnail(source, 4, 4, ThumbnailMode.Cut);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    [Fact]
    public void MakeThumbnail_NullSource_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.MakeThumbnail(null!, 4, 4, ThumbnailMode.FixedBoth));
    }

    #endregion

    #region ScaleImage

    [Fact]
    public void ScaleImage_FitWidth_ProducesExactCanvas()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = SkiaSharpHelper.ScaleImage(source, 4, 4);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    [Fact]
    public void ScaleImage_FitHeight_ProducesExactCanvas()
    {
        using var source = CreateSampleImage(4, 8);
        using var result = SkiaSharpHelper.ScaleImage(source, 4, 4);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    [Fact]
    public void ScaleImage_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ScaleImage(null!, 4, 4));
    }

    #endregion

    #region Gray

    [Fact]
    public void Gray_ReturnsSameSize()
    {
        using var source = CreateSampleImage();
        using var result = SkiaSharpHelper.Gray(source);
        result.Width.ShouldBe(source.Width);
        result.Height.ShouldBe(source.Height);
    }

    [Fact]
    public void Gray_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.Gray(null!));
    }

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

    [Fact]
    public void ToBlackWhiteImage_ReturnsSameSize()
    {
        using var source = CreateSampleImage();
        using var result = SkiaSharpHelper.ToBlackWhiteImage(source);
        result.Width.ShouldBe(source.Width);
        result.Height.ShouldBe(source.Height);
    }

    [Fact]
    public void ToBlackWhiteImage_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToBlackWhiteImage(null!));
    }

    #endregion

    #region FilterColor

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

    [Fact]
    public void FilterColor_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.FilterColor(null!));
    }

    #endregion

    #region Plate

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

    [Fact]
    public void Plate_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.Plate(null!));
    }

    #endregion

    #region PerPixelProcess

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

    [Fact]
    public void PerPixelProcess_DoesNotMutateSource()
    {
        using var source = CreateSampleImage(1, 1, new SKColor(100, 100, 100));
        using var result = SkiaSharpHelper.PerPixelProcess(source, color => new SKColor(255, 0, 0, color.Alpha));
        using var bitmap = SKBitmap.FromImage(source);
        bitmap.GetPixel(0, 0).Red.ShouldBe((byte)100);
    }

    [Fact]
    public void PerPixelProcess_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.PerPixelProcess(null!, c => c));
    }

    #endregion

    #region ColorExtensions

    [Fact]
    public void GetGrayScale_White_ReturnsOne()
    {
        SkiaSharpHelper.GetGrayScale(SKColors.White).ShouldBe(1.0f, 0.01f);
    }

    [Fact]
    public void GetGrayScale_Black_ReturnsZero()
    {
        SkiaSharpHelper.GetGrayScale(SKColors.Black).ShouldBe(0.0f, 0.01f);
    }

    [Fact]
    public void ColorDifference_SameColor_ReturnsZero()
    {
        var c = new SKColor(100, 150, 200);
        SkiaSharpHelper.ColorDifference(c, c).ShouldBe(0.0, 0.001);
    }

    [Fact]
    public void ColorDifference_IsSymmetric()
    {
        var a = new SKColor(200, 100, 50);
        var b = new SKColor(50, 100, 200);
        SkiaSharpHelper.ColorDifference(a, b).ShouldBe(SkiaSharpHelper.ColorDifference(b, a), 0.001);
    }

    [Fact]
    public void IsSimilarColors_SameColor_ReturnsTrue()
    {
        var c = new SKColor(100, 150, 200);
        SkiaSharpHelper.IsSimilarColors(c, c).ShouldBeTrue();
    }

    [Fact]
    public void IsSimilarColors_DifferentAlpha_ReturnsFalse()
    {
        var a = new SKColor(128, 128, 128, 100);
        var b = new SKColor(128, 128, 128, 200);
        SkiaSharpHelper.IsSimilarColors(a, b).ShouldBeFalse();
    }

    [Fact]
    public void Blend_Amount1_ReturnsOriginal()
    {
        var foreground = new SKColor(200, 100, 50);
        var background = new SKColor(50, 100, 200);
        var result = SkiaSharpHelper.Blend(foreground, background, 1.0);
        result.Red.ShouldBe((byte)200);
    }

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

    [Fact]
    public void ColorMatrices_CreateBrightnessFilter_Amount1_DiagonalIs1()
    {
        var matrix = ColorMatrices.CreateBrightnessFilter(1f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[1, 1].ShouldBe(1f, 0.001f);
        matrix[2, 2].ShouldBe(1f, 0.001f);
        matrix[3, 3].ShouldBe(1f, 0.001f);
    }

    [Fact]
    public void ColorMatrices_CreateBrightnessFilter_NegativeAmount_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateBrightnessFilter(-0.1f));
    }

    [Fact]
    public void ColorMatrices_CreateContrastFilter_Amount1_DiagonalIs1()
    {
        var matrix = ColorMatrices.CreateContrastFilter(1f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[4, 0].ShouldBe(0f, 0.001f);
    }

    [Fact]
    public void ColorMatrices_CreateGrayScaleFilter_Amount0_ReturnsIdentityLike()
    {
        var matrix = ColorMatrices.CreateGrayScaleFilter(0f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[3, 3].ShouldBe(1f, 0.001f);
    }

    [Fact]
    public void ColorMatrices_CreateSaturationFilter_Amount1_ReturnsIdentityLike()
    {
        var matrix = ColorMatrices.CreateSaturationFilter(1f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[3, 3].ShouldBe(1f, 0.001f);
    }

    [Fact]
    public void ColorMatrices_CreateGrayScaleFilter_OutOfRange_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateGrayScaleFilter(-0.1f));
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateGrayScaleFilter(1.1f));
    }

    [Fact]
    public void ColorMatrices_CreateContrastFilter_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateContrastFilter(-0.1f));
    }

    [Fact]
    public void ColorMatrices_CreateSaturationFilter_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateSaturationFilter(-0.1f));
    }

    #endregion

    #region Helper

    private static SKImage CreateSampleImage(int width = 4, int height = 4, SKColor? fill = null)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(fill ?? new SKColor(128, 128, 128));
        return SKImage.FromBitmap(bitmap);
    }

    #endregion
}
