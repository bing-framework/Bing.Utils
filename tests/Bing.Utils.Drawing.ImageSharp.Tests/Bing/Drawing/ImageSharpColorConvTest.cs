using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 验证 ImageSharp 像素与共享颜色模型及颜色字符串的转换。
/// </summary>
[Trait("Drawing", "ImageSharp.ColorConv")]
public class ImageSharpColorConvTest
{
    /// <summary>
    /// 验证 RGBA 像素转共享颜色模型时保留全部通道。
    /// </summary>
    [Fact]
    public void ToRgbColor_Rgba32_PreservesChannels()
    {
        var src = new Rgba32(10, 20, 30, 40);
        var rgb = ImageSharpHelper.ToRgbColor(src);

        rgb.R.ShouldBe((byte)10);
        rgb.G.ShouldBe((byte)20);
        rgb.B.ShouldBe((byte)30);
        rgb.A.ShouldBe((byte)40);
    }

    /// <summary>
    /// 验证共享颜色模型转 RGBA 像素时保留全部通道。
    /// </summary>
    [Fact]
    public void ToRgba32_RgbColor_PreservesChannels()
    {
        var src = new RgbColor(10, 20, 30, 40);
        var rgba = ImageSharpHelper.ToRgba32(src);

        rgba.R.ShouldBe((byte)10);
        rgba.G.ShouldBe((byte)20);
        rgba.B.ShouldBe((byte)30);
        rgba.A.ShouldBe((byte)40);
    }

    /// <summary>
    /// 验证纯红色像素编码为六位十六进制颜色字符串。
    /// </summary>
    [Fact]
    public void ToHex_Rgba32_ReturnsExpected()
    {
        var hex = ImageSharpHelper.ToHex(new Rgba32(255, 0, 0));
        hex.ShouldBe("#FF0000");
    }

    /// <summary>
    /// 验证绿色十六进制颜色解析为对应 RGB 通道。
    /// </summary>
    [Fact]
    public void FromHex_ValidHex_ReturnsRgba32()
    {
        var color = ImageSharpHelper.FromHex("#00FF00");
        color.R.ShouldBe((byte)0);
        color.G.ShouldBe((byte)255);
        color.B.ShouldBe((byte)0);
    }

    /// <summary>
    /// 验证纯红色像素转换为预期色相、饱和度及亮度。
    /// </summary>
    [Fact]
    public void ToHsl_Rgba32_ReturnsValidRange()
    {
        var hsl = ImageSharpHelper.ToHsl(new Rgba32(255, 0, 0));
        hsl.H.ShouldBe(0, tolerance: 0.01);
        hsl.S.ShouldBe(1.0, tolerance: 0.01);
        hsl.L.ShouldBe(0.5, tolerance: 0.01);
    }

    /// <summary>
    /// 验证 HSL 红色转换为不透明的近似纯红像素。
    /// </summary>
    [Fact]
    public void FromHsl_ValidHsl_ReturnsRgba32()
    {
        var color = ImageSharpHelper.FromHsl(new HslColor(0, 1.0, 0.5));
        ((double)color.R).ShouldBe(255.0, 2.0);
        ((double)color.G).ShouldBe(0.0, 2.0);
        ((double)color.B).ShouldBe(0.0, 2.0);
        color.A.ShouldBe((byte)255);
    }

    /// <summary>
    /// 验证 HSL 转像素时使用指定的 Alpha 通道值。
    /// </summary>
    [Fact]
    public void FromHsl_CustomAlpha_PreservesAlpha()
    {
        var color = ImageSharpHelper.FromHsl(new HslColor(0, 1.0, 0.5), 128);
        color.A.ShouldBe((byte)128);
    }
}
