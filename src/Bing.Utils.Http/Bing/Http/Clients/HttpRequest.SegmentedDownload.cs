using System.Net.Http;

namespace Bing.Http.Clients;

public partial class HttpRequest<TResult> where TResult : class
{
    /// <summary>
    /// 显式执行支持范围请求的并发分片下载。
    /// </summary>
    /// <param name="filePath">最终目标文件路径。</param>
    /// <param name="state">已规划且覆盖完整正文的分片状态。</param>
    /// <param name="readMetadata">执行初始能力探测并返回真实响应元数据的委托。</param>
    /// <param name="downloadSegment">按范围发送真实请求并返回正文流的委托。</param>
    /// <param name="segmentSize">仅用于说明调用方状态规划的分片大小。</param>
    /// <param name="maxConcurrency">最大并发分片数。</param>
    /// <param name="maxRetries">单片最大重试次数。</param>
    /// <param name="retryDelay">单片重试等待时间。</param>
    /// <param name="cancellationToken">取消探测、下载、校验或合并。</param>
    /// <param name="progressObserver">分片成功写入后的进度回调；回调异常会终止调度。</param>
    /// <param name="failureObserver">分片最终失败回调；回调异常会被隔离。</param>
    /// <param name="retryObserver">每次分片重试等待计划回调；回调异常会被隔离。</param>
    /// <param name="retryObservationErrorHandler">重试观察回调异常处理器。</param>
    /// <returns>下载后的分片状态；单片失败时返回未完成状态且不替换目标文件。</returns>
    /// <remarks>该入口不会伪造响应对象，也不会改变默认单流续传 API；调用方负责委托中的真实 HTTP 请求。</remarks>
    public async Task<HttpDownloadSegmentsState> DownloadFileSegmentedAsync(
        string filePath, HttpDownloadSegmentsState state,
        Func<HttpDownloadRange?, CancellationToken, Task<HttpDownloadRangeResponseMetadata>> readMetadata,
        Func<HttpDownloadRange, CancellationToken, Task<Stream>> downloadSegment,
        long segmentSize, int maxConcurrency, int maxRetries = 0,
        TimeSpan? retryDelay = null, CancellationToken cancellationToken = default,
        Action<HttpDownloadSegmentProgress> progressObserver = null,
        Action<HttpDownloadSegmentFailure> failureObserver = null,
        Action<HttpRetryScheduled> retryObserver = null,
        Action<Exception> retryObservationErrorHandler = null)
    {
        if (!HttpMethod.Get.Equals(_httpMethod))
            throw new NotSupportedException("Segmented downloads require the GET method.");
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (readMetadata == null) throw new ArgumentNullException(nameof(readMetadata));
        if (downloadSegment == null) throw new ArgumentNullException(nameof(downloadSegment));
        HttpDownloadSegmentStateStore.ValidatePlan(state, segmentSize);
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = Path.GetFullPath(filePath);
        var targetDirectory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(targetDirectory)) Directory.CreateDirectory(targetDirectory);
        using var downloadLock = AcquireResumeLock(fullPath);

        var statePath = fullPath + ".binghttp.segments.json";
        var stagingDirectory = fullPath + ".binghttp.segments";
        var store = new HttpDownloadSegmentFileStore(stagingDirectory, fullPath);
        var activeState = LoadSegmentState(statePath, state, segmentSize);

        var capability = await HttpDownloadRangeProbe.ProbeAsync(token => readMetadata(null, token),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (capability.Status == HttpDownloadRangeSupportStatus.Unsupported)
            throw new NotSupportedException(capability.Reason);
        if (capability.Status == HttpDownloadRangeSupportStatus.Invalid)
            throw new InvalidDataException(capability.Reason);
        if (capability.TotalLength != activeState.TotalLength)
        {
            activeState = CreateSegmentState(state, capability.TotalLength.Value, segmentSize);
            TryDeleteSegmentState(statePath);
            TryDeleteDirectoryContents(stagingDirectory);
        }

        store.Reconcile(activeState);
        await HttpDownloadSegmentStateStore.SaveAsync(statePath, activeState, cancellationToken).ConfigureAwait(false);
        var scheduler = new HttpDownloadSegmentScheduler(store, maxConcurrency, maxRetries, retryDelay,
            progressObserver, failureObserver, retryObserver, retryObservationErrorHandler);
        var result = await scheduler.DownloadAsync(activeState, downloadSegment, cancellationToken,
            (checkpoint, token) => HttpDownloadSegmentStateStore.SaveAsync(statePath, checkpoint, token))
            .ConfigureAwait(false);
        if (result.Segments.Any(segment => !segment.Completed)) return result;
        await store.MergeAsync(result, _resumableDownloadValidator, cancellationToken).ConfigureAwait(false);
        TryDeleteSegmentState(statePath);
        return result;
    }

    /// <summary>
    /// 加载并校验分片状态；状态无效时删除残留文件并复制请求状态。</summary>
    /// <param name="statePath">分片状态文件路径。</param>
    /// <param name="requested">当前请求规划的分片状态。</param>
    /// <param name="segmentSize">每个分片的目标大小，单位为字节。</param>
    /// <returns>可用于当前请求的分片状态。</returns>
    private static HttpDownloadSegmentsState LoadSegmentState(string statePath, HttpDownloadSegmentsState requested,
        long segmentSize)
    {
        if (!File.Exists(statePath)) return CloneSegmentState(requested);
        try
        {
            var saved = HttpDownloadSegmentStateStore.Load(statePath);
            HttpDownloadSegmentStateStore.ValidatePlan(saved, segmentSize);
            if (saved.TotalLength != requested.TotalLength)
                throw new InvalidDataException("The saved segment plan does not match the requested content length.");
            return saved;
        }
        catch (Exception error) when (error is InvalidDataException || error is NotSupportedException ||
                                      error is System.Text.Json.JsonException)
        {
            TryDeleteSegmentState(statePath);
            TryDeleteDirectoryContents(Path.ChangeExtension(statePath, null));
            return CloneSegmentState(requested);
        }
    }

    /// <summary>
    /// 根据模板和响应长度创建新的分片状态。</summary>
    /// <param name="template">包含请求验证信息的状态模板。</param>
    /// <param name="totalLength">响应正文总长度，单位为字节。</param>
    /// <param name="segmentSize">每个分片的目标大小，单位为字节。</param>
    /// <returns>按响应长度规划的分片状态。</returns>
    private static HttpDownloadSegmentsState CreateSegmentState(HttpDownloadSegmentsState template,
        long totalLength, long segmentSize)
    {
        var state = CloneSegmentState(template);
        state.TotalLength = totalLength;
        state.Segments = HttpDownloadRangePlanner.Create(totalLength, segmentSize)
            .Select(range => new HttpDownloadSegmentState { Start = range.Start, End = range.End }).ToList();
        return state;
    }

    /// <summary>
    /// 复制分片状态及其各分片记录。</summary>
    /// <param name="state">要复制的分片状态。</param>
    /// <returns>字段值与源状态一致的新状态对象。</returns>
    private static HttpDownloadSegmentsState CloneSegmentState(HttpDownloadSegmentsState state)
        => new HttpDownloadSegmentsState
        {
            SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
            TotalLength = state.TotalLength,
            Validator = state.Validator,
            ContentEncoding = state.ContentEncoding,
            Segments = state.Segments.Select(segment => new HttpDownloadSegmentState
            {
                Start = segment.Start,
                End = segment.End,
                BytesWritten = segment.BytesWritten,
                Completed = segment.Completed,
                Sha256 = segment.Sha256
            }).ToList()
        };

    /// <summary>
    /// 尝试删除分片状态文件；文件删除失败时忽略异常。</summary>
    /// <param name="statePath">分片状态文件路径。</param>
    private static void TryDeleteSegmentState(string statePath)
    {
        try { if (File.Exists(statePath)) File.Delete(statePath); }
        catch { }
    }

    /// <summary>
    /// 尝试删除指定目录中的文件，并在目录为空时删除目录。</summary>
    /// <param name="directory">要清理的目录路径。</param>
    private static void TryDeleteDirectoryContents(string directory)
    {
        try
        {
            if (!Directory.Exists(directory)) return;
            foreach (var path in Directory.EnumerateFiles(directory))
            {
                try { File.Delete(path); }
                catch { }
            }
            if (!Directory.EnumerateFileSystemEntries(directory).Any()) Directory.Delete(directory);
        }
        catch { }
    }

    /// <summary>
    /// 使用当前 HttpRequest 的客户端、请求头、拦截器和重试策略执行分片下载。
    /// </summary>
    /// <param name="filePath">最终目标文件路径。</param>
    /// <param name="state">已规划且覆盖完整正文的分片状态。</param>
    /// <param name="segmentSize">分片大小，用于校验状态规划。</param>
    /// <param name="maxConcurrency">最大并发分片数。</param>
    /// <param name="maxRetries">单片最大重试次数。</param>
    /// <param name="retryDelay">单片重试等待时间。</param>
    /// <param name="cancellationToken">取消整个操作。</param>
    /// <param name="progressObserver">分片成功写入后的进度回调；回调异常会终止调度。</param>
    /// <param name="failureObserver">分片最终失败回调；回调异常会被隔离。</param>
    /// <param name="retryObserver">每次分片重试等待计划回调；默认复用当前请求的观察器。</param>
    /// <param name="retryObservationErrorHandler">重试观察回调异常处理器。</param>
    /// <returns>下载后的分片状态。</returns>
    /// <remarks>
    /// 当前请求若注册了 <c>OnSendAfter</c> 会明确拒绝分片下载。该回调绑定单个响应对象，
    /// 而分片下载会产生多个响应并在读取后释放它们，不能安全地伪造一次最终响应。
    /// </remarks>
    public Task<HttpDownloadSegmentsState> DownloadFileSegmentedFromRequestAsync(
        string filePath, HttpDownloadSegmentsState state, long segmentSize, int maxConcurrency,
        int maxRetries = 0, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default,
        Action<HttpDownloadSegmentProgress> progressObserver = null,
        Action<HttpDownloadSegmentFailure> failureObserver = null,
        Action<HttpRetryScheduled> retryObserver = null,
        Action<Exception> retryObservationErrorHandler = null)
    {
        if (SendAfterAction != null || SendAfterWithCancellationAction != null)
            throw new NotSupportedException("Segmented downloads cannot be combined with OnSendAfter.");
        return DownloadFileSegmentedAsync(filePath, state,
            (range, token) =>
            {
                if (range.HasValue) throw new InvalidOperationException("The initial probe does not use a range.");
                return SendSegmentedProbeRequestAsync(token);
            },
            (range, token) => SendSegmentedRangeRequestAsync(range, null, token),
            segmentSize, maxConcurrency, maxRetries, retryDelay, cancellationToken,
            progressObserver, failureObserver, retryObserver ?? _retryObserver,
            retryObservationErrorHandler ?? _retryObservationErrorHandler);
    }

    /// <summary>
    /// 发送用于探测服务器分段下载能力的请求。
    /// </summary>
    private Task<HttpDownloadRangeResponseMetadata> SendSegmentedProbeRequestAsync(CancellationToken cancellationToken)
        => ExecuteAsync(async (response, token) => ReadRangeMetadata(response),
            cancellationToken, nested: true, allowRetries: true);

    /// <summary>
    /// 请求指定范围并返回响应正文流。
    /// </summary>
    private Task<Stream> SendSegmentedRangeRequestAsync(HttpDownloadRange range, long? totalLength,
        CancellationToken cancellationToken)
        => ExecuteAsync(async (response, token) =>
        {
            var metadata = ReadRangeMetadata(response);
            var probe = HttpDownloadRangeProbe.Probe(metadata, range, totalLength);
            if (probe.Status == HttpDownloadRangeSupportStatus.Unsupported)
                throw new NotSupportedException(probe.Reason);
            if (probe.Status == HttpDownloadRangeSupportStatus.Invalid)
                throw new InvalidDataException(probe.Reason);
            if (response.Content == null)
                throw new InvalidDataException("A range response has no content.");
            var output = new MemoryStream();
            using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                await input.CopyToAsync(output, 81920, token).ConfigureAwait(false);
            output.Position = 0;
            return (Stream)output;
        }, cancellationToken, nested: true, allowRetries: true,
        prepareMessage: (message, attempt, client) =>
        {
            message.Headers.Remove("Range");
            message.Headers.TryAddWithoutValidation("Range", range.ToHeaderValue());
        });

    /// <summary>
    /// 从分段响应中读取并校验范围元数据。
    /// </summary>
    private static HttpDownloadRangeResponseMetadata ReadRangeMetadata(HttpResponseMessage response)
    {
        string acceptRanges = null;
        if (response.Headers.TryGetValues("Accept-Ranges", out var values))
            acceptRanges = values.FirstOrDefault();
        return new HttpDownloadRangeResponseMetadata
        {
            StatusCode = response.StatusCode,
            AcceptRanges = acceptRanges,
            ContentLength = response.Content?.Headers.ContentLength,
            ContentRange = response.Content?.Headers.ContentRange
        };
    }
}
