using System.Xml;

namespace Bing.Comments;

/// <summary>
/// 表示从 XML 文档注释中解析出的成员说明信息。
/// </summary>
public sealed class CsComments
{
    /// <summary>
    /// 获取文档注释的主体说明；内容来自 <c>summary</c> 或 <c>value</c> 节点，缺失时为 <see langword="null"/>。
    /// </summary>
    public string Summary { get; private set; }

    /// <summary>
    /// 获取文档注释中的补充说明；缺失时为 <see langword="null"/>。
    /// </summary>
    public string Remarks { get; private set; }

    /// <summary>
    /// 获取文档注释中的返回值说明；缺失时为 <see langword="null"/>。
    /// </summary>
    public string Returns { get; private set; }

    /// <summary>
    /// 获取按参数名索引的参数说明集合；没有参数说明时为空集合。
    /// </summary>
    public CsCommentsParamCollection Param { get; private set; }

    /// <summary>
    /// 获取按泛型参数名索引的泛型参数说明集合；没有说明时为空集合。
    /// </summary>
    public CsCommentsParamCollection TypeParam { get; private set; }

    /// <summary>
    /// 获取文档注释中的异常说明；没有异常说明时为空数组。
    /// </summary>
    public CsCommentsException[] Exception { get; private set; }

    /// <summary>
    /// 获取所有说明均为空的注释对象。
    /// </summary>
    public static readonly CsComments Empty = new();

    /// <summary>
    /// 初始化一个 <see cref="CsComments"/> 类型的实例。
    /// </summary>
    private CsComments()
    {
        Param = new CsCommentsParamCollection();
        TypeParam = new CsCommentsParamCollection();
        Exception = new CsCommentsException[0];
    }

    /// <summary>
    /// 初始化一个 <see cref="CsComments"/> 类型的实例。
    /// </summary>
    /// <param name="node">包含文档注释子节点的 XML 成员节点。</param>
    internal CsComments(XmlNode node)
    {
        var summary = node["summary"] ?? node["value"];
        if (summary != null)
            Summary = summary.InnerText.Trim();
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

        var typeparam = node.SelectNodes("typeparam");
        TypeParam = new CsCommentsParamCollection();
        foreach (XmlNode item in typeparam)
        {
            var p = new CsCommentsParam(item);
            TypeParam[p.Name] = p;
        }

        var exception = node.SelectNodes("exception");
        var index = 0;
        Exception = new CsCommentsException[exception.Count];
        foreach (XmlNode item in exception)
        {
            Exception[index] = new CsCommentsException(index, item);
            index++;
        }
    }

    /// <inheritdoc />
    public override string ToString() => Summary ?? "";
}
