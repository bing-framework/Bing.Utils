namespace Bing.Net.IPv4;
/// <summary>
/// IPv4地址操作器 测试
/// </summary>
[Trait("Bing.Net", "IpOperator")]
public class IPv4OperatorTest : TestBase
{
    /// <inheritdoc />
    public IPv4OperatorTest(ITestOutputHelper output) : base(output)
    {
    }
    #region SortIpAddresses 测试
    /// <summary>
    /// 测试 - SortIpAddresses - 升序排序
    /// </summary>
    [Fact]
    public void SortIpAddresses_AscendingOrder_SortsCorrectly()
    {
        // Arrange
        var ips = new[] { "192.168.1.10", "192.168.1.2", "192.168.1.100", "192.168.1.1" };
        // Act
        var result = IPv4Operator.SortIpAddresses(ips, true);
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(4);
        result[0].ShouldBe("192.168.1.1");
        result[1].ShouldBe("192.168.1.2");
        result[2].ShouldBe("192.168.1.10");
        result[3].ShouldBe("192.168.1.100");
    }
    /// <summary>
    /// 测试 - SortIpAddresses - 降序排序
    /// </summary>
    [Fact]
    public void SortIpAddresses_DescendingOrder_SortsCorrectly()
    {
        // Arrange
        var ips = new[] { "192.168.1.1", "192.168.1.10", "192.168.1.2" };
        // Act
        var result = IPv4Operator.SortIpAddresses(ips, false);
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].ShouldBe("192.168.1.10");
        result[1].ShouldBe("192.168.1.2");
        result[2].ShouldBe("192.168.1.1");
    }
    /// <summary>
    /// 测试 - SortIpAddresses - 包含无效IP地址
    /// </summary>
    [Fact]
    public void SortIpAddresses_WithInvalidIps_FiltersInvalidIps()
    {
        // Arrange
        var ips = new[] { "192.168.1.1", "invalid", "192.168.1.2", "256.1.1.1" };
        // Act
        var result = IPv4Operator.SortIpAddresses(ips);
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].ShouldBe("192.168.1.1");
        result[1].ShouldBe("192.168.1.2");
    }
    /// <summary>
    /// 测试 - SortIpAddresses - null参数抛出异常
    /// </summary>
    [Fact]
    public void SortIpAddresses_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => IPv4Operator.SortIpAddresses(null));
    }
    /// <summary>
    /// 测试 - SortIpAddresses - 空列表
    /// </summary>
    [Fact]
    public void SortIpAddresses_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var ips = new string[0];
        // Act
        var result = IPv4Operator.SortIpAddresses(ips);
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }
    #endregion
    #region GetIpDistance 测试
    /// <summary>
    /// 测试 - GetIpDistance - 正常计算距离
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", "192.168.1.10", 9u)]
    [InlineData("192.168.1.10", "192.168.1.1", 9u)]
    [InlineData("192.168.1.1", "192.168.1.1", 0u)]
    [InlineData("0.0.0.0", "255.255.255.255", 4294967295u)]
    public void GetIpDistance_ValidIps_ReturnsCorrectDistance(string ip1, string ip2, uint expected)
    {
        // Act
        var result = IPv4Operator.GetIpDistance(ip1, ip2);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - GetIpDistance - 无效IP地址抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid", "192.168.1.1")]
    [InlineData("192.168.1.1", "256.1.1.1")]
    [InlineData("192.168.1", "192.168.1.1")]
    public void GetIpDistance_InvalidIp_ThrowsArgumentException(string ip1, string ip2)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4Operator.GetIpDistance(ip1, ip2));
    }
    /// <summary>
    /// 测试 - GetIpDistance - null或空参数抛出异常
    /// </summary>
    [Theory]
    [InlineData(null, "192.168.1.1")]
    [InlineData("192.168.1.1", null)]
    [InlineData("", "192.168.1.1")]
    [InlineData("192.168.1.1", "")]
    [InlineData("   ", "192.168.1.1")]
    public void GetIpDistance_NullOrEmptyInput_ThrowsArgumentNullException(string ip1, string ip2)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => IPv4Operator.GetIpDistance(ip1, ip2));
    }
    #endregion
    #region GetNextIp 测试
    /// <summary>
    /// 测试 - GetNextIp - 正常获取下一个IP
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", 1u, "192.168.1.2")]
    [InlineData("192.168.1.1", 5u, "192.168.1.6")]
    [InlineData("192.168.1.254", 1u, "192.168.1.255")]
    [InlineData("0.0.0.0", 1u, "0.0.0.1")]
    public void GetNextIp_ValidInputs_ReturnsNextIp(string ip, uint step, string expected)
    {
        // Act
        var result = IPv4Operator.GetNextIp(ip, step);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - GetNextIp - IP地址溢出抛出异常
    /// </summary>
    [Theory]
    [InlineData("255.255.255.255", 1u)]
    [InlineData("255.255.255.254", 2u)]
    public void GetNextIp_Overflow_ThrowsArgumentException(string ip, uint step)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4Operator.GetNextIp(ip, step));
    }
    /// <summary>
    /// 测试 - GetNextIp - 无效IP地址抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("256.1.1.1")]
    [InlineData("192.168.1")]
    public void GetNextIp_InvalidIp_ThrowsArgumentException(string invalidIp)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4Operator.GetNextIp(invalidIp));
    }
    #endregion
    #region GetPreviousIp 测试
    /// <summary>
    /// 测试 - GetPreviousIp - 正常获取前一个IP
    /// </summary>
    [Theory]
    [InlineData("192.168.1.10", 1u, "192.168.1.9")]
    [InlineData("192.168.1.10", 5u, "192.168.1.5")]
    [InlineData("192.168.1.1", 1u, "192.168.1.0")]
    [InlineData("255.255.255.255", 1u, "255.255.255.254")]
    public void GetPreviousIp_ValidInputs_ReturnsPreviousIp(string ip, uint step, string expected)
    {
        // Act
        var result = IPv4Operator.GetPreviousIp(ip, step);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - GetPreviousIp - IP地址下溢抛出异常
    /// </summary>
    [Theory]
    [InlineData("0.0.0.0", 1u)]
    [InlineData("0.0.0.1", 2u)]
    [InlineData("0.0.0.5", 10u)]
    public void GetPreviousIp_Underflow_ThrowsArgumentException(string ip, uint step)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4Operator.GetPreviousIp(ip, step));
    }
    /// <summary>
    /// 测试 - GetPreviousIp - 无效IP地址抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("256.1.1.1")]
    public void GetPreviousIp_InvalidIp_ThrowsArgumentException(string invalidIp)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv4Operator.GetPreviousIp(invalidIp));
    }
    /// <summary>
    /// 测试 - GetPreviousIp - 跨网段的正常计算
    /// </summary>
    [Theory]
    [InlineData("192.168.1.5", 10u, "192.168.0.251")]
    [InlineData("10.0.1.0", 1u, "10.0.0.255")]
    public void GetPreviousIp_CrossSegment_ReturnsCorrectIp(string ip, uint step, string expected)
    {
        // Act
        var result = IPv4Operator.GetPreviousIp(ip, step);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region ScanActiveIpsAsync 测试
    /// <summary>
    /// 测试 - ScanActiveIpsAsync - 正常扫描（模拟小网段）
    /// </summary>
    [Fact]
    public async Task ScanActiveIpsAsync_ValidCidr_ReturnsActiveIps()
    {
        // Arrange
        var cidr = "127.0.0.0/30"; // 只包含4个地址，其中127.0.0.1可能活跃
        var timeout = TimeSpan.FromMilliseconds(500);
        // Act
        var result = await IPv4Operator.ScanActiveIpsAsync(cidr, timeout, 2);
        // Assert
        result.ShouldNotBeNull();
        // 结果可能为空或包含127.0.0.1，取决于环境
    }
    /// <summary>
    /// 测试 - ScanActiveIpsAsync - 无效CIDR抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("192.168.1.0/33")]
    [InlineData("256.1.1.0/24")]
    public async Task ScanActiveIpsAsync_InvalidCidr_ThrowsArgumentException(string invalidCidr)
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            IPv4Operator.ScanActiveIpsAsync(invalidCidr));
    }
    /// <summary>
    /// 测试 - ScanActiveIpsAsync - 无效超时时间抛出异常
    /// </summary>
    [Fact]
    public async Task ScanActiveIpsAsync_InvalidTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var cidr = "127.0.0.0/30";
        var invalidTimeout = TimeSpan.FromMilliseconds(-1);
        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            IPv4Operator.ScanActiveIpsAsync(cidr, invalidTimeout));
    }
    /// <summary>
    /// 测试 - ScanActiveIpsAsync - 无效并发数抛出异常
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ScanActiveIpsAsync_InvalidConcurrency_ThrowsArgumentOutOfRangeException(int invalidConcurrency)
    {
        // Arrange
        var cidr = "127.0.0.0/30";
        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            IPv4Operator.ScanActiveIpsAsync(cidr, null, invalidConcurrency));
    }
    #endregion
    #region IsIpReachableAsync 测试
    /// <summary>
    /// 测试 - IsIpReachableAsync - 本地回环地址
    /// </summary>
    [Fact]
    public async Task IsIpReachableAsync_Localhost_ReturnsBoolean()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(1);
        // Act
        var result = await IPv4Operator.IsIpReachableAsync("127.0.0.1", timeout);
        // Assert
        result.ShouldBeOfType<bool>();
        // 注意：在某些环境下127.0.0.1可能不响应ping，这里只验证方法不抛异常
    }
    /// <summary>
    /// 测试 - IsIpReachableAsync - 无效IP地址返回false
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("256.1.1.1")]
    [InlineData("")]
    [InlineData(null)]
    public async Task IsIpReachableAsync_InvalidIp_ReturnsFalse(string invalidIp)
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(1);
        // Act
        var result = await IPv4Operator.IsIpReachableAsync(invalidIp, timeout);
        // Assert
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - IsIpReachableAsync - 无效超时时间抛出异常
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public async Task IsIpReachableAsync_InvalidTimeout_ThrowsArgumentOutOfRangeException(int timeoutMs)
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            IPv4Operator.IsIpReachableAsync("127.0.0.1", timeout));
    }
    /// <summary>
    /// 测试 - IsIpReachableAsync - 超时时间过大抛出异常
    /// </summary>
    [Fact]
    public async Task IsIpReachableAsync_TimeoutTooLarge_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds((long)int.MaxValue + 1);
        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            IPv4Operator.IsIpReachableAsync("127.0.0.1", timeout));
    }
    #endregion
    #region 边界值和异常测试
    /// <summary>
    /// 测试 - 各方法的边界值处理
    /// </summary>
    [Fact]
    public void VariousMethods_BoundaryValues_HandleCorrectly()
    {
        // GetNextIp边界值
        var nextMin = IPv4Operator.GetNextIp("0.0.0.0", 0u);
        nextMin.ShouldBe("0.0.0.0");
        // GetPreviousIp边界值
        var prevMax = IPv4Operator.GetPreviousIp("255.255.255.255", 0u);
        prevMax.ShouldBe("255.255.255.255");
        // GetIpDistance相同IP
        var distance = IPv4Operator.GetIpDistance("192.168.1.1", "192.168.1.1");
        distance.ShouldBe(0u);
    }
    /// <summary>
    /// 测试 - SortIpAddresses - 重复IP地址
    /// </summary>
    [Fact]
    public void SortIpAddresses_DuplicateIps_HandlesCorrectly()
    {
        // Arrange
        var ips = new[] { "192.168.1.1", "192.168.1.2", "192.168.1.1", "192.168.1.2" };
        // Act
        var result = IPv4Operator.SortIpAddresses(ips);
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(4);
        result[0].ShouldBe("192.168.1.1");
        result[1].ShouldBe("192.168.1.1");
        result[2].ShouldBe("192.168.1.2");
        result[3].ShouldBe("192.168.1.2");
    }
    #endregion
}
