namespace Bing.Date;

/// <summary>
/// 验证日期与区间之间按时间点进行比较。
/// </summary>
public class DateTimeRangeInputRegressionTest
{
    /// <summary>
    /// 验证不同 Kind 的相同时间点与范围包含判断一致。
    /// </summary>
    /// <param name="kind">输入时间的种类。</param>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void In_Range_UsesSameInstantAsContains(DateTimeKind kind)
    {
        var middleUtc = new DateTime(2024, 1, 1, 4, 30, 0, DateTimeKind.Utc);
        var local = middleUtc.ToLocalTime();
        var value = kind == DateTimeKind.Utc ? middleUtc : DateTime.SpecifyKind(local, kind);
        var range = new DateTimeRange(middleUtc.AddMinutes(-30), middleUtc.AddMinutes(30));

        range.Contains(value).ShouldBeTrue();
        value.In(range).ShouldBeTrue();
        value.In(range).ShouldBe(range.Contains(value));
    }

    /// <summary>
    /// 验证自定义逆序范围按指定模式处理两个边界。
    /// </summary>
    /// <param name="mode">开闭区间模式。</param>
    /// <param name="includesStart">是否包含起点。</param>
    /// <param name="includesEnd">是否包含终点。</param>
    [Theory]
    [InlineData(RangeMode.Close, true, true)]
    [InlineData(RangeMode.Open, false, false)]
    [InlineData(RangeMode.OpenClose, false, true)]
    [InlineData(RangeMode.CloseOpen, true, false)]
    public void In_CustomReversedRange_UsesNormalizedBoundaries(RangeMode mode, bool includesStart, bool includesEnd)
    {
        var start = new DateTime(2024, 1, 1, 4, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(1);
        var range = new CustomRange(end.ToLocalTime(), start.ToLocalTime());

        start.ToLocalTime().In(range, mode).ShouldBe(includesStart);
        DateTime.SpecifyKind(end.ToLocalTime(), DateTimeKind.Unspecified).In(range, mode).ShouldBe(includesEnd);
        start.AddMinutes(30).ToLocalTime().In(range, mode).ShouldBeTrue();
        start.AddTicks(-1).ToLocalTime().In(range, mode).ShouldBeFalse();
    }

    /// <summary>
    /// 验证空范围和无效区间模式均被拒绝。
    /// </summary>
    [Fact]
    public void In_NullRangeAndInvalidMode_RejectsArguments()
    {
        var value = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Should.Throw<ArgumentNullException>(() => value.In((IDateTimeRange)null));
        Should.Throw<ArgumentOutOfRangeException>(() => value.In(new DateTimeRange(value, value), (RangeMode)42));
    }

    /// <summary>
    /// 提供可包含逆序端点的测试区间。
    /// </summary>
    private sealed class CustomRange : IDateTimeRange
    {
        /// <summary>
        /// 初始化一个 <see cref="CustomRange"/> 类型的实例。
        /// </summary>
        /// <param name="start">原始起点。</param>
        /// <param name="end">原始终点。</param>
        public CustomRange(DateTime start, DateTime end)
        {
            StartTime = start;
            EndTime = end;
        }

        /// <inheritdoc />
        public DateTime StartTime { get; }

        /// <inheritdoc />
        public DateTime EndTime { get; }
    }
}
