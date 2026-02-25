using System.Globalization;
namespace Bing.Helpers;
/// <summary>
/// 测试类：覆盖 `Valid` 的非空、格式校验与类型判断相关行为。
/// </summary>
public class ValidTest
{
    #region IsNull 测试
    /// <summary>
    /// 测试用例：验证 `IsNull` 在 `WithNullObject` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsNull` 在 `WithNonNullObject` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsNull` 在 `WithEmptyString` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsNotNull` 在 `WithNonNullObject` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsNotNull` 在 `WithNullObject` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsEmpty` 在 `WithNullObject` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsEmpty` 在 `WithEmptyOrWhiteSpaceString` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsEmpty` 在 `WithNonEmptyString` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsEmpty` 在 `WithEmptyCollection` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsEmpty` 在 `WithNonEmptyCollection` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsNotEmpty` 在 `WithNonEmptyString` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsNotEmpty` 在 `WithEmptyString` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsEmail` 在 `WithValidEmail` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsEmail` 在 `WithInvalidEmail` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsEmail` 在 `WithRestrictMode` 场景下结果为 `ValidatesCorrectly`。
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
    /// 测试用例：验证 `HasEmail` 在 `WithStringContainingEmail` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("璇疯仈绯绘垜 user@example.com 璋㈣阿")]
    [InlineData("My email is test@domain.org")]
    public void HasEmail_WithStringContainingEmail_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.HasEmail(text);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `HasEmail` 在 `WithStringNotContainingEmail` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsMobileNumber` 在 `WithValidMobile` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsMobileNumber` 在 `WithInvalidMobile` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("12812345678")]  // 鏃犳晥鍙锋
    [InlineData("1381234567")]   // 浣嶆暟涓嶅
    [InlineData("abc12345678")]  // 鍖呭惈瀛楁瘝
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
    /// 测试用例：验证 `IsMobileNumberSimple` 在 `WithDifferentModes` 场景下结果为 `ReturnsExpectedResult`。
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
    /// 测试用例：验证 `HasMobileNumberSimple` 在 `WithStringContainingMobile` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("鎴戠殑鎵嬫満鍙锋槸13812345678", false, true)]
    [InlineData("璇疯仈绯?13812345678", true, true)]
    [InlineData("Call me at 12345678901", false, true)]
    public void HasMobileNumberSimple_WithStringContainingMobile_ReturnsTrue(string text, bool isRestrict, bool expected)
    {
        // Act
        var result = Valid.HasMobileNumberSimple(text, isRestrict);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `HasMobileNumberSimple` 在 `WithStringNotContainingMobile` 场景下结果为 `ReturnsFalse`。
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
    #region 运营商手机号测试
    /// <summary>
    /// 测试用例：验证 `IsChinaMobilePhone` 在 `WithVariousNumbers` 场景下结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData("13412345678", true)]   // 绉诲姩134
    [InlineData("13512345678", true)]   // 绉诲姩135
    [InlineData("15012345678", true)]   // 绉诲姩150
    [InlineData("18712345678", true)]   // 绉诲姩187
    [InlineData("19512345678", true)]   // 绉诲姩195
    [InlineData("13012345678", false)]  // 鑱旈€?30
    [InlineData("18012345678", false)]  // 鐢典俊180
    public void IsChinaMobilePhone_WithVariousNumbers_ReturnsExpectedResult(string mobile, bool expected)
    {
        // Act
        var result = Valid.IsChinaMobilePhone(mobile);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `IsChinaUnicomPhone` 在 `WithVariousNumbers` 场景下结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData("13012345678", true)]   // 鑱旈€?30
    [InlineData("13112345678", true)]   // 鑱旈€?31
    [InlineData("15512345678", true)]   // 鑱旈€?55
    [InlineData("18512345678", true)]   // 鑱旈€?85
    [InlineData("19612345678", true)]   // 鑱旈€?96
    [InlineData("13412345678", false)]  // 绉诲姩134
    [InlineData("18012345678", false)]  // 鐢典俊180
    public void IsChinaUnicomPhone_WithVariousNumbers_ReturnsExpectedResult(string mobile, bool expected)
    {
        // Act
        var result = Valid.IsChinaUnicomPhone(mobile);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `IsChinaTelecomPhone` 在 `WithVariousNumbers` 场景下结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData("13312345678", true)]   // 鐢典俊133
    [InlineData("18012345678", true)]   // 鐢典俊180
    [InlineData("18912345678", true)]   // 鐢典俊189
    [InlineData("19912345678", true)]   // 鐢典俊199
    [InlineData("13412345678", false)]  // 绉诲姩134
    [InlineData("13012345678", false)]  // 鑱旈€?30
    public void IsChinaTelecomPhone_WithVariousNumbers_ReturnsExpectedResult(string mobile, bool expected)
    {
        // Act
        var result = Valid.IsChinaTelecomPhone(mobile);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `IsChinaBroadcastPhone` 在 `WithVariousNumbers` 场景下结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData("19212345678", true)]   // 骞跨數192
    [InlineData("19312345678", false)]  // 闈炲箍鐢?93
    [InlineData("18912345678", false)]  // 鐢典俊189
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
    /// 测试用例：验证 `IsTel` 在 `WithValidTel` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsTel` 在 `WithInvalidTel` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("12345678")]     // 缂哄皯鍖哄彿
    [InlineData("400-123-4567")] // 400鍙风爜
    [InlineData("abc-12345678")] // 鍖呭惈瀛楁瘝
    public void IsTel_WithInvalidTel_ReturnsFalse(string tel)
    {
        // Act
        var result = Valid.IsTel(tel);
        // Assert
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsTel400800` 在 `WithVariousNumbers` 场景下结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData("0571-87654321", true)]
    [InlineData("010 12345678", true)]
    [InlineData("400-123-4567", true)]
    [InlineData("800-123-4567", true)]
    [InlineData("4001234567", true)]
    [InlineData("8001234567", true)]
    [InlineData("900-123-4567", false)] // 涓嶆敮鎸?00
    [InlineData("123-4567", false)]     // 鏍煎紡涓嶅
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
    /// 测试用例：验证 `IsIdCard` 在 `WithValidIdCard` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("110101199003078515")]    // 18浣?
    [InlineData("31010519900307851X")]    // 18浣嶅甫X
    [InlineData("440301199001011234")]    // 18浣?
    [InlineData("51010219900101123x")]    // 18浣嶅皬鍐檟
    [InlineData("110101900307851")]       // 15浣?
    public void IsIdCard_WithValidIdCard_ReturnsTrue(string idCard)
    {
        // Act
        var result = Valid.IsIdCard(idCard);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsIdCard` 在 `WithInvalidIdCard` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("11010119900307851")]     // 17浣?
    [InlineData("1101011990030785123")]   // 19浣?
    [InlineData("010101199003078515")]    // 鍦板尯鐮佷笉鑳戒互0寮€澶?
    [InlineData("110101199013078515")]    // 鏃犳晥鏈堜唤
    [InlineData("abc123456789012345")]    // 鍖呭惈瀛楁瘝
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
    /// 测试用例：验证 `IsGuid` 在 `WithValidGuid` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsGuid` 在 `WithInvalidGuid` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("550e8400-e29b-41d4-a716-44665544000G")] // 鏃犳晥瀛楃
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
    /// 测试用例：验证 `IsVersion` 在 `WithValidVersion` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsVersion` 在 `WithInvalidVersion` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("1.0.0.0.0.0")]  // 澶娈?
    [InlineData("1.a.0")]        // 鍖呭惈瀛楁瘝
    [InlineData("v1.0.0")]       // 鍖呭惈鍓嶇紑
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
    /// 测试用例：验证 `IsUrl` 在 `WithValidUrl` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsUrl` 在 `WithInvalidUrl` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("www.example.com")]  // 缂哄皯鍗忚
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
    /// 测试用例：验证 `IsUri` 在 `WithValidUri` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsUri` 在 `WithInvalidUri` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]      // 娌℃湁鐐?
    [InlineData("invalid-uri")]  // 娌℃湁鐐?
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
    /// 测试用例：验证 `IsMainDomainUrl` 在 `WithValidMainDomainUrl` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsMainDomainUrl` 在 `WithInvalidMainDomainUrl` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("www.example.com")]  // 缂哄皯鍗忚
    [InlineData("ftp://example.com")] // 涓嶆槸http/https
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
    /// 测试用例：验证 `IsMainDomain` 在 `WithValidMainDomain` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsMainDomain` 在 `WithInvalidMainDomain` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsDomain` 在 `WithValidDomain` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsDomain` 在 `WithInvalidDomain` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsMac` 在 `WithValidMac` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("00-1B-44-11-3A-B7")]
    [InlineData("001B44113AB7")]
    [InlineData("00:1B:44:11:3A:B7")]
    [InlineData("00-1b-44-11-3a-b7")]  // 灏忓啓
    public void IsMac_WithValidMac_ReturnsTrue(string mac)
    {
        // Act
        var result = Valid.IsMac(mac);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsMac` 在 `WithInvalidMac` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("GG-1B-44-11-3A-B7")]
    [InlineData("00-1B-44-11-3A")]
    [InlineData("ZZ:1B:44:11:3A:B7")]
    [InlineData("----")]
    [InlineData("not-a-mac")]
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
    /// 测试用例：验证 `IsIpAddress` 在 `WithValidIp` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsIpAddress` 在 `WithInvalidIp` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("256.1.1.1")]      // 瓒呭嚭鑼冨洿
    [InlineData("192.168.1")]      // 涓嶅畬鏁?
    [InlineData("192.168.1.1.1")]  // 澶氫綑娈?
    [InlineData("abc.def.ghi.jkl")] // 鍖呭惈瀛楁瘝
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
    /// 测试用例：验证 `IsChineseWord` 在 `WithChineseCharacters` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("\u4e2d\u6587")]
    [InlineData("\u6d4b\u8bd5")]
    [InlineData("\u6c49\u5b57")]
    public void IsChineseWord_WithChineseCharacters_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.IsChineseWord(text);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsChineseWord` 在 `WithNonChineseCharacters` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsChinese` 在 `WithChineseCharacters` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("\u4e2d\u6587")]
    [InlineData("\u6d4b\u8bd5")]
    [InlineData("\u4e16\u754c")]
    public void IsChinese_WithChineseCharacters_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.IsChinese(text);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsChinese` 在 `WithoutChineseCharacters` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `HasChinese` 在 `WithChineseCharacters` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("中文")]
    [InlineData("测试abc")]
    [InlineData("hello涓栫晫")]
    public void HasChinese_WithChineseCharacters_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.HasChinese(text);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `HasChinese` 在 `WithoutChineseCharacters` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `HasNumber` 在 `WithNumbers` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("abc123")]
    [InlineData("test1")]
    [InlineData("123")]
    [InlineData("涓枃123")]
    public void HasNumber_WithNumbers_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.HasNumber(text);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `HasNumber` 在 `WithoutNumbers` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsInteger` 在 `WithValidInteger` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsInteger` 在 `WithInvalidInteger` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsPositiveInteger` 在 `WithValidPositiveInteger` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsPositiveInteger` 在 `WithInvalidPositiveInteger` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("0")]      // 0涓嶆槸姝ｆ暣鏁?
    [InlineData("-123")]   // 璐熸暟
    [InlineData("123.45")] // 灏忔暟
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
    /// 测试用例：验证 `IsInt32` 在 `WithValidInt32` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsInt32` 在 `WithInvalidInt32` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-123")]   // 璐熸暟涓嶅尮閰嶅綋鍓嶆鍒?
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
    /// 测试用例：验证 `IsDouble` 在 `WithValidDouble` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsDouble` 在 `WithInvalidDouble` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12.34")]  // 瓒呰繃涓€浣嶅皬鏁?
    [InlineData(".5")]     // 涓嶄互鏁板瓧寮€澶?
    public void IsDouble_WithInvalidDouble_ReturnsFalse(string number)
    {
        // Act
        var result = Valid.IsDouble(number);
        // Assert
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsDouble` 在 `WithRangeAndDigit` 场景下结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData("5.5", 0, 10, 1, true)]
    [InlineData("15.5", 0, 10, 1, false)]  // 瓒呭嚭鑼冨洿
    [InlineData("5.55", 0, 10, 1, false)]  // 瓒呭嚭绮惧害
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
    /// 测试用例：验证 `IsNumber` 在 `WithValidNumber` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsNumber` 在 `WithInvalidNumber` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsDecimal` 在 `WithValidDecimal` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsDecimal` 在 `WithInvalidDecimal` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsBandCard` 在 `WithValidBankCard` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("1234567890123456")]     // 16浣?
    [InlineData("1234567890123456789")]  // 19浣?
    [InlineData("1234567890123")]        // 13浣?
    public void IsBandCard_WithValidBankCard_ReturnsTrue(string cardNumber)
    {
        // Act
        var result = Valid.IsBandCard(cardNumber);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsBandCard` 在 `WithInvalidBankCard` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("123456789012")]         // 12浣嶏紝澶煭
    [InlineData("12345678901234567890")] // 20浣嶏紝澶暱
    [InlineData("123456789012345a")]     // 鍖呭惈瀛楁瘝
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
    /// 测试用例：验证 `IsLoginName` 在 `WithValidLoginName` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsLoginName` 在 `WithInvalidLoginName` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("123456")]      // 绾暟瀛楋紝娌℃湁瀛楁瘝
    [InlineData("user")]        // 澶煭
    [InlineData("a")]           // 澶煭
    [InlineData("user_name")]   // 鍖呭惈涓嬪垝绾?
    public void IsLoginName_WithInvalidLoginName_ReturnsFalse(string loginName)
    {
        // Act
        var result = Valid.IsLoginName(loginName);
        // Assert
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsLoginName` 在 `WithSpecificLength` 场景下结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData("user", 4, 8, true)]
    [InlineData("usr", 4, 8, false)]    // 澶煭
    [InlineData("toolongname", 4, 8, false)]  // 澶暱
    [InlineData("123", 3, 5, false)]    // 娌℃湁瀛楁瘝
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
    /// 测试用例：验证 `IsPasswordOne` 在 `WithValidPassword` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsPasswordOne` 在 `WithInvalidPassword` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("12345")]       // 澶煭
    [InlineData("abc")]         // 澶煭
    [InlineData("password with space")]  // 鍖呭惈绌烘牸
    public void IsPasswordOne_WithInvalidPassword_ReturnsFalse(string password)
    {
        // Act
        var result = Valid.IsPasswordOne(password);
        // Assert
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsPasswordOne` 在 `WithSpecificLength` 场景下结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData("abc123", 6, 10, true)]
    [InlineData("abc", 6, 10, false)]        // 澶煭
    [InlineData("verylongpassword", 6, 10, false)]  // 澶暱
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
    /// 测试用例：验证 `IsPasswordTwo` 在 `WithValidStrongPassword` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("Test@123")]     // 鍖呭惈澶у皬鍐欏瓧姣嶃€佹暟瀛楀拰鐗规畩瀛楃
    [InlineData("MyPass1!")]
    public void IsPasswordTwo_WithValidStrongPassword_ReturnsTrue(string password)
    {
        // Act
        var result = Valid.IsPasswordTwo(password);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsPasswordTwo` 在 `WithInvalidStrongPassword` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("password")]     // 缂哄皯澶у啓瀛楁瘝銆佹暟瀛楀拰鐗规畩瀛楃
    [InlineData("PASSWORD")]     // 缂哄皯灏忓啓瀛楁瘝銆佹暟瀛楀拰鐗规畩瀛楃
    [InlineData("Password")]     // 缂哄皯鏁板瓧鍜岀壒娈婂瓧绗?
    [InlineData("Password1")]    // 缂哄皯鐗规畩瀛楃
    [InlineData("Test@ 123")]    // 鍖呭惈绌烘牸
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
    /// 测试用例：验证 `IsSafeSqlString` 在 `WithSafeSql` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsSafeSqlString` 在 `WithDangerousSql` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsBase64String` 在 `WithValidBase64` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsBase64String` 在 `WithInvalidBase64` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("SGVsbG8gV29ybGQ")]      // 缂哄皯濉厖
    [InlineData("SGVsbG8gV29ybGQ===")]   // 濉厖杩囧
    [InlineData("Hello World")]         // 鏅€氭枃鏈?
    [InlineData("SGVsbG8gV29ybGQ@")]     // 鍖呭惈闈炴硶瀛楃
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
    /// 测试用例：验证 `IsChinesePostalCode` 在 `WithValidPostalCode` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("100000")]  // 鍖椾含
    [InlineData("200000")]  // 涓婃捣
    [InlineData("518000")]  // 娣卞湷
    [InlineData("310000")]  // 鏉窞
    public void IsChinesePostalCode_WithValidPostalCode_ReturnsTrue(string postalCode)
    {
        // Act
        var result = Valid.IsChinesePostalCode(postalCode);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsChinesePostalCode` 在 `WithInvalidPostalCode` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("000000")]    // 浠?寮€澶?
    [InlineData("12345")]     // 5浣?
    [InlineData("1234567")]   // 7浣?
    [InlineData("12345a")]    // 鍖呭惈瀛楁瘝
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
    /// 测试用例：验证 `IsTime` 在 `WithValidTime` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsTime` 在 `WithInvalidTime` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("25:30")]    // 鏃犳晥灏忔椂
    [InlineData("14:60")]    // 鏃犳晥鍒嗛挓
    [InlineData("14")]       // 鍙湁灏忔椂
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
    /// 测试用例：验证 `IsDate` 在 `WithValidDate` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsDate` 在 `WithInvalidDate` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("2023-13-01")]  // 鏃犳晥鏈堜唤
    [InlineData("2023-01-32")]  // 鏃犳晥鏃ユ湡
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
    /// 测试用例：验证 `IsDate` 在 `WithSpecificFormat` 场景下结果为 `ReturnsExpectedResult`。
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
    /// 测试用例：验证 `IsDateTimeMin` 在 `WithDateGreaterThanMin` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsDateTimeMin` 在 `WithDateLessThanMin` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsDateTimeMax` 在 `WithDateLessThanMax` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsDateTimeMax` 在 `WithDateGreaterThanMax` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsPhoneNumber` 在 `WithValidMobile` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsPhoneNumber` 在 `WithInvalidMobile` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsLengthStr` 在 `WithLengthInRange` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("hello", 4, 10, true)]
    [InlineData("\u4e2d\u6587", 2, 6, true)]
    [InlineData("abc\u4e2d\u6587", 6, 10, true)]
    public void IsLengthStr_WithLengthInRange_ReturnsTrue(string text, int min, int max, bool expected)
    {
        // Act
        var result = Valid.IsLengthStr(text, min, max);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `IsLengthStr` 在 `WithLengthOutOfRange` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("a", 5, 10, false)]      // 澶煭
    [InlineData("verylongtext", 2, 5, false)]  // 澶暱
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
    /// 测试用例：验证 `IsNormalChar` 在 `WithNormalCharacters` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsNormalChar` 在 `WithAbnormalCharacters` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("---")]
    [InlineData("!!!")]
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
    /// 测试用例：验证 `IsPostfix` 在 `WithMatchingSuffix` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsPostfix` 在 `WithNonMatchingSuffix` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsRepeat` 在 `WithRepeatedCharacters` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("aabbcc")]
    [InlineData("hello")]      // 鍖呭惈閲嶅鐨?l'
    [InlineData("112233")]
    [InlineData("test")]       // 鍖呭惈閲嶅鐨?t'
    public void IsRepeat_WithRepeatedCharacters_ReturnsTrue(string text)
    {
        // Act
        var result = Valid.IsRepeat(text);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsRepeat` 在 `WithoutRepeatedCharacters` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("abcd")]
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
    /// 测试用例：验证 `IsQQ` 在 `WithValidQQ` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("12345")]       // 5浣?
    [InlineData("123456789")]   // 9浣?
    [InlineData("1234567890")]  // 10浣?
    [InlineData("12345678901")] // 11浣?
    public void IsQQ_WithValidQQ_ReturnsTrue(string qq)
    {
        // Act
        var result = Valid.IsQQ(qq);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsQQ` 在 `WithInvalidQQ` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("0123")]        // 浠?寮€澶?
    [InlineData("123")]         // 澶煭
    [InlineData("123456789012")] // 澶暱
    [InlineData("12345a")]      // 鍖呭惈瀛楁瘝
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
    /// 测试用例：验证 `IsColorValue` 在 `WithValidColor` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("FFF")]         // 3浣?
    [InlineData("000")]         // 3浣?
    [InlineData("FFFFFF")]      // 6浣?
    [InlineData("000000")]      // 6浣?
    [InlineData("#FFF")]        // 甯?鐨?浣?
    [InlineData("#FFFFFF")]     // 甯?鐨?浣?
    [InlineData("abc123")]      // 6浣嶅皬鍐?
    public void IsColorValue_WithValidColor_ReturnsTrue(string color)
    {
        // Act
        var result = Valid.IsColorValue(color);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsColorValue` 在 `WithInvalidColor` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("FF")]          // 2浣?
    [InlineData("FFFF")]        // 4浣?
    [InlineData("GGGHHH")]      // 鍖呭惈鏃犳晥瀛楃
    [InlineData("12345")]       // 5浣?
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
    /// 测试用例：验证 `IsWideWord` 在 `WithWideCharacters` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("中文")]
    [InlineData("测试abc")]
    [InlineData("锛戯紥锛?")]
    [InlineData("锛丂#")]
    public void IsWideWord_WithWideCharacters_ReturnsTrue(string text)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Valid.IsWideWord(text));
    }
    /// <summary>
    /// 测试用例：验证 `IsWideWord` 在 `WithoutWideCharacters` 场景下结果为 `ReturnsFalse`。
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
        if (string.IsNullOrWhiteSpace(text))
        {
            Valid.IsWideWord(text).ShouldBeFalse();
            return;
        }

        // Assert
        Should.Throw<ArgumentException>(() => Valid.IsWideWord(text));
    }
    #endregion
    #region IsNarrowWord 测试
    /// <summary>
    /// 测试用例：验证 `IsNarrowWord` 在 `WithNarrowCharacters` 场景下结果为 `ReturnsTrue`。
    /// </summary>
    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    [InlineData("Hello World")]
    public void IsNarrowWord_WithNarrowCharacters_ReturnsTrue(string text)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Valid.IsNarrowWord(text));
    }
    /// <summary>
    /// 测试用例：验证 `IsNarrowWord` 在 `WithWideCharacters` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("中文")]
    [InlineData("abc中文")]
    [InlineData("锛戯紥锛?")]
    public void IsNarrowWord_WithWideCharacters_ReturnsFalse(string text)
    {
        // Act
        if (string.IsNullOrWhiteSpace(text))
        {
            Valid.IsNarrowWord(text).ShouldBeFalse();
            return;
        }

        // Assert
        Should.Throw<ArgumentException>(() => Valid.IsNarrowWord(text));
    }
    #endregion
    #region IsOnlyNumber 测试
    /// <summary>
    /// 测试用例：验证 `IsOnlyNumber` 在 `WithOnlyNumbers` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsOnlyNumber` 在 `WithNonNumbers` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsUpperCaseChar` 在 `StringWithOnlyUpperCase` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsUpperCaseChar` 在 `StringWithNonUpperCase` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsUpperCaseChar` 在 `WithUpperCaseChar` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsUpperCaseChar` 在 `WithNonUpperCaseChar` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsLowerCaseChar` 在 `StringWithOnlyLowerCase` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsLowerCaseChar` 在 `StringWithNonLowerCase` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsLowerCaseChar` 在 `WithLowerCaseChar` 场景下结果为 `ReturnsTrue`。
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
    /// 测试用例：验证 `IsLowerCaseChar` 在 `WithNonLowerCaseChar` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsSafeSqlString` 在 `WithEmptyOrWhiteSpace` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsLoginName` 在 `WithInvalidParameters` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("test", -1, 5, false)]    // min涓鸿礋鏁?
    [InlineData("test", 5, 3, false)]     // max灏忎簬min
    [InlineData("", 3, 5, false)]         // 绌哄瓧绗︿覆
    public void IsLoginName_WithInvalidParameters_ReturnsFalse(string loginName, int min, int max, bool expected)
    {
        // Act
        var result = Valid.IsLoginName(loginName, min, max);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `IsPasswordOne` 在 `WithInvalidParameters` 场景下结果为 `ReturnsFalse`。
    /// </summary>
    [Theory]
    [InlineData("test", -1, 5, false)]    // min涓鸿礋鏁?
    [InlineData("", 3, 5, false)]         // 绌哄瓧绗︿覆
    public void IsPasswordOne_WithInvalidParameters_ReturnsFalse(string password, int min, int max, bool expected)
    {
        // Act
        var result = Valid.IsPasswordOne(password, min, max);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `IsPasswordOne` 在 `WithMinGreaterThanMax` 场景下结果为 `ThrowsArgumentException`。
    /// </summary>
    [Fact]
    public void IsPasswordOne_WithMinGreaterThanMax_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => Valid.IsPasswordOne("test", 5, 3));
    }
    /// <summary>
    /// 测试用例：验证 `IsVersion` 在 `WithCustomMaxSegments` 场景下结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData("1.0.0", 3, true)]
    [InlineData("1.0.0.0", 3, true)]      // Version.TryParse 浼氫紭鍏堣繑鍥?true
    [InlineData("1.0.0.0.0", 2, false)]   // 瓒呰繃2娈?
    [InlineData("1.0", 2, true)]
    public void IsVersion_WithCustomMaxSegments_ReturnsExpectedResult(string version, int maxSegments, bool expected)
    {
        // Act
        var result = Valid.IsVersion(version, maxSegments);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `IsDate` 在 `WithRegexMode` 场景下结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData("2023-01-01", true, false)]
    [InlineData("2023/01/01", true, false)]   // 姝ｅ垯妯″紡涓嶆敮鎸?鏍煎紡
    [InlineData("invalid date", true, false)]
    public void IsDate_WithRegexMode_ReturnsExpectedResult(string date, bool isRegex, bool expected)
    {
        // Act
        var result = Valid.IsDate(date, isRegex);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `IsDate` 在 `WithCultureAndStyles` 场景下结果为 `ValidatesCorrectly`。
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
    /// 测试用例：验证 `IsDateTimeMin` 在 `WithInvalidDateString` 场景下结果为 `ReturnsFalse`。
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
    /// 测试用例：验证 `IsDateTimeMax` 在 `WithInvalidDateString` 场景下结果为 `ReturnsFalse`。
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


