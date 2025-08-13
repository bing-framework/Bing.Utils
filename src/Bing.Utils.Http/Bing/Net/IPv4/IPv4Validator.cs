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
        @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$",
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
    /// </code>
    /// </example>
    public static bool IsValid(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;
        return IPv4Regex.IsMatch(ip) && IPAddress.TryParse(ip, out var address)
                                     && address.AddressFamily == AddressFamily.InterNetwork;
    }

    /// <summary>
    /// 判断是否为本地回环地址
    /// </summary>
    /// <param name="ip">IP地址字符串</param>
    /// <returns>如果是本地回环地址返回true，否则返回false</returns>
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
        return ip == "127.0.0.1" || ip.StartsWith("127.");
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
            return ip >= start && ip <= end;
        }
        catch
        {
            return false;
        }
    }
}