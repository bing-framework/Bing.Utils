using System.Reflection;

namespace Bing.Net.FTP;

/// <summary>
/// FtpClient 到 FluentFTP 配置映射测试
/// </summary>
public class FtpClientConfigurationTest
{
    /// <summary>
    /// 测试目的：验证默认超时和被动模式映射到 FluentFTP，且构造过程不建立连接。
    /// </summary>
    [Fact]
    public void Constructor_WithDefaultConfiguration_MapsUnderlyingConfigurationWithoutConnecting()
    {
        var config = new FtpClientConfig { Host = "127.0.0.1" };

        using var client = new FtpClient(config);

        AssertConfiguration(client, config, FluentFTP.FtpDataConnectionType.AutoPassive);
    }

    /// <summary>
    /// 测试目的：验证自定义超时和被动模式映射到 FluentFTP。
    /// </summary>
    [Fact]
    public void Constructor_WithCustomPassiveConfiguration_MapsUnderlyingConfigurationWithoutConnecting()
    {
        var config = new FtpClientConfig
        {
            Host = "127.0.0.1",
            Timeout = 1_000,
            ReadWriteTimeout = 2_000,
            UsePassive = true
        };

        using var client = new FtpClient(config);

        AssertConfiguration(client, config, FluentFTP.FtpDataConnectionType.AutoPassive);
    }

    /// <summary>
    /// 测试目的：验证关闭被动模式后映射为 FluentFTP 主动模式。
    /// </summary>
    [Fact]
    public void Constructor_WithCustomActiveConfiguration_MapsUnderlyingConfigurationWithoutConnecting()
    {
        var config = new FtpClientConfig
        {
            Host = "127.0.0.1",
            Timeout = 3_000,
            ReadWriteTimeout = 4_000,
            UsePassive = false
        };

        using var client = new FtpClient(config);

        AssertConfiguration(client, config, FluentFTP.FtpDataConnectionType.AutoActive);
    }

    private static void AssertConfiguration(
        FtpClient client,
        FtpClientConfig config,
        FluentFTP.FtpDataConnectionType expectedDataConnectionType)
    {
        var fluentClient = GetInnerClient(client);

        Assert.Equal(config.Timeout, fluentClient.Config.ConnectTimeout);
        Assert.Equal(config.Timeout, fluentClient.Config.DataConnectionConnectTimeout);
        Assert.Equal(config.ReadWriteTimeout, fluentClient.Config.ReadTimeout);
        Assert.Equal(config.ReadWriteTimeout, fluentClient.Config.DataConnectionReadTimeout);
        Assert.Equal(expectedDataConnectionType, fluentClient.Config.DataConnectionType);
        Assert.False(fluentClient.IsConnected);
    }

    private static FluentFTP.FtpClient GetInnerClient(FtpClient client)
    {
        var field = typeof(FtpClient).GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        return Assert.IsType<FluentFTP.FtpClient>(field.GetValue(client));
    }
}
