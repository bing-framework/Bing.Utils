using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 ImageSharp 颜色转换适配层。
/// </summary>
[Trait("Drawing", "ImageSharp.ColorConv")]
public class ImageSharpColorConvTest
{
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

    [Fact]
    public void ToHex_Rgba32_ReturnsExpected()
    {
        var hex = ImageSharpHelper.ToHex(new Rgba32(255, 0, 0));
        hex.ShouldBe("#FF0000");
    }

    [Fact]
    public void FromHex_ValidHex_ReturnsRgba32()
    {
        var color = ImageSharpHelper.FromHex("#00FF00");
        color.R.ShouldBe((byte)0);
        color.G.ShouldBe((byte)255);
        color.B.ShouldBe((byte)0);
    }

    [Fact]
    public void ToHsl_Rgba32_ReturnsValidRange()
    {
        var hsl = ImageSharpHelper.ToHsl(new Rgba32(255, 0, 0));
        hsl.H.ShouldBe(0, tolerance: 0.01);
        hsl.S.ShouldBe(1.0, tolerance: 0.01);
        hsl.L.ShouldBe(0.5, tolerance: 0.01);
    }

    [Fact]
    public void FromHsl_ValidHsl_ReturnsRgba32()
    {
        var color = ImageSharpHelper.FromHsl(new HslColor(0, 1.0, 0.5));
        ((double)color.R).ShouldBe(255.0, 2.0);
        ((double)color.G).ShouldBe(0.0, 2.0);
        ((double)color.B).ShouldBe(0.0, 2.0);
        color.A.ShouldBe((byte)255);
    }

    [Fact]
    public void FromHsl_CustomAlpha_PreservesAlpha()
    {
        var color = ImageSharpHelper.FromHsl(new HslColor(0, 1.0, 0.5), 128);
        color.A.ShouldBe((byte)128);
    }
}
