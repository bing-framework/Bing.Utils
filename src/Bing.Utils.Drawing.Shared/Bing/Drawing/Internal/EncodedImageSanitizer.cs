namespace Bing.Drawing.Internal;

/// <summary>
/// 调度编码图像格式检测和元数据清理。
/// </summary>
internal static class EncodedImageSanitizer
{
    /// <summary>
    /// 按选项清理编码图像的元数据。
    /// </summary>
    /// <param name="data">编码图像数据。</param>
    /// <param name="options">元数据清理选项。</param>
    /// <returns>清理后的图像数据；无需清理时返回原数组。</returns>
    internal static byte[] Sanitize(byte[] data, ImageMetadataOptions options)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));
        if (options is null)
            throw new ArgumentNullException(nameof(options));

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
    /// 根据文件签名检测编码图像格式。
    /// </summary>
    /// <param name="data">编码图像数据。</param>
    /// <returns>检测到的图像格式。</returns>
    internal static ImageFormat DetectFormat(byte[] data)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8)
            return ImageFormat.Jpeg;

        if (data.Length >= 8 &&
            data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return ImageFormat.Png;

        return ImageFormat.Unknown;
    }

    /// <summary>
    /// 表示解析器支持识别的图像格式。
    /// </summary>
    internal enum ImageFormat
    {
        /// <summary>
        /// 未识别的格式。
        /// </summary>
        Unknown,

        /// <summary>
        /// JPEG 格式。
        /// </summary>
        Jpeg,

        /// <summary>
        /// PNG 格式。
        /// </summary>
        Png
    }
}
