using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Bing.Drawing;

/// <summary>
/// 图像(<see cref="Bitmap"/>) 扩展
/// </summary>
public static class BitmapExtensions
{
    /// <summary>
    /// 更改当前图像的亮度
    /// </summary>
    /// <param name="bitmap">图像</param>
    /// <param name="percentage">改变图像亮度的百分比。范围：-100..100。</param>
    public static void SetBrightness(this Bitmap bitmap, float percentage)
    {
        var colorMatrix = ColorMatrices.CreateBrightnessFilter(percentage);
        ColorMatrices.ApplyMatrix(bitmap, colorMatrix);
    }

    /// <summary>
    /// 更改当前图像的对比度
    /// </summary>
    /// <param name="bitmap">图像</param>
    /// <param name="percentage">改变图像对比度的百分比。范围：-100..100。</param>
    public static void SetContrast(this Bitmap bitmap, float percentage)
    {
        var colorMatrix = ColorMatrices.CreateContrastFilter(percentage);
        ColorMatrices.ApplyMatrix(bitmap, colorMatrix);
    }

    /// <summary>
    /// 对图像进行逐像素处理
    /// </summary>
    /// <param name="bitmap">图像</param>
    /// <param name="func">处理回调函数</param>
    public static void PerPixelProcess(this Bitmap bitmap, Func<Color, Color> func)
    {
        var pixelFormat = bitmap.PixelFormat;

        if (pixelFormat != PixelFormat.Format32bppArgb && pixelFormat != PixelFormat.Format24bppRgb)
            throw new NotSupportedException($"Unsupported image pixel format {nameof(pixelFormat)} is used.");

        var cols = bitmap.Width;
        var rows = bitmap.Height;
        var channels = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
        var total = cols * rows * channels;

        // 锁定图片并拷贝图片像素
        var rect = new Rectangle(0, 0, cols, rows);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
        var iPtr = bitmapData.Scan0;
        var data = new byte[total];
        Marshal.Copy(iPtr, data, 0, total);

        // 逐像素处理
        Parallel.For(0, rows, row =>
        {
            for (var col = 0; col < cols; col++)
            {
                var indexOffset = (row * cols + col) * channels;
                if (!TryCreateColorFromData(data, indexOffset, channels, out var color))
                    continue;
                var targetColor = func?.Invoke(color);
                if (targetColor != null)
                    SaveToData(targetColor.Value, data, indexOffset, channels);
            }
        });

        Marshal.Copy(data, 0, iPtr, total);
        bitmap.UnlockBits(bitmapData);
    }

    /// <summary>
    /// 尝试获取颜色
    /// </summary>
    /// <param name="colorData">颜色数组</param>
    /// <param name="offset">偏移量</param>
    /// <param name="channels">图像通道数</param>
    /// <param name="color">颜色</param>
    private static bool TryCreateColorFromData(byte[] colorData, int offset, int channels, out Color color)
    {
        // 需要考虑大小端
        var isLittleEndian = BitConverter.IsLittleEndian;
        color = Color.Black;

        try
        {
            if (channels == 3)
            {
                var r = isLittleEndian ? colorData[offset + 2] : colorData[offset + 0];
                var g = colorData[offset + 1];
                var b = isLittleEndian ? colorData[offset + 0] : colorData[offset + 2];
                color = Color.FromArgb(byte.MaxValue, r, g, b);
                return true;
            }

            if (channels == 4)
            {
                var a = isLittleEndian ? colorData[offset + 3] : colorData[offset + 0];
                var r = isLittleEndian ? colorData[offset + 2] : colorData[offset + 1];
                var g = isLittleEndian ? colorData[offset + 1] : colorData[offset + 2];
                var b = isLittleEndian ? colorData[offset + 0] : colorData[offset + 3];
                color = Color.FromArgb(a, r, g, b);
                return true;
            }
        }
        catch (Exception)
        {
            return false;
        }
        return false;
    }

    /// <summary>
    /// 保存颜色到数组
    /// </summary>
    /// <param name="color">颜色</param>
    /// <param name="colorData">颜色数组</param>
    /// <param name="offset">偏移量</param>
    /// <param name="channels">图像通道数</param>
    private static void SaveToData(Color color, byte[] colorData, int offset, int channels)
    {
        // 需要考虑大小端
        var isLittleEndian = BitConverter.IsLittleEndian;

        // RGB
        if (channels == 3)
        {
            colorData[offset + 0] = isLittleEndian ? color.B : color.R;
            colorData[offset + 1] = color.G;
            colorData[offset + 2] = isLittleEndian ? color.R : color.B;
        }

        // ARGB
        if (channels == 4)
        {
            colorData[offset + 0] = isLittleEndian ? color.B : color.A;
            colorData[offset + 1] = isLittleEndian ? color.G : color.R;
            colorData[offset + 2] = isLittleEndian ? color.R : color.G;
            colorData[offset + 3] = isLittleEndian ? color.A : color.B;
        }
    }
}