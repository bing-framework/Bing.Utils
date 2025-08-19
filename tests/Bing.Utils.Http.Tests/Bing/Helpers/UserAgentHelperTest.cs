namespace Bing.Helpers;

/// <summary>
/// 用户代理帮助类 测试
/// </summary>
[Trait("Bing.Helpers", "UserAgent")]
public class UserAgentHelperTest : TestBase
{
    /// <inheritdoc />
    public UserAgentHelperTest(ITestOutputHelper output) : base(output)
    {
    }

    #region GetOperatingSystemName 测试

    /// 测试 - GetOperatingSystemName - 识别Windows10操作系统
    [Fact]
    public void GetOperatingSystemName_ShouldReturnWindows10_WhenUserAgentContainsNT10()
    {
        // Arrange
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

        // Act
        var result = UserAgentHelper.GetOperatingSystemName(userAgent);

        // Assert
        result.ShouldBe("Windows 10");
    }

    /// 测试 - GetOperatingSystemName - 识别Mac操作系统
    [Fact]
    public void GetOperatingSystemName_ShouldReturnMac_WhenUserAgentContainsMac()
    {
        // Arrange
        const string userAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36";

        // Act
        var result = UserAgentHelper.GetOperatingSystemName(userAgent);

        // Assert
        result.ShouldBe("Mac");
    }

    /// 测试 - GetOperatingSystemName - 不区分大小写匹配
    [Fact]
    public void GetOperatingSystemName_ShouldBeCaseInsensitive_WhenMatching()
    {
        // Arrange
        const string userAgent = "Mozilla/5.0 (linux; android 10) AppleWebKit/537.36";

        // Act
        var result = UserAgentHelper.GetOperatingSystemName(userAgent);

        // Assert
        result.ShouldBe("Linux");
    }

    /// 测试 - GetOperatingSystemName - 未知操作系统返回默认值
    [Fact]
    public void GetOperatingSystemName_ShouldReturnOtherOperationSystem_WhenNoMatch()
    {
        // Arrange
        const string userAgent = "Mozilla/5.0 (Unknown OS) AppleWebKit/537.36";

        // Act
        var result = UserAgentHelper.GetOperatingSystemName(userAgent);

        // Assert
        result.ShouldBe("Other OperationSystem");
    }

    /// 测试 - GetOperatingSystemName - 空UserAgent抛出异常
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetOperatingSystemName_ShouldThrowArgumentException_WhenUserAgentIsNullOrWhiteSpace(string userAgent)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            UserAgentHelper.GetOperatingSystemName(userAgent));
        exception.ParamName.ShouldBe("userAgent");
        exception.Message.ShouldContain("用户代理字符串不能为空或空白字符串");
    }

    #endregion

    #region GetBrowserName 测试

    /// 测试 - GetBrowserName - 识别Chrome浏览器
    [Fact]
    public void GetBrowserName_ShouldReturnChrome_WhenUserAgentContainsChrome()
    {
        // Arrange
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";

        // Act
        var result = UserAgentHelper.GetBrowserName(userAgent);

        // Assert
        result.ShouldBe("Chrome");
    }

    /// 测试 - GetBrowserName - 识别QQ浏览器
    [Fact]
    public void GetBrowserName_ShouldReturnQQBrowser_WhenUserAgentContainsQQBrowser()
    {
        // Arrange
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.25 Safari/537.36 Core/1.70.3741.400 QQBrowser/10.5.3863.400";

        // Act
        var result = UserAgentHelper.GetBrowserName(userAgent);

        // Assert
        result.ShouldBe("QQ浏览器");
    }

    /// 测试 - GetBrowserName - 不区分大小写匹配
    [Fact]
    public void GetBrowserName_ShouldBeCaseInsensitive_WhenMatching()
    {
        // Arrange
        const string userAgent = "mozilla/5.0 firefox/89.0";

        // Act
        var result = UserAgentHelper.GetBrowserName(userAgent);

        // Assert
        result.ShouldBe("Firefox");
    }

    /// 测试 - GetBrowserName - 未知浏览器返回默认值
    [Fact]
    public void GetBrowserName_ShouldReturnOtherBrowser_WhenNoMatch()
    {
        // Arrange
        const string userAgent = "Custom/1.0 (Unknown Browser)";

        // Act
        var result = UserAgentHelper.GetBrowserName(userAgent);

        // Assert
        result.ShouldBe("Other Browser");
    }

    /// 测试 - GetBrowserName - 空UserAgent抛出异常
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetBrowserName_ShouldThrowArgumentException_WhenUserAgentIsNullOrWhiteSpace(string userAgent)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            UserAgentHelper.GetBrowserName(userAgent));
        exception.ParamName.ShouldBe("userAgent");
        exception.Message.ShouldContain("用户代理字符串不能为空或空白字符串");
    }

    #endregion

    #region IsWechatBrowser 测试

    /// 测试 - IsWechatBrowser - 识别微信浏览器
    [Fact]
    public void IsWechatBrowser_ShouldReturnTrue_WhenUserAgentContainsMicroMessenger()
    {
        // Arrange
        const string userAgent = "Mozilla/5.0 (Linux; Android 10; Mi 10) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.62 Mobile Safari/537.36 MicroMessenger/7.0.15.1680";

        // Act
        var result = UserAgentHelper.IsWechatBrowser(userAgent);

        // Assert
        result.ShouldBeTrue();
    }

    /// 测试 - IsWechatBrowser - 不区分大小写匹配
    [Fact]
    public void IsWechatBrowser_ShouldBeCaseInsensitive_WhenMatching()
    {
        // Arrange
        const string userAgent = "Mozilla/5.0 micromessenger/7.0";

        // Act
        var result = UserAgentHelper.IsWechatBrowser(userAgent);

        // Assert
        result.ShouldBeTrue();
    }

    /// 测试 - IsWechatBrowser - 非微信浏览器返回false
    [Fact]
    public void IsWechatBrowser_ShouldReturnFalse_WhenNotWechatBrowser()
    {
        // Arrange
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/91.0.4472.124";

        // Act
        var result = UserAgentHelper.IsWechatBrowser(userAgent);

        // Assert
        result.ShouldBeFalse();
    }

    /// 测试 - IsWechatBrowser - 空UserAgent抛出异常
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsWechatBrowser_ShouldThrowArgumentException_WhenUserAgentIsNullOrWhiteSpace(string userAgent)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            UserAgentHelper.IsWechatBrowser(userAgent));
        exception.ParamName.ShouldBe("userAgent");
        exception.Message.ShouldContain("用户代理字符串不能为空或空白字符串");
    }

    #endregion
}