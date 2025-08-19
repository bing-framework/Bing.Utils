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
        RandomNumberGenerator.Fill(randomBytes);
#else
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
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