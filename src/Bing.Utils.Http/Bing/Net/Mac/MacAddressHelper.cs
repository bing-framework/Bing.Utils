using System.Text;
using System.Text.RegularExpressions;

namespace Bing.Net.Mac;

/// <summary>
/// MAC地址帮助类
/// </summary>
/// <remarks>
/// 提供MAC地址格式化、验证等功能
/// </remarks>
public static class MacAddressHelper
{
    /// <summary>
    /// 解析MAC地址为字节数组
    /// </summary>
    /// <param name="macAddress">MAC地址字符串</param>
    /// <returns>6字节的MAC地址数组</returns>
    /// <exception cref="ArgumentException">当MAC地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// byte[] macBytes = MacAddressHelper.Parse("00:11:22:33:44:55");
    /// </code>
    /// </example>
    public static byte[] Parse(string macAddress)
    {
        if (!IsValid(macAddress))
            throw new ArgumentException("无效的MAC地址格式", nameof(macAddress));

        var cleanMac = macAddress.Replace(":", "").Replace("-", "").Replace(".", "");
        var macBytes = new byte[6];

        for (var i = 0; i < 6; i++) 
            macBytes[i] = Convert.ToByte(cleanMac.Substring(i * 2, 2), 16);
        return macBytes;
    }

    /// <summary>
    /// 从字节数组创建MAC地址字符串
    /// </summary>
    /// <param name="macBytes">6字节的MAC地址数组</param>
    /// <param name="separator">分隔符</param>
    /// <param name="upperCase">是否大写</param>
    /// <returns>格式化的MAC地址字符串</returns>
    /// <exception cref="ArgumentException">当字节数组长度不为6时抛出</exception>
    /// <example>
    /// <code>
    /// byte[] bytes = { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 };
    /// string mac = MacAddressHelper.FromBytes(bytes); // "00:11:22:33:44:55"
    /// </code>
    /// </example>
    public static string FromBytes(byte[] macBytes, string separator = ":", bool upperCase = true)
    {
        if (macBytes == null)
            throw new ArgumentNullException(nameof(macBytes));
        if (macBytes.Length != 6)
            throw new ArgumentException("MAC地址字节数组长度必须为6", nameof(macBytes));

        var hexString = BitConverter.ToString(macBytes).Replace("-", "");
        return Format(hexString, separator, upperCase);
    }

    /// <summary>
    /// 标准化MAC地址格式
    /// </summary>
    /// <param name="macAddress">MAC地址字符串</param>
    /// <param name="targetSeparator">目标分隔符，默认为冒号</param>
    /// <param name="upperCase">是否大写，默认为true</param>
    /// <returns>标准化后的MAC地址</returns>
    /// <example>
    /// <code>
    /// string normalized = MacAddressHelper.Normalize("00-11-22-33-44-55"); // "00:11:22:33:44:55"
    /// </code>
    /// </example>
    public static string Normalize(string macAddress, string targetSeparator = ":", bool upperCase = true)
    {
        if (!IsValid(macAddress))
            throw new ArgumentException("无效的MAC地址格式", nameof(macAddress));

        var cleanMac = new string(macAddress.Where(char.IsLetterOrDigit).ToArray());
        return Format(cleanMac, targetSeparator, upperCase);
    }

    /// <summary>
    /// 格式化MAC地址
    /// </summary>
    /// <param name="macAddress">原始MAC地址</param>
    /// <param name="separator">分隔符，默认为冒号</param>
    /// <param name="upperCase">是否使用大写，默认为true</param>
    /// <returns>格式化后的MAC地址</returns>
    /// <exception cref="ArgumentException">当MAC地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// string mac1 = MacAddressHelper.Format("001122334455");          // "00:11:22:33:44:55"
    /// string mac2 = MacAddressHelper.Format("001122334455", "-");     // "00-11-22-33-44-55"
    /// string mac3 = MacAddressHelper.Format("001122334455", ".", false); // "00.11.22.33.44.55"
    /// </code>
    /// </example>
    public static string Format(string macAddress, string separator = ":", bool upperCase = true)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            throw new ArgumentException("MAC地址不能为空", nameof(macAddress));
        // 移除所有非十六进制字符
        var cleanMac = new string(macAddress.Where(IsHexDigit).ToArray());
        if (cleanMac.Length != 12)
            throw new ArgumentException("无效的MAC地址格式", nameof(macAddress));
        var result = new StringBuilder();
        for (var i = 0; i < cleanMac.Length; i += 2)
        {
            if (i > 0)
                result.Append(separator);
            var pair = cleanMac.Substring(i, 2);
            result.Append(upperCase ? pair.ToUpper() : pair.ToLower());
        }
        return result.ToString();
    }

    /// <summary>
    /// 判断字符是否为十六进制数字
    /// </summary>
    /// <param name="c">字符</param>
    /// <returns>如果是十六进制数字返回true，否则返回false</returns>
    private static bool IsHexDigit(char c)
    {
        return (c >= '0' && c <= '9') ||
               (c >= 'a' && c <= 'f') ||
               (c >= 'A' && c <= 'F');
    }

    /// <summary>
    /// 批量格式化MAC地址
    /// </summary>
    /// <param name="macAddresses">MAC地址列表</param>
    /// <param name="separator">分隔符</param>
    /// <param name="upperCase">是否大写</param>
    /// <param name="skipInvalid">是否跳过无效地址</param>
    /// <returns>格式化后的MAC地址列表</returns>
    /// <example>
    /// <code>
    /// var addresses = new[] { "001122334455", "aabbccddeeff" };
    /// var formatted = MacAddressHelper.FormatBatch(addresses, ":");
    /// </code>
    /// </example>
    public static List<string> FormatBatch(IEnumerable<string> macAddresses, string separator = ":", bool upperCase = true, bool skipInvalid = true)
    {
        var result = new List<string>();

        foreach (var mac in macAddresses ?? [])
        {
            try
            {
                if (IsValid(mac))
                {
                    result.Add(Normalize(mac, separator, upperCase));
                }
                else if (!skipInvalid)
                {
                    result.Add(mac); // 保留原始无效地址
                }
            }
            catch (Exception)
            {
                if (!skipInvalid)
                {
                    result.Add(mac); // 保留原始地址
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 验证MAC地址格式是否有效
    /// </summary>
    /// <param name="macAddress">MAC地址字符串</param>
    /// <returns>如果是有效的MAC地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isValid1 = MacAddressHelper.IsValid("00:11:22:33:44:55");  // true
    /// bool isValid2 = MacAddressHelper.IsValid("00-11-22-33-44-55");  // true
    /// bool isValid3 = MacAddressHelper.IsValid("invalid");            // false
    /// </code>
    /// </example>
    public static bool IsValid(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            return false;
        // 支持多种MAC地址格式
        var macPatterns = new[]
        {
            @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$", // 00:11:22:33:44:55 或 00-11-22-33-44-55
            @"^([0-9A-Fa-f]{2}\.){5}([0-9A-Fa-f]{2})$", // 00.11.22.33.44.55
            @"^[0-9A-Fa-f]{12}$" // 001122334455
        };
        return macPatterns.Any(pattern => Regex.IsMatch(macAddress, pattern));
    }

    /// <summary>
    /// 批量验证MAC地址
    /// </summary>
    /// <param name="macAddresses">MAC地址列表</param>
    /// <returns>验证结果字典</returns>
    /// <example>
    /// <code>
    /// var addresses = new[] { "00:11:22:33:44:55", "invalid", "AA-BB-CC-DD-EE-FF" };
    /// var results = MacAddressHelper.ValidateBatch(addresses);
    /// </code>
    /// </example>
    public static Dictionary<string, bool> ValidateBatch(IEnumerable<string> macAddresses)
    {
        return macAddresses?.ToDictionary(mac => mac, IsValid) ?? new Dictionary<string, bool>();
    }

    /// <summary>
    /// 比较两个MAC地址是否相等
    /// </summary>
    /// <param name="macAddress1">第一个MAC地址</param>
    /// <param name="macAddress2">第二个MAC地址</param>
    /// <returns>如果相等返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isEqual = MacAddressHelper.AreEqual("00:11:22:33:44:55", "00-11-22-33-44-55"); // true
    /// </code>
    /// </example>
    public static bool AreEqual(string macAddress1, string macAddress2)
    {
        if (!IsValid(macAddress1) || !IsValid(macAddress2))
            return false;

        var clean1 = new string(macAddress1.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        var clean2 = new string(macAddress2.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        return clean1.Equals(clean2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 从MAC地址生成EUI-64标识符
    /// </summary>
    /// <param name="macAddress">MAC地址</param>
    /// <returns>8字节的EUI-64标识符</returns>
    public static byte[] GenerateEUI64(string macAddress)
    {
        var cleanMac = macAddress.Replace(":", "").Replace("-", "").Replace(".", "");
        if (cleanMac.Length != 12)
            throw new ArgumentException("无效的MAC地址格式");

        var macBytes = new byte[6];
        for (var i = 0; i < 6; i++) 
            macBytes[i] = Convert.ToByte(cleanMac.Substring(i * 2, 2), 16);

        var eui64 = new byte[8];
        // 复制前3字节
        Array.Copy(macBytes, 0, eui64, 0, 3);
        // 插入FFFE
        eui64[3] = 0xff;
        eui64[4] = 0xfe;
        // 复制后3字节
        Array.Copy(macBytes, 3, eui64, 5, 3);
        // 翻转通用/本地位
        eui64[0] ^= 0x02;

        return eui64;
    }

    /// <summary>
    /// 生成随机MAC地址
    /// </summary>
    /// <param name="useLocallyAdministered">是否使用本地管理地址（设置第2位为1）</param>
    /// <param name="separator">分隔符</param>
    /// <param name="upperCase">是否大写</param>
    /// <returns>随机生成的MAC地址</returns>
    /// <example>
    /// <code>
    /// string randomMac = MacAddressHelper.GenerateRandom(); // "AA:BB:CC:DD:EE:FF"
    /// string localMac = MacAddressHelper.GenerateRandom(true); // 本地管理的MAC地址
    /// </code>
    /// </example>
    public static string GenerateRandom(bool useLocallyAdministered = false, string separator = ":", bool upperCase = true)
    {
        var random = new Random();
        var macBytes = new byte[6];
        random.NextBytes(macBytes);

        if (useLocallyAdministered)
        {
            // 设置本地管理位（第一个字节的第二位）
            macBytes[0] |= 0x02;
        }
        else
        {
            // 清除本地管理位，确保是全球唯一
            macBytes[0] &= 0xFD;
        }

        // 确保不是组播地址（清除第一位）
        macBytes[0] &= 0xFE;

        var hexString = BitConverter.ToString(macBytes).Replace("-", "");
        return Format(hexString, separator, upperCase);
    }

    /// <summary>
    /// 生成MAC地址范围
    /// </summary>
    /// <param name="startMac">起始MAC地址</param>
    /// <param name="endMac">结束MAC地址</param>
    /// <param name="maxCount">最大生成数量（防止内存溢出）</param>
    /// <param name="separator">分隔符</param>
    /// <param name="upperCase">是否大写</param>
    /// <returns>MAC地址范围列表</returns>
    /// <example>
    /// <code>
    /// var range = MacAddressHelper.GenerateRange("00:11:22:33:44:55", "00:11:22:33:44:5A", 10);
    /// </code>
    /// </example>
    public static List<string> GenerateRange(string startMac, string endMac, int maxCount = 1000, string separator = ":", bool upperCase = true)
    {
        if (!IsValid(startMac) || !IsValid(endMac))
            throw new ArgumentException("无效的MAC地址格式");

        var startBytes = Parse(startMac);
        var endBytes = Parse(endMac);

        // 转换为长整型比较
        long startValue = 0, endValue = 0;
        for (var i = 0; i < 6; i++)
        {
            startValue = (startValue << 8) | startBytes[i];
            endValue = (endValue << 8) | endBytes[i];
        }

        if (startValue > endValue)
            throw new ArgumentException("起始MAC地址不能大于结束MAC地址");

        var range = endValue - startValue + 1;
        if (range > maxCount)
            throw new ArgumentException($"MAC地址范围过大：{range}，超过最大限制{maxCount}");

        var result = new List<string>();
        for (var current = startValue; current <= endValue && result.Count < maxCount; current++)
        {
            var currentBytes = new byte[6];
            for (var i = 0; i < 6; i++) 
                currentBytes[5 - i] = (byte)((current >> (i * 8)) & 0xFF);

            result.Add(FromBytes(currentBytes, separator, upperCase));
        }

        return result;
    }

    /// <summary>
    /// 获取MAC地址信息
    /// </summary>
    /// <param name="macAddress">MAC地址字符串</param>
    /// <returns>MAC地址信息</returns>
    /// <example>
    /// <code>
    /// var info = MacAddressHelper.GetMacAddressInfo("00:11:22:33:44:55");
    /// Console.WriteLine($"制造商: {info.IsUniversal}");
    /// Console.WriteLine($"组播: {info.IsMulticast}");
    /// </code>
    /// </example>
    public static MacAddressInfo GetMacAddressInfo(string macAddress)
    {
        if (!IsValid(macAddress))
            throw new ArgumentException("无效的MAC地址格式", nameof(macAddress));

        var macBytes = Parse(macAddress);
        var firstByte = macBytes[0];

        return new MacAddressInfo
        {
            MacAddress = Normalize(macAddress),
            IsUniversal = (firstByte & 0x02) == 0,
            IsLocallyAdministered = (firstByte & 0x02) != 0,
            IsMulticast = (firstByte & 0x01) != 0,
            IsUnicast = (firstByte & 0x01) == 0,
            OUI = $"{macBytes[0]:X2}:{macBytes[1]:X2}:{macBytes[2]:X2}",
            NIC = $"{macBytes[3]:X2}:{macBytes[4]:X2}:{macBytes[5]:X2}"
        };
    }

    /// <summary>
    /// 生成MAC地址的相邻地址
    /// </summary>
    /// <param name="macAddress">基础MAC地址</param>
    /// <param name="offset">偏移量（可以为负数）</param>
    /// <param name="separator">分隔符</param>
    /// <param name="upperCase">是否大写</param>
    /// <returns>计算后的MAC地址</returns>
    /// <example>
    /// <code>
    /// string nextMac = MacAddressHelper.GetAdjacentAddress("00:11:22:33:44:55", 1); // "00:11:22:33:44:56"
    /// string prevMac = MacAddressHelper.GetAdjacentAddress("00:11:22:33:44:55", -1); // "00:11:22:33:44:54"
    /// </code>
    /// </example>
    public static string GetAdjacentAddress(string macAddress, long offset, string separator = ":", bool upperCase = true)
    {
        if (!IsValid(macAddress))
            throw new ArgumentException("无效的MAC地址格式", nameof(macAddress));

        var macBytes = Parse(macAddress);

        // 将MAC地址转换为长整型进行计算
        long macValue = 0;
        for (var i = 0; i < 6; i++)
        {
            macValue = (macValue << 8) | macBytes[i];
        }

        // 应用偏移量
        macValue += offset;

        // 确保在有效范围内
        if (macValue < 0)
            macValue = 0;
        if (macValue > 0xFFFFFFFFFFFF)
            macValue = 0xFFFFFFFFFFFF;

        // 转换回字节数组
        var resultBytes = new byte[6];
        for (var i = 0; i < 6; i++) 
            resultBytes[5 - i] = (byte)((macValue >> (i * 8)) & 0xFF);

        return FromBytes(resultBytes, separator, upperCase);
    }
}