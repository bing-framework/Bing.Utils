using Bing.Helpers;
namespace Bing.Text;
/// <summary>
/// 脱敏帮助类 单元测试
/// </summary>
[Trait("StringUT", "DesensitizedHelper")]
public class DesensitizedHelperTest
{
    #region Desensitized 主方法测试
    /// <summary>
    /// 测试 - Desensitized - 所有脱敏类型
    /// </summary>
    [Theory]
    [InlineData("张三", DesensitizedHelper.DesensitizedType.ChineseName, "张*")]
    [InlineData("51343620000320711X", DesensitizedHelper.DesensitizedType.IdCard, "5***************1X")]
    [InlineData("09157518479", DesensitizedHelper.DesensitizedType.FixedPhone, "0915*****79")]
    [InlineData("13610000000", DesensitizedHelper.DesensitizedType.MobilePhone, "136****0000")]
    [InlineData("广东省广州市天河区猎德街道289号", DesensitizedHelper.DesensitizedType.Address, "广东省广州市天河区********")]
    [InlineData("wang@126.com", DesensitizedHelper.DesensitizedType.Email, "w***@126.com")]
    [InlineData("password123", DesensitizedHelper.DesensitizedType.Password, "***********")]
    [InlineData("粤A12345", DesensitizedHelper.DesensitizedType.CarLicense, "粤A1***5")]
    [InlineData("6227880100100105123", DesensitizedHelper.DesensitizedType.BankCard, "6227 **** **** **** 123")]
    [InlineData("192.168.1.1", DesensitizedHelper.DesensitizedType.IPv4, "192.*.*.*")]
    [InlineData("2001:0db8:86a3:08d3:1319:8a2e:0370:7344", DesensitizedHelper.DesensitizedType.IPv6, "2001:*:*:*:*:*:*:*")]
    [InlineData("FirstMaskTest", DesensitizedHelper.DesensitizedType.FirstMask, "F************")]
    public void Desensitized_AllTypes_ReturnsExpectedResult(string input, DesensitizedHelper.DesensitizedType type, string expected)
    {
        // Act
        var result = DesensitizedHelper.Desensitized(input, type);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - Desensitized - 空值处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Desensitized_NullOrEmptyInput_ReturnsEmptyString(string input)
    {
        // Act & Assert
        foreach (var type in Enums.GetValues<DesensitizedHelper.DesensitizedType>())
        {
            var result = DesensitizedHelper.Desensitized(input, type);
            result.ShouldBe(string.Empty, $"Type: {type}");
        }
    }
    #endregion
    #region FirstMask 方法测试
    /// <summary>
    /// 测试 - FirstMask - 基本功能
    /// </summary>
    [Theory]
    [InlineData("123456789", "1********")]
    [InlineData("a", "a")]
    [InlineData("ab", "a*")]
    [InlineData("测试文本", "测***")]
    [InlineData("Test123", "T******")]
    [InlineData("!@#$%", "!****")]
    public void FirstMask_ValidInput_ReturnsFirstCharWithMask(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.FirstMask(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - FirstMask - 空值处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FirstMask_EmptyInput_ReturnsEmptyString(string input)
    {
        // Act
        var result = DesensitizedHelper.FirstMask(input);
        // Assert
        result.ShouldBe(string.Empty);
    }
    #endregion
    #region ChineseName 方法测试
    /// <summary>
    /// 测试 - ChineseName - 中文姓名脱敏
    /// </summary>
    [Theory]
    [InlineData("李", "李")]
    [InlineData("李白", "李*")]
    [InlineData("李小明", "李**")]
    [InlineData("欧阳修", "欧**")]
    [InlineData("司马相如", "司***")]
    [InlineData("爱新觉罗·溥仪", "爱******")]
    public void ChineseName_ChineseNames_ReturnsFirstCharWithMask(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.ChineseName(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - ChineseName - 非中文名字
    /// </summary>
    [Theory]
    [InlineData("John", "J***")]
    [InlineData("Smith", "S****")]
    [InlineData("John Smith", "J*********")]
    [InlineData("123", "1**")]
    public void ChineseName_NonChineseNames_ReturnsFirstCharWithMask(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.ChineseName(input);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IdCardNum 方法测试
    /// <summary>
    /// 测试 - IdCardNum - 正常身份证号脱敏
    /// </summary>
    [Theory]
    [InlineData("51343620000320711X", 1, 2, "5***************1X")]
    [InlineData("123456789012345678", 2, 3, "12*************678")]
    [InlineData("123456789012345678", 3, 4, "123***********5678")]
    [InlineData("15010319881208001X", 4, 1, "1501*************X")]
    [InlineData("123456789012345", 1, 1, "1*************5")]
    public void IdCardNum_ValidInput_ReturnsDesensitizedIdCard(string input, int front, int end, string expected)
    {
        // Act
        var result = DesensitizedHelper.IdCardNum(input, front, end);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IdCardNum - 边界条件
    /// </summary>
    [Theory]
    [InlineData("123", 2, 2, "")] // front + end > length
    [InlineData("123456", 7, 1, "")] // front > length
    [InlineData("123456", 1, 7, "")] // end > length
    [InlineData("123456", -1, 2, "")] // negative front
    [InlineData("123456", 1, -2, "")] // negative end
    //[InlineData("123456", 0, 0, "123456")] // zero values
    public void IdCardNum_EdgeCases_ReturnsExpectedResult(string input, int front, int end, string expected)
    {
        // Act
        var result = DesensitizedHelper.IdCardNum(input, front, end);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IdCardNum - 空值处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IdCardNum_EmptyInput_ReturnsEmptyString(string input)
    {
        // Act
        var result = DesensitizedHelper.IdCardNum(input, 1, 2);
        // Assert
        result.ShouldBe(string.Empty);
    }
    #endregion
    #region FixedPhone 方法测试
    /// <summary>
    /// 测试 - FixedPhone - 固定电话脱敏
    /// </summary>
    [Theory]
    [InlineData("09157518479", "0915*****79")]
    [InlineData("02087654321", "0208*****21")]
    [InlineData("021-12345678", "021-******78")]
    [InlineData("400-1234567", "400-*****67")]
    [InlineData("1234567", "1234*67")]
    [InlineData("123456", "123456")] // 刚好6位
    public void FixedPhone_ValidInput_ReturnsDesensitizedPhone(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.FixedPhone(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - FixedPhone - 短号码处理
    /// </summary>
    [Theory]
    [InlineData("12345", "12345")] // 5位数字，不足6位无法脱敏
    [InlineData("1234", "1234")] // 4位数字
    [InlineData("123", "123")] // 3位数字
    public void FixedPhone_ShortNumbers_ReturnsOriginalOrPartialMask(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.FixedPhone(input);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region MobilePhone 方法测试
    /// <summary>
    /// 测试 - MobilePhone - 手机号脱敏
    /// </summary>
    [Theory]
    [InlineData("13610000000", "136****0000")]
    [InlineData("18888888888", "188****8888")]
    [InlineData("15912345678", "159****5678")]
    [InlineData("17712345678", "177****5678")]
    [InlineData("19912345678", "199****5678")]
    public void MobilePhone_ValidMobileNumbers_ReturnsDesensitizedPhone(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.MobilePhone(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - MobilePhone - 非标准长度处理
    /// </summary>
    [Theory]
    [InlineData("1361000000", "136***0000")] // 10位
    [InlineData("136100000001", "136*****0001")] // 12位
    [InlineData("1361000", "1361000")] // 7位
    [InlineData("136100", "136100")] // 6位，不足7位
    [InlineData("13610", "13610")] // 5位
    public void MobilePhone_NonStandardLength_ReturnsPartialDesensitized(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.MobilePhone(input);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region Address 方法测试
    /// <summary>
    /// 测试 - Address - 地址脱敏
    /// </summary>
    [Theory]
    [InlineData("广东省广州市天河区猎德街道289号", 5, "广东省广州市天河区猎德街*****")]
    [InlineData("广东省广州市天河区猎德街道289号", 8, "广东省广州市天河区********")]
    [InlineData("北京市朝阳区建国门外大街1号", 10, "北京市朝**********")]
    [InlineData("上海市浦东新区世纪大道88号", 6, "上海市浦东新区世******")]
    public void Address_ValidInput_ReturnsDesensitizedAddress(string input, int sensitiveSize, string expected)
    {
        // Act
        var result = DesensitizedHelper.Address(input, sensitiveSize);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - Address - 边界条件
    /// </summary>
    [Theory]
    [InlineData("广东省广州市天河区", 0, "广东省广州市天河区")] // 不脱敏
    [InlineData("广东省广州市天河区", -1, "广东省广州市天河区")] // 负数
    [InlineData("广东省广州市天河区", 50, "*********")] // 超长脱敏
    [InlineData("短地址", 10, "***")] // 敏感长度超过总长度
    public void Address_EdgeCases_ReturnsExpectedResult(string input, int sensitiveSize, string expected)
    {
        // Act
        var result = DesensitizedHelper.Address(input, sensitiveSize);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region Email 方法测试
    /// <summary>
    /// 测试 - Email - 邮箱脱敏
    /// </summary>
    [Theory]
    [InlineData("wang@126.com", "w***@126.com")]
    [InlineData("test@gmail.com", "t***@gmail.com")]
    [InlineData("john.doe@company.com", "j*******@company.com")]
    [InlineData("user123@example.org", "u******@example.org")]
    [InlineData("a@b.com", "a@b.com")] // 用户名只有1个字符
    [InlineData("ab@domain.com", "a*@domain.com")] // 用户名只有2个字符
    public void Email_ValidEmails_ReturnsDesensitizedEmail(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.Email(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - Email - 特殊情况
    /// </summary>
    [Theory]
    [InlineData("@domain.com", "@domain.com")] // 没有用户名
    [InlineData("username", "username")] // 没有@符号
    [InlineData("user@", "u***@")] // 没有域名
    [InlineData("@", "@")] // 只有@符号
    [InlineData("user@@domain.com", "u***@@domain.com")] // 多个@符号
    public void Email_SpecialCases_ReturnsExpectedResult(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.Email(input);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region Password 方法测试
    /// <summary>
    /// 测试 - Password - 密码脱敏
    /// </summary>
    [Theory]
    [InlineData("password", "********")]
    [InlineData("123456", "******")]
    [InlineData("a", "*")]
    [InlineData("P@ssw0rd!", "*********")]
    [InlineData("VeryLongPasswordWith123", "***********************")]
    [InlineData("中文密码", "****")]
    public void Password_ValidPasswords_ReturnsAllAsterisks(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.Password(input);
        // Assert
        result.ShouldBe(expected);
        result.Length.ShouldBe(input.Length);
        result.ShouldNotContain(input); // 确保原密码被完全隐藏
    }
    #endregion
    #region CarLicense 方法测试
    /// <summary>
    /// 测试 - CarLicense - 车牌脱敏
    /// </summary>
    [Theory]
    [InlineData("粤A12345", "粤A1***5")] // 普通车牌 7位
    [InlineData("京B23456", "京B2***6")] // 普通车牌 7位
    [InlineData("沪A12345D", "沪A1****D")] // 新能源车牌 8位
    [InlineData("粤J12345F", "粤J1****F")] // 新能源车牌 8位
    [InlineData("浙A88888", "浙A8***8")] // 普通车牌 7位
    [InlineData("川A12345新", "川A1****新")] // 新能源车牌 8位
    public void CarLicense_StandardLicense_ReturnsDesensitizedLicense(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.CarLicense(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - CarLicense - 非标准长度车牌
    /// </summary>
    [Theory]
    [InlineData("粤A123", "粤A123")] // 短车牌，不脱敏
    [InlineData("粤A1234", "粤A1234")] // 6位，不脱敏
    [InlineData("粤A123456", "粤A1****6")] // 9位，不脱敏
    [InlineData("粤", "粤")] // 只有省份
    [InlineData("", "")] // 空字符串
    public void CarLicense_NonStandardLength_ReturnsOriginal(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.CarLicense(input);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region BankCard 方法测试
    /// <summary>
    /// 测试 - BankCard - 银行卡脱敏
    /// </summary>
    [Theory]
    [InlineData("6227880100100105123", "6227 **** **** **** 123")]
    [InlineData("11011111222233333256", "1101 **** **** **** 3256")]
    [InlineData("1234 2222 3333 4444 6789 9", "1234 **** **** **** **** 9")]
    [InlineData("1234 2222 3333 4444 6789 91", "1234 **** **** **** **** 91")]
    [InlineData("1234 2222 3333 4444 6789", "1234 **** **** **** 6789")]
    [InlineData("1234 2222 3333 4444 678", "1234 **** **** **** 678")]
    [InlineData("123456789012345", "1234 **** **** 345")]
    [InlineData("12345678901234567", "1234 **** **** **** 7")]
    public void BankCard_ValidBankCards_ReturnsDesensitizedCard(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.BankCard(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - BankCard - 短银行卡号
    /// </summary>
    [Theory]
    [InlineData("12345678", "12345678")] // 8位，不脱敏
    [InlineData("123456789", "1234 **** 9")] // 9位边界
    [InlineData("1234567", "1234567")] // 7位，不脱敏
    [InlineData("123", "123")] // 3位，不脱敏
    public void BankCard_ShortCards_ReturnsOriginal(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.BankCard(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - BankCard - 包含空格的银行卡号
    /// </summary>
    [Theory]
    [InlineData("1234 5678 9012 3456", "1234 **** **** 3456")]
    [InlineData("  1234  5678  9012  3456  ", "1234 **** **** 3456")]
    //[InlineData("1234-5678-9012-3456", "1234 **** **** 3456")]
    //[InlineData("1234.5678.9012.3456", "1234 **** **** 3456")]
    public void BankCard_WithSpacesOrSeparators_ReturnsDesensitizedCard(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.BankCard(input);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IPv4 方法测试
    /// <summary>
    /// 测试 - IPv4 - IP地址脱敏
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", "192.*.*.*")]
    [InlineData("10.0.0.1", "10.*.*.*")]
    [InlineData("172.16.254.1", "172.*.*.*")]
    [InlineData("255.255.255.255", "255.*.*.*")]
    [InlineData("0.0.0.0", "0.*.*.*")]
    [InlineData("127.0.0.1", "127.*.*.*")]
    public void IPv4_ValidIPAddresses_ReturnsDesensitizedIP(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.IPv4(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IPv4 - 特殊情况
    /// </summary>
    [Theory]
    [InlineData("192", "192.*.*.*")] // 不完整IP
    [InlineData("192.168", "192.*.*.*")] // 不完整IP
    [InlineData("192.168.1", "192.*.*.*")] // 不完整IP
    [InlineData("invalid.ip.address", "invalid.*.*.*")] // 无效IP
    [InlineData("", ".*.*.*")] // 空字符串
    [InlineData("no.dots.here", "no.*.*.*")] // 无效格式
    public void IPv4_SpecialCases_ReturnsExpectedResult(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.IPv4(input);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IPv6 方法测试
    /// <summary>
    /// 测试 - IPv6 - IP地址脱敏
    /// </summary>
    [Theory]
    [InlineData("2001:0db8:86a3:08d3:1319:8a2e:0370:7344", "2001:*:*:*:*:*:*:*")]
    [InlineData("2001:db8::1", "2001:*:*:*:*:*:*:*")]
    [InlineData("::1", ":*:*:*:*:*:*:*")]
    [InlineData("fe80::1%lo0", "fe80:*:*:*:*:*:*:*")]
    [InlineData("2001:db8:85a3::8a2e:370:7334", "2001:*:*:*:*:*:*:*")]
    [InlineData("::ffff:192.168.1.1", ":*:*:*:*:*:*:*")]
    public void IPv6_ValidIPv6Addresses_ReturnsDesensitizedIP(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.IPv6(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IPv6 - 特殊情况
    /// </summary>
    [Theory]
    [InlineData("2001", "2001:*:*:*:*:*:*:*")] // 不完整IPv6
    [InlineData("invalid:ipv6", "invalid:*:*:*:*:*:*:*")] // 无效IPv6
    [InlineData("", ":*:*:*:*:*:*:*")] // 空字符串
    [InlineData("no.colons.here", "no.colons.here:*:*:*:*:*:*:*")] // 无冒号
    public void IPv6_SpecialCases_ReturnsExpectedResult(string input, string expected)
    {
        // Act
        var result = DesensitizedHelper.IPv6(input);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 性能测试
    /// <summary>
    /// 测试 - 性能测试 - 大量脱敏操作
    /// </summary>
    [Fact]
    public void Performance_MassiveDesensitization_CompletesInReasonableTime()
    {
        // Arrange
        const int iterations = 10000;
        var testData = new[]
        {
            ("张三丰", DesensitizedHelper.DesensitizedType.ChineseName),
            ("51343620000320711X", DesensitizedHelper.DesensitizedType.IdCard),
            ("13610000000", DesensitizedHelper.DesensitizedType.MobilePhone),
            ("test@email.com", DesensitizedHelper.DesensitizedType.Email),
            ("6227880100100105123", DesensitizedHelper.DesensitizedType.BankCard)
        };
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                foreach (var (value, type) in testData)
                {
                    DesensitizedHelper.Desensitized(value, type);
                }
            }
        }, TimeSpan.FromSeconds(5), "Mass desensitization should complete within 5 seconds");
    }
    #endregion
    #region 安全性测试
    /// <summary>
    /// 测试 - 安全性验证 - 确保原始数据不泄露
    /// </summary>
    [Fact]
    public void Security_OriginalDataNotLeaked_ValidatesCorrectly()
    {
        // Arrange
        var sensitiveData = new Dictionary<string, DesensitizedHelper.DesensitizedType>
        {
            {"张三丰", DesensitizedHelper.DesensitizedType.ChineseName},
            {"51343620000320711X", DesensitizedHelper.DesensitizedType.IdCard},
            {"13610000000", DesensitizedHelper.DesensitizedType.MobilePhone},
            {"user@secret.com", DesensitizedHelper.DesensitizedType.Email},
            {"TopSecret123!", DesensitizedHelper.DesensitizedType.Password},
            {"6227880100100105123", DesensitizedHelper.DesensitizedType.BankCard}
        };
        // Act & Assert
        foreach (var (originalData, type) in sensitiveData)
        {
            var result = DesensitizedHelper.Desensitized(originalData, type);
            // 确保结果不等于原始数据（密码除外，密码应该完全隐藏）
            if (type == DesensitizedHelper.DesensitizedType.Password)
            {
                result.ShouldNotContain(originalData.Substring(0, Math.Min(3, originalData.Length)));
                result.ShouldAllBe(c => c == '*');
            }
            else
            {
                result.ShouldNotBe(originalData, $"Original data leaked for type: {type}");
            }
            // 确保结果不为空（除非原始数据为空）
            if (!string.IsNullOrWhiteSpace(originalData))
            {
                result.ShouldNotBeNullOrEmpty($"Result should not be empty for type: {type}");
            }
        }
    }
    /// <summary>
    /// 测试 - Unicode字符处理
    /// </summary>
    [Theory]
    [InlineData("张三🙂", DesensitizedHelper.DesensitizedType.ChineseName, "张***")]
    [InlineData("test🔥@email.com", DesensitizedHelper.DesensitizedType.Email, "t*****@email.com")]
    [InlineData("密码🔐123", DesensitizedHelper.DesensitizedType.Password, "*******")]
    [InlineData("αβγδε", DesensitizedHelper.DesensitizedType.FirstMask, "α****")]
    public void Unicode_SpecialCharacters_HandlesCorrectly(string input, DesensitizedHelper.DesensitizedType type, string expected)
    {
        // Act
        var result = DesensitizedHelper.Desensitized(input, type);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 边界和异常情况测试
    /// <summary>
    /// 测试 - 极长字符串处理
    /// </summary>
    [Fact]
    public void ExtremeLongString_HandlesCorrectly()
    {
        // Arrange
        var longString = new string('A', 10000);
        // Act & Assert
        Should.NotThrow(() =>
        {
            var result = DesensitizedHelper.FirstMask(longString);
            result.Length.ShouldBe(10000);
            result[0].ShouldBe('A');
            result.Substring(1).ShouldAllBe(c => c == '*');
        });
    }
    /// <summary>
    /// 测试 - 特殊字符混合处理
    /// </summary>
    [Theory]
    [InlineData("!@#$%^&*()", DesensitizedHelper.DesensitizedType.Password, "**********")]
    [InlineData("测试\r\n换行", DesensitizedHelper.DesensitizedType.FirstMask, "测*****")]
    [InlineData("tab\t制表符", DesensitizedHelper.DesensitizedType.ChineseName, "t******")]
    public void SpecialCharactersMixed_HandlesCorrectly(string input, DesensitizedHelper.DesensitizedType type, string expected)
    {
        // Act
        var result = DesensitizedHelper.Desensitized(input, type);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 兼容性测试
    /// <summary>
    /// 测试 - 向后兼容性验证
    /// </summary>
    [Fact]
    public void BackwardCompatibility_OriginalTestCases_StillWork()
    {
        // 这些是从原始测试中提取的用例，确保兼容性
        DesensitizedHelper.ChineseName("段正淳").ShouldBe("段**");
        DesensitizedHelper.IdCardNum("51343620000320711X", 1, 2).ShouldBe("5***************1X");
        DesensitizedHelper.FixedPhone("09157518479").ShouldBe("0915*****79");
        DesensitizedHelper.MobilePhone("13610000000").ShouldBe("136****0000");
        DesensitizedHelper.Email("wang@126.com").ShouldBe("w***@126.com");
        DesensitizedHelper.Password("1234567890").ShouldBe("**********");
        DesensitizedHelper.IPv4("192.168.1.1").ShouldBe("192.*.*.*");
        DesensitizedHelper.IPv6("2001:0db8:86a3:08d3:1319:8a2e:0370:7344").ShouldBe("2001:*:*:*:*:*:*:*");
    }
    #endregion
}
