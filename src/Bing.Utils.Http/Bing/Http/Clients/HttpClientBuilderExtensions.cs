using System.Net.Http;
using System.Runtime.CompilerServices;
using Bing.Http.Clients.Internal;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Bing 请求级传输隔离注册扩展。
/// </summary>
public static class BingHttpClientBuilderExtensions
{
    /// <summary>
    /// 以弱引用关联客户端与隔离注册标识，用于确认请求隔离能力。
    /// </summary>
    private static readonly ConditionalWeakTable<HttpClient, object> Registrations = new();

    /// <summary>
    /// 为 Bing HTTP 请求启用传输隔离。
    /// </summary>
    /// <param name="builder">命名或类型化客户端生成器。</param>
    /// <returns>当前客户端生成器。</returns>
    /// <remarks>
    /// 使用默认主处理器工厂；替代原有主处理器注册，保留外层消息处理器管线。此客户端须通过 Bing HttpRequest 发送请求；重试克隆请求时须复制全部元数据。
    /// </remarks>
    public static IHttpClientBuilder UseBingRequestIsolation(this IHttpClientBuilder builder) =>
        UseBingRequestIsolation(builder, _ => new HttpClientHandler());

    /// <summary>
    /// 为 Bing HTTP 请求启用传输隔离。
    /// </summary>
    /// <param name="builder">命名或类型化客户端生成器。</param>
    /// <param name="handlerFactory">每次返回新处理器的工厂，可配置代理、解压等选项；服务提供者属于处理器作用域。</param>
    /// <returns>当前客户端生成器。</returns>
    /// <remarks>
    /// 替代原有主处理器注册，保留外层消息处理器管线。默认 Cookie 仅在一次执行及其重定向、重试内保留。克隆请求须复制全部元数据。此客户端须通过 Bing HttpRequest 发送，不支持直接调用 HttpClient.SendAsync。
    /// </remarks>
    /// <exception cref="ArgumentNullException">生成器或处理器工厂为 null。</exception>
    public static IHttpClientBuilder UseBingRequestIsolation(this IHttpClientBuilder builder,
        Func<IServiceProvider, HttpClientHandler> handlerFactory)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (handlerFactory == null)
            throw new ArgumentNullException(nameof(handlerFactory));
        var registration = new object();
        builder.ConfigurePrimaryHttpMessageHandler(provider =>
            new RequestIsolationHandler(registration, () => handlerFactory(provider)));
        builder.ConfigureHttpClient((provider, client) =>
        {
            // 确认最终主处理器未被后续注册覆盖，再授予客户端隔离能力。
            var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(builder.Name);
            while (handler is DelegatingHandler delegating)
                handler = delegating.InnerHandler;
            if (handler is not RequestIsolationHandler router || !ReferenceEquals(router.Registration, registration))
                throw new InvalidOperationException("UseBingRequestIsolation 必须是最终的主处理器注册，且只能注册一次。");
            Registrations.Add(client, registration);
        });
        return builder;
    }

    /// <summary>
    /// 获取客户端的隔离注册标识。
    /// </summary>
    /// <param name="client">要查询的客户端。</param>
    /// <returns>隔离注册标识；未注册时返回 <see langword="null" />。</returns>
    internal static object GetRegistration(HttpClient client) =>
        Registrations.TryGetValue(client, out var registration) ? registration : null;
}
