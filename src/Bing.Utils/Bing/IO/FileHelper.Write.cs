using Bing.Helpers;

namespace Bing.IO;

/// <summary>
/// 文件操作帮助类 - 写入
/// </summary>
public static partial class FileHelper
{
    #region SaveFile(保存内容到文件)

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
        if (string.IsNullOrWhiteSpace(dir))
            return;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        using var fs = new FileStream(filePath, fileMode.Value);
        using var sw = new StreamWriter(fs, encoding);
        sw.Write(content);
        sw.Flush();
    }

    #endregion

    #region Write(写入文件)

    /// <summary>
    /// 将字符串写入文件，文件不存在则创建
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="content">内容</param>
    public static void Write(string filePath, string content) => Write(filePath, Conv.ToBytes(content));

    /// <summary>
    /// 将流写入文件，文件不存在则创建
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="stream">内容</param>
    public static void Write(string filePath, Stream stream)
    {
        var bytes = ToBytes(stream);
        Write(filePath, bytes);
    }

    /// <summary>
    /// 将字节流写入文件，文件不存在则创建
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="content">内容</param>
    public static void Write(string filePath, byte[] content)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        if (content == null)
            return;
        DirectoryHelper.CreateDirectory(filePath);
        File.WriteAllBytes(filePath, content);
    }

    /// <summary>
    /// 将字节数组写入目标文件
    /// </summary>
    /// <param name="byteArray">字节数组</param>
    /// <param name="targetFilePath">文件的绝对路径</param>
    /// <param name="appendMode">是否追加</param>
    public static bool Write(byte[] byteArray, string targetFilePath, bool appendMode = false)
    {
        if (string.IsNullOrWhiteSpace(targetFilePath))
            return false;

        if (!appendMode && File.Exists(targetFilePath))
            File.Create(targetFilePath);

        var fileMode = appendMode ? FileMode.Append : FileMode.Open;
        using var fs = new FileStream(targetFilePath, fileMode, FileAccess.Write);
        return fs.TryWrite(byteArray, 0, byteArray.Length);
    }

    #endregion

    #region WriteAsync(写入文件)

#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER

    /// <summary>
    /// 将字符串写入文件，文件不存在则创建
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="content">内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task WriteAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        await WriteAsync(filePath, Conv.ToBytes(content), cancellationToken);
    }

    /// <summary>
    /// 将流写入在文件，文件不存在则创建
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="content">内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task WriteAsync(string filePath, Stream content, CancellationToken cancellationToken = default)
    {
        var bytes = await ToBytesAsync(content, cancellationToken);
        await WriteAsync(filePath, bytes, cancellationToken);
    }

    /// <summary>
    /// 将字节流写入文件，文件不存在则创建
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="content">内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task WriteAsync(string filePath, byte[] content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        if (content == null)
            return;
        DirectoryHelper.CreateDirectory(filePath);
        await File.WriteAllBytesAsync(filePath, content, cancellationToken);
    }
#endif

    /// <summary>
    /// 将字节数组写入目标文件
    /// </summary>
    /// <param name="byteArray">字节数组</param>
    /// <param name="filePath">文件的绝对路径</param>
    /// <param name="appendMode">是否追加</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<bool> WriteAsync(byte[] byteArray, string filePath, bool appendMode = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        DirectoryHelper.CreateDirectory(filePath);
        if (!appendMode && File.Exists(filePath))
            File.Create(filePath);

        var fileMode = appendMode ? FileMode.Append : FileMode.Open;
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
        await using var fs = new FileStream(filePath, fileMode, FileAccess.Write);
#else
        using var fs = new FileStream(filePath, fileMode, FileAccess.Write);
#endif
        return await fs.TryWriteAsync(byteArray, 0, byteArray.Length, cancellationToken);
    }

    #endregion
}