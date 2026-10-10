using System.Net.Http;

namespace Bing.Http;

/// <summary>

/// HTTP 请求重试配置；仅在显式调用 Retry 时生效。

/// </summary>
public sealed class HttpRetryOptions
{
    /// <summary>
    /// 最大重试次数，不包含首次发送。
    /// </summary>
    public int MaxRetries { get; set; } = 2;
    /// <summary>
    /// 指数退避的初始等待时间。
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);
    /// <summary>
    /// 单次退避最长等待时间。
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>
    /// 是否在退避时间内添加随机抖动。
    /// </summary>
    public bool UseJitter { get; set; } = true;
    /// <summary>
    /// 是否遵循 Retry-After。
    /// </summary>
    public bool RespectRetryAfter { get; set; } = true;
    /// <summary>
    /// 获取或设置非安全方法重试是否必须携带非空的 Idempotency-Key 请求头。
    /// </summary>
    public bool RequireIdempotencyKeyForUnsafeMethods { get; set; }

    /// <summary>
    /// 宿主 Handler 是否已经负责重试。
    /// </summary>
    /// <remarks>
    /// 设置为 <see langword="true"/> 后，当前请求不能再配置请求级重试；模块不会尝试探测宿主 Handler。
    /// </remarks>
    public bool HostHandlerOwnsRetries { get; set; }
    /// <summary>
    /// 允许重试的 HTTP 方法；默认仅安全方法。
    /// </summary>
    public ISet<HttpMethod> AllowedMethods { get; set; } = new HashSet<HttpMethod>
    {
        HttpMethod.Get, HttpMethod.Head, HttpMethod.Options
    };
}
