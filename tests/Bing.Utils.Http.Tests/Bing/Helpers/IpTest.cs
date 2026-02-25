using Bing.Net;
namespace Bing.Helpers;
/// <summary>
/// IP地址操作工具类 单元测试
/// </summary>
[Trait("Bing.Helpers", "Ip")]
public class IpTest : TestBase
{
    /// <inheritdoc />
    public IpTest(ITestOutputHelper output) : base(output)
    {
        // 重置IP状态
        Ip.Reset();
    }
    #region 基础IP操作测试
    /// <summary>
    /// 测试 - SetIp - 设置有效IPv4地址
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("127.0.0.1")]
    [InlineData("8.8.8.8")]
    public void SetIp_ValidIPv4_SetsSuccessfully(string ip)
    {
        // Act
        Ip.SetIp(ip);
        // Assert
        Ip.GetIp().ShouldBe(ip);
    }
    /// <summary>
    /// 测试 - SetIp - 设置有效IPv6地址
    /// </summary>
    [Theory]
    [InlineData("2001:db8::1")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("2001:4860:4860::8888")]
    public void SetIp_ValidIPv6_SetsSuccessfully(string ip)
    {
        // Act
        Ip.SetIp(ip);
        // Assert
        Ip.GetIp().ShouldBe(ip);
    }
    /// <summary>
    /// 测试 - SetIp - 设置无效IP地址应抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("256.1.1.1")]
    [InlineData("192.168.1")]        // 缺少最后一个段
    [InlineData("192.168.1.1.1")]    // 多余的段
    [InlineData("gggg::1")]
    [InlineData("192.168")]          // 只有两个段
    [InlineData("192")]              // 只有一个段
    [InlineData("192.168.1.")]       // 末尾有点
    [InlineData(".192.168.1.1")]     // 开头有点
    [InlineData("192..168.1.1")]     // 连续的点
    [InlineData("192.168.01.1")]     // 前导零
    public void SetIp_InvalidIP_ThrowsArgumentException(string invalidIp)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Ip.SetIp(invalidIp))
            .Message.ShouldContain("无效的IP地址格式");
    }
    /// <summary>
    /// 测试 - SetIp - 设置null或空字符串
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void SetIp_NullOrEmpty_SetsSuccessfully(string ip)
    {
        // Act
        Should.NotThrow(() => Ip.SetIp(ip));
    }
    /// <summary>
    /// 测试 - Reset - 重置IP地址
    /// </summary>
    [Fact]
    public void Reset_AfterSetIp_ClearsIpValue()
    {
        // Arrange
        Ip.SetIp("192.168.1.1");
        // Act
        Ip.Reset();
        // Assert
        var result = Ip.GetIp();
        result.ShouldNotBe("192.168.1.1");
    }
    /// <summary>
    /// 测试 - GetAllLocalIps - 获取本机IP地址
    /// </summary>
    [Fact]
    public void GetAllLocalIps_DefaultParameters_ReturnsIPList()
    {
        // Act
        var ips = Ip.GetAllLocalIps();
        // Assert
        ips.ShouldNotBeNull();
        Output.WriteLine($"获取到 {ips.Count} 个IP地址：");
        foreach (var ip in ips)
        {
            Output.WriteLine($"  {ip}");
            IpValidator.IsValid(ip).ShouldBeTrue();
        }
    }
    /// <summary>
    /// 测试 - GetAllLocalIps - 包含IPv6地址
    /// </summary>
    [Fact]
    public void GetAllLocalIps_IncludeIPv6_ReturnsIPv6Addresses()
    {
        // Act
        var ips = Ip.GetAllLocalIps(includeIPv6: true);
        // Assert
        ips.ShouldNotBeNull();
        var ipv6Count = ips.Count(ip => IpValidator.IsValidIPv6(ip));
        Output.WriteLine($"获取到 {ips.Count} 个IP地址，其中 {ipv6Count} 个IPv6地址");
    }
    /// <summary>
    /// 测试 - GetAllLocalIps - 包含回环地址
    /// </summary>
    [Fact]
    public void GetAllLocalIps_IncludeLoopback_ReturnsLoopbackAddresses()
    {
        // Act
        var ips = Ip.GetAllLocalIps(includeLoopback: true);
        // Assert
        ips.ShouldNotBeNull();
        var loopbackCount = ips.Count(ip => IpValidator.IsLocalIp(ip));
        Output.WriteLine($"获取到 {ips.Count} 个IP地址，其中 {loopbackCount} 个回环地址");
    }
    #endregion
}
