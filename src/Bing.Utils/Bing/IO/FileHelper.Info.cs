using System.Diagnostics;
using System.Text;
using Bing.Helpers;

namespace Bing.IO;

/// <summary>
/// 文件操作辅助类 - 信息
/// </summary>
public static partial class FileHelper
{
    #region GetExtension(获取文件扩展名)

    /// <summary>
    /// 获取文件扩展名。例如：a.txt => txt
    /// </summary>
    /// <param name="fileNameWithExtension">文件名。包含扩展名</param>
    public static string GetExtension(string fileNameWithExtension)
    {
        Check.NotNull(fileNameWithExtension, nameof(fileNameWithExtension));

        // Path.GetExtension(fileNameWithExtension);
        var lastDotIndex = fileNameWithExtension.LastIndexOf('.');
        if (lastDotIndex < 0)
            return string.Empty;
        return fileNameWithExtension.Substring(lastDotIndex + 1);
    }

    #endregion

    #region GetContentType(根据扩展名获取文件内容类型)

    /// <summary>
    /// 根据扩展名获取文件内容类型
    /// </summary>
    /// <param name="ext">扩展名</param>
    public static string GetContentType(string ext)
    {
        var dict = Const.FileExtensionDict;
        ext = ext.ToLower();
        if (!ext.StartsWith(".")) 
            ext = "." + ext;
        dict.TryGetValue(ext, out var contentType);
        return contentType;
    }

    #endregion

    #region GetFileSize(获取文件大小)

    /// <summary>
    /// 获取文件大小
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns></returns>
    public static FileSize GetFileSize(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        return GetFileSize(new System.IO.FileInfo(filePath));
    }

    /// <summary>
    /// 获取文件大小
    /// </summary>
    /// <param name="fileInfo">文件信息</param>
    /// <returns></returns>
    public static FileSize GetFileSize(System.IO.FileInfo fileInfo)
    {
        if (fileInfo == null)
            throw new ArgumentNullException(nameof(fileInfo));
        return new FileSize(fileInfo.Length);
    }

    #endregion

    #region GetVersion(获取文件版本号)

    /// <summary>
    /// 获取文件版本号
    /// </summary>
    /// <param name="fileName">完整文件名</param>
    /// <returns></returns>
    public static string GetVersion(string fileName)
    {
        if (File.Exists(fileName))
        {
            var fvi = FileVersionInfo.GetVersionInfo(fileName);
            return fvi.FileVersion;
        }

        return null;
    }

    #endregion

    #region GetEncoding(获取文件的编码格式)

    /// <summary>
    /// 获取文件编码
    /// </summary>
    /// <param name="filePath">文件绝对路径</param>
    /// <returns>字符编码</returns>
    public static Encoding GetEncoding(string filePath) => GetEncoding(filePath, Encoding.Default);

    /// <summary>
    /// 获取文件编码
    /// </summary>
    /// <param name="filePath">文件绝对路径</param>
    /// <param name="defaultEncoding">默认编码</param>
    /// <returns>字符编码</returns>
    public static Encoding GetEncoding(string filePath, Encoding defaultEncoding)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"文件\"{filePath}\"不存在");

        var targetEncoding = defaultEncoding;
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4);
        if (fs.Length >= 2)
        {
            var pos = fs.Position;
            fs.Position = 0;
            var buffer = new int[4];
            buffer[0] = fs.ReadByte();
            buffer[1] = fs.ReadByte();
            buffer[2] = fs.ReadByte();
            buffer[3] = fs.ReadByte();
            fs.Position = pos;

            // 根据文件流的前4个字节判断Encoding Unicode {0xFF,  0xFE}; BE-Unicode  {0xFE,  0xFF}; UTF8  =  {0xEF,  0xBB,  0xBF};
            if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                targetEncoding = Encoding.BigEndianUnicode;
            if (buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] != 0xFF)
                targetEncoding = Encoding.Unicode;
            if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                targetEncoding = Encoding.UTF8;
        }

        return targetEncoding;
    }

    /// <summary>
    /// 获取文件流的编码格式
    /// </summary>
    /// <param name="fs">文件流</param>
    /// <returns>字符编码</returns>
    public static Encoding GetEncoding(FileStream fs)
    {
        if (fs == null)
            throw new ArgumentNullException(nameof(fs));
        var targetEncoding = Encoding.Default;
        using var br = new BinaryReader(fs, Encoding.Default);
        var buffer = br.ReadBytes(3);
        if (buffer[0] >= 0xEF)
        {
            if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                targetEncoding = Encoding.BigEndianUnicode;
            if (buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] != 0xFF)
                targetEncoding = Encoding.Unicode;
            if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                targetEncoding = Encoding.UTF8;
        }
        return targetEncoding;
    }

    /// <summary>
    /// 获取字节数组的字符编码，通过分析其字节顺序标记（BOM）确定文本文件的编码。当文本文件的字节序检测失败时，默认为 ASCII。
    /// </summary>
    /// <param name="data">字节数组</param>
    /// <returns>字符编码</returns>
    public static Encoding GetEncoding(byte[] data)
    {
        if (data == null || data.Length < 3)
            return Encoding.ASCII;
        // 读取BOM
        var bom = data;
        // UTF7
        if (bom[0] == 0x2B && bom[1] == 0x2F && bom[2] == 0x76)
            return Encoding.UTF7;
        // UTF8 带BOM的UTF8
        if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            return Encoding.UTF8;
        // UTF-16LE
        if (bom[0] == 0xFF && bom[1] == 0xFE && (bom.Length < 4 || bom[2] != 0 || bom[3] != 0))
            return Encoding.Unicode;
        // UTF-16bE
        if (bom[0] == 0XFE && bom[1] == 0XFF)
            return Encoding.BigEndianUnicode;
        // UTF-32
        if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xFE && bom[3] == 0xFF)
            return Encoding.UTF32;
        if(IsUTF8Bytes(bom))
            return Encoding.UTF8;
        return Encoding.Default;
    }

    /// <summary>
    /// 判断是否不带BOM的UTF8格式
    /// </summary>
    /// <param name="data">字节数组</param>
    private static bool IsUTF8Bytes(byte[] data)
    {
        // 计算当前正分析的字符应还有的字节数
        int charByteCounter = 1;
        // 当前分析的字节
        byte curByte;
        for (int i = 0; i < data.Length; i++)
        {
            curByte = data[i];
            if (charByteCounter == 1)
            {
                if (curByte >= 0x80)
                {
                    // 判断当前
                    while (((curByte <<= 1) & 0x80) != 0)
                        charByteCounter++;
                    // 标记位首位若为非0，则至少以2个1开始。例如：110XXXXX...........1111110X
                    if (charByteCounter == 1 || charByteCounter > 0)
                        return false;
                }
            }
            else
            {
                // 若是UTF-8，此时第一位必须为0
                if ((curByte & 0xC0) != 0x80)
                    return false;
                charByteCounter--;
            }
        }

        if (charByteCounter > 1)
            throw new Exception("非预期的byte格式");
        return true;
    }

    #endregion

    #region GetFileName(获取文件名称)

    /// <summary>
    /// 获取文件名称
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>文件名.扩展名</returns>
    public static string GetFileName(string path) => Path.GetFileName(path);

    #endregion
}