using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址转换器
/// </summary>
/// <remarks>
/// 提供IPv6地址与字节数组、字符串之间的转换功能，以及IPv4/IPv6地址映射转换
/// </remarks>
public static class IPv6Converter
{
    /// <summary>
    /// 将IPv6地址字符串转换为字节数组
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>16字节数组表示的IPv6地址</returns>
    /// <exception cref="ArgumentException">当IPv6地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// byte[] ipv6Bytes = IPv6Converter.ToBytes("2001:db8::1");
    /// Console.WriteLine($"IPv6字节长度: {ipv6Bytes.Length}"); // 16
    /// </code>
    /// </example>
    public static byte[] ToBytes(string ipv6Address)
    {
        if (!IPAddress.TryParse(ipv6Address, out var ip) || ip.AddressFamily != AddressFamily.InterNetworkV6)
            throw new ArgumentException($"无效的IPv6地址格式: {ipv6Address}", nameof(ipv6Address));
        return ip.GetAddressBytes();
    }

    /// <summary>
    /// 将字节数组转换为IPv6地址字符串
    /// </summary>
    /// <param name="bytes">16字节数组</param>
    /// <returns>IPv6地址字符串</returns>
    /// <exception cref="ArgumentException">当字节数组长度不为16时抛出</exception>
    /// <example>
    /// <code>
    /// byte[] bytes = { 0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
    /// string ipv6 = IPv6Converter.FromBytes(bytes); // "2001:db8::1"
    /// </code>
    /// </example>
    public static string FromBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length != 16)
            throw new ArgumentException("IPv6地址必须是16字节数组", nameof(bytes));
        return new IPAddress(bytes).ToString();
    }

    /// <summary>
    /// 将IPv6地址展开为完整格式
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串（可能是压缩格式）</param>
    /// <returns>完整格式的IPv6地址</returns>
    /// <exception cref="ArgumentException">当IPv6地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// string expanded = IPv6Converter.Expand("2001:db8::1");
    /// Console.WriteLine(expanded); // "2001:0db8:0000:0000:0000:0000:0000:0001"
    /// </code>
    /// </example>
    public static string Expand(string ipv6Address)
    {
        if (!IPv6Validator.IsValid(ipv6Address))
            throw new ArgumentException("无效的IPv6地址格式", nameof(ipv6Address));

        var ip = IPAddress.Parse(ipv6Address);
        var bytes = ip.GetAddressBytes();
        var groups = new string[8];

        for (var i = 0; i < 8; i++)
        {
            var value = (bytes[i * 2] << 8) | bytes[i * 2 + 1];
            groups[i] = value.ToString("x4");
        }

        return string.Join(":", groups);
    }

    /// <summary>
    /// 将IPv6地址压缩为最短格式
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>压缩格式的IPv6地址</returns>
    /// <exception cref="ArgumentException">当IPv6地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// string compressed = IPv6Converter.Compress("2001:0db8:0000:0000:0000:0000:0000:0001");
    /// Console.WriteLine(compressed); // "2001:db8::1"
    /// </code>
    /// </example>
    public static string Compress(string ipv6Address)
    {
        if (!IPv6Validator.IsValid(ipv6Address))
            throw new ArgumentException("无效的IPv6地址格式", nameof(ipv6Address));
        return IPAddress.Parse(ipv6Address).ToString();
    }

    /// <summary>
    /// 检查IPv6地址是否为IPv4映射地址
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>如果是IPv4映射地址返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool isV4Mapped = IPv6Converter.IsIPv4Mapped("::ffff:192.168.1.1");
    /// Console.WriteLine(isV4Mapped); // true
    /// </code>
    /// </example>
    public static bool IsIPv4Mapped(string ipv6Address)
    {
        if (!IPv6Validator.IsValid(ipv6Address))
            return false;
        var ip = IPAddress.Parse(ipv6Address);
        return ip.IsIPv4MappedToIPv6;
    }

    /// <summary>
    /// 将IPv4映射的IPv6地址转换为IPv4地址
    /// </summary>
    /// <param name="ipv6Address">IPv4映射的IPv6地址</param>
    /// <returns>对应的IPv4地址</returns>
    /// <exception cref="ArgumentException">当不是IPv4映射地址时抛出</exception>
    /// <example>
    /// <code>
    /// string ipv4 = IPv6Converter.MapToIPv4("::ffff:192.168.1.1");
    /// Console.WriteLine(ipv4); // "192.168.1.1"
    /// </code>
    /// </example>
    public static string MapToIPv4(string ipv6Address)
    {
        if (!IsIPv4Mapped(ipv6Address))
            throw new ArgumentException("不是IPv4映射的IPv6地址", nameof(ipv6Address));
        var ip = IPAddress.Parse(ipv6Address);
        return ip.MapToIPv4().ToString();
    }

    /// <summary>
    /// 将IPv4地址转换为IPv6映射地址
    /// </summary>
    /// <param name="ipv4Address">IPv4地址字符串</param>
    /// <returns>IPv4映射的IPv6地址</returns>
    /// <exception cref="ArgumentException">当IPv4地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// string ipv6 = IPv6Converter.MapToIPv6("192.168.1.1");
    /// Console.WriteLine(ipv6); // "::ffff:192.168.1.1"
    /// </code>
    /// </example>
    public static string MapToIPv6(string ipv4Address)
    {
        if (!Bing.Net.IPv4.IPv4Validator.IsValid(ipv4Address))
            throw new ArgumentException("无效的IPv4地址格式", nameof(ipv4Address));
        var ip = IPAddress.Parse(ipv4Address);
        return ip.MapToIPv6().ToString();
    }

    /// <summary>
    /// 将IPv6地址转换为BigInteger表示
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>BigInteger表示的IPv6地址数值</returns>
    /// <exception cref="ArgumentException">当IPv6地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// var value = IPv6Converter.ToBigInteger("2001:db8::1");
    /// Console.WriteLine($"IPv6数值: {value}");
    /// </code>
    /// </example>
    public static BigInteger ToBigInteger(string ipv6Address)
    {
        if (!IPv6Validator.IsValid(ipv6Address))
            throw new ArgumentException("无效的IPv6地址格式", nameof(ipv6Address));
        var bytes = ToBytes(ipv6Address);
        // 将字节数组转换为BigInteger（大端序）
        return new BigInteger(bytes.Reverse().Concat(new byte[] { 0 }).ToArray());
    }

    /// <summary>
    /// 从BigInteger转换为IPv6地址
    /// </summary>
    /// <param name="value">BigInteger表示的IPv6地址数值</param>
    /// <returns>IPv6地址字符串</returns>
    /// <exception cref="ArgumentException">当数值超出IPv6地址范围时抛出</exception>
    /// <example>
    /// <code>
    /// var bigIntValue = new BigInteger(42540766411282592856903984951653826561);
    /// string ipv6 = IPv6Converter.FromBigInteger(bigIntValue);
    /// Console.WriteLine(ipv6); // "2001:db8::1"
    /// </code>
    /// </example>
    public static string FromBigInteger(BigInteger value)
    {
        // 创建 IPv6 最大值: 2^128 - 1
        var maxValueBytes = new byte[17]; // 16字节全为0xFF，加一个0字节防止符号位
        for (var i = 0; i < 16; i++) 
            maxValueBytes[i] = 0xFF;
        maxValueBytes[16] = 0;
        var maxValue = new BigInteger(maxValueBytes);

        if (value < 0 || value > maxValue)
            throw new ArgumentException("数值超出IPv6地址范围", nameof(value));

        var bytes = value.ToByteArray();

        // 移除符号位字节（如果存在）
        if (bytes.Length > 16 && bytes[bytes.Length - 1] == 0) 
            bytes = bytes.Take(bytes.Length - 1).ToArray();
        // 确保字节数组长度不超过16
        if (bytes.Length > 16) 
            bytes = bytes.Take(16).ToArray();

        // 创建16字节的IPv6地址数组（大端序）
        var ipv6Bytes = new byte[16];

        // BigInteger.ToByteArray() 返回小端序，我们需要将其转换为大端序
        // 将 bytes 逆序复制到 ipv6Bytes 的末尾
        for (int i = 0; i < bytes.Length; i++)
        {
            ipv6Bytes[15 - i] = bytes[i];
        }

        return FromBytes(ipv6Bytes);
    }

    /// <summary>
    /// 获取IPv6地址的十六进制字符串表示（不含分隔符）
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>32位十六进制字符串</returns>
    /// <exception cref="ArgumentException">当IPv6地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// string hex = IPv6Converter.ToHexString("2001:db8::1");
    /// Console.WriteLine(hex); // "20010db8000000000000000000000001"
    /// </code>
    /// </example>
    public static string ToHexString(string ipv6Address)
    {
        if (!IPv6Validator.IsValid(ipv6Address))
            throw new ArgumentException("无效的IPv6地址格式", nameof(ipv6Address));

        var bytes = ToBytes(ipv6Address);
        return BytesToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// 从十六进制字符串转换为IPv6地址
    /// </summary>
    /// <param name="hexString">32位十六进制字符串</param>
    /// <returns>IPv6地址字符串</returns>
    /// <exception cref="ArgumentException">当十六进制字符串格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// string ipv6 = IPv6Converter.FromHexString("20010db8000000000000000000000001");
    /// Console.WriteLine(ipv6); // "2001:db8::1"
    /// </code>
    /// </example>
    public static string FromHexString(string hexString)
    {
        if (string.IsNullOrWhiteSpace(hexString))
            throw new ArgumentException("十六进制字符串不能为空", nameof(hexString));

        // 移除可能的分隔符和空格
        hexString = hexString.Replace(":", "").Replace("-", "").Replace(" ", "").Trim();

        if (hexString.Length != 32)
            throw new ArgumentException("IPv6十六进制字符串必须为32位", nameof(hexString));

        try
        {
            var bytes = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return FromBytes(bytes);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"无效的十六进制字符串格式: {hexString}", nameof(hexString), ex);
        }
    }

    /// <summary>
    /// 获取IPv6地址的二进制字符串表示
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <returns>128位二进制字符串</returns>
    /// <exception cref="ArgumentException">当IPv6地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// string binary = IPv6Converter.ToBinaryString("::1");
    /// Console.WriteLine(binary.Substring(120)); // "00000001" (最后8位)
    /// </code>
    /// </example>
    public static string ToBinaryString(string ipv6Address)
    {
        if (!IPv6Validator.IsValid(ipv6Address))
            throw new ArgumentException("无效的IPv6地址格式", nameof(ipv6Address));

        var bytes = ToBytes(ipv6Address);
        var binaryString = string.Join("", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
        return binaryString;
    }

    /// <summary>
    /// 从二进制字符串转换为IPv6地址
    /// </summary>
    /// <param name="binaryString">128位二进制字符串</param>
    /// <returns>IPv6地址字符串</returns>
    /// <exception cref="ArgumentException">当二进制字符串格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// string binary = "00100000000000010000110110111000" + "00000000000000000000000000000000" +
    ///                 "00000000000000000000000000000000" + "00000000000000000000000000000001";
    /// string ipv6 = IPv6Converter.FromBinaryString(binary);
    /// Console.WriteLine(ipv6); // "2001:db8::1"
    /// </code>
    /// </example>
    public static string FromBinaryString(string binaryString)
    {
        if (string.IsNullOrWhiteSpace(binaryString))
            throw new ArgumentException("二进制字符串不能为空", nameof(binaryString));

        // 移除可能的空格和分隔符
        binaryString = binaryString.Replace(" ", "").Replace(".", "").Replace("-", "").Trim();

        if (binaryString.Length != 128)
            throw new ArgumentException("IPv6二进制字符串必须为128位", nameof(binaryString));

        if (!binaryString.All(c => c == '0' || c == '1'))
            throw new ArgumentException("二进制字符串只能包含0和1", nameof(binaryString));

        try
        {
            var bytes = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                var byteString = binaryString.Substring(i * 8, 8);
                bytes[i] = Convert.ToByte(byteString, 2);
            }
            return FromBytes(bytes);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"无效的二进制字符串格式: {binaryString}", nameof(binaryString), ex);
        }
    }

    /// <summary>
    /// 标准化IPv6地址格式
    /// </summary>
    /// <param name="ipv6Address">IPv6地址字符串</param>
    /// <param name="format">格式化选项</param>
    /// <returns>标准化后的IPv6地址</returns>
    /// <exception cref="ArgumentException">当IPv6地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// string normalized = IPv6Converter.Normalize("2001:0db8:0000:0000:0000:0000:0000:0001", IPv6Format.Compressed);
    /// Console.WriteLine(normalized); // "2001:db8::1"
    /// </code>
    /// </example>
    public static string Normalize(string ipv6Address, IPv6Format format = IPv6Format.Compressed)
    {
        if (!IPv6Validator.IsValid(ipv6Address))
            throw new ArgumentException("无效的IPv6地址格式", nameof(ipv6Address));

        return format switch
        {
            IPv6Format.Compressed => Compress(ipv6Address),
            IPv6Format.Expanded => Expand(ipv6Address),
            IPv6Format.Hexadecimal => ToHexString(ipv6Address),
            IPv6Format.Binary => ToBinaryString(ipv6Address),
            _ => Compress(ipv6Address)
        };
    }

    /// <summary>
    /// 检查两个IPv6地址是否相等（忽略格式差异）
    /// </summary>
    /// <param name="ipv6Address1">第一个IPv6地址</param>
    /// <param name="ipv6Address2">第二个IPv6地址</param>
    /// <returns>如果地址相等返回true，否则返回false</returns>
    /// <example>
    /// <code>
    /// bool equal = IPv6Converter.AreEqual("2001:db8::1", "2001:0db8:0000:0000:0000:0000:0000:0001");
    /// Console.WriteLine(equal); // true
    /// </code>
    /// </example>
    public static bool AreEqual(string ipv6Address1, string ipv6Address2)
    {
        if (!IPv6Validator.IsValid(ipv6Address1) || !IPv6Validator.IsValid(ipv6Address2))
            return false;

        try
        {
            var bytes1 = ToBytes(ipv6Address1);
            var bytes2 = ToBytes(ipv6Address2);
            return bytes1.SequenceEqual(bytes2);
        }
        catch
        {
            return false;
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 将字节数组转换为十六进制字符串（兼容所有.NET版本）
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <returns>十六进制字符串</returns>
    private static string BytesToHexString(byte[] bytes)
    {
#if NET5_0_OR_GREATER
        // .NET 5+ 支持 Convert.ToHexString
        return Convert.ToHexString(bytes);
#else
        // .NET Standard 2.0 和更早版本的兼容实现
        var hex = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            hex.AppendFormat("{0:x2}", b);
        }
        return hex.ToString();
#endif
    }

    #endregion
}