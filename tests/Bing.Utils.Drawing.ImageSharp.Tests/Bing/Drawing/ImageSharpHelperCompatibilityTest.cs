using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 Phase 1 兼容层 API。
/// </summary>
[Trait("Drawing", "ImageSharp.Compatibility")]
public class ImageSharpHelperCompatibilityTest
{
    #region ToStream

    [Fact]
    public void ToStream_ReturnsReadableStreamAtPositionZero()
    {
        using var source = CreateSampleImage();
        using var stream = ImageSharpHelper.ToStream(source);
        stream.Position.ShouldBe(0);
        stream.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ToStream_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToStream(null!));
    }

    [Fact]
    public void ToStream_CanRoundTripWithFromStream()
    {
        using var source = CreateSampleImage();
        using var stream = ImageSharpHelper.ToStream(source);
        using var restored = ImageSharpHelper.FromStream(stream);
        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(source.Width);
        restored.Height.ShouldBe(source.Height);
    }

    #endregion

    #region MakeThumbnail

    [Fact]
    public void MakeThumbnail_FixedBoth_ExactDimensions()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = ImageSharpHelper.MakeThumbnail(source, 4, 4, ThumbnailMode.FixedBoth);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    [Fact]
    public void MakeThumbnail_FixedW_HeightAuto()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = ImageSharpHelper.MakeThumbnail(source, 4, 10, ThumbnailMode.FixedW);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(2);
    }

    [Fact]
    public void MakeThumbnail_FixedH_WidthAuto()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = ImageSharpHelper.MakeThumbnail(source, 10, 2, ThumbnailMode.FixedH);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(2);
    }

    [Fact]
    public void MakeThumbnail_Cut_CropsToAspectRatio()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = ImageSharpHelper.MakeThumbnail(source, 4, 4, ThumbnailMode.Cut);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    [Fact]
    public void MakeThumbnail_NullSource_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.MakeThumbnail(null!, 4, 4, ThumbnailMode.FixedBoth));
    }

    #endregion

    #region ScaleImage

    [Fact]
    public void ScaleImage_FitWidth_ProducesExactCanvas()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = ImageSharpHelper.ScaleImage(source, 4, 4);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    [Fact]
    public void ScaleImage_FitHeight_ProducesExactCanvas()
    {
        using var source = CreateSampleImage(4, 8);
        using var result = ImageSharpHelper.ScaleImage(source, 4, 4);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    [Fact]
    public void ScaleImage_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ScaleImage(null!, 4, 4));
    }

    #endregion

    #region Gray

    [Fact]
    public void Gray_ReturnsSameSizeWithGrayPixels()
    {
        using var source = CreateSampleImage();
        using var result = ImageSharpHelper.Gray(source);
        result.Width.ShouldBe(source.Width);
        result.Height.ShouldBe(source.Height);
    }

    [Fact]
    public void Gray_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.Gray(null!));
    }

    [Fact]
    public void Gray_WhitePixel_PreservesWhite()
    {
        using var source = CreateSampleImage(1, 1, Color.White);
        using var result = ImageSharpHelper.Gray(source);
        var pixel = result.CloneAs<Rgba32>()[0, 0];
        pixel.R.ShouldBe((byte)255);
        pixel.G.ShouldBe((byte)255);
        pixel.B.ShouldBe((byte)255);
    }

    #endregion

    #region ToBlackWhiteImage

    [Fact]
    public void ToBlackWhiteImage_ReturnsSameSize()
    {
        using var source = CreateSampleImage();
        using var result = ImageSharpHelper.ToBlackWhiteImage(source);
        result.Width.ShouldBe(source.Width);
        result.Height.ShouldBe(source.Height);
    }

    [Fact]
    public void ToBlackWhiteImage_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToBlackWhiteImage(null!));
    }

    #endregion

    #region FilterColor

    [Fact]
    public void FilterColor_RemovesRedChannel()
    {
        using var source = CreateSampleImage(1, 1, Color.FromRgb(200, 100, 50));
        using var result = ImageSharpHelper.FilterColor(source);
        var pixel = result.CloneAs<Rgba32>()[0, 0];
        pixel.R.ShouldBe((byte)0);
        pixel.G.ShouldBe((byte)100);
        pixel.B.ShouldBe((byte)50);
    }

    [Fact]
    public void FilterColor_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.FilterColor(null!));
    }

    #endregion

    #region Plate

    [Fact]
    public void Plate_InvertsRgbChannels()
    {
        using var source = CreateSampleImage(1, 1, Color.FromRgb(100, 150, 200));
        using var result = ImageSharpHelper.Plate(source);
        var pixel = result.CloneAs<Rgba32>()[0, 0];
        pixel.R.ShouldBe((byte)155);
        pixel.G.ShouldBe((byte)105);
        pixel.B.ShouldBe((byte)55);
    }

    [Fact]
    public void Plate_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.Plate(null!));
    }

    #endregion

    #region PerPixelProcess

    [Fact]
    public void PerPixelProcess_TransformsAllPixels()
    {
        using var source = CreateSampleImage(2, 2, Color.FromRgb(100, 100, 100));
        using var result = ImageSharpHelper.PerPixelProcess(source, color => new Rgba32(255, 0, 0, color.A));
        var pixel = result.CloneAs<Rgba32>()[0, 0];
        pixel.R.ShouldBe((byte)255);
        pixel.G.ShouldBe((byte)0);
        pixel.B.ShouldBe((byte)0);
    }

    [Fact]
    public void PerPixelProcess_DoesNotMutateSource()
    {
        using var source = CreateSampleImage(1, 1, Color.FromRgb(100, 100, 100));
        using var result = ImageSharpHelper.PerPixelProcess(source, color => new Rgba32(255, 0, 0, color.A));
        var original = source.CloneAs<Rgba32>()[0, 0];
        original.R.ShouldBe((byte)100);
    }

    [Fact]
    public void PerPixelProcess_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.PerPixelProcess(null!, c => c));
    }

    #endregion

    #region ColorExtensions

    [Fact]
    public void GetGrayScale_White_ReturnsOne()
    {
        ImageSharpHelper.GetGrayScale(Color.White).ShouldBe(1.0f, 0.01f);
    }

    [Fact]
    public void GetGrayScale_Black_ReturnsZero()
    {
        ImageSharpHelper.GetGrayScale(Color.Black).ShouldBe(0.0f, 0.01f);
    }

    [Fact]
    public void ColorDifference_SameColor_ReturnsZero()
    {
        var c = Color.FromRgb(100, 150, 200);
        ImageSharpHelper.ColorDifference(c, c).ShouldBe(0.0, 0.001);
    }

    [Fact]
    public void ColorDifference_IsSymmetric()
    {
        var a = Color.FromRgb(200, 100, 50);
        var b = Color.FromRgb(50, 100, 200);
        ImageSharpHelper.ColorDifference(a, b).ShouldBe(ImageSharpHelper.ColorDifference(b, a), 0.001);
    }

    [Fact]
    public void IsSimilarColors_SameColor_ReturnsTrue()
    {
        var c = Color.FromRgb(100, 150, 200);
        ImageSharpHelper.IsSimilarColors(c, c).ShouldBeTrue();
    }

    [Fact]
    public void IsSimilarColors_DifferentAlpha_ReturnsFalse()
    {
        var a = Color.FromRgba(100, 128, 128, 128);
        var b = Color.FromRgba(200, 128, 128, 128);
        ImageSharpHelper.IsSimilarColors(a, b).ShouldBeFalse();
    }

    [Fact]
    public void Blend_Amount1_ReturnsOriginal()
    {
        var foreground = Color.FromRgb(200, 100, 50);
        var background = Color.FromRgb(50, 100, 200);
        var result = ImageSharpHelper.Blend(foreground, background, 1.0);
        result.ToPixel<Rgba32>().R.ShouldBe((byte)200);
    }

    [Fact]
    public void Blend_Amount0_ReturnsBackground()
    {
        var foreground = Color.FromRgb(200, 100, 50);
        var background = Color.FromRgb(50, 100, 200);
        var result = ImageSharpHelper.Blend(foreground, background, 0.0);
        result.ToPixel<Rgba32>().R.ShouldBe((byte)50);
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

    private static Image CreateSampleImage(int width = 4, int height = 4, Color? fill = null)
    {
        var image = new Image<Rgba32>(width, height);
        var fillColor = fill ?? Color.FromRgb(128, 128, 128);
        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
            image[x, y] = fillColor.ToPixel<Rgba32>();
        return image;
    }

    #endregion
}
