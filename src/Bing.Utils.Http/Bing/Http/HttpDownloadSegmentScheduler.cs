namespace Bing.Http;

/// <summary>

/// 显式调度多个 HTTP 分片下载并写入分片文件。

/// </summary>
/// <remarks>调度器不会合并最终文件，也不会接入 <c>HttpRequest</c>；成功后由调用方显式调用合并组件。</remarks>
public sealed class HttpDownloadSegmentScheduler
{
    /// <summary>
    /// 读取、写入和提交分片下载状态的文件存储。
    /// </summary>
    private readonly HttpDownloadSegmentFileStore _fileStore;
    /// <summary>
    /// 允许同时下载的最大分片数。
    /// </summary>
    private readonly int _maxConcurrency;
    /// <summary>
    /// 单个分片失败后的最大重试次数。
    /// </summary>
    private readonly int _maxRetries;
    /// <summary>
    /// 分片重试之间的等待时长。
    /// </summary>
    private readonly TimeSpan _retryDelay;
    /// <summary>
    /// 接收分片下载进度的回调。
    /// </summary>
    private readonly Action<HttpDownloadSegmentProgress> _progressObserver;
    /// <summary>
    /// 接收分片最终失败信息的回调。
    /// </summary>
    private readonly Action<HttpDownloadSegmentFailure> _failureObserver;
    /// <summary>
    /// 接收分片重试等待信息的回调。
    /// </summary>
    private readonly Action<HttpRetryScheduled> _retryObserver;
    /// <summary>
    /// 处理分片重试观测异常的回调。
    /// </summary>
    private readonly Action<Exception> _retryObservationErrorHandler;

    /// <summary>

    /// 创建分片下载调度器。

    /// </summary>
    /// <param name="fileStore">分片文件存储。</param>
    /// <param name="maxConcurrency">同时下载的最大分片数。</param>
    /// <param name="maxRetries">每个分片失败后的最大重试次数。</param>
    /// <param name="retryDelay">相邻重试之间的固定等待时间。</param>
    /// <param name="progressObserver">分片成功写入后的进度回调；回调异常会终止调度。</param>
    /// <param name="failureObserver">分片最终失败回调；回调异常会被隔离。</param>
    /// <param name="retryObserver">每次分片重试等待计划回调；回调异常会被隔离。</param>
    /// <param name="retryObservationErrorHandler">重试观察回调异常处理器。</param>
    public HttpDownloadSegmentScheduler(HttpDownloadSegmentFileStore fileStore,
        int maxConcurrency, int maxRetries = 0, TimeSpan? retryDelay = null,
        Action<HttpDownloadSegmentProgress> progressObserver = null,
        Action<HttpDownloadSegmentFailure> failureObserver = null,
        Action<HttpRetryScheduled> retryObserver = null,
        Action<Exception> retryObservationErrorHandler = null)
    {
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        if (maxConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
        if (maxRetries < 0) throw new ArgumentOutOfRangeException(nameof(maxRetries));
        _retryDelay = retryDelay ?? TimeSpan.Zero;
        if (_retryDelay < TimeSpan.Zero || _retryDelay == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(retryDelay));
        _maxConcurrency = maxConcurrency;
        _maxRetries = maxRetries;
        _progressObserver = progressObserver;
        _failureObserver = failureObserver;
        _retryObserver = retryObserver;
        _retryObservationErrorHandler = retryObservationErrorHandler;
    }

    /// <summary>

    /// 下载所有未完成分片并返回新的状态快照。

    /// </summary>
    /// <param name="state">已验证的分片状态。</param>
    /// <param name="download">根据范围创建响应正文流的委托。</param>
    /// <param name="cancellationToken">取消全部分片任务。</param>
    /// <param name="checkpoint">分片持久化后、报告进度前保存状态快照的委托。</param>
    /// <returns>成功和失败分片的状态；失败分片保持未完成。</returns>
    /// <remarks>单片最终失败不会合并目标文件，也不会抛出该单片异常；取消仍抛出 <see cref="OperationCanceledException"/>。</remarks>
    public async Task<HttpDownloadSegmentsState> DownloadAsync(HttpDownloadSegmentsState state,
        Func<HttpDownloadRange, CancellationToken, Task<Stream>> download,
        CancellationToken cancellationToken = default,
        Func<HttpDownloadSegmentsState, CancellationToken, Task> checkpoint = null)
    {
        HttpDownloadSegmentStateStore.Validate(state);
        if (download == null) throw new ArgumentNullException(nameof(download));
        cancellationToken.ThrowIfCancellationRequested();

        var result = Clone(state);
        var completedBytes = result.Segments.Where(segment => segment.Completed)
            .Sum(segment => segment.End - segment.Start + 1);
        using (var gate = new SemaphoreSlim(_maxConcurrency, _maxConcurrency))
        using (var checkpointGate = new SemaphoreSlim(1, 1))
        {
            var tasks = result.Segments.Where(segment => !segment.Completed)
                .Select(segment => DownloadSegmentAsync(segment, gate, download, cancellationToken,
                    state.TotalLength,
                    () => Interlocked.Add(ref completedBytes, segment.End - segment.Start + 1), result,
                    checkpointGate, checkpoint))
                .ToArray();
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>

    /// 下载单个分片并更新持久状态及观测结果。

    /// </summary>
    private async Task DownloadSegmentAsync(HttpDownloadSegmentState segment, SemaphoreSlim gate,
        Func<HttpDownloadRange, CancellationToken, Task<Stream>> download,
        CancellationToken cancellationToken, long totalLength, Func<long> addCompletedBytes,
        HttpDownloadSegmentsState state, SemaphoreSlim checkpointGate,
        Func<HttpDownloadSegmentsState, CancellationToken, Task> checkpoint)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var range = segment.Range;
            for (var attempt = 0; ; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var progressCallbackFailed = false;
                try
                {
                    using (var stream = await download(range, cancellationToken).ConfigureAwait(false))
                    {
                        if (stream == null) throw new InvalidDataException("The segment download returned no stream.");
                        var completed = await _fileStore.WriteSegmentAsync(range, stream, cancellationToken)
                            .ConfigureAwait(false);
                        var previousBytesWritten = segment.BytesWritten;
                        var previousCompleted = segment.Completed;
                        var previousSha256 = segment.Sha256;
                        await checkpointGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try
                        {
                            segment.BytesWritten = completed.BytesWritten;
                            segment.Completed = completed.Completed;
                            segment.Sha256 = completed.Sha256;
                            if (checkpoint != null)
                                await checkpoint(Clone(state), cancellationToken).ConfigureAwait(false);
                        }
                        catch
                        {
                            segment.BytesWritten = previousBytesWritten;
                            segment.Completed = previousCompleted;
                            segment.Sha256 = previousSha256;
                            throw;
                        }
                        finally { checkpointGate.Release(); }

                        var totalCompleted = addCompletedBytes();
                        if (_progressObserver != null)
                        {
                            progressCallbackFailed = true;
                            _progressObserver(new HttpDownloadSegmentProgress(range, attempt + 1,
                                totalCompleted, totalLength));
                            progressCallbackFailed = false;
                        }
                    }
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch when (progressCallbackFailed)
                {
                    throw;
                }
                catch (Exception error) when (attempt < _maxRetries)
                {
                    NotifyRetryScheduled(attempt + 1, _retryDelay, error);
                    if (_retryDelay > TimeSpan.Zero)
                        await Task.Delay(_retryDelay, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    segment.BytesWritten = 0;
                    segment.Completed = false;
                    segment.Sha256 = null;
                    try { _failureObserver?.Invoke(new HttpDownloadSegmentFailure(range, attempt + 1, error)); }
                    catch { }
                    return;
                }
            }
        }
        finally { gate.Release(); }
    }

    /// <summary>

    /// 报告分片下载已安排的重试等待。

    /// </summary>
    private void NotifyRetryScheduled(int failedAttempt, TimeSpan delay, Exception error)
    {
        if (_retryObserver == null) return;
        try { _retryObserver(new HttpRetryScheduled(failedAttempt, delay, null, error)); }
        catch (Exception observationError)
        {
            try { _retryObservationErrorHandler?.Invoke(observationError); }
            catch { }
        }
    }

    /// <summary>

    /// 复制分片清单及其中的分片状态。

    /// </summary>
    private static HttpDownloadSegmentsState Clone(HttpDownloadSegmentsState source)
        => new HttpDownloadSegmentsState
        {
            SchemaVersion = source.SchemaVersion,
            TotalLength = source.TotalLength,
            Validator = source.Validator,
            ContentEncoding = source.ContentEncoding,
            Segments = source.Segments.Select(segment => new HttpDownloadSegmentState
            {
                Start = segment.Start,
                End = segment.End,
                BytesWritten = segment.BytesWritten,
                Completed = segment.Completed,
                Sha256 = segment.Sha256
            }).ToList()
        };
}
