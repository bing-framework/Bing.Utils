namespace Bing.Http;

/// <summary>
/// 为支持重试观测的请求提供链式注册扩展。
/// </summary>
public static class HttpRetryObservationExtensions
{
    /// <summary>
    /// 设置重试等待观测回调，并返回原请求以继续配置。
    /// </summary>
    /// <typeparam name="TResult">成功响应结果类型。</typeparam>
    /// <param name="request">支持可选重试观测能力的请求。</param>
    /// <param name="observer">在已接受的重试等待前同步执行的回调。</param>
    /// <param name="onObserverError">可选的观测异常处理回调；自身异常也会被隔离。</param>
    /// <returns>同一个请求对象。</returns>
    /// <remarks>重复注册会覆盖旧配置；执行开始时取快照。不会启用或改变重试策略，也不保证下一次发送发生。</remarks>
    /// <exception cref="ArgumentNullException">请求或观测回调为空。</exception>
    /// <exception cref="NotSupportedException">请求未实现可选重试观测能力。</exception>
    public static IHttpRequest<TResult> ObserveRetry<TResult>(this IHttpRequest<TResult> request,
        Action<HttpRetryScheduled> observer, Action<Exception> onObserverError = null) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (observer == null) throw new ArgumentNullException(nameof(observer));
        if (request is not IHttpRequestRetryObservationRegistration<TResult> capability)
            throw new NotSupportedException("This HTTP request does not support retry observation. Implement IHttpRequestRetryObservationRegistration<TResult>.");
        capability.SetRetryObserver(observer, onObserverError);
        return request;
    }
}
