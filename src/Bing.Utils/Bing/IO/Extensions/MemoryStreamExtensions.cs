// ReSharper disable once CheckNamespace
namespace Bing.IO;

/// <summary>
/// 内存流(<see cref="MemoryStream"/>) 扩展
/// </summary>
public static class MemoryStreamExtensions
{
    /// <summary>
    /// 将内存流内容转换为字符串
    /// </summary>
    /// <param name="ms">内存流</param>
    /// <param name="encoding">字符编码，默认值：UTF-8</param>
    /// <returns>内存流中的字符串</returns>
    /// <exception cref="ArgumentNullException">当内存流为 null 时抛出</exception>
    public static string AsString(this MemoryStream ms, Encoding encoding = null)
    {
        if (ms == null)
            throw new ArgumentNullException(nameof(ms));
        encoding ??= Encoding.UTF8;
        return encoding.GetString(ms.ToArray());
    }

    /// <summary>
    /// 将字符串写入到内存流中
    /// </summary>
    /// <param name="ms">内存流</param>
    /// <param name="input">输入值</param>
    /// <param name="encoding">字符编码，默认值：UTF-8</param>
    /// <param name="append">是否追加到现有内容，默认清空后写入</param>
    /// <exception cref="ArgumentNullException">当内存流或输入字符串为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当内存流不可写时抛出</exception>
    public static void FromString(this MemoryStream ms, string input, Encoding encoding = null, bool append = false)
    {
        if (ms == null)
            throw new ArgumentNullException(nameof(ms));
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (!ms.CanWrite)
            throw new InvalidOperationException("内存流不支持写入操作");
        encoding ??= Encoding.UTF8;
        if (!append)
            ms.SetLength(0); // 清空现有内容
        var buffer = encoding.GetBytes(input);
        ms.Write(buffer, 0, buffer.Length);
    }
}