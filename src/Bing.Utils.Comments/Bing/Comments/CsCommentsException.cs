using System.Xml;

namespace Bing.Comments;

/// <summary>
/// 表示注释中的 exception 节点。
/// </summary>
public sealed class CsCommentsException
{
    /// <summary>
    /// 获取节点在文档中的索引。
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// 获取 cref 属性值；合法的 X: 前缀会被移除。
    /// </summary>
    public string Cref { get; private set; }

    /// <summary>
    /// 获取节点中未经兼容处理的 cref 属性值。
    /// </summary>
    public string RawCref { get; private set; }

    /// <summary>
    /// 获取节点中的纯文本内容。
    /// </summary>
    public string Text { get; private set; }

    /// <summary>
    /// 获取 exception 节点的原始 XML。
    /// </summary>
    public string RawXml { get; private set; }

    /// <summary>
    /// 获取 exception 节点的结构化元素快照。
    /// </summary>
    public CsCommentElement Element { get; private set; }

    /// <summary>
    /// 初始化一个<see cref="CsCommentsException"/>类型的实例
    /// </summary>
    /// <param name="index">节点在文档中的索引</param>
    /// <param name="node">Xml节点</param>
    internal CsCommentsException(int index, XmlNode node)
    {
        RawXml = node.OuterXml;
        Element = new CsCommentElement(node);
        Index = index;
        var attr = node.Attributes["cref"];
        if (attr != null)
        {
            RawCref = attr.Value;
            Cref = RawCref != null && RawCref.Length > 1 && RawCref[1] == ':'
                ? RawCref.Substring(2)
                : RawCref;
        }
        Text = node.InnerText.Trim();
    }

    /// <summary>
    /// 输出字符串
    /// </summary>
    /// <returns>异常文本；没有文本时返回空字符串。</returns>
    public override string ToString() => Text ?? "";
}