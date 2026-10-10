namespace Bing.Http.Clients;

/// <summary>

/// 响应正文超过配置的字节上限。

/// </summary>
public sealed class HttpResponseSizeLimitException : IOException
{
    /// <summary>
    /// 创建响应正文超限异常。
    /// </summary>
    /// <param name="limit">已配置的最大响应正文字节数</param>
    public HttpResponseSizeLimitException(long limit)
        : base($"HTTP response exceeds the configured {limit} byte limit.") => Limit = limit;

    /// <summary>

    /// 配置的字节上限。

    /// </summary>
    public long Limit { get; }
}
