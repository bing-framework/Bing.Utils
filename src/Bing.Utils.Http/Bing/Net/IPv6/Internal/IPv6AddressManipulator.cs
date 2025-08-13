namespace Bing.Net.IPv6.Internal;

/// <summary>
/// IPv6地址操作器
/// </summary>
/// <remarks>
/// 提供IPv6地址字节数组的底层操作功能，包括地址递增、递减、掩码应用等
/// </remarks>
internal static class IPv6AddressManipulator
{
    /// <summary>
    /// 应用IPv6网络掩码
    /// </summary>
    /// <param name="addressBytes">地址字节数组（16字节）</param>
    /// <param name="prefixLength">前缀长度（0-128）</param>
    /// <exception cref="ArgumentNullException">当地址字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不为16时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当前缀长度超出有效范围时抛出</exception>
    /// <example>
    /// <code>
    /// var bytes = IPv6Converter.ToBytes("2001:db8:1234:5678:abcd:ef01:2345:6789");
    /// IPv6AddressManipulator.ApplyNetworkMask(bytes, 64);
    /// var networkAddress = IPv6Converter.FromBytes(bytes);
    /// Console.WriteLine(networkAddress); // "2001:db8:1234:5678::"
    /// </code>
    /// </example>
    public static void ApplyNetworkMask(byte[] addressBytes, int prefixLength)
    {
        ValidateAddressBytes(addressBytes);
        ValidatePrefixLength(prefixLength);

        var bytesToMask = prefixLength / 8;
        var bitsToMask = prefixLength % 8;

        // 清零主机部分
        for (var i = bytesToMask; i < 16; i++)
        {
            if (i == bytesToMask && bitsToMask > 0)
            {
                var mask = (byte)(0xFF << (8 - bitsToMask));
                addressBytes[i] &= mask;
            }
            else
            {
                addressBytes[i] = 0;
            }
        }
    }

    /// <summary>
    /// 设置IPv6地址的主机位
    /// </summary>
    /// <param name="addressBytes">地址字节数组（16字节）</param>
    /// <param name="prefixLength">前缀长度（0-128）</param>
    /// <param name="value">要设置的值</param>
    /// <exception cref="ArgumentNullException">当地址字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不为16时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当前缀长度超出有效范围时抛出</exception>
    /// <example>
    /// <code>
    /// var bytes = IPv6Converter.ToBytes("2001:db8::");
    /// IPv6AddressManipulator.SetHostBits(bytes, 64, 0xFF);
    /// var result = IPv6Converter.FromBytes(bytes);
    /// Console.WriteLine(result); // "2001:db8::ffff:ffff:ffff:ffff"
    /// </code>
    /// </example>
    public static void SetHostBits(byte[] addressBytes, int prefixLength, byte value)
    {
        ValidateAddressBytes(addressBytes);
        ValidatePrefixLength(prefixLength);

        var bytesToSet = prefixLength / 8;
        var bitsToSet = prefixLength % 8;

        // 设置主机部分
        for (var i = bytesToSet; i < 16; i++)
        {
            if (i == bytesToSet && bitsToSet > 0)
            {
                var mask = (byte)(0xFF >> bitsToSet);
                addressBytes[i] = (byte)((addressBytes[i] & ~mask) | (value & mask));
            }
            else
            {
                addressBytes[i] = value;
            }
        }
    }

    /// <summary>
    /// IPv6地址加1（地址递增）
    /// </summary>
    /// <param name="addressBytes">地址字节数组（16字节）</param>
    /// <exception cref="ArgumentNullException">当地址字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不为16时抛出</exception>
    /// <example>
    /// <code>
    /// var bytes = IPv6Converter.ToBytes("2001:db8::1");
    /// IPv6AddressManipulator.Increment(bytes);
    /// var result = IPv6Converter.FromBytes(bytes);
    /// Console.WriteLine(result); // "2001:db8::2"
    /// </code>
    /// </example>
    public static void Increment(byte[] addressBytes)
    {
        ValidateAddressBytes(addressBytes);

        for (var i = 15; i >= 0; i--)
        {
            if (addressBytes[i] < 0xFF)
            {
                addressBytes[i]++;
                break;
            }
            addressBytes[i] = 0;
        }
    }

    /// <summary>
    /// IPv6地址减1（地址递减）
    /// </summary>
    /// <param name="addressBytes">地址字节数组（16字节）</param>
    /// <exception cref="ArgumentNullException">当地址字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不为16时抛出</exception>
    /// <example>
    /// <code>
    /// var bytes = IPv6Converter.ToBytes("2001:db8::2");
    /// IPv6AddressManipulator.Decrement(bytes);
    /// var result = IPv6Converter.FromBytes(bytes);
    /// Console.WriteLine(result); // "2001:db8::1"
    /// </code>
    /// </example>
    public static void Decrement(byte[] addressBytes)
    {
        ValidateAddressBytes(addressBytes);

        for (var i = 15; i >= 0; i--)
        {
            if (addressBytes[i] > 0)
            {
                addressBytes[i]--;
                break;
            }
            addressBytes[i] = 0xFF;
        }
    }

    /// <summary>
    /// IPv6地址加指定步长
    /// </summary>
    /// <param name="addressBytes">地址字节数组（16字节）</param>
    /// <param name="step">步长</param>
    /// <exception cref="ArgumentNullException">当地址字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不为16或步长为负数时抛出</exception>
    /// <example>
    /// <code>
    /// var bytes = IPv6Converter.ToBytes("2001:db8::1");
    /// IPv6AddressManipulator.Add(bytes, 5);
    /// var result = IPv6Converter.FromBytes(bytes);
    /// Console.WriteLine(result); // "2001:db8::6"
    /// </code>
    /// </example>
    public static void Add(byte[] addressBytes, int step)
    {
        ValidateAddressBytes(addressBytes);
        if (step < 0)
            throw new ArgumentException("步长不能为负数", nameof(step));
        for (int i = 0; i < step; i++) 
            Increment(addressBytes);
    }

    /// <summary>
    /// IPv6地址减指定步长
    /// </summary>
    /// <param name="addressBytes">地址字节数组（16字节）</param>
    /// <param name="step">步长</param>
    /// <exception cref="ArgumentNullException">当地址字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不为16或步长为负数时抛出</exception>
    /// <example>
    /// <code>
    /// var bytes = IPv6Converter.ToBytes("2001:db8::10");
    /// IPv6AddressManipulator.Subtract(bytes, 3);
    /// var result = IPv6Converter.FromBytes(bytes);
    /// Console.WriteLine(result); // "2001:db8::d"
    /// </code>
    /// </example>
    public static void Subtract(byte[] addressBytes, int step)
    {
        ValidateAddressBytes(addressBytes);

        if (step < 0)
            throw new ArgumentException("步长不能为负数", nameof(step));
        for (int i = 0; i < step; i++) 
            Decrement(addressBytes);
    }

    /// <summary>
    /// 获取IPv6网段的第一个可用地址（字节数组版本）
    /// </summary>
    /// <param name="networkBytes">网络地址字节数组（16字节）</param>
    /// <param name="prefixLength">前缀长度（0-128）</param>
    /// <returns>第一个可用地址的字节数组</returns>
    /// <exception cref="ArgumentNullException">当网络地址字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不为16时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当前缀长度超出有效范围时抛出</exception>
    /// <example>
    /// <code>
    /// var networkBytes = IPv6Converter.ToBytes("2001:db8::");
    /// var firstBytes = IPv6AddressManipulator.GetFirstAddress(networkBytes, 64);
    /// var firstAddress = IPv6Converter.FromBytes(firstBytes);
    /// Console.WriteLine(firstAddress); // "2001:db8::1"
    /// </code>
    /// </example>
    public static byte[] GetFirstAddress(byte[] networkBytes, int prefixLength)
    {
        ValidateAddressBytes(networkBytes);
        ValidatePrefixLength(prefixLength);

        var result = new byte[16];
        Array.Copy(networkBytes, result, 16);

        // 应用网络掩码确保这是真正的网络地址
        ApplyNetworkMask(result, prefixLength);

        // 如果前缀长度是128，网络地址本身就是唯一地址
        if (prefixLength == 128)
            return result;

        // 增加1得到第一个主机地址
        Increment(result);

        return result;
    }

    /// <summary>
    /// 获取IPv6网段的最后一个可用地址（字节数组版本）
    /// </summary>
    /// <param name="networkBytes">网络地址字节数组（16字节）</param>
    /// <param name="prefixLength">前缀长度（0-128）</param>
    /// <returns>最后一个可用地址的字节数组</returns>
    /// <exception cref="ArgumentNullException">当网络地址字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不为16时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当前缀长度超出有效范围时抛出</exception>
    /// <example>
    /// <code>
    /// var networkBytes = IPv6Converter.ToBytes("2001:db8::");
    /// var lastBytes = IPv6AddressManipulator.GetLastAddress(networkBytes, 64);
    /// var lastAddress = IPv6Converter.FromBytes(lastBytes);
    /// Console.WriteLine(lastAddress); // "2001:db8::ffff:ffff:ffff:fffe"
    /// </code>
    /// </example>
    public static byte[] GetLastAddress(byte[] networkBytes, int prefixLength)
    {
        ValidateAddressBytes(networkBytes);
        ValidatePrefixLength(prefixLength);

        var result = new byte[16];
        Array.Copy(networkBytes, result, 16);

        // 应用网络掩码
        ApplyNetworkMask(result, prefixLength);

        // 如果前缀长度是128，网络地址本身就是唯一地址
        if (prefixLength == 128)
            return result;

        // 设置主机部分为全1，然后减1得到最后一个主机地址
        SetHostBits(result, prefixLength, 0xFF);
        Decrement(result);

        return result;
    }

    /// <summary>
    /// 比较两个IPv6地址字节数组
    /// </summary>
    /// <param name="bytes1">第一个地址字节数组</param>
    /// <param name="bytes2">第二个地址字节数组</param>
    /// <returns>比较结果：-1表示第一个地址小于第二个，0表示相等，1表示第一个地址大于第二个</returns>
    /// <exception cref="ArgumentNullException">当任一字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不为16时抛出</exception>
    /// <example>
    /// <code>
    /// var bytes1 = IPv6Converter.ToBytes("2001:db8::1");
    /// var bytes2 = IPv6Converter.ToBytes("2001:db8::2");
    /// int result = IPv6AddressManipulator.Compare(bytes1, bytes2);
    /// Console.WriteLine(result); // -1
    /// </code>
    /// </example>
    public static int Compare(byte[] bytes1, byte[] bytes2)
    {
        ValidateAddressBytes(bytes1);
        ValidateAddressBytes(bytes2);

        for (int i = 0; i < 16; i++)
        {
            if (bytes1[i] < bytes2[i]) return -1;
            if (bytes1[i] > bytes2[i]) return 1;
        }

        return 0;
    }

    /// <summary>
    /// 检查两个IPv6地址字节数组是否相等
    /// </summary>
    /// <param name="bytes1">第一个地址字节数组</param>
    /// <param name="bytes2">第二个地址字节数组</param>
    /// <returns>如果相等返回true，否则返回false</returns>
    /// <exception cref="ArgumentNullException">当任一字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不为16时抛出</exception>
    /// <example>
    /// <code>
    /// var bytes1 = IPv6Converter.ToBytes("2001:db8::1");
    /// var bytes2 = IPv6Converter.ToBytes("2001:0db8:0000:0000:0000:0000:0000:0001");
    /// bool equal = IPv6AddressManipulator.AreEqual(bytes1, bytes2);
    /// Console.WriteLine(equal); // true
    /// </code>
    /// </example>
    public static bool AreEqual(byte[] bytes1, byte[] bytes2)
    {
        ValidateAddressBytes(bytes1);
        ValidateAddressBytes(bytes2);

        return bytes1.SequenceEqual(bytes2);
    }

    /// <summary>
    /// 检查IPv6地址是否在指定网段内（字节数组版本）
    /// </summary>
    /// <param name="addressBytes">要检查的地址字节数组</param>
    /// <param name="networkBytes">网络地址字节数组</param>
    /// <param name="prefixLength">前缀长度</param>
    /// <returns>如果在网段内返回true，否则返回false</returns>
    /// <exception cref="ArgumentNullException">当任一字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不为16时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当前缀长度超出有效范围时抛出</exception>
    /// <example>
    /// <code>
    /// var addressBytes = IPv6Converter.ToBytes("2001:db8::1");
    /// var networkBytes = IPv6Converter.ToBytes("2001:db8::");
    /// bool inSubnet = IPv6AddressManipulator.IsInSubnet(addressBytes, networkBytes, 64);
    /// Console.WriteLine(inSubnet); // true
    /// </code>
    /// </example>
    public static bool IsInSubnet(byte[] addressBytes, byte[] networkBytes, int prefixLength)
    {
        ValidateAddressBytes(addressBytes);
        ValidateAddressBytes(networkBytes);
        ValidatePrefixLength(prefixLength);

        // 计算需要比较的字节数和位数
        var bytesToCheck = prefixLength / 8;
        var bitsToCheck = prefixLength % 8;

        // 比较完整字节
        for (var i = 0; i < bytesToCheck; i++)
        {
            if (addressBytes[i] != networkBytes[i])
                return false;
        }

        // 比较剩余位
        if (bitsToCheck > 0 && bytesToCheck < 16)
        {
            var mask = (byte)(0xFF << (8 - bitsToCheck));
            if ((addressBytes[bytesToCheck] & mask) != (networkBytes[bytesToCheck] & mask))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 复制IPv6地址字节数组
    /// </summary>
    /// <param name="source">源字节数组</param>
    /// <returns>复制的字节数组</returns>
    /// <exception cref="ArgumentNullException">当源字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不为16时抛出</exception>
    /// <example>
    /// <code>
    /// var original = IPv6Converter.ToBytes("2001:db8::1");
    /// var copy = IPv6AddressManipulator.Clone(original);
    /// </code>
    /// </example>
    public static byte[] Clone(byte[] source)
    {
        ValidateAddressBytes(source);

        var result = new byte[16];
        Array.Copy(source, result, 16);
        return result;
    }

    #region 私有验证方法

    /// <summary>
    /// 验证地址字节数组
    /// </summary>
    /// <param name="addressBytes">地址字节数组</param>
    /// <exception cref="ArgumentNullException">当字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不为16时抛出</exception>
    private static void ValidateAddressBytes(byte[] addressBytes)
    {
        if (addressBytes == null)
            throw new ArgumentNullException(nameof(addressBytes));

        if (addressBytes.Length != 16)
            throw new ArgumentException("IPv6地址字节数组必须为16字节", nameof(addressBytes));
    }

    /// <summary>
    /// 验证前缀长度
    /// </summary>
    /// <param name="prefixLength">前缀长度</param>
    /// <exception cref="ArgumentOutOfRangeException">当前缀长度超出有效范围时抛出</exception>
    private static void ValidatePrefixLength(int prefixLength)
    {
        if (prefixLength < 0 || prefixLength > 128)
            throw new ArgumentOutOfRangeException(nameof(prefixLength), "IPv6前缀长度必须在0-128之间");
    }

    #endregion
}