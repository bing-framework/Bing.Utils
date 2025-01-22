using Microsoft.Extensions.Logging;
using Moq;

namespace Bing.Logging;

/// <summary>
/// 日志操作辅助类 测试
/// </summary>
public class LogHelperTest
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly LogHelper _logHelper;

    /// <summary>
    /// 测试初始化
    /// </summary>
    public LogHelperTest()
    {
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger>();
        _mockLogger
            .Setup(logger => logger.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);
        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _logHelper = LogHelper.Instance;
        _logHelper.Initialize(_mockLoggerFactory.Object);
    }

    /// <summary>
    /// 测试初始化方法是否正确设置日志工厂
    /// </summary>
    [Fact]
    public void Initialize_Should_SetLoggerFactory()
    {
        // Arrange
        var newMockLoggerFactory = new Mock<ILoggerFactory>();
        newMockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        // Act
        _logHelper.Initialize(newMockLoggerFactory.Object);

        // Assert
        Assert.NotNull(_logHelper.CreateLogger<LogHelperTest>());
    }

    /// <summary>
    /// 测试 For 方法是否返回 LogHelper 泛型实例
    /// </summary>
    [Fact]
    public void For_Should_ReturnLogHelperOfTInstance()
    {
        // Act
        var logger = _logHelper.For<LogHelperTest>();

        // Assert
        Assert.NotNull(logger);
        Assert.IsType<LogHelper<LogHelperTest>>(logger);
    }

    /// <summary>
    /// 测试创建日志记录器方法
    /// </summary>
    [Fact]
    public void CreateLogger_Should_Return_Logger()
    {
        // Act
        var logger = _logHelper.CreateLogger("TestCategory");

        // Assert
        Assert.NotNull(logger);
        _mockLoggerFactory.Verify(factory => factory.CreateLogger("TestCategory"), Times.Once);
    }

    /// <summary>
    /// 测试释放资源后缓存是否被清空
    /// </summary>
    [Fact]
    public void Dispose_Should_ClearCache()
    {
        // Arrange
        _logHelper.For<LogHelperTest>(); // Add a cache entry

        // Act
        _logHelper.Dispose();

        // Assert
        var cacheField = typeof(LogHelper).GetField("_loggerCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cache = cacheField?.GetValue(_logHelper) as System.Collections.Concurrent.ConcurrentDictionary<Type, object>;
        Assert.NotNull(cache);
        Assert.Empty(cache);
    }

    /// <summary>
    /// 测试 LogInformation 方法是否调用 ILogger 的 LogInformation 方法
    /// </summary>
    [Fact]
    public void LogInformation_Should_Invoke_ILogger_LogInformation()
    {
        // Arrange
        var message = "Information log message";

        // Act
        _logHelper.LogInformation(message);

        // Assert
        _mockLogger.Verify(logger => logger.IsEnabled(LogLevel.Information), Times.Once);
        _mockLogger.VerifyLog(logger => logger.LogInformation(
                (Exception)null, // 没有异常
                It.Is<string>(x => x == "Information log message")),
            Times.Once); // 确保调用了一次
    }

    /// <summary>
    /// 测试 LogError 方法是否传递异常
    /// </summary>
    [Fact]
    public void LogError_Should_Invoke_ILogger_Log_With_Exception()
    {
        // Arrange
        var message = "Error log message";
        var exception = new Exception("Test exception");

        // Act
        _logHelper.LogError(message, exception);

        // Assert
        _mockLogger.VerifyLog(logger => logger.LogError(
                exception, // 验证异常
                It.Is<string>(x => x == "Error log message")),
            Times.Once);
    }

    /// <summary>
    /// 测试日志级别禁用时日志方法不会被调用
    /// </summary>
    [Fact]
    public void Log_Should_Not_Invoke_Logger_When_Level_Disabled()
    {
        // Arrange
        _mockLogger
            .Setup(logger => logger.IsEnabled(LogLevel.Debug))
            .Returns(false); // 禁用 Debug 级别

        // Act
        _logHelper.RefreshLogLevels();
        _logHelper.LogDebug("Debug log message");

        // Assert
        _mockLogger.VerifyLog(logger => logger.LogDebug(
            It.IsAny<EventId>(),
            It.IsAny<Exception>(),
            It.IsAny<string>()), Times.Never); // 确保未调用日志
    }

    /// <summary>
    /// 测试 BeforeLog 和 AfterLog 钩子是否被调用
    /// </summary>
    [Fact]
    public void Log_Should_Invoke_BeforeLog_And_AfterLog()
    {
        // Arrange
        var beforeLogCalled = false;
        var afterLogCalled = false;

        _logHelper.BeforeLog = (level, message, ex, args) => { beforeLogCalled = true; };
        _logHelper.AfterLog = (level, message, ex, args) => { afterLogCalled = true; };

        // Act
        _logHelper.Log(LogLevel.Information, "Test message");

        // Assert
        Assert.True(beforeLogCalled, "BeforeLog should be called.");
        Assert.True(afterLogCalled, "AfterLog should be called.");
    }
}