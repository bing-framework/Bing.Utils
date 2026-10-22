using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 提供 SkiaSharp 兼容图像操作。
/// </summary>
public static partial class SkiaSharpHelper
{
    #region ToStream(转换为内存流)

    /// <summary>
    /// 将图片转换为内存流，调用方负责释放资源
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>定位到起始位置的图像流。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static Stream ToStream(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return new MemoryStream(ToBytes(image), writable: false);
    }

    #endregion

    #region MakeThumbnail(生成缩略图)

    /// <summary>
    /// 生成缩略图。
    /// </summary>
    /// <param name="sourceImage">源图</param>
    /// <param name="width">缩略图宽度</param>
    /// <param name="height">缩略图高度</param>
    /// <param name="mode">缩略图方式</param>
    /// <returns>生成的缩略图。</returns>
    /// <exception cref="ArgumentNullException">源图为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">目标宽度或高度无效。</exception>
    public static SKImage MakeThumbnail(SKImage sourceImage, int width, int height, ThumbnailMode mode)
    {
        if (sourceImage is null)
            throw new ArgumentNullException(nameof(sourceImage));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        var srcW = sourceImage.Width;
        var srcH = sourceImage.Height;
        long destW = width, destH = height;
        long cropX = 0, cropY = 0, cropW = srcW, cropH = srcH;

        switch (mode)
        {
            case ThumbnailMode.FixedBoth:
                break;

            case ThumbnailMode.FixedW:
                destH = checked((long)srcH * width / srcW);
                break;

            case ThumbnailMode.FixedH:
                destW = checked((long)srcW * height / srcH);
                break;

            case ThumbnailMode.Cut:
                if (srcW / (double)srcH > destW / (double)destH)
                {
                    cropW = checked((long)srcH * destW / destH);
                    cropX = (srcW - cropW) / 2;
                }
                else
                {
                    cropH = checked((long)srcW * destH / destW);
                    cropY = (srcH - cropH) / 2;
                }
                break;
        }

        var outputWidth = ToDimension(destW, nameof(width));
        var outputHeight = ToDimension(destH, nameof(height));
        var cropLeft = ToDimension(cropX, nameof(cropX), allowZero: true);
        var cropTop = ToDimension(cropY, nameof(cropY), allowZero: true);
        var cropWidth = ToDimension(cropW, nameof(cropW));
        var cropHeight = ToDimension(cropH, nameof(cropH));

        using var sourceBitmap = SKBitmap.FromImage(sourceImage);

        if (mode == ThumbnailMode.Cut)
        {
            using var cropped = new SKBitmap(cropWidth, cropHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var cropCanvas = new SKCanvas(cropped);
            cropCanvas.Clear(SKColors.Transparent);
            cropCanvas.DrawBitmap(sourceBitmap, new SKRect(cropLeft, cropTop, cropLeft + cropWidth, cropTop + cropHeight),
                new SKRect(0, 0, cropWidth, cropHeight));

            using var output = new SKBitmap(outputWidth, outputHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var canvas = new SKCanvas(output);
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(cropped, new SKRect(0, 0, outputWidth, outputHeight), paint);
            return CopyTrackedFormat(sourceImage, SKImage.FromBitmap(output));
        }

        {
            using var output = new SKBitmap(outputWidth, outputHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var canvas = new SKCanvas(output);
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(sourceBitmap, new SKRect(0, 0, outputWidth, outputHeight), paint);
            return CopyTrackedFormat(sourceImage, SKImage.FromBitmap(output));
        }
    }

    /// <summary>
    /// 将缩略图计算结果限制在 SkiaSharp 可接受的整数尺寸内。
    /// </summary>
    /// <param name="value">计算后的尺寸。</param>
    /// <param name="parameterName">发生异常时使用的参数名。</param>
    /// <param name="allowZero">是否允许结果为零。</param>
    /// <returns>转换后的整数尺寸。</returns>
    private static int ToDimension(long value, string parameterName, bool allowZero = false)
    {
        var minimum = allowZero ? 0 : 1;
        if (value < minimum || value > int.MaxValue)
            throw new ArgumentOutOfRangeException(parameterName, "计算后的图像尺寸超出有效范围。");
        return (int)value;
    }

    /// <summary>
    /// 生成缩略图。
    /// </summary>
    /// <param name="imgBytes">源文件字节数组</param>
    /// <param name="width">缩略图宽度</param>
    /// <param name="height">缩略图高度</param>
    /// <param name="mode">缩略图方式</param>
    /// <returns>生成的缩略图。</returns>
    /// <exception cref="ArgumentNullException">源字节数组为空。</exception>
    public static SKImage MakeThumbnail(byte[] imgBytes, int width, int height, ThumbnailMode mode)
    {
        if (imgBytes is null)
            throw new ArgumentNullException(nameof(imgBytes));

        using var source = FromBytes(imgBytes);
        if (source is null)
            throw new ArgumentException("无法从字节数组加载图像");

        return MakeThumbnail(source, width, height, mode);
    }

    /// <summary>
    /// 生成缩略图。
    /// </summary>
    /// <remarks>从源文件生成缩略图，并将结果写入指定目标路径。</remarks>
    /// <param name="sourceImagePath">源文件路径</param>
    /// <param name="thumbnailPath">缩略图保存路径</param>
    /// <param name="width">缩略图宽度</param>
    /// <param name="height">缩略图高度</param>
    /// <param name="mode">缩略图方式</param>
    /// <exception cref="ArgumentNullException">源文件路径或目标文件路径为空。</exception>
    public static void MakeThumbnail(string sourceImagePath, string thumbnailPath, int width, int height, ThumbnailMode mode)
    {
        if (string.IsNullOrWhiteSpace(sourceImagePath))
            throw new ArgumentNullException(nameof(sourceImagePath));
        if (string.IsNullOrWhiteSpace(thumbnailPath))
            throw new ArgumentNullException(nameof(thumbnailPath));

        var bytes = File.ReadAllBytes(sourceImagePath);
        using var source = FromBytes(bytes);
        if (source is null)
            throw new ArgumentException("无法从文件加载图像");

        using var result = MakeThumbnail(source, width, height, mode);
        var resultBytes = ToBytes(result);
        Internal.ImageFileWriter.Write(thumbnailPath, resultBytes);
    }

    #endregion

    #region ScaleImage(缩放图像)

    /// <summary>
    /// 缩放图像到指定画布大小。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="width">目标画布宽度</param>
    /// <param name="height">目标画布高度</param>
    /// <returns>缩放后的图像。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">目标宽度或高度无效。</exception>
    public static SKImage ScaleImage(SKImage image, int width, int height)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        var scaleW = width / (double)image.Width;
        var scaleH = height / (double)image.Height;
        var scale = Math.Min(scaleW, scaleH);

        var scaledW = Math.Max(1, (int)Math.Round(image.Width * scale, MidpointRounding.AwayFromZero));
        var scaledH = Math.Max(1, (int)Math.Round(image.Height * scale, MidpointRounding.AwayFromZero));
        var offsetX = (width - scaledW) / 2;
        var offsetY = (height - scaledH) / 2;

        using var sourceBitmap = SKBitmap.FromImage(image);
        using var scaledBitmap = new SKBitmap(scaledW, scaledH, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var scaledCanvas = new SKCanvas(scaledBitmap);
        using var scaledPaint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
        scaledCanvas.Clear(SKColors.Transparent);
        scaledCanvas.DrawBitmap(sourceBitmap, new SKRect(0, 0, scaledW, scaledH), scaledPaint);

        using var output = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(output);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(scaledBitmap, offsetX, offsetY);
        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    #endregion

    #region Gray(图片灰度化)

    /// <summary>
    /// 图片灰度化。使用加权公式 Gray = 0.299*R + 0.587*G + 0.114*B。
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>灰度处理后的图像。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static SKImage Gray(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        return TransformImage(image, color =>
        {
            if (color.Red + color.Green + color.Blue == 0)
                return new SKColor(255, 255, 255, color.Alpha);
            var gray = (byte)((color.Red * 19595 + color.Green * 38469 + color.Blue * 7472) >> 16);
            return new SKColor(gray, gray, gray, color.Alpha);
        });
    }

    #endregion

    #region ToBlackWhiteImage(转换为黑白图片)

    /// <summary>
    /// 将图像转换为黑白图片。使用 RGB 均值公式 result = (R + G + B) / 3。
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>黑白处理后的图像。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static SKImage ToBlackWhiteImage(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        return TransformImage(image, color =>
        {
            var avg = (byte)((color.Red + color.Green + color.Blue) / 3);
            return new SKColor(avg, avg, avg, color.Alpha);
        });
    }

    #endregion

    #region FilterColor(滤色处理)

    /// <summary>
    /// 滤色处理，将红色通道置零
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>滤色处理后的图像。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static SKImage FilterColor(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        return TransformImage(image, color => new SKColor(0, color.Green, color.Blue, color.Alpha));
    }

    #endregion

    #region Plate(底片效果)

    /// <summary>
    /// 底片效果，反转 RGB 通道
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>底片处理后的图像。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static SKImage Plate(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        return TransformImage(image, color => new SKColor(
            (byte)(255 - color.Red),
            (byte)(255 - color.Green),
            (byte)(255 - color.Blue),
            color.Alpha));
    }

    #endregion

    #region PerPixelProcess(逐像素处理)

    /// <summary>
    /// 对图像进行逐像素处理，返回新图像
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="func">像素转换函数，输入原始颜色，返回目标颜色</param>
    /// <returns>逐像素处理后的图像。</returns>
    /// <exception cref="ArgumentNullException">图像或像素转换函数为空。</exception>
    public static SKImage PerPixelProcess(SKImage image, Func<SKColor, SKColor> func)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (func is null)
            throw new ArgumentNullException(nameof(func));

        return TransformImage(image, func);
    }

    #endregion

    #region ColorExtensions(颜色扩展 - 公共)

    /// <summary>
    /// 获取颜色的灰度值（0..1）
    /// </summary>
    /// <param name="color">颜色</param>
    /// <returns>范围为 0 到 1 的灰度值。</returns>
    public static float GetGrayScale(SKColor color)
    {
        return (0.30f * color.Red + 0.59f * color.Green + 0.11f * color.Blue) / 255f;
    }

    /// <summary>
    /// 获取双色调效果颜色
    /// </summary>
    /// <param name="sourceColor">原始颜色</param>
    /// <param name="clr1">决定双色调效果的颜色A</param>
    /// <param name="clr2">决定双色调效果的颜色B</param>
    /// <returns>按灰度值在两种颜色之间插值得到的颜色。</returns>
    public static SKColor GetDuotoneColor(SKColor sourceColor, SKColor clr1, SKColor clr2)
    {
        var grayScale = GetGrayScale(sourceColor);
        var r = (byte)(clr1.Red * (1 - grayScale) + clr2.Red * grayScale);
        var g = (byte)(clr1.Green * (1 - grayScale) + clr2.Green * grayScale);
        var b = (byte)(clr1.Blue * (1 - grayScale) + clr2.Blue * grayScale);
        return new SKColor(r, g, b, sourceColor.Alpha);
    }

    /// <summary>
    /// 是否是近似颜色
    /// </summary>
    /// <param name="x">颜色A</param>
    /// <param name="y">颜色B</param>
    /// <param name="accuracy">允许的误差值。默认：36</param>
    /// <returns>颜色相近返回 <see langword="true" />，否则返回 <see langword="false" />。</returns>
    public static bool IsSimilarColors(SKColor x, SKColor y, int accuracy = 36)
    {
        if (Math.Abs(x.Alpha - y.Alpha) > 1)
            return false;

        var offsetR = x.Red - y.Red;
        var offsetG = x.Green - y.Green;
        var offsetB = x.Blue - y.Blue;

        if (offsetB == offsetG && offsetR == offsetB)
        {
            if (Math.Abs(offsetR) > 1)
                return ColorDifferenceInternal(x, y) <= accuracy / 3d;
        }

        return ColorDifferenceInternal(x, y) <= accuracy;
    }

    /// <summary>
    /// 颜色差异，在 RGB 空间上通过公式计算出加权的欧式距离
    /// </summary>
    /// <param name="x">颜色A</param>
    /// <param name="y">颜色B</param>
    /// <returns>两种颜色的加权差异值。</returns>
    public static double ColorDifference(SKColor x, SKColor y)
    {
        return ColorDifferenceInternal(x, y);
    }

    /// <summary>
    /// 混合颜色
    /// </summary>
    /// <param name="color">背景颜色</param>
    /// <param name="backColor">其它混合背景颜色</param>
    /// <param name="amount">保留多少颜色（0..1）</param>
    /// <returns>混合后的颜色。</returns>
    public static SKColor Blend(SKColor color, SKColor backColor, double amount)
    {
        var r = (byte)(color.Red * amount + backColor.Red * (1 - amount));
        var g = (byte)(color.Green * amount + backColor.Green * (1 - amount));
        var b = (byte)(color.Blue * amount + backColor.Blue * (1 - amount));
        return new SKColor(r, g, b, color.Alpha);
    }

    /// <summary>
    /// 计算颜色差异（内部使用）
    /// </summary>
    /// <param name="x">第一个颜色。</param>
    /// <param name="y">第二个颜色。</param>
    /// <returns>两种颜色的加权差异值。</returns>
    private static double ColorDifferenceInternal(SKColor x, SKColor y)
    {
        var m = (x.Red + y.Red) / 2d;
        var r = Math.Pow(x.Red - y.Red, 2);
        var g = Math.Pow(x.Green - y.Green, 2);
        var b = Math.Pow(x.Blue - y.Blue, 2);
        return Math.Sqrt((2 + m / 256d) * r + 4 * g + (2 + (255 - m) / 256d) * b);
    }

    #endregion
}
