using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;

namespace Bing.Comments;

/// <summary>
/// 表示 XML 文档注释中的任意子节点快照。
/// </summary>
public sealed class CsCommentNode
{
    private readonly IReadOnlyDictionary<string, string> _attributes;
    private readonly IReadOnlyList<CsCommentNode> _children;

    internal CsCommentNode(XmlNode node)
    {
        NodeType = node.NodeType;
        Name = node.Name;
        Text = node.NodeType == XmlNodeType.Element ? node.InnerText : node.Value ?? node.InnerText;
        InnerXml = node.InnerXml;
        OuterXml = node.OuterXml;

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        if (node.Attributes != null)
        {
            foreach (XmlAttribute attribute in node.Attributes)
                attributes[attribute.Name] = attribute.Value;
        }
        _attributes = new ReadOnlyDictionary<string, string>(attributes);

        var children = new List<CsCommentNode>();
        foreach (XmlNode child in node.ChildNodes)
            children.Add(new CsCommentNode(child));
        _children = new ReadOnlyCollection<CsCommentNode>(children);
    }

    /// <summary>
    /// 获取节点类型。
    /// </summary>
    public XmlNodeType NodeType { get; }

    /// <summary>
    /// 获取节点名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 获取节点的文本内容。
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// 获取节点的原始内部 XML。
    /// </summary>
    public string InnerXml { get; }

    /// <summary>
    /// 获取节点的原始外部 XML。
    /// </summary>
    public string OuterXml { get; }

    /// <summary>
    /// 获取节点属性的只读快照。
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes => _attributes;

    /// <summary>
    /// 获取节点直接子节点的只读快照。
    /// </summary>
    public IReadOnlyList<CsCommentNode> Children => _children;

    /// <summary>
    /// 获取指定名称的属性值。
    /// </summary>
    /// <param name="name">要读取的属性名称。</param>
    /// <returns>属性值；名称为空或属性不存在时返回 null。</returns>
    public string GetAttribute(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        return _attributes.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>
    /// 输出节点的文本内容。
    /// </summary>
    /// <returns>节点文本；没有文本时返回空字符串。</returns>
    public override string ToString() => Text ?? string.Empty;
}