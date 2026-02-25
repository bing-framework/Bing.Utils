namespace Bing.Net.Mac;
/// <summary>
/// MAC地址帮助类单元测试
/// </summary>
[Trait("Bing.Net", "MacAddressHelper")]
public class MacAddressHelperTest : TestBase
{
    /// <inheritdoc />
    public MacAddressHelperTest(ITestOutputHelper output) : base(output)
    {
    }
    #region Parse/FromBytes 往返测试
    /// <summary>
    /// 测试 - Parse 和 FromBytes - 往返转换
    /// </summary>
    [Theory]
    [InlineData("00:11:22:33:44:55")]
    [InlineData("AA:BB:CC:DD:EE:FF")]
    [InlineData("12-34-56-78-9A-BC")]
    public void Parse_FromBytes_RoundTrip_MaintainsData(string originalMac)
    {
        // Act
        var bytes = MacAddressHelper.Parse(originalMac);
        var reconstructed = MacAddressHelper.FromBytes(bytes);
        // Assert
        bytes.Length.ShouldBe(6);
        MacAddressHelper.AreEqual(originalMac, reconstructed).ShouldBeTrue();
        Output.WriteLine($"往返转换: {originalMac} -> {reconstructed}");
    }
    #endregion
    #region Format 测试
    /// <summary>
    /// 测试 - Format - 基本格式化
    /// </summary>
    [Theory]
    [InlineData("001122334455", ":", true, "00:11:22:33:44:55")]
    [InlineData("001122334455", "-", true, "00-11-22-33-44-55")]
    [InlineData("001122334455", ".", false, "00.11.22.33.44.55")]
    [InlineData("aabbccddeeff", ":", true, "AA:BB:CC:DD:EE:FF")]
    [InlineData("aabbccddeeff", ":", false, "aa:bb:cc:dd:ee:ff")]
    public void Format_ValidInput_ReturnsFormattedMac(string input, string separator, bool upperCase, string expected)
    {
        // Act
        var result = MacAddressHelper.Format(input, separator, upperCase);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"格式化: {input} -> {result}");
    }
    /// <summary>
    /// 测试 - Format - 无效输入抛出异常
    /// </summary>
    [Theory]
    [InlineData("")]                    // 空字符串
    [InlineData("   ")]                 // 只有空白字符
    [InlineData("12345")]               // 长度不足
    [InlineData("12345678901234")]      // 长度过长
    [InlineData("gghhiijjkkll")]        // 包含无效字符
    [InlineData("GGHHIIJJKKLL")]        // 包含无效字符（大写）
    [InlineData("!@#$%^&*(){}")]        // 完全无效字符
    [InlineData("00:11:22:33:44")]      // 长度不足（带分隔符）
    [InlineData("00:11:22:33:44:55:66")] // 长度过长（带分隔符）
    public void Format_InvalidInput_ThrowsException(string invalidInput)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => MacAddressHelper.Format(invalidInput));
        exception.ParamName.ShouldBe("macAddress");
        Output.WriteLine($"无效输入 '{invalidInput}' 正确抛出异常: {exception.Message}");
    }
    /// <summary>
    /// 测试 - Format - null 输入抛出异常
    /// </summary>
    [Fact]
    public void Format_NullInput_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => MacAddressHelper.Format(null));
        exception.ParamName.ShouldBe("macAddress");
        exception.Message.ShouldContain("MAC地址不能为空");
    }
    /// <summary>
    /// 测试 - Format - 边界情况验证
    /// </summary>
    [Theory]
    [InlineData("001122334455", ":", true, "00:11:22:33:44:55")]  // 正好12位
    [InlineData("AABBCCDDEEFF", ":", true, "AA:BB:CC:DD:EE:FF")]  // 全大写
    [InlineData("aabbccddeeff", ":", true, "AA:BB:CC:DD:EE:FF")]  // 全小写转大写
    public void Format_BoundaryConditions_WorksCorrectly(string input, string separator, bool upperCase, string expected)
    {
        // Act
        var result = MacAddressHelper.Format(input, separator, upperCase);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"边界条件测试: {input} -> {result}");
    }
    /// <summary>
    /// 测试 - FormatBatch - 批量格式化
    /// </summary>
    [Fact]
    public void FormatBatch_MixedInput_FormatsValidAddresses()
    {
        // Arrange
        var macAddresses = new[]
        {
            "001122334455",
            "invalid",
            "aa-bb-cc-dd-ee-ff",
            "AABBCCDDEEFF"
        };
        // Act
        var result = MacAddressHelper.FormatBatch(macAddresses, ":", true, skipInvalid: true);
        // Assert
        result.Count.ShouldBe(3); // 跳过无效地址
        result[0].ShouldBe("00:11:22:33:44:55");
        result[1].ShouldBe("AA:BB:CC:DD:EE:FF");
        result[2].ShouldBe("AA:BB:CC:DD:EE:FF");
        Output.WriteLine("批量格式化结果:");
        foreach (var mac in result)
        {
            Output.WriteLine($"  {mac}");
        }
    }
    #endregion
    #region IsValid 测试
    /// <summary>
    /// 测试 - IsValid - 有效MAC地址
    /// </summary>
    [Theory]
    [InlineData("00:11:22:33:44:55")]
    [InlineData("00-11-22-33-44-55")]
    [InlineData("00.11.22.33.44.55")]
    [InlineData("001122334455")]
    [InlineData("AA:BB:CC:DD:EE:FF")]
    [InlineData("aa:bb:cc:dd:ee:ff")]
    public void IsValid_ValidMacAddresses_ReturnsTrue(string validMac)
    {
        // Act
        var result = MacAddressHelper.IsValid(validMac);
        // Assert
        result.ShouldBeTrue();
        Output.WriteLine($"有效MAC地址: {validMac}");
    }
    /// <summary>
    /// 测试 - IsValid - 无效MAC地址
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("00:11:22:33:44")]
    [InlineData("00:11:22:33:44:55:66")]
    [InlineData("GG:HH:II:JJ:KK:LL")]
    public void IsValid_InvalidMacAddresses_ReturnsFalse(string invalidMac)
    {
        // Act
        var result = MacAddressHelper.IsValid(invalidMac);
        // Assert
        result.ShouldBeFalse();
        Output.WriteLine($"无效MAC地址: '{invalidMac}'");
    }
    #endregion
    #region AreEqual 测试
    /// <summary>
    /// 测试 - AreEqual - MAC地址比较
    /// </summary>
    [Theory]
    [InlineData("00:11:22:33:44:55", "00-11-22-33-44-55", true)]
    [InlineData("00:11:22:33:44:55", "00.11.22.33.44.55", true)]
    [InlineData("001122334455", "00:11:22:33:44:55", true)]
    [InlineData("aa:bb:cc:dd:ee:ff", "AA:BB:CC:DD:EE:FF", true)]
    [InlineData("00:11:22:33:44:55", "00:11:22:33:44:56", false)]
    public void AreEqual_VariousFormats_ReturnsExpected(string mac1, string mac2, bool expected)
    {
        // Act
        var result = MacAddressHelper.AreEqual(mac1, mac2);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"比较: '{mac1}' == '{mac2}': {result}");
    }
    #endregion
    #region GenerateEUI64 测试
    /// <summary>
    /// 测试 - GenerateEUI64 - 标准转换
    /// </summary>
    [Fact]
    public void GenerateEUI64_ValidMac_ReturnsCorrectEUI64()
    {
        // Arrange
        var macAddress = "00:11:22:33:44:55";
        // Act
        var result = MacAddressHelper.GenerateEUI64(macAddress);
        // Assert
        result.Length.ShouldBe(8);
        result[0].ShouldBe((byte)0x02); // 翻转U/L位后
        result[1].ShouldBe((byte)0x11);
        result[2].ShouldBe((byte)0x22);
        result[3].ShouldBe((byte)0xFF);
        result[4].ShouldBe((byte)0xFE);
        result[5].ShouldBe((byte)0x33);
        result[6].ShouldBe((byte)0x44);
        result[7].ShouldBe((byte)0x55);
        Output.WriteLine($"EUI-64: {BitConverter.ToString(result)}");
    }
    #endregion
    #region GenerateRandom 测试
    /// <summary>
    /// 测试 - GenerateRandom - 随机MAC地址生成
    /// </summary>
    [Fact]
    public void GenerateRandom_Default_GeneratesValidMac()
    {
        // Act
        var result = MacAddressHelper.GenerateRandom();
        // Assert
        result.ShouldNotBeNullOrEmpty();
        MacAddressHelper.IsValid(result).ShouldBeTrue();
        var info = MacAddressHelper.GetMacAddressInfo(result);
        info.IsUnicast.ShouldBeTrue(); // 应该是单播地址
        Output.WriteLine($"随机生成的MAC地址: {result}");
    }
    /// <summary>
    /// 测试 - GenerateRandom - 本地管理地址
    /// </summary>
    [Fact]
    public void GenerateRandom_LocallyAdministered_SetsCorrectBit()
    {
        // Act
        var result = MacAddressHelper.GenerateRandom(useLocallyAdministered: true);
        // Assert
        var info = MacAddressHelper.GetMacAddressInfo(result);
        info.IsLocallyAdministered.ShouldBeTrue();
        info.IsUnicast.ShouldBeTrue();
        Output.WriteLine($"本地管理MAC地址: {result}");
    }
    #endregion
    #region GenerateRange 测试
    /// <summary>
    /// 测试 - GenerateRange - MAC地址范围生成
    /// </summary>
    [Fact]
    public void GenerateRange_SmallRange_GeneratesCorrectSequence()
    {
        // Arrange
        var startMac = "00:11:22:33:44:55";
        var endMac = "00:11:22:33:44:59";
        // Act
        var result = MacAddressHelper.GenerateRange(startMac, endMac);
        // Assert
        result.Count.ShouldBe(5);
        result[0].ShouldBe("00:11:22:33:44:55");
        result[1].ShouldBe("00:11:22:33:44:56");
        result[2].ShouldBe("00:11:22:33:44:57");
        result[3].ShouldBe("00:11:22:33:44:58");
        result[4].ShouldBe("00:11:22:33:44:59");
        Output.WriteLine($"MAC地址范围: {startMac} 到 {endMac}");
        foreach (var mac in result)
        {
            Output.WriteLine($"  {mac}");
        }
    }
    #endregion
    #region GetMacAddressInfo 测试
    /// <summary>
    /// 测试 - GetMacAddressInfo - 地址信息分析
    /// </summary>
    [Fact]
    public void GetMacAddressInfo_ValidMac_ReturnsCorrectInfo()
    {
        // Arrange
        var macAddress = "02:11:22:33:44:55"; // 本地管理地址
        // Act
        var info = MacAddressHelper.GetMacAddressInfo(macAddress);
        // Assert
        info.ShouldNotBeNull();
        info.MacAddress.ShouldBe("02:11:22:33:44:55");
        info.IsLocallyAdministered.ShouldBeTrue();
        info.IsUniversal.ShouldBeFalse();
        info.IsUnicast.ShouldBeTrue();
        info.IsMulticast.ShouldBeFalse();
        info.OUI.ShouldBe("02:11:22");
        info.NIC.ShouldBe("33:44:55");
        Output.WriteLine($"MAC地址信息: {info}");
    }
    #endregion
    #region GetAdjacentAddress 测试
    /// <summary>
    /// 测试 - GetAdjacentAddress - 相邻地址计算
    /// </summary>
    [Theory]
    [InlineData("00:11:22:33:44:55", 1, "00:11:22:33:44:56")]
    [InlineData("00:11:22:33:44:55", -1, "00:11:22:33:44:54")]
    [InlineData("00:11:22:33:44:FF", 1, "00:11:22:33:45:00")]
    [InlineData("00:11:22:33:45:00", -1, "00:11:22:33:44:FF")]
    public void GetAdjacentAddress_ValidOffset_ReturnsCorrectAddress(string baseMac, long offset, string expected)
    {
        // Act
        var result = MacAddressHelper.GetAdjacentAddress(baseMac, offset);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"相邻地址: {baseMac} + {offset} = {result}");
    }
    #endregion
    #region 性能测试
    /// <summary>
    /// 测试 - 性能测试
    /// </summary>
    [Fact]
    public void Performance_LargeScale_CompletesWithinReasonableTime()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        // 生成大量随机MAC地址并验证
        for (int i = 0; i < 1000; i++)
        {
            var randomMac = MacAddressHelper.GenerateRandom();
            MacAddressHelper.IsValid(randomMac).ShouldBeTrue();
            MacAddressHelper.GetMacAddressInfo(randomMac);
        }
        sw.Stop();
        sw.ElapsedMilliseconds.ShouldBeLessThan(1000, "1000次MAC地址操作应该在1秒内完成");
        Output.WriteLine($"性能测试: 1000次操作耗时 {sw.ElapsedMilliseconds}ms");
    }
    #endregion
}
