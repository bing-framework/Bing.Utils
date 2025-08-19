using Bing.Tests;

namespace Bing.Net.IPv6;

/// <summary>
/// IPv6 CIDR计算器单元测试
/// </summary>
[Trait("Bing.Net", "IpCidrCalculator")]
public class IPv6CidrCalculatorTest : TestBase
{
    /// <inheritdoc />
    public IPv6CidrCalculatorTest(ITestOutputHelper output) : base(output)
    {
    }

    #region IsInIPv6Subnet 测试

    /// <summary>
    /// 测试 - IsInIPv6Subnet - 地址在子网内
    /// </summary>
    [Theory]
    [InlineData("2001:db8::1", "2001:db8::/32", true)]
    [InlineData("2001:db8:1234:5678::1", "2001:db8::/32", true)]
    [InlineData("2001:db8::ffff", "2001:db8::/32", true)]
    [InlineData("::1", "::/0", true)]  // 所有地址都在 ::/0 中
    [InlineData("fe80::1", "fe80::/10", true)]
    [InlineData("2001:db8:1:2:3:4:5:6", "2001:db8:1:2::/64", true)]
    public void IsInIPv6Subnet_AddressInSubnet_ReturnsTrue(string address, string subnet, bool expected)
    {
        // Act
        var result = IPv6CidrCalculator.IsInIPv6Subnet(address, subnet);

        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"地址 '{address}' 在子网 '{subnet}' 中: {result}");
    }

    /// <summary>
    /// 测试 - IsInIPv6Subnet - 地址不在子网内
    /// </summary>
    [Theory]
    [InlineData("2001:db9::1", "2001:db8::/32", false)]
    [InlineData("fe80::1", "2001:db8::/32", false)]
    [InlineData("::1", "2001:db8::/32", false)]
    [InlineData("2001:db8:1:2:3:4:5:6", "2001:db8:1:3::/64", false)]
    public void IsInIPv6Subnet_AddressNotInSubnet_ReturnsFalse(string address, string subnet, bool expected)
    {
        // Act
        var result = IPv6CidrCalculator.IsInIPv6Subnet(address, subnet);

        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"地址 '{address}' 不在子网 '{subnet}' 中: {result}");
    }

    /// <summary>
    /// 测试 - IsInIPv6Subnet - 无效输入返回false
    /// </summary>
    [Theory]
    [InlineData("invalid", "2001:db8::/32")]
    [InlineData("2001:db8::1", "invalid")]
    [InlineData("2001:db8::1", "2001:db8::")]  // 缺少前缀长度
    [InlineData("2001:db8::1", "2001:db8::/129")]  // 无效前缀长度
    [InlineData("", "2001:db8::/32")]
    [InlineData("2001:db8::1", "")]
    [InlineData(null, "2001:db8::/32")]
    [InlineData("2001:db8::1", null)]
    public void IsInIPv6Subnet_InvalidInput_ReturnsFalse(string address, string subnet)
    {
        // Act
        var result = IPv6CidrCalculator.IsInIPv6Subnet(address, subnet);

        // Assert
        result.ShouldBeFalse();
        Output.WriteLine($"无效输入 '{address}', '{subnet}' 返回 false");
    }

    #endregion

    #region GetIPv6SubnetInfo 测试

    /// <summary>
    /// 测试 - GetIPv6SubnetInfo - 基本子网信息
    /// </summary>
    [Theory]
    [InlineData("2001:db8::/64", 64, 64)]
    [InlineData("2001:db8:1234:5678::/64", 64, 64)]
    [InlineData("fe80::/10", 10, 118)]
    [InlineData("::/0", 0, 128)]
    [InlineData("2001:db8::1/128", 128, 0)]
    public void GetIPv6SubnetInfo_ValidCidr_ReturnsCorrectInfo(string cidr, int expectedPrefixLength, int expectedHostBits)
    {
        // Act
        var info = IPv6CidrCalculator.GetIPv6SubnetInfo(cidr);

        // Assert
        info.ShouldNotBeNull();
        info.PrefixLength.ShouldBe(expectedPrefixLength);
        info.HostBits.ShouldBe(expectedHostBits);
        info.NetworkPrefix.ShouldNotBeNullOrEmpty();
        info.SubnetMask.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(info.NetworkPrefix).ShouldBeTrue();
        IPv6Validator.IsValid(info.SubnetMask).ShouldBeTrue();

        Output.WriteLine($"子网信息: {cidr}");
        Output.WriteLine($"  网络前缀: {info.NetworkPrefix}");
        Output.WriteLine($"  前缀长度: {info.PrefixLength}");
        Output.WriteLine($"  主机位数: {info.HostBits}");
        Output.WriteLine($"  子网掩码: {info.SubnetMask}");
        Output.WriteLine($"  总地址数: {info.TotalAddresses?.ToString() ?? "无法计算"}");
        Output.WriteLine($"  可用地址数: {info.AvailableAddresses?.ToString() ?? "无法计算"}");
        Output.WriteLine($"  第一个地址: {info.FirstUsableAddress}");
        Output.WriteLine($"  最后一个地址: {info.LastUsableAddress}");
    }

    /// <summary>
    /// 测试 - GetIPv6SubnetInfo - 小子网地址计算
    /// </summary>
    [Theory]
    [InlineData("2001:db8::/126", 4)]  // 4个地址
    [InlineData("2001:db8::/127", 2)]  // 2个地址
    [InlineData("2001:db8::/120", 256)] // 256个地址
    public void GetIPv6SubnetInfo_SmallSubnets_CalculatesAddressCount(string cidr, ulong expectedTotal)
    {
        // Act
        var info = IPv6CidrCalculator.GetIPv6SubnetInfo(cidr);

        // Assert
        info.TotalAddresses.ShouldNotBeNull();
        info.TotalAddresses.Value.ShouldBe(expectedTotal);
        info.AvailableAddresses.ShouldNotBeNull();

        if (expectedTotal > 2)
        {
            info.AvailableAddresses.Value.ShouldBe(expectedTotal - 2);
        }
        else
        {
            info.AvailableAddresses.Value.ShouldBe(expectedTotal);
        }

        Output.WriteLine($"小子网 {cidr}: 总地址={info.TotalAddresses}, 可用地址={info.AvailableAddresses}");
    }

    /// <summary>
    /// 测试 - GetIPv6SubnetInfo - 大子网处理
    /// </summary>
    [Theory]
    [InlineData("2001:db8::/32")]  // 96位主机部分
    [InlineData("2001:db8::/48")]  // 80位主机部分
    [InlineData("::/0")]           // 128位主机部分
    public void GetIPv6SubnetInfo_LargeSubnets_HandlesCorrectly(string cidr)
    {
        // Act
        var info = IPv6CidrCalculator.GetIPv6SubnetInfo(cidr);

        // Assert
        info.ShouldNotBeNull();
        info.HostBits.ShouldBeGreaterThanOrEqualTo(64);

        // 大子网的地址数应该为null（太大无法计算）
        if (info.HostBits >= 64)
        {
            info.TotalAddresses.ShouldBeNull();
            info.AvailableAddresses.ShouldBeNull();
        }

        Output.WriteLine($"大子网 {cidr}: 主机位={info.HostBits}位, 地址数=无法计算");
    }

    /// <summary>
    /// 测试 - GetIPv6SubnetInfo - 无效输入抛出异常
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("2001:db8::")]        // 缺少前缀长度
    [InlineData("2001:db8::/129")]    // 无效前缀长度
    [InlineData("2001:db8::/-1")]     // 负前缀长度
    [InlineData("invalid/64")]        // 无效IPv6地址
    public void GetIPv6SubnetInfo_InvalidInput_ThrowsException(string invalidCidr)
    {
        // Act & Assert
        if (string.IsNullOrEmpty(invalidCidr))
        {
            Should.Throw<ArgumentNullException>(() => IPv6CidrCalculator.GetIPv6SubnetInfo(invalidCidr));
        }
        else
        {
            Should.Throw<ArgumentException>(() => IPv6CidrCalculator.GetIPv6SubnetInfo(invalidCidr));
        }

        Output.WriteLine($"无效CIDR '{invalidCidr}' 正确抛出异常");
    }

    #endregion

    #region GenerateIPv6Range 测试

    /// <summary>
    /// 测试 - GenerateIPv6Range - 小子网地址生成
    /// </summary>
    [Theory]
    [InlineData("2001:db8::/126", 4, 4)]   // 4个地址
    [InlineData("2001:db8::/127", 2, 2)]   // 2个地址
    [InlineData("2001:db8::/125", 8, 8)]   // 8个地址
    public void GenerateIPv6Range_SmallSubnets_GeneratesAllAddresses(string cidr, int expectedCount, int maxCount)
    {
        // Act
        var addresses = IPv6CidrCalculator.GenerateIPv6Range(cidr, maxCount);

        // Assert
        addresses.Count.ShouldBe(expectedCount);

        // 验证所有地址都有效且在子网内
        foreach (var addr in addresses)
        {
            IPv6Validator.IsValid(addr).ShouldBeTrue();
            IPv6CidrCalculator.IsInIPv6Subnet(addr, cidr).ShouldBeTrue();
        }

        // 验证地址是连续的
        for (int i = 1; i < addresses.Count; i++)
        {
            var prev = IPv6Converter.ToBigInteger(addresses[i - 1]);
            var curr = IPv6Converter.ToBigInteger(addresses[i]);
            (curr - prev).ShouldBe(1, "地址应该是连续的");
        }

        Output.WriteLine($"小子网 {cidr} 生成的地址:");
        foreach (var addr in addresses)
        {
            Output.WriteLine($"  {addr}");
        }
    }

    /// <summary>
    /// 测试 - GenerateIPv6Range - 大子网限制处理
    /// </summary>
    [Theory]
    [InlineData("2001:db8::/32")]
    [InlineData("2001:db8::/48")]
    [InlineData("::/0")]
    public void GenerateIPv6Range_LargeSubnets_ReturnsLimitedAddresses(string cidr)
    {
        // Act
        var addresses = IPv6CidrCalculator.GenerateIPv6Range(cidr, 1000);

        // Assert
        addresses.Count.ShouldBeLessThanOrEqualTo(2); // 只返回网络地址和可能的第一个可用地址

        if (addresses.Count > 0)
        {
            IPv6Validator.IsValid(addresses[0]).ShouldBeTrue();
            IPv6CidrCalculator.IsInIPv6Subnet(addresses[0], cidr).ShouldBeTrue();
        }

        Output.WriteLine($"大子网 {cidr} 返回的有限地址数: {addresses.Count}");
        foreach (var addr in addresses)
        {
            Output.WriteLine($"  {addr}");
        }
    }

    /// <summary>
    /// 测试 - GenerateIPv6Range - 最大数量限制
    /// </summary>
    [Fact]
    public void GenerateIPv6Range_MaxCountLimit_RespectsLimit()
    {
        // Arrange
        var cidr = "2001:db8::/120"; // 256个地址
        var maxCount = 10;

        // Act
        var addresses = IPv6CidrCalculator.GenerateIPv6Range(cidr, maxCount);

        // Assert
        addresses.Count.ShouldBe(maxCount);

        // 验证前几个地址
        addresses[0].ShouldBe("2001:db8::");
        addresses[1].ShouldBe("2001:db8::1");
        addresses[2].ShouldBe("2001:db8::2");

        Output.WriteLine($"限制数量测试: 请求{maxCount}个，返回{addresses.Count}个");
    }

    /// <summary>
    /// 测试 - GenerateIPv6Range - 空输入处理
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    public void GenerateIPv6Range_InvalidInput_ReturnsEmptyList(string invalidCidr)
    {
        // Act
        var addresses = IPv6CidrCalculator.GenerateIPv6Range(invalidCidr);

        // Assert
        addresses.ShouldBeEmpty();
        Output.WriteLine($"无效输入 '{invalidCidr}' 返回空列表");
    }

    #endregion

    #region IPv6PrefixToSubnetMask 测试

    /// <summary>
    /// 测试 - IPv6PrefixToSubnetMask - 标准前缀长度
    /// </summary>
    [Theory]
    [InlineData(0, "::")]
    [InlineData(8, "ff00::")]
    [InlineData(16, "ffff::")]
    [InlineData(32, "ffff:ffff::")]
    [InlineData(48, "ffff:ffff:ffff::")]
    [InlineData(64, "ffff:ffff:ffff:ffff::")]
    [InlineData(128, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
    public void IPv6PrefixToSubnetMask_StandardPrefixes_ReturnsCorrectMask(int prefixLength, string expectedMask)
    {
        // Act
        var result = IPv6CidrCalculator.IPv6PrefixToSubnetMask(prefixLength);

        // Assert
        IPv6Converter.AreEqual(result, expectedMask).ShouldBeTrue($"前缀长度 {prefixLength} 应该产生掩码 '{expectedMask}', 实际为 '{result}'");
        Output.WriteLine($"前缀 /{prefixLength} -> 掩码 {result}");
    }

    /// <summary>
    /// 测试 - IPv6PrefixToSubnetMask - 部分字节前缀长度
    /// </summary>
    [Theory]
    [InlineData(1, "8000::")]
    [InlineData(4, "f000::")]
    [InlineData(12, "fff0::")]
    [InlineData(20, "ffff:f000::")]
    [InlineData(36, "ffff:ffff:f000::")]
    public void IPv6PrefixToSubnetMask_PartialBytePrefixes_ReturnsCorrectMask(int prefixLength, string expectedMask)
    {
        // Act
        var result = IPv6CidrCalculator.IPv6PrefixToSubnetMask(prefixLength);

        // Assert
        IPv6Converter.AreEqual(result, expectedMask).ShouldBeTrue($"前缀长度 {prefixLength} 应该产生掩码 '{expectedMask}', 实际为 '{result}'");
        Output.WriteLine($"部分字节前缀 /{prefixLength} -> 掩码 {result}");
    }

    /// <summary>
    /// 测试 - IPv6PrefixToSubnetMask - 无效前缀长度抛出异常
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(129)]
    [InlineData(200)]
    public void IPv6PrefixToSubnetMask_InvalidPrefix_ThrowsException(int invalidPrefix)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => IPv6CidrCalculator.IPv6PrefixToSubnetMask(invalidPrefix))
            .Message.ShouldContain("IPv6前缀长度必须在0-128之间");

        Output.WriteLine($"无效前缀长度 {invalidPrefix} 正确抛出异常");
    }

    #endregion

    #region IPv6SubnetMaskToPrefix 测试

    /// <summary>
    /// 测试 - IPv6SubnetMaskToPrefix - 标准子网掩码
    /// </summary>
    [Theory]
    [InlineData("::", 0)]
    [InlineData("ff00::", 8)]
    [InlineData("ffff::", 16)]
    [InlineData("ffff:ffff::", 32)]
    [InlineData("ffff:ffff:ffff::", 48)]
    [InlineData("ffff:ffff:ffff:ffff::", 64)]
    [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", 128)]
    public void IPv6SubnetMaskToPrefix_StandardMasks_ReturnsCorrectPrefix(string mask, int expectedPrefix)
    {
        // Act
        var result = IPv6CidrCalculator.IPv6SubnetMaskToPrefix(mask);

        // Assert
        result.ShouldBe(expectedPrefix);
        Output.WriteLine($"掩码 {mask} -> 前缀长度 /{result}");
    }

    /// <summary>
    /// 测试 - IPv6SubnetMaskToPrefix - 部分字节掩码
    /// </summary>
    [Theory]
    [InlineData("8000::", 1)]
    [InlineData("f000::", 4)]
    [InlineData("fff0::", 12)]
    [InlineData("ffff:f000::", 20)]
    [InlineData("ffff:ffff:f000::", 36)]
    public void IPv6SubnetMaskToPrefix_PartialByteMasks_ReturnsCorrectPrefix(string mask, int expectedPrefix)
    {
        // Act
        var result = IPv6CidrCalculator.IPv6SubnetMaskToPrefix(mask);

        // Assert
        result.ShouldBe(expectedPrefix);
        Output.WriteLine($"部分字节掩码 {mask} -> 前缀长度 /{result}");
    }

    /// <summary>
    /// 测试 - IPv6SubnetMaskToPrefix - 无效掩码抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("ff00:ff00::")]     // 非连续位
    [InlineData("ffff:00ff::")]     // 非连续位
    [InlineData("ff00::ff00")]      // 零之后有非零
    [InlineData("")]
    [InlineData("192.168.1.0")]     // IPv4地址
    public void IPv6SubnetMaskToPrefix_InvalidMask_ThrowsException(string invalidMask)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv6CidrCalculator.IPv6SubnetMaskToPrefix(invalidMask));
        Output.WriteLine($"无效掩码 '{invalidMask}' 正确抛出异常");
    }

    #endregion

    #region IsValidIPv6SubnetMask 测试

    /// <summary>
    /// 测试 - IsValidIPv6SubnetMask - 有效掩码
    /// </summary>
    [Theory]
    [InlineData("::")]
    [InlineData("ffff::")]
    [InlineData("ffff:ffff:ffff:ffff::")]
    [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
    [InlineData("f000::")]
    [InlineData("fff0::")]
    public void IsValidIPv6SubnetMask_ValidMasks_ReturnsTrue(string validMask)
    {
        // Act
        var result = IPv6CidrCalculator.IsValidIPv6SubnetMask(validMask);

        // Assert
        result.ShouldBeTrue();
        Output.WriteLine($"有效掩码: {validMask}");
    }

    /// <summary>
    /// 测试 - IsValidIPv6SubnetMask - 无效掩码
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("ff00:ff00::")]     // 非连续位
    [InlineData("ffff:00ff::")]     // 非连续位
    [InlineData("ff00::ff00")]      // 零之后有非零
    [InlineData("")]
    [InlineData(null)]
    [InlineData("192.168.1.0")]     // IPv4地址
    public void IsValidIPv6SubnetMask_InvalidMasks_ReturnsFalse(string invalidMask)
    {
        // Act
        var result = IPv6CidrCalculator.IsValidIPv6SubnetMask(invalidMask);

        // Assert
        result.ShouldBeFalse();
        Output.WriteLine($"无效掩码: '{invalidMask}'");
    }

    #endregion

    #region SubdivideIPv6Network 测试

    /// <summary>
    /// 测试 - SubdivideIPv6Network - 基本子网划分
    /// </summary>
    [Theory]
    [InlineData("2001:db8::/60", 64, 16)]  // /60 -> /64, 16个子网
    [InlineData("2001:db8::/62", 64, 4)]   // /62 -> /64, 4个子网
    [InlineData("2001:db8::/63", 64, 2)]   // /63 -> /64, 2个子网
    public void SubdivideIPv6Network_ValidInput_ReturnsCorrectSubnets(string originalCidr, int newPrefix, int expectedCount)
    {
        // Act
        var subnets = IPv6CidrCalculator.SubdivideIPv6Network(originalCidr, newPrefix);

        // Assert
        subnets.Count.ShouldBe(expectedCount);

        foreach (var subnet in subnets)
        {
            subnet.ShouldEndWith($"/{newPrefix}");

            // 验证子网格式
            var parts = subnet.Split('/');
            IPv6Validator.IsValid(parts[0]).ShouldBeTrue();
        }

        Output.WriteLine($"子网划分: {originalCidr} -> /{newPrefix}");
        foreach (var subnet in subnets.Take(Math.Min(5, subnets.Count)))
        {
            Output.WriteLine($"  {subnet}");
        }
        if (subnets.Count > 5)
        {
            Output.WriteLine($"  ... 还有 {subnets.Count - 5} 个子网");
        }
    }

    /// <summary>
    /// 测试 - SubdivideIPv6Network - 无效输入返回空列表
    /// </summary>
    [Theory]
    [InlineData("", 64)]
    [InlineData(null, 64)]
    [InlineData("invalid", 64)]
    [InlineData("2001:db8::", 64)]      // 缺少前缀长度
    [InlineData("2001:db8::/64", 60)]   // 新前缀小于原前缀
    [InlineData("2001:db8::/64", 64)]   // 新前缀等于原前缀
    public void SubdivideIPv6Network_InvalidInput_ReturnsEmptyList(string invalidCidr, int newPrefix)
    {
        // Act & Assert
        if (invalidCidr != null && invalidCidr.Contains("/64") && newPrefix <= 64)
        {
            // 新前缀长度必须大于原前缀长度
            Should.Throw<ArgumentException>(() => IPv6CidrCalculator.SubdivideIPv6Network(invalidCidr, newPrefix));
        }
        else
        {
            var result = IPv6CidrCalculator.SubdivideIPv6Network(invalidCidr, newPrefix);
            result.ShouldBeEmpty();
        }

        Output.WriteLine($"无效输入 '{invalidCidr}' -> /{newPrefix}");
    }

    /// <summary>
    /// 测试 - SubdivideIPv6Network - 过大子网数量抛出异常
    /// </summary>
    [Fact]
    public void SubdivideIPv6Network_TooManySubnets_ThrowsException()
    {
        // Arrange
        var cidr = "2001:db8::/48";
        var newPrefix = 64; // 这将产生 2^16 = 65536 个子网，超过10000限制

        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv6CidrCalculator.SubdivideIPv6Network(cidr, newPrefix))
            .Message.ShouldContain("子网数量过大");

        Output.WriteLine($"过大子网数量测试: {cidr} -> /{newPrefix}");
    }

    #endregion

    #region GetFirstIPv6Address 和 GetLastIPv6Address 测试

    /// <summary>
    /// 测试 - GetFirstIPv6Address - 获取第一个地址
    /// </summary>
    [Theory]
    [InlineData("2001:db8::", 64, "2001:db8::")]      // /64: 网络地址就是第一个地址
    [InlineData("2001:db8::", 126, "2001:db8::")]     // /126: 网络地址就是第一个地址
    [InlineData("2001:db8::", 127, "2001:db8::")]     // /127: 网络地址就是第一个地址
    [InlineData("2001:db8::", 128, "2001:db8::")]     // /128: 只有一个地址
    [InlineData("2001:db8::1", 128, "2001:db8::1")]   // /128: 单主机网络
    public void GetFirstIPv6Address_ValidInput_ReturnsFirstAddress(string network, int prefixLength, string expectedFirst)
    {
        // Act
        var result = IPv6CidrCalculator.GetFirstIPv6Address(network, prefixLength);

        // Assert
        IPv6Converter.AreEqual(result, expectedFirst).ShouldBeTrue($"网络 {network}/{prefixLength} 的第一个地址应该是 '{expectedFirst}', 实际为 '{result}'");
        Output.WriteLine($"网络 {network}/{prefixLength} 的第一个地址: {result}");
    }

    /// <summary>
    /// 测试 - GetLastIPv6Address - 获取最后一个地址
    /// </summary>
    [Theory]
    [InlineData("2001:db8::", 126, "2001:db8::3")]
    [InlineData("2001:db8::", 127, "2001:db8::1")]
    [InlineData("2001:db8::", 128, "2001:db8::")]
    public void GetLastIPv6Address_ValidInput_ReturnsLastAddress(string network, int prefixLength, string expectedLast)
    {
        // Act
        var result = IPv6CidrCalculator.GetLastIPv6Address(network, prefixLength);

        // Assert
        IPv6Converter.AreEqual(result, expectedLast).ShouldBeTrue();
        Output.WriteLine($"网络 {network}/{prefixLength} 的最后一个地址: {result}");
    }

    /// <summary>
    /// 测试 - GetFirstIPv6Address - 无效输入抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid", 64)]
    [InlineData("192.168.1.0", 64)]
    [InlineData("", 64)]
    [InlineData(null, 64)]
    public void GetFirstIPv6Address_InvalidNetwork_ThrowsException(string invalidNetwork, int prefixLength)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv6CidrCalculator.GetFirstIPv6Address(invalidNetwork, prefixLength));
    }

    /// <summary>
    /// 测试 - GetLastIPv6Address - 无效前缀长度抛出异常
    /// </summary>
    [Theory]
    [InlineData("2001:db8::", -1)]
    [InlineData("2001:db8::", 129)]
    public void GetLastIPv6Address_InvalidPrefixLength_ThrowsException(string network, int invalidPrefix)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => IPv6CidrCalculator.GetLastIPv6Address(network, invalidPrefix));
    }

    #endregion

    #region CompareIPv6Addresses 测试

    /// <summary>
    /// 测试 - CompareIPv6Addresses - 地址比较
    /// </summary>
    [Theory]
    [InlineData("::", "::1", -1)]
    [InlineData("::1", "::", 1)]
    [InlineData("::1", "::1", 0)]
    [InlineData("2001:db8::1", "2001:db8::2", -1)]
    [InlineData("2001:db8::2", "2001:db8::1", 1)]
    [InlineData("fe80::1", "2001:db8::1", 1)]  // fe80 > 2001
    public void CompareIPv6Addresses_VariousPairs_ReturnsCorrectComparison(string addr1, string addr2, int expected)
    {
        // Act
        var result = IPv6CidrCalculator.CompareIPv6Addresses(addr1, addr2);

        // Assert
        result.ShouldBe(expected);

        var comparison = expected < 0 ? "<" : expected > 0 ? ">" : "=";
        Output.WriteLine($"地址比较: '{addr1}' {comparison} '{addr2}' (结果: {result})");
    }

    /// <summary>
    /// 测试 - CompareIPv6Addresses - 无效地址抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid", "2001:db8::1")]
    [InlineData("2001:db8::1", "invalid")]
    [InlineData("192.168.1.1", "2001:db8::1")]
    public void CompareIPv6Addresses_InvalidAddress_ThrowsException(string addr1, string addr2)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv6CidrCalculator.CompareIPv6Addresses(addr1, addr2))
            .Message.ShouldContain("无效的IPv6地址");
    }

    #endregion

    #region 集成测试

    /// <summary>
    /// 测试 - 前缀和掩码转换的往返一致性
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(48)]
    [InlineData(64)]
    [InlineData(96)]
    [InlineData(128)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(15)]
    [InlineData(31)]
    public void PrefixMaskConversion_RoundTrip_MaintainsConsistency(int originalPrefix)
    {
        // Act
        var mask = IPv6CidrCalculator.IPv6PrefixToSubnetMask(originalPrefix);
        var backToPrefix = IPv6CidrCalculator.IPv6SubnetMaskToPrefix(mask);

        // Assert
        backToPrefix.ShouldBe(originalPrefix);
        Output.WriteLine($"往返转换: /{originalPrefix} -> {mask} -> /{backToPrefix}");
    }

    /// <summary>
    /// 测试 - 子网信息与地址生成的一致性
    /// </summary>
    [Theory]
    [InlineData("2001:db8::/126")]
    [InlineData("2001:db8::/127")]
    [InlineData("fe80::/127")]
    public void SubnetInfoAndGeneration_Consistency(string cidr)
    {
        // Act
        var info = IPv6CidrCalculator.GetIPv6SubnetInfo(cidr);
        var addresses = IPv6CidrCalculator.GenerateIPv6Range(cidr, 100);

        // Assert
        if (info.TotalAddresses.HasValue && info.TotalAddresses <= 100)
        {
            addresses.Count.ShouldBe((int)info.TotalAddresses.Value);

            if (addresses.Count > 0)
            {
                IPv6Converter.AreEqual(addresses.First(), info.FirstUsableAddress).ShouldBeTrue();
                IPv6Converter.AreEqual(addresses.Last(), info.LastUsableAddress).ShouldBeTrue();
            }
        }

        Output.WriteLine($"一致性测试: {cidr}");
        Output.WriteLine($"  子网信息总数: {info.TotalAddresses}");
        Output.WriteLine($"  生成地址数: {addresses.Count}");
    }

    /// <summary>
    /// 测试 - 子网包含关系验证
    /// </summary>
    [Fact]
    public void SubnetContainment_Integration()
    {
        // Arrange
        var parentCidr = "2001:db8::/60";
        var childPrefix = 64;

        // Act
        var childSubnets = IPv6CidrCalculator.SubdivideIPv6Network(parentCidr, childPrefix);

        // Assert
        foreach (var childCidr in childSubnets)
        {
            var childInfo = IPv6CidrCalculator.GetIPv6SubnetInfo(childCidr);

            // 验证子网的网络地址在父网络中
            IPv6CidrCalculator.IsInIPv6Subnet(childInfo.NetworkPrefix, parentCidr).ShouldBeTrue();

            // 验证子网的第一个和最后一个地址都在父网络中
            if (childInfo.FirstUsableAddress != null)
            {
                IPv6CidrCalculator.IsInIPv6Subnet(childInfo.FirstUsableAddress, parentCidr).ShouldBeTrue();
            }
            if (childInfo.LastUsableAddress != null)
            {
                IPv6CidrCalculator.IsInIPv6Subnet(childInfo.LastUsableAddress, parentCidr).ShouldBeTrue();
            }
        }

        Output.WriteLine($"子网包含关系验证: {parentCidr} 包含 {childSubnets.Count} 个 /{childPrefix} 子网");
    }

    #endregion

    #region 性能测试

    /// <summary>
    /// 测试 - 大量子网计算性能
    /// </summary>
    [Fact]
    public void SubnetCalculation_Performance()
    {
        // Arrange
        var testCidrs = new[]
        {
            "2001:db8::/64",
            "fe80::/64",
            "::1/128",
            "2001:db8:1234:5678::/64"
        };

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 1000; i++)
        {
            foreach (var cidr in testCidrs)
            {
                var info = IPv6CidrCalculator.GetIPv6SubnetInfo(cidr);
                var inSubnet = IPv6CidrCalculator.IsInIPv6Subnet("2001:db8::1", cidr);
            }
        }

        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.ShouldBeLessThan(1000, "1000次子网计算应该在1秒内完成");
        Output.WriteLine($"性能测试: 4000次子网操作耗时 {sw.ElapsedMilliseconds}ms");
    }

    #endregion
}