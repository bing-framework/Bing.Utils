using System.Security.Cryptography;

// ReSharper disable once CheckNamespace
namespace Bing.Extensions;

/// <summary>
/// 字节流(<see cref="Stream"/>) 扩展
/// </summary>
public static class StreamExtensions
{
    #region ToFile(将流写入指定文件路径)

    /// <summary>
    /// 从流当前位置写入指定文件路径，覆盖并截断原文件；不关闭输入流。
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="path">文件路径</param>
    /// <param name="bufferSize">缓冲区大小。默认：32KB</param>
    public static bool ToFile(this Stream stream, string path, int bufferSize = 32 * 1024)
    {
        if (stream == null || string.IsNullOrWhiteSpace(path) || bufferSize <= 0)
            return false;
        try
        {
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[bufferSize];
                int len;
                while ((len = stream.Read(buffer, 0, bufferSize)) > 0)
                {
                    fileStream.Write(buffer, 0, len);
                }
            }
            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region ContentsEqual(比较流内容是否相等)

    /// <summary>
    /// 比较两个流从各自当前位置到结束的内容，不关闭或复位输入流。
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="other">待比较的流</param>
    public static bool ContentsEqual(this Stream stream, Stream other)
    {
        stream.CheckNotNull(nameof(stream));
        other.CheckNotNull(nameof(other));

        const int bufferSize = 2048;
        var streamBuffer = new byte[bufferSize];
        var otherBuffer = new byte[bufferSize];
        while (true)
        {
            // 各自填满一块或到达 EOF，不假定一次 Read 会返回所有请求的字节。
            var streamLen = ReadComparisonBlock(stream, streamBuffer);
            var otherLen = ReadComparisonBlock(other, otherBuffer);
            if (streamLen != otherLen)
                return false;
            if (streamLen == 0)
                return true;
            for (var i = 0; i < streamLen; i++)
                if (streamBuffer[i] != otherBuffer[i])
                    return false;
        }
    }

    /// <summary>读取一块用于比较的数据，支持短读及不可定位流。</summary>
    private static int ReadComparisonBlock(Stream stream, byte[] buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer, total, buffer.Length - total);
            if (read == 0)
                break;
            total += read;
        }
        return total;
    }

    #endregion

    #region GetMd5(获取流的MD5值)

    /// <summary>
    /// 获取流的MD5值
    /// </summary>
    /// <param name="stream">流</param>
    [Obsolete("MD5 仅用于兼容非安全校验；安全完整性校验请使用 Bing.Security.Hashing.Hashing 的 SHA-2 API。")]
    public static string GetMd5(this Stream stream)
    {
        using (var md5 = MD5.Create())
        {
            var buffer = md5.ComputeHash(stream);
            var md5Builder = new StringBuilder();
            foreach (var b in buffer)
                md5Builder.Append(b.ToString("x2"));
            return md5Builder.ToString();
        }
    }

    #endregion
}
