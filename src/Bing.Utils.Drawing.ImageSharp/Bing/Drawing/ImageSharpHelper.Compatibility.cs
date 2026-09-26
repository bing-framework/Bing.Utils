using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace Bing.Drawing;

/// <summary>
/// 提供 ImageSharp 兼容图像操作。
/// </summary>
public static partial class ImageSharpHelper
{
    #region ToStream(转换为内存流)

    /// <summary>
    /// 将图片转换为内存流，调用方负责释放资源
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>定位到起始位置的图像流。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static Stream ToStream(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        var ms = new MemoryStream();
        try
        {
            Save(image, ms, PngFormat.Instance);
            ms.Position = 0;
            return ms;
        }
        catch
        {
            ms.Dispose();
            throw;
        }
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
    public static Image MakeThumbnail(Image sourceImage, int width, int height, ThumbnailMode mode)
    {
        if (sourceImage is null)
            throw new ArgumentNullException(nameof(sourceImage));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        var srcW = sourceImage.Width;
        var srcH = sourceImage.Height;
        int destW = width, destH = height;
        int cropX = 0, cropY = 0, cropW = srcW, cropH = srcH;

        switch (mode)
        {
            case ThumbnailMode.FixedBoth:
                break;

            case ThumbnailMode.FixedW:
                destH = checked((int)((long)srcH * width / srcW));
                break;

            case ThumbnailMode.FixedH:
                destW = checked((int)((long)srcW * height / srcH));
                break;

            case ThumbnailMode.Cut:
                if (srcW / (double)srcH > destW / (double)destH)
                {
                    cropW = checked((int)((long)srcH * destW / destH));
                    cropX = (srcW - cropW) / 2;
                }
                else
                {
                    cropH = checked((int)((long)srcW * destH / destW));
                    cropY = (srcH - cropH) / 2;
                }
                break;
        }

        if (mode == ThumbnailMode.Cut)
        {
            var cropRect = new Rectangle(cropX, cropY, cropW, cropH);
            using var cropped = sourceImage.CloneAs<Rgba32>();
            cropped.Mutate(ctx => ctx.Crop(cropRect));
            Image<Rgba32>? output = null;
            try
            {
                output = cropped.CloneAs<Rgba32>();
                output.Mutate(ctx => ctx.Resize(destW, destH));
                var result = CopyTrackedFormat(sourceImage, output);
                output = null;
                return result;
            }
            finally { output?.Dispose(); }
        }

        {
            Image<Rgba32>? output = null;
            try
            {
                output = sourceImage.CloneAs<Rgba32>();
                output.Mutate(ctx => ctx.Resize(destW, destH));
                var result = CopyTrackedFormat(sourceImage, output);
                output = null;
                return result;
            }
            finally { output?.Dispose(); }
        }
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
    public static Image MakeThumbnail(byte[] imgBytes, int width, int height, ThumbnailMode mode)
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
    public static Image ScaleImage(Image image, int width, int height)
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

        using var scaled = image.CloneAs<Rgba32>();
        scaled.Mutate(ctx => ctx.Resize(scaledW, scaledH));

        Image<Rgba32>? output = null;
        try
        {
            output = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, 0));
            output.Mutate(ctx => ctx.DrawImage(scaled, new Point(offsetX, offsetY), 1f));
            var result = CopyTrackedFormat(image, output);
            output = null;
            return result;
        }
        finally { output?.Dispose(); }
    }

    #endregion

    #region Gray(图片灰度化)

    /// <summary>
    /// 图片灰度化。使用加权公式 Gray = 0.299*R + 0.587*G + 0.114*B。
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>灰度处理后的图像。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static Image Gray(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        Image<Rgba32>? output = null;
        try
        {
            output = image.CloneAs<Rgba32>();
            for (var x = 0; x < output.Width; x++)
            {
                for (var y = 0; y < output.Height; y++)
                {
                    var color = output[x, y];
                    if (color.R + color.G + color.B == 0)
                        output[x, y] = new Rgba32(255, 255, 255, color.A);
                    else
                    {
                        var gray = (byte)((color.R * 19595 + color.G * 38469 + color.B * 7472) >> 16);
                        output[x, y] = new Rgba32(gray, gray, gray, color.A);
                    }
                }
            }
            var result = CopyTrackedFormat(image, output);
            output = null;
            return result;
        }
        finally { output?.Dispose(); }
    }

    #endregion

    #region ToBlackWhiteImage(转换为黑白图片)

    /// <summary>
    /// 将图像转换为黑白图片。使用 RGB 均值公式 result = (R + G + B) / 3。
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>黑白处理后的图像。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static Image ToBlackWhiteImage(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        Image<Rgba32>? output = null;
        try
        {
            output = image.CloneAs<Rgba32>();
            for (var x = 0; x < output.Width; x++)
            {
                for (var y = 0; y < output.Height; y++)
                {
                    var color = output[x, y];
                    var avg = (byte)((color.R + color.G + color.B) / 3);
                    output[x, y] = new Rgba32(avg, avg, avg, color.A);
                }
            }
            var result = CopyTrackedFormat(image, output);
            output = null;
            return result;
        }
        finally { output?.Dispose(); }
    }

    #endregion

    #region FilterColor(滤色处理)

    /// <summary>
    /// 滤色处理，将红色通道置零
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>滤色处理后的图像。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static Image FilterColor(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        Image<Rgba32>? output = null;
        try
        {
            output = image.CloneAs<Rgba32>();
            for (var x = 0; x < output.Width; x++)
            {
                for (var y = 0; y < output.Height; y++)
                {
                    var color = output[x, y];
                    output[x, y] = new Rgba32(0, color.G, color.B, color.A);
                }
            }
            var result = CopyTrackedFormat(image, output);
            output = null;
            return result;
        }
        finally { output?.Dispose(); }
    }

    #endregion

    #region Plate(底片效果)

    /// <summary>
    /// 底片效果，反转 RGB 通道
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>底片处理后的图像。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static Image Plate(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        Image<Rgba32>? output = null;
        try
        {
            output = image.CloneAs<Rgba32>();
            for (var x = 0; x < output.Width; x++)
            {
                for (var y = 0; y < output.Height; y++)
                {
                    var color = output[x, y];
                    output[x, y] = new Rgba32(
                        (byte)(255 - color.R),
                        (byte)(255 - color.G),
                        (byte)(255 - color.B),
                        color.A);
                }
            }
            var result = CopyTrackedFormat(image, output);
            output = null;
            return result;
        }
        finally { output?.Dispose(); }
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
    public static Image PerPixelProcess(Image image, Func<Rgba32, Rgba32> func)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (func is null)
            throw new ArgumentNullException(nameof(func));

        Image<Rgba32>? output = null;
        try
        {
            output = image.CloneAs<Rgba32>();
            for (var x = 0; x < output.Width; x++)
            {
                for (var y = 0; y < output.Height; y++)
                    output[x, y] = func(output[x, y]);
            }

            var result = CopyTrackedFormat(image, output);
            output = null;
            return result;
        }
        finally { output?.Dispose(); }
    }

    #endregion

    #region ColorExtensions(颜色扩展 - 公共)

    /// <summary>
    /// 获取颜色的灰度值（0..1）
    /// </summary>
    /// <param name="color">颜色</param>
    /// <returns>范围为 0 到 1 的灰度值。</returns>
    public static float GetGrayScale(Color color)
    {
        var pixel = color.ToPixel<Rgba32>();
        return (0.30f * pixel.R + 0.59f * pixel.G + 0.11f * pixel.B) / 255f;
    }

    /// <summary>
    /// 获取双色调效果颜色
    /// </summary>
    /// <param name="sourceColor">原始颜色</param>
    /// <param name="clr1">决定双色调效果的颜色A</param>
    /// <param name="clr2">决定双色调效果的颜色B</param>
    /// <returns>按灰度值在两种颜色之间插值得到的颜色。</returns>
    public static Color GetDuotoneColor(Color sourceColor, Color clr1, Color clr2)
    {
        var grayScale = GetGrayScale(sourceColor);
        var c1 = clr1.ToPixel<Rgba32>();
        var c2 = clr2.ToPixel<Rgba32>();
        var src = sourceColor.ToPixel<Rgba32>();
        var r = c1.R * (1 - grayScale) + c2.R * grayScale;
        var g = c1.G * (1 - grayScale) + c2.G * grayScale;
        var b = c1.B * (1 - grayScale) + c2.B * grayScale;
        return Color.FromRgba((byte)r, (byte)g, (byte)b, src.A);
    }

    /// <summary>
    /// 是否是近似颜色
    /// </summary>
    /// <param name="x">颜色A</param>
    /// <param name="y">颜色B</param>
    /// <param name="accuracy">允许的误差值。默认：36</param>
    /// <returns>颜色相近返回 <see langword="true" />，否则返回 <see langword="false" />。</returns>
    public static bool IsSimilarColors(Color x, Color y, int accuracy = 36)
    {
        var px = x.ToPixel<Rgba32>();
        var py = y.ToPixel<Rgba32>();

        if (Math.Abs(px.A - py.A) > 1)
            return false;

        var offsetR = px.R - py.R;
        var offsetG = px.G - py.G;
        var offsetB = px.B - py.B;

        if (offsetB == offsetG && offsetR == offsetB)
        {
            if (Math.Abs(offsetR) > 1)
                return ColorDifferenceInternal(px, py) <= accuracy / 3d;
        }

        return ColorDifferenceInternal(px, py) <= accuracy;
    }

    /// <summary>
    /// 颜色差异，在 RGB 空间上通过公式计算出加权的欧式距离
    /// </summary>
    /// <param name="x">颜色A</param>
    /// <param name="y">颜色B</param>
    /// <returns>两种颜色的加权差异值。</returns>
    public static double ColorDifference(Color x, Color y)
    {
        return ColorDifferenceInternal(x.ToPixel<Rgba32>(), y.ToPixel<Rgba32>());
    }

    /// <summary>
    /// 混合颜色
    /// </summary>
    /// <param name="color">背景颜色</param>
    /// <param name="backColor">其它混合背景颜色</param>
    /// <param name="amount">保留多少颜色（0..1）</param>
    /// <returns>混合后的颜色。</returns>
    public static Color Blend(Color color, Color backColor, double amount)
    {
        var c1 = color.ToPixel<Rgba32>();
        var c2 = backColor.ToPixel<Rgba32>();
        var r = (byte)(c1.R * amount + c2.R * (1 - amount));
        var g = (byte)(c1.G * amount + c2.G * (1 - amount));
        var b = (byte)(c1.B * amount + c2.B * (1 - amount));
        return Color.FromRgba(r, g, b, c1.A);
    }

    /// <summary>
    /// 计算颜色差异（内部使用）
    /// </summary>
    /// <param name="x">第一个颜色。</param>
    /// <param name="y">第二个颜色。</param>
    /// <returns>两种颜色的加权差异值。</returns>
    private static double ColorDifferenceInternal(Rgba32 x, Rgba32 y)
    {
        var m = (x.R + y.R) / 2d;
        var r = Math.Pow(x.R - y.R, 2);
        var g = Math.Pow(x.G - y.G, 2);
        var b = Math.Pow(x.B - y.B, 2);
        return Math.Sqrt((2 + m / 256d) * r + 4 * g + (2 + (255 - m) / 256d) * b);
    }

    #endregion
}
