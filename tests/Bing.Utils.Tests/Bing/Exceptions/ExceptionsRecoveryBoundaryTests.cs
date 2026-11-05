using System.Threading;
using Bing.Exceptions;

namespace Bing.Utils.Tests.Bing.Exceptions;

/// <summary>
/// 验证失败恢复和异步安全取值不会泄漏空结果或空任务。
/// </summary>
public class ExceptionsRecoveryBoundaryTests
{
    /// <summary>
    /// 两种值恢复和操作恢复返回空结果时，均产生可继续处理的失败。
    /// </summary>
    [Fact]
    public void RecoverWith_NullResult_ReturnsFailure()
    {
        var valueError = new InvalidOperationException("read");
        var actionError = new InvalidOperationException("invoke");
        var valueFailure = Try.LiftException<int>(valueError, "read-cause");
        var actionFailure = Try.Invoke((Action)(() => throw actionError), "invoke-cause");
        var callbackCalls = 0;

        var wrapperResult = valueFailure.RecoverWith((TryCreatingValueException exception) =>
        {
            exception.ShouldBeSameAs(valueFailure.Exception);
            exception.InnerException.ShouldBeSameAs(valueError);
            exception.Cause.ShouldBe("read-cause");
            callbackCalls++;
            return null;
        });
        var detailResult = valueFailure.RecoverWith((Exception exception, string cause) =>
        {
            exception.ShouldBeSameAs(valueError);
            cause.ShouldBe("read-cause");
            callbackCalls++;
            return null;
        });
        var actionResult = actionFailure.RecoverWith((Exception exception) =>
        {
            exception.ShouldBeSameAs(actionFailure.Exception);
            exception.InnerException.ShouldBeSameAs(actionError);
            actionFailure.Cause.ShouldBe("invoke-cause");
            callbackCalls++;
            return null;
        });

        foreach (var result in new[] { wrapperResult, detailResult })
        {
            result.ShouldNotBeNull();
            result.IsFailure.ShouldBeTrue();
            result.ExceptionAs<InvalidOperationException>().ShouldNotBeNull();
        }
        actionResult.ShouldNotBeNull();
        actionResult.IsFailure.ShouldBeTrue();
        actionResult.ExceptionAs<InvalidOperationException>().ShouldNotBeNull();
        callbackCalls.ShouldBe(3);
    }

    /// <summary>
    /// 空的操作恢复委托转换为带参数信息的失败，而不是抛出空引用异常。
    /// </summary>
    [Fact]
    public void FailureActionRecoverWith_NullDelegate_ReturnsArgumentFailure()
    {
        var failed = Try.Invoke((Action)(() => throw new InvalidOperationException("invoke")));

        var result = failed.RecoverWith((Func<Exception, TryAction>)null);

        result.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue();
        result.ExceptionAs<ArgumentNullException>().ParamName.ShouldBe("recoverFunction");
    }

    /// <summary>
    /// 有效恢复结果按原实例返回，成功分支不调用恢复函数。
    /// </summary>
    [Fact]
    public void RecoverWith_PreservesReturnedResultAndShortCircuitsSuccess()
    {
        var failed = Try.LiftException<int>(new InvalidOperationException("source"));
        Try<int> recoveredSuccess = Try.LiftValue(42);
        var valueRecoveryError = new ArgumentException("recovery");
        Try<int> recoveredFailure = Try.LiftException<int>(valueRecoveryError, "recovery-cause");
        var failedAction = Try.Invoke((Action)(() => throw new InvalidOperationException("source")));
        TryAction recoveredActionSuccess = Try.Invoke((Action)(() => { }));
        var actionRecoveryError = new ArgumentException("action recovery");
        TryAction recoveredActionFailure = Try.Invoke((Action)(() => throw actionRecoveryError), "action-recovery-cause");
        var valueSuccessCalls = 0;
        var valueFailureCalls = 0;
        var actionSuccessCalls = 0;
        var actionFailureCalls = 0;

        failed.RecoverWith(_ =>
        {
            valueSuccessCalls++;
            return recoveredSuccess;
        }).ShouldBeSameAs(recoveredSuccess);
        failed.RecoverWith((Exception _, string _) =>
        {
            valueFailureCalls++;
            return recoveredFailure;
        }).ShouldBeSameAs(recoveredFailure);
        failedAction.RecoverWith(_ =>
        {
            actionSuccessCalls++;
            return recoveredActionSuccess;
        }).ShouldBeSameAs(recoveredActionSuccess);
        failedAction.RecoverWith(_ =>
        {
            actionFailureCalls++;
            return recoveredActionFailure;
        }).ShouldBeSameAs(recoveredActionFailure);

        recoveredFailure.Exception.InnerException.ShouldBeSameAs(valueRecoveryError);
        recoveredFailure.Exception.Cause.ShouldBe("recovery-cause");
        recoveredActionFailure.Exception.InnerException.ShouldBeSameAs(actionRecoveryError);
        recoveredActionFailure.Cause.ShouldBe("action-recovery-cause");
        valueSuccessCalls.ShouldBe(1);
        valueFailureCalls.ShouldBe(1);
        actionSuccessCalls.ShouldBe(1);
        actionFailureCalls.ShouldBe(1);

        var successfulValue = Try.LiftValue(7);
        var successfulAction = Try.Invoke((Action)(() => { }));
        var successfulBranchCalls = 0;
        successfulValue.RecoverWith(_ =>
        {
            successfulBranchCalls++;
            return null;
        }).ShouldBeSameAs(successfulValue);
        successfulAction.RecoverWith(_ =>
        {
            successfulBranchCalls++;
            return null;
        }).ShouldBeSameAs(successfulAction);
        successfulBranchCalls.ShouldBe(0);
    }

    /// <summary>
    /// 恢复回调异常保留原异常实例，并返回失败结果。
    /// </summary>
    [Fact]
    public void RecoverWith_CallbackExceptionIsPreserved()
    {
        var callbackError = new InvalidOperationException("recovery callback");
        var valueFailure = Try.LiftException<int>(new Exception("source"));
        var actionFailure = Try.Invoke((Action)(() => throw new Exception("source")));
        var callbackCalls = 0;

        var valueResult = valueFailure.RecoverWith(_ =>
        {
            callbackCalls++;
            throw callbackError;
        });
        var actionResult = actionFailure.RecoverWith(_ =>
        {
            callbackCalls++;
            throw callbackError;
        });

        valueResult.IsFailure.ShouldBeTrue();
        valueResult.Exception.InnerException.ShouldBeSameAs(callbackError);
        valueResult.Exception.Cause.ShouldBe("An exception occurred during recovery.");
        actionResult.IsFailure.ShouldBeTrue();
        actionResult.Exception.InnerException.ShouldBeSameAs(callbackError);
        actionResult.Cause.ShouldBe("An exception occurred during recovery.");
        callbackCalls.ShouldBe(2);
    }

    /// <summary>
    /// 三种异步 fallback 返回空任务时，直接入口和输出参数入口均返回故障任务。
    /// </summary>
    [Fact]
    public async Task GetSafeValueAsync_NullReturnedTask_ReturnsFaultedTask()
    {
        var original = new InvalidOperationException("lookup");
        var failed = Try.LiftException<int>(original, "lookup-cause");
        var simpleTask = failed.GetSafeValueAsync((Func<Task<int>>)(() => null));
        var wrapperTask = failed.GetSafeValueAsync((TryCreatingValueException _) => (Task<int>)null);
        var detailTask = failed.GetSafeValueAsync((Exception exception, string cause) =>
        {
            exception.ShouldBeSameAs(original);
            cause.ShouldBe("lookup-cause");
            return (Task<int>)null;
        });
        Task<int> simpleOutTask;
        Task<int> wrapperOutTask;
        Task<int> detailOutTask;
        failed.GetSafeValueOutAsync(out simpleOutTask, (Func<Task<int>>)(() => null));
        failed.GetSafeValueOutAsync(out wrapperOutTask, (TryCreatingValueException _) => (Task<int>)null);
        failed.GetSafeValueOutAsync(out detailOutTask, (Exception _, string _) => (Task<int>)null);

        foreach (var task in new[] { simpleTask, wrapperTask, detailTask, simpleOutTask, wrapperOutTask, detailOutTask })
        {
            task.ShouldNotBeNull();
            task.IsFaulted.ShouldBeTrue();
            await Should.ThrowAsync<InvalidOperationException>(() => task);
        }
    }

    /// <summary>
    /// 空 fallback 仍返回默认值任务，成功结果不调用 fallback，正常任务保持身份。
    /// </summary>
    [Fact]
    public async Task GetSafeValueAsync_PreservesFallbackAndTaskIdentity()
    {
        var failed = Try.LiftException<string>(new InvalidOperationException("source"));
        var calls = 0;
        var nullFallbackTasks = new[]
        {
            failed.GetSafeValueAsync((Func<Task<string>>)null),
            failed.GetSafeValueAsync((Func<TryCreatingValueException, Task<string>>)null),
            failed.GetSafeValueAsync((Func<Exception, string, Task<string>>)null)
        };
        foreach (var task in nullFallbackTasks)
        {
            task.ShouldNotBeNull();
            (await task).ShouldBeNull();
        }

        var originalTask = Task.FromResult<string>(null);
        var resultTask = failed.GetSafeValueAsync(() =>
        {
            calls++;
            return originalTask;
        });
        resultTask.ShouldBeSameAs(originalTask);
        (await resultTask).ShouldBeNull();
        calls.ShouldBe(1);

        var success = Try.LiftValue("available");
        var successTask = success.GetSafeValueAsync(() =>
        {
            calls++;
            return Task.FromResult("fallback");
        });
        (await successTask).ShouldBe("available");
        calls.ShouldBe(1);
    }

    /// <summary>
    /// 既有故障与取消任务保留 await 时的异常和取消令牌。
    /// </summary>
    [Fact]
    public async Task GetSafeValueAsync_PreservesFaultAndCancellation()
    {
        var failed = Try.LiftException<int>(new InvalidOperationException("source"));
        var original = new ArgumentException("fallback fault");
        var faultedTask = Task.FromException<int>(original);
        failed.GetSafeValueAsync(() => faultedTask).ShouldBeSameAs(faultedTask);
        (await Should.ThrowAsync<ArgumentException>(() => faultedTask)).ShouldBeSameAs(original);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceledTask = Task.FromCanceled<int>(cancellation.Token);
        failed.GetSafeValueAsync(() => canceledTask).ShouldBeSameAs(canceledTask);
        canceledTask.IsCanceled.ShouldBeTrue();
        (await Should.ThrowAsync<OperationCanceledException>(() => canceledTask))
            .CancellationToken.ShouldBe(cancellation.Token);
    }

    /// <summary>
    /// fallback 同步抛错保持同步传播，避免改变调用方原有异常时机。
    /// </summary>
    [Fact]
    public void GetSafeValueAsync_SynchronousFallbackExceptionIsPropagatedImmediately()
    {
        var failed = Try.LiftException<int>(new InvalidOperationException("source"));
        var original = new ArgumentException("fallback callback");

        Should.Throw<ArgumentException>(() => failed.GetSafeValueAsync((Func<Task<int>>)(() => throw original)))
            .ShouldBeSameAs(original);
    }

    /// <summary>
    /// 三种异步 fallback 委托形态均保留任务身份、故障和取消行为，并在成功时短路。
    /// </summary>
    [Fact]
    public async Task GetSafeValueAsync_AllCallbackShapes_PreserveTaskContract()
    {
        var original = new InvalidOperationException("lookup");
        var failed = Try.LiftException<int>(original, "lookup-cause");
        var succeeded = Try.LiftValue(42);
        Func<Try<int>, Func<Task<int>>, Task<int>>[] callbackShapes =
        {
            (result, fallback) => result.GetSafeValueAsync(fallback),
            (result, fallback) => result.GetSafeValueAsync(exception =>
            {
                exception.ShouldBeSameAs(result.Exception);
                return fallback();
            }),
            (result, fallback) => result.GetSafeValueAsync((exception, cause) =>
            {
                exception.ShouldBeSameAs(result.Exception?.InnerException);
                cause.ShouldBe(result.Exception?.Cause);
                return fallback();
            })
        };

        foreach (var invoke in callbackShapes)
            await AssertAsyncFallbackContract(invoke, failed, succeeded);
    }

    /// <summary>
    /// 三个 Out 入口返回原结果链，并将 callback 返回的同一非空任务写入输出参数。
    /// </summary>
    [Fact]
    public async Task GetSafeValueOutAsync_AllCallbackShapes_PreserveResultAndTaskIdentity()
    {
        var original = new InvalidOperationException("lookup");
        var failed = Try.LiftException<int>(original, "lookup-cause");
        var simpleTask = Task.FromResult(1);
        var wrapperTask = Task.FromResult(2);
        var detailTask = Task.FromResult(3);

        var simpleResult = failed.GetSafeValueOutAsync(out var simpleOutput, (Func<Task<int>>)(() => simpleTask));
        var wrapperResult = failed.GetSafeValueOutAsync(out var wrapperOutput,
            (Func<TryCreatingValueException, Task<int>>)(exception =>
            {
                exception.ShouldBeSameAs(failed.Exception);
                return wrapperTask;
            }));
        var detailResult = failed.GetSafeValueOutAsync(out var detailOutput,
            (Func<Exception, string, Task<int>>)((exception, cause) =>
            {
                exception.ShouldBeSameAs(original);
                cause.ShouldBe("lookup-cause");
                return detailTask;
            }));

        simpleResult.ShouldBeSameAs(failed);
        wrapperResult.ShouldBeSameAs(failed);
        detailResult.ShouldBeSameAs(failed);
        simpleOutput.ShouldNotBeNull();
        wrapperOutput.ShouldNotBeNull();
        detailOutput.ShouldNotBeNull();
        simpleOutput.ShouldBeSameAs(simpleTask);
        wrapperOutput.ShouldBeSameAs(wrapperTask);
        detailOutput.ShouldBeSameAs(detailTask);
        (await simpleOutput).ShouldBe(1);
        (await wrapperOutput).ShouldBe(2);
        (await detailOutput).ShouldBe(3);
    }

    /// <summary>
    /// 三个 Out 重载的空回调均返回类型默认值。
    /// </summary>
    [Fact]
    public async Task GetSafeValueOutAsync_NullCallbacksReturnDefaultValue()
    {
        var failed = Try.LiftException<int>(new InvalidOperationException("source"));

        var simpleResult = failed.GetSafeValueOutAsync(out var simpleOutput, (Func<Task<int>>)null);
        var wrapperResult = failed.GetSafeValueOutAsync(out var wrapperOutput,
            (Func<TryCreatingValueException, Task<int>>)null);
        var detailResult = failed.GetSafeValueOutAsync(out var detailOutput,
            (Func<Exception, string, Task<int>>)null);

        simpleResult.ShouldBeSameAs(failed);
        wrapperResult.ShouldBeSameAs(failed);
        detailResult.ShouldBeSameAs(failed);
        _ = simpleOutput.ShouldNotBeNull();
        _ = wrapperOutput.ShouldNotBeNull();
        _ = detailOutput.ShouldNotBeNull();
        (await simpleOutput).ShouldBe(default(int));
        (await wrapperOutput).ShouldBe(default(int));
        (await detailOutput).ShouldBe(default(int));
    }

    /// <summary>
    /// 三个 Out 重载均保留故障与取消任务的身份和 await 行为。
    /// </summary>
    [Fact]
    public async Task GetSafeValueOutAsync_AllCallbackShapes_PreserveFaultAndCancellation()
    {
        var failed = Try.LiftException<int>(new InvalidOperationException("source"));
        var callbackError = new ArgumentException("fallback fault");
        var faultedTask = Task.FromException<int>(callbackError);

        failed.GetSafeValueOutAsync(out var simpleFault, (Func<Task<int>>)(() => faultedTask))
            .ShouldBeSameAs(failed);
        failed.GetSafeValueOutAsync(out var wrapperFault,
            (Func<TryCreatingValueException, Task<int>>)(_ => faultedTask)).ShouldBeSameAs(failed);
        failed.GetSafeValueOutAsync(out var detailFault,
            (Func<Exception, string, Task<int>>)((_, _) => faultedTask)).ShouldBeSameAs(failed);

        simpleFault.ShouldBeSameAs(faultedTask);
        wrapperFault.ShouldBeSameAs(faultedTask);
        detailFault.ShouldBeSameAs(faultedTask);
        (await Should.ThrowAsync<ArgumentException>(() => simpleFault)).ShouldBeSameAs(callbackError);
        (await Should.ThrowAsync<ArgumentException>(() => wrapperFault)).ShouldBeSameAs(callbackError);
        (await Should.ThrowAsync<ArgumentException>(() => detailFault)).ShouldBeSameAs(callbackError);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceledTask = Task.FromCanceled<int>(cancellation.Token);

        failed.GetSafeValueOutAsync(out var simpleCanceled, (Func<Task<int>>)(() => canceledTask))
            .ShouldBeSameAs(failed);
        failed.GetSafeValueOutAsync(out var wrapperCanceled,
            (Func<TryCreatingValueException, Task<int>>)(_ => canceledTask)).ShouldBeSameAs(failed);
        failed.GetSafeValueOutAsync(out var detailCanceled,
            (Func<Exception, string, Task<int>>)((_, _) => canceledTask)).ShouldBeSameAs(failed);

        simpleCanceled.ShouldBeSameAs(canceledTask);
        wrapperCanceled.ShouldBeSameAs(canceledTask);
        detailCanceled.ShouldBeSameAs(canceledTask);
        simpleCanceled.IsCanceled.ShouldBeTrue();
        wrapperCanceled.IsCanceled.ShouldBeTrue();
        detailCanceled.IsCanceled.ShouldBeTrue();
        (await Should.ThrowAsync<OperationCanceledException>(() => simpleCanceled))
            .CancellationToken.ShouldBe(cancellation.Token);
        (await Should.ThrowAsync<OperationCanceledException>(() => wrapperCanceled))
            .CancellationToken.ShouldBe(cancellation.Token);
        (await Should.ThrowAsync<OperationCanceledException>(() => detailCanceled))
            .CancellationToken.ShouldBe(cancellation.Token);
    }

    /// <summary>
    /// Invoke 和旧 InvokeAsync 的空输入作为带参数信息的失败结果返回。
    /// </summary>
    [Fact]
    public async Task Invoke_NullInputs_ReturnArgumentFailures()
    {
        var actionFailure = Try.Invoke((Action)null);
        var genericFailure = Try.Invoke((Action<int>)null, 1);
        var asyncFailure = await Try.InvokeAsync((Task<Action>)null);
        var asyncGenericFailure = await Try.InvokeAsync(Task.FromResult<Action<int>>(null), 1);

        actionFailure.ExceptionAs<ArgumentNullException>().ParamName.ShouldBe("invokeAction");
        genericFailure.ExceptionAs<ArgumentNullException>().ParamName.ShouldBe("invokeAction");
        asyncFailure.ExceptionAs<ArgumentNullException>().ParamName.ShouldBe("invokeAsyncAction");
        asyncGenericFailure.ExceptionAs<ArgumentNullException>().ParamName.ShouldBe("invokeAction");
    }

    private static async Task AssertAsyncFallbackContract(
        Func<Try<int>, Func<Task<int>>, Task<int>> invoke,
        Try<int> failed,
        Try<int> succeeded)
    {
        var callbackCalls = 0;
        var normalTask = Task.FromResult(7);
        invoke(failed, () =>
        {
            callbackCalls++;
            return normalTask;
        }).ShouldBeSameAs(normalTask);
        (await normalTask).ShouldBe(7);
        callbackCalls.ShouldBe(1);

        var fault = new ArgumentException("fallback fault");
        var faultedTask = Task.FromException<int>(fault);
        callbackCalls = 0;
        var faultResult = invoke(failed, () =>
        {
            callbackCalls++;
            return faultedTask;
        });
        faultResult.ShouldBeSameAs(faultedTask);
        (await Should.ThrowAsync<ArgumentException>(() => faultResult)).ShouldBeSameAs(fault);
        callbackCalls.ShouldBe(1);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceledTask = Task.FromCanceled<int>(cancellation.Token);
        callbackCalls = 0;
        var canceledResult = invoke(failed, () =>
        {
            callbackCalls++;
            return canceledTask;
        });
        canceledResult.ShouldBeSameAs(canceledTask);
        canceledResult.IsCanceled.ShouldBeTrue();
        (await Should.ThrowAsync<OperationCanceledException>(() => canceledResult))
            .CancellationToken.ShouldBe(cancellation.Token);
        callbackCalls.ShouldBe(1);

        var synchronousError = new InvalidOperationException("synchronous fallback error");
        callbackCalls = 0;
        Should.Throw<InvalidOperationException>(() => invoke(failed, () =>
        {
            callbackCalls++;
            throw synchronousError;
        })).ShouldBeSameAs(synchronousError);
        callbackCalls.ShouldBe(1);

        callbackCalls = 0;
        var shortCircuited = invoke(succeeded, () =>
        {
            callbackCalls++;
            return Task.FromResult(-1);
        });
        (await shortCircuited).ShouldBe(42);
        callbackCalls.ShouldBe(0);
    }
}
