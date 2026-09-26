using System.Threading;
using Bing.Drawing.Internal;
namespace Bing.Drawing;

/// <summary>
/// 提供基于 GDI+ 的统一图像处理入口。
/// </summary>
/// <remarks>
/// 处理过程内部拥有并释放图像表面；返回结果仅包含托管编码字节数组，不需要调用方释放。
/// </remarks>
public static partial class ImageHelper
{
    /// <summary>
    /// 获取统一处理流程支持的输出格式。
    /// </summary>
    public static IReadOnlyList<ImageOutputFormat> SupportedOutputFormats { get; } = Array.AsReadOnly(new[] { ImageOutputFormat.Png, ImageOutputFormat.Jpeg });
    /// <summary>
    /// 获取 GDI+ 原生编码器支持的输出格式。
    /// </summary>
    public static IReadOnlyList<ImageOutputFormat> SupportedNativeOutputFormats { get; } = Array.AsReadOnly(new[] { ImageOutputFormat.Png, ImageOutputFormat.Jpeg });
    /// <summary>
    /// 读取并验证图像头部信息。
    /// </summary>
    /// <param name="source">编码图像数据。</param>
    /// <param name="limits">图像处理限制；为空时使用默认限制。</param>
    /// <returns>图像的格式、尺寸和帧信息。</returns>
    public static ImageInfo Identify(byte[] source, ImageProcessingLimits? limits = null) => ImagePipeline.Identify(source, limits);
    /// <summary>
    /// 读取并验证图像头部信息。
    /// </summary>
    /// <param name="source">输入流。</param>
    /// <param name="limits">图像处理限制；为空时使用默认限制。</param>
    /// <param name="leaveOpen">是否在读取后保持输入流打开。</param>
    /// <param name="cancellationToken">用于取消读取的令牌。</param>
    /// <returns>图像的格式、尺寸和帧信息。</returns>
    public static ImageInfo Identify(Stream source, ImageProcessingLimits? limits = null, bool leaveOpen = true, CancellationToken cancellationToken = default) =>
        Identify(ImagePipeline.Read(source, limits ?? new ImageProcessingLimits(), leaveOpen, cancellationToken), limits);
    /// <summary>
    /// 读取并验证图像头部信息。
    /// </summary>
    /// <param name="path">图像文件路径。</param>
    /// <param name="limits">图像处理限制；为空时使用默认限制。</param>
    /// <returns>图像的格式、尺寸和帧信息。</returns>
    public static ImageInfo Identify(string path, ImageProcessingLimits? limits = null)
    {
        using var stream = File.OpenRead(path);
        return Identify(stream, limits);
    }
    /// <summary>
    /// 处理图像并返回编码结果。
    /// </summary>
    /// <param name="source">编码图像数据。</param>
    /// <param name="options">图像处理选项；为空时使用默认选项。</param>
    /// <param name="cancellationToken">用于取消处理的令牌。</param>
    /// <returns>处理后的编码结果。</returns>
    /// <remarks>
    /// 输入字节数组不会被修改；处理过程内部负责图像表面的生命周期。
    /// </remarks>
    public static ImageProcessResult Process(byte[] source, ImageProcessOptions? options = null, CancellationToken cancellationToken = default) =>
        ImagePipeline.Process(new GdiImageBackend(), source, options, cancellationToken);
    /// <summary>
    /// 处理图像并返回编码结果。
    /// </summary>
    /// <param name="source">输入流。</param>
    /// <param name="options">图像处理选项；为空时使用默认选项。</param>
    /// <param name="leaveOpen">是否在读取后保持输入流打开。</param>
    /// <param name="cancellationToken">用于取消处理的令牌。</param>
    /// <returns>处理后的编码结果。</returns>
    public static ImageProcessResult Process(Stream source, ImageProcessOptions? options = null, bool leaveOpen = true, CancellationToken cancellationToken = default) =>
        Process(ImagePipeline.Read(source, options?.Limits ?? new ImageProcessingLimits(), leaveOpen, cancellationToken), options, cancellationToken);
    /// <summary>
    /// 处理图像并返回编码结果。
    /// </summary>
    /// <param name="path">图像文件路径。</param>
    /// <param name="options">图像处理选项；为空时使用默认选项。</param>
    /// <param name="cancellationToken">用于取消处理的令牌。</param>
    /// <returns>处理后的编码结果。</returns>
    public static ImageProcessResult Process(string path, ImageProcessOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(path);
        return Process(stream, options, true, cancellationToken);
    }
    /// <summary>
    /// 按输入顺序合成多张图像并编码结果。
    /// </summary>
    /// <param name="sources">待合成的编码图像数据。</param>
    /// <param name="options">合成和图像处理选项；为空时使用默认选项。</param>
    /// <param name="cancellationToken">用于取消处理的令牌。</param>
    /// <returns>合成后的编码结果。</returns>
    public static ImageProcessResult Compose(IReadOnlyList<byte[]> sources, ImageComposeOptions? options = null, CancellationToken cancellationToken = default) =>
        ImagePipeline.Compose(new GdiImageBackend(), sources, options, cancellationToken);
    /// <summary>
    /// 将图像处理结果保存到文件。
    /// </summary>
    /// <param name="result">待写入的处理结果。</param>
    /// <param name="path">目标文件路径。</param>
    /// <param name="cancellationToken">用于取消写入的令牌。</param>
    /// <remarks>
    /// 使用原子文件写入；写入失败或取消时保留目标文件的已有内容。
    /// </remarks>
    public static void Save(ImageProcessResult result, string path, CancellationToken cancellationToken = default)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        ImageFileWriter.Write(path, result.Bytes, cancellationToken);
    }
    /// <summary>
    /// 将图像处理结果保存到文件。
    /// </summary>
    /// <param name="sourcePath">源文件路径。</param>
    /// <param name="destinationPath">目标文件路径。</param>
    /// <param name="options">图像处理选项；为空时使用默认选项。</param>
    /// <param name="cancellationToken">用于取消处理或写入的令牌。</param>
    /// <returns>处理后的编码结果。</returns>
    /// <remarks>
    /// 先按选项处理源图像，再保存编码结果；源路径和目标路径可以相同。
    /// </remarks>
    public static ImageProcessResult Save(string sourcePath, string destinationPath, ImageProcessOptions? options = null, CancellationToken cancellationToken = default)
    {
        var result = Process(sourcePath, options, cancellationToken);
        Save(result, destinationPath, cancellationToken);
        return result;
    }
}
