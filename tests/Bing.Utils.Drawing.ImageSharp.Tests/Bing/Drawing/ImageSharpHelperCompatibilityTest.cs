using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 验证图像兼容接口的缩放、像素效果及颜色矩阵行为。
/// </summary>
[Trait("Drawing", "ImageSharp.Compatibility")]
public class ImageSharpHelperCompatibilityTest
{
    #region ToStream

    /// <summary>
    /// 验证编码流包含图像数据且初始位置为零。
    /// </summary>
    [Fact]
    public void ToStream_ReturnsReadableStreamAtPositionZero()
    {
        using var source = CreateSampleImage();
        using var stream = ImageSharpHelper.ToStream(source);
        stream.Position.ShouldBe(0);
        stream.Length.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// 验证图像流编码拒绝空图像。
    /// </summary>
    [Fact]
    public void ToStream_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToStream(null!));
    }

    /// <summary>
    /// 验证图像经流编码及解码后保持尺寸。
    /// </summary>
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

    /// <summary>
    /// 验证固定宽高缩略图使用指定尺寸。
    /// </summary>
    [Fact]
    public void MakeThumbnail_FixedBoth_ExactDimensions()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = ImageSharpHelper.MakeThumbnail(source, 4, 4, ThumbnailMode.FixedBoth);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证固定宽度缩略图按原始比例计算高度。
    /// </summary>
    [Fact]
    public void MakeThumbnail_FixedW_HeightAuto()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = ImageSharpHelper.MakeThumbnail(source, 4, 10, ThumbnailMode.FixedW);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(2);
    }

    /// <summary>
    /// 验证固定高度缩略图按原始比例计算宽度。
    /// </summary>
    [Fact]
    public void MakeThumbnail_FixedH_WidthAuto()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = ImageSharpHelper.MakeThumbnail(source, 10, 2, ThumbnailMode.FixedH);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(2);
    }

    /// <summary>
    /// 验证裁剪缩略图输出指定宽高。
    /// </summary>
    [Fact]
    public void MakeThumbnail_Cut_CropsToAspectRatio()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = ImageSharpHelper.MakeThumbnail(source, 4, 4, ThumbnailMode.Cut);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证缩略图生成拒绝空源图像。
    /// </summary>
    [Fact]
    public void MakeThumbnail_NullSource_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.MakeThumbnail((Image)null!, 4, 4, ThumbnailMode.FixedBoth));
    }

    #endregion

    #region ScaleImage

    /// <summary>
    /// 验证横向源图像缩放后使用指定画布尺寸。
    /// </summary>
    [Fact]
    public void ScaleImage_FitWidth_ProducesExactCanvas()
    {
        using var source = CreateSampleImage(8, 4);
        using var result = ImageSharpHelper.ScaleImage(source, 4, 4);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证纵向源图像缩放后使用指定画布尺寸。
    /// </summary>
    [Fact]
    public void ScaleImage_FitHeight_ProducesExactCanvas()
    {
        using var source = CreateSampleImage(4, 8);
        using var result = ImageSharpHelper.ScaleImage(source, 4, 4);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证画布缩放拒绝空图像。
    /// </summary>
    [Fact]
    public void ScaleImage_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ScaleImage(null!, 4, 4));
    }

    #endregion

    #region Gray

    /// <summary>
    /// 验证灰度转换保持源图像尺寸。
    /// </summary>
    [Fact]
    public void Gray_ReturnsSameSizeWithGrayPixels()
    {
        using var source = CreateSampleImage();
        using var result = ImageSharpHelper.Gray(source);
        result.Width.ShouldBe(source.Width);
        result.Height.ShouldBe(source.Height);
    }

    /// <summary>
    /// 验证灰度转换拒绝空图像。
    /// </summary>
    [Fact]
    public void Gray_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.Gray(null!));
    }

    /// <summary>
    /// 验证灰度转换保留纯白像素的三个颜色通道。
    /// </summary>
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

    /// <summary>
    /// 验证黑白图像转换保持源图像尺寸。
    /// </summary>
    [Fact]
    public void ToBlackWhiteImage_ReturnsSameSize()
    {
        using var source = CreateSampleImage();
        using var result = ImageSharpHelper.ToBlackWhiteImage(source);
        result.Width.ShouldBe(source.Width);
        result.Height.ShouldBe(source.Height);
    }

    /// <summary>
    /// 验证黑白图像转换拒绝空图像。
    /// </summary>
    [Fact]
    public void ToBlackWhiteImage_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToBlackWhiteImage(null!));
    }

    #endregion

    #region FilterColor

    /// <summary>
    /// 验证颜色过滤清除红色通道且保留绿色与蓝色通道。
    /// </summary>
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

    /// <summary>
    /// 验证颜色过滤拒绝空图像。
    /// </summary>
    [Fact]
    public void FilterColor_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.FilterColor(null!));
    }

    #endregion

    #region Plate

    /// <summary>
    /// 验证反色效果分别反转三个颜色通道。
    /// </summary>
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

    /// <summary>
    /// 验证反色效果拒绝空图像。
    /// </summary>
    [Fact]
    public void Plate_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.Plate(null!));
    }

    #endregion

    #region PerPixelProcess

    /// <summary>
    /// 验证逐像素处理结果的采样点被替换为纯红色。
    /// </summary>
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

    /// <summary>
    /// 验证逐像素处理不改变源图像的采样像素。
    /// </summary>
    [Fact]
    public void PerPixelProcess_DoesNotMutateSource()
    {
        using var source = CreateSampleImage(1, 1, Color.FromRgb(100, 100, 100));
        using var result = ImageSharpHelper.PerPixelProcess(source, color => new Rgba32(255, 0, 0, color.A));
        var original = source.CloneAs<Rgba32>()[0, 0];
        original.R.ShouldBe((byte)100);
    }

    /// <summary>
    /// 验证逐像素处理拒绝空图像。
    /// </summary>
    [Fact]
    public void PerPixelProcess_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.PerPixelProcess(null!, c => c));
    }

    #endregion

    #region ColorExtensions

    /// <summary>
    /// 验证白色的归一化灰度为一。
    /// </summary>
    [Fact]
    public void GetGrayScale_White_ReturnsOne()
    {
        ImageSharpHelper.GetGrayScale(Color.White).ShouldBe(1.0f, 0.01f);
    }

    /// <summary>
    /// 验证黑色的归一化灰度为零。
    /// </summary>
    [Fact]
    public void GetGrayScale_Black_ReturnsZero()
    {
        ImageSharpHelper.GetGrayScale(Color.Black).ShouldBe(0.0f, 0.01f);
    }

    /// <summary>
    /// 验证相同颜色的色差为零。
    /// </summary>
    [Fact]
    public void ColorDifference_SameColor_ReturnsZero()
    {
        var c = Color.FromRgb(100, 150, 200);
        ImageSharpHelper.ColorDifference(c, c).ShouldBe(0.0, 0.001);
    }

    /// <summary>
    /// 验证交换两个颜色不改变色差结果。
    /// </summary>
    [Fact]
    public void ColorDifference_IsSymmetric()
    {
        var a = Color.FromRgb(200, 100, 50);
        var b = Color.FromRgb(50, 100, 200);
        ImageSharpHelper.ColorDifference(a, b).ShouldBe(ImageSharpHelper.ColorDifference(b, a), 0.001);
    }

    /// <summary>
    /// 验证相同颜色被识别为相似。
    /// </summary>
    [Fact]
    public void IsSimilarColors_SameColor_ReturnsTrue()
    {
        var c = Color.FromRgb(100, 150, 200);
        ImageSharpHelper.IsSimilarColors(c, c).ShouldBeTrue();
    }

    /// <summary>
    /// 验证红色通道差异较大的两个颜色不被识别为相似。
    /// </summary>
    [Fact]
    public void IsSimilarColors_DifferentAlpha_ReturnsFalse()
    {
        var a = Color.FromRgba(100, 128, 128, 128);
        var b = Color.FromRgba(200, 128, 128, 128);
        ImageSharpHelper.IsSimilarColors(a, b).ShouldBeFalse();
    }

    /// <summary>
    /// 验证混合比例为一时红色通道取前景值。
    /// </summary>
    [Fact]
    public void Blend_Amount1_ReturnsOriginal()
    {
        var foreground = Color.FromRgb(200, 100, 50);
        var background = Color.FromRgb(50, 100, 200);
        var result = ImageSharpHelper.Blend(foreground, background, 1.0);
        result.ToPixel<Rgba32>().R.ShouldBe((byte)200);
    }

    /// <summary>
    /// 验证混合比例为零时红色通道取背景值。
    /// </summary>
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

    /// <summary>
    /// 验证单位亮度矩阵的颜色及透明度主对角元素为一。
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
    /// 验证亮度矩阵生成拒绝负系数。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateBrightnessFilter_NegativeAmount_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateBrightnessFilter(-0.1f));
    }

    /// <summary>
    /// 验证单位对比度矩阵保留红色通道系数且不增加偏移。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateContrastFilter_Amount1_DiagonalIs1()
    {
        var matrix = ColorMatrices.CreateContrastFilter(1f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[4, 0].ShouldBe(0f, 0.001f);
    }

    /// <summary>
    /// 验证零灰度比例矩阵保留红色及透明度通道系数。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateGrayScaleFilter_Amount0_ReturnsIdentityLike()
    {
        var matrix = ColorMatrices.CreateGrayScaleFilter(0f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[3, 3].ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 验证单位饱和度矩阵保留红色及透明度通道系数。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateSaturationFilter_Amount1_ReturnsIdentityLike()
    {
        var matrix = ColorMatrices.CreateSaturationFilter(1f);
        matrix[0, 0].ShouldBe(1f, 0.001f);
        matrix[3, 3].ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 验证灰度矩阵生成拒绝零至一范围外的比例。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateGrayScaleFilter_OutOfRange_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateGrayScaleFilter(-0.1f));
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateGrayScaleFilter(1.1f));
    }

    /// <summary>
    /// 验证对比度矩阵生成拒绝负系数。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateContrastFilter_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateContrastFilter(-0.1f));
    }

    /// <summary>
    /// 验证饱和度矩阵生成拒绝负系数。
    /// </summary>
    [Fact]
    public void ColorMatrices_CreateSaturationFilter_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateSaturationFilter(-0.1f));
    }

    #endregion

    #region Helper

    /// <summary>
    /// 创建指定尺寸及填充颜色的兼容接口测试图像。
    /// </summary>
    /// <param name="width">样例图像的宽度，单位为像素，默认为 4。</param>
    /// <param name="height">样例图像的高度，单位为像素，默认为 4。</param>
    /// <param name="fill">填充颜色；为 null 时使用红绿蓝通道均为 128 的灰色。</param>
    /// <returns>指定尺寸的纯色图像，调用方负责释放。</returns>
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
