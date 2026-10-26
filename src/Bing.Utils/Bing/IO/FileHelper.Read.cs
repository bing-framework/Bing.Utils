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
        using var output = new MemoryStream();
        fs.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>
    /// 读取流直至结束并转换成字节数组，支持短读及不可定位流。
    /// </summary>
    /// <remarks>不关闭输入流；可定位流从起点读取，成功后复位到起点；不可定位流从当前位置读取。</remarks>
    /// <param name="stream">流</param>
    public static byte[] ReadToBytes(Stream stream)
    {
        if (stream == null)
            return null;
        if (stream.CanRead == false)
            return null;
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        using var output = new MemoryStream();
        stream.CopyTo(output);
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        return output.ToArray();
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
        using var output = new MemoryStream();
        await fs.CopyToAsync(output, 81920, cancellationToken).ConfigureAwait(false);
        return output.ToArray();
    }

    /// <summary>
    /// 读取流直至结束并转换成字节数组，支持短读及不可定位流。
    /// </summary>
    /// <remarks>不关闭输入流；可定位流从起点读取，成功后复位到起点；不可定位流从当前位置读取。</remarks>
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
        using var output = new MemoryStream();
        await stream.CopyToAsync(output, 81920, cancellationToken).ConfigureAwait(false);
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        return output.ToArray();
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
    /// <remarks>成功返回时位置位于末尾，读取前须复位；调用方负责释放。读取失败返回 null。</remarks>
    public static MemoryStream ReadToMemoryStream(string filePath)
    {
        MemoryStream memoryStream = null;
        try
        {
            if (Exists(filePath) == false)
                return null;
            memoryStream = new MemoryStream();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            stream.CopyTo(memoryStream);
            return memoryStream;
        }
        catch
        {
            memoryStream?.Dispose();
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
    /// <remarks>成功返回时位置位于末尾，读取前须复位；调用方负责释放。取消正常传播，其他读取失败返回 null。</remarks>
    public static async Task<MemoryStream> ReadToMemoryStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MemoryStream memoryStream = null;
        try
        {
            if (Exists(filePath) == false)
                return null;
            memoryStream = new MemoryStream();
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            return memoryStream;
        }
        catch (OperationCanceledException)
        {
            memoryStream?.Dispose();
            throw;
        }
        catch
        {
            memoryStream?.Dispose();
            return null;
        }
    }
#else
    /// <summary>
    /// 读取文件到内存流中
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>成功返回时位置位于末尾，读取前须复位；调用方负责释放。取消正常传播，其他读取失败返回 null。</remarks>
    public static async Task<MemoryStream> ReadToMemoryStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MemoryStream memoryStream = null;
        try
        {
            if (Exists(filePath) == false)
                return null;
            memoryStream = new MemoryStream();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            return memoryStream;
        }
        catch (OperationCanceledException)
        {
            memoryStream?.Dispose();
            throw;
        }
        catch
        {
            memoryStream?.Dispose();
            return null;
        }
    }
#endif
    #endregion

}
