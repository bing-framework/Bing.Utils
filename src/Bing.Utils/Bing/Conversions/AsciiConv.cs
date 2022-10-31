using System.Text;

namespace Bing.Conversions;

/// <summary>
/// ASCII 转换帮助类
/// </summary>
internal static class AsciiConvHelper
{
    /// <summary>
    /// 字符转ASCII
    /// </summary>
    /// <param name="char">字符</param>
    public static byte CharToAscii(char @char) => (byte) @char;

    /// <summary>
    /// 字符串转ASCII
    /// </summary>
    /// <param name="string">字符串</param>
    public static byte[] StringToAscii(string @string) => Encoding.ASCII.GetBytes(@string);

    /// <summary>
    /// ASCII转字符
    /// </summary>
    /// <param name="byte">ASCII字节</param>
    public static char AsciiToChar(byte @byte) => (char) @byte;

    /// <summary>
    /// ASCII转字符串
    /// </summary>
    /// <param name="bytes">ASCII字节数组</param>
    public static string AsciiToString(byte[] bytes) => Encoding.ASCII.GetString(bytes, 0, bytes.Length);
}

/// <summary>
/// ASCII转换器
/// </summary>
public static class AsciiConv
{
    /// <summary>
    /// 将 byte[] 转换为 ASCII <see cref="string"/>
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <example>in: new byte[] {65, 66, 67}; out: ABC</example>
    public static string BytesToAsciiString(byte[] bytes) => AsciiConvHelper.AsciiToString(bytes);

    /// <summary>
    /// 将 ASCII <see cref="string"/> 转换为 byte[]
    /// </summary>
    /// <param name="asciiStr">ASCII字符串</param>
    /// <example>in: ABC; out: new byte[] {65, 66, 67}</example>
    public static byte[] AsciiStringToBytes(string asciiStr) => AsciiConvHelper.StringToAscii(asciiStr);
}