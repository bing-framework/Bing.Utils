using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 基于 SkiaSharp 实现的图片操作辅助类
/// </summary>
public static partial class SkiaSharpHelper
{
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
                output.SetPixel(x, y, color.WithAlpha((byte)(0xFF * opacity)));
            }
        }
        return SKImage.FromBitmap(output);
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
        return SKImage.FromBitmap(output);
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
        return SKImage.FromBitmap(output);
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

        return SKImage.FromBitmap(output);
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
        return SKImage.FromBitmap(output);
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
        return SKImage.FromBitmap(output);
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
        var output = ApplySoftEdgeEffect(bitmap, radius);
        return SKImage.FromBitmap(output);
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
        return ApplyConvolutionEffect(image, [1, 2, 1, 2, 4, 2, 1, 2, 1], 16f);
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
        return ApplyConvolutionEffect(image, [0, -1, 0, -1, 5, -1, 0, -1, 0], 1f);
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
        return ApplyConvolutionEffect(image, [-1, -1, 0, -1, 0, 1, 0, 1, 1], 1f, 128f);
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
        return SKImage.FromBitmap(output);
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
        return SKImage.FromBitmap(output);
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
    /// 创建指定长度的验证码图片
    /// </summary>
    /// <param name="length">验证码长度</param>
    /// <param name="code">生成的验证码文本</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SKImage CreateCaptchaImage(int length, out string code)
    {
        code = GetCaptchaCode(length);
        return CreateCaptchaImage(code);
    }

    /// <summary>
    /// 创建验证码图片
    /// </summary>
    /// <param name="code">验证码文本</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage CreateCaptchaImage(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentNullException(nameof(code));

        const int fontSize = 20;
        const int fontWidth = 20;
        var width = fontWidth * code.Length + fontWidth;
        var height = fontSize + fontSize / 2;
        var background = new SKColor(240, 240, 240, 255);

        using var output = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(output);
        canvas.Clear(background);

        DrawCaptchaBorder(canvas, width, height);
        DrawCaptchaDisorderLine(canvas, code, width, height);
        DrawCaptchaDisorderPoint(output, code, background);
        DrawCaptchaText(canvas, code, width, height, fontSize);
        canvas.Flush();

        return SKImage.FromBitmap(output);
    }

    #endregion

    /// <summary>
    /// 克隆图片
    /// </summary>
    private static SKImage CloneImage(SKImage image)
    {
        using var bitmap = SKBitmap.FromImage(image);
        return SKImage.FromBitmap(bitmap);
    }
    /// <summary>
    /// 获取编码格式
    /// </summary>
    private static SKEncodedImageFormat GetEncodedImageFormat(SKImage image)
    {
        using var data = image.EncodedData ?? image.Encode(SKEncodedImageFormat.Png, 100);
        using var codec = SKCodec.Create(data);
        return codec?.EncodedFormat ?? SKEncodedImageFormat.Png;
    }

    /// <summary>
    /// 计算缩放尺寸
    /// </summary>
    private static SKSizeI CalculateResizeSize(int sourceWidth, int sourceHeight, int targetWidth, int targetHeight,
        bool keepAspectRatio, bool allowEnlarge)
    {
        if (!keepAspectRatio)
        {
            return new SKSizeI(
                allowEnlarge ? targetWidth : Math.Min(targetWidth, sourceWidth),
                allowEnlarge ? targetHeight : Math.Min(targetHeight, sourceHeight));
        }

        var scaleX = targetWidth / (double)sourceWidth;
        var scaleY = targetHeight / (double)sourceHeight;
        var scale = Math.Min(scaleX, scaleY);
        if (!allowEnlarge)
            scale = Math.Min(scale, 1d);

        var width = Math.Max(1, (int)Math.Round(sourceWidth * scale, MidpointRounding.AwayFromZero));
        var height = Math.Max(1, (int)Math.Round(sourceHeight * scale, MidpointRounding.AwayFromZero));
        return new SKSizeI(width, height);
    }

    /// <summary>
    /// 规范化裁剪区域
    /// </summary>
    private static SKRectI NormalizeCropRectangle(SKRectI rectangle, int imageWidth, int imageHeight)
    {
        if (rectangle.Width <= 0)
            throw new ArgumentOutOfRangeException(nameof(rectangle), "裁剪区域宽度必须大于0");
        if (rectangle.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(rectangle), "裁剪区域高度必须大于0");

        var left = Math.Clamp(rectangle.Left, 0, imageWidth);
        var top = Math.Clamp(rectangle.Top, 0, imageHeight);
        var right = Math.Clamp(rectangle.Right, 0, imageWidth);
        var bottom = Math.Clamp(rectangle.Bottom, 0, imageHeight);

        if (right <= left || bottom <= top)
            throw new ArgumentOutOfRangeException(nameof(rectangle), "裁剪区域超出图片边界");

        return new SKRectI(left, top, right, bottom);
    }

    /// <summary>
    /// 规范化角度
    /// </summary>
    private static float NormalizeAngle(int angle)
    {
        var normalized = angle % 360;
        if (normalized < 0)
            normalized += 360;
        return normalized;
    }

    /// <summary>
    /// 转换图片像素
    /// </summary>
    private static SKImage TransformImage(SKImage image, Func<SKColor, SKColor> transform)
    {
        using var bitmap = SKBitmap.FromImage(image);
        using var output = new SKBitmap(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (var x = 0; x < bitmap.Width; x++)
        {
            for (var y = 0; y < bitmap.Height; y++)
                output.SetPixel(x, y, transform(bitmap.GetPixel(x, y)));
        }

        return SKImage.FromBitmap(output);
    }

    /// <summary>
    /// 验证阈值
    /// </summary>
    private static void ValidateThreshold(float threshold)
    {
        if (threshold < 0 || threshold > 1)
            throw new ArgumentOutOfRangeException(nameof(threshold), "阈值必须为0-1之间的浮点数");
    }

    /// <summary>
    /// 验证非负比例
    /// </summary>
    private static void ValidateAmount(float amount, string paramName)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(paramName, "参数必须大于或等于0");
    }

    /// <summary>
    /// 验证颜色矩阵
    /// </summary>
    private static void ValidateColorMatrix(float[,] matrix)
    {
        if (matrix is null)
            throw new ArgumentNullException(nameof(matrix));
        if (matrix.GetLength(0) != 5 || matrix.GetLength(1) != 5)
            throw new ArgumentException("颜色矩阵必须为5x5", nameof(matrix));
    }

    /// <summary>
    /// 判断是否为近似颜色
    /// </summary>
    private static bool IsSimilarColors(SKColor x, SKColor y, int accuracy)
    {
        var offsetA = x.Alpha - y.Alpha;
        var offsetR = x.Red - y.Red;
        var offsetG = x.Green - y.Green;
        var offsetB = x.Blue - y.Blue;

        if (Math.Abs(offsetA) > 1)
            return false;

        if (offsetB == offsetG && offsetR == offsetB)
        {
            if (Math.Abs(offsetR) > 1)
                return ColorDifference(x, y) <= accuracy / 3d;
        }

        return ColorDifference(x, y) <= accuracy;
    }

    /// <summary>
    /// 计算颜色差异
    /// </summary>
    private static double ColorDifference(SKColor x, SKColor y)
    {
        var m = (x.Red + y.Red) / 2d;
        var r = Math.Pow(x.Red - y.Red, 2);
        var g = Math.Pow(x.Green - y.Green, 2);
        var b = Math.Pow(x.Blue - y.Blue, 2);
        return Math.Sqrt((2 + m / 256d) * r + 4 * g + (2 + (255 - m) / 256d) * b);
    }

    /// <summary>
    /// 获取灰度值
    /// </summary>
    private static float GetGrayScale(SKColor color)
    {
        return (0.30f * color.Red + 0.59f * color.Green + 0.11f * color.Blue) / 255f;
    }

    /// <summary>
    /// 获取双色调颜色
    /// </summary>
    private static SKColor GetDuotoneColor(SKColor sourceColor, SKColor colorA, SKColor colorB)
    {
        var grayScale = GetGrayScale(sourceColor);
        var r = ClampToByte(colorA.Red * (1 - grayScale) + colorB.Red * grayScale);
        var g = ClampToByte(colorA.Green * (1 - grayScale) + colorB.Green * grayScale);
        var b = ClampToByte(colorA.Blue * (1 - grayScale) + colorB.Blue * grayScale);
        return new SKColor(r, g, b, sourceColor.Alpha);
    }

    /// <summary>
    /// 应用颜色矩阵到单像素
    /// </summary>
    private static SKColor ApplyColorMatrix(SKColor color, float[,] matrix)
    {
        var r = color.Red / 255f;
        var g = color.Green / 255f;
        var b = color.Blue / 255f;
        var a = color.Alpha / 255f;

        var resultR = r * matrix[0, 0] + g * matrix[1, 0] + b * matrix[2, 0] + a * matrix[3, 0] + matrix[4, 0];
        var resultG = r * matrix[0, 1] + g * matrix[1, 1] + b * matrix[2, 1] + a * matrix[3, 1] + matrix[4, 1];
        var resultB = r * matrix[0, 2] + g * matrix[1, 2] + b * matrix[2, 2] + a * matrix[3, 2] + matrix[4, 2];
        var resultA = r * matrix[0, 3] + g * matrix[1, 3] + b * matrix[2, 3] + a * matrix[3, 3] + matrix[4, 3];

        return new SKColor(
            ClampToByte(resultR * 255f),
            ClampToByte(resultG * 255f),
            ClampToByte(resultB * 255f),
            ClampToByte(resultA * 255f));
    }

    /// <summary>
    /// 创建亮度矩阵
    /// </summary>
    private static float[,] CreateBrightnessMatrix(float amount)
    {
        return new[,]
        {
            { amount, 0, 0, 0, 0 },
            { 0, amount, 0, 0, 0 },
            { 0, 0, amount, 0, 0 },
            { 0, 0, 0, 1, 0 },
            { 0, 0, 0, 0, 1 }
        };
    }

    /// <summary>
    /// 创建对比度矩阵
    /// </summary>
    private static float[,] CreateContrastMatrix(float amount)
    {
        var contrast = (-.5F * amount) + .5F;
        return new[,]
        {
            { amount, 0, 0, 0, 0 },
            { 0, amount, 0, 0, 0 },
            { 0, 0, amount, 0, 0 },
            { 0, 0, 0, 1, 0 },
            { contrast, contrast, contrast, 0, 1 }
        };
    }

    /// <summary>
    /// 创建饱和度矩阵
    /// </summary>
    private static float[,] CreateSaturationMatrix(float amount)
    {
        var matrix00 = .213F + (.787F * amount);
        var matrix10 = .715F - (.715F * amount);
        var matrix20 = 1F - (matrix00 + matrix10);

        var matrix01 = .213F - (.213F * amount);
        var matrix11 = .715F + (.285F * amount);
        var matrix21 = 1F - (matrix01 + matrix11);

        var matrix02 = .213F - (.213F * amount);
        var matrix12 = .715F - (.715F * amount);
        var matrix22 = 1F - (matrix02 + matrix12);

        return new[,]
        {
            { matrix00, matrix01, matrix02, 0, 0 },
            { matrix10, matrix11, matrix12, 0, 0 },
            { matrix20, matrix21, matrix22, 0, 0 },
            { 0, 0, 0, 1, 0 },
            { 0, 0, 0, 0, 1 }
        };
    }

    /// <summary>
    /// 应用柔化边缘效果
    /// </summary>
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
    /// 创建 Alpha 蒙层
    /// </summary>
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
    /// 应用 Alpha 蒙层
    /// </summary>
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
    /// 腐蚀 Alpha 蒙层
    /// </summary>
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
    /// 模糊 Alpha 蒙层
    /// </summary>
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
    /// 应用雾化效果
    /// </summary>
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

        return SKImage.FromBitmap(output);
    }

    /// <summary>
    /// 绘制验证码边框
    /// </summary>
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
    /// 绘制验证码干扰线
    /// </summary>
    private static void DrawCaptchaDisorderLine(SKCanvas canvas, string code, int width, int height)
    {
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            IsAntialias = true
        };

        var lineCount = Math.Max(2, Math.Min(4, code.Length));
        for (var i = 0; i < lineCount; i++)
        {
            var seed = code[i % code.Length] + i * 31;
            paint.Color = CreateCaptchaAccentColor(seed);

            var startX = (seed * 7) % width;
            var startY = (seed * 11) % height;
            var endX = width - 1 - ((seed * 13) % width);
            var endY = height - 1 - ((seed * 17) % height);
            canvas.DrawLine(startX, startY, endX, endY, paint);
        }
    }

    /// <summary>
    /// 绘制验证码干扰点
    /// </summary>
    private static void DrawCaptchaDisorderPoint(SKBitmap bitmap, string code, SKColor background)
    {
        var pointCount = Math.Max(6, bitmap.Width * bitmap.Height / 45);
        for (var i = 0; i < pointCount; i++)
        {
            var seed = code[i % code.Length] + i * 53;
            var x = (seed * 19 + i * 7) % bitmap.Width;
            var y = (seed * 23 + i * 11) % bitmap.Height;
            var color = CreateCaptchaAccentColor(seed + background.Red + background.Green + background.Blue);
            bitmap.SetPixel(x, y, color);
        }
    }

    /// <summary>
    /// 绘制验证码文本
    /// </summary>
    private static void DrawCaptchaText(SKCanvas canvas, string code, int width, int height, float fontSize)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(24, 24, 24, 255),
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = SKTypeface.Default,
            FakeBoldText = true
        };

        var metrics = paint.FontMetrics;
        var baseline = (height - metrics.Bottom - metrics.Top) / 2f;
        var cellWidth = width / (float)(code.Length + 1);

        for (var i = 0; i < code.Length; i++)
        {
            var seed = code[i] + i * 17;
            var x = cellWidth * (i + 0.55f);
            var y = baseline + seed % 5 - 2;
            var angle = seed % 21 - 10;

            canvas.Save();
            canvas.Translate(x, y);
            canvas.RotateDegrees(angle);
            canvas.DrawText(code[i].ToString(), 0, 0, paint);
            canvas.Restore();
        }
    }

    /// <summary>
    /// 创建验证码强调色
    /// </summary>
    private static SKColor CreateCaptchaAccentColor(int seed)
    {
        var normalized = Math.Abs(seed);
        return new SKColor(
            (byte)(20 + normalized % 120),
            (byte)(20 + normalized * 3 % 120),
            (byte)(20 + normalized * 5 % 120),
            255);
    }

    /// <summary>
    /// 应用卷积效果
    /// </summary>
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
                        var sampleX = Math.Clamp(x + kx, 0, source.Width - 1);
                        var sampleY = Math.Clamp(y + ky, 0, source.Height - 1);
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

        return SKImage.FromBitmap(output);
    }

    /// <summary>
    /// 将值限制到字节范围
    /// </summary>
    private static byte ClampToByte(float value)
    {
        return (byte)Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 255);
    }
}