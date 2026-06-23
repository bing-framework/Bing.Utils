using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

// 图片操作辅助类 - 验证码
public static partial class ImageSharpHelper
{
    #region GetCaptchaCode(获取验证码文本 - 带类型)

    /// <summary>
    /// 按类型获取验证码文本
    /// </summary>
    /// <param name="length">验证码长度</param>
    /// <param name="captchaType">验证码类型</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string GetCaptchaCode(int length, CaptchaType captchaType)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        return CaptchaCodeGenerator.Generate(length, captchaType);
    }

    #endregion

    #region CreateCaptchaImage(创建验证码图片)

    /// <summary>
    /// 创建验证码图片
    /// </summary>
    /// <param name="code">验证码文本</param>
    /// <param name="options">验证码配置</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Image CreateCaptchaImage(string code, CaptchaOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentNullException(nameof(code));

        options = options ?? new CaptchaOptions();
        var fontSize = options.FontSize;
        var fontWidth = options.FontWidth;
        var width = fontWidth * code.Length + fontWidth;
        var height = fontSize + fontSize / 2;
        var background = new Rgba32(options.BackgroundR, options.BackgroundG, options.BackgroundB, 255);

        var output = new Image<Rgba32>(width, height);

        // 填充背景
        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
            output[x, y] = background;

        // 绘制边框
        if (options.HasBorder)
            DrawCaptchaBorder(output, width, height);

        // 绘制干扰线
        DrawCaptchaDisorderLine(output, code, width, height, options.NoiseLineCount, options.RandomColor, background);

        // 绘制干扰点
        var pointCount = options.NoisePointCount >= 0
            ? options.NoisePointCount
            : Math.Max(6, width * height / 45);
        DrawCaptchaDisorderPoint(output, code, pointCount, options.RandomColor, background);

        // 绘制文本（使用内嵌 5x7 点阵字体）
        DrawCaptchaText(output, code, width, height, fontSize, fontWidth, options);

        return output;
    }

    /// <summary>
    /// 创建指定长度的验证码图片
    /// </summary>
    /// <param name="length">验证码长度</param>
    /// <param name="code">生成的验证码文本</param>
    /// <param name="captchaType">验证码类型</param>
    /// <param name="options">验证码配置</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Image CreateCaptchaImage(int length, out string code, CaptchaType captchaType = CaptchaType.NumberAndLetter, CaptchaOptions? options = null)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        code = GetCaptchaCode(length, captchaType);
        return CreateCaptchaImage(code, options);
    }

    #endregion

    #region 验证码绘制内部方法

    private static void DrawCaptchaBorder(Image<Rgba32> image, int width, int height)
    {
        var borderColor = new Rgba32(192, 192, 192, 255); // Silver
        for (var x = 0; x < width; x++)
        {
            image[x, 0] = borderColor;
            image[x, height - 1] = borderColor;
        }

        for (var y = 0; y < height; y++)
        {
            image[0, y] = borderColor;
            image[width - 1, y] = borderColor;
        }
    }

    private static void DrawCaptchaDisorderLine(Image<Rgba32> image, string code, int width, int height, int lineCount, bool randomColor, Rgba32 background)
    {
        for (var i = 0; i < lineCount; i++)
        {
            var seed = code[i % code.Length] + i * 31;
            var color = CreateCaptchaAccentColor(seed, randomColor, background);

            var startX = Math.Abs(seed * 7) % width;
            var startY = Math.Abs(seed * 11) % height;
            var endX = width - 1 - (Math.Abs(seed * 13) % width);
            var endY = height - 1 - (Math.Abs(seed * 17) % height);

            DrawLine(image, startX, startY, endX, endY, color, width, height);
        }
    }

    private static void DrawLine(Image<Rgba32> image, int x0, int y0, int x1, int y1, Rgba32 color, int width, int height)
    {
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
                image[x0, y0] = color;

            if (x0 == x1 && y0 == y1)
                break;

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private static void DrawCaptchaDisorderPoint(Image<Rgba32> image, string code, int pointCount, bool randomColor, Rgba32 background)
    {
        var width = image.Width;
        var height = image.Height;
        for (var i = 0; i < pointCount; i++)
        {
            var seed = code[i % code.Length] + i * 53;
            var x = Math.Abs(seed * 19 + i * 7) % width;
            var y = Math.Abs(seed * 23 + i * 11) % height;
            var color = CreateCaptchaAccentColor(seed + background.R + background.G + background.B, randomColor, background);
            image[x, y] = color;
        }
    }

    private static void DrawCaptchaText(Image<Rgba32> image, string code, int width, int height, int fontSize, int fontWidth, CaptchaOptions options)
    {
        var textColor = options.RandomColor
            ? new Rgba32(24, 24, 24, 255)
            : new Rgba32((byte)(255 - options.BackgroundR), (byte)(255 - options.BackgroundG), (byte)(255 - options.BackgroundB), 255);

        var cellWidth = fontWidth;
        var baseline = Math.Max(2, (height - fontSize) / 2);

        for (var i = 0; i < code.Length; i++)
        {
            var c = code[i];
            var seed = c + i * 17;
            var offsetX = cellWidth / 4 + cellWidth * i + (options.RandomPosition ? (seed % (cellWidth / 2)) - cellWidth / 4 : 0);
            var offsetY = baseline + (options.RandomPosition ? (seed % 5) - 2 : 0);

            var glyph = BitmapFont.GetGlyph(c);
            if (glyph == null)
                continue;

            var gw = glyph.GetLength(0);
            var gh = glyph.GetLength(1);
            var pixelSize = Math.Max(1, fontSize / 7);

            for (var gx = 0; gx < gw; gx++)
            {
                for (var gy = 0; gy < gh; gy++)
                {
                    if (glyph[gx, gy] == 0)
                        continue;

                    for (var px = 0; px < pixelSize; px++)
                    {
                        for (var py = 0; py < pixelSize; py++)
                        {
                            var drawX = offsetX + gx * pixelSize + px;
                            var drawY = offsetY + gy * pixelSize + py;
                            if (drawX >= 0 && drawX < width && drawY >= 0 && drawY < height)
                                image[drawX, drawY] = textColor;
                        }
                    }
                }
            }
        }
    }

    private static Rgba32 CreateCaptchaAccentColor(int seed, bool randomColor, Rgba32 background)
    {
        if (!randomColor)
            return new Rgba32(0, 0, 0, 255);

        var normalized = Math.Abs(seed);
        return new Rgba32(
            (byte)(20 + normalized % 120),
            (byte)(20 + normalized * 3 % 120),
            (byte)(20 + normalized * 5 % 120),
            255);
    }

    #endregion

    #region 内嵌点阵字体（5x7 ASCII 子集）

    /// <summary>
    /// 内嵌 5x7 点阵字体，支持数字和常见字母
    /// </summary>
    private static class BitmapFont
    {
        /// <summary>
        /// 获取字符的 5x7 点阵
        /// </summary>
        internal static byte[,]? GetGlyph(char c)
        {
            switch (c)
            {
                case '0': return Glyph(new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" });
                case '1': return Glyph(new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" });
                case '2': return Glyph(new[] { "01110", "10001", "00001", "00110", "01000", "10000", "11111" });
                case '3': return Glyph(new[] { "01110", "10001", "00001", "00110", "00001", "10001", "01110" });
                case '4': return Glyph(new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" });
                case '5': return Glyph(new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" });
                case '6': return Glyph(new[] { "01110", "10000", "11110", "10001", "10001", "10001", "01110" });
                case '7': return Glyph(new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" });
                case '8': return Glyph(new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" });
                case '9': return Glyph(new[] { "01110", "10001", "10001", "01111", "00001", "00001", "01110" });
                case 'A': case 'a': return Glyph(new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" });
                case 'B': case 'b': return Glyph(new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" });
                case 'C': case 'c': return Glyph(new[] { "01110", "10001", "10000", "10000", "10000", "10001", "01110" });
                case 'D': case 'd': return Glyph(new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" });
                case 'E': case 'e': return Glyph(new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" });
                case 'F': case 'f': return Glyph(new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" });
                case 'G': case 'g': return Glyph(new[] { "01110", "10001", "10000", "10111", "10001", "10001", "01110" });
                case 'H': case 'h': return Glyph(new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" });
                case 'J': case 'j': return Glyph(new[] { "00111", "00010", "00010", "00010", "00010", "10010", "01100" });
                case 'K': case 'k': return Glyph(new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" });
                case 'M': case 'm': return Glyph(new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" });
                case 'N': case 'n': return Glyph(new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" });
                case 'P': case 'p': return Glyph(new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" });
                case 'Q': case 'q': return Glyph(new[] { "01110", "10001", "10001", "10001", "10101", "01110", "00001" });
                case 'R': case 'r': return Glyph(new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" });
                case 'S': case 's': return Glyph(new[] { "01110", "10001", "10000", "01110", "00001", "10001", "01110" });
                case 'T': case 't': return Glyph(new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" });
                case 'U': case 'u': return Glyph(new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" });
                case 'V': case 'v': return Glyph(new[] { "10001", "10001", "10001", "10001", "01010", "01010", "00100" });
                case 'W': case 'w': return Glyph(new[] { "10001", "10001", "10001", "10101", "10101", "11011", "10001" });
                case 'X': case 'x': return Glyph(new[] { "10001", "10001", "01010", "00100", "01010", "10001", "10001" });
                case 'Y': case 'y': return Glyph(new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" });
                case 'Z': case 'z': return Glyph(new[] { "11111", "00001", "00010", "00100", "01000", "10000", "11111" });
                default: return null;
            }
        }

        private static byte[,] Glyph(string[] rows)
        {
            var result = new byte[5, 7];
            for (var y = 0; y < 7; y++)
            for (var x = 0; x < 5; x++)
                result[x, y] = (byte)(rows[y][x] == '1' ? 1 : 0);
            return result;
        }
    }

    #endregion
}
