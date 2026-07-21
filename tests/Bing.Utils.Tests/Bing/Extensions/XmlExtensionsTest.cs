using System.Text;
using Bing.Extensions;
using Bing.Helpers;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// <see cref="XmlExtensions"/> 单元测试
/// </summary>
public class XmlExtensionsTest : IDisposable
{
    /// <summary>
    /// 测试目录。
    /// </summary>
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), $"XmlExtensionsTest_{Guid.NewGuid():N}");

    /// <summary>
    /// 初始化测试目录。
    /// </summary>
    public XmlExtensionsTest() => Directory.CreateDirectory(_testDirectory);

    /// <summary>
    /// 测试目的：XML 字符串应能反序列化为对象。
    /// </summary>
    [Fact]
    public void FromXml_ValidContent_ReturnsObject()
    {
        // Arrange
        const string xml = "<XmlSerializationSample><Name>张三</Name></XmlSerializationSample>";

        // Act
        var result = xml.FromXml<XmlSerializationSample>();

        // Assert
        result.Name.ShouldBe("张三");
    }

    /// <summary>
    /// 测试目的：调用方传入的流在反序列化后不应被关闭。
    /// </summary>
    [Fact]
    public void DeserializeXml_Stream_KeepsCallerStreamOpen()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("<XmlSerializationSample><Name>stream</Name></XmlSerializationSample>");
        using var stream = new MemoryStream(bytes);

        // Act
        var result = stream.DeserializeXml<XmlSerializationSample>();

        // Assert
        result.Name.ShouldBe("stream");
        stream.CanRead.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：调用方传入的读取器在反序列化后不应被关闭。
    /// </summary>
    [Fact]
    public void DeserializeXml_TextReader_KeepsCallerReaderOpen()
    {
        // Arrange
        using var reader = new StringReader("<XmlSerializationSample><Name>reader</Name></XmlSerializationSample>");

        // Act
        var result = reader.DeserializeXml<XmlSerializationSample>();

        // Assert
        result.Name.ShouldBe("reader");
        Should.NotThrow(reader.ReadToEnd);
    }

    /// <summary>
    /// 测试目的：带 UTF-8 BOM 的 XML 文件应正确读取。
    /// </summary>
    [Fact]
    public void FromXmlFile_Utf8BomFile_ReturnsObject()
    {
        // Arrange
        var fileName = Path.Combine(_testDirectory, "utf8-bom.xml");
        File.WriteAllText(fileName, "<XmlSerializationSample><Name>bom</Name></XmlSerializationSample>", new UTF8Encoding(true));

        // Act
        var result = Serialize.FromXmlFile<XmlSerializationSample>(fileName);

        // Assert
        result.Name.ShouldBe("bom");
    }

    /// <summary>
    /// 测试目的：无 BOM 的非 UTF-8 文件应使用调用方指定的回退编码读取。
    /// </summary>
    [Fact]
    public void FromXmlFile_NonUtf8FileWithFallbackEncoding_ReturnsObject()
    {
        // Arrange
        var fileName = Path.Combine(_testDirectory, "latin1.xml");
        var encoding = Encoding.Unicode;
        File.WriteAllBytes(fileName, encoding.GetBytes("<XmlSerializationSample><Name>café</Name></XmlSerializationSample>"));

        // Act
        var result = Serialize.FromXmlFile<XmlSerializationSample>(fileName, encoding);

        // Assert
        result.Name.ShouldBe("café");
    }

    /// <summary>
    /// 测试目的：包含 DTD 的 XML 应被拒绝，避免外部实体解析。
    /// </summary>
    [Fact]
    public void DeserializeXml_DtdContent_ThrowsInvalidOperationException()
    {
        // Arrange
        const string xml = "<!DOCTYPE XmlSerializationSample [<!ELEMENT XmlSerializationSample ANY>]><XmlSerializationSample><Name>safe</Name></XmlSerializationSample>";
        using var reader = new StringReader(xml);

        // Act and Assert
        Should.Throw<InvalidOperationException>(() => reader.DeserializeXml<XmlSerializationSample>());
    }

    /// <summary>
    /// 测试目的：不存在的文件应保留文件未找到异常。
    /// </summary>
    [Fact]
    public void FromXmlFile_MissingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var fileName = Path.Combine(_testDirectory, "missing.xml");

        // Act and Assert
        Should.Throw<FileNotFoundException>(() => Serialize.FromXmlFile<XmlSerializationSample>(fileName));
    }

    /// <summary>
    /// 释放测试资源。
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }
}

/// <summary>
/// XML 测试对象。
/// </summary>
public class XmlSerializationSample
{
    /// <summary>
    /// 名称。
    /// </summary>
    public string Name { get; set; }
}