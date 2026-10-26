namespace Bing.Http.Clients;

/// <summary>
/// 最终下载内容超过已配置的总量限额。
/// </summary>
public sealed class HttpDownloadSizeLimitException : IOException
{
    /// <summary>
    /// 创建下载超限错误。
    /// </summary>
    /// <param name="limit">逻辑总字节上限。</param>
    public HttpDownloadSizeLimitException(long limit) : base($"HTTP download exceeds the configured {limit} byte limit.") => Limit = limit;
    /// <summary>
    /// 获取逻辑总字节上限。
    /// </summary>
    public long Limit { get; }
}

public partial class HttpRequest<TResult> : IHttpRequestDownloadLimitPolicy<TResult> where TResult : class
{
    /// <summary>
    /// 请求配置；在执行开始固定到异步上下文。
    /// </summary>
    private long? _maxDownloadBytes;
    /// <summary>
    /// 本次执行的下载总量上限。
    /// </summary>
    private long? EffectiveMaxDownloadBytes => _responseLimitContext.Value is { } snapshot ? snapshot.DownloadMaxBytes : _maxDownloadBytes;

    /// <inheritdoc />
    void IHttpRequestDownloadLimitPolicy<TResult>.SetMaxDownloadBytes(long? maxBytes)
    {
        if (maxBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        _maxDownloadBytes = maxBytes;
    }

    /// <summary>
    /// 使用减法检查预算，防止累计字节溢出。
    /// </summary>
    private static void EnsureDownloadSize(long existing, long additional, long? limit)
    {
        if (limit.HasValue && (existing > limit.Value || additional > limit.Value - existing))
            throw new HttpDownloadSizeLimitException(limit.Value);
    }
}
