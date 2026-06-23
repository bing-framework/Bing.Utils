using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 OCR 预处理 API。
/// </summary>
[Trait("Drawing", "ImageSharp.Ocr")]
public class ImageSharpHelperOcrTest
{
    #region ToGrayArray2D

    [Fact]
    public void ToGrayArray2D_ReturnsCorrectDimensions()
    {
        using var image = CreateTestImage(5, 3);
        var gray = ImageSharpHelper.ToGrayArray2D(image);
        gray.GetLength(0).ShouldBe(5);
        gray.GetLength(1).ShouldBe(3);
    }

    [Fact]
    public void ToGrayArray2D_WhiteImage_Returns255()
    {
        using var image = CreateTestImage(1, 1, Color.White);
        var gray = ImageSharpHelper.ToGrayArray2D(image);
        gray[0, 0].ShouldBe((byte)255);
    }

    [Fact]
    public void ToGrayArray2D_BlackImage_Returns0()
    {
        using var image = CreateTestImage(1, 1, Color.Black);
        var gray = ImageSharpHelper.ToGrayArray2D(image);
        gray[0, 0].ShouldBe((byte)0);
    }

    [Fact]
    public void ToGrayArray2D_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToGrayArray2D(null!));
    }

    #endregion

    #region ToBinaryArray2D

    [Fact]
    public void ToBinaryArray2D_WhiteImage_ReturnsAllBackground()
    {
        using var image = CreateTestImage(2, 2, Color.White);
        var bin = ImageSharpHelper.ToBinaryArray2D(image, 128);
        bin[0, 0].ShouldBe((byte)255);
        bin[1, 1].ShouldBe((byte)255);
    }

    [Fact]
    public void ToBinaryArray2D_BlackImage_ReturnsAllForeground()
    {
        using var image = CreateTestImage(2, 2, Color.Black);
        var bin = ImageSharpHelper.ToBinaryArray2D(image, 128);
        bin[0, 0].ShouldBe((byte)0);
        bin[1, 1].ShouldBe((byte)0);
    }

    #endregion

    #region Binaryzation

    [Fact]
    public void Binaryzation_ReturnsLoadableImage()
    {
        using var image = CreateTestImage(4, 4, Color.FromRgb(100, 100, 100));
        using var result = ImageSharpHelper.Binaryzation(image, 128);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    [Fact]
    public void Binaryzation_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.Binaryzation(null!, 128));
    }

    #endregion

    #region DeepenForeground

    [Fact]
    public void DeepenForeground_DarkPixels_BecomeBlack()
    {
        using var image = CreateTestImage(1, 1, Color.FromRgb(50, 50, 50));
        using var result = ImageSharpHelper.DeepenForeground(image, 200);
        var pixel = result.CloneAs<Rgba32>()[0, 0];
        pixel.R.ShouldBe((byte)0);
        pixel.G.ShouldBe((byte)0);
        pixel.B.ShouldBe((byte)0);
    }

    [Fact]
    public void DeepenForeground_BrightPixels_Preserved()
    {
        using var image = CreateTestImage(1, 1, Color.FromRgb(220, 220, 220));
        using var result = ImageSharpHelper.DeepenForeground(image, 200);
        var pixel = result.CloneAs<Rgba32>()[0, 0];
        pixel.R.ShouldBe((byte)220);
    }

    #endregion

    #region CreateImageFromGrayArray

    [Fact]
    public void CreateImageFromGrayArray_CreatesCorrectSize()
    {
        var gray = new byte[3, 4];
        using var image = ImageSharpHelper.CreateImageFromGrayArray(gray);
        image.Width.ShouldBe(3);
        image.Height.ShouldBe(4);
    }

    [Fact]
    public void CreateImageFromGrayArray_NullArray_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.CreateImageFromGrayArray(null!));
    }

    #endregion

    #region Projection

    [Fact]
    public void GetVerticalProjection_BlackImage_AllNonZero()
    {
        using var image = CreateTestImage(3, 1, Color.Black);
        var proj = ImageSharpHelper.GetVerticalProjection(image, 128);
        proj.Length.ShouldBe(3);
        proj[0].ShouldBeGreaterThan(0);
        proj[1].ShouldBeGreaterThan(0);
        proj[2].ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetHorizontalProjection_WhiteImage_AllZero()
    {
        using var image = CreateTestImage(1, 3, Color.White);
        var proj = ImageSharpHelper.GetHorizontalProjection(image, 128);
        proj.Length.ShouldBe(3);
        proj[0].ShouldBe(0);
        proj[1].ShouldBe(0);
        proj[2].ShouldBe(0);
    }

    #endregion

    #region Helper

    private static Image CreateTestImage(int width, int height, Color? fill = null)
    {
        var image = new Image<Rgba32>(width, height);
        var fillColor = fill ?? Color.FromRgb(128, 128, 128);
        var pixel = fillColor.ToPixel<Rgba32>();
        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
            image[x, y] = pixel;
        return image;
    }

    #endregion
}
