using System.Net;

namespace Bing.Http.Clients;

/// <summary>
/// Http请求的重试与诊断实现。
/// </summary>
public partial class HttpRequest<TResult> where TResult : class
{
    /// <summary>
    /// 生成重试延迟抖动值的随机数源。
    /// </summary>
    private static readonly Random RetryRandom = new Random();
    /// <summary>
    /// 保护共享随机数源的同步锁。
    /// </summary>
    private static readonly object RetryRandomLock = new object();

    /// <summary>

    /// 当前请求使用的重试配置副本。

    /// </summary>
    private HttpRetryOptions _retryOptions;
    /// <summary>
    /// 接收请求最终完成信息的回调。
    /// </summary>
    private Action<HttpRequestCompletion> _completionObserver;
    /// <summary>
    /// 处理完成观测回调异常的回调。
    /// </summary>
    private Action<Exception> _observationErrorHandler;

    /// <summary>

    /// 汇总单次请求的结果和观测状态。

    /// </summary>
    private sealed class ObservationState
    {
        /// <summary>
        /// 最后收到的 HTTP 状态码。
        /// </summary>
        public HttpStatusCode? StatusCode;
        /// <summary>
        /// 已经执行的发送尝试次数。
        /// </summary>
        public int Attempts;
        /// <summary>
        /// 标记请求是否被发送前回调拒绝。
        /// </summary>
        public bool Rejected;
        /// <summary>
        /// 标记请求是否因超时取消。
        /// </summary>
        public bool TimedOut;
    }

    /// <inheritdoc />
    public IHttpRequest<TResult> Retry(HttpRetryOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (options.MaxRetries < 0 || options.BaseDelay < TimeSpan.Zero || options.MaxDelay < options.BaseDelay || options.AllowedMethods == null)
            throw new ArgumentOutOfRangeException(nameof(options));
        _retryOptions = new HttpRetryOptions
        {
            MaxRetries = options.MaxRetries,
            BaseDelay = options.BaseDelay,
            MaxDelay = options.MaxDelay,
            UseJitter = options.UseJitter,
            RespectRetryAfter = options.RespectRetryAfter,
            RequireIdempotencyKeyForUnsafeMethods = options.RequireIdempotencyKeyForUnsafeMethods,
            HostHandlerOwnsRetries = options.HostHandlerOwnsRetries,
            AllowedMethods = new HashSet<HttpMethod>(options.AllowedMethods)
        };
        return this;
    }

    /// <inheritdoc />
    public void SetCompletionObserver(Action<HttpRequestCompletion> observer, Action<Exception> onObserverError = null)
    {
        _completionObserver = observer ?? throw new ArgumentNullException(nameof(observer));
        _observationErrorHandler = onObserverError;
    }

    /// <summary>
    /// 将发送请求前的验证错误纳入完成观测后抛出。
    /// </summary>
    /// <typeparam name="T">请求结果的类型。</typeparam>
    /// <param name="error">导致请求无法发送的错误。</param>
    /// <returns>此操作以所提供的错误结束，不会产生正常结果。</returns>
    private Task<T> FailPreflightAsync<T>(Exception error)
    {
        if (_completionObserver == null)
        {
            DisposeOwnedUploadStreams(error);
            throw error;
        }
        return ObserveOperationAsync<T>(_ =>
        {
            DisposeOwnedUploadStreams(error);
            return Task.FromException<T>(error);
        });
    }

    /// <summary>
    /// 执行异步操作并在结束时报告请求完成情况。
    /// </summary>
    /// <typeparam name="T">异步操作结果的类型。</typeparam>
    /// <param name="operation">接收观测状态并执行的异步操作。</param>
    /// <returns>异步操作产生的结果。</returns>
    private async Task<T> ObserveOperationAsync<T>(Func<ObservationState, Task<T>> operation)
    {
        var observer = _completionObserver;
        if (observer == null) return await operation(null).ConfigureAwait(false);
        var onError = _observationErrorHandler;
        var state = new ObservationState();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Exception error = null;
        try { return await operation(state).ConfigureAwait(false); }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            watch.Stop();
            var outcome = error is OperationCanceledException
                ? state.TimedOut ? HttpRequestOutcome.TimedOut : HttpRequestOutcome.Cancelled
                : error != null ? HttpRequestOutcome.Faulted
                : state.Rejected ? HttpRequestOutcome.Rejected
                : state.StatusCode.HasValue && (int)state.StatusCode.Value >= 200 && (int)state.StatusCode.Value <= 299
                    ? HttpRequestOutcome.Success : HttpRequestOutcome.HttpFailure;
            try { observer(new HttpRequestCompletion(state.StatusCode, state.Attempts, watch.Elapsed, outcome, error)); }
            catch (Exception observationError)
            {
                try { onError?.Invoke(observationError); }
                catch { /* 观测失败不得替换请求结果。 */ }
            }
        }
    }

    /// <summary>

    /// 判断响应状态码是否属于可重试状态。

    /// </summary>
    private static bool IsRetryable(HttpStatusCode status) =>
        status == HttpStatusCode.RequestTimeout || (int)status == 429 ||
        status == HttpStatusCode.InternalServerError || status == HttpStatusCode.BadGateway ||
        status == HttpStatusCode.ServiceUnavailable || status == HttpStatusCode.GatewayTimeout;

    /// <summary>

    /// 检查异常链中是否包含证书验证失败。

    /// </summary>
    private static bool IsCertificateFailure(Exception error)
    {
        for (var current = error; current != null; current = current.InnerException)
        {
            if (current is System.Security.Authentication.AuthenticationException)
                return true;
        }
        return false;
    }

    /// <summary>

    /// 计算并执行下一次重试前的等待。

    /// </summary>
    private async Task<bool> DelayBeforeRetryAsync(HttpResponseMessage response, int attempt, DateTimeOffset deadline,
        CancellationToken token, Exception error, Action<HttpRetryScheduled> observer, Action<Exception> onObserverError)
    {
        var factor = Math.Pow(2, Math.Min(attempt, 20));
        var milliseconds = Math.Min(_retryOptions.MaxDelay.TotalMilliseconds, _retryOptions.BaseDelay.TotalMilliseconds * factor);
        if (_retryOptions.UseJitter)
            milliseconds *= 0.5 + GetRetryJitter() * 0.5;
        var delay = TimeSpan.FromMilliseconds(milliseconds);
        if (_retryOptions.RespectRetryAfter && response?.Headers.RetryAfter is { } retryAfter)
        {
            var serverDelay = retryAfter.Delta ?? (retryAfter.Date - GetUtcNow());
            if (serverDelay > delay) delay = serverDelay.Value;
        }
        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
        if (deadline != DateTimeOffset.MaxValue && delay >= deadline - GetUtcNow()) return false;
        if (delay.TotalMilliseconds > uint.MaxValue - 1) return false;
        var statusCode = response?.StatusCode;
        response?.Dispose();
        token.ThrowIfCancellationRequested();
        NotifyRetryScheduled(observer, onObserverError, attempt, delay, statusCode, error);
        await WaitBeforeRetryAsync(delay, token).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 获取当前 UTC 时间；重试截止时间计算通过该入口读取。
    /// </summary>
    protected virtual DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

    /// <summary>
    /// 获取重试抖动因子。
    /// </summary>
    /// <returns>范围为 0（含）到 1（不含）的随机因子。</returns>
    protected virtual double GetRetryJitter()
    {
        lock (RetryRandomLock) return RetryRandom.NextDouble();
    }

    /// <summary>
    /// 等待下一次重试；测试实现可覆盖此入口控制等待。
    /// </summary>
    /// <param name="delay">计划等待时长。</param>
    /// <param name="token">取消令牌。</param>
    /// <returns>表示等待操作的任务。</returns>
    protected virtual Task WaitBeforeRetryAsync(TimeSpan delay, CancellationToken token) => Task.Delay(delay, token);
}
