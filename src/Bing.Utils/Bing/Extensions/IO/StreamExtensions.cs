﻿using System.Security.Cryptography;

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
    public static bool ToFile(this Stream stream, string path)
    {
        if (stream == null)
            return false;
        const int bufferSize = 32768;
        bool result = true;
        Stream fileStream = null;
        byte[] buffer = new byte[bufferSize];
        try
        {
            using (fileStream = File.OpenWrite(path))
            {
                int len;
                while ((len = stream.Read(buffer, 0, bufferSize)) > 0)
                {
                    fileStream.Write(buffer, 0, len);
                }
            }
        }
        catch
        {
            result = false;
        }
        finally
        {
            if (fileStream != null)
            {
                fileStream.Close();
                fileStream.Dispose();
            }
        }

        return (result && File.Exists(path));
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
        byte[] streamBuffer = new byte[bufferSize];
        byte[] otherBuffer = new byte[bufferSize];

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
        encoding = encoding ?? Encoding.UTF8;
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
        encoding = encoding ?? Encoding.UTF8;
        return new StreamWriter(stream, encoding);
    }

    #endregion

    #region ReadToEnd(读取所有文本)

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
        using (var reader = stream.GetReader(encoding))
        {
            return reader.ReadToEnd();
        }
    }

    #endregion

    #region SeekToBegin(设置流指针指向流的开始位置)

    /// <summary>
    /// 设置流指针指向流的开始位置
    /// </summary>
    /// <param name="stream">流</param>
    /// <exception cref="InvalidOperationException"></exception>
    public static Stream SeekToBegin(this Stream stream)
    {
        if (stream.CanSeek == false)
            throw new InvalidOperationException("Stream 不支持寻址操作");
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    #endregion

    #region SeekToEnd(设置流指针指向流的结束位置)

    /// <summary>
    /// 设置流指针指向流的结束位置
    /// </summary>
    /// <param name="stream">流</param>
    /// <exception cref="InvalidOperationException"></exception>
    public static Stream SeekToEnd(this Stream stream)
    {
        if (stream.CanSeek == false)
            throw new InvalidOperationException("Stream 不支持寻址操作");
        stream.Seek(0, SeekOrigin.End);
        return stream;
    }

    #endregion

    #region Write(将字符串写入流)

    /// <summary>
    /// 将字符串写以指定编码方式写入流
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="content">内容</param>
    /// <param name="encoding">编码类型</param>
    public static void Write(this Stream stream, string content, Encoding encoding)
    {
        byte[] buffer = encoding.GetBytes(content);
        stream.Write(buffer, 0, buffer.Length);
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