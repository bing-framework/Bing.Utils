using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Bing.Drawing;

/// <summary>
/// 基于 SixLabors.ImageSharp 实现的图片操作辅助类
/// </summary>
public static partial class ImageSharpHelper
{
    private static readonly ConditionalWeakTable<Image, ImageFormatHolder> ImageFormats = new();

    private sealed class ImageFormatHolder
    {
        public ImageFormatHolder(IImageFormat format)
        {
            Format = format;
        }

        public IImageFormat Format { get; }
    }

    #region GetCaptchaCode(获取验证码文本)

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

    #endregion

    #region GetImageExtension(获取图片扩展名)

    /// <summary>
    /// 获取图片扩展名
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetImageExtension(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return NormalizeImageFormat(GetTrackedFormat(image)).FileExtensions.FirstOrDefault() ?? string.Empty;
    }

    #endregion

    #region GetMimeType(获取 Mime 类型)

    /// <summary>
    /// 获取 Mime 类型
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetMimeType(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return NormalizeImageFormat(GetTrackedFormat(image)).DefaultMimeType;
    }

    #endregion

    #region HasAlpha(是否包含透明像素)

    /// <summary>
    /// 是否包含透明像素
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool HasAlpha(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        using var rgba = image.CloneAs<Rgba32>();
        for (var x = 0; x < rgba.Width; x++)
        {
            for (var y = 0; y < rgba.Height; y++)
            {
                if (rgba[x, y].A < byte.MaxValue)
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
    public static Image SetOpacity(Image image, float opacity)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (opacity < 0 || opacity > 1)
            throw new ArgumentOutOfRangeException(nameof(opacity), "不透明度必须为0-1之间的浮点数");

        var output = image.CloneAs<Rgba32>();
        output.Mutate(o => o.Opacity(opacity));
        CopyTrackedFormat(image, output);
        return output;
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
    public static Image Resize(Image image, int width, int height, bool keepAspectRatio = true, bool allowEnlarge = true)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        var size = CalculateResizeSize(image.Width, image.Height, width, height, keepAspectRatio, allowEnlarge);
        if (size.Width == image.Width && size.Height == image.Height)
            return CopyTrackedFormat(image, image.CloneAs<Rgba32>());

        var output = image.CloneAs<Rgba32>();
        output.Mutate(ctx => ctx.Resize(size));
        return CopyTrackedFormat(image, output);
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
    public static Image Crop(Image image, Rectangle rectangle)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var cropRectangle = NormalizeCropRectangle(rectangle, image.Width, image.Height);
        var output = image.CloneAs<Rgba32>();
        output.Mutate(ctx => ctx.Crop(cropRectangle));
        return CopyTrackedFormat(image, output);
    }

    #endregion

    #region Rotate(旋转图片)

    /// <summary>
    /// 旋转图片
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="angle">旋转角度</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Image Rotate(Image image, int angle)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var normalizedAngle = NormalizeAngle(angle);
        if (normalizedAngle == 0)
            return CopyTrackedFormat(image, image.CloneAs<Rgba32>());

        var output = image.CloneAs<Rgba32>();
        output.Mutate(ctx => ctx.Rotate(normalizedAngle));
        return CopyTrackedFormat(image, output);
    }

    #endregion

    #region FlipHorizontal(左右翻转)

    /// <summary>
    /// 左右翻转
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Image FlipHorizontal(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        var output = image.CloneAs<Rgba32>();
        output.Mutate(ctx => ctx.Flip(FlipMode.Horizontal));
        return CopyTrackedFormat(image, output);
    }

    #endregion

    #region FlipVertical(上下翻转)

    /// <summary>
    /// 上下翻转
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Image FlipVertical(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        var output = image.CloneAs<Rgba32>();
        output.Mutate(ctx => ctx.Flip(FlipMode.Vertical));
        return CopyTrackedFormat(image, output);
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
    public static Image ReplaceColor(Image image, Color colorA, Color colorB, int accuracy = 36)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (accuracy < 0)
            throw new ArgumentOutOfRangeException(nameof(accuracy));

        var source = colorA.ToPixel<Rgba32>();
        var target = colorB.ToPixel<Rgba32>();
        var output = image.CloneAs<Rgba32>();
        for (var x = 0; x < output.Width; x++)
        {
            for (var y = 0; y < output.Height; y++)
            {
                if (IsSimilarColors(output[x, y], source, accuracy))
                    output[x, y] = target;
            }
        }

        return CopyTrackedFormat(image, output);
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
    public static Image SetBlackWhiteEffect(Image image, float threshold)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        ValidateThreshold(threshold);

        var output = image.CloneAs<Rgba32>();
        for (var x = 0; x < output.Width; x++)
        {
            for (var y = 0; y < output.Height; y++)
            {
                var color = output[x, y];
                var rgb = GetGrayScale(color) >= threshold ? byte.MaxValue : (byte)0;
                output[x, y] = new Rgba32(rgb, rgb, rgb, color.A);
            }
        }

        return CopyTrackedFormat(image, output);
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
    public static Image SetDuotoneEffect(Image image, Color colorA, Color colorB)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var sourceA = colorA.ToPixel<Rgba32>();
        var sourceB = colorB.ToPixel<Rgba32>();
        var output = image.CloneAs<Rgba32>();
        for (var x = 0; x < output.Width; x++)
        {
            for (var y = 0; y < output.Height; y++)
                output[x, y] = GetDuotoneColor(output[x, y], sourceA, sourceB);
        }

        return CopyTrackedFormat(image, output);
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
    public static Image SetBrightness(Image image, float amount)
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
    public static Image SetContrast(Image image, float amount)
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
    public static Image SetSaturation(Image image, float amount)
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
    public static Image ApplyColorMatrix(Image image, float[,] matrix)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        ValidateColorMatrix(matrix);

        var output = image.CloneAs<Rgba32>();
        for (var x = 0; x < output.Width; x++)
        {
            for (var y = 0; y < output.Height; y++)
                output[x, y] = ApplyColorMatrix(output[x, y], matrix);
        }

        return CopyTrackedFormat(image, output);
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
    public static Image SoftEdge(Image image, float radius)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius), "柔化半径必须大于或等于0");

        var output = image.CloneAs<Rgba32>();
        ApplySoftEdgeEffect(output, radius);
        return CopyTrackedFormat(image, output);
    }

    #endregion

    #region Soften(柔化效果)

    /// <summary>
    /// 柔化效果
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Image Soften(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var output = ApplyConvolutionEffect(image, new float[] { 1, 2, 1, 2, 4, 2, 1, 2, 1 }, 16f);
        return CopyTrackedFormat(image, output);
    }

    #endregion

    #region Sharpen(锐化效果)

    /// <summary>
    /// 锐化效果
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Image Sharpen(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var output = ApplyConvolutionEffect(image, new float[] { 0, -1, 0, -1, 5, -1, 0, -1, 0 }, 1f);
        return CopyTrackedFormat(image, output);
    }

    #endregion

    #region Emboss(浮雕效果)

    /// <summary>
    /// 浮雕效果
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Image Emboss(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var output = ApplyConvolutionEffect(image, new float[] { -1, -1, 0, -1, 0, 1, 0, 1, 1 }, 1f, 128f);
        return CopyTrackedFormat(image, output);
    }

    #endregion

    #region Atomizing(雾化效果)

    /// <summary>
    /// 雾化效果
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Image Atomizing(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var output = ApplyAtomizingEffect(image);
        return CopyTrackedFormat(image, output);
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
    public static Image AddImageWatermark(Image image, Image watermark, Rectangle targetRectangle, float opacity = 1)
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
        var output = image.CloneAs<Rgba32>();
        using var overlay = watermark.CloneAs<Rgba32>();
        if (overlay.Width != normalized.Width || overlay.Height != normalized.Height)
            overlay.Mutate(ctx => ctx.Resize(normalized.Width, normalized.Height));

        output.Mutate(ctx => ctx.DrawImage(overlay, new Point(normalized.X, normalized.Y), opacity));
        return CopyTrackedFormat(image, output);
    }

    #endregion

    /// <summary>
    /// 计算缩放尺寸
    /// </summary>
    private static Size CalculateResizeSize(int sourceWidth, int sourceHeight, int targetWidth, int targetHeight,
        bool keepAspectRatio, bool allowEnlarge)
    {
        var (w, h) = Internal.ImageGeometryHelper.CalculateResizeSize(
            sourceWidth, sourceHeight, targetWidth, targetHeight, keepAspectRatio, allowEnlarge);
        return new Size(w, h);
    }

    /// <summary>
    /// 规范化裁剪区域
    /// </summary>
    private static Rectangle NormalizeCropRectangle(Rectangle rectangle, int imageWidth, int imageHeight)
    {
        var (left, top, width, height) = Internal.ImageGeometryHelper.NormalizeCropRectangle(
            rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height, imageWidth, imageHeight);
        return new Rectangle(left, top, width, height);
    }

    /// <summary>
    /// 规范化角度
    /// </summary>
    private static float NormalizeAngle(int angle)
    {
        return Internal.ImageGeometryHelper.NormalizeAngle(angle);
    }

    /// <summary>
    /// 验证阈值
    /// </summary>
    private static void ValidateThreshold(float threshold)
    {
        Internal.ImageGeometryHelper.ValidateThreshold(threshold);
    }

    /// <summary>
    /// 验证非负比例
    /// </summary>
    private static void ValidateAmount(float amount, string paramName)
    {
        Internal.ImageGeometryHelper.ValidateAmount(amount, paramName);
    }

    /// <summary>
    /// 验证颜色矩阵
    /// </summary>
    private static void ValidateColorMatrix(float[,] matrix)
    {
        Internal.ImageGeometryHelper.ValidateColorMatrix(matrix);
    }

    /// <summary>
    /// 判断是否为近似颜色
    /// </summary>
    private static bool IsSimilarColors(Rgba32 x, Rgba32 y, int accuracy)
    {
        return Internal.ImageGeometryHelper.IsSimilarColors(x.A, x.R, x.G, x.B, y.A, y.R, y.G, y.B, accuracy);
    }

    /// <summary>
    /// 计算颜色差异
    /// </summary>
    private static double ColorDifference(Rgba32 x, Rgba32 y)
    {
        return Internal.ImageGeometryHelper.ColorDifference(x.R, x.G, x.B, y.R, y.G, y.B);
    }

    /// <summary>
    /// 获取灰度值
    /// </summary>
    private static float GetGrayScale(Rgba32 color)
    {
        return Internal.ImageGeometryHelper.GetGrayScale(color.R, color.G, color.B);
    }

    /// <summary>
    /// 获取双色调颜色
    /// </summary>
    private static Rgba32 GetDuotoneColor(Rgba32 sourceColor, Rgba32 colorA, Rgba32 colorB)
    {
        var grayScale = GetGrayScale(sourceColor);
        var r = ClampToByte(colorA.R * (1 - grayScale) + colorB.R * grayScale);
        var g = ClampToByte(colorA.G * (1 - grayScale) + colorB.G * grayScale);
        var b = ClampToByte(colorA.B * (1 - grayScale) + colorB.B * grayScale);
        return new Rgba32(r, g, b, sourceColor.A);
    }

    /// <summary>
    /// 应用颜色矩阵到单像素
    /// </summary>
    private static Rgba32 ApplyColorMatrix(Rgba32 color, float[,] matrix)
    {
        var (r, g, b, a) = Internal.ImageGeometryHelper.ApplyColorMatrix(color.R, color.G, color.B, color.A, matrix);
        return new Rgba32(r, g, b, a);
    }

    /// <summary>
    /// 创建亮度矩阵
    /// </summary>
    private static float[,] CreateBrightnessMatrix(float amount)
    {
        return ColorMatrices.CreateBrightnessFilter(amount);
    }

    /// <summary>
    /// 创建对比度矩阵
    /// </summary>
    private static float[,] CreateContrastMatrix(float amount)
    {
        return ColorMatrices.CreateContrastFilter(amount);
    }

    /// <summary>
    /// 创建饱和度矩阵
    /// </summary>
    private static float[,] CreateSaturationMatrix(float amount)
    {
        return ColorMatrices.CreateSaturationFilter(amount);
    }

    /// <summary>
    /// 应用柔化边缘效果
    /// </summary>
    private static void ApplySoftEdgeEffect(Image<Rgba32> image, float radius)
    {
        var mask = CreateSoftEdgeAlphaMask(image);
        var offset = Math.Max(0, (int)Math.Round(radius / 4.0, MidpointRounding.AwayFromZero));
        if (offset > 0)
        {
            mask = ErodeAlphaMask(mask, offset, offset, 3);
            mask = BlurAlphaMask(mask, offset, offset, 3);
        }

        ApplySoftEdgeAlphaMask(image, mask);
    }

    /// <summary>
    /// 创建 Alpha 蒙层
    /// </summary>
    private static byte[,] CreateSoftEdgeAlphaMask(Image<Rgba32> image)
    {
        var mask = new byte[image.Width, image.Height];
        for (var x = 0; x < image.Width; x++)
        {
            for (var y = 0; y < image.Height; y++)
                mask[x, y] = image[x, y].A == 0 ? (byte)0 : byte.MaxValue;
        }

        return mask;
    }

    /// <summary>
    /// 应用 Alpha 蒙层
    /// </summary>
    private static void ApplySoftEdgeAlphaMask(Image<Rgba32> image, byte[,] mask)
    {
        for (var x = 0; x < image.Width; x++)
        {
            for (var y = 0; y < image.Height; y++)
            {
                var color = image[x, y];
                color.A = ClampToByte(mask[x, y] / 255f * color.A);
                image[x, y] = color;
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
    private static Image<Rgba32> ApplyAtomizingEffect(Image image)
    {
        using var source = image.CloneAs<Rgba32>();
        var output = new Image<Rgba32>(source.Width, source.Height);
        const int blockSize = 19;

        for (var x = 0; x < source.Width; x++)
        {
            for (var y = 0; y < source.Height; y++)
            {
                var offset = ((x + 1) * 13 + (y + 1) * 7 + source.Width * 5 + source.Height * 3) % blockSize;
                var sampleX = Math.Min(source.Width - 1, x + offset);
                var sampleY = Math.Min(source.Height - 1, y + offset);
                output[x, y] = source[sampleX, sampleY];
            }
        }

        return output;
    }

    /// <summary>
    /// 应用卷积效果
    /// </summary>
    private static Image<Rgba32> ApplyConvolutionEffect(Image image, float[] kernel, float divisor, float bias = 0)
    {
        using var source = image.CloneAs<Rgba32>();
        var output = new Image<Rgba32>(source.Width, source.Height);

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
                        var sample = source[sampleX, sampleY];
                        var weight = kernel[index++];
                        totalR += sample.R * weight;
                        totalG += sample.G * weight;
                        totalB += sample.B * weight;
                    }
                }

                output[x, y] = new Rgba32(
                    ClampToByte(totalR / divisor + bias),
                    ClampToByte(totalG / divisor + bias),
                    ClampToByte(totalB / divisor + bias),
                    source[x, y].A);
            }
        }

        return output;
    }

    /// <summary>
    /// 将值限制到字节范围
    /// </summary>
    private static byte ClampToByte(float value)
    {
        return DrawingCompatibilityHelper.ClampToByte(value);
    }

    /// <summary>
    /// 记录图片格式
    /// </summary>
    private static TImage? TrackFormat<TImage>(TImage? image, IImageFormat? imageFormat) where TImage : Image
    {
        if (image is null || imageFormat is null)
            return image;
        ImageFormats.Remove(image);
        ImageFormats.Add(image, new ImageFormatHolder(imageFormat));
        return image;
    }

    /// <summary>
    /// 复制图片格式信息
    /// </summary>
    private static TImage CopyTrackedFormat<TImage>(Image source, TImage target) where TImage : Image
    {
        return TrackFormat(target, GetTrackedFormat(source))!;
    }

    /// <summary>
    /// 获取已跟踪图片格式
    /// </summary>
    private static IImageFormat? GetTrackedFormat(Image image)
    {
        return ImageFormats.TryGetValue(image, out var holder) ? holder.Format : null;
    }
}