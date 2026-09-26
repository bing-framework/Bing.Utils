using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Bing.Http.Clients.Internal;

/// <summary>
/// 单次逻辑请求的传输隔离上下文。
/// </summary>
/// <remarks>
/// 重试克隆请求时须复制全部请求元数据，以复用同一上下文和传输资源。
/// </remarks>
internal sealed class RequestIsolationContext : IDisposable
{
    /// <summary>
    /// 在请求元数据中存取隔离上下文的键。
    /// </summary>
    internal const string PropertyName = "Bing.Http.RequestIsolationContext";
    /// <summary>
    /// 保护隔离传输的创建、访问和释放的同步锁。
    /// </summary>
    private readonly object _sync = new();
    /// <summary>
    /// 本次逻辑请求按需创建并负责释放的独立传输资源。
    /// </summary>
    private RequestTransport _transport;
    /// <summary>
    /// 指示上下文是否已释放；释放后禁止获取传输调用器。
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 初始化 <see cref="RequestIsolationContext" /> 类的新实例。
    /// </summary>
    /// <param name="registration">客户端隔离注册的标识。</param>
    /// <param name="useCookies">是否为本次逻辑请求启用自动 Cookie。</param>
    /// <param name="ignoreSsl">是否跳过服务器证书验证。</param>
    /// <param name="certificatePath">客户端证书路径；为空时不加载证书。</param>
    /// <param name="certificatePassword">客户端证书密码。</param>
    internal RequestIsolationContext(object registration, bool useCookies, bool ignoreSsl,
        string certificatePath, string certificatePassword)
    {
        Registration = registration;
        UseCookies = useCookies;
        IgnoreSsl = ignoreSsl;
        CertificatePath = certificatePath;
        CertificatePassword = certificatePassword;
    }

    /// <summary>
    /// 获取客户端隔离注册标识。
    /// </summary>
    internal object Registration { get; }
    /// <summary>
    /// 获取是否启用自动 Cookie。
    /// </summary>
    internal bool UseCookies { get; }
    /// <summary>
    /// 获取是否跳过服务器证书验证。
    /// </summary>
    internal bool IgnoreSsl { get; }
    /// <summary>
    /// 获取客户端证书路径。
    /// </summary>
    internal string CertificatePath { get; }
    /// <summary>
    /// 获取客户端证书密码。
    /// </summary>
    internal string CertificatePassword { get; }
    /// <summary>
    /// 获取是否需要独立传输资源。
    /// </summary>
    internal bool RequiresPrivateTransport => UseCookies || IgnoreSsl || !string.IsNullOrWhiteSpace(CertificatePath);

    /// <summary>
    /// 将隔离上下文附加到请求元数据。
    /// </summary>
    /// <param name="request">要附加上下文的请求消息。</param>
    internal void Attach(HttpRequestMessage request)
    {
#if NET5_0_OR_GREATER
        request.Options.Set(new HttpRequestOptionsKey<RequestIsolationContext>(PropertyName), this);
#else
        request.Properties[PropertyName] = this;
#endif
    }

    /// <summary>
    /// 获取请求附带的隔离上下文。
    /// </summary>
    /// <param name="request">请求消息。</param>
    /// <returns>附带的隔离上下文；不存在时返回 <see langword="null" />。</returns>
    internal static RequestIsolationContext Get(HttpRequestMessage request)
    {
#if NET5_0_OR_GREATER
        request.Options.TryGetValue(new HttpRequestOptionsKey<RequestIsolationContext>(PropertyName), out var context);
        return context;
#else
        return request.Properties.TryGetValue(PropertyName, out var context) ? context as RequestIsolationContext : null;
#endif
    }

    /// <summary>
    /// 获取适用于当前请求的传输调用器。
    /// </summary>
    /// <param name="router">路由当前请求的隔离处理器。</param>
    /// <returns>共享或当前上下文独占的传输调用器。</returns>
    /// <exception cref="ObjectDisposedException">上下文已释放。</exception>
    /// <exception cref="InvalidOperationException">上下文与处理器的客户端注册不一致。</exception>
    internal HttpMessageInvoker GetInvoker(RequestIsolationHandler router)
    {
        lock (_sync)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RequestIsolationContext));
            if (!ReferenceEquals(Registration, router.Registration))
                throw new InvalidOperationException("请求隔离上下文不能用于其他客户端注册。");
            if (!RequiresPrivateTransport)
                return router.SharedInvoker;
            return (_transport ??= router.CreateTransport(this)).Invoker;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;
            _disposed = true;
            _transport?.Dispose();
        }
    }
}

/// <summary>
/// 请求传输资源的所有者。
/// </summary>
/// <remarks>
/// 释放时同时释放调用器、处理器及本模块加载的客户端证书；不接管调用方工厂提供的证书。
/// </remarks>
internal sealed class RequestTransport : IDisposable
{
    /// <summary>
    /// 由本模块加载并随传输资源释放的客户端证书；未加载时为 null。
    /// </summary>
    private readonly X509Certificate2 _certificate;

    /// <summary>
    /// 初始化 <see cref="RequestTransport" /> 类的新实例。
    /// </summary>
    /// <param name="handler">由当前传输资源接管并释放的处理器。</param>
    /// <param name="certificate">由本模块加载的证书；未加载时为 null。</param>
    internal RequestTransport(HttpClientHandler handler, X509Certificate2 certificate)
    {
        Invoker = new HttpMessageInvoker(handler, disposeHandler: true);
        _certificate = certificate;
    }

    /// <summary>
    /// 获取用于发送请求的传输调用器。
    /// </summary>
    internal HttpMessageInvoker Invoker { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        try { Invoker.Dispose(); }
        finally { _certificate?.Dispose(); }
    }
}
