namespace Bing.IO;

/// <summary>
/// 文件监控器。
/// </summary>
public class FileWatcher : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 接受文件监控信息的事件委托。
    /// </summary>
    /// <param name="sender">事件发送器</param>
    /// <param name="args">文件监控事件参数</param>
    public delegate void FileWatcherEventHandler(object sender, FileWatcherEventArgs args);

    private static readonly StringComparer PathComparer = Path.DirectorySeparatorChar == '\\'
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private readonly object _gate = new();
    private readonly Dictionary<string, WatchRegistration> _watchers = new(PathComparer);
    private readonly Dictionary<long, RunState> _runs = new();
    private readonly Dictionary<long, CallbackTracker> _callbackTrackers = new();
    private readonly Dictionary<string, PendingEvent> _pendingEvents = new(PathComparer);
    private readonly HashSet<string> _rescanPaths = new(PathComparer);
    private readonly SemaphoreSlim _wakeSignal = new(0, 1);
    private readonly FileWatcherOptions _options;
    private readonly Func<string, IReadOnlyDictionary<string, FileWatcherSnapshotEntry>> _snapshotProvider;
    private long _generation;
    private bool _isWatching;
    private bool _disposed;

    /// <summary>
    /// 获取文件监控信息。
    /// </summary>
    public FileWatcherEventHandler EventHandler;

    /// <summary>
    /// 在监控发生错误时触发。
    /// </summary>
    public event EventHandler<FileWatcherErrorEventArgs> Error;

    /// <summary>
    /// 获取文件更改监控是否已启动。
    /// </summary>
    public bool IsWatching
    {
        get
        {
            lock (_gate)
            {
                return _isWatching;
            }
        }
        private set => _isWatching = value;
    }

    /// <summary>
    /// 初始化一个 <see cref="FileWatcher"/> 类型的实例。
    /// </summary>
    /// <param name="paths">要监控的目录</param>
    public FileWatcher(string[] paths)
        : this(paths, new FileWatcherOptions())
    {
    }

    /// <summary>
    /// 使用指定选项初始化一个 <see cref="FileWatcher"/> 类型的实例。
    /// </summary>
    /// <param name="paths">要监控的目录</param>
    /// <param name="options">监控选项</param>
    public FileWatcher(string[] paths, FileWatcherOptions options)
        : this(paths, options, CreateSnapshot)
    {
    }

    internal FileWatcher(string[] paths, FileWatcherOptions options,
        Func<string, IReadOnlyDictionary<string, FileWatcherSnapshotEntry>> snapshotProvider)
    {
        _options = (options ?? throw new ArgumentNullException(nameof(options))).CreateSnapshot();
        _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));

        if (paths == null)
        {
            return;
        }

        foreach (var path in paths)
        {
            AddPath(path);
        }
    }

    /// <summary>
    /// 添加目录到监控列表。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <returns>成功添加时返回 true</returns>
    public bool AddPath(string path)
    {
        if (!TryNormalizePath(path, out var fullPath) || !Directory.Exists(fullPath))
        {
            return false;
        }

        lock (_gate)
        {
            ThrowIfDisposed();
            if (_watchers.ContainsKey(fullPath))
            {
                return false;
            }

            _watchers.Add(fullPath, new WatchRegistration(fullPath));
        }

        SignalWorker();
        return true;
    }

    /// <summary>
    /// 从监控列表中删除目录并释放对应监控器。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <returns>成功删除时返回 true</returns>
    public bool DeletePath(string path)
    {
        if (!TryNormalizePath(path, out var fullPath))
        {
            return false;
        }

        FileSystemWatcher watcher;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (!_watchers.TryGetValue(fullPath, out var registration))
            {
                return false;
            }

            _watchers.Remove(fullPath);
            watcher = registration.Watcher;
            registration.Watcher = null;
            _rescanPaths.Remove(fullPath);
            foreach (var key in _pendingEvents.Where(item => PathComparer.Equals(item.Value.RootPath, fullPath)).Select(item => item.Key).ToArray())
                _pendingEvents.Remove(key);
            registration.RescanCompletion?.TrySetCanceled();
        }

        DisposeWatcher(watcher);
        SignalWorker();
        return true;
    }

    /// <summary>
    /// 启动文件监控；重复调用不会创建重复后台任务。
    /// </summary>
    public void Start()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (IsWatching)
            {
                return;
            }

            IsWatching = true;
            _generation++;
            var generation = _generation;
            var tracker = new CallbackTracker(generation);
            _callbackTrackers[generation] = tracker;
            var run = new RunState(generation, new CancellationTokenSource());
            _runs[generation] = run;
            run.Task = Task.Run(() => RunLoopAsync(run));
        }
    }

    /// <summary>
    /// 发起停止并清空监控目录；此方法不等待后台任务和在途回调结束。
    /// </summary>
    public void Stop()
    {
        _ = BeginStop(false);
    }

    /// <summary>
    /// 清空监控目录并等待后台任务及已进入的回调全部结束。
    /// </summary>
    /// <returns>表示停止和回调收尾的异步操作</returns>
    public async Task StopAsync()
    {
        var stop = BeginStop(false);
        await WaitForStopAsync(stop).ConfigureAwait(false);
    }

    /// <summary>
    /// 释放监控器并发起后台任务停止。
    /// </summary>
    public void Dispose()
    {
        _ = BeginStop(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放监控器并等待后台任务及在途回调结束。
    /// </summary>
    /// <returns>表示异步释放的值任务</returns>
    public async ValueTask DisposeAsync()
    {
        var stop = BeginStop(true);
        await WaitForStopAsync(stop).ConfigureAwait(false);
        _wakeSignal.Dispose();
        GC.SuppressFinalize(this);
    }

    internal void NotifyForTesting(string rootPath, FileWatcherEventArgs args)
    {
        OnFileNotification(_generation, this, args, rootPath);
    }

    internal bool IsWatcherAttachedForTesting(string path)
    {
        if (!TryNormalizePath(path, out var fullPath))
        {
            return false;
        }

        lock (_gate)
        {
            return _watchers.TryGetValue(fullPath, out var registration) && registration.Watcher != null;
        }
    }

    internal Task NotifyOverflowForTestingAsync(string rootPath)
    {
        var generation = _generation;
        var path = NormalizePath(rootPath);
        var completion = QueueRescan(generation, path);
        PublishError(generation, path, new InternalBufferOverflowException(), true);
        return completion ?? Task.CompletedTask;
    }

    private async Task RunLoopAsync(RunState run)
    {
        var token = run.Cancellation.Token;
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                RefreshWatchers(run.Generation, token);
                ProcessRequestedRescans(run.Generation, token);
                FlushDueEvents(run.Generation);

                var delay = GetNextDelay();
                using var waitCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                var signalTask = _wakeSignal.WaitAsync(waitCancellation.Token);
                var delayTask = Task.Delay(delay, waitCancellation.Token);
                var completed = await Task.WhenAny(signalTask, delayTask).ConfigureAwait(false);
                waitCancellation.Cancel();
                try
                {
                    await completed.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    // 取消未获胜的等待分支。
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            PublishError(run.Generation, null, exception, false);
            DeactivateGeneration(run.Generation);
        }
        finally
        {
            lock (_gate)
            {
                _runs.Remove(run.Generation);
                if (_callbackTrackers.TryGetValue(run.Generation, out var tracker) &&
                    !tracker.Accepting && tracker.ActiveCallbacks == 0)
                {
                    _callbackTrackers.Remove(run.Generation);
                }
            }

            run.Cancellation.Dispose();
        }
    }

    private void RefreshWatchers(long generation, CancellationToken cancellationToken)
    {
        WatchRegistration[] registrations;
        lock (_gate)
        {
            if (!IsGenerationActiveLocked(generation))
            {
                return;
            }

            registrations = _watchers.Values.ToArray();
        }

        foreach (var registration in registrations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(registration.Path))
            {
                FileSystemWatcher stale = null;
                lock (_gate)
                {
                    if (_watchers.TryGetValue(registration.Path, out var current) &&
                        ReferenceEquals(current, registration) && current.Watcher != null)
                    {
                        stale = current.Watcher;
                        current.Watcher = null;
                    }
                }

                DisposeWatcher(stale);
                continue;
            }

            lock (_gate)
            {
                if (!_watchers.TryGetValue(registration.Path, out var current) ||
                    !ReferenceEquals(current, registration) || current.Watcher != null ||
                    !IsGenerationActiveLocked(generation))
                {
                    continue;
                }
            }

            FileSystemWatcher candidate = null;
            try
            {
                candidate = CreateWatcher(registration.Path, generation);
                candidate.EnableRaisingEvents = true;
                var snapshot = _snapshotProvider(registration.Path);
                IReadOnlyDictionary<string, FileWatcherSnapshotEntry> previous = null;
                var installed = false;
                lock (_gate)
                {
                    if (_watchers.TryGetValue(registration.Path, out var current) &&
                        ReferenceEquals(current, registration) && current.Watcher == null &&
                        IsGenerationActiveLocked(generation))
                    {
                        previous = current.Snapshot;
                        current.Snapshot = snapshot;
                        current.Watcher = candidate;
                        installed = true;
                        candidate = null;
                    }
                }

                DisposeWatcher(candidate);
                if (installed && previous != null)
                {
                    PublishSnapshotDifference(generation, registration.Path, previous, snapshot);
                }
            }
            catch (Exception exception)
            {
                DisposeWatcher(candidate);
                PublishError(generation, registration.Path, exception, false);
            }
        }
    }

    private FileSystemWatcher CreateWatcher(string path, long generation)
    {
        var watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            NotifyFilter = (NotifyFilters)383,
            InternalBufferSize = _options.InternalBufferSize,
            EnableRaisingEvents = false
        };

        watcher.Created += (sender, args) => OnFileNotification(generation, sender,
            CreateEventArgs(args.ChangeType, args.FullPath, null, null), path);
        watcher.Changed += (sender, args) => OnFileNotification(generation, sender,
            CreateEventArgs(args.ChangeType, args.FullPath, null, null), path);
        watcher.Deleted += (sender, args) => OnFileNotification(generation, sender,
            CreateEventArgs(args.ChangeType, args.FullPath, null, null), path);
        watcher.Renamed += (sender, args) => OnFileNotification(generation, sender,
            CreateEventArgs(args.ChangeType, args.FullPath, args.OldFullPath, args.OldName), path);
        watcher.Error += (_, args) => OnWatcherError(generation, path, args.GetException());
        return watcher;
    }

    private void OnFileNotification(long generation, object sender, FileWatcherEventArgs args, string rootPath)
    {
        lock (_gate)
        {
            if (!IsGenerationActiveLocked(generation) || !_watchers.ContainsKey(rootPath)) return;
        }
        if (_options.DebounceInterval == null)
        {
            InvokeEvent(generation, sender, args);
            return;
        }

        if (!TryEnterCallback(generation))
        {
            return;
        }

        var overflowed = false;
        try
        {
            lock (_gate)
            {
                if (!IsGenerationActiveLocked(generation))
                {
                    return;
                }

                var key = NormalizePath(args.FullPath);
                if (_pendingEvents.TryGetValue(key, out var pending))
                {
                    pending.Args = MergeEvents(pending.Args, args);
                    pending.DueAtUtc = DateTime.UtcNow + _options.DebounceInterval.Value;
                }
                else if (_pendingEvents.Count < _options.MaxPendingEvents)
                {
                    _pendingEvents.Add(key,
                        new PendingEvent(rootPath, args, DateTime.UtcNow + _options.DebounceInterval.Value));
                }
                else
                {
                    overflowed = true;
                }
            }
        }
        finally
        {
            ExitCallback(generation);
        }

        if (overflowed)
        {
            QueueRescan(generation, rootPath);
            PublishError(generation, rootPath,
                new InternalBufferOverflowException("待处理事件达到配置上限，已请求目录快照重扫。"), true);
        }

        SignalWorker();
    }

    private void OnWatcherError(long generation, string path, Exception exception)
    {
        var overflow = exception is InternalBufferOverflowException;
        if (overflow)
        {
            QueueRescan(generation, path);
        }

        PublishError(generation, path, exception ?? new IOException("文件监控发生未知错误。"), overflow);
    }

    private void InvokeEvent(long generation, object sender, FileWatcherEventArgs args)
    {
        if (!TryEnterCallback(generation))
        {
            return;
        }

        try
        {
            var handlers = EventHandler;
            if (handlers == null)
            {
                return;
            }

            foreach (FileWatcherEventHandler handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception exception)
                {
                    PublishError(generation, args.FullPath, exception, false);
                }
            }
        }
        finally
        {
            ExitCallback(generation);
        }
    }

    private void PublishError(long generation, string path, Exception exception, bool bufferOverflow)
    {
        if (!TryEnterCallback(generation))
        {
            return;
        }

        try
        {
            var handlers = Error;
            if (handlers == null)
            {
                return;
            }

            var args = new FileWatcherErrorEventArgs(path, exception, bufferOverflow);
            foreach (EventHandler<FileWatcherErrorEventArgs> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(this, args);
                }
                catch
                {
                    // 错误订阅者异常不能再次递归触发 Error。
                }
            }
        }
        finally
        {
            ExitCallback(generation);
        }
    }

    private bool TryEnterCallback(long generation)
    {
        lock (_gate)
        {
            if (!_callbackTrackers.TryGetValue(generation, out var tracker) || !tracker.Accepting)
            {
                return false;
            }

            if (tracker.ActiveCallbacks++ == 0)
            {
                tracker.Drained = NewSignal();
            }

            return true;
        }
    }

    private void ExitCallback(long generation)
    {
        lock (_gate)
        {
            if (_callbackTrackers.TryGetValue(generation, out var tracker) &&
                --tracker.ActiveCallbacks == 0)
            {
                tracker.Drained.TrySetResult(true);
                if (!tracker.Accepting)
                {
                    _callbackTrackers.Remove(generation);
                }
            }
        }
    }

    private Task QueueRescan(long generation, string path)
    {
        Task task = null;
        lock (_gate)
        {
            if (!IsGenerationActiveLocked(generation) || !_watchers.TryGetValue(path, out var registration))
            {
                return null;
            }

            _rescanPaths.Add(path);
            registration.RescanCompletion ??= NewSignal();
            task = registration.RescanCompletion.Task;
        }

        SignalWorker();
        return task;
    }

    private void ProcessRequestedRescans(long generation, CancellationToken cancellationToken)
    {
        string[] paths;
        lock (_gate)
        {
            if (!IsGenerationActiveLocked(generation) || _rescanPaths.Count == 0)
            {
                return;
            }

            paths = _rescanPaths.ToArray();
            _rescanPaths.Clear();
        }

        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                RescanPath(generation, path);
            }
            catch (Exception exception)
            {
                PublishError(generation, path, exception, false);
            }
            finally
            {
                lock (_gate)
                {
                    if (_watchers.TryGetValue(path, out var registration))
                    {
                        registration.RescanCompletion?.TrySetResult(true);
                        registration.RescanCompletion = null;
                    }
                }
            }
        }
    }

    private void RescanPath(long generation, string path)
    {
        var next = _snapshotProvider(path);
        IReadOnlyDictionary<string, FileWatcherSnapshotEntry> previous;
        lock (_gate)
        {
            if (!IsGenerationActiveLocked(generation) || !_watchers.TryGetValue(path, out var registration))
            {
                return;
            }

            previous = registration.Snapshot ?? new Dictionary<string, FileWatcherSnapshotEntry>(PathComparer);
            registration.Snapshot = next;
        }

        PublishSnapshotDifference(generation, path, previous, next);
    }

    private void PublishSnapshotDifference(long generation, string rootPath,
        IReadOnlyDictionary<string, FileWatcherSnapshotEntry> previous,
        IReadOnlyDictionary<string, FileWatcherSnapshotEntry> current)
    {
        foreach (var oldItem in previous)
        {
            if (!current.TryGetValue(oldItem.Key, out var newValue))
            {
                InvokeEvent(generation, this, CreateEventArgs(WatcherChangeTypes.Deleted, oldItem.Key, null, null));
            }
            else if (!oldItem.Value.Equals(newValue))
            {
                InvokeEvent(generation, this, CreateEventArgs(WatcherChangeTypes.Changed, oldItem.Key, null, null));
            }
        }

        foreach (var currentItem in current)
        {
            if (!previous.ContainsKey(currentItem.Key))
            {
                // 重扫已经逐项计算差异，直接交付，避免再次挤满去重队列后丢失差异。
                InvokeEvent(generation, this, CreateEventArgs(WatcherChangeTypes.Created, currentItem.Key, null, null));
            }
        }
    }

    private void FlushDueEvents(long generation)
    {
        List<PendingEvent> due;
        var now = DateTime.UtcNow;
        lock (_gate)
        {
            if (!IsGenerationActiveLocked(generation) || _pendingEvents.Count == 0)
            {
                return;
            }

            var dueKeys = _pendingEvents.Where(x => x.Value.DueAtUtc <= now).Select(x => x.Key).ToArray();
            due = new List<PendingEvent>(dueKeys.Length);
            foreach (var key in dueKeys)
            {
                due.Add(_pendingEvents[key]);
                _pendingEvents.Remove(key);
            }
        }

        foreach (var item in due)
        {
            InvokeEvent(generation, this, item.Args);
        }
    }

    private TimeSpan GetNextDelay()
    {
        lock (_gate)
        {
            var delay = _options.ScanInterval;
            if (_options.DebounceInterval.HasValue && _pendingEvents.Count > 0)
            {
                var earliest = _pendingEvents.Values.Min(x => x.DueAtUtc);
                var untilDue = earliest - DateTime.UtcNow;
                if (untilDue < TimeSpan.Zero)
                {
                    untilDue = TimeSpan.Zero;
                }

                if (untilDue < delay)
                {
                    delay = untilDue;
                }
            }

            return delay;
        }
    }

    private void DeactivateGeneration(long generation)
    {
        List<FileSystemWatcher> watchers = new();
        lock (_gate)
        {
            if (generation != _generation)
            {
                return;
            }

            IsWatching = false;
            if (_callbackTrackers.TryGetValue(generation, out var tracker))
            {
                tracker.Accepting = false;
            }

            foreach (var registration in _watchers.Values)
            {
                if (registration.Watcher != null)
                {
                    watchers.Add(registration.Watcher);
                    registration.Watcher = null;
                }
            }

            _pendingEvents.Clear();
            _rescanPaths.Clear();
            foreach (var registration in _watchers.Values)
            {
                registration.RescanCompletion?.TrySetCanceled();
                registration.RescanCompletion = null;
            }
        }

        foreach (var watcher in watchers)
        {
            DisposeWatcher(watcher);
        }
    }

    private StopSnapshot BeginStop(bool dispose)
    {
        List<FileSystemWatcher> watchers = new();
        RunState[] runs;
        Task[] callbackTasks;
        lock (_gate)
        {
            if (dispose)
            {
                _disposed = true;
            }

            foreach (var tracker in _callbackTrackers.Values)
            {
                tracker.Accepting = false;
            }

            IsWatching = false;
            runs = _runs.Values.ToArray();
            callbackTasks = _callbackTrackers.Values.Select(x => x.Drained.Task).ToArray();
            foreach (var registration in _watchers.Values)
            {
                if (registration.Watcher != null)
                {
                    watchers.Add(registration.Watcher);
                    registration.Watcher = null;
                }

                registration.RescanCompletion?.TrySetCanceled();
                registration.RescanCompletion = null;
            }

            // 保留旧 Stop 的目录清空契约；再次启动前由调用方重新 AddPath。
            _watchers.Clear();
            _pendingEvents.Clear();
            _rescanPaths.Clear();
        }

        foreach (var run in runs)
        {
            try
            {
                run.Cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        foreach (var watcher in watchers)
        {
            DisposeWatcher(watcher);
        }

        SignalWorker();
        return new StopSnapshot(runs.Select(x => x.Task).Where(x => x != null).ToArray(), callbackTasks);
    }

    private static async Task WaitForStopAsync(StopSnapshot stop)
    {
        await Task.WhenAll(stop.BackgroundTasks).ConfigureAwait(false);
        await Task.WhenAll(stop.CallbackTasks).ConfigureAwait(false);
    }

    private void SignalWorker()
    {
        try
        {
            _wakeSignal.Release();
        }
        catch (SemaphoreFullException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private bool IsGenerationActiveLocked(long generation)
    {
        return generation == _generation && _callbackTrackers.TryGetValue(generation, out var tracker) &&
               tracker.Accepting;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FileWatcher));
        }
    }

    private static FileWatcherEventArgs CreateEventArgs(WatcherChangeTypes type, string fullPath,
        string oldFullPath, string oldName)
    {
        return new FileWatcherEventArgs(type, fullPath, Path.GetFileName(fullPath), oldFullPath, oldName);
    }

    private static FileWatcherEventArgs MergeEvents(FileWatcherEventArgs first, FileWatcherEventArgs second)
    {
        var oldFullPath = second.OldFullPath ?? first.OldFullPath;
        var oldName = second.OldName ?? first.OldName;
        return new FileWatcherEventArgs(first.ChangeTypes | second.ChangeTypes, second.FullPath, second.Name,
            oldFullPath, oldName);
    }

    private static bool TryNormalizePath(string path, out string fullPath)
    {
        fullPath = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            fullPath = Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizePath(string path)
    {
        return TryNormalizePath(path, out var fullPath) ? fullPath : path;
    }

    private static void DisposeWatcher(FileSystemWatcher watcher)
    {
        if (watcher == null)
        {
            return;
        }

        try
        {
            watcher.EnableRaisingEvents = false;
        }
        catch
        {
        }

        try
        {
            watcher.Dispose();
        }
        catch
        {
        }
    }

    private static IReadOnlyDictionary<string, FileWatcherSnapshotEntry> CreateSnapshot(string rootPath)
    {
        var snapshot = new Dictionary<string, FileWatcherSnapshotEntry>(PathComparer);
        if ((File.GetAttributes(rootPath) & FileAttributes.ReparsePoint) != 0)
        {
            return snapshot;
        }

        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootPath);
        while (pendingDirectories.Count > 0)
        {
            var directory = pendingDirectories.Pop();
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                FileAttributes attributes;
                try
                {
                    attributes = File.GetAttributes(entry);
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
                catch (DirectoryNotFoundException)
                {
                    continue;
                }

                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    snapshot[Path.GetFullPath(entry)] = new FileWatcherSnapshotEntry(-1, Directory.GetLastWriteTimeUtc(entry));
                    pendingDirectories.Push(entry);
                    continue;
                }

                try
                {
                    var info = new FileInfo(entry);
                    snapshot[Path.GetFullPath(entry)] = new FileWatcherSnapshotEntry(info.Length,
                        info.LastWriteTimeUtc);
                }
                catch (FileNotFoundException)
                {
                    // 扫描期间被删除的文件不属于最终快照。
                }
                catch (DirectoryNotFoundException)
                {
                    // 扫描期间被删除的目录不属于最终快照。
                }
            }
        }

        return snapshot;
    }

    private sealed class WatchRegistration
    {
        public WatchRegistration(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public FileSystemWatcher Watcher { get; set; }

        public IReadOnlyDictionary<string, FileWatcherSnapshotEntry> Snapshot { get; set; }

        public TaskCompletionSource<bool> RescanCompletion { get; set; }
    }

    private sealed class PendingEvent
    {
        public PendingEvent(string rootPath, FileWatcherEventArgs args, DateTime dueAtUtc)
        {
            RootPath = rootPath;
            Args = args;
            DueAtUtc = dueAtUtc;
        }

        public string RootPath { get; }

        public FileWatcherEventArgs Args { get; set; }

        public DateTime DueAtUtc { get; set; }
    }

    private sealed class RunState
    {
        public RunState(long generation, CancellationTokenSource cancellation)
        {
            Generation = generation;
            Cancellation = cancellation;
        }

        public long Generation { get; }

        public CancellationTokenSource Cancellation { get; }

        public Task Task { get; set; }

    }

    private sealed class CallbackTracker
    {
        public CallbackTracker(long generation)
        {
            Generation = generation;
            Drained = CompletedSignal();
        }

        public long Generation { get; }

        public bool Accepting { get; set; } = true;

        public int ActiveCallbacks { get; set; }

        public TaskCompletionSource<bool> Drained { get; set; }
    }

    private sealed class StopSnapshot
    {
        public StopSnapshot(Task[] backgroundTasks, Task[] callbackTasks)
        {
            BackgroundTasks = backgroundTasks;
            CallbackTasks = callbackTasks;
        }

        public Task[] BackgroundTasks { get; }

        public Task[] CallbackTasks { get; }
    }

    internal static TaskCompletionSource<bool> NewSignal()
    {
        return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static TaskCompletionSource<bool> CompletedSignal()
    {
        var signal = NewSignal();
        signal.TrySetResult(true);
        return signal;
    }
}

/// <summary>
/// 文件监控事件参数。
/// </summary>
public class FileWatcherEventArgs
{
    /// <summary>
    /// 获取变更类型。
    /// </summary>
    public WatcherChangeTypes ChangeTypes { get; }

    /// <summary>
    /// 获取文件全路径。
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// 获取文件名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 获取旧的文件全路径。
    /// </summary>
    public string OldFullPath { get; }

    /// <summary>
    /// 获取旧的文件名称。
    /// </summary>
    public string OldName { get; }

    /// <summary>
    /// 初始化一个 <see cref="FileWatcherEventArgs"/> 类型的实例。
    /// </summary>
    /// <param name="type">监控变更类型</param>
    /// <param name="fullPath">文件全路径</param>
    /// <param name="name">文件名称</param>
    /// <param name="oldFullPath">旧的文件全路径</param>
    /// <param name="oldName">旧的文件名称</param>
    public FileWatcherEventArgs(WatcherChangeTypes type, string fullPath, string name, string oldFullPath,
        string oldName)
    {
        ChangeTypes = type;
        FullPath = fullPath;
        Name = name;
        OldFullPath = oldFullPath;
        OldName = oldName;
    }
}

/// <summary>
/// 文件监控错误事件参数。
/// </summary>
public sealed class FileWatcherErrorEventArgs : EventArgs
{
    /// <summary>
    /// 初始化一个 <see cref="FileWatcherErrorEventArgs"/> 类型的实例。
    /// </summary>
    /// <param name="path">发生错误的监控目录</param>
    /// <param name="exception">错误信息</param>
    /// <param name="isBufferOverflow">是否为通知缓冲区溢出</param>
    public FileWatcherErrorEventArgs(string path, Exception exception, bool isBufferOverflow)
    {
        Path = path;
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        IsBufferOverflow = isBufferOverflow;
    }

    /// <summary>
    /// 获取发生错误的监控目录。
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// 获取错误信息。
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// 获取错误是否由通知缓冲区溢出引起。
    /// </summary>
    public bool IsBufferOverflow { get; }
}

internal readonly struct FileWatcherSnapshotEntry : IEquatable<FileWatcherSnapshotEntry>
{
    public FileWatcherSnapshotEntry(long length, DateTime lastWriteTimeUtc)
    {
        Length = length;
        LastWriteTimeUtc = lastWriteTimeUtc;
    }

    public long Length { get; }

    public DateTime LastWriteTimeUtc { get; }

    public bool Equals(FileWatcherSnapshotEntry other)
    {
        return Length == other.Length && LastWriteTimeUtc == other.LastWriteTimeUtc;
    }

    public override bool Equals(object obj)
    {
        return obj is FileWatcherSnapshotEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Length.GetHashCode() * 397) ^ LastWriteTimeUtc.GetHashCode();
        }
    }
}
