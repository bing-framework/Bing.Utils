using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 基于 SkiaSharp 实现的图片操作辅助类
/// </summary>
public static partial class SkiaSharpHelper
{
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
}