namespace Bing.Drawing;

/// <summary>
/// 提供 ImageSharp 编码图像的元数据清理。
/// </summary>
public static partial class ImageSharpHelper
{
    #region DeleteCoordinate(删除图片中的 GPS 经纬度信息)

    /// <summary>
    /// 清理编码图像数据中的指定元数据。
    /// </summary>
    /// <param name="source">编码图像数据。</param>
    /// <param name="options">清理选项；为空时使用默认选项。</param>
    /// <returns>清理后的编码图像数据。</returns>
    /// <exception cref="ArgumentNullException">编码数据为空。</exception>
    /// <exception cref="NotSupportedException">输入格式不受支持。</exception>
    public static byte[] DeleteCoordinate(byte[] source, ImageMetadataOptions? options = null)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        return Internal.EncodedImageSanitizer.Sanitize(source, options ?? new ImageMetadataOptions());
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
    public static byte[] DeleteCoordinate(Stream input, ImageMetadataOptions? options = null)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        using var ms = new MemoryStream();
        input.CopyTo(ms);
        return Internal.EncodedImageSanitizer.Sanitize(ms.ToArray(), options ?? new ImageMetadataOptions());
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
    public static void DeleteCoordinate(Stream input, Stream output, ImageMetadataOptions? options = null, bool leaveOpen = true)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));
        if (output is null)
            throw new ArgumentNullException(nameof(output));
        if (ReferenceEquals(input, output))
            throw new ArgumentException("输入流和输出流不能是同一个实例。", nameof(output));

        try
        {
            using var ms = new MemoryStream();
            input.CopyTo(ms);
            var result = Internal.EncodedImageSanitizer.Sanitize(ms.ToArray(), options ?? new ImageMetadataOptions());
            output.Write(result, 0, result.Length);
        }
        finally
        {
            if (!leaveOpen)
            {
                try
                {
                    input.Dispose();
                }
                finally
                {
                    output.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// 清理编码图像数据中的指定元数据。
    /// </summary>
    /// <param name="filePath">待处理的文件路径。</param>
    /// <param name="options">清理选项；为空时使用默认选项。</param>
    /// <exception cref="ArgumentNullException">文件路径为空。</exception>
    /// <exception cref="ArgumentException">文件数据无效。</exception>
    /// <exception cref="NotSupportedException">输入格式不受支持。</exception>
    /// <remarks>
    /// 方法原子覆盖原文件。
    /// </remarks>
    public static void DeleteCoordinate(string filePath, ImageMetadataOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        var data = File.ReadAllBytes(filePath);
        var result = Internal.EncodedImageSanitizer.Sanitize(data, options ?? new ImageMetadataOptions());
        Internal.ImageFileWriter.Write(filePath, result);
    }

    /// <summary>
    /// 清理编码图像数据中的指定元数据。
    /// </summary>
    /// <param name="sourcePath">源文件路径。</param>
    /// <param name="destinationPath">目标文件路径。</param>
    /// <param name="options">清理选项；为空时使用默认选项。</param>
    /// <exception cref="ArgumentNullException">源文件路径或目标文件路径为空。</exception>
    /// <exception cref="ArgumentException">文件数据无效。</exception>
    /// <exception cref="NotSupportedException">输入格式不受支持。</exception>
    /// <remarks>
    /// 方法从源文件读取，并将结果原子写入目标文件。
    /// </remarks>
    public static void DeleteCoordinate(string sourcePath, string destinationPath, ImageMetadataOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentNullException(nameof(sourcePath));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentNullException(nameof(destinationPath));

        var data = File.ReadAllBytes(sourcePath);
        var result = Internal.EncodedImageSanitizer.Sanitize(data, options ?? new ImageMetadataOptions());
        Internal.ImageFileWriter.Write(destinationPath, result);
    }

    #endregion
}
