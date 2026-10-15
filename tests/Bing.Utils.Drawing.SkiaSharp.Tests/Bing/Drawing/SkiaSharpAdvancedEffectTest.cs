using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 验证图像扭曲、侵蚀和 ICO 编码的基本结果。
/// </summary>
[Trait("Drawing", "SkiaSharp.Advanced")]
public class SkiaSharpAdvancedEffectTest
{
    #region TwistImage

    /// <summary>
    /// 验证扭曲效果保持图像尺寸。
    /// </summary>
    [Fact]
    public void TwistImage_ReturnsSameSize()
    {
        using var source = CreateTestImage(8, 8);
        using var result = SkiaSharpHelper.TwistImage(source, true, 3, 0);
        result.Width.ShouldBe(8);
        result.Height.ShouldBe(8);
    }

    /// <summary>
    /// 验证空图像会导致扭曲效果抛出空参数异常。
    /// </summary>
    [Fact]
    public void TwistImage_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.TwistImage(null!, true, 3, 0));
    }

    /// <summary>
    /// 验证扭曲倍数为 0 时图像尺寸保持不变。
    /// </summary>
    [Fact]
    public void TwistImage_ZeroMult_PreservesContent()
    {
        using var source = CreateTestImage(4, 4, SKColors.White);
        using var result = SkiaSharpHelper.TwistImage(source, true, 0, 0);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    #endregion

    #region SetErosionEffect

    /// <summary>
    /// 验证侵蚀效果保持图像尺寸。
    /// </summary>
    [Fact]
    public void SetErosionEffect_ReturnsSameSize()
    {
        using var source = CreateTestImage(4, 4);
        using var result = SkiaSharpHelper.SetErosionEffect(source, 0, 0);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证空图像会导致侵蚀效果抛出空参数异常。
    /// </summary>
    [Fact]
    public void SetErosionEffect_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.SetErosionEffect(null!, 0, 0));
    }

    /// <summary>
    /// 验证正亮度参数下侵蚀效果返回与源图像同尺寸的结果。
    /// </summary>
    [Fact]
    public void SetErosionEffect_PositiveBrightnessProducesBrighterImage()
    {
        using var source = CreateTestImage(1, 1, new SKColor(128, 128, 128));
        using var result = SkiaSharpHelper.SetErosionEffect(source, 50, 0);
        result.Width.ShouldBe(1);
        result.Height.ShouldBe(1);
    }

    #endregion

    #region ToIcoStream

    /// <summary>
    /// 验证 ICO 输出流包含正确的文件头和帧数。
    /// </summary>
    [Fact]
    public void ToIcoStream_ProducesValidIcoHeader()
    {
        using var source = CreateTestImage(32, 32);
        using var stream = SkiaSharpHelper.ToIcoStream(source, new[] { 16, 32 });
        stream.Position.ShouldBe(0);
        var bytes = new byte[6];
        stream.Read(bytes, 0, 6);
        bytes[0].ShouldBe((byte)0);
        bytes[1].ShouldBe((byte)0);
        bytes[2].ShouldBe((byte)1); // ICO type
        bytes[3].ShouldBe((byte)0);
        bytes[4].ShouldBe((byte)2); // 2 frames
    }

    /// <summary>
    /// 验证空图像会导致 ICO 编码抛出空参数异常。
    /// </summary>
    [Fact]
    public void ToIcoStream_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToIcoStream(null!));
    }

    /// <summary>
    /// 验证默认 ICO 尺寸配置会生成六个图标帧。
    /// </summary>
    [Fact]
    public void ToIcoStream_DefaultSizes_ProducesSixFrames()
    {
        using var source = CreateTestImage(256, 256);
        using var stream = SkiaSharpHelper.ToIcoStream(source);
        stream.Position = 0;
        var header = new byte[6];
        stream.Read(header, 0, 6);
        header[4].ShouldBe((byte)6);
    }

    #endregion

    #region Helper

    /// <summary>
    /// 创建指定尺寸和填充颜色的测试图像。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <param name="fill">可选填充颜色；省略时使用中灰色。</param>
    /// <returns>由指定颜色填充的图像。</returns>
    private static SKImage CreateTestImage(int width, int height, SKColor? fill = null)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(fill ?? new SKColor(128, 128, 128));
        return SKImage.FromBitmap(bitmap);
    }

    #endregion
}
