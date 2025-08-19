using System.Net;

namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址分析器
/// </summary>
/// <remarks>
/// 提供IPv6地址类型判断、特征分析、网络归属分析等功能
/// </remarks>
public static class IPv6AddressAnalyzer
{
    /// <summary>
    /// 获取IPv6地址的类型
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>IPv6地址类型</returns>
    /// <exception cref="ArgumentException">当IPv6地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// var type = IPv6AddressAnalyzer.GetAddressType("2001:db8::1");
    /// Console.WriteLine(type); // GlobalUnicast
    /// </code>
    /// </example>
    public static IPv6AddressType GetAddressType(string ipv6Address)
    {
        if (!IPv6Validator.IsValid(ipv6Address))
            return IPv6AddressType.Invalid;

        var ip = IPAddress.Parse(ipv6Address);
        var bytes = ip.GetAddressBytes();

        // 回环地址 ::1
        if (IPAddress.IsLoopback(ip))
            return IPv6AddressType.Loopback;

        // 未指定地址 ::
        if (ip.Equals(IPAddress.IPv6Any))
            return IPv6AddressType.Unspecified;

        // IPv4映射地址 ::ffff:0:0/96
        if (ip.IsIPv4MappedToIPv6)
            return IPv6AddressType.IPv4Mapped;

        // 链路本地地址 fe80::/10
        if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
            return IPv6AddressType.LinkLocal;

        // 站点本地地址 fec0::/10 (已废弃)
        if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0xc0)
            return IPv6AddressType.SiteLocal;

        // 唯一本地地址 fc00::/7
        if ((bytes[0] & 0xfe) == 0xfc)
            return IPv6AddressType.UniqueLocal;

        // 组播地址 ff00::/8
        if (bytes[0] == 0xff)
            return IPv6AddressType.Multicast;

        // 文档用地址 2001:db8::/32
        if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8)
            return IPv6AddressType.Documentation;

        // 6to4地址 2002::/16
        if (bytes[0] == 0x20 && bytes[1] == 0x02)
            return IPv6AddressType.SixToFour;

        // Teredo地址 2001::/32
        if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x00 && bytes[3] == 0x00)
            return IPv6AddressType.Teredo;

        // 全局单播地址
        return IPv6AddressType.GlobalUnicast;
    }

    /// <summary>
    /// 判断IPv6地址是否为全局单播地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是全局单播地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isGlobal = IPv6AddressAnalyzer.IsGlobalUnicast("2001:4860:4860::8888");
    /// Console.WriteLine(isGlobal); // true
    /// </code>
    /// </example>
    public static bool IsGlobalUnicast(string ipv6Address) =>
        GetAddressType(ipv6Address) == IPv6AddressType.GlobalUnicast;

    /// <summary>
    /// 判断IPv6地址是否为组播地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是组播地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isMulticast = IPv6AddressAnalyzer.IsMulticast("ff02::1");
    /// Console.WriteLine(isMulticast); // true
    /// </code>
    /// </example>
    public static bool IsMulticast(string ipv6Address) =>
        GetAddressType(ipv6Address) == IPv6AddressType.Multicast;

    /// <summary>
    /// 判断IPv6地址是否为链路本地地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是链路本地地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isLinkLocal = IPv6AddressAnalyzer.IsLinkLocal("fe80::1");
    /// Console.WriteLine(isLinkLocal); // true
    /// </code>
    /// </example>
    public static bool IsLinkLocal(string ipv6Address) =>
        GetAddressType(ipv6Address) == IPv6AddressType.LinkLocal;

    /// <summary>
    /// 判断IPv6地址是否为站点本地地址（已废弃）
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是站点本地地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isSiteLocal = IPv6AddressAnalyzer.IsSiteLocal("fec0::1");
    /// Console.WriteLine(isSiteLocal); // true
    /// </code>
    /// </example>
    public static bool IsSiteLocal(string ipv6Address) =>
        GetAddressType(ipv6Address) == IPv6AddressType.SiteLocal;

    /// <summary>
    /// 判断IPv6地址是否为唯一本地地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是唯一本地地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isUniqueLocal = IPv6AddressAnalyzer.IsUniqueLocal("fc00::1");
    /// Console.WriteLine(isUniqueLocal); // true
    /// </code>
    /// </example>
    public static bool IsUniqueLocal(string ipv6Address) =>
        GetAddressType(ipv6Address) == IPv6AddressType.UniqueLocal;

    /// <summary>
    /// 判断IPv6地址是否为回环地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是回环地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isLoopback = IPv6AddressAnalyzer.IsLoopback("::1");
    /// Console.WriteLine(isLoopback); // true
    /// </code>
    /// </example>
    public static bool IsLoopback(string ipv6Address) =>
        GetAddressType(ipv6Address) == IPv6AddressType.Loopback;

    /// <summary>
    /// 判断IPv6地址是否为未指定地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是未指定地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isUnspecified = IPv6AddressAnalyzer.IsUnspecified("::");
    /// Console.WriteLine(isUnspecified); // true
    /// </code>
    /// </example>
    public static bool IsUnspecified(string ipv6Address) =>
        GetAddressType(ipv6Address) == IPv6AddressType.Unspecified;

    /// <summary>
    /// 判断IPv6地址是否为IPv4映射地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是IPv4映射地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isIPv4Mapped = IPv6AddressAnalyzer.IsIPv4Mapped("::ffff:192.168.1.1");
    /// Console.WriteLine(isIPv4Mapped); // true
    /// </code>
    /// </example>
    public static bool IsIPv4Mapped(string ipv6Address) =>
        GetAddressType(ipv6Address) == IPv6AddressType.IPv4Mapped;

    /// <summary>
    /// 判断IPv6地址是否为文档用地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是文档用地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isDocumentation = IPv6AddressAnalyzer.IsDocumentation("2001:db8::1");
    /// Console.WriteLine(isDocumentation); // true
    /// </code>
    /// </example>
    public static bool IsDocumentation(string ipv6Address) =>
        GetAddressType(ipv6Address) == IPv6AddressType.Documentation;

    /// <summary>
    /// 判断IPv6地址是否为6to4地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是6to4地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool is6to4 = IPv6AddressAnalyzer.Is6to4("2002:c000:0204::1");
    /// Console.WriteLine(is6to4); // true
    /// </code>
    /// </example>
    public static bool Is6to4(string ipv6Address) =>
        GetAddressType(ipv6Address) == IPv6AddressType.SixToFour;

    /// <summary>
    /// 判断IPv6地址是否为Teredo地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是Teredo地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isTeredo = IPv6AddressAnalyzer.IsTeredo("2001:0000:4136:e378:8000:63bf:3fff:fdd2");
    /// Console.WriteLine(isTeredo); // true
    /// </code>
    /// </example>
    public static bool IsTeredo(string ipv6Address) =>
        GetAddressType(ipv6Address) == IPv6AddressType.Teredo;

    /// <summary>
    /// 判断IPv6地址是否为私有/内网地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是私有/内网地址返回true，否则返回false</returns>
    /// <remarks>
    /// 私有/内网IPv6地址包括：<br />
    /// - ::1 (回环地址)<br />
    /// - fe80::/10 (链路本地地址)<br />
    /// - fc00::/7 (唯一本地地址)<br />
    /// - fec0::/10 (站点本地地址，已废弃)<br />
    /// - 2001:db8::/32 (文档用地址)<br />
    /// - ::ffff:0:0/96 (IPv4映射地址中的私有IP)
    /// </remarks>
    /// <example>
    /// <code>
    /// bool isPrivate1 = IPv6AddressAnalyzer.IsPrivate("::1");           // true
    /// bool isPrivate2 = IPv6AddressAnalyzer.IsPrivate("fe80::1");       // true
    /// bool isPrivate3 = IPv6AddressAnalyzer.IsPrivate("2001:db8::1");   // true
    /// bool isPrivate4 = IPv6AddressAnalyzer.IsPrivate("2001:4860:4860::8888"); // false
    /// </code>
    /// </example>
    public static bool IsPrivate(string ipv6Address)
    {
        var addressType = GetAddressType(ipv6Address);

        switch (addressType)
        {
            case IPv6AddressType.Loopback:
            case IPv6AddressType.LinkLocal:
            case IPv6AddressType.UniqueLocal:
            case IPv6AddressType.SiteLocal:
            case IPv6AddressType.Documentation:
                return true;

            case IPv6AddressType.IPv4Mapped:
                // 检查映射的IPv4地址是否为私有IP
                try
                {
                    var ipv4 = IPv6Converter.MapToIPv4(ipv6Address);
                    return Bing.Net.IPv4.IPv4Validator.IsInnerIp(ipv4);
                }
                catch
                {
                    return false;
                }

            default:
                return false;
        }
    }

    /// <summary>
    /// 判断IPv6地址是否为公网地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是公网地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isPublic1 = IPv6AddressAnalyzer.IsPublic("2001:4860:4860::8888"); // true
    /// bool isPublic2 = IPv6AddressAnalyzer.IsPublic("::1");                  // false
    /// bool isPublic3 = IPv6AddressAnalyzer.IsPublic("fe80::1");              // false
    /// </code>
    /// </example>
    public static bool IsPublic(string ipv6Address)
    {
        return IsGlobalUnicast(ipv6Address) && !IsPrivate(ipv6Address);
    }

    /// <summary>
    /// 判断IPv6地址是否可路由
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果可路由返回true，否则返回false</returns>
    /// <remarks>
    /// 可路由的IPv6地址包括全局单播地址，不包括链路本地地址、回环地址等
    /// </remarks>
    /// <example>
    /// <code>
    /// bool isRoutable1 = IPv6AddressAnalyzer.IsRoutable("2001:4860:4860::8888"); // true
    /// bool isRoutable2 = IPv6AddressAnalyzer.IsRoutable("fe80::1");              // false
    /// bool isRoutable3 = IPv6AddressAnalyzer.IsRoutable("::1");                  // false
    /// </code>
    /// </example>
    public static bool IsRoutable(string ipv6Address)
    {
        var addressType = GetAddressType(ipv6Address);

        return addressType switch
        {
            IPv6AddressType.GlobalUnicast => true,
            IPv6AddressType.UniqueLocal => true, // ULA addresses are routable within their scope
            _ => false
        };
    }

    /// <summary>
    /// 获取IPv6地址的作用域
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>IPv6地址作用域</returns>
    /// <example>
    /// <code>
    /// var scope = IPv6AddressAnalyzer.GetAddressScope("2001:4860:4860::8888");
    /// Console.WriteLine(scope); // Global
    /// </code>
    /// </example>
    public static IPv6AddressScope GetAddressScope(string ipv6Address)
    {
        var addressType = GetAddressType(ipv6Address);

        return addressType switch
        {
            IPv6AddressType.Invalid => IPv6AddressScope.Invalid,
            IPv6AddressType.Loopback => IPv6AddressScope.Node,
            IPv6AddressType.Unspecified => IPv6AddressScope.Node,
            IPv6AddressType.LinkLocal => IPv6AddressScope.Link,
            IPv6AddressType.SiteLocal => IPv6AddressScope.Site,
            IPv6AddressType.UniqueLocal => IPv6AddressScope.Organization,
            IPv6AddressType.GlobalUnicast => IPv6AddressScope.Global,
            IPv6AddressType.Multicast => GetMulticastScope(ipv6Address),
            IPv6AddressType.IPv4Mapped => IPv6AddressScope.Global,
            IPv6AddressType.Documentation => IPv6AddressScope.Documentation,
            IPv6AddressType.SixToFour => IPv6AddressScope.Global,
            IPv6AddressType.Teredo => IPv6AddressScope.Global,
            _ => IPv6AddressScope.Unknown
        };
    }

    /// <summary>
    /// 获取组播地址的作用域
    /// </summary>
    /// <param name="ipv6Address">IPv6组播地址字符串</param>
    /// <returns>组播地址作用域</returns>
    /// <example>
    /// <code>
    /// var scope = IPv6AddressAnalyzer.GetMulticastScope("ff02::1");
    /// Console.WriteLine(scope); // Link
    /// </code>
    /// </example>
    public static IPv6AddressScope GetMulticastScope(string ipv6Address)
    {
        if (!IsMulticast(ipv6Address))
            return IPv6AddressScope.Invalid;

        var bytes = IPv6Converter.ToBytes(ipv6Address);
        var scopeField = bytes[1] & 0x0F; // 获取作用域字段

        return scopeField switch
        {
            0x1 => IPv6AddressScope.Node,
            0x2 => IPv6AddressScope.Link,
            0x4 => IPv6AddressScope.Admin,
            0x5 => IPv6AddressScope.Site,
            0x8 => IPv6AddressScope.Organization,
            0xE => IPv6AddressScope.Global,
            _ => IPv6AddressScope.Unknown
        };
    }

    /// <summary>
    /// 分析IPv6地址的详细信息
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>IPv6地址分析结果</returns>
    /// <example>
    /// <code>
    /// var analysis = IPv6AddressAnalyzer.AnalyzeAddress("2001:db8::1");
    /// Console.WriteLine($"类型: {analysis.AddressType}");
    /// Console.WriteLine($"作用域: {analysis.Scope}");
    /// Console.WriteLine($"是否私有: {analysis.IsPrivate}");
    /// </code>
    /// </example>
    public static IPv6AddressAnalysisResult AnalyzeAddress(string ipv6Address)
    {
        var addressType = GetAddressType(ipv6Address);
        var scope = GetAddressScope(ipv6Address);
        var isPrivate = IsPrivate(ipv6Address);
        var isPublic = IsPublic(ipv6Address);
        var isRoutable = IsRoutable(ipv6Address);

        string description = GetAddressTypeDescription(addressType);
        string scopeDescription = GetScopeDescription(scope);

        return new IPv6AddressAnalysisResult
        {
            Address = ipv6Address,
            AddressType = addressType,
            Scope = scope,
            IsPrivate = isPrivate,
            IsPublic = isPublic,
            IsRoutable = isRoutable,
            Description = description,
            ScopeDescription = scopeDescription,
            CompressedForm = IPv6Converter.Compress(ipv6Address),
            ExpandedForm = IPv6Converter.Expand(ipv6Address)
        };
    }

    /// <summary>
    /// 获取地址类型的描述
    /// </summary>
    /// <param name="addressType">地址类型</param>
    /// <returns>地址类型描述</returns>
    private static string GetAddressTypeDescription(IPv6AddressType addressType)
    {
        return addressType switch
        {
            IPv6AddressType.Invalid => "无效地址",
            IPv6AddressType.Loopback => "回环地址",
            IPv6AddressType.Unspecified => "未指定地址",
            IPv6AddressType.LinkLocal => "链路本地地址",
            IPv6AddressType.SiteLocal => "站点本地地址（已废弃）",
            IPv6AddressType.UniqueLocal => "唯一本地地址",
            IPv6AddressType.GlobalUnicast => "全局单播地址",
            IPv6AddressType.Multicast => "组播地址",
            IPv6AddressType.IPv4Mapped => "IPv4映射地址",
            IPv6AddressType.Documentation => "文档用地址",
            IPv6AddressType.SixToFour => "6to4隧道地址",
            IPv6AddressType.Teredo => "Teredo隧道地址",
            _ => "未知类型"
        };
    }

    /// <summary>
    /// 获取作用域的描述
    /// </summary>
    /// <param name="scope">作用域</param>
    /// <returns>作用域描述</returns>
    private static string GetScopeDescription(IPv6AddressScope scope)
    {
        return scope switch
        {
            IPv6AddressScope.Invalid => "无效作用域",
            IPv6AddressScope.Node => "节点本地",
            IPv6AddressScope.Link => "链路本地",
            IPv6AddressScope.Admin => "管理本地",
            IPv6AddressScope.Site => "站点本地",
            IPv6AddressScope.Organization => "组织本地",
            IPv6AddressScope.Global => "全局",
            IPv6AddressScope.Documentation => "文档用",
            IPv6AddressScope.Unknown => "未知作用域",
            _ => "未定义"
        };
    }
}