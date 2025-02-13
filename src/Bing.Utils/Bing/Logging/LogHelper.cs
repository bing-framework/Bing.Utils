using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bing.Logging;

/// <summary>
/// 日志操作辅助类
/// </summary>
public class LogHelper : BaseLogHelper, IDisposable
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
    private LogHelper() : base(NullLogger.Instance) { }

    #endregion

    #region Initialize(初始化)

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="loggerFactory">外部传入的 <see cref="ILoggerFactory"/>。</param>
    public void Initialize(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory), "日志工厂不能为空。");
        var defaultLogger = _loggerFactory.CreateLogger("Default");
        SetLogger(defaultLogger);
    }

    #endregion

    #region For(日志实例)

    /// <summary>
    /// 获取指定类型的日志记录器。
    /// </summary>
    /// <typeparam name="T">日志所属的类别类型</typeparam>
    /// <returns>泛型日志类实例</returns>
    public LogHelper<T> For<T>()
    {
        return (LogHelper<T>)_loggerCache.GetOrAdd(typeof(T), _ =>
        {
            var logger = _loggerFactory.CreateLogger<T>();
            return new LogHelper<T>(logger);
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
    public ILogger GetDefaultLogger() => Logger;

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _loggerCache.Clear();
            (_loggerFactory as IDisposable)?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// 日志操作辅助类
/// </summary>
/// <typeparam name="T">日志所属的类别类型</typeparam>
public class LogHelper<T> : BaseLogHelper
{
    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="LogHelper{T}"/>类型的实例
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public LogHelper(ILogger<T> logger) : base(logger)
    {
    }

    #endregion
}

/// <summary>
/// 日志操作辅助类，用于封装通用日志功能。
/// </summary>
public abstract class BaseLogHelper
{
    #region 字段

    /// <summary>
    /// 日志记录器
    /// </summary>
    protected ILogger Logger;

    /// <summary>
    /// 是否启用跟踪日志
    /// </summary>
    private bool _isTraceEnabled;

    /// <summary>
    /// 是否启用调试日志
    /// </summary>
    private bool _isDebugEnabled;

    /// <summary>
    /// 是否启用信息日志
    /// </summary>
    private bool _isInfoEnabled;

    /// <summary>
    /// 是否启用警告日志
    /// </summary>
    private bool _isWarningEnabled;

    /// <summary>
    /// 是否启用错误日志
    /// </summary>
    private bool _isErrorEnabled;

    /// <summary>
    /// 是否启用严重错误日志
    /// </summary>
    private bool _isCriticalEnabled;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="BaseLogHelper"/>类型的实例
    /// </summary>
    /// <param name="logger">日志记录器</param>
    protected BaseLogHelper(ILogger logger) => SetLogger(logger);

    #endregion

    #region 属性

    /// <summary>
    /// 日志记录的扩展钩子【之前】
    /// </summary>
    public Action<LogLevel, string, Exception, object[]> BeforeLog { get; set; }

    /// <summary>
    /// 日志记录的扩展钩子【之后】
    /// </summary>
    public Action<LogLevel, string, Exception, object[]> AfterLog { get; set; }

    /// <summary>
    /// 日志记录异常的扩展钩子
    /// </summary>
    public Action<Exception> OnLogException { get; set; }

    /// <summary>
    /// 日志抑制规则的扩展钩子
    /// </summary>
    public Func<LogLevel, string, bool> SuppressLog { get; set; } = (level, message) => false;

    /// <summary>
    /// 日志格式化
    /// </summary>
    public Func<string, object[], string> LogFormat { get; set; } = (message, args) =>
    {
        if (args == null || args.Length == 0)
            return message;
        return string.Format(message, args);
    };

    #endregion

    #region SetLogger(设置日志记录器)

    /// <summary>
    /// 设置日志记录器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <exception cref="ArgumentNullException"></exception>
    protected void SetLogger(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeLogLevels();
    }

    /// <summary>
    /// 初始化日志级别检查缓存
    /// </summary>
    private void InitializeLogLevels()
    {
        _isTraceEnabled = Logger.IsEnabled(LogLevel.Trace);
        _isDebugEnabled = Logger.IsEnabled(LogLevel.Debug);
        _isInfoEnabled = Logger.IsEnabled(LogLevel.Information);
        _isWarningEnabled = Logger.IsEnabled(LogLevel.Warning);
        _isErrorEnabled = Logger.IsEnabled(LogLevel.Error);
        _isCriticalEnabled = Logger.IsEnabled(LogLevel.Critical);
    }

    #endregion

    #region RefreshLogLevels(刷新日志级别)

    /// <summary>
    /// 刷新日志级别
    /// </summary>
    public void RefreshLogLevels() => InitializeLogLevels();

    #endregion

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
        if ((level == LogLevel.Trace && !_isTraceEnabled) ||
            (level == LogLevel.Debug && !_isDebugEnabled) ||
            (level == LogLevel.Information && !_isInfoEnabled) ||
            (level == LogLevel.Warning && !_isWarningEnabled) ||
            (level == LogLevel.Error && !_isErrorEnabled) ||
            (level == LogLevel.Critical && !_isCriticalEnabled))
            return;
        if (SuppressLog(level, message))
            return;
        try
        {
            BeforeLog?.Invoke(level, message, exception, args);
            Logger.Log(level, 0, exception, message, args);
            AfterLog?.Invoke(level, message, exception, args);
        }
        catch (Exception e)
        {
            OnLogException?.Invoke(e);
            Console.Error.WriteLine($"[{GetType().FullName}]Logging failed: {e.Message}");
        }
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
    /// <param name="exception">日志异常</param>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogError(Exception exception, string message, params object[] args) => Log(LogLevel.Error, message, exception, args);

    /// <summary>
    /// 写严重错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogCritical(string message, params object[] args) => Log(LogLevel.Critical, message, null, args);

    /// <summary>
    /// 写严重错误日志
    /// </summary>
    /// <param name="exception">日志异常</param>
    /// <param name="message">日志消息</param>
    /// <param name="args">日志参数</param>
    public void LogCritical(Exception exception, string message, params object[] args) => Log(LogLevel.Critical, message, null, args);

    #endregion
}