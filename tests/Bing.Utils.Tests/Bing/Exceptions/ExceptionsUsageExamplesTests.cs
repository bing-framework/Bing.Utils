using System.Threading;
using Bing.Exceptions;

namespace Bing.Utils.Tests.Bing.Exceptions;

/// <summary>
/// 以接近业务代码的场景展示 Exceptions 的常见用法。
/// </summary>
public class ExceptionsUsageExamplesTests
{
    /// <summary>
    /// 成功结果可以解构出值、成功状态和空异常。
    /// </summary>
    [Fact(DisplayName = "使用案例：读取成功结果")]
    public void SuccessfulResult_CanBeDeconstructed()
    {
        Try<int> result = Try.Create(() => 2 + 3);

        var (value, isSuccess, exception) = result;

        value.ShouldBe(5);
        isSuccess.ShouldBeTrue();
        exception.ShouldBeNull();
    }

    /// <summary>
    /// 失败结果解构不会读取失败时会抛异常的 Value。
    /// </summary>
    [Fact(DisplayName = "使用案例：解构失败并查看原因")]
    public void FailedResult_CanBeInspectedWithoutThrowing()
    {
        var original = new InvalidOperationException("库存服务暂不可用");
        Try<int> result = Try.LiftException<int>(original, "查询库存");

        var (value, isSuccess, exception) = result;

        value.ShouldBe(default);
        isSuccess.ShouldBeFalse();
        exception.ShouldBeSameAs(result.Exception);
        exception.InnerException.ShouldBeSameAs(original);
        result.Exception.Cause.ShouldBe("查询库存");
    }

    /// <summary>
    /// 订单金额可以通过 LINQ 风格组合逐步计算并匹配结果。
    /// </summary>
    [Fact(DisplayName = "使用案例：计算订单金额")]
    public void OrderPricing_ComposesSuccessfulSteps()
    {
        var order = new Order("A-100", Quantity: 2, UnitPrice: 12.5m);

        var total = (from value in Try.LiftValue(order)
                     where value.Quantity > 0
                     select value.Quantity * value.UnitPrice)
            .Match(
                value => $"{order.Id}: {value:C}",
                (TryCreatingValueException _) => "订单金额计算失败");

        total.ShouldBe($"A-100: {25m:C}");
    }

    /// <summary>
    /// 库存不足时后续计算短路，调用方可以选择业务兜底值。
    /// </summary>
    [Fact(DisplayName = "使用案例：库存不足时短路并恢复")]
    public void OrderWithoutStock_ShortCircuitsAndCanRecover()
    {
        var priceCalls = 0;
        var order = Try.LiftValue(new Order("A-101", Quantity: 0, UnitPrice: 10m))
            .Where(value => value.Quantity > 0)
            .Select(value =>
            {
                priceCalls++;
                return value.Quantity * value.UnitPrice;
            });

        var displayTotal = order.Recover(_ => 0m).Value;

        order.ExceptionAs<InvalidOperationException>().Message.ShouldBe("Predicate not satisfied");
        priceCalls.ShouldBe(0);
        displayTotal.ShouldBe(0m);
    }

    /// <summary>
    /// RecoverWith 可以检查原异常和业务原因后返回显式兜底结果。
    /// </summary>
    [Fact(DisplayName = "使用案例：根据失败原因选择恢复值")]
    public void RecoverWith_SelectsFallbackFromFailureDetails()
    {
        var original = new TimeoutException("库存服务超时");
        var failed = Try.LiftException<int>(original, "查询库存");
        var callbackCalls = 0;

        var recovered = failed.RecoverWith((Exception exception, string cause) =>
        {
            exception.ShouldBeSameAs(original);
            cause.ShouldBe("查询库存");
            callbackCalls++;
            return Try.LiftValue(0);
        });

        recovered.IsSuccess.ShouldBeTrue();
        recovered.Value.ShouldBe(0);
        callbackCalls.ShouldBe(1);
        failed.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// 异步安全取值可以从失败上下文读取缓存结果。
    /// </summary>
    [Fact(DisplayName = "使用案例：读取失败时异步查询缓存")]
    public async Task GetSafeValueAsync_UsesCachedValueForFailure()
    {
        var failed = Try.LiftException<string>(new TimeoutException("库存服务暂不可用"), "查询库存");

        var value = await failed.GetSafeValueAsync((Exception exception, string cause) =>
        {
            exception.ShouldBeOfType<TimeoutException>();
            cause.ShouldBe("查询库存");
            return Task.FromResult("上次已知结果");
        });

        value.ShouldBe("上次已知结果");
    }

    /// <summary>
    /// 恢复回调再次失败时，调用方仍能读取恢复异常。
    /// </summary>
    [Fact(DisplayName = "使用案例：恢复过程再次失败时保留新异常")]
    public void RecoverWith_PreservesExceptionFromRecovery()
    {
        var failed = Try.Invoke((Action)(() => throw new InvalidOperationException("提交失败")));
        var recoveryError = new IOException("备用处理也失败");
        var recoveryCalls = 0;

        var recovered = failed.RecoverWith(_ =>
        {
            recoveryCalls++;
            throw recoveryError;
        });

        recovered.IsFailure.ShouldBeTrue();
        recovered.ExceptionAs<IOException>().ShouldBeSameAs(recoveryError);
        recovered.Cause.ShouldBe("An exception occurred during recovery.");
        recoveryCalls.ShouldBe(1);

        Exception observedFailure = null;
        var successCalls = 0;
        var observed = recovered.Tap(
            successAction: () => successCalls++,
            failureAction: exception => observedFailure = exception);
        observed.ShouldBeSameAs(recovered);
        observedFailure.ShouldBeSameAs(recovered.Exception);
        successCalls.ShouldBe(0);

        var final = observed.RecoverWith(_ => Try.Invoke(() => successCalls++));
        final.IsSuccess.ShouldBeTrue();
        successCalls.ShouldBe(1);
    }

    /// <summary>
    /// 恢复失败后，组合操作保留错误并短路，后续仍可恢复和计算。
    /// </summary>
    [Fact(DisplayName = "使用案例：恢复再次失败后继续组合和兜底")]
    public void RecoverWith_RecoveryFails_CanContinueAndRecover()
    {
        var source = Try.LiftException<int>(new TimeoutException("库存服务超时"), "查询库存");
        var recoveryError = new IOException("备用库存服务也失败");
        var recoveryCalls = 0;
        var failedRecovery = source.RecoverWith(_ =>
        {
            recoveryCalls++;
            throw recoveryError;
        });
        var calculationCalls = 0;

        var mapped = failedRecovery.Select(value =>
        {
            calculationCalls++;
            return value * 2;
        });

        mapped.IsFailure.ShouldBeTrue();
        mapped.Exception.ShouldBeSameAs(failedRecovery.Exception);
        mapped.Exception.InnerException.ShouldBeSameAs(recoveryError);
        mapped.Exception.Cause.ShouldBe("An exception occurred during recovery.");
        recoveryCalls.ShouldBe(1);
        calculationCalls.ShouldBe(0);

        Exception observedError = null;
        string observedCause = null;
        var final = mapped.Recover((Exception exception, string cause) =>
        {
            observedError = exception;
            observedCause = cause;
            return 5;
        });

        observedError.ShouldBeSameAs(recoveryError);
        observedCause.ShouldBe("An exception occurred during recovery.");
        final.IsSuccess.ShouldBeTrue();
        final.Select(value => value * 2).Value.ShouldBe(10);
    }

    /// <summary>
    /// Tap 可记录失败信息，同时保留原失败结果供后续处理。
    /// </summary>
    [Fact(DisplayName = "使用案例：记录失败但保留原结果")]
    public void Tap_ObservesFailureAndReturnsSameResult()
    {
        var original = new TimeoutException("库存查询超时");
        Try<int> result = Try.LiftException<int>(original, "查询库存");
        string recordedCause = null;

        var returned = result.Tap(failureAction: (_, cause) => recordedCause = cause);

        returned.ShouldBeSameAs(result);
        recordedCause.ShouldBe("查询库存");
        returned.ExceptionAs<TimeoutException>().ShouldBeSameAs(original);
    }

    /// <summary>
    /// Tap 和 Match 的业务回调异常会由调用方直接观察到。
    /// </summary>
    [Fact(DisplayName = "使用案例：副作用回调错误向外传播")]
    public void TapAndMatch_PropagateCallbackExceptions()
    {
        var result = Try.LiftValue(10);
        var tapError = new InvalidOperationException("日志服务失败");
        var matchError = new ArgumentException("展示失败");

        Should.Throw<InvalidOperationException>(() => result.Tap(_ => throw tapError))
            .ShouldBeSameAs(tapError);
        Should.Throw<ArgumentException>(() => result.Match<int>(
                _ => throw matchError,
                (TryCreatingValueException _) => 0))
            .ShouldBeSameAs(matchError);
    }

    /// <summary>
    /// 批量操作将单项异常变成失败结果，并继续处理后续订单。
    /// </summary>
    [Fact(DisplayName = "使用案例：批量处理时隔离单项失败")]
    public void InvokeRange_ContinuesAfterOneOrderFails()
    {
        var processed = new List<string>();
        var errors = Try.InvokeRange(
            new[] { "A-1", "A-2", "A-3" },
            id =>
            {
                if (id == "A-2")
                    throw new InvalidOperationException("订单暂时无法提交");
                processed.Add(id);
            },
            cause: "批量提交")
            .ToArray();

        errors.Select(item => item.IsSuccess).ShouldBe(new[] { true, false, true });
        errors[1].ExceptionAs<InvalidOperationException>().Message.ShouldBe("订单暂时无法提交");
        errors[1].Cause.ShouldBe("批量提交");
        processed.ShouldBe(new[] { "A-1", "A-3" });
    }

    /// <summary>
    /// CreateAsync 会等待真实异步操作完成后返回成功结果。
    /// </summary>
    [Fact(DisplayName = "使用案例：等待异步查询完成")]
    public async Task CreateAsync_WaitsForTheQueryResult()
    {
        var response = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        var pending = Try.CreateAsync(() => response.Task, "查询订单");
        pending.IsCompleted.ShouldBeFalse();

        response.SetResult(42);
        var result = await pending;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    /// <summary>
    /// 预取消异步查询会形成失败结果，并保留取消令牌。
    /// </summary>
    [Fact(DisplayName = "使用案例：把异步取消作为失败结果处理")]
    public async Task CreateAsync_PreCanceledRequestReturnsFailure()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var calls = 0;

        var task = Try.CreateAsync<int>(token =>
        {
            calls++;
            return Task.FromResult(1);
        }, cancellation.Token, "用户取消查询");
        var result = await task;

        task.Status.ShouldBe(TaskStatus.RanToCompletion);
        result.IsFailure.ShouldBeTrue();
        result.ExceptionAs<OperationCanceledException>().CancellationToken.ShouldBe(cancellation.Token);
        result.Exception.Cause.ShouldBe("用户取消查询");
        calls.ShouldBe(0);
    }

    /// <summary>
    /// InvokeTaskAsync 包装真实异步操作的失败，而不会把异步委托当作 Action 执行。
    /// </summary>
    [Fact(DisplayName = "使用案例：捕获异步提交失败")]
    public async Task InvokeTaskAsync_CapturesOperationFailure()
    {
        var original = new IOException("远端暂时不可用");

        var result = await Try.InvokeTaskAsync(
            () => Task.FromException(original),
            "提交订单");

        result.IsFailure.ShouldBeTrue();
        result.ExceptionAs<IOException>().ShouldBeSameAs(original);
        result.Cause.ShouldBe("提交订单");
    }

    /// <summary>
    /// 聚合异常的默认查找保持单链行为，显式打开后可以找到后续分支。
    /// </summary>
    [Fact(DisplayName = "使用案例：查找聚合异常的第二个分支")]
    public void AggregateException_SearchesLaterBranchOnlyWhenEnabled()
    {
        var target = new TimeoutException("支付网关超时");
        var aggregate = new AggregateException(
            new ArgumentException("参数无效"),
            new InvalidOperationException("包装节点", target));

        aggregate.Unwrap<TimeoutException>().ShouldBeNull();
        aggregate.Unwrap<TimeoutException>(includeAggregateExceptions: true).ShouldBeSameAs(target);
    }

    /// <summary>
    /// 异常诊断文本超过预算时会被截断。
    /// </summary>
    [Fact(DisplayName = "使用案例：限制异常诊断文本长度")]
    public void FormatMessage_LongMessagesRespectOutputBudget()
    {
        var exception = new InvalidOperationException(new string('x', 512));
        var options = new ExceptionFormatOptions
        {
            MaxOutputLength = 128
        };

        var message = exception.FormatMessage(options);
        var detail = ExceptionHelper.GetExceptionDetail(exception, options);

        message.Length.ShouldBeLessThanOrEqualTo(128);
        detail.Length.ShouldBeLessThanOrEqualTo(128);
        message.ShouldContain("[已截断：输出长度上限]");
        detail.ShouldContain("[已截断：输出长度上限]");
    }

    /// <summary>
    /// 默认诊断包含实际堆栈，显式隐藏后则省略堆栈字段。
    /// </summary>
    [Fact(DisplayName = "使用案例：按需显示异常堆栈")]
    public void FormatMessage_ShowsOrHidesARealStackTrace()
    {
        var exception = CaptureThrownException();
        var stack = exception.StackTrace;
        stack.ShouldNotBeNull();

        exception.FormatMessage().ShouldContain(stack);
        var options = new ExceptionFormatOptions { HideStackTrace = true };
        exception.FormatMessage(options).ShouldNotContain("异常堆栈：");
        ExceptionHelper.GetExceptionDetail(exception, options).ShouldNotContain("异常堆栈：");
    }

    private static Exception CaptureThrownException()
    {
        try
        {
            throw new InvalidOperationException("库存服务不可用");
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }

    private sealed record Order(string Id, int Quantity, decimal UnitPrice);
}
