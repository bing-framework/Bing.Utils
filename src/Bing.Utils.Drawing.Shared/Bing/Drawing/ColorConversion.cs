namespace Bing.Drawing;

/// <summary>
/// 颜色空间转换工具（纯算法，不依赖 System.Drawing / ImageSharp / SkiaSharp）。
/// </summary>
public static class ColorConversion
{
    #region RgbToHsl / HslToRgb

    /// <summary>
    /// RGB 转 HSL
    /// </summary>
    /// <param name="color">RGB 颜色</param>
    public static HslColor RgbToHsl(RgbColor color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        double h, s;
        var l = (max + min) / 2.0;

        if (Math.Abs(max - min) < 1e-10)
        {
            h = 0;
            s = 0;
        }
        else
        {
            var d = max - min;
            s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

            if (Math.Abs(max - r) < 1e-10)
                h = (g - b) / d + (g < b ? 6 : 0);
            else if (Math.Abs(max - g) < 1e-10)
                h = (b - r) / d + 2;
            else
                h = (r - g) / d + 4;

            h *= 60;
        }

        return new HslColor(h, s, l);
    }

    /// <summary>
    /// HSL 转 RGB
    /// </summary>
    /// <param name="color">HSL 颜色</param>
    public static RgbColor HslToRgb(HslColor color)
    {
        var h = color.H;
        var s = color.S;
        var l = color.L;

        if (s < 1e-10)
        {
            var v = (byte)DrawingCompatibilityHelper.ClampToByte((int)Math.Round(l * 255, MidpointRounding.AwayFromZero));
            return new RgbColor(v, v, v);
        }

        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        var hNorm = h / 360.0;

        return new RgbColor(
            HueToChannel(p, q, hNorm + 1.0 / 3),
            HueToChannel(p, q, hNorm),
            HueToChannel(p, q, hNorm - 1.0 / 3));
    }

    /// <summary>
    /// HSL 色相到单通道转换
    /// </summary>
    private static byte HueToChannel(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;

        double value;
        if (t * 6 < 1)
            value = p + (q - p) * 6 * t;
        else if (t * 2 < 1)
            value = q;
        else if (t * 3 < 2)
            value = p + (q - p) * (2.0 / 3 - t) * 6;
        else
            value = p;

        return DrawingCompatibilityHelper.ClampToByte((int)Math.Round(value * 255, MidpointRounding.AwayFromZero));
    }

    #endregion

    #region sRGB / Linear RGB

    /// <summary>
    /// 将 sRGB 单通道值（0-1）转换为线性 RGB
    /// </summary>
    /// <param name="sRgb">sRGB 值，范围 0-1</param>
    /// <exception cref="ArgumentOutOfRangeException">输入超出 0-1 范围</exception>
    public static double SRgbToLinearRgb(double sRgb)
    {
        if (sRgb < 0 || sRgb > 1)
            throw new ArgumentOutOfRangeException(nameof(sRgb), "sRGB 值必须在 0-1 之间");

        if (sRgb <= 0.04045)
            return sRgb / 12.92;
        return Math.Pow((sRgb + 0.055) / 1.055, 2.4);
    }

    /// <summary>
    /// 将线性 RGB 单通道值（0-1）转换为 sRGB
    /// </summary>
    /// <param name="linearRgb">线性 RGB 值，范围 0-1</param>
    /// <exception cref="ArgumentOutOfRangeException">输入超出 0-1 范围</exception>
    public static double LinearRgbToSRgb(double linearRgb)
    {
        if (linearRgb < 0 || linearRgb > 1)
            throw new ArgumentOutOfRangeException(nameof(linearRgb), "线性 RGB 值必须在 0-1 之间");

        if (linearRgb < 0.0031308)
            return 12.92 * linearRgb;
        return Math.Pow(linearRgb, 1.0 / 2.4) * 1.055 - 0.055;
    }

    #endregion

    #region Hex

    /// <summary>
    /// 将 RGB 颜色转换为 Hex 字符串（#RRGGBB 格式）
    /// </summary>
    /// <param name="color">RGB 颜色</param>
    /// <param name="includeAlpha">是否包含 Alpha 通道</param>
    public static string ToHex(RgbColor color, bool includeAlpha = false)
    {
        return includeAlpha
            ? $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}"
            : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>
    /// 将 RGB 颜色转换为 RGB(...) 字符串
    /// </summary>
    /// <param name="color">RGB 颜色</param>
    /// <param name="includeAlpha">是否包含 Alpha 通道</param>
    public static string ToRgbString(RgbColor color, bool includeAlpha = false)
    {
        return includeAlpha
            ? $"RGBA({color.R},{color.G},{color.B},{color.A})"
            : $"RGB({color.R},{color.G},{color.B})";
    }

    /// <summary>
    /// 解析 Hex 字符串为 RGB 颜色。支持格式：#RRGGBB、RRGGBB、#AARRGGBB、AARRGGBB
    /// </summary>
    /// <param name="text">Hex 字符串</param>
    /// <exception cref="FormatException">格式不合法</exception>
    public static RgbColor ParseHex(string text)
    {
        if (TryParseHex(text, out var color))
            return color;
        throw new FormatException($"无法解析颜色 Hex 字符串：{text}");
    }

    /// <summary>
    /// 尝试解析 Hex 字符串为 RGB 颜色。支持格式：#RRGGBB、RRGGBB、#AARRGGBB、AARRGGBB
    /// </summary>
    /// <param name="text">Hex 字符串</param>
    /// <param name="color">解析结果</param>
    public static bool TryParseHex(string? text, out RgbColor color)
    {
        color = default;
        if (text is null)
            return false;

        var hex = text.Trim();
        if (hex.Length == 0)
            return false;
        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (hex.Length == 6)
        {
            if (!TryParseHexChannel(hex, 0, out var r) ||
                !TryParseHexChannel(hex, 2, out var g) ||
                !TryParseHexChannel(hex, 4, out var b))
                return false;
            color = new RgbColor(r, g, b);
            return true;
        }

        if (hex.Length == 8)
        {
            if (!TryParseHexChannel(hex, 0, out var a) ||
                !TryParseHexChannel(hex, 2, out var r) ||
                !TryParseHexChannel(hex, 4, out var g) ||
                !TryParseHexChannel(hex, 6, out var b))
                return false;
            color = new RgbColor(r, g, b, a);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试解析两位 Hex 为 byte
    /// </summary>
    private static bool TryParseHexChannel(string hex, int offset, out byte value)
    {
        value = 0;
        var part = hex.Substring(offset, 2);
        if (!int.TryParse(part, System.Globalization.NumberStyles.HexNumber, null, out var parsed))
            return false;
        value = (byte)parsed;
        return true;
    }

    #endregion
}
