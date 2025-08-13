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
        var validAddresses = ipv6Addresses.Where(IPv6Validator.IsValid).ToList();
        if (ascending)
            return validAddresses.OrderBy(IPv6Converter.ToBytes, new ByteArrayComparer()).ToList();
        return validAddresses.OrderByDescending(IPv6Converter.ToBytes, new ByteArrayComparer()).ToList();
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