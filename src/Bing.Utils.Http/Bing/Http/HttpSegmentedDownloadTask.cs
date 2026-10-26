namespace Bing.Http;

/// <summary>可管理分片任务的状态。</summary>
public enum HttpSegmentedDownloadTaskStatus
{
    /// <summary>尚未启动。</summary>
    Created,
    /// <summary>正在执行或等待网络。</summary>
    Running,
    /// <summary>已请求取消，正在等待在途工作结束。</summary>
    Pausing,
    /// <summary>已取消且收尾完成；完整检查点可恢复。</summary>
    Paused,
    /// <summary>仍有失败片段，可再次启动重试。</summary>
    Incomplete,
    /// <summary>目标文件已提交。</summary>
    Completed,
    /// <summary>执行异常，可读取 LastError 后决定重试。</summary>
    Failed,
    /// <summary>已显式清除所属检查点。</summary>
    Discarded,
    /// <summary>已释放，保留磁盘检查点。</summary>
    Disposed
}

/// <summary>管理单个目标的分片下载；暂停等待所有在途工作收尾，恢复使用已有磁盘检查点。</summary>
/// <typeparam name="TResult">请求结果类型。</typeparam>
/// <remarks>不拥有请求或共享限速器。运行时不可并发更改请求配置。回调中不得等待 PauseAsync/DisposeAsync，以免等待当前回调自身。</remarks>
public sealed class HttpSegmentedDownloadTask<TResult> : IAsyncDisposable where TResult : class
{
    private readonly object _gate = new();
    private readonly IHttpRequestSegmentedDownload<TResult> _request;
    private readonly HttpSegmentedDownloadOptions _options;
    private CancellationTokenSource _cancellation;
    private Task<HttpDownloadSegmentsState> _operation;
    private HttpSegmentedDownloadTaskStatus _status;
    private Exception _error;
    private bool _disposed;
    private Task _disposeOperation;

    internal HttpSegmentedDownloadTask(IHttpRequestSegmentedDownload<TResult> request, string path, HttpSegmentedDownloadOptions options)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A target path is required.", nameof(path));
        _request = request; FilePath = Path.GetFullPath(path); _options = (options ?? new HttpSegmentedDownloadOptions()).Snapshot();
    }

    /// <summary>获取最终目标完整路径。</summary>
    public string FilePath { get; }
    /// <summary>获取当前状态；Paused 表示已经收尾，Pausing 尚未结束。</summary>
    public HttpSegmentedDownloadTaskStatus Status { get { lock (_gate) return _status; } }
    /// <summary>获取最近一次非取消异常；单片失败通过返回状态和 Failure 回调报告。</summary>
    public Exception LastError { get { lock (_gate) return _error; } }

    /// <summary>启动或恢复任务；运行中、已完成、已丢弃或已释放时拒绝再次启动。</summary>
    public Task<HttpDownloadSegmentsState> StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);
            if (_operation != null && !_operation.IsCompleted || _status == HttpSegmentedDownloadTaskStatus.Completed || _status == HttpSegmentedDownloadTaskStatus.Discarded)
                throw new InvalidOperationException("The task cannot be started in its current state.");
            cancellationToken.ThrowIfCancellationRequested();
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _status = HttpSegmentedDownloadTaskStatus.Running; _error = null;
            var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _operation = RunAsync(start.Task, _cancellation);
            start.SetResult(true);
            return _operation;
        }
    }

    /// <summary>重新启动暂停或失败任务，沿用原配置及磁盘检查点。</summary>
    public Task<HttpDownloadSegmentsState> ResumeAsync(CancellationToken cancellationToken = default) => StartAsync(cancellationToken);

    /// <summary>请求取消并等待操作结束；原执行任务仍返回取消。真实非取消失败正常传播。</summary>
    public async Task PauseAsync()
    {
        Task operation; CancellationTokenSource source;
        lock (_gate)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);
            operation = _operation; source = _cancellation;
            if (operation == null || operation.IsCompleted) return;
            _status = HttpSegmentedDownloadTaskStatus.Pausing;
        }
        Exception cancelError = null;
        try { source?.Cancel(); }
        catch (ObjectDisposedException) { } // 操作可能刚完成 finally，但 Task 尚未标为完成。
        catch (Exception ex) { cancelError = ex; }
        try { await operation.ConfigureAwait(false); }
        catch (OperationCanceledException) when (source != null && source.IsCancellationRequested) { }
        if (cancelError != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cancelError).Throw();
    }

    /// <summary>查询磁盘检查点；运行中的磁盘独占锁冲突会抛出 IOException，运行状态请使用 Status。</summary>
    public HttpSegmentedDownloadSnapshot InspectCheckpoint() => HttpSegmentedDownloadFiles.Inspect(FilePath);

    /// <summary>清除本任务所属分片和检查点，保留最终目标；调用前须完成暂停。</summary>
    public HttpSegmentedDownloadCleanupResult Discard()
    {
        lock (_gate)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);
            if (_operation != null && !_operation.IsCompleted) throw new InvalidOperationException("Pause the task before discarding it.");
            var result = HttpSegmentedDownloadFiles.Discard(FilePath);
            if (result.Errors.Count == 0) _status = HttpSegmentedDownloadTaskStatus.Discarded;
            return result;
        }
    }

    private async Task<HttpDownloadSegmentsState> RunAsync(Task start, CancellationTokenSource source)
    {
        await start.ConfigureAwait(false);
        try
        {
            var result = await _request.DownloadFileSegmentedAsync(FilePath, _options.Snapshot(), source.Token).ConfigureAwait(false);
            lock (_gate) _status = result.Segments.All(x => x.Completed) ? HttpSegmentedDownloadTaskStatus.Completed : HttpSegmentedDownloadTaskStatus.Incomplete;
            return result;
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
            lock (_gate) _status = HttpSegmentedDownloadTaskStatus.Paused;
            throw;
        }
        catch (Exception exception)
        {
            lock (_gate) { _status = HttpSegmentedDownloadTaskStatus.Failed; _error = exception; }
            throw;
        }
        finally
        {
            lock (_gate) { _cancellation = null; source.Dispose(); }
        }
    }

    /// <summary>取消并等待工作结束，保留检查点；运行异常仍保留在原任务及 LastError 中。</summary>
    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (_disposeOperation != null) return new ValueTask(_disposeOperation);
            _disposed = true;
            var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _disposeOperation = DisposeCoreAsync(start.Task, _operation, _cancellation);
            start.SetResult(true);
            return new ValueTask(_disposeOperation);
        }
    }

    private async Task DisposeCoreAsync(Task start, Task operation, CancellationTokenSource source)
    {
        await start.ConfigureAwait(false);
        Exception cancelError = null;
        try { source?.Cancel(); }
        catch (ObjectDisposedException) { }
        catch (Exception ex) { cancelError = ex; }
        if (operation != null) { try { await operation.ConfigureAwait(false); } catch { } }
        lock (_gate)
        {
            _status = HttpSegmentedDownloadTaskStatus.Disposed;
            if (cancelError != null && _error == null) _error = cancelError;
        }
        if (cancelError != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cancelError).Throw();
    }
}
