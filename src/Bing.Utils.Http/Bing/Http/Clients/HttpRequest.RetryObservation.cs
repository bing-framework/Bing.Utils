namespace Bing.Http.Clients;

/// <summary>

/// HTTP 请求的可选重试等待观测能力。

/// </summary>
public partial class HttpRequest<TResult> : IHttpRequestRetryObservationRegistration<TResult> where TResult : class
{
    /// <summary>
    /// 接收已安排重试等待信息的回调。
    /// </summary>
    private Action<HttpRetryScheduled> _retryObserver;
    /// <summary>
    /// 处理重试观测回调异常的回调。
    /// </summary>
    private Action<Exception> _retryObservationErrorHandler;

    /// <inheritdoc />
    public void SetRetryObserver(Action<HttpRetryScheduled> observer, Action<Exception> onObserverError = null)
    {
        _retryObserver = observer ?? throw new ArgumentNullException(nameof(observer));
        _retryObservationErrorHandler = onObserverError;
    }

    /// <summary>

    /// 报告分片下载已安排的重试等待。

    /// </summary>
    private static void NotifyRetryScheduled(Action<HttpRetryScheduled> observer, Action<Exception> onObserverError,
        int attempt, TimeSpan delay, System.Net.HttpStatusCode? statusCode, Exception error)
    {
        if (observer == null) return;
        try { observer(new HttpRetryScheduled(attempt + 1, delay, statusCode, error)); }
        catch (Exception observationError)
        {
            try { onObserverError?.Invoke(observationError); }
            catch { /* 观测异常不得改变请求结果。 */ }
        }
    }
}
