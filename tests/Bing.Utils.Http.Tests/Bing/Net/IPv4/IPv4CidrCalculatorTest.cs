namespace Bing.Net.IPv4;
/// <summary>
/// IPv4 CIDR网络计算器 测试
/// </summary>
[Trait("Bing.Net", "CidrCalculator")]
public class IPv4CidrCalculatorTest : TestBase
{
    /// <inheritdoc />
    public IPv4CidrCalculatorTest(ITestOutputHelper output) : base(output)
    {
    }
    #region IsInSubnet 测试
    /// <summary>
    /// 测试 - IsInSubnet - 正常情况下的子网判断
    /// </summary>
    [Theory]
    [InlineData("192.168.1.100", "192.168.1.0/24", true)]
    [InlineData("192.168.1.1", "192.168.1.0/24", true)]
    [InlineData("192.168.1.254", "192.168.1.0/24", true)]
    [InlineData("192.168.2.1", "192.168.1.0/24", false)]
    [InlineData("10.0.0.1", "192.168.1.0/24", false)]
    [InlineData("192.168.1.0", "192.168.1.0/24", true)]
    [InlineData("192.168.1.255", "192.168.1.0/24", true)]
    public void IsInSubnet_VariousIps_ReturnsExpectedResult(string ip, string cidr, bool expected)
    {
        // Act
        var result = IPv4CidrCalculator.IsInSubnet(ip, cidr);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsInSubnet - 有效IP在子网内
    /// </summary>
    [Theory]
    [InlineData("192.168.1.100", "192.168.1.0/24", true)]
    [InlineData("192.168.1.1", "192.168.1.0/24", true)]
    [InlineData("192.168.1.255", "192.168.1.0/24", true)]
    [InlineData("10.0.0.1", "10.0.0.0/8", true)]
    [InlineData("172.16.5.10", "172.16.0.0/16", true)]
    [InlineData("192.168.1.1", "192.168.1.0/30", true)]
    [InlineData("192.168.1.2", "192.168.1.0/30", true)]
    public void IsInSubnet_ValidIPInSubnet_ReturnsTrue(string ip, string cidr, bool expected)
    {
        // Act
        var result = IPv4CidrCalculator.IsInSubnet(ip, cidr);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsInSubnet - 有效IP不在子网内
    /// </summary>
    [Theory]
    [InlineData("192.168.2.1", "192.168.1.0/24", false)]
    [InlineData("10.1.0.1", "192.168.1.0/24", false)]
    [InlineData("192.168.1.5", "192.168.1.0/30", false)]
    [InlineData("172.17.0.1", "172.16.0.0/16", false)]
    public void IsInSubnet_ValidIPNotInSubnet_ReturnsFalse(string ip, string cidr, bool expected)
    {
        // Act
        var result = IPv4CidrCalculator.IsInSubnet(ip, cidr);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - IsInSubnet - 无效输入返回false
    /// </summary>
    [Theory]
    [InlineData("invalid", "192.168.1.0/24")]
    [InlineData("192.168.1.1", "invalid")]
    [InlineData("192.168.1.1", "192.168.1.0/33")]
    [InlineData("192.168.1.1", "192.168.1.0/-1")]
    [InlineData("192.168.1.1", "192.168.1.0")]
    [InlineData("", "192.168.1.0/24")]
    [InlineData("192.168.1.1", "")]
    [InlineData(null, "192.168.1.0/24")]
    [InlineData("192.168.1.1", null)]
    public void IsInSubnet_InvalidInput_ReturnsFalse(string ip, string cidr)
    {
        // Act
        var result = IPv4CidrCalculator.IsInSubnet(ip, cidr);
        // Assert
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - IsInSubnet - 边界情况
    /// </summary>
    [Theory]
    [InlineData("0.0.0.0", "0.0.0.0/0", true)]  // 任意地址网段
    [InlineData("255.255.255.255", "0.0.0.0/0", true)]
    [InlineData("192.168.1.1", "192.168.1.1/32", true)]  // 主机路由
    [InlineData("192.168.1.2", "192.168.1.1/32", false)]
    [InlineData("192.168.1.0", "192.168.1.0/31", true)]  // 点对点链路
    [InlineData("192.168.1.1", "192.168.1.0/31", true)]
    [InlineData("192.168.1.2", "192.168.1.0/31", false)]
    public void IsInSubnet_EdgeCases_ReturnsExpectedResult(string ip, string cidr, bool expected)
    {
        // Act
        var result = IPv4CidrCalculator.IsInSubnet(ip, cidr);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region GenerateIpRange  测试
    /// <summary>
    /// 测试 - GenerateIpRange - 小型网段生成IP列表
    /// </summary>
    [Fact]
    public void GenerateIpRange_SmallSubnet_GeneratesAllIps()
    {
        // Arrange
        var cidr = "192.168.1.0/30";
        // Act
        var result = IPv4CidrCalculator.GenerateIpRange(cidr);
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(4);
        result[0].ShouldBe("192.168.1.0");
        result[1].ShouldBe("192.168.1.1");
        result[2].ShouldBe("192.168.1.2");
        result[3].ShouldBe("192.168.1.3");
    }
    /// <summary>
    /// 测试 - GenerateIpRange - 小网段生成正确数量
    /// </summary>
    [Theory]
    [InlineData("192.168.1.0/30", 4)] // /30 网段包含4个地址
    [InlineData("10.0.0.0/29", 8)]    // /29 网段包含8个地址
    [InlineData("172.16.0.0/28", 16)] // /28 网段包含16个地址
    public void GenerateIpRange_SmallSubnet_ReturnsCorrectCount(string cidr, int expectedCount)
    {
        // Act
        var result = IPv4CidrCalculator.GenerateIpRange(cidr);
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(expectedCount);
        // 验证所有生成的IP都在子网内
        foreach (var ip in result)
        {
            IPv4CidrCalculator.IsInSubnet(ip, cidr).ShouldBeTrue();
        }
    }
    /// <summary>
    /// 测试 - GenerateIpRange - 空CIDR抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateIpRange_NullOrEmptyCidr_ThrowsArgumentException(string cidr)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4CidrCalculator.GenerateIpRange(cidr))
            .ParamName.ShouldBe("cidr");
    }
    /// <summary>
    /// 测试 - GenerateIpRange - 无效CIDR格式抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("192.168.1.0")]
    [InlineData("192.168.1.0/")]
    [InlineData("192.168.1.0/abc")]
    [InlineData("192.168.1.0/33")]
    [InlineData("192.168.1.0/-1")]
    [InlineData("256.1.1.0/24")]
    public void GenerateIpRange_InvalidCidrFormat_ThrowsArgumentException(string cidr)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4CidrCalculator.GenerateIpRange(cidr))
            .ParamName.ShouldBe("cidr");
    }
    /// <summary>
    /// 测试 - GenerateIpRange - 单主机网段
    /// </summary>
    [Fact]
    public void GenerateIpRange_HostRoute_GeneratesSingleIp()
    {
        // Arrange
        var cidr = "192.168.1.100/32";
        // Act
        var result = IPv4CidrCalculator.GenerateIpRange(cidr);
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ShouldBe("192.168.1.100");
    }
    /// <summary>
    /// 测试 - GenerateIpRange - 过大网段超过限制抛出异常
    /// </summary>
    [Fact]
    public void GenerateIpRange_LargeSubnetExceedsLimit_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => IPv4CidrCalculator.GenerateIpRange("10.0.0.0/8", 1000))
            .ParamName.ShouldBe("maxCount");
    }
    /// <summary>
    /// 测试 - GenerateIpRange - 超出最大数量限制抛出异常
    /// </summary>
    [Fact]
    public void GenerateIpRange_ExceedsMaxCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var cidr = "192.168.0.0/16"; // 65536个地址
        var maxCount = 1000;
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            IPv4CidrCalculator.GenerateIpRange(cidr, maxCount));
    }
    /// <summary>
    /// 测试 - GenerateIpRange - 无效最大数量抛出异常
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void GenerateIpRange_InvalidMaxCount_ThrowsArgumentOutOfRangeException(int maxCount)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => IPv4CidrCalculator.GenerateIpRange("192.168.1.0/30", maxCount))
            .ParamName.ShouldBe("maxCount");
    }
    #endregion
    #region SubnetMaskToCidr 测试
    /// <summary>
    /// 测试 - SubnetMaskToCidr - 标准子网掩码转换
    /// </summary>
    [Theory]
    [InlineData("255.255.255.0", 24)]
    [InlineData("255.255.0.0", 16)]
    [InlineData("255.0.0.0", 8)]
    [InlineData("255.255.255.255", 32)]
    [InlineData("0.0.0.0", 0)]
    [InlineData("255.255.255.128", 25)]
    [InlineData("255.255.255.192", 26)]
    [InlineData("255.255.255.224", 27)]
    [InlineData("255.255.255.240", 28)]
    [InlineData("255.255.255.248", 29)]
    [InlineData("255.255.255.252", 30)]
    [InlineData("255.255.255.254", 31)]
    public void SubnetMaskToCidr_StandardMasks_ReturnsCorrectPrefix(string mask, int expected)
    {
        // Act
        var result = IPv4CidrCalculator.SubnetMaskToCidr(mask);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - SubnetMaskToCidr - 空或无效掩码抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("256.0.0.0")]
    [InlineData("192.168.1")]
    public void SubnetMaskToCidr_NullOrInvalidMask_ThrowsArgumentException(string mask)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4CidrCalculator.SubnetMaskToCidr(mask))
            .ParamName.ShouldBe("subnetMask");
    }
    #endregion
    #region CidrToSubnetMask 测试
    /// <summary>
    /// 测试 - CidrToSubnetMask - 有效前缀转换
    /// </summary>
    [Theory]
    [InlineData(0, "0.0.0.0")]           // 边界：最小前缀
    [InlineData(1, "128.0.0.0")]         // 单个网络位
    [InlineData(8, "255.0.0.0")]         // A类网络
    [InlineData(16, "255.255.0.0")]      // B类网络
    [InlineData(24, "255.255.255.0")]    // C类网络
    [InlineData(25, "255.255.255.128")]  // 子网划分
    [InlineData(26, "255.255.255.192")]  // 更小子网
    [InlineData(27, "255.255.255.224")]  // 更小子网
    [InlineData(28, "255.255.255.240")]  // 更小子网
    [InlineData(29, "255.255.255.248")]  // 更小子网
    [InlineData(30, "255.255.255.252")]  // 点对点链路
    [InlineData(31, "255.255.255.254")]  // RFC 3021
    [InlineData(32, "255.255.255.255")]  // 边界：最大前缀
    public void CidrToSubnetMask_ValidPrefix_ReturnsCorrectMask(int prefix, string expected)
    {
        // Act
        var result = IPv4CidrCalculator.CidrToSubnetMask(prefix);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - CidrToSubnetMask - 无效前缀抛出异常
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(33)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void CidrToSubnetMask_InvalidPrefix_ThrowsArgumentOutOfRangeException(int prefix)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => IPv4CidrCalculator.CidrToSubnetMask(prefix))
            .ParamName.ShouldBe("prefixLength");
    }
    /// <summary>
    /// 测试 - CidrToSubnetMask - 往返转换一致性
    /// </summary>
    [Fact]
    public void CidrToSubnetMask_RoundTripConversion_MaintainsConsistency()
    {
        // 测试所有有效的前缀长度 0-32
        for (int prefix = 0; prefix <= 32; prefix++)
        {
            // Act
            var mask = IPv4CidrCalculator.CidrToSubnetMask(prefix);
            var backToPrefix = IPv4CidrCalculator.SubnetMaskToCidr(mask);
            // Assert
            backToPrefix.ShouldBe(prefix, $"前缀 {prefix} 的往返转换失败");
        }
    }
    /// <summary>
    /// 测试 - CidrToSubnetMask - 掩码的二进制连续性验证
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    public void CidrToSubnetMask_BinaryContinuity_ValidatesBitPattern(int prefix)
    {
        // Act
        var mask = IPv4CidrCalculator.CidrToSubnetMask(prefix);
        var maskNum = IPv4Converter.IpToUInt32(mask);
        // Assert - 验证掩码的二进制表示是连续的1
        var binaryMask = Convert.ToString(maskNum, 2).PadLeft(32, '0');
        Output.WriteLine($"前缀 {prefix}: 掩码 {mask}, 二进制: {binaryMask}");
        // 验证二进制表示：前prefix位应该是1，后面应该是0
        for (var i = 0; i < 32; i++)
        {
            var expectedBit = i < prefix;
            var actualBit = binaryMask[i] == '1';
            actualBit.ShouldBe(expectedBit, $"前缀 {prefix} 在位 {i} 处的位值不正确");
        }
    }
    /// <summary>
    /// 测试 - CidrToSubnetMask - 实际子网计算验证
    /// </summary>
    [Theory]
    [InlineData(24, "192.168.1.100", "192.168.1.0", "192.168.1.255")]
    [InlineData(16, "10.1.2.3", "10.1.0.0", "10.1.255.255")]
    [InlineData(8, "172.16.5.4", "172.0.0.0", "172.255.255.255")]
    [InlineData(30, "192.168.1.5", "192.168.1.4", "192.168.1.7")]
    public void CidrToSubnetMask_SubnetCalculation_WorksCorrectly(int prefix, string testIp, string expectedNetwork, string expectedBroadcast)
    {
        // Act
        var mask = IPv4CidrCalculator.CidrToSubnetMask(prefix);
        var networkAddress = IPv4Converter.GetNetworkAddress(testIp, mask);
        var broadcastAddress = IPv4Converter.GetBroadcastAddress(testIp, mask);
        // Assert
        networkAddress.ShouldBe(expectedNetwork);
        broadcastAddress.ShouldBe(expectedBroadcast);
    }
    #endregion
    #region GetSubnetInfo 测试
    /// <summary>
    /// 测试 - GetSubnetInfo - 标准网段信息获取
    /// </summary>
    [Fact]
    public void GetSubnetInfo_StandardSubnet_ReturnsCompleteInfo()
    {
        // Arrange
        var cidr = "192.168.1.0/24";
        // Act
        var result = IPv4CidrCalculator.GetSubnetInfo(cidr);
        // Assert
        result.ShouldNotBeNull();
        result.NetworkAddress.ShouldBe("192.168.1.0");
        result.BroadcastAddress.ShouldBe("192.168.1.255");
        result.SubnetMask.ShouldBe("255.255.255.0");
        result.PrefixLength.ShouldBe(24);
        result.TotalHosts.ShouldBe(256u);
        result.AvailableHosts.ShouldBe(254u);
        result.FirstUsableIp.ShouldBe("192.168.1.1");
        result.LastUsableIp.ShouldBe("192.168.1.254");
    }
    /// <summary>
    /// 测试 - GetSubnetInfo - 主机路由信息
    /// </summary>
    [Fact]
    public void GetSubnetInfo_HostRoute_ReturnsHostInfo()
    {
        // Arrange
        var cidr = "192.168.1.100/32";
        // Act
        var result = IPv4CidrCalculator.GetSubnetInfo(cidr);
        // Assert
        result.ShouldNotBeNull();
        result.NetworkAddress.ShouldBe("192.168.1.100");
        result.BroadcastAddress.ShouldBe("192.168.1.100");
        result.TotalHosts.ShouldBe(1u);
        result.AvailableHosts.ShouldBe(1u);
        result.FirstUsableIp.ShouldBe("192.168.1.100");
        result.LastUsableIp.ShouldBe("192.168.1.100");
    }
    /// <summary>
    /// 测试 - GetSubnetInfo - 点对点网段信息
    /// </summary>
    [Fact]
    public void GetSubnetInfo_PointToPointSubnet_ReturnsCorrectInfo()
    {
        // Arrange
        var cidr = "192.168.1.0/30";
        // Act
        var result = IPv4CidrCalculator.GetSubnetInfo(cidr);
        // Assert
        result.ShouldNotBeNull();
        result.NetworkAddress.ShouldBe("192.168.1.0");
        result.BroadcastAddress.ShouldBe("192.168.1.3");
        result.SubnetMask.ShouldBe("255.255.255.252");
        result.PrefixLength.ShouldBe(30);
        result.TotalHosts.ShouldBe(4u);
        result.AvailableHosts.ShouldBe(2u);
        result.FirstUsableIp.ShouldBe("192.168.1.1");
        result.LastUsableIp.ShouldBe("192.168.1.2");
    }
    /// <summary>
    /// 测试 - GetSubnetInfo - 无效CIDR抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("192.168.1.0")]
    [InlineData("192.168.1.0/33")]
    [InlineData("192.168.1.0/-1")]
    public void GetSubnetInfo_InvalidCidr_ThrowsArgumentException(string cidr)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4CidrCalculator.GetSubnetInfo(cidr))
            .ParamName.ShouldBe("cidr");
    }
    #endregion
    #region SubdivideNetwork 测试
    /// <summary>
    /// 测试 - SubdivideNetwork - 标准子网划分
    /// </summary>
    [Fact]
    public void SubdivideNetwork_StandardSubdivision_ReturnsCorrectSubnets()
    {
        // Arrange
        var cidr = "192.168.1.0/24";
        var newPrefix = 26;
        // Act
        var result = IPv4CidrCalculator.SubdivideNetwork(cidr, newPrefix);
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(4);
        result[0].ShouldBe("192.168.1.0/26");
        result[1].ShouldBe("192.168.1.64/26");
        result[2].ShouldBe("192.168.1.128/26");
        result[3].ShouldBe("192.168.1.192/26");
    }
    /// <summary>
    /// 测试 - SubdivideNetwork - 新前缀不大于原前缀抛出异常
    /// </summary>
    [Theory]
    [InlineData(24)]
    [InlineData(23)]
    [InlineData(16)]
    public void SubdivideNetwork_NewPrefixNotGreater_ThrowsArgumentException(int newPrefix)
    {
        // Arrange
        var cidr = "192.168.1.0/24";
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4CidrCalculator.SubdivideNetwork(cidr, newPrefix))
            .ParamName.ShouldBe("newPrefixLength");
    }
    /// <summary>
    /// 测试 - SubdivideNetwork - 无效CIDR抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    public void SubdivideNetwork_InvalidCidr_ThrowsArgumentException(string cidr)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4CidrCalculator.SubdivideNetwork(cidr, 26))
            .ParamName.ShouldBe("cidr");
    }
    #endregion
    #region GetNetworkIntersection 测试
    /// <summary>
    /// 测试 - GetNetworkIntersection - 有交集的网段
    /// </summary>
    [Theory]
    [InlineData("192.168.1.0/24", "192.168.1.128/25", "192.168.1.128/25")]
    [InlineData("192.168.0.0/16", "192.168.1.0/24", "192.168.1.0/24")]
    [InlineData("10.0.0.0/8", "10.10.0.0/16", "10.10.0.0/16")]
    [InlineData("10.0.0.0/16", "10.0.1.0/24", "10.0.1.0/24")]
    public void GetNetworkIntersection_OverlappingNetworks_ReturnsIntersection(string cidr1, string cidr2, string expected)
    {
        // Act
        var result = IPv4CidrCalculator.GetNetworkIntersection(cidr1, cidr2);
        // Assert
        result.ShouldBe(expected);
        // 验证返回的交集确实包含在两个原网段中
        var intersection = IPv4CidrCalculator.GetSubnetInfo(result);
        IPv4CidrCalculator.IsInSubnet(intersection.NetworkAddress, cidr1).ShouldBeTrue();
        IPv4CidrCalculator.IsInSubnet(intersection.NetworkAddress, cidr2).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - GetNetworkIntersection - 无交集的网段
    /// </summary>
    [Theory]
    [InlineData("192.168.1.0/24", "192.168.2.0/24")]
    [InlineData("10.0.0.0/24", "172.16.0.0/24")]
    public void GetNetworkIntersection_NonOverlappingNetworks_ReturnsNull(string cidr1, string cidr2)
    {
        // Act
        var result = IPv4CidrCalculator.GetNetworkIntersection(cidr1, cidr2);
        // Assert
        result.ShouldBeNull();
    }
    /// <summary>
    /// 测试 - GetNetworkIntersection - 无效输入返回null
    /// </summary>
    [Theory]
    [InlineData(null, "192.168.1.0/24")]
    [InlineData("192.168.1.0/24", null)]
    [InlineData("", "192.168.1.0/24")]
    [InlineData("192.168.1.0/24", "")]
    [InlineData("invalid", "192.168.1.0/24")]
    [InlineData("192.168.1.0/24", "invalid")]
    public void GetNetworkIntersection_InvalidInput_ReturnsNull(string cidr1, string cidr2)
    {
        // Act
        var result = IPv4CidrCalculator.GetNetworkIntersection(cidr1, cidr2);
        // Assert
        result.ShouldBeNull();
    }
    #endregion
    #region AggregateNetworks 测试
    /// <summary>
    /// 测试 - AggregateNetworks - 可聚合的相邻网段
    /// </summary>
    [Fact]
    public void AggregateNetworks_AdjacentNetworks_ReturnsAggregated()
    {
        // Arrange
        var cidrs = new[] { "192.168.0.0/25", "192.168.0.128/25" };
        // Act
        var result = IPv4CidrCalculator.AggregateNetworks(cidrs);
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ShouldBe("192.168.0.0/24");
    }
    /// <summary>
    /// 测试 - AggregateNetworks - 不可聚合的网段
    /// </summary>
    [Fact]
    public void AggregateNetworks_NonAdjacentNetworks_ReturnsOriginal()
    {
        // Arrange
        var cidrs = new[] { "192.168.1.0/24", "192.168.3.0/24" };
        // Act
        var result = IPv4CidrCalculator.AggregateNetworks(cidrs);
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain("192.168.1.0/24");
        result.ShouldContain("192.168.3.0/24");
    }
    /// <summary>
    /// 测试 - AggregateNetworks - 空列表或单个网段
    /// </summary>
    [Fact]
    public void AggregateNetworks_EmptyOrSingleNetwork_ReturnsOriginal()
    {
        // Act
        var emptyResult = IPv4CidrCalculator.AggregateNetworks(new List<string>());
        var singleResult = IPv4CidrCalculator.AggregateNetworks(new[] { "192.168.1.0/24" });
        // Assert
        emptyResult.ShouldBeEmpty();
        singleResult.Count.ShouldBe(1);
        singleResult[0].ShouldBe("192.168.1.0/24");
    }
    /// <summary>
    /// 测试 - AggregateNetworks - null输入抛出异常
    /// </summary>
    [Fact]
    public void AggregateNetworks_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => IPv4CidrCalculator.AggregateNetworks(null))
            .ParamName.ShouldBe("cidrs");
    }
    /// <summary>
    /// 测试 - AggregateNetworks - 包含无效CIDR的列表
    /// </summary>
    [Fact]
    public void AggregateNetworks_ListWithInvalidCidrs_HandlesGracefully()
    {
        // Arrange
        var cidrs = new[] { "192.168.1.0/24", "invalid", "10.0.0.0/8", "" };
        // Act
        var result = IPv4CidrCalculator.AggregateNetworks(cidrs);
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThan(0);
    }
    #endregion
    #region TryAggregate 测试
    /// <summary>
    /// 测试 - TryAggregate - 成功聚合相邻网段
    /// </summary>
    [Theory]
    [InlineData("192.168.0.0/25", "192.168.0.128/25", "192.168.0.0/24")]
    [InlineData("10.0.0.0/26", "10.0.0.64/26", "10.0.0.0/25")]
    public void TryAggregate_AdjacentNetworks_ReturnsAggregated(string cidr1, string cidr2, string expected)
    {
        // Act
        var result = IPv4CidrCalculator.TryAggregate(cidr1, cidr2);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - TryAggregate - 无法聚合返回null
    /// </summary>
    [Theory]
    [InlineData("192.168.1.0/24", "192.168.3.0/24")] // 不相邻
    [InlineData("192.168.1.0/24", "192.168.1.0/25")] // 大小不同
    public void TryAggregate_CannotAggregate_ReturnsNull(string cidr1, string cidr2)
    {
        // Act
        var result = IPv4CidrCalculator.TryAggregate(cidr1, cidr2);
        // Assert
        result.ShouldBeNull();
    }
    /// <summary>
    /// 测试 - TryAggregate - 无效输入返回null
    /// </summary>
    [Theory]
    [InlineData("invalid", "192.168.1.0/24")]
    [InlineData("192.168.1.0/24", "invalid")]
    [InlineData("", "192.168.1.0/24")]
    [InlineData(null, "192.168.1.0/24")]
    public void TryAggregate_InvalidInput_ReturnsNull(string cidr1, string cidr2)
    {
        // Act
        var result = IPv4CidrCalculator.TryAggregate(cidr1, cidr2);
        // Assert
        result.ShouldBeNull();
    }
    #endregion
    #region 边界和性能测试
    /// <summary>
    /// 测试 - 边界情况 - 最小和最大前缀长度
    /// </summary>
    [Theory]
    [InlineData(0)]  // 任何地址
    [InlineData(32)] // 单个主机
    public void EdgeCase_MinMaxPrefixLength_HandlesCorrectly(int prefix)
    {
        // Act & Assert - 这些操作都应该成功
        Should.NotThrow(() => IPv4CidrCalculator.CidrToSubnetMask(prefix));
        Should.NotThrow(() => IPv4CidrCalculator.GetSubnetInfo($"192.168.1.0/{prefix}"));
    }
    /// <summary>
    /// 测试 - 性能 - 大量CIDR转换操作
    /// </summary>
    [Fact]
    public void Performance_MassiveCidrConversions_CompletesQuickly()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const int iterations = 1000;
        // Act
        for (int i = 0; i < iterations; i++)
        {
            for (int prefix = 0; prefix <= 32; prefix++)
            {
                IPv4CidrCalculator.CidrToSubnetMask(prefix);
                IPv4CidrCalculator.SubnetMaskToCidr(IPv4CidrCalculator.CidrToSubnetMask(prefix));
            }
        }
        stopwatch.Stop();
        // Assert
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(2000, "性能测试超时");
    }
    #endregion
}
