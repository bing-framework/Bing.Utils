using Bing.Net.IPv6.Internal;

namespace Bing.Net.IPv6;

/// <summary>
/// IPv6网络分析器
/// </summary>
/// <remarks>
/// 提供IPv6网络相关的分析功能，包括网络归属判断、地址范围分析等
/// </remarks>
public static class IPv6NetworkAnalyzer
{
    /// <summary>
    /// 检查两个IPv6地址是否在同一网段
    /// </summary>
    /// <param name="ipv6Address1">第一个IPv6地址</param>
    /// <param name="ipv6Address2">第二个IPv6地址</param>
    /// <param name="prefixLength">网段前缀长度</param>
    /// <returns>如果在同一网段返回true，否则返回false</returns>
    public static bool AreInSameNetwork(string ipv6Address1, string ipv6Address2, int prefixLength)
    {
        if (!IPv6Validator.IsValid(ipv6Address1) || !IPv6Validator.IsValid(ipv6Address2))
            return false;

        if (prefixLength < 0 || prefixLength > 128)
            return false;

        try
        {
            var bytes1 = IPv6Converter.ToBytes(ipv6Address1);
            var bytes2 = IPv6Converter.ToBytes(ipv6Address2);

            var bytesToCheck = prefixLength / 8;
            var bitsToCheck = prefixLength % 8;

            // 比较完整字节
            for (var i = 0; i < bytesToCheck; i++)
            {
                if (bytes1[i] != bytes2[i])
                    return false;
            }

            // 比较剩余位
            if (bitsToCheck > 0 && bytesToCheck < 16)
            {
                var mask = (byte)(0xFF << (8 - bitsToCheck));
                if ((bytes1[bytesToCheck] & mask) != (bytes2[bytesToCheck] & mask))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
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
}