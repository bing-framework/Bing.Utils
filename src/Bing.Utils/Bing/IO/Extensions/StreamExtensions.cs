using StreamReader = System.IO.StreamReader;

// ReSharper disable once CheckNamespace
namespace Bing.IO;

/// <summary>
/// 流 <see cref="Stream"/> 扩展
/// </summary>
public static class StreamExtensions
{
    #region Seek

    /// <summary>
    /// 尝试重新设定流位置
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="offset">偏移量</param>
    /// <param name="origin">流定位</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long TrySeek(this Stream stream, long offset, SeekOrigin origin)
        => stream.CanSeek ? stream.Seek(offset, origin) : default;

    /// <summary>
    /// 设置流指针指向流的开始位置
    /// </summary>
    /// <param name="stream">流</param>
    /// <returns>流</returns>
    /// <exception cref="InvalidOperationException">当流不支持寻址操作时抛出</exception>
    public static Stream SeekToBegin(this Stream stream)
    {
        if (stream.CanSeek == false)
            throw new InvalidOperationException("Stream 不支持寻址操作");
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    /// <summary>
    /// 设置流指针指向流的结束位置
    /// </summary>
    /// <param name="stream">流</param>
    /// <returns>流</returns>
    /// <exception cref="InvalidOperationException">当流不支持寻址操作时抛出</exception>
    public static Stream SeekToEnd(this Stream stream)
    {
        if (stream.CanSeek == false)
            throw new InvalidOperationException("Stream 不支持寻址操作");
        stream.Seek(0, SeekOrigin.End);
        return stream;
    }

    #endregion

    #region Read

    /// <summary>
    /// 从流中读取所有文本，默认编码：UTF-8
    /// </summary>
    /// <param name="stream">流</param>
    public static string ReadToEnd(this Stream stream) => ReadToEnd(stream, Encoding.UTF8);

    /// <summary>
    /// 从流中读取所有文本，使用指定编码
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="encoding">编码，默认编码：UTF-8</param>
    public static string ReadToEnd(this Stream stream, Encoding encoding)
    {
        using var streamReader = stream.GetReader(encoding);
        return streamReader.ReadToEnd();
    }

    /// <summary>
    /// 从流中读取所有文本，默认编码：UTF-8
    /// </summary>
    /// <param name="stream">流</param>
    public static Task<string> ReadToEndAsync(this Stream stream) => ReadToEndAsync(stream, Encoding.UTF8);

    /// <summary>
    /// 从流中读取所有文本，使用指定编码
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="encoding">编码，默认编码：UTF-8</param>
    public static async Task<string> ReadToEndAsync(this Stream stream, Encoding encoding)
    {
        using var streamReader = stream.GetReader(encoding);
        return await streamReader.ReadToEndAsync();
    }

    /// <summary>
    /// 读取流当中所有字节数组
    /// </summary>
    /// <param name="stream">流</param>
    public static byte[] ReadAllBytes(this Stream stream)
    {
        long originalPosition = 0;

        if (stream.CanSeek)
        {
            originalPosition = stream.Position;
            stream.Position = 0;
        }

        try
        {
            var readBuffer = new byte[4096];

            var totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
            {
                totalBytesRead += bytesRead;

                if (totalBytesRead == readBuffer.Length)
                {
                    var nextByte = stream.ReadByte();
                    if (nextByte != -1)
                    {
                        var temp = new byte[readBuffer.Length * 2];
                        Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                        Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                        readBuffer = temp;
                        totalBytesRead++;
                    }
                }
            }

            var buffer = readBuffer;
            if (readBuffer.Length != totalBytesRead)
            {
                buffer = new byte[totalBytesRead];
                Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
            }

            return buffer;
        }
        finally
        {
            if (stream.CanSeek)
                stream.Position = originalPosition;
        }
    }

    /// <summary>
    /// 尝试读取
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="buffer">缓冲字节数组</param>
    /// <param name="offset">偏移量</param>
    /// <param name="count">读取数量</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TryRead(this Stream stream, byte[] buffer, int offset, int count)
        => stream.CanRead ? stream.Read(buffer, offset, count) : default;

    /// <summary>
    /// 尝试读取
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="buffer">缓冲字节数组</param>
    /// <param name="offset">偏移量</param>
    /// <param name="count">读取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<int> TryReadAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        => stream.CanRead ? await stream.ReadAsync(buffer, offset, count, cancellationToken) : default;

    /// <summary>
    /// 尝试读取字节
    /// </summary>
    /// <param name="stream">流</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TryReadByte(this Stream stream)
        => stream.CanRead ? stream.ReadByte() : default;

    /// <summary>
    /// 尝试设置读取超时时间
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="milliseconds">毫秒数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetReadTimeout(this Stream stream, int milliseconds)
    {
        if (stream.CanTimeout)
            stream.ReadTimeout = milliseconds;
        return stream.CanTimeout;
    }

    /// <summary>
    /// 尝试设置读取超时时间
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="timeout">时间跨度</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetReadTimeout(this Stream stream, TimeSpan timeout)
        => stream.TrySetReadTimeout(timeout.Milliseconds);

    #endregion

    #region Write

    /// <summary>
    /// 将字符串写以指定编码方式写入流
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="content">内容</param>
    /// <param name="encoding">编码，默认编码：UTF-8</param>
    public static void Write(this Stream stream, string content, Encoding encoding)
    {
        encoding ??= Encoding.UTF8;
        var buffer = encoding.GetBytes(content);
        Write(stream, buffer);
    }

    /// <summary>
    /// 将字节数组写入流
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="buffer">字节数组</param>
    public static void Write(this Stream stream, byte[] buffer) => stream.Write(buffer, 0, buffer.Length);

    /// <summary>
    /// 尝试写入
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="buffer">字节数组</param>
    /// <param name="offset">偏移量</param>
    /// <param name="count">写入数量</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryWrite(this Stream stream, byte[] buffer, int offset, int count)
    {
        if (stream.CanWrite)
            stream.Write(buffer, offset, count);
        return stream.CanWrite;
    }

    /// <summary>
    /// 尝试写入
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="buffer">字节数组</param>
    /// <param name="offset">偏移量</param>
    /// <param name="count">写入数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<bool> TryWriteAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (stream.CanWrite)
            await stream.WriteAsync(buffer, offset, count, cancellationToken);
        return stream.CanWrite;
    }

    /// <summary>
    /// 尝试写入字节
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="value">字节</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryWriteByte(this Stream stream, byte value)
    {
        if (stream.CanWrite)
            stream.WriteByte(value);
        return stream.CanWrite;
    }

    /// <summary>
    /// 尝试设置写入超时时间
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="milliseconds">毫秒数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetWriteTimeout(this Stream stream, int milliseconds)
    {
        if (stream.CanTimeout)
            stream.WriteTimeout = milliseconds;
        return stream.CanTimeout;
    }

    /// <summary>
    /// 尝试设置写入超时时间
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="timeout">时间跨度</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetWriteTimeout(this Stream stream, TimeSpan timeout)
        => stream.TrySetWriteTimeout(timeout.Milliseconds);

    #endregion

    #region GetReader(获取流读取器)

    /// <summary>
    /// 获取流读取器，默认编码：UTF-8
    /// </summary>
    /// <param name="stream">流</param>
    public static StreamReader GetReader(this Stream stream) => GetReader(stream, Encoding.UTF8);

    /// <summary>
    /// 获取流读取器，使用指定编码
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="encoding">编码，默认：UTF-8</param>
    /// <exception cref="InvalidOperationException"></exception>
    public static StreamReader GetReader(this Stream stream, Encoding encoding)
    {
        if (stream.CanRead == false)
            throw new InvalidOperationException("Stream 不支持读取操作");
        encoding ??= Encoding.UTF8;
        return new StreamReader(stream, encoding);
    }

    #endregion

    #region GetWriter(获取流写入器)

    /// <summary>
    /// 获取流写入器，默认编码：UTF-8
    /// </summary>
    /// <param name="stream">流</param>
    public static StreamWriter GetWriter(this Stream stream) => GetWriter(stream, Encoding.UTF8);

    /// <summary>
    /// 获取流写入器，使用指定编码
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="encoding">编码，默认编码：UTF-8</param>
    /// <exception cref="InvalidOperationException"></exception>
    public static StreamWriter GetWriter(this Stream stream, Encoding encoding)
    {
        if (stream.CanWrite == false)
            throw new InvalidOperationException("Stream 不支持写入操作");
        encoding ??= Encoding.UTF8;
        return new StreamWriter(stream, encoding);
    }

    #endregion

    #region GetAllBytes(获取所有字节数组)

    /// <summary>
    /// 获取所有字节数组
    /// </summary>
    /// <param name="stream">流</param>
    public static byte[] GetAllBytes(this Stream stream)
    {
        if (stream is MemoryStream memoryStream)
            return memoryStream.ToArray();
        using var ms = stream.CreateMemoryStream();
        return ms.ToArray();
    }

    #endregion

    #region GetAllBytesAsync(获取所有字节数组)

    /// <summary>
    /// 获取所有字节数组
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<byte[]> GetAllBytesAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream is MemoryStream memoryStream)
            return memoryStream.ToArray();
        using var ms = await stream.CreateMemoryStreamAsync(cancellationToken);
        return ms.ToArray();
    }

    #endregion

    #region CopyToFile(以文件流的形式复制大文件)

    /// <summary>
    /// 以文件流的形式复制大文件
    /// </summary>
    /// <param name="stream">源</param>
    /// <param name="dest">目标地址</param>
    /// <param name="bufferSize">缓冲区大小。默认：8MB</param>
    public static void CopyToFile(this Stream stream, string dest, int bufferSize = 1024 * 8 * 1024)
    {
        stream.Seek(0, SeekOrigin.Begin);
        using var fs = new FileStream(dest, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        var bs = new BufferedStream(stream, bufferSize);
        bs.CopyTo(fs);
        stream.Seek(0, SeekOrigin.Begin);
    }

    #endregion

    #region CopyToFileAsync(以文件流的形式复制大文件)

    /// <summary>
    /// 以文件流的形式复制大文件
    /// </summary>
    /// <param name="stream">源</param>
    /// <param name="dest">目标地址</param>
    /// <param name="bufferSize">缓冲区大小。默认：8MB</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static Task CopyToFileAsync(this Stream stream, string dest, int bufferSize = 1024 * 8 * 1024, CancellationToken cancellationToken = default)
    {
        using var fs = new FileStream(dest, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        var bs = new BufferedStream(stream, bufferSize);
        return bs.CopyToAsync(fs, cancellationToken);
    }

    #endregion

    #region SaveFile(将内存流转储成文件)

    /// <summary>
    /// 将内存流转储成文件
    /// </summary>
    /// <param name="stream">源</param>
    /// <param name="fileName">文件名</param>
    public static void SaveFile(this Stream stream, string fileName)
    {
        stream.Seek(0, SeekOrigin.Begin);
        using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        var bs = new BufferedStream(stream, 1048576);
        bs.CopyTo(fs);
        stream.Seek(0, SeekOrigin.Begin);
    }

    /// <summary>
    /// 将内存流转储成文件
    /// </summary>
    /// <param name="stream">源</param>
    /// <param name="fileName">文件名</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task SaveFileAsync(this Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        stream.Seek(0, SeekOrigin.Begin);
        using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        var bs = new BufferedStream(stream, 1048576);
        await bs.CopyToAsync(fs, cancellationToken);
        stream.Seek(0, SeekOrigin.Begin);
    }

    #endregion

    #region CopyToAsync(复制流)

    /// <summary>
    /// 复制当前流到目标流中
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="destination">目标流</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static Task CopyToAsync(this Stream stream, Stream destination, CancellationToken cancellationToken = default)
    {
        if (stream.CanSeek)
            stream.Position = 0;
        return stream.CopyToAsync(destination,
            81920, // 默认值，用于传递取消令牌
            cancellationToken);
    }

    #endregion

    #region CreateMemoryStream(创建内存流)

    /// <summary>
    /// 创建内存流
    /// </summary>
    /// <param name="stream">流</param>
    public static MemoryStream CreateMemoryStream(this Stream stream)
    {
        if (stream.CanSeek)
            stream.Position = 0;
        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        if (stream.CanSeek)
            stream.Position = 0;
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// 创建内存流
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<MemoryStream> CreateMemoryStreamAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream.CanSeek)
            stream.Position = 0;
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        if (stream.CanSeek)
            stream.Position = 0;
        memoryStream.Position = 0;
        return memoryStream;
    }

    #endregion

}