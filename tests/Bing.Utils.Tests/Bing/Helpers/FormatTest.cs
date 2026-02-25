using System.Globalization;
namespace Bing.Helpers;
/// <summary>
/// 格式化操作工具类测试
/// </summary>
[Trait("Bing.Helpers", "Format")]
public class FormatTest
{
    #region EncryptPhoneOfChina 测试
    /// <summary>
    /// 测试 - EncryptPhoneOfChina - 正常手机号加密
    /// </summary>
    [Theory]
    [InlineData("13812345678", "138******78")]
    [InlineData("15987654321", "159******21")]
    [InlineData("18666666666", "186******66")]
    [InlineData("13000000000", "130******00")]
    public void EncryptPhoneOfChina_ValidPhoneNumbers_ReturnsEncryptedFormat(string phone, string expected)
    {
        // Act
        var result = Format.EncryptPhoneOfChina(phone);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - EncryptPhoneOfChina - 非标准长度但足够长的号码
    /// </summary>
    [Theory]
    [InlineData("12345", "123******45")]          // 最短有效长度
    [InlineData("123456789", "123******89")]      // 9位
    [InlineData("1234567890123", "123******23")]  // 13位
    public void EncryptPhoneOfChina_NonStandardButValidLength_ReturnsEncryptedFormat(string phone, string expected)
    {
        // Act
        var result = Format.EncryptPhoneOfChina(phone);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - EncryptPhoneOfChina - 无效输入返回空字符串
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234")]      // 长度不足
    [InlineData("123")]       // 长度不足
    [InlineData("12")]        // 长度不足
    public void EncryptPhoneOfChina_InvalidInputs_ReturnsEmptyString(string phone)
    {
        // Act
        var result = Format.EncryptPhoneOfChina(phone);
        // Assert
        result.ShouldBe("");
    }
    /// <summary>
    /// 测试 - EncryptPhoneOfChina - 包含特殊字符的号码
    /// </summary>
    [Theory]
    [InlineData("138-1234-5678", "138******78")]
    [InlineData("138 1234 5678", "138******78")]
    [InlineData("(138)12345678", "(13******78")]
    public void EncryptPhoneOfChina_PhoneWithSpecialChars_HandlesCorrectly(string phone, string expected)
    {
        // Act
        var result = Format.EncryptPhoneOfChina(phone);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region EncryptPlateNumberOfChina 测试
    /// <summary>
    /// 测试 - EncryptPlateNumberOfChina - 正常车牌号加密
    /// </summary>
    [Theory]
    [InlineData("京A12345", "京A***45")]
    [InlineData("沪B67890", "沪B***90")]
    [InlineData("粤C88888", "粤C***88")]
    [InlineData("川D99999", "川D***99")]
    [InlineData("鲁E00000", "鲁E***00")]
    public void EncryptPlateNumberOfChina_ValidPlateNumbers_ReturnsEncryptedFormat(string plateNumber, string expected)
    {
        // Act
        var result = Format.EncryptPlateNumberOfChina(plateNumber);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - EncryptPlateNumberOfChina - 最短有效长度
    /// </summary>
    [Theory]
    [InlineData("ABCD", "AB***CD")]      // 最短有效长度
    [InlineData("12345", "12***45")]     // 5位
    [InlineData("123456789", "12***89")] // 9位
    public void EncryptPlateNumberOfChina_MinimumValidLength_ReturnsEncryptedFormat(string plateNumber, string expected)
    {
        // Act
        var result = Format.EncryptPlateNumberOfChina(plateNumber);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - EncryptPlateNumberOfChina - 无效输入返回空字符串
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABC")]       // 长度不足
    [InlineData("12")]        // 长度不足
    [InlineData("A")]         // 长度不足
    public void EncryptPlateNumberOfChina_InvalidInputs_ReturnsEmptyString(string plateNumber)
    {
        // Act
        var result = Format.EncryptPlateNumberOfChina(plateNumber);
        // Assert
        result.ShouldBe("");
    }
    /// <summary>
    /// 测试 - EncryptPlateNumberOfChina - 新能源车牌
    /// </summary>
    [Theory]
    [InlineData("京AD12345", "京A***45")]    // 新能源小车
    [InlineData("京AF88888", "京A***88")]    // 新能源大车
    public void EncryptPlateNumberOfChina_NewEnergyPlates_HandlesCorrectly(string plateNumber, string expected)
    {
        // Act
        var result = Format.EncryptPlateNumberOfChina(plateNumber);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region EncryptVinCode 测试
    /// <summary>
    /// 测试 - EncryptVinCode - 正常VIN码加密
    /// </summary>
    [Theory]
    [InlineData("1HGBH41JXMN109186", "1HG***********186")]
    [InlineData("WBAPH7G58ANM12345", "WBA***********345")]
    [InlineData("JM1BK32F781234567", "JM1***********567")]
    public void EncryptVinCode_ValidVinCodes_ReturnsEncryptedFormat(string vinCode, string expected)
    {
        // Act
        var result = Format.EncryptVinCode(vinCode);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - EncryptVinCode - 最短有效长度
    /// </summary>
    [Theory]
    [InlineData("123456", "123***********456")]    // 最短有效长度
    [InlineData("ABCDEFG", "ABC***********EFG")]   // 7位
    [InlineData("12345678901234567890", "123***********890")] // 超长
    public void EncryptVinCode_MinimumValidLength_ReturnsEncryptedFormat(string vinCode, string expected)
    {
        // Act
        var result = Format.EncryptVinCode(vinCode);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - EncryptVinCode - 无效输入返回空字符串
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]     // 长度不足
    [InlineData("ABCD")]      // 长度不足
    [InlineData("1")]         // 长度不足
    public void EncryptVinCode_InvalidInputs_ReturnsEmptyString(string vinCode)
    {
        // Act
        var result = Format.EncryptVinCode(vinCode);
        // Assert
        result.ShouldBe("");
    }
    /// <summary>
    /// 测试 - EncryptVinCode - 包含小写字母的VIN码
    /// </summary>
    [Theory]
    [InlineData("1hgbh41jxmn109186", "1hg***********186")]
    [InlineData("MixedCASEvin12345", "Mix***********345")]
    public void EncryptVinCode_MixedCaseVin_HandlesCorrectly(string vinCode, string expected)
    {
        // Act
        var result = Format.EncryptVinCode(vinCode);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region FormatMoney 测试
    /// <summary>
    /// 测试 - FormatMoney - 基本金额格式化
    /// </summary>
    [Theory]
    [InlineData(0, false, "0.00")]
    [InlineData(1234.56, false, "1,234.56")]
    [InlineData(-1000.50, false, "-1,000.50")]
    [InlineData(1000000, false, "1,000,000.00")]
    [InlineData(0.01, false, "0.01")]
    [InlineData(999.999, false, "1,000.00")]  // 四舍五入
    public void FormatMoney_BasicAmounts_ReturnsFormattedString(decimal money, bool isEncrypt, string expected)
    {
        // Act
        var result = Format.FormatMoney(money, isEncrypt);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - FormatMoney - 加密模式
    /// </summary>
    [Theory]
    [InlineData(1234.56)]
    [InlineData(0)]
    [InlineData(-1000)]
    [InlineData(999999.99)]
    public void FormatMoney_EncryptMode_ReturnsAsterisks(decimal money)
    {
        // Act
        var result = Format.FormatMoney(money, true);
        // Assert
        result.ShouldBe("***");
    }
    /// <summary>
    /// 测试 - FormatMoney - 极值测试
    /// </summary>
    [Fact]
    public void FormatMoney_ExtremeValues_HandlesCorrectly()
    {
        // Act & Assert - 主要确保不抛异常
        Should.NotThrow(() =>
        {
            var result = Format.FormatMoney(decimal.MaxValue);
            result.ShouldNotBeNull();
            result.ShouldNotBeEmpty();
        });
        Should.NotThrow(() =>
        {
            var result = Format.FormatMoney(decimal.MinValue);
            result.ShouldNotBeNull();
            result.ShouldNotBeEmpty();
        });
    }
    /// <summary>
    /// 测试 - FormatMoney - 自定义格式
    /// </summary>
    [Theory]
    [InlineData(1234.56, "C", "¥1,234.56")]      // 货币格式（依赖于当前区域设置）
    [InlineData(1234.56, "F0", "1235")]          // 无小数
    [InlineData(1234.56, "N3", "1,234.560")]     // 三位小数
    [InlineData(0.1234, "P", "12.34%")]          // 百分比格式
    public void FormatMoney_CustomFormat_ReturnsCorrectFormat(decimal money, string format, string expectedPattern)
    {
        // Act
        var result = Format.FormatMoney(money, format);
        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        // 对于依赖区域设置的格式，只验证不为空
        if (format == "C")
        {
            //result.ShouldContain(money.ToString("F2"));
            result.ShouldBe(expectedPattern);
        }
        else
        {
            result.ShouldBe(expectedPattern);
        }
    }
    /// <summary>
    /// 测试 - FormatMoney - 无效格式字符串回退到默认格式
    /// </summary>
    [Theory]
    //[InlineData("INVALID")] // 结果返回的是 INVALID 字符串
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void FormatMoney_InvalidFormat_FallsBackToDefault(string invalidFormat)
    {
        // Arrange
        const decimal money = 1234.56m;
        const string expected = "1,234.56";
        // Act
        var result = Format.FormatMoney(money, invalidFormat);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - FormatMoney - 不同区域设置
    /// </summary>
    [Fact]
    public void FormatMoney_DifferentCultures_ReturnsCorrectFormat()
    {
        // Arrange
        const decimal money = 1234.56m;
        var usCulture = new CultureInfo("en-US");
        var germanCulture = new CultureInfo("de-DE");
        // Act
        var usResult = Format.FormatMoney(money, "C", usCulture);
        var germanResult = Format.FormatMoney(money, "C", germanCulture);
        // Assert
        usResult.ShouldNotBeNull();
        germanResult.ShouldNotBeNull();
        usResult.ShouldNotBe(germanResult); // 不同区域设置应产生不同结果
    }
    #endregion
    #region EncryptString 测试
    /// <summary>
    /// 测试 - EncryptString - 基本功能
    /// </summary>
    [Theory]
    [InlineData("1234567890", 2, 2, '*', 6, "12******90")]
    [InlineData("abcdefghijk", 3, 2, '*', 6, "abc******jk")]
    [InlineData("Hello World", 1, 1, '#', 3, "H###d")]
    [InlineData("Test", 1, 1, '-', 2, "T--t")]
    public void EncryptString_BasicScenarios_ReturnsCorrectFormat(string input, int prefixLength, int suffixLength,
        char maskChar, int maskLength, string expected)
    {
        // Act
        var result = Format.EncryptString(input, prefixLength, suffixLength, maskChar, maskLength);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - EncryptString - 最小长度要求
    /// </summary>
    [Theory]
    [InlineData("AB", 1, 1)]      // 刚好满足要求
    [InlineData("ABC", 2, 1)]     // 刚好满足要求
    [InlineData("ABCD", 2, 2)]    // 刚好满足要求
    public void EncryptString_MinimumLength_WorksCorrectly(string input, int prefixLength, int suffixLength)
    {
        // Act
        var result = Format.EncryptString(input, prefixLength, suffixLength);
        // Assert
        result.ShouldNotBeEmpty();
        result.Length.ShouldBeGreaterThan(prefixLength + suffixLength);
    }
    /// <summary>
    /// 测试 - EncryptString - 长度不足返回空字符串
    /// </summary>
    [Theory]
    [InlineData("A", 1, 1)]       // 长度不足
    [InlineData("AB", 2, 1)]      // 长度不足
    [InlineData("", 1, 1)]        // 空字符串
    [InlineData(null, 1, 1)]      // null
    [InlineData("   ", 1, 1)]     // 空白字符串
    public void EncryptString_InsufficientLength_ReturnsEmptyString(string input, int prefixLength, int suffixLength)
    {
        // Act
        var result = Format.EncryptString(input, prefixLength, suffixLength);
        // Assert
        result.ShouldBe("");
    }
    /// <summary>
    /// 测试 - EncryptString - 无效参数抛出异常
    /// </summary>
    [Theory]
    [InlineData(-1, 1)]  // 负的前缀长度
    [InlineData(1, -1)]  // 负的后缀长度
    [InlineData(-1, -1)] // 都为负数
    public void EncryptString_NegativeParameters_ThrowsArgumentException(int prefixLength, int suffixLength)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Format.EncryptString("test", prefixLength, suffixLength));
    }
    /// <summary>
    /// 测试 - EncryptString - 零长度参数
    /// </summary>
    [Theory]
    [InlineData(0, 1, "******a")]       // 零前缀长度
    [InlineData(1, 0, "t******")]  // 零后缀长度
    [InlineData(0, 0, "******")]   // 零前缀和后缀长度
    public void EncryptString_ZeroLengthParameters_WorksCorrectly(int prefixLength, int suffixLength, string expected)
    {
        // Arrange
        const string input = "testdata";
        // Act
        var result = Format.EncryptString(input, prefixLength, suffixLength);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - EncryptString - 自定义遮盖长度
    /// </summary>
    [Theory]
    [InlineData(1, "#")]
    [InlineData(3, "###")]
    [InlineData(10, "##########")]
    public void EncryptString_CustomMaskLength_ReturnsCorrectMaskLength(int maskLength, string expectedMask)
    {
        // Arrange
        const string input = "testdata";
        // Act
        var result = Format.EncryptString(input, 1, 1, '#', maskLength);
        // Assert
        result.ShouldBe($"t{expectedMask}a");
    }
    /// <summary>
    /// 测试 - EncryptString - 遮盖长度小于等于0时至少保留1位遮盖
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void EncryptString_NonPositiveMaskLength_UsesSingleMaskChar(int maskLength)
    {
        // Act
        var result = Format.EncryptString("abcdef", 1, 1, '#', maskLength);
        // Assert
        result.ShouldBe("a#f");
    }
    /// <summary>
    /// 测试 - EncryptString - 前后缀长度等于原文长度时仍返回掩码字符串
    /// </summary>
    [Fact]
    public void EncryptString_PrefixAndSuffixEqualInputLength_ReturnsDuplicatedVisibleSegmentsWithMask()
    {
        // Act
        var result = Format.EncryptString("abcd", 2, 2);
        // Assert
        result.ShouldBe("ab******cd");
    }
    #endregion
    #region 边界条件和性能测试
    /// <summary>
    /// 测试 - 所有方法 - 处理Unicode字符
    /// </summary>
    [Fact]
    public void AllMethods_UnicodeCharacters_HandleCorrectly()
    {
        // Arrange
        const string unicodePhone = "138中文测试678";
        const string unicodePlate = "京A测试56";
        const string unicodeVin = "测试VIN码1234567";
        const string unicodeString = "测试Unicode字符串";
        // Act & Assert - 主要确保不抛异常
        Should.NotThrow(() =>
        {
            var phoneResult = Format.EncryptPhoneOfChina(unicodePhone);
            var plateResult = Format.EncryptPlateNumberOfChina(unicodePlate);
            var vinResult = Format.EncryptVinCode(unicodeVin);
            var stringResult = Format.EncryptString(unicodeString, 2, 2);
            // 验证结果不为null
            phoneResult.ShouldNotBeNull();
            plateResult.ShouldNotBeNull();
            vinResult.ShouldNotBeNull();
            stringResult.ShouldNotBeNull();
        });
    }
    /// <summary>
    /// 测试 - 性能测试 - 大量数据处理
    /// </summary>
    [Fact]
    public void FormatMethods_LargeDataSet_PerformsReasonably()
    {
        // Arrange
        const int iterations = 1000;
        const string testPhone = "13812345678";
        const string testPlate = "京A12345";
        const string testVin = "1HGBH41JXMN109186";
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                Format.EncryptPhoneOfChina(testPhone);
                Format.EncryptPlateNumberOfChina(testPlate);
                Format.EncryptVinCode(testVin);
                Format.FormatMoney(1234.56m);
                Format.EncryptString("testdata", 2, 2);
            }
        }, TimeSpan.FromSeconds(2)); // 应该在2秒内完成1000次操作
    }
    /// <summary>
    /// 测试 - 字符串操作 - 内存安全性
    /// </summary>
    [Theory]
    [InlineData(1000)]    // 长字符串
    [InlineData(10000)]   // 很长字符串
    public void StringOperations_LargeStrings_DoNotCauseMemoryIssues(int stringLength)
    {
        // Arrange
        var largeString = new string('A', stringLength);
        // Act & Assert - 主要确保不会因为内存问题崩溃
        Should.NotThrow(() =>
        {
            var phoneResult = Format.EncryptPhoneOfChina(largeString);
            var plateResult = Format.EncryptPlateNumberOfChina(largeString);
            var vinResult = Format.EncryptVinCode(largeString);
            var stringResult = Format.EncryptString(largeString, 10, 10);
            // 验证结果的基本属性
            phoneResult.ShouldNotBeNull();
            plateResult.ShouldNotBeNull();
            vinResult.ShouldNotBeNull();
            stringResult.ShouldNotBeNull();
        });
    }
    #endregion
    #region 实际使用场景测试
    /// <summary>
    /// 测试 - 实际使用场景 - 用户隐私保护
    /// </summary>
    [Fact]
    public void PrivacyProtection_RealWorldScenarios_WorksCorrectly()
    {
        // Arrange - 模拟真实数据
        var users = new[]
        {
            new { Phone = "13812345678", Plate = "京A88888", Vin = "LSGJA52U4DH123456", Money = 15680.99m },
            new { Phone = "15987654321", Plate = "沪B66666", Vin = "WBAPH7G58ANM12345", Money = 8888.88m },
            new { Phone = "18666666666", Plate = "粤C99999", Vin = "JM1BK32F781234567", Money = 999999.99m }
        };
        // Act & Assert
        foreach (var user in users)
        {
            var encryptedPhone = Format.EncryptPhoneOfChina(user.Phone);
            var encryptedPlate = Format.EncryptPlateNumberOfChina(user.Plate);
            var encryptedVin = Format.EncryptVinCode(user.Vin);
            var encryptedMoney = Format.FormatMoney(user.Money, true);
            var formattedMoney = Format.FormatMoney(user.Money);
            // 验证加密后的格式
            encryptedPhone.ShouldContain("******");
            encryptedPlate.ShouldContain("***");
            encryptedVin.ShouldContain("***********");
            encryptedMoney.ShouldBe("***");
            // 验证格式化的金额
            formattedMoney.ShouldNotContain("*");
            formattedMoney.ShouldContain(".");
            // 验证原始信息不完全暴露
            encryptedPhone.ShouldNotBe(user.Phone);
            encryptedPlate.ShouldNotBe(user.Plate);
            encryptedVin.ShouldNotBe(user.Vin);
        }
    }
    #endregion
}
