namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址分析结果
/// </summary>
public class IPv6AddressAnalysis
{
    /// <summary>
    /// 原始地址
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// 地址类型
    /// </summary>
    public IPv6AddressType AddressType { get; set; }

    /// <summary>
    /// 作用域
    /// </summary>
    public IPv6AddressScope Scope { get; set; }

    /// <summary>
    /// 是否为私有地址
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// 是否为公网地址
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// 是否可路由
    /// </summary>
    public bool IsRoutable { get; set; }

    /// <summary>
    /// 地址类型描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 作用域描述
    /// </summary>
    public string ScopeDescription { get; set; }

    /// <summary>
    /// 压缩格式
    /// </summary>
    public string CompressedForm { get; set; }

    /// <summary>
    /// 展开格式
    /// </summary>
    public string ExpandedForm { get; set; }

    /// <summary>
    /// 返回格式化的字符串表示
    /// </summary>
    public override string ToString() => $"{Address} - {Description} ({ScopeDescription})";
}