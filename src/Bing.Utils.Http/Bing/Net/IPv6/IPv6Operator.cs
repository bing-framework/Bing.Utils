using System.Numerics;
using Bing.Net.IPv6.Internal;

namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址操作器
/// </summary>
/// <remarks>
/// 提供IPv6地址的高级操作功能，如排序、距离计算、地址递增等
/// </remarks>
public static class IPv6Operator
{
    /// <summary>
    /// 对IPv6地址列表进行排序
    /// </summary>
    /// <param name="ipv6Addresses">IPv6地址列表</param>
    /// <param name="ascending">是否升序排列，默认为true</param>
    /// <returns>排序后的IPv6地址列表</returns>
    public static List<string> Sort(IEnumerable<string> ipv6Addresses, bool ascending = true)
    {
        if (ipv6Addresses == null)
            return new List<string>();
        var validAddresses = ipv6Addresses.Where(IPv6Validator.IsValid).ToList();
        if (ascending)
            return validAddresses.OrderBy(IPv6Converter.ToBytes, new ByteArrayComparer()).ToList();
        return validAddresses.OrderByDescending(IPv6Converter.ToBytes, new ByteArrayComparer()).ToList();
    }

    /// <summary>
    /// 高性能排序（避免重复转换）
    /// </summary>
    /// <param name="ipv6Addresses">IPv6地址列表</param>
    /// <param name="ascending">是否升序排列</param>
    /// <returns>排序后的IPv6地址列表</returns>
    public static List<string> SortOptimized(IEnumerable<string> ipv6Addresses, bool ascending = true)
    {
        if (ipv6Addresses == null)
            return new List<string>();

        // 预先转换为字节数组，避免排序过程中重复转换
        var addressPairs = ipv6Addresses
            .Where(addr => !string.IsNullOrWhiteSpace(addr) && IPv6Validator.IsValid(addr))
            .Select(addr => new { Address = addr, Bytes = IPv6Converter.ToBytes(addr) })
            .ToList();

        var comparer = new ByteArrayComparer();
        var sorted = ascending
            ? addressPairs.OrderBy(pair => pair.Bytes, comparer)
            : addressPairs.OrderByDescending(pair => pair.Bytes, comparer);

        return sorted.Select(pair => pair.Address).ToList();
    }

    /// <summary>
    /// 查找最接近的IPv6地址
    /// </summary>
    /// <param name="targetAddress">目标地址</param>
    /// <param name="candidateAddresses">候选地址列表</param>
    /// <returns>最接近的地址</returns>
    public static string FindClosest(string targetAddress, IEnumerable<string> candidateAddresses)
    {
        if (!IPv6Validator.IsValid(targetAddress))
            throw new ArgumentException("无效的目标IPv6地址", nameof(targetAddress));

        if (candidateAddresses == null || !candidateAddresses.Any())
            throw new ArgumentException("候选地址列表不能为空", nameof(candidateAddresses));

        var validCandidates = candidateAddresses
            .Where(addr => !string.IsNullOrWhiteSpace(addr) && IPv6Validator.IsValid(addr))
            .ToList();

        if (!validCandidates.Any())
            throw new ArgumentException("没有有效的候选地址");

        return validCandidates
            .OrderBy(addr => GetIpDistance(targetAddress, addr))
            .First();
    }

    /// <summary>
    /// 计算两个IPv6地址之间的数值距离
    /// </summary>
    /// <param name="ipv6Address1">第一个IPv6地址</param>
    /// <param name="ipv6Address2">第二个IPv6地址</param>
    /// <returns>IPv6地址之间的距离（使用BigInteger表示）</returns>
    public static BigInteger GetIpDistance(string ipv6Address1, string ipv6Address2)
    {
        if (!IPv6Validator.IsValid(ipv6Address1) || !IPv6Validator.IsValid(ipv6Address2))
            throw new ArgumentException("无效的IPv6地址");

        var bytes1 = IPv6Converter.ToBytes(ipv6Address1);
        var bytes2 = IPv6Converter.ToBytes(ipv6Address2);

        // 将字节数组转换为BigInteger（大端序）
        var value1 = new System.Numerics.BigInteger(bytes1.Reverse().Concat(new byte[] { 0 }).ToArray());
        var value2 = new System.Numerics.BigInteger(bytes2.Reverse().Concat(new byte[] { 0 }).ToArray());

        return BigInteger.Abs(value1 - value2);
    }

    /// <summary>
    /// 获取IPv6地址的下一个地址
    /// </summary>
    /// <param name="ipv6Address">当前IPv6地址</param>
    /// <param name="step">步长，默认为1</param>
    /// <returns>下一个IPv6地址</returns>
    public static string GetNextIp(string ipv6Address, int step = 1)
    {
        if (!IPv6Validator.IsValid(ipv6Address))
            throw new ArgumentException("无效的IPv6地址", nameof(ipv6Address));
        if (step < 0)
            throw new ArgumentException("步长不能为负数", nameof(step));
        var addressBytes = IPv6Converter.ToBytes(ipv6Address);
        for (int i = 0; i < step; i++)
            IPv6AddressManipulator.Increment(addressBytes);
        return IPv6Converter.FromBytes(addressBytes);
    }

    /// <summary>
    /// 获取IPv6地址的前一个地址
    /// </summary>
    /// <param name="ipv6Address">当前IPv6地址</param>
    /// <param name="step">步长，默认为1</param>
    /// <returns>前一个IPv6地址</returns>
    public static string GetPreviousIp(string ipv6Address, int step = 1)
    {
        if (!IPv6Validator.IsValid(ipv6Address))
            throw new ArgumentException("无效的IPv6地址", nameof(ipv6Address));
        if (step < 0)
            throw new ArgumentException("步长不能为负数", nameof(step));
        var addressBytes = IPv6Converter.ToBytes(ipv6Address);
        for (int i = 0; i < step; i++)
            IPv6AddressManipulator.Decrement(addressBytes);
        return IPv6Converter.FromBytes(addressBytes);
    }

    /// <summary>
    /// 获取两个地址之间的中点地址
    /// </summary>
    /// <param name="address1">第一个地址</param>
    /// <param name="address2">第二个地址</param>
    /// <returns>中点地址</returns>
    public static string GetMidpoint(string address1, string address2)
    {
        if (!IPv6Validator.IsValid(address1) || !IPv6Validator.IsValid(address2))
            throw new ArgumentException("无效的IPv6地址");

        var value1 = IPv6Converter.ToBigInteger(address1);
        var value2 = IPv6Converter.ToBigInteger(address2);

        var midpoint = (value1 + value2) / 2;
        return IPv6Converter.FromBigInteger(midpoint);
    }

    /// <summary>
    /// 获取IPv6地址的网络前缀
    /// </summary>
    /// <param name="ipv6Address">IPv6地址</param>
    /// <param name="prefixLength">前缀长度</param>
    /// <returns>网络前缀地址</returns>
    public static string GetNetworkPrefix(string ipv6Address, int prefixLength)
    {
        if (!IPv6Validator.IsValid(ipv6Address))
            throw new ArgumentException("无效的IPv6地址", nameof(ipv6Address));

        if (prefixLength < 0 || prefixLength > 128)
            throw new ArgumentOutOfRangeException(nameof(prefixLength));

        var addressBytes = IPv6Converter.ToBytes(ipv6Address);
        IPv6AddressManipulator.ApplyNetworkMask(addressBytes, prefixLength);

        return IPv6Converter.FromBytes(addressBytes);
    }

    /// <summary>
    /// 获取IPv6地址列表的统计信息
    /// </summary>
    /// <param name="ipv6Addresses">IPv6地址列表</param>
    /// <returns>统计信息</returns>
    public static IPv6AddressStatistics GetStatistics(IEnumerable<string> ipv6Addresses)
    {
        var allAddresses = ipv6Addresses?.ToList() ?? new List<string>();
        var validAddresses = allAddresses
            .Where(addr => !string.IsNullOrWhiteSpace(addr) && IPv6Validator.IsValid(addr))
            .ToList();

        var statistics = new IPv6AddressStatistics
        {
            TotalCount = allAddresses.Count,
            ValidCount = validAddresses.Count,
            InvalidCount = allAddresses.Count - validAddresses.Count
        };

        if (!validAddresses.Any())
        {
            return statistics;
        }

        var sortedAddresses = Sort(validAddresses);
        statistics.SmallestAddress = sortedAddresses.First();
        statistics.LargestAddress = sortedAddresses.Last();
        statistics.AddressRange = GetIpDistance(sortedAddresses.First(), sortedAddresses.Last());
        statistics.AddressTypes = validAddresses
            .GroupBy(addr => IPv6AddressAnalyzer.GetAddressType(addr))
            .ToDictionary(g => g.Key, g => g.Count());

        return statistics;
    }

    /// <summary>
    /// 字节数组比较器
    /// </summary>
    private class ByteArrayComparer : IComparer<byte[]>
    {
        public int Compare(byte[] x, byte[] y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int minLength = Math.Min(x.Length, y.Length);
            for (int i = 0; i < minLength; i++)
            {
                if (x[i] < y[i]) return -1;
                if (x[i] > y[i]) return 1;
            }

            return x.Length.CompareTo(y.Length);
        }
    }
}