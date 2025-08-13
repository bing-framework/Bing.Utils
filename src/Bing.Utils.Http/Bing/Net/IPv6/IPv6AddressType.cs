namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址类型枚举
/// </summary>
public enum IPv6AddressType
{
    /// <summary>
    /// 无效地址
    /// </summary>
    Invalid,

    /// <summary>
    /// 回环地址 (::1)
    /// </summary>
    Loopback,

    /// <summary>
    /// 未指定地址 (::)
    /// </summary>
    Unspecified,

    /// <summary>
    /// IPv4映射地址 (::ffff:0:0/96)
    /// </summary>
    IPv4Mapped,

    /// <summary>
    /// 链路本地地址 (fe80::/10)
    /// </summary>
    LinkLocal,

    /// <summary>
    /// 站点本地地址 (fec0::/10) - 已废弃
    /// </summary>
    SiteLocal,

    /// <summary>
    /// 唯一本地地址 (fc00::/7)
    /// </summary>
    UniqueLocal,

    /// <summary>
    /// 组播地址 (ff00::/8)
    /// </summary>
    Multicast,

    /// <summary>
    /// 文档用地址 (2001:db8::/32)
    /// </summary>
    Documentation,

    /// <summary>
    /// 6to4地址 (2002::/16)
    /// </summary>
    SixToFour,

    /// <summary>
    /// Teredo地址 (2001::/32)
    /// </summary>
    Teredo,

    /// <summary>
    /// 全局单播地址
    /// </summary>
    GlobalUnicast
}