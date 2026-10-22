using System.Xml;

namespace Bing.Comments;

/// <summary>
/// 表示未能解析的 inheritdoc 节点快照。
/// </summary>
public sealed class CsCommentInheritdoc
{
    internal CsCommentInheritdoc(XmlNode node)
    {
        Cref = node.Attributes?["cref"]?.Value;
        Path = node.Attributes?["path"]?.Value;
        RawXml = node.OuterXml;
        Element = new CsCommentElement(node);
    }

    /// <summary>
    /// 获取节点中未经解析的 cref 属性值。
    /// </summary>
    public string Cref { get; }

    /// <summary>
    /// 获取节点中未经解析的 XPath path 属性值。
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// 获取 inheritdoc 节点的原始 XML。
    /// </summary>
    public string RawXml { get; }

    /// <summary>
    /// 获取 inheritdoc 节点的结构化元素快照。
    /// </summary>
    public CsCommentElement Element { get; }
}
