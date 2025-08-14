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
    /// 默认Ping超时时间
    /// </summary>
    private const int DEFAULT_PING_TIMEOUT_SECONDS = 1;

    /// <summary>
    /// 默认最大并发数
    /// </summary>
    private const int DEFAULT_MAX_CONCURRENCY = 50;

    /// <summary>
    /// 对IP地址列表进行排序
    /// </summary>
    /// <param name="ipAddresses">IP地址列表</param>
    /// <param name="ascending">是否升序排列，默认为true</param>
    /// <returns>排序后的IP地址列表</returns>
    /// <exception cref="ArgumentNullException">当IP地址列表为null时抛出</exception>
    /// <example>
    /// <code>
    /// var ips = new[] { "192.168.1.10", "192.168.1.2", "192.168.1.100" };
    /// var sortedIps = IPv4Operator.SortIpAddresses(ips);
    /// // 返回: ["192.168.1.2", "192.168.1.10", "192.168.1.100"]
    /// </code>
    /// </example>
    public static List<string> SortIpAddresses(IEnumerable<string> ipAddresses, bool ascending = true)
    {
        if (ipAddresses == null)
            throw new ArgumentNullException(nameof(ipAddresses));

        var validIps = ipAddresses.Where(IPv4Validator.IsValid).ToList();
        if (ascending)
            return validIps.OrderBy(IPv4Converter.IpToUInt32).ToList();
        return validIps.OrderByDescending(IPv4Converter.IpToUInt32).ToList();
    }

    /// <summary>
    /// 计算两个IP地址之间的数值差
    /// </summary>
    /// <param name="ip1">第一个IP地址</param>
    /// <param name="ip2">第二个IP地址</param>
    /// <returns>IP地址之间的数值差（绝对值）</returns>
    /// <exception cref="ArgumentNullException">当IP地址为null或空白时抛出</exception>
    /// <exception cref="ArgumentException">当IP地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// uint distance = IPv4Operator.GetIpDistance("192.168.1.1", "192.168.1.10");
    /// Console.WriteLine($"IP距离: {distance}"); // 9
    /// </code>
    /// </example>
    public static uint GetIpDistance(string ip1, string ip2)
    {
        if (string.IsNullOrWhiteSpace(ip1))
            throw new ArgumentNullException(nameof(ip1));
        if (string.IsNullOrWhiteSpace(ip2))
            throw new ArgumentNullException(nameof(ip2));
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
    /// <exception cref="ArgumentException">当IP地址格式无效或IP地址溢出时抛出</exception>
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
            checked
            {
                var nextIpNum = ipNum + step;
                return IPv4Converter.UInt32ToIp(nextIpNum);
            }
        }
        catch (OverflowException)
        {
            throw new ArgumentException("IP地址溢出", nameof(step));
        }
    }

    /// <summary>
    /// 获取指定IP地址的前一个IP地址
    /// </summary>
    /// <param name="ipAddress">当前IP地址</param>
    /// <param name="step">步长，默认为1</param>
    /// <returns>前一个IP地址</returns>
    /// <exception cref="ArgumentException">当IP地址格式无效或IP地址下溢时抛出</exception>
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
            throw new ArgumentException("IP地址下溢", nameof(step));
        return IPv4Converter.UInt32ToIp(ipNum - step);
    }

    /// <summary>
    /// 扫描指定IP段内活跃的IP地址
    /// </summary>
    /// <param name="cidr">CIDR网段，如 "192.168.1.0/24"</param>
    /// <param name="timeout">每个IP的ping超时时间，默认1秒</param>
    /// <param name="maxConcurrency">最大并发数，默认50</param>
    /// <returns>活跃的IP地址列表</returns>
    /// <exception cref="ArgumentException">当CIDR格式无效时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当超时时间或并发数超出合理范围时抛出</exception>
    /// <example>
    /// <code>
    /// var activeIps = await IPv4Operations.ScanActiveIpsAsync("192.168.1.0/24");
    /// foreach (var ip in activeIps)
    /// {
    ///     Console.WriteLine($"活跃IP: {ip}");
    /// }
    /// </code>
    /// </example>
    public static async Task<List<string>> ScanActiveIpsAsync(string cidr, TimeSpan? timeout = null, int maxConcurrency = DEFAULT_MAX_CONCURRENCY)
    {
        timeout ??= TimeSpan.FromSeconds(DEFAULT_PING_TIMEOUT_SECONDS);

        if (timeout.Value.TotalMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeout), "超时时间必须大于0");
        if (maxConcurrency <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "最大并发数必须大于0");

        var ipList = IPv4CidrCalculator.GenerateIpRange(cidr);
        if (!ipList.Any())
            return new List<string>();

        using var semaphore = new SemaphoreSlim(maxConcurrency);
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

        return activeIps.OrderBy(IPv4Converter.IpToUInt32).ToList();
    }

    /// <summary>
    /// 检查IP地址是否可达（使用Ping）
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="timeout">超时时间</param>
    /// <returns>如果可达返回true，否则返回false</returns>
    /// <exception cref="ArgumentOutOfRangeException">当超时时间不合理时抛出</exception>
    /// <example>
    /// <code>
    /// bool isReachable = await IPv4Operator.IsIpReachableAsync("192.168.1.1", TimeSpan.FromSeconds(2));
    /// Console.WriteLine($"IP可达: {isReachable}");
    /// </code>
    /// </example>
    public static async Task<bool> IsIpReachableAsync(string ipAddress, TimeSpan timeout)
    {
        if (!IPv4Validator.IsValid(ipAddress))
            return false;
        if (timeout.TotalMilliseconds <= 0 || timeout.TotalMilliseconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(timeout), "超时时间必须在合理范围内");
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, (int)timeout.TotalMilliseconds);
            return reply.Status == IPStatus.Success;
        }
        catch(Exception)
        {
            return false;
        }
    }
}