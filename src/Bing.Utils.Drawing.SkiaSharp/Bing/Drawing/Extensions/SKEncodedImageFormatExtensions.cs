using SkiaSharp;

// ReSharper disable once CheckNamespace
namespace Bing.Drawing;

/// <summary>
/// 图片格式(<see cref="SKEncodedImageFormat"/>) 扩展
/// </summary>
// ReSharper disable once InconsistentNaming
public static class SKEncodedImageFormatExtensions
{
    /// <summary>
    /// 获取Mime类型
    /// </summary>
    /// <param name="format">图片格式</param>
    public static string GetMimeType(this SKEncodedImageFormat format)
    {
        return format switch
        {
            SKEncodedImageFormat.Jpeg => "image/jpeg",
            SKEncodedImageFormat.Png => "image/png",
            SKEncodedImageFormat.Webp => "image/webp",
            SKEncodedImageFormat.Wbmp => "image/vnd.wap.wbmp",
            SKEncodedImageFormat.Bmp => "image/bmp",
            SKEncodedImageFormat.Gif => "image/gif",
            SKEncodedImageFormat.Ico => "image/icon",
            SKEncodedImageFormat.Avif => "image/avif",
            _ => "application/octet-stream"
        };
    }
}