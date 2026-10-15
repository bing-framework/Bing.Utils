using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 验证扭曲、腐蚀兼容效果及多尺寸 ICO 编码。
/// </summary>
[Trait("Drawing", "ImageSharp.Advanced")]
public class ImageSharpAdvancedEffectTest
{
    #region TwistImage

    /// <summary>
    /// 验证扭曲处理保持图像尺寸。
    /// </summary>
    [Fact]
    public void TwistImage_ReturnsSameSize()
    {
        using var source = CreateTestImage(8, 8);
        using var result = ImageSharpHelper.TwistImage(source, true, 3, 0);
        result.Width.ShouldBe(8);
        result.Height.ShouldBe(8);
    }

    /// <summary>
    /// 验证扭曲处理拒绝空图像。
    /// </summary>
    [Fact]
    public void TwistImage_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.TwistImage(null!, true, 3, 0));
    }

    /// <summary>
    /// 验证零扭曲幅度时图像尺寸保持不变。
    /// </summary>
    [Fact]
    public void TwistImage_ZeroMult_PreservesContent()
    {
        using var source = CreateTestImage(4, 4, Color.White);
        using var result = ImageSharpHelper.TwistImage(source, true, 0, 0);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    #endregion

    #region SetErosionEffect

    /// <summary>
    /// 验证腐蚀兼容效果保持图像尺寸。
    /// </summary>
    [Fact]
    public void SetErosionEffect_ReturnsSameSize()
    {
        using var source = CreateTestImage(4, 4);
        using var result = ImageSharpHelper.SetErosionEffect(source, 0, 0);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证腐蚀兼容效果拒绝空图像。
    /// </summary>
    [Fact]
    public void SetErosionEffect_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.SetErosionEffect(null!, 0, 0));
    }

    /// <summary>
    /// 验证指定正亮度时腐蚀兼容效果保持单像素尺寸。
    /// </summary>
    [Fact]
    public void SetErosionEffect_PositiveBrightnessProducesBrighterImage()
    {
        using var source = CreateTestImage(1, 1, Color.FromRgb(128, 128, 128));
        using var result = ImageSharpHelper.SetErosionEffect(source, 50, 0);
        result.Width.ShouldBe(1);
        result.Height.ShouldBe(1);
    }

    #endregion

    #region ToIcoStream

    /// <summary>
    /// 验证自定义两种尺寸的 ICO 流从起点开始且文件头标明两帧。
    /// </summary>
    [Fact]
    public void ToIcoStream_ProducesValidIcoHeader()
    {
        using var source = CreateTestImage(32, 32);
        using var stream = ImageSharpHelper.ToIcoStream(source, new[] { 16, 32 });
        stream.Position.ShouldBe(0);
        var bytes = new byte[6];
        stream.Read(bytes, 0, 6);
        bytes[0].ShouldBe((byte)0); // reserved
        bytes[1].ShouldBe((byte)0); // reserved
        bytes[2].ShouldBe((byte)1); // type: ICO
        bytes[3].ShouldBe((byte)0);
        bytes[4].ShouldBe((byte)2); // 2 frames
    }

    /// <summary>
    /// 验证 ICO 编码拒绝空图像。
    /// </summary>
    [Fact]
    public void ToIcoStream_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToIcoStream(null!));
    }

    /// <summary>
    /// 验证默认 ICO 输出的文件头标明六帧。
    /// </summary>
    [Fact]
    public void ToIcoStream_DefaultSizes_ProducesSixFrames()
    {
        using var source = CreateTestImage(256, 256);
        using var stream = ImageSharpHelper.ToIcoStream(source);
        stream.Position = 0;
        var header = new byte[6];
        stream.Read(header, 0, 6);
        header[4].ShouldBe((byte)6); // 6 default sizes
    }

    #endregion

    #region Helper

    /// <summary>
    /// 创建指定尺寸及填充颜色的高级效果测试图像。
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
