namespace Bing.Date;

/// <summary>
/// 测试类：覆盖 `ToCst` 的时间来源与输出契约行为。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.ToCst.Contract")]
public class DateTimeToCstContractTest
{
    /// <summary>
    /// 测试用例：验证 `ToCst` 在 `AnyInputDateTime` 场景下，结果为 `UsesCurrentClockInsteadOfInput`。
    /// </summary>
    [Fact]
    public void ToCst_AnyInputDateTime_UsesCurrentClockInsteadOfInput()
    {
        var past = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var future = new DateTime(2099, 12, 31, 23, 59, 59, DateTimeKind.Local);

        var left = past.ToCst();
        var right = future.ToCst();

        Math.Abs((right - left).TotalSeconds).ShouldBeLessThanOrEqualTo(10);
        left.Kind.ShouldBe(DateTimeKind.Unspecified);
        right.Kind.ShouldBe(DateTimeKind.Unspecified);
    }

    /// <summary>
    /// 测试用例：验证 `ToCst` 在 `UtcNowBaseline` 场景下，结果为 `ApproximatelyUtcPlusEightHours`。
    /// </summary>
    [Fact]
    public void ToCst_UtcNowBaseline_ApproximatelyUtcPlusEightHours()
    {
        var lower = DateTime.UtcNow.AddHours(8);
        var result = DateTime.MinValue.ToCst();
        var upper = DateTime.UtcNow.AddHours(8);

        result.ShouldBeGreaterThanOrEqualTo(lower.AddSeconds(-5));
        result.ShouldBeLessThanOrEqualTo(upper.AddSeconds(5));
        result.Kind.ShouldBe(DateTimeKind.Unspecified);
    }
}
