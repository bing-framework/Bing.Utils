namespace Bing.Http.Clients;

public partial class HttpRequest<TResult> : IHttpRequestResponseLimitPolicy<TResult> where TResult : class
{
    /// <summary>true 表示请求显式设置了数值或无限制；false 表示继承客户端。</summary>
    private bool _hasResponseLimitOverride;
    /// <summary>每次执行的不可变限额快照；异步嵌套操作与并行执行互不覆盖。</summary>
    private readonly System.Threading.AsyncLocal<ResponseLimitSnapshot> _responseLimitContext = new();

    private long? EffectiveMaxResponseBytes => _responseLimitContext.Value is { } snapshot
        ? snapshot.MaxBytes : _maxResponseBytes;

    void IHttpRequestResponseLimitPolicy<TResult>.DisableResponseLimit()
    {
        _maxResponseBytes = null;
        _hasResponseLimitOverride = true;
    }

    void IHttpRequestResponseLimitPolicy<TResult>.ResetResponseLimit()
    {
        _maxResponseBytes = null;
        _hasResponseLimitOverride = false;
    }

    private sealed class ResponseLimitSnapshot
    {
        internal readonly long? MaxBytes;
        internal readonly long? DownloadMaxBytes;
        internal ResponseLimitSnapshot(long? maxBytes, long? downloadMaxBytes)
        {
            MaxBytes = maxBytes;
            DownloadMaxBytes = downloadMaxBytes;
        }
    }
}
