using System.Net.Http;

namespace Bing.Http;

/// <summary>
/// 为既有 HTTP 客户端和请求对象提供基于可选接口的扩展能力。
/// </summary>
public static class HttpCapabilityExtensions
{
    /// <summary>
    /// 注册整次请求完成时的观测回调。
    /// </summary>
    public static IHttpRequest<TResult> ObserveCompletion<TResult>(this IHttpRequest<TResult> request,
        Action<HttpRequestCompletion> observer, Action<Exception> onObserverError = null) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (observer == null) throw new ArgumentNullException(nameof(observer));
        if (request is not IHttpRequestObservationRegistration<TResult> capability)
            throw new NotSupportedException("This HTTP request does not support completion observation. Implement IHttpRequestObservationRegistration<TResult>.");
        capability.SetCompletionObserver(observer, onObserverError);
        return request;
    }

    /// <summary>
    /// 通过客户端的请求工厂创建指定 HTTP 方法的请求。
    /// </summary>
    public static IHttpRequest<TResult> Request<TResult>(this IHttpClient client, HttpMethod method, string url)
        where TResult : class
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (client is not IHttpRequestFactory factory)
            throw new NotSupportedException("This HTTP client does not support arbitrary HTTP methods. Implement IHttpRequestFactory.");
        return factory.CreateRequest<TResult>(method, url);
    }

    /// <summary>
    /// 创建指定成功响应类型的 PATCH 请求。
    /// </summary>
    public static IHttpRequest<TResult> Patch<TResult>(this IHttpClient client, string url) where TResult : class =>
        client.Request<TResult>(new HttpMethod("PATCH"), url);

    /// <summary>
    /// 创建 PATCH 请求并设置可选正文。
    /// </summary>
    public static IHttpRequest<TResult> Patch<TResult>(this IHttpClient client, string url, object content)
        where TResult : class =>
        content == null ? client.Patch<TResult>(url) : client.Patch<TResult>(url).Content(content);

    /// <summary>
    /// 创建 PATCH 请求并将响应正文按字符串读取。
    /// </summary>
    public static IHttpRequest<string> Patch(this IHttpClient client, string url) => client.Patch<string>(url);

    /// <summary>
    /// 创建 PATCH 请求、设置可选正文并将响应正文按字符串读取。
    /// </summary>
    public static IHttpRequest<string> Patch(this IHttpClient client, string url, object content) =>
        client.Patch<string>(url, content);

    /// <summary>
    /// 创建 HEAD 请求并将结果类型设为字符串。
    /// </summary>
    public static IHttpRequest<string> Head(this IHttpClient client, string url) =>
        client.Request<string>(HttpMethod.Head, url);

    /// <summary>
    /// 创建 OPTIONS 请求并将结果类型设为字符串。
    /// </summary>
    public static IHttpRequest<string> Options(this IHttpClient client, string url) =>
        client.Request<string>(HttpMethod.Options, url);

    /// <summary>
    /// 发送请求并读取完整响应元数据和转换后的结果。
    /// </summary>
    public static Task<HttpResponseResult<TResult>> GetResponseAsync<TResult>(this IHttpRequest<TResult> request,
        bool includeRawContent = false, CancellationToken cancellationToken = default) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request is not IHttpRequestResponse<TResult> capability)
            throw new NotSupportedException("This HTTP request does not support response metadata. Implement IHttpRequestResponse<TResult>.");
        return capability.GetResponseAsync(includeRawContent, cancellationToken);
    }

    /// <summary>
    /// 将异步拦截器追加到请求处理链，并返回原请求以继续链式配置。
    /// </summary>
    public static IHttpRequest<TResult> UseInterceptor<TResult>(this IHttpRequest<TResult> request,
        HttpRequestInterceptor interceptor) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (interceptor == null) throw new ArgumentNullException(nameof(interceptor));
        if (request is not IHttpRequestInterceptorRegistration<TResult> capability)
            throw new NotSupportedException("This HTTP request does not support interceptors. Implement IHttpRequestInterceptorRegistration<TResult>.");
        capability.AddInterceptor(interceptor);
        return request;
    }
}
