using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Bing.Threading;

// ─────────────────────────────────────────────────────────────────────────────
//  TaskCompletionSourceExtensions Tests
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="TaskCompletionSourceExtensions.TryCompleteFromCompletedTask{TResult}"/>
/// </summary>
public class TaskCompletionSourceExtensionsTests
{
    // ── 参数校验 ───────────────────────────────────────────────────────────────

    [Fact]
    public void TryCompleteFromCompletedTask_NullSource_ThrowsArgumentNullException()
    {
        TaskCompletionSource<int> tcs = null!;
        var completedTask = Task.FromResult(1);
        Should.Throw<ArgumentNullException>(() => tcs.TryCompleteFromCompletedTask(completedTask));
    }

    [Fact]
    public void TryCompleteFromCompletedTask_NullTask_ThrowsArgumentNullException()
    {
        var tcs = new TaskCompletionSource<int>();
        Should.Throw<ArgumentNullException>(() => tcs.TryCompleteFromCompletedTask(null!));
    }

    [Fact]
    public void TryCompleteFromCompletedTask_IncompleteTask_ThrowsArgumentException()
    {
        var tcs = new TaskCompletionSource<int>();
        var runningTask = new TaskCompletionSource<int>().Task; // never completes
        Should.Throw<ArgumentException>(() => tcs.TryCompleteFromCompletedTask(runningTask));
    }

    // ── RanToCompletion ────────────────────────────────────────────────────────

    [Fact]
    public void TryCompleteFromCompletedTask_RanToCompletion_SetsResult()
    {
        var tcs = new TaskCompletionSource<int>();
        var completedTask = Task.FromResult(42);
        tcs.TryCompleteFromCompletedTask(completedTask).ShouldBeTrue();
        tcs.Task.IsCompletedSuccessfully.ShouldBeTrue();
        tcs.Task.Result.ShouldBe(42);
    }

    [Fact]
    public void TryCompleteFromCompletedTask_AlreadySet_ReturnsFalse()
    {
        var tcs = new TaskCompletionSource<int>();
        tcs.SetResult(99);
        // 第二次 TrySet 应返回 false
        var completedTask = Task.FromResult(1);
        tcs.TryCompleteFromCompletedTask(completedTask).ShouldBeFalse();
    }

    // ── Faulted ────────────────────────────────────────────────────────────────

    [Fact]
    public void TryCompleteFromCompletedTask_Faulted_SetsException()
    {
        var tcs = new TaskCompletionSource<int>();
        var ex = new InvalidOperationException("test error");
        var faultedTask = Task.FromException<int>(ex);
        tcs.TryCompleteFromCompletedTask(faultedTask).ShouldBeTrue();
        tcs.Task.IsFaulted.ShouldBeTrue();
        tcs.Task.Exception!.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    // ── Canceled ───────────────────────────────────────────────────────────────

    [Fact]
    public void TryCompleteFromCompletedTask_Canceled_SetsCanceled()
    {
        var tcs = new TaskCompletionSource<int>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var canceledTask = Task.FromCanceled<int>(cts.Token);
        tcs.TryCompleteFromCompletedTask(canceledTask).ShouldBeTrue();
        tcs.Task.IsCanceled.ShouldBeTrue();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  ValueTaskExtensions Tests
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="ValueTaskExtensions"/> — NoSync 扩展方法
/// </summary>
public class ValueTaskExtensionsTests
{
    [Fact]
    public async Task NoSync_ValueTask_CompletesSuccessfully()
    {
        var vt = new ValueTask(Task.CompletedTask);
        await vt.NoSync();
        // 未抛出异常即为通过
    }

    [Fact]
    public async Task NoSync_ValueTaskOfT_ReturnsCorrectValue()
    {
        var vt = new ValueTask<int>(Task.FromResult(42));
        var result = await vt.NoSync();
        result.ShouldBe(42);
    }

    [Fact]
    public async Task NoSync_ValueTask_AlreadyCompleted_Works()
    {
        // ValueTask.CompletedTask is .NET5+; use Task.CompletedTask wrapped in ValueTask for netcoreapp3.1 compat
        var vt = new ValueTask(Task.CompletedTask);
        await vt.NoSync();
    }

    [Fact]
    public async Task NoSync_ValueTaskOfT_AlreadyCompleted_ReturnsValue()
    {
        var vt = new ValueTask<string>("hello");
        var result = await vt.NoSync();
        result.ShouldBe("hello");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  LockExtensions Tests
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="LockExtensions"/> — LockAndRun / LockAndReturn
/// </summary>
public class LockExtensionsTests
{
    // ── LockAndRun (object source) ────────────────────────────────────────────

    [Fact]
    public void LockAndRun_ExecutesAction()
    {
        var lockObj = new object();
        var executed = false;
        lockObj.LockAndRun(() => executed = true);
        executed.ShouldBeTrue();
    }

    [Fact]
    public void LockAndRun_MutatesSharedState_UnderLock()
    {
        var lockObj = new object();
        var counter = 0;
        // 连续调用多次，确保累加正确（非并发场景）
        for (var i = 0; i < 10; i++)
            lockObj.LockAndRun(() => counter++);
        counter.ShouldBe(10);
    }

    // ── LockAndRun<T> (typed source) ──────────────────────────────────────────

    [Fact]
    public void LockAndRun_TypedSource_PassesSourceToAction()
    {
        var list = new System.Collections.Generic.List<int>();
        list.LockAndRun(l => l.Add(99));
        list.ShouldContain(99);
    }

    // ── LockAndReturn (object source) ─────────────────────────────────────────

    [Fact]
    public void LockAndReturn_ReturnsValueFromFunc()
    {
        var lockObj = new object();
        var result = lockObj.LockAndReturn(() => 42);
        result.ShouldBe(42);
    }

    [Fact]
    public void LockAndReturn_CalculatesInsideLock()
    {
        var lockObj = new object();
        var counter = 5;
        var result = lockObj.LockAndReturn(() => counter * 2);
        result.ShouldBe(10);
    }

    // ── LockAndReturn<T, TResult> (typed source) ──────────────────────────────

    [Fact]
    public void LockAndReturn_TypedSource_PassesSourceToFunc()
    {
        var list = new System.Collections.Generic.List<int> { 1, 2, 3 };
        var count = list.LockAndReturn(l => l.Count);
        count.ShouldBe(3);
    }

    [Fact]
    public void LockAndReturn_TypedSource_ReturnsComputedValue()
    {
        var list = new System.Collections.Generic.List<int> { 10, 20, 30 };
        var sum = list.LockAndReturn(l => l.Sum());
        sum.ShouldBe(60);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  TaskExtensions (ContinueWithSynchronously / ToCancellationTokenSource)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="TaskExtensions"/> 未被 TaskExtensionsTest 覆盖的方法：
/// <c>ContinueWithSynchronously</c> 和 <c>ToCancellationTokenSource</c>
/// </summary>
public class TaskExtensionsContinueTests
{
    // ── 参数校验 ───────────────────────────────────────────────────────────────

    [Fact]
    public void ContinueWithSynchronously_NullTask_ThrowsArgumentNullException()
    {
        Task task = null!;
        Should.Throw<ArgumentNullException>(() =>
            task.ContinueWithSynchronously(_ => { }));
    }

    [Fact]
    public void ContinueWithSynchronously_NullAction_ThrowsArgumentNullException()
    {
        var task = Task.CompletedTask;
        Should.Throw<ArgumentNullException>(() =>
            task.ContinueWithSynchronously((Action<Task>)null!));
    }

    // ── ContinueWithSynchronously(Action<Task>) ───────────────────────────────

    [Fact]
    public async Task ContinueWithSynchronously_ActionTask_ExecutesContinuation()
    {
        var executed = false;
        var task = Task.CompletedTask;
        await task.ContinueWithSynchronously(_ => executed = true);
        executed.ShouldBeTrue();
    }

    // ── ContinueWithSynchronously(Action<Task, object>) ──────────────────────

    [Fact]
    public async Task ContinueWithSynchronously_ActionWithState_PassesState()
    {
        object capturedState = null!;
        var task = Task.CompletedTask;
        var stateObj = new object();
        await task.ContinueWithSynchronously((_, state) => capturedState = state, stateObj);
        capturedState.ShouldBe(stateObj);
    }

    // ── ContinueWithSynchronously(Func<Task, TResult>) ───────────────────────

    [Fact]
    public async Task ContinueWithSynchronously_FuncTask_ReturnsValue()
    {
        var task = Task.CompletedTask;
        var result = await task.ContinueWithSynchronously(_ => 123);
        result.ShouldBe(123);
    }

    // ── ContinueWithSynchronously(Func<Task, object, TResult>) ───────────────

    [Fact]
    public async Task ContinueWithSynchronously_FuncWithState_PassesStateAndReturnsValue()
    {
        var task = Task.CompletedTask;
        var state = "ctx";
        var result = await task.ContinueWithSynchronously((_, s) => (string)s! + "_done", state);
        result.ShouldBe("ctx_done");
    }

    // ── ContinueWithSynchronously(Action<Task<TResult>>) ─────────────────────

    [Fact]
    public async Task ContinueWithSynchronously_ActionOnTypedTask_ExecutesContinuation()
    {
        var capturedResult = 0;
        var task = Task.FromResult(77);
        await task.ContinueWithSynchronously(t => capturedResult = t.Result);
        capturedResult.ShouldBe(77);
    }

    // ── ContinueWithSynchronously(Action<Task<TResult>, object>) ─────────────

    [Fact]
    public async Task ContinueWithSynchronously_ActionOnTypedTaskWithState_PassesState()
    {
        object capturedState = null!;
        var task = Task.FromResult(10);
        var stateObj = new object();
        await task.ContinueWithSynchronously((_, state) => capturedState = state, stateObj);
        capturedState.ShouldBe(stateObj);
    }

    // ── ContinueWithSynchronously<TResult, TNewResult> ───────────────────────

    [Fact]
    public async Task ContinueWithSynchronously_FuncOnTypedTask_TransformsResult()
    {
        var task = Task.FromResult(5);
        var result = await task.ContinueWithSynchronously(t => t.Result * 2);
        result.ShouldBe(10);
    }

    [Fact]
    public async Task ContinueWithSynchronously_FuncOnTypedTaskWithState_TransformsResult()
    {
        var task = Task.FromResult(5);
        var factor = 3;
        var result = await task.ContinueWithSynchronously((t, s) => t.Result * (int)s!, factor);
        result.ShouldBe(15);
    }

    // ── ToCancellationTokenSource ─────────────────────────────────────────────

    [Fact]
    public void ToCancellationTokenSource_NullTask_ThrowsArgumentNullException()
    {
        Task task = null!;
        Should.Throw<ArgumentNullException>(() => task.ToCancellationTokenSource());
    }

    [Fact]
    public async Task ToCancellationTokenSource_WhenTaskCompletes_CancelsCts()
    {
        var tcs = new TaskCompletionSource<bool>();
        var cts = tcs.Task.ToCancellationTokenSource();
        cts.IsCancellationRequested.ShouldBeFalse();
        tcs.SetResult(true);
        // 等待 continuation 执行
        await Task.Delay(200);
        cts.IsCancellationRequested.ShouldBeTrue();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  TaskFactoryExtensions Tests
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="TaskFactoryExtensions.StartDelayedTask"/>
/// </summary>
public class TaskFactoryExtensionsTests
{
    // ── 参数校验 ───────────────────────────────────────────────────────────────

    [Fact]
    public void StartDelayedTask_NullFactory_ThrowsArgumentNullException()
    {
        TaskFactory factory = null!;
        Should.Throw<ArgumentNullException>(() =>
            factory.StartDelayedTask(0, () => { }));
    }

    [Fact]
    public void StartDelayedTask_NegativeDelay_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Task.Factory.StartDelayedTask(-1, () => { }));
    }

    [Fact]
    public void StartDelayedTask_NullAction_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            Task.Factory.StartDelayedTask(0, null!));
    }

    // ── 正常执行 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StartDelayedTask_ZeroDelay_ExecutesAction()
    {
        var executed = false;
        await Task.Factory.StartDelayedTask(0, () => executed = true);
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task StartDelayedTask_SmallDelay_ExecutesActionAfterDelay()
    {
        var executed = false;
        await Task.Factory.StartDelayedTask(50, () => executed = true);
        executed.ShouldBeTrue();
    }
}
