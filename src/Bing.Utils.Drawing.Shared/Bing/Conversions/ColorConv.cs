using Bing.Drawing;

// ReSharper disable once CheckNamespace
namespace Bing.Conversions;

/// <summary>
/// 颜色转换兼容层。
/// </summary>
/// <remarks>
/// 注意：旧版 <c>RgbToHsb</c> / <c>HsbToRgb</c> 使用 GDI+ 的 HSL 语义
/// （<c>Color.GetBrightness()</c> 返回 lightness，公式为 HSL）。
/// 为保持历史行为兼容，本类内部委托到 <see cref="ColorConversion"/> 的 HSL 核心实现。
/// </remarks>
public static class ColorConv
{
    /// <summary>
    /// 将 RGB 分量转换为 HSB 数组。
    /// </summary>
    /// <remarks>
    /// 返回值沿用旧版 HSL 语义。
    /// 返回数组按色相、饱和度和亮度排列，范围分别为 [0,360)、[0,1]、[0,1]。
    /// </remarks>
    /// <param name="r">红色分量，范围为 0-255。</param>
    /// <param name="g">绿色分量，范围为 0-255。</param>
    /// <param name="b">蓝色分量，范围为 0-255。</param>
    /// <returns>按色相、饱和度和亮度顺序排列的 HSL 值。</returns>
    /// <exception cref="ArgumentOutOfRangeException">任一分量超出 0-255 范围。</exception>
    public static float[] RgbToHsb(int r, int g, int b)
    {
        ValidateRgb(r, g, b);
        var hsl = ColorConversion.RgbToHsl(new RgbColor((byte)r, (byte)g, (byte)b));
        return new[] { (float)hsl.H, (float)hsl.S, (float)hsl.L };
    }

    /// <summary>
    /// 将 HSB 值转换为 RGB 颜色。
    /// </summary>
    /// <remarks>
    /// 参数和返回值沿用旧版 HSL 语义。
    /// </remarks>
    /// <param name="hue">色相，范围为 0-360。</param>
    /// <param name="saturation">饱和度，范围为 0-1。</param>
    /// <param name="value">亮度，范围为 0-1。</param>
    /// <returns>转换后的 RGB 颜色。</returns>
    public static RgbColor HsbToRgb(double hue, double saturation, double value)
    {
        return ColorConversion.HslToRgb(new HslColor(hue, saturation, value));
    }

    /// <summary>
    /// 将 sRGB 单通道值转换为线性 RGB 值。
    /// </summary>
    /// <param name="sRgb">sRGB 值，范围为 0-1。</param>
    /// <returns>对应的线性 RGB 值。</returns>
    public static double SRgbToLinearRgb(double sRgb) => ColorConversion.SRgbToLinearRgb(sRgb);

    /// <summary>
    /// 将线性 RGB 单通道值转换为 sRGB 值。
    /// </summary>
    /// <param name="linearRgb">线性 RGB 值，范围为 0-1。</param>
    /// <returns>对应的 sRGB 值。</returns>
    public static double LinearRgbToSRgb(double linearRgb) => ColorConversion.LinearRgbToSRgb(linearRgb);

    /// <summary>
    /// 将 RGB 分量转换为十六进制颜色字符串。
    /// </summary>
    /// <param name="r">红色分量，范围为 0-255。</param>
    /// <param name="g">绿色分量，范围为 0-255。</param>
    /// <param name="b">蓝色分量，范围为 0-255。</param>
    /// <returns>格式为 <c>#RRGGBB</c> 的颜色字符串。</returns>
    /// <exception cref="ArgumentOutOfRangeException">任一分量超出 0-255 范围。</exception>
    public static string RgbToHex(int r, int g, int b)
    {
        ValidateRgb(r, g, b);
        return ColorConversion.ToHex(new RgbColor((byte)r, (byte)g, (byte)b));
    }

    /// <summary>
    /// 将 RGB 颜色转换为十六进制颜色字符串。
    /// </summary>
    /// <param name="color">RGB 颜色。</param>
    /// <returns>格式为 <c>#RRGGBB</c> 的颜色字符串。</returns>
    public static string ToHex(RgbColor color) => ColorConversion.ToHex(color);

    /// <summary>
    /// 将 RGB 颜色转换为 CSS RGB 字符串。
    /// </summary>
    /// <param name="color">RGB 颜色。</param>
    /// <returns>格式为 <c>RGB(r,g,b)</c> 的颜色字符串。</returns>
    public static string ToRgb(RgbColor color) => ColorConversion.ToRgbString(color);

    /// <summary>
    /// 验证三个 RGB 分量均可转换为字节。
    /// </summary>
    /// <param name="r">红色分量。</param>
    /// <param name="g">绿色分量。</param>
    /// <param name="b">蓝色分量。</param>
    /// <exception cref="ArgumentOutOfRangeException">任一分量超出字节范围。</exception>
    private static void ValidateRgb(int r, int g, int b)
    {
        ValidateRgbComponent(r, nameof(r));
        ValidateRgbComponent(g, nameof(g));
        ValidateRgbComponent(b, nameof(b));
    }

    /// <summary>
    /// 验证单个 RGB 分量范围。
    /// </summary>
    /// <param name="value">待验证的分量。</param>
    /// <param name="parameterName">异常中使用的参数名。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> 超出字节范围。</exception>
    private static void ValidateRgbComponent(int value, string parameterName)
    {
        if ((uint)value > byte.MaxValue)
            throw new ArgumentOutOfRangeException(parameterName, value, "RGB 分量必须在 0-255 之间。");
    }
}
