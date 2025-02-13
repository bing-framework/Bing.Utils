namespace Bing.Text;

/// <summary>
/// 脱敏帮助类
/// </summary>
public class DesensitizedHelper
{
    /// <summary>
    /// 脱敏类型
    /// </summary>
    public enum DesensitizedType
    {
        /// <summary>
        /// 中文名
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
        var newStr = value;
        switch (type)
        {
            case DesensitizedType.ChineseName:
                newStr = ChineseName(value);
                break;
            case DesensitizedType.IdCard:
                newStr = IdCardNum(value, 1, 2);
                break;
            case DesensitizedType.FixedPhone:
                newStr = FixedPhone(value);
                break;
            case DesensitizedType.MobilePhone:
                newStr = MobilePhone(value);
                break;
            case DesensitizedType.Address:
                newStr = Address(value, 8);
                break;
            case DesensitizedType.Email:
                newStr = Email(value);
                break;
            case DesensitizedType.Password:
                newStr = Password(value);
                break;
            case DesensitizedType.CarLicense:
                newStr = CarLicense(value);
                break;
            case DesensitizedType.BankCard:
                newStr = BankCard(value);
                break;
            case DesensitizedType.IPv4:
                newStr = IPv4(value);
                break;
            case DesensitizedType.IPv6:
                newStr = IPv6(value);
                break;
            case DesensitizedType.FirstMask:
                newStr = FirstMask(value);
                break;
        }
        return newStr;
    }

    /// <summary>
    /// 只显示第一个字符。
    /// </summary>
    /// <param name="value">字符串</param>
    /// <returns>脱敏后的字符串</returns>
    /// <remarks>脱敏前：123456789；脱敏后：1********。</remarks>
    public static string FirstMask(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return Strings.Hide(value, 1, value.Length);
    }

    /// <summary>
    /// 【中文姓名】只显示第一个汉字，其他隐藏为2个星号，比如：李**
    /// </summary>
    /// <param name="fullName">姓名</param>
    /// <returns>脱敏后的姓名</returns>
    public static string ChineseName(string fullName) => FirstMask(fullName);

    /// <summary>
    /// 【身份证号】前1位，后2位
    /// </summary>
    /// <param name="idCardNum">身份证</param>
    /// <param name="front">保留：前面的front位数；从1开始</param>
    /// <param name="end">保留：后面的end位数；从1开始</param>
    /// <returns>脱敏后的身份证</returns>
    public static string IdCardNum(string idCardNum, int front, int end)
    {
        if (string.IsNullOrWhiteSpace(idCardNum))
            return string.Empty;
        // 需要截取的长度不能大于身份证号长度
        if ((front + end) > idCardNum.Length)
            return string.Empty;
        // 需要截取的不能小于0
        if (front < 0 || end < 0)
            return string.Empty;
        return Strings.Hide(idCardNum, front, idCardNum.Length - end);
    }

    /// <summary>
    /// 【固定电话】前4位，后2位
    /// </summary>
    /// <param name="num">固定电话</param>
    /// <returns>脱敏后的固定电话</returns>
    public static string FixedPhone(string num)
    {
        if (string.IsNullOrWhiteSpace(num))
            return string.Empty;
        return Strings.Hide(num, 4, num.Length - 2);
    }

    /// <summary>
    /// 【手机号码】前3位，后4位，其它隐藏，比如136****2210
    /// </summary>
    /// <param name="num">移动电话</param>
    /// <returns>脱敏后的移动电话</returns>
    public static string MobilePhone(string num)
    {
        if (string.IsNullOrWhiteSpace(num))
            return string.Empty;
        return Strings.Hide(num, 3, num.Length - 4);
    }

    /// <summary>
    /// 【地址】只显示到地区，不显示详细地址，比如：广东省广州市****
    /// </summary>
    /// <param name="address">家庭地址</param>
    /// <param name="sensitiveSize">敏感信息长度</param>
    /// <returns>脱敏后的家庭地址</returns>
    public static string Address(string address, int sensitiveSize)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;
        var length = address.Length;
        return Strings.Hide(address, length - sensitiveSize, length);
    }

    /// <summary>
    /// 【电子邮箱】邮箱前缀仅显示第一个字母，前缀其它隐藏，用星号代替，@及后面的地址显示，比如：j**@126.com
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <returns>脱敏后的邮箱</returns>
    public static string Email(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;
        var index = email.IndexOf('@');
        if (index <= 1)
            return email;
        return Strings.Hide(email, 1, index);
    }

    /// <summary>
    /// 【密码】密码的全部字符都用*代替，比如：******
    /// </summary>
    /// <param name="password">密码</param>
    /// <returns>脱敏后的密码</returns>
    public static string Password(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return string.Empty;
        return Strings.Repeat("*", password.Length);
    }

    /// <summary>
    /// 【中国车牌】车牌中间用*代替
    /// </summary>
    /// <param name="carLicense">完整的车牌号</param>
    /// <returns>脱敏后的车牌</returns>
    /// <remarks>
    /// <para>eg1: null      -》 ""</para>
    /// <para>eg2: ""        -》 ""</para>
    /// <para>eg3: 粤A40000  -》 粤A4***0</para>
    /// <para>eg4: 粤J12345D -》 粤J1***D</para>
    /// <para>eg5: 粤B123    -》 粤B123</para>
    /// </remarks>
    public static string CarLicense(string carLicense)
    {
        if (string.IsNullOrWhiteSpace(carLicense))
            return string.Empty;
        carLicense = carLicense.Length switch
        {
            // 普通车牌
            7 => Strings.Hide(carLicense, 3, 6),
            // 新能源车牌
            8 => Strings.Hide(carLicense, 3, 7),
            _ => carLicense
        };
        return carLicense;
    }

    /// <summary>
    /// 【银行卡】由于银行卡号长度不定，所以只展示前4位，后面的位数根据卡号决定展示1-4位
    /// </summary>
    /// <param name="bankCardNo">银行卡号</param>
    /// <returns>脱敏之后的银行卡号</returns>
    /// <remarks>
    /// 例如：
    /// <para>1. "1234 2222 3333 4444 6789 9"    ->   "1234 **** **** **** **** 9"</para>
    /// <para>2. "1234 2222 3333 4444 6789 91"   ->   "1234 **** **** **** **** 91"</para>
    /// <para>3. "1234 2222 3333 4444 678"       ->    "1234 **** **** **** 678"</para>
    /// <para>4. "1234 2222 3333 4444 6789"      ->    "1234 **** **** **** 6789"</para>
    /// </remarks>
    public static string BankCard(string bankCardNo)
    {
        if (string.IsNullOrWhiteSpace(bankCardNo))
            return string.Empty;
        bankCardNo = Strings.CleanBlank(bankCardNo);
        if (bankCardNo.Length < 9)
            return bankCardNo;
        var length = bankCardNo.Length;
        var endLength = length % 4 == 0 ? 4 : length % 4;
        var midLength = length - 4 - endLength;

        var sb = new StringBuilder();
        sb.Append(bankCardNo[..4]);
        for (var i = 0; i < midLength; ++i)
        {
            if (i % 4 == 0)
                sb.Append(' ');
            sb.Append('*');
        }
        sb.Append(' ').Append(bankCardNo[^endLength..]); // 添加最后的数字
        return sb.ToString();
    }

    /// <summary>
    /// 【IPv4】
    /// </summary>
    /// <remarks>
    /// 例如：<br />
    /// 脱敏前：192.0.2.1；脱敏后：192.*.*.*
    /// </remarks>
    /// <param name="ipv4">IPv4地址</param>
    /// <returns>脱敏后的地址</returns>
    // ReSharper disable once InconsistentNaming
    public static string IPv4(string ipv4) => Strings.SubstringBefore(ipv4, '.', false) + ".*.*.*";

    /// <summary>
    /// 【IPv6】
    /// </summary>
    /// <remarks>
    /// 例如：<br />
    /// 脱敏前：2001:0db8:86a3:08d3:1319:8a2e:0370:7344；脱敏后：2001:*:*:*:*:*:*:*
    /// </remarks>
    /// <param name="ipv6">IPv6地址</param>
    /// <returns>脱敏后的地址</returns>
    // ReSharper disable once InconsistentNaming
    public static string IPv6(string ipv6) => Strings.SubstringBefore(ipv6, ':', false) + ":*:*:*:*:*:*:*";
}