using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;

namespace Bing.Comments;

/// <summary>
/// 表示 XML 文档注释中的不可变元素快照。
/// </summary>
public sealed class CsCommentElement
{
    private readonly IReadOnlyDictionary<string, string> _attributes;
    private readonly IReadOnlyList<CsCommentElement> _children;
    private readonly IReadOnlyList<CsCommentNode> _nodes;

    internal CsCommentElement(XmlNode node)
    {
        Name = node.Name;
        Text = node.InnerText;
        InnerXml = node.InnerXml;
        OuterXml = node.OuterXml;

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        if (node.Attributes != null)
        {
            foreach (XmlAttribute attribute in node.Attributes)
                attributes[attribute.Name] = attribute.Value;
        }
        _attributes = new ReadOnlyDictionary<string, string>(attributes);

        var children = new List<CsCommentElement>();
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
                children.Add(new CsCommentElement(child));
        }
        _children = new ReadOnlyCollection<CsCommentElement>(children);

        var nodes = new List<CsCommentNode>();
        foreach (XmlNode child in node.ChildNodes)
            nodes.Add(new CsCommentNode(child));
        _nodes = new ReadOnlyCollection<CsCommentNode>(nodes);
    }

    /// <summary>
    /// 获取元素名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 获取元素及其后代节点的纯文本内容。
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// 获取元素的原始内部 XML。
    /// </summary>
    public string InnerXml { get; }

    /// <summary>
    /// 获取元素的原始外部 XML。
    /// </summary>
    public string OuterXml { get; }

    /// <summary>
    /// 获取元素属性的只读快照。
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes => _attributes;

    /// <summary>
    /// 获取元素直接子元素的只读快照。
    /// </summary>
    public IReadOnlyList<CsCommentElement> Children => _children;

    /// <summary>
    /// 获取包含文本、CDATA、注释和处理指令在内的直接子节点快照。
    /// </summary>
    public IReadOnlyList<CsCommentNode> Nodes => _nodes;

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
    /// 输出元素的纯文本内容。
    /// </summary>
    /// <returns>元素文本；没有文本时返回空字符串。</returns>
    public override string ToString() => Text ?? string.Empty;
}