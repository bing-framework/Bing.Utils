using System.IO;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 提供 ImageSharp 图像加载和解码。
/// </summary>
public static partial class ImageSharpHelper
{
    /// <summary>
    /// 匹配图像 Data URL 的正则表达式。
    /// </summary>
    internal static readonly Regex ImageDataUrl = new(@"^data\:(?<MIME>image\/[a-z0-9.+-]+)\;base64\,(?<DATA>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// 从文件加载图像。
    /// </summary>
    /// <param name="filePath">图像文件路径。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentException">文件路径为空。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static Image? FromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("文件路径不能为空。", nameof(filePath));
        return LoadOrThrow(File.ReadAllBytes(filePath));
    }

    /// <summary>
    /// 从文件加载图像。
    /// </summary>
    /// <typeparam name="TPixel">图像使用的像素类型。</typeparam>
    /// <param name="filePath">图像文件路径。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentException">文件路径为空。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static Image<TPixel>? FromFile<TPixel>(string filePath) where TPixel : unmanaged, IPixel<TPixel>
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("文件路径不能为空。", nameof(filePath));
        return LoadOrThrow<TPixel>(File.ReadAllBytes(filePath));
    }

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
    public static Image? FromStream(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        return LoadOrThrow(ReadBytesAndDetectFormat(stream));
    }

    /// <summary>
    /// 从流加载图像。
    /// </summary>
    /// <typeparam name="TPixel">图像使用的像素类型。</typeparam>
    /// <param name="stream">包含图像数据的输入流；从当前位置读取。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentNullException">输入流为空。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    /// <remarks>
    /// 方法不会关闭输入流。
    /// </remarks>
    public static Image<TPixel>? FromStream<TPixel>(Stream stream) where TPixel : unmanaged, IPixel<TPixel>
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        return LoadOrThrow<TPixel>(ReadBytesAndDetectFormat(stream));
    }

    /// <summary>
    /// 从字节数组加载图像。
    /// </summary>
    /// <param name="bytes">编码图像数据。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentNullException">字节数组为空。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static Image? FromBytes(byte[] bytes)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        return LoadOrThrow(bytes);
    }

    /// <summary>
    /// 从字节数组加载图像。
    /// </summary>
    /// <typeparam name="TPixel">图像使用的像素类型。</typeparam>
    /// <param name="bytes">编码图像数据。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentNullException">字节数组为空。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static Image<TPixel>? FromBytes<TPixel>(byte[] bytes) where TPixel : unmanaged, IPixel<TPixel>
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        return LoadOrThrow<TPixel>(bytes);
    }

    /// <summary>
    /// 从 Base64 字符串加载图像。
    /// </summary>
    /// <param name="base64String">包含编码图像数据的 Base64 字符串。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentException">字符串为空。</exception>
    /// <exception cref="FormatException">字符串不是有效的 Base64。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static Image? FromBase64String(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String)) throw new ArgumentException("Base64 内容不能为空。", nameof(base64String));
        return LoadOrThrow(Convert.FromBase64String(base64String));
    }

    /// <summary>
    /// 从 Base64 字符串加载图像。
    /// </summary>
    /// <typeparam name="TPixel">图像使用的像素类型。</typeparam>
    /// <param name="base64String">包含编码图像数据的 Base64 字符串。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentException">字符串为空。</exception>
    /// <exception cref="FormatException">字符串不是有效的 Base64。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static Image<TPixel>? FromBase64String<TPixel>(string base64String) where TPixel : unmanaged, IPixel<TPixel>
    {
        if (string.IsNullOrWhiteSpace(base64String)) throw new ArgumentException("Base64 内容不能为空。", nameof(base64String));
        return LoadOrThrow<TPixel>(Convert.FromBase64String(base64String));
    }

    /// <summary>
    /// 从图像 Data URL 加载图像。
    /// </summary>
    /// <param name="dataUrl">图像 Data URL。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentException">Data URL 为空。</exception>
    /// <exception cref="FormatException">Data URL 格式无效。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static Image? FromDataUrl(string dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) throw new ArgumentException("Data URL 不能为空。", nameof(dataUrl));
        var match = ImageDataUrl.Match(dataUrl);
        if (!match.Success) throw new FormatException("Data URL 格式无效。");
        return FromBase64String(match.Groups["DATA"].Value);
    }

    /// <summary>
    /// 从图像 Data URL 加载图像。
    /// </summary>
    /// <typeparam name="TPixel">图像使用的像素类型。</typeparam>
    /// <param name="dataUrl">图像 Data URL。</param>
    /// <returns>解码后的图像。</returns>
    /// <exception cref="ArgumentException">Data URL 为空。</exception>
    /// <exception cref="FormatException">Data URL 格式无效。</exception>
    /// <exception cref="InvalidDataException">图像内容无效或格式不受支持。</exception>
    public static Image<TPixel>? FromDataUrl<TPixel>(string dataUrl) where TPixel : unmanaged, IPixel<TPixel>
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) throw new ArgumentException("Data URL 不能为空。", nameof(dataUrl));
        var match = ImageDataUrl.Match(dataUrl);
        if (!match.Success) throw new FormatException("Data URL 格式无效。");
        return FromBase64String<TPixel>(match.Groups["DATA"].Value);
    }

    /// <summary>
    /// 尝试从字节数组解码图像。
    /// </summary>
    /// <param name="bytes">编码图像数据。</param>
    /// <param name="image">成功解码的图像；失败时为 <c>null</c>。</param>
    /// <returns>解码成功返回 <see langword="true" />，否则返回 <see langword="false" />。</returns>
    /// <exception cref="ArgumentNullException">字节数组为空。</exception>
    public static bool TryLoad(byte[] bytes, out Image? image)
    {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        try { image = LoadOrThrow(bytes); return true; }
        catch (Exception exception) when (exception is InvalidDataException || exception is UnknownImageFormatException)
        { image = null; return false; }
    }

    /// <summary>
    /// 读取流内容并检测图像格式。
    /// </summary>
    /// <param name="stream">输入流。</param>
    /// <returns>读取到的字节和检测到的图像格式。</returns>
    private static (byte[] Bytes, IImageFormat? Format) ReadBytesAndDetectFormat(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        try { return (bytes, Image.DetectFormat(bytes)); }
        catch (UnknownImageFormatException exception)
        { throw new InvalidDataException("图像内容无效或格式不受支持。", exception); }
    }

    /// <summary>
    /// 加载并记录图像的编码格式。
    /// </summary>
    /// <param name="bytes">编码图像数据。</param>
    /// <returns>解码后的图像。</returns>
    private static Image LoadOrThrow(byte[] bytes)
    {
        try { return TrackFormat(Image.Load(bytes), Image.DetectFormat(bytes)); }
        catch (Exception exception) when (exception is UnknownImageFormatException || exception is InvalidImageContentException)
        { throw new InvalidDataException("图像内容无效或格式不受支持。", exception); }
    }

    /// <summary>
    /// 加载并记录图像的编码格式。
    /// </summary>
    /// <typeparam name="TPixel">图像使用的像素类型。</typeparam>
    /// <param name="bytes">编码图像数据。</param>
    /// <returns>解码后的图像。</returns>
    private static Image<TPixel> LoadOrThrow<TPixel>(byte[] bytes) where TPixel : unmanaged, IPixel<TPixel>
    {
        try { return TrackFormat(Image.Load<TPixel>(bytes), Image.DetectFormat(bytes)); }
        catch (Exception exception) when (exception is UnknownImageFormatException || exception is InvalidImageContentException)
        { throw new InvalidDataException("图像内容无效或格式不受支持。", exception); }
    }

    /// <summary>
    /// 加载并记录图像的编码格式。
    /// </summary>
    /// <param name="input">图像字节和格式。</param>
    /// <returns>解码后的图像。</returns>
    /// <remarks>
    /// 使用输入中预先检测的编码格式记录图像格式。
    /// </remarks>
    private static Image LoadOrThrow((byte[] Bytes, IImageFormat? Format) input)
    {
        try { return TrackFormat(Image.Load(input.Bytes), input.Format); }
        catch (Exception exception) when (exception is UnknownImageFormatException || exception is InvalidImageContentException)
        { throw new InvalidDataException("图像内容无效或格式不受支持。", exception); }
    }

    /// <summary>
    /// 加载并记录图像的编码格式。
    /// </summary>
    /// <typeparam name="TPixel">图像使用的像素类型。</typeparam>
    /// <param name="input">图像字节和格式。</param>
    /// <returns>解码后的图像。</returns>
    /// <remarks>
    /// 使用输入中预先检测的编码格式记录图像格式。
    /// </remarks>
    private static Image<TPixel> LoadOrThrow<TPixel>((byte[] Bytes, IImageFormat? Format) input) where TPixel : unmanaged, IPixel<TPixel>
    {
        try { return TrackFormat(Image.Load<TPixel>(input.Bytes), input.Format); }
        catch (Exception exception) when (exception is UnknownImageFormatException || exception is InvalidImageContentException)
        { throw new InvalidDataException("图像内容无效或格式不受支持。", exception); }
    }
}
