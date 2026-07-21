using System.Xml;
using System.Xml.Serialization;

// ReSharper disable once CheckNamespace
namespace Bing.Extensions;

/// <summary>
/// XML 反序列化扩展
/// </summary>
public static partial class XmlExtensions
{
    /// <summary>
    /// 从流中反序列化 XML。
    /// </summary>
    /// <typeparam name="T">目标对象类型。</typeparam>
    /// <param name="stream">包含 XML 内容的可读流。</param>
    /// <returns>反序列化后的对象。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="stream"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="stream"/> 不可读时抛出。</exception>
    /// <remarks>
    /// XML 读取器禁用 DTD 和外部 XML 解析器。该方法不会关闭或释放调用方传入的流，
    /// 并由 XML 声明或 BOM 检测输入编码。
    /// </remarks>
    /// <example>
    /// <code>
    /// using var stream = File.OpenRead("settings.xml");
    /// var settings = stream.DeserializeXml&lt;Settings&gt;();
    /// </code>
    /// </example>
    public static T DeserializeXml<T>(this Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead)
            throw new ArgumentException("XML 流必须可读。", nameof(stream));

        var serializer = new XmlSerializer(typeof(T));
        return Deserialize<T>(serializer, stream);
    }

    /// <summary>
    /// 从文本读取器中反序列化 XML。
    /// </summary>
    /// <typeparam name="T">目标对象类型。</typeparam>
    /// <param name="reader">包含 XML 内容的文本读取器。</param>
    /// <returns>反序列化后的对象。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="reader"/> 为 <c>null</c> 时抛出。</exception>
    /// <remarks>
    /// XML 读取器禁用 DTD 和外部 XML 解析器。该方法不会关闭或释放调用方传入的读取器；
    /// 调用方负责选择并管理文本编码。
    /// </remarks>
    /// <example>
    /// <code>
    /// using var reader = new StringReader("&lt;Settings /&gt;");
    /// var settings = reader.DeserializeXml&lt;Settings&gt;();
    /// </code>
    /// </example>
    public static T DeserializeXml<T>(this TextReader reader)
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));

        var serializer = new XmlSerializer(typeof(T));
        return Deserialize<T>(serializer, reader);
    }

    /// <summary>
    /// 使用受限 XML 读取器反序列化流。
    /// </summary>
    /// <typeparam name="T">目标对象类型。</typeparam>
    /// <param name="serializer">XML 序列化器。</param>
    /// <param name="stream">XML 流。</param>
    /// <returns>反序列化后的对象。</returns>
    private static T Deserialize<T>(XmlSerializer serializer, Stream stream)
    {
        using var reader = XmlReader.Create(stream, CreateSecureReaderSettings());
        return (T)serializer.Deserialize(reader);
    }

    /// <summary>
    /// 使用受限 XML 读取器反序列化文本读取器。
    /// </summary>
    /// <typeparam name="T">目标对象类型。</typeparam>
    /// <param name="serializer">XML 序列化器。</param>
    /// <param name="textReader">XML 文本读取器。</param>
    /// <returns>反序列化后的对象。</returns>
    private static T Deserialize<T>(XmlSerializer serializer, TextReader textReader)
    {
        using var reader = XmlReader.Create(textReader, CreateSecureReaderSettings());
        return (T)serializer.Deserialize(reader);
    }

    /// <summary>
    /// 使用受限 XML 读取器反序列化 XML 读取器。
    /// </summary>
    /// <typeparam name="T">目标对象类型。</typeparam>
    /// <param name="serializer">XML 序列化器。</param>
    /// <param name="xmlReader">XML 读取器。</param>
    /// <returns>反序列化后的对象。</returns>
    private static T Deserialize<T>(XmlSerializer serializer, XmlReader xmlReader) => (T)serializer.Deserialize(xmlReader);

    /// <summary>
    /// 创建禁止 DTD 与外部实体解析的 XML 读取器设置。
    /// </summary>
    /// <returns>安全的 XML 读取器设置。</returns>
    private static XmlReaderSettings CreateSecureReaderSettings() => new()
    {
        CloseInput = false,
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null
    };
}