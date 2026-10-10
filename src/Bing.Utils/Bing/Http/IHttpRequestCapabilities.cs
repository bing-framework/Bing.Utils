namespace Bing.Http;

/// <summary>
/// 为 HTTP 请求提供读取状态、头信息和转换结果的可选能力。
/// </summary>
/// <typeparam name="TResult">成功响应正文转换后的类型。</typeparam>
public interface IHttpRequestResponse<TResult> where TResult : class
{
    /// <summary>
    /// 发送请求并返回独立的响应元数据快照及转换结果。
    /// </summary>
    Task<HttpResponseResult<TResult>> GetResponseAsync(bool includeRawContent = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 为 HTTP 请求提供追加异步拦截器的可选能力。
/// </summary>
/// <typeparam name="TResult">请求响应结果类型。</typeparam>
public interface IHttpRequestInterceptorRegistration<TResult> where TResult : class
{
    /// <summary>
    /// 将拦截器追加到请求的发送前和发送后处理链。
    /// </summary>
    void AddInterceptor(HttpRequestInterceptor interceptor);
}

/// <summary>
/// 为请求提供整次执行完成观测的可选能力。
/// </summary>
public interface IHttpRequestObservationRegistration<TResult> where TResult : class
{
    /// <summary>
    /// 设置完成观测回调及可选的观测错误处理回调。
    /// </summary>
    void SetCompletionObserver(Action<HttpRequestCompletion> observer, Action<Exception> onObserverError = null);
}

/// <summary>
/// 为请求提供重试等待观测的可选能力，不改变已有重试策略。
/// </summary>
/// <typeparam name="TResult">成功响应结果类型。</typeparam>
public interface IHttpRequestRetryObservationRegistration<TResult> where TResult : class
{
    /// <summary>
    /// 设置重试等待观测回调及可选的观测错误处理回调。
    /// </summary>
    void SetRetryObserver(Action<HttpRetryScheduled> observer, Action<Exception> onObserverError = null);
}
