using System;
using System.Linq;

namespace Bing.Date.Calendars;
/// <summary>
/// 周休息日、节假日和补班规则测试。
/// </summary>
public class BusinessCalendarTest
{
    /// <summary>
    /// 验证周末和节假日不是业务日，且补班覆盖两者。
    /// </summary>
    [Fact]
    public void IsBusinessDay_OverrideTakesPrecedenceOverHolidayAndRestDay()
    {
        var holiday = new DateTime(2024, 1, 6, 18, 30, 0);
        var calendar = new BusinessCalendar(
            holidays: new[] { holiday, new DateTime(2024, 1, 8, 15, 0, 0) },
            workingDayOverrides: new[] { new DateTime(2024, 1, 6, 7, 0, 0) });

        calendar.IsBusinessDay(new DateTime(2024, 1, 6)).ShouldBeTrue();
        calendar.IsBusinessDay(new DateTime(2024, 1, 7)).ShouldBeFalse();
        calendar.IsBusinessDay(new DateTime(2024, 1, 8)).ShouldBeFalse();
        calendar.IsBusinessDay(new DateTime(2024, 1, 9)).ShouldBeTrue();
    }
    /// <summary>
    /// 验证具有不同时间部分的重复节假日和补班日期按自然日归一化。
    /// </summary>
    [Fact]
    public void Constructor_NormalizesRepeatedHolidayAndWorkingOverrideDates()
    {
        var holiday = new DateTime(2024, 1, 8);
        var saturday = new DateTime(2024, 1, 6);
        var calendar = new BusinessCalendar(
            holidays: new[] { holiday.AddHours(8), holiday.AddHours(18), holiday.AddHours(8) },
            workingDayOverrides: new[] { saturday.AddHours(7), saturday.AddHours(19), saturday.AddHours(7) });

        calendar.IsBusinessDay(holiday).ShouldBeFalse();
        calendar.IsBusinessDay(holiday.AddHours(23)).ShouldBeFalse();
        calendar.IsBusinessDay(saturday).ShouldBeTrue();
        calendar.IsBusinessDay(saturday.AddHours(23)).ShouldBeTrue();
    }
    /// <summary>
    /// 验证正负业务日偏移跳过节假日并保留时间分量和 Kind。
    /// </summary>
    [Fact]
    public void AddBusinessDays_SkipsHolidayAndPreservesTimeAndKind()
    {
        var calendar = new BusinessCalendar(holidays: new[] { new DateTime(2024, 1, 8) });
        var friday = new DateTime(2024, 1, 5, 14, 25, 30, DateTimeKind.Utc).AddTicks(9876);
        var tuesday = new DateTime(2024, 1, 9, 14, 25, 30, DateTimeKind.Utc).AddTicks(9876);

        var forward = calendar.AddBusinessDays(friday, 1);
        forward.Ticks.ShouldBe(tuesday.Ticks);
        forward.Kind.ShouldBe(friday.Kind);
        forward.TimeOfDay.ShouldBe(friday.TimeOfDay);
        (forward.Ticks % TimeSpan.TicksPerMillisecond).ShouldBe(friday.Ticks % TimeSpan.TicksPerMillisecond);

        var backward = calendar.AddBusinessDays(tuesday, -1);
        backward.Ticks.ShouldBe(friday.Ticks);
        backward.Kind.ShouldBe(tuesday.Kind);
        backward.TimeOfDay.ShouldBe(tuesday.TimeOfDay);
        (backward.Ticks % TimeSpan.TicksPerMillisecond).ShouldBe(tuesday.Ticks % TimeSpan.TicksPerMillisecond);
    }
    /// <summary>
    /// 验证零业务日偏移返回原始值并保持 Kind 和时间分量。
    /// </summary>
    [Fact]
    public void AddBusinessDays_ZeroReturnsOriginalDateTime()
    {
        var calendar = new BusinessCalendar();
        var original = new DateTime(2024, 1, 6, 9, 8, 7, DateTimeKind.Local);

        var result = calendar.AddBusinessDays(original, 0);

        result.ShouldBe(original);
        result.Kind.ShouldBe(DateTimeKind.Local);
        result.TimeOfDay.ShouldBe(original.TimeOfDay);
    }
    /// <summary>
    /// 验证补班日期可参与正向和反向业务日偏移。
    /// </summary>
    [Fact]
    public void AddBusinessDays_UsesWorkingDayOverrideInBothDirections()
    {
        var calendar = new BusinessCalendar(
            holidays: new[] { new DateTime(2024, 1, 8) },
            workingDayOverrides: new[] { new DateTime(2024, 1, 6) });
        var friday = new DateTime(2024, 1, 5, 10, 0, 0);
        var tuesday = new DateTime(2024, 1, 9, 10, 0, 0);

        calendar.AddBusinessDays(friday, 1).ShouldBe(new DateTime(2024, 1, 6, 10, 0, 0));
        calendar.AddBusinessDays(tuesday, -1).ShouldBe(new DateTime(2024, 1, 6, 10, 0, 0));
    }
    /// <summary>
    /// 验证半开日期区间使用补班规则并在反向输入时返回负数。
    /// </summary>
    [Fact]
    public void CountBusinessDays_UsesHalfOpenRangeAndSignedReverseCount()
    {
        var calendar = new BusinessCalendar(
            holidays: new[] { new DateTime(2024, 1, 8, 17, 0, 0) },
            workingDayOverrides: new[] { new DateTime(2024, 1, 6, 9, 0, 0) });
        var start = new DateTime(2024, 1, 5, 23, 0, 0);
        var end = new DateTime(2024, 1, 9, 1, 0, 0);

        calendar.CountBusinessDays(start, end).ShouldBe(2);
        calendar.CountBusinessDays(end, start).ShouldBe(-2);
        var nextDate = new DateTime(2024, 1, 6);
        calendar.CountBusinessDays(start, nextDate).ShouldBe(1);
        calendar.CountBusinessDays(nextDate, start).ShouldBe(-1);
        calendar.CountBusinessDays(start, new DateTime(2024, 1, 5)).ShouldBe(0);
    }
    /// <summary>
    /// 验证一周七天均为休息日时在构造阶段明确拒绝。
    /// </summary>
    [Fact]
    public void Constructor_RejectsAllSevenRestDays()
    {
        var allDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>();

        Assert.Throws<ArgumentException>(() => new BusinessCalendar(restDays: allDays));
    }
    /// <summary>
    /// 验证非法星期值在构造时以周休息日参数名拒绝。
    /// </summary>
    /// <param name="day">非法星期枚举值。</param>
    [Theory]
    [InlineData((DayOfWeek)(-1))]
    [InlineData((DayOfWeek)7)]
    [InlineData((DayOfWeek)int.MinValue)]
    [InlineData((DayOfWeek)int.MaxValue)]
    public void Constructor_RejectsInvalidRestDayValues(DayOfWeek day)
    {
        var exception = Assert.Throws<ArgumentException>(() => new BusinessCalendar(restDays: new[] { day }));

        exception.ParamName.ShouldBe("restDays");
    }
    /// <summary>
    /// 验证日期运算超出 DateTime 上下界时传播范围异常。
    /// </summary>
    [Fact]
    public void AddBusinessDays_PropagatesDateTimeBoundaryOverflow()
    {
        var calendar = new BusinessCalendar();

        Assert.Throws<ArgumentOutOfRangeException>(() => calendar.AddBusinessDays(DateTime.MaxValue, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => calendar.AddBusinessDays(DateTime.MinValue, -1));
    }
}
