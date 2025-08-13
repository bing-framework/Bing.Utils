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
    public static string GenerateLinkLocal(string macAddress = null)
    {
        var linkLocalPrefix = new byte[16];
        linkLocalPrefix[0] = 0xfe;
        linkLocalPrefix[1] = 0x80;

        if (!string.IsNullOrWhiteSpace(macAddress) && Bing.Net.Mac.MacAddressHelper.IsValid(macAddress))
        {
            // 生成EUI-64标识符
            var eui64 = Bing.Net.Mac.MacAddressHelper.GenerateEUI64(macAddress);
            Array.Copy(eui64, 0, linkLocalPrefix, 8, 8);
        }
        else
        {
            var random = new Random();
            var randomBytes = new byte[8];
            random.NextBytes(randomBytes);
            Array.Copy(randomBytes, 0, linkLocalPrefix, 8, 8);
        }

        return IPv6Converter.FromBytes(linkLocalPrefix);
    }

    /// <summary>
    /// 生成随机IPv6地址
    /// </summary>
    /// <param name="prefix">前缀（可选）</param>
    /// <param name="prefixLength">前缀长度</param>
    /// <returns>生成的IPv6地址</returns>
    public static string GenerateRandom(string prefix = null, int prefixLength = 0)
    {
        var addressBytes = new byte[16];
        var random = new Random();

        if (!string.IsNullOrWhiteSpace(prefix) && IPv6Validator.IsValid(prefix))
        {
            var prefixBytes = IPv6Converter.ToBytes(prefix);
            var bytesToCopy = Math.Min(prefixLength / 8, 16);
            var bitsToMask = prefixLength % 8;

            Array.Copy(prefixBytes, addressBytes, bytesToCopy);

            if (bitsToMask > 0 && bytesToCopy < 16)
            {
                var mask = (byte)(0xFF << (8 - bitsToMask));
                addressBytes[bytesToCopy] = (byte)((prefixBytes[bytesToCopy] & mask) |
                                                   (random.Next(256) & ~mask));
                bytesToCopy++;
            }

            // 填充剩余字节
            var i = bytesToCopy;
            for (; i < 16; i++)
            {
                addressBytes[i] = (byte)random.Next(256);
            }
        }
        else
        {
            random.NextBytes(addressBytes);
        }

        return IPv6Converter.FromBytes(addressBytes);
    }
}