using Bing.Drawing;

namespace Bing.Conversions;

/// <summary>
/// 验证 RGB、HSL、线性颜色及十六进制颜色转换。
/// </summary>
[Trait("Drawing", "ColorConversion")]
public class ColorConversionTest
{
    #region RgbToHsl

    /// <summary>
    /// 验证基本颜色转换所得色相、饱和度和亮度处于有效范围。
    /// </summary>
    /// <param name="r">红色通道的样例值，范围为 0 至 255。</param>
    /// <param name="g">绿色通道的样例值，范围为 0 至 255。</param>
    /// <param name="b">蓝色通道的样例值，范围为 0 至 255。</param>
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

    /// <summary>
    /// 验证纯红色对应零度色相、完全饱和及一半亮度。
    /// </summary>
    [Fact]
    public void RgbToHsl_RedColor_ReturnsCorrectHsl()
    {
        var hsl = ColorConversion.RgbToHsl(new RgbColor(255, 0, 0));

        hsl.H.ShouldBe(0, tolerance: 0.01);
        hsl.S.ShouldBe(1.0, tolerance: 0.01);
        hsl.L.ShouldBe(0.5, tolerance: 0.01);
    }

    /// <summary>
    /// 验证黑白无彩色转换后的饱和度为零。
    /// </summary>
    /// <param name="r">无彩色的红色通道样例值。</param>
    /// <param name="g">无彩色的绿色通道样例值。</param>
    /// <param name="b">无彩色的蓝色通道样例值。</param>
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

    /// <summary>
    /// 验证零饱和度转换为红绿蓝通道相等的灰色。
    /// </summary>
    [Fact]
    public void HslToRgb_ZeroSaturation_ReturnsGrayscale()
    {
        var color = ColorConversion.HslToRgb(new HslColor(0, 0, 0.5));
        color.R.ShouldBe(color.G);
        color.G.ShouldBe(color.B);
    }

    /// <summary>
    /// 验证红色色相的 HSL 值转换为近似纯红色。
    /// </summary>
    [Fact]
    public void HslToRgb_RedColor_ReturnsRed()
    {
        var color = ColorConversion.HslToRgb(new HslColor(0, 1.0, 0.5));
        ((double)color.R).ShouldBe(255.0, 2.0);
        ((double)color.G).ShouldBe(0.0, 2.0);
        ((double)color.B).ShouldBe(0.0, 2.0);
    }

    /// <summary>
    /// 验证边界 HSL 样例转换后的通道均在字节范围内。
    /// </summary>
    /// <param name="h">以度为单位的色相样例值。</param>
    /// <param name="s">范围为 0 至 1 的饱和度样例值。</param>
    /// <param name="l">范围为 0 至 1 的亮度样例值。</param>
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

    /// <summary>
    /// 验证 RGB 经 HSL 往返转换的各通道误差不超过二。
    /// </summary>
    /// <param name="r">原始颜色的红色通道值。</param>
    /// <param name="g">原始颜色的绿色通道值。</param>
    /// <param name="b">原始颜色的蓝色通道值。</param>
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

    /// <summary>
    /// 验证标准 RGB 分量经线性 RGB 往返转换保持原值。
    /// </summary>
    /// <param name="sRgb">范围为 0 至 1 的标准 RGB 分量样例值。</param>
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

    /// <summary>
    /// 验证标准 RGB 转线性 RGB 时拒绝范围外的分量。
    /// </summary>
    [Fact]
    public void SRgbToLinearRgb_OutOfRange_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorConversion.SRgbToLinearRgb(-0.1));
        Should.Throw<ArgumentOutOfRangeException>(() => ColorConversion.SRgbToLinearRgb(1.1));
    }

    /// <summary>
    /// 验证线性 RGB 转标准 RGB 时拒绝范围外的分量。
    /// </summary>
    [Fact]
    public void LinearRgbToSRgb_OutOfRange_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorConversion.LinearRgbToSRgb(-0.1));
        Should.Throw<ArgumentOutOfRangeException>(() => ColorConversion.LinearRgbToSRgb(1.1));
    }

    #endregion

    #region Hex

    /// <summary>
    /// 验证 RGB 颜色编码为带井号的六位十六进制字符串。
    /// </summary>
    /// <param name="r">红色通道样例值。</param>
    /// <param name="g">绿色通道样例值。</param>
    /// <param name="b">蓝色通道样例值。</param>
    /// <param name="expected">预期的十六进制颜色字符串。</param>
    [Theory]
    [InlineData(255, 0, 0, "#FF0000")]
    [InlineData(0, 255, 0, "#00FF00")]
    [InlineData(0, 0, 255, "#0000FF")]
    public void ToHex_WithoutAlpha_ReturnsExpected(int r, int g, int b, string expected)
    {
        var hex = ColorConversion.ToHex(new RgbColor((byte)r, (byte)g, (byte)b));
        hex.ShouldBe(expected);
    }

    /// <summary>
    /// 验证包含透明度的十六进制编码将 Alpha 通道放在最前。
    /// </summary>
    [Fact]
    public void ToHex_WithAlpha_ReturnsExpected()
    {
        var hex = ColorConversion.ToHex(new RgbColor(255, 0, 0, 128), includeAlpha: true);
        hex.ShouldBe("#80FF0000");
    }

    /// <summary>
    /// 验证带或不带井号的六位十六进制颜色解析为指定 RGB 通道。
    /// </summary>
    /// <param name="hex">待解析的十六进制颜色样例。</param>
    /// <param name="r">预期红色通道值。</param>
    /// <param name="g">预期绿色通道值。</param>
    /// <param name="b">预期蓝色通道值。</param>
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

    /// <summary>
    /// 验证带井号的六位颜色解析为不透明的纯红色。
    /// </summary>
    [Fact]
    public void TryParseHex_6Digit_StripsHash()
    {
        ColorConversion.TryParseHex("#FF0000", out var color).ShouldBeTrue();
        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)0);
        color.A.ShouldBe((byte)255);
    }

    /// <summary>
    /// 验证八位十六进制颜色解析包含 Alpha 通道。
    /// </summary>
    [Fact]
    public void TryParseHex_8Digit_IncludesAlpha()
    {
        ColorConversion.TryParseHex("#80FF0000", out var color).ShouldBeTrue();
        color.A.ShouldBe((byte)128);
        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)0);
    }

    /// <summary>
    /// 验证空值、空白及格式错误的颜色字符串解析失败。
    /// </summary>
    /// <param name="input">不能解析为六位或八位颜色的样例字符串，可为 null。</param>
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

    /// <summary>
    /// 验证强制解析非法十六进制颜色时抛出格式异常。
    /// </summary>
    [Fact]
    public void ParseHex_InvalidInput_ThrowsFormatException()
    {
        Should.Throw<FormatException>(() => ColorConversion.ParseHex("invalid"));
    }

    #endregion
}
