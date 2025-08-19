namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址验证器单元测试
/// </summary>
[Trait("Bing.Net", "IpValidator")]
public class IPv6ValidatorTest : TestBase
{
    /// <inheritdoc />
    public IPv6ValidatorTest(ITestOutputHelper output) : base(output)
    {
    }

    #region IsValid 测试

    /// <summary>
    /// 测试 - IsValid - 有效IPv6地址验证
    /// </summary>
    [Theory]
    [InlineData("2001:db8::1")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("2001:4860:4860::8888")]
    [InlineData("fe80::1")]
    [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
    [InlineData("2001:0db8:0000:0000:0000:0000:0000:0001")]
    [InlineData("::ffff:192.168.1.1")]  // IPv4映射
    [InlineData("::ffff:0:192.168.1.1")] // IPv4映射变体
    [InlineData("2001:db8:85a3::8a2e:370:7334")]
    [InlineData("ff02::1")]              // 组播
    [InlineData("fc00::1")]              // ULA
    [InlineData("fd12:3456:789a::1")]    // ULA
    [InlineData("fec0::1")]              // 站点本地（已废弃）
    public void IsValid_ValidIPv6Addresses_ReturnsTrue(string ipv6)
    {
        // Act
        var result = IPv6Validator.IsValid(ipv6);

        // Assert
        result.ShouldBeTrue($"'{ipv6}' 应该是有效的IPv6地址");
        Output.WriteLine($"✓ Valid IPv6: {ipv6}");
    }

    /// <summary>
    /// 测试 - IsValid - 无效IPv6地址验证
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("192.168.1.1")]          // IPv4地址
    [InlineData("2001:db8::1::1")]       // 双重::
    [InlineData("2001:db8:invalid::1")]  // 无效十六进制
    [InlineData("gggg::1")]              // 无效字符
    [InlineData("2001:db8::gggg")]       // 无效字符
    [InlineData("2001:db8:1:2:3:4:5:6:7")] // 过多段
    [InlineData("12345::1")]             // 段超过4位
    [InlineData(":2001:db8::1")]         // 开头多余冒号
    [InlineData("2001:db8::1:")]         // 结尾多余冒号
    [InlineData("256.1.1.1")]           // 无效IPv4
    public void IsValid_InvalidIPv6Addresses_ReturnsFalse(string invalidIpv6)
    {
        // Act
        var result = IPv6Validator.IsValid(invalidIpv6);

        // Assert
        result.ShouldBeFalse($"'{invalidIpv6}' 应该是无效的IPv6地址");
        Output.WriteLine($"✗ Invalid IPv6: {invalidIpv6}");
    }

    #endregion

    #region IsValidRegex 测试

    /// <summary>
    /// 测试 - IsValidRegex - 正则表达式验证
    /// </summary>
    [Theory]
    [InlineData("2001:db8::1", true)]
    [InlineData("::1", true)]
    [InlineData("::", true)]
    [InlineData("invalid", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidRegex_VariousAddresses_ReturnsExpected(string ipv6, bool expected)
    {
        // Act
        var result = IPv6Validator.IsValidRegex(ipv6);

        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"Regex validation for '{ipv6}': {result}");
    }

    /// <summary>
    /// 测试 - IsValid vs IsValidRegex - 一致性比较
    /// </summary>
    [Theory]
    [InlineData("2001:db8::1")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("fe80::1")]
    [InlineData("::ffff:192.168.1.1")]
    public void IsValid_vs_IsValidRegex_ConsistentResults(string ipv6)
    {
        // Act
        var standardResult = IPv6Validator.IsValid(ipv6);
        var regexResult = IPv6Validator.IsValidRegex(ipv6);

        // Assert
        standardResult.ShouldBe(regexResult, $"IsValid 和 IsValidRegex 对于 '{ipv6}' 应该返回相同结果");
        Output.WriteLine($"Consistency check for '{ipv6}': Standard={standardResult}, Regex={regexResult}");
    }

    #endregion

    #region TryValidate 测试

    /// <summary>
    /// 测试 - TryValidate - 成功验证并获取详细信息
    /// </summary>
    [Theory]
    [InlineData("::1", IPv6AddressType.Loopback)]
    [InlineData("2001:db8::1", IPv6AddressType.Documentation)]
    [InlineData("fe80::1", IPv6AddressType.LinkLocal)]
    [InlineData("fc00::1", IPv6AddressType.UniqueLocal)]
    [InlineData("ff02::1", IPv6AddressType.Multicast)]
    [InlineData("::ffff:192.168.1.1", IPv6AddressType.IPv4Mapped)]
    [InlineData("2001:4860:4860::8888", IPv6AddressType.GlobalUnicast)]
    public void TryValidate_ValidAddresses_ReturnsCorrectInfo(string ipv6, IPv6AddressType expectedType)
    {
        // Act
        var result = IPv6Validator.TryValidate(ipv6, out var address, out var addressType);

        // Assert
        result.ShouldBeTrue($"'{ipv6}' 应该验证成功");
        address.ShouldNotBeNull();
        address.AddressFamily.ShouldBe(System.Net.Sockets.AddressFamily.InterNetworkV6);
        addressType.ShouldBe(expectedType);

        Output.WriteLine($"TryValidate('{ipv6}'): Type={addressType}, Address={address}");
    }

    /// <summary>
    /// 测试 - TryValidate - 无效地址处理
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("2001:db8::1::1")]       // 双重::
    [InlineData("gggg::1")]              // 无效字符
    public void TryValidate_InvalidAddresses_ReturnsFalse(string invalidIpv6)
    {
        // Act
        var result = IPv6Validator.TryValidate(invalidIpv6, out var address, out var addressType);

        // Assert
        result.ShouldBeFalse($"'{invalidIpv6}' 应该验证失败");
        address.ShouldBeNull();
        addressType.ShouldBe(IPv6AddressType.Invalid);

        Output.WriteLine($"TryValidate('{invalidIpv6}'): Failed as expected");
    }

    /// <summary>
    /// 测试 - TryValidate - IPv4地址作为无效IPv6地址处理
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("127.0.0.1")]
    [InlineData("8.8.8.8")]
    public void TryValidate_IPv4Addresses_ReturnsFalse(string ipv4)
    {
        // Act
        var result = IPv6Validator.TryValidate(ipv4, out var address, out var addressType);

        // Assert
        result.ShouldBeFalse($"IPv4地址 '{ipv4}' 不应该被验证为IPv6地址");
        address.ShouldBeNull("验证失败时 address 应该为 null");
        addressType.ShouldBe(IPv6AddressType.Invalid);

        Output.WriteLine($"TryValidate('{ipv4}'): Correctly rejected IPv4 address");
    }

    #endregion

    #region IsLocalIp 测试

    /// <summary>
    /// 测试 - IsLocalIp - 回环地址判断
    /// </summary>
    [Theory]
    [InlineData("::1", true)]
    [InlineData("0000:0000:0000:0000:0000:0000:0000:0001", true)] // 展开格式现在应该匹配
    [InlineData("2001:db8::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("127.0.0.1", false)]  // IPv4回环
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsLocalIp_VariousAddresses_ReturnsExpected(string ip, bool expected)
    {
        // Act
        var result = IPv6Validator.IsLocalIp(ip);

        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"IsLocalIp('{ip}'): {result}");
    }

    #endregion

    #region IsInnerIp 测试

    /// <summary>
    /// 测试 - IsInnerIp - 内网地址判断
    /// </summary>
    [Theory]
    [InlineData("::1", true)]                    // 回环
    [InlineData("fe80::1", true)]               // 链路本地
    [InlineData("fc00::1", true)]               // ULA
    [InlineData("fd00::1", true)]               // ULA
    [InlineData("fec0::1", true)]               // 站点本地（已废弃）
    [InlineData("2001:db8::1", true)]           // 文档用途
    [InlineData("::ffff:192.168.1.1", true)]   // IPv4映射私有IP
    [InlineData("::ffff:10.0.0.1", true)]      // IPv4映射私有IP
    [InlineData("::ffff:172.16.0.1", true)]    // IPv4映射私有IP
    [InlineData("::ffff:127.0.0.1", true)]     // IPv4映射回环
    [InlineData("2001:4860:4860::8888", false)] // 全局单播
    [InlineData("::ffff:8.8.8.8", false)]      // IPv4映射公网IP
    [InlineData("2606:4700:4700::1111", false)] // Cloudflare DNS
    public void IsInnerIp_VariousAddresses_ReturnsExpected(string ipv6, bool expected)
    {
        // Act
        var result = IPv6Validator.IsInnerIp(ipv6);

        // Assert
        result.ShouldBe(expected, $"'{ipv6}' 内网判断应该返回 {expected}");
        Output.WriteLine($"IsInnerIp('{ipv6}'): {result}");
    }

    #endregion

    #region 地址类型判断测试

    /// <summary>
    /// 测试 - IsGlobalUnicast - 全局单播地址判断
    /// </summary>
    [Theory]
    [InlineData("2001:4860:4860::8888", true)]  // Google DNS
    [InlineData("2606:4700:4700::1111", true)]  // Cloudflare DNS
    [InlineData("2400:3200::1", true)]          // Alibaba DNS
    [InlineData("fe80::1", false)]              // 链路本地
    [InlineData("fc00::1", false)]              // ULA
    [InlineData("::1", false)]                  // 回环
    [InlineData("ff02::1", false)]              // 组播
    [InlineData("2001:db8::1", false)]          // 文档用途
    public void IsGlobalUnicast_VariousAddresses_ReturnsExpected(string ipv6, bool expected)
    {
        // Act
        var result = IPv6Validator.IsGlobalUnicast(ipv6);

        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"IsGlobalUnicast('{ipv6}'): {result}");
    }

    /// <summary>
    /// 测试 - IsMulticast - 组播地址判断
    /// </summary>
    [Theory]
    [InlineData("ff02::1", true)]               // 链路本地组播
    [InlineData("ff05::1", true)]               // 站点本地组播
    [InlineData("ff0e::1", true)]               // 全局组播
    [InlineData("ff00::", true)]                // 组播范围开始
    [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", true)] // 组播范围结束
    [InlineData("2001:db8::1", false)]          // 单播
    [InlineData("fe80::1", false)]              // 链路本地单播
    [InlineData("::1", false)]                  // 回环
    public void IsMulticast_VariousAddresses_ReturnsExpected(string ipv6, bool expected)
    {
        // Act
        var result = IPv6Validator.IsMulticast(ipv6);

        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"IsMulticast('{ipv6}'): {result}");
    }

    /// <summary>
    /// 测试 - IsLinkLocal - 链路本地地址判断
    /// </summary>
    [Theory]
    [InlineData("fe80::1", true)]
    [InlineData("fe80::", true)]
    [InlineData("fe80::abcd:1234", true)]
    [InlineData("febf:ffff:ffff:ffff:ffff:ffff:ffff:ffff", true)] // 链路本地范围上限
    [InlineData("fe7f:ffff:ffff:ffff:ffff:ffff:ffff:ffff", false)] // 超出范围
    [InlineData("fec0::1", false)]              // 站点本地，不是链路本地
    [InlineData("2001:db8::1", false)]
    [InlineData("fc00::1", false)]
    [InlineData("::1", false)]
    public void IsLinkLocal_VariousAddresses_ReturnsExpected(string ipv6, bool expected)
    {
        // Act
        var result = IPv6Validator.IsLinkLocal(ipv6);

        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"IsLinkLocal('{ipv6}'): {result}");
    }

    /// <summary>
    /// 测试 - IsUniqueLocal - 唯一本地地址判断
    /// </summary>
    [Theory]
    [InlineData("fc00::1", true)]
    [InlineData("fd00::1", true)]
    [InlineData("fc12:3456:789a::1", true)]
    [InlineData("fd12:3456:789a::1", true)]
    [InlineData("fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", true)]
    [InlineData("fe00::1", false)]              // 超出ULA范围
    [InlineData("fb00::1", false)]              // 超出ULA范围
    [InlineData("fe80::1", false)]              // 链路本地
    [InlineData("2001:db8::1", false)]
    public void IsUniqueLocal_VariousAddresses_ReturnsExpected(string ipv6, bool expected)
    {
        // Act
        var result = IPv6Validator.IsUniqueLocal(ipv6);

        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"IsUniqueLocal('{ipv6}'): {result}");
    }

    /// <summary>
    /// 测试 - IsDocumentation - 文档用途地址判断
    /// </summary>
    [Theory]
    [InlineData("2001:db8::1", true)]
    [InlineData("2001:db8::", true)]
    [InlineData("2001:db8:1234:5678::1", true)]
    [InlineData("2001:db8:ffff:ffff:ffff:ffff:ffff:ffff", true)]
    [InlineData("2001:db7:ffff:ffff:ffff:ffff:ffff:ffff", false)]
    [InlineData("2001:db9::", false)]
    [InlineData("2001:4860:4860::8888", false)]
    [InlineData("fe80::1", false)]
    public void IsDocumentation_VariousAddresses_ReturnsExpected(string ipv6, bool expected)
    {
        // Act
        var result = IPv6Validator.IsDocumentation(ipv6);

        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"IsDocumentation('{ipv6}'): {result}");
    }

    #endregion

    #region IsInRange 测试

    /// <summary>
    /// 测试 - IsInRange - 地址范围检查
    /// </summary>
    [Theory]
    [InlineData("2001:db8::50", "2001:db8::1", "2001:db8::100", true)]
    [InlineData("2001:db8::1", "2001:db8::1", "2001:db8::100", true)]   // 边界值
    [InlineData("2001:db8::100", "2001:db8::1", "2001:db8::100", true)] // 边界值
    [InlineData("2001:db8::200", "2001:db8::1", "2001:db8::100", false)]
    [InlineData("2001:db7::50", "2001:db8::1", "2001:db8::100", false)]
    [InlineData("::1", "::", "::100", true)]
    [InlineData("::101", "::", "::100", false)]
    public void IsInRange_VariousRanges_ReturnsExpected(string address, string start, string end, bool expected)
    {
        // Act
        var result = IPv6Validator.IsInRange(address, start, end);

        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"IsInRange('{address}', '{start}', '{end}'): {result}");
    }

    /// <summary>
    /// 测试 - IsInRange - 无效参数处理
    /// </summary>
    [Theory]
    [InlineData("invalid", "2001:db8::1", "2001:db8::100")]
    [InlineData("2001:db8::50", "invalid", "2001:db8::100")]
    [InlineData("2001:db8::50", "2001:db8::1", "invalid")]
    [InlineData("", "2001:db8::1", "2001:db8::100")]
    [InlineData("2001:db8::50", "", "2001:db8::100")]
    [InlineData("2001:db8::50", "2001:db8::1", "")]
    [InlineData("192.168.1.1", "2001:db8::1", "2001:db8::100")] // IPv4地址
    public void IsInRange_InvalidParameters_ReturnsFalse(string address, string start, string end)
    {
        // Act
        var result = IPv6Validator.IsInRange(address, start, end);

        // Assert
        result.ShouldBeFalse("无效参数应该返回false");
        Output.WriteLine($"IsInRange with invalid params: {result}");
    }

    #endregion

    #region 性能测试

    /// <summary>
    /// 测试 - 验证方法性能比较
    /// </summary>
    [Fact]
    public void ValidationMethods_PerformanceComparison()
    {
        // Arrange
        var testAddresses = new[]
        {
            "2001:db8::1",
            "::1",
            "::",
            "fe80::1",
            "2001:4860:4860::8888",
            "fc00::1",
            "ff02::1",
            "invalid",
            "192.168.1.1"
        };

        const int iterations = 1000;

        // Act & Measure IsValid
        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            foreach (var addr in testAddresses)
            {
                IPv6Validator.IsValid(addr);
            }
        }
        sw1.Stop();

        // Act & Measure IsValidRegex
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            foreach (var addr in testAddresses)
            {
                IPv6Validator.IsValidRegex(addr);
            }
        }
        sw2.Stop();

        // Act & Measure TryValidate
        var sw3 = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            foreach (var addr in testAddresses)
            {
                IPv6Validator.TryValidate(addr, out _, out _);
            }
        }
        sw3.Stop();

        // Assert & Report
        var totalTests = iterations * testAddresses.Length;
        Output.WriteLine($"性能测试结果 ({totalTests} 次验证):");
        Output.WriteLine($"IsValid: {sw1.ElapsedMilliseconds}ms");
        Output.WriteLine($"IsValidRegex: {sw2.ElapsedMilliseconds}ms");
        Output.WriteLine($"TryValidate: {sw3.ElapsedMilliseconds}ms");
        Output.WriteLine($"Regex/Standard 比率: {(double)sw2.ElapsedMilliseconds / sw1.ElapsedMilliseconds:F2}x");
        Output.WriteLine($"TryValidate/Standard 比率: {(double)sw3.ElapsedMilliseconds / sw1.ElapsedMilliseconds:F2}x");

        // 通常IPAddress.TryParse应该比正则表达式快
        sw1.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(sw2.ElapsedMilliseconds, "IPAddress.TryParse 通常应该比正则表达式更快或相等");
    }

    #endregion

    #region 边界情况测试

    /// <summary>
    /// 测试 - 特殊IPv6地址格式
    /// </summary>
    [Theory]
    [InlineData("2001:db8:0:0:1:0:0:1")]        // 无压缩
    [InlineData("2001:0db8:0000:0000:0001:0000:0000:0001")] // 完全展开
    [InlineData("2001:db8::1")]                 // 标准压缩
    [InlineData("2001:db8:0::1")]               // 部分压缩
    [InlineData("::ffff:192.168.1.1")]         // IPv4映射
    [InlineData("::1")]                         // 回环压缩
    [InlineData("::")]                          // 全零压缩
    public void IsValid_SpecialFormats_AllValid(string ipv6)
    {
        // Act
        var result = IPv6Validator.IsValid(ipv6);

        // Assert
        result.ShouldBeTrue($"特殊格式 '{ipv6}' 应该是有效的");
        Output.WriteLine($"Special format validation: {ipv6} -> {result}");
    }

    /// <summary>
    /// 测试 - 所有地址类型验证方法的一致性
    /// </summary>
    [Theory]
    [InlineData("::1")]                    // 回环
    [InlineData("fe80::1")]               // 链路本地
    [InlineData("fc00::1")]               // ULA
    [InlineData("ff02::1")]               // 组播
    [InlineData("2001:db8::1")]           // 文档
    [InlineData("2001:4860:4860::8888")]  // 全局单播
    [InlineData("::ffff:192.168.1.1")]   // IPv4映射
    public void AddressTypeValidation_Consistency_OnlyOneTypeShouldBeTrue(string ipv6)
    {
        // Act
        var isLoopback = IPv6Validator.IsLocalIp(ipv6);
        var isLinkLocal = IPv6Validator.IsLinkLocal(ipv6);
        var isULA = IPv6Validator.IsUniqueLocal(ipv6);
        var isMulticast = IPv6Validator.IsMulticast(ipv6);
        var isDocumentation = IPv6Validator.IsDocumentation(ipv6);
        var isGlobalUnicast = IPv6Validator.IsGlobalUnicast(ipv6);

        // Count true results
        var trueCount = new[] { isLoopback, isLinkLocal, isULA, isMulticast, isDocumentation, isGlobalUnicast }
            .Count(x => x);

        // Assert - 每个地址应该只属于一种主要类型
        trueCount.ShouldBeLessThanOrEqualTo(1, $"地址 '{ipv6}' 应该最多只属于一种主要类型");

        Output.WriteLine($"Address type analysis for '{ipv6}':");
        Output.WriteLine($"  Loopback: {isLoopback}");
        Output.WriteLine($"  LinkLocal: {isLinkLocal}");
        Output.WriteLine($"  ULA: {isULA}");
        Output.WriteLine($"  Multicast: {isMulticast}");
        Output.WriteLine($"  Documentation: {isDocumentation}");
        Output.WriteLine($"  GlobalUnicast: {isGlobalUnicast}");
    }

    #endregion

    #region 综合测试

    /// <summary>
    /// 测试 - IPv6Validator 与其他IPv6工具的集成
    /// </summary>
    [Fact]
    public void IPv6Validator_IntegrationWithOtherTools()
    {
        // Arrange
        var testAddresses = new[]
        {
            "2001:db8::1",
            "fe80::1",
            "::1",
            "fc00::1",
            "ff02::1"
        };

        foreach (var ipv6 in testAddresses)
        {
            // Act & Assert - 验证器认为有效的地址，其他工具也应该能处理
            if (IPv6Validator.IsValid(ipv6))
            {
                // 应该能被转换器处理
                Should.NotThrow(() => IPv6Converter.ToBytes(ipv6), $"转换器应该能处理 '{ipv6}'");
                Should.NotThrow(() => IPv6Converter.Expand(ipv6), $"展开方法应该能处理 '{ipv6}'");
                Should.NotThrow(() => IPv6Converter.Compress(ipv6), $"压缩方法应该能处理 '{ipv6}'");

                // TryValidate 应该成功
                var tryValidateResult = IPv6Validator.TryValidate(ipv6, out var address, out var addressType);
                tryValidateResult.ShouldBeTrue($"TryValidate 应该成功处理 '{ipv6}'");
                address.ShouldNotBeNull();
                addressType.ShouldNotBe(IPv6AddressType.Invalid);

                Output.WriteLine($"Integration test passed for '{ipv6}' (Type: {addressType})");
            }
        }
    }

    #endregion
}