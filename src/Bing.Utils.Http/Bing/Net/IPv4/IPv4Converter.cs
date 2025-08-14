using System.Net;
using System.Net.Sockets;

namespace Bing.Net.IPv4;

/// <summary>
/// IPv4地址转换器
/// </summary>
/// <remarks>
/// 提供IPv4地址与数值之间的转换功能
/// </remarks>
public static class IPv4Converter
{
    /// <summary>
    /// 将IPv4地址字符串转换为32位无符号整数
    /// </summary>
    /// <param name="ipAddress">IPv4地址字符串</param>
    /// <returns>32位无符号整数表示的IP地址</returns>
    /// <exception cref="ArgumentException">当IP地址格式无效时抛出</exception>
    /// <exception cref="ArgumentNullException">当IP地址格式为空时抛出</exception>
    /// <example>
    /// <code>
    /// uint ipNum = Ip.IpToUInt32("192.168.1.1");
    /// Console.WriteLine($"IP数值: {ipNum}"); // 输出: IP数值: 3232235777
    /// </code>
    /// </example>
    public static uint IpToUInt32(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentNullException(nameof(ipAddress));

        if (!IPAddress.TryParse(ipAddress, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException($"无效的IPv4地址格式: {ipAddress}", nameof(ipAddress));

        var bytes = ip.GetAddressBytes();
        // 网络字节序转换为主机字节序
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    /// <summary>
    /// 将32位无符号整数转换为IPv4地址字符串
    /// </summary>
    /// <param name="ipNum">32位无符号整数表示的IP地址</param>
    /// <returns>IPv4地址字符串</returns>
    /// <example>
    /// <code>
    /// string ip = IPv4Converter.UInt32ToIp(3232235777);
    /// Console.WriteLine(ip); // 输出: 192.168.1.1
    /// </code>
    /// </example>
    public static string UInt32ToIp(uint ipNum)
    {
        var bytes = BitConverter.GetBytes(ipNum);
        // 主机字节序转换为网络字节序
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return new IPAddress(bytes).ToString();
    }

    /// <summary>
    /// 获取IP地址的网络部分
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="subnetMask">子网掩码</param>
    /// <returns>网络地址</returns>
    /// <exception cref="ArgumentException">当IP地址或子网掩码格式无效时抛出</exception>
    /// <exception cref="ArgumentNullException">当IP地址或子网掩码为空时抛出</exception>
    /// <example>
    /// <code>
    /// string network = Ip.GetNetworkAddress("192.168.1.100", "255.255.255.0");
    /// Console.WriteLine($"网络地址: {network}");  // 输出: 网络地址: 192.168.1.0
    /// </code>
    /// </example>
    public static string GetNetworkAddress(string ipAddress, string subnetMask)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentNullException(nameof(ipAddress));
        if (string.IsNullOrWhiteSpace(subnetMask))
            throw new ArgumentNullException(nameof(subnetMask));

        if (!IPv4Validator.IsValid(ipAddress) || !IPv4Validator.IsValid(subnetMask))
            throw new ArgumentException("IP地址或子网掩码格式无效");

        var ipNum = IpToUInt32(ipAddress);
        var maskNum = IpToUInt32(subnetMask);
        var networkNum = ipNum & maskNum;
        return UInt32ToIp(networkNum);
    }

    /// <summary>
    /// 获取IP地址的广播地址
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="subnetMask">子网掩码</param>
    /// <returns>广播地址</returns>
    /// <exception cref="ArgumentException">当IP地址或子网掩码格式无效时抛出</exception>
    /// <exception cref="ArgumentNullException">当IP地址或子网掩码为空时抛出</exception>
    /// <example>
    /// <code>
    /// string broadcast = Ip.GetBroadcastAddress("192.168.1.100", "255.255.255.0");
    /// Console.WriteLine($"广播地址: {broadcast}");  // 输出: 广播地址: 192.168.1.255
    /// </code>
    /// </example>
    public static string GetBroadcastAddress(string ipAddress, string subnetMask)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentNullException(nameof(ipAddress));
        if (string.IsNullOrWhiteSpace(subnetMask))
            throw new ArgumentNullException(nameof(subnetMask));

        if (!IPv4Validator.IsValid(ipAddress) || !IPv4Validator.IsValid(subnetMask))
            throw new ArgumentException("IP地址或子网掩码格式无效");

        var ipNum = IpToUInt32(ipAddress);
        var maskNum = IpToUInt32(subnetMask);
        var broadcastNum = ipNum | ~maskNum;
        return UInt32ToIp(broadcastNum);
    }
}