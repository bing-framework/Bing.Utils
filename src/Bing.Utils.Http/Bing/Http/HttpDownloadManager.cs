using System.Diagnostics;

namespace Bing.Http;

/// <summary>
/// 下载管理器的聚合进度快照。
/// </summary>
public sealed class HttpDownloadManagerProgress
{
    /// <summary>
    /// 获取等待调度的任务数，不包含尚未取得队列容量的调用方。
    /// </summary>
    public int Queued { get; internal set; }
    /// <summary>
    /// 获取正在执行的下载任务数。
    /// </summary>
    public int Running { get; internal set; }
    /// <summary>
    /// 获取已结束任务数，包含失败和取消。
    /// </summary>
    public long Finished { get; internal set; }
    /// <summary>
    /// 获取本管理器各次执行累计确认的分片字节数，恢复提交视为另一次执行。
    /// </summary>
    public long CompletedBytes { get; internal set; }
    /// <summary>
    /// 获取已探测到总长的各次执行累计正文长度，不包含尚未知长度的任务。
    /// </summary>
    public long KnownTotalBytes { get; internal set; }
}

/// <summary>
/// 按全局及主机上限调度下载任务的有界队列。
/// </summary>
/// <remarks>上限约束下载任务，每个任务的分片数由自身 MaxConcurrency 决定。
/// origin 用于主机分组，调用方须传入请求的实际来源；重定向主机不重新分组。
/// 不拥有请求或共享临时存储。释放会取消排队与运行任务并等待收尾，任务须合作响应取消。</remarks>
public sealed class HttpDownloadManager : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly int _maximum, _perHost;
    private readonly SemaphoreSlim _spaces;
    private readonly SemaphoreSlim _signal = new(0, 1);
    private readonly CancellationTokenSource _stop = new();
    private readonly LinkedList<Job> _queue = new();
    private readonly Dictionary<string, int> _hosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Task _dispatcher;
    private int _running;
    private bool _closing;
    private long _finished, _bytes, _total;
    private Task _disposeTask;

    /// <summary>
    /// 创建管理器，队列容量只约束待调度任务；容量耗尽时 EnqueueAsync 等待。
    /// </summary>
    public HttpDownloadManager(int maxConcurrentDownloads = 4, int maxDownloadsPerHost = 2, int queueCapacity = 64)
    {
        if (maxConcurrentDownloads <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrentDownloads));
        if (maxDownloadsPerHost <= 0) throw new ArgumentOutOfRangeException(nameof(maxDownloadsPerHost));
        if (queueCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(queueCapacity));
        _maximum = maxConcurrentDownloads; _perHost = maxDownloadsPerHost; _spaces = new SemaphoreSlim(queueCapacity, queueCapacity);
        _dispatcher = DispatchAsync();
    }

    /// <summary>
    /// 获取或设置聚合快照观察器；回调可能并发，异常被隔离。
    /// </summary>
    public Action<HttpDownloadManagerProgress> Progress { get; set; }

    /// <summary>
    /// 获取当前聚合快照。
    /// </summary>
    /// <returns>独立进度快照。</returns>
    public HttpDownloadManagerProgress GetProgress()
    {
        lock (_gate) return new HttpDownloadManagerProgress { Queued = _queue.Count, Running = _running,
            Finished = _finished, CompletedBytes = _bytes, KnownTotalBytes = _total };
    }

    /// <summary>
    /// 等待队列容量并排队执行一次分片下载，返回该任务的最终结果。
    /// </summary>
    /// <remarks>不会修改调用方 options；排队期间取消不会启动请求。不要并发复用具有可变执行状态的同一个请求实例。</remarks>
    /// <returns>执行完成的结果；异常仅影响本任务。</returns>
    public async Task<HttpSegmentedDownloadResult> EnqueueAsync<TResult>(Uri origin, IHttpRequest<TResult> request, string filePath,
        HttpSegmentedDownloadOptions options = null, CancellationToken cancellationToken = default) where TResult : class
    {
        if (origin == null || !origin.IsAbsoluteUri || (origin.Scheme != "http" && origin.Scheme != "https"))
            throw new ArgumentException("An absolute HTTP origin is required.", nameof(origin));
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("A target is required.", nameof(filePath));
        var config = (options ?? new HttpSegmentedDownloadOptions()).Snapshot(); var path = Path.GetFullPath(filePath);
        lock (_gate) { if (_closing) throw new ObjectDisposedException(nameof(HttpDownloadManager)); }
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stop.Token);
        try { await _spaces.WaitAsync(lifetime.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { throw new OperationCanceledException(cancellationToken); }
        var job = new Job { Host = origin.IdnHost, Token = lifetime.Token, StartedAt = Stopwatch.GetTimestamp(), Metrics = config.Metrics };
        var original = config.Progress;
        config.Progress = progress => { Report(job, progress.CompletedBytes, progress.TotalBytes); original?.Invoke(progress); };
        job.Execute = token => request.DownloadFileSegmentedResultAsync(path, config, token);
        using var registration = lifetime.Token.Register(Pulse);
        lock (_gate)
        {
            if (_closing)
            {
                _spaces.Release();
                throw new OperationCanceledException(cancellationToken.IsCancellationRequested ? cancellationToken : lifetime.Token);
            }
            _queue.AddLast(job);
        }
        Pulse(); Notify();
        try { return await job.Completion.Task.ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { throw new OperationCanceledException(cancellationToken); }
    }

    /// <summary>
    /// 选择第一个主机仍有容量的任务，避免同一主机阻塞其他主机。
    /// </summary>
    private async Task DispatchAsync()
    {
        while (true)
        {
            await _signal.WaitAsync().ConfigureAwait(false);
            var start = new List<Job>();
            lock (_gate)
            {
                var node = _queue.First;
                while (node != null)
                {
                    var next = node.Next; var job = node.Value;
                    if (_closing || job.Token.IsCancellationRequested)
                    {
                        _queue.Remove(node); _spaces.Release(); _finished++; job.Completion.TrySetCanceled(job.Token);
                    }
                    else if (_running < _maximum && (!_hosts.TryGetValue(job.Host, out var active) || active < _perHost))
                    {
                        _queue.Remove(node); _spaces.Release(); _running++; _hosts.TryGetValue(job.Host, out active); _hosts[job.Host] = active + 1; start.Add(job);
                    }
                    node = next;
                }
                if (_closing && _queue.Count == 0 && _running == 0) return;
            }
            foreach (var job in start) _ = RunAsync(job);
            Notify();
        }
    }

    /// <summary>
    /// 每个活动下载只有一个执行任务；异常通过该条目的完成源传播。
    /// </summary>
    private async Task RunAsync(Job job)
    {
        try
        {
            job.Token.ThrowIfCancellationRequested(); job.Metrics?.AddQueueTime(job.StartedAt);
            var result = await job.Execute(job.Token).ConfigureAwait(false);
            Report(job, result.State.Segments.Where(x => x.Completed).Sum(x => x.BytesWritten), result.State.TotalLength);
            job.Completion.TrySetResult(result);
        }
        catch (OperationCanceledException) when (job.Token.IsCancellationRequested) { job.Completion.TrySetCanceled(job.Token); }
        catch (Exception error) { job.Completion.TrySetException(error); }
        finally
        {
            lock (_gate) { _running--; _finished++; if (--_hosts[job.Host] == 0) _hosts.Remove(job.Host); }
            Pulse(); Notify();
        }
    }
    private void Report(Job job, long bytes, long total)
    {
        lock (_gate)
        {
            // 回调可乱序，只累积每个任务已确认的最大进度。
            if (bytes > job.Bytes) { _bytes += bytes - job.Bytes; job.Bytes = bytes; }
            if (!job.TotalKnown) { _total += total; job.TotalKnown = true; }
        }
        Notify();
    }
    private void Notify() { try { Progress?.Invoke(GetProgress()); } catch { } }
    private void Pulse() { try { _signal.Release(); } catch (SemaphoreFullException) { } }

    /// <summary>
    /// 幂等地取消任务并等待所有活动操作收尾。
    /// </summary>
    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (_disposeTask != null) return new ValueTask(_disposeTask);
            _closing = true; _disposeTask = CloseAsync(); return new ValueTask(_disposeTask);
        }
    }
    private async Task CloseAsync()
    {
        await Task.Yield();
        Exception error = null;
        try { _stop.Cancel(); } catch (Exception exception) { error = exception; }
        Pulse(); await _dispatcher.ConfigureAwait(false);
        // 并发等待队列容量的调用方仍持有取消注册；不在这里释放共享同步对象。
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
    private sealed class Job
    {
        internal string Host;
        internal CancellationToken Token;
        internal long StartedAt, Bytes;
        internal bool TotalKnown;
        internal HttpTransferMetrics Metrics;
        internal Func<CancellationToken, Task<HttpSegmentedDownloadResult>> Execute;
        internal readonly TaskCompletionSource<HttpSegmentedDownloadResult> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
