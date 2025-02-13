using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Bing.IO;

namespace Bing.Helpers;

/// <summary>
/// Xml操作 - 工具
/// </summary>
public partial class Xml
{
    /// <summary>
    /// 将Xml字符串转换为XDocument
    /// </summary>
    /// <param name="xml">Xml字符串</param>
    public static XDocument ToDocument(string xml) => XDocument.Parse(xml);

    /// <summary>
    /// 将Xml字符串转换为XELement列表
    /// </summary>
    /// <param name="xml">Xml字符串</param>
    public static List<XElement> ToElements(string xml)
    {
        var document = ToDocument(xml);
        return document?.Root == null ? new List<XElement>() : document.Root.Elements().ToList();
    }

    /// <summary>
    /// 校验Xml字符串是否符合指定Xml架构文件
    /// </summary>
    /// <param name="xmlFile">Xml文件</param>
    /// <param name="schemaFile">架构文件</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static void Validate(string xmlFile, string schemaFile)
    {
        if (string.IsNullOrWhiteSpace(xmlFile))
            throw new ArgumentNullException(nameof(xmlFile));
        if (string.IsNullOrWhiteSpace(schemaFile))
            throw new ArgumentNullException(nameof(schemaFile));
        XmlReader reader = null;
        try
        {
            var result = new Tuple<bool, string>(true, string.Empty);
            var settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.Schemas.Add(null, schemaFile);
            // 设置验证Xml出错时的事件
            settings.ValidationEventHandler += (obj, e) =>
            {
                result = new Tuple<bool, string>(false, e.Message);
            };
            using (reader = XmlReader.Create(xmlFile, settings))
            {
                while (reader.Read()) { }
            }
            if (!result.Item1)
                throw new ArgumentException(result.Item2);
        }
        finally
        {
            reader?.Close();
        }
    }

    /// <summary>
    /// 加载Xml文件到XDocument
    /// </summary>
    /// <param name="filePath">Xml文件绝对路径</param>
    public static Task<XDocument> LoadFileToDocumentAsync(string filePath) => LoadFileToDocumentAsync(filePath, Encoding.UTF8);

    /// <summary>
    /// 加载Xml文件到XDocument
    /// </summary>
    /// <param name="filePath">Xml文件绝对路径</param>
    /// <param name="encoding">字符编码</param>
    public static async Task<XDocument> LoadFileToDocumentAsync(string filePath, Encoding encoding)
    {
        var xml = await FileHelper.ReadToStringAsync(filePath, encoding);
        return ToDocument(xml);
    }

    /// <summary>
    /// 加载Xml文件到XElement列表
    /// </summary>
    /// <param name="filePath">Xml文件绝对路径</param>
    public static Task<List<XElement>> LoadFileToElementsAsync(string filePath) => LoadFileToElementsAsync(filePath, Encoding.UTF8);

    /// <summary>
    /// 加载Xml文件到XElement列表
    /// </summary>
    /// <param name="filePath">Xml文件绝对路径</param>
    /// <param name="encoding">字符编码</param>
    public static async Task<List<XElement>> LoadFileToElementsAsync(string filePath, Encoding encoding)
    {
        var xml = await FileHelper.ReadToStringAsync(filePath, encoding);
        return ToElements(xml);
    }
}