using SkiaSharp;

namespace Bing.Drawing;

// // 图片操作辅助类 - 转换
public static partial class SkiaSharpHelper
{
    /// <summary>
    /// 规范化图片格式参数
    /// </summary>
    private static (SKEncodedImageFormat Format, int Quality) NormalizeImageFormat(SKImage image, (SKEncodedImageFormat Format, int Quality)? imageFormat)
    {
        var format = imageFormat ?? (GetEncodedImageFormat(image), 100);
        return (format.Format, ValidateQuality(format.Quality));
    }

    /// <summary>
    /// 编码图片数据
    /// </summary>
    private static SKData Encode(SKImage image, (SKEncodedImageFormat Format, int Quality)? imageFormat = default)
    {
        var format = NormalizeImageFormat(image, imageFormat);
        return image.Encode(format.Format, format.Quality);
    }

    /// <summary>
    /// 验证质量参数
    /// </summary>
    /// <param name="quality">质量</param>
    private static int ValidateQuality(int quality)
    {
        if (quality < 1 || quality > 100)
            throw new ArgumentOutOfRangeException(nameof(quality), "质量参数必须为1-100之间的整数");
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
        using var data = Encode(image, imageFormat);
        return data.ToArray();
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
        return Convert.ToBase64String(ToBytes(image, imageFormat));
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
        var format = NormalizeImageFormat(image, imageFormat);
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