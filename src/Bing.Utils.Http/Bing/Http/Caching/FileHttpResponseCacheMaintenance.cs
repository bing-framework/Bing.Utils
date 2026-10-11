using System.Threading;

namespace Bing.Http.Caching;

/// <summary>

/// 显式调度文件 HTTP 响应缓存清理的后台维护器。

/// </summary>
/// <remarks>
/// 创建实例不会启动后台任务；调用 <see cref="Start"/> 后才会立即执行一次清理并按周期重复。
/// 单次清理异常通过回调报告后继续运行，取消或释放实例会停止维护器。
/// </remarks>
public sealed class FileHttpResponseCacheMaintenance : IDisposable
{
    /// <summary>
    /// 执行单轮缓存清理的委托。
    /// </summary>
    private readonly Func<CancellationToken, Task> _cleanup;
    /// <summary>
    /// 相邻清理轮次开始时间之间的间隔。
    /// </summary>
    private readonly TimeSpan _interval;
    /// <summary>
    /// 清理时使用的临时文件保留时间。
    /// </summary>
    private readonly TimeSpan _staleAfter;
    /// <summary>
    /// 清理失败时调用的可选错误观察器。
    /// </summary>
    private readonly Action<Exception> _onError;
    /// <summary>
    /// 保护维护器运行状态的锁。
    /// </summary>
    private readonly object _gate = new object();
    /// <summary>
    /// 当前后台任务使用的取消源。
    /// </summary>
    private CancellationTokenSource _source;
    /// <summary>
    /// 当前后台清理任务；未运行时为空。
    /// </summary>
    private Task _worker;
    /// <summary>
    /// 指示维护器是否已释放。
    /// </summary>
    private bool _disposed;

    /// <summary>

    /// 初始化一个<see cref="FileHttpResponseCacheMaintenance"/>类型的实例。

    /// </summary>
    /// <param name="cache">要定期清理的文件缓存。</param>
    /// <param name="interval">两次清理开始之间的周期；必须为正数且为有限值。</param>
    /// <param name="staleAfter">传给缓存清理方法的过期保留时间。</param>
    /// <param name="onError">单次清理失败时的可选观察回调；回调异常会被忽略。</param>
    public FileHttpResponseCacheMaintenance(FileHttpResponseCache cache, TimeSpan interval,
        TimeSpan staleAfter, Action<Exception> onError = null)
    {
        if (cache == null) throw new ArgumentNullException(nameof(cache));
        if (interval <= TimeSpan.Zero || interval == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(interval));
        if (staleAfter < TimeSpan.Zero || staleAfter == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(staleAfter));
        _interval = interval;
        _staleAfter = staleAfter;
        _onError = onError;
        _cleanup = token => cache.CleanupAsync(_staleAfter, token);
    }

    /// <summary>

    /// 初始化一个<see cref="FileHttpResponseCacheMaintenance"/>类型的实例。

    /// </summary>
    /// <param name="cleanup">单次清理委托。</param>
    /// <param name="interval">两次清理开始之间的周期。</param>
    /// <param name="onError">清理失败时的观察回调。</param>
    internal FileHttpResponseCacheMaintenance(Func<CancellationToken, Task> cleanup,
        TimeSpan interval, Action<Exception> onError = null)
    {
        _cleanup = cleanup ?? throw new ArgumentNullException(nameof(cleanup));
        if (interval <= TimeSpan.Zero || interval == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(interval));
        _interval = interval;
        _staleAfter = TimeSpan.Zero;
        _onError = onError;
    }

    /// <summary>

    /// 获取维护器当前是否正在运行。

    /// </summary>
    public bool IsRunning
    {
        get { lock (_gate) return _worker != null; }
    }

    /// <summary>

    /// 启动后台清理；重复调用不会创建第二个任务。

    /// </summary>
    public void Start()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_worker != null) return;
            _source = new CancellationTokenSource();
            _worker = Task.Run(() => RunAsync(_source.Token));
        }
    }

    /// <summary>

    /// 停止后台清理并等待当前清理轮次结束；重复调用安全。

    /// </summary>
    public void Stop()
    {
        Task worker;
        CancellationTokenSource source;
        lock (_gate)
        {
            worker = _worker;
            source = _source;
        }

        if (worker == null) return;
        source.Cancel();
        try { worker.GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { }

        lock (_gate)
        {
            if (ReferenceEquals(_worker, worker))
            {
                _worker = null;
                _source = null;
                source.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
        }
        Stop();
    }

    /// <summary>

    /// 按配置间隔执行后台缓存清理任务。

    /// </summary>
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                await _cleanup(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception error)
            {
                try { _onError?.Invoke(error); }
                catch { }
            }

            try { await Task.Delay(_interval, cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    /// <summary>

    /// 对象已释放时抛出异常。

    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(FileHttpResponseCacheMaintenance));
    }
}
