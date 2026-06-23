using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

// 图片操作辅助类 - 颜色转换适配
public static partial class ImageSharpHelper
{
    #region Rgba32 <-> RgbColor

    /// <summary>
    /// 将 Rgba32 转换为 Shared RgbColor
    /// </summary>
    /// <param name="color">ImageSharp 颜色</param>
    public static RgbColor ToRgbColor(Rgba32 color) => new(color.R, color.G, color.B, color.A);

    /// <summary>
    /// 将 Shared RgbColor 转换为 Rgba32
    /// </summary>
    /// <param name="color">Shared 颜色</param>
    public static Rgba32 ToRgba32(RgbColor color) => new(color.R, color.G, color.B, color.A);

    /// <summary>
    /// 将 Shared RgbColor 转换为 ImageSharp Color
    /// </summary>
    /// <param name="color">Shared 颜色</param>
    public static Color ToColor(RgbColor color) => Color.FromRgba(color.R, color.G, color.B, color.A);

    #endregion

    #region HSL 便捷方法

    /// <summary>
    /// 将 Rgba32 转换为 HSL 颜色
    /// </summary>
    /// <param name="color">ImageSharp 颜色</param>
    public static HslColor ToHsl(Rgba32 color) => ColorConversion.RgbToHsl(new RgbColor(color.R, color.G, color.B));

    /// <summary>
    /// 将 HSL 颜色转换为 Rgba32
    /// </summary>
    /// <param name="hsl">HSL 颜色</param>
    /// <param name="alpha">Alpha 值，默认 255</param>
    public static Rgba32 FromHsl(HslColor hsl, byte alpha = 255)
    {
        var rgb = ColorConversion.HslToRgb(hsl);
        return new Rgba32(rgb.R, rgb.G, rgb.B, alpha);
    }

    #endregion

    #region Hex 便捷方法

    /// <summary>
    /// 将 Rgba32 转换为 Hex 字符串
    /// </summary>
    /// <param name="color">ImageSharp 颜色</param>
    /// <param name="includeAlpha">是否包含 Alpha 通道</param>
    public static string ToHex(Rgba32 color, bool includeAlpha = false) =>
        ColorConversion.ToHex(new RgbColor(color.R, color.G, color.B, color.A), includeAlpha);

    /// <summary>
    /// 从 Hex 字符串解析为 Rgba32
    /// </summary>
    /// <param name="hex">Hex 字符串</param>
    /// <exception cref="FormatException"></exception>
    public static Rgba32 FromHex(string hex)
    {
        var rgb = ColorConversion.ParseHex(hex);
        return new Rgba32(rgb.R, rgb.G, rgb.B, rgb.A);
    }

    #endregion
}
