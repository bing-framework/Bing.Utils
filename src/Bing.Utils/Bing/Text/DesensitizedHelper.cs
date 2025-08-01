namespace Bing.Text;

/// <summary>
/// 数据脱敏帮助类，提供多种常见数据类型的脱敏处理功能
/// </summary>
public class DesensitizedHelper
{
    /// <summary>
    /// 脱敏类型
    /// </summary>
    public enum DesensitizedType
    {
        /// <summary>
        /// 中文姓名
        /// </summary>
        ChineseName,

        /// <summary>
        /// 身份证
        /// </summary>
        IdCard,

        /// <summary>
        /// 座机号
        /// </summary>
        FixedPhone,

        /// <summary>
        /// 手机号
        /// </summary>
        MobilePhone,

        /// <summary>
        /// 地址
        /// </summary>
        Address,

        /// <summary>
        /// 电子邮件
        /// </summary>
        Email,

        /// <summary>
        /// 密码
        /// </summary>
        Password,

        /// <summary>
        /// 中国大陆车牌，包含普通车辆、新能源车辆
        /// </summary>
        CarLicense,

        /// <summary>
        /// 银行卡
        /// </summary>
        BankCard,

        /// <summary>
        /// IPv4地址
        /// </summary>
        // ReSharper disable once InconsistentNaming
        IPv4,

        /// <summary>
        /// IPv6地址
        /// </summary>
        // ReSharper disable once InconsistentNaming
        IPv6,

        /// <summary>
        /// 只显示第一个字符。
        /// </summary>
        FirstMask,
    }

    /// <summary>
    /// 脱敏
    /// </summary>
    /// <param name="value">字符串</param>
    /// <param name="type">脱敏类型</param>
    /// <returns>脱敏之后的字符串</returns>
    public static string Desensitized(string value, DesensitizedType type)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return type switch
        {
            DesensitizedType.ChineseName => ChineseName(value),
            DesensitizedType.IdCard => IdCardNum(value, 1, 2),
            DesensitizedType.FixedPhone => FixedPhone(value),
            DesensitizedType.MobilePhone => MobilePhone(value),
            DesensitizedType.Address => Address(value, 8),
            DesensitizedType.Email => Email(value),
            DesensitizedType.Password => Password(value),
            DesensitizedType.CarLicense => CarLicense(value),
            DesensitizedType.BankCard => BankCard(value),
            DesensitizedType.IPv4 => IPv4(value),
            DesensitizedType.IPv6 => IPv6(value),
            DesensitizedType.FirstMask => FirstMask(value),
            _ => value
        };
    }

    /// <summary>
    /// 仅显示第一个字符，其余字符用星号替换
    /// </summary>
    /// <param name="value">待脱敏的字符串</param>
    /// <returns>脱敏后的字符串，格式：第一个字符 + N个星号</returns>
    /// <example>
    /// 脱敏前：123456789；脱敏后：1********
    /// </example>
    public static string FirstMask(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return Strings.Hide(value, 1, value.Length);
    }

    /// <summary>
    /// 中文姓名脱敏，仅显示第一个汉字，其余用星号替换
    /// </summary>
    /// <param name="fullName">完整姓名</param>
    /// <returns>脱敏后的姓名，格式：姓 + **</returns>
    /// <example>
    /// 脱敏前：张三丰；脱敏后：张**
    /// </example>
    public static string ChineseName(string fullName) => FirstMask(fullName);

    /// <summary>
    /// 身份证号脱敏，保留前N位和后N位，中间用星号替换
    /// </summary>
    /// <param name="idCard">身份证号码</param>
    /// <param name="frontKeep">保留前面的位数，从1开始</param>
    /// <param name="endKeep">保留后面的位数，从1开始</param>
    /// <returns>脱敏后的身份证号</returns>
    /// <example>
    /// 脱敏前：51343620000320711X；脱敏后：5***************1X
    /// </example>
    public static string IdCardNum(string idCard, int frontKeep, int endKeep)
    {
        if (string.IsNullOrWhiteSpace(idCard))
            return string.Empty;
        // 参数验证
        if (frontKeep < 0 || endKeep < 0)
            return string.Empty;
        // 保留位数不能超过身份证号长度
        if ((frontKeep + endKeep) >= idCard.Length)
            return string.Empty;
        return Strings.Hide(idCard, frontKeep, idCard.Length - endKeep);
    }

    /// <summary>
    /// 固定电话脱敏，保留前4位和后2位，中间用星号替换
    /// </summary>
    /// <param name="phoneNumber">固定电话号码</param>
    /// <returns>脱敏后的固定电话</returns>
    /// <example>
    /// 脱敏前：09157518479；脱敏后：0915*****79
    /// </example>
    public static string FixedPhone(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;
        return Strings.Hide(phoneNumber, 4, phoneNumber.Length - 2);
    }

    /// <summary>
    /// 手机号码脱敏，保留前3位和后4位，中间用星号替换
    /// </summary>
    /// <param name="mobileNumber">手机号码</param>
    /// <returns>脱敏后的手机号码</returns>
    /// <example>
    /// 脱敏前：13610000000；脱敏后：136****0000
    /// </example>
    public static string MobilePhone(string mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber))
            return string.Empty;
        return Strings.Hide(mobileNumber, 3, mobileNumber.Length - 4);
    }

    /// <summary>
    /// 地址脱敏，仅显示前面部分，后面指定长度用星号替换
    /// </summary>
    /// <param name="address">完整地址</param>
    /// <param name="sensitiveLength">需要脱敏的长度</param>
    /// <returns>脱敏后的地址</returns>
    /// <example>
    /// 脱敏前：广东省广州市天河区猎德街道289号；脱敏后：广东省广州市天河区猎德街*****
    /// </example>
    public static string Address(string address, int sensitiveLength)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;
        if (sensitiveLength <= 0)
            return address;
        var hideStart = Math.Max(0, address.Length - sensitiveLength);
        return Strings.Hide(address, hideStart, address.Length);
    }

    /// <summary>
    /// 电子邮箱脱敏，保留第一个字符和@后的域名，中间用星号替换
    /// </summary>
    /// <param name="email">电子邮箱地址</param>
    /// <returns>脱敏后的邮箱地址</returns>
    /// <example>
    /// 脱敏前：wang@126.com；脱敏后：w***@126.com
    /// </example>
    public static string Email(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
            return email;
        return Strings.Hide(email, 1, atIndex);
    }

    /// <summary>
    /// 密码脱敏，所有字符用星号替换
    /// </summary>
    /// <param name="password">密码字符串</param>
    /// <returns>脱敏后的密码，全部显示为星号</returns>
    /// <example>
    /// 脱敏前：password123；脱敏后：***********
    /// </example>
    public static string Password(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return string.Empty;
        return Strings.Repeat("*", password.Length);
    }

    /// <summary>
    /// 中国车牌号脱敏，保留前3位和最后1位，中间用星号替换
    /// </summary>
    /// <param name="carLicense">完整车牌号</param>
    /// <returns>脱敏后的车牌号</returns>
    /// <example>
    /// <para>粤A40000 -> 粤A4***0</para>
    /// <para>粤J12345D -> 粤J1***D</para>
    /// <para>粤B123 -> 粤B123 (长度不足7位不脱敏)</para>
    /// </example>
    public static string CarLicense(string carLicense)
    {
        if (string.IsNullOrWhiteSpace(carLicense))
            return string.Empty;
        return carLicense.Length switch
        {
            7 => Strings.Hide(carLicense, 3, 6),  // 普通车牌：7位
            8 => Strings.Hide(carLicense, 3, 7),  // 新能源车牌：8位
            _ => carLicense  // 其他长度不处理
        };
    }

    /// <summary>
    /// 银行卡号脱敏，保留前4位和后N位（根据总长度确定），中间用星号替换
    /// </summary>
    /// <param name="bankCardNumber">银行卡号</param>
    /// <returns>脱敏后的银行卡号，保持原有格式</returns>
    /// <example>
    /// <para>1234 2222 3333 4444 6789 9 -> 1234 **** **** **** **** 9</para>
    /// <para>1234 2222 3333 4444 6789 91 -> 1234 **** **** **** **** 91</para>
    /// <para>1234 2222 3333 4444 678 -> 1234 **** **** **** 678</para>
    /// <para>1234 2222 3333 4444 6789 -> 1234 **** **** **** 6789</para>
    /// </example>
    public static string BankCard(string bankCardNumber)
    {
        if (string.IsNullOrWhiteSpace(bankCardNumber))
            return string.Empty;
        var cleanCard = Strings.CleanBlank(bankCardNumber);
        if (cleanCard.Length < 9)
            return bankCardNumber;

        var length = cleanCard.Length;
        var endLength = length % 4 == 0 ? 4 : length % 4;
        var midLength = length - 4 - endLength;

        var sb = new StringBuilder();

        // 前4位
        sb.Append(cleanCard[..4]);

        // 中间星号部分，每4位加一个空格
        for (var i = 0; i < midLength; i++)
        {
            if (i % 4 == 0)
                sb.Append(' ');
            sb.Append('*');
        }

        // 最后N位
        sb.Append(' ').Append(cleanCard[^endLength..]);
        return sb.ToString();
    }

    /// <summary>
    /// IPv4地址脱敏，仅保留第一段，其余段用星号替换
    /// </summary>
    /// <param name="ipv4">IPv4地址</param>
    /// <returns>脱敏后的IPv4地址</returns>
    /// <example>
    /// 脱敏前：192.0.2.1；脱敏后：192.*.*.*
    /// </example>
    // ReSharper disable once InconsistentNaming
    public static string IPv4(string ipv4) => Strings.SubstringBefore(ipv4, '.', false) + ".*.*.*";

    /// <summary>
    /// IPv6地址脱敏，仅保留第一段，其余段用星号替换
    /// </summary>
    /// <param name="ipv6">IPv6地址</param>
    /// <returns>脱敏后的IPv6地址</returns>
    /// <example>
    /// 脱敏前：2001:0db8:86a3:08d3:1319:8a2e:0370:7344；脱敏后：2001:*:*:*:*:*:*:*
    /// </example>
    // ReSharper disable once InconsistentNaming
    public static string IPv6(string ipv6) => Strings.SubstringBefore(ipv6, ':', false) + ":*:*:*:*:*:*:*";
}