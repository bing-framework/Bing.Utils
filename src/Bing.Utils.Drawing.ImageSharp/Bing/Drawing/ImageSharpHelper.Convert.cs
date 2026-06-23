using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;

namespace Bing.Drawing;

// 图片操作辅助类 - 转换
public static partial class ImageSharpHelper
{
    /// <summary>
    /// 获取默认图片格式
    /// </summary>
    /// <param name="imageFormat">图片格式</param>
    private static IImageFormat NormalizeImageFormat(IImageFormat? imageFormat) => imageFormat ?? PngFormat.Instance;

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

    /// <summary>
    /// 保存图像到流
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="stream">流</param>
    /// <param name="imageFormat">图片格式</param>
    /// <param name="quality">质量</param>
    private static void Save(Image image, Stream stream, IImageFormat imageFormat, int? quality = default)
    {
        if (quality.HasValue)
            ValidateQuality(quality.Value);

        if (quality.HasValue && imageFormat.Name.Equals(JpegFormat.Instance.Name, StringComparison.OrdinalIgnoreCase))
        {
            image.Save(stream, new JpegEncoder { Quality = quality.Value });
            return;
        }

        if (imageFormat.Name.Equals(PngFormat.Instance.Name, StringComparison.OrdinalIgnoreCase))
        {
            image.Save(stream, new PngEncoder());
            return;
        }

        if (imageFormat.Name.Equals(GifFormat.Instance.Name, StringComparison.OrdinalIgnoreCase))
        {
            image.Save(stream, new GifEncoder());
            return;
        }

        if (imageFormat.Name.Equals(BmpFormat.Instance.Name, StringComparison.OrdinalIgnoreCase))
        {
            image.Save(stream, new BmpEncoder());
            return;
        }

        if (imageFormat.Name.Equals(TiffFormat.Instance.Name, StringComparison.OrdinalIgnoreCase))
        {
            image.Save(stream, new TiffEncoder());
            return;
        }

        image.Save(stream, imageFormat);
    }

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
        imageFormat = NormalizeImageFormat(imageFormat ?? GetTrackedFormat(image));
        using var ms = new MemoryStream();
        Save(image, ms, imageFormat);
        return ms.ToArray();
    }

    /// <summary>
    /// 将图像转换为字节数组
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式</param>
    /// <param name="quality">编码质量。当前对 JPEG 生效</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static byte[] ToBytes(Image image, IImageFormat imageFormat, int quality)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (imageFormat is null)
            throw new ArgumentNullException(nameof(imageFormat));
        using var ms = new MemoryStream();
        Save(image, ms, imageFormat, quality);
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
        imageFormat = NormalizeImageFormat(imageFormat ?? GetTrackedFormat(image));
        using var ms = new MemoryStream();
        Save(image, ms, imageFormat);
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// 将图像转换为base64字符串
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式</param>
    /// <param name="quality">编码质量。当前对 JPEG 生效</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string ToBase64String(Image image, IImageFormat imageFormat, int quality)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (imageFormat is null)
            throw new ArgumentNullException(nameof(imageFormat));
        using var ms = new MemoryStream();
        Save(image, ms, imageFormat, quality);
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
        imageFormat = NormalizeImageFormat(imageFormat ?? GetTrackedFormat(image));
        return $"data:{imageFormat.DefaultMimeType};base64,{ToBase64String(image, imageFormat)}";
    }

    /// <summary>
    /// 将图像转换为 DataUrl
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图片格式</param>
    /// <param name="quality">编码质量。当前对 JPEG 生效</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string ToDataUrl(Image image, IImageFormat imageFormat, int quality)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (imageFormat is null)
            throw new ArgumentNullException(nameof(imageFormat));
        return $"data:{imageFormat.DefaultMimeType};base64,{ToBase64String(image, imageFormat, quality)}";
    }

    #endregion
}