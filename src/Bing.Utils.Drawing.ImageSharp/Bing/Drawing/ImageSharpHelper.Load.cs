using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

// 图片操作辅助类 - 加载
public static partial class ImageSharpHelper
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
    public static Image? FromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return default;
        try
        {
            return TrackFormat(Image.Load(filePath), Image.DetectFormat(filePath));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 从指定文件创建图片
    /// </summary>
    /// <typeparam name="TPixel">像素类型</typeparam>
    /// <param name="filePath">文件的绝对路径</param>
    public static Image<TPixel>? FromFile<TPixel>(string filePath)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return default;
        try
        {
            return TrackFormat(Image.Load<TPixel>(filePath), Image.DetectFormat(filePath));
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
    public static Image? FromStream(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        try
        {
            var (bytes, format) = ReadBytesAndDetectFormat(stream);
            return TrackFormat(Image.Load(bytes), format);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 从指定流创建图片
    /// </summary>
    /// <typeparam name="TPixel">像素类型</typeparam>
    /// <param name="stream">流</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Image<TPixel>? FromStream<TPixel>(Stream stream)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        try
        {
            var (bytes, format) = ReadBytesAndDetectFormat(stream);
            return TrackFormat(Image.Load<TPixel>(bytes), format);
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
    public static Image? FromBytes(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        try
        {
            return TrackFormat(Image.Load(bytes), Image.DetectFormat(bytes));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 从指定字节数组创建图片
    /// </summary>
    /// <typeparam name="TPixel">像素类型</typeparam>
    /// <param name="bytes">字节数组</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Image<TPixel>? FromBytes<TPixel>(byte[] bytes)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        try
        {
            return TrackFormat(Image.Load<TPixel>(bytes), Image.DetectFormat(bytes));
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
    public static Image? FromBase64String(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            return default;
        try
        {
            var bytes = Convert.FromBase64String(base64String);
            return TrackFormat(Image.Load(bytes), Image.DetectFormat(bytes));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 从指定Base64字符串创建图片
    /// </summary>
    /// <typeparam name="TPixel">像素类型</typeparam>
    /// <param name="base64String">Base64字符串</param>
    public static Image<TPixel>? FromBase64String<TPixel>(string base64String)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        if (string.IsNullOrWhiteSpace(base64String))
            return default;
        try
        {
            var bytes = Convert.FromBase64String(base64String);
            return TrackFormat(Image.Load<TPixel>(bytes), Image.DetectFormat(bytes));
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
    public static Image? FromDataUrl(string dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
            return default;
        var match = ImageDataUrl.Match(dataUrl);
        if (!match.Success)
            return default;
        return FromBase64String(match.Groups["DATA"].Value);
    }

    /// <summary>
    /// 从指定DataUrl字符串创建图片。<br />
    /// 格式：data:image/png;base64,base64String
    /// </summary>
    /// <typeparam name="TPixel">像素类型</typeparam>
    /// <param name="dataUrl">DataUrl字符串</param>
    public static Image<TPixel>? FromDataUrl<TPixel>(string dataUrl)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
            return default;
        var match = ImageDataUrl.Match(dataUrl);
        if (!match.Success)
            return default;
        return FromBase64String<TPixel>(match.Groups["DATA"].Value);
    }

    #endregion

    /// <summary>
    /// 读取流内容并检测图片格式
    /// </summary>
    private static (byte[] Bytes, IImageFormat? Format) ReadBytesAndDetectFormat(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        return (bytes, Image.DetectFormat(bytes));
    }
}