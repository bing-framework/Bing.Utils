using Bing.Date.DateUtils;

namespace Bing.Date;

/// <summary>
/// 验证日期公开入口的星期校验和异常参数契约。
/// </summary>
public class DatePublicInputContractTest
{
    /// <summary>
    /// 验证整数和枚举重载均拒绝非法星期，不把输入转换成默认星期。
    /// </summary>
    /// <param name="invalid">非法星期值。</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void WeekdayCalculators_InvalidValue_ReportPublicParameter(int invalid)
    {
        var date = new DateTime(2024, 1, 1);
        var weekday = (DayOfWeek)invalid;
        Action[] calls =
        {
            () => DateTimeCalc.OffsetByWeek(2024, 1, 1, weekday),
            () => DateTimeCalc.OffsetByWeek(2024, 1, 1, invalid),
            () => DateTimeCalc.OffsetByWeekBefore(date, weekday),
            () => DateTimeCalc.OffsetByWeekAfter(date, weekday),
            () => DateTimeCalc.OffsetOfDayOfWeek(date, weekday, -1),
            () => DateTimeCalc.OffsetOfDayOfWeek(date, weekday, 0),
            () => DateTimeCalc.OffsetOfDayOfWeek(date, weekday, 1)
        };
        foreach (var call in calls)
            Should.Throw<ArgumentOutOfRangeException>(call).ParamName.ShouldBe("dayOfWeek");

        DateTimeCalc.TryOffsetByWeek(2024, 1, 1, weekday, out var enumResult).ShouldBeFalse();
        enumResult.ShouldBe(DateTime.MinValue);
        DateTimeCalc.TryOffsetByWeek(2024, 1, 1, invalid, out var intResult).ShouldBeFalse();
        intResult.ShouldBe(DateTime.MinValue);
    }

    /// <summary>
    /// 验证日期工厂透传原始星期值，并以公开参数名拒绝非法输入。
    /// </summary>
    /// <param name="invalid">非法星期值。</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void WeekdayFactories_InvalidValue_ReportPublicParameter(int invalid)
    {
        var date = new DateTime(2024, 1, 1);
        var weekday = (DayOfWeek)invalid;
        Action[] calls =
        {
            () => DateTimeFactory.CreateFirstDayOfMonth(2024, 1, weekday),
            () => DateTimeFactory.CreateFirstDayOfMonth(2024, 1, invalid),
            () => DateTimeFactory.CreateLastDayOfMonth(2024, 1, weekday),
            () => DateTimeFactory.CreateLastDayOfMonth(2024, 1, invalid),
            () => DateTimeFactory.CreateByWeek(2024, 1, weekday, 1),
            () => DateTimeFactory.CreateNextDayByWeek(date, weekday),
            () => DateTimeFactory.CreateNextDayByWeek(2024, 1, 1, weekday),
            () => DateTimeFactory.CreatePreviousDayByWeek(date, weekday),
            () => DateTimeFactory.CreatePreviousDayByWeek(2024, 1, 1, weekday)
        };
        foreach (var call in calls)
            Should.Throw<ArgumentOutOfRangeException>(call).ParamName.ShouldBe("dayOfWeek");
    }

    /// <summary>
    /// 验证首尾星期导航、跨周导航和带偏移导航使用调用方的参数名。
    /// </summary>
    /// <param name="invalid">非法星期值。</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void WeekdayNavigation_InvalidValue_ReportPublicParameter(int invalid)
    {
        var date = new DateTime(2024, 1, 1);
        var offset = new DateTimeOffset(date, TimeSpan.FromHours(8));
        var weekday = (DayOfWeek)invalid;
        Action[] calls =
        {
            () => date.FirstDayOfMonth(weekday), () => date.LastDayOfMonth(weekday),
            () => date.FirstDayOfQuarter(weekday), () => date.LastDayOfQuarter(weekday),
            () => date.FirstDayOfYear(weekday), () => date.LastDayOfYear(weekday),
            () => date.NextDayOfWeek(weekday), () => date.PreviousDayOfWeek(weekday),
            () => offset.NextDayOfWeek(weekday), () => offset.PreviousDayOfWeek(weekday)
        };
        foreach (var call in calls)
            Should.Throw<ArgumentOutOfRangeException>(call).ParamName.ShouldBe("dayOfWeek");

        Should.Throw<ArgumentOutOfRangeException>(() => date.GetWeekOfYear(weekday)).ParamName.ShouldBe("firstDayOfWeek");
        Should.Throw<ArgumentOutOfRangeException>(() => DateJudge.IsSameWeek(date, date, weekday)).ParamName.ShouldBe("firstDayOfWeek");
        Should.Throw<ArgumentOutOfRangeException>(() => DateJudge.IsSameWeek(null, date, weekday)).ParamName.ShouldBe("firstDayOfWeek");
        Should.Throw<ArgumentOutOfRangeException>(() => DateJudge.IsSameWeek(date, date.AddYears(1), weekday)).ParamName.ShouldBe("firstDayOfWeek");
    }

    /// <summary>
    /// 验证格式化和延迟枚举也在公开入口立即拒绝非法星期。
    /// </summary>
    /// <param name="invalid">非法星期值。</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void WeekdayExtensions_InvalidValue_ReportPublicParameter(int invalid)
    {
        var weekday = (DayOfWeek)invalid;
        Should.Throw<ArgumentOutOfRangeException>(() => weekday.ToChinese()).ParamName.ShouldBe("week");
        Should.Throw<ArgumentOutOfRangeException>(() => weekday.ToChinese("周")).ParamName.ShouldBe("week");
        Should.Throw<ArgumentOutOfRangeException>(() => weekday.GetDaysBetween(DayOfWeek.Monday)).ParamName.ShouldBe("from");
        Should.Throw<ArgumentOutOfRangeException>(() => DayOfWeek.Monday.GetDaysBetween(weekday)).ParamName.ShouldBe("to");
    }

    /// <summary>
    /// 验证 Try 入口在日历参数越界时返回失败及原有日期哨兵。
    /// </summary>
    /// <param name="year">年份。</param>
    /// <param name="month">月份。</param>
    /// <param name="occurrence">月内星期序号。</param>
    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(10000, 1, 1)]
    [InlineData(2024, 0, 1)]
    [InlineData(2024, 13, 1)]
    [InlineData(2024, 1, 0)]
    [InlineData(2024, 1, 6)]
    public void TryOffsetByWeek_InvalidCalendarInput_ReturnsFalse(int year, int month, int occurrence)
    {
        DateTimeCalc.TryOffsetByWeek(year, month, occurrence, DayOfWeek.Monday, out var result).ShouldBeFalse();
        result.ShouldBe(DateTime.MinValue);
    }

    /// <summary>
    /// 验证合法星期在各入口仍得到固定日历日期，并保留时间精度和偏移。
    /// </summary>
    [Fact]
    public void WeekdayEntrypoints_AllSevenValues_KeepCalendarResults()
    {
        var firstDays = new[] { 7, 1, 2, 3, 4, 5, 6 };
        var nextDays = new[] { 7, 8, 2, 3, 4, 5, 6 };
        var previousDays = new[] { 31, 25, 26, 27, 28, 29, 30 };
        var date = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddTicks(123);
        var offset = new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Unspecified), TimeSpan.FromHours(8));
        for (var i = 0; i < 7; i++)
        {
            var weekday = (DayOfWeek)i;
            DateTimeFactory.CreateByWeek(2024, 1, weekday, 1).ShouldBe(new DateTime(2024, 1, firstDays[i]));
            DateTimeCalc.TryOffsetByWeek(2024, 1, 1, weekday, out var found).ShouldBeTrue();
            found.ShouldBe(new DateTime(2024, 1, firstDays[i]));
            var next = date.NextDayOfWeek(weekday);
            next.Ticks.ShouldBe(new DateTime(2024, 1, nextDays[i], 12, 0, 0).Ticks + 123);
            next.Kind.ShouldBe(DateTimeKind.Utc);
            date.PreviousDayOfWeek(weekday).Ticks.ShouldBe(new DateTime(2023, 12, previousDays[i], 12, 0, 0).Ticks + 123);
            var nextOffset = offset.NextDayOfWeek(weekday);
            nextOffset.Ticks.ShouldBe(next.Ticks);
            nextOffset.Offset.ShouldBe(offset.Offset);
        }
    }

    /// <summary>
    /// 验证月份越界异常不泄露内部帮助方法的参数名。
    /// </summary>
    /// <param name="months">超出日期范围的月数。</param>
    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void OffsetByMonths_OutOfRange_ReportsMonths(int months)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => DateTimeCalc.OffsetByMonths(new DateTime(2024, 1, 1), months, DateTimeOffsetOptions.Relatively))
            .ParamName.ShouldBe("months");
    }

    /// <summary>
    /// 验证转发到月计算的公开入口仍报告各自的参数名。
    /// </summary>
    /// <param name="options">偏移选项。</param>
    [Theory]
    [InlineData(DateTimeOffsetOptions.Absolute)]
    [InlineData(DateTimeOffsetOptions.Relatively)]
    public void CalendarOffsets_OutOfRange_ReportOriginalParameters(DateTimeOffsetOptions options)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => DateTimeCalc.OffsetByMonths(DateTime.MaxValue, 1, options)).ParamName.ShouldBe("months");
        Should.Throw<ArgumentOutOfRangeException>(() => DateTimeCalc.OffsetByQuarters(DateTime.MaxValue, 1, options)).ParamName.ShouldBe("quarters");
        Should.Throw<ArgumentOutOfRangeException>(() => DateTimeCalc.OffsetByYears(DateTime.MaxValue, 1, options)).ParamName.ShouldBe("years");
    }

    /// <summary>
    /// 验证日期工厂的出现序号异常不暴露内部的 weekAtMonth 参数名。
    /// </summary>
    /// <param name="occurrence">越界的出现序号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void CreateByWeek_InvalidOccurrence_ReportsOccurrence(int occurrence)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => DateTimeFactory.CreateByWeek(2024, 1, DayOfWeek.Monday, occurrence)).ParamName.ShouldBe("occurrence");
    }
}
