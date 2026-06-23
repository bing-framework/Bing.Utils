using Bing.Drawing;

// ReSharper disable once CheckNamespace
namespace Bing.Conversions;

/// <summary>
/// 颜色转换兼容层。
/// <para>
/// 注意：旧版 <c>RgbToHsb</c> / <c>HsbToRgb</c> 使用 GDI+ 的 HSL 语义
/// （<c>Color.GetBrightness()</c> 返回 lightness，公式为 HSL）。
/// 为保持历史行为兼容，本类内部委托到 <see cref="ColorConversion"/> 的 HSL 核心实现。
/// </para>
/// </summary>
public static class ColorConv
{
    /// <summary>
    /// RGB 转 HSB（实际语义为 HSL）。
    /// 返回数组 [色相, 饱和度, 亮度]，范围分别为 [0,360)、[0,1]、[0,1]。
    /// </summary>
    /// <param name="r">红色分量 (0-255)</param>
    /// <param name="g">绿色分量 (0-255)</param>
    /// <param name="b">蓝色分量 (0-255)</param>
    public static float[] RgbToHsb(int r, int g, int b)
    {
        var hsl = ColorConversion.RgbToHsl(new RgbColor((byte)r, (byte)g, (byte)b));
        return new[] { (float)hsl.H, (float)hsl.S, (float)hsl.L };
    }

    /// <summary>
    /// HSV(HSB) 转 RGB（实际语义为 HSL）。
    /// </summary>
    /// <param name="hue">色相 (0-360)</param>
    /// <param name="saturation">饱和度 (0-1)</param>
    /// <param name="value">亮度 (0-1)</param>
    public static RgbColor HsbToRgb(double hue, double saturation, double value)
    {
        return ColorConversion.HslToRgb(new HslColor(hue, saturation, value));
    }

    /// <summary>
    /// 将 sRGB 颜色值转换为线性 RGB
    /// </summary>
    /// <param name="sRgb">sRGB 值，范围 0-1</param>
    public static double SRgbToLinearRgb(double sRgb) => ColorConversion.SRgbToLinearRgb(sRgb);

    /// <summary>
    /// 将线性 RGB 颜色值转换为 sRGB
    /// </summary>
    /// <param name="linearRgb">线性 RGB 值，范围 0-1</param>
    public static double LinearRgbToSRgb(double linearRgb) => ColorConversion.LinearRgbToSRgb(linearRgb);

    /// <summary>
    /// 将 RGB 转换为 Hex 字符串（#RRGGBB）
    /// </summary>
    /// <param name="r">红色分量</param>
    /// <param name="g">绿色分量</param>
    /// <param name="b">蓝色分量</param>
    public static string RgbToHex(int r, int g, int b) => ColorConversion.ToHex(new RgbColor((byte)r, (byte)g, (byte)b));

    /// <summary>
    /// 将 RGB 转换为 Hex 字符串
    /// </summary>
    /// <param name="color">RGB 颜色</param>
    public static string ToHex(RgbColor color) => ColorConversion.ToHex(color);

    /// <summary>
    /// 将 RGB 转换为 RGB(...) 字符串
    /// </summary>
    /// <param name="color">RGB 颜色</param>
    public static string ToRgb(RgbColor color) => ColorConversion.ToRgbString(color);
}
