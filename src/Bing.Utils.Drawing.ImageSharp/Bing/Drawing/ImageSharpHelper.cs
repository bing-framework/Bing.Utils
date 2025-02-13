using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Bing.Drawing;

/// <summary>
/// 基于 SixLabors.ImageSharp 实现的图片操作辅助类
/// </summary>
public static partial class ImageSharpHelper
{
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
        return output;
    }

    #endregion
}