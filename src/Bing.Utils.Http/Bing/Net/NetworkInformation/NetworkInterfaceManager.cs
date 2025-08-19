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

    /// <summary>
    /// 根据接口名称获取网络接口信息
    /// </summary>
    /// <param name="interfaceName">接口名称</param>
    /// <returns>网络接口信息，未找到返回null</returns>
    /// <example>
    /// <code>
    /// var ethInterface = NetworkInterfaceManager.GetNetworkInterfaceByName("以太网");
    /// </code>
    /// </example>
    public static NetworkInterfaceInfo GetNetworkInterfaceByName(string interfaceName)
    {
        if (string.IsNullOrWhiteSpace(interfaceName))
            return null;

        return GetNetworkInterfaces()
            .FirstOrDefault(i => i.Name.Equals(interfaceName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取指定IP地址对应的网络接口
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <returns>网络接口信息，未找到返回null</returns>
    /// <example>
    /// <code>
    /// var interface = NetworkInterfaceManager.GetNetworkInterfaceByIp("192.168.1.100");
    /// </code>
    /// </example>
    public static NetworkInterfaceInfo GetNetworkInterfaceByIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return null;

        return GetNetworkInterfaces()
            .FirstOrDefault(i => i.IpAddresses.Contains(ipAddress, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取活动的网络接口（有网关的接口）
    /// </summary>
    /// <returns>活动网络接口列表</returns>
    /// <example>
    /// <code>
    /// var activeInterfaces = NetworkInterfaceManager.GetActiveNetworkInterfaces();
    /// </code>
    /// </example>
    public static List<NetworkInterfaceInfo> GetActiveNetworkInterfaces()
    {
        return GetNetworkInterfaces()
            .Where(i => i.HasGateway)
            .OrderByDescending(i => i.Speed)
            .ToList();
    }

    /// <summary>
    /// 获取本机主要IP地址
    /// </summary>
    /// <param name="preferIPv4">是否优先返回IPv4地址，默认为true</param>
    /// <returns>主要IP地址</returns>
    /// <example>
    /// <code>
    /// string primaryIp = NetworkInterfaceManager.GetPrimaryIpAddress();
    /// string primaryIpv6 = NetworkInterfaceManager.GetPrimaryIpAddress(false);
    /// </code>
    /// </example>
    public static string GetPrimaryIpAddress(bool preferIPv4 = true)
    {
        try
        {
            var interfaces = GetNetworkInterfaces()
                .Where(i => i.HasGateway)
                .OrderByDescending(i => i.Speed)
                .ToList();

            if (!interfaces.Any())
                return string.Empty;

            if (preferIPv4)
            {
                // 优先返回IPv4地址
                var ipv4 = interfaces.SelectMany(i => i.IPv4Addresses).FirstOrDefault();
                if (!string.IsNullOrEmpty(ipv4))
                    return ipv4;

                // 如果没有IPv4，返回IPv6
                return interfaces.SelectMany(i => i.IPv6Addresses)
                    .FirstOrDefault(ip => !ip.StartsWith("fe80:") && !ip.StartsWith("::1")) ?? string.Empty;
            }
            else
            {
                // 优先返回IPv6（排除本地链路地址）
                var ipv6 = interfaces.SelectMany(i => i.IPv6Addresses)
                    .FirstOrDefault(ip => !ip.StartsWith("fe80:") && !ip.StartsWith("::1"));
                if (!string.IsNullOrEmpty(ipv6))
                    return ipv6;

                // 如果没有IPv6，返回IPv4
                return interfaces.SelectMany(i => i.IPv4Addresses).FirstOrDefault() ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取主要IP地址失败: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// 检查是否连接到互联网
    /// </summary>
    /// <param name="timeout">超时时间（毫秒），默认5000ms</param>
    /// <returns>是否连接到互联网</returns>
    /// <example>
    /// <code>
    /// bool isConnected = await NetworkInterfaceManager.IsConnectedToInternetAsync();
    /// </code>
    /// </example>
    public static async Task<bool> IsConnectedToInternetAsync(int timeout = 5000)
    {
        try
        {
            // 检查是否有活动的网络接口
            var activeInterfaces = GetNetworkInterfaces()
                .Where(i => i.HasGateway && (i.IPv4Addresses.Any() || i.IPv6Addresses.Any()))
                .ToList();

            if (!activeInterfaces.Any())
                return false;

            // 尝试连接到可靠的服务器
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(timeout);

            var response = await client.GetAsync("https://www.baidu.com");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取所有IPv4地址
    /// </summary>
    /// <param name="excludeLoopback">是否排除回环地址，默认为true</param>
    /// <returns>IPv4地址列表</returns>
    /// <example>
    /// <code>
    /// var ipv4Addresses = NetworkInterfaceManager.GetAllIPv4Addresses();
    /// </code>
    /// </example>
    public static List<string> GetAllIPv4Addresses(bool excludeLoopback = true)
    {
        var addresses = GetNetworkInterfaces()
            .SelectMany(i => i.IPv4Addresses)
            .Distinct()
            .ToList();

        if (excludeLoopback)
        {
            addresses = addresses.Where(ip => !ip.StartsWith("127.")).ToList();
        }

        return addresses;
    }

    /// <summary>
    /// 获取所有IPv6地址
    /// </summary>
    /// <param name="excludeLoopback">是否排除回环地址，默认为true</param>
    /// <param name="excludeLinkLocal">是否排除链路本地地址，默认为true</param>
    /// <returns>IPv6地址列表</returns>
    /// <example>
    /// <code>
    /// var ipv6Addresses = NetworkInterfaceManager.GetAllIPv6Addresses();
    /// </code>
    /// </example>
    public static List<string> GetAllIPv6Addresses(bool excludeLoopback = true, bool excludeLinkLocal = true)
    {
        var addresses = GetNetworkInterfaces()
            .SelectMany(i => i.IPv6Addresses)
            .Distinct()
            .ToList();

        if (excludeLoopback) 
            addresses = addresses.Where(ip => !ip.Equals("::1", StringComparison.OrdinalIgnoreCase)).ToList();
        if (excludeLinkLocal) 
            addresses = addresses.Where(ip => !ip.StartsWith("fe80:", StringComparison.OrdinalIgnoreCase)).ToList();
        return addresses;
    }

    /// <summary>
    /// 刷新网络接口信息缓存
    /// </summary>
    /// <returns>刷新后的网络接口信息列表</returns>
    /// <example>
    /// <code>
    /// var refreshedInterfaces = NetworkInterfaceManager.RefreshNetworkInterfaces();
    /// </code>
    /// </example>
    public static List<NetworkInterfaceInfo> RefreshNetworkInterfaces()
    {
        // 清除可能的缓存（如果有的话）
        GC.Collect();
        GC.WaitForPendingFinalizers();

        return GetNetworkInterfaces();
    }

    /// <summary>
    /// 检查指定端口是否在本地监听
    /// </summary>
    /// <param name="port">端口号</param>
    /// <param name="protocol">协议类型</param>
    /// <returns>是否在监听</returns>
    /// <example>
    /// <code>
    /// bool isListening = NetworkInterfaceManager.IsPortListening(80, ProtocolType.Tcp);
    /// </code>
    /// </example>
    public static bool IsPortListening(int port, ProtocolType protocol = ProtocolType.Tcp)
    {
        try
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();

            if (protocol == ProtocolType.Tcp)
            {
                var tcpListeners = properties.GetActiveTcpListeners();
                return tcpListeners.Any(ep => ep.Port == port);
            }
            else if (protocol == ProtocolType.Udp)
            {
                var udpListeners = properties.GetActiveUdpListeners();
                return udpListeners.Any(ep => ep.Port == port);
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"检查端口监听状态失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取网络统计信息
    /// </summary>
    /// <returns>网络统计信息</returns>
    /// <example>
    /// <code>
    /// var stats = NetworkInterfaceManager.GetNetworkStatistics();
    /// Console.WriteLine($"发送字节数: {stats.BytesSent}");
    /// </code>
    /// </example>
    public static NetworkStatistics GetNetworkStatistics()
    {
        try
        {
            var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .ToList();

            var totalBytesSent = 0L;
            var totalBytesReceived = 0L;
            var totalPacketsSent = 0L;
            var totalPacketsReceived = 0L;

            foreach (var networkInterface in interfaces)
            {
                var stats = networkInterface.GetIPStatistics();
                totalBytesSent += stats.BytesSent;
                totalBytesReceived += stats.BytesReceived;
                totalPacketsSent += stats.UnicastPacketsSent;
                totalPacketsReceived += stats.UnicastPacketsReceived;
            }

            return new NetworkStatistics
            {
                BytesSent = totalBytesSent,
                BytesReceived = totalBytesReceived,
                PacketsSent = totalPacketsSent,
                PacketsReceived = totalPacketsReceived,
                ActiveInterfaceCount = interfaces.Count
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取网络统计信息失败: {ex.Message}");
            return new NetworkStatistics();
        }
    }

    /// <summary>
    /// 测试网络连接质量
    /// </summary>
    /// <param name="targetHost">目标主机，默认为百度</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="retryCount">重试次数</param>
    /// <returns>网络质量信息</returns>
    public static async Task<NetworkQuality> TestNetworkQualityAsync(
        string targetHost = "www.baidu.com",
        int timeout = 5000,
        int retryCount = 3)
    {
        var quality = new NetworkQuality();
        var successful = 0;
        var totalLatency = 0L;
        var minLatency = long.MaxValue;
        var maxLatency = 0L;

        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMilliseconds(timeout);

                var response = await client.GetAsync($"https://{targetHost}");
                sw.Stop();

                if (response.IsSuccessStatusCode)
                {
                    successful++;
                    var latency = sw.ElapsedMilliseconds;
                    totalLatency += latency;
                    minLatency = Math.Min(minLatency, latency);
                    maxLatency = Math.Max(maxLatency, latency);
                }
            }
            catch
            {
                // 连接失败
            }

            if (i < retryCount - 1)
                await Task.Delay(1000); // 重试间隔
        }

        quality.SuccessRate = (double)successful / retryCount;
        quality.IsConnected = successful > 0;

        if (successful > 0)
        {
            quality.AverageLatency = totalLatency / successful;
            quality.MinLatency = minLatency;
            quality.MaxLatency = maxLatency;
            quality.PacketLoss = 1.0 - quality.SuccessRate;
        }

        // 评估连接质量
        if (quality.SuccessRate >= 0.9 && quality.AverageLatency <= 100)
            quality.QualityLevel = NetworkQualityLevel.Excellent;
        else if (quality.SuccessRate >= 0.7 && quality.AverageLatency <= 300)
            quality.QualityLevel = NetworkQualityLevel.Good;
        else if (quality.SuccessRate >= 0.5 && quality.AverageLatency <= 1000)
            quality.QualityLevel = NetworkQualityLevel.Fair;
        else
            quality.QualityLevel = NetworkQualityLevel.Poor;

        return quality;
    }

    /// <summary>
    /// 使用Ping测试网络延迟
    /// </summary>
    /// <param name="hostNameOrAddress">主机名或IP地址</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="retryCount">重试次数</param>
    /// <returns>Ping结果</returns>
    public static async Task<PingResult> PingAsync(
        string hostNameOrAddress = "8.8.8.8",
        int timeout = 5000,
        int retryCount = 4)
    {
        var result = new PingResult { TargetHost = hostNameOrAddress };
        var successful = 0;
        var totalTime = 0L;
        var minTime = long.MaxValue;
        var maxTime = 0L;
        var replies = new List<long>();

        using var ping = new Ping();

        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                var reply = await ping.SendPingAsync(hostNameOrAddress, timeout);

                if (reply.Status == IPStatus.Success)
                {
                    successful++;
                    var roundtripTime = reply.RoundtripTime;
                    replies.Add(roundtripTime);
                    totalTime += roundtripTime;
                    minTime = Math.Min(minTime, roundtripTime);
                    maxTime = Math.Max(maxTime, roundtripTime);
                }
            }
            catch
            {
                // Ping失败
            }
        }

        result.PacketsSent = retryCount;
        result.PacketsReceived = successful;
        result.PacketLoss = 1.0 - (double)successful / retryCount;

        if (successful > 0)
        {
            result.AverageRoundtripTime = totalTime / successful;
            result.MinRoundtripTime = minTime;
            result.MaxRoundtripTime = maxTime;

            // 计算抖动（jitter）
            if (replies.Count > 1)
            {
                var variance = replies.Select(t => Math.Pow(t - result.AverageRoundtripTime, 2)).Average();
                result.Jitter = Math.Sqrt(variance);
            }
        }

        return result;
    }

    /// <summary>
    /// 开始网络接口监控
    /// </summary>
    /// <param name="onNetworkChanged">网络状态变化回调</param>
    /// <param name="monitorInterval">监控间隔（毫秒），默认5秒</param>
    /// <returns>监控任务的取消令牌</returns>
    public static CancellationTokenSource StartNetworkMonitoring(
        Action<NetworkChangeEventArgs> onNetworkChanged,
        int monitorInterval = 5000)
    {
        var cts = new CancellationTokenSource();
        var previousInterfaces = GetNetworkInterfaces().ToDictionary(i => i.Name, i => i);

        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(monitorInterval, cts.Token);

                    var currentInterfaces = GetNetworkInterfaces().ToDictionary(i => i.Name, i => i);
                    var changes = DetectNetworkChanges(previousInterfaces, currentInterfaces);

                    if (changes.Any())
                    {
                        onNetworkChanged?.Invoke(new NetworkChangeEventArgs
                        {
                            Changes = changes,
                            Timestamp = DateTime.Now,
                            CurrentInterfaces = currentInterfaces.Values.ToList()
                        });
                    }

                    previousInterfaces = currentInterfaces;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"网络监控异常: {ex.Message}");
                }
            }
        }, cts.Token);

        return cts;
    }

    /// <summary>
    /// 检测网络变化
    /// </summary>
    private static List<NetworkChange> DetectNetworkChanges(
        Dictionary<string, NetworkInterfaceInfo> previous,
        Dictionary<string, NetworkInterfaceInfo> current)
    {
        var changes = new List<NetworkChange>();

        // 检测新增的接口
        foreach (var currentInterface in current.Values)
        {
            if (!previous.ContainsKey(currentInterface.Name))
            {
                changes.Add(new NetworkChange
                {
                    Type = NetworkChangeType.InterfaceAdded,
                    InterfaceName = currentInterface.Name,
                    Description = $"新增网络接口: {currentInterface.Name}"
                });
            }
        }

        // 检测移除的接口
        foreach (var previousInterface in previous.Values)
        {
            if (!current.ContainsKey(previousInterface.Name))
            {
                changes.Add(new NetworkChange
                {
                    Type = NetworkChangeType.InterfaceRemoved,
                    InterfaceName = previousInterface.Name,
                    Description = $"移除网络接口: {previousInterface.Name}"
                });
            }
        }

        // 检测IP地址变化
        foreach (var currentInterface in current.Values)
        {
            if (previous.TryGetValue(currentInterface.Name, out var previousInterface))
            {
                if (!currentInterface.IpAddresses.SequenceEqual(previousInterface.IpAddresses))
                {
                    changes.Add(new NetworkChange
                    {
                        Type = NetworkChangeType.IpAddressChanged,
                        InterfaceName = currentInterface.Name,
                        Description = $"接口 {currentInterface.Name} IP地址发生变化",
                        OldValue = string.Join(", ", previousInterface.IpAddresses),
                        NewValue = string.Join(", ", currentInterface.IpAddresses)
                    });
                }
            }
        }

        return changes;
    }
}