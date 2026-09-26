using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 提供 SkiaSharp 图像编码和格式转换。
/// </summary>
public static partial class SkiaSharpHelper
{
    /// <summary>
    /// 规范化图片格式参数
    /// </summary>
    /// <param name="image">待编码的图像。</param>
    /// <param name="imageFormat">编码格式和质量；为空时使用 PNG 与质量 100。</param>
    /// <returns>规范化后的编码格式和质量。</returns>
    private static (SKEncodedImageFormat Format, int Quality) NormalizeImageFormat(SKImage image, (SKEncodedImageFormat Format, int Quality)? imageFormat)
    {
        var format = imageFormat ?? (SKEncodedImageFormat.Png, 100);
        return (format.Format, ValidateQuality(format.Quality));
    }

    /// <summary>
    /// 编码图片数据
    /// </summary>
    /// <param name="image">待编码的图像。</param>
    /// <param name="imageFormat">编码格式和质量；为空时使用 PNG 与质量 100。</param>
    /// <returns>编码后的图像数据。</returns>
    private static SKData Encode(SKImage image, (SKEncodedImageFormat Format, int Quality)? imageFormat = default)
    {
        var format = NormalizeImageFormat(image, imageFormat);
        return image.Encode(format.Format, format.Quality);
    }

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

    #region ToBytes(将图像转换为字节数组)

    /// <summary>
    /// 将图像转换为字节数组。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">编码格式和质量；为空时使用 PNG 与质量 100。</param>
    /// <returns>编码后的图像数据。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static byte[] ToBytes(SKImage image, (SKEncodedImageFormat Format, int Quality)? imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        // ToBytes 的无参结果必须稳定为 PNG，避免输入来源格式影响调用方的文件协议。
        using var data = Encode(image, imageFormat ?? (SKEncodedImageFormat.Png, 100));
        return data.ToArray();
    }

    /// <summary>
    /// 将图像转换为字节数组。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像编码格式。</param>
    /// <param name="quality">编码质量；取值范围为 1 到 100。</param>
    /// <returns>编码后的图像数据。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">质量不在 1 到 100 之间。</exception>
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
    /// 将图像转换为 Base64 字符串。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">编码格式和质量；为空时使用 PNG 与质量 100。</param>
    /// <returns>图像的 Base64 编码字符串。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static string ToBase64String(SKImage image, (SKEncodedImageFormat Format, int Quality)? imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        var format = imageFormat ?? (SKEncodedImageFormat.Png, 100);
        return Convert.ToBase64String(ToBytes(image, format));
    }

    /// <summary>
    /// 将图像转换为 Base64 字符串。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像编码格式。</param>
    /// <param name="quality">编码质量；取值范围为 1 到 100。</param>
    /// <returns>图像的 Base64 编码字符串。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">质量不在 1 到 100 之间。</exception>
    public static string ToBase64String(SKImage image, SKEncodedImageFormat imageFormat, int quality = 100)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return Convert.ToBase64String(ToBytes(image, imageFormat, quality));
    }

    #endregion

    #region ToDataUrl(转换为DataUrl)

    /// <summary>
    /// 将图像转换为 Data URL。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">编码格式和质量；为空时使用 PNG 与质量 100。</param>
    /// <returns>包含图像 MIME 类型和 Base64 数据的 Data URL。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static string ToDataUrl(SKImage image, (SKEncodedImageFormat Format, int Quality)? imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        var format = NormalizeImageFormat(image, imageFormat);
        return $"data:{format.Format.GetMimeType()};base64,{ToBase64String(image, format)}";
    }

    /// <summary>
    /// 将图像转换为 Data URL。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="imageFormat">图像编码格式。</param>
    /// <param name="quality">编码质量；取值范围为 1 到 100。</param>
    /// <returns>包含图像 MIME 类型和 Base64 数据的 Data URL。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">质量不在 1 到 100 之间。</exception>
    public static string ToDataUrl(SKImage image, SKEncodedImageFormat imageFormat, int quality = 100)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        return $"data:{imageFormat.GetMimeType()};base64,{ToBase64String(image, imageFormat, quality)}";
    }

    #endregion
}
