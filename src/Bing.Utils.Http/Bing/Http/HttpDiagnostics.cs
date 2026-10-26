using System.Diagnostics;
using System.Net;
#if NET6_0_OR_GREATER
using System.Diagnostics.Metrics;
#endif

namespace Bing.Http;

/// <summary>
/// 不含 URL、请求头、正文或异常文本的诊断事件。
/// </summary>
public sealed class HttpDiagnosticEvent
{
    /// <summary>
    /// 创建有限元数据事件。
    /// </summary>
    internal HttpDiagnosticEvent(string kind, HttpStatusCode? status, int attempts, TimeSpan elapsed, string outcome)
    { Kind = kind; StatusCode = status; Attempts = attempts; Elapsed = elapsed; Outcome = outcome; }
    /// <summary>
    /// 获取事件类型：attempt、completion 或 retry。
    /// </summary>
    public string Kind { get; }
    /// <summary>
    /// 获取状态码，没有响应时为空。
    /// </summary>
    public HttpStatusCode? StatusCode { get; }
    /// <summary>
    /// 获取尝试序号或完成时的总尝试次数。
    /// </summary>
    public int Attempts { get; }
    /// <summary>
    /// 获取耗时；retry 事件表示计划等待时间。
    /// </summary>
    public TimeSpan Elapsed { get; }
    /// <summary>
    /// 获取固定集合中的结果分类，不导出异常文本。
    /// </summary>
    public string Outcome { get; }
}

/// <summary>
/// 将现有 HTTP 观察入口适配到诊断回调及现代框架的 Activity/Meter。
/// </summary>
/// <remarks>
/// 显式注册 CreateInterceptor()、RecordCompletion 和 RecordRetry；不会修改重试或覆盖既有回调。
/// 拦截器 span 仅覆盖发送至响应头阶段；完成指标包含正文处理和资源收尾。netstandard2.0 仅发出回调。
/// 本实例可共享；应在所有请求结束后释放。用户观察器失败被隔离，不影响 HTTP 结果。
/// </remarks>
public sealed class HttpDiagnostics : IDisposable
{
    /// <summary>
    /// 默认 ActivitySource 和 Meter 名称。
    /// </summary>
    public const string DefaultName = "Bing.Utils.Http";
    private readonly Action<HttpDiagnosticEvent> _observer;
    private readonly Action<Exception> _onError;
    private int _disposed;
#if NET6_0_OR_GREATER
    private readonly ActivitySource _activities;
    private readonly Meter _meter;
    private readonly Counter<long> _completed;
    private readonly Counter<long> _retried;
    private readonly Histogram<double> _duration;
#endif

    /// <summary>
    /// 创建可供多个请求共享的诊断适配器。
    /// </summary>
    /// <param name="observer">可选诊断事件回调，所有框架均可使用。</param>
    /// <param name="onObserverError">可选错误回调，自身异常也会被隔离。</param>
    /// <param name="name">稳定的仪表名称，不要包含每次请求的动态标识。</param>
    public HttpDiagnostics(Action<HttpDiagnosticEvent> observer = null, Action<Exception> onObserverError = null, string name = DefaultName)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("诊断名称不能为空。", nameof(name));
        _observer = observer; _onError = onObserverError;
#if NET6_0_OR_GREATER
        _activities = new ActivitySource(name);
        _meter = new Meter(name);
        _completed = _meter.CreateCounter<long>("bing.http.requests", "{request}");
        _retried = _meter.CreateCounter<long>("bing.http.retries", "{retry}");
        _duration = _meter.CreateHistogram<double>("bing.http.duration", "s");
#endif
    }

    /// <summary>
    /// 创建发送阶段拦截器；不会记录地址、头部或正文。
    /// </summary>
    /// <returns>通过 UseInterceptor 注册的拦截器。</returns>
    public HttpRequestInterceptor CreateInterceptor() => new DiagnosticInterceptor(this);

    /// <summary>
    /// 记录整次执行结果，适用于 ObserveCompletion 回调，也可在既有回调中显式调用。
    /// </summary>
    /// <param name="completion">执行结果。</param>
    public void RecordCompletion(HttpRequestCompletion completion)
    {
        if (completion == null) throw new ArgumentNullException(nameof(completion));
        Safe(() =>
        {
#if NET6_0_OR_GREATER
            var tags = new TagList { { "outcome", completion.Outcome.ToString() }, { "status", (int?)completion.StatusCode } };
            _completed.Add(1, tags); _duration.Record(completion.Elapsed.TotalSeconds, tags);
#endif
            _observer?.Invoke(new HttpDiagnosticEvent("completion", completion.StatusCode, completion.Attempts, completion.Elapsed, completion.Outcome.ToString()));
        });
    }

    /// <summary>
    /// 记录已接受的重试计划，不承诺后续请求一定发生。
    /// </summary>
    /// <param name="retry">重试计划。</param>
    public void RecordRetry(HttpRetryScheduled retry)
    {
        if (retry == null) throw new ArgumentNullException(nameof(retry));
        Safe(() =>
        {
#if NET6_0_OR_GREATER
            _retried.Add(1, new KeyValuePair<string, object>("status", (int?)retry.StatusCode));
#endif
            _observer?.Invoke(new HttpDiagnosticEvent("retry", retry.StatusCode, retry.FailedAttempt, retry.Delay, "Scheduled"));
        });
    }

    /// <summary>
    /// 隔离诊断故障；释放后停止产生新事件。
    /// </summary>
    private void Safe(Action action)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        try { action(); }
        catch (Exception error) { ReportError(error); }
    }

    /// <summary>
    /// 隔离用户监听器和错误回调自身的异常。
    /// </summary>
    private void ReportError(Exception error)
    {
        try { _onError?.Invoke(error); } catch { }
    }

    /// <summary>
    /// 释放仪表；请在所有使用本实例的请求结束后调用。
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
#if NET6_0_OR_GREATER
        _activities.Dispose(); _meter.Dispose();
#endif
    }

    /// <summary>
    /// 每个尝试独立保存 span，支持共享适配器与请求重试。
    /// </summary>
    private sealed class DiagnosticInterceptor : HttpRequestInterceptor
    {
        private readonly HttpDiagnostics _owner;
        private readonly string _key = "Bing.Diagnostics." + Guid.NewGuid().ToString("N");
        internal DiagnosticInterceptor(HttpDiagnostics owner) => _owner = owner;
        /// <inheritdoc />
        public override Task BeforeSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
#if NET6_0_OR_GREATER
            _owner.Safe(() =>
            {
                var activity = _owner._activities.StartActivity("http.send", ActivityKind.Client);
                if (activity != null) context.Items[_key] = activity;
            });
#endif
            return Task.CompletedTask;
        }
        /// <inheritdoc />
        public override Task AfterSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            var outcome = context.Exception is OperationCanceledException ? "Cancelled" : context.Exception != null ? "Faulted" :
                context.StatusCode.HasValue && (int)context.StatusCode.Value < 400 ? "Success" : "HttpFailure";
#if NET6_0_OR_GREATER
            if (context.Items.TryGetValue(_key, out var value) && value is Activity activity)
            {
                context.Items.Remove(_key);
                try
                {
                    try
                    {
                        activity.SetTag("status", (int?)context.StatusCode);
                        activity.SetTag("outcome", outcome);
                        if (outcome != "Success") activity.SetStatus(ActivityStatusCode.Error);
                    }
                    finally { activity.Dispose(); }
                }
                catch (Exception error) { _owner.ReportError(error); }
            }
#endif
            _owner.Safe(() => _owner._observer?.Invoke(new HttpDiagnosticEvent("attempt", context.StatusCode, context.Attempt, context.Elapsed, outcome)));
            return Task.CompletedTask;
        }
    }
}
