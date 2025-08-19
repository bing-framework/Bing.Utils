using Bing.Net.IPv6.Internal;
using System.Security.Cryptography;

namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址生成器
/// </summary>
/// <remarks>
/// 提供IPv6地址生成功能
/// </remarks>
public static class IPv6Generator
{
#if NET6_0_OR_GREATER
    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
#else
    private static readonly ThreadLocal<RandomNumberGenerator> _rng = new(() => RandomNumberGenerator.Create());
#endif

    /// <summary>
    /// 生成IPv6链路本地地址
    /// </summary>
    /// <param name="macAddress">MAC地址（可选，用于生成EUI-64标识符）</param>
    /// <returns>生成的IPv6链路本地地址</returns>
    /// <exception cref="ArgumentException">当MAC地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// // 基于MAC地址生成
    /// string linkLocal1 = IPv6Generator.GenerateLinkLocal("00:11:22:33:44:55");
    /// 
    /// // 随机生成
    /// string linkLocal2 = IPv6Generator.GenerateLinkLocal();
    /// </code>
    /// </example>
    public static string GenerateLinkLocal(string macAddress = null)
    {
        var linkLocalPrefix = new byte[16];
        linkLocalPrefix[0] = 0xfe;
        linkLocalPrefix[1] = 0x80;

        if (!string.IsNullOrWhiteSpace(macAddress) && Bing.Net.Mac.MacAddressHelper.IsValid(macAddress))
        {
            try
            {
                // 生成EUI-64标识符
                var eui64 = Bing.Net.Mac.MacAddressHelper.GenerateEUI64(macAddress);
                Array.Copy(eui64, 0, linkLocalPrefix, 8, 8);
            }
            catch (ArgumentException)
            {
                // MAC地址无效时回退到随机生成
                FillRandomBytes(linkLocalPrefix, 8, 8);
            }
        }
        else
        {
            // 使用加密级随机数生成器以提高安全性
            FillRandomBytes(linkLocalPrefix, 8, 8);
        }

        return IPv6Converter.FromBytes(linkLocalPrefix);
    }

    /// <summary>
    /// 生成随机IPv6地址
    /// </summary>
    /// <param name="prefix">前缀（可选）</param>
    /// <param name="prefixLength">前缀长度（0-128）</param>
    /// <returns>生成的IPv6地址</returns>
    /// <exception cref="ArgumentOutOfRangeException">当前缀长度超出有效范围时抛出</exception>
    /// <example>
    /// <code>
    /// // 完全随机
    /// string random1 = IPv6Generator.GenerateRandom();
    /// 
    /// // 带前缀
    /// string random2 = IPv6Generator.GenerateRandom("2001:db8::", 32);
    /// </code>
    /// </example>
    public static string GenerateRandom(string prefix = null, int prefixLength = 0)
    {
        if (prefixLength < 0 || prefixLength > 128)
            throw new ArgumentOutOfRangeException(nameof(prefixLength), "前缀长度必须在0-128之间");

        var addressBytes = new byte[16];

        if (!string.IsNullOrWhiteSpace(prefix) && IPv6Validator.IsValid(prefix) && prefixLength > 0)
        {
            var prefixBytes = IPv6Converter.ToBytes(prefix);
            var bytesToCopy = Math.Min(prefixLength / 8, 16);
            var bitsToMask = prefixLength % 8;

            // 复制完整字节
            Array.Copy(prefixBytes, addressBytes, bytesToCopy);

            // 处理部分字节
            if (bitsToMask > 0 && bytesToCopy < 16)
            {
                var mask = (byte)(0xFF << (8 - bitsToMask));
                var randomByte = GenerateRandomByte();
                addressBytes[bytesToCopy] = (byte)((prefixBytes[bytesToCopy] & mask) | (randomByte & ~mask));
                bytesToCopy++;
            }

            // 填充剩余字节
            if (bytesToCopy < 16) 
                FillRandomBytes(addressBytes, bytesToCopy, 16 - bytesToCopy);
        }
        else
        {
            // 完全随机生成
            FillRandomBytes(addressBytes, 0, 16);
        }

        return IPv6Converter.FromBytes(addressBytes);
    }

    /// <summary>
    /// 批量生成随机IPv6地址
    /// </summary>
    /// <param name="count">生成数量</param>
    /// <param name="prefix">前缀（可选）</param>
    /// <param name="prefixLength">前缀长度</param>
    /// <param name="ensureUnique">是否确保唯一性</param>
    /// <returns>生成的IPv6地址列表</returns>
    public static List<string> GenerateRandomBatch(int count, string prefix = null, int prefixLength = 0, bool ensureUnique = true)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "生成数量必须大于0");
        if (count > 10000)
            throw new ArgumentOutOfRangeException(nameof(count), "生成数量不能超过10000");

        var result = new List<string>();
        var generated = ensureUnique ? new HashSet<string>() : null;

        while (result.Count < count)
        {
            var address = GenerateRandom(prefix, prefixLength);

            if (!ensureUnique || generated.Add(address)) 
                result.Add(address);

            // 防止无限循环（当前缀范围太小时）
            if (ensureUnique && generated.Count > count * 100)
                throw new InvalidOperationException("无法在指定前缀范围内生成足够的唯一地址");
        }

        return result;
    }

    /// <summary>
    /// 生成唯一本地IPv6地址 (ULA, fc00::/7)
    /// </summary>
    /// <param name="globalId">全局ID（可选，5字节）</param>
    /// <param name="subnetId">子网ID（可选，2字节）</param>
    /// <returns>生成的唯一本地IPv6地址</returns>
    /// <example>
    /// <code>
    /// string ula = IPv6Generator.GenerateUniqueLocal();
    /// // 结果类似: fd12:3456:789a:bcde::1
    /// </code>
    /// </example>
    public static string GenerateUniqueLocal(byte[] globalId = null, byte[] subnetId = null)
    {
        var ulaBytes = new byte[16];

        // ULA前缀 fc00::/7，使用fd00::/8表示本地分配
        ulaBytes[0] = 0xfd;

        // 全局ID（5字节）
        if (globalId?.Length == 5)
            Array.Copy(globalId, 0, ulaBytes, 1, 5);
        else
            FillRandomBytes(ulaBytes, 1, 5);

        // 子网ID（2字节）
        if (subnetId?.Length == 2)
            Array.Copy(subnetId, 0, ulaBytes, 6, 2);
        else
            FillRandomBytes(ulaBytes, 6, 2);

        // 接口ID（8字节）- 可以基于MAC或随机
        FillRandomBytes(ulaBytes, 8, 8);

        return IPv6Converter.FromBytes(ulaBytes);
    }

    /// <summary>
    /// 生成IPv6组播地址
    /// </summary>
    /// <param name="scope">组播范围（0-F）</param>
    /// <param name="groupId">组播组ID（可选）</param>
    /// <returns>生成的IPv6组播地址</returns>
    /// <example>
    /// <code>
    /// string multicast = IPv6Generator.GenerateMulticast(0x2); // 链路本地组播
    /// </code>
    /// </example>
    public static string GenerateMulticast(byte scope = 0x2, byte[] groupId = null)
    {
        if (scope > 0xF)
            throw new ArgumentOutOfRangeException(nameof(scope), "组播范围必须在0-F之间");

        var multicastBytes = new byte[16];
        multicastBytes[0] = 0xFF;
        multicastBytes[1] = (byte)((0x0 << 4) | (scope & 0xF)); // 标志位=0，范围=scope

        // 保留字节2-15为0或使用指定的组ID
        if (groupId != null)
        {
            var copyLength = Math.Min(groupId.Length, 14);
            Array.Copy(groupId, 0, multicastBytes, 16 - copyLength, copyLength);
        }
        else
        {
            // 生成随机组ID（最后4字节）
            FillRandomBytes(multicastBytes, 12, 4);
        }

        return IPv6Converter.FromBytes(multicastBytes);
    }

    /// <summary>
    /// 生成IPv6全局单播地址
    /// </summary>
    /// <param name="prefix">全局前缀（默认使用2001:db8::用于文档目的）</param>
    /// <param name="prefixLength">前缀长度（默认32）</param>
    /// <returns>生成的IPv6全局单播地址</returns>
    public static string GenerateGlobalUnicast(string prefix = "2001:db8::", int prefixLength = 32) => GenerateRandom(prefix, prefixLength);

    /// <summary>
    /// 生成IPv6任播地址（与单播地址格式相同，但用于任播）
    /// </summary>
    /// <param name="prefix">前缀</param>
    /// <param name="prefixLength">前缀长度</param>
    /// <returns>生成的IPv6任播地址</returns>
    public static string GenerateAnycast(string prefix, int prefixLength)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("任播地址必须指定前缀", nameof(prefix));
        return GenerateRandom(prefix, prefixLength);
    }

    /// <summary>
    /// 生成基于MAC地址的IPv6地址（使用修改的EUI-64）
    /// </summary>
    /// <param name="macAddress">MAC地址</param>
    /// <param name="prefix">网络前缀（默认使用链路本地前缀）</param>
    /// <param name="prefixLength">前缀长度（默认64）</param>
    /// <returns>基于MAC的IPv6地址</returns>
    public static string GenerateFromMac(string macAddress, string prefix = "fe80::", int prefixLength = 64)
    {
        if (!Bing.Net.Mac.MacAddressHelper.IsValid(macAddress))
            throw new ArgumentException("无效的MAC地址格式", nameof(macAddress));

        if (prefixLength < 0 || prefixLength > 128)
            throw new ArgumentOutOfRangeException(nameof(prefixLength));

        var addressBytes = new byte[16];

        // 设置前缀
        if (!string.IsNullOrWhiteSpace(prefix) && IPv6Validator.IsValid(prefix))
        {
            var prefixBytes = IPv6Converter.ToBytes(prefix);
            var bytesToCopy = Math.Min(prefixLength / 8, 16);
            Array.Copy(prefixBytes, addressBytes, bytesToCopy);

            // 应用网络掩码确保前缀正确
            IPv6AddressManipulator.ApplyNetworkMask(addressBytes, prefixLength);
        }

        // 生成EUI-64标识符
        var eui64 = Bing.Net.Mac.MacAddressHelper.GenerateEUI64(macAddress);
        Array.Copy(eui64, 0, addressBytes, 8, 8);

        return IPv6Converter.FromBytes(addressBytes);
    }

    /// <summary>
    /// 生成IPv6地址范围
    /// </summary>
    /// <param name="startAddress">起始地址</param>
    /// <param name="endAddress">结束地址</param>
    /// <param name="maxCount">最大生成数量，防止内存溢出</param>
    /// <returns>地址范围列表</returns>
    public static List<string> GenerateRange(string startAddress, string endAddress, int maxCount = 1000)
    {
        if (!IPv6Validator.IsValid(startAddress) || !IPv6Validator.IsValid(endAddress))
            throw new ArgumentException("无效的IPv6地址");

        var result = new List<string>();
        var current = startAddress;
        var distance = IPv6Operator.GetIpDistance(startAddress, endAddress);

        if (distance > (ulong)maxCount)
            throw new ArgumentException($"地址范围过大，超过最大限制 {maxCount}");

        var compareResult = IPv6CidrCalculator.CompareIPv6Addresses(startAddress, endAddress);
        if (compareResult > 0)
            throw new ArgumentException("起始地址不能大于结束地址");

        while (IPv6CidrCalculator.CompareIPv6Addresses(current, endAddress) <= 0)
        {
            result.Add(current);
            if (result.Count >= maxCount)
                break;

            if (IPv6Converter.AreEqual(current, endAddress))
                break;

            current = IPv6Operator.GetNextIp(current);
        }

        return result;
    }

    /// <summary>
    /// 生成IPv6地址池
    /// </summary>
    /// <param name="poolSize">池大小</param>
    /// <param name="addressType">地址类型</param>
    /// <param name="prefix">前缀（可选）</param>
    /// <param name="prefixLength">前缀长度</param>
    /// <returns>IPv6地址池</returns>
    public static IPv6AddressPool GenerateAddressPool(int poolSize, IPv6AddressType addressType = IPv6AddressType.GlobalUnicast, string prefix = null, int prefixLength = 0)
    {
        var addresses = new List<string>();

        for (int i = 0; i < poolSize; i++)
        {
            string address = addressType switch
            {
                IPv6AddressType.LinkLocal => GenerateLinkLocal(),
                IPv6AddressType.UniqueLocal => GenerateUniqueLocal(),
                IPv6AddressType.Multicast => GenerateMulticast(),
                IPv6AddressType.GlobalUnicast => GenerateGlobalUnicast(prefix, prefixLength),
                _ => GenerateRandom(prefix, prefixLength)
            };

            addresses.Add(address);
        }

        return new IPv6AddressPool
        {
            Addresses = addresses,
            AddressType = addressType,
            Prefix = prefix,
            PrefixLength = prefixLength,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region 私有辅助方法

    /// <summary>
    /// 使用加密安全的随机数生成器填充字节数组
    /// </summary>
    /// <param name="buffer">目标缓冲区</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="count">字节数量</param>
    private static void FillRandomBytes(byte[] buffer, int offset, int count)
    {
        if (count <= 0) return;

        var randomBytes = new byte[count];

#if NET6_0_OR_GREATER
        lock (_rng)
        {
            _rng.GetBytes(randomBytes);
        }
#else
        _rng.Value.GetBytes(randomBytes);
#endif


        Array.Copy(randomBytes, 0, buffer, offset, count);
    }

    /// <summary>
    /// 生成单个随机字节
    /// </summary>
    /// <returns>随机字节</returns>
    private static byte GenerateRandomByte()
    {
        var buffer = new byte[1];
        FillRandomBytes(buffer, 0, 1);
        return buffer[0];
    }

    #endregion
}