using Bing.IO;

namespace Bing.Http.Clients;

public partial class HttpRequest<TResult> : IHttpRequestSegmentedDownload<TResult> where TResult : class
{
    /// <summary>自动读取远端长度和验证器，按配置规划分片并恢复检查点。</summary>
    /// <remarks>未完成片段以返回状态表示；成功合并后才删除检查点。默认不限制带宽。</remarks>
    public Task<HttpDownloadSegmentsState> DownloadFileSegmentedAsync(string filePath,
        HttpSegmentedDownloadOptions options = null, CancellationToken cancellationToken = default)
    {
        var config = (options ?? new HttpSegmentedDownloadOptions()).Snapshot();
        var initial = new HttpDownloadSegmentsState
        {
            SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
            TotalLength = 0, Segments = new List<HttpDownloadSegmentState>()
        };
        if (SendAfterAction != null || SendAfterWithCancellationAction != null)
            throw new NotSupportedException("Segmented downloads cannot be combined with OnSendAfter.");
        return DownloadFileSegmentedCoreAsync(filePath, initial,
            (_, token) => SendSegmentedProbeRequestAsync(token), null,
            (store, range, state, token) => SendSegmentedRangeToStoreAsync(store, range, state.TotalLength, state, token, config.RateLimiter, config.Metrics),
            config.SegmentSize, config.MaxConcurrency, config.MaxRetries, config.RetryDelay, cancellationToken,
            config.Progress, config.Failure, _retryObserver, _retryObservationErrorHandler, bindRequestIdentity: true,
            temporaryStore: config.TemporaryStore, metrics: config.Metrics);
    }
}
