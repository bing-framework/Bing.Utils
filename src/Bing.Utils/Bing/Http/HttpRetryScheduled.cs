using System.Net;

namespace Bing.Http;

/// <summary>

/// 一次已接受的重试等待计划；不保证等待结束后会再次发送。

/// </summary>
public sealed class HttpRetryScheduled
{
    /// <summary>
    /// 初始化一个 <see cref="HttpRetryScheduled"/> 类型的实例。
    /// </summary>
    /// <param name="failedAttempt">刚失败的发送尝试序号，从 1 开始。</param>
    /// <param name="delay">最终计划等待时间，包含现有退避、抖动及 Retry-After 决策。</param>
    /// <param name="statusCode">导致重试的响应状态码；发送异常时为空。</param>
    /// <param name="exception">导致重试的原始发送异常；响应状态重试时为空。</param>
    /// <exception cref="ArgumentOutOfRangeException">尝试序号小于 1，或等待时间为负数。</exception>
    public HttpRetryScheduled(int failedAttempt, TimeSpan delay, HttpStatusCode? statusCode, Exception exception)
    {
        if (failedAttempt < 1) throw new ArgumentOutOfRangeException(nameof(failedAttempt));
        if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay));
        FailedAttempt = failedAttempt;
        Delay = delay;
        StatusCode = statusCode;
        Exception = exception;
    }

    /// <summary>

    /// 获取刚失败的发送尝试序号，从 1 开始。

    /// </summary>
    public int FailedAttempt { get; }
    /// <summary>
    /// 获取计划等待时长，不是实际等待耗时。
    /// </summary>
    public TimeSpan Delay { get; }
    /// <summary>
    /// 获取导致重试的响应状态码；发送异常时为空。
    /// </summary>
    public HttpStatusCode? StatusCode { get; }
    /// <summary>
    /// 获取原始发送异常引用；可能包含底层信息，导出时由调用方选择字段。
    /// </summary>
    public Exception Exception { get; }
}
