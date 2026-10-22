using System.Net.Http;

namespace Bing.Http;

/// <summary>
/// HTTP 请求重试配置；仅在显式调用 Retry 时生效。
/// </summary>
public sealed class HttpRetryOptions
{
    /// <summary>
    /// 获取或设置最大重试次数，不包含首次发送；默认值为 2，且不得小于 0。
    /// </summary>
    public int MaxRetries { get; set; } = 2;
    /// <summary>
    /// 获取或设置指数退避的初始等待时间；默认值为 200 毫秒，且不得小于零。
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);
    /// <summary>
    /// 获取或设置单次退避最长等待时间；默认值为 5 秒，且不得小于初始等待时间。
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>
    /// 获取或设置是否在退避时间内添加随机抖动；默认值为 true。
    /// </summary>
    public bool UseJitter { get; set; } = true;
    /// <summary>
    /// 获取或设置是否遵循 Retry-After 响应头；默认值为 true。
    /// </summary>
    public bool RespectRetryAfter { get; set; } = true;
    /// <summary>
    /// 获取或设置非安全方法重试是否必须携带非空的 Idempotency-Key 请求头；默认值为 false。
    /// </summary>
    public bool RequireIdempotencyKeyForUnsafeMethods { get; set; }

    /// <summary>
    /// 获取或设置宿主 Handler 是否已经负责重试；默认值为 false。
    /// </summary>
    /// <remarks>
    /// 设置为 <see langword="true"/> 后，当前请求不能再配置请求级重试；模块不会尝试探测宿主 Handler。
    /// </remarks>
    public bool HostHandlerOwnsRetries { get; set; }
    /// <summary>
    /// 获取或设置允许重试的 HTTP 方法；默认值为 GET、HEAD 和 OPTIONS，设置为 null 无效。
    /// </summary>
    public ISet<HttpMethod> AllowedMethods { get; set; } = new HashSet<HttpMethod>
    {
        HttpMethod.Get, HttpMethod.Head, HttpMethod.Options
    };
}
