using System.Net;

namespace Bing.Http;

/// <summary>
/// 整次请求执行的最终结果分类。
/// </summary>
public enum HttpRequestOutcome
{
    /// <summary>
    /// 请求成功并返回响应。
    /// </summary>
    Success,

    /// <summary>
    /// 请求已发送，但响应为 HTTP 失败状态。
    /// </summary>
    HttpFailure,

    /// <summary>
    /// 请求在发送前被拦截或拒绝。
    /// </summary>
    Rejected,

    /// <summary>
    /// 请求因取消令牌而结束。
    /// </summary>
    Cancelled,

    /// <summary>
    /// 请求因超时而结束。
    /// </summary>
    TimedOut,

    /// <summary>
    /// 请求因未分类异常而结束。
    /// </summary>
    Faulted
}

/// <summary>
/// 整次请求结束后的观测信息，不包含地址、请求头或正文。
/// </summary>
public sealed class HttpRequestCompletion
{
    /// <summary>
    /// 初始化一个 <see cref="HttpRequestCompletion"/> 类型的实例。
    /// </summary>
    /// <param name="statusCode">最终响应状态码；没有响应时为空。</param>
    /// <param name="attempts">实际开始的发送尝试次数。</param>
    /// <param name="elapsed">总耗时。</param>
    /// <param name="outcome">最终结果分类。</param>
    /// <param name="exception">传播给调用方的异常；正常返回时为空。</param>
    public HttpRequestCompletion(HttpStatusCode? statusCode, int attempts, TimeSpan elapsed,
        HttpRequestOutcome outcome, Exception exception)
    {
        if (attempts < 0) throw new ArgumentOutOfRangeException(nameof(attempts));
        if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));
        StatusCode = statusCode;
        Attempts = attempts;
        Elapsed = elapsed;
        Outcome = outcome;
        Exception = exception;
    }

    /// <summary>
    /// 获取最终响应状态码；尚未收到响应时为空。
    /// </summary>
    public HttpStatusCode? StatusCode { get; }
    /// <summary>
    /// 获取实际开始的发送尝试次数。
    /// </summary>
    public int Attempts { get; }
    /// <summary>
    /// 获取从执行准备到资源清理结束的总耗时。
    /// </summary>
    public TimeSpan Elapsed { get; }
    /// <summary>
    /// 获取最终结果分类。
    /// </summary>
    public HttpRequestOutcome Outcome { get; }
    /// <summary>
    /// 获取传播给调用方的原始异常；正常返回时为空。
    /// </summary>
    public Exception Exception { get; }
}
