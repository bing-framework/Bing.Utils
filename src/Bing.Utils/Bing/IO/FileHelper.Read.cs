namespace Bing.IO;

/// <summary>
/// 文件操作帮助类 - 读取
/// </summary>
public static partial class FileHelper
{
    #region ReadToBytes(读取文件到字节流中)

    /// <summary>
    /// 读取文件到字节流中
    /// </summary>
    /// <param name="targetFilePath">目标文件路径</param>
    public static byte[] ReadToBytes(string targetFilePath)
    {
        if (!Exists(targetFilePath))
            return null;

        using var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var byteArray = new byte[fs.Length];
        _ = fs.Read(byteArray, 0, byteArray.Length);
        return byteArray;
    }

    /// <summary>
    /// 读取流并转换成字节数组
    /// </summary>
    /// <param name="stream">流</param>
    public static byte[] ReadToBytes(Stream stream)
    {
        if (stream == null)
            return null;
        if (stream.CanRead == false)
            return null;
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        var buffer = new byte[stream.Length];
        _ = stream.Read(buffer, 0, buffer.Length);
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        return buffer;
    }

    #endregion

    #region ReadToBytesAsync(读取文件到字节流中)

    /// <summary>
    /// 读取文件到字节流中
    /// </summary>
    /// <param name="targetFilePath">目标文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<byte[]> ReadToBytesAsync(string targetFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(targetFilePath))
            return null;
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
        await using var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
#else
        using var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
#endif
        var byteArray = new byte[fs.Length];
        _ = await fs.ReadAsync(byteArray, 0, byteArray.Length, cancellationToken);
        return byteArray;
    }

    /// <summary>
    /// 读取流并转换成字节数组
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<byte[]> ReadToBytesAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            return null;
        if (stream.CanRead == false)
            return null;
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        var buffer = new byte[stream.Length];
        _ = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        return buffer;
    }

    #endregion

    #region ReadToString(读取文件到字符串中)

    /// <summary>
    /// 读取文件到字符串中
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    public static string ReadToString(string filePath) => ReadToString(filePath, Encoding.UTF8);

    /// <summary>
    /// 读取文件到字符串中
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="encoding">字符编码</param>
    public static string ReadToString(string filePath, Encoding encoding)
    {
        if (File.Exists(filePath) == false)
            return string.Empty;
        using var reader = new StreamReader(filePath, encoding);
        return reader.ReadToEnd();
    }

    #endregion

    #region ReadToStringAsync(读取文件到字符串中)

    /// <summary>
    /// 读取文件到字符串中
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    public static Task<string> ReadToStringAsync(string filePath) => ReadToStringAsync(filePath, Encoding.UTF8);

    /// <summary>
    /// 读取文件到字符串中
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="encoding">字符编码</param>
    public static async Task<string> ReadToStringAsync(string filePath, Encoding encoding)
    {
        if (File.Exists(filePath) == false)
            return string.Empty;
        using var reader = new StreamReader(filePath, encoding);
        return await reader.ReadToEndAsync();
    }

    #endregion

    #region ReadToStream(读取文件到流中)

    /// <summary>
    /// 读取文件到流中
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    public static Stream ReadToStream(string filePath)
    {
        try
        {
            return new FileStream(filePath, FileMode.Open);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region ReadToMemoryStream(读取文件到内存流中)

    /// <summary>
    /// 读取文件到内存流中
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    public static MemoryStream ReadToMemoryStream(string filePath)
    {
        try
        {
            if (Exists(filePath) == false)
                return null;
            var memoryStream = new MemoryStream();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            stream.CopyTo(memoryStream);
            return memoryStream;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region ReadToMemoryStreamAsync(读取文件到内存流中)
#if NET6_0_OR_GREATER
    /// <summary>
    /// 读取文件到内存流中
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<MemoryStream> ReadToMemoryStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (Exists(filePath) == false)
                return null;
            var memoryStream = new MemoryStream();
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            return memoryStream;
        }
        catch
        {
            return null;
        }
    }
#endif
    #endregion

}