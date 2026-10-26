using System.IO;
using System.Net.Sockets;
using System.Threading;
using FluentFTP;

namespace Bing.Net.FTP;

public partial class FtpClient
{
    /// <summary>
    /// 判断目录传输是否全部成功或按规则跳过，失败项不能被解释为整体成功。
    /// </summary>
    internal static bool TransfersSucceeded(IEnumerable<FtpResult> results)
        => results != null && results.All(result => result != null && !result.IsFailed && (result.IsSuccess || result.IsSkipped));

    /// <summary>
    /// 保存本实例的配置兼容模式。
    /// </summary>
    private readonly FtpConfigurationMode _configurationMode;
    /// <summary>
    /// 完整模式的重入操作门闩；历史模式不使用。
    /// </summary>
    private readonly FtpOperationGate _operationGate;
    /// <summary>
    /// 保存创建时固定的本地绝对目录。
    /// </summary>
    private readonly string _localDirectory;
    /// <summary>
    /// 保存当前远程相对路径基准。
    /// </summary>
    private string _remoteDirectory;

    /// <summary>
    /// 根据配置和兼容模式初始化同步 FTP 客户端。
    /// </summary>
    /// <param name="config">FTP 客户端配置。</param>
    /// <param name="mode">配置兼容模式。</param>
    public FtpClient(FtpClientConfig config, FtpConfigurationMode mode)
    {
        if (config == null && mode == FtpConfigurationMode.Complete)
            throw new ArgumentNullException(nameof(config));
        if (!Enum.IsDefined(typeof(FtpConfigurationMode), mode))
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "不支持的 FTP 配置模式。");

        _configurationMode = mode;
        _config = mode == FtpConfigurationMode.Complete ? config.CreateSnapshot() : config;
        if (mode == FtpConfigurationMode.Complete)
        {
            ValidateCompleteConfiguration(_config);
            _remoteDirectory = GetRemoteDirectoryRoot(_config.RemoteDirectory);
            var localDirectory = string.IsNullOrEmpty(_config.LocalDirectory)
                ? Environment.CurrentDirectory
                : _config.LocalDirectory;
            _localDirectory = Path.GetFullPath(localDirectory);
        }

        var encryptionMode = ToNativeEncryptionMode(_config.EncryptionType, _config.EnableSsl, mode);
        _client = CreateNativeClient(_config, mode);
        _client.Config.EncryptionMode = encryptionMode;
        _client.Config.RetryAttempts = RETRY_ATTEMPTS_NUMBER;
        _client.Config.ConnectTimeout = _config.Timeout;
        _client.Config.DataConnectionConnectTimeout = _config.Timeout;
        var readTimeout = mode == FtpConfigurationMode.Complete && _config.IsReceiveTimeoutExplicitlyConfigured
            ? _config.ReceiveTimeout
            : _config.ReadWriteTimeout;
        _client.Config.ReadTimeout = readTimeout;
        _client.Config.DataConnectionReadTimeout = readTimeout;
        _client.Config.DataConnectionType = _config.UsePassive
            ? FtpDataConnectionType.AutoPassive
            : FtpDataConnectionType.AutoActive;
        if (mode == FtpConfigurationMode.Complete)
        {
            var transferType = _config.UseBinary ? FtpDataType.Binary : FtpDataType.ASCII;
            _client.Config.UploadDataType = transferType;
            _client.Config.DownloadDataType = transferType;
        }
        _client.Encoding = System.Text.Encoding.GetEncoding(_config.CharsetName);
        if (encryptionMode != FtpEncryptionMode.None)
            _client.ValidateCertificate += Client_ValidateCertificate;

        _operationGate = mode == FtpConfigurationMode.Complete
            ? new FtpOperationGate(_config.KeepAlive, EnsureConnectedCore, DisconnectCore)
            : null;
    }

    /// <summary>
    /// 在创建底层客户端前拒绝无效或无法实现的完整配置。
    /// </summary>
    private static void ValidateCompleteConfiguration(FtpClientConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Host) || ContainsWhitespace(config.Host))
            throw new ArgumentException("必须指定不含空白字符的 FTP 服务器主机。", nameof(config.Host));
        if (config.Port < 1 || config.Port > 65535)
            throw new ArgumentOutOfRangeException(nameof(config.Port), config.Port, "FTP 服务器端口必须位于 1 到 65535 之间。");
        if (config.Timeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.Timeout), config.Timeout, "连接超时必须大于零。");
        if (config.ReadWriteTimeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.ReadWriteTimeout), config.ReadWriteTimeout, "读写超时必须大于零。");
        if (config.IsReceiveTimeoutExplicitlyConfigured && config.ReceiveTimeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.ReceiveTimeout), config.ReceiveTimeout, "接收超时必须大于零。");
        if (config.ConnectCount < 0)
            throw new ArgumentOutOfRangeException(nameof(config.ConnectCount), config.ConnectCount, "额外连接重试次数不能小于零。");
        if (!string.IsNullOrEmpty(config.ProxyHost))
        {
            if (string.IsNullOrWhiteSpace(config.ProxyHost) || ContainsWhitespace(config.ProxyHost))
                throw new ArgumentException("代理服务器主机不能包含空白字符。", nameof(config.ProxyHost));
            if (config.ProxyPort < 1 || config.ProxyPort > 65535)
                throw new ArgumentOutOfRangeException(nameof(config.ProxyPort), config.ProxyPort, "代理服务器端口必须位于 1 到 65535 之间。");
        }
        if (config.IsSendTimeoutExplicitlyConfigured && config.SendTimeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.SendTimeout), config.SendTimeout, "发送超时必须大于零。");
        if (config.IsDisconnectTimeoutExplicitlyConfigured && config.DisconnectTimeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.DisconnectTimeout), config.DisconnectTimeout, "断开超时必须大于零。");
        if (config.IsSendTimeoutExplicitlyConfigured)
            throw new NotSupportedException("当前 FluentFTP 后端不支持独立配置 FTP 发送超时。");
        if (config.IsDisconnectTimeoutExplicitlyConfigured)
            throw new NotSupportedException("当前 FluentFTP 后端不支持独立配置 FTP 断开超时。");
        if (!Enum.IsDefined(typeof(FtpAsyncProxyType), config.ProxyType))
            throw new ArgumentOutOfRangeException(nameof(config.ProxyType), config.ProxyType, "不支持的 FTP 代理类型。");
    }

    /// <summary>
    /// 判断主机字段是否含空白字符。
    /// </summary>
    private static bool ContainsWhitespace(string value)
    {
        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 按显式代理类型创建同步底层客户端。
    /// </summary>
    private static FluentFTP.FtpClient CreateNativeClient(
        FtpClientConfig config,
        FtpConfigurationMode mode)
    {
        if (mode != FtpConfigurationMode.Complete || string.IsNullOrEmpty(config.ProxyHost))
            return new FluentFTP.FtpClient(
                config.Host,
                new System.Net.NetworkCredential(config.UserName, config.Password),
                config.Port);

        var profile = new FtpProxyProfile
        {
            ProxyHost = config.ProxyHost,
            ProxyPort = config.ProxyPort,
            ProxyCredentials = string.IsNullOrEmpty(config.ProxyUserName) && string.IsNullOrEmpty(config.ProxyPassword)
                ? null
                : new System.Net.NetworkCredential(config.ProxyUserName, config.ProxyPassword),
            FtpHost = config.Host,
            FtpPort = config.Port,
            FtpCredentials = new System.Net.NetworkCredential(config.UserName, config.Password)
        };

        switch (config.ProxyType)
        {
            case FtpAsyncProxyType.Http11:
                return new FluentFTP.Proxy.SyncProxy.FtpClientHttp11Proxy(profile);
            case FtpAsyncProxyType.Socks4:
                return new FluentFTP.Proxy.SyncProxy.FtpClientSocks4Proxy(profile);
            case FtpAsyncProxyType.Socks4a:
                return new FluentFTP.Proxy.SyncProxy.FtpClientSocks4aProxy(profile);
            case FtpAsyncProxyType.Socks5:
                return new FluentFTP.Proxy.SyncProxy.FtpClientSocks5Proxy(profile);
            case FtpAsyncProxyType.BlueCoat:
                return new FluentFTP.Proxy.SyncProxy.FtpClientBlueCoatProxy(profile);
            case FtpAsyncProxyType.UserAtHost:
                return new FluentFTP.Proxy.SyncProxy.FtpClientUserAtHostProxy(profile);
            default:
                throw new ArgumentOutOfRangeException(nameof(config.ProxyType), config.ProxyType, "不支持的 FTP 代理类型。");
        }
    }

    /// <summary>
    /// 转换加密模式；完整模式仅在未指定类型时使用 EnableSsl 回退。
    /// </summary>
    internal static FtpEncryptionMode ToNativeEncryptionMode(
        EncryptionType type,
        bool enableSsl,
        FtpConfigurationMode mode)
    {
        if (mode == FtpConfigurationMode.Complete && enableSsl && type == EncryptionType.None)
            type = EncryptionType.Explicit;

        switch (type)
        {
            case EncryptionType.None:
                return FtpEncryptionMode.None;
            case EncryptionType.Implicit:
                return FtpEncryptionMode.Implicit;
            case EncryptionType.Explicit:
                return FtpEncryptionMode.Explicit;
            default:
                if (mode == FtpConfigurationMode.Legacy)
                    return FtpEncryptionMode.None;
                throw new ArgumentOutOfRangeException(nameof(type), type, "不支持的 FTP 加密模式。");
        }
    }

    /// <summary>
    /// 将远程基准目录标准化为以斜线开头的路径。
    /// </summary>
    internal static string GetRemoteDirectoryRoot(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return "/";

        var normalized = directory.Replace('\\', '/');
        if (!normalized.StartsWith("/", StringComparison.Ordinal))
            normalized = "/" + normalized;
        normalized = normalized.TrimEnd('/');
        return normalized.Length == 0 ? "/" : normalized;
    }

    /// <summary>
    /// 将相对远程路径定位到基准目录，绝对路径保持不变。
    /// </summary>
    internal static string ResolveRemotePath(string baseDirectory, string path)
    {
        if (string.IsNullOrEmpty(path) || path.StartsWith("/", StringComparison.Ordinal))
            return path;

        var root = GetRemoteDirectoryRoot(baseDirectory);
        return root == "/"
            ? "/" + path.TrimStart('/')
            : root + "/" + path.TrimStart('/');
    }

    /// <summary>
    /// 以构造时固定目录解析本地相对路径。
    /// </summary>
    internal static string ResolveLocalPath(string baseDirectory, string path)
    {
        if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
            return path;
        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    /// <summary>
    /// 完整模式 ASCII 传输允许服务端换行转换，不执行二进制长度校验。
    /// </summary>
    internal static bool ShouldCheckTransferLength(FtpConfigurationMode mode, bool useBinary) =>
        mode == FtpConfigurationMode.Legacy || useBinary;

    /// <summary>
    /// 完整模式必须检查底层下载结果；历史模式保持原行为。
    /// </summary>
    internal static bool ShouldAcceptDownloadStatus(FtpConfigurationMode mode, bool status) =>
        mode == FtpConfigurationMode.Legacy || status;

    /// <summary>
    /// 为 ASCII 传输禁用不适用的字节摘要校验。
    /// </summary>
    internal static FtpVerify GetTransferVerification(FtpConfigurationMode mode, bool useBinary, FtpVerify verification) =>
        ShouldCheckTransferLength(mode, useBinary) ? verification : FtpVerify.None;

    /// <summary>
    /// 按当前模式解析远程操作路径。
    /// </summary>
    private string GetRemotePath(string path) =>
        _configurationMode == FtpConfigurationMode.Complete
            ? ResolveRemotePath(_remoteDirectory, path)
            : path;

    /// <summary>
    /// 提供内部路径解析验证接缝。
    /// </summary>
    internal string ResolveConfiguredRemotePath(string path) => GetRemotePath(path);

    /// <summary>
    /// 仅在完整模式更新后续相对路径的基准。
    /// </summary>
    internal void SetRemoteDirectoryBase(string directory)
    {
        if (_configurationMode == FtpConfigurationMode.Complete)
            _remoteDirectory = GetRemoteDirectoryRoot(directory);
    }

    /// <summary>
    /// 按当前模式解析本地操作路径。
    /// </summary>
    private string GetLocalPath(string path) =>
        _configurationMode == FtpConfigurationMode.Complete
            ? ResolveLocalPath(_localDirectory, path)
            : path;

    /// <summary>
    /// 串行进入最外层完整操作；嵌套调用复用当前连接。
    /// </summary>
    private IDisposable BeginOperation(bool connectBefore = true, bool disconnectAfter = true)
    {
        return _operationGate?.Enter(connectBefore, disconnectAfter) ?? EmptyOperationScope.Instance;
    }

    /// <summary>
    /// 保证控制连接已建立，重试仅覆盖连接阶段。
    /// </summary>
    private void EnsureConnectedCore()
    {
        if (_client == null || _client.IsConnected)
            return;

        if (_configurationMode == FtpConfigurationMode.Legacy)
        {
            _client.Connect();
            return;
        }

        try
        {
            ExecuteConnectionAttempts(_config.ConnectCount, _client.Connect, DisconnectCore);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"发生连接错误: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
        catch (Exception e) when (e is IOException || e is SocketException || e is TimeoutException)
        {
            throw new FtpClientException($"发生连接错误: {e.Message}", e);
        }
    }

    /// <summary>
    /// 执行首次连接和限定次数的重试；清理错误不覆盖连接错误。
    /// </summary>
    internal static void ExecuteConnectionAttempts(int additionalAttempts, Action connect, Action cleanup)
    {
        if (additionalAttempts < 0)
            throw new ArgumentOutOfRangeException(nameof(additionalAttempts));
        if (connect == null)
            throw new ArgumentNullException(nameof(connect));
        if (cleanup == null)
            throw new ArgumentNullException(nameof(cleanup));

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                connect();
                return;
            }
            catch (Exception e)
            {
                try
                {
                    cleanup();
                }
                catch
                {
                    // 连接重试期间的清理失败不应覆盖原始连接错误。
                }

                if (!IsRetryableConnectionFailure(e) || attempt >= additionalAttempts)
                    throw;
            }
        }
    }

    /// <summary>
    /// 识别可参与连接重试的底层连接异常。
    /// </summary>
    internal static bool IsRetryableConnectionFailure(Exception exception) =>
        exception is FluentFTP.FtpException
        || exception is IOException
        || exception is SocketException
        || exception is TimeoutException;

    /// <summary>
    /// 断开仍有效的底层控制连接。
    /// </summary>
    private void DisconnectCore()
    {
        if (_client?.IsConnected == true)
            _client.Disconnect();
    }

    /// <summary>
    /// 获取内部配置快照，用于验证映射。
    /// </summary>
    internal FtpClientConfig ConfigurationSnapshot => _config;

    /// <summary>
    /// 获取当前实例的兼容模式。
    /// </summary>
    internal FtpConfigurationMode ConfigurationMode => _configurationMode;

    /// <summary>
    /// 历史模式使用的无操作作用域。
    /// </summary>
    private sealed class EmptyOperationScope : IDisposable
    {
        /// <summary>
        /// 共享无操作作用域实例。
        /// </summary>
        internal static readonly EmptyOperationScope Instance = new EmptyOperationScope();

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}

/// <summary>
/// 串行执行同步 FTP 复合操作，仅在最外层结束时自动断开。
/// </summary>
internal sealed class FtpOperationGate
{
    /// <summary>
    /// 保护整个复合操作的可重入锁。
    /// </summary>
    private readonly object _sync = new object();
    /// <summary>
    /// 指示操作结束后保留控制连接。
    /// </summary>
    private readonly bool _keepAlive;
    /// <summary>
    /// 保证连接可用的回调。
    /// </summary>
    private readonly Action _connect;
    /// <summary>
    /// 断开当前连接的回调。
    /// </summary>
    private readonly Action _disconnect;
    /// <summary>
    /// 当前线程持有的嵌套操作深度。
    /// </summary>
    private int _depth;
    /// <summary>
    /// 保存最外层操作指定的自动断开策略。
    /// </summary>
    private bool _disconnectAtExit;

    /// <summary>
    /// 创建完整配置模式的操作门闩。
    /// </summary>
    internal FtpOperationGate(bool keepAlive, Action connect, Action disconnect)
    {
        _keepAlive = keepAlive;
        _connect = connect;
        _disconnect = disconnect;
    }

    /// <summary>
    /// 进入操作；显式连接和断开可选择跳过自动清理。
    /// </summary>
    internal IDisposable Enter(bool connectBefore = true, bool disconnectAfter = true)
    {
        Monitor.Enter(_sync);
        var outermost = _depth++ == 0;
        if (outermost)
        {
            _disconnectAtExit = disconnectAfter;
            try
            {
                if (connectBefore)
                    _connect();
            }
            catch
            {
                Exit(outermost);
                throw;
            }
        }

        return new Scope(this, outermost);
    }

    /// <summary>
    /// 离开操作并释放门闩；自动清理不覆盖操作结果。
    /// </summary>
    private void Exit(bool outermost)
    {
        _depth--;
        try
        {
            if (outermost && !_keepAlive && _disconnectAtExit)
            {
                try
                {
                    _disconnect();
                }
                catch
                {
                    // 自动断开失败不应覆盖 FTP 操作本身的结果或异常。
                }
            }
        }
        finally
        {
            Monitor.Exit(_sync);
        }
    }

    /// <summary>
    /// 只允许释放一次的同步操作作用域。
    /// </summary>
    private sealed class Scope : IDisposable
    {
        /// <summary>
        /// 所属操作门闩。
        /// </summary>
        private readonly FtpOperationGate _owner;
        /// <summary>
        /// 标记当前作用域是否为最外层。
        /// </summary>
        private readonly bool _outermost;
        /// <summary>
        /// 防止重复释放门闩。
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// 记录门闩和最外层身份。
        /// </summary>
        internal Scope(FtpOperationGate owner, bool outermost)
        {
            _owner = owner;
            _outermost = outermost;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _owner.Exit(_outermost);
        }
    }
}
