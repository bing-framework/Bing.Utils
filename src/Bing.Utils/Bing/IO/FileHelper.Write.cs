﻿using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Bing.IO;

/// <summary>
/// 文件操作帮助类 - 写入
/// </summary>
public static partial class FileHelper
{
    /// <summary>
    /// 保存内容到文件
    /// <para>
    /// 注：使用系统默认编码；若文件不存在则创建新的，若存在则覆盖
    /// </para>
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="content">数据</param>
    public static bool SaveFile(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        try
        {
            SaveFile(filePath, content, null, null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 保存内容到文件
    /// <para>
    /// 注：使用自定义编码；若文件不存在则创建新的，若存在则覆盖
    /// </para>
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="content">数据</param>
    /// <param name="encoding">字符编码</param>
    public static bool SaveFile(string filePath, string content, Encoding encoding)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        try
        {
            SaveFile(filePath, content, encoding, FileMode.Create);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 保存内容到文件
    /// <para>
    /// 注：使用自定义模式，使用UTF-8编码
    /// </para>
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="content">数据</param>
    /// <param name="fileMode">写入模式</param>
    public static bool SaveFile(string filePath, string content, FileMode fileMode)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        try
        {
            SaveFile(filePath, content, Encoding.UTF8, fileMode);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 保存内容到文件
    /// <para>
    /// 注：使用自定义编码以及写入模式
    /// </para>
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="content">数据</param>
    /// <param name="encoding">字符编码</param>
    /// <param name="fileMode">写入模式</param>
    public static bool SaveFile(string filePath, string content, Encoding encoding, FileMode fileMode)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        try
        {
            SaveFile(filePath, content, encoding, fileMode);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 保存内容到文件
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="content">数据</param>
    /// <param name="encoding">字符编码</param>
    /// <param name="fileMode">写入模式</param>
    private static void SaveFile(string filePath, string content, Encoding encoding, FileMode? fileMode)
    {
        encoding ??= Encoding.UTF8;
        fileMode ??= FileMode.Create;
        var dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        using var fs = new FileStream(filePath, fileMode.Value);
        using var sw = new StreamWriter(fs, encoding);
        sw.Write(content);
        sw.Flush();
    }

    /// <summary>
    /// 将内容写入文件，文件不存在则创建
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="content">数据</param>
    public static void Write(string filePath, string content)
    {
        Write(filePath, FileHelper.ToBytes(content));
    }

    /// <summary>
    /// 将内容写入文件，文件不存在则创建
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="bytes">数据</param>
    public static void Write(string filePath, byte[] bytes)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        if (bytes == null)
            return;
        File.WriteAllBytes(filePath, bytes);
    }

#if NETSTANDARD2_1 || NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0
    /// <summary>
    /// 将内容写入文件，文件不存在则创建
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="content">数据</param>
    public static async Task WriteAsync(string filePath, string content)
    {
        await WriteAsync(filePath, FileHelper.ToBytes(content));
    }

    /// <summary>
    /// 将内容写入文件，文件不存在则创建
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="bytes">数据</param>
    public static async Task WriteAsync(string filePath, byte[] bytes)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        if (bytes == null)
            return;
        await File.WriteAllBytesAsync(filePath, bytes);
    }
#endif

    /// <summary>
    /// 将字节数组写入目标文件
    /// </summary>
    /// <param name="byteArray">字节数组</param>
    /// <param name="targetFilePath">目标文件路径</param>
    /// <param name="appendMode">是否追加</param>
    public static bool Write(byte[] byteArray, string targetFilePath, bool appendMode = false)
    {
        if (!appendMode && File.Exists(targetFilePath))
            File.Create(targetFilePath);

        var fileMode = appendMode ? FileMode.Append : FileMode.Open;
        using var fs = new FileStream(targetFilePath, fileMode, FileAccess.Write);
        return fs.TryWrite(byteArray, 0, byteArray.Length);
    }

    /// <summary>
    /// 将字节数组写入目标文件
    /// </summary>
    /// <param name="byteArray">字节数组</param>
    /// <param name="targetFilePath">目标文件路径</param>
    /// <param name="appendMode">是否追加</param>
    public static async Task<bool> WriteAsync(byte[] byteArray, string targetFilePath, bool appendMode = false)
    {
        if (!appendMode && File.Exists(targetFilePath))
            File.Create(targetFilePath);

        var fileMode = appendMode ? FileMode.Append : FileMode.Open;
#if NETSTANDARD2_1 || NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0
        await using var fs = new FileStream(targetFilePath, fileMode, FileAccess.Write);
#else
            using var fs = new FileStream(targetFilePath, fileMode, FileAccess.Write);
#endif
        return await fs.TryWriteAsync(byteArray, 0, byteArray.Length);
    }
}