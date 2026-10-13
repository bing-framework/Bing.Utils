using System.Xml;

namespace Bing.Comments;

/// <summary>
/// 表示文档注释中的一个参数或泛型参数说明。
/// </summary>
public sealed class CsCommentsParam
{
    /// <summary>
    /// 获取参数节点 <c>name</c> 属性的值；属性缺失时为 <see langword="null"/>。
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// 获取参数节点的说明文本；保留 XML 节点解析得到的空白。
    /// </summary>
    public string Text { get; private set; }

    /// <summary>
    /// 获取名称和说明均未设置的空参数说明对象。
    /// </summary>
    public static readonly CsCommentsParam Empty = new();

    /// <summary>
    /// 初始化一个 <see cref="CsCommentsParam"/> 类型的实例。
    /// </summary>
    private CsCommentsParam() { }

    /// <summary>
    /// 初始化一个 <see cref="CsCommentsParam"/> 类型的实例。
    /// </summary>
    /// <param name="node">包含 <c>name</c> 属性和说明文本的 XML 参数节点。</param>
    internal CsCommentsParam(XmlNode node)
    {
        var attr = node.Attributes["name"];
        if (attr != null)
            Name = attr.InnerText.Trim();
        Text = node.InnerText;
    }

    /// <inheritdoc />
    public override string ToString() => Text ?? "";
}
