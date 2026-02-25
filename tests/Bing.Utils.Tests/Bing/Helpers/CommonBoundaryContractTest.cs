namespace Bing.Helpers;

/// <summary>
/// 测试类：`Common` 边界契约测试
/// </summary>
[Trait("Bing.Helpers", "Common.Boundary")]
public class CommonBoundaryContractTest
{
    /// <summary>
    /// 测试用例：`GetType(Type)` 在 `type=null` 时应抛出 `ArgumentNullException(type)`
    /// </summary>
    [Fact]
    public void GetType_NullType_ThrowsArgumentNullExceptionWithParamName()
    {
        var ex = Should.Throw<ArgumentNullException>(() => Common.GetType((Type)null));

        ex.ParamName.ShouldBe("type");
    }

    /// <summary>
    /// 测试用例：`RetryExecute` 在 `maxRetries=0` 且首轮失败时应仅调用一次，并抛出聚合异常
    /// </summary>
    [Fact]
    public void RetryExecute_MaxRetriesIsZero_InvokesOnceAndThrowsAggregateException()
    {
        var callCount = 0;

        var ex = Should.Throw<AggregateException>(() =>
            Common.RetryExecute<int>(() =>
            {
                callCount++;
                throw new InvalidOperationException("fail");
            }, maxRetries: 0, delay: TimeSpan.Zero));

        callCount.ShouldBe(1);
        ex.InnerExceptions.Count.ShouldBe(1);
        ex.InnerExceptions[0].ShouldBeOfType<InvalidOperationException>();
    }

    /// <summary>
    /// 测试用例：`RetryExecute` 在 `maxRetries<0` 时应抛出 `ArgumentOutOfRangeException(maxRetries)`
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void RetryExecute_NegativeMaxRetries_ThrowsArgumentOutOfRangeExceptionWithParamName(int maxRetries)
    {
        var ex = Should.Throw<ArgumentOutOfRangeException>(() =>
            Common.RetryExecute(() => 1, maxRetries: maxRetries, delay: TimeSpan.Zero));

        ex.ParamName.ShouldBe("maxRetries");
    }

    /// <summary>
    /// 测试用例：`RetryExecute` 在非正延迟下不应阻塞重试，当前行为视为无延迟
    /// </summary>
    [Theory]
    [MemberData(nameof(GetNonPositiveDelayCases))]
    public void RetryExecute_NonPositiveDelay_CurrentBehaviorTreatsAsNoDelay(TimeSpan delay)
    {
        var callCount = 0;

        var result = Common.RetryExecute(() =>
        {
            callCount++;
            if (callCount < 2)
                throw new InvalidOperationException("retry");
            return 7;
        }, maxRetries: 1, delay: delay);

        result.ShouldBe(7);
        callCount.ShouldBe(2);
    }

    /// <summary>
    /// 测试数据：非正延迟边界值
    /// </summary>
    public static IEnumerable<object[]> GetNonPositiveDelayCases()
    {
        yield return new object[] { TimeSpan.Zero };
        yield return new object[] { TimeSpan.FromMilliseconds(-1) };
        yield return new object[] { TimeSpan.FromMilliseconds(-100) };
    }
}
