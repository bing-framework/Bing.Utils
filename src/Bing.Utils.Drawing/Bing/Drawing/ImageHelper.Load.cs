using System.Drawing;
using System.Text.RegularExpressions;

namespace Bing.Drawing;

/// <summary>
/// 图片操作辅助类 - 加载
/// </summary>
public static partial class ImageHelper
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
    public static Image FromFile(string filePath) => Image.FromFile(filePath);

    #endregion

    #region FromStream(从指定流创建图片)

    /// <summary>
    /// 从指定流创建图片
    /// </summary>
    /// <param name="stream">流</param>
    public static Image FromStream(Stream stream) => Image.FromStream(stream);

    #endregion

    #region FromBytes(从指定字节数组创建图片)

    /// <summary>
    /// 从指定字节数组创建图片
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Image FromBytes(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        using var ms = new MemoryStream(bytes);
        return Image.FromStream(ms);
    }

    #endregion

    #region FromBase64String(从指定Base64字符串创建图片)

    /// <summary>
    /// 从指定Base64字符串创建图片
    /// </summary>
    /// <param name="base64String">Base64字符串</param>
    public static Image FromBase64String(string base64String)
    {
        var bytes = Convert.FromBase64String(base64String);
        using var ms = new MemoryStream(bytes);
        return Image.FromStream(ms);
    }

    #endregion

    #region FromDataUrl(从指定DataUrl字符串创建图片)

    /// <summary>
    /// 从指定DataUrl字符串创建图片。<br />
    /// 格式：data:image/png;base64,base64String
    /// </summary>
    /// <param name="dataUrl">DataUrl字符串</param>
    public static Image FromDataUrl(string dataUrl)
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