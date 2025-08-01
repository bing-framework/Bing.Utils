using System.Globalization;

namespace Bing.Helpers;

/// <summary>
/// 验证操作 单元测试
/// </summary>
public class ValidTest
{
    #region IsNull 测试

    /// <summary>
    /// 测试 - IsNull - 对象为null时返回true
    /// </summary>
    [Fact]
    public void IsNull_WithNullObject_ReturnsTrue()
    {
        // Arrange
        object value = null;

        // Act
        var result = Valid.IsNull(value);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsNull - 对象不为null时返回false
    /// </summary>
    [Fact]
    public void IsNull_WithNonNullObject_ReturnsFalse()
    {
        // Arrange
        var value = new object();

        // Act
        var result = Valid.IsNull(value);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - IsNull - 空字符串不为null
    /// </summary>
    [Fact]
    public void IsNull_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var value = "";

        // Act
        var result = Valid.IsNull(value);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsNotNull 测试

    /// <summary>
    /// 测试 - IsNotNull - 对象不为null时返回true
    /// </summary>
    [Fact]
    public void IsNotNull_WithNonNullObject_ReturnsTrue()
    {
        // Arrange
        var value = new object();

        // Act
        var result = Valid.IsNotNull(value);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsNotNull - 对象为null时返回false
    /// </summary>
    [Fact]
    public void IsNotNull_WithNullObject_ReturnsFalse()
    {
        // Arrange
        object value = null;

        // Act
        var result = Valid.IsNotNull(value);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsEmpty 测试

    /// <summary>
    /// 测试 - IsEmpty - null对象返回true
    /// </summary>
    [Fact]
    public void IsEmpty_WithNullObject_ReturnsTrue()
    {
        // Arrange
        object value = null;

        // Act
        var result = Valid.IsEmpty(value);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsEmpty - 空字符串返回true
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void IsEmpty_WithEmptyOrWhiteSpaceString_ReturnsTrue(string value)
    {
        // Act
        var result = Valid.IsEmpty(value);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsEmpty - 非空字符串返回false
    /// </summary>
    [Fact]
    public void IsEmpty_WithNonEmptyString_ReturnsFalse()
    {
        // Arrange
        var value = "Hello World";

        // Act
        var result = Valid.IsEmpty(value);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - IsEmpty - 空集合返回true
    /// </summary>
    [Fact]
    public void IsEmpty_WithEmptyCollection_ReturnsTrue()
    {
        // Arrange
        var value = new List<object>();

        // Act
        var result = Valid.IsEmpty(value);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsEmpty - 非空集合返回false
    /// </summary>
    [Fact]
    public void IsEmpty_WithNonEmptyCollection_ReturnsFalse()
    {
        // Arrange
        var value = new List<object> { new object() };

        // Act
        var result = Valid.IsEmpty(value);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsNotEmpty 测试

    /// <summary>
    /// 测试 - IsNotEmpty - 非空字符串返回true
    /// </summary>
    [Fact]
    public void IsNotEmpty_WithNonEmptyString_ReturnsTrue()
    {
        // Arrange
        var value = "Hello World";

        // Act
        var result = Valid.IsNotEmpty(value);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsNotEmpty - 空字符串返回false
    /// </summary>
    [Fact]
    public void IsNotEmpty_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var value = "";

        // Act
        var result = Valid.IsNotEmpty(value);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsEmail 测试

    /// <summary>
    /// 测试 - IsEmail - 有效邮箱地址返回true
    /// </summary>
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.emailtag@domain.co.uk")]
    [InlineData("simple@example.org")]
    [InlineData("user_name@test-domain.com")]
    public void IsEmail_WithValidEmail_ReturnsTrue(string email)
    {
        // Act
        var result = Valid.IsEmail(email);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsEmail - 无效邮箱地址返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid-email")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user@domain")]
    public void IsEmail_WithInvalidEmail_ReturnsFalse(string email)
    {
        // Act
        var result = Valid.IsEmail(email);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - IsEmail - 严格模式验证
    /// </summary>
    [Fact]
    public void IsEmail_WithRestrictMode_ValidatesCorrectly()
    {
        // Act & Assert
        Valid.IsEmail("user@example.com", true).ShouldBeTrue();
        Valid.IsEmail("user@example.com", false).ShouldBeTrue();
    }

    #endregion

    #region HasEmail 测试

    /// <summary>
    /// 测试 - HasEmail - 包含邮箱的字符串返回true
    /// </summary>
    [Theory]
    [InlineData("请联系我 user@example.com 谢谢")]
    [InlineData("My email is test@domain.org")]
    public void HasEmail_WithStringContainingEmail_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.HasEmail(text);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - HasEmail - 不包含邮箱的字符串返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("This is just a text")]
    [InlineData("No email here at all")]
    public void HasEmail_WithStringNotContainingEmail_ReturnsFalse(string text)
    {
        // Act
        var result = Valid.HasEmail(text);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsMobileNumber 测试

    /// <summary>
    /// 测试 - IsMobileNumber - 有效手机号码返回true
    /// </summary>
    [Theory]
    [InlineData("13812345678")]
    [InlineData("15912345678")]
    [InlineData("18612345678")]
    public void IsMobileNumber_WithValidMobile_ReturnsTrue(string mobile)
    {
        // Act
        var result = Valid.IsMobileNumber(mobile);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsMobileNumber - 无效手机号码返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("12812345678")]  // 无效号段
    [InlineData("1381234567")]   // 位数不够
    [InlineData("abc12345678")]  // 包含字母
    public void IsMobileNumber_WithInvalidMobile_ReturnsFalse(string mobile)
    {
        // Act
        var result = Valid.IsMobileNumber(mobile);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsMobileNumberSimple 测试

    /// <summary>
    /// 测试 - IsMobileNumberSimple - 简单验证模式
    /// </summary>
    [Theory]
    [InlineData("13812345678", false, true)]
    [InlineData("12345678901", false, true)]
    [InlineData("13812345678", true, true)]
    [InlineData("12345678901", true, false)]
    public void IsMobileNumberSimple_WithDifferentModes_ReturnsExpectedResult(string mobile, bool isRestrict, bool expected)
    {
        // Act
        var result = Valid.IsMobileNumberSimple(mobile, isRestrict);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region HasMobileNumberSimple 测试

    /// <summary>
    /// 测试 - HasMobileNumberSimple - 包含手机号的字符串返回true
    /// </summary>
    [Theory]
    [InlineData("我的手机号是13812345678", false, true)]
    [InlineData("请联系 13812345678", true, true)]
    [InlineData("Call me at 12345678901", false, true)]
    public void HasMobileNumberSimple_WithStringContainingMobile_ReturnsTrue(string text, bool isRestrict, bool expected)
    {
        // Act
        var result = Valid.HasMobileNumberSimple(text, isRestrict);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - HasMobileNumberSimple - 不包含手机号的字符串返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("This is just text")]
    [InlineData("No mobile number here")]
    public void HasMobileNumberSimple_WithStringNotContainingMobile_ReturnsFalse(string text)
    {
        // Act
        var result = Valid.HasMobileNumberSimple(text);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region 运营商手机号码测试

    /// <summary>
    /// 测试 - IsChinaMobilePhone - 中国移动号码验证
    /// </summary>
    [Theory]
    [InlineData("13412345678", true)]   // 移动134
    [InlineData("13512345678", true)]   // 移动135
    [InlineData("15012345678", true)]   // 移动150
    [InlineData("18712345678", true)]   // 移动187
    [InlineData("19512345678", true)]   // 移动195
    [InlineData("13012345678", false)]  // 联通130
    [InlineData("18012345678", false)]  // 电信180
    public void IsChinaMobilePhone_WithVariousNumbers_ReturnsExpectedResult(string mobile, bool expected)
    {
        // Act
        var result = Valid.IsChinaMobilePhone(mobile);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsChinaUnicomPhone - 中国联通号码验证
    /// </summary>
    [Theory]
    [InlineData("13012345678", true)]   // 联通130
    [InlineData("13112345678", true)]   // 联通131
    [InlineData("15512345678", true)]   // 联通155
    [InlineData("18512345678", true)]   // 联通185
    [InlineData("19612345678", true)]   // 联通196
    [InlineData("13412345678", false)]  // 移动134
    [InlineData("18012345678", false)]  // 电信180
    public void IsChinaUnicomPhone_WithVariousNumbers_ReturnsExpectedResult(string mobile, bool expected)
    {
        // Act
        var result = Valid.IsChinaUnicomPhone(mobile);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsChinaTelecomPhone - 中国电信号码验证
    /// </summary>
    [Theory]
    [InlineData("13312345678", true)]   // 电信133
    [InlineData("18012345678", true)]   // 电信180
    [InlineData("18912345678", true)]   // 电信189
    [InlineData("19912345678", true)]   // 电信199
    [InlineData("13412345678", false)]  // 移动134
    [InlineData("13012345678", false)]  // 联通130
    public void IsChinaTelecomPhone_WithVariousNumbers_ReturnsExpectedResult(string mobile, bool expected)
    {
        // Act
        var result = Valid.IsChinaTelecomPhone(mobile);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsChinaBroadcastPhone - 中国广电号码验证
    /// </summary>
    [Theory]
    [InlineData("19212345678", true)]   // 广电192
    [InlineData("19312345678", false)]  // 非广电193
    [InlineData("18912345678", false)]  // 电信189
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsChinaBroadcastPhone_WithVariousNumbers_ReturnsExpectedResult(string mobile, bool expected)
    {
        // Act
        var result = Valid.IsChinaBroadcastPhone(mobile);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region IsTel 测试

    /// <summary>
    /// 测试 - IsTel - 有效固定电话返回true
    /// </summary>
    [Theory]
    [InlineData("010-12345678")]
    [InlineData("021-87654321")]
    [InlineData("0571-87654321")]
    [InlineData("01012345678")]
    [InlineData("02187654321")]
    public void IsTel_WithValidTel_ReturnsTrue(string tel)
    {
        // Act
        var result = Valid.IsTel(tel);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsTel - 无效固定电话返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("12345678")]     // 缺少区号
    [InlineData("400-123-4567")] // 400号码
    [InlineData("abc-12345678")] // 包含字母
    public void IsTel_WithInvalidTel_ReturnsFalse(string tel)
    {
        // Act
        var result = Valid.IsTel(tel);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - IsTel400800 - 座机400800电话验证
    /// </summary>
    [Theory]
    [InlineData("0571-87654321", true)]
    [InlineData("010 12345678", true)]
    [InlineData("400-123-4567", true)]
    [InlineData("800-123-4567", true)]
    [InlineData("4001234567", true)]
    [InlineData("8001234567", true)]
    [InlineData("900-123-4567", false)] // 不支持900
    [InlineData("123-4567", false)]     // 格式不对
    [InlineData("", false)]
    public void IsTel400800_WithVariousNumbers_ReturnsExpectedResult(string tel, bool expected)
    {
        // Act
        var result = Valid.IsTel400800(tel);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region IsIdCard 测试

    /// <summary>
    /// 测试 - IsIdCard - 有效身份证号码返回true
    /// </summary>
    [Theory]
    [InlineData("110101199003078515")]    // 18位
    [InlineData("31010519900307851X")]    // 18位带X
    [InlineData("440301199001011234")]    // 18位
    [InlineData("51010219900101123x")]    // 18位小写x
    [InlineData("110101900307851")]       // 15位
    public void IsIdCard_WithValidIdCard_ReturnsTrue(string idCard)
    {
        // Act
        var result = Valid.IsIdCard(idCard);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsIdCard - 无效身份证号码返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("11010119900307851")]     // 17位
    [InlineData("1101011990030785123")]   // 19位
    [InlineData("010101199003078515")]    // 地区码不能以0开头
    [InlineData("110101199013078515")]    // 无效月份
    [InlineData("abc123456789012345")]    // 包含字母
    public void IsIdCard_WithInvalidIdCard_ReturnsFalse(string idCard)
    {
        // Act
        var result = Valid.IsIdCard(idCard);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsGuid 测试

    /// <summary>
    /// 测试 - IsGuid - 有效Guid返回true
    /// </summary>
    [Theory]
    [InlineData("550e8400-e29b-41d4-a716-446655440000")]
    [InlineData("6ba7b810-9dad-11d1-80b4-00c04fd430c8")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("{550e8400-e29b-41d4-a716-446655440000}")]
    public void IsGuid_WithValidGuid_ReturnsTrue(string guid)
    {
        // Act
        var result = Valid.IsGuid(guid);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsGuid - 无效Guid返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("550e8400-e29b-41d4-a716-44665544000G")] // 无效字符
    [InlineData("invalid-guid-string")]
    public void IsGuid_WithInvalidGuid_ReturnsFalse(string guid)
    {
        // Act
        var result = Valid.IsGuid(guid);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsVersion 测试

    /// <summary>
    /// 测试 - IsVersion - 有效版本号返回true
    /// </summary>
    [Theory]
    [InlineData("1.0")]
    [InlineData("1.0.0")]
    [InlineData("1.0.0.0")]
    [InlineData("2.1.3")]
    [InlineData("10.20.30.40")]
    public void IsVersion_WithValidVersion_ReturnsTrue(string version)
    {
        // Act
        var result = Valid.IsVersion(version);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsVersion - 无效版本号返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("1.0.0.0.0.0")]  // 太多段
    [InlineData("1.a.0")]        // 包含字母
    [InlineData("v1.0.0")]       // 包含前缀
    public void IsVersion_WithInvalidVersion_ReturnsFalse(string version)
    {
        // Act
        var result = Valid.IsVersion(version);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsUrl 测试

    /// <summary>
    /// 测试 - IsUrl - 有效URL返回true
    /// </summary>
    [Theory]
    [InlineData("http://www.example.com")]
    [InlineData("https://example.com/path")]
    [InlineData("http://localhost:8080")]
    [InlineData("https://sub.domain.com/path?param=value")]
    public void IsUrl_WithValidUrl_ReturnsTrue(string url)
    {
        // Act
        var result = Valid.IsUrl(url);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsUrl - 无效URL返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("www.example.com")]  // 缺少协议
    [InlineData("example.com")]
    [InlineData("invalid-url")]
    public void IsUrl_WithInvalidUrl_ReturnsFalse(string url)
    {
        // Act
        var result = Valid.IsUrl(url);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsUri 测试

    /// <summary>
    /// 测试 - IsUri - 有效URI返回true
    /// </summary>
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://www.example.com")]
    [InlineData("ftp://files.example.com")]
    [InlineData("mailto:user@example.com")]
    [InlineData("example.com")]
    public void IsUri_WithValidUri_ReturnsTrue(string uri)
    {
        // Act
        var result = Valid.IsUri(uri);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsUri - 无效URI返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]      // 没有点
    [InlineData("invalid-uri")]  // 没有点
    public void IsUri_WithInvalidUri_ReturnsFalse(string uri)
    {
        // Act
        var result = Valid.IsUri(uri);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsMainDomainUrl 测试

    /// <summary>
    /// 测试 - IsMainDomainUrl - 有效主域名URL返回true
    /// </summary>
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://www.example.com")]
    [InlineData("http://test.org")]
    [InlineData("https://domain.net")]
    public void IsMainDomainUrl_WithValidMainDomainUrl_ReturnsTrue(string url)
    {
        // Act
        var result = Valid.IsMainDomainUrl(url);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsMainDomainUrl - 无效主域名URL返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("www.example.com")]  // 缺少协议
    [InlineData("ftp://example.com")] // 不是http/https
    public void IsMainDomainUrl_WithInvalidMainDomainUrl_ReturnsFalse(string url)
    {
        // Act
        var result = Valid.IsMainDomainUrl(url);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsMainDomain 测试

    /// <summary>
    /// 测试 - IsMainDomain - 有效主域名返回true
    /// </summary>
    [Theory]
    [InlineData("example.com")]
    [InlineData("www.example.com")]
    [InlineData("test.org")]
    [InlineData("domain.net:8080")]
    public void IsMainDomain_WithValidMainDomain_ReturnsTrue(string domain)
    {
        // Act
        var result = Valid.IsMainDomain(domain);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsMainDomain - 无效主域名返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("localhost")]
    [InlineData("192.168.1.1")]
    public void IsMainDomain_WithInvalidMainDomain_ReturnsFalse(string domain)
    {
        // Act
        var result = Valid.IsMainDomain(domain);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsDomain 测试

    /// <summary>
    /// 测试 - IsDomain - 有效域名返回true
    /// </summary>
    [Theory]
    [InlineData("example.com")]
    [InlineData("sub.example.com")]
    [InlineData("test.org")]
    [InlineData("domain.net:8080")]
    public void IsDomain_WithValidDomain_ReturnsTrue(string domain)
    {
        // Act
        var result = Valid.IsDomain(domain);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsDomain - 无效域名返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("localhost")]
    [InlineData("192.168.1.1")]
    public void IsDomain_WithInvalidDomain_ReturnsFalse(string domain)
    {
        // Act
        var result = Valid.IsDomain(domain);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsMac 测试

    /// <summary>
    /// 测试 - IsMac - 有效MAC地址返回true
    /// </summary>
    [Theory]
    [InlineData("00-1B-44-11-3A-B7")]
    [InlineData("001B44113AB7")]
    [InlineData("00:1B:44:11:3A:B7")]
    [InlineData("00-1b-44-11-3a-b7")]  // 小写
    public void IsMac_WithValidMac_ReturnsTrue(string mac)
    {
        // Act
        var result = Valid.IsMac(mac);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsMac - 无效MAC地址返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("GG-1B-44-11-3A-B7")]  // 无效字符
    [InlineData("00-1B-44-11-3A")]     // 不完整
    [InlineData("00:1B:44:11:3A:B7:C8")] // 太长
    public void IsMac_WithInvalidMac_ReturnsFalse(string mac)
    {
        // Act
        var result = Valid.IsMac(mac);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsIpAddress 测试

    /// <summary>
    /// 测试 - IsIpAddress - 有效IP地址返回true
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("0.0.0.0")]
    [InlineData("127.0.0.1")]
    public void IsIpAddress_WithValidIp_ReturnsTrue(string ip)
    {
        // Act
        var result = Valid.IsIpAddress(ip);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsIpAddress - 无效IP地址返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("256.1.1.1")]      // 超出范围
    [InlineData("192.168.1")]      // 不完整
    [InlineData("192.168.1.1.1")]  // 多余段
    [InlineData("abc.def.ghi.jkl")] // 包含字母
    public void IsIpAddress_WithInvalidIp_ReturnsFalse(string ip)
    {
        // Act
        var result = Valid.IsIpAddress(ip);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsChineseWord 测试

    /// <summary>
    /// 测试 - IsChineseWord - 纯中文字符返回true
    /// </summary>
    [Theory]
    [InlineData("中文")]
    [InlineData("测试")]
    [InlineData("汉字")]
    [InlineData("中")]
    public void IsChineseWord_WithChineseCharacters_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.IsChineseWord(text);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsChineseWord - 非纯中文字符返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("中文abc")]
    [InlineData("123")]
    public void IsChineseWord_WithNonChineseCharacters_ReturnsFalse(string text)
    {
        // Act
        var result = Valid.IsChineseWord(text);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsChinese 测试

    /// <summary>
    /// 测试 - IsChinese - 包含中文字符返回true
    /// </summary>
    [Theory]
    [InlineData("中文")]
    [InlineData("测试")]
    [InlineData("世界")]
    [InlineData("中")]
    public void IsChinese_WithChineseCharacters_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.IsChinese(text);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsChinese - 不包含中文字符返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("hello")]
    public void IsChinese_WithoutChineseCharacters_ReturnsFalse(string text)
    {
        // Act
        var result = Valid.IsChinese(text);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region HasChinese 测试

    /// <summary>
    /// 测试 - HasChinese - 包含中文字符返回true
    /// </summary>
    [Theory]
    [InlineData("中文")]
    [InlineData("测试abc")]
    [InlineData("hello世界")]
    public void HasChinese_WithChineseCharacters_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.HasChinese(text);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - HasChinese - 不包含中文字符返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("123")]
    public void HasChinese_WithoutChineseCharacters_ReturnsFalse(string text)
    {
        // Act
        var result = Valid.HasChinese(text);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region HasNumber 测试

    /// <summary>
    /// 测试 - HasNumber - 包含数字返回true
    /// </summary>
    [Theory]
    [InlineData("abc123")]
    [InlineData("test1")]
    [InlineData("123")]
    [InlineData("中文123")]
    public void HasNumber_WithNumbers_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.HasNumber(text);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - HasNumber - 不包含数字返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("test")]
    [InlineData("中文")]
    public void HasNumber_WithoutNumbers_ReturnsFalse(string text)
    {
        // Act
        var result = Valid.HasNumber(text);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsInteger 测试

    /// <summary>
    /// 测试 - IsInteger - 有效整数返回true
    /// </summary>
    [Theory]
    [InlineData("123")]
    [InlineData("-123")]
    [InlineData("0")]
    [InlineData("999999")]
    public void IsInteger_WithValidInteger_ReturnsTrue(string number)
    {
        // Act
        var result = Valid.IsInteger(number);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsInteger - 无效整数返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("123.45")]
    [InlineData("abc")]
    [InlineData("12.3.4")]
    public void IsInteger_WithInvalidInteger_ReturnsFalse(string number)
    {
        // Act
        var result = Valid.IsInteger(number);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsPositiveInteger 测试

    /// <summary>
    /// 测试 - IsPositiveInteger - 有效正整数返回true
    /// </summary>
    [Theory]
    [InlineData("1")]
    [InlineData("123")]
    [InlineData("999")]
    public void IsPositiveInteger_WithValidPositiveInteger_ReturnsTrue(string number)
    {
        // Act
        var result = Valid.IsPositiveInteger(number);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsPositiveInteger - 无效正整数返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("0")]      // 0不是正整数
    [InlineData("-123")]   // 负数
    [InlineData("123.45")] // 小数
    [InlineData("abc")]
    public void IsPositiveInteger_WithInvalidPositiveInteger_ReturnsFalse(string number)
    {
        // Act
        var result = Valid.IsPositiveInteger(number);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsInt32 测试

    /// <summary>
    /// 测试 - IsInt32 - 有效Int32返回true
    /// </summary>
    [Theory]
    [InlineData("123")]
    [InlineData("0")]
    [InlineData("999")]
    public void IsInt32_WithValidInt32_ReturnsTrue(string number)
    {
        // Act
        var result = Valid.IsInt32(number);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsInt32 - 无效Int32返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-123")]   // 负数不匹配当前正则
    [InlineData("123.45")]
    public void IsInt32_WithInvalidInt32_ReturnsFalse(string number)
    {
        // Act
        var result = Valid.IsInt32(number);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsDouble 测试

    /// <summary>
    /// 测试 - IsDouble - 有效Double返回true
    /// </summary>
    [Theory]
    [InlineData("1")]
    [InlineData("1.0")]
    [InlineData("1.")]
    public void IsDouble_WithValidDouble_ReturnsTrue(string number)
    {
        // Act
        var result = Valid.IsDouble(number);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsDouble - 无效Double返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12.34")]  // 超过一位小数
    [InlineData(".5")]     // 不以数字开头
    public void IsDouble_WithInvalidDouble_ReturnsFalse(string number)
    {
        // Act
        var result = Valid.IsDouble(number);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - IsDouble - 带范围和精度验证
    /// </summary>
    [Theory]
    [InlineData("5.5", 0, 10, 1, true)]
    [InlineData("15.5", 0, 10, 1, false)]  // 超出范围
    [InlineData("5.55", 0, 10, 1, false)]  // 超出精度
    public void IsDouble_WithRangeAndDigit_ReturnsExpectedResult(string number, double min, double max, int digit, bool expected)
    {
        // Act
        var result = Valid.IsDouble(number, min, max, digit);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region IsNumber 测试

    /// <summary>
    /// 测试 - IsNumber - 有效数字返回true
    /// </summary>
    [Theory]
    [InlineData("123")]
    [InlineData("-123")]
    [InlineData("123.45")]
    [InlineData("-123.45")]
    [InlineData("0")]
    [InlineData("0.5")]
    public void IsNumber_WithValidNumber_ReturnsTrue(string number)
    {
        // Act
        var result = Valid.IsNumber(number);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsNumber - 无效数字返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12.34.56")]
    [InlineData("12abc")]
    public void IsNumber_WithInvalidNumber_ReturnsFalse(string number)
    {
        // Act
        var result = Valid.IsNumber(number);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsDecimal 测试

    /// <summary>
    /// 测试 - IsDecimal - 有效Decimal返回true
    /// </summary>
    [Theory]
    [InlineData("1")]
    [InlineData("1.0")]
    [InlineData("123.456")]
    public void IsDecimal_WithValidDecimal_ReturnsTrue(string number)
    {
        // Act
        var result = Valid.IsDecimal(number);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsDecimal - 无效Decimal返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    public void IsDecimal_WithInvalidDecimal_ReturnsFalse(string number)
    {
        // Act
        var result = Valid.IsDecimal(number);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsBandCard 测试

    /// <summary>
    /// 测试 - IsBandCard - 有效银行卡号返回true
    /// </summary>
    [Theory]
    [InlineData("1234567890123456")]     // 16位
    [InlineData("1234567890123456789")]  // 19位
    [InlineData("1234567890123")]        // 13位
    public void IsBandCard_WithValidBankCard_ReturnsTrue(string cardNumber)
    {
        // Act
        var result = Valid.IsBandCard(cardNumber);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsBandCard - 无效银行卡号返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("123456789012")]         // 12位，太短
    [InlineData("12345678901234567890")] // 20位，太长
    [InlineData("123456789012345a")]     // 包含字母
    public void IsBandCard_WithInvalidBankCard_ReturnsFalse(string cardNumber)
    {
        // Act
        var result = Valid.IsBandCard(cardNumber);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsLoginName 测试

    /// <summary>
    /// 测试 - IsLoginName - 有效登录名返回true
    /// </summary>
    [Theory]
    [InlineData("user123")]
    [InlineData("testUser")]
    [InlineData("abc123def")]
    public void IsLoginName_WithValidLoginName_ReturnsTrue(string loginName)
    {
        // Act
        var result = Valid.IsLoginName(loginName);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsLoginName - 无效登录名返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("123456")]      // 纯数字，没有字母
    [InlineData("user")]        // 太短
    [InlineData("a")]           // 太短
    [InlineData("user_name")]   // 包含下划线
    public void IsLoginName_WithInvalidLoginName_ReturnsFalse(string loginName)
    {
        // Act
        var result = Valid.IsLoginName(loginName);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - IsLoginName - 指定长度范围
    /// </summary>
    [Theory]
    [InlineData("user", 4, 8, true)]
    [InlineData("usr", 4, 8, false)]    // 太短
    [InlineData("toolongname", 4, 8, false)]  // 太长
    [InlineData("123", 3, 5, false)]    // 没有字母
    public void IsLoginName_WithSpecificLength_ReturnsExpectedResult(string loginName, int min, int max, bool expected)
    {
        // Act
        var result = Valid.IsLoginName(loginName, min, max);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region IsPasswordOne 测试

    /// <summary>
    /// 测试 - IsPasswordOne - 有效密码返回true
    /// </summary>
    [Theory]
    [InlineData("abc123")]
    [InlineData("Test@123")]
    [InlineData("password!")]
    public void IsPasswordOne_WithValidPassword_ReturnsTrue(string password)
    {
        // Act
        var result = Valid.IsPasswordOne(password);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsPasswordOne - 无效密码返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("12345")]       // 太短
    [InlineData("abc")]         // 太短
    [InlineData("password with space")]  // 包含空格
    public void IsPasswordOne_WithInvalidPassword_ReturnsFalse(string password)
    {
        // Act
        var result = Valid.IsPasswordOne(password);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - IsPasswordOne - 指定长度范围
    /// </summary>
    [Theory]
    [InlineData("abc123", 6, 10, true)]
    [InlineData("abc", 6, 10, false)]        // 太短
    [InlineData("verylongpassword", 6, 10, false)]  // 太长
    public void IsPasswordOne_WithSpecificLength_ReturnsExpectedResult(string password, int min, int max, bool expected)
    {
        // Act
        var result = Valid.IsPasswordOne(password, min, max);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region IsPasswordTwo 测试

    /// <summary>
    /// 测试 - IsPasswordTwo - 有效强密码返回true
    /// </summary>
    [Theory]
    [InlineData("Test@123")]     // 包含大小写字母、数字和特殊字符
    [InlineData("MyPass1!")]
    public void IsPasswordTwo_WithValidStrongPassword_ReturnsTrue(string password)
    {
        // Act
        var result = Valid.IsPasswordTwo(password);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsPasswordTwo - 无效强密码返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("password")]     // 缺少大写字母、数字和特殊字符
    [InlineData("PASSWORD")]     // 缺少小写字母、数字和特殊字符
    [InlineData("Password")]     // 缺少数字和特殊字符
    [InlineData("Password1")]    // 缺少特殊字符
    [InlineData("Test@ 123")]    // 包含空格
    public void IsPasswordTwo_WithInvalidStrongPassword_ReturnsFalse(string password)
    {
        // Act
        var result = Valid.IsPasswordTwo(password);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsSafeSqlString 测试

    /// <summary>
    /// 测试 - IsSafeSqlString - 安全SQL字符串返回true
    /// </summary>
    [Theory]
    [InlineData("normal text")]
    [InlineData("safe content")]
    public void IsSafeSqlString_WithSafeSql_ReturnsTrue(string sql)
    {
        // Act
        var result = Valid.IsSafeSqlString(sql);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsSafeSqlString - 危险SQL字符串返回false
    /// </summary>
    [Theory]
    [InlineData("SELECT * FROM users")]
    [InlineData("DROP TABLE users")]
    [InlineData("INSERT INTO users")]
    [InlineData("'; DROP TABLE users;--")]
    [InlineData("OR 1=1")]
    [InlineData("user'; exec master")]
    public void IsSafeSqlString_WithDangerousSql_ReturnsFalse(string sql)
    {
        // Act
        var result = Valid.IsSafeSqlString(sql);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsBase64String 测试

    /// <summary>
    /// 测试 - IsBase64String - 有效Base64字符串返回true
    /// </summary>
    [Theory]
    [InlineData("SGVsbG8gV29ybGQ=")]     // "Hello World"
    [InlineData("YWJjZA==")]            // "abcd"
    [InlineData("MTIzNA==")]            // "1234"
    public void IsBase64String_WithValidBase64_ReturnsTrue(string base64)
    {
        // Act
        var result = Valid.IsBase64String(base64);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsBase64String - 无效Base64字符串返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("SGVsbG8gV29ybGQ")]      // 缺少填充
    [InlineData("SGVsbG8gV29ybGQ===")]   // 填充过多
    [InlineData("Hello World")]         // 普通文本
    [InlineData("SGVsbG8gV29ybGQ@")]     // 包含非法字符
    public void IsBase64String_WithInvalidBase64_ReturnsFalse(string base64)
    {
        // Act
        var result = Valid.IsBase64String(base64);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsChinesePostalCode 测试

    /// <summary>
    /// 测试 - IsChinesePostalCode - 有效邮政编码返回true
    /// </summary>
    [Theory]
    [InlineData("100000")]  // 北京
    [InlineData("200000")]  // 上海
    [InlineData("518000")]  // 深圳
    [InlineData("310000")]  // 杭州
    public void IsChinesePostalCode_WithValidPostalCode_ReturnsTrue(string postalCode)
    {
        // Act
        var result = Valid.IsChinesePostalCode(postalCode);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsChinesePostalCode - 无效邮政编码返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("000000")]    // 以0开头
    [InlineData("12345")]     // 5位
    [InlineData("1234567")]   // 7位
    [InlineData("12345a")]    // 包含字母
    public void IsChinesePostalCode_WithInvalidPostalCode_ReturnsFalse(string postalCode)
    {
        // Act
        var result = Valid.IsChinesePostalCode(postalCode);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsTime 测试

    /// <summary>
    /// 测试 - IsTime - 有效时间格式返回true
    /// </summary>
    [Theory]
    [InlineData("14:30")]
    [InlineData("14:30:25")]
    [InlineData("9:05")]
    [InlineData("23:59")]
    [InlineData("0:00")]
    public void IsTime_WithValidTime_ReturnsTrue(string time)
    {
        // Act
        var result = Valid.IsTime(time);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsTime - 无效时间格式返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("25:30")]    // 无效小时
    [InlineData("14:60")]    // 无效分钟
    [InlineData("14")]       // 只有小时
    [InlineData("abc")]
    public void IsTime_WithInvalidTime_ReturnsFalse(string time)
    {
        // Act
        var result = Valid.IsTime(time);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsDate 测试

    /// <summary>
    /// 测试 - IsDate - 有效日期返回true
    /// </summary>
    [Theory]
    [InlineData("2023-01-01")]
    [InlineData("2023/01/01")]
    [InlineData("01/01/2023")]
    [InlineData("2023-12-31")]
    public void IsDate_WithValidDate_ReturnsTrue(string date)
    {
        // Act
        var result = Valid.IsDate(date);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsDate - 无效日期返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("2023-13-01")]  // 无效月份
    [InlineData("2023-01-32")]  // 无效日期
    [InlineData("abc")]
    [InlineData("not a date")]
    public void IsDate_WithInvalidDate_ReturnsFalse(string date)
    {
        // Act
        var result = Valid.IsDate(date);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - IsDate - 指定格式验证
    /// </summary>
    [Theory]
    [InlineData("2023-01-01", "yyyy-MM-dd", true)]
    [InlineData("01/01/2023", "MM/dd/yyyy", true)]
    [InlineData("2023-01-01", "MM/dd/yyyy", false)]
    public void IsDate_WithSpecificFormat_ReturnsExpectedResult(string date, string format, bool expected)
    {
        // Act
        var result = Valid.IsDate(date, format);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region IsDateTimeMin 测试

    /// <summary>
    /// 测试 - IsDateTimeMin - 大于最小时间返回true
    /// </summary>
    [Fact]
    public void IsDateTimeMin_WithDateGreaterThanMin_ReturnsTrue()
    {
        // Arrange
        var minDate = new DateTime(2023, 1, 1);
        var testDate = "2023-06-01";

        // Act
        var result = Valid.IsDateTimeMin(testDate, minDate);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsDateTimeMin - 小于最小时间返回false
    /// </summary>
    [Fact]
    public void IsDateTimeMin_WithDateLessThanMin_ReturnsFalse()
    {
        // Arrange
        var minDate = new DateTime(2023, 6, 1);
        var testDate = "2023-01-01";

        // Act
        var result = Valid.IsDateTimeMin(testDate, minDate);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsDateTimeMax 测试

    /// <summary>
    /// 测试 - IsDateTimeMax - 小于最大时间返回true
    /// </summary>
    [Fact]
    public void IsDateTimeMax_WithDateLessThanMax_ReturnsTrue()
    {
        // Arrange
        var maxDate = new DateTime(2023, 12, 31);
        var testDate = "2023-06-01";

        // Act
        var result = Valid.IsDateTimeMax(testDate, maxDate);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsDateTimeMax - 大于最大时间返回false
    /// </summary>
    [Fact]
    public void IsDateTimeMax_WithDateGreaterThanMax_ReturnsFalse()
    {
        // Arrange
        var maxDate = new DateTime(2023, 6, 1);
        var testDate = "2023-12-31";

        // Act
        var result = Valid.IsDateTimeMax(testDate, maxDate);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsPhoneNumber 测试

    /// <summary>
    /// 测试 - IsPhoneNumber - 有效手机号码返回true（过时方法）
    /// </summary>
    [Theory]
    [InlineData("13812345678")]
    [InlineData("15912345678")]
    [InlineData("18612345678")]
    [InlineData("8613812345678")]
    [InlineData("013812345678")]
    public void IsPhoneNumber_WithValidMobile_ReturnsTrue(string mobile)
    {
        // Act
        var result = Valid.IsPhoneNumber(mobile);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsPhoneNumber - 无效手机号码返回false（过时方法）
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("12812345678")]
    [InlineData("abc12345678")]
    public void IsPhoneNumber_WithInvalidMobile_ReturnsFalse(string mobile)
    {
        // Act
        var result = Valid.IsPhoneNumber(mobile);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsLengthStr 测试

    /// <summary>
    /// 测试 - IsLengthStr - 长度在范围内返回true
    /// </summary>
    [Theory]
    [InlineData("hello", 4, 10, true)]
    [InlineData("中文", 2, 6, true)]   // 中文字符按2个字符计算
    [InlineData("abc中文", 6, 10, true)]
    public void IsLengthStr_WithLengthInRange_ReturnsTrue(string text, int min, int max, bool expected)
    {
        // Act
        var result = Valid.IsLengthStr(text, min, max);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsLengthStr - 长度超出范围返回false
    /// </summary>
    [Theory]
    [InlineData("a", 5, 10, false)]      // 太短
    [InlineData("verylongtext", 2, 5, false)]  // 太长
    public void IsLengthStr_WithLengthOutOfRange_ReturnsFalse(string text, int min, int max, bool expected)
    {
        // Act
        var result = Valid.IsLengthStr(text, min, max);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region IsNormalChar 测试

    /// <summary>
    /// 测试 - IsNormalChar - 正常字符返回true
    /// </summary>
    [Theory]
    [InlineData("abc123")]
    [InlineData("test_user")]
    [InlineData("ABC_123")]
    [InlineData("user123")]
    public void IsNormalChar_WithNormalCharacters_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.IsNormalChar(text);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsNormalChar - 非正常字符返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc-123")]     // 包含连字符
    [InlineData("user@domain")] // 包含@符号
    [InlineData("test space")]  // 包含空格
    [InlineData("中文")]        // 包含中文
    public void IsNormalChar_WithAbnormalCharacters_ReturnsFalse(string text)
    {
        // Act
        var result = Valid.IsNormalChar(text);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsPostfix 测试

    /// <summary>
    /// 测试 - IsPostfix - 匹配指定后缀返回true
    /// </summary>
    [Theory]
    [InlineData("file.txt", new string[] { "txt", "doc" }, true)]
    [InlineData("image.jpg", new string[] { "jpg", "png" }, true)]
    [InlineData("document.pdf", new string[] { "doc", "pdf" }, true)]
    public void IsPostfix_WithMatchingSuffix_ReturnsTrue(string filename, string[] suffixes, bool expected)
    {
        // Act
        var result = Valid.IsPostfix(filename, suffixes);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsPostfix - 不匹配指定后缀返回false
    /// </summary>
    [Theory]
    [InlineData("", new string[] { "txt" }, false)]
    [InlineData("file.exe", new string[] { "txt", "doc" }, false)]
    [InlineData("image.bmp", new string[] { "jpg", "png" }, false)]
    public void IsPostfix_WithNonMatchingSuffix_ReturnsFalse(string filename, string[] suffixes, bool expected)
    {
        // Act
        var result = Valid.IsPostfix(filename, suffixes);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region IsRepeat 测试

    /// <summary>
    /// 测试 - IsRepeat - 包含重复字符返回true
    /// </summary>
    [Theory]
    [InlineData("aabbcc")]
    [InlineData("hello")]      // 包含重复的'l'
    [InlineData("112233")]
    [InlineData("test")]       // 包含重复的't'
    public void IsRepeat_WithRepeatedCharacters_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.IsRepeat(text);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsRepeat - 不包含重复字符返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("unique")]
    public void IsRepeat_WithoutRepeatedCharacters_ReturnsFalse(string text)
    {
        // Act
        var result = Valid.IsRepeat(text);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsQQ 测试

    /// <summary>
    /// 测试 - IsQQ - 有效QQ号码返回true
    /// </summary>
    [Theory]
    [InlineData("12345")]       // 5位
    [InlineData("123456789")]   // 9位
    [InlineData("1234567890")]  // 10位
    [InlineData("12345678901")] // 11位
    public void IsQQ_WithValidQQ_ReturnsTrue(string qq)
    {
        // Act
        var result = Valid.IsQQ(qq);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsQQ - 无效QQ号码返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("0123")]        // 以0开头
    [InlineData("123")]         // 太短
    [InlineData("123456789012")] // 太长
    [InlineData("12345a")]      // 包含字母
    public void IsQQ_WithInvalidQQ_ReturnsFalse(string qq)
    {
        // Act
        var result = Valid.IsQQ(qq);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsColorValue 测试

    /// <summary>
    /// 测试 - IsColorValue - 有效颜色值返回true
    /// </summary>
    [Theory]
    [InlineData("FFF")]         // 3位
    [InlineData("000")]         // 3位
    [InlineData("FFFFFF")]      // 6位
    [InlineData("000000")]      // 6位
    [InlineData("#FFF")]        // 带#的3位
    [InlineData("#FFFFFF")]     // 带#的6位
    [InlineData("abc123")]      // 6位小写
    public void IsColorValue_WithValidColor_ReturnsTrue(string color)
    {
        // Act
        var result = Valid.IsColorValue(color);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsColorValue - 无效颜色值返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("FF")]          // 2位
    [InlineData("FFFF")]        // 4位
    [InlineData("GGGHHH")]      // 包含无效字符
    [InlineData("12345")]       // 5位
    public void IsColorValue_WithInvalidColor_ReturnsFalse(string color)
    {
        // Act
        var result = Valid.IsColorValue(color);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsWideWord 测试

    /// <summary>
    /// 测试 - IsWideWord - 包含全角字符返回true
    /// </summary>
    [Theory]
    [InlineData("中文")]
    [InlineData("测试abc")]
    [InlineData("１２３")]      // 全角数字
    [InlineData("！@#")]       // 全角符号
    public void IsWideWord_WithWideCharacters_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.IsWideWord(text);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsWideWord - 不包含全角字符返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    public void IsWideWord_WithoutWideCharacters_ReturnsFalse(string text)
    {
        // Act
        var result = Valid.IsWideWord(text);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsNarrowWord 测试

    /// <summary>
    /// 测试 - IsNarrowWord - 只包含半角字符返回true
    /// </summary>
    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    [InlineData("Hello World")]
    public void IsNarrowWord_WithNarrowCharacters_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.IsNarrowWord(text);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsNarrowWord - 包含全角字符返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("中文")]
    [InlineData("abc中文")]
    [InlineData("１２３")]
    public void IsNarrowWord_WithWideCharacters_ReturnsFalse(string text)
    {
        // Act
        var result = Valid.IsNarrowWord(text);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsOnlyNumber 测试

    /// <summary>
    /// 测试 - IsOnlyNumber - 只包含数字返回true
    /// </summary>
    [Theory]
    [InlineData("123")]
    [InlineData("0")]
    [InlineData("999999")]
    [InlineData("000")]
    public void IsOnlyNumber_WithOnlyNumbers_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.IsOnlyNumber(text);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsOnlyNumber - 包含非数字字符返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123abc")]
    [InlineData("12.34")]
    [InlineData("-123")]
    [InlineData("1 2 3")]
    public void IsOnlyNumber_WithNonNumbers_ReturnsFalse(string text)
    {
        // Act
        var result = Valid.IsOnlyNumber(text);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsUpperCaseChar 测试

    /// <summary>
    /// 测试 - IsUpperCaseChar - 字符串只包含大写字母返回true
    /// </summary>
    [Theory]
    [InlineData("ABC")]
    [InlineData("HELLO")]
    [InlineData("XYZ")]
    [InlineData("A")]
    public void IsUpperCaseChar_StringWithOnlyUpperCase_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.IsUpperCaseChar(text);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsUpperCaseChar - 字符串包含非大写字母返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("Abc")]
    [InlineData("ABC123")]
    [InlineData("HELLO world")]
    public void IsUpperCaseChar_StringWithNonUpperCase_ReturnsFalse(string text)
    {
        // Act
        var result = Valid.IsUpperCaseChar(text);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - IsUpperCaseChar - 单个字符大写字母返回true
    /// </summary>
    [Theory]
    [InlineData('A', true)]
    [InlineData('Z', true)]
    [InlineData('B', true)]
    public void IsUpperCaseChar_WithUpperCaseChar_ReturnsTrue(char character, bool expected)
    {
        // Act
        var result = Valid.IsUpperCaseChar(character);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsUpperCaseChar - 单个字符非大写字母返回false
    /// </summary>
    [Theory]
    [InlineData('a', false)]
    [InlineData('1', false)]
    [InlineData('@', false)]
    [InlineData(' ', false)]
    public void IsUpperCaseChar_WithNonUpperCaseChar_ReturnsFalse(char character, bool expected)
    {
        // Act
        var result = Valid.IsUpperCaseChar(character);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region IsLowerCaseChar 测试

    /// <summary>
    /// 测试 - IsLowerCaseChar - 字符串只包含小写字母返回true
    /// </summary>
    [Theory]
    [InlineData("abc")]
    [InlineData("hello")]
    [InlineData("xyz")]
    [InlineData("a")]
    public void IsLowerCaseChar_StringWithOnlyLowerCase_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.IsLowerCaseChar(text);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsLowerCaseChar - 字符串包含非小写字母返回false
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABC")]
    [InlineData("Abc")]
    [InlineData("abc123")]
    [InlineData("hello WORLD")]
    public void IsLowerCaseChar_StringWithNonLowerCase_ReturnsFalse(string text)
    {
        // Act
        var result = Valid.IsLowerCaseChar(text);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - IsLowerCaseChar - 单个字符小写字母返回true
    /// </summary>
    [Theory]
    [InlineData('a', true)]
    [InlineData('z', true)]
    [InlineData('b', true)]
    public void IsLowerCaseChar_WithLowerCaseChar_ReturnsTrue(char character, bool expected)
    {
        // Act
        var result = Valid.IsLowerCaseChar(character);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsLowerCaseChar - 单个字符非小写字母返回false
    /// </summary>
    [Theory]
    [InlineData('A', false)]
    [InlineData('1', false)]
    [InlineData('@', false)]
    [InlineData(' ', false)]
    public void IsLowerCaseChar_WithNonLowerCaseChar_ReturnsFalse(char character, bool expected)
    {
        // Act
        var result = Valid.IsLowerCaseChar(character);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region SuppressMessage 警告抑制测试

    /// <summary>
    /// 测试 - IsSafeSqlString - 空字符串或null的特殊处理
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsSafeSqlString_WithEmptyOrWhiteSpace_ReturnsFalse(string sql)
    {
        // Act
        var result = Valid.IsSafeSqlString(sql);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region 边界测试和异常场景

    /// <summary>
    /// 测试 - IsLoginName - 参数边界验证
    /// </summary>
    [Theory]
    [InlineData("test", -1, 5, false)]    // min为负数
    [InlineData("test", 5, 3, false)]     // max小于min
    [InlineData("", 3, 5, false)]         // 空字符串
    public void IsLoginName_WithInvalidParameters_ReturnsFalse(string loginName, int min, int max, bool expected)
    {
        // Act
        var result = Valid.IsLoginName(loginName, min, max);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsPasswordOne - 参数边界验证
    /// </summary>
    [Theory]
    [InlineData("test", -1, 5, false)]    // min为负数
    [InlineData("test", 5, 3, false)]     // max小于min
    [InlineData("", 3, 5, false)]         // 空字符串
    public void IsPasswordOne_WithInvalidParameters_ReturnsFalse(string password, int min, int max, bool expected)
    {
        // Act
        var result = Valid.IsPasswordOne(password, min, max);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsVersion - 自定义最大段数
    /// </summary>
    [Theory]
    [InlineData("1.0.0", 3, true)]
    [InlineData("1.0.0.0", 3, false)]     // 超过3段
    [InlineData("1.0.0.0.0", 2, false)]   // 超过2段
    [InlineData("1.0", 2, true)]
    public void IsVersion_WithCustomMaxSegments_ReturnsExpectedResult(string version, int maxSegments, bool expected)
    {
        // Act
        var result = Valid.IsVersion(version, maxSegments);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsDate - 正则表达式模式验证
    /// </summary>
    [Theory]
    [InlineData("2023-01-01", true, true)]
    [InlineData("2023/01/01", true, false)]   // 正则模式不支持/格式
    [InlineData("invalid date", true, false)]
    public void IsDate_WithRegexMode_ReturnsExpectedResult(string date, bool isRegex, bool expected)
    {
        // Act
        var result = Valid.IsDate(date, isRegex);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsDate - 带文化信息和样式的验证
    /// </summary>
    [Fact]
    public void IsDate_WithCultureAndStyles_ValidatesCorrectly()
    {
        // Arrange
        var date = "01/15/2023";
        var format = "MM/dd/yyyy";
        var provider = CultureInfo.InvariantCulture;
        var styles = DateTimeStyles.None;

        // Act
        var result = Valid.IsDate(date, format, provider, styles);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsDateTimeMin - 无效日期字符串
    /// </summary>
    [Theory]
    [InlineData("", false)]
    [InlineData("invalid date", false)]
    [InlineData(null, false)]
    public void IsDateTimeMin_WithInvalidDateString_ReturnsFalse(string dateString, bool expected)
    {
        // Arrange
        var minDate = DateTime.Now;

        // Act
        var result = Valid.IsDateTimeMin(dateString, minDate);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsDateTimeMax - 无效日期字符串
    /// </summary>
    [Theory]
    [InlineData("", false)]
    [InlineData("invalid date", false)]
    [InlineData(null, false)]
    public void IsDateTimeMax_WithInvalidDateString_ReturnsFalse(string dateString, bool expected)
    {
        // Arrange
        var maxDate = DateTime.Now;

        // Act
        var result = Valid.IsDateTimeMax(dateString, maxDate);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion
}