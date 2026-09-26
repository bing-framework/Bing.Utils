using System.Globalization;
using System.Text.RegularExpressions;
using Bing.Conversions.Internals;

namespace Bing.Conversions;

/// <summary>
/// 进制字符集策略
/// </summary>
public enum RadixCharsetStrategy
{
    /// <summary>
    /// 数字 + 小写字母 + 大写字母
    /// </summary>
    LowerFirst,

    /// <summary>
    /// 按进制选择字符集以减少视觉混淆。
    /// </summary>
    /// <remarks>
    /// 未单独列出的进制采用数字、大写字母、小写字母的顺序。<br />
    /// 26进制：使用全小写字母作为26进制的字符集。<br />
    /// 32进制：使用 Crockford 字符集，排除 I、L、O、U；历史编码需按旧映射迁移。<br />
    /// 36进制：包含数字和小写字母。<br />
    /// 52进制：包含小写和大写字母，不包含数字。<br />
    /// 58进制：排除数字 0、大写字母 I、O 和小写字母 l。<br />
    /// 62进制：完整混合了数字、小写和大写字母。<br />
    /// </remarks>
    AvoidConfusion
}

/// <summary>
/// 任意[2,62]进制转换器
/// </summary>
public static class AnyRadixConvert
{
    /// <summary>
    /// 将数值从源进制转换为目标进制。
    /// </summary>
    /// <remarks>字符排序由策略决定；非空输入仅支持 0 到 long.MaxValue。同进制转换也校验输入，但保留原文本。</remarks>
    /// <param name="things">要转换的字符串。</param>
    /// <param name="baseOfSource">源数制的基数。</param>
    /// <param name="baseOfTarget">目标数制的基数。</param>
    /// <param name="strategy">进制字符集策略</param>
    /// <returns>转换后的字符串；输入为空白时返回空字符串。</returns>
    /// <exception cref="ArgumentOutOfRangeException">输入非空白且源进制或目标进制不在 2 到 62 之间。</exception>
    /// <exception cref="OverflowException">输入数值超过 long.MaxValue。</exception>
    /// <exception cref="ArgumentException">输入包含所选字符集之外的字符。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string X2X(string things, int baseOfSource, int baseOfTarget, RadixCharsetStrategy strategy = RadixCharsetStrategy.AvoidConfusion) => ScaleConvHelper.ThingsToThings(things, baseOfSource, baseOfTarget, strategy);

    #region Binary(二进制)

    /// <summary>
    /// 二进制值转换为八进制值
    /// </summary>
    /// <example>in: 101110; out: 56</example>
    /// <param name="binaryThings">二进制</param>
    /// <returns>转换后的八进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string BinToOct(string binaryThings) => ScaleConvHelper.ThingsToThings(binaryThings, 2, 8);

    /// <summary>
    /// 二进制值转换为十进制值
    /// </summary>
    /// <example>in: 101110; out: 46</example>
    /// <param name="binaryThings">二进制</param>
    /// <returns>转换后的十进制整数。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinToDec(string binaryThings) => Convert.ToInt32(ScaleConvHelper.ThingsToThings(binaryThings, 2, 10));

    /// <summary>
    /// 二进制值转换为十六进制值
    /// </summary>
    /// <example>in: 101110; out: 2E</example>
    /// <param name="binaryThings">二进制</param>
    /// <returns>转换后的十六进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string BinToHex(string binaryThings) => ScaleConvHelper.ThingsToThings(binaryThings, 2, 16);

    #endregion

    #region Octal(八进制)

    /// <summary>
    /// 八进制值转换为二进制值
    /// </summary>
    /// <example>in: 140; out: 1100000</example>
    /// <param name="octalThings">八进制</param>
    /// <returns>转换后的二进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string OctToBin(string octalThings) => ScaleConvHelper.ThingsToThings(octalThings, 8, 2);

    /// <summary>
    /// 八进制值转换为十进制值
    /// </summary>
    /// <example>in: 140; out: 96</example>
    /// <param name="octalThings">八进制</param>
    /// <returns>转换后的十进制整数。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int OctToDec(string octalThings) => Convert.ToInt32(ScaleConvHelper.ThingsToThings(octalThings, 8, 10));

    /// <summary>
    /// 八进制值转换为十六进制值
    /// </summary>
    /// <example>in: 140; out: 60</example>
    /// <param name="octalThings">八进制</param>
    /// <returns>转换后的十六进制字符串。</returns>
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
    /// <returns>转换后的二进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToBin(byte decimalThings) => ScaleConvHelper.ThingsToThings(decimalThings.ToString(), 10, 2);

    /// <summary>
    /// 十进制值转换为二进制值
    /// </summary>
    /// <example>in: 46; out: 101110</example>
    /// <example>in: 128; out: 10000000</example>
    /// <param name="decimalThings">十进制</param>
    /// <returns>转换后的二进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToBin(string decimalThings) => ScaleConvHelper.ThingsToThings(decimalThings, 10, 2);

    /// <summary>
    /// 十进制值转换为八进制值
    /// </summary>
    /// <example>in: 128; out: 200</example>
    /// <param name="decimalThings">十进制</param>
    /// <returns>转换后的八进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToOct(byte decimalThings) => ScaleConvHelper.ThingsToThings(decimalThings.ToString(), 10, 8);

    /// <summary>
    /// 十进制值转换为八进制值
    /// </summary>
    /// <example>in: 128; out: 200</example>
    /// <param name="decimalThings">十进制</param>
    /// <returns>转换后的八进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToOct(string decimalThings) => ScaleConvHelper.ThingsToThings(decimalThings, 10, 8);

    /// <summary>
    /// 十进制值转换为十六进制值
    /// </summary>
    /// <example>in: 128; out: 80</example>
    /// <param name="decimalThings">十进制</param>
    /// <returns>转换后的十六进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToHex(byte decimalThings) => ScaleConvHelper.ThingsToThings(decimalThings.ToString(), 10, 16);

    /// <summary>
    /// 十进制值转换为十六进制值
    /// </summary>
    /// <example>in: 46; out: 2E</example>
    /// <example>in: 128; out: 80</example>
    /// <param name="decimalThings">十进制</param>
    /// <returns>转换后的十六进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecToHex(string decimalThings) => ScaleConvHelper.ThingsToThings(decimalThings, 10, 16);

    /// <summary>
    /// 十进制值转换为十六进制值
    /// </summary>
    /// <example>in: 46; out: 002E</example>
    /// <example>in: 128; out: 0080</example>
    /// <param name="decimalThings">十进制</param>
    /// <param name="formatLength">结果的最小字符数；不足时在左侧补零，不截断较长结果。</param>
    /// <returns>转换后的十六进制字符串。</returns>
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
    public static string DecToHex(byte highThings, byte lowThings) => $"{highThings:X2}{lowThings:X2}";

    /// <summary>
    /// 将字节数组转换为十六进制文本。
    /// </summary>
    /// <example>in: new byte[] {65 , 66, 67}; out: 41 42 43</example>
    /// <param name="decimalBytes">要转换的十进制字节数组。</param>
    /// <returns>用空格分隔的两位大写十六进制字节；空数组返回空字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DecBytesToLongHex(byte[] decimalBytes)
    {
        var sb = new StringBuilder();
        foreach (var decimalThings in decimalBytes)
            sb.Append(decimalThings.ToString("X2", CultureInfo.InvariantCulture)).Append(" ");
        return sb.Length > 0 ? sb.ToString(0, sb.Length - 1) : string.Empty;
    }

    #endregion

    #region Hexadecimal(十六进制)

    /// <summary>
    /// 十六进制值转换为二进制值
    /// </summary>
    /// <example>in: 2E; out: 101110</example>
    /// <param name="hexadecimalThings">十六进制</param>
    /// <returns>转换后的二进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HexToBin(string hexadecimalThings) => ScaleConvHelper.ThingsToThings(hexadecimalThings, 16, 2);

    /// <summary>
    /// 十六进制值转换为八进制值
    /// </summary>
    /// <example>in: 2E; out: 56</example>
    /// <param name="hexadecimalThings">十六进制</param>
    /// <returns>转换后的八进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HexToOct(string hexadecimalThings) => ScaleConvHelper.ThingsToThings(hexadecimalThings, 16, 8);

    /// <summary>
    /// 十六进制值转换为十进制值
    /// </summary>
    /// <example>in: 2E; out: 46</example>
    /// <param name="hexadecimalThings">十六进制</param>
    /// <returns>转换后的十进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HexToDec(string hexadecimalThings) => ScaleConvHelper.ThingsToThings(hexadecimalThings, 16, 10);

    /// <summary>
    /// 将字符串编码为十六进制文本。
    /// </summary>
    /// <param name="letters">要转换的字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（默认为UTF-8）。</param>
    /// <returns>字符串的十六进制表示，每个字节之间用空格分隔。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string LettersToHex(string letters, Encoding encoding = null) => BitConverter.ToString((encoding ?? Encoding.UTF8).GetBytes(letters)).Replace("-", " ");

    /// <summary>
    /// 将十六进制字节文本解码为字符串。
    /// </summary>
    /// <param name="hexadecimalThings">十六进制</param>
    /// <param name="encoding">用于字节解码的字符编码（默认为UTF-8）。</param>
    /// <returns>十六进制字符串对应的字符串。</returns>
    /// <remarks>按出现顺序提取双位十六进制片段，忽略其他文本。</remarks>
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
    /// 将十六进制字节文本转换为字节数组。
    /// </summary>
    /// <example>in: 2E3D; out: result[0] is 46, result[1] is 61</example>
    /// <param name="hexadecimalThings">要转换的十六进制字符串。</param>
    /// <returns>由文本中匹配的双位十六进制片段组成的字节数组；无匹配时返回空数组。</returns>
    /// <remarks>按出现顺序提取双位十六进制片段，忽略其他文本。</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] LongHexToDecBytes(string hexadecimalThings)
    {
        var mc = Regex.Matches(hexadecimalThings, @"(?i)[\da-f]{2}");
        return (from Match m in mc select Convert.ToByte(m.Value, 16)).ToArray();
    }

    #endregion
}
