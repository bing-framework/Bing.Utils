using Bing.Exceptions;

namespace Bing.Utils.Tests.Bing.Exceptions;

/// <summary>
/// 验证异常结果在公开调用、解构及组合过程中的契约。
/// </summary>
public class TryContractRegressionTests
{
    /// <summary>
    /// 验证成功的二元与三元解构返回同一值和空异常。
    /// </summary>
    [Fact]
    public void SuccessDeconstruction_PreservesValueAndClearsException()
    {
        var original = new object();
        Try<object> result = Try.LiftValue(original);
        var (twoValue, twoException) = result;
        var (threeValue, success, threeException) = result;
        twoValue.ShouldBeSameAs(original);
        threeValue.ShouldBeSameAs(twoValue);
        success.ShouldBeTrue();
        twoException.ShouldBeNull();
        threeException.ShouldBeNull();
    }

    /// <summary>
    /// 验证失败三元解构返回默认值及原异常，而不是再次抛错。
    /// </summary>
    [Fact]
    public void FailureDeconstruction_PreservesFailureWithoutThrowing()
    {
        var original = new InvalidOperationException("original");
        var failure = Try.LiftException<int>(original, "cause");
        var (binaryValue, binaryException) = failure;
        var (value, success, exception) = failure;
        value.ShouldBe(0);
        binaryValue.ShouldBe(value);
        binaryException.ShouldBeSameAs(exception);
        success.ShouldBeFalse();
        exception.ShouldBeSameAs(failure.Exception);
        exception.InnerException.ShouldBeSameAs(original);
    }

    /// <summary>
    /// 验证抽象和具体结果类型均可调用无歧义的便捷重载。
    /// </summary>
    [Fact]
    public void Tap_ConvenienceOverloads_WorkForEveryStaticType()
    {
        Try<int> result = Try.LiftValue(7);
        var success = (Success<int>)result;
        var failure = (Failure<int>)Try.LiftException<int>(new Exception("failed"));
        var sum = 0;
        result.Tap().ShouldBeSameAs(result);
        success.Tap().ShouldBeSameAs(success);
        failure.Tap().ShouldBeSameAs(failure);
        result.Tap(successAction: x => sum += x).ShouldBeSameAs(result);
        success.Tap(successAction: x => sum += x).ShouldBeSameAs(success);
        failure.Tap(successAction: _ => throw new Exception()).ShouldBeSameAs(failure);
        sum.ShouldBe(14);
    }

    /// <summary>
    /// 验证组合算子将回调异常转换为可恢复的失败结果。
    /// </summary>
    [Theory]
    [InlineData("Map")]
    [InlineData("Select")]
    [InlineData("SelectMany")]
    [InlineData("SelectManyConvert")]
    [InlineData("SelectManyProject")]
    [InlineData("Where")]
    public void Composition_CapturesCallbackException(string operation)
    {
        var original = new InvalidOperationException("callback");
        var value = Try.LiftValue(5);
        Try<int> result = operation switch
        {
            "Map" => value.Map<int>(_ => throw original),
            "Select" => value.Select<int, int>(_ => throw original),
            "SelectMany" => value.SelectMany<int, int>(_ => throw original),
            "SelectManyConvert" => value.SelectMany<int, int, int>(_ => throw original, (a, b) => a + b),
            "SelectManyProject" => value.SelectMany<int, int, int>(_ => Try.LiftValue(2), (a, b) => throw original),
            _ => value.Where(_ => throw original)
        };
        result.IsFailure.ShouldBeTrue();
        result.ExceptionAs<InvalidOperationException>().ShouldBeSameAs(original);
        result.Recover(_ => 42).Value.ShouldBe(42);
    }

    /// <summary>
    /// 验证失败短路保留异常身份和业务原因且不执行回调。
    /// </summary>
    [Fact]
    public void FailureComposition_PreservesWrapperAndCauseWithoutCallingCallbacks()
    {
        var original = new ArgumentException("original");
        var failure = Try.LiftException<int>(original, "business-cause");
        var calls = 0;
        var result = failure.Map(x => { calls++; return x; })
            .Select(x => { calls++; return x; })
            .SelectMany(x => { calls++; return Try.LiftValue(x); })
            .Where(x => { calls++; return true; });
        result.Exception.ShouldBeSameAs(failure.Exception);
        result.Exception.InnerException.ShouldBeSameAs(original);
        result.Exception.Cause.ShouldBe("business-cause");
        result.Exception.Code.ShouldBe(failure.Exception.Code);
        result.Exception.Flag.ShouldBe(failure.Exception.Flag);
        calls.ShouldBe(0);
    }

    /// <summary>
    /// 验证空的中间尝试结果被转换为明确失败。
    /// </summary>
    [Fact]
    public void SelectMany_NullResultBecomesFailure()
    {
        Try.LiftValue(1).SelectMany<int, int>(_ => null)
            .ExceptionAs<InvalidOperationException>().ShouldNotBeNull();
        Try.LiftValue(1).SelectMany<int, int, int>(_ => null, (a, b) => a + b)
            .ExceptionAs<InvalidOperationException>().ShouldNotBeNull();
    }

    /// <summary>
    /// 验证参数错误和副作用回调保留直接抛出契约。
    /// </summary>
    [Fact]
    public void Composition_ValidatesArgumentsAndDoesNotCaptureTapOrMatch()
    {
        var result = Try.LiftValue(1);
        Should.Throw<ArgumentNullException>(() => result.SelectMany<int, int>(null));
        Should.Throw<ArgumentNullException>(() => result.Where(null));
        Should.Throw<ArgumentNullException>(() => TryExtensions.Select<int, int>(null, x => x));
        var error = new InvalidOperationException("side-effect");
        Should.Throw<InvalidOperationException>(() => result.Tap(_ => throw error)).ShouldBeSameAs(error);
        Should.Throw<InvalidOperationException>(() => result.Match<int>(_ => throw error, _ => 0)).ShouldBeSameAs(error);
    }

    /// <summary>
    /// 验证 Where 条件不满足时返回固定类型和消息的失败结果。
    /// </summary>
    [Fact]
    public void Where_RejectionUsesExpectedFailureTypeAndMessage()
    {
        var result = Try.LiftValue(3).Where(value => value > 5);
        result.IsFailure.ShouldBeTrue();
        var exception = result.ExceptionAs<InvalidOperationException>();
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("Predicate not satisfied");
    }

    /// <summary>
    /// 验证 TryGetValue 的常量和委托 fallback 保持既有布尔及输出值契约。
    /// </summary>
    [Fact]
    public void TryGetValue_FallbackOverloadsPreserveResultSemantics()
    {
        var original = new ArgumentException("original");
        var failure = Try.LiftException<int>(original, "fallback-cause");
        var success = Try.LiftValue(5);
        var value = 0;

        success.TryGetValue(out value, -1).ShouldBeTrue();
        value.ShouldBe(5);
        failure.TryGetValue(out value, -1).ShouldBeTrue();
        value.ShouldBe(-1);

        var simpleCalls = 0;
        Func<int> simpleFallback = () => { simpleCalls++; return -2; };
        success.TryGetValue(out value, simpleFallback).ShouldBeTrue();
        value.ShouldBe(5);
        simpleCalls.ShouldBe(0);
        failure.TryGetValue(out value, simpleFallback).ShouldBeTrue();
        value.ShouldBe(-2);
        simpleCalls.ShouldBe(1);
        failure.TryGetValue(out value, (Func<int>)(() => throw new InvalidOperationException("fallback")))
            .ShouldBeFalse();
        value.ShouldBe(default);

        var wrappedCalls = 0;
        Func<TryCreatingValueException, int> wrappedFallback = exception =>
        {
            exception.ShouldBeSameAs(failure.Exception);
            wrappedCalls++;
            return -3;
        };
        success.TryGetValue(out value, wrappedFallback).ShouldBeTrue();
        value.ShouldBe(5);
        wrappedCalls.ShouldBe(0);
        failure.TryGetValue(out value, wrappedFallback).ShouldBeTrue();
        value.ShouldBe(-3);
        wrappedCalls.ShouldBe(1);
        failure.TryGetValue(out value, (Func<TryCreatingValueException, int>)(_ => throw new InvalidOperationException("fallback")))
            .ShouldBeFalse();
        value.ShouldBe(default);

        var detailCalls = 0;
        Func<Exception, string, int> detailFallback = (exception, cause) =>
        {
            exception.ShouldBeSameAs(original);
            cause.ShouldBe("fallback-cause");
            detailCalls++;
            return -4;
        };
        success.TryGetValue(out value, detailFallback).ShouldBeTrue();
        value.ShouldBe(5);
        detailCalls.ShouldBe(0);
        failure.TryGetValue(out value, detailFallback).ShouldBeTrue();
        value.ShouldBe(-4);
        detailCalls.ShouldBe(1);
        failure.TryGetValue(out value, (Func<Exception, string, int>)((_, _) => throw new InvalidOperationException("fallback")))
            .ShouldBeFalse();
        value.ShouldBe(default);
    }

    /// <summary>
    /// 验证成功操作结果按实例身份比较。
    /// </summary>
    [Fact]
    public void SuccessAction_UsesResultIdentity()
    {
        Action operation = () => { };
        var first = Try.Invoke(operation);
        var second = Try.Invoke(operation);
        var third = Try.Invoke(() => { });
        first.Equals(first).ShouldBeTrue();
        first.Equals(second).ShouldBeFalse();
        (first == second).ShouldBeFalse();
        (first != third).ShouldBeTrue();
        new HashSet<TryAction> { first, first, second, third }.Count.ShouldBe(3);
        first.GetHashCode().ShouldBe(first.GetHashCode());
    }

    /// <summary>
    /// 验证异步取值、默认值回调和恢复失败保留原异常。
    /// </summary>
    [Fact]
    public async Task ValueAccessAndRecovery_PreserveFailureContract()
    {
        var original = new InvalidOperationException("initial");
        var failure = Try.LiftException<int>(original, "cause");
        var thrown = await Should.ThrowAsync<TryCreatingValueException>(() => failure.GetValueAsync());
        thrown.ShouldBeSameAs(failure.Exception);
        (await Try.LiftValue(6).GetValueAsync()).ShouldBe(6);
        var calls = 0;
        (await failure.GetSafeValueAsync(() => { calls++; return Task.FromResult(9); })).ShouldBe(9);
        calls.ShouldBe(1);
        (await Try.LiftValue(6).GetSafeValueAsync(() => { calls++; return Task.FromResult(9); })).ShouldBe(6);
        calls.ShouldBe(1);
        var recovery = new ArgumentException("recovery");
        failure.Recover(_ => throw recovery).Exception.InnerException.ShouldBeSameAs(recovery);
        failure.RecoverWith(_ => throw recovery).Exception.InnerException.ShouldBeSameAs(recovery);
        var taskFailure = failure.GetSafeValueAsync(() => Task.FromException<int>(recovery));
        (await Should.ThrowAsync<ArgumentException>(() => taskFailure)).ShouldBeSameAs(recovery);
    }
}
