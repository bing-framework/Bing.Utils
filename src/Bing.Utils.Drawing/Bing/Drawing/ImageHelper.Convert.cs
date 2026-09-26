using System.Drawing;
using System.Drawing.Imaging;

namespace Bing.Drawing;

/// <summary>
/// 图片操作辅助类 - 转换
/// </summary>
public static partial class ImageHelper
{
    /// <summary>
    /// 获取可持久化的图片格式
    /// </summary>
    /// <param name="imageFormat">图片格式</param>
    /// <returns>可持久化的图像格式；未指定或为内存位图时返回 PNG。</returns>
    private static ImageFormat NormalizeImageFormat(ImageFormat imageFormat)
    {
        if (imageFormat == null || imageFormat.Guid == ImageFormat.MemoryBmp.Guid)
            return ImageFormat.Png;
        return imageFormat;
    }

    /// <summary>
    /// 获取图片 Mime 类型
    /// </summary>
    /// <param name="imageFormat">图片格式</param>
    /// <returns>图像格式对应的 MIME 类型。</returns>
    private static string GetMimeType(ImageFormat imageFormat)
    {
        var codec = GetCodecInfo(imageFormat);
        if (!string.IsNullOrWhiteSpace(codec?.MimeType))
            return codec.MimeType;

        return imageFormat.Guid == ImageFormat.Icon.Guid
            ? "image/x-icon"
            : $"image/{imageFormat.ToString().ToLowerInvariant()}";
    }

    #region ToBytes(将图像转换为字节数组)

    /// <summary>
    /// 将图像转换成字节数组。
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="format">图像格式；为空时使用 PNG。</param>
    /// <returns>编码后的图像数据。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static byte[] ToBytes(Image image, ImageFormat format = default)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));
        format = NormalizeImageFormat(format);
        using var ms = new MemoryStream();
        image.Save(ms, format);
        return ms.ToArray();
    }

    #endregion

    #region ToStream(转换为内存流)

    /// <summary>
    /// 将图像转换为内存流。
    /// </summary>
    /// <param name="image">图像。</param>
    /// <returns>定位到起始位置且由调用方释放的图像流。</returns>
    public static Stream ToStream(Image image)
    {
        return new MemoryStream(ToBytes(image), writable: false);
    }

    /// <summary>
    /// 将图像转换为内存流。
    /// </summary>
    /// <param name="bitmap">位图。</param>
    /// <returns>定位到起始位置且由调用方释放的图像流。</returns>
    public static Stream ToStream(Bitmap bitmap)
    {
        return new MemoryStream(ToBytes(bitmap), writable: false);
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
    public static string ToBase64String(Image image, ImageFormat imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        imageFormat = NormalizeImageFormat(imageFormat);
        using var ms = new MemoryStream();
        image.Save(ms, imageFormat);
        var result = Convert.ToBase64String(ms.ToArray());
        return result;
    }

    /// <summary>
    /// 将图像转换为 Base64 字符串。
    /// </summary>
    /// <param name="bitmap">位图。</param>
    /// <param name="imageFormat">图像格式；为空时使用 PNG。</param>
    /// <returns>图像的 Base64 编码字符串。</returns>
    /// <exception cref="ArgumentNullException">位图为空。</exception>
    public static string ToBase64String(Bitmap bitmap, ImageFormat imageFormat = default)
    {
        if (bitmap is null)
            throw new ArgumentNullException(nameof(bitmap));
        imageFormat = NormalizeImageFormat(imageFormat);
        using var ms = new MemoryStream();
        bitmap.Save(ms, imageFormat);
        return Convert.ToBase64String(ms.ToArray());
    }

    #endregion

    #region ToDataUrl(转换为DataUrl)

    /// <summary>
    /// 将图像转换为 Data URL。
    /// </summary>
    /// <param name="bitmap">位图。</param>
    /// <param name="imageFormat">图像格式；为空时使用 PNG。</param>
    /// <returns>包含图像 MIME 类型和 Base64 数据的 Data URL。</returns>
    /// <exception cref="ArgumentNullException">位图为空。</exception>
    public static string ToDataUrl(Bitmap bitmap, ImageFormat imageFormat = default)
    {
        if (bitmap is null)
            throw new ArgumentNullException(nameof(bitmap));
        imageFormat = NormalizeImageFormat(imageFormat);
        using var ms = new MemoryStream();
        bitmap.Save(ms, imageFormat);
        var result = Convert.ToBase64String(ms.ToArray());
        return $"data:{GetMimeType(imageFormat)};base64,{result}";
    }

    /// <summary>
    /// 将图像转换为 Data URL。
    /// </summary>
    /// <param name="image">图像。</param>
    /// <param name="imageFormat">图像格式；为空时使用 PNG。</param>
    /// <returns>包含图像 MIME 类型和 Base64 数据的 Data URL。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    public static string ToDataUrl(Image image, ImageFormat imageFormat = default)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        imageFormat = NormalizeImageFormat(imageFormat);
        using var ms = new MemoryStream();
        image.Save(ms, imageFormat);
        var result = Convert.ToBase64String(ms.ToArray());
        return $"data:{GetMimeType(imageFormat)};base64,{result}";
    }

    #endregion

    #region ToIcoStream(将图像转换为ICO流)

    /// <summary>
    /// 将图像转换为ICO流
    /// </summary>
    /// <param name="image">调用方持有的图像，本方法不修改或释放该图像。</param>
    /// <param name="size">目标宽高，均须在 1 到 256 之间。</param>
    /// <returns>定位到起始处的 ICO 流，由调用方释放。</returns>
    /// <exception cref="ArgumentNullException">图像为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">目标宽高不在支持范围内。</exception>
    public static MemoryStream ToIcoStream(Image image, Size size)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (size.Width < 1 || size.Width > 256)
            throw new ArgumentOutOfRangeException(nameof(size), size.Width, "ICO 宽度必须在 1 到 256 之间。");
        if (size.Height < 1 || size.Height > 256)
            throw new ArgumentOutOfRangeException(nameof(size), size.Height, "ICO 高度必须在 1 到 256 之间。");

        using var bmp = new Bitmap(image, size);
        byte[] png;
        using (var ms = new MemoryStream())
        {
            bmp.Save(ms, ImageFormat.Png);
            png = ms.ToArray();
        }

        // ICO 目录项固定为 16 字节，宽高 256 使用 0 表示；长度和偏移均为完整的 32 位小端值。
        var header = new byte[]
        {
            0, 0, 1, 0, 1, 0,
            size.Width == 256 ? (byte)0 : (byte)size.Width,
            size.Height == 256 ? (byte)0 : (byte)size.Height,
            0, 0, 1, 0, 32, 0,
            0, 0, 0, 0,
            0, 0, 0, 0
        };
        WriteUInt32LittleEndian(header, 14, checked((uint)png.Length));
        WriteUInt32LittleEndian(header, 18, checked((uint)header.Length));

        var outMs = new MemoryStream();
        try
        {
            outMs.Write(header, 0, header.Length);
            outMs.Write(png, 0, png.Length);
            outMs.Position = 0;
            return outMs;
        }
        catch
        {
            outMs.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 将 32 位整数按 ICO 使用的小端顺序写入缓冲区。
    /// </summary>
    /// <param name="buffer">目标缓冲区。</param>
    /// <param name="offset">写入起始偏移量。</param>
    /// <param name="value">待写入的整数。</param>
    private static void WriteUInt32LittleEndian(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }

    #endregion
}
