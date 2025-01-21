using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bing.Logging;

/// <summary>
/// 日志操作辅助类
/// </summary>
public class LogHelper : IDisposable
{
    #region 单例模式

    /// <summary>
    /// 懒加载实例
    /// </summary>
    private static readonly Lazy<LogHelper> InstanceLazy = new(() => new LogHelper());

    /// <summary>
    /// 实例
    /// </summary>
    public static LogHelper Instance => InstanceLazy.Value;

    #endregion

    #region 字段

    /// <summary>
    /// 日志工厂
    /// </summary>
    private ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    /// <summary>
    /// 默认日志记录器
    /// </summary>
    private ILogger _defaultLogger;

    /// <summary>
    /// 日志记录器缓存
    /// </summary>
    private readonly ConcurrentDictionary<Type, object> _loggerCache = new();

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="LogHelper"/>类型的实例
    /// </summary>
    private LogHelper() { }

    #endregion

    #region Initialize(初始化)

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="loggerFactory">外部传入的 <see cref="ILoggerFactory"/>。</param>
    public void Initialize(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory), "日志工厂不能为空。");
        _defaultLogger = _loggerFactory.CreateLogger("Default");
    }

    #endregion

    #region For(日志实例)

    /// <summary>
    /// 获取指定类型的日志记录器。
    /// </summary>
    /// <typeparam name="T">日志所属的类别类型</typeparam>
    /// <returns>泛型日志类实例</returns>
    public GenericLogger<T> For<T>()
    {
        return (GenericLogger<T>)_loggerCache.GetOrAdd(typeof(T), _ =>
        {
            var logger = _loggerFactory.CreateLogger<T>();
            return new GenericLogger<T>(logger);
        });
    }

    #endregion

    #region CreateLogger(创建日志记录器)

    /// <summary>
    /// 创建指定类型的日志记录器。
    /// </summary>
    /// <typeparam name="T">日志记录器的类别类型。</typeparam>
    /// <returns>指定类型的日志记录器实例。</returns>
    public ILogger<T> CreateLogger<T>() => _loggerFactory?.CreateLogger<T>();

    /// <summary>
    /// 创建指定分类名称的日志记录器。
    /// </summary>
    /// <param name="categoryName">日志记录器的分类名称。</param>
    /// <returns>指定分类名称的日志记录器实例。</returns>
    public ILogger CreateLogger(string categoryName) => _loggerFactory?.CreateLogger(categoryName);

    /// <summary>
    /// 创建指定类型的日志记录器。
    /// </summary>
    /// <param name="type">日志记录器的类型。</param>
    /// <returns>指定类型的日志记录器实例。</returns>
    public ILogger CreateLogger(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type), "日志类型不能为空。");
        return CreateLogger(type.FullName ?? type.Name);
    }

    /// <summary>
    /// 获取默认日志记录器。
    /// </summary>
    /// <returns>默认日志记录器。</returns>
    public ILogger GetDefaultLogger() => _defaultLogger;

    #endregion

    #region Log(日志)

    /// <summary>
    /// 写日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    /// <param name="args">日志参数</param>
    public void Log(LogLevel level, string message, Exception exception = null, params object[] args)
    {
        if (!_defaultLogger.IsEnabled(level))
            return;
        _defaultLogger.Log(level, exception, message, args);
    }

    /// <summary>
    /// 写跟踪日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogTrace(string message, params object[] args) => Log(LogLevel.Trace, message, null, args);

    /// <summary>
    /// 写调试日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogDebug(string message, params object[] args) => Log(LogLevel.Debug, message, null, args);

    /// <summary>
    /// 写信息日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogInformation(string message, params object[] args) => Log(LogLevel.Information, message, null, args);

    /// <summary>
    /// 写警告日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogWarning(string message, params object[] args) => Log(LogLevel.Warning, message, null, args);

    /// <summary>
    /// 写错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogError(string message, params object[] args) => Log(LogLevel.Error, message, null, args);

    /// <summary>
    /// 写错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">日志异常</param>
    /// <param name="args">日志参数</param>
    public void LogError(string message, Exception exception, params object[] args) => Log(LogLevel.Error, message, exception, args);

    /// <summary>
    /// 写严重错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogCritical(string message, params object[] args) => Log(LogLevel.Critical, message, null, args);

    /// <summary>
    /// 写严重错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">日志异常</param>
    /// <param name="args">日志参数</param>
    public void LogCritical(string message, Exception exception, params object[] args) => Log(LogLevel.Critical, message, null, args);

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            (_loggerFactory as IDisposable)?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// 泛型日志记录器
/// </summary>
/// <typeparam name="T">日志所属的类别类型</typeparam>
public class GenericLogger<T>
{
    /// <summary>
    /// 日志记录器
    /// </summary>
    private readonly ILogger<T> _logger;

    /// <summary>
    /// 初始化一个<see cref="GenericLogger{T}"/>类型的实例
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <exception cref="ArgumentNullException"></exception>
    public GenericLogger(ILogger<T> logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    #region Log(日志)

    /// <summary>
    /// 写日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常消息</param>
    /// <param name="args">日志参数</param>
    public void Log(LogLevel level, string message, Exception exception = null, params object[] args)
    {
        if (_logger.IsEnabled(level))
            _logger.Log(level, exception, message, args);
    }

    /// <summary>
    /// 写跟踪日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogTrace(string message, params object[] args) => Log(LogLevel.Trace, message, null, args);

    /// <summary>
    /// 写调试日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogDebug(string message, params object[] args) => Log(LogLevel.Debug, message, null, args);

    /// <summary>
    /// 写信息日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogInformation(string message, params object[] args) => Log(LogLevel.Information, message, null, args);

    /// <summary>
    /// 写警告日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogWarning(string message, params object[] args) => Log(LogLevel.Warning, message, null, args);

    /// <summary>
    /// 写错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogError(string message, params object[] args) => Log(LogLevel.Error, message, null, args);

    /// <summary>
    /// 写错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">日志异常</param>
    /// <param name="args">日志参数</param>
    public void LogError(string message, Exception exception, params object[] args) => Log(LogLevel.Error, message, exception, args);

    /// <summary>
    /// 写严重错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogCritical(string message, params object[] args) => Log(LogLevel.Critical, message, null, args);

    /// <summary>
    /// 写严重错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">日志异常</param>
    /// <param name="args">日志参数</param>
    public void LogCritical(string message, Exception exception, params object[] args) => Log(LogLevel.Critical, message, null, args);

    #endregion
}
