using Bing.Drawing.Internal;

namespace Bing.Drawing;

/// <summary>
/// 提供 GDI+ 编码图像的元数据清理。
/// </summary>
/// <remarks>
/// 清理操作基于编码字节执行，不要求创建 GDI+ 图像对象。
/// </remarks>
public static partial class ImageHelper
{
    /// <summary>
    /// 清理编码图像数据中的指定元数据。
    /// </summary>
    /// <param name="source">编码图像数据。</param>
    /// <param name="options">清理选项；为空时使用默认选项。</param>
    /// <returns>清理后的编码图像数据。</returns>
    /// <exception cref="ArgumentNullException">编码数据为空。</exception>
    /// <exception cref="NotSupportedException">输入格式不受支持。</exception>
    public static byte[] DeleteCoordinate(byte[] source, ImageMetadataOptions options = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return EncodedImageSanitizer.Sanitize(source, options ?? new ImageMetadataOptions());
    }

    /// <summary>
    /// 清理编码图像数据中的指定元数据。
    /// </summary>
    /// <param name="input">输入流；从当前位置读取。</param>
    /// <param name="options">清理选项；为空时使用默认选项。</param>
    /// <returns>清理后的编码图像数据。</returns>
    /// <exception cref="ArgumentNullException">输入流为空。</exception>
    /// <exception cref="NotSupportedException">输入格式不受支持。</exception>
    /// <remarks>
    /// 方法从输入流当前位置读取，且不会关闭输入流。
    /// </remarks>
    public static byte[] DeleteCoordinate(Stream input, ImageMetadataOptions options = null)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        using var buffer = new MemoryStream(); input.CopyTo(buffer);
        return DeleteCoordinate(buffer.ToArray(), options);
    }

    /// <summary>
    /// 清理编码图像数据中的指定元数据。
    /// </summary>
    /// <param name="input">输入流；从当前位置读取。</param>
    /// <param name="output">输出流。</param>
    /// <param name="options">清理选项；为空时使用默认选项。</param>
    /// <param name="leaveOpen">是否在操作后保持输入和输出流打开。</param>
    /// <exception cref="ArgumentNullException">输入流或输出流为空。</exception>
    /// <exception cref="ArgumentException">输入流和输出流是同一个实例。</exception>
    /// <exception cref="NotSupportedException">输入格式不受支持。</exception>
    /// <remarks>
    /// 方法从输入流当前位置读取并写入输出流；<paramref name="leaveOpen" /> 为 <see langword="false" /> 时关闭两个流。
    /// </remarks>
    public static void DeleteCoordinate(Stream input, Stream output, ImageMetadataOptions options = null, bool leaveOpen = true)
    {
        try
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (ReferenceEquals(input, output)) throw new ArgumentException("输入输出流必须不同。");
            var bytes = DeleteCoordinate(input, options);
            output.Write(bytes, 0, bytes.Length);
        }
        finally
        {
            if (!leaveOpen)
            {
                try { input?.Dispose(); }
                finally { if (!ReferenceEquals(input, output)) output?.Dispose(); }
            }
        }
    }

    /// <summary>
    /// 清理编码图像数据中的指定元数据。
    /// </summary>
    /// <param name="filePath">待处理的文件路径。</param>
    /// <param name="options">清理选项；为空时使用默认选项。</param>
    /// <exception cref="ArgumentNullException">文件路径为空。</exception>
    /// <exception cref="NotSupportedException">输入格式不受支持。</exception>
    /// <remarks>
    /// 方法原子覆盖原文件。
    /// </remarks>
    public static void DeleteCoordinate(string filePath, ImageMetadataOptions options) =>
        ImageFileWriter.Write(filePath, DeleteCoordinate(File.ReadAllBytes(filePath), options));

    /// <summary>
    /// 清理编码图像数据中的指定元数据。
    /// </summary>
    /// <param name="sourcePath">源文件路径。</param>
    /// <param name="destinationPath">目标文件路径。</param>
    /// <param name="options">清理选项；为空时使用默认选项。</param>
    /// <exception cref="ArgumentNullException">源文件路径或目标文件路径为空。</exception>
    /// <exception cref="NotSupportedException">输入格式不受支持。</exception>
    /// <remarks>
    /// 方法从源文件读取，并将结果原子写入目标文件。
    /// </remarks>
    public static void DeleteCoordinate(string sourcePath, string destinationPath, ImageMetadataOptions options) =>
        ImageFileWriter.Write(destinationPath, DeleteCoordinate(File.ReadAllBytes(sourcePath), options));
}
