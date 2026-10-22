using SixLabors.ImageSharp.Drawing;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Bing.Drawing.Internal;

/// <summary>
/// ImageSharp 对统一图像后端契约的实现。
/// </summary>
internal sealed class ImageSharpImageBackend : IImageBackend
{
    /// <inheritdoc />
    public ImageSurface Load(byte[] bytes)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));
        return new ImageSharpSurface(Image.Load<Rgba32>(bytes));
    }

    /// <inheritdoc />
    public ImageSurface Create(int width, int height, RgbColor background)
    {
        ValidateSize(width, height);
        return new ImageSharpSurface(new Image<Rgba32>(width, height, ToColor(background)));
    }

    /// <inheritdoc />
    public ImageSurface Resize(ImageSurface image, int width, int height)
    {
        ValidateSize(width, height);
        var source = Get(image);
        var result = source.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Stretch
        }));
        return new ImageSharpSurface(result);
    }

    /// <inheritdoc />
    public ImageSurface Crop(ImageSurface image, int x, int y, int width, int height)
    {
        ValidateSize(width, height);
        var source = Get(image);
        return new ImageSharpSurface(source.Clone(ctx => ctx.Crop(new Rectangle(x, y, width, height))));
    }

    /// <inheritdoc />
    public ImageSurface Rotate(ImageSurface image, int angle)
    {
        var source = Get(image);
        return new ImageSharpSurface(source.Clone(ctx => ctx.Rotate(angle)));
    }

    /// <inheritdoc />
    public ImageSurface Flip(ImageSurface image, bool horizontal, bool vertical)
    {
        var result = Get(image).Clone();
        try
        {
            if (horizontal)
                result.Mutate(ctx => ctx.Flip(FlipMode.Horizontal));
            if (vertical)
                result.Mutate(ctx => ctx.Flip(FlipMode.Vertical));
            return new ImageSharpSurface(result);
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public void DrawImage(ImageSurface target, ImageSurface image, int x, int y, float opacity)
    {
        ValidateOpacity(opacity);
        Get(target).Mutate(ctx => ctx.DrawImage(Get(image), new Point(x, y), opacity));
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
        var actualRadius = Math.Min(radius, Math.Min(surface.Width, surface.Height) / 2f);
        surface.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < surface.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < surface.Width; x++)
                {
                    var dx = Math.Max(actualRadius - (x + .5f), Math.Max(x + .5f - (surface.Width - actualRadius), 0));
                    var dy = Math.Max(actualRadius - (y + .5f), Math.Max(y + .5f - (surface.Height - actualRadius), 0));
                    var distance = Math.Sqrt(dx * dx + dy * dy);
                    if (actualRadius > 0 && distance > actualRadius)
                    {
                        row[x].A = 0;
                        continue;
                    }

                    if (borderWidth > 0 && (x < borderWidth || y < borderWidth ||
                        x >= surface.Width - borderWidth || y >= surface.Height - borderWidth ||
                        (actualRadius > 0 && distance > Math.Max(0, actualRadius - borderWidth))))
                        row[x] = new Rgba32(borderColor.R, borderColor.G, borderColor.B, borderColor.A);
                }
            }
        });
    }

    /// <inheritdoc />
    public (int Width, int Height) MeasureText(ImageTextOptions text)
    {
        var font = LoadFont(text);
        var size = TextMeasurer.MeasureSize(text.Text ?? string.Empty, new TextOptions(font) { Dpi = 72 });
        return ((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));
    }

    /// <inheritdoc />
    public void Annotate(ImageSurface image, ImageAnnotation annotation, float opacity)
    {
        if (annotation is null)
            throw new ArgumentNullException(nameof(annotation));
        ValidateOpacity(opacity);
        var surface = Get(image);
        var color = ToColor(annotation.Color).WithAlpha((byte)Math.Round(annotation.Color.A * opacity));
        var width = Math.Max(1, annotation.StrokeWidth);
        surface.Mutate(ctx =>
        {
            switch (annotation.Kind)
            {
                case ImageAnnotationKind.Rectangle:
                    DrawRectangle(ctx, annotation, color, width, opacity);
                    break;
                case ImageAnnotationKind.Ellipse:
                    if (annotation.FillColor.HasValue)
                        ctx.Fill(ToColor(annotation.FillColor.Value).WithAlpha((byte)Math.Round(annotation.FillColor.Value.A * opacity)),
                            new EllipsePolygon(annotation.X + annotation.Width / 2f,
                                annotation.Y + annotation.Height / 2f,
                                Math.Abs(annotation.Width / 2f), Math.Abs(annotation.Height / 2f)));
                    ctx.Draw(color, width, new EllipsePolygon(
                        annotation.X + annotation.Width / 2f,
                        annotation.Y + annotation.Height / 2f,
                        Math.Abs(annotation.Width / 2f),
                        Math.Abs(annotation.Height / 2f)));
                    break;
                case ImageAnnotationKind.Line:
                    ctx.DrawLine(color, width, new PointF(annotation.X, annotation.Y),
                        new PointF(annotation.X2, annotation.Y2));
                    break;
                case ImageAnnotationKind.Arrow:
                    DrawArrow(ctx, annotation, color, width);
                    break;
                case ImageAnnotationKind.Text:
                    if (annotation.Text is null)
                        throw new ArgumentException("文字标注必须提供Text。", nameof(annotation));
                    var textColor = ToColor(annotation.Text.Color)
                        .WithAlpha((byte)Math.Round(annotation.Text.Color.A * opacity));
                    var richTextOptions = new RichTextOptions(LoadFont(annotation.Text))
                    {
                        Dpi = 72,
                        Origin = new Vector2(annotation.X, annotation.Y)
                    };
                    ctx.DrawText(richTextOptions, annotation.Text.Text ?? string.Empty, textColor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(annotation.Kind));
            }
        });
    }

    /// <inheritdoc />
    public byte[] Encode(ImageSurface image, ImageOutputFormat format, int quality, RgbColor background)
    {
        if (quality < 1 || quality > 100)
            throw new ArgumentOutOfRangeException(nameof(quality));

        using var source = Get(image).Clone();
        using var flattened = format == ImageOutputFormat.Jpeg ? Flatten(source, background) : null;
        var output = flattened ?? source;
        using var stream = new MemoryStream();
        switch (format)
        {
            case ImageOutputFormat.Png:
                output.Save(stream, new PngEncoder());
                break;
            case ImageOutputFormat.Jpeg:
                output.Save(stream, new JpegEncoder { Quality = quality });
                break;
            case ImageOutputFormat.WebP:
                output.Save(stream, new WebpEncoder { Quality = quality });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
        return stream.ToArray();
    }

    /// <summary>
    /// 获取 ImageSharp 图像实例。
    /// </summary>
    /// <param name="surface">统一图像表面。</param>
    /// <returns>底层 ImageSharp 图像。</returns>
    private static Image<Rgba32> Get(ImageSurface surface)
    {
        if (surface is not ImageSharpSurface typed)
            throw new ArgumentException("图像表面不是 ImageSharp 实例。", nameof(surface));
        return typed.Image;
    }

    /// <summary>
    /// 将图像绘制到指定背景色上以生成不透明图像。
    /// </summary>
    /// <param name="source">源图像。</param>
    /// <param name="background">背景颜色。</param>
    /// <returns>绘制后的不透明图像。</returns>
    private static Image<Rgba32> Flatten(Image<Rgba32> source, RgbColor background)
    {
        var output = new Image<Rgba32>(source.Width, source.Height, ToColor(background));
        try
        {
            output.Mutate(ctx => ctx.DrawImage(source, Point.Empty, 1f));
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 加载并验证文本使用的外部字体。
    /// </summary>
    /// <param name="options">文本选项。</param>
    /// <returns>加载的字体。</returns>
    private static Font LoadFont(ImageTextOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.FontPath))
            throw new ArgumentException("必须提供包含所需字形的外部字体文件。", nameof(options.FontPath));
        if (!File.Exists(options.FontPath))
            throw new FileNotFoundException("字体文件不存在。", options.FontPath);
        if (options.FontSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.FontSize));
        FontGlyphValidator.Validate(options.FontPath, options.Text ?? string.Empty);

        var collection = new FontCollection();
        var header = File.ReadAllBytes(options.FontPath);
        var isCollection = header.Length >= 4 && header[0] == (byte)'t' && header[1] == (byte)'t' && header[2] == (byte)'c' && header[3] == (byte)'f';
        var family = isCollection
            ? collection.AddCollection(options.FontPath).FirstOrDefault()
            : collection.Add(options.FontPath);
        if (family == null)
            throw new InvalidDataException("字体文件未包含可用字体。");
        return family.CreateFont(options.FontSize, FontStyle.Regular);
    }

    /// <summary>
    /// 绘制矩形标注。
    /// </summary>
    /// <param name="ctx">ImageSharp 绘图上下文。</param>
    /// <param name="annotation">矩形标注。</param>
    /// <param name="color">边框颜色。</param>
    /// <param name="width">边框宽度。</param>
    /// <param name="opacity">透明度。</param>
    private static void DrawRectangle(IImageProcessingContext ctx, ImageAnnotation annotation, Color color, float width, float opacity)
    {
        var rectangle = new RectangleF(annotation.X, annotation.Y, annotation.Width, annotation.Height);
        if (annotation.FillColor.HasValue)
            ctx.Fill(ToColor(annotation.FillColor.Value).WithAlpha((byte)Math.Round(annotation.FillColor.Value.A * opacity)), rectangle);
        ctx.Draw(color, width, rectangle);
    }

    /// <summary>
    /// 绘制箭头标注。
    /// </summary>
    /// <param name="ctx">ImageSharp 绘图上下文。</param>
    /// <param name="annotation">箭头标注。</param>
    /// <param name="color">线条颜色。</param>
    /// <param name="width">线条宽度。</param>
    private static void DrawArrow(IImageProcessingContext ctx, ImageAnnotation annotation, Color color, float width)
    {
        var start = new PointF(annotation.X, annotation.Y);
        var end = new PointF(annotation.X2, annotation.Y2);
        ctx.DrawLine(color, width, start, end);
        var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        var size = Math.Max(6, width * 4);
        var left = new PointF(end.X - (float)(Math.Cos(angle - Math.PI / 6) * size),
            end.Y - (float)(Math.Sin(angle - Math.PI / 6) * size));
        var right = new PointF(end.X - (float)(Math.Cos(angle + Math.PI / 6) * size),
            end.Y - (float)(Math.Sin(angle + Math.PI / 6) * size));
        ctx.DrawLine(color, width, end, left);
        ctx.DrawLine(color, width, end, right);
    }

    /// <summary>
    /// 将统一 RGB 颜色转换为 ImageSharp 颜色。
    /// </summary>
    /// <param name="color">统一 RGB 颜色。</param>
    /// <returns>ImageSharp 颜色。</returns>
    private static Color ToColor(RgbColor color) => Color.FromRgba(color.R, color.G, color.B, color.A);

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
        if (opacity < 0 || opacity > 1)
            throw new ArgumentOutOfRangeException(nameof(opacity));
    }

    /// <summary>
    /// 封装 ImageSharp 图像并提供统一表面生命周期。
    /// </summary>
    private sealed class ImageSharpSurface : ImageSurface
    {
        /// <summary>
        /// 初始化 <see cref="ImageSharpSurface" /> 类的新实例。
        /// </summary>
        /// <param name="image">底层 ImageSharp 图像。</param>
        internal ImageSharpSurface(Image<Rgba32> image) => Image = image;

        /// <summary>
        /// 获取底层 ImageSharp 图像。
        /// </summary>
        internal Image<Rgba32> Image { get; }

        /// <inheritdoc />
        internal override int Width => Image.Width;

        /// <inheritdoc />
        internal override int Height => Image.Height;

        /// <inheritdoc />
        public override void Dispose() => Image.Dispose();
    }
}
