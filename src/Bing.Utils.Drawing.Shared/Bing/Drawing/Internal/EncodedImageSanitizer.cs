namespace Bing.Drawing.Internal;

/// <summary>
/// 编码图像格式检测与元数据清理调度
/// </summary>
internal static class EncodedImageSanitizer
{
    /// <summary>
    /// 对编码图像数据执行元数据清理
    /// </summary>
    /// <param name="data">编码图像数据</param>
    /// <param name="options">清理选项</param>
    /// <returns>清理后的数据</returns>
    internal static byte[] Sanitize(byte[] data, ImageMetadataOptions options)
    {
        var format = DetectFormat(data);
        switch (format)
        {
            case ImageFormat.Jpeg:
                return JpegMetadataSanitizer.Sanitize(data, options);
            case ImageFormat.Png:
                return PngMetadataSanitizer.Sanitize(data, options);
            default:
                if (options.UnsupportedFormatBehavior == UnsupportedFormatBehavior.PassThrough)
                    return data;
                throw new NotSupportedException($"不支持的图像格式：{format}。支持 JPEG 和 PNG。");
        }
    }

    /// <summary>
    /// 检测编码图像格式
    /// </summary>
    internal static ImageFormat DetectFormat(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return ImageFormat.Jpeg;

        if (data.Length >= 8 &&
            data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return ImageFormat.Png;

        return ImageFormat.Unknown;
    }

    /// <summary>
    /// 已知图像格式枚举
    /// </summary>
    internal enum ImageFormat
    {
        Unknown,
        Jpeg,
        Png
    }
}
