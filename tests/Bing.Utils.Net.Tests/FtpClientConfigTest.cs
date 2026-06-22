namespace Bing.Net.FTP;

/// <summary>
/// FtpClientConfig 单元测试
/// </summary>
public class FtpClientConfigTest
{
    #region 默认值验证

    /// <summary>
    /// 测试目的：验证 FtpClientConfig 默认端口为 21
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
    /// 测试目的：验证连接超时默认为 100000 毫秒
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
    /// 测试目的：验证读写超时默认为 300000 毫秒
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
    /// 测试目的：验证远程目录默认为 "/"
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
    /// 测试目的：验证字符集默认为 UTF-8
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
    /// 测试目的：验证默认不启用 SSL
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
    /// 测试目的：验证默认使用二进制模式
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
    /// 测试目的：验证默认使用被动模式
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
    /// 测试目的：验证默认 KeepAlive 为 true
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
    /// 测试目的：验证默认加密类型为 None
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
    /// 测试目的：验证配置属性可以被设置并读取
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
    /// 测试目的：验证未设置代理时 GetProxy 返回 null
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
    /// 测试目的：验证设置代理主机后 GetProxy 返回非 null
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
/// FtpClientException 单元测试
/// </summary>
public class FtpClientExceptionTest
{
    /// <summary>
    /// 测试目的：验证构造函数正确设置 Message
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
    /// 测试目的：验证传入 null 消息时 Message 为空字符串
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
    /// 测试目的：验证构造函数可以包装内部异常
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
    /// 测试目的：验证 FtpClientException 继承自 Exception
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
/// EncryptionType 枚举 单元测试
/// </summary>
public class EncryptionTypeTest
{
    /// <summary>
    /// 测试目的：验证枚举值的整数编码正确
    /// </summary>
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
    /// 测试目的：验证枚举包含三个值
    /// </summary>
    [Fact]
    public void EncryptionType_HasThreeValues()
    {
        // Assert
        Assert.Equal(3, Enum.GetValues(typeof(EncryptionType)).Length);
    }
}
