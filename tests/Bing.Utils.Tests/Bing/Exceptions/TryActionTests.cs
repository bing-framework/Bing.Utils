using Bing.Exceptions;

namespace Bing.Utils.Tests.Bing.Exceptions;

/// <summary>
/// <see cref="TryAction"/>（SuccessAction / FailureAction）单元测试
/// </summary>
public class TryActionTests
{
    // ─────────────────────────────────────────────────────────────────
    // Try.Invoke (no-arg)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证操作执行成功后返回成功状态。
    /// </summary>
    [Fact]
    public void Invoke_ActionSucceeds_ReturnsSuccessAction()
    {
        var called = false;
        var result = Try.Invoke(() => { called = true; });
        called.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
    }

    /// <summary>
    /// 验证操作抛出异常后返回失败状态及异常。
    /// </summary>
    [Fact]
    public void Invoke_ActionThrows_ReturnsFailureAction()
    {
        var result = Try.Invoke(() => throw new InvalidOperationException("boom"));
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Exception.ShouldNotBeNull();
    }

    /// <summary>
    /// 验证空操作返回失败状态。
    /// </summary>
    [Fact]
    public void Invoke_NullAction_ThrowsOrReturnsFailure()
    {
        // null action → ArgumentNullException gets wrapped in FailureAction
        var result = Try.Invoke((Action)null!);
        result.IsFailure.ShouldBeTrue();
    }

    // ─────────────────────────────────────────────────────────────────
    // Try.Invoke<T> (single-arg)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证带参数操作接收输入并返回成功状态。
    /// </summary>
    [Fact]
    public void Invoke_WithArg_ActionSucceeds_ReturnsSuccess()
    {
        var captured = 0;
        var result = Try.Invoke((int x) => { captured = x; }, 42);
        result.IsSuccess.ShouldBeTrue();
        captured.ShouldBe(42);
    }

    /// <summary>
    /// 验证带参数操作失败时保留原异常消息。
    /// </summary>
    [Fact]
    public void Invoke_WithArg_ActionThrows_ReturnsFailure()
    {
        var result = Try.Invoke((int _) => throw new Exception("fail"), 0);
        result.IsFailure.ShouldBeTrue();
        result.Exception.InnerException!.Message.ShouldBe("fail");
    }

    /// <summary>
    /// 验证双参数操作接收输入并返回成功状态。
    /// </summary>
    [Fact]
    public void Invoke_WithTwoArgs_ActionSucceeds_ReturnsSuccess()
    {
        var sum = 0;
        var result = Try.Invoke((int a, int b) => { sum = a + b; }, 3, 4);
        result.IsSuccess.ShouldBeTrue();
        sum.ShouldBe(7);
    }

    // ─────────────────────────────────────────────────────────────────
    // SuccessAction.Exception / Cause
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证成功操作结果不包含异常。
    /// </summary>
    [Fact]
    public void SuccessAction_Exception_IsDefault()
    {
        var result = Try.Invoke(() => { });
        result.Exception.ShouldBeNull();
    }

    /// <summary>
    /// 验证成功操作结果的失败原因为空文本。
    /// </summary>
    [Fact]
    public void SuccessAction_Cause_IsEmpty()
    {
        var result = Try.Invoke(() => { });
        result.Cause.ShouldBe(string.Empty);
    }

    // ─────────────────────────────────────────────────────────────────
    // FailureAction.Exception / Cause
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证失败操作结果包装原异常。
    /// </summary>
    [Fact]
    public void FailureAction_Exception_ContainsOriginal()
    {
        var result = Try.Invoke(() => throw new ArgumentException("arg"));
        result.IsFailure.ShouldBeTrue();
        result.Exception.InnerException.ShouldBeOfType<ArgumentException>();
    }

    /// <summary>
    /// 验证失败操作结果保留指定原因。
    /// </summary>
    [Fact]
    public void FailureAction_WithCause_CauseIsPreserved()
    {
        var result = Try.Invoke<int>((int _) => throw new Exception("err"), 0, cause: "my-cause");
        result.Cause.ShouldBe("my-cause");
    }

    // ─────────────────────────────────────────────────────────────────
    // SuccessAction.Recover → returns self
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证成功结果恢复后仍返回原实例。
    /// </summary>
    [Fact]
    public void SuccessAction_Recover_ReturnsSelf()
    {
        var result = Try.Invoke(() => { });
        var recovered = result.Recover(_ => { });
        recovered.IsSuccess.ShouldBeTrue();
        recovered.ShouldBeSameAs(result);
    }

    /// <summary>
    /// 验证成功结果经结果恢复后仍为成功状态。
    /// </summary>
    [Fact]
    public void SuccessAction_RecoverWith_ReturnsSelf()
    {
        var result = Try.Invoke(() => { });
        var recovered = result.RecoverWith(_ => Try.Invoke(() => { }));
        recovered.IsSuccess.ShouldBeTrue();
    }

    // ─────────────────────────────────────────────────────────────────
    // FailureAction.Recover → wraps recovery in new Try.Invoke
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证失败结果经成功恢复后转为成功状态。
    /// </summary>
    [Fact]
    public void FailureAction_Recover_RecoverySucceeds_ReturnsSuccessAction()
    {
        var result = Try.Invoke(() => throw new Exception("x"));
        var recovered = result.Recover(_ => { /* handle */ });
        recovered.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// 验证恢复操作抛出异常后返回失败状态。
    /// </summary>
    [Fact]
    public void FailureAction_Recover_RecoveryThrows_ReturnsNewFailure()
    {
        var result = Try.Invoke(() => throw new Exception("original"));
        var recovered = result.Recover(_ => throw new Exception("recovery-fail"));
        recovered.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// 验证失败结果采用成功的恢复结果。
    /// </summary>
    [Fact]
    public void FailureAction_RecoverWith_RecoverySucceeds_ReturnsSuccess()
    {
        var result = Try.Invoke(() => throw new Exception("x"));
        var recovered = result.RecoverWith(_ => Try.Invoke(() => { }));
        recovered.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// 验证结果恢复委托抛出异常后返回失败状态。
    /// </summary>
    [Fact]
    public void FailureAction_RecoverWith_RecoveryThrows_ReturnsFailure()
    {
        var result = Try.Invoke(() => throw new Exception("x"));
        var recovered = result.RecoverWith(_ => throw new Exception("rethrow"));
        recovered.IsFailure.ShouldBeTrue();
    }

    // ─────────────────────────────────────────────────────────────────
    // SuccessAction.Tap
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证成功结果调用成功回调。
    /// </summary>
    [Fact]
    public void SuccessAction_Tap_InvokesSuccessCallback()
    {
        var called = false;
        var result = Try.Invoke(() => { });
        result.Tap(() => called = true, _ => { });
        called.ShouldBeTrue();
    }

    /// <summary>
    /// 验证成功结果不调用失败回调。
    /// </summary>
    [Fact]
    public void SuccessAction_Tap_DoesNotInvokeFailureCallback()
    {
        var failureCalled = false;
        var result = Try.Invoke(() => { });
        result.Tap(() => { }, _ => failureCalled = true);
        failureCalled.ShouldBeFalse();
    }

    /// <summary>
    /// 验证成功结果执行回调后返回原实例。
    /// </summary>
    [Fact]
    public void SuccessAction_Tap_ReturnsSelf()
    {
        var result = Try.Invoke(() => { });
        var returned = result.Tap(() => { }, _ => { });
        returned.ShouldBeSameAs(result);
    }

    // ─────────────────────────────────────────────────────────────────
    // FailureAction.Tap
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证失败结果调用失败回调。
    /// </summary>
    [Fact]
    public void FailureAction_Tap_InvokesFailureCallback()
    {
        var called = false;
        var result = Try.Invoke(() => throw new Exception("x"));
        result.Tap(() => { }, _ => called = true);
        called.ShouldBeTrue();
    }

    /// <summary>
    /// 验证失败结果不调用成功回调。
    /// </summary>
    [Fact]
    public void FailureAction_Tap_DoesNotInvokeSuccessCallback()
    {
        var successCalled = false;
        var result = Try.Invoke(() => throw new Exception("x"));
        result.Tap(() => successCalled = true, _ => { });
        successCalled.ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // Deconstruct
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证成功结果解构为成功标记和空异常。
    /// </summary>
    [Fact]
    public void SuccessAction_Deconstruct_IsSuccessTrue_ExceptionNull()
    {
        var result = Try.Invoke(() => { });
        result.Deconstruct(out var isSuccess, out var exception);
        isSuccess.ShouldBeTrue();
        exception.ShouldBeNull();
    }

    /// <summary>
    /// 验证失败结果解构为失败标记和非空异常。
    /// </summary>
    [Fact]
    public void FailureAction_Deconstruct_IsSuccessFalse_ExceptionNotNull()
    {
        var result = Try.Invoke(() => throw new Exception("x"));
        result.Deconstruct(out var isSuccess, out var exception);
        isSuccess.ShouldBeFalse();
        exception.ShouldNotBeNull();
    }
}
