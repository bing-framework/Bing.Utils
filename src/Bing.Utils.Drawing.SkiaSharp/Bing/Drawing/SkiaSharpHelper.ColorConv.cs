using SkiaSharp;

namespace Bing.Drawing;

// 图片操作辅助类 - 颜色转换适配
public static partial class SkiaSharpHelper
{
    #region SKColor <-> RgbColor

    /// <summary>
    /// 将 SKColor 转换为 Shared RgbColor
    /// </summary>
    /// <param name="color">SkiaSharp 颜色</param>
    public static RgbColor ToRgbColor(SKColor color) => new(color.Red, color.Green, color.Blue, color.Alpha);

    /// <summary>
    /// 将 Shared RgbColor 转换为 SKColor
    /// </summary>
    /// <param name="color">Shared 颜色</param>
    public static SKColor ToSKColor(RgbColor color) => new(color.R, color.G, color.B, color.A);

    #endregion

    #region HSL 便捷方法

    /// <summary>
    /// 将 SKColor 转换为 HSL 颜色
    /// </summary>
    /// <param name="color">SkiaSharp 颜色</param>
    public static HslColor ToHsl(SKColor color) => ColorConversion.RgbToHsl(new RgbColor(color.Red, color.Green, color.Blue));

    /// <summary>
    /// 将 HSL 颜色转换为 SKColor
    /// </summary>
    /// <param name="hsl">HSL 颜色</param>
    /// <param name="alpha">Alpha 值，默认 255</param>
    public static SKColor FromHsl(HslColor hsl, byte alpha = 255)
    {
        var rgb = ColorConversion.HslToRgb(hsl);
        return new SKColor(rgb.R, rgb.G, rgb.B, alpha);
    }

    #endregion

    #region Hex 便捷方法

    /// <summary>
    /// 将 SKColor 转换为 Hex 字符串
    /// </summary>
    /// <param name="color">SkiaSharp 颜色</param>
    /// <param name="includeAlpha">是否包含 Alpha 通道</param>
    public static string ToHex(SKColor color, bool includeAlpha = false) =>
        ColorConversion.ToHex(new RgbColor(color.Red, color.Green, color.Blue, color.Alpha), includeAlpha);

    /// <summary>
    /// 从 Hex 字符串解析为 SKColor
    /// </summary>
    /// <param name="hex">Hex 字符串</param>
    /// <exception cref="FormatException"></exception>
    public static SKColor FromHex(string hex)
    {
        var rgb = ColorConversion.ParseHex(hex);
        return new SKColor(rgb.R, rgb.G, rgb.B, rgb.A);
    }

    #endregion
}
