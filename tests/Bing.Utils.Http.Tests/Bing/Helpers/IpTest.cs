//using Bing.Tests;
//using System.Collections.Concurrent;
//using System.Net.NetworkInformation;
//using System.Numerics;
//using Bing.Net.Cidr;

//namespace Bing.Helpers;

///// <summary>
///// IP地址操作工具类 单元测试
///// </summary>
//[Trait("Bing.Helpers", "Ip")]
//public class IpTest : TestBase
//{
//    /// <inheritdoc />
//    public IpTest(ITestOutputHelper output) : base(output)
//    {
//        // 重置IP状态
//        Ip.Reset();
//    }

//    #region 基础IP操作测试

//    /// <summary>
//    /// 测试 - SetIp - 设置有效IPv4地址
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.1")]
//    [InlineData("10.0.0.1")]
//    [InlineData("172.16.0.1")]
//    [InlineData("127.0.0.1")]
//    [InlineData("8.8.8.8")]
//    public void SetIp_ValidIPv4_SetsSuccessfully(string ip)
//    {
//        // Act
//        Ip.SetIp(ip);

//        // Assert
//        Ip.GetIp().ShouldBe(ip);
//    }

//    /// <summary>
//    /// 测试 - SetIp - 设置有效IPv6地址
//    /// </summary>
//    [Theory]
//    [InlineData("2001:db8::1")]
//    [InlineData("::1")]
//    [InlineData("fe80::1")]
//    [InlineData("2001:4860:4860::8888")]
//    public void SetIp_ValidIPv6_SetsSuccessfully(string ip)
//    {
//        // Act
//        Ip.SetIp(ip);

//        // Assert
//        Ip.GetIp().ShouldBe(ip);
//    }

//    /// <summary>
//    /// 测试 - SetIp - 设置无效IP地址应抛出异常
//    /// </summary>
//    [Theory]
//    [InlineData("invalid")]
//    [InlineData("256.1.1.1")]
//    [InlineData("192.168.1")]        // 缺少最后一个段
//    [InlineData("192.168.1.1.1")]    // 多余的段
//    [InlineData("gggg::1")]
//    [InlineData("192.168")]          // 只有两个段
//    [InlineData("192")]              // 只有一个段
//    [InlineData("192.168.1.")]       // 末尾有点
//    [InlineData(".192.168.1.1")]     // 开头有点
//    [InlineData("192..168.1.1")]     // 连续的点
//    [InlineData("192.168.01.1")]     // 前导零
//    public void SetIp_InvalidIP_ThrowsArgumentException(string invalidIp)
//    {
//        // Act & Assert
//        Should.Throw<ArgumentException>(() => Ip.SetIp(invalidIp))
//            .Message.ShouldContain("无效的IP地址格式");
//    }

//    /// <summary>
//    /// 测试 - SetIp - 设置null或空字符串
//    /// </summary>
//    [Theory]
//    [InlineData(null)]
//    [InlineData("")]
//    [InlineData(" ")]
//    public void SetIp_NullOrEmpty_SetsSuccessfully(string ip)
//    {
//        // Act
//        Should.NotThrow(() => Ip.SetIp(ip));
//    }

//    /// <summary>
//    /// 测试 - Reset - 重置IP地址
//    /// </summary>
//    [Fact]
//    public void Reset_AfterSetIp_ClearsIpValue()
//    {
//        // Arrange
//        Ip.SetIp("192.168.1.1");

//        // Act
//        Ip.Reset();

//        // Assert
//        var result = Ip.GetIp();
//        result.ShouldNotBe("192.168.1.1");
//    }

//    /// <summary>
//    /// 测试 - GetAllLocalIps - 获取本机IP地址
//    /// </summary>
//    [Fact]
//    public void GetAllLocalIps_DefaultParameters_ReturnsIPList()
//    {
//        // Act
//        var ips = Ip.GetAllLocalIps();

//        // Assert
//        ips.ShouldNotBeNull();
//        Output.WriteLine($"获取到 {ips.Count} 个IP地址：");
//        foreach (var ip in ips)
//        {
//            Output.WriteLine($"  {ip}");
//            Ip.IsValidIp(ip).ShouldBeTrue();
//        }
//    }

//    /// <summary>
//    /// 测试 - GetAllLocalIps - 包含IPv6地址
//    /// </summary>
//    [Fact]
//    public void GetAllLocalIps_IncludeIPv6_ReturnsIPv6Addresses()
//    {
//        // Act
//        var ips = Ip.GetAllLocalIps(includeIPv6: true);

//        // Assert
//        ips.ShouldNotBeNull();
//        var ipv6Count = ips.Count(ip => Ip.IsValidIPv6(ip));
//        Output.WriteLine($"获取到 {ips.Count} 个IP地址，其中 {ipv6Count} 个IPv6地址");
//    }

//    /// <summary>
//    /// 测试 - GetAllLocalIps - 包含回环地址
//    /// </summary>
//    [Fact]
//    public void GetAllLocalIps_IncludeLoopback_ReturnsLoopbackAddresses()
//    {
//        // Act
//        var ips = Ip.GetAllLocalIps(includeLoopback: true);

//        // Assert
//        ips.ShouldNotBeNull();
//        var loopbackCount = ips.Count(ip => Ip.IsLocalIp(ip));
//        Output.WriteLine($"获取到 {ips.Count} 个IP地址，其中 {loopbackCount} 个回环地址");
//    }

//    #endregion

//    #region IPv4地址验证测试

//    /// <summary>
//    /// 测试 - IsValidIp - 有效IP地址
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.1", true)]
//    [InlineData("8.8.8.8", true)]
//    [InlineData("127.0.0.1", true)]
//    [InlineData("0.0.0.0", true)]
//    [InlineData("255.255.255.255", true)]
//    [InlineData("2001:db8::1", true)]
//    [InlineData("::1", true)]
//    [InlineData("invalid", false)]
//    [InlineData("256.1.1.1", false)]
//    [InlineData("", false)]
//    [InlineData(null, false)]
//    public void IsValidIp_VariousInputs_ReturnsExpected(string ip, bool expected)
//    {
//        // Act
//        var result = Ip.IsValidIp(ip);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - IsValidIp - 边界情况和格式验证
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.1", true)]    // 有效IPv4
//    [InlineData("192.168.1", false)]     // 缺少段
//    [InlineData("192.168", false)]       // 只有两个段
//    [InlineData("192", false)]           // 只有一个段
//    [InlineData("192.168.1.", false)]    // 末尾有点
//    [InlineData(".192.168.1.1", false)] // 开头有点
//    [InlineData("192..168.1.1", false)] // 连续的点
//    [InlineData("192.168.01.1", false)] // 前导零
//    [InlineData("192.168.1.01", false)] // 前导零
//    [InlineData("192.168.1.256", false)] // 超出范围
//    [InlineData("192.168.-1.1", false)] // 负数
//    [InlineData("2001:db8::1", true)]   // 有效IPv6
//    [InlineData("::1", true)]           // 有效IPv6回环
//    [InlineData("", false)]             // 空字符串
//    [InlineData(null, false)]           // null
//    [InlineData("   ", false)]          // 空白字符
//    public void IsValidIp_EdgeCases_ReturnsExpected(string ip, bool expected)
//    {
//        // Act
//        var result = Ip.IsValidIp(ip);

//        // Assert
//        result.ShouldBe(expected, $"IP '{ip}' 验证结果应该是 {expected}");
//    }

//    /// <summary>
//    /// 测试 - IsValidIPv4 - IPv4地址验证
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.1", true)]
//    [InlineData("8.8.8.8", true)]
//    [InlineData("127.0.0.1", true)]
//    [InlineData("0.0.0.0", true)]
//    [InlineData("255.255.255.255", true)]
//    [InlineData("2001:db8::1", false)]
//    [InlineData("::1", false)]
//    [InlineData("256.1.1.1", false)]
//    [InlineData("192.168.1", false)]
//    [InlineData("invalid", false)]
//    public void IsValidIPv4_VariousInputs_ReturnsExpected(string ip, bool expected)
//    {
//        // Act
//        var result = Ip.IsValidIPv4(ip);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - IsLocalIp - 本地回环地址判断
//    /// </summary>
//    [Theory]
//    [InlineData("127.0.0.1", true)]
//    [InlineData("127.0.0.2", true)]
//    [InlineData("127.255.255.255", true)]
//    [InlineData("::1", true)]
//    [InlineData("192.168.1.1", false)]
//    [InlineData("8.8.8.8", false)]
//    [InlineData("2001:db8::1", false)]
//    public void IsLocalIp_VariousInputs_ReturnsExpected(string ip, bool expected)
//    {
//        // Act
//        var result = Ip.IsLocalIp(ip);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - IsInnerIp - 内网IP地址判断
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.1", true)] // C类私有地址
//    [InlineData("10.0.0.1", true)]    // A类私有地址
//    [InlineData("172.16.0.1", true)]  // B类私有地址
//    [InlineData("169.254.0.1", true)] // 链路本地地址
//    [InlineData("127.0.0.1", true)]   // 回环地址
//    [InlineData("8.8.8.8", false)]    // 公网地址
//    [InlineData("114.114.114.114", false)] // 公网地址
//    [InlineData("::1", true)]         // IPv6回环地址
//    [InlineData("fe80::1", true)]     // IPv6链路本地地址
//    [InlineData("2001:4860:4860::8888", false)] // IPv6公网地址
//    public void IsInnerIp_VariousInputs_ReturnsExpected(string ip, bool expected)
//    {
//        // Act
//        var result = Ip.IsInnerIp(ip);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    #endregion

//    #region IPv6地址验证测试

//    /// <summary>
//    /// 测试 - IsValidIPv6 - IPv6地址验证
//    /// </summary>
//    [Theory]
//    [InlineData("2001:db8::1", true)]
//    [InlineData("::1", true)]
//    [InlineData("fe80::1", true)]
//    [InlineData("2001:4860:4860::8888", true)]
//    [InlineData("::", true)]
//    [InlineData("192.168.1.1", false)]
//    [InlineData("invalid", false)]
//    [InlineData("gggg::1", false)]
//    public void IsValidIPv6_VariousInputs_ReturnsExpected(string ip, bool expected)
//    {
//        // Act
//        var result = Ip.IsValidIPv6(ip);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - IsValidIPv6Regex - IPv6地址正则验证
//    /// </summary>
//    [Theory]
//    [InlineData("2001:db8::1", true)]
//    [InlineData("::1", true)]
//    [InlineData("fe80::1", true)]
//    [InlineData("invalid", false)]
//    [InlineData("192.168.1.1", false)]
//    public void IsValidIPv6Regex_VariousInputs_ReturnsExpected(string ip, bool expected)
//    {
//        // Act
//        var result = Ip.IsValidIPv6Regex(ip);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - GetIPv6AddressType - IPv6地址类型判断
//    /// </summary>
//    [Theory]
//    [InlineData("::1", IPv6AddressType.Loopback)]
//    [InlineData("::", IPv6AddressType.Unspecified)]
//    [InlineData("fe80::1", IPv6AddressType.LinkLocal)]
//    [InlineData("fc00::1", IPv6AddressType.UniqueLocal)]
//    [InlineData("ff02::1", IPv6AddressType.Multicast)]
//    [InlineData("2001:db8::1", IPv6AddressType.Documentation)]
//    [InlineData("2002::1", IPv6AddressType.SixToFour)]
//    [InlineData("2001:4860:4860::8888", IPv6AddressType.GlobalUnicast)]
//    [InlineData("invalid", IPv6AddressType.Invalid)]
//    public void GetIPv6AddressType_VariousInputs_ReturnsExpected(string ip, IPv6AddressType expected)
//    {
//        // Act
//        var result = Ip.GetIPv6AddressType(ip);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - IsGlobalUnicastIPv6 - IPv6全局单播地址判断
//    /// </summary>
//    [Theory]
//    [InlineData("2001:4860:4860::8888", true)]
//    [InlineData("2400:3200::1", true)]
//    [InlineData("::1", false)]
//    [InlineData("fe80::1", false)]
//    [InlineData("ff02::1", false)]
//    public void IsGlobalUnicastIPv6_VariousInputs_ReturnsExpected(string ip, bool expected)
//    {
//        // Act
//        var result = Ip.IsGlobalUnicastIPv6(ip);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - IsMulticastIPv6 - IPv6组播地址判断
//    /// </summary>
//    [Theory]
//    [InlineData("ff02::1", true)]
//    [InlineData("ff05::1", true)]
//    [InlineData("2001:db8::1", false)]
//    [InlineData("::1", false)]
//    public void IsMulticastIPv6_VariousInputs_ReturnsExpected(string ip, bool expected)
//    {
//        // Act
//        var result = Ip.IsMulticastIPv6(ip);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - IsLinkLocalIPv6 - IPv6链路本地地址判断
//    /// </summary>
//    [Theory]
//    [InlineData("fe80::1", true)]
//    [InlineData("fe80:0:0:0:0:0:0:1", true)]
//    [InlineData("2001:db8::1", false)]
//    [InlineData("::1", false)]
//    public void IsLinkLocalIPv6_VariousInputs_ReturnsExpected(string ip, bool expected)
//    {
//        // Act
//        var result = Ip.IsLinkLocalIPv6(ip);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    #endregion

//    #region IPv4地址转换测试

//    /// <summary>
//    /// 测试 - IpToUInt32 - IPv4地址转换为数值
//    /// </summary>
//    [Theory]
//    [InlineData("0.0.0.0", 0u)]
//    [InlineData("0.0.0.1", 1u)]
//    [InlineData("192.168.1.1", 3232235777u)]
//    [InlineData("255.255.255.255", 4294967295u)]
//    public void IpToUInt32_ValidIPv4_ReturnsExpected(string ip, uint expected)
//    {
//        // Act
//        var result = Ip.IpToUInt32(ip);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - IpToUInt32 - 无效地址抛出异常
//    /// </summary>
//    [Theory]
//    [InlineData("invalid")]
//    [InlineData("2001:db8::1")]
//    [InlineData("256.1.1.1")]
//    public void IpToUInt32_InvalidIP_ThrowsArgumentException(string invalidIp)
//    {
//        // Act & Assert
//        Should.Throw<ArgumentException>(() => Ip.IpToUInt32(invalidIp))
//            .Message.ShouldContain("无效的IPv4地址格式");
//    }

//    /// <summary>
//    /// 测试 - UInt32ToIp - 数值转换为IPv4地址
//    /// </summary>
//    [Theory]
//    [InlineData(0u, "0.0.0.0")]
//    [InlineData(1u, "0.0.0.1")]
//    [InlineData(3232235777u, "192.168.1.1")]
//    [InlineData(4294967295u, "255.255.255.255")]
//    public void UInt32ToIp_ValidValue_ReturnsExpected(uint value, string expected)
//    {
//        // Act
//        var result = Ip.UInt32ToIp(value);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - GetNetworkAddress - 获取网络地址
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.100", "255.255.255.0", "192.168.1.0")]
//    [InlineData("10.1.2.3", "255.0.0.0", "10.0.0.0")]
//    [InlineData("172.16.5.4", "255.255.0.0", "172.16.0.0")]
//    public void GetNetworkAddress_ValidInputs_ReturnsExpected(string ip, string mask, string expected)
//    {
//        // Act
//        var result = Ip.GetNetworkAddress(ip, mask);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - GetBroadcastAddress - 获取广播地址
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.100", "255.255.255.0", "192.168.1.255")]
//    [InlineData("10.1.2.3", "255.0.0.0", "10.255.255.255")]
//    [InlineData("172.16.5.4", "255.255.0.0", "172.16.255.255")]
//    public void GetBroadcastAddress_ValidInputs_ReturnsExpected(string ip, string mask, string expected)
//    {
//        // Act
//        var result = Ip.GetBroadcastAddress(ip, mask);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    #endregion

//    #region IPv6地址转换测试

//    /// <summary>
//    /// 测试 - IPv6ToBytes - IPv6地址转换为字节数组
//    /// </summary>
//    [Fact]
//    public void IPv6ToBytes_ValidIPv6_Returns16Bytes()
//    {
//        // Arrange
//        var ipv6 = "2001:db8::1";

//        // Act
//        var result = Ip.IPv6ToBytes(ipv6);

//        // Assert
//        result.ShouldNotBeNull();
//        result.Length.ShouldBe(16);
//    }

//    /// <summary>
//    /// 测试 - IPv6ToBytes - 无效地址抛出异常
//    /// </summary>
//    [Theory]
//    [InlineData("invalid")]
//    [InlineData("192.168.1.1")]
//    public void IPv6ToBytes_InvalidIPv6_ThrowsArgumentException(string invalidIp)
//    {
//        // Act & Assert
//        Should.Throw<ArgumentException>(() => Ip.IPv6ToBytes(invalidIp))
//            .Message.ShouldContain("无效的IPv6地址格式");
//    }

//    /// <summary>
//    /// 测试 - BytesToIPv6 - 字节数组转换为IPv6地址
//    /// </summary>
//    [Fact]
//    public void BytesToIPv6_Valid16Bytes_ReturnsIPv6()
//    {
//        // Arrange
//        var bytes = new byte[16] { 0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };

//        // Act
//        var result = Ip.BytesToIPv6(bytes);

//        // Assert
//        result.ShouldNotBeNullOrEmpty();
//        Ip.IsValidIPv6(result).ShouldBeTrue();
//    }

//    /// <summary>
//    /// 测试 - BytesToIPv6 - 无效字节数组抛出异常
//    /// </summary>
//    [Theory]
//    [InlineData(null)]
//    //[InlineData(new byte[15])]
//    //[InlineData(new byte[17])]
//    public void BytesToIPv6_InvalidBytes_ThrowsArgumentException(byte[] invalidBytes)
//    {
//        // Act & Assert
//        Should.Throw<ArgumentException>(() => Ip.BytesToIPv6(invalidBytes))
//            .Message.ShouldContain("IPv6地址必须是16字节数组");
//    }

//    /// <summary>
//    /// 测试 - ExpandIPv6 - IPv6地址展开
//    /// </summary>
//    [Theory]
//    [InlineData("2001:db8::1", "2001:0db8:0000:0000:0000:0000:0000:0001")]
//    [InlineData("::1", "0000:0000:0000:0000:0000:0000:0000:0001")]
//    public void ExpandIPv6_ValidIPv6_ReturnsExpanded(string compressed, string expected)
//    {
//        // Act
//        var result = Ip.ExpandIPv6(compressed);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - CompressIPv6 - IPv6地址压缩
//    /// </summary>
//    [Theory]
//    [InlineData("2001:0db8:0000:0000:0000:0000:0000:0001")]
//    [InlineData("0000:0000:0000:0000:0000:0000:0000:0001")]
//    public void CompressIPv6_ValidIPv6_ReturnsCompressed(string expanded)
//    {
//        // Act
//        var result = Ip.CompressIPv6(expanded);

//        // Assert
//        result.ShouldNotBeNullOrEmpty();
//        Ip.IsValidIPv6(result).ShouldBeTrue();
//        result.Length.ShouldBeLessThanOrEqualTo(expanded.Length);
//    }

//    /// <summary>
//    /// 测试 - IsIPv4MappedIPv6 - IPv4映射地址判断
//    /// </summary>
//    [Theory]
//    [InlineData("::ffff:192.168.1.1", true)]
//    [InlineData("::ffff:8.8.8.8", true)]
//    [InlineData("2001:db8::1", false)]
//    [InlineData("::1", false)]
//    public void IsIPv4MappedIPv6_VariousInputs_ReturnsExpected(string ip, bool expected)
//    {
//        // Act
//        var result = Ip.IsIPv4MappedIPv6(ip);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - MapIPv6ToIPv4 - IPv6映射地址转换为IPv4
//    /// </summary>
//    [Theory]
//    [InlineData("::ffff:192.168.1.1", "192.168.1.1")]
//    [InlineData("::ffff:8.8.8.8", "8.8.8.8")]
//    public void MapIPv6ToIPv4_ValidMappedAddress_ReturnsIPv4(string ipv6, string expected)
//    {
//        // Act
//        var result = Ip.MapIPv6ToIPv4(ipv6);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - MapIPv4ToIPv6 - IPv4地址转换为IPv6映射地址
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.1")]
//    [InlineData("8.8.8.8")]
//    public void MapIPv4ToIPv6_ValidIPv4_ReturnsMappedIPv6(string ipv4)
//    {
//        // Act
//        var result = Ip.MapIPv4ToIPv6(ipv4);

//        // Assert
//        result.ShouldNotBeNullOrEmpty();
//        Ip.IsIPv4MappedIPv6(result).ShouldBeTrue();
//    }

//    #endregion

//    #region IPv4地址范围和子网操作测试

//    /// <summary>
//    /// 测试 - IsInRange - IP地址范围判断
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.100", "192.168.1.1", "192.168.1.255", true)]
//    [InlineData("192.168.1.1", "192.168.1.1", "192.168.1.255", true)]
//    [InlineData("192.168.1.255", "192.168.1.1", "192.168.1.255", true)]
//    [InlineData("192.168.2.1", "192.168.1.1", "192.168.1.255", false)]
//    [InlineData("192.168.0.255", "192.168.1.1", "192.168.1.255", false)]
//    public void IsInRange_VariousInputs_ReturnsExpected(string ip, string start, string end, bool expected)
//    {
//        // Act
//        var result = Ip.IsInRange(ip, start, end);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - IsInSubnet - CIDR子网判断
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.100", "192.168.1.0/24", true)]
//    [InlineData("192.168.1.1", "192.168.1.0/24", true)]
//    [InlineData("192.168.1.255", "192.168.1.0/24", true)]
//    [InlineData("192.168.2.1", "192.168.1.0/24", false)]
//    [InlineData("10.0.0.1", "10.0.0.0/8", true)]
//    [InlineData("172.16.0.1", "172.16.0.0/16", true)]
//    public void IsInSubnet_VariousInputs_ReturnsExpected(string ip, string cidr, bool expected)
//    {
//        // Act
//        var result = Ip.IsInSubnet(ip, cidr);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - GenerateIpRange - 生成IP范围
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.0/30", 4)] // /30 网段包含4个地址
//    [InlineData("10.0.0.0/29", 8)]    // /29 网段包含8个地址
//    public void GenerateIpRange_ValidCIDR_ReturnsExpectedCount(string cidr, int expectedCount)
//    {
//        // Act
//        var result = Ip.GenerateIpRange(cidr);

//        // Assert
//        result.ShouldNotBeNull();
//        result.Count.ShouldBe(expectedCount);
//        foreach (var ip in result)
//        {
//            Ip.IsValidIPv4(ip).ShouldBeTrue();
//            Ip.IsInSubnet(ip, cidr).ShouldBeTrue();
//        }
//    }

//    /// <summary>
//    /// 测试 - GenerateIpRange - 过大网段限制
//    /// </summary>
//    [Fact]
//    public void GenerateIpRange_LargeSubnet_ReturnsEmptyList()
//    {
//        // Act
//        var result = Ip.GenerateIpRange("10.0.0.0/8"); // 过大的网段

//        // Assert
//        result.ShouldNotBeNull();
//        result.Count.ShouldBe(0);
//    }

//    /// <summary>
//    /// 测试 - SubnetMaskToCidr - 子网掩码转换为CIDR
//    /// </summary>
//    [Theory]
//    [InlineData("255.255.255.0", 24)]
//    [InlineData("255.255.0.0", 16)]
//    [InlineData("255.0.0.0", 8)]
//    [InlineData("255.255.255.255", 32)]
//    [InlineData("0.0.0.0", 0)]
//    public void SubnetMaskToCidr_ValidMask_ReturnsExpected(string mask, int expected)
//    {
//        // Act
//        var result = Ip.SubnetMaskToCidr(mask);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - CidrToSubnetMask - CIDR转换为子网掩码
//    /// </summary>
//    [Theory]
//    [InlineData(24, "255.255.255.0")]
//    [InlineData(16, "255.255.0.0")]
//    [InlineData(8, "255.0.0.0")]
//    [InlineData(32, "255.255.255.255")]
//    [InlineData(0, "0.0.0.0")]
//    public void CidrToSubnetMask_ValidPrefix_ReturnsExpected(int prefix, string expected)
//    {
//        // Act
//        var result = Ip.CidrToSubnetMask(prefix);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - CidrToSubnetMask - CIDR转换为子网掩码 - 完整测试
//    /// </summary>
//    [Theory]
//    [InlineData(0, "0.0.0.0")]           // 边界：最小前缀
//    [InlineData(1, "128.0.0.0")]         // 单个网络位
//    [InlineData(8, "255.0.0.0")]         // A类网络
//    [InlineData(16, "255.255.0.0")]      // B类网络
//    [InlineData(24, "255.255.255.0")]    // C类网络
//    [InlineData(25, "255.255.255.128")]  // 子网划分
//    [InlineData(26, "255.255.255.192")]  // 更小子网
//    [InlineData(27, "255.255.255.224")]  // 更小子网
//    [InlineData(28, "255.255.255.240")]  // 更小子网
//    [InlineData(29, "255.255.255.248")]  // 更小子网
//    [InlineData(30, "255.255.255.252")]  // 点对点链路
//    [InlineData(31, "255.255.255.254")]  // RFC 3021
//    [InlineData(32, "255.255.255.255")]  // 边界：最大前缀
//    public void CidrToSubnetMask_AllValidPrefixes_ReturnsCorrectMask(int prefix, string expected)
//    {
//        // Act
//        var result = Ip.CidrToSubnetMask(prefix);

//        // Assert
//        result.ShouldBe(expected);

//        // 验证结果是有效的IPv4地址
//        Ip.IsValidIPv4(result).ShouldBeTrue();

//        // 验证往返转换的一致性
//        var backToPrefix = Ip.SubnetMaskToCidr(result);
//        backToPrefix.ShouldBe(prefix);
//    }

//    /// <summary>
//    /// 测试 - CidrToSubnetMask - 无效前缀长度抛出异常
//    /// </summary>
//    [Theory]
//    [InlineData(-1)]
//    [InlineData(33)]
//    [InlineData(int.MinValue)]
//    [InlineData(int.MaxValue)]
//    public void CidrToSubnetMask_InvalidPrefix_ThrowsArgumentOutOfRangeException(int invalidPrefix)
//    {
//        // Act & Assert
//        var exception = Should.Throw<ArgumentOutOfRangeException>(() => Ip.CidrToSubnetMask(invalidPrefix));
//        exception.ParamName.ShouldBe("prefixLength");
//        exception.Message.ShouldContain("前缀长度必须在0-32之间");
//    }

//    /// <summary>
//    /// 测试 - CidrToSubnetMask 和 SubnetMaskToCidr - 往返转换一致性
//    /// </summary>
//    [Fact]
//    public void CidrToSubnetMask_RoundTripConversion_MaintainsConsistency()
//    {
//        // 测试所有有效的前缀长度 0-32
//        for (int prefix = 0; prefix <= 32; prefix++)
//        {
//            // Act
//            var mask = Ip.CidrToSubnetMask(prefix);
//            var backToPrefix = Ip.SubnetMaskToCidr(mask);

//            // Assert
//            backToPrefix.ShouldBe(prefix, $"前缀 {prefix} 的往返转换失败");
//            Ip.IsValidIPv4(mask).ShouldBeTrue($"前缀 {prefix} 生成的掩码无效: {mask}");
//        }
//    }

//    /// <summary>
//    /// 测试 - CidrToSubnetMask - 掩码的二进制连续性验证
//    /// </summary>
//    [Theory]
//    [InlineData(0)]
//    [InlineData(8)]
//    [InlineData(16)]
//    [InlineData(24)]
//    [InlineData(32)]
//    public void CidrToSubnetMask_BinaryContinuity_ValidatesBitPattern(int prefix)
//    {
//        // Act
//        var mask = Ip.CidrToSubnetMask(prefix);
//        var maskNum = Ip.IpToUInt32(mask);

//        // Assert - 验证掩码的二进制表示是连续的1
//        var binaryMask = Convert.ToString(maskNum, 2).PadLeft(32, '0');
//        Output.WriteLine($"前缀 {prefix}: 掩码 {mask}, 二进制: {binaryMask}");

//        // 验证二进制表示：前prefix位应该是1，后面应该是0
//        for (int i = 0; i < 32; i++)
//        {
//            bool expectedBit = i < prefix;
//            bool actualBit = binaryMask[i] == '1';
//            actualBit.ShouldBe(expectedBit, $"前缀 {prefix} 在位 {i} 处的位值不正确");
//        }
//    }

//    /// <summary>
//    /// 测试 - CidrToSubnetMask - 实际子网计算验证
//    /// </summary>
//    [Theory]
//    [InlineData(24, "192.168.1.100", "192.168.1.0", "192.168.1.255")]
//    [InlineData(16, "10.1.2.3", "10.1.0.0", "10.1.255.255")]
//    [InlineData(8, "172.16.5.4", "172.0.0.0", "172.255.255.255")]
//    [InlineData(30, "192.168.1.5", "192.168.1.4", "192.168.1.7")]
//    public void CidrToSubnetMask_SubnetCalculation_WorksCorrectly(int prefix, string testIp, string expectedNetwork, string expectedBroadcast)
//    {
//        // Act
//        var mask = Ip.CidrToSubnetMask(prefix);
//        var networkAddress = Ip.GetNetworkAddress(testIp, mask);
//        var broadcastAddress = Ip.GetBroadcastAddress(testIp, mask);

//        // Assert
//        networkAddress.ShouldBe(expectedNetwork);
//        broadcastAddress.ShouldBe(expectedBroadcast);
//    }

//    /// <summary>
//    /// 测试 - GetSubnetInfo - 获取子网信息
//    /// </summary>
//    [Fact]
//    public void GetSubnetInfo_ValidCIDR_ReturnsCompleteInfo()
//    {
//        // Arrange
//        var cidr = "192.168.1.0/24";

//        // Act
//        var result = Ip.GetSubnetInfo(cidr);

//        // Assert
//        result.ShouldNotBeNull();
//        result.NetworkAddress.ShouldBe("192.168.1.0");
//        result.BroadcastAddress.ShouldBe("192.168.1.255");
//        result.SubnetMask.ShouldBe("255.255.255.0");
//        result.PrefixLength.ShouldBe(24);
//        result.TotalHosts.ShouldBe(256u);
//        result.AvailableHosts.ShouldBe(254u);
//        result.FirstUsableIp.ShouldBe("192.168.1.1");
//        result.LastUsableIp.ShouldBe("192.168.1.254");
//    }

//    #endregion

//    #region IPv6地址范围和子网操作测试

//    /// <summary>
//    /// 测试 - IsInIPv6Subnet - IPv6子网判断
//    /// </summary>
//    [Theory]
//    [InlineData("2001:db8::1", "2001:db8::/32", true)]
//    [InlineData("2001:db8:1::1", "2001:db8::/32", true)]
//    [InlineData("2001:db9::1", "2001:db8::/32", false)]
//    [InlineData("2002:db8::1", "2001:db8::/32", false)]
//    public void IsInIPv6Subnet_VariousInputs_ReturnsExpected(string ip, string cidr, bool expected)
//    {
//        // Act
//        var result = Ip.IsInIPv6Subnet(ip, cidr);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - GetIPv6SubnetInfo - 获取IPv6子网信息
//    /// </summary>
//    [Fact]
//    public void GetIPv6SubnetInfo_ValidCIDR_ReturnsCompleteInfo()
//    {
//        // Arrange
//        var cidr = "2001:db8::/64";

//        // Act
//        var result = Ip.GetIPv6SubnetInfo(cidr);

//        // Assert
//        result.ShouldNotBeNull();
//        result.NetworkPrefix.ShouldBe("2001:db8::");
//        result.PrefixLength.ShouldBe(64);
//        result.HostBits.ShouldBe(64);
//        result.IsLargeSubnet.ShouldBeTrue();
//        result.AddressType.ShouldBe(IPv6AddressType.Documentation);
//    }

//    /// <summary>
//    /// 测试 - GenerateIPv6Range - 生成IPv6地址范围
//    /// </summary>
//    [Fact]
//    public void GenerateIPv6Range_SmallSubnet_ReturnsAddresses()
//    {
//        // Arrange
//        var cidr = "2001:db8::/126"; // 只包含4个地址

//        // Act
//        var result = Ip.GenerateIPv6Range(cidr, 10);

//        // Assert
//        result.ShouldNotBeNull();
//        result.Count.ShouldBe(4);
//        foreach (var ip in result)
//        {
//            Ip.IsValidIPv6(ip).ShouldBeTrue();
//            Ip.IsInIPv6Subnet(ip, cidr).ShouldBeTrue();
//        }
//    }

//    /// <summary>
//    /// 测试 - IPv6PrefixToSubnetMask - IPv6前缀转换为子网掩码
//    /// </summary>
//    [Theory]
//    [InlineData(64, "ffff:ffff:ffff:ffff::")]
//    [InlineData(48, "ffff:ffff:ffff::")]
//    [InlineData(32, "ffff:ffff::")]
//    [InlineData(128, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
//    public void IPv6PrefixToSubnetMask_ValidPrefix_ReturnsExpected(int prefix, string expected)
//    {
//        // Act
//        var result = Ip.IPv6PrefixToSubnetMask(prefix);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - IPv6SubnetMaskToPrefix - IPv6子网掩码转换为前缀
//    /// </summary>
//    [Theory]
//    [InlineData("ffff:ffff:ffff:ffff::", 64)]
//    [InlineData("ffff:ffff:ffff::", 48)]
//    [InlineData("ffff:ffff::", 32)]
//    public void IPv6SubnetMaskToPrefix_ValidMask_ReturnsExpected(string mask, int expected)
//    {
//        // Act
//        var result = Ip.IPv6SubnetMaskToPrefix(mask);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - IsValidIPv6SubnetMask - IPv6子网掩码验证
//    /// </summary>
//    [Theory]
//    [InlineData("ffff:ffff:ffff:ffff::", true)]
//    [InlineData("ffff:ffff:ffff::", true)]
//    [InlineData("ffff:ff00:ffff::", false)] // 非连续位
//    [InlineData("invalid", false)]
//    public void IsValidIPv6SubnetMask_VariousInputs_ReturnsExpected(string mask, bool expected)
//    {
//        // Act
//        var result = Ip.IsValidIPv6SubnetMask(mask);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - GetFirstIPv6Address - 获取IPv6网段首地址
//    /// </summary>
//    [Theory]
//    [InlineData("2001:db8::", 64, "2001:db8::1")]
//    [InlineData("2001:db8::", 126, "2001:db8::1")]
//    public void GetFirstIPv6Address_ValidInputs_ReturnsExpected(string network, int prefix, string expected)
//    {
//        // Act
//        var result = Ip.GetFirstIPv6Address(network, prefix);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - GetLastIPv6Address - 获取IPv6网段末地址
//    /// </summary>
//    [Fact]
//    public void GetLastIPv6Address_ValidInputs_ReturnsLastAddress()
//    {
//        // Arrange
//        var network = "2001:db8::";
//        var prefix = 126;

//        // Act
//        var result = Ip.GetLastIPv6Address(network, prefix);

//        // Assert
//        result.ShouldNotBeNullOrEmpty();
//        Ip.IsValidIPv6(result).ShouldBeTrue();
//        Ip.IsInIPv6Subnet(result, $"{network}/{prefix}").ShouldBeTrue();
//    }

//    #endregion

//    #region 高级IP操作测试

//    /// <summary>
//    /// 测试 - SortIpAddresses - IP地址排序
//    /// </summary>
//    [Fact]
//    public void SortIpAddresses_VariousIPs_ReturnsSorted()
//    {
//        // Arrange
//        var ips = new[] { "192.168.1.10", "192.168.1.2", "192.168.1.100", "192.168.1.1" };

//        // Act
//        var result = Ip.SortIpAddresses(ips);

//        // Assert
//        result.ShouldNotBeNull();
//        result.Count.ShouldBe(4);
//        result[0].ShouldBe("192.168.1.1");
//        result[1].ShouldBe("192.168.1.2");
//        result[2].ShouldBe("192.168.1.10");
//        result[3].ShouldBe("192.168.1.100");
//    }

//    /// <summary>
//    /// 测试 - GetIpDistance - IP地址距离计算
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.1", "192.168.1.10", 9u)]
//    [InlineData("192.168.1.10", "192.168.1.1", 9u)]
//    [InlineData("192.168.1.1", "192.168.1.1", 0u)]
//    public void GetIpDistance_ValidIPs_ReturnsDistance(string ip1, string ip2, uint expected)
//    {
//        // Act
//        var result = Ip.GetIpDistance(ip1, ip2);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - GetNextIp - 获取下一个IP地址
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.1", 1u, "192.168.1.2")]
//    [InlineData("192.168.1.1", 5u, "192.168.1.6")]
//    [InlineData("192.168.1.254", 1u, "192.168.1.255")]
//    public void GetNextIp_ValidInputs_ReturnsNextIp(string ip, uint step, string expected)
//    {
//        // Act
//        var result = Ip.GetNextIp(ip, step);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - GetPreviousIp - 获取前一个IP地址
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.10", 1u, "192.168.1.9")]
//    [InlineData("192.168.1.10", 5u, "192.168.1.5")]
//    public void GetPreviousIp_ValidInputs_ReturnsPreviousIp(string ip, uint step, string expected)
//    {
//        // Act
//        var result = Ip.GetPreviousIp(ip, step);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - IsIpReachableAsync - IP可达性检测
//    /// </summary>
//    [Fact]
//    public async Task IsIpReachableAsync_LocalHost_ReturnsTrue()
//    {
//        // Arrange
//        var timeout = TimeSpan.FromSeconds(1);

//        // Act
//        var result = await Ip.IsIpReachableAsync("127.0.0.1", timeout);

//        // Assert
//        // 本地回环地址通常是可达的，但在某些环境下可能不可达，所以这里只验证方法不抛异常
//        result.ShouldBeOfType<bool>();
//    }

//    #endregion

//    #region IPv6地址生成和工具测试

//    /// <summary>
//    /// 测试 - GetIPv6Distance - IPv6地址距离计算
//    /// </summary>
//    [Theory]
//    [InlineData("2001:db8::1", "2001:db8::10", "15")]
//    [InlineData("2001:db8::10", "2001:db8::1", "15")]
//    [InlineData("2001:db8::1", "2001:db8::1", "0")]
//    public void GetIPv6Distance_ValidInputs_ReturnsDistance(string ip1, string ip2, string expectedStr)
//    {
//        // Arrange
//        var expected = BigInteger.Parse(expectedStr);

//        // Act
//        var result = Ip.GetIPv6Distance(ip1, ip2);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - GetNextIPv6Address - 获取下一个IPv6地址
//    /// </summary>
//    [Theory]
//    [InlineData("2001:db8::1", 1, "2001:db8::2")]
//    [InlineData("2001:db8::1", 5, "2001:db8::6")]
//    public void GetNextIPv6Address_ValidInputs_ReturnsNextAddress(string ip, int step, string expected)
//    {
//        // Act
//        var result = Ip.GetNextIPv6Address(ip, step);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - GetPreviousIPv6Address - 获取前一个IPv6地址
//    /// </summary>
//    [Theory]
//    [InlineData("2001:db8::10", 1, "2001:db8::f")]
//    [InlineData("2001:db8::10", 5, "2001:db8::b")]
//    public void GetPreviousIPv6Address_ValidInputs_ReturnsPreviousAddress(string ip, int step, string expected)
//    {
//        // Act
//        var result = Ip.GetPreviousIPv6Address(ip, step);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - GenerateIPv6LinkLocal - 生成IPv6链路本地地址
//    /// </summary>
//    [Fact]
//    public void GenerateIPv6LinkLocal_WithoutMAC_GeneratesValidLinkLocal()
//    {
//        // Act
//        var result = Ip.GenerateIPv6LinkLocal();

//        // Assert
//        result.ShouldNotBeNullOrEmpty();
//        Ip.IsValidIPv6(result).ShouldBeTrue();
//        Ip.IsLinkLocalIPv6(result).ShouldBeTrue();
//    }

//    /// <summary>
//    /// 测试 - GenerateIPv6LinkLocal - 使用MAC地址生成
//    /// </summary>
//    [Fact]
//    public void GenerateIPv6LinkLocal_WithMAC_GeneratesValidLinkLocal()
//    {
//        // Arrange
//        var macAddress = "00:11:22:33:44:55";

//        // Act
//        var result = Ip.GenerateIPv6LinkLocal(macAddress);

//        // Assert
//        result.ShouldNotBeNullOrEmpty();
//        Ip.IsValidIPv6(result).ShouldBeTrue();
//        Ip.IsLinkLocalIPv6(result).ShouldBeTrue();
//    }

//    /// <summary>
//    /// 测试 - GenerateRandomIPv6 - 生成随机IPv6地址
//    /// </summary>
//    [Fact]
//    public void GenerateRandomIPv6_WithoutPrefix_GeneratesValidAddress()
//    {
//        // Act
//        var result = Ip.GenerateRandomIPv6();

//        // Assert
//        result.ShouldNotBeNullOrEmpty();
//        Ip.IsValidIPv6(result).ShouldBeTrue();
//    }

//    /// <summary>
//    /// 测试 - GenerateRandomIPv6 - 使用前缀生成
//    /// </summary>
//    [Fact]
//    public void GenerateRandomIPv6_WithPrefix_GeneratesValidAddress()
//    {
//        // Arrange
//        var prefix = "2001:db8::";
//        var prefixLength = 32;

//        // Act
//        var result = Ip.GenerateRandomIPv6(prefix, prefixLength);

//        // Assert
//        result.ShouldNotBeNullOrEmpty();
//        Ip.IsValidIPv6(result).ShouldBeTrue();
//        Ip.IsInIPv6Subnet(result, $"{prefix}/{prefixLength}").ShouldBeTrue();
//    }

//    /// <summary>
//    /// 测试 - AreIPv6InSameNetwork - IPv6同网段判断
//    /// </summary>
//    [Theory]
//    [InlineData("2001:db8::1", "2001:db8::2", 64, true)]
//    [InlineData("2001:db8::1", "2001:db9::1", 64, false)]
//    [InlineData("2001:db8::1", "2001:db8::2", 128, false)]
//    public void AreIPv6InSameNetwork_VariousInputs_ReturnsExpected(string ip1, string ip2, int prefix, bool expected)
//    {
//        // Act
//        var result = Ip.AreIPv6InSameNetwork(ip1, ip2, prefix);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    #endregion

//    #region IPv6实用工具测试

//    /// <summary>
//    /// 测试 - CompareIPv6Addresses - IPv6地址比较
//    /// </summary>
//    [Theory]
//    [InlineData("2001:db8::1", "2001:db8::2", -1)]
//    [InlineData("2001:db8::2", "2001:db8::1", 1)]
//    [InlineData("2001:db8::1", "2001:db8::1", 0)]
//    public void CompareIPv6Addresses_VariousInputs_ReturnsExpected(string ip1, string ip2, int expected)
//    {
//        // Act
//        var result = Ip.CompareIPv6Addresses(ip1, ip2);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - SortIPv6Addresses - IPv6地址排序
//    /// </summary>
//    [Fact]
//    public void SortIPv6Addresses_VariousAddresses_ReturnsSorted()
//    {
//        // Arrange
//        var addresses = new[] { "2001:db8::10", "2001:db8::2", "2001:db8::1", "2001:db8::100" };

//        // Act
//        var result = Ip.SortIPv6Addresses(addresses);

//        // Assert
//        result.ShouldNotBeNull();
//        result.Count.ShouldBe(4);
//        // 验证排序顺序
//        for (int i = 1; i < result.Count; i++)
//        {
//            Ip.CompareIPv6Addresses(result[i - 1], result[i]).ShouldBeLessThanOrEqualTo(0);
//        }
//    }

//    /// <summary>
//    /// 测试 - IsIPv6InRange - IPv6地址范围判断
//    /// </summary>
//    [Theory]
//    [InlineData("2001:db8::5", "2001:db8::1", "2001:db8::10", true)]
//    [InlineData("2001:db8::1", "2001:db8::1", "2001:db8::10", true)]
//    [InlineData("2001:db8::10", "2001:db8::1", "2001:db8::10", true)]
//    [InlineData("2001:db8::20", "2001:db8::1", "2001:db8::10", false)]
//    public void IsIPv6InRange_VariousInputs_ReturnsExpected(string ip, string start, string end, bool expected)
//    {
//        // Act
//        var result = Ip.IsIPv6InRange(ip, start, end);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - GetIPv6NetworkPrefix - 获取IPv6网络前缀
//    /// </summary>
//    [Theory]
//    [InlineData("2001:db8:1234:5678:abcd:ef01:2345:6789", 64, "2001:db8:1234:5678::")]
//    [InlineData("2001:db8:1234:5678:abcd:ef01:2345:6789", 48, "2001:db8:1234::")]
//    public void GetIPv6NetworkPrefix_ValidInputs_ReturnsPrefix(string ip, int prefix, string expected)
//    {
//        // Act
//        var result = Ip.GetIPv6NetworkPrefix(ip, prefix);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    #endregion

//    #region 网络接口操作测试

//    /// <summary>
//    /// 测试 - GetNetworkInterfaces - 获取网络接口信息
//    /// </summary>
//    [Fact]
//    public void GetNetworkInterfaces_ReturnsInterfaceList()
//    {
//        // Act
//        var result = Ip.GetNetworkInterfaces();

//        // Assert
//        result.ShouldNotBeNull();
//        Output.WriteLine($"找到 {result.Count} 个网络接口：");
//        foreach (var iface in result)
//        {
//            Output.WriteLine($"  {iface.Name} - {iface.Type} - {iface.IpAddress}");
//            iface.Name.ShouldNotBeNullOrEmpty();
//        }
//    }

//    /// <summary>
//    /// 测试 - GetIpByInterface - 根据接口类型获取IP
//    /// </summary>
//    [Fact]
//    public void GetIpByInterface_Ethernet_ReturnsValidIPOrEmpty()
//    {
//        // Act
//        var result = Ip.GetIpByInterface(NetworkInterfaceType.Ethernet);

//        // Assert
//        // 结果可能为空（没有以太网接口）或有效IP地址
//        if (!string.IsNullOrEmpty(result))
//        {
//            Ip.IsValidIPv4(result).ShouldBeTrue();
//        }
//        Output.WriteLine($"以太网接口IP: {result}");
//    }

//    #endregion

//    #region MAC地址操作测试

//    /// <summary>
//    /// 测试 - FormatMacAddress - MAC地址格式化
//    /// </summary>
//    [Theory]
//    [InlineData("001122334455", ":", true, "00:11:22:33:44:55")]
//    [InlineData("001122334455", "-", true, "00-11-22-33-44-55")]
//    [InlineData("001122334455", ".", false, "00.11.22.33.44.55")]
//    [InlineData("aabbccddeeff", ":", true, "AA:BB:CC:DD:EE:FF")]
//    public void FormatMacAddress_ValidInputs_ReturnsFormatted(string mac, string separator, bool upperCase, string expected)
//    {
//        // Act
//        var result = Ip.FormatMacAddress(mac, separator, upperCase);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    /// <summary>
//    /// 测试 - FormatMacAddress - 无效MAC地址抛出异常
//    /// </summary>
//    [Theory]
//    [InlineData("00112233445")]  // 长度不足
//    [InlineData("001122334455aa")] // 长度过长
//    [InlineData("")]
//    [InlineData(null)]
//    public void FormatMacAddress_InvalidMAC_ThrowsOrReturnsEmpty(string invalidMac)
//    {
//        if (string.IsNullOrWhiteSpace(invalidMac))
//        {
//            // Act
//            var result = Ip.FormatMacAddress(invalidMac);
//            // Assert
//            result.ShouldBe(string.Empty);
//        }
//        else
//        {
//            // Act & Assert
//            Should.Throw<ArgumentException>(() => Ip.FormatMacAddress(invalidMac));
//        }
//    }

//    /// <summary>
//    /// 测试 - IsValidMacAddress - MAC地址验证
//    /// </summary>
//    [Theory]
//    [InlineData("00:11:22:33:44:55", true)]
//    [InlineData("00-11-22-33-44-55", true)]
//    [InlineData("00.11.22.33.44.55", true)]
//    [InlineData("001122334455", true)]
//    [InlineData("AA:BB:CC:DD:EE:FF", true)]
//    [InlineData("invalid", false)]
//    [InlineData("00:11:22:33:44", false)]
//    [InlineData("", false)]
//    [InlineData(null, false)]
//    public void IsValidMacAddress_VariousInputs_ReturnsExpected(string mac, bool expected)
//    {
//        // Act
//        var result = Ip.IsValidMacAddress(mac);

//        // Assert
//        result.ShouldBe(expected);
//    }

//    #endregion

//    #region 异常情况和边界测试

//    /// <summary>
//    /// 测试 - 各种方法的null参数处理
//    /// </summary>
//    [Fact]
//    public void VariousMethods_WithNullInput_HandleGracefully()
//    {
//        // 这些方法应该能优雅处理null输入
//        Ip.IsValidIp(null).ShouldBeFalse();
//        Ip.IsValidIPv4(null).ShouldBeFalse();
//        Ip.IsValidIPv6(null).ShouldBeFalse();
//        Ip.IsLocalIp(null).ShouldBeFalse();
//        Ip.IsInnerIp(null).ShouldBeFalse();
//        Ip.IsValidMacAddress(null).ShouldBeFalse();
//    }

//    /// <summary>
//    /// 测试 - IP地址转换方法的边界值
//    /// </summary>
//    [Fact]
//    public void IpConversion_BoundaryValues_WorksCorrectly()
//    {
//        // 测试最小值
//        var minIp = Ip.UInt32ToIp(0);
//        minIp.ShouldBe("0.0.0.0");
//        Ip.IpToUInt32(minIp).ShouldBe(0u);

//        // 测试最大值
//        var maxIp = Ip.UInt32ToIp(uint.MaxValue);
//        maxIp.ShouldBe("255.255.255.255");
//        Ip.IpToUInt32(maxIp).ShouldBe(uint.MaxValue);
//    }

//    /// <summary>
//    /// 测试 - CIDR前缀长度边界值
//    /// </summary>
//    [Theory]
//    [InlineData(0)]
//    [InlineData(32)]
//    public void CidrOperations_BoundaryValues_WorksCorrectly(int prefix)
//    {
//        // Act & Assert
//        Should.NotThrow(() => Ip.CidrToSubnetMask(prefix));
//    }

//    /// <summary>
//    /// 测试 - CIDR前缀长度超出范围
//    /// </summary>
//    [Theory]
//    [InlineData(-1)]
//    [InlineData(33)]
//    public void CidrToSubnetMask_OutOfRange_ThrowsException(int invalidPrefix)
//    {
//        // Act & Assert
//        Should.Throw<ArgumentOutOfRangeException>(() => Ip.CidrToSubnetMask(invalidPrefix));
//    }

//    /// <summary>
//    /// 测试 - CidrToSubnetMask - 特殊CIDR场景
//    /// </summary>
//    [Theory]
//    [InlineData(0, "任何地址")]     // /0 表示任何地址
//    [InlineData(32, "主机路由")]   // /32 表示单个主机
//    [InlineData(31, "点对点链路")] // /31 用于点对点链路 (RFC 3021)
//    [InlineData(30, "点对点网络")] // /30 传统点对点网络
//    public void CidrToSubnetMask_SpecialCases_HandlesCorrectly(int prefix, string description)
//    {
//        // Act
//        var mask = Ip.CidrToSubnetMask(prefix);

//        // Assert
//        Ip.IsValidIPv4(mask).ShouldBeTrue($"{description} (/{prefix}) 应该生成有效掩码");

//        // 验证子网信息
//        var subnetInfo = Ip.GetSubnetInfo($"192.168.1.0/{prefix}");
//        subnetInfo.ShouldNotBeNull();
//        subnetInfo.SubnetMask.ShouldBe(mask);

//        Output.WriteLine($"{description} (/{prefix}): 掩码={mask}, 主机数={subnetInfo.TotalHosts}");
//    }

//    /// <summary>
//    /// 测试 - CidrToSubnetMask - 与标准网络类别的对应关系
//    /// </summary>
//    [Theory]
//    [InlineData(8, "255.0.0.0", "A类网络默认掩码")]
//    [InlineData(16, "255.255.0.0", "B类网络默认掩码")]
//    [InlineData(24, "255.255.255.0", "C类网络默认掩码")]
//    public void CidrToSubnetMask_ClassfulNetworks_MatchesStandards(int prefix, string expectedMask, string description)
//    {
//        // Act
//        var result = Ip.CidrToSubnetMask(prefix);

//        // Assert
//        result.ShouldBe(expectedMask, description);
//    }

//    /// <summary>
//    /// 测试 - IPv6前缀长度边界值
//    /// </summary>
//    [Theory]
//    [InlineData(0)]
//    [InlineData(128)]
//    public void IPv6PrefixOperations_BoundaryValues_WorksCorrectly(int prefix)
//    {
//        // Act & Assert
//        Should.NotThrow(() => Ip.IPv6PrefixToSubnetMask(prefix));
//    }

//    /// <summary>
//    /// 测试 - IPv6前缀长度超出范围
//    /// </summary>
//    [Theory]
//    [InlineData(-1)]
//    [InlineData(129)]
//    public void IPv6PrefixToSubnetMask_OutOfRange_ThrowsException(int invalidPrefix)
//    {
//        // Act & Assert
//        Should.Throw<ArgumentOutOfRangeException>(() => Ip.IPv6PrefixToSubnetMask(invalidPrefix));
//    }

//    #endregion

//    #region 线程安全测试

//    /// <summary>
//    /// 测试 - SetIp 线程安全性
//    /// </summary>
//    [Fact]
//    public async Task SetIp_ConcurrentAccess_ThreadSafe()
//    {
//        // Arrange
//        var tasks = new List<Task>();
//        var results = new ConcurrentBag<string>();

//        // Act
//        for (int i = 0; i < 10; i++)
//        {
//            var ip = $"192.168.1.{i + 1}";
//            tasks.Add(Task.Run(() =>
//            {
//                Ip.SetIp(ip);
//                results.Add(Ip.GetIp());
//                Ip.Reset();
//            }));
//        }

//        await Task.WhenAll(tasks);

//        // Assert
//        results.Count.ShouldBe(10);
//        // 验证每个结果都是有效的IP地址
//        foreach (var result in results)
//        {
//            Ip.IsValidIp(result).ShouldBeTrue();
//        }
//    }

//    #endregion

//    #region 性能测试

//    /// <summary>
//    /// 测试 - CidrToSubnetMask - 性能测试
//    /// </summary>
//    [Fact]
//    public void CidrToSubnetMask_Performance_CompletesQuickly()
//    {
//        // Arrange
//        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
//        const int iterations = 10000;

//        // Act
//        for (int i = 0; i < iterations; i++)
//        {
//            for (int prefix = 0; prefix <= 32; prefix++)
//            {
//                Ip.CidrToSubnetMask(prefix);
//            }
//        }

//        stopwatch.Stop();

//        // Assert
//        Output.WriteLine($"执行 {iterations * 33} 次转换耗时: {stopwatch.ElapsedMilliseconds} ms");
//        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000, "性能测试超时");
//    }

//    #endregion

//    #region 集成测试

//    /// <summary>
//    /// 测试 - CidrToSubnetMask - 与其他IP方法的集成
//    /// </summary>
//    [Theory]
//    [InlineData("192.168.1.0/24")]
//    [InlineData("10.0.0.0/8")]
//    [InlineData("172.16.0.0/16")]
//    [InlineData("192.168.1.0/30")]
//    public void CidrToSubnetMask_Integration_WorksWithOtherMethods(string cidr)
//    {
//        // Arrange
//        var parts = cidr.Split('/');
//        var networkIp = parts[0];
//        var prefix = int.Parse(parts[1]);

//        // Act
//        var mask = Ip.CidrToSubnetMask(prefix);
//        var subnetInfo = Ip.GetSubnetInfo(cidr);
//        var testIp = "192.168.1.100";
//        var isInSubnet = Ip.IsInSubnet(testIp, cidr);

//        // Assert
//        subnetInfo.SubnetMask.ShouldBe(mask);

//        // 验证生成的IP范围
//        if (prefix >= 16) // 避免生成过多IP
//        {
//            var ipRange = Ip.GenerateIpRange(cidr);
//            ipRange.ShouldNotBeEmpty();

//            foreach (var ip in ipRange.Take(10)) // 只验证前10个
//            {
//                Ip.IsInSubnet(ip, cidr).ShouldBeTrue($"生成的IP {ip} 应该在子网 {cidr} 内");
//            }
//        }

//        Output.WriteLine($"CIDR: {cidr}, 掩码: {mask}, 总主机数: {subnetInfo.TotalHosts}");
//    }

//    #endregion
//}