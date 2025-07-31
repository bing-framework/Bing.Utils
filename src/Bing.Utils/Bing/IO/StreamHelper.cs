namespace Bing.IO;

/// <summary>
/// 流操作辅助类
/// </summary>
public static class StreamHelper
{
    /// <summary>
    /// 从字符串生成内存流
    /// </summary>
    /// <param name="content">要转换为流的字符串内容</param>
    /// <param name="encoding">字符编码，如不指定则使用 UTF-8</param>
    /// <returns>包含字符串内容的内存流，流位置已重置为开始位置</returns>
    /// <exception cref="ArgumentNullException">当内容为 null 时抛出</exception>
    public static MemoryStream GenerateStreamFromString(string content, Encoding encoding = null)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));
        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(content);
        var stream = new MemoryStream();
        if (bytes.Length > 0)
            stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;
        return stream;
    }
}