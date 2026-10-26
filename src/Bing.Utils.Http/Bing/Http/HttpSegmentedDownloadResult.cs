namespace Bing.Http;

/// <summary>
/// 一次分片执行的明确结果；异常和取消仍通过任务传播。
/// </summary>
public sealed class HttpSegmentedDownloadResult
{
    /// <summary>
    /// 获取是否所有分片已完成并成功提交最终文件。
    /// </summary>
    public bool IsComplete { get; internal set; }
    /// <summary>
    /// 获取最终目标完整路径。
    /// </summary>
    public string FilePath { get; internal set; }
    /// <summary>
    /// 获取可恢复检查点路径；完成时为 null。
    /// </summary>
    public string RecoveryPath { get; internal set; }
    /// <summary>
    /// 获取执行结束时未完成的分片范围。
    /// </summary>
    public IReadOnlyList<HttpDownloadRange> FailedSegments { get; internal set; }
    /// <summary>
    /// 获取本次最终失败通知；历史失败不会从检查点虚构恢复。
    /// </summary>
    public IReadOnlyList<HttpDownloadSegmentFailure> Failures { get; internal set; }
    /// <summary>
    /// 获取兼容旧调用的分片状态。
    /// </summary>
    public HttpDownloadSegmentsState State { get; internal set; }
}

/// <summary>
/// 保持旧返回值的同时提供明确完成状态。
/// </summary>
public static class HttpSegmentedDownloadResultExtensions
{
    /// <summary>
    /// 执行分片下载并返回完成状态、未完成范围及恢复位置。
    /// </summary>
    /// <returns>正常结束的结果；取消、探测和合并失败仍抛出异常。</returns>
    public static async Task<HttpSegmentedDownloadResult> DownloadFileSegmentedResultAsync<TResult>(this IHttpRequest<TResult> request,
        string filePath, HttpSegmentedDownloadOptions options = null, CancellationToken cancellationToken = default) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("A target is required.", nameof(filePath));
        var path = Path.GetFullPath(filePath);
        var config = (options ?? new HttpSegmentedDownloadOptions()).Snapshot();
        var failures = new List<HttpDownloadSegmentFailure>(); var gate = new object(); var original = config.Failure;
        config.Failure = error => { lock (gate) failures.Add(error); original?.Invoke(error); };
        var state = await request.DownloadFileSegmentedAsync(path, config, cancellationToken).ConfigureAwait(false);
        HttpDownloadSegmentStateStore.Validate(state);
        var failed = state.Segments.Where(segment => !segment.Completed).Select(segment => segment.Range).ToList().AsReadOnly();
        return new HttpSegmentedDownloadResult { FilePath = path, IsComplete = failed.Count == 0,
            RecoveryPath = failed.Count == 0 ? null : path + ".binghttp.segments.json", State = state,
            FailedSegments = failed, Failures = failures.AsReadOnly() };
    }
}
