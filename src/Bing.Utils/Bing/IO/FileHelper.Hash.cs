using System.Security.Cryptography;

namespace Bing.IO;

/// <summary>
/// 文件操作辅助类 - MD5
/// </summary>
public static partial class FileHelper
{
    #region GetMd5(获取文件的MD5值)

    /// <summary>
    /// 获取文件的MD5值
    /// </summary>
    /// <param name="file">文件</param>
    public static string GetMd5(string file) => HashFile(file, nameof(MD5));

    /// <summary>
    /// 计算文件的哈希值
    /// </summary>
    /// <param name="file">文件</param>
    /// <param name="algName">算法名。例如：md5,sha1</param>
    /// <returns>哈希值16进制字符串</returns>
    private static string HashFile(string file, string algName)
    {
        if (!File.Exists(file))
            return string.Empty;
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
        var bytes = HashData(fs, algName);
        return ToHexString(bytes);
    }

    /// <summary>
    /// 计算哈希值
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="algName">算法名。例如：md5,sha1</param>
    /// <returns>哈希值字节数组</returns>
    private static byte[] HashData(Stream stream, string algName = nameof(MD5))
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (string.IsNullOrWhiteSpace(algName))
            throw new ArgumentNullException(nameof(algName));

        stream.Seek(0, SeekOrigin.Begin);
        using HashAlgorithm algorithm = algName switch
        {
            nameof(SHA1) => SHA1.Create(),
            nameof(SHA256) => SHA256.Create(),
            nameof(SHA512) => SHA512.Create(),
            _ => MD5.Create()
        };
        var bs = new BufferedStream(stream, 1048576);
        return algorithm.ComputeHash(bs);
    }

    /// <summary>
    /// 将字节数组转换为16进制表示的字符在
    /// </summary>
    /// <param name="bytes">字节数组</param>
    private static string ToHexString(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "");

    #endregion

    #region GetSha1(获取文件的SHA1值)

    /// <summary>
    /// 获取文件的SHA1值
    /// </summary>
    /// <param name="file">文件</param>
    public static string GetSha1(string file) => HashFile(file, nameof(SHA1));

    #endregion

    #region GetSha256(获取文件的SHA256值)

    /// <summary>
    /// 获取文件的SHA256值
    /// </summary>
    /// <param name="file">文件</param>
    public static string GetSha256(string file) => HashFile(file, nameof(SHA256));

    #endregion

    #region GetSha512(获取文件的SHA512值)

    /// <summary>
    /// 获取文件的SHA512值
    /// </summary>
    /// <param name="file">文件</param>
    public static string GetSha512(string file) => HashFile(file, nameof(SHA512));

    #endregion
}