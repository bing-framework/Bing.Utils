namespace Bing.Date;

/// <summary>
/// 验证夏令时切换日期下的固定小时偏移计算。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.DstBoundary")]
public class DateTimeDstBoundaryContractTest
{
    /// <summary>
    /// 验证夏令时开始日的日首边界按固定偏移计算。
    /// </summary>
    [Fact]
    public void BeginningOfDay_DstStartDate_UsesFixedOffsetArithmetic()
    {
        var dstStartDate = new DateTime(2024, 3, 10, 12, 0, 0, DateTimeKind.Unspecified);

        var standardOffset = dstStartDate.BeginningOfDay(-5);
        var daylightOffset = dstStartDate.BeginningOfDay(-4);

        standardOffset.ShouldBe(new DateTime(2024, 3, 9, 19, 0, 0, DateTimeKind.Unspecified));
        daylightOffset.ShouldBe(new DateTime(2024, 3, 9, 20, 0, 0, DateTimeKind.Unspecified));
        (daylightOffset - standardOffset).TotalHours.ShouldBe(1);
    }

    /// <summary>
    /// 验证夏令时结束日的日末边界按固定偏移计算。
    /// </summary>
    [Fact]
    public void EndOfDay_DstEndDate_UsesFixedOffsetArithmetic()
    {
        var dstEndDate = new DateTime(2024, 11, 3, 12, 0, 0, DateTimeKind.Unspecified);

        var daylightOffset = dstEndDate.EndOfDay(-4);
        var standardOffset = dstEndDate.EndOfDay(-5);

        daylightOffset.ShouldBe(new DateTime(2024, 11, 3, 19, 59, 59, 999, DateTimeKind.Unspecified));
        standardOffset.ShouldBe(new DateTime(2024, 11, 3, 18, 59, 59, 999, DateTimeKind.Unspecified));
        (daylightOffset - standardOffset).TotalHours.ShouldBe(1);
    }
}
