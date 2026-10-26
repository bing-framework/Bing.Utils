namespace Bing.Http;

/// <summary>可选的响应限额继承策略；旧请求实现无需增加成员。</summary>
/// <typeparam name="TResult">响应转换类型。</typeparam>
public interface IHttpRequestResponseLimitPolicy<TResult> where TResult : class
{
    /// <summary>显式取消响应限额，覆盖客户端默认值。</summary>
    void DisableResponseLimit();
    /// <summary>清除请求级覆盖，重新继承本次选定客户端的默认限额。</summary>
    void ResetResponseLimit();
}

/// <summary>为支持限额继承策略的请求提供链式配置。</summary>
public static class HttpResponseLimitExtensions
{
    /// <summary>显式取消响应大小限制；直到再次设置上限或恢复继承。</summary>
    /// <typeparam name="TResult">响应转换类型。</typeparam>
    /// <param name="request">支持限额策略的请求。</param>
    /// <returns>原请求对象。</returns>
    /// <exception cref="NotSupportedException">旧实现未提供限额策略能力。</exception>
    public static IHttpRequest<TResult> WithoutMaxResponseLimit<TResult>(this IHttpRequest<TResult> request) where TResult : class
    {
        GetPolicy(request).DisableResponseLimit();
        return request;
    }

    /// <summary>恢复客户端默认限额；客户端未配置时不限制正文大小。</summary>
    /// <typeparam name="TResult">响应转换类型。</typeparam>
    /// <param name="request">支持限额策略的请求。</param>
    /// <returns>原请求对象。</returns>
    public static IHttpRequest<TResult> UseClientDefaultMaxResponseBytes<TResult>(this IHttpRequest<TResult> request) where TResult : class
    {
        GetPolicy(request).ResetResponseLimit();
        return request;
    }

    private static IHttpRequestResponseLimitPolicy<TResult> GetPolicy<TResult>(IHttpRequest<TResult> request) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        return request as IHttpRequestResponseLimitPolicy<TResult>
            ?? throw new NotSupportedException("请求未实现 IHttpRequestResponseLimitPolicy，不能配置限额继承策略。");
    }
}
