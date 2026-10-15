namespace Bing.Date;

/// <summary>
/// 验证中国标准时间转换当前使用时钟而非输入日期的行为。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.ToCst.Contract")]
public class DateTimeToCstContractTest
{
    /// <summary>
    /// 验证不同输入日期当前均返回接近的中国标准时间。
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
    /// 验证当前中国标准时间约等于 UTC 时钟加八小时。
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
