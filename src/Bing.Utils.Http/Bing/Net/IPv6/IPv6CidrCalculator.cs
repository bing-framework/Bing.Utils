using Bing.Net.IPv6.Internal;

namespace Bing.Net.IPv6;

/// <summary>
/// IPv6 CIDR网络计算器
/// </summary>
/// <remarks>
/// 提供IPv6网络的CIDR计算功能，包括子网划分、地址范围计算、掩码转换等。
/// 支持网络地址计算、可用地址数计算等网络规划相关功能。
/// </remarks>
public static class IPv6CidrCalculator
{
    /// <summary>
    /// 判断IPv6地址是否在指定的IPv6子网内
    /// </summary>
    /// <param name="ipv6Address">要检查的IPv6地址</param>
    /// <param name="subnetCidr">IPv6子网CIDR表示法，如 "2001:db8::/32"</param>
    /// <returns>如果在子网内返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool inSubnet = IPv6CidrCalculator.IsInIPv6Subnet("2001:db8::1", "2001:db8::/32");
    /// Console.WriteLine(inSubnet); // true
    /// </code>
    /// </example>
    public static bool IsInIPv6Subnet(string ipv6Address, string subnetCidr)
    {
        if (!IPv6Validator.IsValid(ipv6Address) || string.IsNullOrWhiteSpace(subnetCidr))
            return false;

        var parts = subnetCidr.Split('/');
        if (parts.Length != 2 || !IPv6Validator.IsValid(parts[0]) || !int.TryParse(parts[1], out var prefixLength))
            return false;

        if (prefixLength < 0 || prefixLength > 128)
            return false;

        try
        {
            var addressBytes = IPv6Converter.ToBytes(ipv6Address);
            var networkBytes = IPv6Converter.ToBytes(parts[0]);

            return IPv6AddressManipulator.IsInSubnet(addressBytes, networkBytes, prefixLength);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取IPv6子网信息
    /// </summary>
    /// <param name="ipv6Cidr">IPv6 CIDR表示法</param>
    /// <returns>IPv6子网信息</returns>
    /// <example>
    /// <code>
    /// var info = IPv6CidrCalculator.GetIPv6SubnetInfo("2001:db8:1234:5678::/64");
    /// Console.WriteLine($"网络前缀: {info.NetworkPrefix}");
    /// Console.WriteLine($"子网掩码: {info.SubnetMask}");
    /// Console.WriteLine($"第一个地址: {info.FirstUsableAddress}");
    /// Console.WriteLine($"最后一个地址: {info.LastUsableAddress}");
    /// </code>
    /// </example>
    public static IPv6SubnetInfo GetIPv6SubnetInfo(string ipv6Cidr)
    {
        if (string.IsNullOrWhiteSpace(ipv6Cidr))
            throw new ArgumentNullException(nameof(ipv6Cidr));

        var parts = ipv6Cidr.Split('/');
        if (parts.Length != 2 || !IPv6Validator.IsValid(parts[0]) || !int.TryParse(parts[1], out var prefixLength))
            throw new ArgumentException("无效的IPv6 CIDR格式", nameof(ipv6Cidr));

        if (prefixLength < 0 || prefixLength > 128)
            throw new ArgumentException("无效的前缀长度", nameof(ipv6Cidr));

        var originalAddress = parts[0];
        var networkBytes = IPv6Converter.ToBytes(originalAddress);

        // 应用子网掩码获取真正的网络地址
        IPv6AddressManipulator.ApplyNetworkMask(networkBytes, prefixLength);
        var networkPrefix = IPv6Converter.FromBytes(networkBytes);

        // 计算地址数量
        var hostBits = 128 - prefixLength;
        var totalAddresses = hostBits >= 64 ? null : (ulong?)Math.Pow(2, hostBits);
        var availableAddresses = totalAddresses.HasValue && totalAddresses > 2 ? totalAddresses - 2 : totalAddresses;

        // 获取第一个和最后一个可用地址
        string firstUsableAddress = null;
        string lastUsableAddress = null;

        if (prefixLength < 128)
        {
            try
            {
                var firstBytes = IPv6AddressManipulator.GetFirstAddress(networkBytes, prefixLength);
                var lastBytes = IPv6AddressManipulator.GetLastAddress(networkBytes, prefixLength);
                firstUsableAddress = IPv6Converter.FromBytes(firstBytes);
                lastUsableAddress = IPv6Converter.FromBytes(lastBytes);
            }
            catch
            {
                // 对于非常大的网段，可能无法计算具体的首末地址
            }
        }
        else
        {
            // /128网段只有一个地址
            firstUsableAddress = lastUsableAddress = networkPrefix;
        }

        return new IPv6SubnetInfo
        {
            NetworkPrefix = networkPrefix,
            PrefixLength = prefixLength,
            SubnetMask = IPv6PrefixToSubnetMask(prefixLength),
            TotalAddresses = totalAddresses,
            AvailableAddresses = availableAddresses,
            AddressType = IPv6AddressAnalyzer.GetAddressType(networkPrefix),
            FirstUsableAddress = firstUsableAddress,
            LastUsableAddress = lastUsableAddress,
            HostBits = hostBits
        };
    }

    /// <summary>
    /// 生成IPv6网段内的地址列表（限制数量以避免内存问题）
    /// </summary>
    /// <param name="ipv6Cidr">IPv6 CIDR网段</param>
    /// <param name="maxCount">最大生成数量，默认1000</param>
    /// <returns>IPv6地址列表</returns>
    /// <example>
    /// <code>
    /// var addresses = IPv6CidrCalculator.GenerateIPv6Range("2001:db8::/126", 10);
    /// foreach (var addr in addresses)
    /// {
    ///     Console.WriteLine(addr);
    /// }
    /// // 输出: 2001:db8::, 2001:db8::1, 2001:db8::2, 2001:db8::3
    /// </code>
    /// </example>
    public static List<string> GenerateIPv6Range(string ipv6Cidr, int maxCount = 1000)
    {
        var result = new List<string>();

        if (string.IsNullOrWhiteSpace(ipv6Cidr))
            return result;

        try
        {
            var subnetInfo = GetIPv6SubnetInfo(ipv6Cidr);

            // 如果总地址数超过maxCount或者无法计算，只返回网络地址
            if (!subnetInfo.TotalAddresses.HasValue || subnetInfo.TotalAddresses > (ulong)maxCount)
            {
                result.Add(subnetInfo.NetworkPrefix);
                if (subnetInfo.FirstUsableAddress != null && subnetInfo.FirstUsableAddress != subnetInfo.NetworkPrefix)
                    result.Add(subnetInfo.FirstUsableAddress);
                return result;
            }

            var currentBytes = IPv6Converter.ToBytes(subnetInfo.NetworkPrefix);
            var count = Math.Min((int)subnetInfo.TotalAddresses.Value, maxCount);

            for (int i = 0; i < count; i++)
            {
                result.Add(IPv6Converter.FromBytes(currentBytes));
                if (i < count - 1) // 避免最后一次不必要的增加
                    IPv6AddressManipulator.Increment(currentBytes);
            }
        }
        catch
        {
            // 发生异常时返回空列表
        }

        return result;
    }

    /// <summary>
    /// 将IPv6前缀长度转换为子网掩码
    /// </summary>
    /// <param name="prefixLength">IPv6前缀长度（0-128）</param>
    /// <returns>IPv6子网掩码字符串</returns>
    /// <exception cref="ArgumentOutOfRangeException">当前缀长度超出有效范围时抛出</exception>
    /// <example>
    /// <code>
    /// string mask64 = IPv6CidrCalculator.IPv6PrefixToSubnetMask(64);
    /// Console.WriteLine(mask64); // "ffff:ffff:ffff:ffff::"
    /// 
    /// string mask48 = IPv6CidrCalculator.IPv6PrefixToSubnetMask(48);
    /// Console.WriteLine(mask48); // "ffff:ffff:ffff::"
    /// </code>
    /// </example>
    public static string IPv6PrefixToSubnetMask(int prefixLength)
    {
        if (prefixLength < 0 || prefixLength > 128)
            throw new ArgumentOutOfRangeException(nameof(prefixLength), "IPv6前缀长度必须在0-128之间");

        var maskBytes = new byte[16];
        var bytesToSet = prefixLength / 8;
        var bitsToSet = prefixLength % 8;

        // 设置完整字节为0xFF
        for (var i = 0; i < bytesToSet && i < 16; i++)
            maskBytes[i] = 0xFF;

        // 设置部分字节的位
        if (bitsToSet > 0 && bytesToSet < 16)
            maskBytes[bytesToSet] = (byte)(0xFF << (8 - bitsToSet));

        return IPv6Converter.FromBytes(maskBytes);
    }

    /// <summary>
    /// 将IPv6子网掩码转换为前缀长度
    /// </summary>
    /// <param name="subnetMask">IPv6子网掩码字符串</param>
    /// <returns>前缀长度</returns>
    /// <exception cref="ArgumentException">当子网掩码格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// int prefix1 = IPv6CidrCalculator.IPv6SubnetMaskToPrefix("ffff:ffff:ffff:ffff::");
    /// Console.WriteLine(prefix1); // 64
    /// 
    /// int prefix2 = IPv6CidrCalculator.IPv6SubnetMaskToPrefix("ffff:ffff:ffff::");
    /// Console.WriteLine(prefix2); // 48
    /// </code>
    /// </example>
    public static int IPv6SubnetMaskToPrefix(string subnetMask)
    {
        if (!IPv6Validator.IsValid(subnetMask))
            throw new ArgumentException("无效的IPv6子网掩码格式", nameof(subnetMask));

        var maskBytes = IPv6Converter.ToBytes(subnetMask);
        var prefixLength = 0;
        var foundZero = false;

        for (var i = 0; i < 16; i++)
        {
            var currentByte = maskBytes[i];

            if (currentByte == 0xFF && !foundZero)
            {
                // 完整的字节，增加8位
                prefixLength += 8;
            }
            else if (currentByte == 0x00)
            {
                // 零字节，标记已找到零
                foundZero = true;
            }
            else if (!foundZero)
            {
                // 部分字节，计算连续的1的个数
                var bits = AddressOperations.CountLeadingOnes(currentByte);
                prefixLength += bits;
                foundZero = true;

                // 验证剩余位是否都为0
                if ((currentByte & (0xFF >> bits)) != 0)
                    throw new ArgumentException("无效的IPv6子网掩码：包含非连续的位", nameof(subnetMask));
            }
            else
            {
                // 在找到零之后又找到非零字节，这是无效的掩码
                if (currentByte != 0x00)
                    throw new ArgumentException("无效的IPv6子网掩码：包含非连续的位", nameof(subnetMask));
            }
        }
        return prefixLength;
    }

    /// <summary>
    /// 验证IPv6子网掩码是否有效
    /// </summary>
    /// <param name="subnetMask">IPv6子网掩码字符串</param>
    /// <returns>如果是有效的IPv6子网掩码返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool valid1 = IPv6CidrCalculator.IsValidIPv6SubnetMask("ffff:ffff:ffff:ffff::");
    /// Console.WriteLine(valid1); // true
    /// 
    /// bool valid2 = IPv6CidrCalculator.IsValidIPv6SubnetMask("ffff:ff00:ffff::");
    /// Console.WriteLine(valid2); // false (非连续的位)
    /// </code>
    /// </example>
    public static bool IsValidIPv6SubnetMask(string subnetMask)
    {
        if (!IPv6Validator.IsValid(subnetMask))
            return false;

        try
        {
            IPv6SubnetMaskToPrefix(subnetMask);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 子网划分：将大IPv6网段划分为多个小网段
    /// </summary>
    /// <param name="ipv6Cidr">原始IPv6 CIDR网段</param>
    /// <param name="newPrefixLength">新的前缀长度（必须大于原前缀长度）</param>
    /// <returns>划分后的子网列表</returns>
    /// <example>
    /// <code>
    /// var subnets = IPv6CidrCalculator.SubdivideIPv6Network("2001:db8::/60", 64);
    /// // 返回: ["2001:db8::/64", "2001:db8:0:1::/64", "2001:db8:0:2::/64", ...]
    /// </code>
    /// </example>
    public static List<string> SubdivideIPv6Network(string ipv6Cidr, int newPrefixLength)
    {
        var result = new List<string>();

        if (string.IsNullOrWhiteSpace(ipv6Cidr))
            return result;

        var parts = ipv6Cidr.Split('/');
        if (parts.Length != 2 || !IPv6Validator.IsValid(parts[0]) || !int.TryParse(parts[1], out var originalPrefix))
            return result;

        if (originalPrefix < 0 || originalPrefix > 128 || newPrefixLength < 0 || newPrefixLength > 128)
            return result;

        if (newPrefixLength <= originalPrefix)
            throw new ArgumentException("新前缀长度必须大于原前缀长度", nameof(newPrefixLength));

        try
        {
            var subnetInfo = GetIPv6SubnetInfo(ipv6Cidr);
            var networkBytes = IPv6Converter.ToBytes(subnetInfo.NetworkPrefix);
            var subnetCount = (ulong)(1 << (newPrefixLength - originalPrefix));

            // 限制子网数量，避免内存溢出
            if (subnetCount > 10000)
                throw new ArgumentException($"子网数量过大：{subnetCount}，超过最大限制10000");

            for (ulong i = 0; i < subnetCount; i++)
            {
                var currentBytes = IPv6AddressManipulator.Clone(networkBytes);

                // 计算当前子网的网络地址
                for (ulong j = 0; j < i; j++)
                {
                    // 在特定位置增加子网编号
                    var subnetBitPosition = 128 - newPrefixLength;
                    AddToIPv6AtPosition(currentBytes, subnetBitPosition, 1);
                }

                var subnetAddress = IPv6Converter.FromBytes(currentBytes);
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
    /// 生成IPv6网段的第一个可用地址
    /// </summary>
    /// <param name="networkAddress">网络地址</param>
    /// <param name="prefixLength">前缀长度</param>
    /// <returns>第一个可用的IPv6地址</returns>
    /// <example>
    /// <code>
    /// string firstAddr = IPv6CidrCalculator.GetFirstIPv6Address("2001:db8::", 64);
    /// Console.WriteLine(firstAddr); // "2001:db8::1"
    /// </code>
    /// </example>
    public static string GetFirstIPv6Address(string networkAddress, int prefixLength)
    {
        if (!IPv6Validator.IsValid(networkAddress))
            throw new ArgumentException("无效的IPv6网络地址", nameof(networkAddress));

        if (prefixLength < 0 || prefixLength > 128)
            throw new ArgumentOutOfRangeException(nameof(prefixLength));

        var networkBytes = IPv6Converter.ToBytes(networkAddress);
        var firstBytes = IPv6AddressManipulator.GetFirstAddress(networkBytes, prefixLength);
        return IPv6Converter.FromBytes(firstBytes);
    }

    /// <summary>
    /// 生成IPv6网段的最后一个可用地址
    /// </summary>
    /// <param name="networkAddress">网络地址</param>
    /// <param name="prefixLength">前缀长度</param>
    /// <returns>最后一个可用的IPv6地址</returns>
    /// <example>
    /// <code>
    /// string lastAddr = IPv6CidrCalculator.GetLastIPv6Address("2001:db8::", 64);
    /// Console.WriteLine(lastAddr); // "2001:db8::ffff:ffff:ffff:fffe"
    /// </code>
    /// </example>
    public static string GetLastIPv6Address(string networkAddress, int prefixLength)
    {
        if (!IPv6Validator.IsValid(networkAddress))
            throw new ArgumentException("无效的IPv6网络地址", nameof(networkAddress));

        if (prefixLength < 0 || prefixLength > 128)
            throw new ArgumentOutOfRangeException(nameof(prefixLength));

        var networkBytes = IPv6Converter.ToBytes(networkAddress);
        var lastBytes = IPv6AddressManipulator.GetLastAddress(networkBytes, prefixLength);
        return IPv6Converter.FromBytes(lastBytes);
    }

    /// <summary>
    /// 比较两个IPv6地址的大小
    /// </summary>
    /// <param name="ipv6Address1">第一个IPv6地址</param>
    /// <param name="ipv6Address2">第二个IPv6地址</param>
    /// <returns>比较结果：-1表示第一个地址小于第二个，0表示相等，1表示第一个地址大于第二个</returns>
    /// <example>
    /// <code>
    /// int result = IPv6CidrCalculator.CompareIPv6Addresses("2001:db8::1", "2001:db8::2");
    /// Console.WriteLine(result); // -1
    /// </code>
    /// </example>
    public static int CompareIPv6Addresses(string ipv6Address1, string ipv6Address2)
    {
        if (!IPv6Validator.IsValid(ipv6Address1) || !IPv6Validator.IsValid(ipv6Address2))
            throw new ArgumentException("无效的IPv6地址");

        var bytes1 = IPv6Converter.ToBytes(ipv6Address1);
        var bytes2 = IPv6Converter.ToBytes(ipv6Address2);

        return IPv6AddressManipulator.Compare(bytes1, bytes2);
    }

    #region 私有辅助方法

    /// <summary>
    /// 在指定位置向IPv6地址添加值
    /// </summary>
    /// <param name="addressBytes">地址字节数组</param>
    /// <param name="bitPosition">位位置</param>
    /// <param name="value">要添加的值</param>
    private static void AddToIPv6AtPosition(byte[] addressBytes, int bitPosition, ulong value)
    {
        // 简化实现：直接在字节级别操作
        var bytePosition = bitPosition / 8;
        if (bytePosition < 16)
        {
            var currentValue = addressBytes[15 - bytePosition];
            addressBytes[15 - bytePosition] = (byte)((currentValue + value) & 0xFF);
        }
    }

    #endregion
}