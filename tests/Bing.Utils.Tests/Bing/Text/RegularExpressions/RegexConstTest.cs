namespace Bing.Text.RegularExpressions;
/// <summary>
/// 正则表达式常量 测试
/// </summary>
public class RegexConstTest
{
    #region 基础字符和数字测试
    /// <summary>
    /// 测试 - General - 英文字母数字下划线完整匹配
    /// </summary>
    [Theory]
    [InlineData("abc123", true)]
    [InlineData("_test", true)]
    [InlineData("A1B2C3", true)]
    [InlineData("test_123", true)]
    [InlineData("123abc", true)]
    [InlineData("_", true)]
    [InlineData("abc-123", false)]
    [InlineData("abc 123", false)]
    [InlineData("", false)]
    //[InlineData("测试", false)]
    [InlineData("abc@123", false)]
    public void General_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.General);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - Numbers - 数字匹配
    /// </summary>
    [Theory]
    [InlineData("123", true)]
    [InlineData("0", true)]
    [InlineData("abc123def", true)]  // 包含数字
    [InlineData("1", true)]
    [InlineData("999999999", true)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    [InlineData("abc_def", false)]
    public void Numbers_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Numbers);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - Word - 英文字母匹配
    /// </summary>
    [Theory]
    [InlineData("abc", true)]
    [InlineData("ABC", true)]
    [InlineData("Hello", true)]
    [InlineData("abc123", true)]  // 包含字母
    [InlineData("A", true)]
    [InlineData("z", true)]
    [InlineData("123", false)]
    [InlineData("", false)]
    [InlineData("测试", false)]
    [InlineData("_abc", true)]  // 下划线开头
    public void Word_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Word);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 中文相关测试
    /// <summary>
    /// 测试 - Chinese - 单个中文字符匹配
    /// </summary>
    [Theory]
    [InlineData("中", true)]
    [InlineData("文", true)]
    [InlineData("测试中文", true)]
    [InlineData("abc中文", true)]  // 包含中文
    [InlineData("汉", true)]
    [InlineData("字", true)]
    [InlineData("abc", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    [InlineData("_", false)]
    public void Chinese_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Chinese);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - Chineses - 一个或多个中文字符
    /// </summary>
    [Theory]
    [InlineData("中文", true)]
    [InlineData("测试", true)]
    [InlineData("中", true)]
    [InlineData("汉字测试内容", true)]
    [InlineData("abc中文", true)]  // 包含中文
    [InlineData("中文abc", true)]  // 包含中文
    [InlineData("abc", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public void Chineses_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Chineses);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - GeneralWithChinese - 中文字母数字下划线
    /// </summary>
    [Theory]
    [InlineData("中文Test123", true)]
    [InlineData("用户名_123", true)]
    [InlineData("测试ABC", true)]
    [InlineData("中文", true)]
    [InlineData("abc123", true)]
    [InlineData("_test", true)]
    [InlineData("中文-123", false)]  // 包含连字符
    [InlineData("中文 123", false)]  // 包含空格
    [InlineData("", false)]
    public void GeneralWithChinese_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.GeneralWithChinese);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - ChineseName - 中文姓名
    /// </summary>
    [Theory]
    [InlineData("张三", true)]
    [InlineData("李小明", true)]
    [InlineData("古丽·努尔", true)]
    [InlineData("欧阳修", true)]
    [InlineData("王二麻子", true)]
    [InlineData("李", false)]  // 太短
    [InlineData("张三123", false)]  // 包含数字
    [InlineData("Zhang San", false)]  // 包含英文
    [InlineData("", false)]
    [InlineData("张三-李四", false)]  // 包含连字符
    public void ChineseName_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.ChineseName);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 网络相关测试
    /// <summary>
    /// 测试 - IPv4 - IP地址格式
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("255.255.255.255", true)]
    [InlineData("0.0.0.0", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("172.16.254.1", true)]
    [InlineData("256.1.1.1", false)]  // 超出范围
    [InlineData("192.168.1", false)]  // 不完整
    [InlineData("192.168.1.1.1", false)]  // 多余段
    [InlineData("abc.def.ghi.jkl", false)]
    [InlineData("", false)]
    [InlineData("192.168.01.1", true)]  // 前导零
    public void IPv4_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.IPv4);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IPv6 - IPv6地址格式
    /// </summary>
    [Theory]
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334", true)]
    [InlineData("2001:db8:85a3:0:0:8a2e:370:7334", true)]
    [InlineData("2001:db8:85a3::8a2e:370:7334", true)]
    [InlineData("::1", true)]
    [InlineData("fe80::1", true)]
    [InlineData("::", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void IPv6_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.IPv6);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - MacAddress - MAC地址格式
    /// </summary>
    [Theory]
    [InlineData("00:1B:44:11:3A:B7", true)]
    [InlineData("00-1B-44-11-3A-B7", true)]
    [InlineData("001B44113AB7", true)]
    [InlineData("0:1B:44:11:3A:B7", true)]
    [InlineData("00:1b:44:11:3a:b7", true)]  // 小写
    [InlineData("0:1b:4:1:3a:b7", true)]     // 单字符段
    [InlineData("GG:1B:44:11:3A:B7", false)]  // 无效字符
    [InlineData("00:1B:44:11:3A", false)]     // 不完整
    [InlineData("", false)]
    [InlineData("00:1B:44:11:3A:B7:C8", true)]  // 匹配到前半部分，位数太多
    public void MacAddress_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.MacAddress);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region URL和URI测试
    /// <summary>
    /// 测试 - Uri - URI格式
    /// </summary>
    [Theory]
    [InlineData("http://example.com", true)]
    [InlineData("https://www.example.com", true)]
    [InlineData("ftp://files.example.com", true)]
    [InlineData("mailto:user@example.com", true)]
    [InlineData("file:///path/to/file", true)]
    [InlineData("relative/path", true)]
    [InlineData("//example.com", true)]
    [InlineData("", true)]  // 空字符串也匹配URI格式
    public void Uri_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Uri);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - Url - URL格式
    /// </summary>
    [Theory]
    [InlineData("http://www.example.com", true)]
    [InlineData("https://example.com/path", true)]
    [InlineData("ftp://files.example.com", true)]
    [InlineData("http://localhost:8080", true)]
    [InlineData("www.example.com", false)]  // 缺少协议
    [InlineData("example.com", false)]
    [InlineData("", false)]
    public void Url_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Url);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - UrlByHttp - HTTP/HTTPS/FTP/File URL
    /// </summary>
    [Theory]
    [InlineData("https://www.example.com", true)]
    [InlineData("http://example.com", true)]
    [InlineData("ftp://files.example.com", true)]
    [InlineData("file:///path/to/file", true)]
    [InlineData("https://example.com/path/to/resource?param=value", true)]
    [InlineData("mailto:user@example.com", false)]  // 不支持mailto
    [InlineData("www.example.com", false)]
    [InlineData("", false)]
    public void UrlByHttp_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.UrlByHttp);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 邮箱测试
    /// <summary>
    /// 测试 - Email - 标准邮箱格式
    /// </summary>
    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("test.email@domain.co.uk", true)]
    [InlineData("user+tag@example.com", true)]
    [InlineData("user_name@test-domain.org", true)]
    [InlineData("a@b.co", true)]
    [InlineData("user@example", false)]  // 缺少顶级域名
    [InlineData("userexample.com", false)]  // 缺少@
    [InlineData("@example.com", false)]  // 缺少用户名
    [InlineData("user@", false)]  // 缺少域名
    [InlineData("", false)]
    public void Email_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Email);
        // Assert
        result.ShouldBe(expected);
    }
    ///// <summary>
    ///// 测试 - EmailWithChinese - 支持中文的邮箱
    ///// </summary>
    //[Theory]
    //[InlineData("用户@example.com", true)]
    //[InlineData("测试@domain.cn", true)]
    //[InlineData("user@中文域名.com", true)]
    //[InlineData("中文用户@中文域名.cn", true)]
    //[InlineData("user@example.com", true)]  // 普通邮箱也支持
    //[InlineData("用户@", false)]  // 缺少域名
    //[InlineData("@中文域名.com", false)]  // 缺少用户名
    //[InlineData("", false)]
    //public void EmailWithChinese_ValidInput_ReturnsExpectedResult(string input, bool expected)
    //{
    //    // Act
    //    var result = Regex.IsMatch(input, RegexConst.EmailWithChinese);
    //    // Assert
    //    result.ShouldBe(expected);
    //}
    #endregion
    #region 电话号码测试
    /// <summary>
    /// 测试 - Mobile - 中国大陆手机号码
    /// </summary>
    [Theory]
    [InlineData("13812345678", true)]
    [InlineData("15912345678", true)]
    [InlineData("18612345678", true)]
    [InlineData("19812345678", true)]
    [InlineData("+8613812345678", true)]
    [InlineData("8613812345678", true)]
    [InlineData("013812345678", true)]
    [InlineData("12812345678", false)]  // 无效号段
    [InlineData("1381234567", false)]   // 位数不够
    [InlineData("138123456789", true)] // 匹配到前半部分，位数太多
    [InlineData("", false)]
    public void Mobile_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Mobile);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - MobileByHK - 中国香港手机号码
    /// </summary>
    [Theory]
    [InlineData("51004810", true)]
    [InlineData("91234567", true)]
    [InlineData("+85251004810", true)]
    [InlineData("85251004810", true)]
    [InlineData("051004810", true)]
    [InlineData("5100481", false)]   // 位数不够
    [InlineData("510048100", true)] // 匹配到前半部分，位数太多
    [InlineData("", false)]
    public void MobileByHK_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.MobileByHK);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - MobileByTW - 中国台湾手机号码
    /// </summary>
    [Theory]
    [InlineData("0960000000", true)]
    [InlineData("0987654321", true)]
    [InlineData("+8860960000000", true)]
    [InlineData("8860960000000", true)]
    [InlineData("886-0960000000", true)]
    [InlineData("0860000000", false)]  // 不是09开头
    [InlineData("096000000", false)]   // 位数不够
    [InlineData("", false)]
    public void MobileByTW_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.MobileByTW);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - MobileByMO - 中国澳门手机号码
    /// </summary>
    [Theory]
    [InlineData("68000000", true)]
    [InlineData("66123456", true)]
    [InlineData("+85368000000", true)]
    [InlineData("85368000000", true)]
    [InlineData("853-68000000", true)]
    [InlineData("58000000", false)]  // 不是6开头
    [InlineData("6800000", false)]   // 位数不够
    [InlineData("680000000", true)] // 位数太多
    [InlineData("", false)]
    public void MobileByMO_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.MobileByMO);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - Tel - 座机号码
    /// </summary>
    [Theory]
    [InlineData("010-12345678", true)]
    [InlineData("021-87654321", true)]
    [InlineData("0571-87654321", true)]
    [InlineData("01012345678", true)]   // 不带连字符
    [InlineData("02187654321", true)]
    [InlineData("057187654321", true)]
    [InlineData("400-123-4567", false)] // 400号码不匹配此格式
    [InlineData("12345678", false)]     // 缺少区号
    [InlineData("", false)]
    public void Tel_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Tel);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - TEL_400_800 - 座机400800电话
    /// </summary>
    [Theory]
    [InlineData("0571-87654321", true)]
    [InlineData("010 12345678", true)]
    [InlineData("400-123-4567", true)]
    [InlineData("800-123-4567", true)]
    [InlineData("400 123 4567", true)]
    [InlineData("8001234567", true)]
    [InlineData("900-123-4567", false)] // 不支持900
    [InlineData("123-4567", false)]     // 格式不对
    [InlineData("", false)]
    public void TEL_400_800_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Tel400800);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 身份证和证件测试
    /// <summary>
    /// 测试 - CitizenId - 18位身份证号码
    /// </summary>
    [Theory]
    [InlineData("110101199003078515", true)]
    [InlineData("31010519900307851X", true)]
    [InlineData("440301199001011234", true)]
    [InlineData("51010219900101123x", true)]  // 小写x
    [InlineData("11010119900307851", false)]  // 17位
    [InlineData("1101011990030785123", true)] // 19位，只匹配了前半部分
    [InlineData("010101199003078515", false)]  // 地区码不能以0开头
    [InlineData("110101199013078515", false)]  // 无效月份
    //[InlineData("110101199002308515", false)]  // 无效日期
    [InlineData("", false)]
    public void CitizenId_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.CitizenId);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 车辆相关测试
    /// <summary>
    /// 测试 - PlateNumber - 中国车牌号码
    /// </summary>
    [Theory]
    [InlineData("京A12345", true)]
    [InlineData("沪B23456", true)]
    [InlineData("粤A123D4", true)]  // 新能源车牌
    [InlineData("京A12345挂", false)]
    [InlineData("使123456", false)]
    [InlineData("ABC12345", false)] // 无效省份
    [InlineData("京12345", false)]  // 缺少字母
    [InlineData("", false)]
    public void PlateNumber_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.PlateNumber);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - CarVin - 车架号
    /// </summary>
    [Theory]
    [InlineData("LDC613P23A1305189", true)]
    [InlineData("LSJA24U62JG269225", true)]
    [InlineData("LBV5S3102ESJ25655", true)]
    [InlineData("1HGBH41JXMN109186", true)]
    [InlineData("LDC613P23A130518", false)]  // 位数不够
    [InlineData("LDC613P23A13051890", false)] // 位数太多
    [InlineData("LDC613P23I1305189", false)]  // 包含I
    [InlineData("LDC613P23O1305189", false)]  // 包含O
    [InlineData("LDC613P23Q1305189", false)]  // 包含Q
    [InlineData("", false)]
    public void CarVin_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.CarVin);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - CarDrivingLicence - 驾驶证档案编号
    /// </summary>
    [Theory]
    [InlineData("430101758218", true)]
    [InlineData("123456789012", true)]
    [InlineData("000000000000", true)]
    [InlineData("12345678901", false)]  // 11位
    [InlineData("1234567890123", false)] // 13位
    [InlineData("43010175821a", false)]  // 包含字母
    [InlineData("", false)]
    public void CarDrivingLicence_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.CarDrivingLicence);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 日期时间测试
    /// <summary>
    /// 测试 - Birthday - 生日格式
    /// </summary>
    [Theory]
    [InlineData("1990/03/07", true)]
    [InlineData("1990-03-07", true)]
    [InlineData("1990.03.07", true)]
    [InlineData("1990年3月7日", true)]
    [InlineData("90/3/7", true)]
    [InlineData("1990年3月7", true)]  // 不带"日"
    [InlineData("90-3-7", true)]
    [InlineData("1990", true)]      // 只有年份
    [InlineData("1990/03", true)]   // 缺少日
    [InlineData("", false)]
    public void Birthday_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Birthday);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - Time - 时间格式
    /// </summary>
    [Theory]
    [InlineData("14:30", true)]
    [InlineData("14:30:25", true)]
    [InlineData("14时30分", true)]
    [InlineData("14时30分25秒", true)]
    [InlineData("9:30", true)]
    [InlineData("9时5分", true)]
    [InlineData("25:30", true)]     // 此正则不验证时间有效性
    [InlineData("14", false)]      // 只有小时
    [InlineData("", false)]
    public void Time_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Time);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 编码和标识测试
    /// <summary>
    /// 测试 - Uuid - 标准UUID格式
    /// </summary>
    [Theory]
    [InlineData("550e8400-e29b-41d4-a716-446655440000", true)]
    [InlineData("6ba7b810-9dad-11d1-80b4-00c04fd430c8", true)]
    [InlineData("123e4567-e89b-12d3-a456-426614174000", true)]
    [InlineData("550e8400e29b41d4a716446655440000", false)]  // 缺少连字符
    [InlineData("550e8400-e29b-41d4-a716-44665544000", false)] // 位数不对
    [InlineData("550e8400-e29b-41d4-a716-44665544000G", false)] // 无效字符
    [InlineData("", false)]
    public void Uuid_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Uuid);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - UuidSimple - 不带连字符的UUID
    /// </summary>
    [Theory]
    [InlineData("550e8400e29b41d4a716446655440000", true)]
    [InlineData("6ba7b8109dad11d180b400c04fd430c8", true)]
    [InlineData("123E4567E89B12D3A456426614174000", true)]  // 大写
    [InlineData("123e4567E89B12d3A456426614174000", true)]  // 混合大小写
    [InlineData("550e8400-e29b-41d4-a716-446655440000", false)] // 包含连字符
    [InlineData("550e8400e29b41d4a716446655440", false)]    // 位数不够
    [InlineData("550e8400e29b41d4a7166466554400000", false)] // 位数太多
    [InlineData("550e8400e29b41d4a716446655440000G", false)] // 无效字符
    [InlineData("", false)]
    public void UuidSimple_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.UuidSimple);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - Hex - 16进制字符串
    /// </summary>
    [Theory]
    [InlineData("FF00CC", true)]
    [InlineData("abc123", true)]
    [InlineData("1234567890ABCDEF", true)]
    [InlineData("0", true)]
    [InlineData("f", true)]
    [InlineData("A", true)]
    [InlineData("GGHHII", false)]  // 无效字符
    [InlineData("123G", false)]    // 包含无效字符
    [InlineData("", false)]
    public void Hex_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Hex);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 中国特色标识测试
    /// <summary>
    /// 测试 - ZipCode - 邮政编码
    /// </summary>
    [Theory]
    [InlineData("100000", true)]  // 北京
    [InlineData("200000", true)]  // 上海
    [InlineData("518000", true)]  // 深圳
    [InlineData("999077", true)]  // 香港特殊编码
    [InlineData("999078", true)]  // 澳门特殊编码
    [InlineData("000000", false)] // 无效编码
    [InlineData("999999", false)] // 无效编码
    [InlineData("12345", false)]  // 位数不够
    [InlineData("1234567", true)] // 位数太多
    [InlineData("", false)]
    public void ZipCode_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.ZipCode);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - CreditCode - 统一社会信用代码
    /// </summary>
    [Theory]
    [InlineData("91110000MA001234X5", true)]
    [InlineData("12345678MA1234567X", true)]
    [InlineData("91320000MA12345678", true)]
    [InlineData("9111000MA001234X5", false)]  // 17位
    [InlineData("91110000MA001234X56", false)] // 19位
    [InlineData("91110000MA001234I5", false)]  // 包含I
    [InlineData("91110000MA001234O5", false)]  // 包含O
    [InlineData("", false)]
    public void CreditCode_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.CreditCode);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 货币和金额测试
    /// <summary>
    /// 测试 - Money - 货币金额格式
    /// </summary>
    [Theory]
    [InlineData("100", true)]
    [InlineData("99.9", true)]
    [InlineData("123.45", true)]
    [InlineData("0", true)]
    [InlineData("0.1", true)]
    [InlineData("0.01", true)]
    [InlineData("999999.99", true)]
    [InlineData("123.456", false)]  // 超过2位小数
    [InlineData("-100", false)]     // 负数
    [InlineData("abc", false)]
    [InlineData("", false)]
    [InlineData("100.", false)]     // 小数点后无数字
    public void Money_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.Money);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 分组变量测试
    /// <summary>
    /// 测试 - GroupVar - 分组变量
    /// </summary>
    [Theory]
    [InlineData("$1", true)]
    [InlineData("$2", true)]
    [InlineData("$10", true)]
    [InlineData("$999", true)]
    [InlineData("$0", true)]
    [InlineData("$", false)]        // 缺少数字
    [InlineData("1", false)]        // 缺少$
    [InlineData("$a", false)]       // 非数字
    [InlineData("", false)]
    public void GroupVar_ValidInput_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = Regex.IsMatch(input, RegexConst.GroupVar);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 边界和异常情况测试
    /// <summary>
    /// 测试 - 空字符串和null处理
    /// </summary>
    [Fact]
    public void RegexPatterns_WithNullOrEmpty_ShouldHandleGracefully()
    {
        // Arrange
        var emptyString = "";
        string nullString = null;
        // Act & Assert
        Should.NotThrow(() => Regex.IsMatch(emptyString, RegexConst.General));
        Should.NotThrow(() => Regex.IsMatch(emptyString, RegexConst.Email));
        Should.NotThrow(() => Regex.IsMatch(emptyString, RegexConst.Mobile));
        // null 字符串测试
        Should.Throw<ArgumentNullException>(() => Regex.IsMatch(nullString, RegexConst.General));
    }
    /// <summary>
    /// 测试 - 极长字符串处理
    /// </summary>
    [Fact]
    public void RegexPatterns_WithVeryLongString_ShouldHandleCorrectly()
    {
        // Arrange
        var longString = new string('a', 10000);
        var longNumberString = new string('1', 1000);
        // Act & Assert
        Should.NotThrow(() => Regex.IsMatch(longString, RegexConst.Word));
        Should.NotThrow(() => Regex.IsMatch(longNumberString, RegexConst.Numbers));
        // 验证结果
        Regex.IsMatch(longString, RegexConst.Word).ShouldBeTrue();
        Regex.IsMatch(longNumberString, RegexConst.Numbers).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - 特殊Unicode字符处理
    /// </summary>
    [Theory]
    [InlineData("😀", false)]      // Emoji
    [InlineData("🚀", false)]      // Emoji
    [InlineData("测试😀", false)]   // 中文+Emoji（对于Word正则）
    [InlineData("α", false)]       // 希腊字母（对于Word正则）
    [InlineData("ß", false)]       // 德文字母（对于Word正则）
    public void RegexPatterns_WithUnicodeCharacters_ShouldHandleCorrectly(string input, bool expectedForWord)
    {
        // Act
        var wordResult = Regex.IsMatch(input, RegexConst.Word);
        // Assert
        wordResult.ShouldBe(expectedForWord);
        // 验证中文正则能正确处理
        if (input.Contains('测') || input.Contains('试'))
        {
            Regex.IsMatch(input, RegexConst.Chinese).ShouldBeTrue();
        }
    }
    /// <summary>
    /// 测试 - 正则表达式性能
    /// </summary>
    [Fact]
    public void RegexPatterns_Performance_ShouldBeReasonable()
    {
        // Arrange
        var testInputs = new[]
        {
            "user@example.com",
            "13812345678",
            "192.168.1.1",
            "测试中文内容",
            "abc123_test"
        };
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                foreach (var input in testInputs)
                {
                    Regex.IsMatch(input, RegexConst.Email);
                    Regex.IsMatch(input, RegexConst.Mobile);
                    Regex.IsMatch(input, RegexConst.IPv4);
                    Regex.IsMatch(input, RegexConst.Chinese);
                    Regex.IsMatch(input, RegexConst.General);
                }
            }
        }, TimeSpan.FromSeconds(5)); // 应该在5秒内完成
    }
    #endregion
    #region 综合场景测试
    /// <summary>
    /// 测试 - 真实数据验证场景
    /// </summary>
    [Fact]
    public void RegexPatterns_RealWorldScenarios_ShouldWorkCorrectly()
    {
        // 真实的邮箱地址
        var emails = new[]
        {
            "john.doe@company.com",
            "user+filter@gmail.com",
            "test.email.with+symbol@example.co.uk",
            "simple@example.org"
        };
        foreach (var email in emails)
        {
            Regex.IsMatch(email, RegexConst.Email).ShouldBeTrue($"邮箱 {email} 应该匹配");
        }
        // 真实的手机号码
        var mobiles = new[]
        {
            "13800138000",
            "15912345678",
            "18611111111",
            "+8613912345678"
        };
        foreach (var mobile in mobiles)
        {
            Regex.IsMatch(mobile, RegexConst.Mobile).ShouldBeTrue($"手机号 {mobile} 应该匹配");
        }
        // 真实的身份证号码格式
        var idCards = new[]
        {
            "110101199003078515",
            "44030119900101123X",
            "51010219900101123x"
        };
        foreach (var idCard in idCards)
        {
            Regex.IsMatch(idCard, RegexConst.CitizenId).ShouldBeTrue($"身份证号 {idCard} 应该匹配");
        }
    }
    /// <summary>
    /// 测试 - 组合验证场景
    /// </summary>
    [Theory]
    [InlineData("张三", true, false, false, true)]   // 姓名: 中文✓, 邮箱✗, 手机✗, 中文字符✓
    [InlineData("user@test.com", false, true, false, false)] // 邮箱: 姓名✗, 邮箱✓, 手机✗, 中文✗
    [InlineData("13812345678", false, false, true, false)]   // 手机: 姓名✗, 邮箱✗, 手机✓, 中文✗
    [InlineData("测试ABC123", false, false, false, true)]    // 混合: 姓名✗, 邮箱✗, 手机✗, 中文字符✓
    public void RegexPatterns_CombinedValidation_ShouldWorkCorrectly(
        string input,
        bool isName,
        bool isEmail,
        bool isMobile,
        bool hasChinese)
    {
        // Act
        var nameResult = Regex.IsMatch(input, RegexConst.ChineseName);
        var emailResult = Regex.IsMatch(input, RegexConst.Email);
        var mobileResult = Regex.IsMatch(input, RegexConst.Mobile);
        var chineseResult = Regex.IsMatch(input, RegexConst.Chinese);
        // Assert
        nameResult.ShouldBe(isName);
        emailResult.ShouldBe(isEmail);
        mobileResult.ShouldBe(isMobile);
        chineseResult.ShouldBe(hasChinese);
    }
    #endregion
}
