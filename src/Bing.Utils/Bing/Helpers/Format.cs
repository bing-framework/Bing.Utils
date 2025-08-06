using System.Globalization;

namespace Bing.Helpers;

/// <summary>
/// 格式化 操作
/// </summary>
public static partial class Format
{
    /// <summary>
    /// 加密中国大陆手机号码，显示前3位和后2位，中间用星号遮盖
    /// </summary>
    /// <param name="phone">手机号码，应为11位数字</param>
    /// <returns>
    /// 加密后的手机号码格式：138******86
    /// 如果输入为空、空白或长度不足5位，则返回空字符串
    /// </returns>
    /// <remarks>
    /// 此方法专门用于中国大陆手机号码的加密显示。
    /// 标准的中国手机号码为11位，但此方法最低要求5位以确保有足够字符进行加密。
    /// 对于长度不足的输入，为了安全考虑返回空字符串而不是抛出异常。
    /// </remarks>
    /// <example>
    /// <code>
    /// string encrypted1 = Format.EncryptPhoneOfChina("13812345678");  // 返回 "138******78"
    /// string encrypted2 = Format.EncryptPhoneOfChina("1234");         // 返回 ""（长度不足）
    /// string encrypted3 = Format.EncryptPhoneOfChina("");             // 返回 ""
    /// string encrypted4 = Format.EncryptPhoneOfChina(null);           // 返回 ""
    /// </code>
    /// </example>
    public static string EncryptPhoneOfChina(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 5)
            return string.Empty;
        try
        {
            return $"{phone.Substring(0, 3)}******{phone.Substring(phone.Length - 2, 2)}";
        }
        catch (ArgumentOutOfRangeException)
        {
            // 如果字符串操作失败，返回空字符串以确保安全
            return string.Empty;
        }
    }

    /// <summary>
    /// 加密中国车牌号，显示前2位和后2位，中间用星号遮盖
    /// </summary>
    /// <param name="plateNumber">车牌号，标准格式如"京A12345"等</param>
    /// <returns>
    /// 加密后的车牌号格式：京A***45
    /// 如果输入为空、空白或长度不足4位，则返回空字符串
    /// </returns>
    /// <remarks>
    /// 此方法用于中国车牌号的加密显示。
    /// 中国标准车牌号通常为7位（包括汉字、字母和数字），但此方法最低要求4位。
    /// 对于长度不足的输入，为了安全考虑返回空字符串。
    /// </remarks>
    /// <example>
    /// <code>
    /// string encrypted1 = Format.EncryptPlateNumberOfChina("京A12345");  // 返回 "京A***45"
    /// string encrypted2 = Format.EncryptPlateNumberOfChina("沪B67890");  // 返回 "沪B***90"
    /// string encrypted3 = Format.EncryptPlateNumberOfChina("ABC");       // 返回 ""（长度不足）
    /// string encrypted4 = Format.EncryptPlateNumberOfChina("");          // 返回 ""
    /// </code>
    /// </example>
    public static string EncryptPlateNumberOfChina(string plateNumber)
    {
        if (string.IsNullOrWhiteSpace(plateNumber) || plateNumber.Length < 4)
            return string.Empty;
        try
        {
            return $"{plateNumber.Substring(0, 2)}***{plateNumber.Substring(plateNumber.Length - 2, 2)}";
        }
        catch (ArgumentOutOfRangeException)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 加密汽车VIN码，显示前3位和后3位，中间用星号遮盖
    /// </summary>
    /// <param name="vinCode">汽车VIN码，标准为17位字符</param>
    /// <returns>
    /// 加密后的VIN码格式：1HG***89X
    /// 如果输入为空、空白或长度不足6位，则返回空字符串
    /// </returns>
    /// <remarks>
    /// 此方法用于汽车VIN（Vehicle Identification Number）码的加密显示。
    /// 标准VIN码为17位字符，但此方法最低要求6位以确保有足够字符进行加密。
    /// VIN码通常包含数字和大写字母（除了I、O、Q）。
    /// </remarks>
    /// <example>
    /// <code>
    /// string encrypted1 = Format.EncryptVinCode("1HGBH41JXMN109186");     // 返回 "1HG***186"
    /// string encrypted2 = Format.EncryptVinCode("WBAPH7G58ANM12345");     // 返回 "WBA***345"
    /// string encrypted3 = Format.EncryptVinCode("12345");                 // 返回 ""（长度不足）
    /// string encrypted4 = Format.EncryptVinCode("");                      // 返回 ""
    /// </code>
    /// </example>
    public static string EncryptVinCode(string vinCode)
    {
        if (string.IsNullOrWhiteSpace(vinCode) || vinCode.Length < 6)
            return string.Empty;
        try
        {
            return $"{vinCode.Substring(0, 3)}***********{vinCode.Substring(vinCode.Length - 3, 3)}";
        }
        catch (ArgumentOutOfRangeException)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 格式化金额，支持加密显示和多种格式化选项
    /// </summary>
    /// <param name="money">金额数值</param>
    /// <param name="isEncrypt">是否加密显示，true时显示为"***"</param>
    /// <returns>
    /// 格式化后的金额字符串。
    /// 加密时返回"***"；正常时返回带两位小数和千位分隔符的格式，如"1,234.56"
    /// </returns>
    /// <remarks>
    /// 此方法使用当前系统区域设置进行数字格式化。
    /// 在中文环境下，千位分隔符为逗号，小数点为点号。
    /// 负数会显示负号。
    /// </remarks>
    /// <example>
    /// <code>
    /// string formatted1 = Format.FormatMoney(1234.56m);           // 返回 "1,234.56"
    /// string formatted2 = Format.FormatMoney(1234.56m, true);     // 返回 "***"
    /// string formatted3 = Format.FormatMoney(-1000.5m);           // 返回 "-1,000.50"
    /// string formatted4 = Format.FormatMoney(0m);                 // 返回 "0.00"
    /// </code>
    /// </example>
    public static string FormatMoney(decimal money, bool isEncrypt = false) =>
        isEncrypt ? "***" : money.ToString("N2", CultureInfo.CurrentCulture);

    /// <summary>
    /// 格式化金额，支持自定义格式和区域设置
    /// </summary>
    /// <param name="money">金额数值</param>
    /// <param name="format">数字格式字符串，如"C"（货币）、"N2"（两位小数）、"F0"（整数）</param>
    /// <param name="culture">区域设置，为null时使用当前系统设置</param>
    /// <returns>根据指定格式和区域设置格式化的金额字符串</returns>
    /// <remarks>
    /// 此方法提供更灵活的金额格式化选项。
    /// 常用格式：
    /// - "C" 或 "C2"：货币格式，如 ¥1,234.56
    /// - "N" 或 "N2"：数字格式，如 1,234.56
    /// - "F0"：无小数的固定点格式，如 1235
    /// - "P"：百分比格式，如 123,456.00%
    /// </remarks>
    /// <example>
    /// <code>
    /// // 货币格式
    /// string currency = Format.FormatMoney(1234.56m, "C");                    // 返回 "¥1,234.56"
    /// 
    /// // 无小数格式
    /// string integer = Format.FormatMoney(1234.56m, "F0");                    // 返回 "1235"
    /// 
    /// // 美国格式
    /// var usCulture = new CultureInfo("en-US");
    /// string usFormat = Format.FormatMoney(1234.56m, "C", usCulture);         // 返回 "$1,234.56"
    /// </code>
    /// </example>
    public static string FormatMoney(decimal money, string format, CultureInfo culture = null)
    {
        if (string.IsNullOrWhiteSpace(format))
            format = "N2";

        culture ??= CultureInfo.CurrentCulture;

        try
        {
            return money.ToString(format, culture);
        }
        catch (FormatException)
        {
            // 如果格式字符串无效，使用默认格式
            return money.ToString("N2", culture);
        }
    }

    /// <summary>
    /// 通用字符串加密方法，显示指定位数的前缀和后缀，中间用星号遮盖
    /// </summary>
    /// <param name="input">要加密的字符串</param>
    /// <param name="prefixLength">保留的前缀长度</param>
    /// <param name="suffixLength">保留的后缀长度</param>
    /// <param name="maskChar">遮盖字符，默认为星号</param>
    /// <param name="maskLength">遮盖部分的长度，默认为6</param>
    /// <returns>
    /// 加密后的字符串。
    /// 如果输入为空或长度不足，返回空字符串
    /// </returns>
    /// <remarks>
    /// 这是一个通用的字符串加密方法，可用于各种需要部分遮盖的场景。
    /// 要求输入字符串长度至少为 prefixLength + suffixLength。
    /// </remarks>
    /// <example>
    /// <code>
    /// string encrypted1 = Format.EncryptString("1234567890", 2, 2);          // 返回 "12******90"
    /// string encrypted2 = Format.EncryptString("abcdefgh", 3, 1, '#', 4);    // 返回 "abc####h"
    /// string encrypted3 = Format.EncryptString("123", 2, 2);                 // 返回 ""（长度不足）
    /// </code>
    /// </example>
    public static string EncryptString(string input, int prefixLength, int suffixLength, char maskChar = '*', int maskLength = 6)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        if (prefixLength < 0 || suffixLength < 0)
            throw new ArgumentException("前缀和后缀长度不能为负数");
        if (input.Length < prefixLength + suffixLength)
            return string.Empty;
        try
        {
            var prefix = input.Substring(0, prefixLength);
            var suffix = input.Substring(input.Length - suffixLength, suffixLength);
            var mask = new string(maskChar, Math.Max(1, maskLength));
            return $"{prefix}{mask}{suffix}";
        }
        catch (ArgumentOutOfRangeException)
        {
            return string.Empty;
        }
    }
}