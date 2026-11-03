using System.Threading;
using Bing.Exceptions;

namespace Bing.Utils.Tests.Bing.Exceptions;

/// <summary>
/// 验证真正异步入口的完成时机、失败身份与协作取消。
/// </summary>
public class TryAsyncTests
{
    /// <summary>
    /// 验证异步入口返回后仍等待底层任务真正结束。
    /// </summary>
    [Fact]
    public async Task AsyncFactories_WaitForActualCompletion()
    {
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var valueTask = Try.CreateAsync(() => completion.Task);
        var actionTask = Try.InvokeTaskAsync(() => completion.Task);
        valueTask.IsCompleted.ShouldBeFalse();
        actionTask.IsCompleted.ShouldBeFalse();
        completion.SetResult(17);
        (await valueTask).Value.ShouldBe(17);
        (await actionTask).IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// 验证同步抛错及故障任务保留原异常和业务原因。
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AsyncFactories_PreserveSynchronousAndTaskFaults(bool faultTask)
    {
        var original = new InvalidOperationException("failed");
        Func<Task<int>> valueFactory = () => faultTask ? Task.FromException<int>(original) : throw original;
        Func<Task> actionFactory = () => faultTask ? Task.FromException(original) : throw original;
        var value = await Try.CreateAsync(valueFactory, "value-cause");
        var action = await Try.InvokeTaskAsync(actionFactory, "action-cause");
        value.Exception.InnerException.ShouldBeSameAs(original);
        value.Exception.Cause.ShouldBe("value-cause");
        action.Exception.InnerException.ShouldBeSameAs(original);
        action.Cause.ShouldBe("action-cause");
    }

    /// <summary>
    /// 验证空委托和空任务形成明确的参数失败。
    /// </summary>
    [Fact]
    public async Task AsyncFactories_NullDelegatesAndTasksBecomeFailures()
    {
        (await Try.CreateAsync<int>((Func<Task<int>>)null)).ExceptionAs<ArgumentNullException>().ParamName.ShouldBe("createFunctionAsync");
        (await Try.CreateAsync<int>(() => null)).ExceptionAs<ArgumentNullException>().ParamName.ShouldBe("task");
        (await Try.InvokeTaskAsync((Func<Task>)null)).ExceptionAs<ArgumentNullException>().ParamName.ShouldBe("invokeAsyncAction");
        (await Try.InvokeTaskAsync(() => null)).ExceptionAs<ArgumentNullException>().ParamName.ShouldBe("task");
        (await Try.CreateAsync<int>(null, CancellationToken.None)).ExceptionAs<ArgumentNullException>().ShouldNotBeNull();
        (await Try.InvokeTaskAsync(null, CancellationToken.None)).ExceptionAs<ArgumentNullException>().ShouldNotBeNull();
        (await Try.CreateAsync<int>(_ => null, CancellationToken.None)).ExceptionAs<ArgumentNullException>().ShouldNotBeNull();
        (await Try.InvokeTaskAsync(_ => null, CancellationToken.None)).ExceptionAs<ArgumentNullException>().ShouldNotBeNull();
    }

    /// <summary>
    /// 验证预取消不执行委托且外层任务返回失败结果。
    /// </summary>
    [Fact]
    public async Task PreCancellation_DoesNotInvokeDelegates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var calls = 0;
        var valueTask = Try.CreateAsync(_ => { calls++; return Task.FromResult(1); }, cancellation.Token);
        var actionTask = Try.InvokeTaskAsync(_ => { calls++; return Task.CompletedTask; }, cancellation.Token);
        var value = await valueTask;
        var action = await actionTask;
        calls.ShouldBe(0);
        value.ExceptionAs<OperationCanceledException>().CancellationToken.ShouldBe(cancellation.Token);
        action.ExceptionAs<OperationCanceledException>().CancellationToken.ShouldBe(cancellation.Token);
        valueTask.Status.ShouldBe(TaskStatus.RanToCompletion);
        actionTask.Status.ShouldBe(TaskStatus.RanToCompletion);
    }

    /// <summary>
    /// 验证取消异常实例和令牌在异步包装后得到保留。
    /// </summary>
    [Fact]
    public async Task Cancellation_PreservesOriginalExceptionAndToken()
    {
        using var cancellation = new CancellationTokenSource();
        var original = new OperationCanceledException(cancellation.Token);
        var value = await Try.CreateAsync<int>(token =>
        {
            token.ShouldBe(cancellation.Token);
            return Task.FromException<int>(original);
        }, cancellation.Token);
        var action = await Try.InvokeTaskAsync(token =>
        {
            token.ShouldBe(cancellation.Token);
            return Task.FromException(original);
        }, cancellation.Token);
        value.Exception.InnerException.ShouldBeSameAs(original);
        action.Exception.InnerException.ShouldBeSameAs(original);
        cancellation.Cancel();
        (await Try.CreateAsync(() => Task.FromCanceled<int>(cancellation.Token)))
            .ExceptionAs<OperationCanceledException>().CancellationToken.ShouldBe(cancellation.Token);
        (await Try.InvokeTaskAsync(() => Task.FromCanceled(cancellation.Token)))
            .ExceptionAs<OperationCanceledException>().CancellationToken.ShouldBe(cancellation.Token);
    }

    /// <summary>
    /// 验证四个入口在操作启动后协作取消，保留实际取消异常且外层正常完成。
    /// </summary>
    /// <param name="passToken">是否使用显式透传令牌的重载。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunningCancellation_PreservesOriginalFailureAndCompletesOuterTask(bool passToken)
    {
        using var cancellation = new CancellationTokenSource();
        OperationCanceledException valueError = null;
        OperationCanceledException actionError = null;
        Task<int> valueOperation = null;
        Task<int> actionOperation = null;
        var calls = 0;
        Func<CancellationToken, Task<int>> create = token =>
        {
            token.ShouldBe(cancellation.Token);
            calls++;
            return valueOperation = WaitForCancellationAsync(token, error => valueError = error);
        };
        Func<CancellationToken, Task> invoke = token =>
        {
            token.ShouldBe(cancellation.Token);
            calls++;
            return actionOperation = WaitForCancellationAsync(token, error => actionError = error);
        };
        var valueTask = passToken
            ? Try.CreateAsync(create, cancellation.Token, "value-cancel")
            : Try.CreateAsync(() => create(cancellation.Token), "value-cancel");
        var actionTask = passToken
            ? Try.InvokeTaskAsync(invoke, cancellation.Token, "action-cancel")
            : Try.InvokeTaskAsync(() => invoke(cancellation.Token), "action-cancel");
        calls.ShouldBe(2);
        valueTask.IsCompleted.ShouldBeFalse();
        actionTask.IsCompleted.ShouldBeFalse();
        valueOperation.IsCompleted.ShouldBeFalse();
        actionOperation.IsCompleted.ShouldBeFalse();

        cancellation.Cancel();
        var completed = Task.WhenAll((Task)valueTask, actionTask);
        (await Task.WhenAny(completed, Task.Delay(5000))).ShouldBeSameAs(completed);
        await completed;
        var value = await valueTask;
        var action = await actionTask;
        value.IsSuccess.ShouldBeFalse();
        action.IsSuccess.ShouldBeFalse();
        valueError.ShouldNotBeNull();
        actionError.ShouldNotBeNull();
        value.Exception.InnerException.ShouldBeSameAs(valueError);
        action.Exception.InnerException.ShouldBeSameAs(actionError);
        valueError.CancellationToken.ShouldBe(cancellation.Token);
        actionError.CancellationToken.ShouldBe(cancellation.Token);
        value.Exception.Cause.ShouldBe("value-cancel");
        action.Cause.ShouldBe("action-cancel");
        valueOperation.Status.ShouldBe(TaskStatus.Canceled);
        actionOperation.Status.ShouldBe(TaskStatus.Canceled);
        valueTask.Status.ShouldBe(TaskStatus.RanToCompletion);
        actionTask.Status.ShouldBe(TaskStatus.RanToCompletion);
    }

    /// <summary>
    /// 等待真实令牌取消并捕获操作实际抛出的原始异常。
    /// </summary>
    /// <param name="token">由被测入口传入的取消令牌。</param>
    /// <param name="capture">保存实际取消异常的回调。</param>
    /// <returns>取消前保持未完成，取消后进入取消状态的任务。</returns>
    private static async Task<int> WaitForCancellationAsync(CancellationToken token, Action<OperationCanceledException> capture)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException exception)
        {
            capture(exception);
            throw;
        }
    }

    /// <summary>
    /// 验证忽略取消的任务仍被等待且可以正常成功。
    /// </summary>
    [Fact]
    public async Task IgnoredCancellation_DoesNotAbandonTheRunningTask()
    {
        using var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var value = Try.CreateAsync(token => { token.ShouldBe(cancellation.Token); return completion.Task; }, cancellation.Token);
        var action = Try.InvokeTaskAsync(token => { token.ShouldBe(cancellation.Token); return completion.Task; }, cancellation.Token);
        cancellation.Cancel();
        value.IsCompleted.ShouldBeFalse();
        action.IsCompleted.ShouldBeFalse();
        completion.SetResult(23);
        (await value).Value.ShouldBe(23);
        (await action).IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// 验证旧 Task<Action> 入口保持同步操作和失败包装契约。
    /// </summary>
    [Fact]
    public async Task LegacyActionTask_PreservesItsSynchronousActionContract()
    {
        var called = 0;
        (await Try.InvokeAsync(Task.FromResult<Action>(() => called++))).IsSuccess.ShouldBeTrue();
        called.ShouldBe(1);
        var original = new InvalidOperationException("action");
        (await Try.InvokeAsync(Task.FromResult<Action>(() => throw original))).Exception.InnerException.ShouldBeSameAs(original);
        (await Try.InvokeAsync(Task.FromException<Action>(original))).Exception.InnerException.ShouldBeSameAs(original);
        (await Try.InvokeAsync((Task<Action>)null)).ExceptionAs<ArgumentNullException>().ShouldNotBeNull();
        (await Try.InvokeAsync(Task.FromResult<Action>(null))).ExceptionAs<ArgumentNullException>().ShouldNotBeNull();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        (await Try.InvokeAsync(Task.FromCanceled<Action>(cancellation.Token))).ExceptionAs<OperationCanceledException>().ShouldNotBeNull();
    }

    /// <summary>
    /// 验证默认上下文的同步桥接保留结果及原任务故障。
    /// </summary>
    [Fact]
    public async Task LegacyBridge_DefaultContextPreservesResultAndOriginalFault()
    {
        await Task.Run(() =>
        {
            Try.CreateFromTask(() => Task.FromResult(5)).Value.ShouldBe(5);
            var original = new ArgumentException("original");
            Try.CreateFromTask(() => Task.FromException<int>(original)).Exception.InnerException.ShouldBeSameAs(original);
            Try.CreateFromTask<int>(() => null).ExceptionAs<ArgumentNullException>().ShouldNotBeNull();
            Try.CreateFromTask<int>(null).ExceptionAs<ArgumentNullException>().ShouldNotBeNull();
        });
    }
}
