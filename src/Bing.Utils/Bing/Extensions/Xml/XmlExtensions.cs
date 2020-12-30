using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

// ReSharper disable once CheckNamespace
namespace Bing.Extensions
{
    /// <summary>
    /// Xml 扩展
    /// </summary>
    public static class XmlExtensions
    {
        #region FromXml(将XML转换为Object)

        /// <summary>
        /// 将XML转换为Object
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="xml">XML字符串</param>
        public static T FromXml<T>(this string xml) where T : new()
        {
            using var sr = new StringReader(xml);
            var xmlSerializer = new XmlSerializer(typeof(T));
            return (T)xmlSerializer.Deserialize(sr);
        }

        #endregion

        #region Deserialize(反序列化指定的XML文档)

        /// <summary>
        /// 反序列化指定的XML文档
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="xmlDocument">Xml文档</param>
        public static T Deserialize<T>(this XDocument xmlDocument)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using var reader = xmlDocument.CreateReader();
            return (T) xmlSerializer.Deserialize(reader);
        }

        #endregion

        #region ToXElement(将XmlNode转换为XElement)

        /// <summary>
        /// 将XmlNode转换为XElement
        /// </summary>
        /// <param name="node">Xml节点</param>
        public static XElement ToXElement(this XmlNode node)
        {
            var xmlDocument = new XDocument();
            using var xmlWriter = xmlDocument.CreateWriter();
            node.WriteTo(xmlWriter);
            return xmlDocument.Root;
        }

        #endregion

        #region ToXmlNode(将XElement转换为XmlNode)

        /// <summary>
        /// 将XElement转换为XmlNode
        /// </summary>
        /// <param name="element">Xml元素</param>
        public static XmlNode ToXmlNode(this XElement element)
        {
            using var xmlReader = element.CreateReader();
            var xml = new XmlDocument();
            xml.Load(xmlReader);
            return xml;
        }

        #endregion
    }
}
