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

    /// <summary>
    /// 在验证码图像的四边绘制银灰色边框。
    /// </summary>
    /// <param name="image">要写入边框像素的图像。</param>
    /// <param name="width">图像宽度，单位为像素。</param>
    /// <param name="height">图像高度，单位为像素。</param>
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

    /// <summary>
    /// 根据验证码文本绘制指定数量的干扰线。
    /// </summary>
    /// <param name="image">要写入干扰线像素的图像。</param>
    /// <param name="code">用于确定干扰线位置和颜色种子的验证码文本。</param>
    /// <param name="width">绘制区域宽度，单位为像素。</param>
    /// <param name="height">绘制区域高度，单位为像素。</param>
    /// <param name="lineCount">要绘制的干扰线数量。</param>
    /// <param name="randomColor">是否根据文本种子生成彩色干扰线；为 false 时使用黑色。</param>
    /// <param name="background">传递给强调色计算的背景色参数；当前计算未使用该值。</param>
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

    /// <summary>
    /// 使用给定颜色绘制线段，并将越界像素裁剪到绘制区域之外。
    /// </summary>
    /// <param name="image">要写入线段像素的图像。</param>
    /// <param name="x0">线段起点的横坐标。</param>
    /// <param name="y0">线段起点的纵坐标。</param>
    /// <param name="x1">线段终点的横坐标。</param>
    /// <param name="y1">线段终点的纵坐标。</param>
    /// <param name="color">线段使用的颜色。</param>
    /// <param name="width">绘制区域宽度，单位为像素。</param>
    /// <param name="height">绘制区域高度，单位为像素。</param>
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

    /// <summary>
    /// 根据验证码文本在图像中绘制指定数量的干扰点。
    /// </summary>
    /// <param name="image">要写入干扰点像素的图像。</param>
    /// <param name="code">用于确定干扰点位置和颜色种子的验证码文本。</param>
    /// <param name="pointCount">要绘制的干扰点数量。</param>
    /// <param name="randomColor">是否根据文本和背景色生成彩色干扰点；为 false 时使用黑色。</param>
    /// <param name="background">参与干扰点颜色种子计算的背景色。</param>
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

    /// <summary>
    /// 根据配置将验证码文本绘制为内嵌点阵字形。
    /// </summary>
    /// <param name="image">要写入文本像素的图像。</param>
    /// <param name="code">要绘制的验证码文本；字符须存在于内嵌字形库中。</param>
    /// <param name="width">绘制区域宽度，单位为像素。</param>
    /// <param name="height">绘制区域高度，单位为像素。</param>
    /// <param name="fontSize">字形的像素尺寸。</param>
    /// <param name="fontWidth">每个字符的水平排布宽度，单位为像素。</param>
    /// <param name="options">控制文字颜色和位置的验证码配置。</param>
    /// <exception cref="NotSupportedException">文本包含内嵌字形库不支持的字符。</exception>
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

            // 先尝试 ASCII 点阵，再尝试中文点阵
            var asciiGlyph = BitmapFont.GetGlyph(c);
            if (asciiGlyph != null)
            {
                DrawGlyph(image, asciiGlyph, offsetX, offsetY, fontSize / 7, width, height, textColor);
                continue;
            }

            var chineseIndex = Internal.ChineseCaptchaGlyphSet.GetCharIndex(c);
            if (chineseIndex >= 0)
            {
                var glyphData = Internal.ChineseCaptchaGlyphSet.GetGlyphData(chineseIndex);
                if (glyphData != null)
                    DrawChineseGlyph(image, glyphData, offsetX, offsetY, fontSize, width, height, textColor);
                continue;
            }

            // 未知字符：显式失败而非静默跳过
            throw new NotSupportedException($"字符 '{c}'（U+{(int)c:X4}）不在内嵌字形库中。验证码文本必须由 ASCII 数字/字母或白名单中的中文字符组成。");
        }
    }

    /// <summary>
    /// 绘制 ASCII 点阵字形
    /// </summary>
    private static void DrawGlyph(Image<Rgba32> image, byte[,] glyph, int offsetX, int offsetY, int pixelSize, int width, int height, Rgba32 textColor)
    {
        pixelSize = Math.Max(1, pixelSize);
        var gw = glyph.GetLength(0);
        var gh = glyph.GetLength(1);

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

    /// <summary>
    /// 绘制 16x16 中文点阵字形
    /// </summary>
    private static void DrawChineseGlyph(Image<Rgba32> image, byte[] glyphData, int offsetX, int offsetY, int fontSize, int width, int height, Rgba32 textColor)
    {
        // 缩放像素大小以匹配目标字体尺寸
        var pixelSize = Math.Max(1, fontSize / 16);

        for (var row = 0; row < 16; row++)
        {
            var hi = glyphData[row * 2];
            var lo = glyphData[row * 2 + 1];

            for (var col = 0; col < 16; col++)
            {
                var bit = col < 8 ? (hi >> (7 - col)) & 1 : (lo >> (15 - col)) & 1;
                if (bit == 0)
                    continue;

                for (var px = 0; px < pixelSize; px++)
                {
                    for (var py = 0; py < pixelSize; py++)
                    {
                        var drawX = offsetX + col * pixelSize + px;
                        var drawY = offsetY + row * pixelSize + py;
                        if (drawX >= 0 && drawX < width && drawY >= 0 && drawY < height)
                            image[drawX, drawY] = textColor;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 根据种子和颜色开关创建不透明的验证码强调色。
    /// </summary>
    /// <param name="seed">用于确定颜色通道值的整数种子。</param>
    /// <param name="randomColor">是否生成种子决定的彩色；为 false 时返回黑色。</param>
    /// <param name="background">背景色参数；当前实现未使用该值。</param>
    /// <returns>颜色开关关闭时的不透明黑色，或由种子确定的不透明彩色。</returns>
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

        /// <summary>
        /// 将五行位图字形转换为列优先的像素矩阵。
        /// </summary>
        /// <param name="rows">每行包含五个字符的位图，字符 <c>1</c> 表示前景像素。</param>
        /// <returns>宽五、高七的矩阵；前景像素为 1，其余为 0。</returns>
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
