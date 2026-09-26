using System.Drawing;
using System.Text.RegularExpressions;

namespace Bing.Drawing;

/// <summary>
/// 图片操作辅助类 - 加载
/// </summary>
public static partial class ImageHelper
{
    /// <summary>
    /// 匹配图像 Data URL 的正则表达式。
    /// </summary>
    internal static readonly Regex ImageDataUrl = new(@"^data\:(?<MIME>image\/[a-z0-9.+-]+)\;base64\,(?<DATA>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// 尝试从字节数组加载图像。
    /// </summary>
    /// <param name="bytes">编码图像数据。</param>
    /// <param name="image">成功加载的图像；失败时为 <c>null</c>。</param>
    /// <returns>加载成功返回 <see langword="true" />，否则返回 <see langword="false" />。</returns>
    /// <exception cref="ArgumentNullException">字节数组为空。</exception>
    public static bool TryLoad(byte[] bytes, out Image image)
    {
        try { image = FromBytes(bytes); return true; }
        catch (InvalidDataException) { image = null; return false; }
        catch (ArgumentException) { image = null; return false; }
        catch (System.Runtime.InteropServices.ExternalException) { image = null; return false; }
        catch (OutOfMemoryException) { image = null; return false; }
    }

    #region FromFile(从指定文件创建图片)

    /// <summary>
    /// 从文件加载图像。
    /// </summary>
    /// <param name="filePath">图像文件路径。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentNullException">文件路径为空。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static Image FromFile(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return FromStream(stream);
    }

    #endregion

    #region FromStream(从指定流创建图片)

    /// <summary>
    /// 从流加载图像。
    /// </summary>
    /// <param name="stream">包含图像数据的输入流；从当前位置读取。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentNullException">输入流为空。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    /// <remarks>
    /// 方法不会关闭输入流。
    /// </remarks>
    public static Image FromStream(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return FromBytes(buffer.ToArray());
    }

    #endregion

    #region FromBytes(从指定字节数组创建图片)

    /// <summary>
    /// 从字节数组加载图像。
    /// </summary>
    /// <param name="bytes">编码图像数据。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentNullException">字节数组为空。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static Image FromBytes(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        try
        {
            using var ms = new MemoryStream(bytes);
            using var image = Image.FromStream(ms);
            return (Image)image.Clone();
        }
        catch (Exception ex) when (ex is ArgumentException || ex is System.Runtime.InteropServices.ExternalException || ex is OutOfMemoryException)
        {
            // GDI+ 对不受支持或损坏的编码也可能报告 OutOfMemoryException。
            throw new InvalidDataException("无法解码图像数据。", ex);
        }
    }

    #endregion

    #region FromBase64String(从指定Base64字符串创建图片)

    /// <summary>
    /// 从 Base64 字符串加载图像。
    /// </summary>
    /// <param name="base64String">包含编码图像数据的 Base64 字符串。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentNullException">Base64 字符串为空。</exception>
    /// <exception cref="FormatException">字符串不是有效的 Base64。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static Image FromBase64String(string base64String)
    {
        return FromBytes(Convert.FromBase64String(base64String));
    }

    #endregion

    #region FromDataUrl(从指定DataUrl字符串创建图片)

    /// <summary>
    /// 从图像 Data URL 加载图像。
    /// </summary>
    /// <param name="dataUrl">图像 Data URL。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="FormatException">Data URL 为空或格式无效。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static Image FromDataUrl(string dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
            throw new FormatException("无效的图像 Data URL。");
        var match = ImageDataUrl.Match(dataUrl);
        if (!match.Success)
            throw new FormatException("无效的图像 Data URL。");
        return FromBase64String(match.Groups["DATA"].Value);
    }

    #endregion
}
