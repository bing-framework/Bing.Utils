using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace Bing.Drawing.Internal;

/// <summary>
/// Windows GDI+ 对统一图像后端契约的实现。
/// </summary>
/// <remarks>
/// 输入表面不转移所有权，输出表面均为独立位图。
/// </remarks>
internal sealed class GdiImageBackend : IImageBackend
{
    /// <summary>
    /// 封装 GDI+ 位图并提供统一表面生命周期。
    /// </summary>
    private sealed class Surface : ImageSurface
    {
        /// <summary>
        /// 获取底层 GDI+ 位图。
        /// </summary>
        internal readonly Bitmap Bitmap;

        /// <summary>
        /// 初始化 <see cref="Surface" /> 类的新实例。
        /// </summary>
        /// <param name="bitmap">底层 GDI+ 位图。</param>
        internal Surface(Bitmap bitmap) => Bitmap = bitmap;

        /// <inheritdoc />
        internal override int Width => Bitmap.Width;

        /// <inheritdoc />
        internal override int Height => Bitmap.Height;

        /// <inheritdoc />
        public override void Dispose() => Bitmap.Dispose();
    }

    /// <summary>
    /// 获取 GDI+ 位图实例。
    /// </summary>
    /// <param name="image">统一图像表面。</param>
    /// <returns>底层 GDI+ 位图。</returns>
    private static Bitmap Get(ImageSurface image) => ((Surface)image).Bitmap;

    /// <summary>
    /// 将统一 RGB 颜色转换为 GDI+ 颜色。
    /// </summary>
    /// <param name="c">统一 RGB 颜色。</param>
    /// <param name="opacity">透明度乘数。</param>
    /// <returns>GDI+ 颜色。</returns>
    private static Color ColorOf(RgbColor c, float opacity = 1) => Color.FromArgb((int)Math.Round(c.A * opacity), c.R, c.G, c.B);

    /// <summary>
    /// 创建配置统一绘制参数的 GDI+ 画布。
    /// </summary>
    /// <param name="bitmap">目标位图。</param>
    /// <returns>配置好的 GDI+ 画布。</returns>
    private static Graphics GraphicsOf(Bitmap bitmap)
    {
        var g = Graphics.FromImage(bitmap);
        // 与其他后端保持相同的通道 alpha 合成，避免 GDI+ 隐式 gamma 校正改变水印透明度。
        g.CompositingQuality = CompositingQuality.AssumeLinear;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        return g;
    }
    /// <inheritdoc />
    public ImageSurface Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var input = Image.FromStream(stream, false, true);
        var result = new Bitmap(input.Width, input.Height, PixelFormat.Format32bppArgb);
        try { using var g = GraphicsOf(result); g.DrawImage(input, new Rectangle(0, 0, input.Width, input.Height)); return new Surface(result); }
        catch { result.Dispose(); throw; }
    }
    /// <inheritdoc />
    public ImageSurface Create(int width, int height, RgbColor background)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        try { using var g = GraphicsOf(bitmap); g.Clear(ColorOf(background)); return new Surface(bitmap); }
        catch { bitmap.Dispose(); throw; }
    }
    /// <inheritdoc />
    public ImageSurface Resize(ImageSurface image, int width, int height)
    {
        var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        try
        {
            using var g = GraphicsOf(result);
            using var attributes = new ImageAttributes();
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            g.DrawImage(Get(image), new Rectangle(0, 0, width, height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            return new Surface(result);
        }
        catch { result.Dispose(); throw; }
    }
    /// <inheritdoc />
    public ImageSurface Crop(ImageSurface image, int x, int y, int width, int height) =>
        new Surface(Get(image).Clone(new Rectangle(x, y, width, height), PixelFormat.Format32bppArgb));
    /// <inheritdoc />
    public ImageSurface Rotate(ImageSurface image, int angle)
    {
        angle = (angle % 360 + 360) % 360;
        if (angle % 90 == 0)
        {
            var clone = (Bitmap)Get(image).Clone();
            try
            {
                clone.RotateFlip(angle == 90 ? RotateFlipType.Rotate90FlipNone : angle == 180 ? RotateFlipType.Rotate180FlipNone : angle == 270 ? RotateFlipType.Rotate270FlipNone : RotateFlipType.RotateNoneFlipNone);
                return new Surface(clone);
            }
            catch { clone.Dispose(); throw; }
        }
        var radians = angle * Math.PI / 180;
        var width = (int)Math.Ceiling(image.Width * Math.Abs(Math.Cos(radians)) + image.Height * Math.Abs(Math.Sin(radians)) - 1e-9);
        var height = (int)Math.Ceiling(image.Height * Math.Abs(Math.Cos(radians)) + image.Width * Math.Abs(Math.Sin(radians)) - 1e-9);
        var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        try
        {
            using var g = GraphicsOf(result);
            g.TranslateTransform(width / 2f, height / 2f); g.RotateTransform(angle); g.TranslateTransform(-image.Width / 2f, -image.Height / 2f);
            g.DrawImage(Get(image), 0, 0, image.Width, image.Height);
            return new Surface(result);
        }
        catch { result.Dispose(); throw; }
    }
    /// <inheritdoc />
    public ImageSurface Flip(ImageSurface image, bool horizontal, bool vertical)
    {
        var result = (Bitmap)Get(image).Clone();
        try
        {
            result.RotateFlip(horizontal && vertical ? RotateFlipType.RotateNoneFlipXY : horizontal ? RotateFlipType.RotateNoneFlipX : vertical ? RotateFlipType.RotateNoneFlipY : RotateFlipType.RotateNoneFlipNone);
            return new Surface(result);
        }
        catch { result.Dispose(); throw; }
    }
    /// <inheritdoc />
    public void DrawImage(ImageSurface target, ImageSurface image, int x, int y, float opacity)
    {
        using var g = GraphicsOf(Get(target));
        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(new ColorMatrix { Matrix33 = opacity });
        g.DrawImage(Get(image), new Rectangle(x, y, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
    }
    /// <inheritdoc />
    public void Frame(ImageSurface image, float radius, int borderWidth, RgbColor borderColor)
    {
        if (radius == 0 && borderWidth == 0) return;
        radius = Math.Min(radius, Math.Min(image.Width, image.Height) / 2f);
        var bitmap = Get(image);
        var data = bitmap.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            var row = new byte[image.Width * 4];
            for (var y = 0; y < image.Height; y++)
            {
                var ptr = IntPtr.Add(data.Scan0, y * data.Stride); Marshal.Copy(ptr, row, 0, row.Length);
                for (var x = 0; x < image.Width; x++)
                {
                    var dx = Math.Max(radius - (x + .5f), Math.Max(x + .5f - (image.Width - radius), 0));
                    var dy = Math.Max(radius - (y + .5f), Math.Max(y + .5f - (image.Height - radius), 0));
                    if (dx * dx + dy * dy > radius * radius) row[x * 4 + 3] = 0;
                    else if (borderWidth > 0 && (x < borderWidth || y < borderWidth || x >= image.Width - borderWidth || y >= image.Height - borderWidth ||
                        (radius > 0 && Math.Sqrt(dx * dx + dy * dy) > Math.Max(0, radius - borderWidth))))
                    {
                        row[x * 4] = borderColor.B; row[x * 4 + 1] = borderColor.G; row[x * 4 + 2] = borderColor.R; row[x * 4 + 3] = borderColor.A;
                    }
                }
                Marshal.Copy(row, 0, ptr, row.Length);
            }
        }
        finally { bitmap.UnlockBits(data); }
    }
    /// <inheritdoc />
    public (int Width, int Height) MeasureText(ImageTextOptions text)
    {
        ImagePipeline.ValidateText(text);
        using var collection = new PrivateFontCollection(); collection.AddFontFile(text.FontPath);
        using var font = new Font(collection.Families[0], text.FontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        using var bitmap = new Bitmap(1, 1); using var g = GraphicsOf(bitmap);
        var size = g.MeasureString(text.Text, font, int.MaxValue, StringFormat.GenericTypographic);
        return ((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));
    }
    /// <inheritdoc />
    public void Annotate(ImageSurface image, ImageAnnotation a, float opacity)
    {
        using var g = GraphicsOf(Get(image));
        if (a.Kind == ImageAnnotationKind.Text)
        {
            var text = a.Text!; ImagePipeline.ValidateText(text);
            using var collection = new PrivateFontCollection(); collection.AddFontFile(text.FontPath);
            using var font = new Font(collection.Families[0], text.FontSize, FontStyle.Regular, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(ColorOf(text.Color, opacity));
            g.DrawString(text.Text, font, brush, a.X, a.Y, StringFormat.GenericTypographic); return;
        }
        using var pen = new Pen(ColorOf(a.Color, opacity), a.StrokeWidth);
        using var fill = new SolidBrush(ColorOf(a.FillColor ?? new RgbColor(0, 0, 0, 0), opacity));
        switch (a.Kind)
        {
            case ImageAnnotationKind.Rectangle:
                if (a.FillColor.HasValue) g.FillRectangle(fill, a.X, a.Y, a.Width, a.Height);
                g.DrawRectangle(pen, a.X, a.Y, a.Width, a.Height); break;
            case ImageAnnotationKind.Ellipse:
                if (a.FillColor.HasValue) g.FillEllipse(fill, a.X, a.Y, a.Width, a.Height);
                g.DrawEllipse(pen, a.X, a.Y, a.Width, a.Height); break;
            case ImageAnnotationKind.Line: g.DrawLine(pen, a.X, a.Y, a.X2, a.Y2); break;
            case ImageAnnotationKind.Arrow:
                using (var cap = new AdjustableArrowCap(4, 5, true)) { pen.CustomEndCap = cap; g.DrawLine(pen, a.X, a.Y, a.X2, a.Y2); } break;
            default: throw new ArgumentOutOfRangeException(nameof(a.Kind));
        }
    }
    /// <inheritdoc />
    public byte[] Encode(ImageSurface image, ImageOutputFormat format, int quality, RgbColor background)
    {
        using var output = new MemoryStream();
        if (format == ImageOutputFormat.Png) Get(image).Save(output, ImageFormat.Png);
        else if (format == ImageOutputFormat.Jpeg)
        {
            using var opaque = Create(image.Width, image.Height, new RgbColor(background.R, background.G, background.B));
            DrawImage(opaque, image, 0, 0, 1);
            var encoder = ImageCodecInfo.GetImageEncoders().Single(x => x.FormatID == ImageFormat.Jpeg.Guid);
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
            Get(opaque).Save(output, encoder, parameters);
        }
        else throw new NotSupportedException("GDI+ 不支持请求的输出格式。");
        return output.ToArray();
    }
}
