using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 验证 SkiaSharp 颜色与 RGB、HSL 及十六进制表示之间的转换。
/// </summary>
[Trait("Drawing", "SkiaSharp.ColorConv")]
public class SkiaSharpColorConvTest
{
    /// <summary>
    /// 验证转换为 RGB 颜色后四个通道值保持不变。
    /// </summary>
    [Fact]
    public void ToRgbColor_SKColor_PreservesChannels()
    {
        var src = new SKColor(10, 20, 30, 40);
        var rgb = SkiaSharpHelper.ToRgbColor(src);

        rgb.R.ShouldBe((byte)10);
        rgb.G.ShouldBe((byte)20);
        rgb.B.ShouldBe((byte)30);
        rgb.A.ShouldBe((byte)40);
    }

    /// <summary>
    /// 验证转换为 SkiaSharp 颜色后四个通道值保持不变。
    /// </summary>
    [Fact]
    public void ToSKColor_RgbColor_PreservesChannels()
    {
        var src = new RgbColor(10, 20, 30, 40);
        var sk = SkiaSharpHelper.ToSKColor(src);

        sk.Red.ShouldBe((byte)10);
        sk.Green.ShouldBe((byte)20);
        sk.Blue.ShouldBe((byte)30);
        sk.Alpha.ShouldBe((byte)40);
    }

    /// <summary>
    /// 验证红色转换为不含 Alpha 分量的十六进制字符串。
    /// </summary>
    [Fact]
    public void ToHex_SKColor_ReturnsExpected()
    {
        var hex = SkiaSharpHelper.ToHex(new SKColor(255, 0, 0));
        hex.ShouldBe("#FF0000");
    }

    /// <summary>
    /// 验证十六进制颜色文本可转换为对应的 SkiaSharp 颜色。
    /// </summary>
    [Fact]
    public void FromHex_ValidHex_ReturnsSKColor()
    {
        var color = SkiaSharpHelper.FromHex("#00FF00");
        color.Red.ShouldBe((byte)0);
        color.Green.ShouldBe((byte)255);
        color.Blue.ShouldBe((byte)0);
    }

    /// <summary>
    /// 验证红色转换为 HSL 后色相、饱和度和亮度符合预期。
    /// </summary>
    [Fact]
    public void ToHsl_SKColor_ReturnsValidRange()
    {
        var hsl = SkiaSharpHelper.ToHsl(new SKColor(255, 0, 0));
        hsl.H.ShouldBe(0, tolerance: 0.01);
        hsl.S.ShouldBe(1.0, tolerance: 0.01);
        hsl.L.ShouldBe(0.5, tolerance: 0.01);
    }

    /// <summary>
    /// 验证 HSL 红色可转换为对应的 SkiaSharp 颜色。
    /// </summary>
    [Fact]
    public void FromHsl_ValidHsl_ReturnsSKColor()
    {
        var color = SkiaSharpHelper.FromHsl(new HslColor(0, 1.0, 0.5));
        ((double)color.Red).ShouldBe(255.0, 2.0);
        ((double)color.Green).ShouldBe(0.0, 2.0);
        ((double)color.Blue).ShouldBe(0.0, 2.0);
        color.Alpha.ShouldBe((byte)255);
    }

    /// <summary>
    /// 验证转换 HSL 颜色时保留指定的 Alpha 值。
    /// </summary>
    [Fact]
    public void FromHsl_CustomAlpha_PreservesAlpha()
    {
        var color = SkiaSharpHelper.FromHsl(new HslColor(0, 1.0, 0.5), 128);
        color.Alpha.ShouldBe((byte)128);
    }
}
