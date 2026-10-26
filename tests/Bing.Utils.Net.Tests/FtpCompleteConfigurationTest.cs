using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;

namespace Bing.Net.FTP;

/// <summary>
/// 验证同步 FTP 客户端完整配置模式的映射与操作生命周期。
/// </summary>
public sealed class FtpCompleteConfigurationTest
{
    /// <summary>
    /// 验证完整模式应用 SSL 回退、数据类型和显式接收超时。
    /// </summary>
    [Fact]
    public void CompleteMode_MapsSslTransferTypeAndExplicitReceiveTimeout()
    {
        var config = new FtpClientConfig
        {
            Host = "127.0.0.1",
            EnableSsl = true,
            UseBinary = false,
            ReceiveTimeout = 1_234
        };

        using var client = new FtpClient(config, FtpConfigurationMode.Complete);
        var native = GetNativeClient(client);

        Assert.Equal(FluentFTP.FtpEncryptionMode.Explicit, native.Config.EncryptionMode);
        Assert.Equal(FluentFTP.FtpDataType.ASCII, native.Config.UploadDataType);
        Assert.Equal(FluentFTP.FtpDataType.ASCII, native.Config.DownloadDataType);
        Assert.Equal(1_234, native.Config.ReadTimeout);
        Assert.Equal(1_234, native.Config.DataConnectionReadTimeout);
    }

    /// <summary>
    /// 验证显式加密类型优先于 EnableSsl 的显式回退规则。
    /// </summary>
    [Theory]
    [InlineData(EncryptionType.Explicit, FluentFTP.FtpEncryptionMode.Explicit)]
    [InlineData(EncryptionType.Implicit, FluentFTP.FtpEncryptionMode.Implicit)]
    public void CompleteMode_ExplicitEncryptionTypeTakesPrecedence(
        EncryptionType encryptionType,
        FluentFTP.FtpEncryptionMode expected)
    {
        var actual = FtpClient.ToNativeEncryptionMode(encryptionType, true, FtpConfigurationMode.Complete);

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// 验证 Legacy 模式仍按历史配置映射 SSL 标志和默认读取超时。
    /// </summary>
    [Fact]
    public void LegacyMode_IgnoresCompleteModeOnlySettings()
    {
        var config = new FtpClientConfig
        {
            Host = "127.0.0.1",
            EnableSsl = true,
            SendTimeout = 777
        };

        using var client = new FtpClient(config);
        var native = GetNativeClient(client);

        Assert.Equal(FluentFTP.FtpEncryptionMode.None, native.Config.EncryptionMode);
        Assert.Equal(config.ReadWriteTimeout, native.Config.ReadTimeout);
    }

    /// <summary>
    /// 验证完整模式使用构造时配置快照，不受调用方后续修改影响。
    /// </summary>
    [Fact]
    public void CompleteMode_CopiesConfigurationSnapshot()
    {
        var config = new FtpClientConfig
        {
            Host = "127.0.0.1",
            RemoteDirectory = "/initial",
            KeepAlive = true
        };

        using var client = new FtpClient(config, FtpConfigurationMode.Complete);
        config.Host = "changed.example";
        config.RemoteDirectory = "/changed";
        config.KeepAlive = false;

        Assert.Equal("127.0.0.1", client.ConfigurationSnapshot.Host);
        Assert.Equal("/initial", client.ConfigurationSnapshot.RemoteDirectory);
        Assert.True(client.ConfigurationSnapshot.KeepAlive);
    }

    /// <summary>
    /// 验证完整模式拒绝当前后端无法兑现的显式超时。
    /// </summary>
    [Fact]
    public void CompleteMode_RejectsExplicitUnsupportedTimeouts()
    {
        var sendTimeout = new FtpClientConfig { Host = "127.0.0.1", SendTimeout = 300 };
        var disconnectTimeout = new FtpClientConfig { Host = "127.0.0.1", DisconnectTimeout = 300 };

        Assert.Throws<NotSupportedException>(() => new FtpClient(sendTimeout, FtpConfigurationMode.Complete));
        Assert.Throws<NotSupportedException>(() => new FtpClient(disconnectTimeout, FtpConfigurationMode.Complete));
        using var defaults = new FtpClient(new FtpClientConfig { Host = "127.0.0.1" }, FtpConfigurationMode.Complete);
        Assert.Equal(defaults.ConfigurationSnapshot.ReadWriteTimeout, GetNativeClient(defaults).Config.ReadTimeout);
        Assert.False(defaults.ConfigurationSnapshot.IsReceiveTimeoutExplicitlyConfigured);
    }

    /// <summary>
    /// 验证完整模式拒绝无效主机、端口、超时和负重试次数。
    /// </summary>
    [Fact]
    public void CompleteMode_RejectsInvalidConnectionConfiguration()
    {
        Assert.Throws<ArgumentException>(() => new FtpClient(
            new FtpClientConfig { Host = "ftp host" }, FtpConfigurationMode.Complete));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FtpClient(
            new FtpClientConfig { Host = "127.0.0.1", Port = 0 }, FtpConfigurationMode.Complete));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FtpClient(
            new FtpClientConfig { Host = "127.0.0.1", Port = 65536 }, FtpConfigurationMode.Complete));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FtpClient(
            new FtpClientConfig { Host = "127.0.0.1", Timeout = 0 }, FtpConfigurationMode.Complete));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FtpClient(
            new FtpClientConfig { Host = "127.0.0.1", ReadWriteTimeout = -1 }, FtpConfigurationMode.Complete));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FtpClient(
            new FtpClientConfig { Host = "127.0.0.1", ReceiveTimeout = 0 }, FtpConfigurationMode.Complete));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FtpClient(
            new FtpClientConfig { Host = "127.0.0.1", ConnectCount = -1 }, FtpConfigurationMode.Complete));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FtpClient(
            new FtpClientConfig { Host = "127.0.0.1", ProxyHost = "proxy", ProxyPort = 0 }, FtpConfigurationMode.Complete));
    }

    /// <summary>
    /// 验证连接计数表示首次连接之外的额外尝试，且失败清理不会阻止后续连接。
    /// </summary>
    [Fact]
    public void ConnectionAttempts_RetriesOnlyFailedConnectionAttempts()
    {
        var attempts = 0;
        var cleanups = 0;

        FtpClient.ExecuteConnectionAttempts(2, () =>
        {
            attempts++;
            if (attempts < 3)
                throw new FluentFTP.FtpException("connection failed");
        }, () => cleanups++);

        Assert.Equal(3, attempts);
        Assert.Equal(2, cleanups);
    }

    /// <summary>
    /// 验证连接阶段的 FTP、I/O 和 Socket 异常会重试，其他异常不会重试。
    /// </summary>
    [Fact]
    public void ConnectionAttempts_RetriesIoAndSocketFailuresOnly()
    {
        foreach (var failure in new Exception[]
                 {
                     new IOException("io failed"),
                     new SocketException((int)SocketError.ConnectionReset),
                     new TimeoutException("connection timed out")
                 })
        {
            var attempts = 0;
            var cleanups = 0;
            FtpClient.ExecuteConnectionAttempts(1, () =>
            {
                attempts++;
                if (attempts == 1)
                    throw failure;
            }, () => cleanups++);

            Assert.Equal(2, attempts);
            Assert.Equal(1, cleanups);
        }

        var nonConnectionAttempts = 0;
        var nonConnectionCleanups = 0;
        var unexpected = new InvalidOperationException("not a connection error");
        var thrown = Assert.Throws<InvalidOperationException>(() => FtpClient.ExecuteConnectionAttempts(3, () =>
        {
            nonConnectionAttempts++;
            throw unexpected;
        }, () => nonConnectionCleanups++));

        Assert.Same(unexpected, thrown);
        Assert.Equal(1, nonConnectionAttempts);
        Assert.Equal(1, nonConnectionCleanups);
    }

    /// <summary>
    /// 验证最终连接失败时会清理资源，且清理异常不会覆盖连接异常。
    /// </summary>
    [Fact]
    public void ConnectionAttempts_CleansUpAfterFinalFailureAndPreservesConnectionException()
    {
        var failure = new IOException("connection failed");
        var cleanups = 0;

        var thrown = Assert.Throws<IOException>(() => FtpClient.ExecuteConnectionAttempts(0, () => throw failure, () =>
        {
            cleanups++;
            throw new InvalidOperationException("cleanup failed");
        }));

        Assert.Same(failure, thrown);
        Assert.Equal(1, cleanups);
    }

    /// <summary>
    /// 验证完整模式默认代理类型为 HTTP/1.1，并可按配置选择 SOCKS5。
    /// </summary>
    [Fact]
    public void CompleteMode_CreatesConfiguredProxyClient()
    {
        var defaultProxyConfig = new FtpClientConfig
        {
            Host = "ftp.example.test",
            ProxyHost = "proxy.example.test",
            ProxyPort = 8080
        };
        var socksConfig = new FtpClientConfig
        {
            Host = "ftp.example.test",
            ProxyHost = "proxy.example.test",
            ProxyPort = 1080,
            ProxyType = FtpAsyncProxyType.Socks5
        };

        using var defaultProxyClient = new FtpClient(defaultProxyConfig, FtpConfigurationMode.Complete);
        using var socksProxyClient = new FtpClient(socksConfig, FtpConfigurationMode.Complete);

        Assert.IsType<FluentFTP.Proxy.SyncProxy.FtpClientHttp11Proxy>(GetNativeClient(defaultProxyClient));
        Assert.IsType<FluentFTP.Proxy.SyncProxy.FtpClientSocks5Proxy>(GetNativeClient(socksProxyClient));
    }

    /// <summary>
    /// 验证代理凭据在用户名或密码任一项配置时都会创建。
    /// </summary>
    [Fact]
    public void GetProxy_CreatesCredentialsWhenEitherCredentialIsProvided()
    {
        var usernameOnly = new FtpClientConfig
        {
            ProxyHost = "proxy.example.test",
            ProxyPort = 8080,
            ProxyUserName = "proxy-user"
        };
        var passwordOnly = new FtpClientConfig
        {
            ProxyHost = "proxy.example.test",
            ProxyPort = 8080,
            ProxyPassword = "proxy-password"
        };

        Assert.NotNull(usernameOnly.GetProxy().Credentials);
        Assert.NotNull(passwordOnly.GetProxy().Credentials);
    }

    /// <summary>
    /// 验证远程相对路径以配置目录为基准，绝对路径保持不变。
    /// </summary>
    [Fact]
    public void ResolveRemotePath_UsesConfiguredBaseAndPreservesAbsolutePath()
    {
        Assert.Equal("/base/child/file.txt", FtpClient.ResolveRemotePath("/base/", "child/file.txt"));
        Assert.Equal("/absolute/file.txt", FtpClient.ResolveRemotePath("/base", "/absolute/file.txt"));
        Assert.Equal("/base", FtpClient.GetRemoteDirectoryRoot("base/"));

        using var client = new FtpClient(
            new FtpClientConfig { Host = "127.0.0.1", RemoteDirectory = "/initial" },
            FtpConfigurationMode.Complete);
        Assert.Equal("/initial/child", client.ResolveConfiguredRemotePath("child"));
        Assert.Equal("/initial", client.GetCurrentDirectory());
        client.SetRemoteDirectoryBase("/next");
        Assert.Equal("/next", client.GetCurrentDirectory());
        Assert.Equal("/next/child", client.ResolveConfiguredRemotePath("child"));
        Assert.Equal("/absolute/child", client.ResolveConfiguredRemotePath("/absolute/child"));
    }

    /// <summary>
    /// 验证本地相对路径按配置目录解析，绝对路径不被改写。
    /// </summary>
    [Fact]
    public void ResolveLocalPath_UsesConfiguredBaseAndPreservesAbsolutePath()
    {
        var baseDirectory = Path.GetFullPath(Path.Combine("test-root", "local"));
        var absolutePath = Path.GetFullPath(Path.Combine("test-root", "absolute.txt"));

        Assert.Equal(Path.GetFullPath(Path.Combine(baseDirectory, "child.txt")), FtpClient.ResolveLocalPath(baseDirectory, "child.txt"));
        Assert.Equal(absolutePath, FtpClient.ResolveLocalPath(baseDirectory, absolutePath));
    }

    /// <summary>
    /// 验证完整模式 ASCII 传输不使用二进制长度比较和校验标志。
    /// </summary>
    [Fact]
    public void AsciiTransfer_SkipsBinaryLengthAndVerificationChecks()
    {
        Assert.False(FtpClient.ShouldCheckTransferLength(FtpConfigurationMode.Complete, false));
        Assert.Equal(
            FluentFTP.FtpVerify.None,
            FtpClient.GetTransferVerification(FtpConfigurationMode.Complete, false, FluentFTP.FtpVerify.Retry | FluentFTP.FtpVerify.Throw));
        Assert.True(FtpClient.ShouldCheckTransferLength(FtpConfigurationMode.Complete, true));
        Assert.True(FtpClient.ShouldCheckTransferLength(FtpConfigurationMode.Legacy, false));
        Assert.False(FtpClient.ShouldAcceptDownloadStatus(FtpConfigurationMode.Complete, false));
        Assert.True(FtpClient.ShouldAcceptDownloadStatus(FtpConfigurationMode.Complete, true));
        Assert.True(FtpClient.ShouldAcceptDownloadStatus(FtpConfigurationMode.Legacy, false));
    }

    /// <summary>
    /// 验证显式 Connect 保留连接，之后的独立操作仍按 KeepAlive 设置断开。
    /// </summary>
    [Fact]
    public void OperationGate_ExplicitConnectDoesNotDisconnectUntilNextOperation()
    {
        var connected = false;
        var events = new List<string>();
        var gate = new FtpOperationGate(false, () =>
        {
            if (connected)
                return;
            connected = true;
            events.Add("connect");
        }, () =>
        {
            connected = false;
            events.Add("disconnect");
        });

        using (gate.Enter(true, false))
            events.Add("explicit connect");

        Assert.True(connected);
        Assert.Equal(new[] { "connect", "explicit connect" }, events);

        using (gate.Enter())
            events.Add("operation");

        Assert.False(connected);
        Assert.Equal(new[] { "connect", "explicit connect", "operation", "disconnect" }, events);
    }

    /// <summary>
    /// 验证嵌套操作只在最外层结束时断开，并保留 FTP 操作原始异常。
    /// </summary>
    [Fact]
    public void OperationGate_DisconnectsAtOutermostExitAndPreservesActionException()
    {
        var events = new List<string>();
        var gate = new FtpOperationGate(false, () => events.Add("connect"), () =>
        {
            events.Add("disconnect");
            throw new InvalidOperationException("disconnect failed");
        });

        var exception = Assert.Throws<IOException>((Action)(() =>
        {
            using (gate.Enter())
            {
                using (gate.Enter())
                    events.Add("nested");
                Assert.DoesNotContain("disconnect", events);
                throw new IOException("operation failed");
            }
        }));

        Assert.Equal("operation failed", exception.Message);
        Assert.Equal(new[] { "connect", "nested", "disconnect" }, events);
    }

    /// <summary>
    /// 目录返回失败项时不能将整体传输报告为成功。
    /// </summary>
    [Fact]
    public void DirectoryResults_PropagateFailedTransfers()
    {
        Assert.True(FtpClient.TransfersSucceeded(new[] { new FluentFTP.FtpResult { IsSuccess = true }, new FluentFTP.FtpResult { IsSkipped = true } }));
        Assert.False(FtpClient.TransfersSucceeded(new[] { new FluentFTP.FtpResult { IsSuccess = true }, new FluentFTP.FtpResult() }));
    }

    /// <summary>
    /// 读取底层实例以验证配置映射，不连接网络。
    /// </summary>
    private static FluentFTP.FtpClient GetNativeClient(FtpClient client)
    {
        var field = typeof(FtpClient).GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<FluentFTP.FtpClient>(field.GetValue(client));
    }
}
