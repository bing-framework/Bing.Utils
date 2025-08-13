using Bing.Helpers;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Bing.Net.NetworkInformation;

namespace Bing.Net;

/// <summary>
/// IP地址提供器
/// </summary>
/// <remarks>
/// 提供本机和公网IP地址获取功能
/// </remarks>
public static class IpAddressProvider
{
    /// <summary>
    /// 当前线程的IP地址缓存
    /// </summary>
    private static readonly AsyncLocal<string> _ip = new();

    /// <summary>
    /// 设置当前线程的IP地址
    /// </summary>
    /// <param name="ip">IP地址字符串</param>
    /// <exception cref="ArgumentException">当IP地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// IpAddressProvider.SetIp("192.168.1.100");
    /// IpAddressProvider.SetIp("2001:db8::1");  // IPv6地址
    /// </code>
    /// </example>
    public static void SetIp(string ip)
    {
        if (!string.IsNullOrWhiteSpace(ip) && !IpValidator.IsValid(ip))
            throw new ArgumentException($"无效的IP地址格式: {ip}", nameof(ip));
        _ip.Value = ip;
    }

    /// <summary>
    /// 重置当前线程的IP地址缓存
    /// </summary>
    public static void Reset() => _ip.Value = null;

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    /// <returns>
    /// 返回客户端IP地址。优先级：<br />
    /// 1. 手动设置的IP地址<br />
    /// 2. HTTP上下文中的远程IP地址<br />
    /// 3. 本机局域网IP地址
    /// </returns>
    /// <example>
    /// <code>
    /// string clientIp = IpAddressProvider.GetIp();
    /// Console.WriteLine($"客户端IP: {clientIp}");
    /// </code>
    /// </example>
    public static string GetIp()
    {
        // 优先返回手动设置的IP
        if (!string.IsNullOrWhiteSpace(_ip.Value))
            return _ip.Value;
        // 尝试从HTTP上下文获取远程IP
        var remoteIp = GetRemoteIpFromContext();
        if (!string.IsNullOrWhiteSpace(remoteIp) && !IpValidator.IsLocalIp(remoteIp))
            return remoteIp;
        return GetLocalIp();
    }

    /// <summary>
    /// 从HTTP上下文获取远程IP地址
    /// </summary>
    /// <returns>远程IP地址字符串</returns>
    private static string GetRemoteIpFromContext()
    {
        try
        {
            var context = Web.HttpContext;
            if (context?.Connection?.RemoteIpAddress == null)
                return string.Empty;
            var remoteIp = context.Connection.RemoteIpAddress;
            // 处理IPv4映射的IPv6地址
            if (remoteIp.IsIPv4MappedToIPv6)
                remoteIp = remoteIp.MapToIPv4();
            return remoteIp.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取本机局域网IP地址
    /// </summary>
    /// <returns>局域网IP地址</returns>
    private static string GetLocalIp()
    {
        // Windows平台使用简单方法
        if (Env.IsWindows)
            return GetLanIp();
        // Linux/macOS平台使用网络接口方法
        return NetworkInterfaceManager.GetIpByInterface(NetworkInterfaceType.Ethernet);
    }

    /// <summary>
    /// 获取局域网IP（简单方法）
    /// </summary>
    /// <returns>局域网IP地址</returns>
    private static string GetLanIp()
    {
        try
        {
            var hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var address in hostAddresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork && !IpValidator.IsLocalIp(address.ToString()))
                    return address.ToString();
            }
        }
        catch
        {
            // 忽略异常
        }
        return string.Empty;
    }


    /// <summary>
    /// 获取本机所有IP地址
    /// </summary>
    /// <param name="includeIPv6">是否包含IPv6地址</param>
    /// <param name="includeLoopback">是否包含回环地址</param>
    /// <returns>本机IP地址列表</returns>
    /// <example>
    /// <code>
    /// var allIps = IpAddressProvider.GetAllLocalIps(includeIPv6: true, includeLoopback: false);
    /// foreach (var ip in allIps)
    /// {
    ///     Console.WriteLine($"本机IP: {ip}");
    /// }
    /// </code>
    /// </example>
    public static List<string> GetAllLocalIps(bool includeIPv6 = false, bool includeLoopback = false)
    {
        var result = new List<string>();
        try
        {
            var hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());

            foreach (var address in hostAddresses)
            {
                var addressStr = address.ToString();

                // 过滤条件
                if (!includeLoopback && IpValidator.IsLocalIp(addressStr))
                    continue;
                if (!includeIPv6 && address.AddressFamily == AddressFamily.InterNetworkV6)
                    continue;
                result.Add(addressStr);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取本机IP地址失败: {ex.Message}");    // 记录异常但不抛出，返回空列表
        }
        return result;
    }

    /// <summary>
    /// 获取本机的公网IP地址（通过外部服务）
    /// </summary>
    /// <param name="timeout">超时时间，默认5秒</param>
    /// <param name="useParallel">是否并行请求多个服务以提高成功率</param>
    /// <returns>公网IP地址，获取失败返回null</returns>
    /// <example>
    /// <code>
    /// string publicIp = await IpAddressProvider.GetPublicIpAsync();
    /// if (publicIp != null)
    /// {
    ///     Console.WriteLine($"公网IP: {publicIp}");
    /// }
    /// </code>
    /// </example>
    public static async Task<string> GetPublicIpAsync(TimeSpan? timeout = null, bool useParallel = false)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        // 备用的IP查询服务
        var services = new[]
        {
            "https://api.ipify.org",
            "https://icanhazip.com",
            "https://ipecho.net/plain",
            "https://myexternalip.com/raw",
            "https://checkip.amazonaws.com",
            "https://ip.42.pl/raw"
        };
        if (useParallel)
            return await GetPublicIpParallelAsync(services, timeout.Value);
        return await GetPublicIpSequentialAsync(services, timeout.Value);
    }

    /// <summary>
    /// 并行请求获取公网IP
    /// </summary>
    private static async Task<string> GetPublicIpParallelAsync(string[] services, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var tasks = services.Select(async service =>
        {
            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
#if NET5_0_OR_GREATER
                var response = await httpClient.GetStringAsync(service, cts.Token);
#else
                var response = await httpClient.GetStringAsync(service);
#endif
                var ip = response.Trim();

                if (IpValidator.IsValid(ip) && !IpValidator.IsInnerIp(ip))
                    return ip;
            }
            catch
            {
                // 忽略单个服务的失败
            }
            return null;
        });

        var results = await Task.WhenAll(tasks);
        return results.FirstOrDefault(ip => !string.IsNullOrEmpty(ip));
    }

    /// <summary>
    /// 顺序请求获取公网IP
    /// </summary>
    private static async Task<string> GetPublicIpSequentialAsync(string[] services, TimeSpan timeout)
    {
        using var httpClient = new HttpClient { Timeout = timeout };
        foreach (var service in services)
        {
            try
            {
                var response = await httpClient.GetStringAsync(service);
                var ip = response.Trim();

                if (IpValidator.IsValid(ip) && !IpValidator.IsInnerIp(ip))
                    return ip;
            }
            catch
            {
                continue; // 忽略单个服务的失败，尝试下一个
            }
        }
        return null;
    }
}