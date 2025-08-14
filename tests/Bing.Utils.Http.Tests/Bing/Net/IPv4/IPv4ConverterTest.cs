using Bing.Tests;

namespace Bing.Net.IPv4;

/// <summary>
/// IPv4地址转换器 测试
/// </summary>
[Trait("Bing.Net", "IpConverter")]
public class IPv4ConverterTest : TestBase
{
    /// <inheritdoc />
    public IPv4ConverterTest(ITestOutputHelper output) : base(output)
    {
    }

    #region IpToUInt32 测试

    /// <summary>
    /// 测试 - IpToUInt32 - 标准IPv4地址转换
    /// </summary>
    [Theory]
    [InlineData("0.0.0.0", 0u)]
    [InlineData("0.0.0.1", 1u)]
    [InlineData("192.168.1.1", 3232235777u)]
    [InlineData("255.255.255.255", 4294967295u)]
    [InlineData("127.0.0.1", 2130706433u)]
    [InlineData("10.0.0.1", 167772161u)]
    [InlineData("172.16.0.1", 2886729729u)]
    public void IpToUInt32_ValidIPv4_ReturnsCorrectNumber(string ip, uint expected)
    {
        // Act
        var result = IPv4Converter.IpToUInt32(ip);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IpToUInt32 - 空或null输入抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IpToUInt32_NullOrEmptyInput_ThrowsArgumentException(string ip)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4Converter.IpToUInt32(ip))
            .ParamName.ShouldBe("ipAddress");
    }

    /// <summary>
    /// 测试 - IpToUInt32 - 无效IPv4地址抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("256.1.1.1")]
    //[InlineData("192.168.1")]
    [InlineData("192.168.1.1.1")]
    [InlineData("2001:db8::1")]
    //[InlineData("192.168.01.1")]
    [InlineData("192.168.-1.1")]
    public void IpToUInt32_InvalidIPv4_ThrowsArgumentException(string invalidIp)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => IPv4Converter.IpToUInt32(invalidIp));
        exception.ParamName.ShouldBe("ipAddress");
        exception.Message.ShouldContain("无效的IPv4地址格式");
    }

    #endregion

    #region UInt32ToIp 测试

    /// <summary>
    /// 测试 - UInt32ToIp - 数值转换为IPv4地址
    /// </summary>
    [Theory]
    [InlineData(0u, "0.0.0.0")]
    [InlineData(1u, "0.0.0.1")]
    [InlineData(3232235777u, "192.168.1.1")]
    [InlineData(4294967295u, "255.255.255.255")]
    [InlineData(2130706433u, "127.0.0.1")]
    [InlineData(167772161u, "10.0.0.1")]
    [InlineData(2886729729u, "172.16.0.1")]
    public void UInt32ToIp_ValidNumber_ReturnsCorrectIP(uint ipNum, string expected)
    {
        // Act
        var result = IPv4Converter.UInt32ToIp(ipNum);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - UInt32ToIp - 边界值处理
    /// </summary>
    [Fact]
    public void UInt32ToIp_BoundaryValues_HandlesCorrectly()
    {
        // Act & Assert
        IPv4Converter.UInt32ToIp(uint.MinValue).ShouldBe("0.0.0.0");
        IPv4Converter.UInt32ToIp(uint.MaxValue).ShouldBe("255.255.255.255");
    }

    #endregion

    #region 往返转换 测试

    /// <summary>
    /// 测试 - 往返转换 - IP地址与数值互转一致性
    /// </summary>
    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("127.0.0.1")]
    [InlineData("8.8.8.8")]
    public void RoundTripConversion_IPToNumberToIP_MaintainsConsistency(string originalIp)
    {
        // Act
        var number = IPv4Converter.IpToUInt32(originalIp);
        var convertedIp = IPv4Converter.UInt32ToIp(number);

        // Assert
        convertedIp.ShouldBe(originalIp);
    }

    /// <summary>
    /// 测试 - 往返转换 - 数值与IP地址互转一致性
    /// </summary>
    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(3232235777u)]
    [InlineData(4294967295u)]
    [InlineData(2130706433u)]
    public void RoundTripConversion_NumberToIPToNumber_MaintainsConsistency(uint originalNumber)
    {
        // Act
        var ip = IPv4Converter.UInt32ToIp(originalNumber);
        var convertedNumber = IPv4Converter.IpToUInt32(ip);

        // Assert
        convertedNumber.ShouldBe(originalNumber);
    }

    #endregion

    #region GetNetworkAddress 测试

    /// <summary>
    /// 测试 - GetNetworkAddress - 标准网络地址计算
    /// </summary>
    [Theory]
    [InlineData("192.168.1.100", "255.255.255.0", "192.168.1.0")]
    [InlineData("10.1.2.3", "255.0.0.0", "10.0.0.0")]
    [InlineData("172.16.5.4", "255.255.0.0", "172.16.0.0")]
    [InlineData("192.168.1.254", "255.255.255.252", "192.168.1.252")]
    [InlineData("10.10.10.10", "255.255.255.255", "10.10.10.10")]
    [InlineData("192.168.1.1", "0.0.0.0", "0.0.0.0")]
    public void GetNetworkAddress_ValidInputs_ReturnsCorrectNetwork(string ip, string mask, string expected)
    {
        // Act
        var result = IPv4Converter.GetNetworkAddress(ip, mask);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - GetNetworkAddress - 空或null参数抛出异常
    /// </summary>
    [Theory]
    [InlineData(null, "255.255.255.0")]
    [InlineData("", "255.255.255.0")]
    [InlineData("   ", "255.255.255.0")]
    [InlineData("192.168.1.100", null)]
    [InlineData("192.168.1.100", "")]
    [InlineData("192.168.1.100", "   ")]
    public void GetNetworkAddress_NullOrEmptyParams_ThrowsArgumentException(string ip, string mask)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => IPv4Converter.GetNetworkAddress(ip, mask));
        exception.ParamName.ShouldNotBeNull();
    }

    /// <summary>
    /// 测试 - GetNetworkAddress - 无效IP地址或掩码抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid", "255.255.255.0")]
    [InlineData("256.1.1.1", "255.255.255.0")]
    [InlineData("192.168.1.100", "invalid")]
    [InlineData("192.168.1.100", "256.255.255.0")]
    [InlineData("2001:db8::1", "255.255.255.0")]
    [InlineData("192.168.1.100", "2001:db8::1")]
    public void GetNetworkAddress_InvalidParams_ThrowsArgumentException(string ip, string mask)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4Converter.GetNetworkAddress(ip, mask))
            .Message.ShouldContain("IP地址或子网掩码格式无效");
    }

    #endregion

    #region GetBroadcastAddress 测试

    /// <summary>
    /// 测试 - GetBroadcastAddress - 标准广播地址计算
    /// </summary>
    [Theory]
    [InlineData("192.168.1.100", "255.255.255.0", "192.168.1.255")]
    [InlineData("10.1.2.3", "255.0.0.0", "10.255.255.255")]
    [InlineData("172.16.5.4", "255.255.0.0", "172.16.255.255")]
    [InlineData("192.168.1.252", "255.255.255.252", "192.168.1.255")]
    [InlineData("10.10.10.10", "255.255.255.255", "10.10.10.10")]
    [InlineData("192.168.1.1", "0.0.0.0", "255.255.255.255")]
    public void GetBroadcastAddress_ValidInputs_ReturnsCorrectBroadcast(string ip, string mask, string expected)
    {
        // Act
        var result = IPv4Converter.GetBroadcastAddress(ip, mask);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - GetBroadcastAddress - 空或null参数抛出异常
    /// </summary>
    [Theory]
    [InlineData(null, "255.255.255.0")]
    [InlineData("", "255.255.255.0")]
    [InlineData("   ", "255.255.255.0")]
    [InlineData("192.168.1.100", null)]
    [InlineData("192.168.1.100", "")]
    [InlineData("192.168.1.100", "   ")]
    public void GetBroadcastAddress_NullOrEmptyParams_ThrowsArgumentException(string ip, string mask)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => IPv4Converter.GetBroadcastAddress(ip, mask));
        exception.ParamName.ShouldNotBeNull();
    }

    /// <summary>
    /// 测试 - GetBroadcastAddress - 无效IP地址或掩码抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid", "255.255.255.0")]
    [InlineData("256.1.1.1", "255.255.255.0")]
    [InlineData("192.168.1.100", "invalid")]
    [InlineData("192.168.1.100", "256.255.255.0")]
    public void GetBroadcastAddress_InvalidParams_ThrowsArgumentException(string ip, string mask)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4Converter.GetBroadcastAddress(ip, mask))
            .Message.ShouldContain("IP地址或子网掩码格式无效");
    }

    #endregion

    #region 集成测试

    /// <summary>
    /// 测试 - 集成 - 网络地址和广播地址计算一致性
    /// </summary>
    [Theory]
    [InlineData("192.168.1.100", "255.255.255.0")]
    [InlineData("10.1.2.3", "255.0.0.0")]
    [InlineData("172.16.5.4", "255.255.0.0")]
    public void Integration_NetworkAndBroadcastCalculation_IsConsistent(string ip, string mask)
    {
        // Act
        var network = IPv4Converter.GetNetworkAddress(ip, mask);
        var broadcast = IPv4Converter.GetBroadcastAddress(ip, mask);

        // Assert
        var networkNum = IPv4Converter.IpToUInt32(network);
        var broadcastNum = IPv4Converter.IpToUInt32(broadcast);
        var ipNum = IPv4Converter.IpToUInt32(ip);

        // IP地址应该在网络地址和广播地址之间
        ipNum.ShouldBeGreaterThanOrEqualTo(networkNum);
        ipNum.ShouldBeLessThanOrEqualTo(broadcastNum);

        // 网络地址应该小于等于广播地址
        networkNum.ShouldBeLessThanOrEqualTo(broadcastNum);
    }

    #endregion

    #region 边界和性能测试

    /// <summary>
    /// 测试 - 边界情况 - 特殊子网掩码处理
    /// </summary>
    [Fact]
    public void EdgeCase_SpecialSubnetMasks_HandlesCorrectly()
    {
        // 全0掩码
        var network1 = IPv4Converter.GetNetworkAddress("192.168.1.100", "0.0.0.0");
        network1.ShouldBe("0.0.0.0");

        var broadcast1 = IPv4Converter.GetBroadcastAddress("192.168.1.100", "0.0.0.0");
        broadcast1.ShouldBe("255.255.255.255");

        // 全1掩码（主机路由）
        var network2 = IPv4Converter.GetNetworkAddress("192.168.1.100", "255.255.255.255");
        network2.ShouldBe("192.168.1.100");

        var broadcast2 = IPv4Converter.GetBroadcastAddress("192.168.1.100", "255.255.255.255");
        broadcast2.ShouldBe("192.168.1.100");
    }

    /// <summary>
    /// 测试 - 性能 - 大量IP转换操作
    /// </summary>
    [Fact]
    public void Performance_MassiveIPConversions_CompletesQuickly()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const int iterations = 10000;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var ip = $"192.168.{i % 256}.{(i * 7) % 256}";
            var num = IPv4Converter.IpToUInt32(ip);
            var convertedIp = IPv4Converter.UInt32ToIp(num);

            // 验证往返转换一致性
            convertedIp.ShouldBe(ip);
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000, "性能测试超时");
    }

    /// <summary>
    /// 测试 - IPv4地址范围覆盖
    /// </summary>
    [Fact]
    public void EdgeCase_IPv4AddressRangeCoverage_WorksCorrectly()
    {
        // 测试所有四个字节的边界值
        var testCases = new[]
        {
            ("0.0.0.0", 0u),
            ("0.0.0.255", 255u),
            ("0.0.255.0", 65280u),
            ("0.255.0.0", 16711680u),
            ("255.0.0.0", 4278190080u),
            ("255.255.255.255", 4294967295u)
        };

        foreach (var (ip, expectedNum) in testCases)
        {
            var num = IPv4Converter.IpToUInt32(ip);
            num.ShouldBe(expectedNum, $"IP {ip} 转换失败");

            var convertedIp = IPv4Converter.UInt32ToIp(num);
            convertedIp.ShouldBe(ip, $"数值 {num} 转换失败");
        }
    }

    #endregion
}