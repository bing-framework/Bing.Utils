using Bing.IO;

namespace Bing.Http;

/// <summary>显式分片下载配置；每次执行固定标量和回调，共享限速器可在运行中调整。</summary>
public sealed class HttpSegmentedDownloadOptions
{
    /// <summary>单片最大字节数，默认 4 MiB。</summary>
    public long SegmentSize { get; set; } = 4 * 1024 * 1024;
    /// <summary>最大在途分片数，默认四路。</summary>
    public int MaxConcurrency { get; set; } = 4;
    /// <summary>每片最大重试次数，默认不重试。</summary>
    public int MaxRetries { get; set; }
    /// <summary>分片重试间隔，默认零。</summary>
    public TimeSpan RetryDelay { get; set; }
    /// <summary>分片正文共享的速率限制器；null 不限速，不限制本地合并 I/O。</summary>
    public TransferRateLimiter RateLimiter { get; set; }
    /// <summary>
    /// 获取或设置共享临时额度存储；为全部分片和合并临时目标预留两倍正文长度。
    /// </summary>
    /// <remarks>默认不启用。暂停/失败保留有保护标记的额度；恢复须传入同一存储。
    /// 不包含原有最终文件及文件系统元数据，不能代替磁盘可用空间检查。</remarks>
    public TemporaryFileStore TemporaryStore { get; set; }
    /// <summary>
    /// 获取或设置本次执行的传输诊断累积器；共享实例可统计多个下载。
    /// </summary>
    public HttpTransferMetrics Metrics { get; set; }
    /// <summary>分片检查点持久化后的进度；回调异常终止操作。</summary>
    public Action<HttpDownloadSegmentProgress> Progress { get; set; }
    /// <summary>分片最终失败通知；回调异常沿用原调度器的隔离约定。</summary>
    public Action<HttpDownloadSegmentFailure> Failure { get; set; }

    internal HttpSegmentedDownloadOptions Snapshot()
    {
        if (SegmentSize <= 0) throw new ArgumentOutOfRangeException(nameof(SegmentSize));
        if (MaxConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(MaxConcurrency));
        if (MaxRetries < 0) throw new ArgumentOutOfRangeException(nameof(MaxRetries));
        if (RetryDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(RetryDelay));
        return (HttpSegmentedDownloadOptions)MemberwiseClone();
    }
}

/// <summary>分片下载的可选能力，保持旧 IHttpRequest 实现的源和二进制兼容。</summary>
/// <typeparam name="TResult">请求结果类型。</typeparam>
public interface IHttpRequestSegmentedDownload<TResult> where TResult : class
{
    /// <summary>探测远端长度、规划分片并恢复同一请求身份的检查点；失败分片以未完成状态返回，取消正常传播。</summary>
    Task<HttpDownloadSegmentsState> DownloadFileSegmentedAsync(string filePath,
        HttpSegmentedDownloadOptions options = null, CancellationToken cancellationToken = default);
}

/// <summary>在接口类型请求上访问分片下载和可管理任务。</summary>
public static class HttpSegmentedDownloadExtensions
{
    /// <summary>自动探测并执行分片下载；不支持该可选能力的旧实现明确抛出 NotSupportedException。</summary>
    public static Task<HttpDownloadSegmentsState> DownloadFileSegmentedAsync<TResult>(this IHttpRequest<TResult> request,
        string filePath, HttpSegmentedDownloadOptions options = null, CancellationToken cancellationToken = default) where TResult : class
        => GetCapability(request).DownloadFileSegmentedAsync(filePath, options, cancellationToken);

    /// <summary>创建尚未启动的任务；请求配置须在运行期间保持不变，任务不释放调用方请求。</summary>
    public static HttpSegmentedDownloadTask<TResult> CreateSegmentedDownloadTask<TResult>(this IHttpRequest<TResult> request,
        string filePath, HttpSegmentedDownloadOptions options = null) where TResult : class
        => new(GetCapability(request), filePath, options);

    private static IHttpRequestSegmentedDownload<TResult> GetCapability<TResult>(IHttpRequest<TResult> request) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        return request as IHttpRequestSegmentedDownload<TResult>
            ?? throw new NotSupportedException("The HTTP request does not support segmented downloads.");
    }
}
