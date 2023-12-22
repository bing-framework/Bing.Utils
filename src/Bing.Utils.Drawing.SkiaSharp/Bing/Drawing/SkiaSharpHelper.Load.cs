using System.Text.RegularExpressions;
using SkiaSharp;

namespace Bing.Drawing;

// // 图片操作辅助类 - 加载
public static partial class SkiaSharpHelper
{
    /// <summary>
    /// 图片DataUrl正则表达式
    /// </summary>
    internal static readonly Regex ImageDataUrl = new(@"^data\:(?<MIME>image\/(bmp|emf|exif|gif|icon|jpeg|png|tiff|wmf))\;base64\,(?<DATA>.+)");

    #region FromFile(从指定文件创建图片)

    /// <summary>
    /// 从指定文件创建图片
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    public static SKImage? FromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return default;
        try
        {
            var bytes = File.ReadAllBytes(filePath);
            return SKImage.FromEncodedData(bytes);
        }
        catch
        {
            return default;
        }
    }

    #endregion

    #region FromStream(从指定流创建图片)

    /// <summary>
    /// 从指定流创建图片
    /// </summary>
    /// <param name="stream">流</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage? FromStream(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        try
        {
            return SKImage.FromEncodedData(stream);
        }
        catch
        {
            return default;
        }
    }

    #endregion

    #region FromBytes(从指定字节数组创建图片)

    /// <summary>
    /// 从指定字节数组创建图片
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage? FromBytes(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        try
        {
            return SKImage.FromEncodedData(bytes);
        }
        catch
        {
            return default;
        }
    }

    #endregion

    #region FromBase64String(从指定Base64字符串创建图片)

    /// <summary>
    /// 从指定Base64字符串创建图片
    /// </summary>
    /// <param name="base64String">Base64字符串</param>
    public static SKImage? FromBase64String(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            return default;
        try
        {
            return SKImage.FromEncodedData(Convert.FromBase64String(base64String));
        }
        catch
        {
            return default;
        }
    }

    #endregion

    #region FromDataUrl(从指定DataUrl字符串创建图片)

    /// <summary>
    /// 从指定DataUrl字符串创建图片。<br />
    /// 格式：data:image/png;base64,base64String
    /// </summary>
    /// <param name="dataUrl">DataUrl字符串</param>
    public static SKImage? FromDataUrl(string dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
            return default;
        var match = ImageDataUrl.Match(dataUrl);
        if (!match.Success)
            return default;
        return FromBase64String(match.Groups["DATA"].Value);
    }

    #endregion
}