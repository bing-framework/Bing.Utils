using System.Xml;

namespace Bing.Comments;

/// <summary>
/// 表示文档注释中的一条异常说明。
/// </summary>
public sealed class CsCommentsException
{
    /// <summary>
    /// 获取异常节点在所属注释中的从零开始的索引。
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// 获取异常节点 <c>cref</c> 属性去除两字符文档标识前缀后的值；属性缺失时为 <see langword="null"/>。
    /// </summary>
    public string Cref { get; private set; }

    /// <summary>
    /// 获取异常节点去除首尾空白后的说明文本。
    /// </summary>
    public string Text { get; private set; }

    /// <summary>
    /// 初始化一个 <see cref="CsCommentsException"/> 类型的实例。
    /// </summary>
    /// <param name="index">异常节点在所属注释中的从零开始的索引。</param>
    /// <param name="node">包含 <c>cref</c> 属性和说明文本的 XML 异常节点。</param>
    internal CsCommentsException(int index, XmlNode node)
    {
        Index = index;
        var attr = node.Attributes["cref"];
        if (attr != null)
        {
            Cref = attr.InnerText;
            Cref = Cref?.Remove(0, 2);
        }
        Text = node.InnerText.Trim();
    }

    /// <inheritdoc />
    public override string ToString() => Text ?? "";
}
