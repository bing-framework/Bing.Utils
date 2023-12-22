namespace Bing.IO;

/// <summary>
/// 文件流(<see cref="FileStream"/>) 扩展
/// </summary>
public static class FileStreamExtensions
{
    /// <summary>
    /// 读取所有行
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="encoding">字符编码</param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    public static List<string> ReadAllLines(this FileStream stream, Encoding encoding, bool closeAfter = true)
    {
        var list = new List<string>();
        var reader = new StreamReader(stream, encoding);
        while (reader.ReadLine() is {} str) 
            list.Add(str);
        if (closeAfter)
        {
            reader.Close();
            reader.Dispose();
            stream.Close();
            stream.Dispose();
        }
        return list;
    }
}