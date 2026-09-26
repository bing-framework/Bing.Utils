using System.Text.RegularExpressions;
using System;
using System.IO;
using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 提供 SkiaSharp 图像加载和解码。
/// </summary>
public static partial class SkiaSharpHelper
{
    /// <summary>
    /// 匹配图像 Data URL 的正则表达式。
    /// </summary>
    internal static readonly Regex ImageDataUrl = new(@"^data\:(?<MIME>image\/[a-z0-9.+-]+)\;base64\,(?<DATA>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    #region FromFile(从指定文件创建图片)

    /// <summary>
    /// 从文件加载图像。
    /// </summary>
    /// <param name="filePath">图像文件路径。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentException">文件路径为空。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static SKImage? FromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("文件路径不能为空。", nameof(filePath));

        return FromBytes(File.ReadAllBytes(filePath));
    }

    #endregion

    #region FromStream(从指定流创建图片)

    /// <summary>
    /// 从流加载图像。
    /// </summary>
    /// <param name="stream">包含图像数据的输入流；从当前位置读取。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentNullException">输入流为空。</exception>
    /// <remarks>
    /// 方法不会关闭输入流。
    /// </remarks>
    public static SKImage? FromStream(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        return FromBytes(ReadBytes(stream));
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
    public static SKImage? FromBytes(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));

        if (!TryLoad(bytes, out var image))
            throw new InvalidDataException("图像内容无效或格式不受支持。");

        return image;
    }

    #endregion

    #region FromBase64String(从指定Base64字符串创建图片)

    /// <summary>
    /// 从 Base64 字符串加载图像。
    /// </summary>
    /// <param name="base64String">包含编码图像数据的 Base64 字符串。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentException">字符串为空。</exception>
    /// <exception cref="FormatException">字符串不是有效的 Base64。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static SKImage? FromBase64String(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            throw new ArgumentException("Base64 内容不能为空。", nameof(base64String));

        return FromBytes(Convert.FromBase64String(base64String));
    }

    #endregion

    #region FromDataUrl(从指定DataUrl字符串创建图片)

    /// <summary>
    /// 从图像 Data URL 加载图像。
    /// </summary>
    /// <param name="dataUrl">图像 Data URL。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentException">Data URL 为空。</exception>
    /// <exception cref="FormatException">Data URL 格式无效。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static SKImage? FromDataUrl(string dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
            throw new ArgumentException("Data URL 不能为空。", nameof(dataUrl));
        var match = ImageDataUrl.Match(dataUrl);
        if (!match.Success)
            throw new FormatException("Data URL 格式无效。");
        return FromBase64String(match.Groups["DATA"].Value);
    }

    #endregion

    /// <summary>
    /// 尝试从字节数组解码图像。
    /// </summary>
    /// <param name="bytes">编码图像数据。</param>
    /// <param name="image">成功解码的图像；失败时为 <c>null</c>。</param>
    /// <returns>解码成功返回 <see langword="true" />，否则返回 <see langword="false" />。</returns>
    /// <exception cref="ArgumentNullException">字节数组为空。</exception>
    public static bool TryLoad(byte[] bytes, out SKImage? image)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));

        image = null;
        SKImage? loaded = null;
        try
        {
            if (bytes.Length == 0)
                return false;

            loaded = SKImage.FromEncodedData(bytes);
            if (loaded is null)
                return false;

            var format = DetectEncodedImageFormat(bytes);
            image = TrackFormat(loaded, format);
            return true;
        }
        catch (Exception exception) when (!(exception is OutOfMemoryException))
        {
            loaded?.Dispose();
            image?.Dispose();
            image = null;
            return false;
        }
    }

    /// <summary>
    /// 读取流中的全部字节内容。
    /// </summary>
    /// <param name="stream">输入流。</param>
    /// <returns>读取到的字节数组。</returns>
    private static byte[] ReadBytes(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// 检测图像编码格式。
    /// </summary>
    /// <param name="bytes">编码图像数据。</param>
    /// <returns>检测到的编码格式；无法检测时返回 PNG。</returns>
    private static SKEncodedImageFormat DetectEncodedImageFormat(byte[] bytes)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        return codec?.EncodedFormat ?? SKEncodedImageFormat.Png;
    }
}
