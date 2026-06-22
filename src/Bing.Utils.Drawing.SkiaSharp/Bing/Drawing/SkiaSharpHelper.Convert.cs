using SkiaSharp;

namespace Bing.Drawing;

// // 图片操作辅助类 - 转换
public static partial class SkiaSharpHelper
{
    /// <summary>
    /// 验证质量参数
    /// </summary>
    /// <param name="quality">质量</param>
    private static int ValidateQuality(int quality)
    {
        if (quality < 0 || quality > 100)
            throw new ArgumentOutOfRangeException(nameof(quality), "质量参数必须为0-100之间的整数");
        return quality;
    }

    #region ToBytes(将图像转换为字节数组)

    /// <summary>
    /// 将图像转换为字节数组
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static byte[] ToBytes(SKImage image, (SKEncodedImageFormat Format, int Quality)? imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        var format = imageFormat ?? (SKEncodedImageFormat.Png, 100);
        return image.Encode(format.Format, format.Quality).ToArray();
    }

    /// <summary>
    /// 将图像转换为字节数组
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式</param>
    /// <param name="quality">编码质量</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static byte[] ToBytes(SKImage image, SKEncodedImageFormat imageFormat, int quality = 100)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        using var data = image.Encode(imageFormat, ValidateQuality(quality));
        return data.ToArray();
    }

    #endregion

    #region ToBase64String(转换为Base64字符串)

    /// <summary>
    /// 将图像转换为base64字符串
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图片格式</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string ToBase64String(SKImage image, (SKEncodedImageFormat Format, int Quality)? imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        var format = imageFormat ?? (SKEncodedImageFormat.Png, 100);
        var bytes = image.Encode(format.Format, format.Quality).ToArray();
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 将图像转换为base64字符串
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图片格式</param>
    /// <param name="quality">编码质量</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string ToBase64String(SKImage image, SKEncodedImageFormat imageFormat, int quality = 100)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return Convert.ToBase64String(ToBytes(image, imageFormat, quality));
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
    public static string ToDataUrl(SKImage image, (SKEncodedImageFormat Format, int Quality)? imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        var format = imageFormat ?? (SKEncodedImageFormat.Png, 100);
        return $"data:{format.Format.GetMimeType()};base64,{ToBase64String(image, format)}";
    }

    /// <summary>
    /// 将图像转换为 DataUrl
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图片格式</param>
    /// <param name="quality">编码质量</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string ToDataUrl(SKImage image, SKEncodedImageFormat imageFormat, int quality = 100)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return $"data:{imageFormat.GetMimeType()};base64,{ToBase64String(image, imageFormat, quality)}";
    }

    #endregion
}