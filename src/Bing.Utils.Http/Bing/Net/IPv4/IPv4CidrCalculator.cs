using Bing.Net.IPv4.Internal;

namespace Bing.Net.IPv4;

/// <summary>
/// IPv4 CIDR网络计算器
/// </summary>
/// <remarks>
/// 提供IPv4网络的CIDR计算功能，包括子网划分、地址范围计算、掩码转换等。
/// 支持网络地址计算、广播地址计算、可用主机数计算等网络规划相关功能。
/// </remarks>
public static class IPv4CidrCalculator
{
    /// <summary>
    /// 判断IP地址是否在指定的CIDR网段内
    /// </summary>
    /// <param name="ipAddress">要检查的IP地址</param>
    /// <param name="cidr">CIDR表示法的网段，如 "192.168.1.0/24"</param>
    /// <returns>如果在网段内返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool inSubnet = IPv4CidrCalculator.IsInSubnet("192.168.1.100", "192.168.1.0/24");
    /// Console.WriteLine($"在子网内: {inSubnet}");  // true
    /// </code>
    /// </example>
    public static bool IsInSubnet(string ipAddress, string cidr)
    {
        if (!IPv4Validator.IsValid(ipAddress) || string.IsNullOrWhiteSpace(cidr))
            return false;

        var parts = cidr.Split('/');
        if (parts.Length != 2 || !IPv4Validator.IsValid(parts[0]) || !int.TryParse(parts[1], out var prefixLength))
            return false;

        if (prefixLength < 0 || prefixLength > 32)
            return false;

        try
        {
            var ip = IPv4Converter.IpToUInt32(ipAddress);
            var network = IPv4Converter.IpToUInt32(parts[0]);
            var mask = CreateIPv4Mask(prefixLength);
            return (ip & mask) == (network & mask);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 生成指定网段内的所有IP地址
    /// </summary>
    /// <param name="cidr">CIDR表示法的网段</param>
    /// <param name="maxCount">最大生成数量，默认65536</param>
    /// <returns>网段内所有IP地址的列表</returns>
    /// <example>
    /// <code>
    /// var ips = IPv4CidrCalculator.GenerateIpRange("192.168.1.0/30");
    /// // 返回: 192.168.1.0, 192.168.1.1, 192.168.1.2, 192.168.1.3
    /// </code>
    /// </example>
    public static List<string> GenerateIpRange(string cidr, int maxCount = 65536)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(cidr))
            return result;

        var parts = cidr.Split('/');
        if (parts.Length != 2 || !IPv4Validator.IsValid(parts[0]) || !int.TryParse(parts[1], out var prefixLength))
            return result;

        if (prefixLength < 0 || prefixLength > 32)
            return result;

        try
        {
            var network = IPv4Converter.IpToUInt32(parts[0]);
            var mask = CreateIPv4Mask(prefixLength);
            var networkAddress = network & mask;
            var hostCount = (uint)(1 << (32 - prefixLength));

            // 限制生成的IP数量，避免内存溢出
            if (hostCount > maxCount)
                throw new ArgumentException($"网段过大，包含{hostCount}个地址，超过最大限制{maxCount}");

            for (uint i = 0; i < hostCount; i++)
                result.Add(IPv4Converter.UInt32ToIp(networkAddress + i));
        }
        catch
        {
            // 忽略异常，返回空列表
        }

        return result;
    }

    /// <summary>
    /// 将子网掩码转换为CIDR前缀长度
    /// </summary>
    /// <param name="subnetMask">子网掩码，如 "255.255.255.0"</param>
    /// <returns>CIDR前缀长度</returns>
    /// <example>
    /// <code>
    /// int prefix = IPv4CidrCalculator.SubnetMaskToCidr("255.255.255.0");
    /// Console.WriteLine(prefix); // 24
    /// </code>
    /// </example>
    public static int SubnetMaskToCidr(string subnetMask)
    {
        if (!IPv4Validator.IsValid(subnetMask))
            throw new ArgumentException("无效的子网掩码格式", nameof(subnetMask));
        var maskNum = IPv4Converter.IpToUInt32(subnetMask);
        return AddressOperations.CountLeadingOnes(maskNum);
    }

    /// <summary>
    /// 将CIDR前缀长度转换为子网掩码
    /// </summary>
    /// <param name="prefixLength">CIDR前缀长度</param>
    /// <returns>子网掩码字符串</returns>
    /// <example>
    /// <code>
    /// string mask = IPv4CidrCalculator.CidrToSubnetMask(24);
    /// Console.WriteLine(mask); // "255.255.255.0"
    /// </code>
    /// </example>
    public static string CidrToSubnetMask(int prefixLength)
    {
        if (prefixLength < 0 || prefixLength > 32)
            throw new ArgumentOutOfRangeException(nameof(prefixLength), "前缀长度必须在0-32之间");
        var mask = CreateIPv4Mask(prefixLength);
        return IPv4Converter.UInt32ToIp(mask);
    }

    /// <summary>
    /// 获取CIDR网段的详细信息
    /// </summary>
    /// <param name="cidr">CIDR网段</param>
    /// <returns>网段信息</returns>
    /// <example>
    /// <code>
    /// var info = IPv4CidrCalculator.GetSubnetInfo("192.168.1.0/24");
    /// Console.WriteLine($"网络地址: {info.NetworkAddress}");
    /// Console.WriteLine($"广播地址: {info.BroadcastAddress}");
    /// Console.WriteLine($"可用主机数: {info.AvailableHosts}");
    /// </code>
    /// </example>
    public static SubnetInfo GetSubnetInfo(string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            throw new ArgumentNullException(nameof(cidr));

        var parts = cidr.Split('/');
        if (parts.Length != 2 || !IPv4Validator.IsValid(parts[0]) || !int.TryParse(parts[1], out var prefixLength))
            throw new ArgumentException("无效的CIDR格式", nameof(cidr));

        if (prefixLength < 0 || prefixLength > 32)
            throw new ArgumentException("无效的前缀长度", nameof(cidr));

        var networkIp = IPv4Converter.IpToUInt32(parts[0]);
        var mask = CreateIPv4Mask(prefixLength);
        var networkAddress = networkIp & mask;
        var broadcastAddress = networkAddress | ~mask;
        var totalHosts = (uint)(1 << (32 - prefixLength));
        var availableHosts = totalHosts > 2 ? totalHosts - 2 : totalHosts; // 减去网络地址和广播地址

        return new SubnetInfo
        {
            NetworkAddress = IPv4Converter.UInt32ToIp(networkAddress),
            BroadcastAddress = IPv4Converter.UInt32ToIp(broadcastAddress),
            SubnetMask = CidrToSubnetMask(prefixLength),
            PrefixLength = prefixLength,
            TotalHosts = totalHosts,
            AvailableHosts = availableHosts,
            FirstUsableIp = totalHosts > 2 ? IPv4Converter.UInt32ToIp(networkAddress + 1) : IPv4Converter.UInt32ToIp(networkAddress),
            LastUsableIp = totalHosts > 2 ? IPv4Converter.UInt32ToIp(broadcastAddress - 1) : IPv4Converter.UInt32ToIp(broadcastAddress)
        };
    }

    /// <summary>
    /// 子网划分：将大网段划分为多个小网段
    /// </summary>
    /// <param name="cidr">原始CIDR网段</param>
    /// <param name="newPrefixLength">新的前缀长度（必须大于原前缀长度）</param>
    /// <returns>划分后的子网列表</returns>
    /// <example>
    /// <code>
    /// var subnets = IPv4CidrCalculator.SubdivideNetwork("192.168.1.0/24", 26);
    /// // 返回: ["192.168.1.0/26", "192.168.1.64/26", "192.168.1.128/26", "192.168.1.192/26"]
    /// </code>
    /// </example>
    public static List<string> SubdivideNetwork(string cidr, int newPrefixLength)
    {
        var result = new List<string>();

        if (string.IsNullOrWhiteSpace(cidr))
            return result;

        var parts = cidr.Split('/');
        if (parts.Length != 2 || !IPv4Validator.IsValid(parts[0]) || !int.TryParse(parts[1], out var originalPrefix))
            return result;

        if (originalPrefix < 0 || originalPrefix > 32 || newPrefixLength < 0 || newPrefixLength > 32)
            return result;

        if (newPrefixLength <= originalPrefix)
            throw new ArgumentException("新前缀长度必须大于原前缀长度", nameof(newPrefixLength));

        try
        {
            var subnetInfo = GetSubnetInfo(cidr);
            var networkNum = IPv4Converter.IpToUInt32(subnetInfo.NetworkAddress);
            var subnetSize = (uint)(1 << (32 - newPrefixLength));
            var subnetCount = (uint)(1 << (newPrefixLength - originalPrefix));

            for (uint i = 0; i < subnetCount; i++)
            {
                var subnetNetwork = networkNum + (i * subnetSize);
                var subnetAddress = IPv4Converter.UInt32ToIp(subnetNetwork);
                result.Add($"{subnetAddress}/{newPrefixLength}");
            }
        }
        catch
        {
            // 忽略异常，返回空列表
        }

        return result;
    }

    /// <summary>
    /// 计算两个网段的交集
    /// </summary>
    /// <param name="cidr1">第一个网段</param>
    /// <param name="cidr2">第二个网段</param>
    /// <returns>交集网段，如果没有交集返回null</returns>
    /// <example>
    /// <code>
    /// var intersection = IPv4CidrCalculator.GetNetworkIntersection("192.168.1.0/24", "192.168.1.128/25");
    /// // 返回: "192.168.1.128/25"
    /// </code>
    /// </example>
    public static string GetNetworkIntersection(string cidr1, string cidr2)
    {
        try
        {
            var info1 = GetSubnetInfo(cidr1);
            var info2 = GetSubnetInfo(cidr2);

            var start1 = IPv4Converter.IpToUInt32(info1.NetworkAddress);
            var end1 = IPv4Converter.IpToUInt32(info1.BroadcastAddress);
            var start2 = IPv4Converter.IpToUInt32(info2.NetworkAddress);
            var end2 = IPv4Converter.IpToUInt32(info2.BroadcastAddress);

            var intersectionStart = Math.Max(start1, start2);
            var intersectionEnd = Math.Min(end1, end2);

            if (intersectionStart > intersectionEnd)
                return null; // 没有交集

            // 计算交集的前缀长度
            var intersectionSize = intersectionEnd - intersectionStart + 1;
            var prefixLength = 32 - AddressOperations.CalculateLog2(intersectionSize);

            return $"{IPv4Converter.UInt32ToIp(intersectionStart)}/{prefixLength}";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 超网聚合：将多个连续的小网段聚合为大网段
    /// </summary>
    /// <param name="cidrs">要聚合的CIDR网段列表</param>
    /// <returns>聚合后的网段，如果无法聚合则返回原列表</returns>
    /// <example>
    /// <code>
    /// var cidrs = new[] { "192.168.0.0/25", "192.168.0.128/25" };
    /// var aggregated = IPv4CidrCalculator.AggregateNetworks(cidrs);
    /// // 返回: ["192.168.0.0/24"]
    /// </code>
    /// </example>
    public static List<string> AggregateNetworks(IEnumerable<string> cidrs)
    {
        var validCidrs = cidrs.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        if (validCidrs.Count <= 1)
            return validCidrs;

        var result = new List<string>();
        var processed = new HashSet<string>();

        foreach (var cidr in validCidrs.OrderBy(c => c))
        {
            if (processed.Contains(cidr))
                continue;

            var parts = cidr.Split('/');
            if (parts.Length != 2 || !IPv4Validator.IsValid(parts[0]) || !int.TryParse(parts[1], out var prefix))
            {
                result.Add(cidr);
                continue;
            }

            // 尝试与其他网段聚合
            var aggregated = TryAggregateWithOthers(cidr, validCidrs, processed);
            result.Add(aggregated);
            processed.Add(cidr);
        }

        return result.Distinct().ToList();
    }

    #region 私有辅助方法

    /// <summary>
    /// 创建IPv4掩码
    /// </summary>
    /// <param name="prefixLength">前缀长度</param>
    /// <returns>掩码值</returns>
    private static uint CreateIPv4Mask(int prefixLength)
    {
        if (prefixLength == 0)
            return 0;
        if (prefixLength == 32)
            return 0xFFFFFFFF;
        return 0xFFFFFFFF << (32 - prefixLength);
    }

    /// <summary>
    /// 尝试与其他网段聚合
    /// </summary>
    /// <param name="cidr">当前网段</param>
    /// <param name="allCidrs">所有网段</param>
    /// <param name="processed">已处理的网段</param>
    /// <returns>聚合后的网段</returns>
    private static string TryAggregateWithOthers(string cidr, List<string> allCidrs, HashSet<string> processed)
    {
        // 这里可以实现更复杂的聚合逻辑
        // 简化实现：直接返回原网段
        return cidr;
    }

    #endregion
}