namespace Bing.IO;

/// <summary>
/// Bing <see cref="Stream"/> 扩展
/// </summary>
public static class BingStreamExtensions
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

    #endregion

    #region Read

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
        if(stream.CanWrite)
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
}