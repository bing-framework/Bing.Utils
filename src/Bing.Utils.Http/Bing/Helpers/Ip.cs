using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using Bing.Extensions;
using Bing.OS;

namespace Bing.Helpers;

/// <summary>
/// Ip地址操作
/// </summary>
public static class Ip
{
    /// <summary>
    /// Ip地址
    /// </summary>
    private static readonly AsyncLocal<string> _ip = new();

    /// <summary>
    /// 设置Ip地址
    /// </summary>
    /// <param name="ip">Ip地址</param>
    public static void SetIp(string ip) => _ip.Value = ip;

    /// <summary>
    /// 重置Ip地址
    /// </summary>
    public static void Reset() => _ip.Value = null;

    /// <summary>
    /// 获取客户端Ip地址
    /// </summary>
    public static string GetIp()
    {
        if (!string.IsNullOrWhiteSpace(_ip.Value))
            return _ip.Value;
        var result = Web.HttpContext?.Connection.RemoteIpAddress.SafeString();
        if (string.IsNullOrWhiteSpace(result) || IsLocalIp(result))
            result = Platform.IsWindows ? GetLanIp() : GetLanIp(NetworkInterfaceType.Ethernet);
        return result;
    }

    /// <summary>
    /// 判断是否为本地IP地址
    /// </summary>
    /// <param name="ip">IP地址</param>
    /// <returns>如果是本地IP地址，则为 true；否则为 false。</returns>
    private static bool IsLocalIp(string ip) => ip == "127.0.0.1" || ip == "::1";

    /// <summary>
    /// 获取局域网Ip
    /// </summary>
    private static string GetLanIp()
    {
        foreach (var hostAddress in Dns.GetHostAddresses(Dns.GetHostName()))
        {
            if (hostAddress.AddressFamily == AddressFamily.InterNetwork)
                return hostAddress.ToString();
        }
        return string.Empty;
    }

    /// <summary>
    /// 获取局域网Ip。
    /// 参考地址：https://stackoverflow.com/questions/6803073/get-local-ip-address/28621250#28621250
    /// 解决OSX下获取Ip地址产生"Device not configured"的问题
    /// </summary>
    /// <param name="type">网络接口类型</param>
    private static string GetLanIp(NetworkInterfaceType type)
    {
        try
        {
            foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType != type || item.OperationalStatus != OperationalStatus.Up)
                    continue;
                var ipProperties = item.GetIPProperties();
                if (ipProperties.GatewayAddresses.FirstOrDefault() == null)
                    continue;
                foreach (var ip in ipProperties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        return ip.Address.ToString();
                }
            }
        }
        catch
        {
            return string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// 判断给定的IP地址是否为内部IP地址。
    /// </summary>
    /// <param name="ipAddress">要检查的IP地址字符串</param>
    /// <returns>如果是内部IP地址，则为 true；否则为 false。</returns>
    public static bool IsInnerIp(string ipAddress)
    {
        var ipNum = GetIpNum(ipAddress);
        // 定义内部IP地址范围。
        var internalRanges = new (long begin, long end)[]
        {
            (GetIpNum("10.0.0.0"), GetIpNum("10.255.255.255")),      // A类
            (GetIpNum("172.16.0.0"), GetIpNum("172.31.255.255")),    // B类
            (GetIpNum("192.168.0.0"), GetIpNum("192.168.255.255"))   // C类
        };
        // 判断给定的IP地址是否在任何一个内部IP地址范围内，或者是否为本地回环地址（127.0.0.1）
        return internalRanges.Any(range => IsInner(ipNum, range.begin, range.end)) || ipAddress == "127.0.0.1";
    }

    /// <summary>
    /// 将IP地址转换为long。
    /// </summary>
    /// <param name="ipAddress">IP地址字符串</param>
    /// <returns>IP地址的long表示。</returns>
    private static long GetIpNum(string ipAddress)
    {
        if (IPAddress.TryParse(ipAddress, out var ip))
        {
            var ipBytes = ip.GetAddressBytes();
            // 如果系统是小端序（Little Endian），则翻转字节数组，以确保正确的顺序。
            if (BitConverter.IsLittleEndian)
                Array.Reverse(ipBytes);
            return BitConverter.ToInt32(ipBytes, 0);
        }
        throw new ArgumentException($@"Invalid IP address format: {ipAddress}", nameof(ipAddress));
    }

    /// <summary>
    /// 判断用户IP是否在指定范围内。
    /// </summary>
    /// <param name="userIp">用户的IP地址（长整型表示）</param>
    /// <param name="begin">IP范围的起始值</param>
    /// <param name="end">IP范围的结束值</param>
    /// <returns>如果用户IP在指定范围内，则为 true；否则为 false。</returns>
    private static bool IsInner(long userIp, long begin, long end) => userIp >= begin && userIp <= end;
}