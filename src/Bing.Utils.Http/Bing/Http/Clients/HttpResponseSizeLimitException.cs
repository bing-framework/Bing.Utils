namespace Bing.Http.Clients;

/// <summary>
/// 响应正文超过配置的字节上限。
/// </summary>
public sealed class HttpResponseSizeLimitException : IOException
{
    /// <summary>
    /// 初始化一个 <see cref="HttpResponseSizeLimitException"/> 类型的实例。
    /// </summary>
    /// <param name="limit">已配置的最大响应正文字节数</param>
    public HttpResponseSizeLimitException(long limit)
        : base($"HTTP response exceeds the configured {limit} byte limit.") => Limit = limit;

    /// <summary>
    /// 获取配置的字节上限。
    /// </summary>
    public long Limit { get; }
}
