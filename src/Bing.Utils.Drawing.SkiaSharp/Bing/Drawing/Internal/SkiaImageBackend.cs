using System;
using System.IO;
using SkiaSharp;

namespace Bing.Drawing.Internal;

/// <summary>
/// SkiaSharp 对统一图像后端契约的实现。
/// </summary>
internal sealed class SkiaImageBackend : IImageBackend
{
    /// <inheritdoc />
    public ImageSurface Load(byte[] bytes)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length == 0)
            throw new InvalidDataException("图像数据不能为空。");

        try
        {
            // 这里保留编码数据中的原始像素方向，由公共管线按 EXIF 方向统一处理。
            var bitmap = SKBitmap.Decode(bytes);
            if (bitmap is null || bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                bitmap?.Dispose();
                throw new InvalidDataException("图像数据不包含有效像素。");
            }

            return new SkiaImageSurface(bitmap);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (!(exception is OutOfMemoryException))
        {
            throw new InvalidDataException("图像内容无效或格式不受支持。", exception);
        }
    }

    /// <inheritdoc />
    public ImageSurface Create(int width, int height, RgbColor background)
    {
        ValidateSize(width, height);
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        try
        {
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(ToColor(background));
            canvas.Flush();
            return new SkiaImageSurface(bitmap);
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public ImageSurface Resize(ImageSurface image, int width, int height)
    {
        ValidateSize(width, height);
        var source = Get(image);
        var output = CreateBitmap(width, height);
        try
        {
            using var canvas = new SKCanvas(output);
            using var paint = CreateBitmapPaint();
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(source.Bitmap, new SKRect(0, 0, width, height), paint);
            canvas.Flush();
            return new SkiaImageSurface(output);
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public ImageSurface Crop(ImageSurface image, int x, int y, int width, int height)
    {
        ValidateSize(width, height);
        var source = Get(image);
        if (x < 0 || y < 0 || x > source.Width - width || y > source.Height - height)
            throw new ArgumentOutOfRangeException(nameof(x), "裁剪区域必须位于源图像范围内。");

        var output = CreateBitmap(width, height);
        try
        {
            using var canvas = new SKCanvas(output);
            using var paint = CreateBitmapPaint();
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(source.Bitmap, new SKRect(x, y, x + width, y + height),
                new SKRect(0, 0, width, height), paint);
            canvas.Flush();
            return new SkiaImageSurface(output);
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public ImageSurface Rotate(ImageSurface image, int angle)
    {
        var source = Get(image);
        var normalizedAngle = NormalizeAngle(angle);
        if (normalizedAngle == 0)
            return new SkiaImageSurface(source.Bitmap.Copy());

        var radians = normalizedAngle * Math.PI / 180d;
        var width = Math.Max(1, checked((int)Math.Ceiling(source.Width * Math.Abs(Math.Cos(radians)) + source.Height * Math.Abs(Math.Sin(radians)) - 1e-9)));
        var height = Math.Max(1, checked((int)Math.Ceiling(source.Width * Math.Abs(Math.Sin(radians)) + source.Height * Math.Abs(Math.Cos(radians)) - 1e-9)));
        var output = CreateBitmap(width, height);
        try
        {
            using var canvas = new SKCanvas(output);
            using var paint = CreateBitmapPaint();
            canvas.Clear(SKColors.Transparent);
            canvas.Translate(width / 2f, height / 2f);
            canvas.RotateDegrees(normalizedAngle);
            canvas.Translate(-source.Width / 2f, -source.Height / 2f);
            canvas.DrawBitmap(source.Bitmap, 0, 0, paint);
            canvas.Flush();
            return new SkiaImageSurface(output);
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public ImageSurface Flip(ImageSurface image, bool horizontal, bool vertical)
    {
        var source = Get(image);
        var output = CreateBitmap(source.Width, source.Height);
        try
        {
            using var canvas = new SKCanvas(output);
            using var paint = CreateBitmapPaint();
            canvas.Clear(SKColors.Transparent);
            if (horizontal)
            {
                canvas.Translate(source.Width, 0);
                canvas.Scale(-1, 1);
            }

            if (vertical)
            {
                canvas.Translate(0, source.Height);
                canvas.Scale(1, -1);
            }

            canvas.DrawBitmap(source.Bitmap, 0, 0, paint);
            canvas.Flush();
            return new SkiaImageSurface(output);
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public void DrawImage(ImageSurface target, ImageSurface image, int x, int y, float opacity)
    {
        ValidateOpacity(opacity);
        var destination = Get(target);
        var source = Get(image);
        using var canvas = new SKCanvas(destination.Bitmap);
        using var paint = CreateBitmapPaint();
        paint.Color = SKColors.White.WithAlpha(ToAlpha(opacity));
        canvas.DrawBitmap(source.Bitmap, x, y, paint);
        canvas.Flush();
    }

    /// <inheritdoc />
    public void Frame(ImageSurface image, float radius, int borderWidth, RgbColor borderColor)
    {
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius));
        if (borderWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(borderWidth));
        if (radius == 0 && borderWidth == 0)
            return;

        var surface = Get(image);
        using var canvas = new SKCanvas(surface.Bitmap);
        var actualRadius = Math.Min(radius, Math.Min(surface.Width, surface.Height) / 2f);
        if (actualRadius > 0)
        {
            using var path = new SKPath();
            path.AddRoundRect(new SKRect(0, 0, surface.Width, surface.Height), actualRadius, actualRadius);
            canvas.Save();
            canvas.ClipPath(path, SKClipOperation.Difference, true);
            canvas.Clear(SKColors.Transparent);
            canvas.Restore();
        }

        if (borderWidth > 0)
        {
            if (borderWidth >= Math.Min(surface.Width, surface.Height))
            {
                using var fill = new SKPaint
                {
                    Color = ToColor(borderColor),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };
                var bounds = new SKRect(0, 0, surface.Width, surface.Height);
                if (actualRadius > 0)
                    canvas.DrawRoundRect(bounds, actualRadius, actualRadius, fill);
                else
                    canvas.DrawRect(bounds, fill);
                canvas.Flush();
                return;
            }

            var inset = borderWidth / 2f;
            var rectangle = new SKRect(inset, inset, surface.Width - inset, surface.Height - inset);
            using var paint = new SKPaint
            {
                Color = ToColor(borderColor),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = borderWidth,
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High
            };
            var borderRadius = Math.Max(0, actualRadius - inset);
            if (borderRadius > 0)
                canvas.DrawRoundRect(rectangle, borderRadius, borderRadius, paint);
            else
                canvas.DrawRect(rectangle, paint);
        }

        canvas.Flush();
    }

    /// <inheritdoc />
    public (int Width, int Height) MeasureText(ImageTextOptions text)
    {
        using var typeface = LoadTypeface(text, out var value);
        using var paint = CreateTextPaint(typeface, value);
        var bounds = new SKRect();
        paint.MeasureText(value.Text ?? string.Empty, ref bounds);
        paint.GetFontMetrics(out var metrics);
        return ((int)Math.Ceiling(Math.Max(0, bounds.Width)),
            (int)Math.Ceiling(Math.Max(0, metrics.Descent - metrics.Ascent)));
    }

    /// <inheritdoc />
    public void Annotate(ImageSurface image, ImageAnnotation annotation, float opacity)
    {
        if (annotation is null)
            throw new ArgumentNullException(nameof(annotation));
        ValidateOpacity(opacity);

        var surface = Get(image);
        var color = ToColor(annotation.Color).WithAlpha(ToAlpha(opacity * annotation.Color.A / 255f));
        var strokeWidth = Math.Max(1f, annotation.StrokeWidth);
        using var canvas = new SKCanvas(surface.Bitmap);
        using var paint = new SKPaint
        {
            Color = color,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        switch (annotation.Kind)
        {
            case ImageAnnotationKind.Rectangle:
                if (annotation.FillColor.HasValue)
                {
                    using var fill = CreateFillPaint(annotation.FillColor.Value, opacity);
                    canvas.DrawRect(new SKRect(annotation.X, annotation.Y, annotation.X + annotation.Width, annotation.Y + annotation.Height), fill);
                }
                canvas.DrawRect(new SKRect(annotation.X, annotation.Y, annotation.X + annotation.Width, annotation.Y + annotation.Height), paint);
                break;
            case ImageAnnotationKind.Ellipse:
                if (annotation.FillColor.HasValue)
                {
                    using var fill = CreateFillPaint(annotation.FillColor.Value, opacity);
                    canvas.DrawOval(new SKRect(annotation.X, annotation.Y, annotation.X + annotation.Width, annotation.Y + annotation.Height), fill);
                }
                canvas.DrawOval(new SKRect(annotation.X, annotation.Y, annotation.X + annotation.Width, annotation.Y + annotation.Height), paint);
                break;
            case ImageAnnotationKind.Line:
                canvas.DrawLine(annotation.X, annotation.Y, annotation.X2, annotation.Y2, paint);
                break;
            case ImageAnnotationKind.Arrow:
                DrawArrow(canvas, annotation, paint, strokeWidth);
                break;
            case ImageAnnotationKind.Text:
                if (annotation.Text is null)
                    throw new ArgumentException("文字标注必须提供Text。", nameof(annotation));
                using (var typeface = LoadTypeface(annotation.Text, out var text))
                using (var textPaint = CreateTextPaint(typeface, text))
                {
                    var textColor = ToColor(text.Color).WithAlpha(ToAlpha(opacity * text.Color.A / 255f));
                    textPaint.Color = textColor;
                    textPaint.GetFontMetrics(out var metrics);
                    var baseline = annotation.Y - metrics.Ascent;
                    canvas.DrawText(text.Text, annotation.X, baseline, textPaint);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(annotation.Kind));
        }

        canvas.Flush();
    }

    /// <inheritdoc />
    public byte[] Encode(ImageSurface image, ImageOutputFormat format, int quality, RgbColor background)
    {
        if (quality < 1 || quality > 100)
            throw new ArgumentOutOfRangeException(nameof(quality));

        var surface = Get(image);
        SKBitmap bitmap = null;
        try
        {
            bitmap = format == ImageOutputFormat.Jpeg ? Flatten(surface.Bitmap, background) : surface.Bitmap.Copy();
            using var encodedImage = SKImage.FromBitmap(bitmap);
            using var data = encodedImage.Encode(ToEncodedFormat(format), quality);
            if (data is null)
                throw new InvalidOperationException("SkiaSharp无法编码图像。");
            return data.ToArray();
        }
        finally
        {
            bitmap?.Dispose();
        }
    }

    /// <summary>
    /// 获取 SkiaSharp 图像表面实例。
    /// </summary>
    /// <param name="surface">统一图像表面。</param>
    /// <returns>底层 SkiaSharp 图像表面。</returns>
    private static SkiaImageSurface Get(ImageSurface surface)
    {
        if (surface is not SkiaImageSurface typed)
            throw new ArgumentException("图像表面不是SkiaSharp实例。", nameof(surface));
        return typed;
    }

    /// <summary>
    /// 创建指定尺寸的 RGBA 位图。
    /// </summary>
    /// <param name="width">位图宽度。</param>
    /// <param name="height">位图高度。</param>
    /// <returns>创建的 RGBA 位图。</returns>
    private static SKBitmap CreateBitmap(int width, int height) =>
        new(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);

    /// <summary>
    /// 创建图像绘制画笔。
    /// </summary>
    /// <returns>配置好的图像画笔。</returns>
    private static SKPaint CreateBitmapPaint() => new()
    {
        FilterQuality = SKFilterQuality.High,
        IsAntialias = true,
        BlendMode = SKBlendMode.SrcOver
    };

    /// <summary>
    /// 创建带透明度的填充画笔。
    /// </summary>
    /// <param name="color">填充颜色。</param>
    /// <param name="opacity">透明度。</param>
    /// <returns>配置好的填充画笔。</returns>
    private static SKPaint CreateFillPaint(RgbColor color, float opacity) => new()
    {
        Color = ToColor(color).WithAlpha(ToAlpha(opacity * color.A / 255f)),
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
        FilterQuality = SKFilterQuality.High
    };

    /// <summary>
    /// 创建文本绘制画笔。
    /// </summary>
    /// <param name="typeface">文本字体。</param>
    /// <param name="text">文本选项。</param>
    /// <returns>配置好的文本画笔。</returns>
    private static SKPaint CreateTextPaint(SKTypeface typeface, ImageTextOptions text) => new()
    {
        Color = ToColor(text.Color),
        IsAntialias = true,
        TextSize = text.FontSize,
        Typeface = typeface,
        FilterQuality = SKFilterQuality.High
    };

    /// <summary>
    /// 加载并验证文本使用的外部字体。
    /// </summary>
    /// <param name="text">文本选项。</param>
    /// <param name="value">验证后的文本选项。</param>
    /// <returns>加载的字体。</returns>
    private static SKTypeface LoadTypeface(ImageTextOptions text, out ImageTextOptions value)
    {
        if (text is null)
            throw new ArgumentNullException(nameof(text));

        // 统一字形校验先读取外部字体 cmap，避免 Skia 在缺字形时静默替换系统字体。
        ImagePipeline.ValidateText(text);

        var typeface = SKTypeface.FromFile(text.FontPath);
        if (typeface is null)
            throw new InvalidDataException("无法加载外部字体文件。");
        if (!string.IsNullOrEmpty(text.Text) && !typeface.ContainsGlyphs(text.Text))
        {
            typeface.Dispose();
            throw new InvalidDataException("外部字体缺少所需字形。");
        }

        value = text;
        return typeface;
    }

    /// <summary>
    /// 绘制箭头标注。
    /// </summary>
    /// <param name="canvas">SkiaSharp 画布。</param>
    /// <param name="annotation">箭头标注。</param>
    /// <param name="paint">线条画笔。</param>
    /// <param name="strokeWidth">线条宽度。</param>
    private static void DrawArrow(SKCanvas canvas, ImageAnnotation annotation, SKPaint paint, float strokeWidth)
    {
        canvas.DrawLine(annotation.X, annotation.Y, annotation.X2, annotation.Y2, paint);
        var angle = Math.Atan2(annotation.Y2 - annotation.Y, annotation.X2 - annotation.X);
        var size = Math.Max(6f, strokeWidth * 4f);
        var left = new SKPoint(annotation.X2 - (float)(Math.Cos(angle - Math.PI / 6) * size),
            annotation.Y2 - (float)(Math.Sin(angle - Math.PI / 6) * size));
        var right = new SKPoint(annotation.X2 - (float)(Math.Cos(angle + Math.PI / 6) * size),
            annotation.Y2 - (float)(Math.Sin(angle + Math.PI / 6) * size));
        canvas.DrawLine(annotation.X2, annotation.Y2, left.X, left.Y, paint);
        canvas.DrawLine(annotation.X2, annotation.Y2, right.X, right.Y, paint);
    }

    /// <summary>
    /// 将图像绘制到指定背景色上以生成不透明位图。
    /// </summary>
    /// <param name="source">源位图。</param>
    /// <param name="background">背景颜色。</param>
    /// <returns>绘制后的不透明位图。</returns>
    private static SKBitmap Flatten(SKBitmap source, RgbColor background)
    {
        var output = CreateBitmap(source.Width, source.Height);
        try
        {
            using var canvas = new SKCanvas(output);
            using var paint = CreateBitmapPaint();
            canvas.Clear(ToColor(background));
            canvas.DrawBitmap(source, 0, 0, paint);
            canvas.Flush();
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 将统一输出格式转换为 SkiaSharp 编码格式。
    /// </summary>
    /// <param name="format">统一输出格式。</param>
    /// <returns>SkiaSharp 编码格式。</returns>
    private static SKEncodedImageFormat ToEncodedFormat(ImageOutputFormat format) => format switch
    {
        ImageOutputFormat.Png => SKEncodedImageFormat.Png,
        ImageOutputFormat.Jpeg => SKEncodedImageFormat.Jpeg,
        ImageOutputFormat.WebP => SKEncodedImageFormat.Webp,
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    /// <summary>
    /// 将统一 RGB 颜色转换为 SkiaSharp 颜色。
    /// </summary>
    /// <param name="color">统一 RGB 颜色。</param>
    /// <returns>SkiaSharp 颜色。</returns>
    private static SKColor ToColor(RgbColor color) => new(color.R, color.G, color.B, color.A);

    /// <summary>
    /// 将 0 到 1 的透明度转换为 8 位 Alpha 值。
    /// </summary>
    /// <param name="opacity">透明度。</param>
    /// <returns>8 位 Alpha 值。</returns>
    private static byte ToAlpha(float opacity)
    {
        if (opacity <= 0)
            return 0;
        if (opacity >= 1)
            return byte.MaxValue;
        return (byte)Math.Round(opacity * 255f, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// 验证图像尺寸为正数。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    private static void ValidateSize(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
    }

    /// <summary>
    /// 验证透明度位于 0 到 1 之间。
    /// </summary>
    /// <param name="opacity">透明度。</param>
    private static void ValidateOpacity(float opacity)
    {
        if (float.IsNaN(opacity) || opacity < 0 || opacity > 1)
            throw new ArgumentOutOfRangeException(nameof(opacity));
    }

    /// <summary>
    /// 将旋转角度规范化到 0 到 359 度。
    /// </summary>
    /// <param name="angle">原始旋转角度。</param>
    /// <returns>规范化后的角度。</returns>
    private static int NormalizeAngle(int angle)
    {
        var normalized = angle % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    /// <summary>
    /// 封装 SkiaSharp 位图并提供统一表面生命周期。
    /// </summary>
    private sealed class SkiaImageSurface : ImageSurface
    {
        /// <summary>
        /// 初始化 <see cref="SkiaImageSurface" /> 类的新实例。
        /// </summary>
        /// <param name="bitmap">底层 SkiaSharp 位图。</param>
        internal SkiaImageSurface(SKBitmap bitmap) => Bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));

        /// <summary>
        /// 获取底层 SkiaSharp 位图。
        /// </summary>
        internal SKBitmap Bitmap { get; }

        /// <inheritdoc />
        internal override int Width => Bitmap.Width;

        /// <inheritdoc />
        internal override int Height => Bitmap.Height;

        /// <inheritdoc />
        public override void Dispose() => Bitmap.Dispose();
    }
}
