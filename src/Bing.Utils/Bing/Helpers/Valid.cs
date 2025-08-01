using System.Globalization;
using System.Text.RegularExpressions;
using Bing.Extensions;
using Bing.Text.RegularExpressions;

namespace Bing.Helpers;

/// <summary>
/// 验证 操作
/// </summary>
public static partial class Valid
{
    #region IsNull(是否为null)

    /// <summary>
    /// 判断对象是否为 null
    /// </summary>
    /// <param name="value">要检查的对象</param>
    /// <returns>如果对象为 null，则返回 true；否则返回 false</returns>
    public static bool IsNull(object value) => null == value;

    #endregion

    #region IsNotNull(是否不为null)

    /// <summary>
    /// 判断对象是否不为 null
    /// </summary>
    /// <param name="value">要检查的对象</param>
    /// <returns>如果对象不为 null，则返回 true；否则返回 false</returns>
    public static bool IsNotNull(object value) => null != value;

    #endregion

    #region IsEmpty(验证是否为null)

    /// <summary>
    /// 判断对象是否为空
    /// </summary>
    /// <param name="value">要检查的对象</param>
    /// <returns>如果对象为 null 或空字符串，则返回 true；否则返回 false</returns>
    public static bool IsEmpty(object value)
    {
        return value switch
        {
            null => true,
            string stringValue => string.IsNullOrWhiteSpace(stringValue),
            IEnumerable<object> enumerableValue => !enumerableValue.Any(),
            _ => false
        };
    }

    #endregion

    #region IsNotEmpty(是否不为空)

    /// <summary>
    /// 判断对象是否不为空
    /// </summary>
    /// <param name="value">要检查的对象</param>
    /// <returns>如果对象不为 null 且不为空，则返回 true；否则返回 false</returns>
    public static bool IsNotEmpty(object value) => !IsEmpty(value);

    #endregion

    #region 邮箱相关

    /// <summary>
    /// 判断是否为有效的邮箱地址
    /// </summary>
    /// <param name="value">邮箱地址字符串</param>
    /// <param name="isRestrict">是否使用严格验证模式，默认为 false</param>
    /// <returns>如果是有效邮箱地址，则返回 true；否则返回 false</returns>
    public static bool IsEmail(string value, bool isRestrict = false)
    {
        if (value.IsEmpty())
            return false;
        var pattern = isRestrict
            ? @"^(?("")("".+?""@)|(([0-9a-zA-Z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-zA-Z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,6}))$"
            : @"^[\w-]+(\.[\w-]+)*@[\w-]+(\.[\w-]+)+$";
        return value.IsMatch(pattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 判断字符串中是否包含邮箱地址
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <param name="isRestrict">是否使用严格验证模式，默认为 false</param>
    /// <returns>如果字符串包含邮箱地址，则返回 true；否则返回 false</returns>
    public static bool HasEmail(string value, bool isRestrict = false)
    {
        if (value.IsEmpty())
            return false;
        var pattern = isRestrict
            ? @"^(?("")("".+?""@)|(([0-9a-zA-Z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-zA-Z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,6}))$"
            : @"[\w-]+(\.[\w-]+)*@[\w-]+(\.[\w-]+)+";
        return value.IsMatch(pattern, RegexOptions.IgnoreCase);
    }

    #endregion

    #region 电话号码

    /// <summary>
    /// 是否合法的手机号码（旧版格式，已过时）
    /// </summary>
    /// <param name="value">手机号码</param>
    /// <remarks>建议使用 <see cref="IsMobileNumber"/> 方法</remarks>
    [Obsolete("建议使用 IsMobileNumber 方法，此方法的正则表达式已过时")]
    public static bool IsPhoneNumber(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"^(0|86|17951)?(13[0-9]|15[012356789]|18[0-9]|14[57]|17[678])[0-9]{8}$");
    }

    /// <summary>
    /// 判断是否为有效的手机号码（简单验证）
    /// </summary>
    /// <param name="value">手机号码字符串</param>
    /// <param name="isRestrict">是否使用严格验证模式，默认为 false</param>
    /// <returns>如果是有效手机号码，则返回 true；否则返回 false</returns>
    public static bool IsMobileNumberSimple(string value, bool isRestrict = false)
    {
        if (value.IsEmpty())
            return false;
        var pattern = isRestrict ? @"^[1][3-8]\d{9}$" : @"^[1]\d{10}$";
        return value.IsMatch(pattern);
    }

    /// <summary>
    /// 判断是否为有效的中国大陆手机号码
    /// </summary>
    /// <param name="value">手机号码字符串</param>
    /// <returns>如果是有效的中国大陆手机号码，则返回 true；否则返回 false</returns>
    public static bool IsMobileNumber(string value)
    {
        if (value.IsEmpty())
            return false;
        value = value.Trim();
        /*
         * 最新手机号码号段规则 (截至2024年):
         * 13[0-9], 14[0,9], 15[0-9], 16[0-9], 17[0-9], 18[0-9], 19[0-9]
         * 移动号段: 134,135,136,137,138,139,147,148,150,151,152,157,158,159,172,178,182,183,184,187,188,195,198
         * 联通号段: 130,131,132,145,146,155,156,166,167,171,175,176,185,186,196
         * 电信号段: 133,149,153,173,174,177,180,181,189,190,191,193,194,199
         * 广电号段: 192
         * 虚拟运营商: 170,171(部分)
         */
        return value.IsMatch(@"^1(3[0-9]|4[57]|5[0-35-9]|8[0-9]|70)\d{8}$");
    }

    /// <summary>
    /// 判断字符串中是否包含手机号码
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <param name="isRestrict">是否使用严格验证模式，默认为 false</param>
    /// <returns>如果字符串包含手机号码，则返回 true；否则返回 false</returns>
    public static bool HasMobileNumberSimple(string value, bool isRestrict = false)
    {
        if (value.IsEmpty())
            return false;
        var pattern = isRestrict ? @"[1][3-8]\d{9}" : @"[1]\d{10}";
        return value.IsMatch(pattern);
    }

    /// <summary>
    /// 判断是否为中国移动手机号码
    /// </summary>
    /// <param name="value">手机号码字符串</param>
    /// <returns>如果是中国移动号码，则返回 true；否则返回 false</returns>
    public static bool IsChinaMobilePhone(string value)
    {
        if (value.IsEmpty())
            return false;
        /*
         * 中国移动：China Mobile (2024年最新)
         * 134,135,136,137,138,139,147,148,150,151,152,157,158,159,172,178,182,183,184,187,188,195,198
         */
        return value.IsMatch(@"^(?:0|86|\+86)?1(3[4-9]|4[78]|5[0127-9]|7[28]|8[2-478]|9[58])\d{8}$");
    }

    /// <summary>
    /// 判断是否为中国联通手机号码
    /// </summary>
    /// <param name="value">手机号码字符串</param>
    /// <returns>如果是中国联通号码，则返回 true；否则返回 false</returns>
    public static bool IsChinaUnicomPhone(string value)
    {
        if (value.IsEmpty())
            return false;
        /*
         * 中国联通：China Unicom (2024年最新)
         * 130,131,132,145,146,155,156,166,167,171,175,176,185,186,196
         */
        return value.IsMatch(@"^(?:0|86|\+86)?1(3[0-2]|4[56]|5[56]|6[67]|7[156]|8[56]|96)\d{8}$");
    }

    /// <summary>
    /// 判断是否为中国电信手机号码
    /// </summary>
    /// <param name="value">手机号码字符串</param>
    /// <returns>如果是中国电信号码，则返回 true；否则返回 false</returns>
    public static bool IsChinaTelecomPhone(string value)
    {
        if (value.IsEmpty())
            return false;
        /*
         * 中国电信：China Telecom (2024年最新)
         * 133,149,153,173,174,177,180,181,189,190,191,193,194,199
         */
        return value.IsMatch(@"^(?:0|86|\+86)?1(33|49|53|7[347]|8[019]|9[01349])\d{8}$");
    }

    /// <summary>
    /// 判断是否为中国广电手机号码
    /// </summary>
    /// <param name="value">手机号码字符串</param>
    /// <returns>如果是中国广电号码，则返回 true；否则返回 false</returns>
    public static bool IsChinaBroadcastPhone(string value)
    {
        if (value.IsEmpty())
            return false;
        /*
         * 中国广电：China Broadcasting Network
         * 192
         */
        return value.IsMatch(@"^(?:0|86|\+86)?192\d{8}$");
    }

    /// <summary>
    /// 判断是否为有效的中国固定电话号码，格式：010-85849685 或 01085849685
    /// </summary>
    /// <param name="value">电话号码字符串</param>
    /// <returns>如果是有效的固定电话，则返回 true；否则返回 false</returns>
    public static bool IsTel(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"^\d{3,4}-?\d{6,8}$", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 判断是否为座机号码、400电话、800电话
    /// </summary>
    /// <param name="value">电话号码字符串</param>
    /// <returns>如果是有效的座机或400/800电话，则返回 true；否则返回 false</returns>
    public static bool IsTel400800(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"^(0\d{2,3}[\- ]?[1-9]\d{6,7}|[48]00[\- ]?[1-9]\d{2}[\- ]?\d{4})$");
    }

    #endregion

    #region IsIdCard(是否身份证号码)

    /// <summary>
    /// 判断是否为有效的身份证号码（支持15位和18位）
    /// </summary>
    /// <param name="value">身份证号码字符串</param>
    /// <returns>如果是有效的身份证号码，则返回 true；否则返回 false</returns>
    public static bool IsIdCard(string value)
    {
        if (value.IsEmpty())
            return false;
        if (value.Length == 15)
            return value.IsMatch(@"^[1-9]\d{7}((0\d)|(1[0-2]))(([0|1|2]\d)|3[0-1])\d{3}$");
        if (value.Length == 18)
            return value.IsMatch(@"^[1-9]\d{5}[1-9]\d{3}((0\d)|(1[0-2]))(([0|1|2]\d)|3[0-1])((\d{4})|\d{3}[Xx])$", RegexOptions.IgnoreCase);
        return false;
    }

    #endregion

    #region IsGuid(是否Guid)

    /// <summary>
    /// 判断字符串是否为有效的Guid格式
    /// </summary>
    /// <param name="value">Guid字符串</param>
    /// <returns>如果是有效的Guid格式，则返回 true；否则返回 false</returns>
    public static bool IsGuid(string value)
    {
        if (value.IsEmpty())
            return false;
        return Guid.TryParse(value, out _);
    }

    #endregion

    #region IsVersion(是否有效的版本号)

    /// <summary>
    /// 判断是否为有效的版本号格式，范例：1.3,1.1.5,1.25.256
    /// </summary>
    /// <param name="value">版本号字符串</param>
    /// <param name="maxSegments">最大段数，默认为5</param>
    /// <returns>如果是有效版本号，则返回 true；否则返回 false</returns>
    public static bool IsVersion(string value, int maxSegments = 5)
    {
        if (value.IsEmpty())
            return false;
        value = value.Replace("^", "").Replace("$", "");
        return Version.TryParse(value, out _) || value.IsMatch($@"^\d{{1,4}}(?:\.\d{{1,4}}){{0,{maxSegments - 1}}}$");
    }

    #endregion

    #region 网络相关

    /// <summary>
    /// 判断字符串是否为有效的URL地址
    /// </summary>
    /// <param name="value">URL地址字符串</param>
    /// <returns>如果是有效的URL，则返回 true；否则返回 false</returns>
    public static bool IsUrl(string value)
    {
        if (value.IsEmpty())
            return false;
        return
            value.IsMatch(
                @"^(http|https)\://([a-zA-Z0-9\.\-]+(\:[a-zA-Z0-9\.&%\$\-]+)*@)*((25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9])\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])|localhost|([a-zA-Z0-9\-]+\.)*[a-zA-Z0-9\-]+\.(com|edu|gov|int|mil|net|org|biz|arpa|info|name|pro|aero|coop|museum|[a-zA-Z]{1,10}))(\:[0-9]+)*(/($|[a-zA-Z0-9\.\,\?\'\\\+&%\$#\=~_\-]+))*$",
                RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 判断字符串是否为有效的URI格式
    /// </summary>
    /// <param name="value">URI字符串</param>
    /// <returns>如果是有效的URI，则返回 true；否则返回 false</returns>
    public static bool IsUri(string value)
    {
        if (value.IsEmpty())
            return false;
        if (value.IndexOf(".", StringComparison.OrdinalIgnoreCase) == -1)
            return false;
        var schemes = new[]
        {
            "file",
            "ftp",
            "gopher",
            "http",
            "https",
            "ldap",
            "mailto",
            "net.pipe",
            "net.tcp",
            "news",
            "nntp",
            "telnet",
            "uuid"
        };

        var hasValidSchema = false;
        foreach (var scheme in schemes)
        {
            if (hasValidSchema)
                continue;
            if (value.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                hasValidSchema = true;
        }
        if (!hasValidSchema)
            value = "http://" + value;
        return Uri.IsWellFormedUriString(value, UriKind.Absolute);
    }

    /// <summary>
    /// 判断是否为主域名或www开头的域名URL
    /// </summary>
    /// <param name="value">URL地址字符串</param>
    /// <returns>如果是主域名URL，则返回 true；否则返回 false</returns>
    public static bool IsMainDomainUrl(string value)
    {
        if (value.IsEmpty())
            return false;
        return
            value.IsMatch(
                @"^http(s)?\://((www.)?[a-zA-Z0-9\-]+\.(com|edu|gov|int|mil|net|org|biz|arpa|info|name|pro|aero|coop|museum|[a-zA-Z]{1,10}))(\:[0-9]+)*(/($|[a-zA-Z0-9\.\,\?\'\\\+&%\$#\=~_\-]+))*$");
    }

    /// <summary>
    /// 判断是否为主域名格式
    /// </summary>
    /// <param name="value">域名字符串</param>
    /// <returns>如果是主域名格式，则返回 true；否则返回 false</returns>
    public static bool IsMainDomain(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(
            @"^((www.)?[a-zA-Z0-9\-]+\.(com|edu|gov|int|mil|net|org|biz|arpa|info|name|pro|aero|coop|museum|[a-zA-Z]{1,10}))(\:[0-9]+)*$",
            RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 判断是否为有效的域名格式
    /// </summary>
    /// <param name="value">域名字符串</param>
    /// <returns>如果是有效域名，则返回 true；否则返回 false</returns>
    public static bool IsDomain(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(
            @"^(([a-zA-Z0-9\-]+\.)?[a-zA-Z0-9\-]+\.(com|edu|gov|int|mil|net|org|biz|arpa|info|name|pro|aero|coop|museum|[a-zA-Z]{1,10}))(\:[0-9]+)*$",
            RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 判断是否为有效的MAC地址格式
    /// </summary>
    /// <param name="value">MAC地址字符串</param>
    /// <returns>如果是有效的MAC地址，则返回 true；否则返回 false</returns>
    public static bool IsMac(string value)
    {
        if (value.IsEmpty())
            return false;
        //return value.IsMatch(@"^([0-9A-F]{2}-){5}[0-9A-F]{2}$") || value.IsMatch(@"^[0-9A-F]{12}$");
        return value.IsMatch(RegexConst.MacAddress);
    }

    /// <summary>
    /// 判断字符串是否为有效的IPv4地址
    /// </summary>
    /// <param name="value">IP地址字符串</param>
    /// <returns>如果是有效IPv4地址，则返回 true；否则返回 false</returns>
    public static bool IsIpAddress(string value)
    {
        if (value.IsEmpty())
            return false;
        //return value.IsMatch(@"^(\d(25[0-5]|2[0-4][0-9]|1?[0-9]?[0-9])\d\.){3}\d(25[0-5]|2[0-4][0-9]|1?[0-9]?[0-9])\d$");
        return value.IsMatch(RegexConst.IPv4);
    }

    #endregion

    #region 中文相关

    /// <summary>
    /// 判断字符串是否只包含中文字符
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <returns>如果只包含中文字符，则返回 true；否则返回 false</returns>
    public static bool IsChineseWord(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return value.IsMatch(@"^[\u4e00-\u9fa5]{0,}$");
    }

    /// <summary>
    /// 判断字符串是否包含中文字符
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <returns>如果包含中文字符，则返回 true；否则返回 false</returns>
    public static bool IsChinese(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"^[\u4e00-\u9fa5]+$", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 判断字符串是否包含中文字符
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <returns>如果包含中文字符，则返回 true；否则返回 false</returns>
    public static bool HasChinese(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"[\u4e00-\u9fa5]+", RegexOptions.IgnoreCase);
    }

    #endregion

    #region 数字相关

    /// <summary>
    /// 判断字符串是否包含数字字符
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <returns>如果包含数字字符，则返回 true；否则返回 false</returns>
    public static bool HasNumber(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"\d");
    }

    /// <summary>
    /// 判断字符串是否表示整数（包括负数）
    /// </summary>
    /// <param name="value">数字字符串</param>
    /// <returns>如果是整数，则返回 true；否则返回 false</returns>
    public static bool IsInteger(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"^\-?[0-9]+$");
    }

    /// <summary>
    /// 判断字符串是否表示大于0的正整数
    /// </summary>
    /// <param name="value">数字字符串</param>
    /// <returns>如果是大于0的正整数，则返回 true；否则返回 false</returns>
    public static bool IsPositiveInteger(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"^[1-9]+\d*$");
    }

    /// <summary>
    /// 判断字符串是否可以转换为Int32类型
    /// </summary>
    /// <param name="value">数字字符串</param>
    /// <returns>如果可以转换为Int32，则返回 true；否则返回 false</returns>
    public static bool IsInt32(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"^[0-9]*$");
    }

    /// <summary>
    /// 判断字符串是否可以转换为Double类型
    /// </summary>
    /// <param name="value">数字字符串</param>
    /// <returns>如果可以转换为Double，则返回 true；否则返回 false</returns>
    public static bool IsDouble(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"^\d[.]?\d?$");
    }

    /// <summary>
    /// 判断字符串是否为指定范围和小数位数的Double类型
    /// </summary>
    /// <param name="value">数字字符串</param>
    /// <param name="minValue">最小值</param>
    /// <param name="maxValue">最大值</param>
    /// <param name="digit">小数位数限制，0表示不限制</param>
    /// <returns>如果满足条件，则返回 true；否则返回 false</returns>
    public static bool IsDouble(string value, double minValue, double maxValue, int digit)
    {
        if (value.IsEmpty())
            return false;
        var patten = $@"^\d[.]?\d{"{0,10}"}$";
        if (digit > 0)
            patten = $@"^\d[.]?\d{"{" + digit + "}"}$";
        if (value.IsMatch(patten))
        {
            var val = Convert.ToDouble(value);
            if (val >= minValue && val <= maxValue)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 判断字符串是否表示数字（包括小数）
    /// </summary>
    /// <param name="value">数字字符串</param>
    /// <returns>如果是数字，则返回 true；否则返回 false</returns>
    public static bool IsNumber(string value)
    {
        if (value.IsEmpty())
            return false;
        const string pattern = @"^(-?\d*)(\.\d+)?$";
        return Regex.IsMatch(value, pattern);
    }

    /// <summary>
    /// 判断字符串是否表示十进制数字
    /// </summary>
    /// <param name="value">数字字符串</param>
    /// <returns>如果是十进制数字，则返回 true；否则返回 false</returns>
    public static bool IsDecimal(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"^([0-9])[0-9]*(\.\w*)?$");
    }

    #endregion

    #region 银行卡相关

    /// <summary>
    /// 判断是否为有效的银行卡号格式
    /// </summary>
    /// <param name="value">银行卡号字符串</param>
    /// <returns>如果是有效银行卡号，则返回 true；否则返回 false</returns>
    public static bool IsBandCard(string value)
    {
        if (value.IsEmpty())
            return false;

        // 银行卡号通常为13-19位数字
        return value.IsMatch(@"^\d{13,19}$");
    }

    #endregion

    #region 账号相关

    /// <summary>
    /// 判断是否为有效的登录账号格式（6-30位字母数字组合，必须包含字母）
    /// </summary>
    /// <param name="value">登录账号字符串</param>
    /// <returns>如果是有效登录账号，则返回 true；否则返回 false</returns>
    public static bool IsLoginName(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"^(?=.*[a-zA-Z])[A-Za-z0-9]{6,30}$");
    }

    /// <summary>
    /// 判断是否为指定长度的有效登录账号格式
    /// </summary>
    /// <param name="value">登录账号字符串</param>
    /// <param name="min">最小长度</param>
    /// <param name="max">最大长度</param>
    /// <returns>如果是有效登录账号，则返回 true；否则返回 false</returns>
    public static bool IsLoginName(string value, int min, int max)
    {
        if (value.IsEmpty() || min < 0 || max < min)
            return false;
        return value.IsMatch($@"^(?=.*[a-zA-Z])[A-Za-z0-9]{{{min},{max}}}$");
    }

    /// <summary>
    /// 判断是否为有效的密码格式（6-25位，包含字母、数字和特殊字符）
    /// </summary>
    /// <param name="value">密码字符串</param>
    /// <returns>如果是有效密码格式，则返回 true；否则返回 false</returns>
    public static bool IsPasswordOne(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"^[\@A-Za-z0-9\!\#\$\%\^\&\*\.\~_]{6,25}$");
    }

    /// <summary>
    /// 判断是否为指定长度的有效密码格式
    /// </summary>
    /// <param name="value">密码字符串</param>
    /// <param name="min">最小长度</param>
    /// <param name="max">最大长度</param>
    /// <returns>如果是有效密码格式，则返回 true；否则返回 false</returns>
    public static bool IsPasswordOne(string value, int min, int max)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch($@"^[\@A-Za-z0-9\!\#\$\%\^\&\*\.\~_]{{{min},{max}}}$");
    }

    /// <summary>
    /// 判断是否为强密码格式（包含大小写字母、数字和特殊字符）
    /// </summary>
    /// <param name="value">密码字符串</param>
    /// <returns>如果是强密码格式，则返回 true；否则返回 false</returns>
    public static bool IsPasswordTwo(string value)
    {
        if (value.IsEmpty())
            return false;
        return
            value.IsMatch(
                @"(?=^.{6,25}$)(?=(?:.*?\d){1})(?=.*[a-z])(?=(?:.*?[A-Z]){1})(?=(?:.*?[!@#$%*()_+^&}{:;?.]){1})(?!.*\s)[0-9a-zA-Z!@#$%*()_+^&]*$");
    }

    #endregion

    #region 数据库相关

    /// <summary>
    /// 判断SQL语句是否包含危险字符
    /// </summary>
    /// <param name="value">SQL语句字符串</param>
    /// <returns>如果包含危险字符，则返回 true；否则返回 false</returns>
    public static bool IsSafeSqlString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        // 检查危险字符和SQL注入关键词
        var dangerousPatterns = new[]
        {
            @"[-|;|,|\/|\(|\)|\[|\]|\}|\{|%|@|\*|!|\']",
            @"\b(?:select|insert|delete|from|count|drop\s+table|update|truncate|asc|mid|char|xp_cmdshell|exec\s+master|netlocalgroup\s+administrators|net\s+user|or|and)\b"
        };
        return !dangerousPatterns.Any(pattern => value.IsMatch(pattern, RegexOptions.IgnoreCase));
    }

    #endregion

    #region 编码相关

    /// <summary>
    /// 判断字符串是否为有效的Base64编码
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <returns>如果是有效的Base64编码，则返回 true；否则返回 false</returns>
    public static bool IsBase64String(string value)
    {
        if (value.IsEmpty())
            return false;

        // 检查长度是否为4的倍数，并且只包含Base64字符
        return value.Length % 4 == 0 && value.IsMatch(@"^[A-Za-z0-9+/]*={0,2}$");
    }

    #endregion

    #region 中国特色标识

    /// <summary>
    /// 判断是否为有效的中国邮政编码
    /// </summary>
    /// <param name="value">邮政编码字符串</param>
    /// <returns>如果是有效邮政编码，则返回 true；否则返回 false</returns>
    public static bool IsChinesePostalCode(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"^[1-9]\d{5}$", RegexOptions.IgnoreCase);
    }

    #endregion

    #region 日期时间

    /// <summary>
    /// 判断字符串是否为有效的时间格式
    /// </summary>
    /// <param name="value">时间字符串</param>
    /// <returns>如果是有效时间格式，则返回 true；否则返回 false</returns>
    public static bool IsTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return value.IsMatch(@"^((([0-1]?[0-9])|(2[0-3])):([0-5]?[0-9])(:[0-5]?[0-9])?)$");
    }

    /// <summary>
    /// 判断字符串是否为有效的日期格式
    /// </summary>
    /// <param name="value">日期字符串</param>
    /// <param name="isRegex">是否使用正则表达式验证，默认为false</param>
    /// <returns>如果是有效日期，则返回 true；否则返回 false</returns>
    public static bool IsDate(string value, bool isRegex = false)
    {
        if (value.IsEmpty())
            return false;
        if (isRegex)
            //考虑到4年一度的366天，还有特殊的2月的日期
            return
                value.IsMatch(
                    @"^((((1[6-9]|[2-9]\d)\d{2})-(0?[13578]|1[02])-(0?[1-9]|[12]\d|3[01]))|(((1[6-9]|[2-9]\d)\d{2})-(0?[13456789]|1[012])-(0?[1-9]|[12]\d|30))|(((1[6-9]|[2-9]\d)\d{2})-0?2-(0?[1-9]|1\d|2[0-8]))|(((1[6-9]|[2-9]\d)(0[48]|[2468][048]|[13579][26])|((16|[2468][048]|[3579][26])00))-0?2-29-)) (20|21|22|23|[0-1]?\d):[0-5]?\d:[0-5]?\d$");
        return DateTime.TryParse(value, out var minValue);
    }

    /// <summary>
    /// 判断字符串是否符合指定格式的日期
    /// </summary>
    /// <param name="value">日期字符串</param>
    /// <param name="format">日期格式</param>
    /// <returns>如果符合指定格式，则返回 true；否则返回 false</returns>
    public static bool IsDate(string value, string format) => IsDate(value, format, null, DateTimeStyles.None);

    /// <summary>
    /// 判断字符串是否符合指定格式和文化的日期
    /// </summary>
    /// <param name="value">日期字符串</param>
    /// <param name="format">日期格式</param>
    /// <param name="provider">格式提供者</param>
    /// <param name="styles">日期样式</param>
    /// <returns>如果符合指定格式，则返回 true；否则返回 false</returns>
    public static bool IsDate(string value, string format, IFormatProvider provider, DateTimeStyles styles)
    {
        if (value.IsEmpty())
            return false;
        return DateTime.TryParseExact(value, format, provider, styles, out var minValue);
    }

    /// <summary>
    /// 判断日期时间是否大于最小值
    /// </summary>
    /// <param name="value">日期时间字符串</param>
    /// <param name="min">最小日期时间</param>
    /// <returns>如果大于最小值，则返回 true；否则返回 false</returns>
    public static bool IsDateTimeMin(string value, DateTime min)
    {
        if (value.IsEmpty())
            return false;
        if (DateTime.TryParse(value, out var dateTime))
        {
            if (DateTime.Compare(dateTime, min) >= 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 判断日期时间是否小于最大值
    /// </summary>
    /// <param name="value">日期时间字符串</param>
    /// <param name="max">最大日期时间</param>
    /// <returns>如果小于最大值，则返回 true；否则返回 false</returns>
    public static bool IsDateTimeMax(string value, DateTime max)
    {
        if (value.IsEmpty())
            return false;
        if (DateTime.TryParse(value, out var dateTime))
        {
            if (DateTime.Compare(max, dateTime) >= 0)
                return true;
        }
        return false;
    }

    #endregion

    #region IsLengthStr(字符串长度是否在指定范围内)

    /// <summary>
    /// 判断字符串长度是否在指定范围内（中文字符按2个字符计算）
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <param name="minLength">最小长度</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>如果长度在范围内，则返回 true；否则返回 false</returns>
    public static bool IsLengthStr(string value, int minLength, int maxLength)
    {
        var length = Regex.Replace(value, @"[^\x00-\xff]", "OK").Length;
        if (length <= minLength || length >= maxLength)
            return false;
        return true;
    }

    #endregion

    #region IsNormalChar(是否正常字符，字母、数字、下划线的组合)

    /// <summary>
    /// 判断字符串是否只包含字母、数字、下划线
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <returns>如果只包含正常字符，则返回 true；否则返回 false</returns>
    public static bool IsNormalChar(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"[\w\d_]+", RegexOptions.IgnoreCase);
    }

    #endregion

    #region IsPostfix(是否指定后缀)

    /// <summary>
    /// 判断字符串是否以指定后缀结尾
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <param name="postfixes">后缀数组</param>
    /// <returns>如果以指定后缀结尾，则返回 true；否则返回 false</returns>
    public static bool IsPostfix(string value, string[] postfixes)
    {
        if (value.IsEmpty())
            return false;
        var postfix = string.Join("|", postfixes);
        return value.IsMatch(string.Format(@".(?i:{0})$", postfix));
    }

    #endregion

    #region IsRepeat(是否重复)

    /// <summary>
    /// 判断字符串是否包含重复字符
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <returns>如果包含重复字符，则返回 true；否则返回 false</returns>
    public static bool IsRepeat(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.Distinct().Count() != value.Length;
    }

    #endregion

    #region IsQQ(是否合法QQ号码)

    /// <summary>
    /// 判断是否为有效的QQ号码格式
    /// </summary>
    /// <param name="value">QQ号码字符串</param>
    /// <returns>如果是有效QQ号码，则返回 true；否则返回 false</returns>
    // ReSharper disable once InconsistentNaming
    public static bool IsQQ(string value)
    {
        if (value.IsEmpty())
            return false;
        return value.IsMatch(@"^[1-9]\d{4,10}$");
    }

    #endregion

    #region IsColorValue(是否颜色值)

    /// <summary>
    /// 判断是否为有效的颜色值（3位或6位十六进制）
    /// </summary>
    /// <param name="value">颜色值字符串</param>
    /// <returns>如果是有效颜色值，则返回 true；否则返回 false</returns>
    public static bool IsColorValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        value = value.Trim().TrimStart('#');
        return (value.Length == 3 || value.Length == 6) && value.IsMatch(@"^[0-9a-fA-F]+$");
    }

    #endregion

    #region IsWideWord(是否全角字符)

    /// <summary>
    /// 判断字符串是否包含全角字符
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <returns>如果包含全角字符，则返回 true；否则返回 false</returns>
    public static bool IsWideWord(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return value.IsMatch(@"[^/x00-/xff]");
    }

    #endregion

    #region IsNarrowWord(是否半角字符)

    /// <summary>
    /// 判断字符串是否只包含半角字符
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <returns>如果只包含半角字符，则返回 true；否则返回 false</returns>
    public static bool IsNarrowWord(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return value.IsMatch(@"[/x00-/xff]");
    }

    #endregion

    #region IsOnlyNumber(是否数字)

    /// <summary>
    /// 判断字符串是否只包含数字字符
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <returns>如果只包含数字字符，则返回 true；否则返回 false</returns>
    public static bool IsOnlyNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return value.IsMatch(@"^\d+$");
    }

    #endregion

    #region IsUpperCaseChar(是否大写英文字母)

    /// <summary>
    /// 判断字符串是否只包含大写英文字母
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <returns>如果只包含大写字母，则返回 true；否则返回 false</returns>
    public static bool IsUpperCaseChar(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return value.IsMatch(@"^[A-Z]+$");
    }

    /// <summary>
    /// 判断字符是否为大写英文字母
    /// </summary>
    /// <param name="value">要检查的字符</param>
    /// <returns>如果是大写字母，则返回 true；否则返回 false</returns>
    public static bool IsUpperCaseChar(char value) => char.IsUpper(value) && char.IsLetter(value);

    #endregion

    #region IsLowerCaseChar(是否小写英文字母)

    /// <summary>
    /// 判断字符串是否只包含小写英文字母
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <returns>如果只包含小写字母，则返回 true；否则返回 false</returns>
    public static bool IsLowerCaseChar(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return value.IsMatch(@"^[a-z]+$");
    }

    /// <summary>
    /// 判断字符是否为小写英文字母
    /// </summary>
    /// <param name="value">要检查的字符</param>
    /// <returns>如果是小写字母，则返回 true；否则返回 false</returns>
    public static bool IsLowerCaseChar(char value) => char.IsLower(value) && char.IsLetter(value);

    #endregion
}