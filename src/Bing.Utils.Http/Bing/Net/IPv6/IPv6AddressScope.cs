namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址作用域枚举
/// </summary>
public enum IPv6AddressScope
{
    /// <summary>
    /// 无效作用域
    /// </summary>
    Invalid = 0,

    /// <summary>
    /// 节点本地作用域
    /// </summary>
    Node = 1,

    /// <summary>
    /// 链路本地作用域
    /// </summary>
    Link = 2,

    /// <summary>
    /// 管理本地作用域
    /// </summary>
    Admin = 4,

    /// <summary>
    /// 站点本地作用域
    /// </summary>
    Site = 5,

    /// <summary>
    /// 组织本地作用域
    /// </summary>
    Organization = 8,

    /// <summary>
    /// 全局作用域
    /// </summary>
    Global = 14,

    /// <summary>
    /// 文档用作用域
    /// </summary>
    Documentation = 99,

    /// <summary>
    /// 未知作用域
    /// </summary>
    Unknown = 255
}
