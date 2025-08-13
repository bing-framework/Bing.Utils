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
    /// 格式化MAC地址
    /// </summary>
    /// <param name="macAddress">原始MAC地址</param>
    /// <param name="separator">分隔符，默认为冒号</param>
    /// <param name="upperCase">是否使用大写，默认为true</param>
    /// <returns>格式化后的MAC地址</returns>
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
            return string.Empty;
        // 移除所有非十六进制字符
        var cleanMac = new string(macAddress.Where(char.IsLetterOrDigit).ToArray());
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
}