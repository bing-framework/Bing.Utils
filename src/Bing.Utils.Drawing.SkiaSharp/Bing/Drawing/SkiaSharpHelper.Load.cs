using System.Text.RegularExpressions;
using SkiaSharp;

namespace Bing.Drawing;

// // 图片操作辅助类 - 加载
public static partial class SkiaSharpHelper
{
    /// <summary>
    /// 图片DataUrl正则表达式
    /// </summary>
    internal static readonly Regex ImageDataUrl = new(@"^data\:(?<MIME>image\/[a-z0-9.+-]+)\;base64\,(?<DATA>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            return TrackFormat(SKImage.FromEncodedData(bytes), DetectEncodedImageFormat(bytes));
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
            var bytes = ReadBytes(stream);
            return TrackFormat(SKImage.FromEncodedData(bytes), DetectEncodedImageFormat(bytes));
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
            return TrackFormat(SKImage.FromEncodedData(bytes), DetectEncodedImageFormat(bytes));
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
            var bytes = Convert.FromBase64String(base64String);
            return TrackFormat(SKImage.FromEncodedData(bytes), DetectEncodedImageFormat(bytes));
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

    /// <summary>
    /// 读取流字节内容
    /// </summary>
    private static byte[] ReadBytes(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// 检测图片编码格式
    /// </summary>
    private static SKEncodedImageFormat DetectEncodedImageFormat(byte[] bytes)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        return codec?.EncodedFormat ?? SKEncodedImageFormat.Png;
    }
}