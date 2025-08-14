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
    /// 默认最大IP生成数量
    /// </summary>
    private const int DEFAULT_MAX_IP_COUNT = 65536;

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
            var mask = AddressOperations.CreateIPv4Mask(prefixLength);
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
    /// <exception cref="ArgumentException">当CIDR格式无效时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当网段过大超过限制时抛出</exception>
    /// <example>
    /// <code>
    /// var ips = IPv4CidrCalculator.GenerateIpRange("192.168.1.0/30");
    /// // 返回: 192.168.1.0, 192.168.1.1, 192.168.1.2, 192.168.1.3
    /// </code>
    /// </example>
    public static List<string> GenerateIpRange(string cidr, int maxCount = DEFAULT_MAX_IP_COUNT)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            throw new ArgumentException("CIDR不能为空", nameof(cidr));
        if (maxCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCount), "最大数量必须大于0");

        var parts = cidr.Split('/');
        if (parts.Length != 2 || !IPv4Validator.IsValid(parts[0]) || !int.TryParse(parts[1], out var prefixLength))
            throw new ArgumentException("无效的CIDR格式", nameof(cidr));
        if (prefixLength < 0 || prefixLength > 32)
            throw new ArgumentException("前缀长度必须在0-32之间", nameof(cidr));

        var network = IPv4Converter.IpToUInt32(parts[0]);
        var mask = AddressOperations.CreateIPv4Mask(prefixLength);
        var networkAddress = network & mask;
        var hostCount = (uint)(1 << (32 - prefixLength));

        // 限制生成的IP数量，避免内存溢出
        if (hostCount > maxCount)
            throw new ArgumentOutOfRangeException(nameof(maxCount), $"网段过大，包含{hostCount}个地址，超过最大限制{maxCount}");

        var result = new List<string>((int)hostCount);
        for (uint i = 0; i < hostCount; i++)
            result.Add(IPv4Converter.UInt32ToIp(networkAddress + i));

        return result;
    }

    /// <summary>
    /// 将子网掩码转换为CIDR前缀长度
    /// </summary>
    /// <param name="subnetMask">子网掩码，如 "255.255.255.0"</param>
    /// <returns>CIDR前缀长度</returns>
    /// <exception cref="ArgumentException">当子网掩码格式无效时抛出</exception>
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
        var prefix = AddressOperations.CountLeadingOnes(maskNum);

        // 验证掩码的有效性（连续的1后面必须是连续的0）
        var expectedMask = AddressOperations.CreateIPv4Mask(prefix);
        if (maskNum != expectedMask)
            throw new ArgumentException("无效的子网掩码，必须是连续的二进制位", nameof(subnetMask));

        return prefix;
    }

    /// <summary>
    /// 将CIDR前缀长度转换为子网掩码
    /// </summary>
    /// <param name="prefixLength">CIDR前缀长度</param>
    /// <returns>子网掩码字符串</returns>
    /// <exception cref="ArgumentOutOfRangeException">当前缀长度超出范围时抛出</exception>
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

        var mask = AddressOperations.CreateIPv4Mask(prefixLength);
        return IPv4Converter.UInt32ToIp(mask);
    }

    /// <summary>
    /// 获取CIDR网段的详细信息
    /// </summary>
    /// <param name="cidr">CIDR网段</param>
    /// <returns>网段信息</returns>
    /// <exception cref="ArgumentNullException">当CIDR为null或空白时抛出</exception>
    /// <exception cref="ArgumentException">当CIDR格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// var info = IPv4CidrCalculator.GetSubnetInfo("192.168.1.0/24");
    /// Console.WriteLine($"网络地址: {info.NetworkAddress}");
    /// Console.WriteLine($"广播地址: {info.BroadcastAddress}");
    /// Console.WriteLine($"可用主机数: {info.AvailableHosts}");
    /// </code>
    /// </example>
    public static IPv4SubnetInfo GetSubnetInfo(string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            throw new ArgumentNullException(nameof(cidr));

        var parts = cidr.Split('/');
        if (parts.Length != 2 || !IPv4Validator.IsValid(parts[0]) || !int.TryParse(parts[1], out var prefixLength))
            throw new ArgumentException("无效的CIDR格式", nameof(cidr));
        if (prefixLength < 0 || prefixLength > 32)
            throw new ArgumentException("无效的前缀长度", nameof(cidr));

        var networkIp = IPv4Converter.IpToUInt32(parts[0]);
        var mask = AddressOperations.CreateIPv4Mask(prefixLength);
        var networkAddress = networkIp & mask;
        var broadcastAddress = networkAddress | ~mask;
        var totalHosts = (uint)(1 << (32 - prefixLength));
        var availableHosts = totalHosts > 2 ? totalHosts - 2 : totalHosts; // 减去网络地址和广播地址

        return new IPv4SubnetInfo
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
    /// <exception cref="ArgumentNullException">当CIDR为null或空白时抛出</exception>
    /// <exception cref="ArgumentException">当CIDR格式无效或新前缀长度不合理时抛出</exception>
    /// <example>
    /// <code>
    /// var subnets = IPv4CidrCalculator.SubdivideNetwork("192.168.1.0/24", 26);
    /// // 返回: ["192.168.1.0/26", "192.168.1.64/26", "192.168.1.128/26", "192.168.1.192/26"]
    /// </code>
    /// </example>
    public static List<string> SubdivideNetwork(string cidr, int newPrefixLength)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            throw new ArgumentNullException(nameof(cidr));

        var parts = cidr.Split('/');
        if (parts.Length != 2 || !IPv4Validator.IsValid(parts[0]) || !int.TryParse(parts[1], out var originalPrefix))
            throw new ArgumentException("无效的CIDR格式", nameof(cidr));
        if (originalPrefix < 0 || originalPrefix > 32 || newPrefixLength < 0 || newPrefixLength > 32)
            throw new ArgumentException("前缀长度必须在0-32之间");
        if (newPrefixLength <= originalPrefix)
            throw new ArgumentException("新前缀长度必须大于原前缀长度", nameof(newPrefixLength));

        var subnetInfo = GetSubnetInfo(cidr);
        var networkNum = IPv4Converter.IpToUInt32(subnetInfo.NetworkAddress);
        var subnetSize = (uint)(1 << (32 - newPrefixLength));
        var subnetCount = (uint)(1 << (newPrefixLength - originalPrefix));

        var result = new List<string>((int)subnetCount);
        for (uint i = 0; i < subnetCount; i++)
        {
            var subnetNetwork = networkNum + (i * subnetSize);
            var subnetAddress = IPv4Converter.UInt32ToIp(subnetNetwork);
            result.Add($"{subnetAddress}/{newPrefixLength}");
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
        if (string.IsNullOrWhiteSpace(cidr1) || string.IsNullOrWhiteSpace(cidr2))
            return null;

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

            // 找到能包含整个交集范围的最小CIDR网段
            var intersectionSize = intersectionEnd - intersectionStart + 1;

            // 计算最大可能的前缀长度
            var maxPrefixLength = 32;
            var requiredSize = 1u;
            while (requiredSize < intersectionSize && maxPrefixLength > 0)
            {
                maxPrefixLength--;
                requiredSize <<= 1;
            }

            // 确保起始地址在该前缀长度下是网络边界对齐的
            var mask = AddressOperations.CreateIPv4Mask(maxPrefixLength);
            var alignedStart = intersectionStart & mask;

            return $"{IPv4Converter.UInt32ToIp(alignedStart)}/{maxPrefixLength}";
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
    /// <returns>聚合后的网段列表</returns>
    /// <exception cref="ArgumentNullException">当网段列表为null时抛出</exception>
    /// <remarks>
    /// 此方法会尝试将相邻的、可以合并的网段聚合为更大的网段，以减少路由表条目。
    /// 聚合条件：两个网段必须相邻且大小相同，聚合后的网段大小是原来的两倍。
    /// </remarks>
    /// <example>
    /// <code>
    /// var cidrs = new[] { "192.168.0.0/25", "192.168.0.128/25" };
    /// var aggregated = IPv4CidrCalculator.AggregateNetworks(cidrs);
    /// // 返回: ["192.168.0.0/24"]
    /// </code>
    /// </example>
    public static List<string> AggregateNetworks(IEnumerable<string> cidrs)
    {
        if (cidrs == null)
            throw new ArgumentNullException(nameof(cidrs));

        var validCidrs = cidrs.Where(c => !string.IsNullOrWhiteSpace(c) && IsValidCidrFormat(c)).ToList();
        if (validCidrs.Count <= 1)
            return validCidrs;

        // 按网络地址排序
        var sortedCidrs = validCidrs
            .Select(c => new CidrInfo(c, GetSubnetInfo(c)))
            .OrderBy(x => IPv4Converter.IpToUInt32(x.Info.NetworkAddress))
            .ToList();

        var result = new List<string>();
        var processed = new HashSet<string>();

        for (int i = 0; i < sortedCidrs.Count; i++)
        {
            var current = sortedCidrs[i];
            if (processed.Contains(current.Cidr))
                continue;

            // 尝试与下一个网段聚合
            var aggregated = TryAggregateWithNext(current, sortedCidrs, i + 1, processed);
            result.Add(aggregated);
            processed.Add(current.Cidr);
        }

        return result;
    }

    /// <summary>
    /// 判断两个CIDR网段是否相邻且可以聚合
    /// </summary>
    /// <param name="cidr1">第一个网段</param>
    /// <param name="cidr2">第二个网段</param>
    /// <returns>如果可以聚合返回聚合后的网段，否则返回null</returns>
    /// <example>
    /// <code>
    /// var aggregated = IPv4CidrCalculator.TryAggregate("192.168.0.0/25", "192.168.0.128/25");
    /// // 返回: "192.168.0.0/24"
    /// </code>
    /// </example>
    public static string TryAggregate(string cidr1, string cidr2)
    {
        if (string.IsNullOrWhiteSpace(cidr1) || string.IsNullOrWhiteSpace(cidr2))
            return null;

        try
        {
            var info1 = GetSubnetInfo(cidr1);
            var info2 = GetSubnetInfo(cidr2);

            // 必须是相同大小的网段
            if (info1.PrefixLength != info2.PrefixLength)
                return null;

            var net1 = IPv4Converter.IpToUInt32(info1.NetworkAddress);
            var net2 = IPv4Converter.IpToUInt32(info2.NetworkAddress);
            var size = info1.TotalHosts;

            // 检查是否相邻
            if (Math.Abs((long)net1 - (long)net2) != size)
                return null;

            // 检查是否可以在更小的前缀长度下对齐
            var newPrefixLength = info1.PrefixLength - 1;
            if (newPrefixLength < 0)
                return null;

            var newMask = AddressOperations.CreateIPv4Mask(newPrefixLength);
            var alignedNetwork = Math.Min(net1, net2) & newMask;

            return $"{IPv4Converter.UInt32ToIp(alignedNetwork)}/{newPrefixLength}";
        }
        catch
        {
            return null;
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// CIDR信息包装类
    /// </summary>
    /// <param name="Cidr">CIDR字符串</param>
    /// <param name="Info">子网信息</param>
    private readonly record struct CidrInfo(string Cidr, IPv4SubnetInfo Info);

    /// <summary>
    /// 验证CIDR格式是否有效
    /// </summary>
    /// <param name="cidr">CIDR字符串</param>
    /// <returns>格式是否有效</returns>
    private static bool IsValidCidrFormat(string cidr)
    {
        var parts = cidr.Split('/');
        return parts.Length == 2
               && IPv4Validator.IsValid(parts[0])
               && int.TryParse(parts[1], out var prefix)
               && prefix >= 0 && prefix <= 32;
    }

    /// <summary>
    /// 尝试与下一个网段聚合
    /// </summary>
    /// <param name="current">当前网段信息</param>
    /// <param name="allCidrs">所有网段列表</param>
    /// <param name="nextIndex">下一个网段的索引</param>
    /// <param name="processed">已处理的网段集合</param>
    /// <returns>聚合后的网段</returns>
    private static string TryAggregateWithNext(
        CidrInfo current,
        List<CidrInfo> allCidrs,
        int nextIndex,
        HashSet<string> processed)
    {
        if (nextIndex >= allCidrs.Count)
            return current.Cidr;

        var next = allCidrs[nextIndex];
        if (processed.Contains(next.Cidr))
            return current.Cidr;

        var aggregated = TryAggregate(current.Cidr, next.Cidr);
        if (aggregated != null)
        {
            processed.Add(next.Cidr);
            return aggregated;
        }

        return current.Cidr;
    }

    #endregion
}