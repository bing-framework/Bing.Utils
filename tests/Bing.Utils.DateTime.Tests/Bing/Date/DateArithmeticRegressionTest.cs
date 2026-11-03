using System.Linq;
using Bing.Date.DateUtils;
using NodaTime;

namespace Bing.Date;

/// <summary>星期枚举、日历周期与数值边界的缺陷回归。</summary>
public class DateArithmeticRegressionTest
{
    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Weekdays_InvalidInput_FailsBeforeEnumeration(int invalid)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => DayOfWeekCalc.GetDaysBetween(DayOfWeek.Monday, (DayOfWeek)invalid));
        Should.Throw<ArgumentOutOfRangeException>(() => DayOfWeekCalc.GetDaysBetween((DayOfWeek)invalid, DayOfWeek.Monday));
        DayOfWeekCalc.TryDaysBetween((DayOfWeek)invalid, DayOfWeek.Monday, out var days).ShouldBeFalse();
        days.ShouldBe(0);
        Should.Throw<ArgumentOutOfRangeException>(() => DayOfWeekCalc.AddDays((DayOfWeek)invalid, 1));
    }

    [Fact]
    public void Weekdays_AllPairs_AreFiniteAndOrdered()
    {
        for (var start = 0; start < 7; start++)
        for (var end = 0; end < 7; end++)
        {
            var count = (end - start + 7) % 7 + 1;
            var expected = Enumerable.Range(0, count).Select(i => (DayOfWeek)((start + i) % 7)).ToArray();
            DayOfWeekCalc.GetDaysBetween((DayOfWeek)start, (DayOfWeek)end).Take(8).ToArray().ShouldBe(expected);
            DayOfWeekCalc.GetDaysBetween((DayOfWeek)start, (DayOfWeek)end, false).Take(8).ToArray()
                .ShouldBe(expected.Skip(1).Take(Math.Max(0, count - 2)).ToArray());
        }
    }

    [Fact]
    public void CalendarUnits_YearsAndNegation_UseCalendarArithmetic()
    {
        2.Years().From(new DateTime(2024, 1, 15)).ShouldBe(new DateTime(2026, 1, 15));
        (-1.Month()).From(new DateTime(2024, 3, 31)).ShouldBe(new DateTime(2024, 2, 29));
    }

    [Fact]
    public void PeriodConversions_PreserveCalendarAndAllFixedComponents()
    {
        var period = new PeriodBuilder { Years = 2, Months = 3, Weeks = 1, Days = 2, Hours = 4, Minutes = 5, Seconds = 6, Milliseconds = 7, Ticks = 8 }.Build();
        DateTimeSpan span = period;
        span.Years.ShouldBe(2);
        span.Months.ShouldBe(3);
        span.TimeSpan.ShouldBe(new TimeSpan(9, 4, 5, 6, 7).Add(TimeSpan.FromTicks(8)));
        Period restored = span;
        restored.Years.ShouldBe(2);
        restored.Months.ShouldBe(3);
        ((DateTimeSpan)restored).ShouldBe(span);
        Duration duration = span;
        duration.ShouldBe(span.AsDuration());
        duration.ToTimeSpan().ShouldBe((TimeSpan)span);
    }

    [Theory]
    [InlineData(199, 1)]
    [InlineData(-199, -1)]
    public void FixedPeriod_ConvertsWholeDurationAndTruncatesNanoseconds(long nanoseconds, long ticks)
    {
        var period = Period.FromHours(2) + Period.FromDays(2);
        period.AsTimeSpan().ShouldBe(TimeSpan.FromHours(50));
        period.AsDuration().ShouldBe(Duration.FromHours(50));
        Period.FromNanoseconds(nanoseconds).AsTimeSpan().Ticks.ShouldBe(ticks);
    }

    [Fact]
    public void FixedPeriod_WithCalendarUnits_RejectsAmbiguousDuration()
    {
        foreach (var period in new[] { Period.FromYears(1), Period.FromMonths(1), Period.FromMonths(-1) + Period.FromHours(1) })
        {
            Should.Throw<InvalidOperationException>(() => period.AsTimeSpan());
            Should.Throw<InvalidOperationException>(() => period.AsDuration());
        }
    }

    [Fact]
    public void UnitMultiplication_DoesNotWrapToSmallOffsets()
    {
        Should.Throw<OverflowException>(() => 1431655766.Quarters());
        Should.Throw<OverflowException>(() => 1431655766.AsPeriodOfQuarters());
        Should.Throw<OverflowException>(() => DateTimeCalc.OffsetByQuarters(new DateTime(2024, 1, 1), 1431655766));
        Should.Throw<OverflowException>(() => DateTimeCalc.OffsetByYears(new DateTime(2024, 1, 1), 1073741824, DateTimeOffsetOptions.Relatively));
        Should.Throw<OverflowException>(() => { TimeSpan ignored = new DateTimeSpan(11767034, 0, TimeSpan.Zero); });
        1000000.Weeks().TimeSpan.Ticks.ShouldBe(7000000L * TimeSpan.TicksPerDay);
        Should.Throw<OverflowException>(() => -new DateTimeSpan(int.MinValue, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("zh-CN")]
    [InlineData("ar-SA")]
    [InlineData("th-TH")]
    public void WeekDefaults_PreserveFirstDaySundayAndCurrentCalendar(string cultureName)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            var culture = System.Globalization.CultureInfo.GetCultureInfo(cultureName);
            System.Globalization.CultureInfo.CurrentCulture = culture;
            var date = new DateTime(2024, 12, 31);
            date.GetWeekOfYear().ShouldBe(culture.Calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday));
            date.GetWeekOfYear(DayOfWeek.Monday).ShouldBe(culture.Calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday));
            date.GetWeekOfYear(System.Globalization.CalendarWeekRule.FirstFourDayWeek)
                .ShouldBe(culture.Calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData(613566757)]
    [InlineData(-613566757)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void NodaWeeks_OutOfRangeCannotWrapIntoSmallDuration(int weeks)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => weeks.AsDurationOfWeeks());
    }

    [Fact]
    public void NodaWeeks_LargeRepresentableDurationKeepsAllDays()
    {
        2000000.AsDurationOfWeeks().ShouldBe(Duration.FromDays(14000000));
    }
}
