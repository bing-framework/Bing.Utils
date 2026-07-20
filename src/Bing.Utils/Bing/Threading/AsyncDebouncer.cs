using System.Diagnostics;
using Bing.Helpers;

namespace Bing.Threading;

/// <summary>
/// 异步尾部防抖执行器，将连续提交的操作合并为最后一次有效提交的执行。
/// </summary>
/// <remarks>
/// <para>延迟时间从最后一次有效提交开始计算。等待期间的新提交会取消之前尚未开始的提交，其返回任务将以取消状态结束。</para>
/// <para>业务回调默认不会并发执行。回调已经开始后再次提交不会中断该回调，新提交将在自身防抖等待结束并等待当前回调完成后执行。</para>
/// <para>该类型是线程安全的。调用方应观察 <see cref="DebounceAsync(Func{CancellationToken, Task}, CancellationToken)"/> 返回的任务，以获取执行、取消或异常结果。</para>
/// </remarks>
/// <example>
/// <code>
/// await using var debouncer = new AsyncDebouncer(TimeSpan.FromSeconds(2));
/// await debouncer.DebounceAsync(async cancellationToken =&gt;
/// {
///     await SaveAsync(cancellationToken);
/// });
///
/// _ = debouncer.DebounceAsync(token =&gt; RefreshAsync("A", token));
/// _ = debouncer.DebounceAsync(token =&gt; RefreshAsync("B", token));
/// await debouncer.DebounceAsync(token =&gt; RefreshAsync("C", token));
/// // 仅执行最后一次有效提交的 C。
/// </code>
/// </example>
public sealed class AsyncDebouncer : IAsyncDisposable
{
    /// <summary>
    /// 保护待执行提交、释放状态和活动协调器计数的同步对象。
    /// </summary>
    private readonly object _syncRoot = new();

    /// <summary>
    /// 确保业务回调按提交开始顺序串行执行的信号量。
    /// </summary>
    private readonly SemaphoreSlim _executionSemaphore = new(1, 1);

    /// <summary>
    /// 防抖等待时长。
    /// </summary>
    private readonly TimeSpan _delay;

    /// <summary>
    /// 当前尚未开始业务回调的提交。
    /// </summary>
    private PendingSubmission _currentPendingSubmission;

    /// <summary>
    /// 尚未完成清理的协调器数量。
    /// </summary>
    private int _activeCoordinatorCount;

    /// <summary>
    /// 在没有活动协调器时完成的任务源。
    /// </summary>
    private TaskCompletionSource<object> _idleSource = CreateCompletedSource();

    /// <summary>
    /// 指示实例已开始释放，禁止接受新提交。
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 共享的异步释放任务。
    /// </summary>
    private Task _disposeTask;

    /// <summary>
    /// 初始化 <see cref="AsyncDebouncer"/> 的新实例。
    /// </summary>
    /// <param name="delay">从最后一次有效提交到开始执行回调之间的延迟。允许为 <see cref="TimeSpan.Zero"/>。</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="delay"/> 小于 <see cref="TimeSpan.Zero"/> 时抛出。</exception>
    public AsyncDebouncer(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "防抖延迟不能小于零。");

        _delay = delay;
    }

    /// <summary>
    /// 获取从最后一次有效提交开始计算的防抖等待时长。
    /// </summary>
    public TimeSpan Delay => _delay;

    /// <summary>
    /// 提交异步回调以进行尾部防抖执行。
    /// </summary>
    /// <param name="action">在防抖等待结束后执行的异步回调。回调收到调用方提供的取消令牌。</param>
    /// <param name="cancellationToken">取消本次尚未开始回调的提交的令牌。回调开始后，该令牌仍会传递给回调以支持协作取消。</param>
    /// <returns>表示本次提交最终结果的异步操作。被后续提交替换、主动取消或释放时以取消状态结束；业务回调异常将通过该任务传播。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 null 时抛出。</exception>
    /// <exception cref="ObjectDisposedException">当实例已经释放时抛出。</exception>
    public Task DebounceAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        Check.NotNull(action, nameof(action));
        return DebounceAsyncCore(action, cancellationToken);
    }

    /// <summary>
    /// 提交同步回调以进行尾部防抖执行。
    /// </summary>
    /// <param name="action">在防抖等待结束后执行的同步回调。</param>
    /// <param name="cancellationToken">取消本次尚未开始回调的提交的令牌。</param>
    /// <returns>表示本次提交最终结果的异步操作。被后续提交替换、主动取消或释放时以取消状态结束；回调异常将通过该任务传播。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 null 时抛出。</exception>
    /// <exception cref="ObjectDisposedException">当实例已经释放时抛出。</exception>
    public Task DebounceAsync(Action action, CancellationToken cancellationToken = default)
    {
        Check.NotNull(action, nameof(action));
        return DebounceAsyncCore(_ =>
        {
            action();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    /// <summary>
    /// 取消当前尚未开始业务回调的提交，并等待其协调器完成清理。
    /// </summary>
    /// <returns>表示取消协调器完成清理的异步操作。没有待执行提交、重复调用或实例已释放时将成功完成。</returns>
    public Task CancelAsync()
    {
        lock (_syncRoot)
        {
            if (_disposed || _currentPendingSubmission == null)
                return Task.CompletedTask;

            var submission = _currentPendingSubmission;
            _currentPendingSubmission = null;
            CancelSubmission(submission);
            return submission.FinishedSource.Task;
        }
    }

    /// <summary>
    /// 异步释放执行器，取消尚未开始的提交并等待所有已启动回调结束。
    /// </summary>
    /// <returns>表示释放完成的异步操作。</returns>
    public ValueTask DisposeAsync()
    {
        lock (_syncRoot)
        {
            if (_disposeTask != null)
                return new ValueTask(_disposeTask);

            _disposed = true;
            if (_currentPendingSubmission != null)
            {
                var submission = _currentPendingSubmission;
                _currentPendingSubmission = null;
                CancelSubmission(submission);
            }

            _disposeTask = DisposeCoreAsync(_idleSource.Task);
            return new ValueTask(_disposeTask);
        }
    }

    /// <summary>
    /// 创建并启动本次提交的协调器。
    /// </summary>
    /// <param name="action">业务回调。</param>
    /// <param name="cancellationToken">调用方取消令牌。</param>
    /// <returns>表示本次提交最终结果的任务。</returns>
    /// <exception cref="ObjectDisposedException">当实例已经释放时抛出。</exception>
    private Task DebounceAsyncCore(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        PendingSubmission submission;
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            if (_currentPendingSubmission != null)
            {
                var previousSubmission = _currentPendingSubmission;
                _currentPendingSubmission = null;
                CancelSubmission(previousSubmission);
            }

            if (_activeCoordinatorCount == 0)
                _idleSource = CreateSource();

            submission = new PendingSubmission(action, cancellationToken);
            _currentPendingSubmission = submission;
            _activeCoordinatorCount++;
        }

        _ = RunSubmissionAsync(submission);
        return submission.CompletionSource.Task;
    }

    /// <summary>
    /// 等待提交延迟、串行执行业务回调并完成提交结果。
    /// </summary>
    /// <param name="submission">要协调的提交。</param>
    /// <returns>表示协调过程的异步操作。</returns>
    private async Task RunSubmissionAsync(PendingSubmission submission)
    {
        var executionSemaphoreEntered = false;
        try
        {
            // 确保零延迟提交也不会在 DebounceAsync 调用栈内同步执行用户回调。
            await Task.Yield();
            await WaitForDelayAsync(submission).ConfigureAwait(false);
            await _executionSemaphore.WaitAsync(submission.SchedulingToken).ConfigureAwait(false);
            executionSemaphoreEntered = true;

            lock (_syncRoot)
            {
                if (_disposed || !ReferenceEquals(_currentPendingSubmission, submission) || submission.SchedulingToken.IsCancellationRequested)
                {
                    submission.CompletionSource.TrySetCanceled();
                    return;
                }

                _currentPendingSubmission = null;
            }

            try
            {
                await submission.Action(submission.ExternalCancellationToken).ConfigureAwait(false);
                submission.CompletionSource.TrySetResult(null);
            }
            catch (OperationCanceledException)
            {
                submission.CompletionSource.TrySetCanceled();
            }
            catch (Exception exception)
            {
                submission.CompletionSource.TrySetException(exception);
            }
        }
        catch (OperationCanceledException)
        {
            submission.CompletionSource.TrySetCanceled();
        }
        catch (Exception exception)
        {
            submission.CompletionSource.TrySetException(exception);
        }
        finally
        {
            if (executionSemaphoreEntered)
                _executionSemaphore.Release();

            submission.Dispose();
            CompleteCoordinator(submission);
        }
    }

    /// <summary>
    /// 按提交时刻计算剩余防抖等待时间。
    /// </summary>
    /// <param name="submission">要等待的提交。</param>
    /// <returns>表示剩余等待的异步操作。</returns>
    private Task WaitForDelayAsync(PendingSubmission submission)
    {
        var remainingDelay = _delay - submission.Stopwatch.Elapsed;
        return Task.Delay(remainingDelay > TimeSpan.Zero ? remainingDelay : TimeSpan.Zero, submission.SchedulingToken);
    }

    /// <summary>
    /// 将提交置为取消状态并取消其调度等待。
    /// </summary>
    /// <param name="submission">要取消的提交。</param>
    private static void CancelSubmission(PendingSubmission submission)
    {
        submission.CompletionSource.TrySetCanceled();
        submission.Cancel();
    }

    /// <summary>
    /// 记录协调器结束，并在最后一个协调器结束时唤醒释放操作。
    /// </summary>
    /// <param name="submission">已完成协调的提交。</param>
    private void CompleteCoordinator(PendingSubmission submission)
    {
        lock (_syncRoot)
        {
            if (ReferenceEquals(_currentPendingSubmission, submission))
                _currentPendingSubmission = null;

            _activeCoordinatorCount--;
            if (_activeCoordinatorCount == 0)
                _idleSource.TrySetResult(null);
        }

        submission.FinishedSource.TrySetResult(null);
    }

    /// <summary>
    /// 等待活动协调器完成后释放内部同步资源。
    /// </summary>
    /// <param name="idleTask">在没有活动协调器时完成的任务。</param>
    /// <returns>表示资源释放完成的异步操作。</returns>
    private async Task DisposeCoreAsync(Task idleTask)
    {
        await idleTask.ConfigureAwait(false);
        _executionSemaphore.Dispose();
    }

    /// <summary>
    /// 在实例已释放时抛出异常。
    /// </summary>
    /// <exception cref="ObjectDisposedException">当实例已经释放时抛出。</exception>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AsyncDebouncer));
    }

    /// <summary>
    /// 创建可异步完成的任务源。
    /// </summary>
    /// <returns>新建任务源。</returns>
    private static TaskCompletionSource<object> CreateSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// 创建已经成功完成的任务源。
    /// </summary>
    /// <returns>已经完成的任务源。</returns>
    private static TaskCompletionSource<object> CreateCompletedSource()
    {
        var source = CreateSource();
        source.TrySetResult(null);
        return source;
    }

    /// <summary>
    /// 表示单次防抖提交的可取消协调状态。
    /// </summary>
    private sealed class PendingSubmission : IDisposable
    {
        /// <summary>
        /// 初始化 <see cref="PendingSubmission"/> 的新实例。
        /// </summary>
        /// <param name="action">业务回调。</param>
        /// <param name="externalCancellationToken">调用方取消令牌。</param>
        public PendingSubmission(Func<CancellationToken, Task> action, CancellationToken externalCancellationToken)
        {
            Action = action;
            ExternalCancellationToken = externalCancellationToken;
            SchedulingSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
            Stopwatch = Stopwatch.StartNew();
            CompletionSource = CreateSource();
            FinishedSource = CreateSource();
        }

        /// <summary>
        /// 获取业务回调。
        /// </summary>
        public Func<CancellationToken, Task> Action { get; }

        /// <summary>
        /// 获取调用方提供的取消令牌。
        /// </summary>
        public CancellationToken ExternalCancellationToken { get; }

        /// <summary>
        /// 获取取消延迟和执行锁等待的令牌源。
        /// </summary>
        public CancellationTokenSource SchedulingSource { get; }

        /// <summary>
        /// 获取取消延迟和执行锁等待的令牌。
        /// </summary>
        public CancellationToken SchedulingToken => SchedulingSource.Token;

        /// <summary>
        /// 获取从提交创建时刻开始计时的单调时钟。
        /// </summary>
        public Stopwatch Stopwatch { get; }

        /// <summary>
        /// 获取承载提交最终业务结果的任务源。
        /// </summary>
        public TaskCompletionSource<object> CompletionSource { get; }

        /// <summary>
        /// 获取在协调器完成资源清理时完成的任务源。
        /// </summary>
        public TaskCompletionSource<object> FinishedSource { get; }

        /// <summary>
        /// 取消本次提交尚未开始的延迟或执行锁等待。
        /// </summary>
        public void Cancel() => SchedulingSource.Cancel();

        /// <summary>
        /// 释放本次提交持有的取消源。
        /// </summary>
        public void Dispose() => SchedulingSource.Dispose();
    }
}