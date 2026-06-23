using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 OCR 预处理 API。
/// </summary>
[Trait("Drawing", "SkiaSharp.Ocr")]
public class SkiaSharpHelperOcrTest
{
    #region ToGrayArray2D

    [Fact]
    public void ToGrayArray2D_ReturnsCorrectDimensions()
    {
        using var image = CreateTestImage(5, 3);
        var gray = SkiaSharpHelper.ToGrayArray2D(image);
        gray.GetLength(0).ShouldBe(5);
        gray.GetLength(1).ShouldBe(3);
    }

    [Fact]
    public void ToGrayArray2D_WhiteImage_Returns255()
    {
        using var image = CreateTestImage(1, 1, SKColors.White);
        var gray = SkiaSharpHelper.ToGrayArray2D(image);
        gray[0, 0].ShouldBe((byte)255);
    }

    [Fact]
    public void ToGrayArray2D_BlackImage_Returns0()
    {
        using var image = CreateTestImage(1, 1, SKColors.Black);
        var gray = SkiaSharpHelper.ToGrayArray2D(image);
        gray[0, 0].ShouldBe((byte)0);
    }

    [Fact]
    public void ToGrayArray2D_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToGrayArray2D(null!));
    }

    #endregion

    #region ToBinaryArray2D

    [Fact]
    public void ToBinaryArray2D_WhiteImage_ReturnsAllBackground()
    {
        using var image = CreateTestImage(2, 2, SKColors.White);
        var bin = SkiaSharpHelper.ToBinaryArray2D(image, 128);
        bin[0, 0].ShouldBe((byte)255);
        bin[1, 1].ShouldBe((byte)255);
    }

    [Fact]
    public void ToBinaryArray2D_BlackImage_ReturnsAllForeground()
    {
        using var image = CreateTestImage(2, 2, SKColors.Black);
        var bin = SkiaSharpHelper.ToBinaryArray2D(image, 128);
        bin[0, 0].ShouldBe((byte)0);
        bin[1, 1].ShouldBe((byte)0);
    }

    #endregion

    #region Binaryzation

    [Fact]
    public void Binaryzation_ReturnsLoadableImage()
    {
        using var image = CreateTestImage(4, 4, new SKColor(100, 100, 100));
        using var result = SkiaSharpHelper.Binaryzation(image, 128);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    [Fact]
    public void Binaryzation_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.Binaryzation(null!, 128));
    }

    #endregion

    #region DeepenForeground

    [Fact]
    public void DeepenForeground_DarkPixels_BecomeBlack()
    {
        using var image = CreateTestImage(1, 1, new SKColor(50, 50, 50));
        using var result = SkiaSharpHelper.DeepenForeground(image, 200);
        using var bitmap = SKBitmap.FromImage(result);
        var pixel = bitmap.GetPixel(0, 0);
        pixel.Red.ShouldBe((byte)0);
    }

    [Fact]
    public void DeepenForeground_BrightPixels_Preserved()
    {
        using var image = CreateTestImage(1, 1, new SKColor(220, 220, 220));
        using var result = SkiaSharpHelper.DeepenForeground(image, 200);
        using var bitmap = SKBitmap.FromImage(result);
        bitmap.GetPixel(0, 0).Red.ShouldBe((byte)220);
    }

    #endregion

    #region CreateImageFromGrayArray

    [Fact]
    public void CreateImageFromGrayArray_CreatesCorrectSize()
    {
        var gray = new byte[3, 4];
        using var image = SkiaSharpHelper.CreateImageFromGrayArray(gray);
        image.Width.ShouldBe(3);
        image.Height.ShouldBe(4);
        image.Dispose();
    }

    [Fact]
    public void CreateImageFromGrayArray_NullArray_Throws()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.CreateImageFromGrayArray(null!));
    }

    #endregion

    #region Projection

    [Fact]
    public void GetVerticalProjection_BlackImage_AllNonZero()
    {
        using var image = CreateTestImage(3, 1, SKColors.Black);
        var proj = SkiaSharpHelper.GetVerticalProjection(image, 128);
        proj.Length.ShouldBe(3);
        proj[0].ShouldBeGreaterThan(0);
        proj[1].ShouldBeGreaterThan(0);
        proj[2].ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetHorizontalProjection_WhiteImage_AllZero()
    {
        using var image = CreateTestImage(1, 3, SKColors.White);
        var proj = SkiaSharpHelper.GetHorizontalProjection(image, 128);
        proj.Length.ShouldBe(3);
        proj[0].ShouldBe(0);
        proj[1].ShouldBe(0);
        proj[2].ShouldBe(0);
    }

    #endregion

    #region ClearBorder / AddBorder / TrimToContent

    [Fact]
    public void TrimToContent_BlackCenter_CropsWhiteBorder()
    {
        // [5,5] white with black center
        var bin = new byte[,]
        {
            { 255, 255, 255, 255, 255 },
            { 255, 0, 0, 255, 255 },
            { 255, 0, 0, 255, 255 },
            { 255, 255, 255, 255, 255 },
            { 255, 255, 255, 255, 255 }
        };
        using var image = SkiaSharpHelper.CreateImageFromBinaryArray(bin);
        using var result = SkiaSharpHelper.TrimToContent(image, 128);
        result.Width.ShouldBe(2);
        result.Height.ShouldBe(2);
    }

    #endregion

    #region Helper

    private static SKImage CreateTestImage(int width, int height, SKColor? fill = null)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(fill ?? new SKColor(128, 128, 128));
        return SKImage.FromBitmap(bitmap);
    }

    #endregion
}
