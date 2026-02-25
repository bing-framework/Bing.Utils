namespace Bing.Net.IPv4;
/// <summary>
/// IPv4地址验证器 测试
/// </summary>
[Trait("Bing.Net", "IpValidator")]
public class IPv4ValidatorTest : TestBase
{
    /// <inheritdoc />
    public IPv4ValidatorTest(ITestOutputHelper output) : base(output)
    {
    }
    #region IsValid 测试
    /// <summary>
    /// 测试 - IsValid - 有效IPv4地址验证
    /// </summary>
    [Theory]
    [InlineData("0.0.0.0", true)]
    [InlineData("192.168.1.1", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("255.255.255.255", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("8.8.8.8", true)]
    [InlineData("114.114.114.114", true)]
    [InlineData("1.1.1.1", true)]
    [InlineData("208.67.222.222", true)]
    public void IsValid_ValidIPv4Addresses_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsValid(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsValid - 无效IPv4地址验证
    /// </summary>
    [Theory]
    [InlineData("256.1.1.1", false)]       // 超出范围
    [InlineData("192.168.1", false)]       // 缺少段
    [InlineData("192.168.1.1.1", false)]   // 过多段
    [InlineData("invalid", false)]         // 非数字
    [InlineData("2001:db8::1", false)]     // IPv6地址
    [InlineData("192.168.01.1", false)]    // 前导零 - 应该无效
    [InlineData("192.168.1.01", false)]    // 前导零 - 应该无效
    [InlineData("192.168.001.1", false)]   // 前导零 - 应该无效
    [InlineData("01.1.1.1", false)]       // 前导零 - 应该无效
    [InlineData("192.168.-1.1", false)]    // 负数
    [InlineData("192.168.1.", false)]      // 末尾有点
    [InlineData(".192.168.1.1", false)]    // 开头有点
    [InlineData("192..168.1.1", false)]    // 连续的点
    [InlineData("", false)]                // 空字符串
    [InlineData(null, false)]              // null
    [InlineData("   ", false)]             // 空白字符
    public void IsValid_InvalidIPv4Addresses_ReturnsFalse(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsValid(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsValid - 边界值验证
    /// </summary>
    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("255.255.255.255")]
    [InlineData("0.0.0.255")]
    [InlineData("255.0.0.0")]
    public void IsValid_BoundaryValues_ReturnsTrue(string ip)
    {
        // Act
        var result = IPv4Validator.IsValid(ip);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - IsValid - 前导零详细测试
    /// </summary>
    [Theory]
    [InlineData("0.0.0.0", true)]          // 单独的0是有效的
    [InlineData("01.1.1.1", false)]        // 前导零无效
    [InlineData("1.01.1.1", false)]        // 前导零无效
    [InlineData("1.1.01.1", false)]        // 前导零无效
    [InlineData("1.1.1.01", false)]        // 前导零无效
    [InlineData("192.168.001.1", false)]   // 多位前导零无效
    [InlineData("192.168.010.1", false)]   // 前导零无效
    [InlineData("192.168.100.1", true)]    // 无前导零有效
    public void IsValid_LeadingZerosCases_ReturnsExpected(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsValid(ip);
        // Assert
        result.ShouldBe(expected, $"IP地址 '{ip}' 的验证结果应该是 {expected}");
    }
    #endregion
    #region IsLocalIp 测试
    /// <summary>
    /// 测试 - IsLocalIp - 回环地址验证
    /// </summary>
    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("127.0.0.2", true)]
    [InlineData("127.1.1.1", true)]
    [InlineData("127.255.255.255", true)]
    [InlineData("127.0.0.0", true)]
    public void IsLocalIp_LoopbackAddresses_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsLocalIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsLocalIp - 非回环地址验证
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", false)]
    [InlineData("10.0.0.1", false)]
    [InlineData("8.8.8.8", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("128.0.0.1", false)]
    [InlineData("126.255.255.255", false)]
    public void IsLocalIp_NonLoopbackAddresses_ReturnsFalse(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsLocalIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsLocalIp - 无效输入处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("127")]
    [InlineData("127.")]
    [InlineData("127.01.1.1")]  // 前导零使整个地址无效
    public void IsLocalIp_InvalidInput_ReturnsFalse(string ip)
    {
        // Act
        var result = IPv4Validator.IsLocalIp(ip);
        // Assert
        result.ShouldBeFalse();
    }
    #endregion
    #region IsInnerIp 测试
    /// <summary>
    /// 测试 - IsInnerIp - A类私有地址验证
    /// </summary>
    [Theory]
    [InlineData("10.0.0.0", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("10.255.255.255", true)]
    [InlineData("10.1.2.3", true)]
    public void IsInnerIp_ClassAPrivateAddresses_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInnerIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsInnerIp - B类私有地址验证
    /// </summary>
    [Theory]
    [InlineData("172.16.0.0", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("172.20.1.1", true)]
    public void IsInnerIp_ClassBPrivateAddresses_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInnerIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsInnerIp - C类私有地址验证
    /// </summary>
    [Theory]
    [InlineData("192.168.0.0", true)]
    [InlineData("192.168.1.1", true)]
    [InlineData("192.168.255.255", true)]
    [InlineData("192.168.100.200", true)]
    public void IsInnerIp_ClassCPrivateAddresses_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInnerIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsInnerIp - 链路本地地址验证
    /// </summary>
    [Theory]
    [InlineData("169.254.0.0", true)]
    [InlineData("169.254.1.1", true)]
    [InlineData("169.254.255.255", true)]
    public void IsInnerIp_LinkLocalAddresses_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInnerIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsInnerIp - 回环地址验证
    /// </summary>
    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("127.0.0.2", true)]
    [InlineData("127.255.255.255", true)]
    public void IsInnerIp_LoopbackAddresses_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInnerIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsInnerIp - 公网地址验证
    /// </summary>
    [Theory]
    [InlineData("8.8.8.8", false)]
    [InlineData("114.114.114.114", false)]
    [InlineData("1.1.1.1", false)]
    [InlineData("208.67.222.222", false)]
    [InlineData("4.4.4.4", false)]
    [InlineData("9.255.255.255", false)]
    [InlineData("11.0.0.0", false)]
    [InlineData("172.15.255.255", false)]
    [InlineData("172.32.0.0", false)]
    [InlineData("192.167.255.255", false)]
    [InlineData("192.169.0.0", false)]
    public void IsInnerIp_PublicAddresses_ReturnsFalse(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInnerIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsInnerIp - 无效输入处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("256.1.1.1")]
    [InlineData("2001:db8::1")]
    public void IsInnerIp_InvalidInput_ReturnsFalse(string ip)
    {
        // Act
        var result = IPv4Validator.IsInnerIp(ip);
        // Assert
        result.ShouldBeFalse();
    }
    #endregion
    #region IsInRange 测试
    /// <summary>
    /// 测试 - IsInRange - 在范围内的地址
    /// </summary>
    [Theory]
    [InlineData("192.168.1.100", "192.168.1.1", "192.168.1.254", true)]
    [InlineData("192.168.1.1", "192.168.1.1", "192.168.1.254", true)]
    [InlineData("192.168.1.254", "192.168.1.1", "192.168.1.254", true)]
    [InlineData("192.168.1.127", "192.168.1.1", "192.168.1.254", true)]
    [InlineData("10.0.0.1", "10.0.0.0", "10.0.0.255", true)]
    public void IsInRange_AddressInRange_ReturnsTrue(string ip, string start, string end, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInRange(ip, start, end);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsInRange - 不在范围内的地址
    /// </summary>
    [Theory]
    [InlineData("192.168.2.1", "192.168.1.1", "192.168.1.254", false)]
    [InlineData("192.168.0.255", "192.168.1.1", "192.168.1.254", false)]
    [InlineData("192.168.1.0", "192.168.1.1", "192.168.1.254", false)]
    [InlineData("192.168.1.255", "192.168.1.1", "192.168.1.254", false)]
    [InlineData("10.0.1.0", "10.0.0.0", "10.0.0.255", false)]
    public void IsInRange_AddressNotInRange_ReturnsFalse(string ip, string start, string end, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInRange(ip, start, end);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsInRange - 起始和结束IP顺序颠倒
    /// </summary>
    [Theory]
    [InlineData("192.168.1.100", "192.168.1.254", "192.168.1.1")]
    [InlineData("10.0.0.128", "10.0.0.255", "10.0.0.0")]
    [InlineData("172.16.0.50", "172.16.0.100", "172.16.0.10")]
    [InlineData("192.168.1.5", "192.168.1.10", "192.168.1.1")]
    public void IsInRange_ReversedStartEnd_HandlesCorrectly(string ip, string start, string end)
    {
        // Act
        var result = IPv4Validator.IsInRange(ip, start, end);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - IsInRange - 颠倒范围但IP不在范围内
    /// </summary>
    [Theory]
    [InlineData("192.168.2.100", "192.168.1.254", "192.168.1.1", false)]
    [InlineData("10.0.1.128", "10.0.0.255", "10.0.0.0", false)]
    [InlineData("172.15.0.50", "172.16.0.100", "172.16.0.10", false)]
    public void IsInRange_ReversedStartEndButNotInRange_ReturnsFalse(string ip, string start, string end, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInRange(ip, start, end);
        // Assert
        result.ShouldBe(expected, $"IP {ip} 不应该在范围 {start}-{end} 内");
    }
    /// <summary>
    /// 测试 - IsInRange - 无效输入处理
    /// </summary>
    [Theory]
    [InlineData(null, "192.168.1.1", "192.168.1.254")]
    [InlineData("192.168.1.100", null, "192.168.1.254")]
    [InlineData("192.168.1.100", "192.168.1.1", null)]
    [InlineData("", "192.168.1.1", "192.168.1.254")]
    [InlineData("192.168.1.100", "", "192.168.1.254")]
    [InlineData("192.168.1.100", "192.168.1.1", "")]
    [InlineData("invalid", "192.168.1.1", "192.168.1.254")]
    [InlineData("192.168.1.100", "invalid", "192.168.1.254")]
    [InlineData("192.168.1.100", "192.168.1.1", "invalid")]
    [InlineData("192.168.01.100", "192.168.1.1", "192.168.1.254")]  // 前导零使地址无效
    [InlineData("192.168.1.100", "192.168.01.1", "192.168.1.254")]  // 前导零使地址无效
    [InlineData("192.168.1.100", "192.168.1.1", "192.168.01.254")]  // 前导零使地址无效
    public void IsInRange_InvalidInput_ReturnsFalse(string ip, string start, string end)
    {
        // Act
        var result = IPv4Validator.IsInRange(ip, start, end);
        // Assert
        result.ShouldBeFalse();
    }
    #endregion
    #region IsInSameSubnet 测试
    /// <summary>
    /// 测试 - IsInSameSubnet - 同子网地址验证
    /// </summary>
    [Theory]
    [InlineData("192.168.1.10", "192.168.1.20", "255.255.255.0", true)]
    [InlineData("192.168.1.1", "192.168.1.254", "255.255.255.0", true)]
    [InlineData("10.0.0.1", "10.0.0.255", "255.0.0.0", true)]
    [InlineData("172.16.1.1", "172.16.2.1", "255.255.0.0", true)]
    [InlineData("192.168.1.1", "192.168.1.2", "255.255.255.252", true)]
    public void IsInSameSubnet_SameSubnetAddresses_ReturnsTrue(string ip1, string ip2, string mask, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInSameSubnet(ip1, ip2, mask);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsInSameSubnet - 不同子网地址验证
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", "192.168.2.1", "255.255.255.0", false)]
    [InlineData("10.0.0.1", "11.0.0.1", "255.0.0.0", false)]
    [InlineData("172.16.1.1", "172.17.1.1", "255.255.0.0", false)]
    [InlineData("192.168.1.1", "192.168.1.5", "255.255.255.252", false)]
    public void IsInSameSubnet_DifferentSubnetAddresses_ReturnsFalse(string ip1, string ip2, string mask, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInSameSubnet(ip1, ip2, mask);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsInSameSubnet - null或空参数抛出异常
    /// </summary>
    [Theory]
    [InlineData(null, "192.168.1.2", "255.255.255.0")]
    [InlineData("192.168.1.1", null, "255.255.255.0")]
    [InlineData("192.168.1.1", "192.168.1.2", null)]
    [InlineData("", "192.168.1.2", "255.255.255.0")]
    [InlineData("192.168.1.1", "", "255.255.255.0")]
    [InlineData("192.168.1.1", "192.168.1.2", "")]
    public void IsInSameSubnet_NullOrEmptyParams_ThrowsArgumentNullException(string ip1, string ip2, string mask)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => IPv4Validator.IsInSameSubnet(ip1, ip2, mask));
    }
    /// <summary>
    /// 测试 - IsInSameSubnet - 无效参数抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid", "192.168.1.2", "255.255.255.0")]
    [InlineData("192.168.1.1", "invalid", "255.255.255.0")]
    [InlineData("192.168.1.1", "192.168.1.2", "invalid")]
    [InlineData("256.1.1.1", "192.168.1.2", "255.255.255.0")]
    [InlineData("192.168.1.1", "256.1.1.1", "255.255.255.0")]
    [InlineData("192.168.1.1", "192.168.1.2", "256.255.255.0")]
    [InlineData("192.168.01.1", "192.168.1.2", "255.255.255.0")]  // 前导零
    [InlineData("192.168.1.1", "192.168.01.2", "255.255.255.0")]  // 前导零
    [InlineData("192.168.1.1", "192.168.1.2", "255.255.255.01")]  // 前导零
    public void IsInSameSubnet_InvalidParams_ThrowsArgumentException(string ip1, string ip2, string mask)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4Validator.IsInSameSubnet(ip1, ip2, mask))
            .Message.ShouldContain("IP地址或子网掩码格式无效");
    }
    #endregion
    #region IsPublicIp 测试
    /// <summary>
    /// 测试 - IsPublicIp - 公网地址验证
    /// </summary>
    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("114.114.114.114", true)]
    [InlineData("1.1.1.1", true)]
    [InlineData("208.67.222.222", true)]
    [InlineData("4.4.4.4", true)]
    [InlineData("9.255.255.255", true)]
    [InlineData("11.0.0.0", true)]
    [InlineData("172.15.255.255", true)]
    [InlineData("172.32.0.0", true)]
    [InlineData("192.167.255.255", true)]
    [InlineData("192.169.0.0", true)]
    public void IsPublicIp_PublicAddresses_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsPublicIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsPublicIp - 私有地址验证
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", false)]
    [InlineData("10.0.0.1", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("127.0.0.1", false)]
    [InlineData("169.254.1.1", false)]
    public void IsPublicIp_PrivateAddresses_ReturnsFalse(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsPublicIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsPublicIp - 保留地址验证
    /// </summary>
    [Theory]
    [InlineData("0.0.0.0", false)]
    [InlineData("224.0.0.1", false)]
    [InlineData("255.255.255.255", false)]
    [InlineData("240.0.0.1", false)]
    public void IsPublicIp_ReservedAddresses_ReturnsFalse(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsPublicIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IsReservedIp 测试
    /// <summary>
    /// 测试 - IsReservedIp - 本网络地址验证
    /// </summary>
    [Theory]
    [InlineData("0.0.0.0", true)]
    [InlineData("0.0.0.1", true)]
    [InlineData("0.255.255.255", true)]
    public void IsReservedIp_ThisNetworkAddresses_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsReservedIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsReservedIp - 组播地址验证
    /// </summary>
    [Theory]
    [InlineData("224.0.0.0", true)]
    [InlineData("224.0.0.1", true)]
    [InlineData("239.255.255.255", true)]
    [InlineData("230.1.1.1", true)]
    public void IsReservedIp_MulticastAddresses_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsReservedIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsReservedIp - 实验性地址验证
    /// </summary>
    [Theory]
    [InlineData("240.0.0.0", true)]
    [InlineData("240.0.0.1", true)]
    [InlineData("255.255.255.255", true)]
    [InlineData("250.1.1.1", true)]
    public void IsReservedIp_ExperimentalAddresses_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsReservedIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsReservedIp - 普通地址验证
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", false)]
    [InlineData("8.8.8.8", false)]
    [InlineData("127.0.0.1", false)]
    [InlineData("1.1.1.1", false)]
    [InlineData("223.255.255.255", false)]
    public void IsReservedIp_NormalAddresses_ReturnsFalse(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsReservedIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IsMulticastIp 测试
    /// <summary>
    /// 测试 - IsMulticastIp - 组播地址验证
    /// </summary>
    [Theory]
    [InlineData("224.0.0.0", true)]
    [InlineData("224.0.0.1", true)]
    [InlineData("239.255.255.255", true)]
    [InlineData("230.1.1.1", true)]
    [InlineData("224.0.1.1", true)]
    [InlineData("239.0.0.1", true)]
    public void IsMulticastIp_MulticastAddresses_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsMulticastIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsMulticastIp - 非组播地址验证
    /// </summary>
    [Theory]
    [InlineData("223.255.255.255", false)]
    [InlineData("240.0.0.0", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("8.8.8.8", false)]
    [InlineData("127.0.0.1", false)]
    public void IsMulticastIp_NonMulticastAddresses_ReturnsFalse(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsMulticastIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region IsLinkLocalIp 测试
    /// <summary>
    /// 测试 - IsLinkLocalIp - 链路本地地址验证
    /// </summary>
    [Theory]
    [InlineData("169.254.0.0", true)]
    [InlineData("169.254.1.1", true)]
    [InlineData("169.254.255.255", true)]
    [InlineData("169.254.128.1", true)]
    public void IsLinkLocalIp_LinkLocalAddresses_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsLinkLocalIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsLinkLocalIp - 非链路本地地址验证
    /// </summary>
    [Theory]
    [InlineData("169.253.255.255", false)]
    [InlineData("169.255.0.0", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("8.8.8.8", false)]
    [InlineData("127.0.0.1", false)]
    public void IsLinkLocalIp_NonLinkLocalAddresses_ReturnsFalse(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsLinkLocalIp(ip);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 集成和边界测试
    /// <summary>
    /// 测试 - 集成 - 地址类型分类一致性
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1")]  // 私有地址
    [InlineData("8.8.8.8")]      // 公网地址
    [InlineData("127.0.0.1")]    // 回环地址
    [InlineData("169.254.1.1")]  // 链路本地地址
    [InlineData("224.0.0.1")]    // 组播地址
    [InlineData("0.0.0.0")]      // 保留地址
    public void Integration_AddressClassification_IsConsistent(string ip)
    {
        // Act
        var isValid = IPv4Validator.IsValid(ip);
        var isInner = IPv4Validator.IsInnerIp(ip);
        var isPublic = IPv4Validator.IsPublicIp(ip);
        var isLocal = IPv4Validator.IsLocalIp(ip);
        var isReserved = IPv4Validator.IsReservedIp(ip);
        var isMulticast = IPv4Validator.IsMulticastIp(ip);
        var isLinkLocal = IPv4Validator.IsLinkLocalIp(ip);
        // Assert - 基本一致性检查
        isValid.ShouldBeTrue($"IP {ip} 应该是有效的");
        // 公网地址不应该是内网地址
        if (isPublic)
        {
            isInner.ShouldBeFalse($"公网地址 {ip} 不应该是内网地址");
            isReserved.ShouldBeFalse($"公网地址 {ip} 不应该是保留地址");
        }
        // 内网地址不应该是公网地址
        if (isInner)
        {
            isPublic.ShouldBeFalse($"内网地址 {ip} 不应该是公网地址");
        }
        // 保留地址不应该是公网地址
        if (isReserved)
        {
            isPublic.ShouldBeFalse($"保留地址 {ip} 不应该是公网地址");
        }
        // 组播地址应该是保留地址
        if (isMulticast)
        {
            isReserved.ShouldBeTrue($"组播地址 {ip} 应该是保留地址");
        }
        // 链路本地地址应该是内网地址
        if (isLinkLocal)
        {
            isInner.ShouldBeTrue($"链路本地地址 {ip} 应该是内网地址");
        }
        // 回环地址应该是内网地址
        if (isLocal)
        {
            isInner.ShouldBeTrue($"回环地址 {ip} 应该是内网地址");
        }
    }
    /// <summary>
    /// 测试 - 性能 - 大量地址验证操作
    /// </summary>
    [Fact]
    public void Performance_MassiveValidation_CompletesQuickly()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const int iterations = 10000;
        // Act
        for (int i = 0; i < iterations; i++)
        {
            var ip = $"192.168.{i % 256}.{(i * 7) % 256}";
            IPv4Validator.IsValid(ip);
            IPv4Validator.IsInnerIp(ip);
            IPv4Validator.IsPublicIp(ip);
            IPv4Validator.IsLocalIp(ip);
        }
        stopwatch.Stop();
        // Assert
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(2000, "性能测试超时");
    }
    /// <summary>
    /// 测试 - 边界情况 - 正则表达式与IPAddress.TryParse一致性
    /// </summary>
    [Fact]
    public void EdgeCase_RegexAndIPAddressParse_Consistency()
    {
        var testCases = new[]
        {
            ("0.0.0.0", true),           // 有效
            ("255.255.255.255", true),  // 有效
            ("192.168.1.1", true),      // 有效
            ("192.168.01.1", false),    // 前导零 - 无效
            ("256.1.1.1", false),       // 超出范围
            ("192.168.1", false),       // 不完整
            ("192.168.1.1.1", false),   // 过多段
            ("", false),                // 空字符串
            (null, false)               // null
        };
        foreach (var (testCase, expectedValid) in testCases)
        {
            var validatorResult = IPv4Validator.IsValid(testCase);
            // Assert
            validatorResult.ShouldBe(expectedValid,
                $"验证结果不一致: {testCase}, 期望: {expectedValid}, 实际: {validatorResult}");
        }
    }
    /// <summary>
    /// 测试 - 边界情况 - 各种前导零格式
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", true)]      // 正常格式
    [InlineData("192.168.01.1", false)]    // 第三段有前导零
    [InlineData("192.168.001.1", false)]   // 第三段有多个前导零
    [InlineData("01.168.1.1", false)]      // 第一段有前导零
    [InlineData("192.01.1.1", false)]      // 第二段有前导零
    [InlineData("192.168.1.01", false)]    // 第四段有前导零
    [InlineData("01.01.01.01", false)]     // 所有段都有前导零
    [InlineData("0.0.0.0", true)]          // 单独的0是有效的
    [InlineData("0.0.0.1", true)]          // 部分0是有效的
    [InlineData("192.168.0.1", true)]      // 单独的0是有效的
    public void EdgeCase_LeadingZeroValidation_WorksCorrectly(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsValid(ip);
        // Assert
        result.ShouldBe(expected, $"IP地址 '{ip}' 的验证应该返回 {expected}");
    }
    #endregion
}
