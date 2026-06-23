using Bing.Drawing;

namespace Bing.Conversions;

/// <summary>
/// 测试类：覆盖 Shared ColorConversion 核心算法。
/// </summary>
[Trait("Drawing", "ColorConversion")]
public class ColorConversionTest
{
    #region RgbToHsl

    [Theory]
    [InlineData(255, 0, 0)]     // 红色
    [InlineData(0, 255, 0)]     // 绿色
    [InlineData(0, 0, 255)]     // 蓝色
    [InlineData(255, 255, 255)] // 白色
    [InlineData(0, 0, 0)]       // 黑色
    [InlineData(128, 128, 128)] // 灰色
    public void RgbToHsl_PrimaryColors_ReturnsValidRange(int r, int g, int b)
    {
        var color = new RgbColor((byte)r, (byte)g, (byte)b);
        var hsl = ColorConversion.RgbToHsl(color);

        hsl.H.ShouldBeGreaterThanOrEqualTo(0);
        hsl.H.ShouldBeLessThan(360);
        hsl.S.ShouldBeGreaterThanOrEqualTo(0);
        hsl.S.ShouldBeLessThanOrEqualTo(1);
        hsl.L.ShouldBeGreaterThanOrEqualTo(0);
        hsl.L.ShouldBeLessThanOrEqualTo(1);
    }

    [Fact]
    public void RgbToHsl_RedColor_ReturnsCorrectHsl()
    {
        var hsl = ColorConversion.RgbToHsl(new RgbColor(255, 0, 0));

        hsl.H.ShouldBe(0, tolerance: 0.01);
        hsl.S.ShouldBe(1.0, tolerance: 0.01);
        hsl.L.ShouldBe(0.5, tolerance: 0.01);
    }

    [Theory]
    [InlineData(255, 255, 255)] // 白色
    [InlineData(0, 0, 0)]       // 黑色
    public void RgbToHsl_Achromatic_ReturnsSaturationZero(int r, int g, int b)
    {
        var hsl = ColorConversion.RgbToHsl(new RgbColor((byte)r, (byte)g, (byte)b));
        hsl.S.ShouldBe(0, tolerance: 1e-10);
    }

    #endregion

    #region HslToRgb

    [Fact]
    public void HslToRgb_ZeroSaturation_ReturnsGrayscale()
    {
        var color = ColorConversion.HslToRgb(new HslColor(0, 0, 0.5));
        color.R.ShouldBe(color.G);
        color.G.ShouldBe(color.B);
    }

    [Fact]
    public void HslToRgb_RedColor_ReturnsRed()
    {
        var color = ColorConversion.HslToRgb(new HslColor(0, 1.0, 0.5));
        ((double)color.R).ShouldBe(255.0, 2.0);
        ((double)color.G).ShouldBe(0.0, 2.0);
        ((double)color.B).ShouldBe(0.0, 2.0);
    }

    [Theory]
    [InlineData(0, 0, 0)]       // 黑色
    [InlineData(0, 0, 1)]       // 白色
    [InlineData(180, 0.5, 0.5)] // 中间值
    public void HslToRgb_BoundaryValues_ReturnsValidBytes(double h, double s, double l)
    {
        var color = ColorConversion.HslToRgb(new HslColor(h, s, l));
        color.R.ShouldBeGreaterThanOrEqualTo((byte)0);
        color.R.ShouldBeLessThanOrEqualTo((byte)255);
        color.G.ShouldBeGreaterThanOrEqualTo((byte)0);
        color.G.ShouldBeLessThanOrEqualTo((byte)255);
        color.B.ShouldBeGreaterThanOrEqualTo((byte)0);
        color.B.ShouldBeLessThanOrEqualTo((byte)255);
    }

    #endregion

    #region Round-trip

    [Theory]
    [InlineData(255, 0, 0)]
    [InlineData(0, 255, 0)]
    [InlineData(0, 0, 255)]
    [InlineData(255, 255, 0)]
    [InlineData(255, 0, 255)]
    [InlineData(0, 255, 255)]
    public void RoundTrip_RgbToHslToRgb_ReturnsNearOriginal(int r, int g, int b)
    {
        var original = new RgbColor((byte)r, (byte)g, (byte)b);
        var hsl = ColorConversion.RgbToHsl(original);
        var roundTripped = ColorConversion.HslToRgb(hsl);

        // 允许 ±2 误差
        Math.Abs(roundTripped.R - r).ShouldBeLessThanOrEqualTo(2);
        Math.Abs(roundTripped.G - g).ShouldBeLessThanOrEqualTo(2);
        Math.Abs(roundTripped.B - b).ShouldBeLessThanOrEqualTo(2);
    }

    #endregion

    #region sRGB / Linear RGB

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void SRgbToLinearRgb_RoundTrip_ReturnsNearOriginal(double sRgb)
    {
        var linear = ColorConversion.SRgbToLinearRgb(sRgb);
        var roundTripped = ColorConversion.LinearRgbToSRgb(linear);
        roundTripped.ShouldBe(sRgb, tolerance: 1e-10);
    }

    [Fact]
    public void SRgbToLinearRgb_OutOfRange_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorConversion.SRgbToLinearRgb(-0.1));
        Should.Throw<ArgumentOutOfRangeException>(() => ColorConversion.SRgbToLinearRgb(1.1));
    }

    [Fact]
    public void LinearRgbToSRgb_OutOfRange_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorConversion.LinearRgbToSRgb(-0.1));
        Should.Throw<ArgumentOutOfRangeException>(() => ColorConversion.LinearRgbToSRgb(1.1));
    }

    #endregion

    #region Hex

    [Theory]
    [InlineData(255, 0, 0, "#FF0000")]
    [InlineData(0, 255, 0, "#00FF00")]
    [InlineData(0, 0, 255, "#0000FF")]
    public void ToHex_WithoutAlpha_ReturnsExpected(int r, int g, int b, string expected)
    {
        var hex = ColorConversion.ToHex(new RgbColor((byte)r, (byte)g, (byte)b));
        hex.ShouldBe(expected);
    }

    [Fact]
    public void ToHex_WithAlpha_ReturnsExpected()
    {
        var hex = ColorConversion.ToHex(new RgbColor(255, 0, 0, 128), includeAlpha: true);
        hex.ShouldBe("#80FF0000");
    }

    [Theory]
    [InlineData("#FF0000", 255, 0, 0)]
    [InlineData("FF0000", 255, 0, 0)]
    [InlineData("#00FF00", 0, 255, 0)]
    public void TryParseHex_6Digits_ParsesCorrectly(string hex, int r, int g, int b)
    {
        var result = ColorConversion.TryParseHex(hex, out var color);
        result.ShouldBeTrue();
        color.R.ShouldBe((byte)r);
        color.G.ShouldBe((byte)g);
        color.B.ShouldBe((byte)b);
    }

    [Fact]
    public void TryParseHex_6Digit_StripsHash()
    {
        ColorConversion.TryParseHex("#FF0000", out var color).ShouldBeTrue();
        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)0);
        color.A.ShouldBe((byte)255);
    }

    [Fact]
    public void TryParseHex_8Digit_IncludesAlpha()
    {
        ColorConversion.TryParseHex("#80FF0000", out var color).ShouldBeTrue();
        color.A.ShouldBe((byte)128);
        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)0);
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#FFF")]
    [InlineData("#12345")]
    [InlineData("invalid")]
    public void TryParseHex_InvalidInput_ReturnsFalse(string input)
    {
        ColorConversion.TryParseHex(input, out _).ShouldBeFalse();
    }

    [Fact]
    public void ParseHex_InvalidInput_ThrowsFormatException()
    {
        Should.Throw<FormatException>(() => ColorConversion.ParseHex("invalid"));
    }

    #endregion
}
