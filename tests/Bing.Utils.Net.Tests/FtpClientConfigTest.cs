namespace Bing.Net.FTP;

/// <summary>
/// 验证 FTP 客户端配置的默认值、属性赋值和代理创建行为。
/// </summary>
public class FtpClientConfigTest
{
    #region 默认值验证

    /// <summary>
    /// 验证默认控制连接端口为 21。
    /// </summary>
    [Fact]
    public void DefaultPort_Is21()
    {
        // Act
        var config = new FtpClientConfig();

        // Assert
        Assert.Equal(21, config.Port);
    }

    /// <summary>
    /// 验证默认连接超时为 100000 毫秒。
    /// </summary>
    [Fact]
    public void DefaultTimeout_Is100000()
    {
        // Act
        var config = new FtpClientConfig();

        // Assert
        Assert.Equal(100_000, config.Timeout);
    }

    /// <summary>
    /// 验证默认读写超时为 300000 毫秒。
    /// </summary>
    [Fact]
    public void DefaultReadWriteTimeout_Is300000()
    {
        // Act
        var config = new FtpClientConfig();

        // Assert
        Assert.Equal(300_000, config.ReadWriteTimeout);
    }

    /// <summary>
    /// 验证默认远程目录为根目录 `/`。
    /// </summary>
    [Fact]
    public void DefaultRemoteDirectory_IsRootSlash()
    {
        // Act
        var config = new FtpClientConfig();

        // Assert
        Assert.Equal("/", config.RemoteDirectory);
    }

    /// <summary>
    /// 验证默认字符集名称为 UTF-8。
    /// </summary>
    [Fact]
    public void DefaultCharsetName_IsUtf8()
    {
        // Act
        var config = new FtpClientConfig();

        // Assert
        Assert.Equal("UTF-8", config.CharsetName);
    }

    /// <summary>
    /// 验证默认未启用 SSL。
    /// </summary>
    [Fact]
    public void DefaultEnableSsl_IsFalse()
    {
        // Act
        var config = new FtpClientConfig();

        // Assert
        Assert.False(config.EnableSsl);
    }

    /// <summary>
    /// 验证默认使用二进制传输模式。
    /// </summary>
    [Fact]
    public void DefaultUseBinary_IsTrue()
    {
        // Act
        var config = new FtpClientConfig();

        // Assert
        Assert.True(config.UseBinary);
    }

    /// <summary>
    /// 验证默认使用被动连接模式。
    /// </summary>
    [Fact]
    public void DefaultUsePassive_IsTrue()
    {
        // Act
        var config = new FtpClientConfig();

        // Assert
        Assert.True(config.UsePassive);
    }

    /// <summary>
    /// 验证默认启用连接保持。
    /// </summary>
    [Fact]
    public void DefaultKeepAlive_IsTrue()
    {
        // Act
        var config = new FtpClientConfig();

        // Assert
        Assert.True(config.KeepAlive);
    }

    /// <summary>
    /// 验证默认加密类型为 <see cref="EncryptionType.None"/>。
    /// </summary>
    [Fact]
    public void DefaultEncryptionType_IsNone()
    {
        // Act
        var config = new FtpClientConfig();

        // Assert
        Assert.Equal(EncryptionType.None, config.EncryptionType);
    }

    #endregion

    #region 属性设置

    /// <summary>
    /// 验证主机、端口、凭据和加密模式可通过配置对象读回。
    /// </summary>
    [Fact]
    public void SetProperties_CanBeReadBack()
    {
        // Arrange & Act
        var config = new FtpClientConfig
        {
            Host = "192.168.1.1",
            Port = 2121,
            UserName = "admin",
            Password = "pass123",
            EncryptionType = EncryptionType.Explicit
        };

        // Assert
        Assert.Equal("192.168.1.1", config.Host);
        Assert.Equal(2121, config.Port);
        Assert.Equal("admin", config.UserName);
        Assert.Equal("pass123", config.Password);
        Assert.Equal(EncryptionType.Explicit, config.EncryptionType);
    }

    #endregion

    #region GetProxy

    /// <summary>
    /// 验证未配置代理主机时不创建代理对象。
    /// </summary>
    [Fact]
    public void GetProxy_WhenProxyHostIsEmpty_ReturnsNull()
    {
        // Arrange
        var config = new FtpClientConfig();

        // Act
        var proxy = config.GetProxy();

        // Assert
        Assert.Null(proxy);
    }

    /// <summary>
    /// 验证配置代理主机和端口后可创建代理对象。
    /// </summary>
    [Fact]
    public void GetProxy_WhenProxyHostSet_ReturnsNonNull()
    {
        // Arrange
        var config = new FtpClientConfig
        {
            ProxyHost = "proxy.example.com",
            ProxyPort = 8080
        };

        // Act
        var proxy = config.GetProxy();

        // Assert
        Assert.NotNull(proxy);
    }

    #endregion
}

/// <summary>
/// 验证 FTP 客户端异常的消息、内部异常和基类行为。
/// </summary>
public class FtpClientExceptionTest
{
    /// <summary>
    /// 验证异常构造函数保留传入的消息。
    /// </summary>
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Act
        var ex = new FtpClientException("FTP error occurred");

        // Assert
        Assert.Equal("FTP error occurred", ex.Message);
        Assert.Null(ex.InnerException);
    }

    /// <summary>
    /// 验证传入 null 消息时异常消息为空字符串。
    /// </summary>
    [Fact]
    public void Constructor_NullMessage_MessageIsEmpty()
    {
        // Act
        var ex = new FtpClientException(null);

        // Assert
        Assert.Equal("", ex.Message);
    }

    /// <summary>
    /// 验证异常构造函数保留传入的内部异常。
    /// </summary>
    [Fact]
    public void Constructor_WithInnerException_SetsInnerException()
    {
        // Arrange
        var innerEx = new InvalidOperationException("inner");

        // Act
        var ex = new FtpClientException("outer", innerEx);

        // Assert
        Assert.Equal("outer", ex.Message);
        Assert.Same(innerEx, ex.InnerException);
    }

    /// <summary>
    /// 验证 FTP 客户端异常可作为标准 <see cref="Exception"/> 使用。
    /// </summary>
    [Fact]
    public void FtpClientException_IsException()
    {
        // Act
        var ex = new FtpClientException("test");

        // Assert
        Assert.IsAssignableFrom<Exception>(ex);
    }
}

/// <summary>
/// 验证 <see cref="EncryptionType"/> 成员的数值和数量。
/// </summary>
public class EncryptionTypeTest
{
    /// <summary>
    /// 验证加密类型成员使用预期的整数值。
    /// </summary>
    /// <param name="type">要检查的加密类型。</param>
    /// <param name="expected">预期的枚举整数值。</param>
    [Theory]
    [InlineData(EncryptionType.None, 0)]
    [InlineData(EncryptionType.Implicit, 1)]
    [InlineData(EncryptionType.Explicit, 2)]
    public void EncryptionType_Values_AreCorrect(EncryptionType type, int expected)
    {
        // Assert
        Assert.Equal(expected, (int)type);
    }

    /// <summary>
    /// 验证加密类型当前包含三个成员。
    /// </summary>
    [Fact]
    public void EncryptionType_HasThreeValues()
    {
        // Assert
        Assert.Equal(3, Enum.GetValues(typeof(EncryptionType)).Length);
    }
}
