using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 图片操作辅助类 - 加载
/// </summary>
public static partial class ImageSharpHelper
{
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
            return Image.Load(filePath);
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
            return Image.Load<TPixel>(filePath);
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
            return Image.Load(stream);
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
            return Image.Load<TPixel>(stream);
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
            return Image.Load(bytes);
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
            return Image.Load<TPixel>(bytes);
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
            return Image.Load(Convert.FromBase64String(GetBase64String(base64String)));
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
            return Image.Load<TPixel>(Convert.FromBase64String(GetBase64String(base64String)));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 获取真正的图片Base64数据。
    /// 即去掉data:image/jpg;base64,这样的格式
    /// </summary>
    /// <param name="base64String">带前缀的Base64图片字符串</param>
    private static string GetBase64String(string base64String)
    {
        var pattern = "^(data:image/.*?;base64,).*?$";
        var match = Regex.Match(base64String, pattern);
        return base64String.Replace(match.Groups[1].ToString(), "");
    }

    #endregion
}