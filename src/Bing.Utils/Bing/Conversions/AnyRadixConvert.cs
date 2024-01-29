using System.Globalization;
using System.Text.RegularExpressions;
using Bing.Conversions.Internals;

namespace Bing.Conversions;

/// <summary>
/// 任意[2,62]进制转换器
/// </summary>
public static class AnyRadixConvert
{
    /// <summary>
    /// X 进制转换为 Y 进制，通用进制转换入口
    /// </summary>
    /// <param name="things">要转换的字符串。</param>
    /// <param name="baseOfSource">源数制的基数。</param>
    /// <param name="baseOfTarget">目标数制的基数。</param>
    /// <returns>转换后的字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string X2X(string things, int baseOfSource, int baseOfTarget) => ScaleConvHelper.ThingsToThings(things, baseOfSource, baseOfTarget);

    #region Binary(二进制)

    /// <summary>
    /// 二进制值转换为八进制值
    /// </summary>
    /// <example>in: 101110; out: 56</example>
    /// <param name="binaryThings">二进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string BinToOct(string binaryThings) => ScaleConvHelper.ThingsToThings(binaryThings, 2, 8);

    /// <summary>
    /// 二进制值转换为十进制值
    /// </summary>
    /// <example>in: 101110; out: 46</example>
    /// <param name="binaryThings">二进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinToDec(string binaryThings) => Convert.ToInt32(ScaleConvHelper.ThingsToThings(binaryThings, 2, 10));

    /// <summary>
    /// 二进制值转换为十六进制值
    /// </summary>
    /// <example>in: 101110; out: 2E</example>
    /// <param name="binaryThings">二进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string BinToHex(string binaryThings) => ScaleConvHelper.ThingsToThings(binaryThings, 2, 16);

    #endregion

    #region Octal(八进制)

    /// <summary>
    /// 八进制值转换为二进制值
    /// </summary>
    /// <example>in: 140; out: 1100000</example>
    /// <param name="octalThings">八进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string OctToBin(string octalThings) => ScaleConvHelper.ThingsToThings(octalThings, 8, 2);

    /// <summary>
    /// 八进制值转换为十进制值
    /// </summary>
    /// <example>in: 140; out: 96</example>
    /// <param name="octalThings">八进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int OctToDec(string octalThings) => Convert.ToInt32(ScaleConvHelper.ThingsToThings(octalThings, 8, 10));

    /// <summary>
    /// 八进制值转换为十六进制值
    /// </summary>
    /// <example>in: 140; out: 60</example>
    /// <param name="octalThings">八进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string OctToHex(string octalThings) => ScaleConvHelper.ThingsToThings(octalThings, 8, 16);

    #endregion

    #region Decimal(十进制)

    /// <summary>
    /// 十进制值转换为二进制值
    /// </summary>
    /// <example>in: 46; out: 101110</example>
    /// <example>in: 128; out: 10000000</example>
    /// <param name="decimalThings">十进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToBin(byte decimalThings) => ScaleConvHelper.ThingsToThings(decimalThings.ToString(), 10, 2);

    /// <summary>
    /// 十进制值转换为二进制值
    /// </summary>
    /// <example>in: 46; out: 101110</example>
    /// <example>in: 128; out: 10000000</example>
    /// <param name="decimalThings">十进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToBin(string decimalThings) => ScaleConvHelper.ThingsToThings(decimalThings, 10, 2);

    /// <summary>
    /// 十进制值转换为八进制值
    /// </summary>
    /// <example>in: 128; out: 200</example>
    /// <param name="decimalThings">十进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToOct(byte decimalThings) => ScaleConvHelper.ThingsToThings(decimalThings.ToString(), 10, 8);

    /// <summary>
    /// 十进制值转换为八进制值
    /// </summary>
    /// <example>in: 128; out: 200</example>
    /// <param name="decimalThings">十进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToOct(string decimalThings) => ScaleConvHelper.ThingsToThings(decimalThings, 10, 8);

    /// <summary>
    /// 十进制值转换为十六进制值
    /// </summary>
    /// <example>in: 128; out: 80</example>
    /// <param name="decimalThings">十进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToHex(byte decimalThings) => ScaleConvHelper.ThingsToThings(decimalThings.ToString(), 10, 16);

    /// <summary>
    /// 十进制值转换为十六进制值
    /// </summary>
    /// <example>in: 46; out: 2E</example>
    /// <example>in: 128; out: 80</example>
    /// <param name="decimalThings">十进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToHex(string decimalThings) => ScaleConvHelper.ThingsToThings(decimalThings, 10, 16);

    /// <summary>
    /// 十进制值转换为十六进制值
    /// </summary>
    /// <example>in: 46; out: 002E</example>
    /// <example>in: 128; out: 0080</example>
    /// <param name="decimalThings">十进制</param>
    /// <param name="formatLength">格式化长度</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToHex(string decimalThings, int formatLength)
    {
        var system16Val = ScaleConvHelper.ThingsToThings(decimalThings, 10, 16);
        return system16Val.Length > formatLength ? system16Val : system16Val.PadLeft(formatLength, '0');
    }

    /// <summary>
    /// 十进制值转换为十六进制值
    /// </summary>
    /// <example>in: (byte)65, (byte)66; out: 4142</example>
    /// <example>in: (byte)66, (byte)65; out: 4241</example>
    /// <param name="highThings">高位字节</param>
    /// <param name="lowThings">低位字节</param>
    /// <returns>两个字节值合并后的十六进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToHex(byte highThings, byte lowThings) => $"{DecToHex(highThings)}{DecToHex(lowThings)}";

    /// <summary>
    /// 将十进制字节数组转换为对应的长十六进制字符串。
    /// </summary>
    /// <example>in: new byte[] {65 , 66, 67}; out: 414243</example>
    /// <param name="decimalBytes">要转换的十进制字节数组。</param>
    /// <returns>表示十进制字节数组对应长十六进制字符串的结果。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecBytesToLongHex(byte[] decimalBytes)
    {
        var sb = new StringBuilder();
        foreach (var decimalThings in decimalBytes)
            sb.Append(DecToHex(decimalThings.ToString())).Append(" ");
        return sb.Length > 0 ? sb.ToString(0, sb.Length - 1) : string.Empty;
    }

    #endregion

    #region Hexadecimal(十六进制)

    /// <summary>
    /// 十六进制值转换为二进制值
    /// </summary>
    /// <example>in: 2E; out: 101110</example>
    /// <param name="hexadecimalThings">十六进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HexToBin(string hexadecimalThings) => ScaleConvHelper.ThingsToThings(hexadecimalThings, 16, 2);

    /// <summary>
    /// 十六进制值转换为八进制值
    /// </summary>
    /// <example>in: 2E; out: 56</example>
    /// <param name="hexadecimalThings">十六进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HexToOct(string hexadecimalThings) => ScaleConvHelper.ThingsToThings(hexadecimalThings, 16, 8);

    /// <summary>
    /// 十六进制值转换为十进制值
    /// </summary>
    /// <example>in: 2E; out: 46</example>
    /// <param name="hexadecimalThings">十六进制</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HexToDec(string hexadecimalThings) => ScaleConvHelper.ThingsToThings(hexadecimalThings, 16, 10);

    /// <summary>
    /// 将字符串转换为十六进制数的字符串表示形式。
    /// </summary>
    /// <param name="letters">要转换的字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（默认为UTF-8）。</param>
    /// <returns>字符串的十六进制表示，每个字节之间用空格分隔。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string LettersToHex(string letters, Encoding encoding = null) => BitConverter.ToString((encoding ?? Encoding.UTF8).GetBytes(letters)).Replace("-", " ");

    /// <summary>
    /// 将十六进制字符串转换为其对应的字符串表示形式。
    /// </summary>
    /// <param name="hexadecimalThings">十六进制</param>
    /// <param name="encoding">用于字符串编码的编码方式（默认为UTF-8）。</param>
    /// <returns>十六进制字符串对应的字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HexToLetters(string hexadecimalThings, Encoding encoding = null)
    {
        var mc = Regex.Matches(hexadecimalThings, @"(?i)[\da-f]{2}");
        var bytes = new byte[mc.Count];
        for (var i = 0; i < mc.Count; i++)
        {
            if (!byte.TryParse(mc[i].Value, NumberStyles.HexNumber, null, out bytes[i]))
                bytes[i] = 0;
        }
        return (encoding ?? Encoding.UTF8).GetString(bytes);
    }

    /// <summary>
    /// 将长十六进制字符串转换为对应的十进制字节数组。
    /// </summary>
    /// <example>in: 2E3D; out: result[0] is 46, result[1] is 61</example>
    /// <param name="hexadecimalThings">要转换的十六进制字符串。</param>
    /// <returns>表示十六进制字符串对应十进制值的字节数组。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] LongHexToDecBytes(string hexadecimalThings)
    {
        var mc = Regex.Matches(hexadecimalThings, @"(?i)[\da-f]{2}");
        return (from Match m in mc select Convert.ToByte(m.Value, 16)).ToArray();
    }

    #endregion
}