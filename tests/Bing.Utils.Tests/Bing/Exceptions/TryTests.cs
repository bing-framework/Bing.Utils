using Bing.Exceptions;

namespace Bing.Utils.Tests.Bing.Exceptions;

/// <summary>
/// <see cref="Try{T}"/>（Success / Failure / TryExtensions）单元测试
/// </summary>
public class TryTests
{
    // ─────────────────────────────────────────────────────────────────
    // Try.LiftValue / Try.LiftException
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证普通值提升为成功结果并保留原值。
    /// </summary>
    [Fact]
    public void LiftValue_ReturnsSuccess()
    {
        var t = Try.LiftValue(42);
        t.IsSuccess.ShouldBeTrue();
        t.IsFailure.ShouldBeFalse();
        t.Value.ShouldBe(42);
    }

    /// <summary>
    /// 验证空引用值提升为成功结果。
    /// </summary>
    [Fact]
    public void LiftValue_NullReferenceType_ReturnsSuccess()
    {
        var t = Try.LiftValue<string>(null);
        t.IsSuccess.ShouldBeTrue();
        t.Value.ShouldBeNull();
    }

    /// <summary>
    /// 验证异常提升为包含异常的失败结果。
    /// </summary>
    [Fact]
    public void LiftException_ReturnsFailure()
    {
        var ex = new InvalidOperationException("boom");
        var t = Try.LiftException<int>(ex);
        t.IsSuccess.ShouldBeFalse();
        t.IsFailure.ShouldBeTrue();
        t.Exception.ShouldNotBeNull();
    }

    /// <summary>
    /// 验证异常提升时保留指定原因。
    /// </summary>
    [Fact]
    public void LiftException_WithCause_ExceptionCauseIsSet()
    {
        var ex = new InvalidOperationException("boom");
        const string cause = "unit-test-cause";
        var t = Try.LiftException<int>(ex, cause);
        t.IsFailure.ShouldBeTrue();
        t.Exception.Cause.ShouldBe(cause);
    }

    // ─────────────────────────────────────────────────────────────────
    // Try.Create (Func<T>)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证函数执行成功后返回计算值。
    /// </summary>
    [Fact]
    public void Create_NoException_ReturnsSuccess()
    {
        var t = Try.Create(() => 99);
        t.IsSuccess.ShouldBeTrue();
        t.Value.ShouldBe(99);
    }

    /// <summary>
    /// 验证函数抛出异常后返回包装异常的失败结果。
    /// </summary>
    [Fact]
    public void Create_FuncThrows_ReturnsFailure()
    {
        var t = Try.Create<int>(() => throw new ArithmeticException("divide"));
        t.IsFailure.ShouldBeTrue();
        t.Exception.InnerException.ShouldBeOfType<ArithmeticException>();
    }

    /// <summary>
    /// 验证带参数函数返回计算后的成功结果。
    /// </summary>
    [Fact]
    public void Create_WithArg_Success()
    {
        var t = Try.Create((int x) => x * 2, 5);
        t.IsSuccess.ShouldBeTrue();
        t.Value.ShouldBe(10);
    }

    // ─────────────────────────────────────────────────────────────────
    // Success<T> 行为
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证成功结果采用值匹配分支。
    /// </summary>
    [Fact]
    public void Success_Match_CallsWhenValue()
    {
        var t = Try.LiftValue("hello");
        var result = t.Match(v => v.ToUpperInvariant(), _ => "error");
        result.ShouldBe("HELLO");
    }

    /// <summary>
    /// 验证成功结果恢复后保留原值。
    /// </summary>
    [Fact]
    public void Success_Recover_ReturnsSelf()
    {
        var t = Try.LiftValue(7);
        var recovered = t.Recover(_ => -1);
        recovered.IsSuccess.ShouldBeTrue();
        recovered.Value.ShouldBe(7);
    }

    /// <summary>
    /// 验证成功结果经结果恢复后保留原值。
    /// </summary>
    [Fact]
    public void Success_RecoverWith_ReturnsSelf()
    {
        var t = Try.LiftValue(7);
        var recovered = t.RecoverWith(_ => Try.LiftValue(-1));
        recovered.Value.ShouldBe(7);
    }

    /// <summary>
    /// 验证成功结果将值传入成功回调并返回原实例。
    /// </summary>
    [Fact]
    public void Success_Tap_InvokesSuccessAction()
    {
        var captured = 0;
        var t = Try.LiftValue(5);
        var returned = t.Tap(v => captured = v, _ => captured = -999);
        captured.ShouldBe(5);
        returned.ShouldBeSameAs(t);
    }

    /// <summary>
    /// 验证成功结果不调用失败回调。
    /// </summary>
    [Fact]
    public void Success_Tap_DoesNotInvokeFailureAction()
    {
        var failureCalled = false;
        var t = Try.LiftValue(5);
        t.Tap(_ => { }, _ => failureCalled = true);
        failureCalled.ShouldBeFalse();
    }

    /// <summary>
    /// 验证成功结果解构为原值和空异常。
    /// </summary>
    [Fact]
    public void Success_Deconstruct_ValueSetExceptionDefault()
    {
        var t = Try.LiftValue(10);
        t.Deconstruct(out var value, out var exception);
        value.ShouldBe(10);
        exception.ShouldBeNull();
    }

    /// <summary>
    /// 验证成功结果的安全取值返回原值。
    /// </summary>
    [Fact]
    public void Success_GetSafeValue_Default_ReturnsValue()
    {
        var t = Try.LiftValue(42);
        t.GetSafeValue().ShouldBe(42);
    }

    /// <summary>
    /// 验证成功结果尝试取值时返回成功标记及原值。
    /// </summary>
    [Fact]
    public void Success_TryGetValue_ReturnsTrue()
    {
        var t = Try.LiftValue(3);
        t.TryGetValue(out var v).ShouldBeTrue();
        v.ShouldBe(3);
    }

    // ─────────────────────────────────────────────────────────────────
    // Failure<T> 行为
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证直接读取失败结果的值会抛出异常。
    /// </summary>
    [Fact]
    public void Failure_Value_Throws()
    {
        var ex = new Exception("original");
        var t = Try.LiftException<int>(ex);
        Should.Throw<Exception>(() => _ = t.Value);
    }

    /// <summary>
    /// 验证失败结果采用异常匹配分支。
    /// </summary>
    [Fact]
    public void Failure_Match_CallsWhenException()
    {
        var ex = new InvalidOperationException("bad");
        var t = Try.LiftException<string>(ex, "reason");
        var result = t.Match(v => "ok:" + v, e => "err:" + e.InnerException!.Message);
        result.ShouldBe("err:bad");
    }

    /// <summary>
    /// 验证失败结果恢复为指定的成功值。
    /// </summary>
    [Fact]
    public void Failure_Recover_ReturnsSuccessWithRecoveredValue()
    {
        var ex = new Exception("oops");
        var t = Try.LiftException<int>(ex);
        var recovered = t.Recover(_ => 99);
        recovered.IsSuccess.ShouldBeTrue();
        recovered.Value.ShouldBe(99);
    }

    /// <summary>
    /// 验证结果恢复抛出异常后仍返回失败状态。
    /// </summary>
    [Fact]
    public void Failure_RecoverWith_RecoveryThrows_ReturnsNewFailure()
    {
        var ex = new Exception("original");
        var t = Try.LiftException<int>(ex);
        var result = t.RecoverWith(_ => throw new Exception("recovery-failed"));
        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// 验证失败结果调用失败回调。
    /// </summary>
    [Fact]
    public void Failure_Tap_InvokesFailureAction()
    {
        var captured = false;
        var t = Try.LiftException<int>(new Exception("x"));
        t.Tap(_ => captured = false, _ => captured = true);
        captured.ShouldBeTrue();
    }

    /// <summary>
    /// 验证失败结果不调用成功回调。
    /// </summary>
    [Fact]
    public void Failure_Tap_DoesNotInvokeSuccessAction()
    {
        var successCalled = false;
        var t = Try.LiftException<int>(new Exception("x"));
        t.Tap(_ => successCalled = true, _ => { });
        successCalled.ShouldBeFalse();
    }

    /// <summary>
    /// 验证失败结果解构为默认值和非空异常。
    /// </summary>
    [Fact]
    public void Failure_Deconstruct_ValueDefaultExceptionSet()
    {
        var t = Try.LiftException<int>(new Exception("x"));
        t.Deconstruct(out var value, out var exception);
        value.ShouldBe(default);
        exception.ShouldNotBeNull();
    }

    /// <summary>
    /// 验证失败结果尝试取值时返回失败标记及默认值。
    /// </summary>
    [Fact]
    public void Failure_TryGetValue_ReturnsFalse()
    {
        var t = Try.LiftException<int>(new Exception("x"));
        t.TryGetValue(out var v).ShouldBeFalse();
        v.ShouldBe(default);
    }

    /// <summary>
    /// 验证失败结果的安全取值返回默认值。
    /// </summary>
    [Fact]
    public void Failure_GetSafeValue_Default_ReturnsDefault()
    {
        var t = Try.LiftException<int>(new Exception("x"));
        t.GetSafeValue().ShouldBe(default);
    }

    /// <summary>
    /// 验证失败结果的安全取值采用指定回退值。
    /// </summary>
    [Fact]
    public void Failure_GetSafeValue_CustomFallback_ReturnsFallback()
    {
        var t = Try.LiftException<int>(new Exception("x"));
        t.GetSafeValue(-1).ShouldBe(-1);
    }

    // ─────────────────────────────────────────────────────────────────
    // Map / Bind
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证映射成功结果后得到转换值。
    /// </summary>
    [Fact]
    public void Map_Success_TransformsValue()
    {
        var t = Try.LiftValue(5);
        var mapped = t.Map(x => x * 10);
        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe(50);
    }

    /// <summary>
    /// 验证映射失败结果后仍为失败状态。
    /// </summary>
    [Fact]
    public void Map_Failure_StaysFailure()
    {
        var t = Try.LiftException<int>(new Exception("x"));
        var mapped = t.Map(x => x * 10);
        mapped.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// 验证成功结果绑定到新的成功值。
    /// </summary>
    [Fact]
    public void Bind_Success_ReturnsBound()
    {
        // Bind is not public; exercise via SelectMany extension
        var t = Try.LiftValue(3);
        var bound = t.SelectMany(x => Try.LiftValue(x.ToString()));
        bound.IsSuccess.ShouldBeTrue();
        bound.Value.ShouldBe("3");
    }

    /// <summary>
    /// 验证绑定失败结果后仍为失败状态。
    /// </summary>
    [Fact]
    public void Bind_Failure_StaysFailure()
    {
        var t = Try.LiftException<int>(new Exception("x"));
        var bound = t.SelectMany(x => Try.LiftValue(x.ToString()));
        bound.IsFailure.ShouldBeTrue();
    }

    // ─────────────────────────────────────────────────────────────────
    // TryExtensions: Select / SelectMany / Where
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证投影成功结果后得到转换值。
    /// </summary>
    [Fact]
    public void Select_Success_TransformsValue()
    {
        var t = Try.LiftValue(4);
        var selected = t.Select(x => x + 1);
        selected.IsSuccess.ShouldBeTrue();
        selected.Value.ShouldBe(5);
    }

    /// <summary>
    /// 验证投影失败结果后仍为失败状态。
    /// </summary>
    [Fact]
    public void Select_Failure_RemainsFailure()
    {
        var t = Try.LiftException<int>(new Exception("x"));
        var selected = t.Select(x => x + 1);
        selected.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// 验证投影拒绝空结果对象。
    /// </summary>
    [Fact]
    public void Select_NullSource_Throws()
    {
        Try<int> t = null!;
        Should.Throw<ArgumentNullException>(() => t.Select(x => x));
    }

    /// <summary>
    /// 验证投影拒绝空选择器。
    /// </summary>
    [Fact]
    public void Select_NullSelector_Throws()
    {
        var t = Try.LiftValue(1);
        Should.Throw<ArgumentNullException>(() => t.Select<int, int>(null!));
    }

    /// <summary>
    /// 验证成功结果可以连续绑定计算。
    /// </summary>
    [Fact]
    public void SelectMany_Success_Chains()
    {
        var t = Try.LiftValue(3);
        var result = t.SelectMany(x => Try.LiftValue(x * 2));
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(6);
    }

    /// <summary>
    /// 验证满足筛选条件时保留成功值。
    /// </summary>
    [Fact]
    public void Where_PredicateSatisfied_ReturnsSuccess()
    {
        var t = Try.LiftValue(10);
        var filtered = t.Where(x => x > 5);
        filtered.IsSuccess.ShouldBeTrue();
        filtered.Value.ShouldBe(10);
    }

    /// <summary>
    /// 验证不满足筛选条件时返回失败状态。
    /// </summary>
    [Fact]
    public void Where_PredicateNotSatisfied_ReturnsFailure()
    {
        var t = Try.LiftValue(3);
        var filtered = t.Where(x => x > 5);
        filtered.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// 验证筛选失败结果后仍为失败状态。
    /// </summary>
    [Fact]
    public void Where_OnFailure_RemainsFailure()
    {
        var t = Try.LiftException<int>(new Exception("x"));
        var filtered = t.Where(_ => true);
        filtered.IsFailure.ShouldBeTrue();
    }

    // ─────────────────────────────────────────────────────────────────
    // LINQ query syntax (uses Select / SelectMany / Where)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证查询表达式连续绑定并投影成功值。
    /// </summary>
    [Fact]
    public void LinqQuery_ChainsSuccessfully()
    {
        var t =
            from x in Try.LiftValue(10)
            from y in Try.LiftValue(x + 5)
            select y * 2;

        t.IsSuccess.ShouldBeTrue();
        t.Value.ShouldBe(30);
    }
}
