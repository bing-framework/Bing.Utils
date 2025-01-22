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
    /// 将流写入指定文件路径
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="path">文件路径</param>
    /// <param name="bufferSize">缓冲区大小。默认：32KB</param>
    public static bool ToFile(this Stream stream, string path, int bufferSize = 32 * 1024)
    {
        if (stream == null || string.IsNullOrWhiteSpace(path))
            return false;
        try
        {
            using (var fileStream = File.OpenWrite(path))
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
    /// 比较流内容是否相等
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="other">待比较的流</param>
    public static bool ContentsEqual(this Stream stream, Stream other)
    {
        stream.CheckNotNull(nameof(stream));
        other.CheckNotNull(nameof(other));

        if (stream.Length != other.Length)
            return false;

        const int bufferSize = 2048;
        var streamBuffer = new byte[bufferSize];
        var otherBuffer = new byte[bufferSize];

        while (true)
        {
            int streamLen = stream.Read(streamBuffer, 0, bufferSize);
            int otherLen = other.Read(otherBuffer, 0, bufferSize);

            if (streamLen != otherLen)
                return false;
            if (streamLen == 0)
                return true;

            int iterations = (int)Math.Ceiling((double)streamLen / sizeof(Int64));
            for (int i = 0; i < iterations; i++)
            {
                if (BitConverter.ToInt64(streamBuffer, i * sizeof(Int64)) !=
                    BitConverter.ToInt64(otherBuffer, i * sizeof(Int64)))
                    return false;
            }
        }
    }

    #endregion

    #region GetMd5(获取流的MD5值)

    /// <summary>
    /// 获取流的MD5值
    /// </summary>
    /// <param name="stream">流</param>
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