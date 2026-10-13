using System.Data;
using System.Xml;
using Bing.Extensions;

namespace Bing.Helpers;

/// <summary>
/// Xml操作 - 生成器
/// </summary>
public partial class Xml
{
    #region 构造函数

    /// <summary>
    /// 初始化一个 <see cref="Xml"/> 类型的实例。
    /// </summary>
    /// <param name="xml">Xml字符串</param>
    public Xml(string xml = null)
    {
        Document = new XmlDocument();
        Document.LoadXml(GetXml(xml));
        Root = Document.DocumentElement;
        if (Root == null)
            throw new ArgumentException(nameof(xml));
    }

    /// <summary>
    /// 初始化一个 <see cref="Xml"/> 类型的实例。
    /// </summary>
    /// <param name="stream">流</param>
    public Xml(Stream stream)
    {
        Document = new XmlDocument();
        Document.Load(stream);
        Root = Document.DocumentElement;
        if (Root == null)
            throw new ArgumentException(nameof(stream));
    }

    /// <summary>
    /// 获取Xml字符串
    /// </summary>
    /// <param name="xml">Xml字符串</param>
    /// <returns>原始 XML 文本；输入为空时返回空的 xml 根元素文本。</returns>
    private string GetXml(string xml) => string.IsNullOrWhiteSpace(xml) ? "<xml></xml>" : xml;

    #endregion

    #region 属性

    /// <summary>
    /// 获取Xml文档
    /// </summary>
    public XmlDocument Document { get; }

    /// <summary>
    /// 获取Xml根节点
    /// </summary>
    public XmlElement Root { get; }

    #endregion

    #region AddNode(添加节点)

    /// <summary>
    /// 添加节点
    /// </summary>
    /// <param name="name">节点名称</param>
    /// <param name="value">值</param>
    /// <param name="parent">父节点</param>
    public XmlNode AddNode(string name, object value = null, XmlNode parent = null)
    {
        var node = CreateNode(name, value, XmlNodeType.Element);
        GetParent(parent).AppendChild(node);
        return node;
    }

    /// <summary>
    /// 获取父节点
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <returns>指定的父节点；未指定时返回根节点。</returns>
    private XmlNode GetParent(XmlNode parent) => parent ?? Root;

    /// <summary>
    /// 创建节点
    /// </summary>
    /// <param name="name">节点名称</param>
    /// <param name="value">值</param>
    /// <param name="type">节点类型</param>
    /// <returns>按指定名称、值和节点类型创建的 XML 节点。</returns>
    private XmlNode CreateNode(string name, object value, XmlNodeType type)
    {
        var node = Document.CreateNode(type, name, string.Empty);
        if (string.IsNullOrWhiteSpace(value.SafeString()) == false)
            node.InnerText = value.SafeString();
        return node;
    }

    #endregion

    #region AddCDataNode(添加CDATA节点)

    /// <summary>
    /// 添加CDATA节点
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="parent">父节点</param>
    public XmlNode AddCDataNode(object value, XmlNode parent = null)
    {
        var node = CreateNode(CreateId(), value, XmlNodeType.CDATA);
        GetParent(parent).AppendChild(node);
        return node;
    }

    /// <summary>
    /// 添加CDATA节点
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="parentName">父节点名称</param>
    public XmlNode AddCDataNode(object value, string parentName)
    {
        var parent = CreateNode(parentName, null, XmlNodeType.Element);
        Root.AppendChild(parent);
        return AddCDataNode(value, parent);
    }

    /// <summary>
    /// 创建标识
    /// </summary>
    /// <returns>新生成的 GUID 文本。</returns>
    private string CreateId() => Guid.NewGuid().ToString("N");

    #endregion

    #region UpdateNode(更新节点)

    /// <summary>
    /// 更新节点
    /// </summary>
    /// <param name="xmlPathNode">Xml路径节点</param>
    /// <param name="content">内容</param>
    public void UpdateNode(string xmlPathNode, string content)
    {
        var node = Root.SelectSingleNode(xmlPathNode);
        if (node == null)
            return;
        node.InnerText = content;
    }

    #endregion

    #region DeleteNode(删除节点)

    /// <summary>
    /// 删除节点
    /// </summary>
    /// <param name="node">节点</param>
    public void DeleteNode(string node)
    {
        var mainNode = node.Substring(0, node.LastIndexOf("/", StringComparison.Ordinal));
        var currentNode = Root.SelectSingleNode(node);
        if (currentNode == null)
            return;
        Root.SelectSingleNode(mainNode)?.RemoveChild(currentNode);
    }

    #endregion

    #region SelectNodes(选择节点列表)

    /// <summary>
    /// 选择节点列表。匹配 XPath表达式的节点列表
    /// </summary>
    /// <param name="xpath">XPath表达式</param>
    public XmlNodeList SelectNodes(string xpath) => Root.SelectNodes(xpath);

    #endregion

    #region GetDataView(获取数据视图)

    /// <summary>
    /// 获取指定路径的数据视图
    /// </summary>
    /// <param name="xmlPathNode">Xml路径节点</param>
    public DataView GetDataView(string xmlPathNode)
    {
        using (var ds = new DataSet())
        {
            var node = Root.SelectSingleNode(xmlPathNode);
            if (node == null)
                return null;
            using (var reader = new StringReader(node.OuterXml))
            {
                ds.ReadXml(reader);
                return ds.Tables.Count <= 0 ? null : ds.Tables[0].DefaultView;
            }
        }
    }

    #endregion

    #region GetAllDataView(获取所有数据视图)

    /// <summary>
    /// 获取指定路径所有数据视图
    /// </summary>
    /// <param name="xmlPathNode">Xml路径节点</param>
    public DataView GetAllDataView(string xmlPathNode)
    {
        using (var ds = new DataSet())
        {
            using (var nodes = Root.SelectNodes(xmlPathNode))
            {
                if (nodes == null)
                    return null;
                foreach (XmlNode node in nodes)
                {
                    using (var reader = new StringReader(node.OuterXml))
                        ds.ReadXml(reader);
                }
                return ds.Tables.Count <= 0 ? null : ds.Tables[0].DefaultView;
            }
        }
    }

    #endregion

    #region ToString(输出Xml)

    /// <inheritdoc />
    public override string ToString() => Document.OuterXml;

    #endregion
}
