using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Bing.Net.NetworkInformation;

/// <summary>
/// 网络接口管理器
/// </summary>
/// <remarks>
/// 提供网络接口信息查询和管理功能
/// </remarks>
public static class NetworkInterfaceManager
{
    /// <summary>
    /// 获取指定网络接口类型的IP地址
    /// </summary>
    /// <param name="interfaceType">网络接口类型</param>
    /// <returns>指定类型网络接口的IP地址，未找到返回空字符串</returns>
    /// <remarks>
    /// 解决OSX下获取Ip地址产生"Device not configured"的问题
    /// 参考地址：https://stackoverflow.com/questions/6803073/get-local-ip-address/28621250#28621250
    /// </remarks>
    /// <example>
    /// <code>
    /// string ethernetIp = NetworkInterfaceManager.GetIpByInterface(NetworkInterfaceType.Ethernet);
    /// string wifiIp = NetworkInterfaceManager.GetIpByInterface(NetworkInterfaceType.Wireless80211);
    /// </code>
    /// </example>
    public static string GetIpByInterface(NetworkInterfaceType interfaceType)
    {
        try
        {
            var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

            foreach (var networkInterface in networkInterfaces)
            {
                if (networkInterface.NetworkInterfaceType != interfaceType ||
                    networkInterface.OperationalStatus != OperationalStatus.Up)
                    continue;
                var ipProperties = networkInterface.GetIPProperties();
                // 确保有网关（表示连接到网络）
                if (!ipProperties.GatewayAddresses.Any())
                    continue;
                foreach (var unicastAddress in ipProperties.UnicastAddresses)
                {
                    if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                        return unicastAddress.Address.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取网络接口IP失败: {ex.Message}");
        }
        return string.Empty;
    }

    /// <summary>
    /// 获取所有可用的网络接口信息
    /// </summary>
    /// <returns>网络接口信息列表</returns>
    /// <example>
    /// <code>
    /// var interfaces = NetworkInterfaceManager.GetNetworkInterfaces();
    /// foreach (var iface in interfaces)
    /// {
    ///     Console.WriteLine($"接口: {iface.Name}, IP: {iface.IpAddress}, 类型: {iface.Type}");
    /// }
    /// </code>
    /// </example>
    public static List<NetworkInterfaceInfo> GetNetworkInterfaces()
    {
        var result = new List<NetworkInterfaceInfo>();
        try
        {
            var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (var networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    continue;
                var ipProperties = networkInterface.GetIPProperties();
                var ipAddresses = new List<string>();
                var ipv4Addresses = new List<string>();
                var ipv6Addresses = new List<string>();
                foreach (var unicastAddress in ipProperties.UnicastAddresses)
                {
                    var addressStr = unicastAddress.Address.ToString();
                    ipAddresses.Add(addressStr);
                    switch (unicastAddress.Address.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            ipv4Addresses.Add(addressStr);
                            break;
                        case AddressFamily.InterNetworkV6:
                            ipv6Addresses.Add(addressStr);
                            break;
                    }
                }
                if (ipAddresses.Any())
                {
                    var gatewayAddresses = ipProperties.GatewayAddresses
                        .Select(g => g.Address.ToString()).ToList();
                    var dnsAddresses = ipProperties.DnsAddresses
                        .Select(d => d.ToString()).ToList();

                    result.Add(new NetworkInterfaceInfo
                    {
                        Name = networkInterface.Name,
                        Description = networkInterface.Description,
                        Type = networkInterface.NetworkInterfaceType,
                        Status = networkInterface.OperationalStatus,
                        IpAddresses = ipAddresses,
                        IPv4Addresses = ipv4Addresses,
                        IPv6Addresses = ipv6Addresses,
                        MacAddress = networkInterface.GetPhysicalAddress().ToString(),
                        HasGateway = ipProperties.GatewayAddresses.Any(),
                        GatewayAddresses = gatewayAddresses,
                        DnsAddresses = dnsAddresses,
                        Speed = networkInterface.Speed,
                        SupportsIPv6 = networkInterface.Supports(NetworkInterfaceComponent.IPv6)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取网络接口信息失败: {ex.Message}");
        }
        return result;
    }
}