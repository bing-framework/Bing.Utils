using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Bing.Http.Clients.Internal;

/// <summary>
/// 命名客户端管线末端的请求隔离路由器。
/// </summary>
/// <remarks>
/// 根据请求上下文选择传输资源，不修改池化处理器的请求级设置。
/// </remarks>
internal sealed class RequestIsolationHandler : HttpMessageHandler
{
    /// <summary>
    /// 以弱引用登记已使用的处理器，防止工厂重复返回同一实例。
    /// </summary>
    private static readonly ConditionalWeakTable<HttpClientHandler, object> ClaimedHandlers = new();
    /// <summary>
    /// 每次创建独立主处理器的工厂。
    /// </summary>
    private readonly Func<HttpClientHandler> _factory;
    /// <summary>
    /// 协调共享传输资源创建与释放的同步锁。
    /// </summary>
    private readonly object _sync = new();
    /// <summary>
    /// 供无需请求级隔离的请求复用并随路由器释放的传输资源。
    /// </summary>
    private RequestTransport _sharedTransport;
    /// <summary>
    /// 指示路由器是否已释放；释放后禁止发送请求或获取共享调用器。
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 初始化 <see cref="RequestIsolationHandler" /> 类的新实例。
    /// </summary>
    /// <param name="registration">客户端隔离注册的标识。</param>
    /// <param name="factory">每次返回新主处理器的工厂。</param>
    internal RequestIsolationHandler(object registration, Func<HttpClientHandler> factory)
    {
        Registration = registration;
        _factory = factory;
    }

    /// <summary>
    /// 获取客户端隔离注册标识。
    /// </summary>
    internal object Registration { get; }
    /// <summary>
    /// 获取共享传输调用器。
    /// </summary>
    /// <remarks>
    /// 首次访问时创建传输资源，创建和释放使用同一同步锁。
    /// </remarks>
    /// <exception cref="ObjectDisposedException">路由器已释放。</exception>
    internal HttpMessageInvoker SharedInvoker
    {
        get
        {
            // 创建与释放共用锁，避免释放时遗漏尚在工厂中创建的共享处理器。
            lock (_sync)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(RequestIsolationHandler));
                return (_sharedTransport ??= CreateTransport(null)).Invoker;
            }
        }
    }

    /// <summary>
    /// 创建请求传输资源。
    /// </summary>
    /// <param name="context">请求隔离配置；为 null 时创建禁用自动 Cookie 的共享传输。</param>
    /// <returns>由调用方负责释放的传输资源。</returns>
    /// <exception cref="InvalidOperationException">工厂返回 null 或已使用的处理器实例。</exception>
    internal RequestTransport CreateTransport(RequestIsolationContext context)
    {
        var handler = _factory() ?? throw new InvalidOperationException("隔离处理器工厂不能返回 null。");
        // 弱引用登记不会永久保留已释放处理器，但能拒绝重复返回同一实例。
        lock (ClaimedHandlers)
        {
            if (ClaimedHandlers.TryGetValue(handler, out _))
                throw new InvalidOperationException("隔离处理器工厂每次必须返回新的 HttpClientHandler 实例。");
            ClaimedHandlers.Add(handler, new object());
        }

        X509Certificate2 certificate = null;
        try
        {
            handler.UseCookies = context?.UseCookies ?? false;
            if (handler.UseCookies)
                handler.CookieContainer = new CookieContainer();
            if (context?.IgnoreSsl == true)
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            if (!string.IsNullOrWhiteSpace(context?.CertificatePath))
            {
                certificate = new X509Certificate2(context.CertificatePath, context.CertificatePassword,
#if NETSTANDARD2_0
                    X509KeyStorageFlags.DefaultKeySet);
#else
                    // Windows Schannel 不支持部分临时私钥，使用随证书释放的默认密钥集。
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? X509KeyStorageFlags.DefaultKeySet
                        : X509KeyStorageFlags.EphemeralKeySet);
#endif
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ClientCertificates.Clear();
                handler.ClientCertificates.Add(certificate);
            }
            return new RequestTransport(handler, certificate);
        }
        catch
        {
            handler.Dispose();
            certificate?.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RequestIsolationHandler));
        }
        var context = RequestIsolationContext.Get(request)
            ?? throw new InvalidOperationException("缺少请求隔离上下文。请通过 Bing HttpRequest 发送，重试克隆请求时必须复制全部请求元数据。");
        return context.GetInvoker(this).SendAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_sync)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _sharedTransport?.Dispose();
                }
            }
        }
        base.Dispose(disposing);
    }
}
