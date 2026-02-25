using Shouldly;
using System.Collections.Concurrent;
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
    /// <summary>
    /// 测试 - GetOperatingSystemName - 识别各种Windows版本
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36", "Windows 10")]
    [InlineData("Mozilla/5.0 (Windows NT 6.2; Win64; x64) AppleWebKit/537.36", "Windows 8")]
    [InlineData("Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36", "Windows 7")]
    [InlineData("Mozilla/5.0 (Windows NT 6.0; Win64; x64) AppleWebKit/537.36", "Windows Vista/Server 2008")]
    [InlineData("Mozilla/5.0 (Windows NT 5.2; Win64; x64) AppleWebKit/537.36", "Windows Server 2003")]
    [InlineData("Mozilla/5.0 (Windows NT 5.1; Win64; x64) AppleWebKit/537.36", "Windows XP")]
    [InlineData("Mozilla/5.0 (Windows NT 5.0; Win64; x64) AppleWebKit/537.36", "Windows 2000")]
    [InlineData("Mozilla/5.0 (Windows ME) AppleWebKit/537.36", "Windows ME")]
    public void GetOperatingSystemName_WindowsVersions_ReturnsCorrectName(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetOperatingSystemName(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result}");
    }
    /// <summary>
    /// 测试 - GetOperatingSystemName - 识别非Windows操作系统
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36", "Mac")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36", "Linux")]
    [InlineData("Mozilla/5.0 (X11; Ubuntu; Linux x86_64) AppleWebKit/537.36", "Linux")]
    [InlineData("Mozilla/5.0 (X11; Unix) AppleWebKit/537.36", "UNIX")]
    [InlineData("Mozilla/5.0 (X11; SunOs) AppleWebKit/537.36", "Solaris")]
    [InlineData("Mozilla/5.0 (X11; FreeBSD) AppleWebKit/537.36", "FreeBSD")]
    public void GetOperatingSystemName_NonWindowsOS_ReturnsCorrectName(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetOperatingSystemName(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result}");
    }
    /// <summary>
    /// 测试 - GetOperatingSystemName - 移动设备操作系统
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (Linux; Android 11; SM-G991B) AppleWebKit/537.36", "Linux")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 14_6 like Mac OS X) AppleWebKit/605.1.15", "Mac")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 14_6 like Mac OS X) AppleWebKit/605.1.15", "Mac")]
    public void GetOperatingSystemName_MobileOS_ReturnsCorrectName(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetOperatingSystemName(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"移动设备UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result}");
    }
    /// <summary>
    /// 测试 - GetOperatingSystemName - 不区分大小写匹配
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (linux; android 10) AppleWebKit/537.36", "Linux")]
    [InlineData("Mozilla/5.0 (LINUX; ANDROID 10) AppleWebKit/537.36", "Linux")]
    [InlineData("Mozilla/5.0 (mac) AppleWebKit/537.36", "Mac")]
    [InlineData("Mozilla/5.0 (MAC) AppleWebKit/537.36", "Mac")]
    public void GetOperatingSystemName_CaseInsensitive_ReturnsCorrectName(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetOperatingSystemName(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"大小写测试 - UserAgent: {userAgent}, 结果: {result}");
    }
    /// <summary>
    /// 测试 - GetOperatingSystemName - 未知操作系统返回默认值
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (Unknown OS) AppleWebKit/537.36")]
    [InlineData("Custom/1.0 (UnknownSystem)")]
    [InlineData("Bot/1.0")]
    [InlineData("RandomUserAgent/1.0")]
    public void GetOperatingSystemName_UnknownOS_ReturnsDefaultValue(string userAgent)
    {
        // Act
        var result = UserAgentHelper.GetOperatingSystemName(userAgent);
        // Assert
        result.ShouldBe("Other OperationSystem");
        Output.WriteLine($"未知系统测试 - UserAgent: {userAgent}, 结果: {result}");
    }
    /// <summary>
    /// 测试 - GetOperatingSystemName - 空UserAgent抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void GetOperatingSystemName_NullOrWhiteSpace_ThrowsArgumentException(string userAgent)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            UserAgentHelper.GetOperatingSystemName(userAgent));
        exception.ParamName.ShouldBe("userAgent");
        exception.Message.ShouldContain("用户代理字符串不能为空或空白字符串");
        Output.WriteLine($"空值测试 - 输入: '{userAgent}', 异常: {exception.Message}");
    }
    /// <summary>
    /// 测试 - GetOperatingSystemName - 修复MicroMessenger误识别问题
    /// </summary>
    [Fact]
    public void GetOperatingSystemName_MicroMessengerBug_FixedCorrectly()
    {
        // Arrange
        var problematicUserAgent = "Mozilla/5.0 (Linux; Android 11; SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.62 Mobile Safari/537.36 MicroMessenger/8.0.2";
        // Act
        var result = UserAgentHelper.GetOperatingSystemName(problematicUserAgent);
        // Assert
        result.ShouldBe("Linux", "包含MicroMessenger的Android UserAgent应该被识别为Linux，而不是Windows ME");
        result.ShouldNotBe("Windows ME", "不应该因为MicroMessenger中包含ME而被误识别为Windows ME");
        Output.WriteLine($"修复测试 - UserAgent: {problematicUserAgent}");
        Output.WriteLine($"识别结果: {result}");
        // 验证UserAgent确实包含ME和Linux
        problematicUserAgent.ShouldContain("ME", customMessage: "UserAgent应该包含ME（在MicroMessenger中）");
        problematicUserAgent.ShouldContain("Linux", customMessage: "UserAgent应该包含Linux");
        Output.WriteLine("✓ 确认UserAgent同时包含ME和Linux标识符");
        Output.WriteLine("✓ 优先级设置正确，Linux优先于ME被匹配");
    }
    /// <summary>
    /// 测试 - GetOperatingSystemName - Android设备识别
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (Linux; Android 11; SM-G991B) AppleWebKit/537.36", "Linux")]
    [InlineData("Mozilla/5.0 (Linux; Android 10; Mi 10) AppleWebKit/537.36", "Linux")]
    [InlineData("Mozilla/5.0 (Linux; Android 9; SAMSUNG SM-G973F) AppleWebKit/537.36", "Linux")]
    [InlineData("Mozilla/5.0 (Linux; U; Android 8.1.0; zh-cn; MI 8 Build/OPM1.171019.011) AppleWebKit/537.36", "Linux")]
    public void GetOperatingSystemName_AndroidDevices_ReturnsLinux(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetOperatingSystemName(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"Android设备测试 - UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result}");
    }
    /// <summary>
    /// 测试 - GetOperatingSystemName - iOS设备识别
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 14_6 like Mac OS X) AppleWebKit/605.1.15", "Mac")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 14_6 like Mac OS X) AppleWebKit/605.1.15", "Mac")]
    [InlineData("Mozilla/5.0 (iPod touch; CPU iPhone OS 13_7 like Mac OS X) AppleWebKit/605.1.15", "Mac")]
    public void GetOperatingSystemName_iOSDevices_ReturnsMac(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetOperatingSystemName(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"iOS设备测试 - UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result}");
    }
    /// <summary>
    /// 测试 - GetOperatingSystemName - 真实Windows ME识别
    /// </summary>
    [Theory]
    [InlineData("Mozilla/4.0 (compatible; MSIE 5.5; Windows ME)", "Windows ME")]
    [InlineData("Mozilla/4.0 (compatible; MSIE 6.0; Windows ME; .NET CLR 1.1.4322)", "Windows ME")]
    [InlineData("Mozilla/4.0 (compatible; MSIE 5.0; Win 9x 4.90)", "Windows ME")]
    public void GetOperatingSystemName_RealWindowsME_ReturnsWindowsME(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetOperatingSystemName(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"真实Windows ME测试 - UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result}");
    }
    /// <summary>
    /// 测试 - 操作系统字典优先级诊断
    /// </summary>
    [Fact]
    public void OperationSystemDict_Diagnostics_VerifyOrder()
    {
        // Arrange & Act
        var dict = UserAgentHelper.OperationSystemDict;
        var keys = dict.Keys.ToList();
        // 查找关键操作系统标识符的位置
        var linuxIndex = keys.IndexOf("Linux");
        var androidIndex = keys.IndexOf("Android");
        var meIndex = keys.IndexOf("ME");
        var macIndex = keys.IndexOf("Mac");
        // Assert
        linuxIndex.ShouldBeGreaterThanOrEqualTo(0, "Linux标识符应该存在于字典中");
        meIndex.ShouldBeGreaterThanOrEqualTo(0, "ME标识符应该存在于字典中");
        if (linuxIndex >= 0 && meIndex >= 0)
        {
            linuxIndex.ShouldBeLessThan(meIndex, "Linux应该在ME之前被匹配，以避免MicroMessenger误识别");
        }
        if (androidIndex >= 0 && linuxIndex >= 0)
        {
            androidIndex.ShouldBeLessThan(linuxIndex, "Android应该在Linux之前被匹配，提供更具体的识别");
        }
        Output.WriteLine("=== 操作系统字典顺序诊断 ===");
        Output.WriteLine($"Android 位置: {androidIndex}");
        Output.WriteLine($"Linux 位置: {linuxIndex}");
        Output.WriteLine($"Mac 位置: {macIndex}");
        Output.WriteLine($"ME 位置: {meIndex}");
        Output.WriteLine("\n前10个操作系统标识符:");
        for (int i = 0; i < Math.Min(10, keys.Count); i++)
        {
            Output.WriteLine($"  {i}: {keys[i]} -> {dict[keys[i]]}");
        }
    }
    /// <summary>
    /// 测试 - GetOperatingSystemName - 边界情况：包含ME但不是Windows ME
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (Linux; Android 11) AppleWebKit/537.36 MicroMessenger/8.0.2", "Linux")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 SomeAppWithMEInName/1.0", "Mac")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 TestMEApplication/2.0", "Linux")]
    public void GetOperatingSystemName_ContainsMEButNotWindowsME_ReturnsCorrectOS(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetOperatingSystemName(userAgent);
        // Assert
        result.ShouldBe(expected);
        result.ShouldNotBe("Windows ME", "包含ME但不是Windows ME的UserAgent不应该被误识别");
        Output.WriteLine($"包含ME的边界测试 - UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result} (期望: {expected})");
    }
    #endregion
    #region GetBrowserName 测试
    /// <summary>
    /// 测试 - GetBrowserName - 识别主流浏览器
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36", "Safari")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0", "Firefox")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.59", "Microsoft Edge")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Safari/605.1.15", "Safari")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.25 Safari/537.36 Core/1.70.3741.400 Opera/56.0.3051.104", "Opera")]
    public void GetBrowserName_MainstreamBrowsers_ReturnsCorrectName(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetBrowserName(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"主流浏览器测试 - UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result}");
    }
    /// <summary>
    /// 测试 - GetBrowserName - 识别IE浏览器各版本
    /// </summary>
    [Theory]
    [InlineData("Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)", "Internet Explorer 6.0")]
    [InlineData("Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0)", "Internet Explorer 7.0")]
    [InlineData("Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1)", "Internet Explorer 8.0")]
    [InlineData("Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1)", "Internet Explorer 9.0")]
    [InlineData("Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2)", "Internet Explorer 10.0")]
    public void GetBrowserName_InternetExplorerVersions_ReturnsCorrectName(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetBrowserName(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"IE版本测试 - UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result}");
    }
    /// <summary>
    /// 测试 - GetBrowserName - 识别国产浏览器
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.25 Safari/537.36 Core/1.70.3741.400 QQBrowser/10.5.3863.400", "QQ浏览器")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36 BIDUBrowser/8.6.0.3090", "百度浏览器")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36 LBBROWSER", "360安全浏览器")] // 测试部分匹配
    [InlineData("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36 360se", "360安全浏览器")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36 MetaSr 1.0", "搜狗高速浏览器")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Maxthon/5.2.7.5000 Chrome/55.0.2883.87 Safari/537.36", "遨游浏览器")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) GreenBrowser", "Green浏览器")]
    public void GetBrowserName_ChineseBrowsers_ReturnsCorrectName(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetBrowserName(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"国产浏览器测试 - UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result}");
    }
    /// <summary>
    /// 测试 - 移动端浏览器支持
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (Linux; Android 11; M2102J2SC Build/RP1A.200720.011; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/91.0.4472.114 Mobile Safari/537.36 MiuiBrowser/13.10.0-gn", "小米浏览器")]
    [InlineData("Mozilla/5.0 (Linux; Android 11; VOG-AL00 Build/HUAWEIVOG-AL00; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/91.0.4472.114 Mobile Safari/537.36 HuaweiBrowser/12.0.4.301", "华为浏览器")]
    [InlineData("Mozilla/5.0 (Linux; Android 11; V2055A Build/RP1A.200720.012; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/91.0.4472.114 Mobile Safari/537.36 VivoBrowser/9.2.0.0", "Vivo浏览器")]
    public void GetBrowserName_MobileBrowsers_ReturnsCorrectName(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetBrowserName(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"移动端浏览器测试 - UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result}");
    }
    /// <summary>
    /// 测试 - GetBrowserName - 识别现代浏览器
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.59", "Microsoft Edge")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Brave/1.26.74", "Brave")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Vivaldi/4.0.2312.41", "Vivaldi")]
    [InlineData("Mozilla/5.0 (Linux; Android 11; SAMSUNG SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/14.2 Chrome/87.0.4280.141 Mobile Safari/537.36", "三星浏览器")]
    [InlineData("Mozilla/5.0 (Linux; U; Android 11; zh-CN; MI 11 Build/RKQ1.200826.002) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 UCBrowser/13.4.0.1306 Mobile Safari/537.36", "UC浏览器")]
    public void GetBrowserName_ModernBrowsers_ReturnsCorrectName(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetBrowserName(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"现代浏览器测试 - UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result}");
    }
    /// <summary>
    /// 测试 - 动态添加浏览器支持
    /// </summary>
    [Fact]
    public void AddBrowserSupport_CustomBrowser_WorksCorrectly()
    {
        try
        {
            // Arrange
            var customIdentifier = "CustomBrowser";
            var customDisplayName = "自定义浏览器";
            var testUserAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) {customIdentifier}/1.0";
            // Act - 添加自定义浏览器支持
            UserAgentHelper.AddBrowserSupport(customIdentifier, customDisplayName);
            // Assert
            UserAgentHelper.IsBrowserSupported(customIdentifier).ShouldBeTrue();
            var result = UserAgentHelper.GetBrowserName(testUserAgent);
            result.ShouldBe(customDisplayName);
            Output.WriteLine($"动态添加浏览器测试 - 标识符: {customIdentifier}, 显示名: {customDisplayName}");
            Output.WriteLine($"测试UserAgent: {testUserAgent}");
            Output.WriteLine($"识别结果: {result}");
        }
        finally
        {
            // Cleanup
            UserAgentHelper.RemoveBrowserSupport("CustomBrowser");
        }
    }
    /// <summary>
    /// 测试 - 错误处理 - 无效参数
    /// </summary>
    [Theory]
    [InlineData(null, "浏览器标识符不能为空")]
    [InlineData("", "浏览器标识符不能为空")]
    [InlineData("   ", "浏览器标识符不能为空")]
    public void AddBrowserSupport_InvalidIdentifier_ThrowsArgumentException(string invalidIdentifier, string expectedMessage)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            UserAgentHelper.AddBrowserSupport(invalidIdentifier, "测试浏览器"));
        exception.ParamName.ShouldBe("identifier");
        exception.Message.ShouldContain(expectedMessage);
        Output.WriteLine($"无效标识符测试 - 输入: '{invalidIdentifier}', 异常: {exception.Message}");
    }
    /// <summary>
    /// 测试 - 错误处理 - 无效显示名称
    /// </summary>
    [Theory]
    [InlineData(null, "浏览器显示名称不能为空")]
    [InlineData("", "浏览器显示名称不能为空")]
    [InlineData("   ", "浏览器显示名称不能为空")]
    public void AddBrowserSupport_InvalidDisplayName_ThrowsArgumentException(string invalidDisplayName, string expectedMessage)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            UserAgentHelper.AddBrowserSupport("TestBrowser", invalidDisplayName));
        exception.ParamName.ShouldBe("displayName");
        exception.Message.ShouldContain(expectedMessage);
        Output.WriteLine($"无效显示名称测试 - 输入: '{invalidDisplayName}', 异常: {exception.Message}");
    }
    /// <summary>
    /// 测试 - 浏览器优先级
    /// </summary>
    [Fact]
    public void BrowserPriority_SpecificBeforeGeneric_WorksCorrectly()
    {
        try
        {
            // Arrange - 测试更具体的浏览器标识符应该优先于通用的
            var specificBrowser = "SpecificBrowser";
            var genericBrowser = "Generic";
            var testUserAgent = $"Mozilla/5.0 {specificBrowser}/1.0 {genericBrowser}/2.0";
            // Act - 先添加通用浏览器，再添加特定浏览器（高优先级）
            UserAgentHelper.AddBrowserSupport(genericBrowser, "通用浏览器");
            UserAgentHelper.AddBrowserSupport(specificBrowser, "特定浏览器", 1); // 高优先级
            // Assert
            var result = UserAgentHelper.GetBrowserName(testUserAgent);
            result.ShouldBe("特定浏览器"); // 应该匹配优先级更高的
            Output.WriteLine($"优先级测试 - UserAgent: {testUserAgent}");
            Output.WriteLine($"识别结果: {result} (期望: 特定浏览器)");
        }
        finally
        {
            // Cleanup
            UserAgentHelper.RemoveBrowserSupport("SpecificBrowser");
            UserAgentHelper.RemoveBrowserSupport("Generic");
        }
    }
    /// <summary>
    /// 测试 - GetBrowserName - 不区分大小写匹配
    /// </summary>
    [Theory]
    [InlineData("mozilla/5.0 firefox/89.0", "Firefox")]
    [InlineData("MOZILLA/5.0 CHROME/91.0", "Chrome")]
    [InlineData("mozilla/5.0 safari/605.1", "Safari")]
    [InlineData("mozilla/5.0 opera/76.0", "Opera")]
    public void GetBrowserName_CaseInsensitive_ReturnsCorrectName(string userAgent, string expected)
    {
        // Act
        var result = UserAgentHelper.GetBrowserName(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"大小写测试 - UserAgent: {userAgent}, 结果: {result}");
    }
    /// <summary>
    /// 测试 - GetBrowserName - 浏览器优先级（先匹配的优先）
    /// </summary>
    [Fact]
    public void GetBrowserName_BrowserPriority_ReturnsFirstMatch()
    {
        // Arrange - QQ浏览器基于Chrome，但QQBrowser在字典中排在Chrome前面
        var userAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.25 Safari/537.36 Core/1.70.3741.400 QQBrowser/10.5.3863.400";
        // Act
        var result = UserAgentHelper.GetBrowserName(userAgent);
        // Assert
        result.ShouldBe("QQ浏览器"); // 应该识别为QQ浏览器而不是Chrome
        Output.WriteLine($"优先级测试 - UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result}");
    }
    /// <summary>
    /// 测试 - GetBrowserName - 未知浏览器返回默认值
    /// </summary>
    [Theory]
    [InlineData("Custom/1.0 (Unknown Browser)")]
    [InlineData("Bot/1.0")]
    [InlineData("Crawler/2.0")]
    [InlineData("UnknownUserAgent/1.0")]
    public void GetBrowserName_UnknownBrowser_ReturnsDefaultValue(string userAgent)
    {
        // Act
        var result = UserAgentHelper.GetBrowserName(userAgent);
        // Assert
        result.ShouldBe("Other Browser");
        Output.WriteLine($"未知浏览器测试 - UserAgent: {userAgent}, 结果: {result}");
    }
    /// <summary>
    /// 测试 - GetBrowserName - 空UserAgent抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void GetBrowserName_NullOrWhiteSpace_ThrowsArgumentException(string userAgent)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            UserAgentHelper.GetBrowserName(userAgent));
        exception.ParamName.ShouldBe("userAgent");
        exception.Message.ShouldContain("用户代理字符串不能为空或空白字符串");
        Output.WriteLine($"空值测试 - 输入: '{userAgent}', 异常: {exception.Message}");
    }
    #endregion
    #region IsWechatBrowser 测试
    /// <summary>
    /// 测试 - IsWechatBrowser - 识别微信浏览器
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (Linux; Android 10; Mi 10) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.62 Mobile Safari/537.36 MicroMessenger/7.0.15.1680")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/8.0.2(0x18000235) NetType/WIFI Language/zh_CN")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.116 Safari/537.36 QBCore/4.0.1316.400 QQBrowser/9.0.2524.400 Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2875.116 Safari/537.36 NetType/WIFI MicroMessenger/6.5.2.501 WindowsWechat")]
    public void IsWechatBrowser_WechatUserAgents_ReturnsTrue(string userAgent)
    {
        // Act
        var result = UserAgentHelper.IsWechatBrowser(userAgent);
        // Assert
        result.ShouldBeTrue();
        Output.WriteLine($"微信浏览器测试 - UserAgent: {userAgent}");
        Output.WriteLine($"识别结果: {result}");
    }
    /// <summary>
    /// 测试 - IsWechatBrowser - 不区分大小写匹配
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 micromessenger/7.0")]
    [InlineData("Mozilla/5.0 MICROMESSENGER/7.0")]
    [InlineData("Mozilla/5.0 MicroMessenger/7.0")]
    [InlineData("Mozilla/5.0 microMESSENGER/7.0")]
    public void IsWechatBrowser_CaseInsensitive_ReturnsTrue(string userAgent)
    {
        // Act
        var result = UserAgentHelper.IsWechatBrowser(userAgent);
        // Assert
        result.ShouldBeTrue();
        Output.WriteLine($"大小写测试 - UserAgent: {userAgent}, 结果: {result}");
    }
    /// <summary>
    /// 测试 - IsWechatBrowser - 非微信浏览器返回false
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/91.0.4472.124")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 14_6 like Mac OS X) AppleWebKit/605.1.15 Safari/604.1")]
    [InlineData("Mozilla/5.0 (Linux; Android 11; SM-G991B) AppleWebKit/537.36 Chrome/91.0.4472.120 Mobile Safari/537.36")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 QQBrowser/10.5.3863.400")]
    [InlineData("MicroMessage/1.0")] // 相似但不是MicroMessenger
    [InlineData("Messenger/1.0")] // 包含Messenger但不是MicroMessenger
    public void IsWechatBrowser_NonWechatBrowsers_ReturnsFalse(string userAgent)
    {
        // Act
        var result = UserAgentHelper.IsWechatBrowser(userAgent);
        // Assert
        result.ShouldBeFalse();
        Output.WriteLine($"非微信浏览器测试 - UserAgent: {userAgent}, 结果: {result}");
    }
    /// <summary>
    /// 测试 - IsWechatBrowser - 部分匹配测试
    /// </summary>
    [Theory]
    [InlineData("MicroMessenger", true)]
    [InlineData("PrefixMicroMessengerSuffix", true)]
    [InlineData("MicroMessen", false)] // 不完整匹配
    [InlineData("Messenger", false)] // 只有部分字符
    public void IsWechatBrowser_PartialMatching_ReturnsExpected(string userAgent, bool expected)
    {
        // Act
        var result = UserAgentHelper.IsWechatBrowser(userAgent);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"部分匹配测试 - UserAgent: {userAgent}, 期望: {expected}, 结果: {result}");
    }
    /// <summary>
    /// 测试 - IsWechatBrowser - 空UserAgent抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void IsWechatBrowser_NullOrWhiteSpace_ThrowsArgumentException(string userAgent)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            UserAgentHelper.IsWechatBrowser(userAgent));
        exception.ParamName.ShouldBe("userAgent");
        exception.Message.ShouldContain("用户代理字符串不能为空或空白字符串");
        Output.WriteLine($"空值测试 - 输入: '{userAgent}', 异常: {exception.Message}");
    }
    #endregion
    #region 字典配置测试
    /// <summary>
    /// 测试 - OperationSystemDict - 字典不为空且包含预期项
    /// </summary>
    [Fact]
    public void OperationSystemDict_ContainsExpectedEntries()
    {
        // Arrange & Act
        var dict = UserAgentHelper.OperationSystemDict;
        // Assert
        dict.ShouldNotBeNull();
        dict.ShouldNotBeEmpty();
        // 验证一些关键条目
        dict.ShouldContainKey("NT 10.0");
        dict["NT 10.0"].ShouldBe("Windows 10");
        dict.ShouldContainKey("Mac");
        dict["Mac"].ShouldBe("Mac");
        dict.ShouldContainKey("Linux");
        dict["Linux"].ShouldBe("Linux");
        Output.WriteLine($"操作系统字典包含 {dict.Count} 个条目:");
        foreach (var kvp in dict)
        {
            Output.WriteLine($"  {kvp.Key} -> {kvp.Value}");
        }
    }
    /// <summary>
    /// 测试 - BrowserDict - 字典不为空且包含预期项
    /// </summary>
    [Fact]
    public void BrowserDict_ContainsExpectedEntries()
    {
        // Arrange & Act
        var dict = UserAgentHelper.BrowserDict;
        // Assert
        dict.ShouldNotBeNull();
        dict.ShouldNotBeEmpty();
        // 验证一些关键条目
        dict.ShouldContainKey("Chrome");
        dict["Chrome"].ShouldBe("Chrome");
        dict.ShouldContainKey("Firefox");
        dict["Firefox"].ShouldBe("Firefox");
        dict.ShouldContainKey("QQBrowser");
        dict["QQBrowser"].ShouldBe("QQ浏览器");
        Output.WriteLine($"浏览器字典包含 {dict.Count} 个条目:");
        foreach (var kvp in dict)
        {
            Output.WriteLine($"  {kvp.Key} -> {kvp.Value}");
        }
    }
    /// <summary>
    /// 测试 - 字典可配置性
    /// </summary>
    [Fact]
    public void Dictionaries_AreConfigurable()
    {
        try
        {
            // Arrange
            var originalOSCount = UserAgentHelper.OperationSystemDict.Count;
            var originalBrowserCount = UserAgentHelper.BrowserDict.Count;
            // Act - 添加自定义条目
            UserAgentHelper.OperationSystemDict["TestOS"] = "Test Operating System";
            UserAgentHelper.BrowserDict["TestBrowser"] = "Test Browser";
            // Assert
            UserAgentHelper.OperationSystemDict.Count.ShouldBe(originalOSCount + 1);
            UserAgentHelper.BrowserDict.Count.ShouldBe(originalBrowserCount + 1);
            UserAgentHelper.OperationSystemDict["TestOS"].ShouldBe("Test Operating System");
            UserAgentHelper.BrowserDict["TestBrowser"].ShouldBe("Test Browser");
            // 测试新添加的条目是否生效
            var testOSResult = UserAgentHelper.GetOperatingSystemName("Mozilla/5.0 (TestOS) AppleWebKit/537.36");
            var testBrowserResult = UserAgentHelper.GetBrowserName("Mozilla/5.0 TestBrowser/1.0");
            testOSResult.ShouldBe("Test Operating System");
            testBrowserResult.ShouldBe("Test Browser");
            Output.WriteLine("字典可配置性测试通过");
        }
        finally
        {
            // Cleanup - 移除测试条目
            UserAgentHelper.OperationSystemDict.Remove("TestOS");
            UserAgentHelper.BrowserDict.Remove("TestBrowser");
        }
    }
    #endregion
    #region 性能测试
    /// <summary>
    /// 测试 - 性能测试 - 大量调用性能
    /// </summary>
    [Fact]
    public void PerformanceTest_MultipleOperations_CompletesQuickly()
    {
        // Arrange
        var userAgents = new[]
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 14_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Mobile/15E148 Safari/604.1",
            "Mozilla/5.0 (Linux; Android 11; SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.120 Mobile Safari/537.36 MicroMessenger/8.0.2",
            "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.25 Safari/537.36 Core/1.70.3741.400 QQBrowser/10.5.3863.400",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36"
        };
        var sw = System.Diagnostics.Stopwatch.StartNew();
        // Act - 执行大量操作
        for (int i = 0; i < 1000; i++)
        {
            foreach (var userAgent in userAgents)
            {
                UserAgentHelper.GetOperatingSystemName(userAgent);
                UserAgentHelper.GetBrowserName(userAgent);
                UserAgentHelper.IsWechatBrowser(userAgent);
            }
        }
        sw.Stop();
        // Assert
        sw.ElapsedMilliseconds.ShouldBeLessThan(1000, "15000次操作应该在1秒内完成");
        Output.WriteLine($"性能测试: 15000次操作耗时 {sw.ElapsedMilliseconds}ms");
        Output.WriteLine($"平均每次操作耗时: {(double)sw.ElapsedMilliseconds / 15000:F4}ms");
    }
    #endregion
    #region 边界条件和异常处理测试
    /// <summary>
    /// 测试 - 边界条件 - 极长UserAgent字符串
    /// </summary>
    [Fact]
    public void BoundaryTest_VeryLongUserAgent_HandlesCorrectly()
    {
        // Arrange - 创建一个很长的UserAgent字符串
        var longUserAgent = "Mozilla/5.0 " + new string('A', 10000) + " Chrome/91.0.4472.124";
        // Act & Assert - 应该不抛出异常
        Should.NotThrow(() =>
        {
            var os = UserAgentHelper.GetOperatingSystemName(longUserAgent);
            var browser = UserAgentHelper.GetBrowserName(longUserAgent);
            var isWechat = UserAgentHelper.IsWechatBrowser(longUserAgent);
            Output.WriteLine($"极长UserAgent测试 - 长度: {longUserAgent.Length}");
            Output.WriteLine($"OS: {os}, Browser: {browser}, IsWechat: {isWechat}");
        });
    }
    /// <summary>
    /// 测试 - 边界条件 - 特殊字符UserAgent
    /// </summary>
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0) <script>alert('xss')</script> Chrome/91.0")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0) \"quotes\" Chrome/91.0")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0) 中文字符 Chrome/91.0")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0) !@#$%^&*() Chrome/91.0")]
    [InlineData("Mozilla/5.0\t(Windows\nNT\r10.0)\tChrome/91.0")]
    public void BoundaryTest_SpecialCharacters_HandlesCorrectly(string userAgent)
    {
        // Act & Assert - 应该不抛出异常
        Should.NotThrow(() =>
        {
            var os = UserAgentHelper.GetOperatingSystemName(userAgent);
            var browser = UserAgentHelper.GetBrowserName(userAgent);
            var isWechat = UserAgentHelper.IsWechatBrowser(userAgent);
            Output.WriteLine($"特殊字符测试 - UserAgent: {userAgent}");
            Output.WriteLine($"OS: {os}, Browser: {browser}, IsWechat: {isWechat}");
        });
    }
    /// <summary>
    /// 测试 - 线程安全性
    /// </summary>
    [Fact]
    public async Task ThreadSafety_ConcurrentAccess_WorksCorrectly()
    {
        // Arrange
        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/91.0.4472.124 Safari/537.36";
        var tasks = new List<Task>();
        var results = new ConcurrentBag<(string OS, string Browser, bool IsWechat)>();
        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var os = UserAgentHelper.GetOperatingSystemName(userAgent);
                var browser = UserAgentHelper.GetBrowserName(userAgent);
                var isWechat = UserAgentHelper.IsWechatBrowser(userAgent);
                results.Add((os, browser, isWechat));
            }));
        }
        await Task.WhenAll(tasks);
        // Assert
        results.Count.ShouldBe(100);
        // 所有结果应该一致
        var distinctResults = results.Distinct().ToList();
        distinctResults.Count.ShouldBe(1, "所有并发调用的结果应该一致");
        var result = distinctResults.First();
        result.OS.ShouldBe("Windows 10");
        result.Browser.ShouldBe("Safari");
        result.IsWechat.ShouldBeFalse();
        Output.WriteLine($"线程安全测试完成 - 100个并发调用，结果一致: {result}");
    }
    #endregion
    #region 集成测试
    /// <summary>
    /// 测试 - 集成测试 - 真实UserAgent字符串
    /// </summary>
    [Theory]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
        "Windows 10", "Safari", false)]
    [InlineData(
        "Mozilla/5.0 (iPhone; CPU iPhone OS 14_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Mobile/15E148 Safari/604.1",
        "Mac", "Safari", false)]
    [InlineData(
        "Mozilla/5.0 (Linux; Android 11; SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.62 Mobile Safari/537.36 MicroMessenger/8.0.2",
        "Linux", "Safari", true)]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.25 Safari/537.36 Core/1.70.3741.400 QQBrowser/10.5.3863.400",
        "Windows 10", "QQ浏览器", false)]
    [InlineData(
        "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0)",
        "Windows 7", "Internet Explorer 8.0", false)]
    public void IntegrationTest_RealUserAgents_ReturnsExpectedResults(
        string userAgent, string expectedOS, string expectedBrowser, bool expectedIsWechat)
    {
        // Act
        var actualOS = UserAgentHelper.GetOperatingSystemName(userAgent);
        var actualBrowser = UserAgentHelper.GetBrowserName(userAgent);
        var actualIsWechat = UserAgentHelper.IsWechatBrowser(userAgent);
        // Assert
        actualOS.ShouldBe(expectedOS);
        actualBrowser.ShouldBe(expectedBrowser);
        actualIsWechat.ShouldBe(expectedIsWechat);
        Output.WriteLine("=== 集成测试结果 ===");
        Output.WriteLine($"UserAgent: {userAgent}");
        Output.WriteLine($"操作系统: {actualOS} (期望: {expectedOS})");
        Output.WriteLine($"浏览器: {actualBrowser} (期望: {expectedBrowser})");
        Output.WriteLine($"是否微信: {actualIsWechat} (期望: {expectedIsWechat})");
    }
    #endregion
}
