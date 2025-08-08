namespace Bing.Helpers;

/// <summary>
/// GZip压缩 操作
/// </summary>
[Obsolete("建议使用 Compression 类获得更完整的功能和更好的异常处理")]
public static partial class GZip
{
    #region Compress(压缩)

    /// <summary>
    /// 压缩字符串（使用 UTF-8 编码）
    /// </summary>
    /// <param name="content">待压缩的内容</param>
    /// <returns>压缩后的 Base64 编码字符串</returns>
    public static string Compress(string content) => Compression.Compress(content, Encoding.UTF8);

    /// <summary>
    /// 异步压缩字符串（使用 UTF-8 编码）
    /// </summary>
    /// <param name="content">待压缩的内容</param>
    /// <returns>压缩后的 Base64 编码字符串</returns>
    public static async Task<string> CompressAsync(string content) => await Compression.CompressAsync(content, Encoding.UTF8);

    /// <summary>
    /// 压缩字符串（指定编码）
    /// </summary>
    /// <param name="content">待压缩的内容</param>
    /// <param name="encoding">字符编码</param>
    /// <returns>压缩后的 Base64 编码字符串</returns>
    public static string Compress(string content, Encoding encoding) =>
        Compression.Compress(content, encoding);

    /// <summary>
    /// 异步压缩字符串（指定编码）
    /// </summary>
    /// <param name="content">待压缩的内容</param>
    /// <param name="encoding">字符编码</param>
    /// <returns>压缩后的 Base64 编码字符串</returns>
    public static async Task<string> CompressAsync(string content, Encoding encoding) =>
        await Compression.CompressAsync(content, encoding);

    /// <summary>
    /// 压缩字节数组
    /// </summary>
    /// <param name="buffer">待压缩的字节数组</param>
    /// <returns>压缩后的字节数组</returns>
    public static byte[] Compress(byte[] buffer) => Compression.Compress(buffer);

    /// <summary>
    /// 异步压缩字节数组
    /// </summary>
    /// <param name="buffer">待压缩的字节数组</param>
    /// <returns>压缩后的字节数组</returns>
    public static async Task<byte[]> CompressAsync(byte[] buffer) =>
        await Compression.CompressAsync(buffer);

    /// <summary>
    /// 压缩流
    /// </summary>
    /// <param name="stream">待压缩的流</param>
    /// <returns>压缩后的字节数组</returns>
    public static byte[] Compress(Stream stream) => Compression.Compress(stream);

    /// <summary>
    /// 异步压缩流
    /// </summary>
    /// <param name="stream">待压缩的流</param>
    /// <returns>压缩后的字节数组</returns>
    public static async Task<byte[]> CompressAsync(Stream stream) =>
        await Compression.CompressAsync(stream);

    #endregion

    #region Decompress(解压缩)

    /// <summary>
    /// 解压缩 Base64 编码的字符串
    /// </summary>
    /// <param name="content">Base64 编码的压缩内容</param>
    /// <returns>解压后的原始字符串</returns>
    public static string Decompress(string content) => Compression.Decompress(content);

    /// <summary>
    /// 异步解压缩 Base64 编码的字符串
    /// </summary>
    /// <param name="content">Base64 编码的压缩内容</param>
    /// <returns>解压后的原始字符串</returns>
    public static async Task<string> DecompressAsync(string content) =>
        await Compression.DecompressAsync(content);

    /// <summary>
    /// 解压缩字节数组
    /// </summary>
    /// <param name="buffer">压缩的字节数组</param>
    /// <returns>解压后的字节数组</returns>
    public static byte[] Decompress(byte[] buffer) => Compression.Decompress(buffer);

    /// <summary>
    /// 解压缩流
    /// </summary>
    /// <param name="stream">压缩流</param>
    /// <returns>解压后的字节数组</returns>
    public static byte[] Decompress(Stream stream) => Compression.Decompress(stream);

    /// <summary>
    /// 解压缩流（指定编码）
    /// </summary>
    /// <param name="stream">压缩流</param>
    /// <param name="encoding">字符编码</param>
    /// <returns>解压后的字节数组</returns>
    public static byte[] Decompress(Stream stream, Encoding encoding)
    {
        var decompressedBytes = Compression.Decompress(stream);
        var text = encoding.GetString(decompressedBytes);
        return encoding.GetBytes(text);
    }

    #endregion
}