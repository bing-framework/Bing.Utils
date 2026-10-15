namespace Bing.Net;

/// <summary>
/// 验证依赖公网查询服务的 IP 地址提供器集成行为。
/// </summary>
[Trait("Bing.Net", "IpAddressProviderIntegration")]
public class IpAddressProviderIntegrationTest : TestBase
{
    /// <summary>
    /// 初始化一个 <see cref="IpAddressProviderIntegrationTest"/> 类型的实例。
    /// </summary>
    /// <param name="output">当前测试使用的输出助手。</param>
    public IpAddressProviderIntegrationTest(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// 验证顺序模式在可用公网查询服务下应返回有效的非本地 IP，服务不可用时可返回 null。
    /// </summary>
    [Fact]
    public async Task GetPublicIpAsync_SequentialMode_ReturnsValidPublicIpOrNull()
    {
        // Act
        var result = await IpAddressProvider.GetPublicIpAsync(TimeSpan.FromSeconds(10), useParallel: false);

        // Assert
        AssertPublicIpOrNull(result);
    }

    /// <summary>
    /// 验证并行模式在可用公网查询服务下应返回有效的非本地 IP，服务不可用时可返回 null。
    /// </summary>
    [Fact]
    public async Task GetPublicIpAsync_ParallelMode_ReturnsValidPublicIpOrNull()
    {
        // Act
        var result = await IpAddressProvider.GetPublicIpAsync(TimeSpan.FromSeconds(10), useParallel: true);

        // Assert
        AssertPublicIpOrNull(result);
    }

    /// <summary>
    /// 验证极短超时应由公网查询路径处理，不应抛出异常。
    /// </summary>
    [Fact]
    public async Task GetPublicIpAsync_WithShortTimeout_HandlesTimeoutGracefully()
    {
        // Act
        var result = await IpAddressProvider.GetPublicIpAsync(TimeSpan.FromMilliseconds(1), useParallel: false);

        // Assert
        AssertPublicIpOrNull(result);
    }

    /// <summary>
    /// 验证默认参数应在公网查询服务可用时返回有效的非本地 IP，服务不可用时可返回 null。
    /// </summary>
    [Fact]
    public async Task GetPublicIpAsync_DefaultParameters_ReturnsValidPublicIpOrNull()
    {
        // Act
        var result = await IpAddressProvider.GetPublicIpAsync();

        // Assert
        AssertPublicIpOrNull(result);
    }

    /// <summary>
    /// 验证查询结果为空或为有效的非本地公网地址。
    /// </summary>
    /// <param name="result">公网查询服务返回的 IP 地址或空值。</param>
    private void AssertPublicIpOrNull(string result)
    {
        if (result == null)
        {
            Output.WriteLine("未能获取公网 IP，当前网络或查询服务不可用。");
            return;
        }

        IpValidator.IsValid(result).ShouldBeTrue();
        IpValidator.IsInnerIp(result).ShouldBeFalse();
        IpValidator.IsLocalIp(result).ShouldBeFalse();
        Output.WriteLine($"获取到公网 IP: {result}");
    }
}