using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Bing.Drawing;

/// <summary>
/// 图片操作辅助类 - 转换
/// </summary>
public static partial class ImageSharpHelper
{
    #region ToBytes(将图像转换为字节数组)

    /// <summary>
    /// 将图像转换为字节数组
    /// </summary>
    /// <param name="image">图像</param>
    public static byte[] ToBytes(Image image) => ToBytes(image, JpegFormat.Instance);

    /// <summary>
    /// 将图像转换为字节数组
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static byte[] ToBytes(Image image, IImageFormat? imageFormat)
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
    /// <param name="appendPrefix">是否追加前缀</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string ToBase64String(Image image, bool appendPrefix = false) => ToBase64String(image, JpegFormat.Instance, appendPrefix);

    /// <summary>
    /// 将图像转换为base64字符串
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式</param>
    /// <param name="appendPrefix">是否追加前缀</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string ToBase64String(Image image, IImageFormat? imageFormat = default, bool appendPrefix = false)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        imageFormat ??= JpegFormat.Instance;
        using var ms = new MemoryStream();
        image.Save(ms, imageFormat);
        var base64String = Convert.ToBase64String(ms.ToArray());
        if (appendPrefix)
            base64String = $"data:{imageFormat.DefaultMimeType};base64,{base64String}";
        return base64String;
    }

    #endregion
}