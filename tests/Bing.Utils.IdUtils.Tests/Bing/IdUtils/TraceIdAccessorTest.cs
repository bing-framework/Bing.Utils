namespace Bing.IdUtils;
/// <summary>
/// 测试类：覆盖 `TraceIdAccessor` 相关行为。
/// </summary>
[Trait("IdUtilsUT", "TraceIdAccessor")]
public class TraceIdAccessorTest
{
    /// <summary>
    /// 测试用例：验证 `GetTraceId` 在 `CustomMakerProvided` 场景下，结果为 `ReturnsCustomValue`。
    /// </summary>
    [Fact]
    public void GetTraceId_CustomMakerProvided_ReturnsCustomValue()
    {
        var accessor = new TraceIdAccessor(new FixedTraceIdMaker("trace-custom-001"));
        var result = accessor.GetTraceId();
        result.ShouldBe("trace-custom-001");
    }
    /// <summary>
    /// 测试用例：验证 `GetTraceId` 在 `CalledMultipleTimes` 场景下，结果为 `ReturnsSameValue`。
    /// </summary>
    [Fact]
    public void GetTraceId_CalledMultipleTimes_ReturnsSameValue()
    {
        var accessor = new TraceIdAccessor(new FixedTraceIdMaker("trace-fixed"));
        var first = accessor.GetTraceId();
        var second = accessor.GetTraceId();
        first.ShouldBe(second);
    }
    /// <summary>
    /// 测试用例：验证 `GetTraceId` 在 `NullMakerProvided` 场景下，结果为 `UsesDefaultMaker`。
    /// </summary>
    [Fact]
    public void GetTraceId_NullMakerProvided_UsesDefaultMaker()
    {
        var accessor = new TraceIdAccessor(null);
        var result = accessor.GetTraceId();
        result.ShouldNotBeNullOrWhiteSpace();
        result.Length.ShouldBeGreaterThan(20);
    }

    /// <summary>
    /// 测试用例：验证 `GetTraceId` 在 `CustomMakerReturnsNull` 场景下，结果为 `ReturnsNullWithoutFallback`。
    /// </summary>
    [Fact]
    public void GetTraceId_CustomMakerReturnsNull_ReturnsNullWithoutFallback()
    {
        var accessor = new TraceIdAccessor(new FixedTraceIdMaker(null));

        accessor.GetTraceId().ShouldBeNull();
    }

    /// <summary>
    /// 测试用例：验证构造 `TraceIdAccessor` 时，若自定义 Maker 抛异常，应将异常透传。
    /// </summary>
    [Fact]
    public void Constructor_CustomMakerThrows_PropagatesException()
    {
        Should.Throw<InvalidOperationException>(() => new TraceIdAccessor(new ThrowingTraceIdMaker()))
            .Message.ShouldContain("maker-failed");
    }
    /// <summary>
    /// 测试辅助：提供 `FixedTraceIdMaker` 的测试支撑逻辑。
    /// </summary>
    private sealed class FixedTraceIdMaker : ITraceIdMaker
    {
        private readonly string _value;
        public FixedTraceIdMaker(string value) => _value = value;
        public string Create() => _value;
    }

    /// <summary>
    /// 测试辅助：用于模拟 `ITraceIdMaker.Create()` 抛异常场景。
    /// </summary>
    private sealed class ThrowingTraceIdMaker : ITraceIdMaker
    {
        public string Create() => throw new InvalidOperationException("maker-failed");
    }
}

