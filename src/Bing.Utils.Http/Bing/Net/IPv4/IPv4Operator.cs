using System.Collections.Concurrent;
using System.Net.NetworkInformation;

namespace Bing.Net.IPv4;

/// <summary>
/// IPv4地址操作器
/// </summary>
/// <remarks>
/// 提供IPv4地址的高级操作功能，如排序、距离计算、地址递增等
/// </remarks>
public static class IPv4Operator
{
    /// <summary>
    /// 对IP地址列表进行排序
    /// </summary>
    /// <param name="ipAddresses">IP地址列表</param>
    /// <param name="ascending">是否升序排列，默认为true</param>
    /// <returns>排序后的IP地址列表</returns>
    /// <example>
    /// <code>
    /// var ips = new[] { "192.168.1.10", "192.168.1.2", "192.168.1.100" };
    /// var sortedIps = IPv4Operator.SortIpAddresses(ips);
    /// // 返回: ["192.168.1.2", "192.168.1.10", "192.168.1.100"]
    /// </code>
    /// </example>
    public static List<string> SortIpAddresses(IEnumerable<string> ipAddresses, bool ascending = true)
    {
        var validIps = ipAddresses.Where(IPv4Validator.IsValid).ToList();
        if (ascending)
            return validIps.OrderBy(IPv4Converter.IpToUInt32).ToList();
        return validIps.OrderByDescending(IPv4Converter.IpToUInt32).ToList();
    }

    /// <summary>
    /// 计算两个IP地址之间的数值距离
    /// </summary>
    /// <param name="ip1">第一个IP地址</param>
    /// <param name="ip2">第二个IP地址</param>
    /// <returns>IP地址之间的数值距离</returns>
    /// <example>
    /// <code>
    /// uint distance = IPv4Operator.GetIpDistance("192.168.1.1", "192.168.1.10");
    /// Console.WriteLine($"IP距离: {distance}"); // 9
    /// </code>
    /// </example>
    public static uint GetIpDistance(string ip1, string ip2)
    {
        if (!IPv4Validator.IsValid(ip1) || !IPv4Validator.IsValid(ip2))
            throw new ArgumentException("无效的IPv4地址");
        var num1 = IPv4Converter.IpToUInt32(ip1);
        var num2 = IPv4Converter.IpToUInt32(ip2);
        return num1 > num2 ? num1 - num2 : num2 - num1;
    }

    /// <summary>
    /// 获取指定IP地址的下一个IP地址
    /// </summary>
    /// <param name="ipAddress">当前IP地址</param>
    /// <param name="step">步长，默认为1</param>
    /// <returns>下一个IP地址</returns>
    /// <example>
    /// <code>
    /// string nextIp = IPv4Operator.GetNextIp("192.168.1.1", 5);
    /// Console.WriteLine(nextIp); // "192.168.1.6"
    /// </code>
    /// </example>
    public static string GetNextIp(string ipAddress, uint step = 1)
    {
        if (!IPv4Validator.IsValid(ipAddress))
            throw new ArgumentException("无效的IPv4地址", nameof(ipAddress));
        try
        {
            var ipNum = IPv4Converter.IpToUInt32(ipAddress);
            return IPv4Converter.UInt32ToIp(ipNum + step);
        }
        catch (OverflowException)
        {
            throw new ArgumentException("IP地址溢出");
        }
    }

    /// <summary>
    /// 获取指定IP地址的前一个IP地址
    /// </summary>
    /// <param name="ipAddress">当前IP地址</param>
    /// <param name="step">步长，默认为1</param>
    /// <returns>前一个IP地址</returns>
    /// <example>
    /// <code>
    /// string prevIp = IPv4Operator.GetPreviousIp("192.168.1.10", 3);
    /// Console.WriteLine(prevIp); // "192.168.1.7"
    /// </code>
    /// </example>
    public static string GetPreviousIp(string ipAddress, uint step = 1)
    {
        if (!IPv4Validator.IsValid(ipAddress))
            throw new ArgumentException("无效的IPv4地址", nameof(ipAddress));
        var ipNum = IPv4Converter.IpToUInt32(ipAddress);
        if (ipNum < step)
            throw new ArgumentException("IP地址下溢");
        return IPv4Converter.UInt32ToIp(ipNum - step);
    }

    /// <summary>
    /// 扫描指定IP段内活跃的IP地址
    /// </summary>
    /// <param name="cidr">CIDR网段，如 "192.168.1.0/24"</param>
    /// <param name="timeout">每个IP的ping超时时间，默认1秒</param>
    /// <param name="maxConcurrency">最大并发数，默认50</param>
    /// <returns>活跃的IP地址列表</returns>
    /// <example>
    /// <code>
    /// var activeIps = await IPv4Operations.ScanActiveIpsAsync("192.168.1.0/24");
    /// foreach (var ip in activeIps)
    /// {
    ///     Console.WriteLine($"活跃IP: {ip}");
    /// }
    /// </code>
    /// </example>
    public static async Task<List<string>> ScanActiveIpsAsync(string cidr, TimeSpan? timeout = null, int maxConcurrency = 50)
    {
        timeout ??= TimeSpan.FromSeconds(1);

        var ipList = IPv4CidrCalculator.GenerateIpRange(cidr);
        if (!ipList.Any())
            return new List<string>();

        var semaphore = new SemaphoreSlim(maxConcurrency);
        var activeIps = new ConcurrentBag<string>();

        var tasks = ipList.Select(async ip =>
        {
            await semaphore.WaitAsync();
            try
            {
                if (await IsIpReachableAsync(ip, timeout.Value))
                {
                    activeIps.Add(ip);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        semaphore.Dispose();

        return activeIps.OrderBy(IPv4Converter.IpToUInt32).ToList();
    }

    /// <summary>
    /// 检查IP地址是否可达（使用Ping）
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="timeout">超时时间</param>
    /// <returns>如果可达返回true，否则返回false</returns>
    public static async Task<bool> IsIpReachableAsync(string ipAddress, TimeSpan timeout)
    {
        if (!IPv4Validator.IsValid(ipAddress))
            return false;
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, (int)timeout.TotalMilliseconds);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}