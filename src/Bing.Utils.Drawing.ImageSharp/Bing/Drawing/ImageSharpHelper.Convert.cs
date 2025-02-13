using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Bing.Drawing;

// 图片操作辅助类 - 转换
public static partial class ImageSharpHelper
{
    #region ToBytes(将图像转换为字节数组)

    /// <summary>
    /// 将图像转换为字节数组
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static byte[] ToBytes(Image image, IImageFormat? imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        imageFormat ??= JpegFormat.Instance;
        using var ms = new MemoryStream();
        image.Save(ms, imageFormat);
        return ms.ToArray();
    }

    #endregion

    #region ToBase64String(转换为Base64字符串)

    /// <summary>
    /// 将图像转换为base64字符串
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string ToBase64String(Image image, IImageFormat? imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        imageFormat ??= JpegFormat.Instance;
        using var ms = new MemoryStream();
        image.Save(ms, imageFormat);
        return Convert.ToBase64String(ms.ToArray());
    }

    #endregion

    #region ToDataUrl(转换为DataUrl)

    /// <summary>
    /// 将图像转换为转换为DataUrl。<br />
    /// 格式：data:image/png;base64,base64String
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图片格式</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string ToDataUrl(Image image, IImageFormat? imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        imageFormat ??= JpegFormat.Instance;
        return $"data:{imageFormat.DefaultMimeType};base64,{ToBase64String(image, imageFormat)}";
    }

    #endregion
}