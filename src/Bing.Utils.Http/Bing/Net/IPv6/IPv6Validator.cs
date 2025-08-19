using Bing.Net.IPv6.Internal;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址验证器
/// </summary>
/// <remarks>
/// 提供IPv6地址格式验证、类型判断等功能
/// </remarks>
public static class IPv6Validator
{
    /// <summary>
    /// IPv6地址正则表达式
    /// </summary>
    private static readonly Regex IPv6Regex = new(
        @"^(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))$",
        RegexOptions.Compiled);

    /// <summary>
    /// 验证是否为有效的IPv6地址
    /// </summary>
    /// <param name="ip">IP地址字符串</param>
    /// <returns>如果是有效的IPv6地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isIPv6 = IPv6Validator.IsValid("2001:db8::1");      // true
    /// bool isIPv6_2 = IPv6Validator.IsValid("192.168.1.1");   // false
    /// </code>
    /// </example>
    public static bool IsValid(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;
        return IPAddress.TryParse(ip, out var address)
               && address.AddressFamily == AddressFamily.InterNetworkV6;
    }

    /// <summary>
    /// 使用正则表达式验证是否为有效的IPv6地址
    /// </summary>
    /// <param name="ip">IP地址字符串</param>
    /// <returns>如果是有效的IPv6地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isValid = IPv6Validator.IsValidRegex("2001:db8::1");      // true
    /// bool isValid2 = IPv6Validator.IsValidRegex("invalid");         // false
    /// </code>
    /// </example>
    public static bool IsValidRegex(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;
        return IPv6Regex.IsMatch(ip);
    }

    /// <summary>
    /// 验证IPv6地址并获取详细信息
    /// </summary>
    /// <param name="ip">IP地址字符串</param>
    /// <param name="address">解析后的IPAddress对象</param>
    /// <param name="addressType">地址类型</param>
    /// <returns>如果是有效的IPv6地址返回true，否则返回false</returns>
    public static bool TryValidate(string ip, out IPAddress address, out IPv6AddressType addressType)
    {
        address = null;
        addressType = IPv6AddressType.Invalid;

        if (string.IsNullOrWhiteSpace(ip))
            return false;

        if (IPAddress.TryParse(ip, out var parsedAddress) && parsedAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            address = parsedAddress;
            addressType = IPv6AddressAnalyzer.GetAddressType(ip);
            return true;
        }

        address = null;
        return false;
    }

    /// <summary>
    /// 判断IPv6地址是否为本地回环地址
    /// </summary>
    /// <param name="ip">IP地址字符串</param>
    /// <returns>如果是本地回环地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isLocal = IPv6Validator.IsLocalIp("::1");              // true
    /// bool isLocal3 = IPv6Validator.IsLocalIp("2001:db8::1");     // false
    /// </code>
    /// </example>
    public static bool IsLocalIp(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;
        if (!IPAddress.TryParse(ip, out var address))
            return false;

        return address.AddressFamily == AddressFamily.InterNetworkV6 &&
               IPAddress.IsLoopback(address);
    }

    /// <summary>
    /// 判断IPv6地址是否为内网地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是内网IPv6地址返回true，否则返回false</returns>
    /// <remarks>
    /// 内网IPv6地址范围包括：<br />
    /// - ::1 (回环地址)<br />
    /// - fe80::/10 (链路本地地址)<br />
    /// - fc00::/7 (唯一本地地址)<br />
    /// - fec0::/10 (站点本地地址，已废弃)<br />
    /// - ::ffff:0:0/96 (IPv4映射地址中的私有IP)
    /// </remarks>
    public static bool IsInnerIp(string ipv6Address)
    {
        if (!IPAddress.TryParse(ipv6Address, out var address))
            return false;
        var addressType = IPv6AddressAnalyzer.GetAddressType(ipv6Address);
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
    /// 判断IPv6地址是否为全局单播地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是全局单播地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isGlobal = IPv6Validator.IsGlobalUnicast("2001:4860:4860::8888");
    /// Console.WriteLine(isGlobal); // true
    /// </code>
    /// </example>
    public static bool IsGlobalUnicast(string ipv6Address) =>
        IPv6AddressAnalyzer.GetAddressType(ipv6Address) == IPv6AddressType.GlobalUnicast;

    /// <summary>
    /// 判断IPv6地址是否为组播地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是组播地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isMulticast = IPv6Validator.IsMulticast("ff02::1");
    /// Console.WriteLine(isMulticast); // true
    /// </code>
    /// </example>
    public static bool IsMulticast(string ipv6Address) =>
        IPv6AddressAnalyzer.GetAddressType(ipv6Address) == IPv6AddressType.Multicast;

    /// <summary>
    /// 判断IPv6地址是否为链路本地地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是链路本地地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isLinkLocal = IPv6Validator.IsLinkLocal("fe80::1");
    /// Console.WriteLine(isLinkLocal); // true
    /// </code>
    /// </example>
    public static bool IsLinkLocal(string ipv6Address) =>
        IPv6AddressAnalyzer.GetAddressType(ipv6Address) == IPv6AddressType.LinkLocal;

    /// <summary>
    /// 判断IPv6地址是否为唯一本地地址 (ULA)
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是ULA地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isULA = IPv6Validator.IsUniqueLocal("fc00::1");
    /// Console.WriteLine(isULA); // true
    /// </code>
    /// </example>
    public static bool IsUniqueLocal(string ipv6Address) =>
        IPv6AddressAnalyzer.GetAddressType(ipv6Address) == IPv6AddressType.UniqueLocal;

    /// <summary>
    /// 判断IPv6地址是否为文档用途地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是文档用途地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isDoc = IPv6Validator.IsDocumentation("2001:db8::1");
    /// Console.WriteLine(isDoc); // true
    /// </code>
    /// </example>
    public static bool IsDocumentation(string ipv6Address) =>
        IPv6AddressAnalyzer.GetAddressType(ipv6Address) == IPv6AddressType.Documentation;

    /// <summary>
    /// 检查IPv6地址是否在指定的地址范围内
    /// </summary>
    /// <param name="ipv6Address">要检查的IPv6地址</param>
    /// <param name="startAddress">范围起始地址</param>
    /// <param name="endAddress">范围结束地址</param>
    /// <returns>如果在范围内返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool inRange = IPv6Validator.IsIPv6InRange("2001:db8::50", "2001:db8::1", "2001:db8::100");
    /// Console.WriteLine(inRange); // true
    /// </code>
    /// </example>
    public static bool IsInRange(string ipv6Address, string startAddress, string endAddress)
    {
        if (!IsValid(ipv6Address) || !IsValid(startAddress) || !IsValid(endAddress))
            return false;

        try
        {
            var addressBytes = IPv6Converter.ToBytes(ipv6Address);
            var startBytes = IPv6Converter.ToBytes(startAddress);
            var endBytes = IPv6Converter.ToBytes(endAddress);

            return IPv6AddressManipulator.Compare(addressBytes, startBytes) >= 0 &&
                   IPv6AddressManipulator.Compare(addressBytes, endBytes) <= 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查两个IPv6地址是否在同一网段
    /// </summary>
    /// <param name="ipv6Address1">第一个IPv6地址</param>
    /// <param name="ipv6Address2">第二个IPv6地址</param>
    /// <param name="prefixLength">网段前缀长度</param>
    /// <returns>如果在同一网段返回true，否则返回false</returns>
    public static bool AreInSameNetwork(string ipv6Address1, string ipv6Address2, int prefixLength)
    {
        if (!IsValid(ipv6Address1) || !IsValid(ipv6Address2))
            return false;

        if (prefixLength < 0 || prefixLength > 128)
            return false;

        try
        {
            var bytes1 = IPv6Converter.ToBytes(ipv6Address1);
            var bytes2 = IPv6Converter.ToBytes(ipv6Address2);

            var bytesToCheck = prefixLength / 8;
            var bitsToCheck = prefixLength % 8;

            // 比较完整字节
            for (var i = 0; i < bytesToCheck; i++)
            {
                if (bytes1[i] != bytes2[i])
                    return false;
            }

            // 比较剩余位
            if (bitsToCheck > 0 && bytesToCheck < 16)
            {
                var mask = (byte)(0xFF << (8 - bitsToCheck));
                if ((bytes1[bytesToCheck] & mask) != (bytes2[bytesToCheck] & mask))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}