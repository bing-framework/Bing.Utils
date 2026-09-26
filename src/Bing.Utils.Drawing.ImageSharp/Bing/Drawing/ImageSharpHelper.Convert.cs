using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;

namespace Bing.Drawing;

/// <summary>
/// 提供 ImageSharp 图像编码和格式转换。
/// </summary>
public static partial class ImageSharpHelper
{
    /// <summary>
    /// 获取默认图片格式
    /// </summary>
    /// <param name="imageFormat">图片格式</param>
    /// <returns>规范化后的图像格式；未指定时返回 PNG。</returns>
    private static IImageFormat NormalizeImageFormat(IImageFormat? imageFormat) => imageFormat ?? PngFormat.Instance;

    /// <summary>
    /// 验证质量参数
    /// </summary>
    /// <param name="quality">质量</param>
    /// <returns>验证后的质量值。</returns>
    /// <exception cref="ArgumentOutOfRangeException">质量不在 1 到 100 之间。</exception>
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
    /// 将图像转换为字节数组。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式；为空时使用 PNG。</param>
    /// <returns>编码后的图像数据。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static byte[] ToBytes(Image image, IImageFormat? imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        imageFormat = NormalizeImageFormat(imageFormat ?? PngFormat.Instance);
        using var ms = new MemoryStream();
        Save(image, ms, imageFormat);
        return ms.ToArray();
    }

    /// <summary>
    /// 将图像转换为字节数组。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式。</param>
    /// <param name="quality">编码质量；取值范围为 1 到 100。</param>
    /// <returns>编码后的图像数据。</returns>
    /// <exception cref="ArgumentNullException">图像或图像格式为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">质量不在 1 到 100 之间。</exception>
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
    /// 将图像转换为 Base64 字符串。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式；为空时使用 PNG。</param>
    /// <returns>图像的 Base64 编码字符串。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static string ToBase64String(Image image, IImageFormat? imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        // 统一入口默认 PNG；需要保留来源格式时必须显式传入格式。
        imageFormat = NormalizeImageFormat(imageFormat ?? PngFormat.Instance);
        using var ms = new MemoryStream();
        Save(image, ms, imageFormat);
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// 将图像转换为 Base64 字符串。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式。</param>
    /// <param name="quality">编码质量；取值范围为 1 到 100。</param>
    /// <returns>图像的 Base64 编码字符串。</returns>
    /// <exception cref="ArgumentNullException">图像或图像格式为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">质量不在 1 到 100 之间。</exception>
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
    /// 将图像转换为 Data URL。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式；为空时使用 PNG。</param>
    /// <returns>包含图像 MIME 类型和 Base64 数据的 Data URL。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static string ToDataUrl(Image image, IImageFormat? imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        // Data URL 的媒体类型必须与实际编码一致，默认始终为 PNG。
        imageFormat = NormalizeImageFormat(imageFormat ?? PngFormat.Instance);
        return $"data:{imageFormat.DefaultMimeType};base64,{ToBase64String(image, imageFormat)}";
    }

    /// <summary>
    /// 将图像转换为 Data URL。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像格式。</param>
    /// <param name="quality">编码质量；取值范围为 1 到 100。</param>
    /// <returns>包含图像 MIME 类型和 Base64 数据的 Data URL。</returns>
    /// <exception cref="ArgumentNullException">图像或图像格式为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">质量不在 1 到 100 之间。</exception>
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
