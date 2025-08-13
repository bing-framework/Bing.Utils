using System.Net;
using System.Net.Sockets;

namespace Bing.Net;

/// <summary>
/// 通用IP地址验证器
/// </summary>
/// <remarks>
/// 提供IPv4和IPv6地址的通用验证功能
/// </remarks>
public static class IpValidator
{
    /// <summary>
    /// 验证IP地址格式是否有效（支持IPv4和IPv6）
    /// </summary>
    /// <param name="ip">IP地址字符串</param>
    /// <returns>如果是有效的IP地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isValid1 = IpValidator.IsValid("192.168.1.1");     // true
    /// bool isValid2 = IpValidator.IsValid("2001:db8::1");      // true
    /// bool isValid3 = IpValidator.IsValid("invalid");          // false
    /// </code>
    /// </example>
    public static bool IsValid(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;
        // 使用 IPAddress.TryParse 进行基本验证
        if (!IPAddress.TryParse(ip, out var address))
            return false;

        // 确保解析后的地址字符串与原始输入完全匹配
        // 这可以防止 "192.168.1" 被解析为 "192.168.1.0" 的情况
        var normalizedAddress = address.ToString();
        if (!string.Equals(normalizedAddress, ip, StringComparison.OrdinalIgnoreCase))
            return false;

        // 对于 IPv4 地址，检查是否有缺失的段
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            // IPv4 地址必须有4个段
            var parts = ip.Split('.');
            if (parts.Length != 4)
                return false;

            // 每个段必须是有效的数字
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part) || !int.TryParse(part, out var value) || value < 0 || value > 255)
                    return false;

                // 检查是否有前导零（除了单独的"0"）
                if (part.Length > 1 && part[0] == '0')
                    return false;
            }

            return true;
        }

        // 对于 IPv6 地址，使用标准验证
        return address.AddressFamily == AddressFamily.InterNetworkV6;
    }

    /// <summary>
    /// 验证是否为有效的IPv4地址
    /// </summary>
    /// <param name="ip">IP地址字符串</param>
    /// <returns>如果是有效的IPv4地址返回true，否则返回false</returns>
    public static bool IsValidIPv4(string ip) =>
        Bing.Net.IPv4.IPv4Validator.IsValid(ip);

    /// <summary>
    /// 验证是否为有效的IPv6地址
    /// </summary>
    /// <param name="ip">IP地址字符串</param>
    /// <returns>如果是有效的IPv6地址返回true，否则返回false</returns>
    public static bool IsValidIPv6(string ip) =>
        Bing.Net.IPv6.IPv6Validator.IsValid(ip);

    /// <summary>
    /// 判断是否为本地回环地址（IPv4或IPv6）
    /// </summary>
    /// <param name="ip">IP地址字符串</param>
    /// <returns>如果是本地回环地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isLocal1 = IpValidator.IsLocalIp("127.0.0.1");        // true
    /// bool isLocal2 = IpValidator.IsLocalIp("::1");              // true
    /// bool isLocal3 = IpValidator.IsLocalIp("192.168.1.1");      // false
    /// </code>
    /// </example>
    public static bool IsLocalIp(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;
        return IPv4.IPv4Validator.IsLocalIp(ip) || IPv6.IPv6Validator.IsLocalIp(ip);
    }

    /// <summary>
    /// 判断给定的IP地址是否为内网地址（IPv4或IPv6）
    /// </summary>
    /// <param name="ipAddress">要检查的IP地址字符串</param>
    /// <returns>如果是内网IP地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isInner1 = IpValidator.IsInnerIp("192.168.1.1");      // true
    /// bool isInner2 = IpValidator.IsInnerIp("8.8.8.8");          // false
    /// bool isInner3 = IpValidator.IsInnerIp("::1");              // true
    /// </code>
    /// </example>
    public static bool IsInnerIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;
        // 处理IPv6地址
        if (IPv6.IPv6Validator.IsValid(ipAddress))
            return IPv6.IPv6Validator.IsInnerIp(ipAddress);
        // 处理IPv4地址
        if (IPv4.IPv4Validator.IsValid(ipAddress))
            return IPv4.IPv4Validator.IsInnerIp(ipAddress);
        return false;
    }
}