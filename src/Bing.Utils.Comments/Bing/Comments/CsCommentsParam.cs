using System.Xml;

namespace Bing.Comments;

/// <summary>
/// 表示注释中的 param 或 typeparam 节点。
/// </summary>
public sealed class CsCommentsParam
{
    /// <summary>
    /// 获取节点中 name 属性的值。
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// 获取节点中的纯文本内容。
    /// </summary>
    public string Text { get; private set; }

    /// <summary>
    /// 获取 param 或 typeparam 节点的原始 XML。
    /// </summary>
    public string RawXml { get; private set; }

    /// <summary>
    /// 获取 param 或 typeparam 节点的结构化元素快照。
    /// </summary>
    public CsCommentElement Element { get; private set; }

    /// <summary>
    /// 获取空参数对象。
    /// </summary>
    public static readonly CsCommentsParam Empty = new();

    /// <summary>
    /// 初始化一个<see cref="CsCommentsParam"/>类型的实例
    /// </summary>
    private CsCommentsParam() { }

    /// <summary>
    /// 初始化一个<see cref="CsCommentsParam"/>类型的实例
    /// </summary>
    /// <param name="node">Xml节点</param>
    internal CsCommentsParam(XmlNode node)
    {
        RawXml = node.OuterXml;
        Element = new CsCommentElement(node);
        var attr = node.Attributes["name"];
        if (attr != null) 
            Name = attr.InnerText.Trim();
        Text = node.InnerText;
    }

    /// <summary>
    /// 输出字符串
    /// </summary>
    /// <returns>参数文本；没有文本时返回空字符串。</returns>
    public override string ToString() => Text ?? "";
}