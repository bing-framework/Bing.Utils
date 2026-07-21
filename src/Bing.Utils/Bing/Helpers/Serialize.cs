using System.Xml;
using System.Xml.Serialization;
using Bing.Extensions;

namespace Bing.Helpers;

/// <summary>
/// 序列化操作辅助类。
/// </summary>
/// <remarks>
/// 普通对象的二进制字节转换应使用 DataContract Binary XML；跨平台、缓存、消息传递和可调试存储应使用 <see cref="Json.ToBytes{T}(T,System.Text.Json.JsonSerializerOptions)"/>
/// 与 <see cref="Json.ToObject{T}(byte[],System.Text.Json.JsonSerializerOptions)"/>；结构体内存布局转换仅适用于不包含托管引用的结构体。
/// </remarks>
public static partial class Serialize
{
    #region Xml序列化

    /// <summary>
    /// 将对象序列化为XML字符串。
    /// </summary>
    /// <param name="data">要序列化的对象</param>
    /// <param name="encoding">编码格式，默认为UTF-8</param>
    /// <returns>XML字符串</returns>
    /// <exception cref="ArgumentNullException">当数据为null时抛出</exception>
    public static string ToXml(object data, Encoding encoding = null)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "要序列化的数据不能为null");
        encoding ??= Encoding.UTF8;

        using var ms = new MemoryStream();
        using var writer = new XmlTextWriter(ms, encoding)
        {
            Formatting = Formatting.Indented
        };

        var serializer = new XmlSerializer(data.GetType());
        var ns = new XmlSerializerNamespaces();
        ns.Add("", ""); // 移除默认命名空间

        serializer.Serialize(writer, data, ns);
        ms.Position = 0;

        using var reader = new StreamReader(ms, encoding);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// 将XML字符串反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="xml">XML字符串</param>
    /// <param name="encoding">编码格式，默认为UTF-8</param>
    /// <returns>反序列化后的对象</returns>
    /// <exception cref="ArgumentException">当XML字符串为null或空时抛出</exception>
    public static T FromXml<T>(string xml, Encoding encoding = null)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new ArgumentException("XML字符串不能为null或空", nameof(xml));

        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(xml);

        using var ms = new MemoryStream(bytes);
        return ms.DeserializeXml<T>();
    }

    /// <summary>
    /// 将对象序列化为XML并写入文件。
    /// </summary>
    /// <param name="fileName">文件路径</param>
    /// <param name="data">要序列化的数据</param>
    /// <param name="encoding">编码格式，默认为UTF-8</param>
    /// <exception cref="ArgumentException">当文件名为null或空时抛出</exception>
    /// <exception cref="ArgumentNullException">当数据为null时抛出</exception>
    public static void ToXmlFile(string fileName, object data, Encoding encoding = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为null或空", nameof(fileName));
        if (data == null)
            throw new ArgumentNullException(nameof(data), "要序列化的数据不能为null");

        encoding ??= Encoding.UTF8;

        // 确保目录存在
        var directory = Path.GetDirectoryName(fileName);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        using var writer = new XmlTextWriter(fs, encoding)
        {
            Formatting = Formatting.Indented
        };

        var serializer = new XmlSerializer(data.GetType());
        var ns = new XmlSerializerNamespaces();
        ns.Add("", ""); // 移除默认命名空间

        serializer.Serialize(writer, data, ns);
    }

    /// <summary>
    /// 从XML文件中反序列化对象。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="fileName">文件路径</param>
    /// <param name="encoding">文件不含 BOM 或 XML 编码声明时使用的回退编码，默认为 UTF-8。</param>
    /// <returns>反序列化后的对象</returns>
    /// <exception cref="ArgumentException">当文件名为null或空时抛出</exception>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    public static T FromXmlFile<T>(string fileName, Encoding encoding = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为null或空", nameof(fileName));
        if (!File.Exists(fileName))
            throw new FileNotFoundException($"文件不存在: {fileName}");

        encoding ??= Encoding.UTF8;
        using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(fs, encoding, detectEncodingFromByteOrderMarks: true);
        return reader.DeserializeXml<T>();
    }

    #endregion
}