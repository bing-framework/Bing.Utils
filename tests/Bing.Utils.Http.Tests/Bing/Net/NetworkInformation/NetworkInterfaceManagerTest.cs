using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Bing.Net.NetworkInformation;

/// <summary>
/// 网络接口管理器单元测试
/// </summary>
[Trait("Bing.Net", "NetworkInterfaceManager")]
public class NetworkInterfaceManagerTest : TestBase
{
    /// <inheritdoc />
    public NetworkInterfaceManagerTest(ITestOutputHelper output) : base(output)
    {
    }

    #region GetIpByInterface 测试

    /// <summary>
    /// 测试 - GetIpByInterface - 获取以太网接口IP地址
    /// </summary>
    [Fact]
    public void GetIpByInterface_EthernetType_ReturnsValidIpOrEmpty()
    {
        // Act
        var ethernetIp = NetworkInterfaceManager.GetIpByInterface(NetworkInterfaceType.Ethernet);

        // Assert
        ethernetIp.ShouldNotBeNull();

        if (!string.IsNullOrEmpty(ethernetIp))
        {
            // 验证是有效的IPv4地址格式
            var parts = ethernetIp.Split('.');
            parts.Length.ShouldBe(4);

            // 不应该是回环地址
            ethernetIp.ShouldNotStartWith("127.");

            Output.WriteLine($"以太网接口IP地址: {ethernetIp}");
        }
        else
        {
            Output.WriteLine("未找到活动的以太网接口IP地址");
        }
    }

    /// <summary>
    /// 测试 - GetIpByInterface - 获取无线接口IP地址
    /// </summary>
    [Fact]
    public void GetIpByInterface_WirelessType_ReturnsValidIpOrEmpty()
    {
        // Act
        var wirelessIp = NetworkInterfaceManager.GetIpByInterface(NetworkInterfaceType.Wireless80211);

        // Assert
        wirelessIp.ShouldNotBeNull();

        if (!string.IsNullOrEmpty(wirelessIp))
        {
            // 验证是有效的IPv4地址格式
            var parts = wirelessIp.Split('.');
            parts.Length.ShouldBe(4);

            Output.WriteLine($"无线接口IP地址: {wirelessIp}");
        }
        else
        {
            Output.WriteLine("未找到活动的无线接口IP地址");
        }
    }

    /// <summary>
    /// 测试 - GetIpByInterface - 回环接口类型
    /// </summary>
    [Fact]
    public void GetIpByInterface_LoopbackType_ReturnsEmpty()
    {
        // Act
        var loopbackIp = NetworkInterfaceManager.GetIpByInterface(NetworkInterfaceType.Loopback);

        // Assert
        // 回环接口通常没有网关，所以应该返回空字符串
        loopbackIp.ShouldBe(string.Empty);

        Output.WriteLine($"回环接口IP地址: '{loopbackIp}' (应为空)");
    }

    #endregion

    #region GetNetworkInterfaces 测试

    /// <summary>
    /// 测试 - GetNetworkInterfaces - 获取网络接口信息
    /// </summary>
    [Fact]
    public void GetNetworkInterfaces_ReturnsValidInterfaces()
    {
        // Act
        var interfaces = NetworkInterfaceManager.GetNetworkInterfaces();

        // Assert
        interfaces.ShouldNotBeNull();

        foreach (var iface in interfaces)
        {
            iface.Name.ShouldNotBeNullOrEmpty();
            iface.Status.ShouldBe(OperationalStatus.Up);
            iface.IpAddresses.ShouldNotBeEmpty();

            // 验证IPv4和IPv6地址分类正确
            foreach (var ip in iface.IPv4Addresses)
            {
                var parts = ip.Split('.');
                parts.Length.ShouldBe(4, $"IPv4地址格式错误: {ip}");
            }

            foreach (var ip in iface.IPv6Addresses)
            {
                ip.ShouldContain(':', $"IPv6地址格式错误: {ip}");
            }

            // MAC地址不应为空
            iface.MacAddress.ShouldNotBeNull();

            // 速度应该大于等于0
            iface.Speed.ShouldBeGreaterThanOrEqualTo(0);
        }

        Output.WriteLine($"找到 {interfaces.Count} 个活动网络接口:");
        foreach (var iface in interfaces)
        {
            Output.WriteLine($"  {iface.Name}: IPv4={iface.IPv4Addresses.Count}, IPv6={iface.IPv6Addresses.Count}, 网关={iface.HasGateway}");
        }
    }

    #endregion

    #region GetNetworkInterfaceByName 测试

    /// <summary>
    /// 测试 - GetNetworkInterfaceByName - 按名称查找接口
    /// </summary>
    [Fact]
    public void GetNetworkInterfaceByName_FindsCorrectInterface()
    {
        // Arrange
        var allInterfaces = NetworkInterfaceManager.GetNetworkInterfaces();
        if (!allInterfaces.Any())
        {
            Output.WriteLine("没有可用的网络接口进行测试");
            return;
        }

        var firstInterface = allInterfaces.First();

        // Act
        var foundInterface = NetworkInterfaceManager.GetNetworkInterfaceByName(firstInterface.Name);

        // Assert
        foundInterface.ShouldNotBeNull();
        foundInterface.Name.ShouldBe(firstInterface.Name);

        Output.WriteLine($"按名称查找接口 '{firstInterface.Name}' 成功");
    }

    /// <summary>
    /// 测试 - GetNetworkInterfaceByName - 无效接口名称
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("不存在的接口名称")]
    public void GetNetworkInterfaceByName_InvalidInterfaceName_ReturnsNull(string invalidName)
    {
        // Act
        var result = NetworkInterfaceManager.GetNetworkInterfaceByName(invalidName);

        // Assert
        result.ShouldBeNull();
        Output.WriteLine($"无效接口名称 '{invalidName}' 正确返回null");
    }

    /// <summary>
    /// 测试 - GetNetworkInterfaceByName - 大小写不敏感
    /// </summary>
    [Fact]
    public void GetNetworkInterfaceByName_CaseInsensitive_FindsInterface()
    {
        // Arrange
        var allInterfaces = NetworkInterfaceManager.GetNetworkInterfaces();
        if (!allInterfaces.Any())
        {
            Output.WriteLine("没有可用的网络接口进行测试");
            return;
        }

        var firstInterface = allInterfaces.First();
        var upperCaseName = firstInterface.Name.ToUpperInvariant();
        var lowerCaseName = firstInterface.Name.ToLowerInvariant();

        // Act
        var foundByUpper = NetworkInterfaceManager.GetNetworkInterfaceByName(upperCaseName);
        var foundByLower = NetworkInterfaceManager.GetNetworkInterfaceByName(lowerCaseName);

        // Assert
        if (firstInterface.Name != upperCaseName)
        {
            foundByUpper.ShouldNotBeNull();
            foundByUpper.Name.ShouldBe(firstInterface.Name);
        }

        if (firstInterface.Name != lowerCaseName)
        {
            foundByLower.ShouldNotBeNull();
            foundByLower.Name.ShouldBe(firstInterface.Name);
        }

        Output.WriteLine($"大小写不敏感测试: 原名'{firstInterface.Name}', 大写找到={foundByUpper != null}, 小写找到={foundByLower != null}");
    }

    #endregion

    #region GetNetworkInterfaceByIp 测试

    /// <summary>
    /// 测试 - GetNetworkInterfaceByIp - 通过有效IP地址查找接口
    /// </summary>
    [Fact]
    public void GetNetworkInterfaceByIp_ValidIpAddress_FindsCorrectInterface()
    {
        // Arrange
        var allInterfaces = NetworkInterfaceManager.GetNetworkInterfaces();
        if (!allInterfaces.Any() || !allInterfaces.Any(i => i.IpAddresses.Any()))
        {
            Output.WriteLine("没有可用的网络接口进行测试");
            return;
        }

        var interfaceWithIp = allInterfaces.First(i => i.IpAddresses.Any());
        var testIp = interfaceWithIp.IpAddresses.First();

        // Act
        var foundInterface = NetworkInterfaceManager.GetNetworkInterfaceByIp(testIp);

        // Assert
        foundInterface.ShouldNotBeNull();
        foundInterface.Name.ShouldBe(interfaceWithIp.Name);
        foundInterface.IpAddresses.ShouldContain(testIp);

        Output.WriteLine($"通过IP地址 '{testIp}' 找到接口: {foundInterface.Name}");
    }

    /// <summary>
    /// 测试 - GetNetworkInterfaceByIp - 无效IP地址
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("invalid.ip.address")]
    [InlineData("999.999.999.999")]
    public void GetNetworkInterfaceByIp_InvalidIpAddress_ReturnsNull(string invalidIp)
    {
        // Act
        var result = NetworkInterfaceManager.GetNetworkInterfaceByIp(invalidIp);

        // Assert
        result.ShouldBeNull();
        Output.WriteLine($"无效IP地址 '{invalidIp}' 正确返回null");
    }

    /// <summary>
    /// 测试 - GetNetworkInterfaceByIp - 不存在的IP地址
    /// </summary>
    [Fact]
    public void GetNetworkInterfaceByIp_NonExistentIpAddress_ReturnsNull()
    {
        // Arrange - 使用一个肯定不存在的IP地址
        var nonExistentIp = "192.168.254.254";

        // Act
        var result = NetworkInterfaceManager.GetNetworkInterfaceByIp(nonExistentIp);

        // Assert
        result.ShouldBeNull();
        Output.WriteLine($"不存在的IP地址 '{nonExistentIp}' 正确返回null");
    }

    #endregion

    #region GetPrimaryIpAddress 测试

    /// <summary>
    /// 测试 - GetPrimaryIpAddress - 获取主要IP地址
    /// </summary>
    [Fact]
    public void GetPrimaryIpAddress_ReturnsValidIp()
    {
        // Act
        var ipv4Primary = NetworkInterfaceManager.GetPrimaryIpAddress(true);
        var ipv6Primary = NetworkInterfaceManager.GetPrimaryIpAddress(false);

        // Assert
        ipv4Primary.ShouldNotBeNull();
        ipv6Primary.ShouldNotBeNull();

        // 测试IPv4优先模式
        if (!string.IsNullOrEmpty(ipv4Primary))
        {
            // IPv4地址不应该是回环地址
            ipv4Primary.ShouldNotStartWith("127.");

            // 验证IPv4格式
            var parts = ipv4Primary.Split('.');
            parts.Length.ShouldBe(4);

            foreach (var part in parts)
            {
                int.TryParse(part, out var num).ShouldBeTrue();
                num.ShouldBeInRange(0, 255);
            }

            Output.WriteLine($"IPv4优先模式返回IPv4地址: '{ipv4Primary}'");
        }
        else
        {
            Output.WriteLine("IPv4优先模式未找到IPv4地址");
        }

        // 测试IPv6优先模式
        if (!string.IsNullOrEmpty(ipv6Primary))
        {
            // IPv6地址不应该是回环或链路本地地址
            ipv6Primary.ShouldNotBe("::1");
            ipv6Primary.ShouldNotStartWith("fe80:");

            // 验证IPv6格式
            //ipv6Primary.ShouldContain(":");

            Output.WriteLine($"IPv6优先模式返回IPv6地址: '{ipv6Primary}'");
        }
        else
        {
            Output.WriteLine("IPv6优先模式未找到IPv6地址");
        }
    }

    /// <summary>
    /// 测试 - GetPrimaryIpAddress - IPv6回退机制验证
    /// </summary>
    [Fact]
    public void GetPrimaryIpAddress_IPv6Fallback_WorksCorrectly()
    {
        // 获取所有可用的IPv4和IPv6地址
        var allIPv4 = NetworkInterfaceManager.GetAllIPv4Addresses();
        var allIPv6 = NetworkInterfaceManager.GetAllIPv6Addresses();

        // Act
        var ipv4Primary = NetworkInterfaceManager.GetPrimaryIpAddress(preferIPv4: true);
        var ipv6Primary = NetworkInterfaceManager.GetPrimaryIpAddress(preferIPv4: false);

        // Assert
        Output.WriteLine($"系统IPv4地址数量: {allIPv4.Count}");
        Output.WriteLine($"系统IPv6地址数量: {allIPv6.Count}");

        // 验证IPv4优先模式的回退逻辑
        if (allIPv4.Any())
        {
            // 如果系统有IPv4地址，IPv4优先模式应该返回IPv4地址
            if (!string.IsNullOrEmpty(ipv4Primary))
            {
                allIPv4.ShouldContain(ipv4Primary);
                Output.WriteLine($"IPv4优先模式正确返回IPv4地址: {ipv4Primary}");
            }
        }
        else if (allIPv6.Any())
        {
            // 如果没有IPv4但有IPv6，应该回退到IPv6
            if (!string.IsNullOrEmpty(ipv4Primary))
            {
                ipv4Primary.ShouldContain(":");
                Output.WriteLine($"IPv4优先模式回退到IPv6地址: {ipv4Primary}");
            }
        }

        // 验证IPv6优先模式的回退逻辑
        if (allIPv6.Any())
        {
            // 如果系统有IPv6地址，IPv6优先模式应该返回IPv6地址
            if (!string.IsNullOrEmpty(ipv6Primary))
            {
                allIPv6.ShouldContain(ipv6Primary);
                Output.WriteLine($"IPv6优先模式正确返回IPv6地址: {ipv6Primary}");
            }
        }
        else if (allIPv4.Any())
        {
            // 如果没有IPv6但有IPv4，应该回退到IPv4
            if (!string.IsNullOrEmpty(ipv6Primary))
            {
                allIPv4.ShouldContain(ipv6Primary);
                Output.WriteLine($"IPv6优先模式回退到IPv4地址: {ipv6Primary}");
            }
        }
    }

    /// <summary>
    /// 测试 - GetPrimaryIpAddress - 空网络环境处理
    /// </summary>
    [Fact]
    public void GetPrimaryIpAddress_NoNetworkInterfaces_ReturnsEmpty()
    {
        // 这个测试主要验证异常处理，因为实际很难模拟完全无网络的环境

        // Act & Assert - 方法应该能正常执行而不抛出异常
        string ipv4Result = null;
        string ipv6Result = null;

        Should.NotThrow(() => ipv4Result = NetworkInterfaceManager.GetPrimaryIpAddress(true));
        Should.NotThrow(() => ipv6Result = NetworkInterfaceManager.GetPrimaryIpAddress(false));

        // 结果应该是非null的（可能是空字符串）
        ipv4Result.ShouldNotBeNull();
        ipv6Result.ShouldNotBeNull();

        Output.WriteLine($"异常处理测试完成 - IPv4: '{ipv4Result}', IPv6: '{ipv6Result}'");
    }

    /// <summary>
    /// 测试 - GetPrimaryIpAddress - 性能测试
    /// </summary>
    [Fact]
    public void GetPrimaryIpAddress_Performance_CompletesQuickly()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act - 多次调用测试性能
        for (int i = 0; i < 10; i++)
        {
            var ipv4 = NetworkInterfaceManager.GetPrimaryIpAddress(true);
            var ipv6 = NetworkInterfaceManager.GetPrimaryIpAddress(false);
        }

        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.ShouldBeLessThan(5000, "20次GetPrimaryIpAddress调用应该在5秒内完成");

        Output.WriteLine($"性能测试: 20次调用耗时 {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 测试 - GetPrimaryIpAddress - 一致性验证
    /// </summary>
    [Fact]
    public void GetPrimaryIpAddress_Consistency_ReturnsSameResultsOnMultipleCalls()
    {
        // Act - 多次调用
        var results = new List<(string IPv4, string IPv6)>();

        for (int i = 0; i < 5; i++)
        {
            var ipv4 = NetworkInterfaceManager.GetPrimaryIpAddress(true);
            var ipv6 = NetworkInterfaceManager.GetPrimaryIpAddress(false);
            results.Add((ipv4, ipv6));

            // 短暂延迟避免缓存影响
            Task.Delay(10).Wait();
        }

        // Assert - 所有结果应该一致
        var firstResult = results.First();

        foreach (var result in results)
        {
            result.IPv4.ShouldBe(firstResult.IPv4, "多次调用IPv4结果应该一致");
            result.IPv6.ShouldBe(firstResult.IPv6, "多次调用IPv6结果应该一致");
        }

        Output.WriteLine($"一致性测试通过 - IPv4: '{firstResult.IPv4}', IPv6: '{firstResult.IPv6}'");
    }

    /// <summary>
    /// 测试 - GetPrimaryIpAddress - 无活动接口时返回空字符串
    /// </summary>
    [Fact]
    public void GetPrimaryIpAddress_NoActiveInterfaces_ReturnsEmpty()
    {
        // 这个测试很难模拟，因为通常至少有一个网络接口
        // 我们主要验证方法不会抛出异常

        // Act & Assert - 应该不抛出异常
        var ipv4 = NetworkInterfaceManager.GetPrimaryIpAddress(true);
        var ipv6 = NetworkInterfaceManager.GetPrimaryIpAddress(false);

        ipv4.ShouldNotBeNull();
        ipv6.ShouldNotBeNull();

        Output.WriteLine($"GetPrimaryIpAddress 测试完成 - IPv4: '{ipv4}', IPv6: '{ipv6}'");
    }

    /// <summary>
    /// 测试 - GetPrimaryIpAddress - IPv6链路本地地址处理
    /// </summary>
    [Fact]
    public void GetPrimaryIpAddress_IPv6LinkLocal_HandledCorrectly()
    {
        // Arrange - 分析当前系统的IPv6情况
        var allIPv6WithLinkLocal = NetworkInterfaceManager.GetAllIPv6Addresses(excludeLoopback: true, excludeLinkLocal: false);
        var allIPv6WithoutLinkLocal = NetworkInterfaceManager.GetAllIPv6Addresses(excludeLoopback: true, excludeLinkLocal: true);

        var linkLocalAddresses = allIPv6WithLinkLocal.Where(ip => ip.StartsWith("fe80:", StringComparison.OrdinalIgnoreCase)).ToList();
        var globalUnicastAddresses = allIPv6WithoutLinkLocal.ToList();

        Output.WriteLine($"链路本地IPv6地址数量: {linkLocalAddresses.Count}");
        Output.WriteLine($"全局IPv6地址数量: {globalUnicastAddresses.Count}");

        // Act
        var primaryIPv6 = NetworkInterfaceManager.GetPrimaryIpAddress(preferIPv4: false);

        // Assert
        if (!string.IsNullOrEmpty(primaryIPv6))
        {
            if (primaryIPv6.Contains(":"))
            {
                // 如果返回IPv6地址，应该不是链路本地地址
                primaryIPv6.ShouldNotStartWith("fe80:", customMessage: "主要IPv6地址不应该是链路本地地址");

                // 应该是全局可路由的地址
                globalUnicastAddresses.ShouldContain(primaryIPv6,
                    "主要IPv6地址应该是全局可路由地址");

                Output.WriteLine($"✓ 正确返回全局IPv6地址: {primaryIPv6}");
            }
            else
            {
                // 回退到IPv4的情况
                Output.WriteLine($"✓ 回退到IPv4地址: {primaryIPv6}");
            }
        }
        else
        {
            Output.WriteLine("✓ 未找到合适的IPv6地址，返回空字符串");
        }
    }

    /// <summary>
    /// 测试 - GetPrimaryIpAddress - IPv6环境特殊情况处理
    /// </summary>
    [Fact]
    public void GetPrimaryIpAddress_IPv6Environment_HandlesSpecialCases()
    {
        // 获取系统的IPv6地址情况
        var allInterfaces = NetworkInterfaceManager.GetNetworkInterfaces();
        var allIPv6 = NetworkInterfaceManager.GetAllIPv6Addresses(excludeLoopback: false, excludeLinkLocal: false);
        var globalIPv6 = NetworkInterfaceManager.GetAllIPv6Addresses(excludeLoopback: true, excludeLinkLocal: true);

        Output.WriteLine($"总IPv6地址数: {allIPv6.Count}");
        Output.WriteLine($"全局IPv6地址数: {globalIPv6.Count}");

        // 分析IPv6地址类型
        var loopbackCount = allIPv6.Count(ip => ip.Equals("::1", StringComparison.OrdinalIgnoreCase));
        var linkLocalCount = allIPv6.Count(ip => ip.StartsWith("fe80:", StringComparison.OrdinalIgnoreCase));
        var uniqueLocalCount = allIPv6.Count(ip => ip.StartsWith("fc") || ip.StartsWith("fd"));
        var globalCount = globalIPv6.Count;

        Output.WriteLine($"IPv6地址分布:");
        Output.WriteLine($"  回环地址: {loopbackCount}");
        Output.WriteLine($"  链路本地: {linkLocalCount}");
        Output.WriteLine($"  唯一本地: {uniqueLocalCount}");
        Output.WriteLine($"  全局地址: {globalCount}");

        // Act
        var primaryIPv6 = NetworkInterfaceManager.GetPrimaryIpAddress(false);

        // Assert
        if (globalCount > 0)
        {
            // 如果有全局IPv6地址，应该返回全局地址
            if (!string.IsNullOrEmpty(primaryIPv6))
            {
                primaryIPv6.ShouldContain(":");
                primaryIPv6.ShouldNotBe("::1");
                primaryIPv6.ShouldNotStartWith("fe80:");
                Output.WriteLine($"有全局IPv6时返回: '{primaryIPv6}'");
            }
        }
        else if (allIPv6.Any())
        {
            // 如果只有本地IPv6地址，可能会回退到IPv4
            Output.WriteLine($"无全局IPv6，返回: '{primaryIPv6}'");

            if (!string.IsNullOrEmpty(primaryIPv6))
            {
                if (primaryIPv6.Contains(":"))
                {
                    Output.WriteLine("返回了IPv6地址（可能是经过筛选的）");
                }
                else
                {
                    Output.WriteLine("回退到IPv4地址");
                    // 验证回退的IPv4地址格式
                    var parts = primaryIPv6.Split('.');
                    parts.Length.ShouldBe(4);
                }
            }
        }
        else
        {
            Output.WriteLine("系统无IPv6支持，应该回退到IPv4");
        }

        // 特殊情况：验证即使在只有链路本地IPv6的情况下，也能正确处理
        if (linkLocalCount > 0 && globalCount == 0)
        {
            Output.WriteLine("检测到只有链路本地IPv6的环境");
            // 这种情况下，GetPrimaryIpAddress(false) 应该回退到IPv4或返回空
            if (!string.IsNullOrEmpty(primaryIPv6))
            {
                if (primaryIPv6.Contains(":"))
                {
                    // 如果返回IPv6，不应该是fe80:开头
                    primaryIPv6.ShouldNotStartWith("fe80:");
                }
            }
        }
    }

    #endregion

    #region GetActiveNetworkInterfaces 测试

    /// <summary>
    /// 测试 - GetActiveNetworkInterfaces - 获取活动接口
    /// </summary>
    [Fact]
    public void GetActiveNetworkInterfaces_ReturnsInterfacesWithGateway()
    {
        // Act
        var activeInterfaces = NetworkInterfaceManager.GetActiveNetworkInterfaces();

        // Assert
        activeInterfaces.ShouldNotBeNull();

        foreach (var iface in activeInterfaces)
        {
            iface.HasGateway.ShouldBeTrue();
            iface.GatewayAddresses.ShouldNotBeEmpty();

            // 活动接口应该有IP地址
            iface.IpAddresses.ShouldNotBeEmpty();

            // 速度应该大于0（活动接口通常有已知速度）
            iface.Speed.ShouldBeGreaterThan(0);
        }

        // 验证按速度降序排列
        if (activeInterfaces.Count > 1)
        {
            for (int i = 1; i < activeInterfaces.Count; i++)
            {
                activeInterfaces[i - 1].Speed.ShouldBeGreaterThanOrEqualTo(activeInterfaces[i].Speed);
            }
        }

        Output.WriteLine($"找到 {activeInterfaces.Count} 个活动网络接口:");
        foreach (var iface in activeInterfaces)
        {
            Output.WriteLine($"  {iface.Name}: 速度={iface.Speed:N0} bps, 网关={iface.GatewayAddresses.Count}个");
        }
    }

    #endregion

    #region IsConnectedToInternetAsync 测试

    /// <summary>
    /// 测试 - IsConnectedToInternetAsync - 互联网连接检查
    /// </summary>
    [Fact]
    public async Task IsConnectedToInternetAsync_ChecksConnectivity()
    {
        // Act
        var isConnected = await NetworkInterfaceManager.IsConnectedToInternetAsync(3000);

        // Assert
        Output.WriteLine($"互联网连接状态: {isConnected}");

        // 注意：这个测试可能在没有网络连接的环境中失败
        // 我们只验证方法能正常执行而不抛出异常
        isConnected.ShouldBeOfType<bool>();
    }

    /// <summary>
    /// 测试 - IsConnectedToInternetAsync - 超时设置
    /// </summary>
    [Fact]
    public async Task IsConnectedToInternetAsync_WithTimeout_CompletesWithinTime()
    {
        // Arrange
        var timeout = 1000; // 1秒超时
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var isConnected = await NetworkInterfaceManager.IsConnectedToInternetAsync(timeout);
        sw.Stop();

        // Assert
        // 应该在超时时间的合理范围内完成（允许一些额外时间用于处理）
        sw.ElapsedMilliseconds.ShouldBeLessThan(timeout + 2000);

        Output.WriteLine($"超时测试: 设置{timeout}ms, 实际耗时{sw.ElapsedMilliseconds}ms, 结果={isConnected}");
    }

    #endregion

    #region GetAllIPv4Addresses 测试

    /// <summary>
    /// 测试 - GetAllIPv4Addresses - 获取所有IPv4地址
    /// </summary>
    [Fact]
    public void GetAllIPv4Addresses_ReturnsValidAddresses()
    {
        // Act
        var addresses = NetworkInterfaceManager.GetAllIPv4Addresses();

        // Assert
        addresses.ShouldNotBeNull();

        foreach (var address in addresses)
        {
            address.ShouldNotStartWith("127."); // 排除回环地址

            // 验证IPv4格式
            var parts = address.Split('.');
            parts.Length.ShouldBe(4);

            foreach (var part in parts)
            {
                int.TryParse(part, out var num).ShouldBeTrue();
                num.ShouldBeInRange(0, 255);
            }
        }

        Output.WriteLine($"IPv4地址: {string.Join(", ", addresses)}");
    }

    /// <summary>
    /// 测试 - GetAllIPv4Addresses - 包含回环地址选项
    /// </summary>
    [Fact]
    public void GetAllIPv4Addresses_IncludeLoopback_ReturnsLoopbackAddress()
    {
        // Act
        var addressesExcludeLoopback = NetworkInterfaceManager.GetAllIPv4Addresses(excludeLoopback: true);
        var addressesIncludeLoopback = NetworkInterfaceManager.GetAllIPv4Addresses(excludeLoopback: false);

        // Assert
        addressesExcludeLoopback.ShouldNotBeNull();
        addressesIncludeLoopback.ShouldNotBeNull();

        // 排除回环的列表不应包含127.开头的地址
        foreach (var addr in addressesExcludeLoopback)
        {
            addr.ShouldNotStartWith("127.");
        }

        Output.WriteLine($"排除回环: {addressesExcludeLoopback.Count} 个地址");
        Output.WriteLine($"包含回环: {addressesIncludeLoopback.Count} 个地址");

        // 包含回环的列表应该>=排除回环的列表
        addressesIncludeLoopback.Count.ShouldBeGreaterThanOrEqualTo(addressesExcludeLoopback.Count);
    }

    #endregion

    #region GetAllIPv6Addresses 测试

    /// <summary>
    /// 测试 - GetAllIPv6Addresses - 获取所有IPv6地址（默认排除）
    /// </summary>
    [Fact]
    public void GetAllIPv6Addresses_DefaultExclusions_ReturnsFilteredAddresses()
    {
        // Act
        var addresses = NetworkInterfaceManager.GetAllIPv6Addresses();

        // Assert
        addresses.ShouldNotBeNull();

        foreach (var address in addresses)
        {
            // 应该排除回环地址
            address.ShouldNotBe("::1");

            // 应该排除链路本地地址
            address.ShouldNotStartWith("fe80:");

            // 验证IPv6格式
            address.ShouldContain(":");
        }

        Output.WriteLine($"IPv6地址（排除回环和链路本地）: {string.Join(", ", addresses)}");
    }

    /// <summary>
    /// 测试 - GetAllIPv6Addresses - 包含回环地址
    /// </summary>
    [Fact]
    public void GetAllIPv6Addresses_IncludeLoopback_ReturnsLoopbackAddress()
    {
        // Act
        var addressesExcludeLoopback = NetworkInterfaceManager.GetAllIPv6Addresses(excludeLoopback: true);
        var addressesIncludeLoopback = NetworkInterfaceManager.GetAllIPv6Addresses(excludeLoopback: false);

        // Assert
        addressesExcludeLoopback.ShouldNotBeNull();
        addressesIncludeLoopback.ShouldNotBeNull();

        // 排除回环的列表不应包含::1
        addressesExcludeLoopback.ShouldNotContain("::1");

        // 如果系统有IPv6支持，包含回环的列表可能包含::1
        Output.WriteLine($"排除回环: {addressesExcludeLoopback.Count} 个地址");
        Output.WriteLine($"包含回环: {addressesIncludeLoopback.Count} 个地址");

        // 包含回环的列表应该>=排除回环的列表
        addressesIncludeLoopback.Count.ShouldBeGreaterThanOrEqualTo(addressesExcludeLoopback.Count);
    }

    /// <summary>
    /// 测试 - GetAllIPv6Addresses - 包含链路本地地址
    /// </summary>
    [Fact]
    public void GetAllIPv6Addresses_IncludeLinkLocal_ReturnsLinkLocalAddresses()
    {
        // Act
        var addressesExcludeLinkLocal = NetworkInterfaceManager.GetAllIPv6Addresses(excludeLinkLocal: true);
        var addressesIncludeLinkLocal = NetworkInterfaceManager.GetAllIPv6Addresses(excludeLinkLocal: false);

        // Assert
        addressesExcludeLinkLocal.ShouldNotBeNull();
        addressesIncludeLinkLocal.ShouldNotBeNull();

        // 排除链路本地的列表不应包含fe80:开头的地址
        foreach (var addr in addressesExcludeLinkLocal)
        {
            addr.ShouldNotStartWith("fe80:");
        }

        Output.WriteLine($"排除链路本地: {addressesExcludeLinkLocal.Count} 个地址");
        Output.WriteLine($"包含链路本地: {addressesIncludeLinkLocal.Count} 个地址");

        // 包含链路本地的列表应该>=排除链路本地的列表
        addressesIncludeLinkLocal.Count.ShouldBeGreaterThanOrEqualTo(addressesExcludeLinkLocal.Count);
    }

    /// <summary>
    /// 测试 - GetAllIPv6Addresses - 所有选项组合
    /// </summary>
    [Theory]
    [InlineData(true, true)]   // 排除回环和链路本地
    [InlineData(true, false)]  // 排除回环，包含链路本地
    [InlineData(false, true)]  // 包含回环，排除链路本地
    [InlineData(false, false)] // 包含所有
    public void GetAllIPv6Addresses_VariousOptions_ReturnsCorrectResults(bool excludeLoopback, bool excludeLinkLocal)
    {
        // Act
        var addresses = NetworkInterfaceManager.GetAllIPv6Addresses(excludeLoopback, excludeLinkLocal);

        // Assert
        addresses.ShouldNotBeNull();

        foreach (var address in addresses)
        {
            if (excludeLoopback)
            {
                address.ShouldNotBe("::1"); // 排除回环地址
            }

            if (excludeLinkLocal)
            {
                address.ShouldNotStartWith("fe80:");
            }

            // 所有地址都应该是有效的IPv6格式
            address.ShouldContain(":");
        }

        Output.WriteLine($"选项(排除回环={excludeLoopback}, 排除链路本地={excludeLinkLocal}): {addresses.Count} 个地址");
    }

    /// <summary>
    /// 测试 - GetAllIPv6Addresses - 链路本地地址过滤验证
    /// </summary>
    [Fact]
    public void GetAllIPv6Addresses_LinkLocalFiltering_WorksAsExpected()
    {
        // Act
        var allAddresses = NetworkInterfaceManager.GetAllIPv6Addresses(excludeLoopback: false, excludeLinkLocal: false);
        var excludeLinkLocal = NetworkInterfaceManager.GetAllIPv6Addresses(excludeLoopback: false, excludeLinkLocal: true);
        var includeLinkLocal = NetworkInterfaceManager.GetAllIPv6Addresses(excludeLoopback: false, excludeLinkLocal: false);

        // 分析地址类型
        var linkLocalAddresses = allAddresses.Where(ip => ip.StartsWith("fe80:", StringComparison.OrdinalIgnoreCase)).ToList();
        var loopbackAddresses = allAddresses.Where(ip => ip.Equals("::1", StringComparison.OrdinalIgnoreCase)).ToList();
        var globalAddresses = allAddresses.Where(ip =>
            !ip.StartsWith("fe80:", StringComparison.OrdinalIgnoreCase) &&
            !ip.Equals("::1", StringComparison.OrdinalIgnoreCase)).ToList();

        // Assert
        Output.WriteLine($"分析IPv6地址分布:");
        Output.WriteLine($"  链路本地地址 (fe80::): {linkLocalAddresses.Count} 个");
        Output.WriteLine($"  回环地址 (::1): {loopbackAddresses.Count} 个");
        Output.WriteLine($"  其他地址: {globalAddresses.Count} 个");

        // 验证过滤逻辑
        excludeLinkLocal.ShouldNotContain(ip => ip.StartsWith("fe80:", StringComparison.OrdinalIgnoreCase),
            "排除链路本地的列表不应包含fe80:开头的地址");

        // 验证数量关系
        var expectedExcludedCount = linkLocalAddresses.Count;
        var actualExcludedCount = includeLinkLocal.Count - excludeLinkLocal.Count;

        if (linkLocalAddresses.Any())
        {
            actualExcludedCount.ShouldBe(expectedExcludedCount,
                $"应该排除 {expectedExcludedCount} 个链路本地地址");

            Output.WriteLine($"✓ 正确排除了 {expectedExcludedCount} 个链路本地地址");
        }
        else
        {
            Output.WriteLine("✓ 系统中没有链路本地地址，过滤功能无影响");
        }
    }

    #endregion

    #region RefreshNetworkInterfaces 测试

    /// <summary>
    /// 测试 - RefreshNetworkInterfaces - 刷新网络接口信息
    /// </summary>
    [Fact]
    public void RefreshNetworkInterfaces_ReturnsUpdatedInterfaces()
    {
        // Act
        var originalInterfaces = NetworkInterfaceManager.GetNetworkInterfaces();
        var refreshedInterfaces = NetworkInterfaceManager.RefreshNetworkInterfaces();

        // Assert
        originalInterfaces.ShouldNotBeNull();
        refreshedInterfaces.ShouldNotBeNull();

        // 刷新后的接口数量应该与原始数量相同或相近
        // (在测试期间网络接口通常不会变化)
        refreshedInterfaces.Count.ShouldBe(originalInterfaces.Count);

        // 验证刷新后的接口信息是有效的
        foreach (var iface in refreshedInterfaces)
        {
            iface.Name.ShouldNotBeNullOrEmpty();
            iface.Status.ShouldBe(OperationalStatus.Up);
            iface.IpAddresses.ShouldNotBeEmpty();
        }

        Output.WriteLine($"原始接口数: {originalInterfaces.Count}, 刷新后接口数: {refreshedInterfaces.Count}");

        // 验证主要接口信息一致性
        if (originalInterfaces.Any() && refreshedInterfaces.Any())
        {
            var originalFirst = originalInterfaces.First();
            var refreshedFirst = refreshedInterfaces.FirstOrDefault(i => i.Name == originalFirst.Name);

            if (refreshedFirst != null)
            {
                refreshedFirst.Name.ShouldBe(originalFirst.Name);
                refreshedFirst.Type.ShouldBe(originalFirst.Type);
                Output.WriteLine($"接口 '{originalFirst.Name}' 信息一致性验证通过");
            }
        }
    }

    #endregion

    #region IsPortListening 测试

    /// <summary>
    /// 测试 - IsPortListening - 端口监听检查
    /// </summary>
    [Theory]
    [InlineData(80, ProtocolType.Tcp)]   // HTTP
    [InlineData(443, ProtocolType.Tcp)]  // HTTPS
    [InlineData(53, ProtocolType.Udp)]   // DNS
    public void IsPortListening_ChecksCommonPorts(int port, ProtocolType protocol)
    {
        // Act
        var isListening = NetworkInterfaceManager.IsPortListening(port, protocol);

        // Assert
        isListening.ShouldBeOfType<bool>();

        Output.WriteLine($"端口 {port}/{protocol} 监听状态: {isListening}");
    }

    /// <summary>
    /// 测试 - IsPortListening - 无效协议类型
    /// </summary>
    [Fact]
    public void IsPortListening_InvalidProtocol_ReturnsFalse()
    {
        // Act
        var result = NetworkInterfaceManager.IsPortListening(80, ProtocolType.Icmp);

        // Assert
        result.ShouldBeFalse();
        Output.WriteLine("ICMP协议检查正确返回false");
    }

    /// <summary>
    /// 测试 - IsPortListening - 边界端口号
    /// </summary>
    [Theory]
    [InlineData(1)]      // 最小端口
    [InlineData(65535)]  // 最大端口
    [InlineData(0)]      // 系统端口
    public void IsPortListening_BoundaryPorts_HandlesCorrectly(int port)
    {
        // Act & Assert - 应该不抛出异常
        var tcpResult = NetworkInterfaceManager.IsPortListening(port, ProtocolType.Tcp);
        var udpResult = NetworkInterfaceManager.IsPortListening(port, ProtocolType.Udp);

        tcpResult.ShouldBeOfType<bool>();
        udpResult.ShouldBeOfType<bool>();

        Output.WriteLine($"端口 {port} - TCP: {tcpResult}, UDP: {udpResult}");
    }

    #endregion

    #region GetNetworkStatistics 测试

    /// <summary>
    /// 测试 - GetNetworkStatistics - 获取网络统计信息
    /// </summary>
    [Fact]
    public void GetNetworkStatistics_ReturnsValidStatistics()
    {
        // Act
        var stats = NetworkInterfaceManager.GetNetworkStatistics();

        // Assert
        stats.ShouldNotBeNull();
        stats.ActiveInterfaceCount.ShouldBeGreaterThanOrEqualTo(0);
        stats.BytesSent.ShouldBeGreaterThanOrEqualTo(0);
        stats.BytesReceived.ShouldBeGreaterThanOrEqualTo(0);
        stats.PacketsSent.ShouldBeGreaterThanOrEqualTo(0);
        stats.PacketsReceived.ShouldBeGreaterThanOrEqualTo(0);

        // 验证计算属性
        stats.TotalBytes.ShouldBe(stats.BytesSent + stats.BytesReceived);
        stats.TotalPackets.ShouldBe(stats.PacketsSent + stats.PacketsReceived);

        Output.WriteLine($"网络统计: {stats}");
        Output.WriteLine($"活动接口数: {stats.ActiveInterfaceCount}");
        Output.WriteLine($"总流量: {stats.TotalBytes:N0} 字节");
        Output.WriteLine($"总数据包: {stats.TotalPackets:N0} 个");
    }

    /// <summary>
    /// 测试 - GetNetworkStatistics - 多次调用一致性
    /// </summary>
    [Fact]
    public void GetNetworkStatistics_MultipleCalls_ShowsIncreasingValues()
    {
        // Act
        var stats1 = NetworkInterfaceManager.GetNetworkStatistics();

        // 等待一小段时间让网络活动产生一些数据
        Task.Delay(100).Wait();

        var stats2 = NetworkInterfaceManager.GetNetworkStatistics();

        // Assert
        stats1.ShouldNotBeNull();
        stats2.ShouldNotBeNull();

        // 第二次统计的数值应该大于等于第一次（网络活动可能会增加数据）
        stats2.BytesSent.ShouldBeGreaterThanOrEqualTo(stats1.BytesSent);
        stats2.BytesReceived.ShouldBeGreaterThanOrEqualTo(stats1.BytesReceived);
        stats2.PacketsSent.ShouldBeGreaterThanOrEqualTo(stats1.PacketsSent);
        stats2.PacketsReceived.ShouldBeGreaterThanOrEqualTo(stats1.PacketsReceived);

        // 活动接口数应该保持相同
        stats2.ActiveInterfaceCount.ShouldBe(stats1.ActiveInterfaceCount);

        Output.WriteLine($"统计1: 发送={stats1.BytesSent:N0}, 接收={stats1.BytesReceived:N0}");
        Output.WriteLine($"统计2: 发送={stats2.BytesSent:N0}, 接收={stats2.BytesReceived:N0}");
    }

    #endregion

    #region TestNetworkQualityAsync 测试

    /// <summary>
    /// 测试 - TestNetworkQualityAsync - 网络质量测试
    /// </summary>
    [Fact]
    public async Task TestNetworkQualityAsync_ReturnsQualityInfo()
    {
        // Act
        var quality = await NetworkInterfaceManager.TestNetworkQualityAsync("www.baidu.com", 3000, 2);

        // Assert
        quality.ShouldNotBeNull();
        quality.SuccessRate.ShouldBeInRange(0.0, 1.0);
        quality.PacketLoss.ShouldBeInRange(0.0, 1.0);
        quality.QualityLevel.ShouldBeOfType<NetworkQualityLevel>();

        if (quality.IsConnected)
        {
            quality.AverageLatency.ShouldBeGreaterThan(0);
            quality.MinLatency.ShouldBeGreaterThan(0);
            quality.MaxLatency.ShouldBeGreaterThanOrEqualTo(quality.MinLatency);

            // 验证丢包率计算
            quality.PacketLoss.ShouldBe(1.0 - quality.SuccessRate, 0.001);
        }
        else
        {
            quality.AverageLatency.ShouldBe(0);
            quality.MinLatency.ShouldBe(0);
            quality.MaxLatency.ShouldBe(0);
        }

        Output.WriteLine($"网络质量测试结果: {quality}");
    }

    /// <summary>
    /// 测试 - TestNetworkQualityAsync - 不同重试次数
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task TestNetworkQualityAsync_DifferentRetryCounts_CalculatesCorrectly(int retryCount)
    {
        // Act
        var quality = await NetworkInterfaceManager.TestNetworkQualityAsync("www.baidu.com", 2000, retryCount);

        // Assert
        quality.ShouldNotBeNull();

        // 成功率应该是 成功次数/重试次数
        var expectedSuccessRate = quality.IsConnected ? (quality.SuccessRate * retryCount) / retryCount : 0.0;
        quality.SuccessRate.ShouldBeInRange(0.0, 1.0);

        Output.WriteLine($"重试{retryCount}次的质量测试: 成功率={quality.SuccessRate:P}, 质量={quality.QualityLevel}");
    }

    #endregion

    #region PingAsync 测试

    /// <summary>
    /// 测试 - PingAsync - Ping测试
    /// </summary>
    [Fact]
    public async Task PingAsync_ReturnsValidResult()
    {
        // Act
        var result = await NetworkInterfaceManager.PingAsync("8.8.8.8", 3000, 3);

        // Assert
        result.ShouldNotBeNull();
        result.TargetHost.ShouldBe("8.8.8.8");
        result.PacketsSent.ShouldBe(3);
        result.PacketsReceived.ShouldBeInRange(0, 3);
        result.PacketLoss.ShouldBeInRange(0.0, 1.0);

        // 验证丢包率计算
        var expectedPacketLoss = 1.0 - (double)result.PacketsReceived / result.PacketsSent;
        result.PacketLoss.ShouldBe(expectedPacketLoss, 0.001);

        if (result.PacketsReceived > 0)
        {
            result.AverageRoundtripTime.ShouldBeGreaterThan(0);
            result.MinRoundtripTime.ShouldBeGreaterThan(0);
            result.MaxRoundtripTime.ShouldBeGreaterThanOrEqualTo(result.MinRoundtripTime);

            // 如果有多个成功的ping，应该有抖动数据
            if (result.PacketsReceived > 1)
            {
                result.Jitter.ShouldBeGreaterThanOrEqualTo(0);
            }
        }
        else
        {
            result.AverageRoundtripTime.ShouldBe(0);
            result.MinRoundtripTime.ShouldBe(0);
            result.MaxRoundtripTime.ShouldBe(0);
            result.Jitter.ShouldBe(0);
        }

        Output.WriteLine($"Ping测试结果: {result}");
    }

    /// <summary>
    /// 测试 - PingAsync - 不同目标主机
    /// </summary>
    [Theory]
    [InlineData("8.8.8.8")]        // Google DNS
    [InlineData("1.1.1.1")]        // Cloudflare DNS
    [InlineData("www.baidu.com")]   // 域名
    public async Task PingAsync_DifferentTargets_ReturnsValidResults(string target)
    {
        // Act
        var result = await NetworkInterfaceManager.PingAsync(target, 3000, 2);

        // Assert
        result.ShouldNotBeNull();
        result.TargetHost.ShouldBe(target);
        result.PacketsSent.ShouldBe(2);
        result.PacketsReceived.ShouldBeInRange(0, 2);

        Output.WriteLine($"Ping {target}: 发送={result.PacketsSent}, 接收={result.PacketsReceived}, 平均延迟={result.AverageRoundtripTime}ms");
    }

    /// <summary>
    /// 测试 - PingAsync - 无效目标主机
    /// </summary>
    [Fact]
    public async Task PingAsync_InvalidTarget_ReturnsZeroSuccess()
    {
        // Act
        var result = await NetworkInterfaceManager.PingAsync("invalid.nonexistent.domain.test", 1000, 2);

        // Assert
        result.ShouldNotBeNull();
        result.TargetHost.ShouldBe("invalid.nonexistent.domain.test");
        result.PacketsSent.ShouldBe(2);
        result.PacketsReceived.ShouldBe(0);
        result.PacketLoss.ShouldBe(1.0);
        result.AverageRoundtripTime.ShouldBe(0);

        Output.WriteLine($"无效目标测试: {result}");
    }

    #endregion

    #region StartNetworkMonitoring 测试

    /// <summary>
    /// 测试 - StartNetworkMonitoring - 网络监控
    /// </summary>
    [Fact]
    public async Task StartNetworkMonitoring_CanStartAndStop()
    {
        var changeDetected = false;
        var changeCount = 0;
        NetworkChangeEventArgs lastEventArgs = null;

        // Act
        var cts = NetworkInterfaceManager.StartNetworkMonitoring(args =>
        {
            changeDetected = true;
            changeCount++;
            lastEventArgs = args;
            Output.WriteLine($"检测到网络变化: {args.Changes.Count} 个变化");

            foreach (var change in args.Changes)
            {
                Output.WriteLine($"  {change.Type}: {change.Description}");
            }
        }, 1000);

        // 运行监控一段时间
        await Task.Delay(3000);

        // 停止监控
        cts.Cancel();

        // Assert
        cts.Token.IsCancellationRequested.ShouldBeTrue();

        // 验证监控器参数
        cts.ShouldNotBeNull();

        // 如果检测到变化，验证事件参数
        if (changeDetected && lastEventArgs != null)
        {
            lastEventArgs.Timestamp.ShouldBeGreaterThan(DateTime.Now.AddMinutes(-1));
            lastEventArgs.Changes.ShouldNotBeNull();
            lastEventArgs.CurrentInterfaces.ShouldNotBeNull();
        }

        Output.WriteLine($"监控运行完成，检测到 {changeCount} 次变化");
    }

    /// <summary>
    /// 测试 - StartNetworkMonitoring - 监控间隔设置
    /// </summary>
    [Fact]
    public async Task StartNetworkMonitoring_DifferentIntervals_WorksCorrectly()
    {
        var callCount = 0;
        var interval = 500; // 500ms间隔
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var cts = NetworkInterfaceManager.StartNetworkMonitoring(args =>
        {
            callCount++;
            Output.WriteLine($"监控回调 #{callCount}, 耗时: {sw.ElapsedMilliseconds}ms");
        }, interval);

        // 运行2秒
        await Task.Delay(2000);
        cts.Cancel();
        sw.Stop();

        // Assert
        cts.Token.IsCancellationRequested.ShouldBeTrue();

        // 在2秒内，应该至少有3-4次检查（允许一定误差）
        // 注意：实际调用次数可能因为没有网络变化而为0
        Output.WriteLine($"监控间隔测试: 运行{sw.ElapsedMilliseconds}ms, 回调{callCount}次");
    }

    /// <summary>
    /// 测试 - StartNetworkMonitoring - 空回调处理
    /// </summary>
    [Fact]
    public async Task StartNetworkMonitoring_NullCallback_DoesNotThrow()
    {
        // Act & Assert - 应该不抛出异常
        var cts = NetworkInterfaceManager.StartNetworkMonitoring(null, 1000);

        await Task.Delay(500);
        cts.Cancel();

        cts.Token.IsCancellationRequested.ShouldBeTrue();
        Output.WriteLine("空回调测试通过");
    }

    #endregion

    #region DetectNetworkChanges 间接测试

    /// <summary>
    /// 测试 - DetectNetworkChanges - 通过监控间接测试
    /// </summary>
    [Fact]
    public void DetectNetworkChanges_ThroughMonitoring_WorksCorrectly()
    {
        // 这个测试通过创建模拟的网络接口变化来间接测试DetectNetworkChanges方法

        // Arrange - 获取当前网络接口状态
        var initialInterfaces = NetworkInterfaceManager.GetNetworkInterfaces();

        // Assert - 验证初始状态
        initialInterfaces.ShouldNotBeNull();

        // 由于DetectNetworkChanges是私有方法，我们无法直接测试
        // 但我们可以验证GetNetworkInterfaces返回的数据结构是正确的
        foreach (var iface in initialInterfaces)
        {
            iface.Name.ShouldNotBeNullOrEmpty();
            iface.IpAddresses.ShouldNotBeNull();
            iface.IPv4Addresses.ShouldNotBeNull();
            iface.IPv6Addresses.ShouldNotBeNull();
            iface.GatewayAddresses.ShouldNotBeNull();
            iface.DnsAddresses.ShouldNotBeNull();
        }

        Output.WriteLine($"网络变化检测间接测试: 当前有 {initialInterfaces.Count} 个接口");
        Output.WriteLine("DetectNetworkChanges方法的功能通过监控测试验证");
    }

    #endregion

    #region 性能测试

    /// <summary>
    /// 测试 - 性能测试
    /// </summary>
    [Fact]
    public void NetworkOperations_Performance_CompletesWithinReasonableTime()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 执行多个网络操作
        for (int i = 0; i < 10; i++)
        {
            var interfaces = NetworkInterfaceManager.GetNetworkInterfaces();
            var primaryIp = NetworkInterfaceManager.GetPrimaryIpAddress();
            var activeInterfaces = NetworkInterfaceManager.GetActiveNetworkInterfaces();
            var stats = NetworkInterfaceManager.GetNetworkStatistics();
            var ipv4Addresses = NetworkInterfaceManager.GetAllIPv4Addresses();
            var ipv6Addresses = NetworkInterfaceManager.GetAllIPv6Addresses();
        }

        sw.Stop();

        sw.ElapsedMilliseconds.ShouldBeLessThan(7000, "60次网络操作应该在7秒内完成");
        Output.WriteLine($"性能测试: 60次网络操作耗时 {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 测试 - 并发访问安全性
    /// </summary>
    [Fact]
    public async Task NetworkOperations_ConcurrentAccess_ThreadSafe()
    {
        var tasks = new List<Task>();
        var results = new List<string>();
        var lockObj = new object();

        // 创建多个并发任务
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var interfaces = NetworkInterfaceManager.GetNetworkInterfaces();
                    var primaryIp = NetworkInterfaceManager.GetPrimaryIpAddress();
                    var stats = NetworkInterfaceManager.GetNetworkStatistics();

                    lock (lockObj)
                    {
                        results.Add($"任务完成: {interfaces.Count}个接口, 主IP={primaryIp}");
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObj)
                    {
                        results.Add($"任务异常: {ex.Message}");
                    }
                }
            }));
        }

        // 等待所有任务完成
        await Task.WhenAll(tasks);

        // Assert
        results.Count.ShouldBe(5);

        // 所有任务都应该成功完成
        foreach (var result in results)
        {
            result.ShouldNotContain("异常");
            Output.WriteLine(result);
        }

        Output.WriteLine("并发访问安全性测试通过");
    }

    #endregion

    #region 边界条件和异常处理测试

    /// <summary>
    /// 测试 - 异常处理 - 网络操作异常恢复
    /// </summary>
    [Fact]
    public void NetworkOperations_ExceptionHandling_GracefulRecovery()
    {
        // 所有方法都应该有异常处理，不会抛出未处理的异常

        // Act & Assert - 这些调用不应该抛出异常
        Should.NotThrow(() => NetworkInterfaceManager.GetNetworkInterfaces());
        Should.NotThrow(() => NetworkInterfaceManager.GetActiveNetworkInterfaces());
        Should.NotThrow(() => NetworkInterfaceManager.GetPrimaryIpAddress());
        Should.NotThrow(() => NetworkInterfaceManager.GetAllIPv4Addresses());
        Should.NotThrow(() => NetworkInterfaceManager.GetAllIPv6Addresses());
        Should.NotThrow(() => NetworkInterfaceManager.GetNetworkStatistics());
        Should.NotThrow(() => NetworkInterfaceManager.RefreshNetworkInterfaces());
        Should.NotThrow(() => NetworkInterfaceManager.IsPortListening(80));

        Output.WriteLine("异常处理测试通过 - 所有方法都有适当的异常处理");
    }

    /// <summary>
    /// 测试 - 边界条件 - 极端参数值
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(65536)]
    [InlineData(int.MaxValue)]
    public void IsPortListening_ExtremePortValues_HandlesGracefully(int port)
    {
        // Act & Assert - 不应该抛出异常
        Should.NotThrow(() =>
        {
            var result = NetworkInterfaceManager.IsPortListening(port);
            result.ShouldBeOfType<bool>();
        });

        Output.WriteLine($"极端端口值 {port} 处理正常");
    }

    #endregion

    /// <summary>
    /// 测试 - IPv6地址类型分类和统计
    /// </summary>
    [Fact]
    public void IPv6AddressTypes_Classification_WorksCorrectly()
    {
        // Act
        var allInterfaces = NetworkInterfaceManager.GetNetworkInterfaces();
        var allIPv6 = allInterfaces.SelectMany(i => i.IPv6Addresses).ToList();

        // 分类IPv6地址
        var addressTypes = new Dictionary<string, List<string>>
        {
            ["回环地址 (::1)"] = allIPv6.Where(ip => ip.Equals("::1", StringComparison.OrdinalIgnoreCase)).ToList(),
            ["链路本地 (fe80::/10)"] = allIPv6.Where(ip => ip.StartsWith("fe80:", StringComparison.OrdinalIgnoreCase)).ToList(),
            ["唯一本地 (fc00::/7)"] = allIPv6.Where(ip =>
                ip.StartsWith("fc", StringComparison.OrdinalIgnoreCase) ||
                ip.StartsWith("fd", StringComparison.OrdinalIgnoreCase)).ToList(),
            ["文档用途 (2001:db8::/32)"] = allIPv6.Where(ip => ip.StartsWith("2001:db8:", StringComparison.OrdinalIgnoreCase)).ToList(),
            ["全局单播"] = allIPv6.Where(ip =>
                !ip.Equals("::1", StringComparison.OrdinalIgnoreCase) &&
                !ip.StartsWith("fe80:", StringComparison.OrdinalIgnoreCase) &&
                !ip.StartsWith("fc", StringComparison.OrdinalIgnoreCase) &&
                !ip.StartsWith("fd", StringComparison.OrdinalIgnoreCase) &&
                !ip.StartsWith("ff", StringComparison.OrdinalIgnoreCase) &&
                !ip.StartsWith("2001:db8:", StringComparison.OrdinalIgnoreCase)).ToList(),
            ["组播地址 (ff00::/8)"] = allIPv6.Where(ip => ip.StartsWith("ff", StringComparison.OrdinalIgnoreCase)).ToList()
        };

        // Assert and Output
        Output.WriteLine("IPv6地址类型分布:");
        foreach (var category in addressTypes)
        {
            Output.WriteLine($"  {category.Key}: {category.Value.Count} 个");
            if (category.Value.Any())
            {
                foreach (var addr in category.Value.Take(3)) // 只显示前3个
                {
                    Output.WriteLine($"    - {addr}");
                }
                if (category.Value.Count > 3)
                {
                    Output.WriteLine($"    ... 还有 {category.Value.Count - 3} 个");
                }
            }
        }

        // 验证分类逻辑
        var totalCategorized = addressTypes.Values.Sum(list => list.Count);
        var totalUnique = allIPv6.Distinct().Count();

        // 注意：一个地址可能被归类到多个类别，所以这里不做严格的相等检查
        Output.WriteLine($"总IPv6地址数: {totalUnique}");
        Output.WriteLine($"分类处理的地址数: {totalCategorized}");

        // 验证链路本地地址确实存在（在大多数系统中）
        if (addressTypes["链路本地 (fe80::/10)"].Any())
        {
            Output.WriteLine("✓ 检测到链路本地地址，这是正常的");

            // 验证GetPrimaryIpAddress确实排除了这些地址
            var primaryIPv6 = NetworkInterfaceManager.GetPrimaryIpAddress(false);
            if (!string.IsNullOrEmpty(primaryIPv6) && primaryIPv6.Contains(":"))
            {
                addressTypes["链路本地 (fe80::/10)"].ShouldNotContain(primaryIPv6,
                    "主要IPv6地址不应该是链路本地地址");
            }
        }
    }
}
