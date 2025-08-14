using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Bing.Net.IPv4;

/// <summary>
/// IPv4地址验证器
/// </summary>
/// <remarks>
/// 提供IPv4地址格式验证、类型判断等功能
/// </remarks>
public static class IPv4Validator
{
    /// <summary>
    /// IPv4地址正则表达式
    /// </summary>
    private static readonly Regex IPv4Regex = new(
        @"^((25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])\.){3}(25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])$",
        RegexOptions.Compiled);

    /// <summary>
    /// 内网IP地址范围定义
    /// </summary>
    private static readonly Lazy<(uint begin, uint end)[]> _internalRanges = new(() =>
    [
        (IPv4Converter.IpToUInt32("10.0.0.0"), IPv4Converter.IpToUInt32("10.255.255.255")),        // A类私有地址
        (IPv4Converter.IpToUInt32("172.16.0.0"), IPv4Converter.IpToUInt32("172.31.255.255")),      // B类私有地址  
        (IPv4Converter.IpToUInt32("192.168.0.0"), IPv4Converter.IpToUInt32("192.168.255.255")),    // C类私有地址
        (IPv4Converter.IpToUInt32("169.254.0.0"), IPv4Converter.IpToUInt32("169.254.255.255")),    // 链路本地地址
        (IPv4Converter.IpToUInt32("127.0.0.0"), IPv4Converter.IpToUInt32("127.255.255.255"))       // 回环地址
    ]);

    /// <summary>
    /// 内网IP地址范围定义
    /// </summary>
    private static (uint begin, uint end)[] InternalRanges => _internalRanges.Value;

    /// <summary>
    /// 验证是否为有效的IPv4地址
    /// </summary>
    /// <param name="ip">IP地址字符串</param>
    /// <returns>如果是有效的IPv4地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isIPv4 = IPv4Validator.IsValid("192.168.1.1");      // true
    /// bool isIPv4_2 = IPv4Validator.IsValid("2001:db8::1");    // false
    /// bool isIPv4_3 = IPv4Validator.IsValid("256.1.1.1");      // false
    /// bool isIPv4_4 = IPv4Validator.IsValid("192.168.01.1");   // false - 前导零无效
    /// </code>
    /// </example>
    public static bool IsValid(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;
        // 使用严格的正则表达式检查（不允许前导零）
        if (!IPv4Regex.IsMatch(ip))
            return false;
        // 使用 IPAddress.TryParse 进行额外验证
        if (!IPAddress.TryParse(ip, out var address) || address.AddressFamily != AddressFamily.InterNetwork)
            return false;
        // 确保解析后的地址与原始输入一致（防止格式化差异）
        return address.ToString() == ip;
    }

    /// <summary>
    /// 判断是否为本地回环地址
    /// </summary>
    /// <param name="ip">IP地址字符串</param>
    /// <returns>如果是本地回环地址返回true，否则返回false</returns>
    /// <remarks>
    /// 回环地址范围：127.0.0.0 - 127.255.255.255
    /// </remarks>
    /// <example>
    /// <code>
    /// bool isLocal1 = IPv4Validator.IsLocalIp("127.0.0.1");        // true
    /// bool isLocal3 = IPv4Validator.IsLocalIp("192.168.1.1");      // false
    /// </code>
    /// </example>
    public static bool IsLocalIp(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;
        if (!IsValid(ip))
            return false;
        return ip == "127.0.0.1" || ip.StartsWith("127.", StringComparison.Ordinal);
    }

    /// <summary>
    /// 判断给定的IP地址是否为内网地址
    /// </summary>
    /// <param name="ipAddress">要检查的IP地址字符串</param>
    /// <returns>如果是内网IP地址返回true，否则返回false</returns>
    /// <remarks>
    /// 内网IP地址范围包括：<br />
    /// - 10.0.0.0 - 10.255.255.255 (A类私有地址)<br />
    /// - 172.16.0.0 - 172.31.255.255 (B类私有地址)<br />
    /// - 192.168.0.0 - 192.168.255.255 (C类私有地址)<br />
    /// - 169.254.0.0 - 169.254.255.255 (链路本地地址)<br />
    /// - 127.0.0.0 - 127.255.255.255 (回环地址)
    /// </remarks>
    /// <example>
    /// <code>
    /// bool isInner1 = IPv4Validator.IsInnerIp("192.168.1.1");      // true
    /// bool isInner2 = IPv4Validator.IsInnerIp("8.8.8.8");          // false
    /// bool isInner3 = IPv4Validator.IsInnerIp("127.0.0.1");        // true
    /// </code>
    /// </example>
    public static bool IsInnerIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        if (!IsValid(ipAddress))
            return false;

        try
        {
            var ipNum = IPv4Converter.IpToUInt32(ipAddress);
            return InternalRanges.Any(range => ipNum >= range.begin && ipNum <= range.end);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 判断IP地址是否在指定的IP范围内
    /// </summary>
    /// <param name="ipAddress">要检查的IP地址</param>
    /// <param name="startIp">范围起始IP</param>
    /// <param name="endIp">范围结束IP</param>
    /// <returns>如果在范围内返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool inRange = IPv4Validator.IsInRange("192.168.1.100", "192.168.1.1", "192.168.1.254");
    /// Console.WriteLine($"在范围内: {inRange}");  // true
    /// </code>
    /// </example>
    public static bool IsInRange(string ipAddress, string startIp, string endIp)
    {
        if (!IsValid(ipAddress) || !IsValid(startIp) || !IsValid(endIp))
            return false;
        try
        {
            var ip = IPv4Converter.IpToUInt32(ipAddress);
            var start = IPv4Converter.IpToUInt32(startIp);
            var end = IPv4Converter.IpToUInt32(endIp);
            
            // 确保起始IP小于等于结束IP
            if (start > end)
                (start, end) = (end, start);

            return ip >= start && ip <= end;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 判断两个IP地址是否在同一子网内
    /// </summary>
    /// <param name="ip1">第一个IP地址</param>
    /// <param name="ip2">第二个IP地址</param>
    /// <param name="subnetMask">子网掩码</param>
    /// <returns>如果在同一子网内返回true，否则返回false</returns>
    /// <exception cref="ArgumentNullException">当IP地址或子网掩码为null或空时抛出</exception>
    /// <exception cref="ArgumentException">当IP地址或子网掩码格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// bool sameSubnet = IPv4Validator.IsInSameSubnet("192.168.1.10", "192.168.1.20", "255.255.255.0");
    /// Console.WriteLine($"在同一子网: {sameSubnet}"); // 输出: 在同一子网: True
    /// </code>
    /// </example>
    public static bool IsInSameSubnet(string ip1, string ip2, string subnetMask)
    {
        if (string.IsNullOrWhiteSpace(ip1))
            throw new ArgumentNullException(nameof(ip1), "第一个IP地址不能为空");
        if (string.IsNullOrWhiteSpace(ip2))
            throw new ArgumentNullException(nameof(ip2), "第二个IP地址不能为空");
        if (string.IsNullOrWhiteSpace(subnetMask))
            throw new ArgumentNullException(nameof(subnetMask), "子网掩码不能为空");

        if (!IsValid(ip1) || !IsValid(ip2) || !IsValid(subnetMask))
            throw new ArgumentException("IP地址或子网掩码格式无效");

        var network1 = IPv4Converter.GetNetworkAddress(ip1, subnetMask);
        var network2 = IPv4Converter.GetNetworkAddress(ip2, subnetMask);
        return network1 == network2;
    }

    /// <summary>
    /// 判断是否为公网地址
    /// </summary>
    /// <param name="ipAddress">要检查的IP地址字符串</param>
    /// <returns>如果是公网IP地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isPublic1 = IPv4Validator.IsPublicIp("8.8.8.8");          // true
    /// bool isPublic2 = IPv4Validator.IsPublicIp("192.168.1.1");      // false
    /// bool isPublic3 = IPv4Validator.IsPublicIp("114.114.114.114");  // true
    /// </code>
    /// </example>
    public static bool IsPublicIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        if (!IsValid(ipAddress))
            return false;

        return !IsInnerIp(ipAddress) && !IsReservedIp(ipAddress);
    }

    /// <summary>
    /// 判断是否为保留地址
    /// </summary>
    /// <param name="ipAddress">要检查的IP地址字符串</param>
    /// <returns>如果是保留IP地址返回true，否则返回false</returns>
    /// <remarks>
    /// 保留地址范围包括：<br />
    /// - 0.0.0.0/8 (本网络地址)<br />
    /// - 224.0.0.0/4 (组播地址)<br />
    /// - 240.0.0.0/4 (实验性地址)<br />
    /// - 255.255.255.255 (限制广播地址)
    /// </remarks>
    /// <example>
    /// <code>
    /// bool isReserved1 = IPv4Validator.IsReservedIp("0.0.0.0");        // true
    /// bool isReserved2 = IPv4Validator.IsReservedIp("224.0.0.1");      // true
    /// bool isReserved3 = IPv4Validator.IsReservedIp("255.255.255.255"); // true
    /// bool isReserved4 = IPv4Validator.IsReservedIp("192.168.1.1");    // false
    /// </code>
    /// </example>
    public static bool IsReservedIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        if (!IsValid(ipAddress))
            return false;

        try
        {
            var ipNum = IPv4Converter.IpToUInt32(ipAddress);

            // 0.0.0.0/8 - 本网络地址
            if (ipNum <= IPv4Converter.IpToUInt32("0.255.255.255"))
                return true;

            // 224.0.0.0/4 - 组播地址 (224.0.0.0 - 239.255.255.255)
            if (ipNum >= IPv4Converter.IpToUInt32("224.0.0.0") &&
                ipNum <= IPv4Converter.IpToUInt32("239.255.255.255"))
                return true;

            // 240.0.0.0/4 - 实验性地址 (240.0.0.0 - 255.255.255.255)
            if (ipNum >= IPv4Converter.IpToUInt32("240.0.0.0"))
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 判断是否为组播地址
    /// </summary>
    /// <param name="ipAddress">要检查的IP地址字符串</param>
    /// <returns>如果是组播IP地址返回true，否则返回false</returns>
    /// <remarks>
    /// 组播地址范围：224.0.0.0 - 239.255.255.255
    /// </remarks>
    /// <example>
    /// <code>
    /// bool isMulticast1 = IPv4Validator.IsMulticastIp("224.0.0.1");    // true
    /// bool isMulticast2 = IPv4Validator.IsMulticastIp("239.255.255.255"); // true
    /// bool isMulticast3 = IPv4Validator.IsMulticastIp("192.168.1.1");  // false
    /// </code>
    /// </example>
    public static bool IsMulticastIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        if (!IsValid(ipAddress))
            return false;

        try
        {
            var ipNum = IPv4Converter.IpToUInt32(ipAddress);
            return ipNum >= IPv4Converter.IpToUInt32("224.0.0.0") &&
                   ipNum <= IPv4Converter.IpToUInt32("239.255.255.255");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 判断是否为链路本地地址
    /// </summary>
    /// <param name="ipAddress">要检查的IP地址字符串</param>
    /// <returns>如果是链路本地IP地址返回true，否则返回false</returns>
    /// <remarks>
    /// 链路本地地址范围：169.254.0.0 - 169.254.255.255
    /// </remarks>
    /// <example>
    /// <code>
    /// bool isLinkLocal1 = IPv4Validator.IsLinkLocalIp("169.254.1.1");  // true
    /// bool isLinkLocal2 = IPv4Validator.IsLinkLocalIp("192.168.1.1");  // false
    /// </code>
    /// </example>
    public static bool IsLinkLocalIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        if (!IsValid(ipAddress))
            return false;

        try
        {
            var ipNum = IPv4Converter.IpToUInt32(ipAddress);
            return ipNum >= IPv4Converter.IpToUInt32("169.254.0.0") &&
                   ipNum <= IPv4Converter.IpToUInt32("169.254.255.255");
        }
        catch
        {
            return false;
        }
    }
}