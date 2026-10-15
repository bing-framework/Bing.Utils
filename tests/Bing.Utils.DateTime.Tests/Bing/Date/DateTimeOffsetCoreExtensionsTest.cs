namespace Bing.Date;
/// <summary>
/// 验证带偏移时间点的跨度运算、工作日、比较及舍入行为。
/// </summary>
[Trait("DateTimeUT", "DateTimeOffsetExtensions.Core")]
public class DateTimeOffsetCoreExtensionsTest
{
    /// <summary>
    /// 验证时间跨度增减的当前年月与时间分量行为。
    /// </summary>
    /// <remarks>当前减法在扣除年月后仍增加时间分量，因此加减往返后的时间比起始值晚四小时。</remarks>
    [Fact]
    public void AddDateTimeSpan_And_SubtractDateTimeSpan_ShouldReturnExpectedCurrentBehavior()
    {
        var offset = TimeSpan.FromHours(8);
        var dto = new DateTimeOffset(2024, 1, 15, 10, 20, 30, offset);
        var span = new DateTimeSpan(1, 1, TimeSpan.FromHours(2));
        var added = dto.AddDateTimeSpan(span);
        var subtracted = added.SubtractDateTimeSpan(span);
        added.ShouldBe(new DateTimeOffset(2025, 2, 15, 12, 20, 30, offset));
        subtracted.ShouldBe(new DateTimeOffset(2024, 1, 15, 14, 20, 30, offset));
    }
    /// <summary>
    /// 验证周五增加一个工作日后到达下周一。
    /// </summary>
    [Fact]
    public void AddBusinessDays_ShouldSkipWeekends()
    {
        var offset = TimeSpan.FromHours(8);
        var friday = new DateTimeOffset(2024, 1, 5, 9, 0, 0, offset);
        var next = friday.AddBusinessDays(1);
        next.Date.ShouldBe(new DateTime(2024, 1, 8));
        next.DayOfWeek.ShouldBe(DayOfWeek.Monday);
    }
    /// <summary>
    /// 验证周一减去一个工作日后到达上周五。
    /// </summary>
    [Fact]
    public void AddBusinessDays_NegativeDays_ShouldMoveBackwardSkippingWeekends()
    {
        var offset = TimeSpan.FromHours(8);
        var monday = new DateTimeOffset(2024, 1, 8, 9, 0, 0, offset);
        var prev = monday.AddBusinessDays(-1);
        prev.Date.ShouldBe(new DateTime(2024, 1, 5));
        prev.DayOfWeek.ShouldBe(DayOfWeek.Friday);
    }
    /// <summary>
    /// 验证工作日减法跳过周末。
    /// </summary>
    [Fact]
    public void SubtractBusinessDays_ShouldDelegateToNegativeAddBusinessDays()
    {
        var offset = TimeSpan.FromHours(8);
        var monday = new DateTimeOffset(2024, 1, 8, 9, 0, 0, offset);
        var result = monday.SubtractBusinessDays(1);
        result.Date.ShouldBe(new DateTime(2024, 1, 5));
    }
    /// <summary>
    /// 验证空时间点不被识别为今天。
    /// </summary>
    [Fact]
    public void IsToday_ForNullableNull_ShouldReturnFalse()
    {
        DateTimeOffset? value = null;
        value.IsToday().ShouldBeFalse();
    }
    /// <summary>
    /// 验证时间点先后比较结果。
    /// </summary>
    [Fact]
    public void IsBeforeAndIsAfter_ShouldReturnExpected()
    {
        var now = DateTimeOffset.Now;
        var future = now.AddMinutes(1);
        var past = now.AddMinutes(-1);
        past.IsBefore(now).ShouldBeTrue();
        future.IsAfter(now).ShouldBeTrue();
        now.IsBefore(past).ShouldBeFalse();
    }
    /// <summary>
    /// 验证未来与过去时间点的判定。
    /// </summary>
    [Fact]
    public void IsInTheFutureAndIsInThePast_ShouldReturnExpected()
    {
        var future = DateTimeOffset.Now.AddMinutes(1);
        var past = DateTimeOffset.Now.AddMinutes(-1);
        future.IsInTheFuture().ShouldBeTrue();
        past.IsInThePast().ShouldBeTrue();
    }
    /// <summary>
    /// 验证同月判定当前忽略年份差异。
    /// </summary>
    [Fact]
    public void IsSameMonth_CurrentBehavior_IgnoresYear()
    {
        var left = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var right = new DateTimeOffset(2023, 1, 31, 23, 0, 0, TimeSpan.Zero);
        left.IsSameMonth(right).ShouldBeTrue();
    }
    /// <summary>
    /// 验证同日与同年判定使用对应日期分量。
    /// </summary>
    [Fact]
    public void IsSameDayAndIsSameYear_ShouldReturnExpected()
    {
        var left = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var sameDay = new DateTimeOffset(2024, 1, 1, 23, 59, 59, TimeSpan.Zero);
        var otherYear = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        left.IsSameDay(sameDay).ShouldBeTrue();
        left.IsSameYear(sameDay).ShouldBeTrue();
        left.IsSameYear(otherYear).ShouldBeFalse();
    }
    /// <summary>
    /// 验证按秒舍入以五百毫秒为边界并保留偏移量。
    /// </summary>
    /// <param name="ms">舍入前的毫秒分量。</param>
    /// <param name="expectedMinute">预期舍入后的分钟分量。</param>
    /// <param name="expectedSecond">预期舍入后的秒分量。</param>
    [Theory]
    [InlineData(499, 12, 30)]
    [InlineData(500, 12, 31)]
    public void Round_Second_ShouldRoundByMilliseconds(int ms, int expectedMinute, int expectedSecond)
    {
        var dto = new DateTimeOffset(2024, 1, 1, 8, 12, 30, ms, TimeSpan.FromHours(8));
        var rounded = dto.Round(RoundTo.Second);
        rounded.Minute.ShouldBe(expectedMinute);
        rounded.Second.ShouldBe(expectedSecond);
        rounded.Offset.ShouldBe(dto.Offset);
    }
    /// <summary>
    /// 验证分钟、小时与日期在中点边界向上舍入。
    /// </summary>
    [Fact]
    public void Round_MinuteHourDay_ShouldRoundByBoundaries()
    {
        var offset = TimeSpan.FromHours(8);
        var minuteBoundary = new DateTimeOffset(2024, 1, 1, 8, 12, 30, 0, offset);
        var hourBoundary = new DateTimeOffset(2024, 1, 1, 8, 30, 0, 0, offset);
        var dayBoundary = new DateTimeOffset(2024, 1, 1, 12, 0, 0, 0, offset);
        minuteBoundary.Round(RoundTo.Minute).ShouldBe(new DateTimeOffset(2024, 1, 1, 8, 13, 0, offset));
        hourBoundary.Round(RoundTo.Hour).ShouldBe(new DateTimeOffset(2024, 1, 1, 9, 0, 0, offset));
        dayBoundary.Round(RoundTo.Day).ShouldBe(new DateTimeOffset(2024, 1, 2, 0, 0, 0, offset));
    }
    /// <summary>
    /// 验证舍入拒绝未定义的目标单位。
    /// </summary>
    [Fact]
    public void Round_UnknownEnum_ShouldThrowArgumentOutOfRangeException()
    {
        var dto = DateTimeOffset.Now;
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => dto.Round((RoundTo)999));
        ex.ParamName.ShouldBe("roundTo");
    }
}
