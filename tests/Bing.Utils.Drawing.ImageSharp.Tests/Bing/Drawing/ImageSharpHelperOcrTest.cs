using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 验证 OCR 图像预处理的灰度、二值化、前景加深及投影。
/// </summary>
[Trait("Drawing", "ImageSharp.Ocr")]
public class ImageSharpHelperOcrTest
{
    #region ToGrayArray2D

    /// <summary>
    /// 验证灰度矩阵的两个维度对应图像宽高。
    /// </summary>
    [Fact]
    public void ToGrayArray2D_ReturnsCorrectDimensions()
    {
        using var image = CreateTestImage(5, 3);
        var gray = ImageSharpHelper.ToGrayArray2D(image);
        gray.GetLength(0).ShouldBe(5);
        gray.GetLength(1).ShouldBe(3);
    }

    /// <summary>
    /// 验证白色像素转为灰度值 255。
    /// </summary>
    [Fact]
    public void ToGrayArray2D_WhiteImage_Returns255()
    {
        using var image = CreateTestImage(1, 1, Color.White);
        var gray = ImageSharpHelper.ToGrayArray2D(image);
        gray[0, 0].ShouldBe((byte)255);
    }

    /// <summary>
    /// 验证黑色像素转为灰度值零。
    /// </summary>
    [Fact]
    public void ToGrayArray2D_BlackImage_Returns0()
    {
        using var image = CreateTestImage(1, 1, Color.Black);
        var gray = ImageSharpHelper.ToGrayArray2D(image);
        gray[0, 0].ShouldBe((byte)0);
    }

    /// <summary>
    /// 验证灰度矩阵转换拒绝空图像。
    /// </summary>
    [Fact]
    public void ToGrayArray2D_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToGrayArray2D(null!));
    }

    #endregion

    #region ToBinaryArray2D

    /// <summary>
    /// 验证白色图像的二值矩阵采样点均为背景值 255。
    /// </summary>
    [Fact]
    public void ToBinaryArray2D_WhiteImage_ReturnsAllBackground()
    {
        using var image = CreateTestImage(2, 2, Color.White);
        var bin = ImageSharpHelper.ToBinaryArray2D(image, 128);
        bin[0, 0].ShouldBe((byte)255);
        bin[1, 1].ShouldBe((byte)255);
    }

    /// <summary>
    /// 验证黑色图像的二值矩阵采样点均为前景值零。
    /// </summary>
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

    /// <summary>
    /// 验证二值化处理保持图像尺寸。
    /// </summary>
    [Fact]
    public void Binaryzation_ReturnsLoadableImage()
    {
        using var image = CreateTestImage(4, 4, Color.FromRgb(100, 100, 100));
        using var result = ImageSharpHelper.Binaryzation(image, 128);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证二值化处理拒绝空图像。
    /// </summary>
    [Fact]
    public void Binaryzation_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.Binaryzation(null!, 128));
    }

    #endregion

    #region DeepenForeground

    /// <summary>
    /// 验证低于阈值的暗色像素被加深为黑色。
    /// </summary>
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

    /// <summary>
    /// 验证高于阈值的亮色像素保留原始红色通道值。
    /// </summary>
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

    /// <summary>
    /// 验证灰度矩阵生成图像的尺寸与矩阵维度一致。
    /// </summary>
    [Fact]
    public void CreateImageFromGrayArray_CreatesCorrectSize()
    {
        var gray = new byte[3, 4];
        using var image = ImageSharpHelper.CreateImageFromGrayArray(gray);
        image.Width.ShouldBe(3);
        image.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证灰度图像生成拒绝空矩阵。
    /// </summary>
    [Fact]
    public void CreateImageFromGrayArray_NullArray_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.CreateImageFromGrayArray(null!));
    }

    #endregion

    #region Projection

    /// <summary>
    /// 验证黑色图像每列的前景投影计数为正。
    /// </summary>
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

    /// <summary>
    /// 验证白色图像每行的前景投影计数为零。
    /// </summary>
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

    /// <summary>
    /// 创建指定尺寸及填充颜色的 OCR 测试图像。
    /// </summary>
    /// <param name="width">样例图像的宽度，单位为像素。</param>
    /// <param name="height">样例图像的高度，单位为像素。</param>
    /// <param name="fill">填充颜色；为 null 时使用红绿蓝通道均为 128 的灰色。</param>
    /// <returns>指定尺寸的纯色图像，调用方负责释放。</returns>
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
