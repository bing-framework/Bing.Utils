using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 基于 SkiaSharp 实现的图片操作辅助类
/// </summary>
public static partial class SkiaSharpHelper
{
    /// <summary>
    /// 以弱引用方式记录图像对象的原始编码格式。
    /// </summary>
    private static readonly ConditionalWeakTable<SKImage, EncodedImageFormatHolder> ImageFormats = new();

    /// <summary>
    /// 保存与图像对象关联的原始编码格式。
    /// </summary>
    private sealed class EncodedImageFormatHolder
    {
        /// <summary>
        /// 初始化一个 <see cref="EncodedImageFormatHolder"/> 类型的实例。
        /// </summary>
        /// <param name="format">与图像关联的编码格式。</param>
        public EncodedImageFormatHolder(SKEncodedImageFormat format)
        {
            Format = format;
        }

        /// <summary>
        /// 获取图像的原始编码格式。
        /// </summary>
        public SKEncodedImageFormat Format { get; }
    }

    #region GetImageExtension(获取图片扩展名)

    /// <summary>
    /// 获取图片扩展名
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetImageExtension(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return GetEncodedImageFormat(image) switch
        {
            SKEncodedImageFormat.Jpeg => "jpg",
            SKEncodedImageFormat.Png => "png",
            SKEncodedImageFormat.Gif => "gif",
            SKEncodedImageFormat.Bmp => "bmp",
            SKEncodedImageFormat.Webp => "webp",
            SKEncodedImageFormat.Ico => "ico",
            SKEncodedImageFormat.Avif => "avif",
            SKEncodedImageFormat.Wbmp => "wbmp",
            _ => string.Empty
        };
    }

    #endregion

    #region GetMimeType(获取 Mime 类型)

    /// <summary>
    /// 获取 Mime 类型
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetMimeType(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return GetEncodedImageFormat(image).GetMimeType();
    }

    #endregion

    #region HasAlpha(是否包含透明像素)

    /// <summary>
    /// 是否包含透明像素
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool HasAlpha(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        using var bitmap = SKBitmap.FromImage(image);
        for (var x = 0; x < bitmap.Width; x++)
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                if (bitmap.GetPixel(x, y).Alpha < byte.MaxValue)
                    return true;
            }
        }

        return false;
    }

    #endregion

    #region SetOpacity(设置透明度)

    /// <summary>
    /// 设置透明度
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="opacity">透明度</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SKImage SetOpacity(SKImage image, float opacity)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (opacity < 0 || opacity > 1)
            throw new ArgumentOutOfRangeException(nameof(opacity), "不透明度必须为0-1之间的浮点数");

        using var bitmap = SKBitmap.FromImage(image);
        using var output = new SKBitmap(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (var x = 0; x < image.Width; x++)
        {
            for (var y = 0; y < image.Height; y++)
            {
                var color = bitmap.GetPixel(x, y);
                output.SetPixel(x, y, color.WithAlpha(ClampToByte(color.Alpha * opacity)));
            }
        }
        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    #endregion

    #region Resize(缩放图片)

    /// <summary>
    /// 缩放图片
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="width">目标宽度</param>
    /// <param name="height">目标高度</param>
    /// <param name="keepAspectRatio">是否保持宽高比</param>
    /// <param name="allowEnlarge">是否允许放大</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SKImage Resize(SKImage image, int width, int height, bool keepAspectRatio = true, bool allowEnlarge = true)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        var size = CalculateResizeSize(image.Width, image.Height, width, height, keepAspectRatio, allowEnlarge);
        if (size.Width == image.Width && size.Height == image.Height)
            return CloneImage(image);

        using var bitmap = SKBitmap.FromImage(image);
        using var output = new SKBitmap(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(output);
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(bitmap, new SKRect(0, 0, size.Width, size.Height), paint);
        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    #endregion

    #region Crop(裁剪图片)

    /// <summary>
    /// 裁剪图片
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="rectangle">裁剪区域</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SKImage Crop(SKImage image, SKRectI rectangle)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var cropRectangle = NormalizeCropRectangle(rectangle, image.Width, image.Height);

        using var bitmap = SKBitmap.FromImage(image);
        using var output = new SKBitmap(cropRectangle.Width, cropRectangle.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(output);
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(bitmap, cropRectangle, new SKRect(0, 0, cropRectangle.Width, cropRectangle.Height), paint);
        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    #endregion

    #region Rotate(旋转图片)

    /// <summary>
    /// 旋转图片
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="angle">旋转角度</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage Rotate(SKImage image, int angle)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var normalizedAngle = NormalizeAngle(angle);
        if (normalizedAngle == 0)
            return CloneImage(image);

        using var bitmap = SKBitmap.FromImage(image);
        var radians = normalizedAngle * Math.PI / 180d;
        var cos = Math.Abs(Math.Cos(radians));
        var sin = Math.Abs(Math.Sin(radians));
        var width = Math.Max(1, (int)Math.Round(bitmap.Width * cos + bitmap.Height * sin, MidpointRounding.AwayFromZero));
        var height = Math.Max(1, (int)Math.Round(bitmap.Width * sin + bitmap.Height * cos, MidpointRounding.AwayFromZero));

        using var output = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(output);
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };

        canvas.Clear(SKColors.Transparent);
        canvas.Translate(width / 2f, height / 2f);
        canvas.RotateDegrees(normalizedAngle);
        canvas.Translate(-bitmap.Width / 2f, -bitmap.Height / 2f);
        canvas.DrawBitmap(bitmap, 0, 0, paint);

        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    #endregion

    #region FlipHorizontal(左右翻转)

    /// <summary>
    /// 左右翻转
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage FlipHorizontal(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        using var bitmap = SKBitmap.FromImage(image);
        using var output = new SKBitmap(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(output);
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
        canvas.Clear(SKColors.Transparent);
        canvas.Translate(bitmap.Width, 0);
        canvas.Scale(-1, 1);
        canvas.DrawBitmap(bitmap, 0, 0, paint);
        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    #endregion

    #region FlipVertical(上下翻转)

    /// <summary>
    /// 上下翻转
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage FlipVertical(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        using var bitmap = SKBitmap.FromImage(image);
        using var output = new SKBitmap(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(output);
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
        canvas.Clear(SKColors.Transparent);
        canvas.Translate(0, bitmap.Height);
        canvas.Scale(1, -1);
        canvas.DrawBitmap(bitmap, 0, 0, paint);
        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    #endregion

    #region ReplaceColor(替换颜色)

    /// <summary>
    /// 替换颜色
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="colorA">原颜色</param>
    /// <param name="colorB">目标颜色</param>
    /// <param name="accuracy">允许误差</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SKImage ReplaceColor(SKImage image, SKColor colorA, SKColor colorB, int accuracy = 36)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (accuracy < 0)
            throw new ArgumentOutOfRangeException(nameof(accuracy));

        return TransformImage(image, color => IsSimilarColors(color, colorA, accuracy) ? colorB : color);
    }

    #endregion

    #region SetBlackWhiteEffect(设置黑白效果)

    /// <summary>
    /// 设置黑白效果
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="threshold">阈值。范围：0-1</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SKImage SetBlackWhiteEffect(SKImage image, float threshold)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        ValidateThreshold(threshold);

        return TransformImage(image, color =>
        {
            var rgb = GetGrayScale(color) >= threshold ? byte.MaxValue : (byte)0;
            return new SKColor(rgb, rgb, rgb, color.Alpha);
        });
    }

    #endregion

    #region SetDuotoneEffect(设置双色调效果)

    /// <summary>
    /// 设置双色调效果
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="colorA">颜色A</param>
    /// <param name="colorB">颜色B</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage SetDuotoneEffect(SKImage image, SKColor colorA, SKColor colorB)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return TransformImage(image, color => GetDuotoneColor(color, colorA, colorB));
    }

    #endregion

    #region SetBrightness(设置亮度)

    /// <summary>
    /// 设置亮度
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="amount">亮度比例，必须大于或等于0</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SKImage SetBrightness(SKImage image, float amount)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        ValidateAmount(amount, nameof(amount));
        return ApplyColorMatrix(image, CreateBrightnessMatrix(amount));
    }

    #endregion

    #region SetContrast(设置对比度)

    /// <summary>
    /// 设置对比度
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="amount">对比度比例，必须大于或等于0</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SKImage SetContrast(SKImage image, float amount)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        ValidateAmount(amount, nameof(amount));
        return ApplyColorMatrix(image, CreateContrastMatrix(amount));
    }

    #endregion

    #region SetSaturation(设置饱和度)

    /// <summary>
    /// 设置饱和度
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="amount">饱和度比例，必须大于或等于0</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SKImage SetSaturation(SKImage image, float amount)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        ValidateAmount(amount, nameof(amount));
        return ApplyColorMatrix(image, CreateSaturationMatrix(amount));
    }

    #endregion

    #region ApplyColorMatrix(应用颜色矩阵)

    /// <summary>
    /// 应用颜色矩阵
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="matrix">5x5 颜色矩阵</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static SKImage ApplyColorMatrix(SKImage image, float[,] matrix)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        ValidateColorMatrix(matrix);
        return TransformImage(image, color => ApplyColorMatrix(color, matrix));
    }

    #endregion

    #region SoftEdge(柔化边缘)

    /// <summary>
    /// 柔化边缘
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="radius">柔化半径。单位：像素</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SKImage SoftEdge(SKImage image, float radius)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius), "柔化半径必须大于或等于0");

        using var bitmap = SKBitmap.FromImage(image);
        using var output = ApplySoftEdgeEffect(bitmap, radius);
        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    #endregion

    #region Soften(柔化效果)

    /// <summary>
    /// 柔化效果
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage Soften(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return ApplyConvolutionEffect(image, new float[] { 1, 2, 1, 2, 4, 2, 1, 2, 1 }, 16f);
    }

    #endregion

    #region Sharpen(锐化效果)

    /// <summary>
    /// 锐化效果
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage Sharpen(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return ApplyConvolutionEffect(image, new float[] { 0, -1, 0, -1, 5, -1, 0, -1, 0 }, 1f);
    }

    #endregion

    #region Emboss(浮雕效果)

    /// <summary>
    /// 浮雕效果
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage Emboss(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return ApplyConvolutionEffect(image, new float[] { -1, -1, 0, -1, 0, 1, 0, 1, 1 }, 1f, 128f);
    }

    #endregion

    #region Atomizing(雾化效果)

    /// <summary>
    /// 雾化效果
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage Atomizing(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return ApplyAtomizingEffect(image);
    }

    #endregion

    #region AddImageWatermark(添加图片水印)

    /// <summary>
    /// 添加图片水印
    /// </summary>
    /// <param name="image">原图</param>
    /// <param name="watermark">水印图</param>
    /// <param name="targetRectangle">目标区域</param>
    /// <param name="opacity">透明度</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SKImage AddImageWatermark(SKImage image, SKImage watermark, SKRectI targetRectangle, float opacity = 1)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (watermark is null)
            throw new ArgumentNullException(nameof(watermark));
        if (opacity < 0 || opacity > 1)
            throw new ArgumentOutOfRangeException(nameof(opacity), "不透明度必须为0-1之间的浮点数");
        if (targetRectangle.Width <= 0 || targetRectangle.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetRectangle), "目标区域宽度和高度必须大于0");

        var normalized = NormalizeCropRectangle(targetRectangle, image.Width, image.Height);
        using var source = SKBitmap.FromImage(image);
        using var output = source.Copy();
        using var overlay = opacity == 1 ? CloneImage(watermark) : SetOpacity(watermark, opacity);
        using var canvas = new SKCanvas(output);
        using var paint = new SKPaint
        {
            BlendMode = SKBlendMode.SrcOver,
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        canvas.DrawImage(overlay, new SKRect(normalized.Left, normalized.Top, normalized.Right, normalized.Bottom), paint);
        canvas.Flush();
        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    #endregion

    #region AddTextWatermark(添加文字水印)

    /// <summary>
    /// 添加文字水印
    /// </summary>
    /// <param name="image">原图</param>
    /// <param name="text">水印文本</param>
    /// <param name="fontFamily">字体名称</param>
    /// <param name="fontSize">字体大小</param>
    /// <param name="opacity">透明度</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SKImage AddTextWatermark(SKImage image, string text, string? fontFamily = default, float fontSize = 16, float opacity = 1)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentNullException(nameof(text));
        if (fontSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(fontSize), "字体大小必须大于0");
        if (opacity < 0 || opacity > 1)
            throw new ArgumentOutOfRangeException(nameof(opacity), "不透明度必须为0-1之间的浮点数");

        using var source = SKBitmap.FromImage(image);
        using var output = source.Copy();
        using var canvas = new SKCanvas(output);
        using var typeface = string.IsNullOrWhiteSpace(fontFamily) ? SKTypeface.Default : SKTypeface.FromFamilyName(fontFamily) ?? SKTypeface.Default;
        using var paint = new SKPaint
        {
            Color = new SKColor(24, 24, 24, ClampToByte(255f * opacity)),
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = typeface,
            FakeBoldText = true,
            FilterQuality = SKFilterQuality.High
        };

        var bounds = new SKRect();
        paint.MeasureText(text, ref bounds);
        var padding = Math.Max(4f, fontSize * 0.5f);
        var x = Math.Max(padding, image.Width - bounds.Width - padding - bounds.Left);
        var y = Math.Max(padding - bounds.Top, image.Height - padding - bounds.Bottom);

        canvas.DrawText(text, x, y, paint);
        canvas.Flush();
        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    #endregion

    #region CreateCaptchaImage(创建验证码图片)

    /// <summary>
    /// 获取验证码文本
    /// </summary>
    /// <param name="length">验证码长度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string GetCaptchaCode(int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        return CaptchaCodeGenerator.Generate(length);
    }

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

    /// <summary>
    /// 创建指定长度的验证码图片
    /// </summary>
    /// <param name="length">验证码长度</param>
    /// <param name="code">生成的验证码文本</param>
    /// <param name="captchaType">验证码类型</param>
    /// <param name="options">验证码配置</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SKImage CreateCaptchaImage(int length, out string code, CaptchaType captchaType = CaptchaType.NumberAndLetter, CaptchaOptions? options = null)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        code = GetCaptchaCode(length, captchaType);
        return CreateCaptchaImage(code, options);
    }

    /// <summary>
    /// 创建验证码图片
    /// </summary>
    /// <param name="code">验证码文本</param>
    /// <param name="options">验证码配置</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage CreateCaptchaImage(string code, CaptchaOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentNullException(nameof(code));

        options = options ?? new CaptchaOptions();
        var fontSize = options.FontSize;
        var fontWidth = options.FontWidth;
        var width = options.Width > 0 ? options.Width : fontWidth * code.Length + fontWidth;
        var height = options.Height > 0 ? options.Height : fontSize + fontSize / 2;
        var background = new SKColor(options.BackgroundR, options.BackgroundG, options.BackgroundB, options.BackgroundA);

        var rng = options.RandomSeed.HasValue ? new Random(options.RandomSeed.Value) : new Random();

        using var output = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(output);
        canvas.Clear(background);

        if (options.HasBorder)
            DrawCaptchaBorder(canvas, width, height);

        DrawCaptchaDisorderLine(canvas, code, width, height, options, rng);
        DrawCaptchaDisorderPoint(output, code, width, height, options, rng);
        DrawCaptchaText(canvas, code, width, height, fontSize, fontWidth, options, rng);
        canvas.Flush();

        return TrackFormat(SKImage.FromBitmap(output), SKEncodedImageFormat.Png)!;
    }

    #endregion

    /// <summary>
    /// 克隆图像并保留其记录的编码格式。
    /// </summary>
    /// <param name="image">要克隆的图像。</param>
    /// <returns>图像副本。</returns>
    private static SKImage CloneImage(SKImage image)
    {
        using var bitmap = SKBitmap.FromImage(image);
        return CopyTrackedFormat(image, SKImage.FromBitmap(bitmap));
    }
    /// <summary>
    /// 获取图像的编码格式。
    /// </summary>
    /// <param name="image">要检查的图像。</param>
    /// <returns>记录或从图像数据识别出的格式；无法识别时使用 PNG。</returns>
    private static SKEncodedImageFormat GetEncodedImageFormat(SKImage image)
    {
        var trackedFormat = GetTrackedFormat(image);
        if (trackedFormat.HasValue)
            return trackedFormat.Value;

        using var data = image.EncodedData ?? image.Encode(SKEncodedImageFormat.Png, 100);
        using var codec = SKCodec.Create(data);
        return codec?.EncodedFormat ?? SKEncodedImageFormat.Png;
    }

    /// <summary>
    /// 计算满足目标边界的图像尺寸。
    /// </summary>
    /// <param name="sourceWidth">源图像宽度。</param>
    /// <param name="sourceHeight">源图像高度。</param>
    /// <param name="targetWidth">目标边界宽度。</param>
    /// <param name="targetHeight">目标边界高度。</param>
    /// <param name="keepAspectRatio">是否保持宽高比。</param>
    /// <param name="allowEnlarge">是否允许放大源图像。</param>
    /// <returns>计算得到的图像尺寸。</returns>
    private static SKSizeI CalculateResizeSize(int sourceWidth, int sourceHeight, int targetWidth, int targetHeight,
        bool keepAspectRatio, bool allowEnlarge)
    {
        var (w, h) = Internal.ImageGeometryHelper.CalculateResizeSize(
            sourceWidth, sourceHeight, targetWidth, targetHeight, keepAspectRatio, allowEnlarge);
        return new SKSizeI(w, h);
    }

    /// <summary>
    /// 将裁剪矩形限制在图像边界内。
    /// </summary>
    /// <param name="rectangle">待规范化的裁剪矩形。</param>
    /// <param name="imageWidth">图像宽度。</param>
    /// <param name="imageHeight">图像高度。</param>
    /// <returns>与图像边界相交后的裁剪矩形。</returns>
    private static SKRectI NormalizeCropRectangle(SKRectI rectangle, int imageWidth, int imageHeight)
    {
        var (left, top, width, height) = Internal.ImageGeometryHelper.NormalizeCropRectangle(
            rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height, imageWidth, imageHeight);
        return new SKRectI(left, top, left + width, top + height);
    }

    /// <summary>
    /// 将角度规范化到 [0, 360) 范围。
    /// </summary>
    /// <param name="angle">以度为单位的角度。</param>
    /// <returns>规范化后的角度。</returns>
    private static float NormalizeAngle(int angle)
    {
        return Internal.ImageGeometryHelper.NormalizeAngle(angle);
    }

    /// <summary>
    /// 将颜色转换函数应用到图像的每个像素。
    /// </summary>
    /// <param name="image">要转换的图像。</param>
    /// <param name="transform">用于转换像素颜色的函数。</param>
    /// <returns>转换后的图像，并保留源图像的编码格式。</returns>
    private static SKImage TransformImage(SKImage image, Func<SKColor, SKColor> transform)
    {
        using var bitmap = SKBitmap.FromImage(image);
        using var output = new SKBitmap(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (var x = 0; x < bitmap.Width; x++)
        {
            for (var y = 0; y < bitmap.Height; y++)
                output.SetPixel(x, y, transform(bitmap.GetPixel(x, y)));
        }

        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    /// <summary>
    /// 验证阈值是否处于 [0, 1] 范围。
    /// </summary>
    /// <param name="threshold">待验证的阈值。</param>
    private static void ValidateThreshold(float threshold)
    {
        Internal.ImageGeometryHelper.ValidateThreshold(threshold);
    }

    /// <summary>
    /// 验证数值参数是否为非负数。
    /// </summary>
    /// <param name="amount">待验证的数值。</param>
    /// <param name="paramName">用于构造范围异常的参数名。</param>
    private static void ValidateAmount(float amount, string paramName)
    {
        Internal.ImageGeometryHelper.ValidateAmount(amount, paramName);
    }

    /// <summary>
    /// 验证颜色矩阵不为 null 且尺寸为 5x5。
    /// </summary>
    /// <param name="matrix">待验证的颜色矩阵。</param>
    private static void ValidateColorMatrix(float[,] matrix)
    {
        Internal.ImageGeometryHelper.ValidateColorMatrix(matrix);
    }

    /// <summary>
    /// 对颜色应用 5x5 颜色变换矩阵。
    /// </summary>
    /// <param name="color">待变换的颜色。</param>
    /// <param name="matrix">5x5 颜色变换矩阵。</param>
    /// <returns>经矩阵变换后的颜色。</returns>
    private static SKColor ApplyColorMatrix(SKColor color, float[,] matrix)
    {
        var (r, g, b, a) = Internal.ImageGeometryHelper.ApplyColorMatrix(color.Red, color.Green, color.Blue, color.Alpha, matrix);
        return new SKColor(r, g, b, a);
    }

    /// <summary>
    /// 创建亮度调整矩阵。
    /// </summary>
    /// <param name="amount">亮度调整幅度。</param>
    /// <returns>5x5 亮度变换矩阵。</returns>
    private static float[,] CreateBrightnessMatrix(float amount)
    {
        return ColorMatrices.CreateBrightnessFilter(amount);
    }

    /// <summary>
    /// 创建对比度调整矩阵。
    /// </summary>
    /// <param name="amount">对比度调整幅度。</param>
    /// <returns>5x5 对比度变换矩阵。</returns>
    private static float[,] CreateContrastMatrix(float amount)
    {
        return ColorMatrices.CreateContrastFilter(amount);
    }

    /// <summary>
    /// 创建饱和度调整矩阵。
    /// </summary>
    /// <param name="amount">饱和度调整幅度。</param>
    /// <returns>5x5 饱和度变换矩阵。</returns>
    private static float[,] CreateSaturationMatrix(float amount)
    {
        return ColorMatrices.CreateSaturationFilter(amount);
    }

    /// <summary>
    /// 根据边缘半径生成已调整透明度的位图。
    /// </summary>
    /// <param name="bitmap">要处理的源位图。</param>
    /// <param name="radius">柔化边缘的半径。</param>
    /// <returns>应用柔化效果的新位图。</returns>
    private static SKBitmap ApplySoftEdgeEffect(SKBitmap bitmap, float radius)
    {
        var output = bitmap.Copy();
        var mask = CreateSoftEdgeAlphaMask(output);
        var offset = Math.Max(0, (int)Math.Round(radius / 4.0, MidpointRounding.AwayFromZero));
        if (offset > 0)
        {
            mask = ErodeAlphaMask(mask, offset, offset, 3);
            mask = BlurAlphaMask(mask, offset, offset, 3);
        }

        ApplySoftEdgeAlphaMask(output, mask);
        return output;
    }

    /// <summary>
    /// 根据位图透明区域创建 Alpha 蒙层。
    /// </summary>
    /// <param name="bitmap">用于生成蒙层的位图。</param>
    /// <returns>以像素 Alpha 值表示的二维蒙层。</returns>
    private static byte[,] CreateSoftEdgeAlphaMask(SKBitmap bitmap)
    {
        var mask = new byte[bitmap.Width, bitmap.Height];
        for (var x = 0; x < bitmap.Width; x++)
        {
            for (var y = 0; y < bitmap.Height; y++)
                mask[x, y] = bitmap.GetPixel(x, y).Alpha == 0 ? (byte)0 : byte.MaxValue;
        }

        return mask;
    }

    /// <summary>
    /// 将 Alpha 蒙层应用到位图像素。
    /// </summary>
    /// <param name="bitmap">要修改透明度的位图。</param>
    /// <param name="mask">每个像素对应的 Alpha 蒙层。</param>
    private static void ApplySoftEdgeAlphaMask(SKBitmap bitmap, byte[,] mask)
    {
        for (var x = 0; x < bitmap.Width; x++)
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                var color = bitmap.GetPixel(x, y);
                var alpha = ClampToByte(mask[x, y] / 255f * color.Alpha);
                bitmap.SetPixel(x, y, new SKColor(color.Red, color.Green, color.Blue, alpha));
            }
        }
    }

    /// <summary>
    /// 按指定偏移和迭代次数腐蚀 Alpha 蒙层。
    /// </summary>
    /// <param name="sourceMask">待处理的 Alpha 蒙层。</param>
    /// <param name="offsetX">水平方向的采样偏移。</param>
    /// <param name="offsetY">垂直方向的采样偏移。</param>
    /// <param name="iteration">腐蚀迭代次数。</param>
    /// <returns>腐蚀后的 Alpha 蒙层。</returns>
    private static byte[,] ErodeAlphaMask(byte[,] sourceMask, int offsetX, int offsetY, int iteration)
    {
        var cols = sourceMask.GetLength(0);
        var rows = sourceMask.GetLength(1);
        var current = sourceMask;

        for (var i = 0; i < iteration; i++)
        {
            var target = new byte[cols, rows];
            for (var x = 0; x < cols; x++)
            {
                for (var y = 0; y < rows; y++)
                {
                    var keep = true;
                    for (var px = x - offsetX; px <= x + offsetX && keep; px++)
                    {
                        for (var py = y - offsetY; py <= y + offsetY; py++)
                        {
                            if (px < 0 || px >= cols || py < 0 || py >= rows || current[px, py] == 0)
                            {
                                keep = false;
                                break;
                            }
                        }
                    }

                    target[x, y] = keep ? byte.MaxValue : (byte)0;
                }
            }

            current = target;
        }

        return current;
    }

    /// <summary>
    /// 按指定偏移和迭代次数模糊 Alpha 蒙层。
    /// </summary>
    /// <param name="sourceMask">待处理的 Alpha 蒙层。</param>
    /// <param name="offsetX">水平方向的采样偏移。</param>
    /// <param name="offsetY">垂直方向的采样偏移。</param>
    /// <param name="iteration">模糊迭代次数。</param>
    /// <returns>模糊后的 Alpha 蒙层。</returns>
    private static byte[,] BlurAlphaMask(byte[,] sourceMask, int offsetX, int offsetY, int iteration)
    {
        var cols = sourceMask.GetLength(0);
        var rows = sourceMask.GetLength(1);
        var current = sourceMask;
        var count = Math.Max(1, (offsetX * 2 + 1) * (offsetY * 2 + 1));

        for (var i = 0; i < iteration; i++)
        {
            var target = new byte[cols, rows];
            for (var x = 0; x < cols; x++)
            {
                for (var y = 0; y < rows; y++)
                {
                    var total = 0;
                    for (var px = x - offsetX; px <= x + offsetX; px++)
                    {
                        for (var py = y - offsetY; py <= y + offsetY; py++)
                        {
                            if (px >= 0 && px < cols && py >= 0 && py < rows)
                                total += current[px, py];
                        }
                    }

                    target[x, y] = (byte)Math.Round(total / (double)count, MidpointRounding.AwayFromZero);
                }
            }

            current = target;
        }

        return current;
    }

    /// <summary>
    /// 生成带有雾化采样效果的图像。
    /// </summary>
    /// <param name="image">要处理的源图像。</param>
    /// <returns>应用雾化效果的新图像。</returns>
    private static SKImage ApplyAtomizingEffect(SKImage image)
    {
        using var source = SKBitmap.FromImage(image);
        using var output = new SKBitmap(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        const int blockSize = 19;

        for (var x = 0; x < source.Width; x++)
        {
            for (var y = 0; y < source.Height; y++)
            {
                var offset = ((x + 1) * 13 + (y + 1) * 7 + source.Width * 5 + source.Height * 3) % blockSize;
                var sampleX = Math.Min(source.Width - 1, x + offset);
                var sampleY = Math.Min(source.Height - 1, y + offset);
                output.SetPixel(x, y, source.GetPixel(sampleX, sampleY));
            }
        }

        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    /// <summary>
    /// 在画布边界绘制验证码边框。
    /// </summary>
    /// <param name="canvas">绘制目标画布。</param>
    /// <param name="width">验证码图像宽度。</param>
    /// <param name="height">验证码图像高度。</param>
    private static void DrawCaptchaBorder(SKCanvas canvas, int width, int height)
    {
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Silver,
            StrokeWidth = 1,
            IsAntialias = true
        };

        canvas.DrawRect(0.5f, 0.5f, width - 1, height - 1, paint);
    }

    /// <summary>
    /// 按验证码选项在画布上绘制随机干扰线。
    /// </summary>
    /// <param name="canvas">绘制目标画布。</param>
    /// <param name="code">用于确定干扰线数量的验证码文本。</param>
    /// <param name="width">验证码图像宽度。</param>
    /// <param name="height">验证码图像高度。</param>
    /// <param name="options">验证码绘制选项。</param>
    /// <param name="rng">用于生成随机位置和颜色的随机数生成器。</param>
    private static void DrawCaptchaDisorderLine(SKCanvas canvas, string code, int width, int height, CaptchaOptions options, Random rng)
    {
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            IsAntialias = true
        };

        var lineCount = options.NoiseLineCount > 0 ? options.NoiseLineCount : Math.Max(2, Math.Min(4, code.Length));
        for (var i = 0; i < lineCount; i++)
        {
            var seed = rng.Next();
            paint.Color = CreateCaptchaAccentColor(seed, options.RandomColor);

            var startX = rng.Next(0, Math.Max(1, width));
            var startY = rng.Next(0, Math.Max(1, height));
            var endX = rng.Next(0, Math.Max(1, width));
            var endY = rng.Next(0, Math.Max(1, height));
            canvas.DrawLine(startX, startY, endX, endY, paint);
        }
    }

    /// <summary>
    /// 按验证码选项在位图上绘制随机干扰点。
    /// </summary>
    /// <param name="bitmap">绘制目标位图。</param>
    /// <param name="code">用于确定干扰点数量的验证码文本。</param>
    /// <param name="width">验证码图像宽度。</param>
    /// <param name="height">验证码图像高度。</param>
    /// <param name="options">验证码绘制选项。</param>
    /// <param name="rng">用于生成随机位置和颜色的随机数生成器。</param>
    private static void DrawCaptchaDisorderPoint(SKBitmap bitmap, string code, int width, int height, CaptchaOptions options, Random rng)
    {
        var pointCount = options.NoisePointCount >= 0
            ? options.NoisePointCount
            : Math.Max(6, width * height / 45);
        var background = new SKColor(options.BackgroundR, options.BackgroundG, options.BackgroundB, options.BackgroundA);
        for (var i = 0; i < pointCount; i++)
        {
            var x = rng.Next(0, Math.Max(1, width));
            var y = rng.Next(0, Math.Max(1, height));
            var color = CreateCaptchaAccentColor(rng.Next(), options.RandomColor);
            bitmap.SetPixel(x, y, color);
        }
    }

    /// <summary>
    /// 在画布上绘制带随机位置和角度的验证码文本。
    /// </summary>
    /// <param name="canvas">绘制目标画布。</param>
    /// <param name="code">要绘制的验证码文本。</param>
    /// <param name="width">验证码图像宽度。</param>
    /// <param name="height">验证码图像高度。</param>
    /// <param name="fontSize">验证码字体大小。</param>
    /// <param name="fontWidth">验证码字符宽度。</param>
    /// <param name="options">验证码绘制选项。</param>
    /// <param name="rng">用于生成文本位置和角度的随机数生成器。</param>
    private static void DrawCaptchaText(SKCanvas canvas, string code, int width, int height, int fontSize, int fontWidth, CaptchaOptions options, Random rng)
    {
        var textColor = options.RandomColor
            ? new SKColor(24, 24, 24, 255)
            : new SKColor(
                (byte)(255 - options.BackgroundR),
                (byte)(255 - options.BackgroundG),
                (byte)(255 - options.BackgroundB),
                255);

        using var paint = new SKPaint
        {
            Color = textColor,
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = SKTypeface.Default,
            FakeBoldText = true
        };

        var metrics = paint.FontMetrics;
        var baseline = (height - metrics.Bottom - metrics.Top) / 2f;
        var cellWidth = width / (float)(code.Length + 1);
        var maxRotation = options.MaxRotationDegrees;

        for (var i = 0; i < code.Length; i++)
        {
            var offsetX = cellWidth * (i + 0.55f) + (options.RandomPosition ? rng.Next(-fontWidth / 4, fontWidth / 4) : 0);
            var offsetY = baseline + (options.RandomPosition ? rng.Next(-2, 3) : 0);
            var angle = options.RandomRotation ? rng.Next(-maxRotation, maxRotation + 1) : 0;

            canvas.Save();
            canvas.Translate(offsetX, offsetY);
            canvas.RotateDegrees(angle);
            canvas.DrawText(code[i].ToString(), 0, 0, paint);
            canvas.Restore();
        }
    }

    /// <summary>
    /// 根据种子和随机颜色选项生成验证码强调色。
    /// </summary>
    /// <param name="seed">用于生成颜色的种子值。</param>
    /// <param name="randomColor">是否生成随机颜色。</param>
    /// <returns>生成的不透明强调色。</returns>
    private static SKColor CreateCaptchaAccentColor(int seed, bool randomColor)
    {
        if (!randomColor)
            return new SKColor(0, 0, 0, 255);

        var normalized = Math.Abs(seed);
        return new SKColor(
            (byte)(20 + normalized % 120),
            (byte)(20 + normalized * 3 % 120),
            (byte)(20 + normalized * 5 % 120),
            255);
    }

    /// <summary>
    /// 使用卷积核处理图像像素。
    /// </summary>
    /// <param name="image">要处理的源图像。</param>
    /// <param name="kernel">用于卷积的权重数组。</param>
    /// <param name="divisor">卷积结果的除数。</param>
    /// <param name="bias">加到卷积结果上的偏移量。</param>
    /// <returns>卷积处理后的新图像，并保留源图像的编码格式。</returns>
    private static SKImage ApplyConvolutionEffect(SKImage image, float[] kernel, float divisor, float bias = 0)
    {
        using var source = SKBitmap.FromImage(image);
        using var output = new SKBitmap(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);

        for (var x = 0; x < source.Width; x++)
        {
            for (var y = 0; y < source.Height; y++)
            {
                float totalR = 0, totalG = 0, totalB = 0;
                var index = 0;
                for (var ky = -1; ky <= 1; ky++)
                {
                    for (var kx = -1; kx <= 1; kx++)
                    {
                        var sampleX = DrawingCompatibilityHelper.Clamp(x + kx, 0, source.Width - 1);
                        var sampleY = DrawingCompatibilityHelper.Clamp(y + ky, 0, source.Height - 1);
                        var sample = source.GetPixel(sampleX, sampleY);
                        var weight = kernel[index++];
                        totalR += sample.Red * weight;
                        totalG += sample.Green * weight;
                        totalB += sample.Blue * weight;
                    }
                }

                var sourceColor = source.GetPixel(x, y);
                output.SetPixel(x, y, new SKColor(
                    ClampToByte(totalR / divisor + bias),
                    ClampToByte(totalG / divisor + bias),
                    ClampToByte(totalB / divisor + bias),
                    sourceColor.Alpha));
            }
        }

        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    /// <summary>
    /// 将数值限制在字节可表示的范围内。
    /// </summary>
    /// <param name="value">待限制的数值。</param>
    /// <returns>限制到 0 到 255 的字节值。</returns>
    private static byte ClampToByte(float value)
    {
        return DrawingCompatibilityHelper.ClampToByte(value);
    }

    /// <summary>
    /// 将编码格式与图像对象关联。
    /// </summary>
    /// <param name="image">要记录格式的图像；为 null 时直接返回。</param>
    /// <param name="imageFormat">要记录的编码格式；为 null 时不添加记录。</param>
    /// <returns>传入的图像对象。</returns>
    private static SKImage? TrackFormat(SKImage? image, SKEncodedImageFormat? imageFormat)
    {
        if (image is null || imageFormat is null)
            return image;

        ImageFormats.Remove(image);
        ImageFormats.Add(image, new EncodedImageFormatHolder(imageFormat.Value));
        return image;
    }

    /// <summary>
    /// 将源图像的编码格式复制到目标图像。
    /// </summary>
    /// <param name="source">提供编码格式记录的源图像。</param>
    /// <param name="target">接收编码格式记录的目标图像。</param>
    /// <returns>已关联源图像编码格式的目标图像。</returns>
    private static SKImage CopyTrackedFormat(SKImage source, SKImage target)
    {
        return TrackFormat(target, GetTrackedFormat(source) ?? GetEncodedImageFormat(source))!;
    }

    /// <summary>
    /// 获取图像对象记录的编码格式。
    /// </summary>
    /// <param name="image">要查询编码格式的图像。</param>
    /// <returns>已记录的编码格式；没有记录时返回 null。</returns>
    private static SKEncodedImageFormat? GetTrackedFormat(SKImage image)
    {
        return ImageFormats.TryGetValue(image, out var holder) ? holder.Format : null;
    }
}
