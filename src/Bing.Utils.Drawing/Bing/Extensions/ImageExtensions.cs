using System.Drawing;
using System.Drawing.Imaging;
using Bing.Drawing;

namespace Bing.Extensions;

/// <summary>
/// 图像(<see cref="Image"/>) 扩展
/// </summary>
public static class ImageExtensions
{
    /// <summary>
    /// 将图像转换为base64字符串
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="format">图像格式</param>
    public static string ToBase64String(this Image image, ImageFormat format) => ImageHelper.ToBase64String(image, format);

    /// <summary>
    /// 将图像转换为base64字符串，带前缀
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="format">图像格式</param>
    /// <returns>
    /// data:image/jpeg;base64,字符串
    /// </returns>
    public static string ToBase64StringWithPrefix(this Image image, ImageFormat format) => ImageHelper.ToDataUrl(image, format);

    /// <summary>
    /// 缩放图像，以使其适合宽度/高度
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public static Image ScaleImage(this Image image, int width, int height) => ImageHelper.ScaleImage(image, width, height);

    /// <summary>
    /// 将图像转换成字节数组
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="format">图像格式</param>
    public static byte[] ToBytes(this Image image, ImageFormat format) => ImageHelper.ToBytes(image, format);
}