using System.Globalization;

namespace Bing.Conversions;

/// <summary>
/// 十六进制工具
/// </summary>
public static class Hex
{
    /// <summary>
    /// 将字节数组转换成十六进制字符串。
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <returns>十六进制字符串</returns>
    public static string ToString(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 3);
        foreach (var b in bytes)
            sb.Append(b.ToString("X2"));
        return sb.ToString();
    }

    /// <summary>
    /// 将十六进制字符串转换为字节数组。
    /// </summary>
    /// <param name="hex">十六进制字符串</param>
    /// <returns>字节数组</returns>
    public static byte[] ToBytes(string hex)
    {
        if (hex == null)
            return [0];
        if(hex.Length==0)
            return [0];
        if (hex.Length % 2 == 1)
            hex = "0" + hex;
        var result =new byte[hex.Length / 2];
        for (var i = 0; i < hex.Length / 2; i++)
            result[i] = byte.Parse(hex.Substring(2 * i, 2), NumberStyles.AllowHexSpecifier);
        return result;
    }
}