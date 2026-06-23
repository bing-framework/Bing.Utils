namespace Bing.Drawing;

// 图片操作辅助类 - 元数据清理
public static partial class ImageSharpHelper
{
    #region DeleteCoordinate(删除图片中的 GPS 经纬度信息)

    /// <summary>
    /// 删除编码图像数据中的 GPS 元数据
    /// </summary>
    /// <param name="source">编码图像数据（JPEG/PNG）</param>
    /// <param name="options">清理选项</param>
    /// <returns>清理后的编码图像数据</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    public static byte[] DeleteCoordinate(byte[] source, ImageMetadataOptions? options = null)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        return Internal.EncodedImageSanitizer.Sanitize(source, options ?? new ImageMetadataOptions());
    }

    /// <summary>
    /// 从输入流读取编码图像数据并删除 GPS 元数据
    /// </summary>
    /// <param name="input">输入流</param>
    /// <param name="options">清理选项</param>
    /// <returns>清理后的编码图像数据</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    public static byte[] DeleteCoordinate(Stream input, ImageMetadataOptions? options = null)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        using var ms = new MemoryStream();
        input.CopyTo(ms);
        return Internal.EncodedImageSanitizer.Sanitize(ms.ToArray(), options ?? new ImageMetadataOptions());
    }

    /// <summary>
    /// 从输入流读取编码图像数据，删除 GPS 元数据后写入输出流
    /// </summary>
    /// <param name="input">输入流</param>
    /// <param name="output">输出流</param>
    /// <param name="options">清理选项</param>
    /// <param name="leaveOpen">是否在操作后保持流打开</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    public static void DeleteCoordinate(Stream input, Stream output, ImageMetadataOptions? options = null, bool leaveOpen = true)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));
        if (output is null)
            throw new ArgumentNullException(nameof(output));

        using var ms = new MemoryStream();
        input.CopyTo(ms);
        var result = Internal.EncodedImageSanitizer.Sanitize(ms.ToArray(), options ?? new ImageMetadataOptions());
        output.Write(result, 0, result.Length);
    }

    /// <summary>
    /// 覆盖文件并删除其中的 GPS 元数据
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="options">清理选项</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    public static void DeleteCoordinate(string filePath, ImageMetadataOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        var data = File.ReadAllBytes(filePath);
        var result = Internal.EncodedImageSanitizer.Sanitize(data, options ?? new ImageMetadataOptions());
        File.WriteAllBytes(filePath, result);
    }

    /// <summary>
    /// 读取源文件，删除 GPS 元数据后保存到目标路径
    /// </summary>
    /// <param name="sourcePath">源文件路径</param>
    /// <param name="destinationPath">目标文件路径</param>
    /// <param name="options">清理选项</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    public static void DeleteCoordinate(string sourcePath, string destinationPath, ImageMetadataOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentNullException(nameof(sourcePath));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentNullException(nameof(destinationPath));

        var data = File.ReadAllBytes(sourcePath);
        var result = Internal.EncodedImageSanitizer.Sanitize(data, options ?? new ImageMetadataOptions());
        File.WriteAllBytes(destinationPath, result);
    }

    #endregion
}
