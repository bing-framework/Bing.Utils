using Bing.Threading;
using System.Threading;

namespace Bing.Utils.Tests.Bing.Threading;

/// <summary>
/// <see cref="AsyncDebouncer"/> 单元测试。
/// </summary>
public class AsyncDebouncerTests
{
    [Fact]
    public async Task Constructor_DoesNotExecuteAnyCallback()
    {
        await using var debouncer = new AsyncDebouncer(TimeSpan.FromMilliseconds(100));

        await Task.Delay(50);

        debouncer.Delay.ShouldBe(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task DebounceAsync_SingleSubmission_ExecutesAfterDelay()
    {
        await using var debouncer = new AsyncDebouncer(TimeSpan.FromMilliseconds(150));
        var invoked = false;

        var submission = debouncer.DebounceAsync(() => invoked = true);

        invoked.ShouldBeFalse();
        await submission;
        invoked.ShouldBeTrue();
    }

    [Fact]
    public async Task DebounceAsync_ZeroDelay_ExecutesCallback()
    {
        await using var debouncer = new AsyncDebouncer(TimeSpan.Zero);
        var invoked = false;

        await debouncer.DebounceAsync(() => invoked = true);

        invoked.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_NegativeDelay_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new AsyncDebouncer(TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public async Task DebounceAsync_NullAction_ThrowsArgumentNullException()
    {
        await using var debouncer = new AsyncDebouncer(TimeSpan.Zero);
        Action action = null!;

        Should.Throw<ArgumentNullException>(() => debouncer.DebounceAsync(action));
    }

    [Fact]
    public async Task DebounceAsync_NullAsyncAction_ThrowsArgumentNullException()
    {
        await using var debouncer = new AsyncDebouncer(TimeSpan.Zero);
        Func<CancellationToken, Task> action = null!;

        Should.Throw<ArgumentNullException>(() => debouncer.DebounceAsync(action));
    }

    [Fact]
    public async Task DebounceAsync_RepeatedSubmissions_OnlyExecutesLastCallback()
    {
        await using var debouncer = new AsyncDebouncer(TimeSpan.FromMilliseconds(200));
        var executed = new List<string>();

        var first = debouncer.DebounceAsync(() => executed.Add("A"));
        await Task.Delay(50);
        var second = debouncer.DebounceAsync(() => executed.Add("B"));
        await Task.Delay(50);
        var third = debouncer.DebounceAsync(() => executed.Add("C"));

        first.IsCanceled.ShouldBeTrue();
        second.IsCanceled.ShouldBeTrue();
        await third;

        executed.ShouldBe(new[] { "C" });
    }

    [Fact]
    public async Task DebounceAsync_NewSubmission_RecalculatesFullDelay()
    {
        await using var debouncer = new AsyncDebouncer(TimeSpan.FromMilliseconds(300));
        var started = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstSubmission = debouncer.DebounceAsync(_ => Task.CompletedTask);
        await Task.Delay(150);
        var finalSubmission = debouncer.DebounceAsync(_ =>
        {
            started.TrySetResult(null);
            return Task.CompletedTask;
        });

        await Task.Delay(175);
        started.Task.IsCompleted.ShouldBeFalse();
        firstSubmission.IsCanceled.ShouldBeTrue();
        await WaitForCompletionAsync(started.Task);
        await finalSubmission;
    }

    [Fact]
    public async Task DebounceAsync_ExternalCancellationBeforeExecution_CancelsSubmission()
    {
        await using var debouncer = new AsyncDebouncer(TimeSpan.FromSeconds(2));
        using var cancellationSource = new CancellationTokenSource();
        var invoked = false;

        var submission = debouncer.DebounceAsync(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        }, cancellationSource.Token);
        cancellationSource.Cancel();

        await WaitForSettlementAsync(submission);
        submission.IsCanceled.ShouldBeTrue();
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task CancelAsync_PendingSubmission_CancelsAndWaitsForCleanup()
    {
        await using var debouncer = new AsyncDebouncer(TimeSpan.FromSeconds(2));
        var invoked = false;
        var submission = debouncer.DebounceAsync(() => invoked = true);

        await debouncer.CancelAsync();

        submission.IsCanceled.ShouldBeTrue();
        invoked.ShouldBeFalse();
        await debouncer.CancelAsync();
    }

    [Fact]
    public async Task DebounceAsync_CallbackException_PropagatesAndDoesNotBreakFutureSubmissions()
    {
        await using var debouncer = new AsyncDebouncer(TimeSpan.Zero);
        var expectedException = new InvalidOperationException("failure");

        var failedSubmission = debouncer.DebounceAsync(() => throw expectedException);
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () => await failedSubmission);
        exception.ShouldBeSameAs(expectedException);

        var invoked = false;
        await debouncer.DebounceAsync(() => invoked = true);
        invoked.ShouldBeTrue();
    }

    [Fact]
    public async Task DebounceAsync_SubmissionDuringRunningCallback_DoesNotRunCallbacksConcurrently()
    {
        await using var debouncer = new AsyncDebouncer(TimeSpan.Zero);
        var firstStarted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var runningCallbacks = 0;
        var maximumRunningCallbacks = 0;

        var first = debouncer.DebounceAsync(async _ =>
        {
            maximumRunningCallbacks = Math.Max(maximumRunningCallbacks, Interlocked.Increment(ref runningCallbacks));
            firstStarted.TrySetResult(null);
            await releaseFirst.Task;
            Interlocked.Decrement(ref runningCallbacks);
        });
        await WaitForCompletionAsync(firstStarted.Task);

        var second = debouncer.DebounceAsync(_ =>
        {
            maximumRunningCallbacks = Math.Max(maximumRunningCallbacks, Interlocked.Increment(ref runningCallbacks));
            secondStarted.TrySetResult(null);
            Interlocked.Decrement(ref runningCallbacks);
            return Task.CompletedTask;
        });

        var completed = await Task.WhenAny(secondStarted.Task, Task.Delay(100));
        completed.ShouldNotBe(secondStarted.Task);
        releaseFirst.TrySetResult(null);

        await first;
        await second;
        maximumRunningCallbacks.ShouldBe(1);
    }

    [Fact]
    public async Task DebounceAsync_ConcurrentSubmissionsThenFinalSubmission_ExecutesOnlyFinalCallback()
    {
        await using var debouncer = new AsyncDebouncer(TimeSpan.FromMilliseconds(150));
        var concurrentExecutions = 0;
        var concurrentSubmissions = Enumerable.Range(0, 10)
            .Select(_ => debouncer.DebounceAsync(() => { Interlocked.Increment(ref concurrentExecutions); }))
            .ToArray();

        var finalExecutions = 0;
        var finalSubmission = debouncer.DebounceAsync(() => { Interlocked.Increment(ref finalExecutions); });

        await finalSubmission;
        concurrentSubmissions.All(task => task.IsCanceled).ShouldBeTrue();
        concurrentExecutions.ShouldBe(0);
        finalExecutions.ShouldBe(1);
    }

    [Fact]
    public async Task DisposeAsync_PendingSubmission_CancelsAndPreventsFutureSubmissions()
    {
        var debouncer = new AsyncDebouncer(TimeSpan.FromSeconds(2));
        var submission = debouncer.DebounceAsync(_ => Task.CompletedTask);

        await debouncer.DisposeAsync();

        submission.IsCanceled.ShouldBeTrue();
        Should.Throw<ObjectDisposedException>(() => debouncer.DebounceAsync(_ => Task.CompletedTask));
        await debouncer.CancelAsync();
        await debouncer.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_RunningCallback_WaitsForCallbackCompletion()
    {
        var debouncer = new AsyncDebouncer(TimeSpan.Zero);
        var started = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var submission = debouncer.DebounceAsync(async _ =>
        {
            started.TrySetResult(null);
            await release.Task;
        });
        await WaitForCompletionAsync(started.Task);

        var disposeTask = debouncer.DisposeAsync().AsTask();
        disposeTask.IsCompleted.ShouldBeFalse();
        release.TrySetResult(null);

        await submission;
        await disposeTask;
    }

    /// <summary>
    /// 在指定时限内等待预期成功完成的任务，避免异步测试永久挂起。
    /// </summary>
    /// <param name="task">要等待的任务。</param>
    /// <param name="timeout">最长等待时间。</param>
    /// <returns>表示等待操作的异步任务。</returns>
    private static async Task WaitForCompletionAsync(Task task, TimeSpan? timeout = null)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout ?? TimeSpan.FromSeconds(5)));
        completed.ShouldBe(task);
        await task;
    }

    /// <summary>
    /// 在指定时限内等待任务进入任意终止状态，避免取消场景的测试永久挂起。
    /// </summary>
    /// <param name="task">要等待的任务。</param>
    /// <param name="timeout">最长等待时间。</param>
    /// <returns>表示等待操作的异步任务。</returns>
    private static async Task WaitForSettlementAsync(Task task, TimeSpan? timeout = null)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout ?? TimeSpan.FromSeconds(5)));
        completed.ShouldBe(task);
    }
}