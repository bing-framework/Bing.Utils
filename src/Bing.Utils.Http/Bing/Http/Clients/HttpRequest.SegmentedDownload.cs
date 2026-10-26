using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using Bing.Http.Clients.Internal;

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
    /// <remarks>该入口不会伪造响应对象，也不会改变默认单流续传 API；调用方负责委托中的真实 HTTP 请求及跨片资源身份一致性。</remarks>
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
        => await DownloadFileSegmentedCoreAsync(filePath, state, readMetadata, downloadSegment, null,
            segmentSize, maxConcurrency, maxRetries, retryDelay, cancellationToken,
            progressObserver, failureObserver, retryObserver, retryObservationErrorHandler).ConfigureAwait(false);

    /// <summary>
    /// 在同一文件提交流程中兼容调用方流委托和直接响应写入。
    /// </summary>
    private async Task<HttpDownloadSegmentsState> DownloadFileSegmentedCoreAsync(
        string filePath, HttpDownloadSegmentsState state,
        Func<HttpDownloadRange?, CancellationToken, Task<HttpDownloadRangeResponseMetadata>> readMetadata,
        Func<HttpDownloadRange, CancellationToken, Task<Stream>> downloadSegment,
        Func<HttpDownloadSegmentFileStore, HttpDownloadRange, HttpDownloadSegmentsState, CancellationToken, Task<HttpDownloadSegmentState>> writeSegment,
        long segmentSize, int maxConcurrency, int maxRetries, TimeSpan? retryDelay, CancellationToken cancellationToken,
        Action<HttpDownloadSegmentProgress> progressObserver, Action<HttpDownloadSegmentFailure> failureObserver,
        Action<HttpRetryScheduled> retryObserver, Action<Exception> retryObservationErrorHandler,
        bool bindRequestIdentity = false, Bing.IO.TemporaryFileStore temporaryStore = null, HttpTransferMetrics metrics = null)
    {
        if (!HttpMethod.Get.Equals(_httpMethod))
            throw new NotSupportedException("Segmented downloads require the GET method.");
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (readMetadata == null) throw new ArgumentNullException(nameof(readMetadata));
        if (downloadSegment == null && writeSegment == null) throw new ArgumentNullException(nameof(downloadSegment));
        HttpDownloadSegmentStateStore.ValidatePlan(state, segmentSize);
        cancellationToken.ThrowIfCancellationRequested();
        var downloadLimit = _maxDownloadBytes;
        EnsureDownloadSize(state.TotalLength, 0, downloadLimit);
        if (maxConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
        if (maxRetries < 0) throw new ArgumentOutOfRangeException(nameof(maxRetries));
        if (retryDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(retryDelay));

        var fullPath = Path.GetFullPath(filePath);
        var targetDirectory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(targetDirectory)) Directory.CreateDirectory(targetDirectory);
        using var downloadLock = AcquireResumeLock(fullPath);

        var statePath = fullPath + ".binghttp.segments.json";
        var stagingDirectory = fullPath + ".binghttp.segments";
        var store = new HttpDownloadSegmentFileStore(stagingDirectory, fullPath)
            { EnforceDiskReservation = temporaryStore != null };

        var probeMetadata = await readMetadata(null, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var capability = HttpDownloadRangeProbe.Probe(probeMetadata);
        if (capability.Status == HttpDownloadRangeSupportStatus.Unsupported)
            throw new NotSupportedException(capability.Reason);
        if (capability.Status == HttpDownloadRangeSupportStatus.Invalid)
            throw new InvalidDataException(capability.Reason);
        EnsureDownloadSize(capability.TotalLength.Value, 0, downloadLimit);
        using var diskReservation = await HttpDownloadDiskReservation.AcquireAsync(fullPath, temporaryStore,
            capability.TotalLength.Value, cancellationToken,
            temporaryStore == null ? 0 : store.GetReservedWorkspaceLength()).ConfigureAwait(false);

        HttpDownloadSegmentsState activeState;
        if (bindRequestIdentity)
        {
            var plannedState = CreateSegmentState(state, capability.TotalLength.Value, segmentSize);
            ApplyRequestIdentity(plannedState, probeMetadata);
            var savedState = LoadSegmentState(statePath, plannedState, segmentSize);
            if (CanResumeAutomaticState(savedState, plannedState))
                activeState = savedState;
            else
            {
                TryDeleteSegmentState(statePath);
                TryDeleteDirectoryContents(stagingDirectory);
                activeState = plannedState;
            }
        }
        else
        {
            activeState = LoadSegmentState(statePath, state, segmentSize);
            if (capability.TotalLength != activeState.TotalLength)
            {
                activeState = CreateSegmentState(state, capability.TotalLength.Value, segmentSize);
                TryDeleteSegmentState(statePath);
                TryDeleteDirectoryContents(stagingDirectory);
            }
        }

        store.Reconcile(activeState);
        if (temporaryStore != null) store.PrepareReservedWorkspace(activeState);
        await SaveCheckpoint(activeState, cancellationToken).ConfigureAwait(false);
        var scheduler = new HttpDownloadSegmentScheduler(store, maxConcurrency, maxRetries, retryDelay,
            progressObserver, failureObserver, retryObserver, retryObservationErrorHandler);
        Func<HttpDownloadSegmentsState, CancellationToken, Task> save = SaveCheckpoint;
        var result = writeSegment == null
            ? await scheduler.DownloadAsync(activeState, downloadSegment, cancellationToken, save).ConfigureAwait(false)
            : await scheduler.DownloadToStoreAsync(activeState,
                (range, token) => writeSegment(store, range, activeState, token), cancellationToken, save).ConfigureAwait(false);
        if (result.Segments.Any(segment => !segment.Completed)) return result;
        var mergeStart = System.Diagnostics.Stopwatch.GetTimestamp();
        try { await store.MergeAsync(result, _resumableDownloadValidator, cancellationToken).ConfigureAwait(false); }
        finally { metrics?.AddMergeTime(mergeStart); }
        TryDeleteSegmentState(statePath);
        // 清理失败时保留额度；不能以成功提交为由低估仍占用的临时正文。
        if (!Directory.Exists(stagingDirectory) || !Directory.EnumerateFileSystemEntries(stagingDirectory).Any())
            diskReservation?.Release();
        return result;

        async Task SaveCheckpoint(HttpDownloadSegmentsState checkpoint, CancellationToken token)
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            try { await HttpDownloadSegmentStateStore.SaveAsync(statePath, checkpoint, token).ConfigureAwait(false); }
            finally { metrics?.AddCheckpointTime(start); }
        }
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
            RequestIdentity = state.RequestIdentity,
            RequestFingerprint = state.RequestFingerprint,
            ValidatorKind = state.ValidatorKind,
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
    /// 自动入口优先使用强 ETag，其次使用 Last-Modified，并在每片请求中发送 If-Range。
    /// 服务端不提供可用验证器时，允许完成本次下载，但不会复用后续调用的已完成分片。
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
        return DownloadFileSegmentedCoreAsync(filePath, state,
            (range, token) =>
            {
                if (range.HasValue) throw new InvalidOperationException("The initial probe does not use a range.");
                return SendSegmentedProbeRequestAsync(token);
            },
            null, (store, range, currentState, token) => SendSegmentedRangeToStoreAsync(store, range, currentState.TotalLength, currentState, token),
            segmentSize, maxConcurrency, maxRetries, retryDelay, cancellationToken,
            progressObserver, failureObserver, retryObserver ?? _retryObserver,
            retryObservationErrorHandler ?? _retryObservationErrorHandler, bindRequestIdentity: true);
    }

    /// <summary>
    /// 发送用于探测服务器分段下载能力的请求。
    /// </summary>
    private Task<HttpDownloadRangeResponseMetadata> SendSegmentedProbeRequestAsync(CancellationToken cancellationToken)
    {
        HttpClient requestClient = null;
        return ExecuteAsync((response, token) => Task.FromResult(ReadRangeMetadata(response, requestClient)),
            cancellationToken, nested: true, allowRetries: true,
            prepareMessage: (message, attempt, client) => requestClient = client);
    }

    /// <summary>
    /// 请求指定范围并在响应关闭前直接提交分片。
    /// </summary>
    private Task<HttpDownloadSegmentState> SendSegmentedRangeToStoreAsync(HttpDownloadSegmentFileStore store, HttpDownloadRange range, long? totalLength,
        HttpDownloadSegmentsState expectedIdentity, CancellationToken cancellationToken, Bing.IO.TransferRateLimiter rateLimiter = null, HttpTransferMetrics metrics = null)
    {
        HttpClient requestClient = null;
        return ExecuteAsync(async (response, token) =>
        {
            var metadata = ReadRangeMetadata(response, requestClient);
            var probe = HttpDownloadRangeProbe.Probe(metadata, range, totalLength);
            if (probe.Status == HttpDownloadRangeSupportStatus.Unsupported)
                throw new NotSupportedException(probe.Reason);
            if (probe.Status == HttpDownloadRangeSupportStatus.Invalid)
                throw new InvalidDataException(probe.Reason);
            ValidateSegmentResponseIdentity(expectedIdentity, metadata);
            if (response.Content == null)
                throw new InvalidDataException("A range response has no content.");
            if (EffectiveMaxResponseBytes.HasValue && response.Content.Headers.ContentLength > EffectiveMaxResponseBytes.Value)
                throw new HttpResponseSizeLimitException(EffectiveMaxResponseBytes.Value);
            // 响应保持存活，直接写入同目录分片临时文件，不缓存整个分片。
            var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var limited = new ResponseBodyReadStream(source, EffectiveMaxResponseBytes);
            using var counted = metrics == null ? null : new HttpTransferReadStream(limited, metrics);
            var input = (Stream)counted ?? limited;
            using var throttled = rateLimiter == null ? null : new Bing.IO.RateLimitedStream(input, rateLimiter, true, metrics != null);
            try { return await store.WriteSegmentAsync(range, (Stream)throttled ?? input, token).ConfigureAwait(false); }
            finally { if (throttled != null) metrics?.AddThrottleTime(throttled.ThrottleWaitTime); }
        }, cancellationToken, nested: true, allowRetries: true,
        prepareMessage: (message, attempt, client) =>
        {
            requestClient = client;
            message.Headers.Remove("Range");
            message.Headers.TryAddWithoutValidation("Range", range.ToHeaderValue());
            message.Headers.Remove("If-Range");
            var validator = GetIfRange(expectedIdentity);
            if (validator != null) message.Headers.TryAddWithoutValidation("If-Range", validator);
        });
    }

    /// <summary>
    /// 从分段响应中读取并校验范围元数据。
    /// </summary>
    private static HttpDownloadRangeResponseMetadata ReadRangeMetadata(HttpResponseMessage response, HttpClient client = null)
    {
        string acceptRanges = null;
        if (response.Headers.TryGetValues("Accept-Ranges", out var values))
            acceptRanges = values.FirstOrDefault();
        var encodings = response.Content?.Headers.ContentEncoding;
        var contentEncoding = encodings == null
            ? string.Empty
            : string.Join(",", encodings.Select(value => value.Trim().ToLowerInvariant()));
        return new HttpDownloadRangeResponseMetadata
        {
            StatusCode = response.StatusCode,
            AcceptRanges = acceptRanges,
            ContentLength = response.Content?.Headers.ContentLength,
            ContentRange = response.Content?.Headers.ContentRange,
            ETag = response.Headers.ETag?.ToString(),
            LastModified = response.Content?.Headers.LastModified?.ToString("R", CultureInfo.InvariantCulture),
            ContentEncoding = contentEncoding,
            RequestIdentity = BuildRequestIdentity(response.RequestMessage, client),
            RequestFingerprint = BuildRequestFingerprint(response.RequestMessage, client)
        };
    }

    /// <summary>
    /// 将探测响应中的请求与表示身份固定到自动续传状态。
    /// </summary>
    private static void ApplyRequestIdentity(HttpDownloadSegmentsState state, HttpDownloadRangeResponseMetadata metadata)
    {
        state.RequestIdentity = metadata.RequestIdentity;
        state.RequestFingerprint = metadata.RequestFingerprint;
        state.ContentEncoding = metadata.ContentEncoding ?? string.Empty;
        state.Validator = null;
        state.ValidatorKind = null;

        if (EntityTagHeaderValue.TryParse(metadata.ETag, out var entityTag) && !entityTag.IsWeak)
        {
            state.Validator = entityTag.ToString();
            state.ValidatorKind = SegmentValidatorETag;
        }
        else if (!string.IsNullOrWhiteSpace(metadata.LastModified))
        {
            state.Validator = metadata.LastModified;
            state.ValidatorKind = SegmentValidatorLastModified;
        }
    }

    /// <summary>
    /// 检查已保存状态是否绑定到当前请求及可靠的远端验证器。
    /// </summary>
    private static bool CanResumeAutomaticState(HttpDownloadSegmentsState saved, HttpDownloadSegmentsState current)
        => IsReliableAutomaticIdentity(current) && IsReliableAutomaticIdentity(saved) &&
           saved.TotalLength == current.TotalLength &&
           string.Equals(saved.RequestIdentity, current.RequestIdentity, StringComparison.Ordinal) &&
           string.Equals(saved.RequestFingerprint, current.RequestFingerprint, StringComparison.Ordinal) &&
           string.Equals(saved.ValidatorKind, current.ValidatorKind, StringComparison.Ordinal) &&
           string.Equals(saved.Validator, current.Validator, StringComparison.Ordinal) &&
           string.Equals(saved.ContentEncoding, current.ContentEncoding, StringComparison.Ordinal);

    private const string SegmentValidatorETag = "ETag";
    private const string SegmentValidatorLastModified = "Last-Modified";

    private static bool IsReliableAutomaticIdentity(HttpDownloadSegmentsState state)
        => !string.IsNullOrWhiteSpace(state.RequestIdentity) &&
           !string.IsNullOrWhiteSpace(state.RequestFingerprint) &&
           !string.IsNullOrWhiteSpace(state.Validator) &&
           (state.ValidatorKind == SegmentValidatorETag || state.ValidatorKind == SegmentValidatorLastModified);

    private static string GetIfRange(HttpDownloadSegmentsState state)
        => !string.IsNullOrWhiteSpace(state.Validator) &&
           (state.ValidatorKind == SegmentValidatorETag || state.ValidatorKind == SegmentValidatorLastModified)
            ? state.Validator : null;

    /// <summary>
    /// 拒绝与初始探测不一致的自动范围响应，避免混合不同请求或资源版本的分片。
    /// </summary>
    private static void ValidateSegmentResponseIdentity(HttpDownloadSegmentsState expected,
        HttpDownloadRangeResponseMetadata actual)
    {
        if (!string.Equals(expected.RequestIdentity, actual.RequestIdentity, StringComparison.Ordinal))
            throw new InvalidDataException("The range response request identity changed during the segmented download.");
        if (!string.Equals(expected.RequestFingerprint, actual.RequestFingerprint, StringComparison.Ordinal))
            throw new InvalidDataException("The range response request headers changed during the segmented download.");
        if (!string.Equals(expected.ContentEncoding ?? string.Empty, actual.ContentEncoding ?? string.Empty, StringComparison.Ordinal))
            throw new InvalidDataException("The range response content encoding changed during the segmented download.");
        if (expected.ValidatorKind == SegmentValidatorETag &&
            !string.Equals(expected.Validator, actual.ETag, StringComparison.Ordinal))
            throw new InvalidDataException("The range response ETag changed or is missing.");
        if (expected.ValidatorKind == SegmentValidatorLastModified &&
            !string.Equals(expected.Validator, actual.LastModified, StringComparison.Ordinal))
            throw new InvalidDataException("The range response Last-Modified value changed or is missing.");
    }
}
