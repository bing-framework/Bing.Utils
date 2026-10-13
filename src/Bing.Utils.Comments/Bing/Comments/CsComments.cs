using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;

namespace Bing.Comments;

/// <summary>
/// 表示一个成员的 XML 文档注释信息。
/// </summary>
public sealed class CsComments
{
    private CsCommentsParamCollection _param;
    private CsCommentsParamCollection _typeParam;
    private IReadOnlyDictionary<string, CsCommentsParam> _parameters;
    private IReadOnlyDictionary<string, CsCommentsParam> _typeParameters;
    private IReadOnlyList<CsCommentsException> _exceptions;

    /// <summary>
    /// 获取文档注释中的 summary 文本；没有 summary 时回退到 value 文本。
    /// </summary>
    public string Summary { get; private set; }

    /// <summary>
    /// 获取文档注释中的 value 节点文本。
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// 获取文档注释中的 remarks 节点文本。
    /// </summary>
    public string Remarks { get; private set; }

    /// <summary>
    /// 获取文档注释中的 returns 节点文本。
    /// </summary>
    public string Returns { get; private set; }

    /// <summary>
    /// 获取文档注释中的 param 节点集合。
    /// </summary>
    public CsCommentsParamCollection Param
    {
        get => ReferenceEquals(this, Empty) ? new CsCommentsParamCollection() : _param;
        private set => _param = value;
    }

    /// <summary>
    /// 获取文档注释中的 typeparam 节点集合。
    /// </summary>
    public CsCommentsParamCollection TypeParam
    {
        get => ReferenceEquals(this, Empty) ? new CsCommentsParamCollection() : _typeParam;
        private set => _typeParam = value;
    }

    /// <summary>
    /// 获取 param 节点的不可变快照视图。
    /// </summary>
    public IReadOnlyDictionary<string, CsCommentsParam> Parameters => _parameters;

    /// <summary>
    /// 获取 typeparam 节点的不可变快照视图。
    /// </summary>
    public IReadOnlyDictionary<string, CsCommentsParam> TypeParameters => _typeParameters;

    /// <summary>
    /// 获取文档注释中的 exception 节点数组。
    /// </summary>
    public CsCommentsException[] Exception { get; private set; }

    /// <summary>
    /// 获取 exception 节点的不可变快照视图。
    /// </summary>
    public IReadOnlyList<CsCommentsException> Exceptions => _exceptions;

    /// <summary>
    /// 获取成员节点中除外层 member 节点之外的原始 XML 内容。
    /// </summary>
    public string RawXml { get; private set; }

    /// <summary>
    /// 获取按 XML 文档顺序排列的顶层元素快照。
    /// </summary>
    public IReadOnlyList<CsCommentElement> Elements { get; private set; }

    /// <summary>
    /// 获取未能解析的 inheritdoc 节点快照。
    /// </summary>
    /// <remarks>
    /// 解析器不会为无法找到来源或无法应用 path 的节点伪造内容；调用方可通过此集合诊断缺失的程序集或无效的继承声明。
    /// </remarks>
    public IReadOnlyList<CsCommentInheritdoc> UnresolvedInheritdoc { get; private set; }

    /// <summary>
    /// 获取空注释对象。
    /// </summary>
    public static readonly CsComments Empty = new();

    /// <summary>
    /// 初始化一个<see cref="CsComments"/>类型的实例
    /// </summary>
    private CsComments()
    {
        Param = new CsCommentsParamCollection();
        TypeParam = new CsCommentsParamCollection();
        Exception = Array.Empty<CsCommentsException>();
        _parameters = new ReadOnlyDictionary<string, CsCommentsParam>(
            new Dictionary<string, CsCommentsParam>());
        _typeParameters = new ReadOnlyDictionary<string, CsCommentsParam>(
            new Dictionary<string, CsCommentsParam>());
        _exceptions = Array.Empty<CsCommentsException>();
        Elements = Array.Empty<CsCommentElement>();
        UnresolvedInheritdoc = Array.Empty<CsCommentInheritdoc>();
    }

    internal CsComments(XmlNode node)
    {
        RawXml = node.InnerXml;
        var elements = new List<CsCommentElement>();
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
                elements.Add(new CsCommentElement(child));
        }
        Elements = new ReadOnlyCollection<CsCommentElement>(elements);

        var unresolvedInheritdoc = new List<CsCommentInheritdoc>();
        foreach (XmlNode item in node.SelectNodes(".//inheritdoc"))
            unresolvedInheritdoc.Add(new CsCommentInheritdoc(item));
        UnresolvedInheritdoc = new ReadOnlyCollection<CsCommentInheritdoc>(unresolvedInheritdoc);

        var summaryNode = node["summary"];
        var valueNode = node["value"];
        var summary = summaryNode ?? valueNode;
        if (summary != null) 
            Summary = summary.InnerText.Trim();
        if (valueNode != null)
            Value = valueNode.InnerText.Trim();
        var remarks = node["remarks"];
        if (remarks != null) 
            Remarks = remarks.InnerText.Trim();
        var returns = node["returns"];
        if (returns != null) 
            Returns = returns.InnerText.Trim();
        var param = node.SelectNodes("param");
        Param = new CsCommentsParamCollection();
        foreach (XmlNode item in param)
        {
            var p = new CsCommentsParam(item);
            Param[p.Name] = p;
        }
        _parameters = new ReadOnlyDictionary<string, CsCommentsParam>(
            new Dictionary<string, CsCommentsParam>(Param));

        var typeparam = node.SelectNodes("typeparam");
        TypeParam = new CsCommentsParamCollection();
        foreach (XmlNode item in typeparam)
        {
            var p = new CsCommentsParam(item);
            TypeParam[p.Name] = p;
        }
        _typeParameters = new ReadOnlyDictionary<string, CsCommentsParam>(
            new Dictionary<string, CsCommentsParam>(TypeParam));

        var exception = node.SelectNodes("exception");
        var index = 0;
        Exception = new CsCommentsException[exception.Count];
        foreach (XmlNode item in exception)
        {
            Exception[index] = new CsCommentsException(index, item);
            index++;
        }
        _exceptions = new ReadOnlyCollection<CsCommentsException>(
            new List<CsCommentsException>(Exception));
    }

    /// <summary>
    /// 获取指定名称的顶层元素快照。
    /// </summary>
    /// <param name="name">要匹配的元素名称。</param>
    /// <returns>按原始顺序排列的匹配元素；名称为空时返回空集合。</returns>
    public IReadOnlyList<CsCommentElement> GetElements(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Array.Empty<CsCommentElement>();

        var result = new List<CsCommentElement>();
        foreach (var element in Elements)
        {
            if (string.Equals(element.Name, name, StringComparison.Ordinal))
                result.Add(element);
        }
        return new ReadOnlyCollection<CsCommentElement>(result);
    }

    /// <summary>
    /// 输出字符串
    /// </summary>
    /// <returns>summary 文本；没有文本时返回空字符串。</returns>
    public override string ToString() => Summary ?? "";
}