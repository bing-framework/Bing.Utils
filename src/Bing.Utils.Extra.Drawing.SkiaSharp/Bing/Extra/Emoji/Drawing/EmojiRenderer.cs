using System;
using System.IO;
using Bing.Extra.Emoji;
using SkiaSharp;

namespace Bing.Extra.Emoji.Drawing;

/// <summary>
/// 使用 SkiaSharp 将完整 Emoji 序列渲染为图像。
/// </summary>
/// <remarks>
/// 渲染使用调用方提供或平台默认的字体，不内置 Emoji 字体资源。复杂 ZWJ、肤色和旗帜序列的最终字形由字体与 SkiaSharp 后端决定。
/// </remarks>
public static class EmojiRenderer
{
    /// <summary>
    /// 渲染一个受支持的完整 Emoji 序列。
    /// </summary>
    /// <param name="unicode">完整 Emoji Unicode 序列，可为官方清单中的兼容形式。</param>
    /// <param name="options">渲染选项；为 null 时使用默认选项。</param>
    /// <returns>包含渲染结果的 SkiaSharp 图像；调用方负责释放。</returns>
    /// <exception cref="ArgumentNullException">unicode 为 null。</exception>
    /// <exception cref="ArgumentException">unicode 不是目录中的完整 Emoji，或字体路径与字体名称同时指定。</exception>
    /// <exception cref="ArgumentOutOfRangeException">字号或内边距超出支持范围。</exception>
    /// <exception cref="FileNotFoundException">指定字体文件不存在。</exception>
    /// <exception cref="InvalidDataException">字体无法加载或缺少完整 Emoji 序列的字形。</exception>
    public static SKImage Render(string unicode, EmojiRenderOptions? options = null)
    {
        if (unicode is null)
            throw new ArgumentNullException(nameof(unicode));
        if (!EmojiUtil.TryGetByUnicode(unicode, out var emoji))
            throw new ArgumentException("必须提供一个完整的受支持 Emoji 序列。", nameof(unicode));

        options ??= new EmojiRenderOptions();
        ValidateOptions(options);

        using var typeface = CreateTypeface(options, emoji.Unicode);
        using var paint = new SKPaint
        {
            Color = options.Color,
            IsAntialias = true,
            TextSize = options.FontSize,
            Typeface = typeface,
            FilterQuality = SKFilterQuality.High
        };

        var bounds = new SKRect();
        paint.MeasureText(emoji.Unicode, ref bounds);
        paint.GetFontMetrics(out var metrics);

        var width = CalculateDimension(bounds.Width, options.Padding);
        var height = CalculateDimension(metrics.Descent - metrics.Ascent, options.Padding);
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(options.Background);
        canvas.DrawText(emoji.Unicode, options.Padding - bounds.Left, options.Padding - metrics.Ascent, paint);
        canvas.Flush();

        return SKImage.FromBitmap(bitmap);
    }

    /// <summary>
    /// 校验渲染选项。
    /// </summary>
    /// <param name="options">待校验的渲染选项。</param>
    /// <exception cref="ArgumentException">字体路径与字体名称同时指定。</exception>
    /// <exception cref="ArgumentOutOfRangeException">字号或内边距超出支持范围。</exception>
    private static void ValidateOptions(EmojiRenderOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.FontPath) && !string.IsNullOrWhiteSpace(options.FontFamily))
            throw new ArgumentException("字体路径和字体名称不能同时指定。", nameof(options));
        if (float.IsNaN(options.FontSize) || float.IsInfinity(options.FontSize) || options.FontSize <= 0 || options.FontSize > 4096)
            throw new ArgumentOutOfRangeException(nameof(options.FontSize), "字号必须大于0且不超过4096像素。");
        if (options.Padding < 0 || options.Padding > 2048)
            throw new ArgumentOutOfRangeException(nameof(options.Padding), "内边距必须位于0到2048像素之间。");
    }

    /// <summary>
    /// 创建并校验渲染所用的字体。
    /// </summary>
    /// <param name="options">渲染选项。</param>
    /// <param name="unicode">规范 Emoji 序列。</param>
    /// <returns>加载的字体实例。</returns>
    /// <exception cref="FileNotFoundException">指定字体文件不存在。</exception>
    /// <exception cref="InvalidDataException">字体无法加载或缺少完整序列字形。</exception>
    private static SKTypeface CreateTypeface(EmojiRenderOptions options, string unicode)
    {
        SKTypeface? typeface;
        if (!string.IsNullOrWhiteSpace(options.FontPath))
        {
            var fontPath = options.FontPath!;
            if (!File.Exists(fontPath))
                throw new FileNotFoundException("字体文件不存在。", fontPath);
            typeface = SKTypeface.FromFile(fontPath);
            if (typeface is null)
                throw new InvalidDataException("无法加载指定字体文件。");
            ValidateGlyphs(typeface, unicode, fontPath);
            return typeface;
        }

        if (!string.IsNullOrWhiteSpace(options.FontFamily))
        {
            var fontFamily = options.FontFamily!;
            typeface = SKTypeface.FromFamilyName(fontFamily);
            if (typeface is null)
                throw new InvalidDataException("无法加载指定字体名称。");
            ValidateGlyphs(typeface, unicode, fontFamily);
            return typeface;
        }

        return SKTypeface.Default;
    }

    /// <summary>
    /// 确认显式字体包含完整 Emoji 序列的字形。
    /// </summary>
    /// <param name="typeface">待检查的字体。</param>
    /// <param name="unicode">规范 Emoji 序列。</param>
    /// <param name="source">字体来源描述。</param>
    /// <exception cref="InvalidDataException">字体缺少所需字形。</exception>
    private static void ValidateGlyphs(SKTypeface typeface, string unicode, string source)
    {
        if (!typeface.ContainsGlyphs(unicode))
            throw new InvalidDataException($"字体“{source}”缺少 Emoji 序列所需的完整字形。");
    }

    /// <summary>
    /// 根据字形尺寸和内边距计算图像边长。
    /// </summary>
    /// <param name="contentSize">字形内容尺寸。</param>
    /// <param name="padding">内边距。</param>
    /// <returns>至少为1像素的图像尺寸。</returns>
    private static int CalculateDimension(float contentSize, int padding)
    {
        var value = Math.Ceiling(contentSize + padding * 2d);
        if (double.IsNaN(value) || double.IsInfinity(value) || value > int.MaxValue)
            throw new InvalidDataException("字体测量结果超出图像尺寸范围。");
        return Math.Max(1, (int)value);
    }
}
