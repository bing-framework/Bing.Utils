using System.Text.RegularExpressions;

namespace Bing.Helpers;

/// <summary>
/// 提供字符串处理工具。
/// </summary>
public partial class Str
{
    /// <summary>
    /// 拼音区码计算使用的固定 GBK 编码。
    /// </summary>
    private static readonly Encoding PinyinEncoding = CodePagesEncodingProvider.Instance.GetEncoding(936);

    #region Join(将集合连接为带分隔符的字符串)

    /// <summary>
    /// 将集合连接为字符串。
    /// </summary>
    /// <typeparam name="T">集合元素类型。</typeparam>
    /// <param name="list">集合。</param>
    /// <param name="quotes">元素两侧的引号。</param>
    /// <param name="separator">元素分隔符。</param>
    /// <returns>连接后的字符串；集合为 null 时返回空字符串。</returns>
    public static string Join<T>(IEnumerable<T> list, string quotes = "", string separator = ",")
    {
        if (list == null)
            return string.Empty;
        var result = new StringBuilder();
        foreach (var each in list)
            result.AppendFormat("{0}{1}{0}{2}", quotes, each, separator);
        if (separator == "")
            return result.ToString();
        return result.ToString().TrimEnd(separator.ToCharArray());
    }

    #endregion

    #region ToUnicode(字符串转Unicode)

    /// <summary>
    /// 将字符串转换为 Unicode 转义序列。
    /// </summary>
    /// <param name="value">待转换字符串。</param>
    /// <returns>UTF-16 代码单元组成的 Unicode 转义序列。</returns>
    public static string ToUnicode(string value)
    {
        var bytes = Encoding.Unicode.GetBytes(value);
        var sb = new StringBuilder();
        for (int i = 0; i < bytes.Length; i += 2)
            sb.AppendFormat("\\u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'),
                bytes[i].ToString("x").PadLeft(2, '0'));
        return sb.ToString();
    }

    #endregion

    #region ToUnicodeByCn(中文字符串转Unicode)

    /// <summary>
    /// 将中文字符转换为 Unicode 转义序列。
    /// </summary>
    /// <param name="value">待转换字符串。</param>
    /// <returns>转换后的字符串；非中文字符保持原样。</returns>
    public static string ToUnicodeByCn(string value)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(value))
        {
            char[] chars = value.ToCharArray();
            for (int i = 0; i < value.Length; i++)
            {
                // 将中文字符串转换为十进制整数，然后转为十六进制Unicode字符
                sb.Append(Regex.IsMatch(chars[i].ToString(), "([\u4e00-\u9fa5])")
                    ? ToUnicode(chars[i].ToString())
                    : chars[i].ToString());
            }
        }
        return sb.ToString();
    }

    #endregion

    #region PinYin(获取汉字的拼音简码)

    /// <summary>
    /// 获取汉字的拼音首字母。
    /// </summary>
    /// <param name="chineseText">要转换的文本。</param>
    /// <returns>小写拼音首字母；无法转换的字符保持原样。</returns>
    public static string PinYin(string chineseText)
    {
        if (string.IsNullOrWhiteSpace(chineseText))
            return string.Empty;
        var result = new StringBuilder();
        foreach (var text in chineseText)
            result.AppendFormat("{0}", ResolvePinYin(text));
        return result.ToString().ToLower();
    }

    /// <summary>
    /// 解析单个字符的拼音首字母。
    /// </summary>
    /// <param name="text">要解析的字符。</param>
    /// <returns>拼音首字母；无法转换时返回原字符。</returns>
    private static string ResolvePinYin(char text)
    {
        var charBytes = PinyinEncoding.GetBytes(text.ToString());
        if (charBytes.Length < 2 || charBytes[0] < 127)
            return text.ToString();
        var unicode = (ushort)(charBytes[0] * 256 + charBytes[1]);
        var pinYin = ResolveByCode(unicode);
        if (!string.IsNullOrWhiteSpace(pinYin))
            return pinYin;
        pinYin = ResolveByConst(text.ToString());
        return string.IsNullOrEmpty(pinYin) ? text.ToString() : pinYin;
    }

    /// <summary>
    /// 按 GBK 区码获取拼音首字母。
    /// </summary>
    /// <param name="unicode">GBK 双字节编码。</param>
    /// <returns>拼音首字母；未命中区码时返回空字符串。</returns>
    private static string ResolveByCode(ushort unicode)
    {
        if (unicode >= '\uB0A1' && unicode <= '\uB0C4')
            return "A";
        if (unicode >= '\uB0C5' && unicode <= '\uB2C0' && unicode != 45464)
            return "B";
        if (unicode >= '\uB2C1' && unicode <= '\uB4ED')
            return "C";
        if (unicode >= '\uB4EE' && unicode <= '\uB6E9')
            return "D";
        if (unicode >= '\uB6EA' && unicode <= '\uB7A1')
            return "E";
        if (unicode >= '\uB7A2' && unicode <= '\uB8C0')
            return "F";
        if (unicode >= '\uB8C1' && unicode <= '\uB9FD')
            return "G";
        if (unicode >= '\uB9FE' && unicode <= '\uBBF6')
            return "H";
        if (unicode >= '\uBBF7' && unicode <= '\uBFA5')
            return "J";
        if (unicode >= '\uBFA6' && unicode <= '\uC0AB')
            return "K";
        if (unicode >= '\uC0AC' && unicode <= '\uC2E7')
            return "L";
        if (unicode >= '\uC2E8' && unicode <= '\uC4C2')
            return "M";
        if (unicode >= '\uC4C3' && unicode <= '\uC5B5')
            return "N";
        if (unicode >= '\uC5B6' && unicode <= '\uC5BD')
            return "O";
        if (unicode >= '\uC5BE' && unicode <= '\uC6D9')
            return "P";
        if (unicode >= '\uC6DA' && unicode <= '\uC8BA')
            return "Q";
        if (unicode >= '\uC8BB' && unicode <= '\uC8F5')
            return "R";
        if (unicode >= '\uC8F6' && unicode <= '\uCBF9')
            return "S";
        if (unicode >= '\uCBFA' && unicode <= '\uCDD9')
            return "T";
        if (unicode >= '\uCDDA' && unicode <= '\uCEF3')
            return "W";
        if (unicode >= '\uCEF4' && unicode <= '\uD188')
            return "X";
        if (unicode >= '\uD1B9' && unicode <= '\uD4D0')
            return "Y";
        if (unicode >= '\uD4D1' && unicode <= '\uD7F9')
            return "Z";
        return string.Empty;
    }

    /// <summary>
    /// 通过内置字符表获取拼音首字母。
    /// </summary>
    /// <param name="text">要查询的单个字符。</param>
    /// <returns>拼音首字母；未收录时返回空字符串。</returns>
    private static string ResolveByConst(string text)
    {
        int index = Const.ChinesePinYin.IndexOf(text, StringComparison.Ordinal);
        if (index < 0)
            return string.Empty;
        return Const.ChinesePinYin.Substring(index + 1, 1);
    }

    #endregion

    #region FullPinYin(获取汉字的全拼)

    /// <summary>
    /// 将汉字转换为不带声调的全拼。
    /// </summary>
    /// <param name="text">要转换的文本。</param>
    /// <returns>首字母大写的连续拼音；非中文及无法转换的字符保持原样。</returns>
    public static string FullPinYin(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var result = new StringBuilder(text.Length);
        foreach (var value in text)
            result.Append(ResolveFullPinYin(value));
        return result.ToString();
    }

    /// <summary>
    /// 解析单个字符的不带声调全拼。
    /// </summary>
    /// <param name="value">要解析的字符。</param>
    /// <returns>首字母大写的拼音；非中文及无法转换的字符保持原样。</returns>
    private static string ResolveFullPinYin(char value)
    {
        if (value < '\u4e00' || value > '\u9fa5')
            return value.ToString();

        var bytes = PinyinEncoding.GetBytes(value.ToString());
        if (bytes.Length < 2)
            return value.ToString();

        var code = bytes[0] * 256 + bytes[1] - 65536;
        var specialPinyin = ResolveSpecialFullPinYin(code);
        if (specialPinyin != null)
            return specialPinyin;

        for (var index = Const.SpellCode.Length - 1; index >= 0; index--)
        {
            if (Const.SpellCode[index] <= code)
                return Const.SpellLetter[index];
        }

        return value.ToString();
    }

    /// <summary>
    /// 解析区码表中的特殊拼音映射。
    /// </summary>
    /// <param name="code">GBK 区码。</param>
    /// <returns>特殊拼音；没有特殊映射时返回 null。</returns>
    private static string ResolveSpecialFullPinYin(int code)
    {
        switch (code)
        {
            case -9254:
                return "Zhen";
            case -8985:
                return "Qian";
            case -5463:
                return "Jia";
            case -8274:
                return "Ge";
            case -5448:
                return "Ga";
            case -5447:
                return "La";
            case -4649:
                return "Chen";
            case -5436:
            case -5213:
                return "Mao";
            case -3597:
                return "Die";
            case -5659:
                return "Tian";
            default:
                return null;
        }
    }

    #endregion

    #region FirstLower(首字母小写)

    /// <summary>
    /// 将首字母转为小写。
    /// </summary>
    /// <param name="value">待转换字符串。</param>
    /// <returns>转换后的字符串；空白输入返回空字符串。</returns>
    public static string FirstLower(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return $"{value.Substring(0, 1).ToLower()}{value.Substring(1)}";
    }

    #endregion

    #region FirstUpper(首字母大写)

    /// <summary>
    /// 将首字母转为大写。
    /// </summary>
    /// <param name="value">待转换字符串。</param>
    /// <returns>转换后的字符串；空白输入返回空字符串。</returns>
    public static string FirstUpper(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return $"{value.Substring(0, 1).ToUpper()}{value.Substring(1)}";
    }

    #endregion

    #region Empty(空字符串)

    /// <summary>
    /// 空字符串。
    /// </summary>
    public static string Empty => string.Empty;

    #endregion

    #region Distinct(去除重复)

    /// <summary>
    /// 移除重复字符。
    /// </summary>
    /// <param name="value">待处理字符串。</param>
    /// <returns>保留字符首次出现顺序的字符串。</returns>
    public static string Distinct(string value)
    {
        var array = value.ToCharArray();
        return new string(array.Distinct().ToArray());
    }

    #endregion

    #region Truncate(截断字符串)

    /// <summary>
    /// 截断字符串。
    /// </summary>
    /// <param name="text">待截断文本。</param>
    /// <param name="length">保留的字符串长度。</param>
    /// <param name="endChatCount">追加的结束符号个数。</param>
    /// <param name="endChar">结束符号。</param>
    /// <returns>截断并追加结束符号后的文本；空白输入返回空字符串。</returns>
    public static string Truncate(string text, int length, int endChatCount = 0, string endChar = ".")
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        if (text.Length < length)
            return text;
        return $"{text.Substring(0, length)}{GetEndString(endChatCount, endChar)}";
    }

    /// <summary>
    /// 生成结束字符串。
    /// </summary>
    /// <param name="endCharCount">结束符号个数。</param>
    /// <param name="endChar">结束符号。</param>
    /// <returns>重复指定次数的结束符号。</returns>
    private static string GetEndString(int endCharCount, string endChar)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < endCharCount; i++)
            sb.Append(endChar);
        return sb.ToString();
    }

    #endregion

    #region GetLastProperty(获取最后一个属性)

    /// <summary>
    /// 获取属性路径的末级名称。
    /// </summary>
    /// <param name="propertyName">以点分隔的属性路径。</param>
    /// <returns>末级属性名称；空白输入返回空字符串。</returns>
    public static string GetLastProperty(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return string.Empty;
        var lastIndex = propertyName.LastIndexOf(".", StringComparison.Ordinal) + 1;
        return propertyName.Substring(lastIndex);
    }

    #endregion

    #region GetHideMobile(获取隐藏中间几位后的手机号码)

    /// <summary>
    /// 遮蔽手机号码中间部分。
    /// </summary>
    /// <param name="value">手机号码。</param>
    /// <returns>保留首尾各三位的遮蔽结果；空白输入返回空字符串。</returns>
    public static string GetHideMobile(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return $"{value.Substring(0, 3)}******{value.Substring(value.Length - 3)}";
    }

    #endregion

    #region GetStringLength(获取字符串的字节数)

    /// <summary>
    /// 按旧规则估算文本长度。
    /// </summary>
    /// <param name="value">待计算文本。</param>
    /// <returns>估算长度；空白输入返回 0。</returns>
    /// <remarks>按 ASCII 编码计算，每个问号字节额外计一次，包括原文中的问号。</remarks>
    public static int GetStringLength(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;
        int strLength = 0;
        var encoding = new ASCIIEncoding();
        // 将字符串转换为ASCII编码的字节数字
        byte[] bytes = encoding.GetBytes(value);
        for (var i = 0; i <= bytes.Length - 1; i++)
        {
            if (bytes[i] == 63)
                strLength++;
            strLength++;
        }
        return strLength;
    }

    #endregion

    #region GenerateNonceStr(生成随机字符串)

    /// <summary>
    /// 生成随机字符串。
    /// </summary>
    /// <returns>不含连字符的 GUID 字符串。</returns>
    public static string GenerateNonceStr() => Guid.NewGuid().ToString("N");

    #endregion

    #region SplitWordGroup(分隔词组)

    /// <summary>
    /// 按单词边界分隔并转为小写。
    /// </summary>
    /// <param name="value">待分隔文本。</param>
    /// <param name="separator">单词分隔符。</param>
    /// <returns>分隔后的字符串；空白输入返回空字符串。</returns>
    public static string SplitWordGroup(string value, char separator = '-')
    {
        var pattern = @"([A-Z])(?=[a-z])|(?<=[a-z])([A-Z]|[0-9]+)";
        return string.IsNullOrWhiteSpace(value) ? string.Empty : System.Text.RegularExpressions.Regex.Replace(value, pattern, $"{separator}$1$2").TrimStart(separator).ToLower();
    }

    #endregion
}
